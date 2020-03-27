using System;
using System.IO;
using System.Linq;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SolutionValidator.DependencyGraph;
using Xunit;

namespace SolutionValidator.Tests.DependencyGraph
{
    public class ColourChartTests
    {
        [Fact]
        public void LoadDuplicateColour_ProducesLoadError()
        {
            string colourConfig =
                "[" +
                "{" +
                "  \"Name\": \"Green\"," +
                "  \"Description\": \"Primary Colour\"" +
                "}," +
                "{" +
                "  \"Name\": \"Red\"," +
                "  \"Description\": \"Primary Colour\"" +
                "}," +
                "{" +
                "  \"Name\": \"Green\"," +
                "  \"Description\": \"Duplicate Colour\"" +
                "}" +
                "]";

            string fullPath = null;
            try
            {
                fullPath = FileUtils.WriteToTempTestFile(colourConfig);
                var logger = A.Fake<ILogger<ColourChart>>();
                var colourChart = new ColourChart(logger);
                var exception = Assert.Throws<Exception>(() => colourChart.AddColoursFromConfig(fullPath));
                exception.Message.Should().Contain("Attempting to add Duplicate colour");
            }
            finally
            {
                if (fullPath != null)
                {
                    File.Delete(fullPath);
                }
            }

        }

        [Fact]
        public void LoadValidationConfiguration_ProducesValidColourChart()
        {
            string colourConfig =
@"[
{
  ""Name"": ""Green"",
  ""Description"": ""Primary colour""
},
{
  ""Name"": ""Red"",
  ""Description"": ""A different primary colour""
},
{
  ""Name"": ""Blue"",
  ""Description"": ""A different primary colour""
},
{
  ""Name"": ""Purple"",
  ""Description"": ""A combined Colour"",
  ""ComponentColours"": [ ""Red"", ""Blue"" ]
},
{
  ""Name"": ""Magenta"",
  ""Description"": ""Another combined Colour"",
  ""ComponentColours"": [ ""Green"", ""Blue"" ]
},
{
  ""Name"": ""Yellow"",
  ""Description"": ""Another combined Colour"",
  ""ComponentColours"": [ ""Purple"", ""Green"" ]
}
]";
            string fullPath = null;
            try
            {
                fullPath = FileUtils.WriteToTempTestFile(colourConfig);
                var logger = A.Fake<ILogger<ColourChart>>();
                var colourChart = new ColourChart(logger);
                colourChart.AddColoursFromConfig(fullPath);
                var colours = colourChart.Values.ToList();
                colours.Count.Should().Be(8);
                colourChart.ContainsColour("Default").Should().BeTrue();
                colourChart.ContainsColour("Invalid").Should().BeTrue();
                colourChart.TryGetColour("Green", out Colour colour).Should().BeTrue();
                colour.Value.Should().Be(1);
                colourChart.TryGetColour("Red", out colour).Should().BeTrue();
                colour.Value.Should().Be(2);
                colourChart.TryGetColour("Blue", out colour).Should().BeTrue();
                colour.Value.Should().Be(4);
                colourChart.TryGetColour("Purple", out colour).Should().BeTrue();
                colour.Value.Should().Be(6);
                colourChart.TryGetColour("Magenta", out colour).Should().BeTrue();
                colour.Value.Should().Be(5);
                colourChart.TryGetColour("Yellow", out colour).Should().BeTrue();
                colour.Value.Should().Be(7);
                colourChart.TryGetColour(3, out colour).Should().BeFalse();
            }
            finally
            {
                if (fullPath != null)
                {
                    File.Delete(fullPath);
                }
            }
        }
    }
}
