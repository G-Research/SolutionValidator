using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;
using SolutionValidator.DependencyGraph;

namespace SolutionValidator.ValidateDependencyGraph
{
    public class ValidateDependencyGraphCommand : ICommand
    {
        private readonly ILogger<ValidateDependencyGraphCommand> _logger;
        private readonly GraphBuilder _graphBuilder;
        private readonly ColourChart _colourChart;

        public ValidateDependencyGraphCommand(ILogger<ValidateDependencyGraphCommand> logger, GraphBuilder graphBuilder, ColourChart colourChart)
        {
            _logger = logger;
            _graphBuilder = graphBuilder;
            _colourChart = colourChart;
        }

        private bool TryLoadAllProjects(ValidateDependencyGraphOptions validateDependencyGraphOptions, out HashSet<FilePath> projectSuperset)
        {
            projectSuperset = new HashSet<FilePath>();
            bool allFilesProcessed = true;
            foreach (var solutionPath in validateDependencyGraphOptions.Solutions)
            {
                if (!File.Exists(solutionPath))
                {
                    _logger.LogError("Unable to file specified solution file {solutionFile}", solutionPath);
                    allFilesProcessed = false;
                    continue;
                }

                SolutionFile solution = SolutionFileHelper.LoadSolution(solutionPath);

                _logger.LogInformation("Loaded the solution file for '{solutionPath}'", solutionPath);

                foreach (var project in solution.GetMsBuildProjectsInSolution())
                {
                    projectSuperset.Add(new FilePath(project.AbsolutePath));
                }
            }

            foreach (var project in validateDependencyGraphOptions.Projects)
            {
                if (!File.Exists(project))
                {
                    _logger.LogError("Unable to file specified project file {projectFile}", project);
                    allFilesProcessed = false;
                    continue;
                }

                projectSuperset.Add(new FilePath(project));
            }

            return allFilesProcessed;
        }

        internal CommandResult Run(ValidateDependencyGraphOptions validateDependencyGraphOptions)
        {
            // TODO: Can simplify this. Can have a single option for specifying solutions of projects but lets start off simple.
            if (!TryLoadAllProjects(validateDependencyGraphOptions, out HashSet<FilePath> projectSuperset))
            {
                return CommandResult.Failure;
            }

            // Is this required option -> probably yes...
            if (!string.IsNullOrEmpty(validateDependencyGraphOptions.ColourChart))
            {
                _colourChart.AddColoursFromConfig(validateDependencyGraphOptions.ColourChart);
            }

            var graph = _graphBuilder.GenerateGraph(projectSuperset);

            if (graph.InvalidNodes.Count > 0)
            {
                _logger.LogError("Invalid nodes detected. Skipping colour validation as this is not possible without the full graph.");
                return CommandResult.Failure;
            }

            (List<ProjectNode> InvalidNodes, Dictionary<int, Colour> NodeColours) result = _graphBuilder.MatchColours(graph, _colourChart, validateDependencyGraphOptions.AddMissingColours);

            if (result.InvalidNodes.Count == 0)
            {
                _logger.LogInformation("Dependency graph is valid.");
                return CommandResult.Success;
            }
            else
            {
                _logger.LogError("Dependency graph is invalid. Breaking nodes:{invalidNodes}", string.Join("", result.InvalidNodes.Select(n => $"\n * {n.Name}")));
                return CommandResult.Failure;
            }
        }
    }
}
