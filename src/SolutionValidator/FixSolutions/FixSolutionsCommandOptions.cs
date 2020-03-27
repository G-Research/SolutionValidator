using System.Collections.Generic;
using Bulldog;
using CommandLine;

namespace SolutionValidator.FixSolutions
{
    [Verb("fix-solutions", HelpText = "Fix solution validation failures")]
    public class FixSolutionsCommandOptions : OptionsBase, IValidatorOptions
    {
        // Initialised as Non-null IEnumerable<string> by CommandLineParse
        [Option("solutions", Required = true, HelpText = "List of solutions to fix ")]
        public IEnumerable<string> Solutions { get; set; }

        [Option("exclude-patterns", HelpText = "Additional exclude patterns when searching for solutions")]
        public IEnumerable<string> ExcludePatterns { get; set; } = new string[0];

        public string CommandName => "FixSolution";
    }
}
