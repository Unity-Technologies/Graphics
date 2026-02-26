
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

        readonly Handle<IReadOnlyGraph> m_Graph;
        readonly DataNodeInfo m_Info;

        /// <summary>
        /// Gets the unique identifier for this data node. Returns an invalid ID if the graph is not valid.
        /// </summary>
        public DataNodeId Id => m_Graph.Valid ? m_Info.Id : DataNodeId.Invalid;

        /// <summary>
        /// Gets the parent data nodes connected to this data node.
        /// </summary>
        public DataNodeLinks Parents => new(m_NodeConverter, m_Node.Parents);
        /// <summary>
        /// Gets the child data nodes connected to this data node.
        /// </summary>
        public DataNodeLinks Children => new(m_NodeConverter, m_Node.Children);

        /// <summary>
        /// Gets the task node this data node belongs to.
        /// </summary>
        public TaskNode TaskNode => m_Graph.Ref.TaskNodes[m_Info.TaskNodeId];

        /// <summary>
        /// Gets the data container represented by this data node.
        /// </summary>
        public DataContainer DataContainer => m_Graph.Ref.DataContainers[m_Info.DataContainerId];

        /// <summary>
        /// Gets the data views used by this data node, as a tree.
        /// </summary>
        public DataView UsedDataViewsRoot => m_Graph.Ref.GetUsedDataViews(Id);

        /// <summary>
        /// Gets the data views used by this data node, as an enumerable.
        /// </summary>
        public DataViewFlatTreeEnumerable UsedDataViews => UsedDataViewsRoot.Flat;

        /// <summary>
        /// Gets the data views read by this data node, as a tree.
        /// </summary>
        public DataView ReadDataViewsRoot => m_Graph.Ref.GetReadDataViews(Id);

        /// <summary>
        /// Gets the data views read by this data node, as an enumerable.
        /// </summary>
        public DataViewFlatTreeEnumerable ReadDataViews => ReadDataViewsRoot.Flat;

        /// <summary>
        /// Gets the data views written by this data node, as a tree.
        /// </summary>
        public DataView WrittenDataViewsRoot => m_Graph.Ref.GetWrittenDataViews(Id);

        /// <summary>
        /// Gets the data views written by this data node, as an enumerable.
        /// </summary>
        public DataViewFlatTreeEnumerable WrittenDataViews => WrittenDataViewsRoot.Flat;
        
        internal DataNode(IIndexable<GraphNode<DataNodeId>, DataNode> nodeConverter, GraphNode<DataNodeId> node, IReadOnlyGraph graph, DataNodeInfo info)
        {
            m_NodeConverter = nodeConverter;
            m_Node = node;
            m_Graph = new(graph);
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
        /// Returns an enumerator that iterates through the data node links.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the data node links.</returns>
        public LinearEnumerator<DataNodeLinks, DataNode> GetEnumerator() => new(this);
    }

    /// <summary>
    /// Represents an enumerable collection of data nodes.
    /// </summary>
    /// <typeparam name="T">The type of the source collection that contains data node IDs.</typeparam>
    /*public*/ readonly struct DataNodeEnumerable<T> : IIndexable<int, DataNode>, ICountable where T : IIndexable<int, DataNodeId>, ICountable
    {
        readonly IIndexable<DataNodeId, DataNode> m_Provider;
        readonly T m_IdSource;

        /// <summary>
        /// Gets the number of data nodes in the collection.
        /// </summary>
        public int Count => m_IdSource.Count;

        /// <summary>
        /// Gets the data node at the specified index in the collection.
        /// </summary>
        /// <param name="index">The zero-based index of the data node to get.</param>
        /// <value>The data node at the specified index.</value>
        public DataNode this[int index] => m_Provider[m_IdSource[index]];

        /// <summary>
        /// Initializes a new instance of the <see cref="DataNodeEnumerable{T}"/> struct.
        /// </summary>
        /// <param name="provider">The provider that can retrieve data nodes by ID.</param>
        /// <param name="idSource">The source collection of data node IDs.</param>
        public DataNodeEnumerable(IIndexable<DataNodeId, DataNode> provider, T idSource)
        {
            m_Provider = provider;
            m_IdSource = idSource;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the data nodes.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the data nodes.</returns>
        public LinearEnumerator<DataNodeEnumerable<T>, DataNode> GetEnumerator() => new(this);
    }

}
