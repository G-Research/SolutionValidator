using System.IO;
using System.Linq;
using System.Reflection;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SolutionValidator.DependencyGraph;
using SolutionValidator.ValidateSolutions;
using Xunit;
using Xunit.Abstractions;

namespace SolutionValidator.Tests.ValidateSolutions
{
    public class FrameworkValidatorTests : BaseFixture
    {
        private ITestOutputHelper _outputHelper;

        public FrameworkValidatorTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public void CanGenerateCorrectProjectTargetGraph()
        {
            var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var absolutePath = Path.Combine(testDirectory, "test-cases", "target-framework-validation", "valid-frameworks.sln");

            var projectLoader = new ProjectLoader(A.Fake<ILogger<ProjectLoader>>());
            var graphBuilder = new ProjectTargetGraphBuilder(A.Fake<ILogger<ProjectTargetGraphBuilder>>(), projectLoader);
            var projects = ProjectGraphBuilder.GetProjectsFromInputFiles(new[] { absolutePath });
            var graph = graphBuilder.GenerateGraph(projects);

            graph.InvalidNodes.Count.Should().Be(0);
            graph.Nodes.Count().Should().Be(7);
            graph.Nodes.Should().ContainSingle(p => p.Name == "B1");
        }


        [Fact]
        public void SolutionWithValidFrameworkDependencies_PassesValidation()
        {
            var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var absolutePath = Path.Combine(testDirectory, "test-cases", "target-framework-validation", "valid-frameworks.sln");

            var solutionFile = SolutionFileHelper.LoadSolution(absolutePath);

            var projectLoader = new ProjectLoader(A.Fake<ILogger<ProjectLoader>>());
            var graphBuilder = new ProjectTargetGraphBuilder(A.Fake<ILogger<ProjectTargetGraphBuilder>>(), projectLoader);

            FrameworkValidator testFrameworkValidator = new FrameworkValidator(A.Fake<ILogger<FrameworkValidator>>(), projectLoader, graphBuilder);

            var result = testFrameworkValidator.Validate(new ValidationContext(absolutePath, solutionFile));
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void SolutionWithInvalidFrameworkDependencies_FailsValidation()
        {
            var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var absolutePath = Path.Combine(testDirectory, "test-cases", "target-framework-validation", "invalid-frameworks.sln");

            var solutionFile = SolutionFileHelper.LoadSolution(absolutePath);

            var projectLoader = new ProjectLoader(A.Fake<ILogger<ProjectLoader>>());
            var graphBuilder = new ProjectTargetGraphBuilder(A.Fake<ILogger<ProjectTargetGraphBuilder>>(), projectLoader);

            FrameworkValidator testFrameworkValidator = new FrameworkValidator(A.Fake<ILogger<FrameworkValidator>>(), projectLoader, graphBuilder);

            var result = testFrameworkValidator.Validate(new ValidationContext(absolutePath, solutionFile));
            result.Should().Be(ValidationResult.Failure);
        }
    }
}
