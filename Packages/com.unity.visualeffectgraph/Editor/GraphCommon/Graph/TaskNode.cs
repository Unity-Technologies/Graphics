
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
        public TaskNodeInfo(TaskNodeId id, ITask task)
        {
            Id = id;
            Task = task;
        }

        public TaskNodeId Id { get; }
        public ITask Task { get; }
    }

    /// <summary>
    /// Represents a node in the graph that contains a task.
    /// </summary>
    /*public*/ readonly struct TaskNode
    {
        readonly IIndexable<GraphNode<TaskNodeId>, TaskNode> m_NodeConverter;
        readonly GraphNode<TaskNodeId> m_Node;

        readonly Handle<IReadOnlyGraph> m_Graph;
        readonly TaskNodeInfo m_Info;

        /// <summary>
        /// Gets the unique identifier for this task node. Returns an invalid ID if the graph is not valid.
        /// </summary>
        public TaskNodeId Id => m_Graph.Valid ? m_Info.Id : TaskNodeId.Invalid;

        /// <summary>
        /// Gets the task associated with this node. Returns null if the graph is not valid.
        /// </summary>
        public ITask Task => m_Graph.Valid ? m_Info.Task : null;

        /// <summary>
        /// Gets the parent task nodes connected to this task node.
        /// </summary>
        public TaskNodeLinks Parents => new(m_NodeConverter, m_Node.Parents);

        /// <summary>
        /// Gets the child task nodes connected to this task node.
        /// </summary>
        public TaskNodeLinks Children => new(m_NodeConverter, m_Node.Children);

        /// <summary>
        /// Gets the data nodes used by this task node.
        /// </summary>
        public DataNodeEnumerable<SubEnumerable<DataNodeId>> DataNodes => m_Graph.Ref.GetDataNodes(Id);

        /// <summary>
        /// Gets the data bindings used by this task node.
        /// </summary>
        public DataBindingEnumerable<SubEnumerable<DataBindingId>> DataBindings => m_Graph.Ref.GetDataBindings(Id);

        internal TaskNode(IIndexable<GraphNode<TaskNodeId>, TaskNode> nodeConverter, GraphNode<TaskNodeId> node, IReadOnlyGraph graph, TaskNodeInfo info)
        {
            m_NodeConverter = nodeConverter;
            m_Node = node;
            m_Graph = new(graph);
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
        /// Returns an enumerator that iterates through the task node links.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the task node links.</returns>
        public LinearEnumerator<TaskNodeLinks, TaskNode> GetEnumerator() => new(this);    }

    /// <summary>
    /// Represents an enumerable collection of task nodes.
    /// </summary>
    /// <typeparam name="T">The type of the source collection that contains task node IDs.</typeparam>
    /*public*/ readonly struct TaskNodeEnumerable<T> : IIndexable<int, TaskNode>, ICountable where T : IIndexable<int, TaskNodeId>, ICountable
    {
        readonly IIndexable<TaskNodeId, TaskNode> m_Provider;
        readonly T m_IdSource;

        /// <summary>
        /// Gets the number of task nodes in the collection.
        /// </summary>
        public int Count => m_IdSource.Count;

        /// <summary>
        /// Gets the task node at the specified index in the collection.
        /// </summary>
        /// <param name="index">The zero-based index of the task node to get.</param>
        /// <value>The task node at the specified index.</value>
        public TaskNode this[int index] => m_Provider[m_IdSource[index]];

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskNodeEnumerable{T}"/> struct.
        /// </summary>
        /// <param name="provider">The provider that can retrieve task nodes by ID.</param>
        /// <param name="idSource">The source collection of task node IDs.</param>
        public TaskNodeEnumerable(IIndexable<TaskNodeId, TaskNode> provider, T idSource)
        {
            m_Provider = provider;
            m_IdSource = idSource;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the task nodes.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the task nodes.</returns>
        public LinearEnumerator<TaskNodeEnumerable<T>, TaskNode> GetEnumerator() => new(this);    }
}
