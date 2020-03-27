using System.Collections.Generic;
using System.Linq;

namespace SolutionValidator.DependencyGraph
{
    public class Graph<T> where T : INode<T>
    {
        private readonly Dictionary<int, T> _nodes = new Dictionary<int, T>();
        private readonly SortedDictionary<int, List<T>> _heightList = new SortedDictionary<int, List<T>>();
        private readonly List<T> _invalidNodes = new List<T>();
        private readonly HashSet<string> _projectFiles = new HashSet<string>();
        private readonly HashSet<ProjectDetails> _projects = new HashSet<ProjectDetails>();

        public Graph()
        {
        }

        public Graph(T node)
        {
            AddNode(node);
        }

        public Graph(IEnumerable<T> nodes)
        {
            foreach (var node in nodes)
            {
                AddNode(node);
            }
        }

        /// <summary>
        /// Returns the number of nodes that were added to the graph.
        /// </summary>
        public int AddNode(T node)
        {
            int nodesAdded = 0;
            if (_nodes.ContainsKey(node.Id))
            {
                return nodesAdded;
            }

            AddNodeInternal(node);
            nodesAdded++;

            foreach (var dependentNode in node.References)
            {
                if (!_nodes.ContainsKey(dependentNode.Id))
                {
                    AddNodeInternal(dependentNode);
                    nodesAdded++;
                }
            }

            foreach (var transitiveReference in node.TransitiveReferences)
            {
                if (!_nodes.ContainsKey(transitiveReference.Id))
                {
                    AddNodeInternal(transitiveReference);
                    nodesAdded++;
                }
            }

            return nodesAdded;
        }


        /// <summary>
        /// Remove a node and all nodes that depend on it from the graph
        /// </summary>
        /// <returns>The number of nodes removed</returns>
        public int RemoveSubtree(T node)
        {
            int nodesRemoved = 0;
            if (!_nodes.ContainsKey(node.Id))
            {
                return nodesRemoved;
            }

            RemoveNodeInternal(node);
            nodesRemoved++;

            // Look through all nodes in reverse topological order and remove any that depend on the removed node
            // Stop if we reach any node at the same height or higher than the removed node as they cannot depend on it
            var candidateDependingNodes = ReverseTopologicalSort().Where(x => x.Height > node.Height).ToArray();
            foreach (var dependingNode in candidateDependingNodes)
            {
                if (dependingNode.AllReferences.Contains(node))
                {
                    RemoveNodeInternal(dependingNode);
                    nodesRemoved++;
                }
            }
            return nodesRemoved;
        }

        private void RemoveNodeInternal(T node)
        {
            _projects.Remove(node.ProjectDetails);
            _projectFiles.Remove(node.NormalisedPath);
            _nodes.Remove(node.Id);

            _heightList.TryGetValue(node.Height, out List<T> nodesAtHeight);

            nodesAtHeight.Remove(node);
            if (nodesAtHeight.Count == 0)
            {
                _heightList.Remove(node.Height);
            }
        }

        private void AddNodeInternal(T node)
        {
            _projects.Add(node.ProjectDetails);
            _projectFiles.Add(node.NormalisedPath);
            _nodes.Add(node.Id, node);

            if (!node.Valid)
            {
                _invalidNodes.Add(node);
            }

            if (!_heightList.TryGetValue(node.Height, out List<T> nodesAtHeight))
            {
                nodesAtHeight = new List<T>();
                _heightList.Add(node.Height, nodesAtHeight);
            }

            nodesAtHeight.Add(node);
        }

        public IReadOnlyList<T> InvalidNodes => _invalidNodes;

        public IEnumerable<T> Nodes => _nodes.Values;

        public IEnumerable<int> Keys => _nodes.Keys;

        public IEnumerable<ProjectDetails> Projects => _projects;

        public IEnumerable<T> TopologicalSort()
        {
            return _heightList.Values.Reverse().SelectMany(i => i);
        }

        public IEnumerable<T> ReverseTopologicalSort()
        {
            return _heightList.Values.SelectMany(i => i);
        }

        public IEnumerable<IReadOnlyList<T>> EnumerateTopDown()
        {
            return _heightList.Values.Reverse();
        }

        public IEnumerable<IReadOnlyList<T>> EnumerateBottomUp()
        {
            return _heightList.Values;
        }

        public bool Contains(int id)
        {
            return _nodes.ContainsKey(id);
        }

        public bool Contains(string filePath)
        {
            return _projectFiles.Contains(filePath);
        }

        public bool TryGetNode(int id, out T node)
        {
            return _nodes.TryGetValue(id, out node);
        }

        public int Count => _nodes.Count;

        public int MaxHeight => Count > 0 ? _heightList.Keys.Max() : 0;

        public bool TryGetNodesAtHeight(int height, out IReadOnlyList<T> nodes)
        {
            List<T> nodesAtHeight;
            bool result = _heightList.TryGetValue(height, out nodesAtHeight);

            nodes = nodesAtHeight;
            return result;
        }

    }
}
