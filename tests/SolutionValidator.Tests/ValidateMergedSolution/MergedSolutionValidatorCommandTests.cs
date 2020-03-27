using System.IO;
using System.Reflection;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SolutionValidator.ValidateMergedSolution;
using Xunit;

namespace SolutionValidator.Tests.ValidateMergedSolution
{
    public class MergedSolutionValidatorCommandTests
    {
        [Fact]
        public void SupersetSolution_PassesValidation_WithoutStrictValidation()
        {
            var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var mergedSolution = Path.Combine(testDirectory, "test-cases", "dependency-graph", "dependency-graph.sln");
            var incompleteSolution = Path.Combine(testDirectory, "test-cases", "dependency-graph", "incomplete-dependency-graph.sln");

            ValidateMergedSolutionCommand validateMergedSolutionCommand = new ValidateMergedSolutionCommand(A.Fake<ILogger<ValidateMergedSolutionCommand>>());
            validateMergedSolutionCommand.Run(new ValidateMergedSolutionOptions { MergedSolution = mergedSolution, Solutions = new[] { incompleteSolution } }).Should().Be(CommandResult.Success);
            validateMergedSolutionCommand.Run(new ValidateMergedSolutionOptions { MergedSolution = incompleteSolution, Solutions = new[] { mergedSolution } }).Should().Be(CommandResult.Failure);
        }

        [Fact]
        public void SupersetSolution_FailsValidation_WithStrictValidation()
        {
            var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var mergedSolution = Path.Combine(testDirectory, "test-cases", "dependency-graph", "dependency-graph.sln");
            var incompleteSolution = Path.Combine(testDirectory, "test-cases", "dependency-graph", "incomplete-dependency-graph.sln");

            ValidateMergedSolutionCommand validateMergedSolutionCommand = new ValidateMergedSolutionCommand(A.Fake<ILogger<ValidateMergedSolutionCommand>>());
            validateMergedSolutionCommand.Run(new ValidateMergedSolutionOptions { MergedSolution = mergedSolution, Solutions = new[] { incompleteSolution }, Strict = true }).Should().Be(CommandResult.Failure);
        }
    }
}
