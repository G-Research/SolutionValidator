using FluentAssertions;
using SolutionValidator.DependencyGraph;
using Xunit;

namespace SolutionValidator.Tests
{
    public class ProjectExtensionTests : BaseFixture
    {
        [Theory]
        [InlineData("MyApp.Test")]
        [InlineData("MyApp.Tests")]
        [InlineData("MyApp.SlowTest")]
        [InlineData("MyApp.SlowTests1")]
        [InlineData("MyAppTest")]
        [InlineData("MyAppTestMelon", false)]
        public void CorrectlyMatchesTestProjectNames(string projectName, bool expected = true)
        {
            projectName.IsTestProjectName().Should().Be(expected);
        }

        [Fact]
        public void GetIdReturnsSensibleId()
        {
            ProjectDetails project = ProjectDetails.GetInvalidNode(9999, "SomeMadeUpPath.csproj");

            project.GetTargetId(new TargetFramework("netcoreapp3.1")).Should().BeGreaterThan(0);
            project.GetTargetId(new TargetFramework("net471")).Should().BeGreaterThan(0);
        }

        [Fact]
        public void DifferentFrameworkTypesGenerateDifferentIds()
        {
            ProjectDetails project = ProjectDetails.GetInvalidNode(9999, "ADifferentMadeUpPath.csproj");

            project.GetTargetId(new TargetFramework("netstandard2.1")).Should().NotBe(project.GetTargetId(new TargetFramework("netcoreapp2.1")));
        }
    }
}
