using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace SolutionValidator.DependencyGraph
{
    public static class ProjectTargetExtensions
    {
        public static int GetTargetId(this ProjectDetails projectDetails, TargetFramework targetFramework)
        {
            return targetFramework.FrameworkId * 10000 + projectDetails.Id; // Works as long as project count is under 10000 which seems reasonable at the moment!
        }
    }

    public class ProjectTarget : Node<ProjectTarget>
    {
        public override int Id { get; }

        public bool IsTestProject => ProjectDetails.IsTestProject;

        public FilePath FilePath => ProjectDetails.FilePath;

        public TargetFramework TargetFramework { get; set; }

        public IImmutableSet<TargetFramework> FrameworkSet { get; }

        public bool FrameworkSetIsValid { get; }

        public bool IsExistingProjectTarget { get; }

        public string Name => ProjectDetails.ProjectName;

        public ProjectTarget(ProjectDetails projectDetails, TargetFramework targetFramework, IImmutableSet<ProjectTarget> references, IImmutableSet<ProjectTarget> transitiveReferences)
            : base(projectDetails, references, transitiveReferences)
        {
            Id = projectDetails.GetTargetId(targetFramework);
            TargetFramework = targetFramework;
            FrameworkSet = AllReferences.Select(r => r.TargetFramework).Append(TargetFramework).ToImmutableHashSet();
            FrameworkSetIsValid = !(FrameworkSet.Any(t => t.FrameworkType == FrameworkType.NetCore) &&
                                    FrameworkSet.Any(t => t.FrameworkType == FrameworkType.NetFramework))
                                  && FrameworkSet.All(targetFramework.IsCompatibleFrameworkReference);

            IsExistingProjectTarget = ProjectDetails.TargetFrameworks.Any(t => t == TargetFramework);
            Valid = ProjectDetails.Valid && IsExistingProjectTarget && FrameworkSetIsValid && References.All(r => r.Valid);
        }
    }
}
