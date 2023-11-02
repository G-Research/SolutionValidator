using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace SolutionValidator.Tests
{
    public class SolutionFinderTests : BaseFixture
    {
        [Fact]
        public void CorrectlyFindSolutionsInSearchPath()
        {
            var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var results = SolutionFinder.FindSolutions(Path.Combine(testDirectory, "test-cases"), "**/*.sln").ToList();

            results.Should().NotBeEmpty();
            results.Count.Should().Be(9);
        }

        [Fact]
        public void CorrectlyFindSolution_WithRelativePath()
        {
            var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var results = SolutionFinder.FindSolutions(testDirectory, "./test-cases/test-frameworks/test-frameworks.sln").ToList();

            results.Count.Should().Be(1);
        }

        [Fact]
        public void CanFindSolutions_WithAbsolutePaths()
        {
            var testDirectory = Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            var results = SolutionFinder.GetSolutions(new[] { Path.Combine(testDirectory, "test-cases", "**/*.sln") });
            results.Count.Should().Be(9);
        }

        [Fact]
        public void CannotFindSlnsInBinAndObjDirectories()
        {
            var testDirectory = Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            var binDir = Path.Combine(testDirectory, "solution-finder-tests", "bin");
            Directory.CreateDirectory(binDir);
            File.Create(Path.Combine(binDir, "ShouldNOTFindThisSolution.sln"));
            var objDir = Path.Combine(testDirectory, "solution-finder-tests", "obj");
            Directory.CreateDirectory(objDir);
            File.Create(Path.Combine(objDir, "ShouldNOTFindThisSolution.sln"));
            var otherDir = Path.Combine(testDirectory, "solution-finder-tests", "other");
            Directory.CreateDirectory(otherDir);
            File.WriteAllText(Path.Combine(otherDir, "ShouldFindThisSolution.sln"),
                @"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 15
VisualStudioVersion = 15.0.26124.0
MinimumVisualStudioVersion = 15.0.26124.0");

            var results = SolutionFinder.GetSolutions(new[] { Path.Combine(testDirectory, "solution-finder-tests", "**/*.sln") });
            results.Count.Should().Be(1);
        }
    }
}
