using System.Collections.Generic;
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
    public class TestFrameworkValidatorTests
    {
        private ITestOutputHelper _outputHelper;

        public TestFrameworkValidatorTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public void SolutionWithValidTestFrameworks_PassesValidation()
        {
            var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var absolutePath = Path.Combine(testDirectory, "test-cases", "test-frameworks", "valid-test-frameworks.sln");

            var solutionFile = SolutionFileHelper.LoadSolution(absolutePath);

            var projectLoader = new ProjectLoader(A.Fake<ILogger<ProjectLoader>>());
            var graphBuilder = new ProjectTargetGraphBuilder(A.Fake<ILogger<ProjectTargetGraphBuilder>>(), projectLoader);

            TestFrameworkValidator testFrameworkValidator = new TestFrameworkValidator(A.Fake<ILogger<TestFrameworkValidator>>(), graphBuilder, projectLoader);

            var result = testFrameworkValidator.Validate(new ValidationContext(absolutePath, solutionFile));
            result.Should().Be(ValidationResult.Success);
            solutionFile.GetMsBuildProjectsInSolution().Count().Should().Be(4);
        }

        [Fact]
        public void SolutionWithMissingTestFrameworks_FailsValidation()
        {
            var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var absolutePath = Path.Combine(testDirectory, "test-cases", "test-frameworks", "test-frameworks.sln");

            var solutionFile = SolutionFileHelper.LoadSolution(absolutePath);

            var projectLoader = new ProjectLoader(A.Fake<ILogger<ProjectLoader>>());
            var graphBuilder = new ProjectTargetGraphBuilder(A.Fake<ILogger<ProjectTargetGraphBuilder>>(), projectLoader);

            var errors = new List<string>();
            var logger = new XUnitLogger<TestFrameworkValidator>(_outputHelper, (level, output) => { if (level == LogLevel.Error) { errors.Add(output); } });

            TestFrameworkValidator testFrameworkValidator = new TestFrameworkValidator(logger, graphBuilder, projectLoader);

            var result = testFrameworkValidator.Validate(new ValidationContext(absolutePath, solutionFile));
            result.Should().Be(ValidationResult.Failure);
            solutionFile.GetMsBuildProjectsInSolution().Count().Should().Be(8);
            errors.Count().Should().Be(2);
            errors[0].Should().Contain("Test Project B.Test is missing framework(s) [net471]");
            errors[1].Should().Contain("Test Project C.Test is missing framework(s) [netcoreapp3.1]");
        }
    }
}
