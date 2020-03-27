using System.Collections.Generic;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;

namespace SolutionValidator.ValidateSolutions
{
    /// <summary>
    /// Check that all projects are included in the solution
    /// </summary>
    public class SolutionClosureValidator : ISolutionValidator
    {
        private readonly ILogger<SolutionClosureValidator> _logger;
        private readonly ProjectLoader _projectLoader;

        public SolutionClosureValidator(ILogger<SolutionClosureValidator> logger, ProjectLoader projectLoader)
        {
            _logger = logger;
            _projectLoader = projectLoader;
        }

        public ValidationResult Validate(ValidationContext validationContext)
        {
            var result = ValidationResult.Success;

            var projects = new Dictionary<string, ProjectInSolution>();
            foreach (var solutionProject in validationContext.Solution.GetMsBuildProjectsInSolution())
            {
                projects[System.IO.Path.GetFullPath(solutionProject.AbsolutePath)] = solutionProject;
            }

            foreach (var solutionProject in validationContext.Solution.GetMsBuildProjectsInSolution())
            {
                if (_projectLoader.TryGetProject(solutionProject.AbsolutePath, out ProjectDetails projectDetails))
                {
                    foreach (var fullPath in projectDetails.ProjectReferences)
                    {
                        if (!projects.ContainsKey(fullPath))
                        {
                            // TODO: Add the error to the ValidationContext;
                            _logger.LogError("Project '{referenceProject}' is referenced in '{Project}' but is not present in Solution", fullPath, solutionProject.AbsolutePath);
                            result = ValidationResult.Failure;
                        }
                    }
                }
                else
                {
                    _logger.LogError("Project '{projectPath}' referenced in solution does not exist.", solutionProject.AbsolutePath);
                    result = ValidationResult.Failure;
                }
            }

            return result;
        }
    }
}
