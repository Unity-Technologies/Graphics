using System;
using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /*public*/ partial class GraphTraverser
    {
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
        /// <param name="order">The traversal order (depth or breadth first).</param>
        /// <param name="direction">The traversal direction (downwards or upwards).</param>
        /// <param name="onVisit">Optional callback that is invoked when visiting each node. Return false to skip child traversal.</param>
        /// <returns>An enumerable object that can be used to traverse the data nodes.</returns>
        public DataTraversalEnumerable TraverseDataRecursive(DataNode node, Direction direction = Direction.Downwards,
            TraversalOrder order = TraversalOrder.DepthFirst, OnVisitDataNode onVisit = null)
        {
            return new DataTraversalEnumerable(node, direction, order, this, onVisit);
        }

        /// <summary>
        /// Traverses data nodes in a depth-first manner, starting from the specified node and moving downwards.
        /// </summary>
        /// <param name="node">The starting data node for traversal.</param>
        /// <param name="onVisit">Optional callback that is invoked when visiting each node. Return false to skip child traversal.</param>
        /// <returns>An enumerable object that can be used to traverse the data nodes.</returns>
        public DataTraversalEnumerable TraverseDataDownwards(DataNode node, OnVisitDataNode onVisit = null)
        {
            return TraverseDataRecursive(node, Direction.Downwards, TraversalOrder.DepthFirst, onVisit);
        }

        /// <summary>
        /// Traverses data nodes in a breadth-first manner, starting from the specified node and moving downwards.
        /// </summary>
        /// <param name="node">The starting data node for traversal.</param>
        /// <param name="onVisit">Optional callback that is invoked when visiting each node. Return false to skip child traversal.</param>
        /// <returns>An enumerable object that can be used to traverse the data nodes.</returns>
        public DataTraversalEnumerable TraverseDataDownwardsBreadthFirst(DataNode node, OnVisitDataNode onVisit = null)
        {
            return TraverseDataRecursive(node, Direction.Downwards, TraversalOrder.BreadthFirst, onVisit);
        }

        /// <summary>
        /// Traverses data nodes in a depth-first manner, starting from the specified node and moving upwards.
        /// </summary>
        /// <param name="node">The starting data node for traversal.</param>
        /// <param name="onVisit">Optional callback that is invoked when visiting each node. Return false to skip parent traversal.</param>
        /// <returns>An enumerable object that can be used to traverse the data nodes.</returns>
        public DataTraversalEnumerable TraverseDataUpwards(DataNode node, OnVisitDataNode onVisit = null)
        {
            return TraverseDataRecursive(node, Direction.Upwards, TraversalOrder.DepthFirst, onVisit);
        }

        /// <summary>
        /// Traverses data nodes in a breadth-first manner, starting from the specified node and moving upwards.
        /// </summary>
        /// <param name="node">The starting data node for traversal.</param>
        /// <param name="onVisit">Optional callback that is invoked when visiting each node. Return false to skip parent traversal.</param>
        /// <returns>An enumerable object that can be used to traverse the data nodes.</returns>
        public DataTraversalEnumerable TraverseDataUpwardsBreadthFirst(DataNode node, OnVisitDataNode onVisit = null)
        {
            return TraverseDataRecursive(node, Direction.Upwards, TraversalOrder.BreadthFirst, onVisit);
        }


        /// <summary>
        /// Represents an enumerable collection for depth-first traversal of data nodes.
        /// </summary>
        public struct DataTraversalEnumerable
        {
            /// <summary>
            /// Initializes a new instance of the DataDepthFirstEnumerable struct.
            /// </summary>
            /// <param name="node">The starting node for traversal.</param>
            /// <param name="direction">The direction of traversal (Upwards or Downwards).</param>
            /// <param name="order"> The order of traversal (depth first or breadth first).</param>
            /// <param name="traverser">The graph traverser object in use.</param>
            /// <param name="onVisit">Optional callback that is invoked when visiting each node.</param>
            public DataTraversalEnumerable(DataNode node, Direction direction, TraversalOrder order,
                GraphTraverser traverser, OnVisitDataNode onVisit = null)
            {
                m_Node = node;
                m_GraphTraverser = traverser;
                m_Direction = direction;
                m_Order = order;
                m_OnVisit = onVisit;
            }

            private DataNode m_Node;
            private GraphTraverser m_GraphTraverser;
            private Direction m_Direction;
            private TraversalOrder m_Order;
            private OnVisitDataNode m_OnVisit;

            /// <summary>
            /// Gets an enumerator for traversing the data nodes in depth-first order.
            /// </summary>
            /// <returns>An enumerator that can be used to traverse the data nodes.</returns>
            public Enumerator GetEnumerator() =>
                new Enumerator(m_Node, m_Direction, m_Order, m_GraphTraverser, m_OnVisit);


            /// <summary>
            /// Executes the traversal by iterating through all nodes using the enumerator.
            /// </summary>
            public void Execute()
            {
                using var enumerator = GetEnumerator();
                while (enumerator.MoveNext()) { }
            }

            /// <summary>
            /// Enumerator for depth-first traversal of data nodes.
            /// </summary>
            public struct Enumerator : IDisposable
            {
                private Deque<DataNode> m_Deque;
                private HashSet<DataNodeId> m_Visited;
                private bool m_OwnsVisited;
                private GraphTraverser m_GraphTraverser;
                private DataNode m_Current;
                private Direction m_Direction;
                private TraversalOrder m_Order;
                private OnVisitDataNode m_OnVisit;

                /// <summary>
                /// Initializes a new instance of the Enumerator struct.
                /// </summary>
                /// <param name="startNode">The starting node for traversal.</param>
                /// <param name="direction">The direction of traversal.</param>
                /// <param name="order"> The order of traversal (depth first or breadth first).</param>
                /// <param name="traverser">The graph traverser object in use.</param>
                /// <param name="onVisit">Optional callback that is invoked when visiting each node.</param>
                public Enumerator(DataNode startNode, Direction direction, TraversalOrder order,
                    GraphTraverser traverser, OnVisitDataNode onVisit = null)
                {
                    m_Direction = direction;
                    m_Order = order;
                    m_GraphTraverser = traverser;
                    m_OwnsVisited = traverser.m_DataVisited == null;
                    m_Visited = m_OwnsVisited ? m_GraphTraverser.m_DataVisitedPool.Get() : traverser.m_DataVisited;
                    m_Deque = m_GraphTraverser.m_DataDequePool.Get();
                    m_Current = new DataNode();
                    m_OnVisit = onVisit;
                    m_Deque.AddFront(startNode);
                }

                /// <summary>
                /// Advances the enumerator to the next data node in the depth-first traversal.
                /// </summary>
                /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
                public bool MoveNext()
                {
                    while (m_Deque.Count > 0)
                    {
                        m_Current = m_Order == TraversalOrder.DepthFirst ? m_Deque.RemoveFront() : m_Deque.RemoveBack();
                        if (m_Visited.Add(m_Current.Id) && (m_OnVisit == null || m_OnVisit.Invoke(m_Current)))
                        {
                            var connections = m_Direction == Direction.Downwards
                                ? m_Current.Children
                                : m_Current.Parents;

                            for (int i = 0; i < connections.Count; i++)
                            {
                                var neighbor = m_Order == TraversalOrder.DepthFirst
                                    ? connections[connections.Count - 1 - i]
                                    : connections[i];
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

                    m_Current = new DataNode();
                    return false;
                }

                /// <summary>
                /// Gets the current data node in the traversal.
                /// </summary>
                public DataNode Current => m_Current.Id.IsValid ? m_Current : default;

                /// <summary>
                /// Releases all resources used by the enumerator.
                /// </summary>
                public void Dispose()
                {
                    if (m_OwnsVisited)
                    {
                        m_Visited.Clear();
                        m_GraphTraverser.m_DataVisitedPool.Release(m_Visited);
                    }

                    m_Deque.Clear();
                    m_GraphTraverser.m_DataDequePool.Release(m_Deque);
                }
            }
        }

        /// <summary>
        /// Represents an enumerable collection for linear traversal of data nodes with optional filtering.
        /// </summary>
        public struct LinearDataEnumerable
        {
            private IReadOnlyGraph m_Graph;
            private OnFilterDataNode m_OnFilter;

            /// <summary>
            /// Initializes a new instance of the <see cref="LinearDataEnumerable"/> struct.
            /// </summary>
            /// <param name="graph">The graph to traverse.</param>
            /// <param name="onFilter">Optional filter callback to control traversal and acceptance of nodes.</param>
            public LinearDataEnumerable(IReadOnlyGraph graph, OnFilterDataNode onFilter = null)
            {
                m_Graph = graph;
                m_OnFilter = onFilter;
            }

            /// <summary>
            /// Gets an enumerator for traversing the data nodes linearly.
            /// </summary>
            /// <returns>An enumerator for the data nodes.</returns>
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
            /// Enumerator for linear traversal of data nodes with filtering.
            /// </summary>
            public struct Enumerator
            {
                private IReadOnlyGraph m_Graph;
                private OnFilterDataNode m_OnFilter;
                private int m_Index;

                /// <summary>
                /// Initializes a new instance of the <see cref="Enumerator"/> struct.
                /// </summary>
                /// <param name="graph">The graph to traverse.</param>
                /// <param name="onFilter">Optional filter callback to control traversal and acceptance of nodes.</param>
                public Enumerator(IReadOnlyGraph graph, OnFilterDataNode onFilter)
                {
                    m_Graph = graph;
                    m_OnFilter = onFilter;
                    m_Index = -1;
                }

                /// <summary>
                /// Gets the current data node in the traversal.
                /// </summary>
                public DataNode Current => m_Graph.DataNodes[m_Index];

                /// <summary>
                /// Advances the enumerator to the next data node that matches the filter.
                /// </summary>
                /// <returns>True if a node was found; otherwise, false.</returns>
                public bool MoveNext()
                {
                    while (m_Index < m_Graph.DataNodes.Count - 1)
                    {
                        m_Index++;
                        var node = m_Graph.DataNodes[m_Index];
                        var control = m_OnFilter(node);
                        if (!control.Continue)
                        {
                            m_Index = m_Graph.DataNodes.Count;
                        }

                        if (control.Accept)
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
