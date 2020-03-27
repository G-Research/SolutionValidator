using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Serilog;
using SolutionValidator.DependencyGraph;

namespace SolutionValidator.ValidateSolutions
{
    /// <summary>
    /// For solutions which represent a single runnable application the lean solution validator ensures that only projects which are part
    /// of the dependency graph for the entry point project and any associated test projects (including those only added by virtue of
    /// other test projects) are part of the solution. Executables and Libraries which are not referenced as part of this graph are
    /// considered extra projects and will fail validation.
    /// </summary>
    public class LeanSolutionValidator : ISolutionValidator
    {
        private readonly ILogger<LeanSolutionValidator> _logger;
        private readonly ProjectLoader _projectLoader;
        private readonly GraphBuilder _graphBuilder;

        public LeanSolutionValidator(ILogger<LeanSolutionValidator> logger, ProjectLoader projectLoader, GraphBuilder graphBuilder)
        {
            _logger = logger;
            _projectLoader = projectLoader;
            _graphBuilder = graphBuilder;
        }

        public ValidationResult Validate(ValidationContext validationContext)
        {
            var entryPointProject = validationContext.Solution.GetMsBuildProjectsInSolution().SingleOrDefault(p => p.ProjectName == validationContext.SolutionName);

            if (entryPointProject == null)
            {
                return ValidationResult.Success;
            }

            var fullPathToEntryPoint = new FilePath(entryPointProject.AbsolutePath);
            var projectGraph = _graphBuilder.GenerateGraph(fullPathToEntryPoint);
            var superfluousProjects = GetSuperfluousProjects(projectGraph, validationContext.Solution.GetMsBuildProjectsInSolution().Select(p => new FilePath(p.AbsolutePath)));

            if (superfluousProjects.Any())
            {
                _logger.LogError("Found {extraProjectCount} superfluous projects in {solution}: {extraProjects}", superfluousProjects.Count, validationContext.SolutionName, string.Join(",", superfluousProjects));
                return ValidationResult.Failure;
            }
            else
            {
                return ValidationResult.Success;
            }
        }

        public static bool IsTestProjectForProjectInGraph(ProjectDetails projectDetails, Graph<ProjectNode> projectGraph)
        {
            var filePath = projectDetails.FilePath;

            if (projectDetails.TryGetProjectUnderTest(out FilePath projectUnderTest))
            {
                if (projectGraph.Contains(projectUnderTest))
                {
                    Log.Debug("Project under test {name} is part of project tree.", projectUnderTest);
                    return true;
                }

                Log.Warning("{fullPath} is test project for {projectUnderTest} which is not part of project tree.", filePath, projectUnderTest);
                return false;
            }
            else
            {
                if (!projectDetails.ProjectReferences.Any(r => projectGraph.Contains(r)))
                {
                    Log.Debug("Cannot determine project under test for {testProject} and does not reference projects in the graph. Adding to list of superfluous projects.", filePath);
                    return false;
                }

                Log.Warning("Cannot determine project under test for {testProject} but it does reference projects in the graph. It is either badly named or simply not required. Consider renaming or removing explicitly!",
                    filePath);
                return true;
            }
        }

        public HashSet<FilePath> GetSuperfluousProjects(Graph<ProjectNode> projectGraph, IEnumerable<FilePath> projectsInSolution, Regex excludedProjectRegex = null)
        {
            var remainingProjects = projectsInSolution.Where(p => !projectGraph.Contains(p)).ToHashSet();

            bool converging = true;

            while (converging)
            {
                List<FilePath> referencedProjects = new List<FilePath>();
                foreach (var project in remainingProjects)
                {
                    if (projectGraph.Contains(project))
                    {
                        referencedProjects.Add(project);
                        continue;
                    }

                    if (_projectLoader.TryGetProject(project, out ProjectDetails projectDetails))
                    {
                        if (project.Name.IsTestProjectName())
                        {
                            if (IsTestProjectForProjectInGraph(projectDetails, projectGraph))
                            {
                                referencedProjects.Add(project);
                                _graphBuilder.AddNode(project, projectGraph);
                            }
                        }
                        else if (projectDetails.IsExecutable)
                        {
                            if (excludedProjectRegex == null || !excludedProjectRegex.IsMatch(projectDetails.ProjectName))
                            {
                                // Permit additional executables and associated graph as part of solution.
                                _logger.LogInformation("Found additional executable project {name} and adding to the project tree.", projectDetails.ProjectName);
                                referencedProjects.Add(project);
                                _graphBuilder.AddNode(project, projectGraph);
                            }
                            else
                            {
                                _logger.LogInformation("NOT adding additional executable project '{name}' as it matches the exclude regex.", projectDetails.ProjectName);
                            }
                        }
                    }
                }

                if (!referencedProjects.Any())
                {
                    converging = false;
                }
                else
                {
                    foreach (var project in referencedProjects)
                    {
                        remainingProjects.Remove(project);
                    }
                }
            }

            return remainingProjects;
        }
    }
}
