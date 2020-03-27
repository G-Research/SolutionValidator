using System.Linq;
using FluentAssertions;
using SolutionValidator.GenerateFootprint;
using Xunit;

namespace SolutionValidator.Tests.GenerateFootprint
{
    public class RelativeDirChildrenFilterTest
    {
        [Theory]
        [InlineData(0, new[] { "/foo", "/bar", "/bar/baz", "/bar", "/baz" }, false)]
        [InlineData(1, new[] { "/foo", null, "/bar/baz", "/bar", "/baz" }, false)]
        [InlineData(1, new[] { "/foo", "/bar", "/bar/baz", "/bar", "/baz" }, true)]
        [InlineData(2, new[] { "/foo", "/bar", "/bar/baz", "/bar", "/baz" }, true)]
        [InlineData(3, new[] { "/foo", null, "/bar/baz", "/bar", "/baz" }, false)]
        [InlineData(3, new[] { "/foo", "/bar", "/bar/baz", "/bar", "/baz" }, true)]
        [InlineData(4, new[] { "/foo", "/bar", "/bar/baz", "/bar", "/baz" }, false)]
        public void IsChildOfAny(int index, string[] dirs, bool result)
        {
            RelativeDirChildrenFilter.IsChildOfAny(index, dirs).Should().Be(result);
        }

        [Theory]
        [InlineData(new[] { "/foo" }, new[] { "/foo/" })]
        [InlineData(new[] { "/foo", "/foo/" }, new[] { "/foo/" })]
        [InlineData(new[] { "/foo", "/bar" }, new[] { "/foo/", "/bar/" })]
        [InlineData(new[] { "/foo", "/bar", "/bar/baz", "/bar" }, new[] { "/foo/", "/bar/" })]
        [InlineData(new[] { "/foo", "/bar", "/bar/baz", "/bar", "/baz" }, new[] { "/foo/", "/bar/", "/baz/" })]
        [InlineData(new[] { "/bar/baz", "/baz/../bar/baz" }, new[] { "/bar/baz/" })]
        [InlineData(new[] { "/baz/../bar/baz", "/bar/baz" }, new[] { "/bar/baz/" })]
        public void FilterChildrenAndAddDirSeparator(string[] dirs, string[] expected)
        {
            var filteredDirs = RelativeDirChildrenFilter
                .FilterChildrenAndAddDirSeparator(dirs)
                // Make the test deterministic on Windows by dropping the drive letter
                .Select(p => p[1] == ':' ? p.Substring(2) : p);
            filteredDirs.Should().BeEquivalentTo(expected);
        }
    }
}
