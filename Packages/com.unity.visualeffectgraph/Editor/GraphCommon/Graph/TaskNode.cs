using System;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// An Id associated to a given TaskNode in a graph.
    /// </summary>
    /*public*/ readonly struct TaskNodeId : System.IEquatable<TaskNodeId>
    {
        /// <summary>
        /// Defines an invalid TaskNodeId.
        /// </summary>
        public static readonly TaskNodeId Invalid = new TaskNodeId(-1);

        /// <summary>
        /// Implicitly converts a <see cref="TaskNodeId"/> to a <see cref="GraphDataId"/>.
        /// </summary>
        /// <param name="id">The task node ID to convert.</param>
        /// <returns>A new graph data ID with the same index value.</returns>
        public static implicit operator GraphDataId(TaskNodeId id) => new GraphDataId(id.Index);

        /// <summary>
        /// Implicitly converts a <see cref="GraphDataId"/> to a <see cref="TaskNodeId"/>.
        /// </summary>
        /// <param name="id">The graph data ID to convert.</param>
        /// <returns>A new task node ID with the same index value.</returns>
        public static implicit operator TaskNodeId(GraphDataId id) => new TaskNodeId(id.Index);

        /// <summary>
        /// Gets the wrapped int index.
        /// </summary>
        public int Index { get; }
        /// <summary>
        /// Returns true if this Id is valid, false otherwise.
        /// </summary>
        public bool IsValid => Index != Invalid.Index;

        internal TaskNodeId(int index)
        {
            Index = index;
        }

        /// <inheritdoc cref="IEquatable"/>
        public bool Equals(TaskNodeId other) => Index == other.Index;
        /// <inheritdoc cref="ValueType"/>
        public override int GetHashCode() => Index;
        /// <inheritdoc cref="ValueType"/>
        public override string ToString() => Index.ToString();
    }

    readonly struct TaskNodeInfo
    {
        public TaskNodeInfo(TaskNodeId id, ITask task, string name)
        {
            Id = id;
            Task = task;
            Name = name;
        }

        public TaskNodeId Id { get; }
        public ITask Task { get; }
        public string Name { get; }
    }

    /// <summary>
    /// Represents a node in the graph that contains a task.
    /// </summary>
    /*public*/
    readonly struct TaskNode
    {
        readonly IIndexable<GraphNode<TaskNodeId>, TaskNode> m_NodeConverter;
        readonly GraphNode<TaskNodeId> m_Node;

        readonly IReadOnlyGraph m_Graph;
        readonly TaskNodeInfo m_Info;

        TaskNodeInfo Info { get { CheckValid(); return m_Info; } }
        IReadOnlyGraph Graph { get { CheckValid(); return m_Graph; } }
        GraphNode<TaskNodeId> Node { get { CheckValid(); return m_Node; } }

        /// <summary>
        /// Gets a value indicating whether this task node still refers to a live entry in the graph.
        /// </summary>
        public bool IsValid => m_Graph != null && m_Graph.IsValid(m_Info.Id);

        /// <summary>
        /// Gets the unique identifier for this task node.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the task node has been removed from the graph.</exception>
        public TaskNodeId Id => Info.Id;

        /// <summary>
        /// Gets the task associated with this node.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the task node has been removed from the graph.</exception>
        public ITask Task => Info.Task;

        /// <summary>
        /// Gets the name of this node.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the task node has been removed from the graph.</exception>
        public string Name => Info.Name;

        /// <summary>
        /// Gets the parent task nodes connected to this task node.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the task node has been removed from the graph.</exception>
        public TaskNodeLinks Parents => new(m_NodeConverter, Node.Parents);

        /// <summary>
        /// Gets the child task nodes connected to this task node.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the task node has been removed from the graph.</exception>
        public TaskNodeLinks Children => new(m_NodeConverter, Node.Children);

        void CheckValid() { if (!IsValid) throw new InvalidOperationException($"TaskNode {m_Info.Id} no longer exists in the graph."); }

        /// <summary>
        /// Gets the data nodes used by this task node.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the task node has been removed from the graph.</exception>
        public DataNodeEnumerable DataNodes => Graph.GetDataNodes(Info.Id);

        /// <summary>
        /// Gets the data bindings used by this task node.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the task node has been removed from the graph.</exception>
        public DataBindingEnumerable DataBindings => Graph.GetDataBindings(Info.Id);

        internal TaskNode(IIndexable<GraphNode<TaskNodeId>, TaskNode> nodeConverter, GraphNode<TaskNodeId> node, IReadOnlyGraph graph, TaskNodeInfo info)
        {
            m_NodeConverter = nodeConverter;
            m_Node = node;
            m_Graph = graph;
            m_Info = info;
        }
    }

    /// <summary>
    /// Represents a collection of links to task nodes in the graph.
    /// </summary>
    /*public*/ readonly struct TaskNodeLinks : IIndexable<int, TaskNode>, ICountable
    {
        readonly IIndexable<GraphNode<TaskNodeId>, TaskNode> m_NodeConverter;
        readonly GraphNodeLinks<TaskNodeId> m_Links;

        /// <summary>
        /// Gets the number of task node links in this collection.
        /// </summary>
        public int Count => m_Links.Count;

        /// <summary>
        /// Gets the task node at the specified index in the collection.
        /// </summary>
        /// <param name="index">The zero-based index of the task node to get.</param>
        /// <value>The task node at the specified index.</value>
        public TaskNode this[int index] => m_NodeConverter[m_Links[index]];

        internal TaskNodeId GetId(int index) => m_Links[index].Data;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskNodeLinks"/> struct.
        /// </summary>
        /// <param name="nodeConverter">The converter used to convert graph nodes to task nodes.</param>
        /// <param name="links">The underlying graph node links.</param>
        public TaskNodeLinks(IIndexable<GraphNode<TaskNodeId>, TaskNode> nodeConverter, GraphNodeLinks<TaskNodeId> links)
        {
            m_NodeConverter = nodeConverter;
            m_Links = links;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the task node links and throws on
        /// concurrent modification of the underlying parents/children sublist. Composes the
        /// version-checked <see cref="GraphNodeLinksEnumerator{T}"/> with the graph-node-to-
        /// <see cref="TaskNode"/> projection via the node converter.
        /// </summary>
        public ResolvingEnumerator<GraphNode<TaskNodeId>, TaskNode, GraphNodeLinksEnumerator<TaskNodeId>> GetEnumerator() =>
            new(m_NodeConverter, m_Links.GetEnumerator());
    }

    /// <summary>
    /// Represents an enumerable collection of task nodes. Wraps a <see cref="SubEnumerable{T}"/>
    /// of <see cref="TaskNodeId"/> and projects each id to a <see cref="TaskNode"/> via a provider.
    /// Iteration via <c>foreach</c> composes the inner <see cref="VersionedSublistEnumerator{T}"/>,
    /// so concurrent modification of the backing sublist is detected and throws.
    /// </summary>
    /*public*/ readonly struct TaskNodeEnumerable : IIndexable<int, TaskNode>, ICountable
    {
        readonly IIndexable<TaskNodeId, TaskNode> m_Provider;
        readonly SubEnumerable<TaskNodeId>        m_IdSource;

        public int Count => m_IdSource.Count;

        /// <remarks>
        /// Indexed access bypasses the version check (matches the BCL contract). Use
        /// <c>foreach</c> for the version-checked path.
        /// </remarks>
        public TaskNode this[int index] => m_Provider[m_IdSource[index]];

        /// <summary>
        /// Gets the id at the specified index, without materializing a <see cref="TaskNode"/>.
        /// Used by <see cref="GraphSnapshots"/>.
        /// </summary>
        internal TaskNodeId GetId(int index) => m_IdSource[index];

        public TaskNodeEnumerable(IIndexable<TaskNodeId, TaskNode> provider, SubEnumerable<TaskNodeId> idSource)
        {
            m_Provider = provider;
            m_IdSource = idSource;
        }

        public ResolvingEnumerator<TaskNodeId, TaskNode, VersionedSublistEnumerator<TaskNodeId>> GetEnumerator() =>
            new(m_Provider, m_IdSource.GetEnumerator());
    }
}
