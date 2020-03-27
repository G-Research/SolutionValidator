using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace SolutionValidator.ValidateProjectPaths
{
    public class ValidateProjectPathsCommand : ICommand
    {
        private readonly ILogger<ValidateProjectPathsCommand> _logger;

        public ValidateProjectPathsCommand(ILogger<ValidateProjectPathsCommand> logger)
        {
            _logger = logger;
        }

        public CommandResult Run(ValidateProjectPathsOptions validateProjectLocationOptions)
        {
            var result = CommandResult.Success;
            var solutionFile = SolutionFileHelper.LoadSolution(validateProjectLocationOptions.Solution);

            HashSet<string> validPathRoots = validateProjectLocationOptions.ValidPathRoots.Select(path => Path.GetFullPath(path)).ToHashSet();

            foreach (var project in solutionFile.GetMsBuildProjectsInSolution())
            {
                bool validPath = false;
                foreach (string pathRoot in validPathRoots)
                {
                    if (project.AbsolutePath.StartsWith(pathRoot))
                    {
                        _logger.LogDebug("Project {projectName} has valid path root {pathRoot}", project.ProjectName, pathRoot);
                        validPath = true;
                        break;
                    }
                }

                if (!validPath)
                {
                    _logger.LogError("Cannot find a valid path root for {projectName}.", project.ProjectName);
                    result = CommandResult.Failure;
                }
            }

            return result;
        }
    }
}
