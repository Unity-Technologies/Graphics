
using System;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// An Id associated to a DataBinding in a graph.
    /// </summary>
    /*public*/ readonly struct DataBindingId : IEquatable<DataBindingId>
    {
        /// <summary>
        /// Defines an invalid DataBindingId.
        /// </summary>
        public static readonly DataBindingId Invalid = new DataBindingId(-1);

        /// <summary>
        /// Implicitly converts a <see cref="DataBindingId"/> to a <see cref="GraphDataId"/>.
        /// </summary>
        /// <param name="id">The data binding ID to convert.</param>
        /// <returns>A new graph data ID with the same index value.</returns>
        public static implicit operator GraphDataId(DataBindingId id) => new GraphDataId(id.Index);
        /// <summary>
        /// Implicitly converts a <see cref="GraphDataId"/> to a <see cref="DataBindingId"/>.
        /// </summary>
        /// <param name="id">The graph data ID to convert.</param>
        /// <returns>A new data binding ID with the same index value.</returns>
        public static implicit operator DataBindingId(GraphDataId id) => new DataBindingId(id.Index);

        /// <summary>
        /// Gets the wrapped int index.
        /// </summary>
        public int Index { get; }
        /// <summary>
        /// Returns true if this Id is valid, false otherwise.
        /// </summary>
        public bool IsValid => Index != Invalid.Index;

        internal DataBindingId(int index)
        {
            Index = index;
        }

        /// <inheritdoc cref="IEquatable"/>
        public bool Equals(DataBindingId other) => Index == other.Index;
        /// <inheritdoc cref="ValueType"/>
        public override int GetHashCode() => Index;
        /// <inheritdoc cref="ValueType"/>
        public override string ToString() => Index.ToString();
    }

    readonly struct DataBindingInfo
    {
        public DataBindingInfo(DataBindingId id, TaskNodeId taskNodeId, DataViewId dataViewId, IDataKey bindingDataKey, BindingUsage usage)
        {
            Id = id;
            TaskNodeId = taskNodeId;
            DataViewId = dataViewId;
            BindingDataKey = bindingDataKey;
            Usage = usage;
        }

        public DataBindingId Id { get; }
        public TaskNodeId TaskNodeId { get; }
        public DataViewId DataViewId { get; }
        public IDataKey BindingDataKey { get; }
        public BindingUsage Usage { get; }
    }

    /// <summary>
    /// Represents a data binding
    /// </summary>
    /*public*/ readonly struct DataBinding
    {
        readonly IIndexable<DataBindingId, DataBinding> m_Source;

        readonly Handle<IReadOnlyGraph> m_Graph;
        readonly DataBindingInfo m_Info;

        /// <summary>
        /// Gets the unique identifier for this <see cref="DataBinding"/>.
        /// Returns <see cref="DataBindingId.Invalid"/> if the graph is not valid.
        /// </summary>
        public DataBindingId Id => m_Graph.Valid ? m_Info.Id : DataBindingId.Invalid;

        /// <summary>
        /// Gets the TaskNode that owns this binding.
        /// </summary>
        public TaskNode TaskNode => m_Graph.Ref.TaskNodes[m_Info.TaskNodeId];

        /// <summary>
        /// Gets the DataView associated with this binding.
        /// </summary>
        public DataView DataView => m_Graph.Ref.DataViews[m_Info.DataViewId];

        /// <summary>
        /// Gets the BindingDataKey used in this binding.
        /// Returns null if the graph is not valid.
        /// </summary>
        public IDataKey BindingDataKey => m_Graph.Valid ? m_Info.BindingDataKey : null;

        /// <summary>
        /// Gets the BindingDataKey used in this binding.
        /// Returns BindingUsage.Unknown if the graph is not valid.
        /// </summary>
        public BindingUsage Usage => m_Graph.Valid ? m_Info.Usage : BindingUsage.Unknown;

        /// <summary>
        /// Gets the DataNode associated with this binding.
        /// </summary>
        public DataNode DataNode => m_Graph.Ref.GetDataNode(m_Info.Id);

        internal DataBinding(IIndexable<DataBindingId, DataBinding> source, IReadOnlyGraph graph, DataBindingInfo info)
        {
            m_Source = source;
            m_Graph = new(graph);
            m_Info = info;
        }
    }

    /// <summary>
    /// Represents an enumerable collection of <see cref="DataView"/> instances, based on an indexed source of IDs.
    /// Combines ID enumeration with the ability to resolve and access <see cref="DataView"/> objects.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the indexed source providing the IDs.
    /// Must implement both <see cref="IIndexable{TIndex, TValue}"/> and <see cref="ICountable"/>.
    /// </typeparam>
    /*public*/ readonly struct DataBindingEnumerable<T> : IIndexable<int, DataBinding>, ICountable where T : IIndexable<int, DataBindingId>, ICountable
    {
        readonly IIndexable<DataBindingId, DataBinding> m_Provider;
        readonly T m_IdSource;

        /// <summary>
        /// Gets the number of items in the enumerable, sourced from the number of IDs in the <typeparamref name="T"/> source.
        /// </summary>
        public int Count => m_IdSource.Count;

        /// <summary>
        /// Gets the <see cref="DataBinding"/> at the specified index, resolving its associated ID.
        /// </summary>
        /// <param name="index">The zero-based index of the <see cref="DataBinding"/>.</param>
        /// <value>The <see cref="DataBinding"/> associated with the ID at the specified index.</value>
        public DataBinding this[int index] => m_Provider[m_IdSource[index]];

        /// <summary>
        /// Initializes a new instance of the <see cref="DataViewEnumerable{T}"/> struct.
        /// </summary>
        /// <param name="provider">The provider that resolves <see cref="DataBindingId"/> to <see cref="DataBinding"/> instances.</param>
        /// <param name="idSource">The source of <see cref="DataBindingId"/> identifiers.</param>
        public DataBindingEnumerable(IIndexable<DataBindingId, DataBinding> provider, T idSource)
        {
            m_Provider = provider;
            m_IdSource = idSource;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="DataBindingEnumerable{T}"/>.
        /// </summary>
        /// <returns>A <see cref="LinearEnumerator{TEnumerable, T}"/> for iterating through the <see cref="DataBinding"/> instances.</returns>
        public LinearEnumerator<DataBindingEnumerable<T>, DataBinding> GetEnumerator() => new(this);
    }
}
