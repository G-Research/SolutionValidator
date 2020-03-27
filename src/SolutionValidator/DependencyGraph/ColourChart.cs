using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace SolutionValidator.DependencyGraph
{
    public class ColourChart
    {
        private readonly ILogger<ColourChart> _logger;
        private readonly Dictionary<int, Colour> _coloursByValue;
        private readonly Dictionary<string, Colour> _coloursByName;

        private readonly Dictionary<string, Dictionary<string, string>> _attributes = new Dictionary<string, Dictionary<string, string>>();

        private int _baseColourCount = 0;
        public ColourChart(ILogger<ColourChart> logger)
        {
            _logger = logger;
            var defaultColours = new[] { Colour.Default, Colour.Invalid };
            _coloursByValue = defaultColours.ToDictionary(c => c.Value);
            _coloursByName = defaultColours.ToDictionary(c => c.Name);
        }

        public bool TryGetColour(int value, out Colour colour)
        {
            return _coloursByValue.TryGetValue(value, out colour);
        }

        public bool TryGetColour(string name, out Colour colour)
        {
            return _coloursByName.TryGetValue(name, out colour);
        }

        public bool TryGetNewColour(Colour baseColour, IEnumerable<Colour> colours, out Colour colour)
        {
            int value = baseColour.Value;
            foreach (var constituentColour in colours)
            {
                value |= constituentColour.Value;
            }

            if (_coloursByValue.TryGetValue(value, out colour))
            {
                return true;
            }

            // Return invalid colour...
            colour = Colour.Invalid;
            return false;
        }

        public bool ContainsColour(string name)
        {
            return _coloursByName.ContainsKey(name);
        }

        public IEnumerable<Colour> Values => _coloursByName.Values;

        public Dictionary<string, string> GetAttributes(string colourName)
        {
            if (!_attributes.TryGetValue(colourName, out Dictionary<string, string> attributes))
            {
                attributes = new Dictionary<string, string>() {
                    { "shape", "rectangle" },
                    { "color", colourName.ToLower() },
                    { "style", "filled" }
                };

                if (colourName == "Default")
                {
                    attributes["color"] = "gold";
                }
                else if (colourName == "Invalid")
                {
                    attributes["color"] = "black";
                    attributes["fontcolor"] = "white";
                }
            }

            return attributes;
        }

        public Colour AddColour(string name, string description, IList<string> componentColours)
        {
            if (_coloursByName.ContainsKey(name))
            {
                throw new Exception($"Attempting to add Duplicate colour {name} to chart.");
            }

            Colour newColour;
            if (componentColours == null || componentColours.Count == 0)
            {
                if (_baseColourCount > 31)
                {
                    throw new Exception("Breached limit of base colours. We only support 31 base colours. Frankly if you want more you've probably done something wrong.");
                }

                int value = 1 << _baseColourCount++;
                _logger.LogInformation("Adding new base colour {name}:{value}.", name, value);
                newColour = new Colour(name, description, value);
            }
            else
            {
                int value = 0;
                foreach (var component in componentColours)
                {
                    value |= _coloursByName[component].Value;
                }

                newColour = new Colour(name, description, value);
            }

            _logger.LogInformation("Adding new colour to the colour chart. Name: {name} Value:{value}", name, newColour.Value);
            _coloursByName.Add(newColour.Name, newColour);
            _coloursByValue.Add(newColour.Value, newColour);
            return newColour;
        }

        // ReSharper disable once ClassNeverInstantiated.Local - Deserialised from json
        private class ColourConfig
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public IList<string> ComponentColours { get; set; }
            public Dictionary<string, string> Attributes { get; set; }
        }

        public void AddColoursFromConfig(string configFile)
        {
            string jsonConfig = File.ReadAllText(configFile, Encoding.UTF8);
#if NET6_0_OR_GREATER
            var options = new JsonSerializerOptions() { ReadCommentHandling = JsonCommentHandling.Skip, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
#else
            var options = new JsonSerializerOptions() { ReadCommentHandling = JsonCommentHandling.Skip, IgnoreNullValues = true };
#endif
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            var colourConfigList = JsonSerializer.Deserialize<List<ColourConfig>>(jsonConfig, options);

            Dictionary<string, Colour> colours = new Dictionary<string, Colour>();

            List<ColourConfig> processedConfiguration = new List<ColourConfig>();
            Action<ColourConfig> addToColourDictionary = (ColourConfig config) =>
            {
                processedConfiguration.Add(config);
                AddColour(config.Name, config.Description, config.ComponentColours);
            };

            while (colourConfigList.Count > 0)
            {
                processedConfiguration.Clear();

                foreach (var colourConfig in colourConfigList)
                {
                    if (colourConfig.ComponentColours == null || colourConfig.ComponentColours.Count() == 0)
                    {
                        if (!Colour.BuildInColours.Contains(colourConfig.Name))
                        {
                            // This is a base colour....                                               
                            addToColourDictionary(colourConfig);
                        }
                    }
                    else
                    {
                        bool canResolveColours = true;
                        foreach (string consistuentColour in colourConfig.ComponentColours)
                        {
                            if (!ContainsColour(consistuentColour))
                            {
                                canResolveColours = false;
                                break;
                            }
                        }

                        if (canResolveColours)
                        {
                            addToColourDictionary(colourConfig);
                        }
                    }
                }

                if (processedConfiguration.Count > 0)
                {
                    foreach (var config in processedConfiguration)
                    {
                        colourConfigList.Remove(config);

                        if (config.Attributes != null)
                        {
                            _attributes.Add(config.Name, config.Attributes);
                        }
                    }
                }
                else
                {
                    throw new Exception($"Invalid colour configuration. Unable to resolve dependencies for: {string.Join(',', colourConfigList.Select(c => c.Name))}.");
                }
            }

            _logger.LogInformation("Add all colours from configuration to colour chart.");
        }
    }
}
