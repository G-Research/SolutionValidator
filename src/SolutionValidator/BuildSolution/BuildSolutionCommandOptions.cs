using System.Collections.Generic;
using Bulldog;
using CommandLine;

namespace SolutionValidator.BuildSolution
{
    [Verb("build-solution", HelpText = "Builds a solution based on the transitive closure of the supplied projects")]
    public class BuildSolutionCommandOptions : OptionsBase, IValidatorOptions
    {
        [Option("exclude-test-projects", Required = false, HelpText = "Exclude associated test projects which by default are added using standard heuristics.")]
        public bool ExcludeTestProjects { get; set; }

        [Option("exclude-projects-regex", HelpText = "A regex to exclude projects from the solution")]
        public string ExcludeProjectsRegex { get; set; }

        [Option("exclude-patterns", HelpText = "Additional exclude patterns when searching for files")]
        public IEnumerable<string> ExcludePatterns { get; set; } = new string[0];

        [Option("input-files", Required = true, HelpText = "selection of projects and solutions to build")]
        public IEnumerable<string> InputFiles { get; set; } = new string[0];

        [Option("output-file", Required = true, HelpText = "The solution file to write to.")]
        public string OutputFile { get; set; }

        [Option("shared-only", Required = false, HelpText = "Whether to exclude projects that exist only in one of the input solutions")]
        public bool SharedOnly { get; set; }

        public string CommandName => "BuildSolution";

        [Option("file-mode", Required = false, Default = SolutionCreationMode.CreateNew, HelpText = "Determines how new solution is built")]
        public SolutionCreationMode FileMode { get; set; }

        [Option("solution-folders", Required = false, HelpText = "Solution folders")]
        public IEnumerable<string> SolutionFolders { get; set; } = new string[0];
    }
}
