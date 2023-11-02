using FluentAssertions;
using Xunit;

namespace SolutionValidator.Tests
{
    public class TargetFrameworkExtensionsTests : BaseFixture
    {
        [Theory]
        [InlineData("net471", "net471", true)]
        [InlineData("net5.0", "net5.0", true)]
        [InlineData("net5.0", "netstandard2.0", true)]
        [InlineData("net5.0", "netstandard2.1", true)]
        [InlineData("net5.0-windows", "net5.0", true)]
        [InlineData("net5.0-windows", "netstandard2.0", true)]
        [InlineData("net5.0-windows", "netstandard2.1", true)]
        [InlineData("netcoreapp3.1", "netstandard2.0", true)]
        [InlineData("netcoreapp3.1", "netstandard2.1", true)]
        [InlineData("netcoreapp2.1", "netstandard2.0", true)]
        [InlineData("netcoreapp2.1", "netstandard2.1", false)]
        [InlineData("net471", "net5.0", false)]
        [InlineData("net5.0", "net5.0-windows", false)]
        [InlineData("net5.0-windows", "net5.0-linux", false)]
        public void IsCompatibleFrameworkReference(string targetFramework, string referenceFramework, bool isCompatible)
        {
            var target = new TargetFramework(targetFramework);
            var reference = new TargetFramework(referenceFramework);

            target.IsCompatibleFrameworkReference(reference).Should().Be(isCompatible);
        }
    }
}
