using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SolutionValidator.DependencyGraph;

namespace SolutionValidator.ValidateSolutions
{
    public class TestFrameworkValidator : ISolutionValidator
    {
        private readonly ILogger<TestFrameworkValidator> _logger;
        private readonly ProjectTargetGraphBuilder _projectTargetGraphBuilder;
        private readonly ProjectLoader _projectLoader;

        public TestFrameworkValidator(ILogger<TestFrameworkValidator> logger, ProjectTargetGraphBuilder projectTargetGraphBuilder, ProjectLoader projectLoader)
        {
            _logger = logger;
            _projectTargetGraphBuilder = projectTargetGraphBuilder;
            _projectLoader = projectLoader;
        }

        public ValidationResult Validate(ValidationContext validationContext)
        {
            var projects = validationContext.Solution.GetMsBuildProjectsInSolution().Select(p => new FilePath(p.AbsolutePath)).ToList();

            var graph = _projectTargetGraphBuilder.GenerateGraph(projects);

            if (graph.InvalidNodes.Any())
            {
                _logger.LogError("Invalid nodes present in the dependency graph for {solution}. Skipping validation as we cannot perform framework resolution.", validationContext.SolutionName);
                return ValidationResult.Failure;
            }

            var referencedFrameworks = GetReferencedFrameworks(graph);

            // for every test project find associated non-test project and look up frameworks...
            bool valid = true;
            foreach (var testProjectPath in projects.Where(p => p.Name.IsTestProjectName()))
            {
                _projectLoader.TryGetProject(testProjectPath, out ProjectDetails testProject);
                var missingFrameworks = GetMissingTestFrameworks(testProject, referencedFrameworks);
                if (missingFrameworks.Any())
                {
                    _logger.LogError("Test Project {testProject} is missing framework(s) [{frameworks}]", testProject.ProjectName, string.Join(',', missingFrameworks));
                    valid = false;
                }
            }

            return valid ? ValidationResult.Success : ValidationResult.Failure;
        }

        public static Dictionary<int, HashSet<TargetFramework>> GetReferencedFrameworks(Graph<ProjectTarget> graph)
        {
            var referenceFrameworks = new Dictionary<int, Dictionary<int, HashSet<TargetFramework>>>();

            HashSet<TargetFramework> GetFrameworksForNode(ProjectTarget projectTarget)
            {
                if (!referenceFrameworks.TryGetValue(projectTarget.ProjectDetails.Id, out Dictionary<int, HashSet<TargetFramework>> projectTargetFrameworks))
                {
                    projectTargetFrameworks = new Dictionary<int, HashSet<TargetFramework>>();

                    referenceFrameworks.Add(projectTarget.ProjectDetails.Id, projectTargetFrameworks);
                }

                if (!projectTargetFrameworks.TryGetValue(projectTarget.Id, out HashSet<TargetFramework> frameworks))
                {
                    frameworks = new HashSet<TargetFramework>();
                    projectTargetFrameworks.Add(projectTarget.Id, frameworks);
                }

                return frameworks;
            }

            foreach (var level in graph.EnumerateTopDown())
            {
                foreach (var project in level)
                {
                    if (project.IsTestProject)
                    {
                        continue;
                    }

                    HashSet<TargetFramework> frameworks = GetFrameworksForNode(project);
                    frameworks.Add(project.TargetFramework);

                    foreach (var child in project.References.Where(p => !p.IsTestProject))
                    {
                        var childFrameworks = GetFrameworksForNode(child);
                        childFrameworks.UnionWith(frameworks);
                    }
                }
            }

            Dictionary<int, HashSet<TargetFramework>> frameworksByProject = new Dictionary<int, HashSet<TargetFramework>>();
            foreach (var entry in referenceFrameworks)
            {
                HashSet<TargetFramework> frameworks = null;

                foreach (var frameworkSet in entry.Value.Values)
                {
                    if (frameworks == null)
                    {
                        frameworks = frameworkSet;
                    }
                    else
                    {
                        frameworks.UnionWith(frameworkSet);
                    }
                }

                frameworksByProject.Add(entry.Key, frameworks);
            }

            return frameworksByProject;
        }

        public HashSet<TargetFramework> GetMissingTestFrameworks(ProjectDetails testProject, Dictionary<int, HashSet<TargetFramework>> referenceFrameworks)
        {
            var missingFrameworks = new HashSet<TargetFramework>();

            // Find test project based on name
            if (!testProject.TryGetProjectUnderTest(out FilePath projectUnderTest))
            {
                _logger.LogWarning("Unable to validate framework versions for {projectName}", testProject.ProjectName);
                return missingFrameworks;
            }

            var referenceProjectPath = testProject.ProjectReferences.Single(pr => pr.Name == projectUnderTest.Name);

            _projectLoader.TryGetProject(referenceProjectPath, out ProjectDetails referenceProject);

            if (!referenceFrameworks.TryGetValue(referenceProject.Id, out HashSet<TargetFramework> frameworks))
            {
                _logger.LogWarning("Cannot find project {referenceProject} in dictionary of reference frameworks.", referenceProject.ProjectName);
                return missingFrameworks;
            }

            var testFrameworks = testProject.TargetFrameworks;
            missingFrameworks.Clear();
            foreach (var framework in frameworks)
            {
                // if net framework - then any net framework version of same major version.
                // if netstandard2.0  -> requiring at least one netcore framework seems reasonable.
                // if netstandard2.1+ -> netcore3+
                // if netcoreapp -> then any matching major version which is greater or equal to version of project under test

                if (framework.FrameworkType == FrameworkType.NetFramework)
                {
                    if (testFrameworks.All(f => f.FrameworkType != FrameworkType.NetFramework))
                    {
                        missingFrameworks.Add(framework);
                    }
                }
                else if (framework.FrameworkType == FrameworkType.NetCore)
                {
                    var versions = testFrameworks.Where(f => f.FrameworkType == FrameworkType.NetCore).Select(f => f.Version);

                    if (!versions.Any(v => v.Major == framework.Version.Major && v.Minor >= framework.Version.Minor))
                    {
                        missingFrameworks.Add(framework);
                    }
                }
                else if (framework.FrameworkType == FrameworkType.NetStandard)
                {
                    var versions = testFrameworks.Where(f => f.FrameworkType == FrameworkType.NetCore).Select(f => f.Version);
                    if (framework.Version == Version.Parse("2.0"))
                    {
                        if (!versions.Any())
                        {
                            missingFrameworks.Add(new TargetFramework("netcoreapp2.1"));
                        }
                    }
                    else
                    {
                        if (versions.All(v => v.Major != 3))
                        {
                            missingFrameworks.Add(new TargetFramework("netcoreapp3.1"));
                        }
                    }
                }
                else
                {
                    _logger.LogError("Unsupported framework: {framework}", framework);
                    missingFrameworks.Add(framework);
                }
            }

            if (missingFrameworks.Any() && testProject.Project.GetPropertyValue("SuppressFrameworkValidationFailure")?.ToLower() == "true")
            {
                _logger.LogWarning("Project {testProject} is missing frameworks [{frameworks}] but has 'SuppressFrameworkValidationFailure' == true so treating as if there are no missing frameworks.", testProject.ProjectName, string.Join(',', missingFrameworks));
                return new HashSet<TargetFramework>();
            }

            return missingFrameworks;
        }
    }
}
