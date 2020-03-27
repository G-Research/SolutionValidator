using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using SolutionValidator.DependencyGraph;

namespace SolutionValidator.GenerateGraph
{
    public class GenerateDependencyGraphCommand : ICommand
    {
        private readonly ILogger<GenerateDependencyGraphCommand> _logger;
        private readonly GraphBuilder _graphBuilder;
        private readonly ColourChart _colourChart;

        public GenerateDependencyGraphCommand(ILogger<GenerateDependencyGraphCommand> logger, GraphBuilder graphBuilder, ColourChart colourChart)
        {
            _logger = logger;
            _graphBuilder = graphBuilder;
            _colourChart = colourChart;
        }

        public CommandResult Run(GenerateDependencyGraphOptions options)
        {
            var inputFiles = FileUtils.FindFiles(options.InputFiles, options.ExcludePatterns.ToArray());
            var projects = ProjectGraphBuilder.GetProjectsFromInputFiles(inputFiles);

            _logger.LogInformation("Loaded {count} project files from input files.", projects.Count);
            if (!string.IsNullOrEmpty(options.ColourChart))
            {
                _colourChart.AddColoursFromConfig(options.ColourChart);
            }

            var graph = _graphBuilder.GenerateGraph(projects);

            var result = _graphBuilder.MatchColours(graph, _colourChart, true);

            GenerateGraph(graph, result.NodeColours, _colourChart, options.OutputFile, options.ExcludeLegend);

            return CommandResult.Success;
        }

        public void GenerateGraph(Graph<ProjectNode> graph, Dictionary<int, Colour> nodeColours, ColourChart colourChart, string outputFile, bool excludeLegend)
        {
            // Stole most of this from Joe... but in part due to custom colour parsing crazy I haven't reused Joe's library :-)!
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("digraph \"DependencyGraph\" {");
            sb.AppendLine("    ratio=\"compress\"");

            if (!excludeLegend)
            {
                // Render the legend
                sb.AppendLine("    subgraph cluster_01 {");
                sb.AppendLine("        rank=sink;");
                sb.AppendLine("        label=\"Legend\";");
                sb.AppendLine("        shape=rectangle;");
                sb.AppendLine("        color=black;");

                foreach (var colour in colourChart.Values)
                {
                    sb.AppendLine($"    \"{colour.Name}\" [{GetAttributes(colour.Name)}]");
                }

                sb.AppendLine("    }");
            }
            foreach (var node in graph.Nodes)
            {
                // Get style from colour...
                var colour = nodeColours[node.Id];
                string attributeString = GetAttributes(colour.Name);

                sb.AppendLine($"{node.Id} [label=\"{node.Name}\",{attributeString}];");

                foreach (var dependency in node.GetRequiredGraphNodes())
                {
                    sb.AppendLine($"{node.Id} -> {dependency.Id};");
                }
            }

            sb.AppendLine("}");

            System.IO.File.WriteAllText(outputFile, sb.ToString());
        }

        private string GetAttributes(string colour)
        {
            var attributes = _colourChart.GetAttributes(colour);
            return string.Join(",", attributes.Select(a => $"{a.Key}=\"{a.Value}\""));
        }

    }
}
