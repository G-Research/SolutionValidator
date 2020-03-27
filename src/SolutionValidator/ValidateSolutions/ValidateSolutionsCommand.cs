using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;

namespace SolutionValidator.ValidateSolutions
{
    public class ValidateSolutionsCommand : ICommand
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<ISolutionValidator> _validators;

        public ValidateSolutionsCommand(ILogger<ValidateSolutionsCommand> logger, IEnumerable<ISolutionValidator> validators)
        {
            _logger = logger;
            _validators = validators;
        }

        public CommandResult Run(ValidateSolutionsOptions options)
        {
            var commandResult = CommandResult.Success;

            Dictionary<string, Dictionary<string, ValidationResult>> resultSummary = new Dictionary<string, Dictionary<string, ValidationResult>>();

            Stopwatch stopwatch = new Stopwatch();
            foreach (var solution in SolutionFinder.GetSolutions(options.Solutions, options.ExcludePatterns.ToArray()))
            {
                var absolutePath = solution;
                SolutionFile solutionFile = SolutionFile.Parse(absolutePath);

                _logger.LogInformation("Loaded the solution file for '{solutionPath}'", absolutePath);

                Dictionary<string, ValidationResult> solutionResult = new Dictionary<string, ValidationResult>();
                resultSummary.Add(absolutePath, solutionResult);
                ValidationContext validationContext = new ValidationContext(absolutePath, solutionFile);

                foreach (ISolutionValidator validator in _validators)
                {
                    _logger.LogInformation("Running {validator} for {solution}", validator.GetType().Name, validationContext.SolutionName);

                    stopwatch.Restart();
                    ValidationResult result = ValidationResult.Failure;
                    try
                    {
                        result = validator.Validate(validationContext);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "Exception when running {validator} for {solution}", validator.GetType().Name, validationContext.SolutionName);
                    }
                    stopwatch.Stop();

                    solutionResult.Add(validator.GetType().Name, result);
                    if (result == ValidationResult.Failure)
                    {
                        _logger.LogError("{validator} failed for {solution}", validator.GetType().Name, validationContext.SolutionName);
                        commandResult = CommandResult.Failure;
                    }
                    else
                    {
                        _logger.LogInformation("{validator} for {solution} completed successfully in {time}", validator.GetType().Name, validationContext.SolutionName, stopwatch.Elapsed);
                    }
                }
            }

            StringBuilder summary = new StringBuilder();

            summary.Append("Validation Summary");
            foreach (var solutionSummary in resultSummary)
            {
                summary.Append($"\n* {solutionSummary.Key}:");
                foreach (var result in solutionSummary.Value)
                {
                    if (result.Value == ValidationResult.Success)
                    {
                        summary.Append($"\n    - {result.Key,-35} : [{result.Value.ToString().Green()}]");
                    }
                    else
                    {
                        summary.Append($"\n    - {result.Key,-35} : [{(result.Value.ToString().Red())}]");
                    }
                }
            }
            _logger.LogInformation(summary.ToString());

            if (commandResult == CommandResult.Failure)
            {
                StringBuilder fixCommandLine = new StringBuilder("dotnet solution-validator fix-solutions");
                fixCommandLine.Append($" --solutions {string.Join(" ", options.Solutions.Select(s => $"\"{s}\""))}");

                if (options.ExcludePatterns.Any())
                {
                    fixCommandLine.Append($" --exclude-patterns {string.Join(" ", options.ExcludePatterns.Select(p => $"\"{p}\""))}");
                }

                _logger.LogError("To fix validation failures run: '{fixCommandLine}'", fixCommandLine);
            }

            return commandResult;
        }
    }
}
