using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using SolutionValidator.DependencyGraph;
using SolutionValidator.ValidateSolutions;

namespace SolutionValidator
{
    public class ProjectGraphBuilder

    {
        private readonly ILogger<ProjectGraphBuilder> _logger;
        private readonly GraphBuilder _graphBuilder;
        private readonly TestProjectFinder _testProjectFinder;

        public ProjectGraphBuilder(ILogger<ProjectGraphBuilder> logger, GraphBuilder graphBuilder, TestProjectFinder testProjectFinder)
        {
            _logger = logger;
            _graphBuilder = graphBuilder;
            _testProjectFinder = testProjectFinder;
        }

        public Graph<ProjectNode> BuildProjectGraph(IEnumerable<string> inputFiles, bool excludeTestProjects, bool sharedOnly = false)
        {
            var projectFiles = sharedOnly ? GetRepeatedProjectsFromInputFiles(inputFiles) : GetProjectsFromInputFiles(inputFiles);

            return GenerateGraph(projectFiles, excludeTestProjects);
        }

        public Graph<ProjectNode> GenerateGraph(IEnumerable<FilePath> projectFiles, bool excludeTestProjects)
        {
            var graph = _graphBuilder.GenerateGraph(projectFiles);

            if (excludeTestProjects)
            {
                return graph;
            }

            _logger.LogInformation("Adding test projects to graph.");

            var projectsToSearch = graph.Nodes.ToList();
            var additionalNodes = projectsToSearch;

            while (additionalNodes.Count > 0)
            {
                additionalNodes = new List<ProjectNode>();
                foreach (var node in projectsToSearch)
                {
                    if (!node.IsTestProject) // Only search for tests for non test projects
                    {
                        foreach (var testProject in _testProjectFinder.GetTestProjects(node.ProjectDetails.FilePath))
                        {
                            var testProjectNode = _graphBuilder.GetNode(testProject);

                            if (!LeanSolutionValidator.IsTestProjectForProjectInGraph(testProjectNode.ProjectDetails, graph))
                            {
                                _logger.LogWarning("Found a test project {testProject} by convention but it does not test any projects in the graph.", testProject);
                                continue;
                            }

                            foreach (var reference in testProjectNode.AllReferences)
                            {
                                if (!graph.Contains(reference.Id))
                                {
                                    additionalNodes.Add(reference);
                                }
                            }
                            // Add the test project we found to the graph.
                            graph.AddNode(testProjectNode);
                        }

                    }
                }

                projectsToSearch = additionalNodes;
            }

            return graph;
        }

        // TODO Move this to handle FilePaths
        public static HashSet<FilePath> GetProjectsFromInputFiles(IEnumerable<string> inputFiles)
            => GetAllProjectsFromInputFiles(inputFiles).ToHashSet();

        public static HashSet<FilePath> GetRepeatedProjectsFromInputFiles(IEnumerable<string> inputFiles)
        {
            return GetAllProjectsFromInputFiles(inputFiles)
                .GroupBy(p => p.NormalisedPath)
                .SelectMany(z => z.Skip(1).Take(1))
                .ToHashSet();
        }

        // TODO: Handle filtering of Solutions and projects!
        public static IEnumerable<FilePath> GetAllProjectsFromInputFiles(IEnumerable<string> inputFiles)
        {
            foreach (var inputFile in inputFiles)
            {
                if (inputFile.ToLower().EndsWith(".sln"))
                {
                    foreach (var project in SolutionFileHelper.LoadSolution(inputFile).GetMsBuildProjectsInSolution())
                    {
                        yield return new FilePath(project.AbsolutePath);
                    }
                }
                else if (inputFile.ToLower().EndsWith("proj"))
                {
                    var projectPath = Path.GetFullPath(inputFile);
                    if (!File.Exists(projectPath))
                    {
                        throw new FileNotFoundException($"Unable to find input file: {inputFile}");
                    }

                    yield return new FilePath(inputFile);
                }
            }
        }
    }
}
