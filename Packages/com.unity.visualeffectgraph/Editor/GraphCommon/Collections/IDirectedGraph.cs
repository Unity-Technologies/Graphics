using System;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Represents a graph structure.
    /// Provides methods to access node data, parent nodes, and child nodes.
    /// </summary>
    /// <typeparam name="T">The type of data stored in each graph node.</typeparam>
    /*public*/ interface IDirectedGraph<T> : IIndexable<int, GraphNode<T>>, ICountable, IVersioned
    {
        /// <summary>
        /// Gets the data associated with the specified node index.
        /// </summary>
        /// <param name="index">The index of the node.</param>
        /// <returns>The data of type <typeparamref name="T"/> stored in the specified node.</returns>
        T GetData(int index);

        /// <summary>
        /// Gets the parent nodes for the specified node index in the graph.
        /// </summary>
        /// <param name="index">The index of the node whose parents are to be retrieved.</param>
        /// <returns>
        /// A <see cref="GraphNodeLinks{T}"/> containing the parent nodes of the specified node.
        /// </returns>
        GraphNodeLinks<T> GetParents(int index);

        /// <summary>
        /// Gets the child nodes for the specified node index in the graph.
        /// </summary>
        /// <param name="index">The index of the node whose children are to be retrieved.</param>
        /// <returns>
        /// A <see cref="GraphNodeLinks{T}"/> containing the child nodes of the specified node.
        /// </returns>
        GraphNodeLinks<T> GetChildren(int index);

        /// <summary>
        /// Indicates whether the node at the specified slot index is alive.
        /// Dead slots remain in the index space (so other indices stay stable) but are skipped by enumerators.
        /// </summary>
        /// <param name="index">The slot index to query.</param>
        /// <returns><c>true</c> if the slot is alive; <c>false</c> if it has been removed.</returns>
        bool IsAlive(int index) => true;
    }

    /// <summary>
    /// Represents a node within the graph, providing access to its associated data,
    /// parent nodes, and child nodes.
    /// </summary>
    /// <typeparam name="T">The type of data stored in the node.</typeparam>
    /*public*/ readonly struct GraphNode<T>
    {
        /// <summary>
        /// Reference to the graph containing this node.
        /// </summary>
        readonly IDirectedGraph<T> m_Owner;

        /// <summary>
        /// The index of this node within its graph.
        /// </summary>
        readonly int m_Index;

        /// <summary>
        /// Gets the slot index of this node within its graph.
        /// </summary>
        public int Index => m_Index;

        /// <summary>
        /// Gets a value indicating whether this node still refers to a live slot in the graph.
        /// </summary>
        public bool IsAlive => m_Owner != null && m_Owner.IsAlive(m_Index);

        /// <summary>
        /// Gets the graph to which this node belongs.
        /// </summary>
        public IDirectedGraph<T> Graph
        {
            get
            {
                CheckAlive();
                return m_Owner;
            }
        }

        void CheckAlive() { if (!IsAlive) throw new InvalidOperationException(
            $"GraphNode<{typeof(T).Name}> at index {m_Index} no longer refers to a live slot in the graph."); }

        /// <summary>
        /// Gets the data of type <typeparamref name="T"/> associated with this node.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the node's slot has been removed from the graph.</exception>
        public T Data => Graph.GetData(m_Index);

        /// <summary>
        /// Gets the parent nodes linked to this node within the graph.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the node's slot has been removed from the graph.</exception>
        public GraphNodeLinks<T> Parents => Graph.GetParents(m_Index);

        /// <summary>
        /// Gets the child nodes linked to this node within the graph.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the node's slot has been removed from the graph.</exception>
        public GraphNodeLinks<T> Children => Graph.GetChildren(m_Index);

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphNode{T}"/> struct.
        /// </summary>
        /// <param name="graph">The graph to which this node belongs.</param>
        /// <param name="index">The index of this node within the graph.</param>
        public GraphNode(IDirectedGraph<T> graph, int index)
        {
            m_Owner = graph;
            m_Index = index;
        }
    }

    /// <summary>
    /// Represents a collection of links (parents or children) associated with a graph node.
    /// Provides indexed access to linked nodes.
    /// </summary>
    /// <typeparam name="T">The type of data stored in the linked nodes.</typeparam>
    /*public*/ readonly struct GraphNodeLinks<T> : IIndexable<int, GraphNode<T>>, ICountable
    {
        /// <summary>
        /// Reference to the graph containing these links.
        /// </summary>
        readonly IDirectedGraph<T> m_Owner;

        /// <summary>
        /// Source mapping indices of links (parent or child slot indices). Depends on
        /// <see cref="IMultiList{T}"/> so the backing storage representation can be swapped
        /// without changing this enumerator.
        /// </summary>
        readonly IMultiList<int> m_Source;

        /// <summary>
        /// The index of the current node within the graph (which sublist of <see cref="m_Source"/>
        /// to walk).
        /// </summary>
        readonly int m_Index;

        /// <summary>
        /// The total number of links (parents or children) associated with the node.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Gets the linked <see cref="GraphNode{T}"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the link.</param>
        /// <value>The linked <see cref="GraphNode{T}"/>.</value>
        public GraphNode<T> this[int index] => m_Owner[m_Source[m_Index, index]];

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphNodeLinks{T}"/> struct.
        /// </summary>
        /// <param name="owner">The graph containing these links.</param>
        /// <param name="source">
        /// The backing <see cref="IMultiList{T}"/> of int slot indices that this links view
        /// reads from (typically <c>LinearGraph&lt;T&gt;.m_Parents</c> or <c>.m_Children</c>).
        /// </param>
        /// <param name="index">The sublist index — the slot of the node whose links this view exposes.</param>
        /// <param name="count">The total number of links associated with this node.</param>
        public GraphNodeLinks(IDirectedGraph<T> owner, IMultiList<int> source, int index, int count)
        {
            m_Owner = owner;
            m_Source = source;
            m_Index = index;
            Count = count;
        }

        /// <summary>
        /// Returns an enumerator that walks the linked nodes and throws on concurrent modification
        /// of the underlying sublist. Mutations to other sublists of the same backing
        /// <see cref="LinearMultiList{T}"/> (e.g. edge changes touching unrelated nodes) do NOT
        /// invalidate this enumerator.
        /// </summary>
        public GraphNodeLinksEnumerator<T> GetEnumerator() =>
            new GraphNodeLinksEnumerator<T>(m_Owner, m_Source, m_Index, Count);
    }

    /// <summary>
    /// Composing enumerator: wraps <see cref="VersionedSublistEnumerator{T}"/> (which yields int
    /// slot indices and performs the per-sublist version check) and projects each popped slot
    /// through <see cref="IDirectedGraph{T}"/> to a <see cref="GraphNode{T}"/>.
    /// </summary>
    /// <typeparam name="T">The data type of the graph nodes being enumerated.</typeparam>
    /*public*/ struct GraphNodeLinksEnumerator<T> : IValueEnumerator<GraphNode<T>>
    {
        VersionedSublistEnumerator<int> m_Inner;
        readonly IDirectedGraph<T> m_Graph;

        internal GraphNodeLinksEnumerator(IDirectedGraph<T> graph, IMultiList<int> source, int listIndex, int count)
        {
            m_Graph = graph;
            m_Inner = new VersionedSublistEnumerator<int>(source, listIndex, count);
        }

        public GraphNode<T> Current => new GraphNode<T>(m_Graph, m_Inner.Current);

        public bool MoveNext() => m_Inner.MoveNext();
    }

}
