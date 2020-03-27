using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SolutionValidator.DependencyGraph;
using Xunit;
using Xunit.Abstractions;

namespace SolutionValidator.Tests
{
    public class ProjectGraphBuilderTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ProjectGraphBuilderTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }
        [Fact]
        public void ValidateThatProjectGraphBuilderRecursivelyFollowsProjectDependenciesWhenIncludingTestAssemblies()
        {
            var testFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "test-cases", "project-graph-builder");
            var absolutePath = Path.Combine(testFilePath, "A", "A.csproj");

            var testProjectFinder = new TestProjectFinder(NullLogger<TestProjectFinder>.Instance);
            var graphBuilder = new GraphBuilder(NullLogger<GraphBuilder>.Instance, new ProjectLoader(_testOutputHelper.BuildLoggerFor<ProjectLoader>()));
            var projectsGraphBuilder = new ProjectGraphBuilder(NullLogger<ProjectGraphBuilder>.Instance, graphBuilder, testProjectFinder);

            var graph = projectsGraphBuilder.BuildProjectGraph(new[] { absolutePath }, false);

            graph.Nodes.Count().Should().Be(7);
            graph.Contains(absolutePath);
            graph.Contains(Path.Combine(testFilePath, "A.Test", "A.Test.csproj"));
            graph.Contains(Path.Combine(testFilePath, "B", "B.csproj"));
            graph.Contains(Path.Combine(testFilePath, "B.Test", "B.Test.csproj"));
            graph.Contains(Path.Combine(testFilePath, "C", "C.csproj"));
            graph.Contains(Path.Combine(testFilePath, "C.Test", "C.Test.csproj"));
            graph.Contains(Path.Combine(testFilePath, "D", "D.csproj"));
        }

        [Fact]
        public void ValidateThatProjectGraphBuilderDoesNotRecursivelyFollowProjectDependenciesWhenNotIncludingTestAssemblies()
        {
            var testFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "test-cases", "project-graph-builder");

            var absolutePath = Path.Combine(testFilePath, "A", "A.csproj");

            var testProjectFinder = new TestProjectFinder(NullLogger<TestProjectFinder>.Instance);
            var graphBuilder = new GraphBuilder(NullLogger<GraphBuilder>.Instance, new ProjectLoader(_testOutputHelper.BuildLoggerFor<ProjectLoader>()));
            var projectsGraphBuilder = new ProjectGraphBuilder(NullLogger<ProjectGraphBuilder>.Instance, graphBuilder, testProjectFinder);

            var graph = projectsGraphBuilder.BuildProjectGraph(new[] { absolutePath }, true);

            graph.Nodes.Count().Should().Be(2);
            graph.Contains(absolutePath);
            graph.Contains(Path.Combine(testFilePath, "B", "B.csproj"));
        }

    }
}
