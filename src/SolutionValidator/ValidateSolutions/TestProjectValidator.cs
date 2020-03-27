using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using SolutionValidator.DependencyGraph;

namespace SolutionValidator.ValidateSolutions
{
    public class TestProjectValidator : ISolutionValidator
    {
        private readonly ILogger<TestProjectValidator> _logger;
        private readonly TestProjectFinder _testFinder;
        private readonly GraphBuilder _graphBuilder;
        private readonly ProjectLoader _projectLoader;

        public TestProjectValidator(ILogger<TestProjectValidator> logger, TestProjectFinder testFinder, GraphBuilder graphBuilder, ProjectLoader projectLoader)
        {
            _logger = logger;
            _testFinder = testFinder;
            _graphBuilder = graphBuilder;
            _projectLoader = projectLoader;
        }

        public ValidationResult Validate(ValidationContext validationContext)
        {
            if (validationContext.SolutionName.Contains("NoTests"))
            {
                _logger.LogInformation("Validating that 'NoTests' solution {solutionName} contains no test projects", validationContext.SolutionName);

                var testProjects = validationContext.Solution.GetMsBuildProjectsInSolution().Where(p => p.ProjectName.IsTestProjectName()).ToList();

                if (testProjects.Count > 0)
                {
                    _logger.LogWarning("Found {testProjectCount} test projects in 'NoTests' solution.\n * {testProjects}",
                        testProjects.Count, string.Join(System.Environment.NewLine + " * ", testProjects));
                    return ValidationResult.Failure;
                }

                _logger.LogInformation("Found no test projects in {solutionName}", validationContext.SolutionName);
                return ValidationResult.Success;
            }
            else
            {
                var projectPaths = validationContext.Solution.GetMsBuildProjectsInSolution().Select(p => new FilePath(p.AbsolutePath)).ToList();

                var missingTestProjects = GetMissingTestProjects(projectPaths, validationContext.SolutionName);

                return missingTestProjects.Any() ? ValidationResult.Failure : ValidationResult.Success;
            }
        }

        public List<FilePath> GetMissingTestProjects(List<FilePath> projectPaths, string solutionName)
        {
            HashSet<string> testProjectsInSolution = projectPaths.Where(p => p.Name.IsTestProjectName()).Select(p => p.NormalisedPath).ToHashSet();

            var nonTestProjects = projectPaths.Where(p => !testProjectsInSolution.Contains(p)).ToList();

            if (nonTestProjects.Count == 0)
            {
                _logger.LogWarning("Unable to find any non-test projects in {solutionName}", solutionName);
                return new List<FilePath>();
            }

            var nonTestProjectGraph = _graphBuilder.GenerateGraph(nonTestProjects);

            HashSet<string> testProjectsFoundByConvention = new HashSet<string>();
            List<FilePath> missingTestProjects = new List<FilePath>();

            foreach (var project in nonTestProjects)
            {
                if (!File.Exists(project))
                {
                    _logger.LogError("Referenced project {project} does not exist. Unable to search for associated test projects", project);
                    continue;
                }

                foreach (var testProject in _testFinder.GetTestProjects(project))
                {
                    if (!_projectLoader.TryGetProject(testProject, out ProjectDetails projectDetails))
                    {
                        _logger.LogError("Unable to load {testProject}", testProject);
                        missingTestProjects.Add(testProject);
                        continue;
                    }

                    if (!LeanSolutionValidator.IsTestProjectForProjectInGraph(projectDetails, nonTestProjectGraph))
                    {
                        _logger.LogWarning("Found test project {testProject} but this is not a test for a project in the project graph so not adding.", testProject);
                        continue;
                    }

                    testProjectsFoundByConvention.Add(testProject);

                    if (!testProjectsInSolution.Contains(testProject))
                    {
                        missingTestProjects.Add(testProject);
                        _logger.LogError("Found test project {testProject} which is not part of solution", testProject);
                    }
                }
            }

            // Find the set of projects which we didn't managed to find on the disk by convention
            testProjectsInSolution.ExceptWith(testProjectsFoundByConvention);
            if (testProjectsInSolution.Count > 0)
            {
                // Do I need to add extra heuristics or can we just sort out the convention?
                _logger.LogWarning("Found {testProjectsNotMeetingConvention} projects which I did not find by convention. Do I need to add extra heuristics or can we just sort out the convention?.\n * {testsBreakingConvention}",
                    testProjectsInSolution.Count, string.Join(System.Environment.NewLine + " * ", testProjectsInSolution));
            }

            return missingTestProjects;
        }
    }
}
