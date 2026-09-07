
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
        readonly IReadOnlyGraph m_Graph;
        readonly DataContainerInfo m_Info;

        DataContainerInfo Info { get { CheckValid(); return m_Info; } }
        IReadOnlyGraph Graph { get { CheckValid(); return m_Graph; } }

        /// <summary>
        /// Gets a value indicating whether this data container still refers to a live entry in the graph.
        /// </summary>
        public bool IsValid => m_Graph != null && m_Graph.IsValid(m_Info.Id);

        /// <summary>
        /// Gets the unique identifier for this <see cref="DataContainer"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data container has been removed from the graph.</exception>
        public DataContainerId Id => Info.Id;

        /// <summary>
        /// Gets the name of this container.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data container has been removed from the graph.</exception>
        public string Name => Info.Name;

        /// <summary>
        /// Gets the name of this container as a C# identifier.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data container has been removed from the graph.</exception>
        public string IdentifierName => Info.Name?.Replace(' ', '_');

        void CheckValid() { if (!IsValid) throw new InvalidOperationException($"DataContainer {m_Info.Id} no longer exists in the graph."); }

        /// <summary>
        /// Gets the root DataView of this container.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data container has been removed from the graph.</exception>
        public DataView RootDataView => Graph.DataViews[Info.RootDataViewId];

        internal DataContainer(IReadOnlyGraph graph, DataContainerInfo info)
        {
            m_Graph = graph;
            m_Info = info;
        }
    }

    /// <summary>
    /// Represents an enumerable collection of <see cref="DataContainer"/> instances. Wraps a
    /// <see cref="SubEnumerable{T}"/> of <see cref="DataContainerId"/> and projects each id to a
    /// <see cref="DataContainer"/> via a provider. Iteration via <c>foreach</c> composes the
    /// inner <see cref="VersionedSublistEnumerator{T}"/>, so concurrent modification of the
    /// backing sublist is detected and throws.
    /// </summary>
    /*public*/ readonly struct DataContainerEnumerable : IIndexable<int, DataContainer>, ICountable
    {
        readonly IIndexable<DataContainerId, DataContainer> m_Provider;
        readonly SubEnumerable<DataContainerId>             m_IdSource;

        public int Count => m_IdSource.Count;

        /// <remarks>
        /// Indexed access bypasses the version check (matches the BCL contract). Use
        /// <c>foreach</c> for the version-checked path.
        /// </remarks>
        public DataContainer this[int index] => m_Provider[m_IdSource[index]];

        /// <summary>
        /// Gets the id at the specified index, without materializing a <see cref="DataContainer"/>.
        /// Used by <see cref="GraphSnapshots"/>.
        /// </summary>
        internal DataContainerId GetId(int index) => m_IdSource[index];

        public DataContainerEnumerable(IIndexable<DataContainerId, DataContainer> provider, SubEnumerable<DataContainerId> idSource)
        {
            m_Provider = provider;
            m_IdSource = idSource;
        }

        public ResolvingEnumerator<DataContainerId, DataContainer, VersionedSublistEnumerator<DataContainerId>> GetEnumerator() =>
            new(m_Provider, m_IdSource.GetEnumerator());
    }
}
