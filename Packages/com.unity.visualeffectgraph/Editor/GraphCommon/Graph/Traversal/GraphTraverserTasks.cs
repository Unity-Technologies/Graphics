using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /*public*/ partial class GraphTraverser
    {
        /// <summary>
        /// Adapter bridging <see cref="TaskNode"/> to the generic traversal core.
        /// </summary>
        public readonly struct TaskTraversalAdapter :
            ITraversalAdapter<TaskNode, TaskNodeId, TaskNodeLinks, TaskNodeEnumerator>
        {
            /// <inheritdoc cref="ITraversalAdapter{TNode, TId, TLinks, TNodeEnumerator}.GetId"/>
            public TaskNodeId GetId(in TaskNode node) => node.Id;
            /// <inheritdoc cref="ITraversalAdapter{TNode, TId, TLinks, TNodeEnumerator}.GetLinks"/>
            public TaskNodeLinks GetLinks(in TaskNode node, Direction direction) =>
                direction == Direction.Downwards ? node.Children : node.Parents;
            /// <inheritdoc cref="ITraversalAdapter{TNode, TId, TLinks, TNodeEnumerator}.GetLinkCount"/>
            public int GetLinkCount(in TaskNodeLinks links) => links.Count;
            /// <inheritdoc cref="ITraversalAdapter{TNode, TId, TLinks, TNodeEnumerator}.GetLinkId"/>
            public TaskNodeId GetLinkId(in TaskNodeLinks links, int index) => links[index].Id;
            /// <inheritdoc cref="ITraversalAdapter{TNode, TId, TLinks, TNodeEnumerator}.Resolve"/>
            public TaskNode Resolve(IReadOnlyGraph graph, TaskNodeId id) => graph.TaskNodes[id];
            /// <inheritdoc cref="ITraversalAdapter{TNode, TId, TLinks, TNodeEnumerator}.GetNodes"/>
            public TaskNodeEnumerator GetNodes(IReadOnlyGraph graph) => graph.TaskNodes.GetEnumerator();
            /// <inheritdoc cref="ITraversalAdapter{TNode, TId, TLinks, TNodeEnumerator}.SeedInDegrees"/>
            public void SeedInDegrees(IReadOnlyGraph graph, Direction direction, Dictionary<TaskNodeId, int> inDegrees, Deque<TaskNodeId> ready)
            {
                foreach (var node in graph.TaskNodes)
                {
                    int inDegree = direction == Direction.Downwards ? node.Parents.Count : node.Children.Count;
                    inDegrees[node.Id] = inDegree;
                    if (inDegree == 0)
                        ready.AddBack(node.Id);
                }
            }
        }

        /// <summary>Enumerable for recursive traversal of task nodes.</summary>
        public readonly struct TaskTraversalEnumerable
        {
            private readonly TraversalEnumerable<TaskNode, TaskNodeId, TaskNodeLinks, TaskNodeEnumerator, TaskTraversalAdapter> m_Inner;

            internal TaskTraversalEnumerable(in TaskNode startNode, Direction direction, TraversalMethod method,
                IReadOnlyGraph graph, TraversalPools<TaskNodeId> pools, OnVisitNode<TaskNode> onVisit)
            {
                m_Inner = new TraversalEnumerable<TaskNode, TaskNodeId, TaskNodeLinks, TaskNodeEnumerator, TaskTraversalAdapter>(
                    in startNode, direction, method, graph, pools, onVisit);
            }

            public TraversalEnumerable<TaskNode, TaskNodeId, TaskNodeLinks, TaskNodeEnumerator, TaskTraversalAdapter>.Enumerator GetEnumerator() =>
                m_Inner.GetEnumerator();

            public void Execute() => m_Inner.Execute();
        }

        /// <summary>Enumerable for topological traversal of task nodes.</summary>
        public readonly struct TopologicalTaskEnumerable
        {
            private readonly TopologicalEnumerable<TaskNode, TaskNodeId, TaskNodeLinks, TaskNodeEnumerator, TaskTraversalAdapter> m_Inner;

            internal TopologicalTaskEnumerable(IReadOnlyGraph graph, TraversalPools<TaskNodeId> pools, Direction direction)
            {
                m_Inner = new TopologicalEnumerable<TaskNode, TaskNodeId, TaskNodeLinks, TaskNodeEnumerator, TaskTraversalAdapter>(
                    graph, pools, direction);
            }

            public TopologicalEnumerable<TaskNode, TaskNodeId, TaskNodeLinks, TaskNodeEnumerator, TaskTraversalAdapter>.Enumerator GetEnumerator() =>
                m_Inner.GetEnumerator();

            public void Execute() => m_Inner.Execute();
        }

        /// <summary>Enumerable for linear traversal of task nodes with optional filtering.</summary>
        public readonly struct LinearTaskEnumerable
        {
            private readonly LinearEnumerable<TaskNode, TaskNodeId, TaskNodeLinks, TaskNodeEnumerator, TaskTraversalAdapter> m_Inner;

            internal LinearTaskEnumerable(IReadOnlyGraph graph, OnFilterNode<TaskNode> onFilter)
            {
                m_Inner = new LinearEnumerable<TaskNode, TaskNodeId, TaskNodeLinks, TaskNodeEnumerator, TaskTraversalAdapter>(graph, onFilter);
            }

            public LinearEnumerable<TaskNode, TaskNodeId, TaskNodeLinks, TaskNodeEnumerator, TaskTraversalAdapter>.Enumerator GetEnumerator() =>
                m_Inner.GetEnumerator();

            public void Execute() => m_Inner.Execute();
        }

        /// <summary>
        /// Traverses all root task nodes in the graph.
        /// </summary>
        /// <returns> An enumerable object </returns>
        public LinearTaskEnumerable TraverseTaskRoots()
        {
            return new LinearTaskEnumerable(m_Graph, node =>
            {
                if(node.Parents.Count == 0)
                    return TraversalControl.AcceptAndContinue;
                return TraversalControl.RejectAndContinue;
            });
        }

        /// <summary>
        /// Traverses all leaf task nodes in the graph.
        /// </summary>
        /// <returns> An enumerable object </returns>
        public LinearTaskEnumerable TraverseTaskLeaves()
        {
            return new LinearTaskEnumerable(m_Graph, node =>
            {
                if(node.Children.Count == 0)
                    return TraversalControl.AcceptAndContinue;
                return TraversalControl.RejectAndContinue;
            });
        }

        /// <summary>
        /// Traverses task nodes recursively, starting from the specified node.
        /// </summary>
        /// <param name="node">The starting task node for traversal.</param>
        /// <param name="direction">The traversal direction (downwards or upwards).</param>
        /// <param name="method">The traversal method.</param>
        /// <param name="onVisit">Optional callback that is invoked when visiting each node. Return false to skip child traversal.</param>
        /// <returns>An enumerable object that can be used to traverse the task nodes.</returns>
        public TaskTraversalEnumerable TraverseTaskRecursive(TaskNode node, Direction direction = Direction.Downwards,
            TraversalMethod method = TraversalMethod.DepthFirstPre, OnVisitNode<TaskNode> onVisit = null)
        {
            return new TaskTraversalEnumerable(in node, direction, method, m_Graph, m_TaskPools, onVisit);
        }

        /// <summary>
        /// Traverses task nodes depth-first pre-order, starting from the specified node and moving downwards.
        /// </summary>
        /// <param name="node">The starting task node for traversal.</param>
        /// <param name="onVisit">Optional callback that is invoked when visiting each node. Return false to skip child traversal.</param>
        /// <returns>An enumerable object that can be used to traverse the task nodes.</returns>
        public TaskTraversalEnumerable TraverseTaskDownwards(TaskNode node, OnVisitNode<TaskNode> onVisit = null)
        {
            return TraverseTaskRecursive(node, Direction.Downwards, TraversalMethod.DepthFirstPre, onVisit);
        }

        /// <summary>
        /// Traverses task nodes breadth-first, starting from the specified node and moving downwards.
        /// </summary>
        /// <param name="node">The starting task node for traversal.</param>
        /// <param name="onVisit">Optional callback that is invoked when visiting each node. Return false to skip child traversal.</param>
        /// <returns>An enumerable object that can be used to traverse the task nodes.</returns>
        public TaskTraversalEnumerable TraverseTaskDownwardsBreadthFirst(TaskNode node, OnVisitNode<TaskNode> onVisit = null)
        {
            return TraverseTaskRecursive(node, Direction.Downwards, TraversalMethod.BreadthFirst, onVisit);
        }

        /// <summary>
        /// Traverses task nodes depth-first pre-order, starting from the specified node and moving upwards.
        /// </summary>
        /// <param name="node">The starting task node for traversal.</param>
        /// <param name="onVisit">Optional callback that is invoked when visiting each node. Return false to skip parent traversal.</param>
        /// <returns>An enumerable object that can be used to traverse the task nodes.</returns>
        public TaskTraversalEnumerable TraverseTaskUpwards(TaskNode node, OnVisitNode<TaskNode> onVisit = null)
        {
            return TraverseTaskRecursive(node, Direction.Upwards, TraversalMethod.DepthFirstPre, onVisit);
        }

        /// <summary>
        /// Traverses task nodes breadth-first, starting from the specified node and moving upwards.
        /// </summary>
        /// <param name="node">The starting task node for traversal.</param>
        /// <param name="onVisit">Optional callback that is invoked when visiting each node. Return false to skip parent traversal.</param>
        /// <returns>An enumerable object that can be used to traverse the task nodes.</returns>
        public TaskTraversalEnumerable TraverseTaskUpwardsBreadthFirst(TaskNode node, OnVisitNode<TaskNode> onVisit = null)
        {
            return TraverseTaskRecursive(node, Direction.Upwards, TraversalMethod.BreadthFirst, onVisit);
        }

        /// <summary>
        /// Traverses all task nodes in parent-first (topological) order: a node is yielded only
        /// after every task node it depends on (its parents) has already been yielded.
        /// The graph is expected to be acyclic; if a cycle is
        /// detected, the remaining nodes are still yielded (in arbitrary order) after an assert,
        /// so that no node is silently dropped.
        /// </summary>
        /// <returns>An enumerable object that yields task nodes in parent-first order.</returns>
        public TopologicalTaskEnumerable TraverseTasksParentsFirst()
        {
            return new TopologicalTaskEnumerable(m_Graph, m_TaskPools, Direction.Downwards);
        }

        /// <summary>
        /// Traverses all task nodes in children-first (reverse topological) order: a node is yielded
        /// only after every task node that depends on it (its children) has already been yielded.
        /// The graph is expected to be acyclic; if a cycle is
        /// detected, the remaining nodes are still yielded (in arbitrary order) after an assert,
        /// so that no node is silently dropped.
        /// </summary>
        /// <returns>An enumerable object that yields task nodes in children-first order.</returns>
        public TopologicalTaskEnumerable TraverseTasksChildrenFirst()
        {
            return new TopologicalTaskEnumerable(m_Graph, m_TaskPools, Direction.Upwards);
        }

    }
}
