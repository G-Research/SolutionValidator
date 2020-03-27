using System.Collections.Generic;
using Bulldog;
using CommandLine;

namespace SolutionValidator.TrimReferences
{
    [Verb("trim-references", HelpText = "Removes any direct project references where they are already added as part of transitive graph")]
    public class TrimReferencesOptions : OptionsBase, IValidatorOptions
    {
        [Option("solutions", Required = true, HelpText = "List of solutions to include in dependency graph")]
        public IEnumerable<string> Solutions { get; set; }

        [Option("exclude-patterns", HelpText = "Additional exclude patterns when searching for solutions")]
        public IEnumerable<string> ExcludePatterns { get; set; } = new string[0];

        [Option("solution-tags", HelpText = "Solution tags with which to filter the solution set.")]
        public IEnumerable<string> SolutionTags { get; set; } = new string[0];

        public string CommandName => "TrimReferences";
    }
}
