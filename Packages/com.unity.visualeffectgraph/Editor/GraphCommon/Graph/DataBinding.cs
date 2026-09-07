
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
        public DataBindingInfo(DataBindingId id, DataNodeId dataNodeId, DataViewId dataViewId, IDataKey bindingDataKey, BindingUsage usage)
        {
            Id = id;
            DataNodeId = dataNodeId;
            DataViewId = dataViewId;
            BindingDataKey = bindingDataKey;
            Usage = usage;
        }

        public DataBindingId Id { get; }
        public DataNodeId DataNodeId { get; }
        public DataViewId DataViewId { get; }
        public IDataKey BindingDataKey { get; }
        public BindingUsage Usage { get; }
    }

    /// <summary>
    /// Represents a data binding
    /// </summary>
    /*public*/ readonly struct DataBinding
    {
        readonly IReadOnlyGraph m_Graph;
        readonly DataBindingInfo m_Info;

        DataBindingInfo Info { get { CheckValid(); return m_Info; } }
        IReadOnlyGraph Graph { get { CheckValid(); return m_Graph; } }

        /// <summary>
        /// Gets a value indicating whether this data binding still refers to a live entry in the graph.
        /// </summary>
        public bool IsValid => m_Graph != null && m_Graph.IsValid(m_Info.Id);

        /// <summary>
        /// Gets the unique identifier for this <see cref="DataBinding"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data binding has been removed from the graph.</exception>
        public DataBindingId Id => Info.Id;

        void CheckValid() { if (!IsValid) throw new InvalidOperationException($"DataBinding {m_Info.Id} no longer exists in the graph."); }

        /// <summary>
        /// Gets the TaskNode that owns this binding.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data binding has been removed from the graph.</exception>
        public TaskNode TaskNode => Graph.DataNodes[Info.DataNodeId].TaskNode;

        /// <summary>
        /// Gets the DataView associated with this binding.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data binding has been removed from the graph.</exception>
        public DataView DataView => Graph.DataViews[Info.DataViewId];

        /// <summary>
        /// Gets the BindingDataKey used in this binding.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data binding has been removed from the graph.</exception>
        public IDataKey BindingDataKey => Info.BindingDataKey;

        /// <summary>
        /// Gets the binding usage.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data binding has been removed from the graph.</exception>
        public BindingUsage Usage => Info.Usage;

        /// <summary>
        /// Gets the DataNode associated with this binding.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data binding has been removed from the graph.</exception>
        public DataNode DataNode => Graph.DataNodes[Info.DataNodeId];

        internal DataBinding(IReadOnlyGraph graph, DataBindingInfo info)
        {
            m_Graph = graph;
            m_Info = info;
        }
    }

    /// <summary>
    /// Represents an enumerable collection of <see cref="DataBinding"/> instances. Wraps a
    /// <see cref="SubEnumerable{T}"/> of <see cref="DataBindingId"/> and projects each id to a
    /// <see cref="DataBinding"/> via a provider. Iteration via <c>foreach</c> composes the inner
    /// <see cref="VersionedSublistEnumerator{T}"/>, so concurrent modification of the backing
    /// sublist is detected and throws.
    /// </summary>
    /*public*/ readonly struct DataBindingEnumerable : IIndexable<int, DataBinding>, ICountable
    {
        readonly IIndexable<DataBindingId, DataBinding> m_Provider;
        readonly SubEnumerable<DataBindingId>           m_IdSource;

        public int Count => m_IdSource.Count;

        /// <remarks>
        /// Indexed access bypasses the version check (matches the BCL contract). Use
        /// <c>foreach</c> for the version-checked path.
        /// </remarks>
        public DataBinding this[int index] => m_Provider[m_IdSource[index]];

        /// <summary>
        /// Gets the id at the specified index, without materializing a <see cref="DataBinding"/>.
        /// Used by <see cref="GraphSnapshots"/>.
        /// </summary>
        internal DataBindingId GetId(int index) => m_IdSource[index];

        public DataBinding? this[IDataKey bindingDataKey]
        {
            get
            {
                foreach (DataBinding dataBinding in this)
                {
                    if (dataBinding.BindingDataKey.Equals(bindingDataKey))
                    {
                        return dataBinding;
                    }
                }
                return null;
            }
        }

        // DataBindings may contain a DataViewId more than once, that's why I prefer a method over an indexer. Do we need a find all?
        public DataBinding? FindDataView(DataViewId dataViewId)
        {
            foreach (DataBinding dataBinding in this)
            {
                if (dataBinding.DataView.Id.Equals(dataViewId))
                {
                    return dataBinding;
                }
            }
            return null;
        }

        public DataBindingEnumerable(IIndexable<DataBindingId, DataBinding> provider, SubEnumerable<DataBindingId> idSource)
        {
            m_Provider = provider;
            m_IdSource = idSource;
        }

        public ResolvingEnumerator<DataBindingId, DataBinding, VersionedSublistEnumerator<DataBindingId>> GetEnumerator() =>
            new(m_Provider, m_IdSource.GetEnumerator());
    }
}
