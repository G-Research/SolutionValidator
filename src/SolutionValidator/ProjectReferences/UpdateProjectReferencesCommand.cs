using Microsoft.Extensions.Logging;
using SlnUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolutionValidator.ProjectReferences
{
    internal class UpdateProjectReferencesCommand : ICommand
    {
        private readonly ProjectLoader _projectLoader;
        private readonly ILogger _logger;

        public UpdateProjectReferencesCommand(ILogger<UpdateProjectReferencesCommand> logger, ProjectLoader projectLoader)
        {
            _projectLoader = projectLoader;
            _logger = logger;
        }

        internal CommandResult Run(UpdateProjectReferencesOptions options)
        {
            _logger.LogInformation("Attempting to find input files using: [{inputFiles}]. Excluding: [{excludePatterns}]",
                string.Join(" ", options.InputFiles), string.Join(", ", options.ExcludePatterns));

            bool validReferenceProjects = true;

            if (options.Action != ProjectAction.Remove)
            {
                foreach (var reference in options.Projects)
                {
                    if (!File.Exists(reference))
                    {
                        _logger.LogError("Unable to find project {projectPath} so will be unable to run {action} action", reference, options.Action);
                        validReferenceProjects = false;
                    }
                }
            }

            if (!validReferenceProjects)
            {
                return CommandResult.Failure;
            }

            var inputFiles = FileUtils.FindFiles(options.InputFiles, options.ExcludePatterns.ToArray());
            _logger.LogInformation("Found {fileCount} files to update.", inputFiles.Count);

            var validExtensions = new[] { ".sln", ".csproj", ".fsproj" };
            var invalidFiles = inputFiles.Where(f => !validExtensions.Contains(Path.GetExtension(f).ToLower())).ToList();
            if (invalidFiles.Count > 0)
            {
                _logger.LogError("Invalid input files found. Can only process project of solution files: {invalidFiles}", string.Join(",", invalidFiles));
            }

            bool updateError = false;
            List<FilePath> projectPaths = options.Projects.Select(p => new FilePath(p)).ToList();
            foreach (var inputFile in inputFiles)
            {
                if (inputFile.EndsWith(".sln"))
                {
                    if (!UpdateSolution(options.Action, projectPaths, inputFile))
                    {
                        _logger.LogError("Error updating Solution {solutionFile}", inputFile);
                        updateError = true;
                    }
                }
                else
                {
                    if (!UpdateProject(options.Action, projectPaths, inputFile))
                    {
                        _logger.LogError("Error updating Project {projectFile}", inputFile);
                        updateError = true;
                    }
                }

            }

            if (updateError)
            {
                return CommandResult.Failure;
            }
            return CommandResult.Success;
        }

        private bool UpdateProject(ProjectAction action, IEnumerable<FilePath> projectPaths, string projectToUpdate)
        {
            _projectLoader.TryGetProject(projectToUpdate, out ProjectDetails projectDetails);

            switch (action)
            {
                case ProjectAction.Add:
                    {
                        projectDetails.UpdateProjectReferences(projectPaths, true);
                        break;
                    }
                case ProjectAction.Update:
                    {
                        projectDetails.UpdateProjectReferences(projectPaths, false);
                        break;
                    }
                case ProjectAction.Remove:
                    {
                        var projectsToRemove = new List<FilePath>();
                        foreach (var project in projectPaths)
                        {
                            if (projectDetails.ProjectReferences.Contains(project))
                            {
                                projectsToRemove.Add(project);
                            }
                            else
                            {
                                projectDetails.ProjectReferences.Any(p => p.Name == project.Name);
                            }
                        }

                        if (projectsToRemove.Any())
                        {
                            projectDetails.RemoveProjectReferences(projectPaths);
                        }

                        break;
                    }

                default:
                    throw new InvalidOperationException($"Unsupported ProjectAction type: '{action}'");
            }

            return true;
        }
        private bool UpdateSolution(ProjectAction action, List<FilePath> projectPaths, string solutionFileToUpdate)
        {
            bool actionSuccessful = true;
            switch (action)
            {
                case ProjectAction.Add:
                case ProjectAction.Update:
                    {
                        if (!TryAddProjectsToSolution(solutionFileToUpdate, projectPaths, action))
                        {
                            actionSuccessful = false;
                        }

                        break;
                    }
                case ProjectAction.Remove:
                    {
                        SlnFile solutionFile = SlnFile.Read(solutionFileToUpdate);

                        bool solutionUpdated = false;
                        var projectsInSolution = solutionFile.GetProjectsInSolution();
                        foreach (var project in projectPaths)
                        {
                            if (projectsInSolution.ContainsKey(project))
                            {
                                _logger.LogInformation("Removing {project} from solution {solution}.", project.Name, solutionFileToUpdate);
                                solutionFile.RemoveProject(projectsInSolution[project].FilePath);
                                solutionUpdated = true;
                                continue;
                            }

                            var matchingProject = projectsInSolution.Keys.SingleOrDefault(k => k.Name == project.Name);
                            if (matchingProject != null)
                            {
                                _logger.LogInformation("Removing {project} from solution {solution}.", project.Name, solutionFileToUpdate);
                                solutionFile.RemoveProject(projectsInSolution[matchingProject].FilePath);
                                solutionUpdated = true;
                            }
                        }

                        if (solutionUpdated)
                        {
                            solutionFile.Write();
                        }
                        break;
                    }
                default:
                    throw new InvalidOperationException($"Unsupported ProjectAction type: '{action}'");
            }

            return actionSuccessful;
        }

        protected bool TryAddProjectsToSolution(string solutionPath, List<FilePath> projectPaths, ProjectAction projectAction)
        {
            SlnFile solutionFile = SlnFile.Read(solutionPath);
            bool completedSuccessfully = true;
            bool solutionUpdated = false;

            var projectsInSolution = solutionFile.GetProjectsInSolution();
            foreach (var project in projectPaths)
            {
                if (projectsInSolution.ContainsKey(project))
                {
                    _logger.LogInformation("Project {project} is already present in {solution}", project, solutionPath);
                    continue;
                }

                if (!_projectLoader.TryGetProject(project, out ProjectDetails projectDetails))
                {
                    _logger.LogInformation("Unable to load project from {inputFile}", solutionPath);
                    completedSuccessfully = false;
                }

                var matchingProject = projectsInSolution.Keys.SingleOrDefault(k => k.Name == project.Name);
                if (matchingProject != null)
                {
                    if (!matchingProject.Exists)
                    {
                        _logger.LogInformation("Project with matching name {project} exists in solution {solution} but does not exist on disk. Overwriting.", project.Name, solutionPath);
                        solutionFile.RemoveProject(matchingProject.FileName);
                        solutionFile.TryAddProject(projectDetails.Project, new List<string>(), out var _);
                        solutionUpdated = true;
                    }
                    else
                    {
                        // Should this be an error for updating too???
                        _logger.LogWarning("Project matching {project} already exists in solution {solution}.", project.Name, solutionPath);
                    }
                }
                else if (projectAction == ProjectAction.Add)
                {
                    solutionFile.TryAddProject(projectDetails.Project, new List<string>(), out var _);
                    // TODO: Check that this worked!!!
                    solutionUpdated = true;
                }
            }

            if (solutionUpdated)
            {
                solutionFile.Write();
            }

            return completedSuccessfully;
        }
    }
}
