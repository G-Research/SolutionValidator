using System.Collections.Generic;
using Bulldog;
using CommandLine;

namespace SolutionValidator.GenerateGraph
{
    [Verb("generate-graph", HelpText = "Generates a dot graph for the supplied solution.")]
    public class GenerateDependencyGraphOptions : OptionsBase, IValidatorOptions
    {
        [Option("input-files", Required = true, HelpText = "Solution or project files to generate the dependency graph for.")]
        public IEnumerable<string> InputFiles { get; set; }

        [Option("exclude-patterns", HelpText = "Additional exclude patterns when searching for files")]
        public IEnumerable<string> ExcludePatterns { get; set; } = new string[0];

        [Option("colour-chart", Required = false, HelpText = "json file containing valid colour combinations")]
        public string ColourChart { get; set; }

        [Option("output-file", Required = false, Default = "dependency-graph.gv", HelpText = "File to write the dependency graph to.")]
        public string OutputFile { get; set; }

        [Option("exclude-legend", Required = false, HelpText = "Exclude the legend from the resulting graph.")]
        public bool ExcludeLegend { get; set; }

        public string CommandName => "GenerateDependencyGraph";
    }
}
