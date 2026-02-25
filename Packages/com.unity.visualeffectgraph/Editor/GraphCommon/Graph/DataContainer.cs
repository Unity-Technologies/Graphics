
using System;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// An Id associated to a DataContainer in a graph.
    /// </summary>
    /*public*/ readonly struct DataContainerId : IEquatable<DataContainerId>
    {
        /// <summary>
        /// Defines an invalid DataContainerId.
        /// </summary>
        public static readonly DataContainerId Invalid = new DataContainerId(-1);

        /// <summary>
        /// Implicitly converts a <see cref="DataContainerId"/> to a <see cref="GraphDataId"/>.
        /// </summary>
        /// <param name="id">The data container ID to convert.</param>
        /// <returns>A new graph data ID with the same index value.</returns>
        public static implicit operator GraphDataId(DataContainerId id) => new GraphDataId(id.Index);

        /// <summary>
        /// Implicitly converts a <see cref="GraphDataId"/> to a <see cref="DataContainerId"/>.
        /// </summary>
        /// <param name="id">The graph data ID to convert.</param>
        /// <returns>A new data container ID with the same index value.</returns>
        public static implicit operator DataContainerId(GraphDataId id) => new DataContainerId(id.Index);

        /// <summary>
        /// Gets the wrapped int index.
        /// </summary>
        public int Index { get; }
        /// <summary>
        /// Returns true if this Id is valid, false otherwise.
        /// </summary>
        public bool IsValid => Index != Invalid.Index;

        internal DataContainerId(int index)
        {
            Index = index;
        }

        /// <inheritdoc cref="IEquatable"/>
        public bool Equals(DataContainerId other) => Index == other.Index;
        /// <inheritdoc cref="ValueType"/>
        public override int GetHashCode() => Index;
        /// <inheritdoc cref="ValueType"/>
        public override string ToString() => Index.ToString();
    }

    readonly struct DataContainerInfo
    {
        public DataContainerInfo(DataContainerId id, string name, DataViewId rootDataViewId)
        {
            Id = id;
            Name = name;
            RootDataViewId = rootDataViewId;
        }

        public DataContainerId Id { get; }
        public string Name { get; }
        public DataViewId RootDataViewId { get; }
    }

    /// <summary>
    /// Represents a data container
    /// </summary>
    /*public*/ readonly struct DataContainer
    {
        readonly Handle<IReadOnlyGraph> m_Graph;
        readonly DataContainerInfo m_Info;

        /// <summary>
        /// Gets the unique identifier for this <see cref="DataContainer"/>.
        /// Returns <see cref="DataContainerId.Invalid"/> if the graph is not valid.
        /// </summary>
        public DataContainerId Id => m_Graph.Valid ? m_Info.Id : DataContainerId.Invalid;

        /// <summary>
        /// Gets the name of this container. Returns null if the graph is not valid.
        /// </summary>
        public string Name => m_Graph.Valid ? m_Info.Name : null;

        /// <summary>
        /// Gets the root DataView of this container.
        /// </summary>
        public DataView RootDataView => m_Graph.Ref.DataViews[m_Info.RootDataViewId];

        internal DataContainer(IReadOnlyGraph graph, DataContainerInfo info)
        {
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
    /*public*/ readonly struct DataContainerEnumerable<T> : IIndexable<int, DataContainer>, ICountable where T : IIndexable<int, DataContainerId>, ICountable
    {
        readonly IIndexable<DataContainerId, DataContainer> m_Provider;
        readonly T m_IdSource;

        /// <summary>
        /// Gets the number of items in the enumerable, sourced from the number of IDs in the <typeparamref name="T"/> source.
        /// </summary>
        public int Count => m_IdSource.Count;

        /// <summary>
        /// Gets the <see cref="DataContainer"/> at the specified index, resolving its associated ID.
        /// </summary>
        /// <param name="index">The zero-based index of the <see cref="DataContainer"/>.</param>
        /// <value>The <see cref="DataContainer"/> associated with the ID at the specified index.</value>
        public DataContainer this[int index] => m_Provider[m_IdSource[index]];

        /// <summary>
        /// Initializes a new instance of the <see cref="DataContainerEnumerable{T}"/> struct.
        /// Initializes a new instance of the <see cref="DataContainerEnumerable{T}"/> struct.
        /// </summary>
        /// <param name="provider">The provider that resolves <see cref="DataContainerId"/> to <see cref="DataContainer"/> instances.</param>
        /// <param name="idSource">The source of <see cref="DataContainerId"/> identifiers.</param>
        public DataContainerEnumerable(IIndexable<DataContainerId, DataContainer> provider, T idSource)
        {
            m_Provider = provider;
            m_IdSource = idSource;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="DataContainerEnumerable{T}"/>.
        /// </summary>
        /// <returns>A <see cref="LinearEnumerator{TEnumerable, T}"/> for iterating through the <see cref="DataContainer"/> instances.</returns>
        public LinearEnumerator<DataContainerEnumerable<T>, DataContainer> GetEnumerator() => new(this);
    }
}
