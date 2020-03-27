using CommandLine;
using SolutionValidator.ValidateMergedSolution;

namespace SolutionValidator.FixMergedSolution
{
    [Verb("fix-merged-solution", HelpText = "Fixes merged solution so that it contains superset of projects in list solutions")]
    public class FixMergedSolutionOptions : MergedSolutionOptions, IValidatorOptions
    {
        [Option("regenerate-no-test-solution", Required = false, HelpText = "Indicates whether we should generate matching no test solution when fixing merged solution")]
        public bool GenerateNoTestSolution { get; set; }

        public string CommandName => "FixMergedSolution";
    }
}
