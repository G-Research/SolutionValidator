using System.Threading.Tasks;
using Bulldog;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using SolutionValidator.BuildSolution;
using SolutionValidator.DependencyGraph;
using SolutionValidator.FixMergedSolution;
using SolutionValidator.FixSolutions;
using SolutionValidator.GenerateFootprint;
using SolutionValidator.GenerateGraph;
using SolutionValidator.ProjectReferences;
using SolutionValidator.TagSolutions;
using SolutionValidator.TrimFrameworks;
using SolutionValidator.TrimReferences;
using SolutionValidator.ValidateDependencyGraph;
using SolutionValidator.ValidateMergedSolution;
using SolutionValidator.ValidateProjectPaths;
using SolutionValidator.ValidateSolutions;

namespace SolutionValidator
{
    public class SolutionValidator : ToolBase<IValidatorOptions>
    {
        private const int Success = 0;
        private const int Failure = 1;
        private const int Cancelled = 2;

        protected override void ConfigureServices(IServiceCollection serviceCollection, IValidatorOptions options)
        {
            serviceCollection.AddSingleton<TestProjectFinder>();
            serviceCollection.AddSingleton<ProjectLoader>();
            serviceCollection.AddSingleton<GraphBuilder>();
            serviceCollection.AddSingleton<ProjectGraphBuilder>();
            serviceCollection.AddSingleton<ProjectTargetGraphBuilder>();
            serviceCollection.AddTransient<ColourChart>();
            serviceCollection.AddTransient<ValidateMergedSolutionCommand>();
            serviceCollection.AddTransient<GenerateDependencyGraphCommand>();
            serviceCollection.AddTransient<ValidateSolutionsCommand>();
            serviceCollection.AddTransient<ValidateDependencyGraphCommand>();
            serviceCollection.AddTransient<ValidateProjectPathsCommand>();
            serviceCollection.AddTransient<BuildSolutionCommand>();
            serviceCollection.AddTransient<GenerateFootprintCommand>();
            serviceCollection.AddTransient<FrameworkFixer>();
            serviceCollection.AddTransient<FixSolutionsCommand>();
            serviceCollection.AddTransient<FixMergedSolutionCommand>();
            serviceCollection.AddTransient<TagSolutionsCommand>();
            serviceCollection.AddTransient<TrimFrameworksCommand>();
            serviceCollection.AddTransient<TrimReferencesCommand>();
            serviceCollection.AddTransient<UpdateProjectReferencesCommand>();
            serviceCollection.AddTransient<YoloCommand>();
            serviceCollection.AddTransient<ISolutionValidator, SolutionConfigurationValidator>();
            serviceCollection.AddTransient<ISolutionValidator, ProjectBuildConfigurationValidator>();
            serviceCollection.AddTransient<ISolutionValidator, SolutionClosureValidator>();
            serviceCollection.AddTransient<ISolutionValidator, FrameworkValidator>();
            serviceCollection.AddTransient<TestProjectValidator>().AddTransient<ISolutionValidator, TestProjectValidator>();
            serviceCollection.AddTransient<ISolutionValidator, DuplicateProjectValidator>();
            serviceCollection.AddTransient<TestFrameworkValidator>().AddTransient<ISolutionValidator, TestFrameworkValidator>();
            serviceCollection.AddTransient<LeanSolutionValidator>().AddTransient<ISolutionValidator, LeanSolutionValidator>();
        }

        protected override bool TryGetOptions(string[] args, out IValidatorOptions options)
        {
            Parser parser = new Parser((config) =>
            {
                config.HelpWriter = null;
                config.CaseInsensitiveEnumValues = true;
            });

            var parserResult = parser.ParseArguments<ValidateSolutionsOptions,
                                                     ValidateMergedSolutionOptions,
                                                     ValidateDependencyGraphOptions,
                                                     GenerateDependencyGraphOptions,
                                                     ValidateProjectPathsOptions,
                                                     BuildSolutionCommandOptions,
                                                     GenerateFootprintOptions,
                                                     FixSolutionsCommandOptions,
                                                     FixMergedSolutionOptions,
                                                     TagSolutionsOptions,
                                                     TrimFrameworksOptions,
                                                     TrimReferencesOptions,
                                                     UpdateProjectReferencesOptions,
                                                     YoloOptions>(args);

            if (parserResult.Tag == ParserResultType.NotParsed)
            {
                options = null;
                Log.Warning($"Failed to parse command line:");
                Log.Information(HelpText.AutoBuild(parserResult));
                return false;
            }

            options = (IValidatorOptions)((Parsed<object>)parserResult).Value;

            return true;
        }

