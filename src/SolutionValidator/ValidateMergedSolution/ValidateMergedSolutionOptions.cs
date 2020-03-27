using System.Collections.Generic;
using Bulldog;
using CommandLine;

namespace SolutionValidator.ValidateMergedSolution
{
    [Verb("validate-merged-solution", HelpText = "Validate that the merged solution is superset of projects in list solutions")]
    public class ValidateMergedSolutionOptions : MergedSolutionOptions, IValidatorOptions
    {
        public string CommandName => "ValidateMergedSolution";
    }

    public class MergedSolutionOptions : OptionsBase
    {
        [Option("merged-solution", Required = true, HelpText = "Merged|All solution to validate")]
        public string MergedSolution { get; set; }

        [Option("solutions", Required = true, HelpText = "List of component solutions")]
        public IEnumerable<string> Solutions { get; set; }

        [Option("exclude-patterns", HelpText = "Additional exclude patterns when searching for solutions")]
        public IEnumerable<string> ExcludePatterns { get; set; } = new string[0];

        [Option("exclude-projects-regex", HelpText = "A regex to exclude projects that are expected to be excluded from the solution")]
        public string ExcludeProjectsRegex { get; set; }

        [Option("solution-tags", HelpText = "Solution tags with which to filter the solution set.")]
        public IEnumerable<string> SolutionTags { get; set; } = new string[0];

        [Option("strict", HelpText = "Strict mode so that merged solution is strict superset with no additional projects")]
        public bool Strict { get; set; }

        [Option("shared-only", Required = false, HelpText = "Whether to exclude projects that exist only in one of the input solutions")]
        public bool SharedOnly { get; set; }

    }
}
