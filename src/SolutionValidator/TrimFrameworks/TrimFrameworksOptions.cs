using System.Collections.Generic;
using Bulldog;
using CommandLine;

namespace SolutionValidator.TrimFrameworks
{
    [Verb("trim-frameworks", HelpText = "Removes any unused target frameworks as determined by dependency graph of input files")]
    public class TrimFrameworksOptions : OptionsBase, IValidatorOptions
    {
        [Option("solutions", Required = true, HelpText = "List of solutions to include in dependency graph")]
        public IEnumerable<string> Solutions { get; set; }

        [Option("exclude-patterns", HelpText = "Additional exclude patterns when searching for solutions")]
        public IEnumerable<string> ExcludePatterns { get; set; } = new string[0];

        [Option("solution-tags", HelpText = "Solution tags with which to filter the solution set.")]
        public IEnumerable<string> SolutionTags { get; set; } = new string[0];

        public string CommandName => "TrimFrameworks";
    }
}
