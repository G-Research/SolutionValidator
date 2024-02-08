using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bulldog;
using CommandLine;

namespace SolutionValidator.ProjectReferences
{
    public enum ProjectAction
    {
        Add,
        Remove,
        Update
    }

    [Verb("update-references", HelpText = "Add/remove project references")]
    internal class UpdateProjectReferencesOptions : OptionsBase, IValidatorOptions
    {
        // Initialised as Non-null IEnumerable<string> by CommandLineParse
        [Option("input-files", Required = true, HelpText = "List of files to update")]
        public IEnumerable<string> InputFiles { get; set; }

        [Option("exclude-patterns", HelpText = "Additional exclude patterns (on top of bin and obj directories) to use when searching for input files.")]
        public IEnumerable<string> ExcludePatterns { get; set; } = new string[0];

        [Option("action", Required = true, HelpText = "Action to perform on projects")]
        public ProjectAction Action { get; set; }

        // Initialised as Non-null IEnumerable<string> by CommandLineParse
        [Option("projects", Required = true, HelpText = "List of projects to run action for")]
        public IEnumerable<string> Projects { get; set; }

        public string CommandName => "UpdateReferences";
    }
}
