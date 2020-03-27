using System.Text.RegularExpressions;

namespace SolutionValidator
{
    public static class ProjectExtensions
    {
        public static readonly Regex TestAssemblyRegex = new Regex("(\\.?)(?i)test(s?)(\\d?)$");

        public static bool IsTestProjectName(this string projectName)
        {
            return TestAssemblyRegex.IsMatch(projectName);
        }
    }
}
