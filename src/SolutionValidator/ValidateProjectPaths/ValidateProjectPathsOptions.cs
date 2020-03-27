using System;
using System.Collections.Generic;
using System.Text;
using Bulldog;
using CommandLine;

namespace SolutionValidator.ValidateProjectPaths
{
    [Verb("validate-project-paths", HelpText = "Validate that the all the projects in the given solution originate from allowed project locations")]
    public class ValidateProjectPathsOptions : OptionsBase, IValidatorOptions
    {
        // Initialised as Non-null IEnumerable<string> by CommandLineParse
        [Option("valid-path-roots", HelpText = "List of validate paths to validate valid projects against.")]
        public IEnumerable<string> ValidPathRoots { get; set; }

        [Option("solution", HelpText = "Solution to validate")]
        public string Solution { get; set; }

        public string CommandName => "ValidateProjectPaths";
    }
}
