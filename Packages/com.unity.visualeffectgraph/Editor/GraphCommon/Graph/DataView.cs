
using System;
using System.Collections;
using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// An Id associated to a DataView in a graph.
    /// </summary>
    /*public*/ readonly struct DataViewId : IEquatable<DataViewId>
    {
        /// <summary>
        /// Defines an invalid DataViewId.
        /// </summary>
        public static readonly DataViewId Invalid = new DataViewId(-1);

        /// <summary>
        /// Implicitly converts a <see cref="DataViewId"/> to a <see cref="GraphDataId"/>.
        /// </summary>
        /// <param name="id">The data view ID to convert.</param>
        /// <returns>A new graph data ID with the same index value.</returns>
        public static implicit operator GraphDataId(DataViewId id) => new GraphDataId(id.Index);
        /// <summary>
        /// Implicitly converts a <see cref="GraphDataId"/> to a <see cref="DataViewId"/>.
        /// </summary>
        /// <param name="id">The graph data ID to convert.</param>
        /// <returns>A new data view ID with the same index value.</returns>
        public static implicit operator DataViewId(GraphDataId id) => new DataViewId(id.Index);

        /// <summary>
        /// Gets the wrapped int index.
        /// </summary>
        public int Index { get; }
        /// <summary>
        /// Returns true if this Id is valid, false otherwise.
        /// </summary>
        public bool IsValid => Index != Invalid.Index;

        internal DataViewId(int index)
        {
            Index = index;
        }

        /// <inheritdoc cref="IEquatable"/>
        public bool Equals(DataViewId other) => Index == other.Index;
        /// <inheritdoc cref="ValueType"/>
        public override int GetHashCode() => Index;
        /// <inheritdoc cref="ValueType"/>
        public override string ToString() => Index.ToString();
    }

    readonly struct DataViewInfo
    {
        public DataViewInfo(DataViewId id, IDataDescription dataDescription)
        {
            Id = id;
            DataDescription = dataDescription;
            ParentDataViewId = DataViewId.Invalid;
            SubDataKey = null;
        }

        public DataViewInfo(DataViewId id, IDataDescription dataDescription, DataViewId parentDataViewId, IDataKey subDataKey)
        {
            Id = id;
            DataDescription = dataDescription;
            ParentDataViewId = parentDataViewId;
            SubDataKey = subDataKey;
        }

        public DataViewId Id { get; }
        public IDataDescription DataDescription { get; }
        public DataViewId ParentDataViewId { get; }
        public IDataKey SubDataKey { get; }
    }

    /// <summary>
    /// Represents a data view in a hierarchical structure.
    /// </summary>
    /*public*/ readonly struct DataView
    {
        readonly IIndexable<MultiTreeNode<DataViewId>, DataView> m_Source;
        readonly MultiTreeNode<DataViewId> m_Node;

        readonly Handle<IReadOnlyGraph> m_Graph;
        readonly DataViewInfo m_Info;

        /// <summary>
        /// Gets the unique identifier for this <see cref="DataView"/>.
        /// Returns <see cref="DataViewId.Invalid"/> if the graph is not valid.
        /// </summary>
        public DataViewId Id => m_Graph.Valid ? m_Info.Id : DataViewId.Invalid;

        /// <summary>
        /// Gets the parent <see cref="DataView"/> of this view, if it exists.
        /// Returns <c>null</c> if the graph is not valid or if there is no valid parent.
        /// </summary>
        public DataView? Parent
        {
            get
            {
                var parent = m_Graph.Valid ? m_Node.Parent : null;
                return parent.HasValue ? m_Source[parent.Value] : null;
            }
        }

        /// <summary>
        /// Gets the root <see cref="DataView"/> of the current data view's hierarchy.
        /// </summary>
        public DataView Root => m_Source[m_Node.Root];

        /// <summary>
        /// Gets the IDataDescription associated with this data view.
        /// Returns null if the graph is not valid.
        /// </summary>
        public IDataDescription DataDescription => m_Graph.Valid ? m_Info.DataDescription : null;

        /// <summary>
        /// Gets the sub-data key associated with this data view.
        /// Returns null if the graph is not valid.
        /// </summary>
        public IDataKey SubDataKey => m_Graph.Valid ? m_Info.SubDataKey : null;

        /// <summary>
        /// Gets an enumerable collection of children <see cref="DataView"/> instances of this view.
        /// </summary>
        public DataViewChildren Children => m_Graph.Valid ? new(m_Source, m_Node.Children) : new();

        /// <summary>
        /// Enumerates all the data views in this data view tree.
        /// </summary>
        public DataViewFlatTreeEnumerable Flat => new(this);

        /// <summary>
        /// Gets the DataContainer where this DataView is stored.
        /// </summary>
        public DataContainer DataContainer => m_Graph.Ref.GetDataContainer(Id);

        /// <summary>
        /// Tries to find a child data view with the specified data key.
        /// </summary>
        /// <param name="subdataKey">The data key used by the child data view.</param>
        /// <param name="subDataView">The child data view, if found. Invalid data view otherwise.</param>
        /// <returns>True if the child data view was found, false otherwise.</returns>
        public bool FindSubData(IDataKey subdataKey, out DataView subDataView)
        {
            foreach (var child in Children)
            {
                if (child.SubDataKey.Equals(subdataKey))
                {
                    subDataView = child;
                    return true;
                }
            }
            subDataView = new DataView();
            return false;
        }

        /// <summary>
        /// Tries to find a child data view with the specified data path.
        /// </summary>
        /// <param name="subdataPath">The data path used by the child data view.</param>
        /// <param name="subDataView">The child data view, if found. Invalid data view otherwise.</param>
        /// <returns>True if the child data view was found, false otherwise.</returns>
        public bool FindSubData(DataPath subdataPath, out DataView subDataView)
        {
            subDataView = this;
            foreach (var key in subdataPath.PathSequence)
            {
                if (key == null) continue; // TODO: Hack to make it work, investigate DataPath class
                if (!subDataView.FindSubData(key, out subDataView))
                {
                    return false;
                }
            }
            return true;
        }

        internal DataView(IIndexable<MultiTreeNode<DataViewId>, DataView> source, MultiTreeNode<DataViewId> node, IReadOnlyGraph graph, DataViewInfo info)
        {
            m_Source = source;
            m_Node = node;
            m_Graph = new(graph);
            m_Info = info;
        }
    }

    /// <summary>
    /// Represents the children of a <see cref="DataView"/> in a hierarchical structure.
    /// Provides indexed access to the child views and implements enumeration functionality.
    /// </summary>
    /*public*/ readonly struct DataViewChildren : IIndexable<int, DataView>, ICountable
    {
        readonly IIndexable<MultiTreeNode<DataViewId>, DataView> m_Source;
        readonly MultiTreeNodeEnumerable<SubEnumerable<int>, DataViewId> m_Children;

        /// <summary>
        /// Gets the number of children for the parent <see cref="DataView"/>.
        /// </summary>
        public int Count => m_Children.Count;

        /// <summary>
        /// Gets the child <see cref="DataView"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the child view.</param>
        /// <value>The <see cref="DataView"/> at the specified index.</value>
        public DataView this[int index] => m_Source[m_Children[index]];

        /// <summary>
        /// Initializes a new instance of the <see cref="DataViewChildren"/> struct.
        /// </summary>
        /// <param name="source">The provider mapping <see cref="DataViewId"/> to <see cref="DataView"/> instances.</param>
        /// <param name="children">The enumerable collection of child nodes.</param>
        public DataViewChildren(IIndexable<MultiTreeNode<DataViewId>, DataView> source, MultiTreeNodeEnumerable<SubEnumerable<int>, DataViewId> children)
        {
            m_Source = source;
            m_Children = children;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="DataViewChildren"/>.
        /// </summary>
        /// <returns>A <see cref="LinearEnumerator{TEnumerable, T}"/> to iterate over the <see cref="DataView"/> children.</returns>
        public LinearEnumerator<DataViewChildren, DataView> GetEnumerator() => new(this);
    }

    /// <summary>
    /// Represents an enumerable collection of <see cref="DataView"/> instances, based on an indexed source of IDs.
    /// Combines ID enumeration with the ability to resolve and access <see cref="DataView"/> objects.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the indexed source providing the IDs.
    /// Must implement both <see cref="IIndexable{TIndex, TValue}"/> and <see cref="ICountable"/>.
    /// </typeparam>
    /*public*/ readonly struct DataViewEnumerable<T> : IIndexable<int, DataView>, ICountable where T : IIndexable<int, DataViewId>, ICountable
    {
        readonly IIndexable<DataViewId, DataView> m_Provider;
        readonly T m_IdSource;

        /// <summary>
        /// Gets the number of items in the enumerable, sourced from the number of IDs in the <typeparamref name="T"/> source.
        /// </summary>
        public int Count => m_IdSource.Count;

        /// <summary>
        /// Gets the <see cref="DataView"/> at the specified index, resolving its associated ID.
        /// </summary>
        /// <param name="index">The zero-based index of the <see cref="DataView"/>.</param>
        /// <value>The <see cref="DataView"/> associated with the ID at the specified index.</value>
        public DataView this[int index] => m_Provider[m_IdSource[index]];

        /// <summary>
        /// Initializes a new instance of the <see cref="DataViewEnumerable{T}"/> struct.
        /// </summary>
        /// <param name="provider">The provider that resolves <see cref="DataViewId"/> to <see cref="DataView"/> instances.</param>
        /// <param name="idSource">The source of <see cref="DataViewId"/> identifiers.</param>
        public DataViewEnumerable(IIndexable<DataViewId, DataView> provider, T idSource)
        {
            m_Provider = provider;
            m_IdSource = idSource;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="DataViewEnumerable{T}"/>.
        /// </summary>
        /// <returns>A <see cref="LinearEnumerator{TEnumerable, T}"/> for iterating through the <see cref="DataView"/> instances.</returns>
        public LinearEnumerator<DataViewEnumerable<T>, DataView> GetEnumerator() => new(this);
    }

    /// <summary>
    /// Flat representation of a <see cref="DataView"/> tree.
    /// </summary>
    /*public*/ readonly struct DataViewFlatTreeEnumerable : IEnumerable<DataView>
    {
        readonly DataView m_RootDataView;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataViewFlatTreeEnumerable"/> struct.
        /// </summary>
        /// <param name="rootDataView">The root of the <see cref="DataView"/> tree to be enumerated.</param>
        public DataViewFlatTreeEnumerable(DataView rootDataView)
        {
            m_RootDataView = rootDataView;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="DataViewFlatTreeEnumerable"/>.
        /// </summary>
        /// <returns>A <see cref="DataViewFlatTreeEnumerator"/> to iterate over the <see cref="DataView"/> children.</returns>
        public DataViewFlatTreeEnumerator GetEnumerator() => new(m_RootDataView);
        IEnumerator<DataView> IEnumerable<DataView>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Enumerator that iterates over all elements of a <see cref="DataView"/> tree.
    /// </summary>
    /*public*/ struct DataViewFlatTreeEnumerator : IEnumerator<DataView>
    {
        readonly DataView m_RootDataView;
        Stack<DataView> m_Stack;
        DataView m_Current;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataViewFlatTreeEnumerator"/> struct.
        /// </summary>
        /// <param name="rootDataView">The root of the <see cref="DataView"/> tree to be enumerated.</param>
        public DataViewFlatTreeEnumerator(DataView rootDataView)
        {
            m_Stack = new();
            m_RootDataView = rootDataView;
            m_Current = new DataView();
            if(m_RootDataView.Id.IsValid)
                m_Stack.Push(m_RootDataView);
        }

        /// <summary>
        /// Gets the current value in the enumeration.
        /// </summary>
        public DataView Current
        {
            get
            {
                return m_Current.Id.IsValid ? m_Current : default;
            }
        }
        object IEnumerator.Current => Current;

        /// <summary>
        /// Disposes the resources used by the enumerator.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Moves to the next item in the enumeration.
        /// </summary>
        /// <returns><see langword="true"/> if there are more items; otherwise, <see langword="false"/>.</returns>
        public bool MoveNext()
        {
            while (m_Stack.Count > 0)
            {
                m_Current = m_Stack.Pop();
                var connections = m_Current.Children;
                for (int i = 0; i < connections.Count; i++)
                {
                    var childrenDataView = connections[connections.Count - 1 - i];
                    m_Stack.Push(childrenDataView);
                }

                return true;
            }
            m_Current = default;
            return false;
        }

        /// <summary>
        /// Resets the enumeration.
        /// </summary>
        public void Reset()
        {
            m_Stack.Clear();
        }
    }
}
