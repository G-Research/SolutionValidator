using System.Collections.Generic;
using System.Collections.Immutable;

namespace SolutionValidator.DependencyGraph
{
    /// <summary>
    /// Immutable type representing a node in a project dependency graph
    /// </summary>
    public class ProjectNode : Node<ProjectNode>
    {
        public bool IsExecutable => ProjectDetails.IsExecutable;
        public bool IsTestProject => ProjectDetails.IsTestProject;

        public FilePath FilePath => ProjectDetails.FilePath;

        public IReadOnlyList<TargetFramework> TargetFrameworks => ProjectDetails.TargetFrameworks;

        public string Colour => ProjectDetails.Colour;

        public string Name => ProjectDetails.ProjectName;

        public ProjectNode(ProjectDetails projectDetails, IImmutableSet<ProjectNode> references, IImmutableSet<ProjectNode> transitiveReferences)
            : base(projectDetails, references, transitiveReferences)
        {
        }
    }
}
