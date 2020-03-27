using System.IO;
using System.Reflection;

namespace SolutionValidator.Tests
{
    public class FileUtils
    {
        public static string WriteToTempTestFile(string contents)
        {
            // Maybe nunit and TestContext is a better fit.
            var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string fileName = Path.GetTempFileName();
            string fullPath = Path.Combine(testDirectory, fileName);
            File.WriteAllText(fullPath, contents);
            return fullPath;
        }

    }
}