        protected override Task<int> Run(IValidatorOptions options)
        {
            CancellationTokenSource.Token.Register(() =>
            {
                Log.Error("Cancellation Requested. Terminating application.");
                System.Environment.Exit(Cancelled);
            });

            Logger.LogInformation("Attempting to find '{commandName}' command for '{optionsType}'", options.CommandName, options.GetType().Name);
            CommandResult commandResult;
            switch (options)
            {
                case BuildSolutionCommandOptions buildSolutionOptions:
                    {
                        var buildSolutionCommand = ServiceProvider.GetService<BuildSolutionCommand>();
                        commandResult = buildSolutionCommand.Run(buildSolutionOptions);
                        break;
                    }
                case GenerateFootprintOptions footprintOptions:
                    {
                        var footprintCommand = ServiceProvider.GetService<GenerateFootprintCommand>();
                        commandResult = footprintCommand.Run(footprintOptions);
                        break;
                    }
                case GenerateDependencyGraphOptions graphOptions:
                    {
                        var graphCommand = ServiceProvider.GetService<GenerateDependencyGraphCommand>();
                        commandResult = graphCommand.Run(graphOptions);
                        break;
                    }
                case ValidateSolutionsOptions validateSolutionOptions:
                    {
                        var validateSolutionCommand = ServiceProvider.GetService<ValidateSolutionsCommand>();
                        commandResult = validateSolutionCommand.Run(validateSolutionOptions);
                        break;
                    }
                case ValidateMergedSolutionOptions validateMergedSolutionOptions:
                    {
                        var validateMergedSolutionCommand = ServiceProvider.GetService<ValidateMergedSolutionCommand>();
                        commandResult = validateMergedSolutionCommand.Run(validateMergedSolutionOptions);
                        break;
                    }
                case ValidateDependencyGraphOptions validateDependencyGraphOptions:
                    {
                        var validateDependencyGraphCommand = ServiceProvider.GetService<ValidateDependencyGraphCommand>();
                        commandResult = validateDependencyGraphCommand.Run(validateDependencyGraphOptions);
                        break;
                    }
                case ValidateProjectPathsOptions validateProjectPathsOptions:
                    {
                        var validateProjectPathsCommand = ServiceProvider.GetService<ValidateProjectPathsCommand>();
                        commandResult = validateProjectPathsCommand.Run(validateProjectPathsOptions);
                        break;
                    }
                case FixSolutionsCommandOptions fixSolutionCommandOptions:
                    {
                        var fixSolutionsCommand = ServiceProvider.GetService<FixSolutionsCommand>();
                        commandResult = fixSolutionsCommand.Run(fixSolutionCommandOptions);
                        break;
                    }
                case FixMergedSolutionOptions fixMergedSolutionOptions:
                    {
                        var fixMergedSolutionCommand = ServiceProvider.GetService<FixMergedSolutionCommand>();
                        commandResult = fixMergedSolutionCommand.Run(fixMergedSolutionOptions);
                        break;
                    }
                case TagSolutionsOptions tagSolutionsOptions:
                    {
                        var tagSolutionsCommand = ServiceProvider.GetService<TagSolutionsCommand>();
                        commandResult = tagSolutionsCommand.Run(tagSolutionsOptions);
                        break;
                    }
                case TrimFrameworksOptions trimFrameworksOptions:
                    {
                        var tagSolutionsCommand = ServiceProvider.GetService<TrimFrameworksCommand>();
                        commandResult = tagSolutionsCommand.Run(trimFrameworksOptions);
                        break;
                    }
                case TrimReferencesOptions trimReferencesOptions:
                    {
                        var trimReferencesCommand = ServiceProvider.GetService<TrimReferencesCommand>();
                        commandResult = trimReferencesCommand.Run(trimReferencesOptions);
                        break;
                    }
                case UpdateProjectReferencesOptions updateProjectReferencesOptions:
                    {
                        var updateProjectReferences = ServiceProvider.GetService<UpdateProjectReferencesCommand>();
                        commandResult = updateProjectReferences.Run(updateProjectReferencesOptions);
                        break;
                    }
                case YoloOptions yoloOptions:
                    {
                        var yoloCommand = ServiceProvider.GetService<YoloCommand>();
                        commandResult = yoloCommand.Run(yoloOptions);
                        break;
                    }
                default:
                    throw new System.Exception($"Unable to find command for options {options.GetType()}");
            }

            if (commandResult == CommandResult.Success)
            {
                Logger.LogInformation("Execution of '{command}' has passed.", options.CommandName);
                return Task.FromResult(Success);
            }
            else
            {
                Logger.LogError("Execution of '{command}' has failed.", options.CommandName);
                return Task.FromResult(Failure);
            }
        }
    }
}
