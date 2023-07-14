using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace SolutionValidator.Tests
{
    public class FileSystemPathTests : BaseFixture
    {
        private ITestOutputHelper _testOutputHelper;

        public FileSystemPathTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void DirectoryParentIsCorrectlyDetermined()
        {
#if NET5_0_OR_GREATER
            if (OperatingSystem.IsWindows())
            {
                DirectoryPath path = new DirectoryPath("C:\\code\\MySolution\\src");

                path.Parent.NormalisedPath.Should().Be("C:\\code\\MySolution");

            }
#endif
        }
    }
}
