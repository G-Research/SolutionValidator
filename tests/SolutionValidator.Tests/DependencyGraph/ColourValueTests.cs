using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using SolutionValidator.DependencyGraph;
using Xunit;

namespace SolutionValidator.Tests.DependencyGraph
{
    public class ColourValueTests
    {
        [Fact]
        public void ColourValues_AddTogether()
        {
            var a = new ColourValue(1);
            var b = new ColourValue(2);

            var c = new ColourValue(3);

            (a + b).Should().Be(c);
            (c + a).Should().Be(c);

        }

        [Fact]
        public void InvalidColourValues_Propagate()
        {

        }
    }
}
