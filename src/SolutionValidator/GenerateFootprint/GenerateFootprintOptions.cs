using System.Collections.Generic;
using Bulldog;
using CommandLine;

namespace SolutionValidator.GenerateFootprint
{
    [Verb("generate-footprint", HelpText = "Lists directories that are in the footprint of the supplied projects")]
    public class GenerateFootprintOptions : OptionsBase, IValidatorOptions
    {
        [Option("code-root", Required = true, HelpText = "Directory where code is checked out")]
        public string CodeRoot { get; set; }

        [Option("input-files", Required = true, HelpText = "selection of projects and solutions to check footprint of")]
        public IEnumerable<string> InputFiles { get; set; } = new string[0];


        [Option("output-file", Required = true, HelpText = "The file to which footprint directories will be written")]
        public string OutputFile { get; set; }

        public string CommandName => "Footprint";
    }
}
