using Bulldog;
using CommandLine;
using System.Collections.Generic;

namespace SolutionValidator.ProjectReferences
{
    [Verb("yolo", HelpText = "Makes all solution and projects file match the disk! Do this with extreme caution and make sure that root directory contains everything you need.")]
    internal class YoloOptions : OptionsBase, IValidatorOptions
    {
        [Option("root-directory", Default = ".", HelpText = "Root directory to search for all solutions and projects in.")]
        public string RootDirectory { get; set; }

        [Option("exclude-patterns", HelpText = "Additional exclude patterns when searching for files")]
        public IEnumerable<string> ExcludePatterns { get; set; } = new string[0];

        public string CommandName => "YOLO";
    }
}
