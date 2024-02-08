using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;

namespace SolutionValidator.GenerateFootprint
{
    public class GenerateFootprintCommand : ICommand
    {
        private readonly ILogger<GenerateFootprintCommand> _logger;
        private readonly ProjectGraphBuilder _projectGraphBuilder;

        public GenerateFootprintCommand(ILogger<GenerateFootprintCommand> logger, ProjectGraphBuilder projectsGraphBuilder)
        {
            _logger = logger;
            _projectGraphBuilder = projectsGraphBuilder;
        }

        public CommandResult Run(GenerateFootprintOptions options)
        {
            if (!Directory.Exists(options.CodeRoot))
            {
                _logger.LogError($"Directory ({options.CodeRoot}) specified by --{nameof(options.CodeRoot)} does not exist.");
                return CommandResult.Failure;
            }

            var filteredProjectDirs = CalculateFootprint(options);

            WriteFootprint(options, filteredProjectDirs);

            return CommandResult.Success;
        }

        private ImmutableSortedSet<string> CalculateFootprint(GenerateFootprintOptions options)
        {
            var projectGraph = _projectGraphBuilder.BuildProjectGraph(options.InputFiles, false);

            var codeRoot = Path.GetFullPath(options.CodeRoot);
            var projectDirs = projectGraph
                .Nodes
                .Select(p => Directory.GetParent(p.NormalisedPath).FullName)
                .Distinct();

            var footprint = RelativeDirChildrenFilter
                .FilterChildrenAndAddDirSeparator(projectDirs)
                .Select(p => Path.GetRelativePath(codeRoot, p)).ToHashSet();

            foreach (var project in projectGraph.Nodes)
            {
                var projectFile = project.ProjectDetails.Project;
                var projectDir = Path.GetDirectoryName(project.FilePath);
                var nugetPackageFolder = project.ProjectDetails.Project.GetPropertyValue("NuGetPackageRoot");

                foreach (var import in projectFile.Imports)
                {
                    if (import.IsImported
                        && import.ImportedProject.FullPath.StartsWith(codeRoot)
                        && !import.ImportedProject.FullPath.StartsWith(projectDir)
                        && !import.ImportedProject.FullPath.StartsWith(nugetPackageFolder))
                    {
                        footprint.Add(Path.GetRelativePath(codeRoot, import.ImportedProject.FullPath));
                    }
                }
            }

            return footprint.ToImmutableSortedSet();
        }

        private void WriteFootprint(GenerateFootprintOptions options, ImmutableSortedSet<string> footprint)
        {
            using (var writer = new StreamWriter(options.OutputFile, false, Encoding.UTF8))
            {
                foreach (var entry in footprint)
                {
                    if (entry.EndsWith("/"))
                    {
                        _logger.LogInformation($"Found directory in footprint: {entry}");
                    }
                    else
                    {
                        _logger.LogInformation($"Found file in footprint: {entry}");
                    }
                    writer.WriteLine(entry);
                }
            }

            _logger.LogInformation($"Written footprint to {options.OutputFile}");
        }
    }
}
