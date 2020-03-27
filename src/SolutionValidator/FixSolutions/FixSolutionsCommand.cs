using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using SlnUtils;
using SolutionValidator.DependencyGraph;
using SolutionValidator.ValidateSolutions;

namespace SolutionValidator.FixSolutions
{
    public class FixSolutionsCommand
    {
        private readonly TestProjectFinder _testProjectFinder;
        private readonly ILogger _logger;
        private readonly ProjectLoader _projectLoader;
        private readonly GraphBuilder _graphBuilder;
        private readonly TestProjectValidator _testProjectValidator;
        private readonly LeanSolutionValidator _leanSolutionValidator;
        private readonly FrameworkFixer _frameworkFixer;

        public FixSolutionsCommand(ILogger<FixSolutionsCommand> logger,
            TestProjectFinder testProjectFinder,
            ProjectLoader projectLoader,
            GraphBuilder graphBuilder,
            TestProjectValidator testProjectValidator,
            LeanSolutionValidator leanSolutionValidator,
            FrameworkFixer frameworkFixer)
        {
            _logger = logger;
            _testProjectFinder = testProjectFinder;
            _projectLoader = projectLoader;
            _graphBuilder = graphBuilder;
            _testProjectValidator = testProjectValidator;
            _leanSolutionValidator = leanSolutionValidator;
            _frameworkFixer = frameworkFixer;
        }

        internal CommandResult Run(FixSolutionsCommandOptions fixSolutionCommandOptions)
        {
            foreach (var solution in SolutionFinder.GetSolutions(fixSolutionCommandOptions.Solutions, fixSolutionCommandOptions.ExcludePatterns.ToArray()))
            {
                var solutionFile = solution.SlnFile;

                // Map full path to project file
                var projectDictionary = solutionFile.Projects.Where(p => p.TypeGuid != ProjectTypeGuids.SolutionFolderGuid)
                    .ToDictionary(p => FilePath.Create(solutionFile.BaseDirectory, p.FilePath));

                if (projectDictionary.Count == 0)
                {
                    _logger.LogWarning("Solution {solution} has no Projects. No fix for this is possible!", solution);
                    continue;
                }

                bool solutionChanged = FixSolutionConfigurations(solutionFile);

                solutionChanged |= RemoveDeletedProjects(solutionFile, projectDictionary);

                solutionChanged |= PruneSolution(solutionFile, projectDictionary);

                solutionChanged |= AddMissingProjects(solutionFile, projectDictionary);

                if (!Path.GetFileName(solutionFile.FullPath).Contains("NoTest"))
                {
                    solutionChanged |= AddMissingTestProjects(solutionFile, projectDictionary);
                }
                else
                {
                    solutionChanged |= RemoveTestProjects(solutionFile, projectDictionary);
                }

                solutionChanged |= AddMissingProjectBuildConfigurations(solutionFile, projectDictionary);

                _frameworkFixer.FixInvalidFrameworks(solutionFile, projectDictionary);

                if (_frameworkFixer.AddMissingTestFrameworks(solutionFile, projectDictionary))
                {
                    _logger.LogInformation("Added missing test targets to {solution}. Refixing any frameworks which are now invalid based on test only library references.", solution);
                    _frameworkFixer.FixInvalidFrameworks(solutionFile, projectDictionary);
                }

                if (solutionChanged)
                {
                    _logger.LogInformation("Solution {solution} has been 'fixed'. Writing file to disk.", solution);
                    solutionFile.Write();
                }
            }

            return CommandResult.Success;
        }

        private bool AddMissingProjectBuildConfigurations(SlnFile solutionFile, Dictionary<FilePath, SlnProject> projectDictionary)
        {
            bool dirty = false;
            var buildConfigurations = solutionFile.SolutionConfigurationsSection.Values;

            foreach (var projectInSolution in solutionFile.GetProjectsInSolution())
            {
                var id = projectInSolution.Value.Id;
                var projectConfigurations = solutionFile.ProjectConfigurationsSection.SingleOrDefault(s => s.Id == id);

                if (projectConfigurations == null)
                {
                    projectConfigurations = new SlnPropertySet(id);
                    solutionFile.ProjectConfigurationsSection.Add(projectConfigurations);
                    dirty = true;
                }

                foreach (var buildConfiguration in buildConfigurations)
                {
                    foreach (var keyString in new[] { "Build.0", "ActiveCfg" })
                    {
                        dirty |= projectConfigurations.TryAdd($"{buildConfiguration}.{keyString}", buildConfiguration);
                    }
                }
            }

            return dirty;
        }

        private bool FixSolutionConfigurations(SlnFile solutionFile)
        {
            // This is not super efficient but is probably fine for our purposes.
            // Currently only removes dodgy configuration rather than adding in missing project configuration.
            var invalidConfigurationSections = solutionFile.SolutionConfigurationsSection.Where(s => !s.Key.Contains("Any CPU")).ToList();

            foreach (var invalidConfiguration in invalidConfigurationSections)
            {
                solutionFile.SolutionConfigurationsSection.Remove(invalidConfiguration.Key);

                foreach (var projectConfiguration in solutionFile.ProjectConfigurationsSection)
                {
                    foreach (var key in projectConfiguration.Keys.Where(k => k.Contains(invalidConfiguration.Key)).ToList())
                    {
                        projectConfiguration.Remove(key);
                    }
                }
            }

            return invalidConfigurationSections.Any();
        }

