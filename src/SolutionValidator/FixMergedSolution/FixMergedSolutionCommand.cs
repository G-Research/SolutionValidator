using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using SlnUtils;
using SolutionValidator.DependencyGraph;

namespace SolutionValidator.FixMergedSolution
{
    public class FixMergedSolutionCommand : ICommand
    {
        private readonly ILogger _logger;
        private readonly ProjectLoader _projectLoader;
        private readonly GraphBuilder _graphBuilder;

        public FixMergedSolutionCommand(ILogger<FixMergedSolutionCommand> logger, ProjectLoader projectLoader, GraphBuilder graphBuilder)
        {
            _logger = logger;
            _projectLoader = projectLoader;
            _graphBuilder = graphBuilder;
        }

        public CommandResult Run(FixMergedSolutionOptions options)
        {
            bool solutionFixed = true;
            HashSet<string> projectSuperset = ValidateMergedSolution.ValidateMergedSolutionCommand.GetProjects(options);

            if (projectSuperset.Count == 0)
            {
                _logger.LogError("No projects found to validate merged solution against.");
                return CommandResult.Failure;
            }

            var mergedSolutionPath = Path.GetFullPath(options.MergedSolution);

            bool dirty = false;
            SlnFile slnFile = SlnFile.Read(mergedSolutionPath);
            var projectsInSolution = slnFile.Projects.ToDictionary(p => Path.GetFullPath(Path.Combine(slnFile.BaseDirectory, p.FilePath)));
            var solutionFolders = slnFile.GetSolutionFolders();

            var missingProjects = projectSuperset.Where(p => !projectsInSolution.ContainsKey(p)).ToList();
            _logger.LogInformation("Found {missingProjectCount} project(s) to be added from Solution", missingProjects.Count);

            foreach (var missingProject in missingProjects)
            {
                // Adding missing projects
                _logger.LogInformation("Merged Solution does not contain {project}. Attempting to add.", missingProject);
                bool projectAdded = false;
                if (_projectLoader.TryGetProject(missingProject, out ProjectDetails projectDetails))
                {
                    dirty = true;
                    projectAdded = slnFile.TryAddProject(projectDetails.Project, solutionFolders, out SlnProject slnProject);
                }

                if (projectAdded != true)
                {
                    solutionFixed = false;
                }
            }

            if (options.Strict)
            {
                var additionalProjects = projectsInSolution.Where(p => !projectSuperset.Contains(p.Key)).ToList();
                _logger.LogInformation("Found {additionalProjectCount} projects to be removed from Solution", additionalProjects.Count);

                foreach (var additionalProject in additionalProjects)
                {
                    // Adding missing projects
                    _logger.LogInformation("Removing additional project {project} from Merged Solution.", additionalProject.Value.FilePath);
                    if (slnFile.RemoveProject(additionalProject.Value.FilePath))
                    {
                        dirty = true;
                    }
                    else
                    {
                        _logger.LogError("Unable to remove additional project {project} from Merged Solution.", additionalProject.Value.FilePath);
                        solutionFixed = false;
                    }
                }
            }

            if (dirty)
            {
                _logger.LogInformation("Saving updated solution {solution}.", options.MergedSolution);
                slnFile.Write();
            }
            else
            {
                _logger.LogInformation("No changes required to merged solution {solution}.", options.MergedSolution);
            }

            if (solutionFixed && options.GenerateNoTestSolution)
            {
                var noTestSolution = SlnFile.Read(mergedSolutionPath);
                noTestSolution.FullPath = Path.Combine(Path.GetDirectoryName(mergedSolutionPath), Path.GetFileNameWithoutExtension(mergedSolutionPath) + "NoTests.sln");

                // Get all the top level projects which are not tests, generate a graph from those and then remove any project which is not in that set.
                var projects = noTestSolution.GetProjectsInSolution();

                var topLevelProjects = _projectLoader.GetTopLevelNonTestProjects(projects.Keys);
                // Remove any test projects
                topLevelProjects.RemoveWhere(p => p.IsTestProject);
                var nonTestProjectGraph = _graphBuilder.GenerateGraph(topLevelProjects.Select(p => p.FilePath));

                foreach (var project in projects)
                {
                    if (!nonTestProjectGraph.Contains(project.Key))
                    {
                        if (!noTestSolution.RemoveProject(project.Value.FilePath))
                        {
                            _logger.LogError("Unable to remove project from non-test Merged Solution.", project.Value.FilePath);
                        }
                    }
                }

                _logger.LogInformation("Saving regenerated no-test solution {solution}.", noTestSolution.FullPath);
                noTestSolution.Write();
            }

            return solutionFixed ? CommandResult.Success : CommandResult.Failure;
        }
    }
}
