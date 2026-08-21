using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX.Test
{
    /// <summary>
    /// Minimal <see cref="ITask"/>: every member has a default implementation, so nothing needs
    /// overriding. The name only makes failures readable.
    /// </summary>
    sealed class TestTask : ITask
    {
        public string Name { get; }

        public TestTask(string name) => Name = name;

        public override string ToString() => Name;
    }

    /// <summary>
    /// Builds a <see cref="TaskGraph"/> fixture from named task nodes, data containers and
    /// dependencies. Task, container and data node names are independent, and a task may own several
    /// data nodes; <see cref="Nodes"/> is shorthand for the one-task-one-container case, which keeps
    /// all three equal so the task and data graphs are topologically identical.
    /// </summary>
    /// <remarks>
    /// <see cref="TaskGraph"/> has no task-to-task connect API: one
    /// <see cref="TaskGraph.AddDataDependency"/> adds an edge to the data graph and a derived one
    /// between the owning tasks. Declare before binding, bind before depending, and never declare a
    /// dependency twice — <c>LinearDirectedGraph.Connect</c> asserts on duplicates.
    /// </remarks>
    sealed class TestGraphBuilder
    {
        readonly TaskGraph m_Graph = new();

        readonly Dictionary<string, TaskNodeId> m_TaskIds = new();
        readonly Dictionary<string, DataViewId> m_ContainerViewIds = new();
        readonly Dictionary<string, DataNodeId> m_DataNodeIds = new();
        // Data node key -> the task that owns it, used to derive the task-level edge.
        readonly Dictionary<string, string> m_DataNodeOwner = new();

        readonly List<(string parent, string child)> m_TaskEdges = new();
        readonly List<(string parent, string child)> m_DataEdges = new();

        public TaskGraph Graph => m_Graph;
        public IReadOnlyGraph ReadOnlyGraph => m_Graph;

        /// <summary>Edges in declaration order, per graph.</summary>
        public IReadOnlyList<(string parent, string child)> TaskEdges => m_TaskEdges;
        public IReadOnlyList<(string parent, string child)> DataEdges => m_DataEdges;

        public GraphTraverser CreateTraverser() => ReadOnlyGraph.CreateTraverser();

        public TaskNode GetTaskNode(string taskName) => m_Graph.TaskNodes[m_TaskIds[taskName]];
        public DataNode GetDataNode(string dataNodeName) => m_Graph.DataNodes[m_DataNodeIds[dataNodeName]];

        // --- Declaration ------------------------------------------------------------------------

        public TestGraphBuilder AddTask(string taskName)
        {
            Assert.That(m_TaskIds.ContainsKey(taskName), Is.False, $"Task '{taskName}' was already declared.");
            m_TaskIds.Add(taskName, m_Graph.AddTask(new TestTask(taskName), taskName));
            return this;
        }

        /// <summary>Declares a container and its root data view.</summary>
        public TestGraphBuilder AddData(string containerName)
        {
            Assert.That(m_ContainerViewIds.ContainsKey(containerName), Is.False,
                $"Data container '{containerName}' was already declared.");
            m_ContainerViewIds.Add(containerName, m_Graph.AddData(containerName, new ValueData<float>()));
            return this;
        }

        /// <summary>
        /// Creates the data node for a (task, container) pair, registered under
        /// <paramref name="dataNodeName"/> (the container name when omitted). The default
        /// <see cref="BindingUsage.Write"/> adds no implicit dependency, so the shape stays
        /// determined by the explicit <see cref="DataEdge"/> calls; <see cref="BindingUsage.Read"/>
        /// would instead link to the container's last writer.
        /// </summary>
        public TestGraphBuilder Bind(string taskName, string containerName,
            BindingUsage usage = BindingUsage.Write, string dataNodeName = null)
        {
            dataNodeName ??= containerName;
            Assert.That(m_DataNodeIds.ContainsKey(dataNodeName), Is.False,
                $"Data node '{dataNodeName}' was already registered; pass an explicit dataNodeName.");

            var taskId = m_TaskIds[taskName];
            var beforeCount = m_Graph.TaskNodes[taskId].DataNodes.Count;

            m_Graph.BindData(taskId, new NameDataKey(dataNodeName), m_ContainerViewIds[containerName], usage);

            var dataNodes = m_Graph.TaskNodes[taskId].DataNodes;
            Assert.That(dataNodes.Count, Is.EqualTo(beforeCount + 1),
                $"Binding '{taskName}' to '{containerName}' should have created exactly one data node.");

            m_DataNodeIds.Add(dataNodeName, dataNodes[dataNodes.Count - 1].Id);
            m_DataNodeOwner.Add(dataNodeName, taskName);
            return this;
        }

        /// <summary>One task bound to one same-named container, per name.</summary>
        public TestGraphBuilder Nodes(params string[] names)
        {
            foreach (var name in names)
                AddTask(name).AddData(name).Bind(name, name);
            return this;
        }

        // --- Dependencies -----------------------------------------------------------------------

        /// <summary>Dependencies written as <c>"Parent-&gt;Child"</c>, to keep shapes readable.</summary>
        public TestGraphBuilder Edges(params string[] edgeSpecs)
        {
            foreach (var spec in edgeSpecs)
            {
                var parts = spec.Split(new[] { "->" }, StringSplitOptions.None);
                Assert.That(parts, Has.Length.EqualTo(2), $"Malformed edge spec '{spec}', expected \"Parent->Child\".");
                DataEdge(parts[0].Trim(), parts[1].Trim());
            }
            return this;
        }

        /// <summary><see cref="DataEdge"/>, named for shapes where all three names coincide.</summary>
        public TestGraphBuilder Edge(string parent, string child) => DataEdge(parent, child);

        /// <summary>Data-node dependency, plus the derived edge between the owning tasks.</summary>
        public TestGraphBuilder DataEdge(string parentDataNodeName, string childDataNodeName)
        {
            string parentTask = m_DataNodeOwner[parentDataNodeName];
            string childTask = m_DataNodeOwner[childDataNodeName];
            // Two data nodes of the same task would derive a task edge onto itself.
            Assert.That(childTask, Is.Not.EqualTo(parentTask),
                $"'{parentDataNodeName}' and '{childDataNodeName}' both belong to task '{parentTask}'; " +
                "a dependency between them would make that task its own parent.");

            bool added = m_Graph.AddDataDependency(m_DataNodeIds[childDataNodeName], m_DataNodeIds[parentDataNodeName]);
            Assert.That(added, Is.True, $"Dependency {parentDataNodeName}->{childDataNodeName} was already declared.");

            m_DataEdges.Add((parentDataNodeName, childDataNodeName));
            m_TaskEdges.Add((parentTask, childTask));
            return this;
        }

        // --- Named shapes shared by the fixtures -------------------------------------------------

        /// <summary>Each node the single parent of the next.</summary>
        public static TestGraphBuilder Chain(params string[] names)
        {
            var builder = new TestGraphBuilder().Nodes(names);
            for (int i = 0; i < names.Length - 1; i++)
                builder.Edge(names[i], names[i + 1]);
            return builder;
        }

        /// <summary>A single node with no edges.</summary>
        public static TestGraphBuilder Single() => new TestGraphBuilder().Nodes("A");

        /// <summary>A→B, A→C, B→D, C→D: one root, one leaf, one shared descendant.</summary>
        public static TestGraphBuilder Diamond() => new TestGraphBuilder()
            .Nodes("A", "B", "C", "D")
            .Edges("A->B", "A->C", "B->D", "C->D");

        /// <summary>A strict tree: A→B, A→C, B→D, B→E, C→F.</summary>
        public static TestGraphBuilder Tree() => new TestGraphBuilder()
            .Nodes("A", "B", "C", "D", "E", "F")
            .Edges("A->B", "A->C", "B->D", "B->E", "C->F");

        /// <summary>Two roots converging: A→C, B→C, C→D.</summary>
        public static TestGraphBuilder TwoRoots() => new TestGraphBuilder()
            .Nodes("A", "B", "C", "D")
            .Edges("A->C", "B->C", "C->D");

        /// <summary>
        /// Two roots, a node with parents in two branches (D), and a leaf reachable through two
        /// mid-level ancestors (H via F and G) — the DAG case <c>DepthFirstPost</c> must handle.
        /// <code>
        ///    A       B      layer 0 (roots)
        ///   / \     / \
        ///  C   \   /   E    layer 1
        ///   \   \ /   /
        ///    \   D   /      D has two parents (A, B)
        ///     \ / \ /
        ///      F   G        layer 2
        ///       \ /
        ///        H          layer 3 (leaf)
        /// </code>
        /// </summary>
        public static TestGraphBuilder ComplexDag() => new TestGraphBuilder()
            .Nodes("A", "B", "C", "D", "E", "F", "G", "H")
            .Edges("A->C", "A->D", "B->D", "B->E", "C->F", "D->F", "D->G", "E->G", "F->H", "G->H");

        // --- Ordering assertions over the declared topology ---------------------------------------

        /// <summary>
        /// Every task appears after all its declared parents, for the tasks present in
        /// <paramref name="order"/>. Accepts any valid ordering rather than one exact sequence.
        /// </summary>
        public void AssertTaskParentsFirst(IReadOnlyList<string> order) => AssertParentsFirst(m_TaskEdges, order);

        /// <summary>Every task appears after all its declared children.</summary>
        public void AssertTaskChildrenFirst(IReadOnlyList<string> order) => AssertChildrenFirst(m_TaskEdges, order);

        public void AssertDataParentsFirst(IReadOnlyList<string> order) => AssertParentsFirst(m_DataEdges, order);
        public void AssertDataChildrenFirst(IReadOnlyList<string> order) => AssertChildrenFirst(m_DataEdges, order);

        static void AssertParentsFirst(List<(string parent, string child)> edges, IReadOnlyList<string> order)
        {
            foreach (var (parent, child) in edges)
            {
                int parentIndex = IndexOf(order, parent);
                int childIndex = IndexOf(order, child);
                if (parentIndex < 0 || childIndex < 0)
                    continue; // edge not covered by this traversal

                Assert.That(parentIndex, Is.LessThan(childIndex),
                    $"'{parent}' must be visited before its child '{child}' in [{string.Join(",", order)}].");
            }
        }

        static void AssertChildrenFirst(List<(string parent, string child)> edges, IReadOnlyList<string> order)
        {
            foreach (var (parent, child) in edges)
            {
                int parentIndex = IndexOf(order, parent);
                int childIndex = IndexOf(order, child);
                if (parentIndex < 0 || childIndex < 0)
                    continue; // edge not covered by this traversal

                Assert.That(childIndex, Is.LessThan(parentIndex),
                    $"'{child}' must be visited before its parent '{parent}' in [{string.Join(",", order)}].");
            }
        }

        static int IndexOf(IReadOnlyList<string> list, string value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == value)
                    return i;
            }
            return -1;
        }
    }
}
