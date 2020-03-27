using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace SolutionValidator.DependencyGraph
{
    public class GraphBuilder
    {
        private readonly ILogger<GraphBuilder> _logger;
        private readonly ProjectLoader _projectLoader;

        private readonly Dictionary<string, ProjectNode> _cache = new Dictionary<string, ProjectNode>();

        public GraphBuilder(ILogger<GraphBuilder> logger, ProjectLoader projectLoader)
        {
            _logger = logger;
            _projectLoader = projectLoader;
        }

        public Graph<ProjectNode> GenerateGraph(IEnumerable<FilePath> projects)
        {
            _logger.LogDebug("Generating Dependency Graph");

            Graph<ProjectNode> graph = new Graph<ProjectNode>();

            foreach (var project in projects)
            {
                AddNode(project, graph);
            }

            _logger.LogDebug("Built project graph. Node count: {count} Max height: {maxHeight}.", graph.Count, graph.MaxHeight);

            if (graph.InvalidNodes.Count > 0)
            {
                _logger.LogError("Built graph but found {invalidNodes} invalid nodes.", graph.InvalidNodes.Count);
            }

            return graph;
        }

        public Graph<ProjectNode> GenerateGraph(FilePath fullPath)
        {
            return new Graph<ProjectNode>(GetNode(fullPath));
        }

        public ProjectNode GetNode(FilePath fullPath)
        {
            if (_cache.TryGetValue(fullPath, out ProjectNode projectNode))
            {
                return projectNode;
            }

            if (!_projectLoader.TryGetProject(fullPath, out ProjectDetails projectDetails))
            {
                _logger.LogError("Project at {projectFilePath} cannot be found. Cannot correctly build dependency graph - adding in Invalid node.", fullPath);
            }

            var references = projectDetails.ProjectReferences
                .Select(path => GetNode(path))
                .ToImmutableHashSet();

            var transitiveReferences = references
                .SelectMany(r => r.References.Concat(r.TransitiveReferences))
                .ToImmutableHashSet();

            if (references.Any(p => !p.Valid))
            {
                _logger.LogError("Project {projectFilePath} has invalid references to: '{projectPaths}'", fullPath, string.Join(",", references.Where(r => !r.Valid).Select(p => p.NormalisedPath)));
            }

            var newNode = new ProjectNode(projectDetails, references, transitiveReferences);
            _cache.Add(fullPath, newNode);
            return newNode;
        }

        public void AddNode(FilePath fullPath, Graph<ProjectNode> graph)
        {
            if (graph.Contains(fullPath))
            {
                return;
            }

            var node = GetNode(fullPath);
            graph.AddNode(node);
        }

        public (List<ProjectNode> InvalidNodes, Dictionary<int, Colour> NodeColours) MatchColours(Graph<ProjectNode> graph, ColourChart colourChart, bool addMissingColourAsPrimary)
        {
            Dictionary<int, Colour> nodeColours = new Dictionary<int, Colour>();
            // List of the nodes which first started failing as all dependents will fail and aren't very interesting.
            List<ProjectNode> invalidNodes = new List<ProjectNode>();

            foreach (var level in graph.EnumerateBottomUp())
            {
                foreach (var node in level)
                {
                    var dependencyColours = node.References.Select(r => nodeColours[r.Id]).ToArray();

                    // If I have invalid dependencies then there is no point doing any more calculations....
                    bool invalidDependencies = dependencyColours.Any(c => !c.IsValid);
                    if (invalidDependencies)
                    {
                        _logger.LogWarning("Dependencies are invalid so {nodeId} is too.", node.Id);
                        nodeColours[node.Id] = Colour.Invalid;
                        continue;
                    }

                    // Otherwise I know that the nodes below me are valid and that if I turn out to be invalid I should record myself!
                    if (colourChart.TryGetColour(node.Colour, out Colour nodeColour))
                    {
                        colourChart.TryGetNewColour(nodeColour, dependencyColours, out nodeColour);

                        if (!nodeColour.IsValid)
                        {
                            _logger.LogError("Unable to matching composite colour for Project {projectName}. Base colour = {nodeColour} DependentColours:{compositeColours}", node.Name, node.Colour, string.Join("", dependencyColours.Distinct().Select(c => $"\n * {c.Name}")));
                        }
                    }
                    else
                    {
                        if (addMissingColourAsPrimary)
                        {
                            _logger.LogInformation("Unable to find {colourName} in ColourChart for Project {name}. Adding as base colour.", node.Colour, node.Name);
                            nodeColour = colourChart.AddColour(node.Colour, "Base colour", new string[0]);
                        }
                        else
                        {
                            _logger.LogError("Unable to find {colourName} in ColourChart setting colour to 'Invalid' for project {name}", node.Colour, node.Name);
                            nodeColour = Colour.Invalid;
                        }
                    }

                    if (nodeColour != Colour.Invalid && node.ProjectDetails.ColourClashes != null)
                    {
                        foreach (var clashingColour in node.ProjectDetails.ColourClashes)
                        {
                            if (colourChart.TryGetColour(clashingColour, out Colour colourClash))
                            {
                                if ((colourClash.Value & nodeColour.Value) != 0)
                                {
                                    _logger.LogError("Project {projectName} depends on colour {colourClash} which which clashes with {nodeColour} depending on. Marking as invalid", node.Name, colourClash, nodeColour);
                                    nodeColour = Colour.Invalid;
                                    break;
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Unable to find restricted dependency {clashingColour} in colour chart.", clashingColour);
                            }
                        }
                    }

                    _logger.LogInformation("Adding Project {name}. Id: {id} with Colour: {colour} to colour list.", node.Name, node.Id, nodeColour.Name);
                    nodeColours.Add(node.Id, nodeColour);

                    if (!nodeColour.IsValid)
                    {
                        invalidNodes.Add(node);
                    }
                }
            }

            return (invalidNodes, nodeColours);
        }
    }
}
