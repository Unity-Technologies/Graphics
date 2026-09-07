using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX.Test
{
    /// <summary>
    /// Minimal <see cref="ITask"/>. Bindings declare no usage unless <see cref="Declare"/> is called for
    /// their key, in which case <c>GetBindingUsage</c> reports it — which is what lets the removal APIs
    /// match a node's reads against another's writes.
    /// </summary>
    sealed class TestTask : ITask
    {
        readonly Dictionary<IDataKey, (DataPathSet read, DataPathSet write)> m_Usage = new();

        public string Name { get; }

        public TestTask(string name) => Name = name;

        public void Declare(IDataKey bindingKey, DataPathSet read, DataPathSet write)
            => m_Usage[bindingKey] = (read, write);

        public BindingUsage GetBindingUsage(IDataKey dataKey, DataPathSet readUsage = null, DataPathSet writeUsage = null)
        {
            if (!m_Usage.TryGetValue(dataKey, out var declared))
                return BindingUsage.Unknown;

            var usage = BindingUsage.Unknown;
            if (!declared.read.Empty)
            {
                readUsage?.Add(declared.read);
                usage |= BindingUsage.Read;
            }
            if (!declared.write.Empty)
            {
                writeUsage?.Add(declared.write);
                usage |= BindingUsage.Write;
            }
            return usage;
        }

        public override string ToString() => Name;
    }

    /// <summary>
    /// Builds a <see cref="TaskGraph"/> fixture from named task nodes, data containers and
    /// dependencies. Task, container and data node names are independent, and a task may own several
    /// data nodes; <see cref="Nodes"/> is shorthand for the one-task-one-container case, which keeps
    /// all three equal so the task and data graphs are topologically identical.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="TaskGraph"/> has no task-to-task connect API: one
    /// <see cref="TaskGraph.AddDataDependency"/> adds an edge to the data graph and a derived one
    /// between the owning tasks. Declare before binding, bind before depending, and never declare a
    /// dependency twice — <c>LinearDirectedGraph.Connect</c> asserts on duplicates.
    /// </para>
    /// <para>
    /// Declaration goes through a <see cref="TaskGraphBuilder"/>, which is what infers the parents of a
    /// read. Removal is only available once building has ended, so the first removal ends it — and
    /// anything declared after that gets no inferred parents, only what <see cref="DataEdge"/> adds.
    /// </para>
    /// </remarks>
    sealed class TestGraphBuilder
    {
        /// <summary>Path entry standing for the container root rather than one of its sub-values.</summary>
        public const string Root = "";

        readonly TaskGraphBuilder m_Builder = new();
        // Handed over by the builder; null while still building.
        TaskGraph m_Graph;

        readonly Dictionary<string, TaskNodeId> m_TaskIds = new();
        readonly Dictionary<string, TestTask> m_Tasks = new();
        readonly Dictionary<string, DataViewId> m_ContainerViewIds = new();
        readonly Dictionary<string, DataContainerId> m_ContainerIds = new();
        readonly Dictionary<string, DataNodeId> m_DataNodeIds = new();
        // Data node key -> the task that owns it, used to derive the task-level edge.
        readonly Dictionary<string, string> m_DataNodeOwner = new();

        readonly List<(string parent, string child)> m_TaskEdges = new();
        readonly List<(string parent, string child)> m_DataEdges = new();

        /// <summary>The graph, whether still being built or already handed over.</summary>
        public IReadOnlyGraph Graph => (IReadOnlyGraph)m_Graph ?? m_Builder.Graph;

        /// <summary>Edges in declaration order, per graph.</summary>
        public IReadOnlyList<(string parent, string child)> TaskEdges => m_TaskEdges;
        public IReadOnlyList<(string parent, string child)> DataEdges => m_DataEdges;

        /// <summary>The graph as mutable, ending the declaration phase to get at it.</summary>
        TaskGraph MutableGraph
        {
            get
            {
                EndBuilding();
                return m_Graph;
            }
        }

        // Declaration goes through the builder until a removal ends building, and through the graph after
        // that. Parent inference only exists on the builder, so a bind declared after a removal gets none;
        // use DataEdge for those.
        TaskNodeId DeclareTask(ITask task, string name)
            => m_Graph == null ? m_Builder.AddTask(task, name) : m_Graph.AddTask(task, name);

        DataViewId DeclareData(string name, IDataDescription dataDescription)
            => m_Graph == null ? m_Builder.AddData(name, dataDescription) : m_Graph.AddData(name, dataDescription);

        void DeclareBinding(TaskNodeId taskId, IDataKey bindingKey, DataViewId dataViewId, BindingUsage usage)
        {
            if (m_Graph == null)
                m_Builder.BindData(taskId, bindingKey, dataViewId, usage);
            else
                m_Graph.BindData(taskId, bindingKey, dataViewId, Array.Empty<DataNodeId>(), usage);
        }

        bool DeclareDependency(DataNodeId dataNodeId, DataNodeId parentDataNodeId)
            => m_Graph == null
                ? m_Builder.AddDataDependency(dataNodeId, parentDataNodeId)
                : m_Graph.AddDataDependency(dataNodeId, parentDataNodeId);

        public GraphTraverser CreateTraverser() => Graph.CreateTraverser();

        public TaskNode GetTaskNode(string taskName) => Graph.TaskNodes[m_TaskIds[taskName]];
        public DataNode GetDataNode(string dataNodeName) => Graph.DataNodes[m_DataNodeIds[dataNodeName]];

        // --- Declaration ------------------------------------------------------------------------

        public TestGraphBuilder AddTask(string taskName)
        {
            Assert.That(m_TaskIds.ContainsKey(taskName), Is.False, $"Task '{taskName}' was already declared.");
            var task = new TestTask(taskName);
            m_Tasks.Add(taskName, task);
            m_TaskIds.Add(taskName, DeclareTask(task, taskName));
            return this;
        }

        /// <summary>
        /// Declares a container and its root data view. Passing sub-keys makes the root a
        /// <see cref="StructuredData"/> holding one float per key, so <see cref="Bind"/> can declare usage
        /// at sub-view granularity.
        /// </summary>
        public TestGraphBuilder AddData(string containerName, params string[] subKeys)
        {
            Assert.That(m_ContainerViewIds.ContainsKey(containerName), Is.False,
                $"Data container '{containerName}' was already declared.");

            IDataDescription description;
            if (subKeys.Length == 0)
            {
                description = new ValueData<float>();
            }
            else
            {
                var structured = new StructuredData();
                foreach (var subKey in subKeys)
                    structured.AddSubdata(new NameDataKey(subKey), new ValueData<float>());
                description = structured;
            }

            var rootDataViewId = DeclareData(containerName, description);
            m_ContainerViewIds.Add(containerName, rootDataViewId);
            // Recorded now, because it can no longer be resolved through the view once the container is removed.
            m_ContainerIds.Add(containerName, Graph.DataViews[rootDataViewId].DataContainer.Id);
            return this;
        }

        /// <summary>
        /// Creates the data node for a (task, container) pair, registered under
        /// <paramref name="dataNodeName"/> (the container name when omitted).
        /// <para>
        /// <paramref name="read"/> and <paramref name="write"/> are comma-separated sub-key lists, or
        /// <see cref="Root"/> for the whole container. Declaring them makes a read depend implicitly on the
        /// last node to have written the same view, so writer-before-reader order shapes the graph. Declaring
        /// neither leaves the shape to the explicit <see cref="DataEdge"/> calls.
        /// </para>
        /// </summary>
        public TestGraphBuilder Bind(string taskName, string containerName,
            string read = null, string write = null,
            BindingUsage usage = BindingUsage.Write, string dataNodeName = null)
        {
            dataNodeName ??= containerName;
            Assert.That(m_DataNodeIds.ContainsKey(dataNodeName), Is.False,
                $"Data node '{dataNodeName}' was already registered; pass an explicit dataNodeName.");

            var taskId = m_TaskIds[taskName];
            var bindingKey = new NameDataKey(dataNodeName);

            if (read != null || write != null)
            {
                m_Tasks[taskName].Declare(bindingKey, ToPathSet(read), ToPathSet(write));
                usage = BindingUsage.Unknown; // resolved from the declaration
            }

            DeclareBinding(taskId, bindingKey, m_ContainerViewIds[containerName], usage);

            // Several bindings of one task to one container share a single data node, so the node is looked
            // up through the binding rather than taken as the last one added.
            var binding = Graph.TaskNodes[taskId].DataBindings[bindingKey];
            Assert.That(binding.HasValue, Is.True, $"Binding '{taskName}' to '{containerName}' was not created.");
            m_DataNodeIds.Add(dataNodeName, binding.Value.DataNode.Id);
            m_DataNodeOwner[dataNodeName] = taskName;
            return this;
        }

        /// <summary>The sub-view of a container declared with sub-keys.</summary>
        public DataViewId GetSubView(string containerName, string subKey)
        {
            var found = Graph.DataViews[m_ContainerViewIds[containerName]]
                .FindSubData(new NameDataKey(subKey), out var subDataView);
            Assert.That(found, Is.True, $"Container '{containerName}' has no materialised sub-view '{subKey}'.");
            return subDataView.Id;
        }

        // --- Mutation and state, by name ---------------------------------------------------------

        /// <summary>Ends the declaration phase. Idempotent; declaring anything afterwards throws.</summary>
        public void EndBuilding()
        {
            if (m_Graph == null)
                m_Graph = m_Builder.EndBuilding();
        }

        public void RemoveTask(string taskName) => MutableGraph.RemoveTask(m_TaskIds[taskName]);

        public void UnbindData(string taskName, string bindingName)
            => MutableGraph.UnbindData(m_TaskIds[taskName], new NameDataKey(bindingName));

        public void RemoveDataContainer(string containerName)
            => MutableGraph.RemoveDataContainer(m_ContainerIds[containerName]);

        public bool IsTaskAlive(string taskName) => Graph.IsValid(m_TaskIds[taskName]);
        public bool IsDataNodeAlive(string dataNodeName) => Graph.IsValid(m_DataNodeIds[dataNodeName]);
        public bool IsContainerAlive(string containerName) => Graph.IsValid(m_ContainerIds[containerName]);
        public int BindingCount(string taskName) => GetTaskNode(taskName).DataBindings.Count;

        /// <summary>Ids, for the state that has to be captured before a removal to be asserted after it.</summary>
        public DataViewId ContainerViewId(string containerName) => m_ContainerViewIds[containerName];
        public DataNodeId DataNodeIdOf(string dataNodeName) => m_DataNodeIds[dataNodeName];
        public bool IsViewAlive(DataViewId dataViewId) => Graph.IsValid(dataViewId);

        public bool IsRead(string dataNodeName, string subKey) => Usage(dataNodeName, subKey, read: true);
        public bool IsWritten(string dataNodeName, string subKey) => Usage(dataNodeName, subKey, read: false);

        bool Usage(string dataNodeName, string subKey, bool read)
        {
            var node = GetDataNode(dataNodeName);
            var view = subKey == Root
                ? m_ContainerViewIds[node.DataContainer.Name]
                : GetSubView(node.DataContainer.Name, subKey);
            return read ? node.IsRead(view) : node.IsWritten(view);
        }

        /// <summary>
        /// Usage by id, which unlike <see cref="IsWritten"/> still answers for a data node that has been
        /// removed — the <see cref="DataNode"/> handle throws once its node is gone.
        /// </summary>
        public bool IsWrittenById(DataNodeId dataNodeId, DataViewId dataViewId)
            => Graph.IsWritten(dataNodeId, dataViewId);

        public bool IsReadById(DataNodeId dataNodeId, DataViewId dataViewId)
            => Graph.IsRead(dataNodeId, dataViewId);

        /// <summary>The registered names of a data node's parents, for order-independent assertions.</summary>
        public string[] ParentNames(string dataNodeName)
        {
            var node = GetDataNode(dataNodeName);
            var names = new List<string>(node.Parents.Count);
            foreach (var parent in node.Parents)
                names.Add(NameOf(parent.Id));
            return names.ToArray();
        }

        string NameOf(DataNodeId dataNodeId)
        {
            foreach (var pair in m_DataNodeIds)
            {
                if (pair.Value.Equals(dataNodeId))
                    return pair.Key;
            }
            return $"<unregistered {dataNodeId}>";
        }

        static DataPathSet ToPathSet(string subKeys)
        {
            var paths = new DataPathSet();
            if (subKeys == null)
                return paths;
            foreach (var subKey in subKeys.Split(','))
            {
                var trimmed = subKey.Trim();
                paths.Add(trimmed.Length == 0 ? DataPath.Root : DataPath.Root + new NameDataKey(trimmed));
            }
            return paths;
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

            bool added = DeclareDependency(m_DataNodeIds[childDataNodeName], m_DataNodeIds[parentDataNodeName]);
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
