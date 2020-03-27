using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace SolutionValidator.ValidateSolutions
{
    public class DuplicateProjectValidator : ISolutionValidator
    {
        private readonly ILogger<DuplicateProjectValidator> _logger;

        public DuplicateProjectValidator(ILogger<DuplicateProjectValidator> logger)
        {
            _logger = logger;
        }

        public ValidationResult Validate(ValidationContext validationContext)
        {
            bool duplicateProjectsDetected = false;
            HashSet<string> projects = new HashSet<string>();
            foreach (var project in validationContext.Solution.GetMsBuildProjectsInSolution())
            {
                if (projects.Contains(project.ProjectName))
                {
                    _logger.LogError("Duplicate project name detected {projectName}", project.ProjectName);
                }
                else
                {
                    projects.Add(project.ProjectName);
                }
            }

            return duplicateProjectsDetected ? ValidationResult.Failure : ValidationResult.Success;
        }
    }
}
