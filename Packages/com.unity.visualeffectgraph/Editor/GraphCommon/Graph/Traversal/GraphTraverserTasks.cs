using System;
using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /*public*/ partial class GraphTraverser
    {
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
        /// <param name="order">The traversal order (depth or breadth first).</param>
        /// <param name="direction">The traversal direction (downwards or upwards).</param>
        /// <param name="onVisit">Optional callback that is invoked when visiting each node. Return false to skip child traversal.</param>
        /// <returns>An enumerable object that can be used to traverse the task nodes.</returns>
        public TaskTraversalEnumerable TraverseTaskRecursive(TaskNode node, Direction direction = Direction.Downwards,
            TraversalOrder order = TraversalOrder.DepthFirst, OnVisitTaskNode onVisit = null)
        {
            return new TaskTraversalEnumerable(node, direction, order, this, onVisit);
        }
        /// <summary>
        /// Traverses task nodes in a depth-first manner, starting from the specified node and moving downwards.
        /// </summary>
        /// <param name="node">The starting task node for traversal.</param>
        /// <param name="onVisit">Optional callback that is invoked when visiting each node. Return false to skip child traversal.</param>
        /// <returns>An enumerable object that can be used to traverse the task nodes.</returns>
        public TaskTraversalEnumerable TraverseTaskDownwards(TaskNode node, OnVisitTaskNode onVisit = null)
        {
            return TraverseTaskRecursive(node, Direction.Downwards, TraversalOrder.DepthFirst, onVisit);
        }

        /// <summary>
        /// Traverses task nodes in a breadth-first manner, starting from the specified node and moving downwards.
        /// </summary>
        /// <param name="node">The starting task node for traversal.</param>
        /// <param name="onVisit">Optional callback that is invoked when visiting each node. Return false to skip child traversal.</param>
        /// <returns>An enumerable object that can be used to traverse the task nodes.</returns>
        public TaskTraversalEnumerable TraverseTaskDownwardsBreadthFirst(TaskNode node, OnVisitTaskNode onVisit = null)
        {
            return TraverseTaskRecursive(node, Direction.Downwards, TraversalOrder.BreadthFirst, onVisit);
        }

        /// <summary>
        /// Traverses task nodes in a depth-first manner, starting from the specified node and moving upwards.
        /// </summary>
        /// <param name="node">The starting task node for traversal.</param>
        /// <param name="onVisit">Optional callback that is invoked when visiting each node. Return false to skip parent traversal.</param>
        /// <returns>An enumerable object that can be used to traverse the task nodes.</returns>
        public TaskTraversalEnumerable TraverseTaskUpwards(TaskNode node, OnVisitTaskNode onVisit = null)
        {
            return TraverseTaskRecursive(node, Direction.Upwards, TraversalOrder.DepthFirst, onVisit);
        }

        /// <summary>
        /// Traverses task nodes in a breadth-first manner, starting from the specified node and moving upwards.
        /// </summary>
        /// <param name="node">The starting task node for traversal.</param>
        /// <param name="onVisit">Optional callback that is invoked when visiting each node. Return false to skip parent traversal.</param>
        /// <returns>An enumerable object that can be used to traverse the task nodes.</returns>
        public TaskTraversalEnumerable TraverseTaskUpwardsBreadthFirst(TaskNode node, OnVisitTaskNode onVisit = null)
        {
            return TraverseTaskRecursive(node, Direction.Upwards, TraversalOrder.BreadthFirst, onVisit);
        }

        /// <summary>
        /// Represents an enumerable collection for depth-first traversal of task nodes.
        /// </summary>
        public struct TaskTraversalEnumerable
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TaskTraversalEnumerable"/> struct.
            /// </summary>
            /// <param name="node">The starting node for traversal</param>
            /// <param name="direction">The direction of traversal (upwards or downwards)</param>
            /// <param name="order"> The order of traversal (depth first or breadth first).</param>
            /// <param name="traverser">The graph traverser object in use.</param>
            /// <param name="onVisit">Optional callback invoked when visiting nodes</param>
            public TaskTraversalEnumerable(TaskNode node, Direction direction, TraversalOrder order,  GraphTraverser traverser, OnVisitTaskNode onVisit = null)            {
                m_Node = node;
                m_GraphTraverser = traverser;
                m_Direction = direction;
                m_Order = order;
                m_OnVisit = onVisit;
            }

            private TaskNode m_Node;
            private GraphTraverser m_GraphTraverser;
            private Direction m_Direction;
            private TraversalOrder m_Order;
            private OnVisitTaskNode m_OnVisit;

            /// <summary>
            /// Gets the enumerator for traversing the task nodes.
            /// </summary>
            /// <returns>An enumerator that can be used to traverse the task nodes</returns>
            public Enumerator GetEnumerator() => new Enumerator(m_Node, m_Direction, m_Order, m_GraphTraverser, m_OnVisit);

            /// <summary>
            /// Executes the traversal, iterating through all nodes.
            /// </summary>
            public void Execute()
            {
                using var enumerator = GetEnumerator();
                while (enumerator.MoveNext()) { }
            }
            /// <summary>
            /// Enumerator for traversal of task nodes.
            /// </summary>
            public struct Enumerator : IDisposable
            {
                private Deque<TaskNode> m_Deque;
                private HashSet<TaskNodeId> m_Visited;
                private bool m_OwnsVisited;
                private GraphTraverser m_GraphTraverser;
                private TaskNode m_Current;
                private Direction m_Direction;
                private TraversalOrder m_Order;
                private OnVisitTaskNode m_OnVisit;

                /// <summary>
                /// Initializes a new instance of the Enumerator struct.
                /// </summary>
                /// <param name="startNode">The starting node for traversal.</param>
                /// <param name="direction">The direction of traversal.</param>
                /// <param name="order"> The order of traversal (depth first or breadth first).</param>
                /// <param name="traverser">The graph traverser object in use.</param>
                /// <param name="onVisit">Optional callback that is invoked when visiting each node.</param>
                public Enumerator(TaskNode startNode, Direction direction, TraversalOrder order, GraphTraverser traverser, OnVisitTaskNode onVisit = null)
                {
                    m_Direction = direction;
                    m_Order = order;
                    m_GraphTraverser = traverser;
                    m_OwnsVisited = traverser.m_TaskVisited == null;
                    m_Visited = m_OwnsVisited ? m_GraphTraverser.m_TaskVisitedPool.Get() : traverser.m_TaskVisited;
                    m_Deque = m_GraphTraverser.m_TaskDequePool.Get();
                    m_Current = new TaskNode();
                    m_OnVisit = onVisit;
                    m_Deque.AddFront(startNode);
                }

                /// <summary>
                /// Advances the enumerator to the next task node in the depth-first traversal.
                /// </summary>
                /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
                public bool MoveNext()
                {
                    while (m_Deque.Count > 0)
                    {
                        m_Current = m_Order == TraversalOrder.DepthFirst ? m_Deque.RemoveFront() : m_Deque.RemoveBack();
                        if (m_Visited.Add(m_Current.Id) && (m_OnVisit == null || m_OnVisit.Invoke(m_Current)))
                        {
                            var connections = m_Direction == Direction.Downwards ? m_Current.Children : m_Current.Parents;
                            for (int i = 0; i < connections.Count; i++)
                            {
                                var neighbor = m_Order == TraversalOrder.DepthFirst ? connections[connections.Count - 1 - i] : connections[i];
                                if (!m_Visited.Contains(neighbor.Id))
                                {
                                    if (m_Order == TraversalOrder.DepthFirst)
                                        m_Deque.AddFront(neighbor);
                                    else
                                        m_Deque.AddBack(neighbor);
                                }
                            }
                            return true;
                        }
                    }

                    m_Current = new TaskNode();
                    return false;
                }

                /// <summary>
                /// Gets the current task node in the traversal.
                /// </summary>
                public TaskNode Current => m_Current.Id.IsValid ? m_Current : default;

                /// <summary>
                /// Releases all resources used by the enumerator.
                /// </summary>
                public void Dispose()
                {
                    if (m_OwnsVisited)
                    {
                        m_Visited.Clear();
                        m_GraphTraverser.m_TaskVisitedPool.Release(m_Visited);
                    }
                    m_Deque.Clear();
                    m_GraphTraverser.m_TaskDequePool.Release(m_Deque);
                }
            }
        }

        /// <summary>
        /// Represents an enumerable collection for linear traversal of task nodes with optional filtering.
        /// </summary>
        public struct LinearTaskEnumerable
        {
            IReadOnlyGraph m_Graph;
            OnFilterTaskNode m_OnFilter;
            /// <summary>
            /// Initializes a new instance of the <see cref="LinearTaskEnumerable"/> struct.
            /// </summary>
            /// <param name="graph">The graph to traverse.</param>
            /// <param name="onFilter">Optional filter callback to control traversal and acceptance of nodes.</param>
            public LinearTaskEnumerable(IReadOnlyGraph graph, OnFilterTaskNode onFilter = null)
            {
                m_Graph = graph;
                m_OnFilter = onFilter;
            }

            /// <summary>
            /// Gets an enumerator for traversing the task nodes linearly.
            /// </summary>
            /// <returns>An enumerator for the task nodes.</returns>
            public Enumerator GetEnumerator() => new Enumerator(m_Graph, m_OnFilter);


            /// <summary>
            /// Executes the traversal, iterating through all nodes.
            /// </summary>
            public void Execute()
            {
                var enumerator = GetEnumerator();
                while (enumerator.MoveNext()) { }
            }

            /// <summary>
            /// Enumerator for linear traversal of task nodes with filtering.
            /// </summary>
            public struct Enumerator
            {
                IReadOnlyGraph m_Graph;
                OnFilterTaskNode m_OnFilter;
                private int m_Index;

                /// <summary>
                /// Initializes a new instance of the <see cref="Enumerator"/> struct.
                /// </summary>
                /// <param name="graph">The graph to traverse.</param>
                /// <param name="onFilter">Optional filter callback to control traversal and acceptance of nodes.</param>
                public Enumerator(IReadOnlyGraph graph,  OnFilterTaskNode onFilter)
                {
                    m_Graph = graph;
                    m_OnFilter = onFilter;
                    m_Index = -1;
                }

                /// <summary>
                /// Gets the current task node in the traversal.
                /// </summary>
                public TaskNode Current => m_Graph.TaskNodes[m_Index];

                /// <summary>
                /// Advances the enumerator to the next task node that matches the filter.
                /// </summary>
                /// <returns>True if a node was found; otherwise, false.</returns>
                public bool MoveNext()
                {
                    while (m_Index < m_Graph.TaskNodes.Count - 1)
                    {
                        m_Index++;
                        var node = m_Graph.TaskNodes[m_Index];
                        var control = m_OnFilter(node);
                        if (!control.Continue)
                        {
                            m_Index = m_Graph.TaskNodes.Count;
                        }
                        if(control.Accept)
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
        }
    }
}
