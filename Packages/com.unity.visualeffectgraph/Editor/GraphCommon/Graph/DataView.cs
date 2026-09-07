using System;
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

        readonly IReadOnlyGraph m_Graph;
        readonly DataViewInfo m_Info;

        private DataViewInfo Info { get { CheckValid();  return m_Info; } }
        IReadOnlyGraph Graph { get { CheckValid(); return m_Graph; } }
        private MultiTreeNode<DataViewId> Node { get { CheckValid(); return m_Node; } }
        private IIndexable<MultiTreeNode<DataViewId>, DataView> SourceProvider { get { CheckValid(); return m_Source; } }

        /// <summary>
        /// Gets a value indicating whether this data view still refers to a live entry in the graph.
        /// </summary>
        public bool Valid => m_Graph != null && m_Graph.IsValid(m_Info.Id);

        void CheckValid() { if (!Valid) throw new InvalidOperationException($"DataView {m_Info.Id} no longer exists in the graph."); }

        /// <summary>
        /// Gets the unique identifier for this <see cref="DataView"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data view has been removed from the graph.</exception>
        public DataViewId Id => Info.Id;

        /// <summary>
        /// Gets the parent <see cref="DataView"/> of this view, if it exists.
        /// Returns <c>null</c> if there is no parent.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data view has been removed from the graph.</exception>
        public DataView? Parent
        {
            get
            {
                var parent = Node.Parent;
                return parent.HasValue ? SourceProvider[parent.Value] : null;
            }
        }

        /// <summary>
        /// Gets the root <see cref="DataView"/> of the current data view's hierarchy.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data view has been removed from the graph.</exception>
        public DataView Root => SourceProvider[Node.Root];

        public bool IsRoot => !Parent.HasValue;

        /// <summary>
        /// Gets the IDataDescription associated with this data view.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data view has been removed from the graph.</exception>
        public IDataDescription DataDescription => Info.DataDescription;

        /// <summary>
        /// Gets the sub-data key associated with this data view.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data view has been removed from the graph.</exception>
        public IDataKey SubDataKey => Info.SubDataKey;

        /// <summary>
        /// Gets an enumerable collection of children <see cref="DataView"/> instances of this view.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data view has been removed from the graph.</exception>
        public DataViewChildren Children => new(SourceProvider, Node.Children);

        /// <summary>
        /// Enumerates all the data views in this data view tree.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data view has been removed from the graph.</exception>
        public DataViewFlatTreeEnumerable Flat => new(Node, SourceProvider);

        /// <summary>
        /// Gets the DataContainer where this DataView is stored.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the data view has been removed from the graph.</exception>
        public DataContainer DataContainer => Graph.GetDataContainer(Info.Id);

        /// <summary>
        /// Tries to find a child data view with the specified data key.
        /// </summary>
        /// <param name="subdataKey">The data key used by the child data view.</param>
        /// <param name="subDataView">The child data view, if found. Invalid data view otherwise.</param>
        /// <returns>True if the child data view was found, false otherwise.</returns>
        public bool FindSubData(IDataKey subdataKey, out DataView subDataView)
        {
            if (Graph.TryGetSubView(Id, subdataKey, out var subDataViewId))
            {
                subDataView = Graph.DataViews[subDataViewId];
                return true;
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
            foreach (var key in subdataPath)
            {
                if (key == null) continue; // TODO: Hack to make it work, investigate DataPath class
                if (!subDataView.FindSubData(key, out subDataView))
                {
                    return false;
                }
            }
            return true;
        }

        public bool ContainsSubData(DataViewId dataViewId)
        {
            if (!Graph.IsValid(dataViewId))
                return false;

            for (DataView? current = Graph.DataViews[dataViewId]; current.HasValue; current = current.Value.Parent)
            {
                if (current.Value.Id.Equals(Id))
                    return true;
            }

            return false;
        }

        internal DataView(IIndexable<MultiTreeNode<DataViewId>, DataView> source, MultiTreeNode<DataViewId> node, IReadOnlyGraph graph, DataViewInfo info)
        {
            m_Source = source;
            m_Node = node;
            m_Graph = graph;
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
        readonly MultiTreeNodeChildren<DataViewId> m_Children;

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
        public DataViewChildren(IIndexable<MultiTreeNode<DataViewId>, DataView> source, MultiTreeNodeChildren<DataViewId> children)
        {
            m_Source = source;
            m_Children = children;
        }

        /// <summary>
        /// Returns a version-checked enumerator over the child <see cref="DataView"/> instances.
        /// Composes the child sublist's <see cref="VersionedSublistEnumerator{T}"/> (slot →
        /// <see cref="MultiTreeNode{T}"/>) with the node → <see cref="DataView"/> projection, so
        /// concurrent modification of the parent's child list is detected and throws.
        /// </summary>
        public ResolvingEnumerator<MultiTreeNode<DataViewId>, DataView,
            ResolvingEnumerator<int, MultiTreeNode<DataViewId>, VersionedSublistEnumerator<int>>> GetEnumerator() =>
            new(m_Source, m_Children.GetEnumerator());
    }

    /// <summary>
    /// Represents an enumerable collection of <see cref="DataView"/> instances. Wraps a
    /// <see cref="SubEnumerable{T}"/> of <see cref="DataViewId"/> and projects each id to a
    /// <see cref="DataView"/> via a provider. Iteration via <c>foreach</c> composes the inner
    /// <see cref="VersionedSublistEnumerator{T}"/>, so concurrent modification of the backing
    /// sublist is detected and throws.
    /// </summary>
    /*public*/ readonly struct DataViewEnumerable : IIndexable<int, DataView>, ICountable
    {
        readonly IIndexable<DataViewId, DataView> m_Provider;
        readonly SubEnumerable<DataViewId>        m_IdSource;

        public int Count => m_IdSource.Count;

        /// <remarks>
        /// Indexed access bypasses the version check (matches the BCL contract). Use
        /// <c>foreach</c> for the version-checked path.
        /// </remarks>
        public DataView this[int index] => m_Provider[m_IdSource[index]];

        /// <summary>
        /// Gets the id at the specified index, without materializing a <see cref="DataView"/>.
        /// Used by <see cref="GraphSnapshots"/>.
        /// </summary>
        internal DataViewId GetId(int index) => m_IdSource[index];

        public DataViewEnumerable(IIndexable<DataViewId, DataView> provider, SubEnumerable<DataViewId> idSource)
        {
            m_Provider = provider;
            m_IdSource = idSource;
        }

        public ResolvingEnumerator<DataViewId, DataView, VersionedSublistEnumerator<DataViewId>> GetEnumerator() =>
            new(m_Provider, m_IdSource.GetEnumerator());
    }

    /// <summary>
    /// Flat representation of a <see cref="DataView"/> tree.
    /// </summary>
    /*public*/ readonly struct DataViewFlatTreeEnumerable
    {
        readonly MultiTreeNode<DataViewId> m_RootNode;
        readonly IIndexable<MultiTreeNode<DataViewId>, DataView> m_Source;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataViewFlatTreeEnumerable"/> struct.
        /// </summary>
        /// <param name="rootNode">The root node of the <see cref="DataView"/> tree to be enumerated.</param>
        /// <param name="source">The provider that resolves tree nodes to <see cref="DataView"/> instances.</param>
        public DataViewFlatTreeEnumerable(MultiTreeNode<DataViewId> rootNode, IIndexable<MultiTreeNode<DataViewId>, DataView> source)
        {
            m_RootNode = rootNode;
            m_Source = source;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="DataViewFlatTreeEnumerable"/>.
        /// </summary>
        /// <returns>A <see cref="DataViewFlatTreeEnumerator"/> to iterate over the <see cref="DataView"/> children.</returns>
        public DataViewFlatTreeEnumerator GetEnumerator() => new(m_RootNode, m_Source);
    }

    /// <summary>
    /// Enumerator that iterates over all elements of a <see cref="DataView"/> tree.
    /// </summary>
    /*public*/ struct DataViewFlatTreeEnumerator
    {
        readonly IMultiTree<DataViewId> m_Tree;
        readonly IIndexable<MultiTreeNode<DataViewId>, DataView> m_Source;
        Stack<int> m_Stack;
        DataView m_Current;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataViewFlatTreeEnumerator"/> struct.
        /// </summary>
        /// <param name="rootNode">The root node of the <see cref="DataView"/> tree to be enumerated.</param>
        /// <param name="source">The provider that resolves tree nodes to <see cref="DataView"/> instances.</param>
        public DataViewFlatTreeEnumerator(MultiTreeNode<DataViewId> rootNode, IIndexable<MultiTreeNode<DataViewId>, DataView> source)
        {
            m_Stack = new Stack<int>();
            m_Current = default;
            m_Tree = rootNode.Tree;
            m_Source = source;
            if (m_Tree != null)
                m_Stack.Push(rootNode.Index);
        }

        /// <summary>
        /// Gets the current value in the enumeration.
        /// </summary>
        public DataView Current => m_Current;

        /// <summary>
        /// Moves to the next item in the enumeration.
        /// </summary>
        /// <returns><see langword="true"/> if there are more items; otherwise, <see langword="false"/>.</returns>
        public bool MoveNext()
        {
            while (m_Stack.Count > 0)
            {
                int slot = m_Stack.Pop();
                if (!m_Tree.IsAlive(slot)) continue;

                m_Current = m_Source[new MultiTreeNode<DataViewId>(m_Tree, slot)];

                var children = m_Tree.GetChildren(slot);
                int count = children.Count;
                for (int i = count - 1; i >= 0; --i)
                    m_Stack.Push(children[i].Index);
                return true;
            }
            m_Current = default;
            return false;
        }
    }
}
