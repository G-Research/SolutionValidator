using System.Threading.Tasks;
using FluentAssertions;
using SolutionValidator.ValidateSolutions;
using Xunit;

namespace SolutionValidator.Tests
{
    public class SolutionValidatorTests : SolutionValidator
    {
        protected override bool TryGetOptions(string[] args, out IValidatorOptions options)
        {
            options = new ValidateSolutionsOptions();
            return true;
        }

        protected override Task<int> Run(IValidatorOptions validatorOptions)
        {
            return Task.FromResult(0);
        }

        [Fact]
        public void ValidateDependencyInjectionConfiguration()
        {
            // Yes to all intents and purposes this looks like a giant waste of time!
            // BUT it is a really important test because it validates that the DI container correctly builds.
            // Well to a point - not sure it validates open generics but I think this is all I need.
            Run(new string[0]).Result.Should().Be(0);
        }
    }
}
