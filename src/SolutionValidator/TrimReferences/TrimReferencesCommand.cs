using System.Linq;
using Microsoft.Extensions.Logging;
using SolutionValidator.DependencyGraph;

namespace SolutionValidator.TrimReferences
{
    public class TrimReferencesCommand : ICommand
    {
        private readonly ILogger _logger;
        private readonly GraphBuilder _graphBuilder;

        public TrimReferencesCommand(ILogger<TrimReferencesCommand> logger, GraphBuilder graphBuilder)
        {
            _logger = logger;
            _graphBuilder = graphBuilder;
        }

        public CommandResult Run(TrimReferencesOptions options)
        {
            var projects = ProjectGraphBuilder.GetProjectsFromInputFiles(
                SolutionFinder.GetSolutions(options.Solutions, options.ExcludePatterns.ToArray()).Select(s => s.NormalisedPath));
            var projectGraph = _graphBuilder.GenerateGraph(projects);

            if (projectGraph.InvalidNodes.Any())
            {
                _logger.LogError("Invalid projects present in the dependency graph. Cannot safely prune references");
                return CommandResult.Failure;
            }

            foreach (var projectNode in projectGraph.Nodes)
            {
                var projectsToRemove = projectNode.References.Where(r => projectNode.TransitiveReferences.Contains(r)).Select(pn => pn.FilePath).ToHashSet();
                if (projectsToRemove.Any())
                {
                    _logger.LogInformation("Removing {projectCount} ProjectReference(s) from {projectName} which are available transitively", projectsToRemove.Count, projectNode.Name);
                    projectNode.ProjectDetails.RemoveProjectReferences(projectsToRemove);
                }
            }

            return CommandResult.Success;
        }
    }
}
