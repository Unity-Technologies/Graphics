namespace UnityEditor.Rendering.HighDefinition
{
    using System;
    using System.Collections;
    using UnityEngine.Assertions;
    using static IndexGraph;

    /// <summary>Class to provide static API for <see cref="IndexGraph{N, E}"/>.</summary>
    public static class IndexGraph
    {
        /// <summary>
        /// Index of a node in an <see cref="IndexGraph{N, E}"/>.
        /// </summary>
        public struct NodeIndex: IEquatable<NodeIndex>
        {
            internal NodeIndex(int index) => this.index = index;

            internal int index;

            public bool Equals(NodeIndex other) => other.index == index;
            public override bool Equals(object obj) => (obj is NodeIndex node) && node.Equals(this);
            public override int GetHashCode() => index.GetHashCode();

            public static bool operator==(in NodeIndex l, in NodeIndex r) => l.Equals(r);
            public static bool operator!=(in NodeIndex l, in NodeIndex r) => l.Equals(r);
        }

        /// <summary>
        /// Index of an edge in an <see cref="IndexGraph{N, E}"/>.
        /// </summary>
        public struct EdgeIndex : IEquatable<EdgeIndex>
        {
            public EdgeIndex(int index) => this.index = index;

            internal int index;

            public bool Equals(EdgeIndex other) => other.index == index;
            public override bool Equals(object obj) => (obj is EdgeIndex edge) && edge.Equals(this);
            public override int GetHashCode() => index.GetHashCode();

            public static bool operator ==(in EdgeIndex l, in EdgeIndex r) => l.Equals(r);
            public static bool operator !=(in EdgeIndex l, in EdgeIndex r) => l.Equals(r);
        }

        //
        // Enumerators
        //

        /// <summary>Enumerate edges by reference of <see cref="IndexGraph{N, E}"/>.</summary>
        /// <typeparam name="E">The type of the edges.</typeparam>
        public struct EdgeRefEnumerator<E> : IRefEnumerator<E>
            where E: struct
        {
            ArrayListRefEnumerator<E> m_En;

            internal EdgeRefEnumerator(ArrayList<E> source)
                => m_En = new ArrayListRefEnumerator<E>(source);

            public ref readonly E current => ref m_En.current;
            public bool MoveNext() => m_En.MoveNext();
            public void Reset() => m_En.Reset();
        }

        /// <summary>Enumerate edges by mutable reference of <see cref="IndexGraph{N, E}"/>.</summary>
        /// <typeparam name="E">The type of the edges.</typeparam>
        public struct EdgeMutEnumerator<E> : IMutEnumerator<E> 
            where E : struct
        {
            ArrayListMutEnumerator<E> m_En;

            internal EdgeMutEnumerator(ArrayList<E> source)
                => m_En = new ArrayListMutEnumerator<E>(source);

            public ref E current => ref m_En.current;
            public bool MoveNext() => m_En.MoveNext();
            public void Reset() => m_En.Reset();
        }

        /// <summary>Enumerate nodes by reference of <see cref="IndexGraph{N, E}"/>.</summary>
        /// <typeparam name="E">The type of the nodes.</typeparam>
        public struct NodeRefEnumerator<N> : IRefEnumerator<N>
            where N: struct
        {
            ArrayListRefEnumerator<N> m_En;

            internal NodeRefEnumerator(ArrayList<N> source)
                => m_En = new ArrayListRefEnumerator<N>(source);

            public ref readonly N current => ref m_En.current;
            public bool MoveNext() => m_En.MoveNext();
            public void Reset() => m_En.Reset();
        }

        /// <summary>Enumerate nodes by mutable reference of <see cref="IndexGraph{N, E}"/>.</summary>
        /// <typeparam name="E">The type of the nodes.</typeparam>
        public struct NodeMutEnumerator<N> : IMutEnumerator<N>
            where N : struct
        {
            ArrayListMutEnumerator<N> m_En;

            internal NodeMutEnumerator(ArrayList<N> source)
                => m_En = new ArrayListMutEnumerator<N>(source);

            public ref N current => ref m_En.current;
            public bool MoveNext() => m_En.MoveNext();
            public void Reset() => m_En.Reset();
        }

        /// <summary>Enumerate node indices of nodes leaving a node index in an <see cref="IndexGraph{N, E}"/>.</summary>
        public struct NodeIndexFromEnumerator : IRefEnumerator<NodeIndex>
        {
            struct FromIndex : IInFunc<(NodeIndex from, NodeIndex to), bool>
            {
                NodeIndex m_From;

                public FromIndex(NodeIndex from) => m_From = from;

                public bool Execute(in (NodeIndex from, NodeIndex to) edgeNodes) => m_From == edgeNodes.from;
            }

            internal NodeIndexFromEnumerator(NodeIndex from, ArrayList<(NodeIndex, NodeIndex)> source)
            {
                m_Enumerator = new WhereRefIterator<(NodeIndex from, NodeIndex to), ArrayListRefEnumerator<(NodeIndex from, NodeIndex to)>, FromIndex>(
                    new ArrayListRefEnumerator<(NodeIndex, NodeIndex)>(source),
                    new FromIndex(from)
                );
            }

            WhereRefIterator<(NodeIndex from, NodeIndex to), ArrayListRefEnumerator<(NodeIndex from, NodeIndex to)>, FromIndex> m_Enumerator;

            public ref readonly NodeIndex current => ref m_Enumerator.current.to;

            public bool MoveNext() => m_Enumerator.MoveNext();

            public void Reset() => m_Enumerator.Reset();
        }

        /// <summary>Enumerate node indices of nodes coming at a node index in an <see cref="IndexGraph{N, E}"/>.</summary>
        public struct NodeIndexToEnumerator : IRefEnumerator<NodeIndex>
        {
            struct ToIndex : IInFunc<(NodeIndex from, NodeIndex to), bool>
            {
                NodeIndex m_To;

                public ToIndex(NodeIndex to) => m_To = to;

                public bool Execute(in (NodeIndex from, NodeIndex to) edgeNodes) => m_To == edgeNodes.from;
            }

            internal NodeIndexToEnumerator(NodeIndex to, ArrayList<(NodeIndex, NodeIndex)> source)
            {
                m_Enumerator = new WhereRefIterator<(NodeIndex To, NodeIndex to), ArrayListRefEnumerator<(NodeIndex from, NodeIndex to)>, ToIndex>(
                    new ArrayListRefEnumerator<(NodeIndex, NodeIndex)>(source),
                    new ToIndex(to)
                );
            }

            WhereRefIterator<(NodeIndex from, NodeIndex to), ArrayListRefEnumerator<(NodeIndex from, NodeIndex to)>, ToIndex> m_Enumerator;

            public ref readonly NodeIndex current => ref m_Enumerator.current.from;

            public bool MoveNext() => m_Enumerator.MoveNext();

            public void Reset() => m_Enumerator.Reset();
        }
    }


    /// <summary>
    /// Graph stored as index based nodes and edges.
    ///
    /// Remove operations are not conservative: <see cref="NodeIndex"/> and <see cref="EdgeIndex"/> may change.
    /// </summary>
    /// <typeparam name="N"></typeparam>
    /// <typeparam name="E"></typeparam>
    public class IndexGraph<N, E>
        where N: struct
        where E: struct
    {
        ArrayList<N> m_Nodes;
        ArrayList<E> m_Edges;
        ArrayList<(NodeIndex from, NodeIndex to)> m_EdgeNodes;

        /// <summary>An iterator on all edges by reference.</summary>
        public EdgeRefEnumerator<E> edges => new EdgeRefEnumerator<E>(m_Edges);
        /// <summary>An iterator on all edges by mutable reference.</summary>
        public EdgeMutEnumerator<E> edgesMut => new EdgeMutEnumerator<E>(m_Edges);
        /// <summary>An iterator on all nodes by reference.</summary>
        public NodeRefEnumerator<N> nodes => new NodeRefEnumerator<N>(m_Nodes);
        /// <summary>An iterator on all nodes by mutable reference.</summary>
        public NodeMutEnumerator<N> nodesMut => new NodeMutEnumerator<N>(m_Nodes);

        /// <summary>Add a node to the graph.</summary>
        /// <param name="node">The node to add.</param>
        /// <returns>The index of the added node.</returns>
        public NodeIndex AddNode(in N node)
        {
            var index = m_Nodes.count;
            m_Nodes.Add(node);
            return new NodeIndex(index);
        }

        /// <summary>Get a node by reference.</summary>
        /// <param name="index">The index of the node.</param>
        /// <returns>A reference to the node.</returns>
        /// <exception cref="ArgumentOutOfRangeException">When the <paramref name="index"/> is out of range.</exception>
        public ref readonly N GetNode(in NodeIndex index)
            => ref m_Nodes.Get(index.index);

        /// <summary>Get a node by mutable reference.</summary>
        /// <param name="index">The index of the node.</param>
        /// <returns>A mutable reference to the node.</returns>
        /// <exception cref="ArgumentOutOfRangeException">When the <paramref name="index"/> is out of range.</exception>
        public ref N GetMutNode(in NodeIndex index)
            => ref m_Nodes.GetMut(index.index);

        /// <summary>
        /// Add an edge to the graph.
        /// </summary>
        /// <param name="from">The starting node index.</param>
        /// <param name="to">The ending node index.</param>
        /// <param name="edge">The edge to add.</param>
        /// <returns>The index of the node.</returns>
        public EdgeIndex AddEdge(in NodeIndex from, in NodeIndex to, in E edge)
        {
            var index = m_Edges.count;
            m_Edges.Add(edge);
            m_EdgeNodes.Add((from, to));
            return new EdgeIndex(index);
        }

        /// <summary>Get an edge by reference.</summary>
        /// <param name="index">The index of the edge.</param>
        /// <returns>A reference to the edge.</returns>
        /// <exception cref="ArgumentOutOfRangeException">When the <paramref name="index"/> is out of range.</exception>
        public ref readonly E GetEdge(in EdgeIndex index) => ref m_Edges.Get(index.index);
        /// <summary>Get an edge by mutable reference.</summary>
        /// <param name="index">The index of the edge.</param>
        /// <returns>A mutable reference to the edge.</returns>
        /// <exception cref="ArgumentOutOfRangeException">When the <paramref name="index"/> is out of range.</exception>
        public ref E GetMutEdge(in EdgeIndex index) => ref m_Edges.GetMut(index.index);

        /// <summary>Get the node indices of the starting node and ending node for an edge index.</summary>
        /// <param name="index">The index of the edge.</param>
        /// <returns>The starting and ending node indices for the edge.</returns>
        /// <exception cref="ArgumentOutOfRangeException">When the <paramref name="index"/> is out of range.</exception>
        public ref readonly (NodeIndex from, NodeIndex to) GetEdgeNodes(in EdgeIndex index)
            => ref m_EdgeNodes.Get(index.index);

        /// <summary>Get an enumerator of the node indices leaving <paramref name="index"/>.</summary>
        /// <param name="index">The node index of the nodes to search.</param>
        /// <returns>The enumerator of the node indices of edges leaving the provided node.</returns>
        public NodeIndexFromEnumerator GetNodeIndicesFrom(in NodeIndex index) => new NodeIndexFromEnumerator(index, m_EdgeNodes);
        /// <summary>Get an enumerator of the node indices arriving at <paramref name="index"/>.</summary>
        /// <param name="index">The node index of the nodes to search.</param>
        /// <returns>The enumerator of the node indices of edges arriving at the provided node.</returns>
        public NodeIndexToEnumerator GetNodeIndicesTo(in NodeIndex index) => new NodeIndexToEnumerator(index, m_EdgeNodes);
    }

    //
    // Enumerator Utilities
    //

    /// <summary>
    /// Iterate by reference over a collection.
    ///
    /// similar to <see cref="System.Collections.Generic.IEnumerator{T}"/> but with a reference to the strong type.
    /// </summary>
    /// <typeparam name="T">The type of the iterator.</typeparam>
    public interface IRefEnumerator<T>
    {
        /// <summary>A reference to the current value.</summary>
        ref readonly T current { get; }

        /// <summary>Move to the next value.</summary>
        /// <returns><c>true</c> when a value was found, <c>false</c> when the enumerator has completed.</returns>
        bool MoveNext();

        /// <summary>Reset the enumerator to its initial state.</summary>
        void Reset();
    }

    /// <summary>
    /// Iterate by mutable reference over a collection.
    ///
    /// similar to <see cref="System.Collections.Generic.IEnumerator{T}"/> but with a mutable reference to the strong type.
    /// </summary>
    /// <typeparam name="T">The type of the iterator.</typeparam>
    public interface IMutEnumerator<T> 
    {
        /// <summary>A mutable reference to the current value.</summary>
        ref T current { get; }

        /// <summary>Move to the next value.</summary>
        /// <returns><c>true</c> when a value was found, <c>false</c> when the enumerator has completed.</returns>
        bool MoveNext();

        /// <summary>Reset the enumerator to its initial state.</summary>
        void Reset();
    }

    /// <summary>
    /// Interface similar to <see cref="System.Func{T, TResult}"/> but consuming <c>in</c> arguments.
    ///
    /// Implement this interface on a struct to have inlined callbacks by the compiler.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument.</typeparam>
    /// <typeparam name="R">The type of the return.</typeparam>
    public interface IInFunc<T1, R>
    {
        /// <summary>Execute the function.</summary>
        R Execute(in T1 t1);
    }

    /// <summary>An enumerator performing a select function.</summary>
    /// <typeparam name="I">Type of the input value.</typeparam>
    /// <typeparam name="O">Type of the output value.</typeparam>
    /// <typeparam name="En">Type of the enumerator to consume by reference.</typeparam>
    /// <typeparam name="S">Type of the select function.</typeparam>
    public struct SelectRefEnumerator<I, O, En, S> : System.Collections.Generic.IEnumerator<O>
        where En : struct, IRefEnumerator<I>
        where S: struct, IInFunc<I, O>
    {
        En m_Enumerator;
        S m_Select;

        public SelectRefEnumerator(En enumerator, S select)
        {
            m_Enumerator = enumerator;
            m_Select = select;
        }

        public O Current => m_Select.Execute(m_Enumerator.current);

        object IEnumerator.Current => m_Select.Execute(m_Enumerator.current);

        public void Dispose() {}

        public bool MoveNext() => m_Enumerator.MoveNext();

        public void Reset() => m_Enumerator.Reset();
    }

    /// <summary>
    /// An enumerator by reference thats skip values based on a where clause.
    /// </summary>
    /// <typeparam name="T">Type of the enumerated values.</typeparam>
    /// <typeparam name="En">Type of the enumerator.</typeparam>
    /// <typeparam name="Wh">Type of the where clause.</typeparam>
    public struct WhereRefIterator<T, En, Wh> : IRefEnumerator<T>
        where Wh : struct, IInFunc<T, bool>
        where En : IRefEnumerator<T>
    {
        class Data
        {
            public En enumerator;
            public Wh whereClause;
        }

        Data m_Data;

        public WhereRefIterator(En enumerator, Wh whereClause)
        {
            m_Data = new Data
            {
                enumerator = enumerator,
                whereClause = whereClause
            };
        }

        public ref readonly T current
        {
            get
            {
                if (m_Data == null)
                    throw new InvalidOperationException("Enumerator not initialized.");

                return ref m_Data.enumerator.current;
            }
        }

        public bool MoveNext()
        {
            if (m_Data == null)
                return false;

            while (m_Data.enumerator.MoveNext())
            {
                ref readonly var value = ref m_Data.enumerator.current;
                if (m_Data.whereClause.Execute(value))
                    return true;
            }

            return false;
        }

        public void Reset()
        {
            if (m_Data == null)
                throw new InvalidOperationException("Enumerator not initialized.");

            m_Data.enumerator.Reset();
        }
    }

    /// <summary>
    /// An enumerator by mutable reference thats skip values based on a where clause.
    /// </summary>
    /// <typeparam name="T">Type of the enumerated values.</typeparam>
    /// <typeparam name="En">Type of the enumerator.</typeparam>
    /// <typeparam name="Wh">Type of the where clause.</typeparam>
    public struct WhereMutIterator<T, En, Wh> : IMutEnumerator<T>
        where Wh : struct, IInFunc<T, bool>
        where En : IMutEnumerator<T>
    {
        class Data
        {
            public En enumerator;
            public Wh whereClause;
        }

        Data m_Data;

        public WhereMutIterator(En enumerator, Wh whereClause)
        {
            m_Data = new Data
            {
                enumerator = enumerator,
                whereClause = whereClause
            };
        }

        public ref T current
        {
            get
            {
                if (m_Data == null)
                    throw new InvalidOperationException("Enumerator not initialized.");

                return ref m_Data.enumerator.current;
            }
        }

        public bool MoveNext()
        {
            if (m_Data == null)
                return false;

            while (m_Data.enumerator.MoveNext())
            {
                ref var value = ref m_Data.enumerator.current;
                if (m_Data.whereClause.Execute(value))
                    return true;
            }

            return false;
        }

        public void Reset()
        {
            if (m_Data == null)
                throw new InvalidOperationException("Enumerator not initialized.");

            m_Data.enumerator.Reset();
        }
    }

    //
    // ArrayList Utilities
    //

    /// <summary>
    /// A list based on an array with reference access.
    ///
    /// If the array does not have any values, it does not allocate memory on the heap.
    ///
    /// This list grows its backend array when items are pushed.
    /// </summary>
    /// <typeparam name="T">Type of the array's item.</typeparam>
    public struct ArrayList<T>
        where T: struct
    {
        // Use a class to store the array information on the heap
        // So we can safely copy the ArrayList struct and still point
        // to the same data.
        class Data
        {
            public int count;
            public T[] storage;
        }

        const float GrowFactor = 2.0f;

        Data m_Data;

        Data data => m_Data ?? (m_Data = new Data());

        ReadOnlySpan<T> span => new ReadOnlySpan<T>(data.storage);
        Span<T> spanMut => new Span<T>(data.storage);

        /// <summary>Number of item in the list.</summary>
        public int count => data.count;

        /// <summary>Iterate over the values of the list by reference.</summary>
        public ArrayListRefEnumerator<T> values => new ArrayListRefEnumerator<T>(this);
        /// <summary>Iterate over the values of the list by mutable reference.</summary>
        public ArrayListMutEnumerator<T> valuesMut => new ArrayListMutEnumerator<T>(this);

        /// <summary>
        /// Add a value to the list.
        ///
        /// If the backend don't have enough memory, the list will increase the allocated memory.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>The index of the added value.</returns>
        public int Add(in T value)
        {
            var index = data.count;
            data.count++;

            unsafe { GrowIfRequiredFor(data.count); }

            data.storage[index] = value;
            return index;
        }

        /// <summary>
        /// Get a reference to an item's value.
        ///
        /// Safety:
        /// If the index is out of bounds, the behaviour is undefined.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <returns>A reference to the item.</returns>
        public unsafe ref readonly T GetUnsafe(int index) => ref span[index];

        /// <summary>Get a reference to an item's value.</summary>
        /// <param name="index">The index of the item.</param>
        /// <returns>A reference to the item.</returns>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="index"/> is out of bounds.</exception>
        public ref readonly T Get(int index)
        {
            if (index < 0 || index >= data.count)
                throw new ArgumentOutOfRangeException(nameof(index));

            unsafe { return ref GetUnsafe(index); }
        }

        /// <summary>
        /// Get a mutable reference to an item's value.
        ///
        /// Safety:
        /// If the index is out of bounds, the behaviour is undefined.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <returns>A reference to the item.</returns>
        public unsafe ref T GetMutUnsafe(int index) => ref spanMut[index];

        /// <summary>Get a mutable reference to an item's value.</summary>
        /// <param name="index">The index of the item.</param>
        /// <returns>A mutable reference to the item.</returns>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="index"/> is out of bounds.</exception>
        public ref T GetMut(int index)
        {
            if (index < 0 || index >= data.count)
                throw new ArgumentOutOfRangeException(nameof(index));

            unsafe { return ref GetMutUnsafe(index); }
        }

        /// <summary>
        /// Removes an item by copying the last entry at <paramref name="index"/> position.
        ///
        /// Safety:
        /// Behaviour is undefined when <paramref name="index"/> is out of bounds.
        /// </summary>
        /// <param name="index">The index of the item to remove.</param>
        public unsafe void RemoveSwapBackUnsafe(int index)
        {
            var spanMut = this.spanMut;
            spanMut[index] = spanMut[data.count - 1];
            data.count--;
        }

        /// <summary>/// Removes an item by copying the last entry at <paramref name="index"/> position.</summary>
        /// <param name="index">The index of the item to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">When the <paramref name="index"/> is out of bounds.</exception>
        public void RemoveSwapBack(int index)
        {
            if (index < 0 || index >= data.count)
                throw new ArgumentOutOfRangeException(nameof(index));

            unsafe { RemoveSwapBackUnsafe(index); }
        }

        /// <summary>
        /// Grow the current backend to have at least <paramref name="size"/> item in memory.
        ///
        /// If the current backend is null, <paramref name="size"/> items will be allocated.
        ///
        /// Safety:
        /// Behaviour is undefined if <paramref name="size"/> is negative or 0.
        /// </summary>
        /// <param name="size"></param>
        unsafe void GrowIfRequiredFor(int size)
        {
            Assert.IsTrue(size > 0);

            if (data.storage == null)
                data.storage = new T[size];
            else if (data.storage.Length < size)
            {
                var nextSize = (float)data.storage.Length;
                while (nextSize < size)
                    nextSize *= GrowFactor;

                Array.Resize(ref data.storage, (int)nextSize + 1);
            }
        }
    }

    /// <summary>
    /// An enumerator by reference over a <see cref="ArrayList{T}"/>.
    /// </summary>
    /// <typeparam name="T">Type of the enumerated values.</typeparam>
    public struct ArrayListRefEnumerator<T> : IRefEnumerator<T>
        where T : struct
    {
        class Data
        {
            public ArrayList<T> source;
            public int index;
        }

        Data m_Data;

        public ArrayListRefEnumerator(ArrayList<T> source)
        {
            m_Data = new Data
            {
                source = source,
                index = -1
            };
        }

        public ref readonly T current
        {
            get
            {
                if (m_Data == null || m_Data.index < 0 || m_Data.index >= m_Data.source.count)
                    throw new InvalidOperationException("Enumerator was not initialized");

                return ref m_Data.source.Get(m_Data.index);
            }
        }

        public bool MoveNext()
        {
            if (m_Data == null)
                return false;

            var next = m_Data.index + 1;
            if (next < m_Data.source.count)
            {
                m_Data.index++;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            if (m_Data == null)
                return;

            m_Data.index = 0;
        }
    }

    /// <summary>
    /// An enumerator by mutable reference over a <see cref="ArrayList{T}"/>.
    /// </summary>
    /// <typeparam name="T">Type of the enumerated values.</typeparam>
    public struct ArrayListMutEnumerator<T> : IMutEnumerator<T>
        where T : struct
    {
        class Data
        {
            public ArrayList<T> source;
            public int index;
        }

        Data m_Data;

        public ArrayListMutEnumerator(ArrayList<T> source)
        {
            m_Data = new Data
            {
                source = source,
                index = -1
            };
        }

        public ref T current
        {
            get
            {
                if (m_Data == null || m_Data.index < 0 || m_Data.index >= m_Data.source.count)
                    throw new InvalidOperationException("Enumerator was not initialized");

                return ref m_Data.source.GetMut(m_Data.index);
            }
        }

        public bool MoveNext()
        {
            if (m_Data == null)
                return false;

            var next = m_Data.index + 1;
            if (next < m_Data.source.count)
            {
                m_Data.index++;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            if (m_Data == null)
                return;

            m_Data.index = 0;
        }
    }
}
