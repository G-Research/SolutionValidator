using Microsoft.Extensions.Logging;

namespace SolutionValidator.ValidateSolutions
{
    public class SolutionConfigurationValidator : ISolutionValidator
    {
        private ILogger _logger;

        public SolutionConfigurationValidator(ILogger<SolutionConfigurationValidator> logger)
        {
            _logger = logger;
        }

        public ValidationResult Validate(ValidationContext validationContext)
        {
            bool valid = true;

            if (validationContext.Solution.SolutionConfigurations.Count != 2)
            {
                _logger.LogError("Expecting 2 solution configurations to be present in solution: {solutionName} but found {configurationCount}", validationContext.SolutionName, validationContext.Solution.SolutionConfigurations.Count);
                valid = false;
            }

            if (validationContext.Solution.GetDefaultConfigurationName() != "Debug")
            {
                _logger.LogError("Debug is not default configuraton for solution: {solutionName}", validationContext.SolutionName);
                valid = false;
            }

            bool releaseConfigFound = false;
            foreach (var solutionConfiguration in validationContext.Solution.SolutionConfigurations)
            {

                _logger.LogInformation("Found solutionConfiguration {solutionConfiguration} in solution {solutionName}", solutionConfiguration.FullName, validationContext.SolutionName);
                // Check that all projects are present in the default configuration
                if (solutionConfiguration.PlatformName != "Any CPU")
                {
                    _logger.LogError("Only supported configuration are 'Any CPU'");
                    valid = false;
                }

                if (solutionConfiguration.ConfigurationName == "Release")
                {
                    releaseConfigFound = true;
                }
            }

            if (!releaseConfigFound)
            {
                _logger.LogError("Unable to find Release configuration in {solutionName}", validationContext.SolutionName);
                valid = false;
            }

            return valid ? ValidationResult.Success : ValidationResult.Failure;
        }
    }
}
