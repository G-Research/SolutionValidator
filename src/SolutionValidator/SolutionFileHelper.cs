using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Serilog;
using SlnUtils;

namespace SolutionValidator
{
    public static class SolutionFileHelper
    {
        public static Dictionary<FilePath, SlnProject> GetProjectsInSolution(this SlnFile solution)
        {
            return solution.Projects.Where(p => p.TypeGuid != ProjectTypeGuids.SolutionFolderGuid).ToDictionary(p => FilePath.Create(solution.BaseDirectory, p.FilePath));
        }

        public static Dictionary<string, (SlnProject SlnProject, FilePath FilePath)> GetProjectsInSolutionByName(this SlnFile solution)
        {
            var projects = new Dictionary<string, (SlnProject slnProject, FilePath filePath)>();
            foreach (var project in solution.Projects.Where(p => p.TypeGuid != ProjectTypeGuids.SolutionFolderGuid))
            {
                projects.Add(project.Name, new(project, FilePath.Create(solution.BaseDirectory, project.FilePath)));
            }

            return projects;
        }

        public static HashSet<string> GetProjects(IEnumerable<string> solutions, IEnumerable<string> excludePatterns)
        {
            HashSet<string> projectSuperset = new HashSet<string>();

            foreach (var solution in SolutionFinder.GetSolutions(solutions, excludePatterns.ToArray()))
            {
                SolutionFile solutionFile = LoadSolution(solution);

                Log.Information("Loaded the solution file for '{solutionPath}'", solution);

                foreach (var project in solutionFile.GetMsBuildProjectsInSolution())
                {
                    projectSuperset.Add(Path.GetFullPath(project.AbsolutePath));
                }
            }

            return projectSuperset;
        }

        public static SolutionFile LoadSolution(string solutionPath)
        {
            return LoadSolution(new FilePath(solutionPath));
        }

        public static SolutionFile LoadSolution(FilePath solutionPath)
        {
            if (!solutionPath.Exists)
            {
                throw new FileNotFoundException("Unable to load Solution as solution file does not exist.", solutionPath);
            }

            return SolutionFile.Parse(solutionPath);
        }

        public static HashSet<string> GetProjectsForSolution(Solution solution)
        {
            SolutionFile solutionFile = LoadSolution(solution);

            HashSet<string> projects = new HashSet<string>();
            foreach (var project in solutionFile.GetMsBuildProjectsInSolution())
            {
                projects.Add(project.AbsolutePath);
            }

            return projects;
        }

        public static IEnumerable<ProjectInSolution> GetMsBuildProjectsInSolution(this SolutionFile solutionFile)
        {
            return solutionFile.ProjectsInOrder.Where(p => p.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat);
        }
    }
}
