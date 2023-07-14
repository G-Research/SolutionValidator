using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SolutionValidator.DependencyGraph;
using Xunit;

namespace SolutionValidator.Tests.DependencyGraph
{
    public class GraphBuilderTests : BaseFixture
    {

        [Fact]
        public void GraphBuild_SuccessfullyGeneratesGraph()
        {
            var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var absolutePath = Path.Combine(testDirectory, "test-cases", "dependency-graph", "dependency-graph.sln");

            var solutionFile = SolutionFileHelper.LoadSolution(absolutePath);

            var projectLoader = new ProjectLoader(A.Fake<ILogger<ProjectLoader>>());
            var graphBuilder = new GraphBuilder(A.Fake<ILogger<GraphBuilder>>(), projectLoader);

            var graph = graphBuilder.GenerateGraph(solutionFile.GetMsBuildProjectsInSolution().Select(p => new FilePath(p.AbsolutePath)));

            var nodes = graph.Nodes.ToList();
            nodes.Count.Should().Be(4);

            var nodeA = nodes.Single(n => n.Name == "A");
            var nodeB = nodes.Single(n => n.Name == "B");
            var nodeC = nodes.Single(n => n.Name == "C");
            var nodeD = nodes.Single(n => n.Name == "D");
            nodeA.References.Count.Should().Be(1);
            nodeA.References.First().Name.Should().Be("B");

            nodeA.TransitiveReferences.Count.Should().Be(2);
            nodeA.TransitiveReferences.Contains(nodeC);
            nodeA.TransitiveReferences.Contains(nodeD);
        }

        [Fact]
        public void GraphBuild_SuccessfullyGeneratesEmptyGraph()
        {
            var projectLoader = new ProjectLoader(A.Fake<ILogger<ProjectLoader>>());
            var graphBuilder = new GraphBuilder(A.Fake<ILogger<GraphBuilder>>(), projectLoader);

            var graph = graphBuilder.GenerateGraph(Enumerable.Empty<FilePath>());

            var nodes = graph.Nodes.ToList();
            nodes.Count.Should().Be(0);
        }
    }
}
