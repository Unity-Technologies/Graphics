using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Struct used by Filter delegate as return value to control graph traversal.
    /// </summary>
    /*public*/ readonly struct TraversalControl
    {
        /// <summary>
        /// Accept the current node and continue traversal.
        /// </summary>
        public static TraversalControl AcceptAndContinue => new TraversalControl(true,true);
        /// <summary>
        /// Accept the current node and stop traversal.
        /// </summary>
        public static TraversalControl AcceptAndBreak => new TraversalControl(true, false);
        /// <summary>
        /// Reject the current node and continue traversal.
        /// </summary>
        public static TraversalControl RejectAndContinue => new TraversalControl(false, true);
        /// <summary>
        /// Reject the current node and stop traversal.
        /// </summary>
        public static TraversalControl RejectAndBreak => new TraversalControl(false, false);

        /// <summary>
        /// True is current node is accepted, false otherwise.
        /// </summary>
        public bool Accept { get; }
        /// <summary>
        /// True is traversal continues, false otherwise.
        /// </summary>
        public bool Continue { get; }

        /// <summary>
        /// Constructor for TraversalControl struct.
        /// </summary>
        /// <param name="acceptElement">true to accept node, false to reject.</param>
        /// <param name="continueTraversal">true to continue graph traversal, false to stop it.</param>
        public TraversalControl(bool acceptElement, bool continueTraversal)
        {
            Accept = acceptElement;
            Continue = continueTraversal;
        }
    }

    /// <summary>
    /// A class that allows efficient traversal of a <see cref = "IReadOnlyGraph"/>.
    /// To create a new traverser, use <see cref = "IReadOnlyGraph.CreateTraverser"/>.
    /// </summary>
    /*public*/ partial class GraphTraverser
    {
        ObjectPool<Deque<TaskNode>> m_TaskDequePool = new(() => new Deque<TaskNode>());
        ObjectPool<HashSet<TaskNodeId>> m_TaskVisitedPool = new(() => new HashSet<TaskNodeId>());

        ObjectPool<Deque<DataNode>> m_DataDequePool = new(() => new Deque<DataNode>());
        ObjectPool<HashSet<DataNodeId>> m_DataVisitedPool = new(() => new HashSet<DataNodeId>());
        HashSet<DataNodeId> m_DataVisited;
        HashSet<TaskNodeId> m_TaskVisited;

        private IReadOnlyGraph m_Graph;

        internal GraphTraverser(IReadOnlyGraph graph)
        {
            m_Graph = graph;
        }

        /// <summary>Specifies the direction for traversing the graph.</summary>
        public enum Direction
        {
            /// <summary>
            /// Traverses from parent nodes to child nodes (following dependencies).
            /// </summary>
            Downwards,
            /// <summary>
            /// Traverses from child nodes to parent nodes (against dependencies).
            /// </summary>
            Upwards,
        }

        /// <summary>
        /// Specifies the order for traversing the graph.
        /// </summary>
        public enum TraversalOrder
        {
            /// <summary>
            /// Traverses in the depth-first order
            /// </summary>
            DepthFirst,

            /// <summary>
            /// Traverses in the breadth-first order
            /// </summary>
            BreadthFirst,
        }

        /// <summary>
        /// Provides a context for sharing visited node sets during graph traversal,
        /// allowing multiple traversals to reuse the same visited sets.
        /// This IDisposable struct manages the lifecycle of the visited sets, acquiring them from
        /// the pool on construction and releasing them on disposal. Nested contexts are not allowed.
        /// </summary>
        public struct SharedVisitedContext : IDisposable
        {
            private GraphTraverser m_GraphTraverser;
            private bool m_Data;
            private bool m_Task;

            /// <summary>
            /// Initializes a new instance of the <see cref="SharedVisitedContext"/> struct.
            /// Acquires visited sets for data and/or task nodes from the pool.
            /// </summary>
            /// <param name="traverser">The <see cref="GraphTraverser"/> to use.</param>
            /// <param name="data">Whether to acquire a visited set for data nodes.</param>
            /// <param name="task">Whether to acquire a visited set for task nodes.</param>
            /// <exception cref="Exception">Thrown if a context is already active (nested contexts are not allowed).</exception>
            public SharedVisitedContext(GraphTraverser traverser, bool data, bool task)
            {
                m_GraphTraverser = traverser;
                m_Data = data;
                m_Task = task;
                if (m_Data)
                {
                    if(m_GraphTraverser.m_DataVisited != null)
                        throw new Exception("Cannot have nested SharedVisitedContext");
                    m_GraphTraverser.m_DataVisited = m_GraphTraverser.m_DataVisitedPool.Get();
                }

                if (m_Task)
                {
                    if(m_GraphTraverser.m_TaskVisited != null)
                        throw new Exception("Cannot have nested SharedVisitedContext");
                    m_GraphTraverser.m_TaskVisited = m_GraphTraverser.m_TaskVisitedPool.Get();
                }
            }

            /// <summary>
            /// Releases the visited sets back to the pool and clears the context.
            /// </summary>
            public void Dispose()
            {
                if (m_Data)
                {
                    m_GraphTraverser.m_DataVisited.Clear();
                    m_GraphTraverser.m_DataVisitedPool.Release(m_GraphTraverser.m_DataVisited);
                    m_GraphTraverser.m_DataVisited = null;
                }

                if (m_Task)
                {
                    m_GraphTraverser.m_TaskVisited.Clear();
                    m_GraphTraverser.m_TaskVisitedPool.Release(m_GraphTraverser.m_TaskVisited);
                    m_GraphTraverser.m_TaskVisited = null;
                }
            }
        }
    }
}
