using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /*public*/ partial class GraphTraverser
    {
        /// <summary>
        /// Adapter bridging <see cref="DataNode"/> to the generic traversal core.
        /// </summary>
        public readonly struct DataTraversalAdapter :
            ITraversalAdapter<DataNode, DataNodeId, DataNodeLinks, DataNodeEnumerator>
        {
            /// <inheritdoc cref="ITraversalAdapter{TNode, TId, TLinks, TNodeEnumerator}.GetId"/>
            public DataNodeId GetId(in DataNode node) => node.Id;
            /// <inheritdoc cref="ITraversalAdapter{TNode, TId, TLinks, TNodeEnumerator}.GetLinks"/>
            public DataNodeLinks GetLinks(in DataNode node, Direction direction) =>
                direction == Direction.Downwards ? node.Children : node.Parents;
            /// <inheritdoc cref="ITraversalAdapter{TNode, TId, TLinks, TNodeEnumerator}.GetLinkCount"/>
            public int GetLinkCount(in DataNodeLinks links) => links.Count;
            /// <inheritdoc cref="ITraversalAdapter{TNode, TId, TLinks, TNodeEnumerator}.GetLinkId"/>
            public DataNodeId GetLinkId(in DataNodeLinks links, int index) => links[index].Id;
            /// <inheritdoc cref="ITraversalAdapter{TNode, TId, TLinks, TNodeEnumerator}.Resolve"/>
            public DataNode Resolve(IReadOnlyGraph graph, DataNodeId id) => graph.DataNodes[id];
            /// <inheritdoc cref="ITraversalAdapter{TNode, TId, TLinks, TNodeEnumerator}.GetNodes"/>
            public DataNodeEnumerator GetNodes(IReadOnlyGraph graph) => graph.DataNodes.GetEnumerator();
            /// <inheritdoc cref="ITraversalAdapter{TNode, TId, TLinks, TNodeEnumerator}.SeedInDegrees"/>
            public void SeedInDegrees(IReadOnlyGraph graph, Direction direction, Dictionary<DataNodeId, int> inDegrees, Deque<DataNodeId> ready)
            {
                foreach (var node in graph.DataNodes)
                {
                    int inDegree = direction == Direction.Downwards ? node.Parents.Count : node.Children.Count;
                    inDegrees[node.Id] = inDegree;
                    if (inDegree == 0)
                        ready.AddBack(node.Id);
                }
            }
        }

        /// <summary>Enumerable for recursive traversal of data nodes.</summary>
        public readonly struct DataTraversalEnumerable
        {
            private readonly TraversalEnumerable<DataNode, DataNodeId, DataNodeLinks, DataNodeEnumerator, DataTraversalAdapter> m_Inner;

            internal DataTraversalEnumerable(in DataNode startNode, Direction direction, TraversalMethod method,
                IReadOnlyGraph graph, TraversalPools<DataNodeId> pools, OnVisitNode<DataNode> onVisit)
            {
                m_Inner = new TraversalEnumerable<DataNode, DataNodeId, DataNodeLinks, DataNodeEnumerator, DataTraversalAdapter>(
                    in startNode, direction, method, graph, pools, onVisit);
            }

            public TraversalEnumerable<DataNode, DataNodeId, DataNodeLinks, DataNodeEnumerator, DataTraversalAdapter>.Enumerator GetEnumerator() =>
                m_Inner.GetEnumerator();

            public void Execute() => m_Inner.Execute();
        }

        /// <summary>Enumerable for topological traversal of data nodes.</summary>
        public readonly struct TopologicalDataEnumerable
        {
            private readonly TopologicalEnumerable<DataNode, DataNodeId, DataNodeLinks, DataNodeEnumerator, DataTraversalAdapter> m_Inner;

            internal TopologicalDataEnumerable(IReadOnlyGraph graph, TraversalPools<DataNodeId> pools, Direction direction)
            {
                m_Inner = new TopologicalEnumerable<DataNode, DataNodeId, DataNodeLinks, DataNodeEnumerator, DataTraversalAdapter>(
                    graph, pools, direction);
            }

            public TopologicalEnumerable<DataNode, DataNodeId, DataNodeLinks, DataNodeEnumerator, DataTraversalAdapter>.Enumerator GetEnumerator() =>
                m_Inner.GetEnumerator();

            public void Execute() => m_Inner.Execute();
        }

        /// <summary>Enumerable for linear traversal of data nodes with optional filtering.</summary>
        public readonly struct LinearDataEnumerable
        {
            private readonly LinearEnumerable<DataNode, DataNodeId, DataNodeLinks, DataNodeEnumerator, DataTraversalAdapter> m_Inner;

            internal LinearDataEnumerable(IReadOnlyGraph graph, OnFilterNode<DataNode> onFilter)
            {
                m_Inner = new LinearEnumerable<DataNode, DataNodeId, DataNodeLinks, DataNodeEnumerator, DataTraversalAdapter>(graph, onFilter);
            }

            public LinearEnumerable<DataNode, DataNodeId, DataNodeLinks, DataNodeEnumerator, DataTraversalAdapter>.Enumerator GetEnumerator() =>
                m_Inner.GetEnumerator();

            public void Execute() => m_Inner.Execute();
        }

        /// <summary>
        /// Traverses all root data nodes in the graph.
        /// </summary>
        /// <returns> An enumerable object </returns>
        public LinearDataEnumerable TraverseDataRoots()
        {
            return new LinearDataEnumerable(m_Graph, node =>
            {
                if (node.Parents.Count == 0)
                    return TraversalControl.AcceptAndContinue;
                return TraversalControl.RejectAndContinue;
            });
        }

        /// <summary>
        /// Traverses all leaf data nodes in the graph.
        /// </summary>
        /// <returns> An enumerable object </returns>
        public LinearDataEnumerable TraverseDataLeaves()
        {
            return new LinearDataEnumerable(m_Graph, node =>
            {
                if (node.Children.Count == 0)
                    return TraversalControl.AcceptAndContinue;
                return TraversalControl.RejectAndContinue;
            });
        }

        /// <summary>
        /// Traverses data nodes recursively, starting from the specified node.
        /// </summary>
        /// <param name="node">The starting data node for traversal.</param>
        /// <param name="direction">The traversal direction (downwards or upwards).</param>
        /// <param name="method">The traversal method.</param>
        /// <param name="onVisit">Optional callback that is invoked when visiting each node. Return false to skip child traversal.</param>
        /// <returns>An enumerable object that can be used to traverse the data nodes.</returns>
        public DataTraversalEnumerable TraverseDataRecursive(DataNode node, Direction direction = Direction.Downwards,
            TraversalMethod method = TraversalMethod.DepthFirstPre, OnVisitNode<DataNode> onVisit = null)
        {
            return new DataTraversalEnumerable(in node, direction, method, m_Graph, m_DataPools, onVisit);
        }

        /// <summary>
        /// Traverses data nodes depth-first pre-order, starting from the specified node and moving downwards.
        /// </summary>
        /// <param name="node">The starting data node for traversal.</param>
        /// <param name="onVisit">Optional callback that is invoked when visiting each node. Return false to skip child traversal.</param>
        /// <returns>An enumerable object that can be used to traverse the data nodes.</returns>
        public DataTraversalEnumerable TraverseDataDownwards(DataNode node, OnVisitNode<DataNode> onVisit = null)
        {
            return TraverseDataRecursive(node, Direction.Downwards, TraversalMethod.DepthFirstPre, onVisit);
        }

        /// <summary>
        /// Traverses data nodes breadth-first, starting from the specified node and moving downwards.
        /// </summary>
        /// <param name="node">The starting data node for traversal.</param>
        /// <param name="onVisit">Optional callback that is invoked when visiting each node. Return false to skip child traversal.</param>
        /// <returns>An enumerable object that can be used to traverse the data nodes.</returns>
        public DataTraversalEnumerable TraverseDataDownwardsBreadthFirst(DataNode node, OnVisitNode<DataNode> onVisit = null)
        {
            return TraverseDataRecursive(node, Direction.Downwards, TraversalMethod.BreadthFirst, onVisit);
        }

        /// <summary>
        /// Traverses data nodes depth-first pre-order, starting from the specified node and moving upwards.
        /// </summary>
        /// <param name="node">The starting data node for traversal.</param>
        /// <param name="onVisit">Optional callback that is invoked when visiting each node. Return false to skip parent traversal.</param>
        /// <returns>An enumerable object that can be used to traverse the data nodes.</returns>
        public DataTraversalEnumerable TraverseDataUpwards(DataNode node, OnVisitNode<DataNode> onVisit = null)
        {
            return TraverseDataRecursive(node, Direction.Upwards, TraversalMethod.DepthFirstPre, onVisit);
        }

        /// <summary>
        /// Traverses data nodes breadth-first, starting from the specified node and moving upwards.
        /// </summary>
        /// <param name="node">The starting data node for traversal.</param>
        /// <param name="onVisit">Optional callback that is invoked when visiting each node. Return false to skip parent traversal.</param>
        /// <returns>An enumerable object that can be used to traverse the data nodes.</returns>
        public DataTraversalEnumerable TraverseDataUpwardsBreadthFirst(DataNode node, OnVisitNode<DataNode> onVisit = null)
        {
            return TraverseDataRecursive(node, Direction.Upwards, TraversalMethod.BreadthFirst, onVisit);
        }

        /// <summary>
        /// Traverses all data nodes in parent-first (topological) order: a node is yielded only
        /// after every data node it depends on (its parents) has already been yielded.
        /// The graph is expected to be acyclic; if a cycle is
        /// detected, the remaining nodes are still yielded (in arbitrary order) after an assert,
        /// so that no node is silently dropped.
        /// </summary>
        /// <returns>An enumerable object that yields data nodes in parent-first order.</returns>
        public TopologicalDataEnumerable TraverseDataParentsFirst()
        {
            return new TopologicalDataEnumerable(m_Graph, m_DataPools, Direction.Downwards);
        }

        /// <summary>
        /// Traverses all data nodes in children-first (reverse topological) order: a node is yielded
        /// only after every data node that depends on it (its children) has already been yielded.
        /// The graph is expected to be acyclic; if a cycle is
        /// detected, the remaining nodes are still yielded (in arbitrary order) after an assert,
        /// so that no node is silently dropped.
        /// </summary>
        /// <returns>An enumerable object that yields data nodes in children-first order.</returns>
        public TopologicalDataEnumerable TraverseDataChildrenFirst()
        {
            return new TopologicalDataEnumerable(m_Graph, m_DataPools, Direction.Upwards);
        }

    }
}
