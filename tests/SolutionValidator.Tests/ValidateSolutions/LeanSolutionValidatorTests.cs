using System.IO;
using System.Reflection;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SolutionValidator.DependencyGraph;
using SolutionValidator.ValidateSolutions;
using Xunit;

namespace SolutionValidator.Tests.ValidateSolutions
{
    public class LeanSolutionValidatorTests
    {
        [Fact]
        public void SolutionWithExtraProjects_FailsValidation()
        {
            var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var absolutePath = Path.Combine(testDirectory, "test-cases", "lean-solution", "A.sln");

            var solutionFile = SolutionFileHelper.LoadSolution(absolutePath);

            ProjectLoader projectLoader = new ProjectLoader(A.Fake<ILogger<ProjectLoader>>());
            GraphBuilder graphBuilder = new GraphBuilder(A.Fake<ILogger<GraphBuilder>>(), projectLoader);
            LeanSolutionValidator leanSolutionValidator = new LeanSolutionValidator(A.Fake<ILogger<LeanSolutionValidator>>(), projectLoader, graphBuilder);

            var result = leanSolutionValidator.Validate(new ValidationContext(absolutePath, solutionFile));
            result.Should().Be(ValidationResult.Failure);
        }

        [Fact]
        public void LeanSolution_PassesValidation()
        {
            var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var absolutePath = Path.Combine(testDirectory, "test-cases", "lean-solution", "A", "A.sln");

            var solutionFile = SolutionFileHelper.LoadSolution(absolutePath);

            ProjectLoader projectLoader = new ProjectLoader(A.Fake<ILogger<ProjectLoader>>());
            GraphBuilder graphBuilder = new GraphBuilder(A.Fake<ILogger<GraphBuilder>>(), projectLoader);
            LeanSolutionValidator leanSolutionValidator = new LeanSolutionValidator(A.Fake<ILogger<LeanSolutionValidator>>(), projectLoader, graphBuilder);

            var result = leanSolutionValidator.Validate(new ValidationContext(absolutePath, solutionFile));
            result.Should().Be(ValidationResult.Success);
        }
    }
}
