using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.Extensions.FileSystemGlobbing;
using Serilog;

namespace SolutionValidator
{
    public static class FileUtils
    {
        public static IReadOnlyList<string> FindFiles(IEnumerable<string> filePaths, params string[] excludePatterns)
        {
            Log.Logger.Information("Attempting to find files for given paths.");
            HashSet<string> files = new HashSet<string>();

            foreach (var filePath in filePaths)
            {
                foreach (var file in FindFiles(filePath, excludePatterns))
                {
                    files.Add(file);
                }
            }

            Log.Logger.Information("Found {fileCount} files.", files.Count);
            return files.ToImmutableList();
        }

        private static readonly char[] DirectorySeparators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        public static IEnumerable<string> FindFiles(string fileSearchString, params string[] excludePatterns)
        {
            int indexOfGlobChar = fileSearchString.IndexOfAny(new char[] { '?', '*', '[' });

            if (indexOfGlobChar != -1)
            {
                var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
                matcher.AddExclude("**/bin/*");
                matcher.AddExclude("**/obj/*");

                foreach (string excludePattern in excludePatterns)
                {
                    matcher.AddExclude(excludePattern);
                }

                int indexOfDirectorySeparator = fileSearchString.LastIndexOfAny(DirectorySeparators, indexOfGlobChar);
                if (indexOfDirectorySeparator == -1)
                {
                    matcher.AddInclude(fileSearchString);
                    return matcher.GetResultsInFullPath(Environment.CurrentDirectory);
                }
                else
                {

                    var searchString = fileSearchString.Substring(indexOfDirectorySeparator).TrimStart(DirectorySeparators);
                    matcher.AddInclude(searchString);
                    var baseDirectory = fileSearchString.Substring(0, indexOfDirectorySeparator);

                    return matcher.GetResultsInFullPath(Path.GetFullPath(baseDirectory));
                }
            }
            else
            {
                return new[] { Path.GetFullPath(fileSearchString) };
            }
        }
    }
}
