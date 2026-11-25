using System;
using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX.Test
{
    /// <summary>
    /// Which node kind a traversal test runs against. Public because it appears in public
    /// <c>[TestCase]</c> signatures; internal there would be CS0051.
    /// </summary>
    public enum NodeKind
    {
        Task,
        Data,
    }

    /// <summary>
    /// Traverses one node kind and returns the visited nodes as labels, so one test body can assert
    /// the same expectation for tasks and for data. The test-side counterpart to the traverser's
    /// <c>ITraversalAdapter</c>, which selects the kind through a generic parameter that cannot be a
    /// <c>[TestCase]</c> argument.
    /// </summary>
    /// <remarks>
    /// <c>start</c> is a task name or a data node key — the same string in
    /// <see cref="TestGraphBuilder"/>'s named shapes, which is what makes parameterising over both
    /// kinds meaningful. Labels are the task's name, or the container's
    /// (<see cref="DataContainer.Name"/>) for data nodes, which have none of their own. Each instance
    /// owns a traverser, so <see cref="SharedVisited"/> and the traversals inside it share state; use
    /// <see cref="With"/> for a view of the other kind over that same traverser.
    /// </remarks>
    sealed class NodeKindTraversal
    {
        readonly TestGraphBuilder m_Builder;
        readonly NodeKind m_Kind;
        readonly GraphTraverser m_Traverser;

        public NodeKindTraversal(TestGraphBuilder builder, NodeKind kind)
            : this(builder, kind, builder.CreateTraverser())
        {
        }

        NodeKindTraversal(TestGraphBuilder builder, NodeKind kind, GraphTraverser traverser)
        {
            m_Builder = builder;
            m_Kind = kind;
            m_Traverser = traverser;
        }

        public NodeKind Kind => m_Kind;
        public TestGraphBuilder Builder => m_Builder;

        /// <summary>A view of another kind sharing this traverser, for tests needing both.</summary>
        public NodeKindTraversal With(NodeKind kind) => new NodeKindTraversal(m_Builder, kind, m_Traverser);

        bool IsTask => m_Kind == NodeKind.Task;

        // --- Traversals --------------------------------------------------------------------------

        /// <summary>Opens a shared-visited scope for this kind.</summary>
        public GraphTraverser.SharedVisitedContext SharedVisited() =>
            new GraphTraverser.SharedVisitedContext(m_Traverser, data: !IsTask, task: IsTask);

        /// <summary>Returning false from <paramref name="onVisit"/> prunes that node by label.</summary>
        public List<string> Recursive(string start, GraphTraverser.Direction direction,
            GraphTraverser.TraversalMethod method, Func<string, bool> onVisit = null) =>
            IsTask
                ? Names(m_Traverser.TraverseTaskRecursive(
                    m_Builder.GetTaskNode(start), direction, method, Visit(onVisit)))
                : Names(m_Traverser.TraverseDataRecursive(
                    m_Builder.GetDataNode(start), direction, method, VisitData(onVisit)));

        public List<string> Downwards(string start, Func<string, bool> onVisit = null) =>
            IsTask
                ? Names(m_Traverser.TraverseTaskDownwards(m_Builder.GetTaskNode(start), Visit(onVisit)))
                : Names(m_Traverser.TraverseDataDownwards(m_Builder.GetDataNode(start), VisitData(onVisit)));

        public List<string> Upwards(string start) =>
            IsTask
                ? Names(m_Traverser.TraverseTaskUpwards(m_Builder.GetTaskNode(start)))
                : Names(m_Traverser.TraverseDataUpwards(m_Builder.GetDataNode(start)));

        public List<string> DownwardsBreadthFirst(string start) =>
            IsTask
                ? Names(m_Traverser.TraverseTaskDownwardsBreadthFirst(m_Builder.GetTaskNode(start)))
                : Names(m_Traverser.TraverseDataDownwardsBreadthFirst(m_Builder.GetDataNode(start)));

        public List<string> UpwardsBreadthFirst(string start) =>
            IsTask
                ? Names(m_Traverser.TraverseTaskUpwardsBreadthFirst(m_Builder.GetTaskNode(start)))
                : Names(m_Traverser.TraverseDataUpwardsBreadthFirst(m_Builder.GetDataNode(start)));

        public List<string> PostOrder(string start) =>
            Recursive(start, GraphTraverser.Direction.Downwards,
                GraphTraverser.TraversalMethod.DepthFirstPost);

        public List<string> Roots() =>
            IsTask ? Names(m_Traverser.TraverseTaskRoots()) : Names(m_Traverser.TraverseDataRoots());

        public List<string> Leaves() =>
            IsTask ? Names(m_Traverser.TraverseTaskLeaves()) : Names(m_Traverser.TraverseDataLeaves());

        public List<string> ParentsFirst() =>
            IsTask
                ? Names(m_Traverser.TraverseTasksParentsFirst())
                : Names(m_Traverser.TraverseDataParentsFirst());

        public List<string> ChildrenFirst() =>
            IsTask
                ? Names(m_Traverser.TraverseTasksChildrenFirst())
                : Names(m_Traverser.TraverseDataChildrenFirst());

        // --- Ordering assertions -----------------------------------------------------------------

        public void AssertParentsFirst(IReadOnlyList<string> order)
        {
            if (IsTask)
                m_Builder.AssertTaskParentsFirst(order);
            else
                m_Builder.AssertDataParentsFirst(order);
        }

        public void AssertChildrenFirst(IReadOnlyList<string> order)
        {
            if (IsTask)
                m_Builder.AssertTaskChildrenFirst(order);
            else
                m_Builder.AssertDataChildrenFirst(order);
        }

        // --- Prune adapters ----------------------------------------------------------------------

        static GraphTraverser.OnVisitNode<TaskNode> Visit(Func<string, bool> onVisit)
        {
            if (onVisit == null)
                return null;
            return node => onVisit(node.Name);
        }

        static GraphTraverser.OnVisitNode<DataNode> VisitData(Func<string, bool> onVisit)
        {
            if (onVisit == null)
                return null;
            return node => onVisit(node.DataContainer.Name);
        }

        // --- Label collection --------------------------------------------------------------------
        // The enumerables are foreach-able structs but do not implement IEnumerable<T>, so LINQ is
        // unavailable and each needs its own overload.

        static List<string> Names(GraphTraverser.TaskTraversalEnumerable traversal)
        {
            var names = new List<string>();
            foreach (var node in traversal)
                names.Add(node.Name);
            return names;
        }

        static List<string> Names(GraphTraverser.TopologicalTaskEnumerable traversal)
        {
            var names = new List<string>();
            foreach (var node in traversal)
                names.Add(node.Name);
            return names;
        }

        static List<string> Names(GraphTraverser.LinearTaskEnumerable traversal)
        {
            var names = new List<string>();
            foreach (var node in traversal)
                names.Add(node.Name);
            return names;
        }

        static List<string> Names(GraphTraverser.DataTraversalEnumerable traversal)
        {
            var names = new List<string>();
            foreach (var node in traversal)
                names.Add(node.DataContainer.Name);
            return names;
        }

        static List<string> Names(GraphTraverser.TopologicalDataEnumerable traversal)
        {
            var names = new List<string>();
            foreach (var node in traversal)
                names.Add(node.DataContainer.Name);
            return names;
        }

        static List<string> Names(GraphTraverser.LinearDataEnumerable traversal)
        {
            var names = new List<string>();
            foreach (var node in traversal)
                names.Add(node.DataContainer.Name);
            return names;
        }
    }
}
