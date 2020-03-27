using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace SolutionValidator.DependencyGraph
{
    public class ProjectTargetGraphBuilder
    {
        private readonly ILogger<ProjectTargetGraphBuilder> _logger;
        private readonly ProjectLoader _projectLoader;
        private readonly Dictionary<int, ProjectTarget> _projectTargetCache = new Dictionary<int, ProjectTarget>();

        public ProjectTargetGraphBuilder(ILogger<ProjectTargetGraphBuilder> logger, ProjectLoader projectLoader)
        {
            _logger = logger;
            _projectLoader = projectLoader;
        }

        public HashSet<ProjectDetails> GetProjectDetails(IEnumerable<FilePath> projectPaths)
        {
            HashSet<FilePath> allReferences = new HashSet<FilePath>();
            var projects = new HashSet<ProjectDetails>();

            foreach (var projectPath in projectPaths)
            {
                _projectLoader.TryGetProject(projectPath, out ProjectDetails project);
                projects.Add(project);
            }

            return projects;
        }

        public Graph<ProjectTarget> GenerateGraph(IEnumerable<FilePath> projectPaths)
        {
            var topLevelProjects = GetProjectDetails(projectPaths);
            return GenerateGraph(topLevelProjects);
        }

        public Graph<ProjectTarget> GenerateGraph(HashSet<ProjectDetails> projects)
        {
            var graph = new Graph<ProjectTarget>();
            foreach (var project in projects)
            {
                foreach (var targetFramework in project.TargetFrameworks)
                {
                    graph.AddNode(GetNode(targetFramework, project));
                }
            }

            return graph;
        }

        public ProjectTarget GetNode(TargetFramework referenceFramework, ProjectDetails project)
        {
            var id = project.GetTargetId(referenceFramework);
            if (_projectTargetCache.TryGetValue(id, out ProjectTarget projectTarget))
            {
                return projectTarget;
            }

            HashSet<ProjectTarget> references = new HashSet<ProjectTarget>();
            foreach (var reference in project.GetProjectReferences(referenceFramework))
            {
                if (!_projectLoader.TryGetProject(reference, out var referenceProject))
                {
                    _logger.LogError("Unable to find valid ProjectDetails for project reference {projectName}.", referenceProject.ProjectName);
                }

                if (!referenceFramework.TryGetBestMatchingTargetFramework(referenceProject.TargetFrameworks, out TargetFramework matchingFramework))
                {
                    _logger.LogError("Unable to find valid matching target framework for {referenceFramework} in {referenceProjectName} for project reference from {projectName}.", referenceFramework, referenceProject.ProjectName, project.ProjectName);

                    if (referenceFramework.TryGetClosestHigherFrameworkMatch(referenceProject.TargetFrameworks, out TargetFramework closestHigherMatch))
                    {
                        _logger.LogWarning("Found higher framework {closestHigherMatch} in {referenceProjectName} which we should use in preference to {referenceFramework} in {projectName}", closestHigherMatch, referenceProject.ProjectName, referenceFramework, project.ProjectName);
                        referenceFramework = closestHigherMatch;
                        matchingFramework = closestHigherMatch;
                    }
                    else
                    {
                        matchingFramework = referenceFramework;
                    }
                }

                references.Add(GetNode(matchingFramework, referenceProject));
            }

            var transitiveReferences = references
                .SelectMany(r => r.References.Concat(r.TransitiveReferences))
                .ToImmutableHashSet();

            projectTarget = new ProjectTarget(project, referenceFramework, references.ToImmutableHashSet(), transitiveReferences);
            _projectTargetCache.Add(projectTarget.Id, projectTarget);

            // In the event that we couldn't get a node for the original reference framework we need to cache the result to avoid repeating the node building process.
            if (id != projectTarget.Id)
            {
                _projectTargetCache.Add(id, projectTarget);
            }

            return projectTarget;
        }

        public void ClearCache()
        {
            // Could take a list of nodes which have been updated and allow those to be re-evaluated but the simplest way to avoid issues with out of date nodes (which only happens when we are fixing solutions) is to just clear the cache.
            _projectTargetCache.Clear();
        }
    }
}
