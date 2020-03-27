using System;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace SolutionValidator.Tests
{
    public class TargetFrameworkTests
    {
        [Theory]
        [InlineData("netcoreapp3.1", new[] { "netcoreapp3.1", "net471" }, true, "netcoreapp3.1")]
        [InlineData("netcoreapp3.1", new[] { "netstandard2.1", "net471" }, true, "netstandard2.1")]
        [InlineData("netcoreapp3.1", new[] { "netcoreapp2.1", "net471" }, true, "netcoreapp2.1")]
        [InlineData("netcoreapp3.1", new[] { "netcoreapp3.1", "netcoreapp2.1", "net471" }, true, "netcoreapp3.1")]
        [InlineData("netstandard2.0", new[] { "netcoreapp2.1", "net471" }, false, null)]
        [InlineData("netstandard2.0", new[] { "netcoreapp3.1", "netcoreapp2.1" }, false, null)]
        [InlineData("net5.0-windows", new[] { "net5.0", "netcoreapp3.1" }, true, "net5.0")]
        [InlineData("net5.0", new[] { "net5.0-windows", "net471" }, false, null)]
        public void TestGetBestMatchingFrameworkReturnsCorrectFramework(string targetFramework, string[] referenceFrameworks, bool expectedResult, string expectedMatchingFramework)
        {
            var result = new TargetFramework(targetFramework).TryGetBestMatchingTargetFramework(referenceFrameworks.Select(f => new TargetFramework(f)).ToImmutableList(), out TargetFramework matchingFramework);

            result.Should().Be(expectedResult);
            if (matchingFramework == null)
            {
                expectedMatchingFramework.Should().BeNull();
            }
            else
            {
                matchingFramework.Framework.Should().Be(expectedMatchingFramework);
            }
        }

        [Theory]
        [InlineData("net5.0")]
        [InlineData("net5")]
        public void Net5TargetFrameworkConversionsWork(string framework)
        {
            TargetFramework targetFramework = new TargetFramework(framework);
            targetFramework.FrameworkType.Should().Be(FrameworkType.NetCore);
            targetFramework.Version.Should().Be(Version.Parse("5.0"));
        }

        [Theory]
        [InlineData("net5.0", new[] { "net5.0-windows", "net471" }, true, "net5.0-windows")]
        [InlineData("net5.0-windows", new[] { "net5.0-linux", "net471" }, false, null)]
        [InlineData("netcoreapp3.1", new[] { "net5.0", "net471" }, true, "net5.0")]
        [InlineData("netcoreapp3.1", new[] { "net5.0-windows", "net5.0", "net471" }, true, "net5.0")]
        public void GettingClosestHigherFrameworkCorrectlySelects(string targetFramework, string[] referenceFrameworks, bool expectedResult, string expectedMatchingFramework)
        {
            var result = new TargetFramework(targetFramework).TryGetClosestHigherFrameworkMatch(referenceFrameworks.Select(f => new TargetFramework(f)).ToImmutableList(), out TargetFramework nextBestMatch);

            result.Should().Be(expectedResult);
            nextBestMatch?.Framework.Should().Be(expectedMatchingFramework);
        }
    }
}
