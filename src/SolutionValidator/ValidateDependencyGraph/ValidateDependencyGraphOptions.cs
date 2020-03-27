using System.Collections.Generic;
using Bulldog;
using CommandLine;

namespace SolutionValidator.ValidateDependencyGraph
{
    [Verb("validate-dependency-graph", HelpText = "Validates the dependency graph for the superset of all projects supplied.")]
    public class ValidateDependencyGraphOptions : OptionsBase, IValidatorOptions
    {
        // Initialised as Non-null IEnumerable<string> by CommandLineParse
        [Option("solutions", HelpText = "List of solutions to validate")]
        public IEnumerable<string> Solutions { get; set; } = new string[0];

        [Option("projects", HelpText = "List of projects to validate")]
        public IEnumerable<string> Projects { get; set; } = new string[0];

        [Option("colour-chart", Required = false, HelpText = "json file containing valid colour combinations")]
        public string ColourChart { get; set; }

        [Option("add-missing-colours", HelpText = "Add any colours defined in Project files to the colour chart")]
        public bool AddMissingColours { get; set; }

        public string CommandName => "ValidateDependencyGraph";
    }
}
