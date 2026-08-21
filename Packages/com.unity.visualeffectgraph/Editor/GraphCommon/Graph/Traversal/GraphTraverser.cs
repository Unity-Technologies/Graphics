using System;

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
        readonly TraversalPools<DataNodeId> m_DataPools = new();
        readonly TraversalPools<TaskNodeId> m_TaskPools = new();

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
        /// Specifies how the graph is traversed.
        /// </summary>
        public enum TraversalMethod
        {
            /// <summary>
            /// Depth-first traversal. Each node is visited before its descendants.
            /// </summary>
            DepthFirstPre,

            /// <summary>
            /// Depth-first traversal. Each node is visited after all its reachable descendants.
            /// Correctly handles DAGs where a descendant is reachable from multiple ancestors.
            /// </summary>
            DepthFirstPost,

            /// <summary>
            /// Breadth-first traversal. Nodes are visited level by level, shallowest first.
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
            /// <exception cref="InvalidOperationException">Thrown if a context is already active (nested contexts are not allowed).</exception>
            public SharedVisitedContext(GraphTraverser traverser, bool data, bool task)
            {
                // Validate both up-front so a throw cannot leak an already-acquired set.
                if (data && traverser.m_DataPools.SharedVisited != null ||
                    task && traverser.m_TaskPools.SharedVisited != null)
                    throw new InvalidOperationException("Cannot have nested SharedVisitedContext");

                m_GraphTraverser = traverser;
                m_Data = data;
                m_Task = task;
                if (m_Data)
                    traverser.m_DataPools.AcquireSharedVisited();
                if (m_Task)
                    traverser.m_TaskPools.AcquireSharedVisited();
            }

            /// <summary>
            /// Releases the visited sets back to the pool and clears the context.
            /// </summary>
            public void Dispose()
            {
                if (m_Data)
                    m_GraphTraverser.m_DataPools.ReleaseSharedVisited();
                if (m_Task)
                    m_GraphTraverser.m_TaskPools.ReleaseSharedVisited();
            }
        }
    }
}
