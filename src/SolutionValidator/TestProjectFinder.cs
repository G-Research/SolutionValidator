using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace SolutionValidator
{
    public class TestProjectFinder
    {
        private readonly ILogger<TestProjectFinder> _logger;

        private readonly Dictionary<FilePath, List<FilePath>> _testProjectDictionary = new Dictionary<FilePath, List<FilePath>>();
        private readonly EnumerationOptions _enumerationOptions = new EnumerationOptions() { MatchType = MatchType.Win32, MatchCasing = MatchCasing.CaseInsensitive };
        public TestProjectFinder(ILogger<TestProjectFinder> logger)
        {
            _logger = logger;
        }

        private IEnumerable<string> GetTestProjectInDirectory(string nonTestProjectName, string baseDirectory, string searchPattern)
        {
            // I expect that all test projects will look like nonTestProjectName*Test(s)*.?sproj
            if (!Directory.Exists(baseDirectory))
            {
                yield break;
            }

            _logger.LogDebug("Searching for test projects for {nonTestProjectName} in {baseDirectory} using {searchPattern}", nonTestProjectName, baseDirectory, searchPattern);

            var directories = Directory.EnumerateDirectories(baseDirectory, searchPattern, _enumerationOptions);
            foreach (string directoryToSearch in directories)
            {
                foreach (var projectFile in Directory.EnumerateFiles(directoryToSearch, $"{nonTestProjectName}*Test*.?sproj", _enumerationOptions))
                {
                    yield return projectFile;
                }
            }
        }

        /// <summary>
        /// Simple heuristic based approach for determining which projects are test projects for the supplied project path.
        /// Look in:
        ///     1. Test(s) sub folder
        ///     2. Test(s)\{AssemblyName}.Test(s) sub folder
        ///     3. SiblingFolder with .Test(s)
        ///     4. If under src folder corresponding folder in test(s) directory hierarchy with .Test(s) appended
        /// </summary>
        /// <param name="absolutePath"></param>
        /// <returns></returns>
        public IEnumerable<FilePath> GetTestProjects(FilePath absolutePath)
        {
            if (_testProjectDictionary.TryGetValue(absolutePath, out List<FilePath> precomputedValues))
            {
                return precomputedValues;
            }

            string nonTestProjectName = Path.GetFileNameWithoutExtension(absolutePath);

            List<string> candidateTestFilePaths = new List<string>();
            var projDir = new DirectoryInfo(Path.GetDirectoryName(absolutePath));

            // 1. Look for sub directory of Test or Tests (case-insensitive)
            candidateTestFilePaths.AddRange(GetTestProjectInDirectory(nonTestProjectName, projDir.FullName, "*Test?"));

            // 2. Look for sub directory of Test if it exists
            candidateTestFilePaths.AddRange(GetTestProjectInDirectory(nonTestProjectName, Path.Combine(projDir.FullName, "Test"), $"{nonTestProjectName}*Test*"));
            candidateTestFilePaths.AddRange(GetTestProjectInDirectory(nonTestProjectName, Path.Combine(projDir.FullName, "Tests"), $"{nonTestProjectName}*Test*"));

            if (projDir.Parent != null)
            {
                var parentPath = projDir.Parent.FullName;
                // 3. Look in sibling folder
                candidateTestFilePaths.AddRange(GetTestProjectInDirectory(nonTestProjectName, parentPath, projDir.Name + ".*Test?"));

                // 4. Look in matching test folder to src directory
                int index = parentPath.LastIndexOf($"{Path.DirectorySeparatorChar}src", StringComparison.Ordinal);
                if (index != -1)
                {
                    string rootPath = parentPath.Substring(0, index + 1);
                    string relativePathToParentDirectory = parentPath.Remove(0, rootPath.Length + 3);

                    foreach (var directory in Directory.EnumerateDirectories(rootPath, "test?", _enumerationOptions))
                    {
                        var directoryToSearch = Path.Combine(directory, relativePathToParentDirectory);
                        candidateTestFilePaths.AddRange(GetTestProjectInDirectory(nonTestProjectName, directoryToSearch, nonTestProjectName + ".*Test?"));
                    }
                }
            }

            var filePaths = candidateTestFilePaths.Select(p => new FilePath(p)).ToList();
            _testProjectDictionary.Add(absolutePath, filePaths);

            return filePaths;
        }
    }
}
