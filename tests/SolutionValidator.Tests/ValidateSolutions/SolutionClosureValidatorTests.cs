using System.IO;
using System.Reflection;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SolutionValidator.ValidateSolutions;
using Xunit;

namespace SolutionValidator.Tests.ValidateSolutions
{
    public class SolutionClosureValidatorTests : BaseFixture
    {
        [Fact]
        public void SolutionWithAllProjects_PassesValidation()
        {
            var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var absolutePath = Path.Combine(testDirectory, "test-cases", "dependency-graph", "dependency-graph.sln");

            var solutionFile = SolutionFileHelper.LoadSolution(absolutePath);

            ProjectLoader projectLoader = new ProjectLoader(A.Fake<ILogger<ProjectLoader>>());

            SolutionClosureValidator solutionClosureValidator = new SolutionClosureValidator(A.Fake<ILogger<SolutionClosureValidator>>(), projectLoader);

            var result = solutionClosureValidator.Validate(new ValidationContext(absolutePath, solutionFile));
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void SolutionWithMissingProjects_FailsValidation()
        {
            var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var absolutePath = Path.Combine(testDirectory, "test-cases", "dependency-graph", "incomplete-dependency-graph.sln");

            var solutionFile = SolutionFileHelper.LoadSolution(absolutePath);

            ProjectLoader projectLoader = new ProjectLoader(A.Fake<ILogger<ProjectLoader>>());

            SolutionClosureValidator solutionClosureValidator = new SolutionClosureValidator(A.Fake<ILogger<SolutionClosureValidator>>(), projectLoader);

            var result = solutionClosureValidator.Validate(new ValidationContext(absolutePath, solutionFile));
            result.Should().Be(ValidationResult.Failure);

        }
    }
}
