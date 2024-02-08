using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SolutionValidator.DependencyGraph;
using SolutionValidator.ValidateSolutions;

namespace SolutionValidator.TrimFrameworks
{
    public class TrimFrameworksCommand : ICommand
    {
        private readonly ILogger _logger;
        private readonly ProjectTargetGraphBuilder _projectTargetGraphBuilder;
        private readonly ProjectLoader _projectLoader;

        public TrimFrameworksCommand(ILogger<TrimFrameworksCommand> logger, ProjectTargetGraphBuilder projectTargetGraphBuilder, ProjectLoader projectLoader)
        {
            _logger = logger;
            _projectTargetGraphBuilder = projectTargetGraphBuilder;
            _projectLoader = projectLoader;
        }

        private bool IsValidTestFrameworkForSet(HashSet<TargetFramework> referenceFrameworks, TargetFramework targetFramework)
        {
            if (targetFramework.FrameworkType == FrameworkType.NetCore && referenceFrameworks.Any(f => f.FrameworkType == FrameworkType.NetStandard))
            {
                return true;
            }

            return referenceFrameworks.Any(f => f.FrameworkType == targetFramework.FrameworkType && f.Version.Major == targetFramework.Version.Major && f.Version <= targetFramework.Version);
        }

        public CommandResult Run(TrimFrameworksOptions options)
        {
            // TODO: Make this better!!
            var projects = ProjectGraphBuilder.GetProjectsFromInputFiles(
                SolutionFinder.GetSolutions(options.Solutions, options.ExcludePatterns.ToArray()).Select(s => s.NormalisedPath));
            var topLevelNonTestProjects = _projectLoader.GetTopLevelNonTestProjects(projects);
            var projectGraph = _projectTargetGraphBuilder.GenerateGraph(topLevelNonTestProjects);

            if (projectGraph.InvalidNodes.Any())
            {
                _logger.LogError("Invalid projects present in the dependency graph. Cannot safely prune target frameworks.");
                return CommandResult.Failure;
            }

            // There is an edge (and in some repos not so edge) case which is hard to handle gracefully.
            // If we have TestProject B' using net471 which references a library A which only has direct root node references from E to its net5.0 target
            // then when we build the dependency graph and add in test project references we will only identify that test A' needs net5.0 and while we will
            // add in the net471 target to the library as a result of the reference from B' we won't have identified that we need A' to have matching framework for testing.
            // i.e.
            //
            // E(net5.0)   A'(net5.0)   B' (net471)
            //         \       |        /    \
            //          \      |       /      \
            //            A  (net5.0)          B (net471)
            //               (net471)
            //
            // As it stands a single pass through will not identifier framework net471 target from A'
            // You need to repeat the determination of the referenced frameworks as you continue to add in missing test (and library) targets
            // This means that while you are still finding project targets being added as part of the test determination logic you have to repeat the process.
            // Clearly it would be nice if we could make the dependency graph less crazy but that is not always possible.

            // Using graph of just the non test projects determine the referenced projects for all nodes...
            var referencedFrameworks = TestFrameworkValidator.GetReferencedFrameworks(projectGraph);

            int iterationCount = 0;
            while (AddTestProjectReferences(projects, projectGraph, referencedFrameworks))
            {
                iterationCount++;
                var updatedReferencedFrameworks = TestFrameworkValidator.GetReferencedFrameworks(projectGraph);
                if (updatedReferencedFrameworks != referencedFrameworks)
                {
                    _logger.LogWarning("Adding in test projects to the graph has changed the library references. Rerunning {AddTestProjectReferences}. Current iteration count {iterationCont}",
                        nameof(AddTestProjectReferences), iterationCount);
                }
                referencedFrameworks = updatedReferencedFrameworks;
            }

            var keys = projectGraph.Keys.ToHashSet();
            foreach (var projectPath in projects)
            {
                _projectLoader.TryGetProject(projectPath, out ProjectDetails project);

                foreach (var targetFramework in project.TargetFrameworks)
                {
                    var targetId = project.GetTargetId(targetFramework);

                    if (!keys.Contains(targetId))
                    {
                        _logger.LogInformation("Removing TargetFramework {targetFramework} from project {projectName} as it is not referenced.", targetFramework, project.ProjectName);
                        project.RemoveTargetFramework(targetFramework);
                    }
                }
            }

            return CommandResult.Success;
        }

        private bool AddTestProjectReferences(HashSet<FilePath> projects, Graph<ProjectTarget> projectGraph, Dictionary<int, HashSet<TargetFramework>> referencedFrameworks)
        {
            bool graphUpdated = false;
            // Determine set of referenced frameworks and add those nodes to the graph.
            foreach (var project in projects.Where(p => p.Name.IsTestProjectName()))
            {
                _projectLoader.TryGetProject(project, out ProjectDetails testProjectDetails);
                var requiredtestProjectFrameworks = new List<TargetFramework>();

                if (testProjectDetails.TryGetProjectUnderTest(out FilePath projectUnderTestPath)
                    && _projectLoader.TryGetProject(projectUnderTestPath, out ProjectDetails projectUnderTest)
                    && referencedFrameworks.TryGetValue(projectUnderTest.Id, out HashSet<TargetFramework> referenceFrameworks))
                {
                    foreach (var framework in testProjectDetails.TargetFrameworks)
                    {
                        if (IsValidTestFrameworkForSet(referenceFrameworks, framework))
                        {
                            requiredtestProjectFrameworks.Add(framework);
                        }
                        else
                        {
                            _logger.LogInformation("{framework} in {project} is not a required test framework", framework, testProjectDetails.ProjectName);
                        }
                    }
                }
                else
                {
                    requiredtestProjectFrameworks = testProjectDetails.TargetFrameworks.ToList();
                }

                foreach (var framework in requiredtestProjectFrameworks)
                {
                    var nodesAdded = projectGraph.AddNode(_projectTargetGraphBuilder.GetNode(framework, testProjectDetails));
                    if (nodesAdded > 0)
                    {
                        graphUpdated = true;
                        _logger.LogInformation("Added {nodesAdded} node(s) to the graph for {testProjectName}:{framework}", nodesAdded, testProjectDetails.ProjectName, framework);
                    }
                }
            }

            return graphUpdated;
        }
    }
}
