using Microsoft.Extensions.Logging;
using SlnUtils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace SolutionValidator.ProjectReferences
{
    internal class YoloCommand : ICommand
    {
        private readonly ProjectLoader _projectLoader;
        private readonly ILogger<YoloCommand> _logger;

        public YoloCommand(ILogger<YoloCommand> logger, ProjectLoader projectLoader)
        {
            _projectLoader = projectLoader;
            _logger = logger;
        }

        internal CommandResult Run(YoloOptions options)
        {
            string projectSearchPattern = Path.Combine(options.RootDirectory, "**/*.csproj");

            var projectExcludePatterns = new List<string>(options.ExcludePatterns);
            projectExcludePatterns.Add("**/RoslynAnalyzers.Vsix.csproj");
            projectExcludePatterns.Add("**/QTGConfig/*");
            projectExcludePatterns.Add("QTG/ERTSampleAlpha/*");

            _logger.LogInformation("Attempting to find projects using [{seachPattern}]. Excluding: [{excludePatterns}]",
                projectSearchPattern, string.Join(", ", projectExcludePatterns));

            var projectMasterList = FileUtils
                .FindFiles(projectSearchPattern, projectExcludePatterns.ToArray())
                .Select(path => new FilePath(path))
                .ToList();

            foreach (var projectPath in projectMasterList)
            {
                _projectLoader.TryGetProject(projectPath, out ProjectDetails projectDetails);
                projectDetails.UpdateProjectReferences(projectMasterList.ToDictionary(p => p.Name), false);
            }

            string solutionSearchPattern = Path.Combine(options.RootDirectory, "**/*.sln");
            var solutionExcludePatterns = new List<string>(options.ExcludePatterns);
            solutionExcludePatterns.Add("**/QTGQuantBasics/Alea/Security/*");
            solutionExcludePatterns.Add("**/ARP.RoslynAnalyzers.sln");

            _logger.LogInformation("Attempting to find solutions using [{seachPattern}]. Excluding: [{excludePatterns}]",
               solutionSearchPattern, string.Join(", ", solutionExcludePatterns));

            var solutions = FileUtils
                .FindFiles(solutionSearchPattern, solutionExcludePatterns.ToArray())
                .ToList();

            var projectsByName = projectMasterList.ToDictionary(p => p.Name);
            foreach (var solution in solutions)
            {
                SlnFile solutionFile = SlnFile.Read(solution);
                bool solutionUpdated = false;
                foreach (var keyValuePair in solutionFile.GetProjectsInSolutionByName())
                {
                    // This is "free" because we've cached the value
                    _projectLoader.TryGetProject(keyValuePair.Value.FilePath, out ProjectDetails projectDetails);

                    if (projectsByName.TryGetValue(keyValuePair.Key, out FilePath projectOnDisk))
                    {
                        if (keyValuePair.Value.FilePath != projectOnDisk)
                        {
                            _logger.LogWarning("Updating path for project {projectName}", projectOnDisk);

                            solutionFile.RemoveProject(keyValuePair.Value.SlnProject.FilePath);
                            solutionFile.TryAddProject(projectDetails.Project, null, out var _);
                            solutionUpdated = true;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Removing project which no longer exists");
                        solutionFile.RemoveProject(keyValuePair.Value.SlnProject.FilePath);
                        solutionUpdated = true;
                    }
                }
                if (solutionUpdated)
                {
                    solutionFile.Write();
                }
            }

            return CommandResult.Success;
        }
    }
}
