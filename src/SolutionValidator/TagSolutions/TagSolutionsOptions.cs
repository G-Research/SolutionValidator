using System.Collections.Generic;
using Bulldog;
using CommandLine;

namespace SolutionValidator.TagSolutions
{
    public enum TagMode
    {
        Set,
        Add,
        Remove
    }

    [Verb("tag-solutions", HelpText = "Add or amend solution tags")]
    public class TagSolutionsOptions : OptionsBase, IValidatorOptions
    {
        [Option("solutions", Required = true, HelpText = "List of component solutions")]
        public IEnumerable<string> Solutions { get; set; }

        [Option("exclude-patterns", HelpText = "Additional exclude patterns when searching for solutions")]
        public IEnumerable<string> ExcludePatterns { get; set; } = new string[0];

        [Option("solution-tags", HelpText = "Solution tags to modify on solution")]
        public IEnumerable<string> SolutionTags { get; set; } = new string[0];

        [Option("tag-mode", Default = TagMode.Set, HelpText = "Whether to overwrite or add tags.")]
        public TagMode TagMode { get; set; }

        public string CommandName => "TagSolutions";
    }
}
