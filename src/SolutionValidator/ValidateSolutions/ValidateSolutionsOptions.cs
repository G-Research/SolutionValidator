using System.Collections.Generic;
using Bulldog;
using CommandLine;

namespace SolutionValidator.ValidateSolutions
{
    [Verb("validate-solutions", HelpText = "Run standard set of solution validation checks")]
    public class ValidateSolutionsOptions : OptionsBase, IValidatorOptions
    {
        // Initialised as Non-null IEnumerable<string> by CommandLineParse
        [Option("solutions", Required = true, HelpText = "List of solutions to validate ")]
        public IEnumerable<string> Solutions { get; set; }

        [Option("exclude-patterns", HelpText = "Additional exclude patterns when searching for solutions")]
        public IEnumerable<string> ExcludePatterns { get; set; } = new string[0];

        public string CommandName => "ValidateSolutions";
    }
}
