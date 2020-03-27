using System.Linq;
using Microsoft.Extensions.Logging;
using SlnUtils;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SolutionValidator.TagSolutions
{
    public class TagSolutionsCommand : ICommand
    {
        private readonly ILogger _logger;

        public TagSolutionsCommand(ILogger<TagSolutionsCommand> logger)
        {
            _logger = logger;
        }

        public CommandResult Run(TagSolutionsOptions options)
        {
            foreach (var solution in SolutionFinder.GetSolutions(options.Solutions, options.ExcludePatterns.ToArray()))
            {
                SlnFile slnFile = solution.SlnFile;

                var extendedProperties = slnFile.Sections.SingleOrDefault(s => s.Id == "ExtendedSolutionProperties");

                if (extendedProperties == null)
                {
                    if (options.TagMode == TagMode.Remove)
                    {
                        continue;
                    }

                    extendedProperties = new SlnSection() { Id = "ExtendedSolutionProperties", SectionType = SlnSectionType.PreProcess };
                    slnFile.Sections.Add(extendedProperties);
                }

                if (extendedProperties.Properties.Keys.Contains("SolutionTags"))
                {
                    if (options.TagMode == TagMode.Set)
                    {
                        extendedProperties.Properties.SetValue("SolutionTags", string.Join(',', options.SolutionTags));
                    }
                    else
                    {
                        var existingTags = extendedProperties.Properties["SolutionTags"].Split(',').ToList();

                        foreach (var tag in options.SolutionTags)
                        {
                            if (existingTags.Contains(tag))
                            {
                                if (options.TagMode == TagMode.Remove)
                                {
                                    existingTags.Remove(tag);
                                }
                            }
                            else if (options.TagMode == TagMode.Add)
                            {
                                existingTags.Add(tag);
                            }
                        }

                        extendedProperties.Properties.SetValue("SolutionTags", string.Join(',', existingTags));
                    }
                }
                else
                {
                    if (options.TagMode == TagMode.Remove)
                    {
                        continue;
                    }

                    extendedProperties.Properties.SetValue("SolutionTags", string.Join(',', options.SolutionTags));
                }

                slnFile.Write();
            }

            return CommandResult.Success;
        }

    }
}
