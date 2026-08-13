using System;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Represents a multi-rooted hierarchical tree structure where nodes can be indexed, counted, and versioned.
    /// Provides methods to access node data, root nodes, and child nodes.
    /// </summary>
    /// <typeparam name="T">The type of data stored in each tree node.</typeparam>
    /*public*/ interface IMultiTree<T> : IIndexable<int, MultiTreeNode<T>>, ICountable, IVersioned
    {
        /// <summary>
        /// Gets the enumerable collection of root nodes in the tree.
        /// </summary>
        MultiTreeNodeEnumerable<IndirectEnumerable, T> RootNodes { get; }

        /// <summary>
        /// Gets the data associated with the specified node index.
        /// </summary>
        /// <param name="index">The index of the node.</param>
        /// <returns>The data of type <typeparamref name="T"/> stored in the specified node.</returns>
        T GetData(int index);

        /// <summary>
        /// Gets the data associated with the specified node index.
        /// </summary>
        /// <param name="index">The index of the node.</param>
        /// <param name="data">.</param>
        void SetData(int index, T data);

        /// <summary>
        /// Gets the root node associated with the specified node index.
        /// </summary>
        /// <param name="index">The index of the node.</param>
        /// <returns>The root node of type <see cref="MultiTreeNode{T}"/>.</returns>
        MultiTreeNode<T> GetRootNode(int index);

        /// <summary>
        /// Gets the parent node associated with the specified node index.
        /// </summary>
        /// <param name="index">The index of the node.</param>
        /// <returns>The parent node of type <see cref="MultiTreeNode{T}"/>.</returns>
        MultiTreeNode<T>? GetParentNode(int index);

        /// <summary>
        /// Gets the enumerable collection of children for the specified node index.
        /// </summary>
        /// <param name="index">The index of the node.</param>
        /// <returns>
        /// A <see cref="MultiTreeNodeChildren{T}"/> over the child nodes of the specified node.
        /// Iterating via <c>foreach</c> throws on concurrent modification of that node's child list.
        /// </returns>
        MultiTreeNodeChildren<T> GetChildren(int index);

        /// <summary>
        /// Indicates whether the node at the specified slot index is alive.
        /// Dead slots remain in the index space (so other indices stay stable) but are skipped by enumerators.
        /// </summary>
        /// <param name="index">The slot index to query.</param>
        /// <returns><c>true</c> if the slot is alive; <c>false</c> if it has been removed.</returns>
        bool IsAlive(int index) => true;
    }

    /// <summary>
    /// Represents a node in a multi-rooted hierarchical tree.
    /// Provides access to the node's data, its root node, and its child nodes.
    /// </summary>
    /// <typeparam name="T">The type of data stored in the tree node.</typeparam>
    /*public*/ readonly struct MultiTreeNode<T>
    {
        /// <summary>
        /// Reference to the tree containing this node.
        /// </summary>
        readonly IMultiTree<T> m_Owner;

        /// <summary>
        /// The index of this node within its tree.
        /// </summary>
        readonly int m_Index;

        /// <summary>
        /// Gets the tree to which this node belongs.
        /// </summary>
        public IMultiTree<T> Tree
        {
            get
            {
                CheckAlive();
                return m_Owner;
            }
        }

        /// <summary>
        /// Gets the slot index of this node within its tree.
        /// </summary>
        public int Index => m_Index;

        /// <summary>
        /// Gets a value indicating whether this node still refers to a live slot in the tree.
        /// </summary>
        public bool IsAlive => m_Owner != null && m_Owner.IsAlive(m_Index);

        void CheckAlive() { if (!IsAlive) throw new InvalidOperationException(
            $"MultiTreeNode<{typeof(T).Name}> at index {m_Index} no longer refers to a live slot in the tree."); }

        /// <summary>
        /// Gets the data of type <typeparamref name="T"/> associated with this node.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the node's slot has been removed from the tree.</exception>
        public T Data => Tree.GetData(m_Index);

        /// <summary>
        /// Gets the root node associated with this node.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the node's slot has been removed from the tree.</exception>
        public MultiTreeNode<T> Root => Tree.GetRootNode(m_Index);

        /// <summary>
        /// Gets the parent node associated with this node, or <c>null</c> if this node is a root.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the node's slot has been removed from the tree.</exception>
        public MultiTreeNode<T>? Parent => Tree.GetParentNode(m_Index);

        /// <summary>
        /// Gets the enumerable collection of child nodes associated with this node.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the node's slot has been removed from the tree.</exception>
        public MultiTreeNodeChildren<T> Children => Tree.GetChildren(m_Index);

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiTreeNode{T}"/> struct.
        /// </summary>
        /// <param name="tree">The tree to which this node belongs.</param>
        /// <param name="index">The index of this node within the tree.</param>
        public MultiTreeNode(IMultiTree<T> tree, int index)
        {
            m_Owner = tree;
            m_Index = index;
        }
    }

    /// <summary>
    /// Represents an enumerable collection of tree nodes.
    /// Provides indexed and counted access to nodes.
    /// </summary>
    /// <typeparam name="TEnumerable">
    /// The type of the source providing indices for nodes.
    /// Must implement both <see cref="IIndexable{TIndex, TValue}"/> and <see cref="ICountable"/>.
    /// </typeparam>
    /// <typeparam name="T">The type of data stored in the nodes.</typeparam>
    /*public*/ readonly struct MultiTreeNodeEnumerable<TEnumerable, T> : IIndexable<int, MultiTreeNode<T>>, ICountable where TEnumerable : IIndexable<int, int>, ICountable
    {
        /// <summary>
        /// Reference to the tree containing these nodes.
        /// </summary>
        readonly IMultiTree<T> m_Owner;

        /// <summary>
        /// The source providing indices for the nodes in the enumerable.
        /// </summary>
        readonly TEnumerable m_Source;

        /// <summary>
        /// Gets the tree to which this enumerable belongs.
        /// </summary>
        public IMultiTree<T> Tree => m_Owner;

        /// <summary>
        /// Gets the number of nodes in the enumerable.
        /// </summary>
        public int Count => m_Source.Count;

        /// <summary>
        /// Gets the <see cref="MultiTreeNode{T}"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the node.</param>
        /// <value>The node of type <see cref="MultiTreeNode{T}"/> at the specified index.</value>
        public MultiTreeNode<T> this[int index] => m_Owner[m_Source[index]];

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiTreeNodeEnumerable{TEnumerable, T}"/> struct.
        /// </summary>
        /// <param name="tree">The tree containing these nodes.</param>
        /// <param name="source">The source providing indices for the nodes.</param>
        public MultiTreeNodeEnumerable(IMultiTree<T> tree, TEnumerable source)
        {
            m_Owner = tree;
            m_Source = source;
        }
    }

    /// <summary>
    /// A node's children, backed by a single sublist of the tree's child <see cref="IMultiList{T}"/>.
    /// Iterating via <c>foreach</c> composes the inner <see cref="VersionedSublistEnumerator{T}"/>,
    /// so concurrent modification of that node's child list is detected and throws. Indexed access
    /// (<c>this[i]</c>) bypasses the version check, matching the BCL contract
    /// (<see cref="System.Collections.Generic.List{T}"/>'s indexer doesn't throw either).
    /// </summary>
    /// <typeparam name="T">Node data type.</typeparam>
    /*public*/ readonly struct MultiTreeNodeChildren<T> : IIndexable<int, MultiTreeNode<T>>, ICountable
    {
        readonly IMultiTree<T> m_Owner;
        readonly SubEnumerable<int> m_Source;

        /// <summary>Gets the number of children.</summary>
        public int Count => m_Source.Count;

        /// <summary>Gets the child node at the specified position.</summary>
        public MultiTreeNode<T> this[int index] => m_Owner[m_Source[index]];

        public MultiTreeNodeChildren(IMultiTree<T> tree, SubEnumerable<int> source)
        {
            m_Owner = tree;
            m_Source = source;
        }

        /// <summary>
        /// Returns a version-checked enumerator. Composes the inner
        /// <see cref="VersionedSublistEnumerator{T}"/> over the child sublist with the
        /// slot-to-<see cref="MultiTreeNode{T}"/> projection (the tree is its own provider).
        /// </summary>
        public ResolvingEnumerator<int, MultiTreeNode<T>, VersionedSublistEnumerator<int>> GetEnumerator() =>
            new(m_Owner, m_Source.GetEnumerator());
    }
}
