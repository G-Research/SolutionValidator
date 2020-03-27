using System.IO;
using System.Linq;
using System.Reflection;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace SolutionValidator.Tests
{
    public class TestProjectFinderTests
    {

        [Fact]
        public void FindTestProjects_CorrectlySearchesDirectoryStructure()
        {
            var logger = A.Fake<ILogger<TestProjectFinder>>();
            TestProjectFinder testProjectFinder = new TestProjectFinder(logger);

            var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var absolutePath = Path.Combine(testDirectory, "test-cases", "find-test-projects", "src", "A", "A.csproj");

            var testProjects = testProjectFinder.GetTestProjects(new FilePath(absolutePath)).ToList();
            testProjects.Count.Should().Be(4);

            testProjects[0].NormalisedPath.Should().EndWith($"Test{Path.DirectorySeparatorChar}A.Test.csproj");
            testProjects[1].NormalisedPath.Should().EndWith($"Test{Path.DirectorySeparatorChar}A.Integration.Test{Path.DirectorySeparatorChar}A.Integration.Test.csproj");
            testProjects[2].NormalisedPath.Should().EndWith($"A.Tests{Path.DirectorySeparatorChar}A.Test.csproj");
            testProjects[3].NormalisedPath.Should().EndWith($"tests{Path.DirectorySeparatorChar}A.Tests{Path.DirectorySeparatorChar}A.Tests.csproj");
        }

        [Fact]
        public void Fu_kYouMicrosoft()
        {
            var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var directoryToSearch = Path.Combine(testDirectory, "test-cases", "find-test-projects", "src", "A");

            Directory.EnumerateDirectories(directoryToSearch, "Test?").Count().Should().Be(1);

            DirectoryInfo directoryInfo = new DirectoryInfo(directoryToSearch);
            directoryInfo.GetDirectories("test?", new EnumerationOptions() { MatchCasing = MatchCasing.CaseInsensitive, MatchType = MatchType.Win32 }).Count().Should().Be(1);

            // WTF?
            Directory.EnumerateDirectories(directoryToSearch, "test?", new EnumerationOptions()).Count().Should().Be(0);
            Directory.EnumerateDirectories(directoryToSearch, "Test?", new EnumerationOptions() { MatchCasing = MatchCasing.CaseInsensitive }).Count().Should().Be(0);
        }
    }
}