        private bool PruneSolution(SlnFile solutionFile, Dictionary<FilePath, SlnProject> projectDictionary)
        {
            var entryPointProject = solutionFile.Projects.SingleOrDefault(p => p.Name == Path.GetFileNameWithoutExtension(solutionFile.FullPath));

            if (entryPointProject == null)
            {
                return false;
            }

            bool dirty = false;
            var fullPathToEntryPoint = FilePath.Create(solutionFile.BaseDirectory, entryPointProject.FilePath);
            var projectGraph = _graphBuilder.GenerateGraph(fullPathToEntryPoint);

            foreach (var project in _leanSolutionValidator.GetSuperfluousProjects(projectGraph, projectDictionary.Keys))
            {
                var slnProject = projectDictionary[project];

                if (solutionFile.RemoveProject(slnProject.FilePath))
                {
                    dirty = true;
                }

                projectDictionary.Remove(project);
            }

            return dirty;
        }

        private bool RemoveTestProjects(SlnFile solutionFile, Dictionary<FilePath, SlnProject> projectDictionary)
        {
            var deletedProjects = new List<FilePath>();
            foreach (var projectPath in projectDictionary.Keys)
            {
                if (Path.GetFileNameWithoutExtension(projectPath).IsTestProjectName())
                {
                    if (!solutionFile.RemoveProject(projectDictionary[projectPath].FilePath))
                    {
                        _logger.LogError("Failed to remove {project} from solution.", projectPath);
                    }
                    deletedProjects.Add(projectPath);
                }
            }

            foreach (var deletedProject in deletedProjects)
            {
                projectDictionary.Remove(deletedProject);
            }

            return deletedProjects.Any();
        }

        private bool RemoveDeletedProjects(SlnFile slnFile, Dictionary<FilePath, SlnProject> projectDictionary)
        {
            var deletedProjects = new List<FilePath>();
            foreach (var projectPath in projectDictionary.Keys)
            {
                if (!File.Exists(projectPath))
                {
                    _logger.LogInformation("Project {projectName} cannot be found on disk. Removing from solution", projectPath);
                    if (!slnFile.RemoveProject(projectDictionary[projectPath].FilePath))
                    {
                        _logger.LogError("Failed to remove {project} from solution.", projectPath);
                    }
                    deletedProjects.Add(projectPath);
                }
            }

            foreach (var deletedProject in deletedProjects)
            {
                projectDictionary.Remove(deletedProject);
            }

            return deletedProjects.Any();
        }

        private bool AddMissingProjects(SlnFile slnFile, Dictionary<FilePath, SlnProject> projectDictionary)
        {
            bool dirty = false;
            var solutionFolders = new List<string>();
            // Generate the dependency tree and add any projects which are not present in the solution
            var graph = _graphBuilder.GenerateGraph(projectDictionary.Keys);
            foreach (var projectNode in graph.Nodes)
            {
                if (!projectDictionary.ContainsKey(projectNode.ProjectDetails.FilePath))
                {
                    if (slnFile.TryAddProject(projectNode.ProjectDetails.Project, solutionFolders, out SlnProject slnProject))
                    {
                        dirty = true;
                        projectDictionary.Add(projectNode.ProjectDetails.FilePath, slnProject);
                    }
                    else
                    {
                        _logger.LogError("Unable to add {project} to solution", projectNode.ProjectDetails.NormalisedPath);
                    }
                }
            }

            return dirty;
        }

        private bool AddMissingTestProjects(SlnFile slnFile, Dictionary<FilePath, SlnProject> projectDictionary)
        {
            var projectPaths = projectDictionary.Keys.ToList();
            var missingTestProjects = _testProjectValidator.GetMissingTestProjects(projectPaths, slnFile.GetSolutionName());

            bool dirty = false;
            foreach (var missingProject in missingTestProjects)
            {
                dirty |= AddTestProject(slnFile, projectDictionary, missingProject);
            }

            return dirty;
        }

        private bool AddTestProject(SlnFile slnFile, Dictionary<FilePath, SlnProject> projectDictionary, FilePath testProject)
        {
            bool dirty = false;
            if (!projectDictionary.ContainsKey(testProject))
            {
                _logger.LogInformation("Found missing test project {testProject}. Adding it and any missing dependencies to the solution.", testProject);
                if (!_projectLoader.TryGetProject(testProject, out ProjectDetails projectDetails))
                {
                    throw new Exception("Unable to load test project so can't fix solution");
                }

                var testProjectGraph = _graphBuilder.GenerateGraph(testProject);

                foreach (var node in testProjectGraph.Nodes)
                {
                    if (projectDictionary.ContainsKey(node.ProjectDetails.FilePath))
                    {
                        continue;
                    }

                    if (slnFile.TryAddProject(node.ProjectDetails.Project, new List<string>(), out SlnProject slnProject))
                    {
                        projectDictionary.Add(node.ProjectDetails.FilePath, slnProject);
                        dirty = true;

                        if (!node.ProjectDetails.IsTestProject)
                        {
                            foreach (var testProjectOfDependency in _testProjectFinder.GetTestProjects(node.ProjectDetails.FilePath))
                            {
                                dirty |= AddTestProject(slnFile, projectDictionary, testProjectOfDependency);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogError("Error attempting to add {testProject} project to solution.", testProject);
                    }
                }
            }

            return dirty;
        }
    }
}
