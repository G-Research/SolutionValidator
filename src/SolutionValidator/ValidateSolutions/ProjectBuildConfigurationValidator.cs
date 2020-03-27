using Microsoft.Extensions.Logging;

namespace SolutionValidator.ValidateSolutions
{
    public class ProjectBuildConfigurationValidator : ISolutionValidator
    {

        private ILogger _logger;

        public ProjectBuildConfigurationValidator(ILogger<ProjectBuildConfigurationValidator> logger)
        {
            _logger = logger;
        }

        public ValidationResult Validate(ValidationContext validationContext)
        {
            bool valid = true;

            _logger.LogInformation("Validating that all projects are present in all build configurations");

            foreach (var projectInSolution in validationContext.Solution.GetMsBuildProjectsInSolution())
            {
                if (projectInSolution.ProjectConfigurations.Count != validationContext.Solution.SolutionConfigurations.Count)
                {
                    _logger.LogError("Project {project} is not present in all Solution Configuration", projectInSolution.ProjectName);
                    valid = false;
                }
            }

            return valid ? ValidationResult.Success : ValidationResult.Failure;
        }
    }
}
