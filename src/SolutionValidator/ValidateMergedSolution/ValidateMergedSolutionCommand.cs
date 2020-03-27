using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using SlnUtils;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SolutionValidator.ValidateMergedSolution
{
    public class ValidateMergedSolutionCommand : ICommand
    {
        private readonly ILogger _logger;

        public ValidateMergedSolutionCommand(ILogger<ValidateMergedSolutionCommand> logger)
        {
            _logger = logger;
        }

        public static HashSet<string> GetProjects(MergedSolutionOptions options)
        {
            var mergedSolution = new FilePath(options.MergedSolution);
            HashSet<string> projectSuperset = new HashSet<string>();
            HashSet<string> seen = new HashSet<string>();

            foreach (var solution in SolutionFinder.GetSolutions(options.Solutions, options.ExcludePatterns.ToArray()))
            {
                if (solution == mergedSolution)
                {
                    Log.Information("Skipping current solution for determining project set.", solution);
                    continue;
                }

                SlnFile slnFile = solution.SlnFile;
                if (options.SolutionTags.Any())
                {
                    var extendedProperties = slnFile.Sections.SingleOrDefault(s => s.Id == "ExtendedSolutionProperties");
                    if (extendedProperties == null)
                    {
                        Log.Debug("Cannot find ExtendedSolutionProperties section of {solutionFile}", solution);
                        continue;
                    }

                    if (!extendedProperties.Properties.Keys.Contains("SolutionTags"))
                    {
                        Log.Information("Cannot find solution tags in ExtendedSolutionProperties section of {solutionFile}", solution);
                        continue;
                    }

                    var tags = extendedProperties.Properties["SolutionTags"].Split(',');
                    bool tagFound = false;
                    foreach (var tag in tags)
                    {
                        if (options.SolutionTags.Contains(tag))
                        {
                            Log.Debug("Found SolutionTag '{tag}' matching filter in {solutionFile}", tag, solution);
                            tagFound = true;
                            break;
                        }
                    }

                    if (!tagFound)
                    {
                        Log.Debug("Cannot find SolutionTag matching filter in {solutionFile}", solution);
                        continue;
                    }
                }

                Log.Information("Loaded the solution file for '{solutionPath}'", solution);

                foreach (var project in slnFile.Projects.Where(p => p.TypeGuid != ProjectTypeGuids.SolutionFolderGuid))
                {
                    var fullPath = Path.GetFullPath(Path.Combine(slnFile.BaseDirectory, project.FilePath));
                    if (!options.SharedOnly || seen.Contains(fullPath))
                    {
                        projectSuperset.Add(fullPath);
                    }
                    // Mark the project as seen so if we are in SharedOnly mode and we see it again, it will be included in the final solution
                    seen.Add(fullPath);
                }
            }

            if (options.ExcludeProjectsRegex != null)
            {
                var regex = new Regex(options.ExcludeProjectsRegex);
                projectSuperset.RemoveWhere(regex.IsMatch);
            }
            return projectSuperset;
        }

        public CommandResult Run(ValidateMergedSolutionOptions options)
        {
            HashSet<string> projectSuperset = GetProjects(options);
            if (projectSuperset.Count == 0)
            {
                _logger.LogError("No projects found to validate merged solution against.");
                return CommandResult.Failure;
            }

            SolutionFile mergedSolution = SolutionFileHelper.LoadSolution(options.MergedSolution);
            HashSet<string> projectsInSolution = mergedSolution.GetMsBuildProjectsInSolution().Select(p => Path.GetFullPath(p.AbsolutePath)).ToHashSet();

            bool isSuperSet = projectSuperset.IsSubsetOf(projectsInSolution);
            bool exactMatch = isSuperSet && projectSuperset.Count == projectsInSolution.Count;

            bool valid = options.Strict ? exactMatch : isSuperSet;

            if (valid)
            {
                if (exactMatch)
                {
                    _logger.LogInformation("Merged Solution exactly matches project list of supplied solutions");
                }
                else
                {
                    projectsInSolution.ExceptWith(projectSuperset);
                    _logger.LogInformation("Merged Solution is superset of supplied solutions but does not match exactly. AdditionalProjects = {additionalProjects}",
                        string.Join("", projectsInSolution.Select(projectName => $"\n* {projectName}")));
                }

                return CommandResult.Success;
            }
            else
            {
                if (isSuperSet)
                {
                    projectsInSolution.ExceptWith(projectSuperset);
                    _logger.LogError("Merged solution is superset but does not overlap project list in supplied projects. AdditionalProjects = {additionalProjects}",
                        string.Join("", projectsInSolution.Select(projectName => $"\n* {projectName}")));
                }
                else
                {
                    projectSuperset.ExceptWith(projectsInSolution);
                    _logger.LogError("Merged solution is NOT a superset of supplied solutions. Missing Projects = {missingProjects}",
                        string.Join("", projectSuperset.Select(projectName => $"\n* {projectName}")));
                }

                StringBuilder fixCommandLine = new StringBuilder("dotnet solution-validator fix-merged-solution");
                fixCommandLine.Append($" --merged-solution {options.MergedSolution}");
                fixCommandLine.Append($" --solutions {string.Join(" ", options.Solutions.Select(s => $"\"{s}\""))}");

                if (options.ExcludePatterns.Any())
                {
                    fixCommandLine.Append($" --exclude-patterns {string.Join(" ", options.ExcludePatterns.Select(p => $"\"{p}\""))}");
                }

                if (options.SolutionTags.Any())
                {
                    fixCommandLine.Append($" --solution-tags {string.Join(" ", options.SolutionTags)}");
                }

                if (options.Strict)
                {
                    fixCommandLine.Append($" --strict");
                }

                if (options.SharedOnly)
                {
                    fixCommandLine.Append($" --shared-only");
                }

                if (options.ExcludeProjectsRegex != null)
                {
                    fixCommandLine.Append($" --exclude-projects-regex \"{options.ExcludeProjectsRegex}\"");
                }

                _logger.LogError("To fix validation failures run: '{fixCommandLine}' optionally with --regenerate-no-test-solution ", fixCommandLine);

                return CommandResult.Failure;
            }
        }
    }
}
