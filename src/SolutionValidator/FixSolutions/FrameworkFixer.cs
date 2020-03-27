using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SlnUtils;
using SolutionValidator.DependencyGraph;
using SolutionValidator.ValidateSolutions;

namespace SolutionValidator.FixSolutions
{
    public class FrameworkFixer
    {
        private readonly ILogger _logger;
        private readonly ProjectLoader _projectLoader;
        private readonly GraphBuilder _graphBuilder;
        private readonly TestFrameworkValidator _testFrameworkValidator;
        private readonly ProjectTargetGraphBuilder _projectTargetGraphBuilder;

        public FrameworkFixer(ILogger<FrameworkFixer> logger, ProjectLoader projectLoader, GraphBuilder graphBuilder, TestFrameworkValidator testFrameworkValidator, ProjectTargetGraphBuilder projectTargetGraphBuilder)
        {
            _logger = logger;
            _projectLoader = projectLoader;
            _graphBuilder = graphBuilder;
            _testFrameworkValidator = testFrameworkValidator;
            _projectTargetGraphBuilder = projectTargetGraphBuilder;
        }

        public void FixInvalidFrameworks(SlnFile solutionFile, Dictionary<FilePath, SlnProject> projectDictionary)
        {
            var graph = _projectTargetGraphBuilder.GenerateGraph(projectDictionary.Keys);

            if (graph.InvalidNodes.Any(n => !n.ProjectDetails.Valid))
            {
                _logger.LogError("Invalid projects present in the dependency graph for {solution}. Skipping framework fixing as we cannot perform framework resolution.", solutionFile.FullPath);
                return;
            }

            // Add any missing nodes
            foreach (var nodeWithMissingFramework in graph.InvalidNodes.Where(n => !n.IsExistingProjectTarget))
            {
                _logger.LogInformation("Add '{targetFramework}' to project {project}", nodeWithMissingFramework.TargetFramework, nodeWithMissingFramework.Name);
                nodeWithMissingFramework.ProjectDetails.AddTargetFrameworks(new[] { nodeWithMissingFramework.TargetFramework });
            }

            // Remove any target frameworks from projects which yield a different target framework to the one from the project as this indicates that the current target framework is invalid
            foreach (var project in graph.Projects)
            {
                foreach (var framework in project.TargetFrameworks.ToList())
                {
                    if (!graph.Contains(project.GetTargetId(framework)))
                    {
                        project.RemoveTargetFramework(framework);
                    }
                }
            }

            // By updating the projects we have invalidated the cache and need to clear it before building any more graphs.
            _projectTargetGraphBuilder.ClearCache();
        }

        public bool AddMissingTestFrameworks(SlnFile solutionFile, Dictionary<FilePath, SlnProject> projectDictionary)
        {
            var graph = _projectTargetGraphBuilder.GenerateGraph(projectDictionary.Keys);

            if (graph.InvalidNodes.Any())
            {
                _logger.LogError("Invalid nodes present in the dependency graph for {solution}. Skipping test framework fixing as we cannot perform framework resolution.", solutionFile.FullPath);
                return false;
            }

            var referencedFrameworks = TestFrameworkValidator.GetReferencedFrameworks(graph);

            bool cacheInvalidated = false;
            foreach (var testProjectPath in projectDictionary.Keys.Where(p => p.Name.IsTestProjectName()))
            {
                _projectLoader.TryGetProject(testProjectPath, out ProjectDetails testProject);
                var missingFrameworks = _testFrameworkValidator.GetMissingTestFrameworks(testProject, referencedFrameworks);
                if (missingFrameworks.Any())
                {
                    _logger.LogInformation("Found '{missingFrameworks}' missing target frameworks {testProject}", string.Join(";", missingFrameworks), testProject.ProjectName);
                    testProject.AddTargetFrameworks(missingFrameworks);
                    cacheInvalidated = true;
                }
            }

            if (cacheInvalidated)
            {
                _projectTargetGraphBuilder.ClearCache();
            }

            return cacheInvalidated;
        }
    }
}
