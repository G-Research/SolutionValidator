using System.Linq;
using Microsoft.Extensions.Logging;
using SolutionValidator.DependencyGraph;

namespace SolutionValidator.ValidateSolutions
{
    /// <summary>
    /// Validates that all projects in the dependency tree have validate framework dependencies
    /// </summary>
    public class FrameworkValidator : ISolutionValidator
    {
        private readonly ILogger _logger;
        private readonly ProjectTargetGraphBuilder _projectTargetGraphBuilder;

        public FrameworkValidator(ILogger<FrameworkValidator> logger, ProjectLoader projectLoader, ProjectTargetGraphBuilder projectTargetGraphBuilder)
        {
            _logger = logger;
            _projectTargetGraphBuilder = projectTargetGraphBuilder;
        }

        public ValidationResult Validate(ValidationContext validationContext)
        {
            var projects = validationContext.Solution.GetMsBuildProjectsInSolution().ToList();

            var graph = _projectTargetGraphBuilder.GenerateGraph(projects.Select(p => new FilePath(p.AbsolutePath)));

            // Any framework targets which are not in the graph are invalid.
            // This can happen if it isn't possible to construct a valid node for the target framework without bumping the target framework id
            var frameworkTargetsNotInGraph = graph.Projects.SelectMany(p => p.TargetFrameworks.Select(t => p.GetTargetId(t))).ToHashSet();
            frameworkTargetsNotInGraph.ExceptWith(graph.Keys);

            if (graph.InvalidNodes.Any() || frameworkTargetsNotInGraph.Any())
            {
                _logger.LogError("Found {invalidProjectTargetCount} invalid project targets {solution} in solution footprint ", graph.InvalidNodes.Count + frameworkTargetsNotInGraph.Count, validationContext.SolutionName);
                return ValidationResult.Failure;
            }
            else
            {
                _logger.LogInformation("Successfully validate project target graph for {solution}", validationContext.SolutionName);
                return ValidationResult.Success;
            }
        }
    }
}
