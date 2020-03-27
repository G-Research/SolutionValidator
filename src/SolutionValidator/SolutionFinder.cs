using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing;
using Serilog;
using SlnUtils;

namespace SolutionValidator
{

    public class SolutionFinder
    {
        // Currently only used in tests!!
        public static IEnumerable<string> FindSolutions(string baseDirectoryPath, string searchPattern)
        {
            var baseDirectoryInfo = new DirectoryInfo(Path.GetFullPath(baseDirectoryPath));
            var baseDirectoryInfoWrapper = new Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper(baseDirectoryInfo, false);

            if (!baseDirectoryInfo.Exists)
            {
                throw new DirectoryNotFoundException($"Unable to find specified base directory {baseDirectoryPath}");
            }

            var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            matcher.AddExclude("**/bin/*");
            matcher.AddExclude("**/obj/*");
            matcher.AddInclude(searchPattern);

            var result = matcher.Execute(baseDirectoryInfoWrapper);
            return result.Files.Select(p => p.Path);
        }

        public static IReadOnlyList<Solution> GetSolutions(IEnumerable<string> solutionPaths, params string[] excludePatterns)
        {
            Log.Logger.Information("Attempting to find solutions using: [{solutionPaths}]. Excluding: [{excludePatterns}]",
                string.Join(" ", solutionPaths), string.Join(", ", excludePatterns));
            var solutions = FileUtils.FindFiles(solutionPaths, excludePatterns);
            Log.Logger.Information("Found {solutionCount} solutions.", solutions.Count);
            return solutions.Select(s => new Solution(s)).Where(s => !SolutionMarkedAsShouldIgnore(s)).ToImmutableList();
        }

        public static bool SolutionMarkedAsShouldIgnore(Solution solution)
        {
            var extendedProperties = solution.SlnFile.Sections.SingleOrDefault(s => s.Id == "ExtendedSolutionProperties");
            if (extendedProperties == null)
            {
                Log.Debug("Cannot find ExtendedSolutionProperties section of {solutionFile}", solution);
                return false;
            }

            if (!extendedProperties.Properties.Keys.Contains("SolutionValidatorIgnored"))
            {
                Log.Debug("Cannot find SolutionValidatorIgnored property in ExtendedSolutionProperties section of {solutionFile}", solution);
                return false;
            }

            var solutionIgnoreProperty = extendedProperties.Properties["SolutionValidatorIgnored"];

            bool shouldIgnoreTheSolution = solutionIgnoreProperty.ToLower() == "true";

            Log.Warning("Solution {solutionPath} is marked as 'SolutionValidatorIgnored' and will be skipped.", solution);

            return shouldIgnoreTheSolution;
        }
    }
}
