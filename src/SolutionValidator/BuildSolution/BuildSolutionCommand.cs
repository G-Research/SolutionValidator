using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SlnUtils;
using SolutionValidator.ValidateSolutions;

namespace SolutionValidator.BuildSolution
{
    public enum SolutionCreationMode
    {
        CreateNew,
        Overwrite,
        Update
    }

    public class BuildSolutionCommand : ICommand
    {
        private readonly ILogger<BuildSolutionCommand> _logger;
        private readonly ProjectGraphBuilder _projectGraphBuilder;
        private readonly LeanSolutionValidator _leanSolutionValidator;

        public BuildSolutionCommand(ILogger<BuildSolutionCommand> logger, ProjectGraphBuilder projectGraphBuilder, LeanSolutionValidator leanSolutionValidator)
        {
            _logger = logger;
            _projectGraphBuilder = projectGraphBuilder;
            _leanSolutionValidator = leanSolutionValidator;
        }

        private IList<string> GetSolutionFolder(List<string> existingSoluionFolders, FilePath projectPath)
        {
            var maxIndex = -1;
            string solutionFolderName = null;
            foreach (var folderName in existingSoluionFolders)
            {
                var index = projectPath.NormalisedPath.LastIndexOf($"{Path.DirectorySeparatorChar}{folderName}{Path.DirectorySeparatorChar}");
                if (index > maxIndex)
                {
                    maxIndex = index;
                    solutionFolderName = folderName;
                }
            }

            if (maxIndex > 0)
            {
                return new string[] { solutionFolderName };
            }
            else
            {
                return new string[0];
            }
        }

        public CommandResult Run(BuildSolutionCommandOptions options)
        {
            string solutionFilePath = Path.GetFullPath(options.OutputFile);
            SlnFile solution = null;

            if (File.Exists(solutionFilePath))
            {
                if (options.FileMode == SolutionCreationMode.CreateNew)
                {
                    _logger.LogError("Solution file already exists. To overwrite specify --file-mode Update|Overwrite");
                    return CommandResult.Failure;
                }

                if (options.FileMode == SolutionCreationMode.Update)
                {
                    solution = SlnFile.Read(solutionFilePath);

                }
            }

            if (solution == null)
            {
                /*  Microsoft Visual Studio Solution File, Format Version 12.00
                    # Visual Studio Version 16
                    VisualStudioVersion = 16.0.29509.3
                    MinimumVisualStudioVersion = 15.0.26124.0 */

                solution = new SlnFile
                {
                    FullPath = solutionFilePath,
                    FormatVersion = "12.00",
                    ProductDescription = "Visual Studio Version 16",
                    MinimumVisualStudioVersion = "15.0.26124.0",
                    VisualStudioVersion = "16.0.29509.3"
                };
            }

            var existingSolutionFolders = solution.Projects.Where(sp => sp.TypeGuid == ProjectTypeGuids.SolutionFolderGuid).Select(p => p.Name).ToList();
            foreach (var solutionFolder in options.SolutionFolders)
            {
                if (!existingSolutionFolders.Contains(solutionFolder))
                {
                    _logger.LogInformation("Adding solution folder {solutionFolder} to solution", solutionFolder);
                    solution.Projects.Add(new SlnProject() { Name = solutionFolder, FilePath = solutionFolder, TypeGuid = ProjectTypeGuids.SolutionFolderGuid, Id = $"{{{System.Guid.NewGuid().ToString().ToUpper()}}}" });
                    existingSolutionFolders.Add(solutionFolder);
                }
            }

            var inputFiles = FileUtils.FindFiles(options.InputFiles, options.ExcludePatterns.ToArray());
            var projectGraph = _projectGraphBuilder.BuildProjectGraph(inputFiles, options.ExcludeTestProjects, options.SharedOnly);

            _logger.LogInformation("Found {projectCount} projects to add to solution ", projectGraph.Count);

            var excludeProjectsRegex = options.ExcludeProjectsRegex == default
                ? default
                : new Regex(options.ExcludeProjectsRegex);

            // Remove excluded nodes and any upstream projects
            if (options.ExcludeProjectsRegex != null)
            {
                var totalRemoved = 0;
                foreach (var node in projectGraph.Nodes)
                {
                    if (excludeProjectsRegex.IsMatch(node.FilePath))
                    {
                        var removedCount = projectGraph.RemoveSubtree(node);
                        totalRemoved += removedCount;
                        _logger.LogInformation($"Removed project {node.Name} and {removedCount - 1} other projects which depended on it");
                    }
                }
                _logger.LogInformation($"A total of {totalRemoved} projects have been removed from the graph by your --exclude-projects-regex option");
            }

            if (projectGraph.InvalidNodes.Count > 0)
            {
                _logger.LogError("Found {invalidNodeCount} invalid nodes when trying to generate project graph. Cannot build solution.", projectGraph.InvalidNodes.Count);
                return CommandResult.Failure;
            }

            var existingProjects = solution.GetProjectsInSolution();

            if (options.ExcludeTestProjects)
            {
                foreach (var extraProject in existingProjects.Keys.Where(existProjectPath => !projectGraph.Contains(existProjectPath)))
                {
                    _logger.LogInformation("Removing existing {project} which is no longer part of dependency graph.", extraProject.Name);
                    solution.RemoveProject(existingProjects[extraProject].FilePath);
                }
            }
            else
            {
                foreach (var extraProject in _leanSolutionValidator.GetSuperfluousProjects(projectGraph, existingProjects.Keys, excludeProjectsRegex))
                {
                    _logger.LogInformation("Removing existing {project} which is no longer part of dependency graph.", extraProject.Name);
                    solution.RemoveProject(existingProjects[extraProject].FilePath);
                }
            }

            foreach (var project in projectGraph.Nodes)
            {
                if (existingProjects.ContainsKey(project.FilePath))
                {
                    _logger.LogInformation("Project {project} already exists in solution.", project.Name);
                }
                else
                {
                    if (!solution.TryAddProject(project.ProjectDetails.Project, GetSolutionFolder(existingSolutionFolders, project.FilePath), out SlnProject slnProject))
                    {
                        _logger.LogError("Unable to add {project} to solution.", project.Name);
                    }
                }
            }

            _logger.LogInformation("Writing solution file to {solutionFilePath}", solutionFilePath);
            solution.Write();

            return CommandResult.Success;
        }
    }
}
