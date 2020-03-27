using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SolutionValidator.DependencyGraph
{
    public interface INode<T>
    {
        int Id { get; }
        bool Valid { get; }
        int Height { get; }
        IImmutableSet<T> References { get; }
        IImmutableSet<T> TransitiveReferences { get; }
        IImmutableSet<T> AllReferences { get; }
        string NormalisedPath { get; }
        ProjectDetails ProjectDetails { get; }
    }

    public class Node<T> : INode<T> where T : INode<T>
    {
        public ProjectDetails ProjectDetails { get; }
        public IImmutableSet<T> References { get; }
        public IImmutableSet<T> TransitiveReferences { get; }
        public IImmutableSet<T> AllReferences => References.Union(TransitiveReferences);

        public virtual int Id => ProjectDetails.Id;

        public bool Valid { get; protected set; }

        public string NormalisedPath => ProjectDetails.NormalisedPath;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is T other && other.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public IEnumerable<T> GetRequiredGraphNodes()
        {
            return References.Except(TransitiveReferences);
        }

        public int Height { get; }

        public Node(ProjectDetails projectDetails, IImmutableSet<T> references, IImmutableSet<T> transitiveReferences)
        {
            ProjectDetails = projectDetails;
            References = references;
            TransitiveReferences = transitiveReferences;
            Valid = projectDetails.Valid && References.All(r => r.Valid);
            Height = References.Any() ? 1 + References.Select(r => r.Height).Max() : 0;
        }
    }
}
