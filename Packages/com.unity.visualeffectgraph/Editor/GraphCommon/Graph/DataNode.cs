
using System;
using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// An Id associated to a given DataNode in a graph.
    /// </summary>
    /*public*/ readonly struct DataNodeId : IEquatable<DataNodeId>
    {
        /// <summary>
        /// Defines an invalid DataNodeId.
        /// </summary>
        public static readonly DataNodeId Invalid = new DataNodeId(-1);

        /// <summary>
        /// Implicitly converts a <see cref="DataNodeId"/> to a <see cref="GraphDataId"/>.
        /// </summary>
        /// <param name="id">The data node ID to convert.</param>
        /// <returns>A new graph data ID with the same index value.</returns>
        public static implicit operator GraphDataId(DataNodeId id) => new GraphDataId(id.Index);

        /// <summary>
        /// Implicitly converts a <see cref="GraphDataId"/> to a <see cref="DataNodeId"/>.
        /// </summary>
        /// <param name="id">The graph data ID to convert.</param>
        /// <returns>A new data node ID with the same index value.</returns>
        public static implicit operator DataNodeId(GraphDataId id) => new DataNodeId(id.Index);

        /// <summary>
        /// Gets the wrapped int index.
        /// </summary>
        public int Index { get; }
        /// <summary>
        /// Returns true if this Id is valid, false otherwise.
        /// </summary>
        public bool IsValid => Index != Invalid.Index;

        internal DataNodeId(int index)
        {
            Index = index;
        }

        /// <inheritdoc cref="IEquatable"/>
        public bool Equals(DataNodeId other) => Index == other.Index;
        /// <inheritdoc cref="ValueType"/>
        public override int GetHashCode() => Index;
        /// <inheritdoc cref="ValueType"/>
        public override string ToString() => Index.ToString();
    }

    readonly struct DataNodeInfo
    {
        public DataNodeInfo(DataNodeId id, TaskNodeId taskNodeId, DataContainerId dataContainerId)
        {
            Id = id;
            TaskNodeId = taskNodeId;
            DataContainerId = dataContainerId;
        }

        public DataNodeId Id { get; }
        public TaskNodeId TaskNodeId { get; }
        public DataContainerId DataContainerId { get; }
    }

    /// <summary>
    /// Represents a node in the graph that contains a data.
    /// </summary>
    /*public*/ readonly struct DataNode
    {
        readonly IIndexable<GraphNode<DataNodeId>, DataNode> m_NodeConverter;
        readonly GraphNode<DataNodeId> m_Node;

        readonly IReadOnlyGraph m_Graph;
        readonly DataNodeInfo m_Info;

        DataNodeInfo Info { get { CheckValid(); return m_Info; } }
        IReadOnlyGraph Graph { get { CheckValid(); return m_Graph; } }
        GraphNode<DataNodeId> Node { get { CheckValid(); return m_Node; } }

        /// <summary>
        /// Gets a value indicating whether this data node still refers to a live entry in the graph.
        /// </summary>
        public bool IsValid => m_Graph != null && m_Graph.IsValid(m_Info.Id);

        /// <summary>
        /// Gets the unique identifier for this data node.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data node has been removed from the graph.</exception>
        public DataNodeId Id => Info.Id;

        /// <summary>
        /// Gets the parent data nodes connected to this data node.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data node has been removed from the graph.</exception>
        public DataNodeLinks Parents => new(m_NodeConverter, Node.Parents);
        /// <summary>
        /// Gets the child data nodes connected to this data node.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data node has been removed from the graph.</exception>
        public DataNodeLinks Children => new(m_NodeConverter, Node.Children);

        void CheckValid() { if (!IsValid) throw new InvalidOperationException($"DataNode {m_Info.Id} no longer exists in the graph."); }

        /// <summary>
        /// Gets the task node this data node belongs to.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data node has been removed from the graph.</exception>
        public TaskNode TaskNode => Graph.TaskNodes[Info.TaskNodeId];

        /// <summary>
        /// Gets the data container represented by this data node.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data node has been removed from the graph.</exception>
        public DataContainer DataContainer => Graph.DataContainers[Info.DataContainerId];

        /// <summary>
        /// Gets the data views used by this data node, as a tree.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data node has been removed from the graph.</exception>
        public DataView UsedDataViewsRoot => Graph.GetUsedDataViews(Info.Id);

        /// <exception cref="InvalidOperationException">Thrown when the data node has been removed from the graph.</exception>
        public DataBindingEnumerable DataBindings => Graph.GetDataBindings(Info.Id);

        /// <summary>
        /// Gets the data views used by this data node, as an enumerable.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data node has been removed from the graph.</exception>
        public DataViewFlatTreeEnumerable UsedDataViews => UsedDataViewsRoot.Flat;

        /// <summary>
        /// Returns whether the specified data view is read by this data node.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data node has been removed from the graph.</exception>
        public bool IsRead(DataViewId dataViewId) => Graph.IsRead(Info.Id, dataViewId);

        /// <summary>
        /// Returns whether the specified data view is written by this data node.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data node has been removed from the graph.</exception>
        public bool IsWritten(DataViewId dataViewId) => Graph.IsWritten(Info.Id, dataViewId);

        internal DataNode(IIndexable<GraphNode<DataNodeId>, DataNode> nodeConverter, GraphNode<DataNodeId> node, IReadOnlyGraph graph, DataNodeInfo info)
        {
            m_NodeConverter = nodeConverter;
            m_Node = node;
            m_Graph = graph;
            m_Info = info;
        }
    }

    /// <summary>
    /// Represents a collection of links to data nodes in the graph.
    /// </summary>
    /*public*/ readonly struct DataNodeLinks : IIndexable<int, DataNode>, ICountable
    {
        readonly IIndexable<GraphNode<DataNodeId>, DataNode> m_NodeConverter;
        readonly GraphNodeLinks<DataNodeId> m_Links;

        /// <summary>
        /// Gets the number of data node links in this collection.
        /// </summary>
        public int Count => m_Links.Count;

        /// <summary>
        /// Gets the data node at the specified index in the collection.
        /// </summary>
        /// <param name="index">The zero-based index of the data node to get.</param>
        /// <value>The data node at the specified index.</value>
        public DataNode this[int index] => m_NodeConverter[m_Links[index]];

        internal DataNodeId GetId(int index) => m_Links[index].Data;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataNodeLinks"/> struct.
        /// </summary>
        /// <param name="nodeConverter">The converter used to convert graph nodes to data nodes.</param>
        /// <param name="links">The underlying graph node links.</param>
        public DataNodeLinks(IIndexable<GraphNode<DataNodeId>, DataNode> nodeConverter, GraphNodeLinks<DataNodeId> links)
        {
            m_NodeConverter = nodeConverter;
            m_Links = links;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the data node links and throws on
        /// concurrent modification of the underlying parents/children sublist. Composes the
        /// version-checked <see cref="GraphNodeLinksEnumerator{T}"/> with the graph-node-to-
        /// <see cref="DataNode"/> projection via the node converter.
        /// </summary>
        public ResolvingEnumerator<GraphNode<DataNodeId>, DataNode, GraphNodeLinksEnumerator<DataNodeId>> GetEnumerator() =>
            new(m_NodeConverter, m_Links.GetEnumerator());
    }

    /// <summary>
    /// Represents an enumerable collection of data nodes. Wraps a <see cref="SubEnumerable{T}"/>
    /// of <see cref="DataNodeId"/> and projects each id to a <see cref="DataNode"/> via a provider.
    /// Iteration via <c>foreach</c> composes the inner <see cref="VersionedSublistEnumerator{T}"/>,
    /// so concurrent modification of the backing sublist is detected and throws.
    /// </summary>
    /*public*/ readonly struct DataNodeEnumerable : IIndexable<int, DataNode>, ICountable
    {
        readonly IIndexable<DataNodeId, DataNode> m_Provider;
        readonly SubEnumerable<DataNodeId>        m_IdSource;

        public int Count => m_IdSource.Count;

        /// <remarks>
        /// Indexed access bypasses the version check (matches the BCL contract:
        /// <see cref="System.Collections.Generic.List{T}"/>'s indexer doesn't throw on
        /// concurrent modification either, only its enumerator does). Use <c>foreach</c> for
        /// the version-checked path.
        /// </remarks>
        public DataNode this[int index] => m_Provider[m_IdSource[index]];

        /// <summary>
        /// Gets the id at the specified index, without materializing a <see cref="DataNode"/>.
        /// Used by <see cref="GraphSnapshots"/> and the graph traverser hot loops.
        /// </summary>
        internal DataNodeId GetId(int index) => m_IdSource[index];

        public DataNodeEnumerable(IIndexable<DataNodeId, DataNode> provider, SubEnumerable<DataNodeId> idSource)
        {
            m_Provider = provider;
            m_IdSource = idSource;
        }

        /// <summary>
        /// Returns a version-checked enumerator. Composes the inner
        /// <see cref="VersionedSublistEnumerator{T}"/> with the id-to-DataNode projection.
        /// </summary>
        public ResolvingEnumerator<DataNodeId, DataNode, VersionedSublistEnumerator<DataNodeId>> GetEnumerator() =>
            new(m_Provider, m_IdSource.GetEnumerator());
    }

}
