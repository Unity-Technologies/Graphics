namespace UnityEditor.Rendering.HighDefinition
{
    using System;
    using static IndexGraph;

    internal static class IndexGraph
    {
        public struct NodeIndex
        {
            internal NodeIndex(int index) => this.index = index;

            internal int index;
        }

        public struct EdgeIndex
        {
            public EdgeIndex(int index) => this.index = index;

            internal int index;
        }

        public struct Edge<E>
        {
            public NodeIndex from;
            public NodeIndex to;
            public E edge;

            public Edge(NodeIndex from, NodeIndex to, E edge)
            {
                this.from = from;
                this.to = to;
                this.edge = edge;
            }
        }

        public struct EdgeRefEnumerator<E> : IRefEnumerator<Edge<E>>
        {
            ArrayListRefEnumerator<Edge<E>> m_En;

            public EdgeRefEnumerator(ArrayList<Edge<E>> source)
                => m_En = new ArrayListRefEnumerator<Edge<E>>(source);

            public ref readonly Edge<E> current => ref m_En.current;
            public bool MoveNext() => m_En.MoveNext();
            public void Reset() => m_En.Reset();
        }

        public struct EdgeMutEnumerator<E> : IMutEnumerator<Edge<E>>
        {
            ArrayListMutEnumerator<Edge<E>> m_En;

            public EdgeMutEnumerator(ArrayList<Edge<E>> source)
                => m_En = new ArrayListMutEnumerator<Edge<E>>(source);

            public ref Edge<E> current => ref m_En.current;
            public bool MoveNext() => m_En.MoveNext();
            public void Reset() => m_En.Reset();
        }

        public struct NodeRefEnumerator<N> : IRefEnumerator<N>
            where N: struct
        {
            ArrayListRefEnumerator<N> m_En;

            public NodeRefEnumerator(ArrayList<N> source)
                => m_En = new ArrayListRefEnumerator<N>(source);

            public ref readonly N current => ref m_En.current;
            public bool MoveNext() => m_En.MoveNext();
            public void Reset() => m_En.Reset();
        }

        public struct NodeMutEnumerator<N> : IMutEnumerator<N>
            where N : struct
        {
            ArrayListMutEnumerator<N> m_En;

            public NodeMutEnumerator(ArrayList<N> source)
                => m_En = new ArrayListMutEnumerator<N>(source);

            public ref N current => ref m_En.current;
            public bool MoveNext() => m_En.MoveNext();
            public void Reset() => m_En.Reset();
        }
    }


    /// <summary>
    /// Graph stored as index based nodes and edges.
    ///
    /// Remove operations are not conservative: <see cref="NodeIndex"/> and <see cref="EdgeIndex"/> may change.
    /// </summary>
    /// <typeparam name="N"></typeparam>
    /// <typeparam name="E"></typeparam>
    internal class IndexGraph<N, E>
        where N: struct
    {
        ArrayList<N> m_Nodes;
        ArrayList<Edge<E>> m_Edges;

        public EdgeRefEnumerator<E> edges => new EdgeRefEnumerator<E>(m_Edges);
        public EdgeMutEnumerator<E> edgesMut => new EdgeMutEnumerator<E>(m_Edges);
        public NodeRefEnumerator<N> nodes => new NodeRefEnumerator<N>(m_Nodes);
        public NodeMutEnumerator<N> nodesMut => new NodeMutEnumerator<N>(m_Nodes);

        public NodeIndex AddNode(in N node)
        {
            var index = m_Nodes.count;
            m_Nodes.Add(node);
            return new NodeIndex(index);
        }

        public ref readonly N GetNode(in NodeIndex index)
            => ref m_Nodes.Get(index.index);

        public ref N GetMutNode(in NodeIndex index)
            => ref m_Nodes.GetMut(index.index);

        public EdgeIndex AddEdge(NodeIndex from, NodeIndex to, in E edge)
        {
            var index = m_Edges.count;
            m_Edges.Add(new Edge<E>(from, to, edge));
            return new EdgeIndex(index);
        }

        public ref readonly Edge<E> GetEdge(EdgeIndex index) => ref m_Edges.Get(index.index);
        public ref Edge<E> GetMutEdge(EdgeIndex index) => ref m_Edges.GetMut(index.index);
    }

    //
    // Enumerator Utilities
    //

    public interface IRefEnumerator<T>
    {
        ref readonly T current { get; }

        bool MoveNext();
        void Reset();
    }

    public interface IMutEnumerator<T>
    {
        ref T current { get; }

        bool MoveNext();
        void Reset();
    }

    public interface IInFunc<T1, R>
    {
        R Execute(in T1 t1);
    }

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

        public int count => data.count;

        public int Add(in T value)
        {
            var index = data.count;
            data.count++;

            GrowIfRequiredFor(data.count);

            data.storage[index] = value;
            return index;
        }

        public unsafe ref readonly T GetUnsafe(int index) => ref span[index];

        public ref readonly T Get(int index)
        {
            if (index < 0 || index >= data.count)
                throw new ArgumentOutOfRangeException(nameof(index));

            unsafe { return ref GetUnsafe(index); }
        }

        public unsafe ref T GetMutUnsafe(int index) => ref spanMut[index];

        public ref T GetMut(int index)
        {
            if (index < 0 || index >= data.count)
                throw new ArgumentOutOfRangeException(nameof(index));

            unsafe { return ref GetMutUnsafe(index); }
        }

        public unsafe void RemoveSwapBackUnsafe(int index)
        {
            var spanMut = this.spanMut;
            spanMut[index] = spanMut[data.count - 1];
            data.count--;
        }

        public void RemoveSwapBack(int index)
        {
            if (index < 0 || index >= data.count)
                throw new ArgumentOutOfRangeException(nameof(index));

            unsafe { RemoveSwapBackUnsafe(index); }
        }

        void GrowIfRequiredFor(int size)
        {
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
                index = 0
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
                index = 0
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
