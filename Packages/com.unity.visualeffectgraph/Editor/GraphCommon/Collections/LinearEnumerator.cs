using System;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Represents an object that can be indexed using a single key.
    /// </summary>
    /// <typeparam name="TIndex">The type of the index key.</typeparam>
    /// <typeparam name="TValue">The type of the value associated with the index.</typeparam>
    /*public*/ interface IIndexable<TIndex, TValue>
    {
        /// <summary>
        /// Gets the value associated with the specified index.
        /// </summary>
        /// <param name="index">The index for the value.</param>
        /// <value>The value at the specified index.</value>
        public TValue this[TIndex index] { get; }
    }

    /// <summary>
    /// Represents an object that can be indexed using two keys.
    /// </summary>
    /// <typeparam name="TIndex0">The type of the first index key.</typeparam>
    /// <typeparam name="TIndex1">The type of the second index key.</typeparam>
    /// <typeparam name="TValue">The type of the value associated with the indices.</typeparam>
    /*public*/ interface IIndexable<TIndex0, TIndex1, TValue>
    {
        /// <summary>
        /// Gets the value associated with the specified indices.
        /// </summary>
        /// <param name="index0">The first index key.</param>
        /// <param name="index1">The second index key.</param>
        /// <value>The value at the specified indices.</value>
        public TValue this[TIndex0 index0, TIndex1 index1] { get; }
    }

    /// <summary>
    /// Represents an object that provides a count of its items.
    /// </summary>
    /*public*/ interface ICountable
    {
        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        public int Count { get; }
    }

    /// <summary>
    /// Minimal value-type enumerator contract — Current + MoveNext.
    /// </summary>
    /*public*/ interface IValueEnumerator<T>
    {
        T Current { get; }
        bool MoveNext();
    }

    /// <summary>
    /// Generic enumerator that wraps an inner value-type enumerator yielding lightweight handles
    /// of type <typeparamref name="TId"/> and resolves each one to a <typeparamref name="TValue"/>.
    /// </summary>
    /*public*/ struct ResolvingEnumerator<TId, TValue, TInnerEnum> : IValueEnumerator<TValue>
        where TInnerEnum : struct, IValueEnumerator<TId>
    {
        readonly IIndexable<TId, TValue> m_Provider;
        TInnerEnum                       m_Inner;

        public ResolvingEnumerator(IIndexable<TId, TValue> provider, TInnerEnum inner)
        {
            m_Provider = provider;
            m_Inner    = inner;
        }

        public TValue Current  => m_Provider[m_Inner.Current];
        public bool   MoveNext() => m_Inner.MoveNext();
    }

    /// <summary>
    /// Provides a subset view into a single sublist of an <see cref="IMultiList{T}"/>.
    /// Iterating this view via <c>foreach</c> yields a <see cref="VersionedSublistEnumerator{T}"/>
    /// that throws on concurrent modification of the underlying sublist.
    /// </summary>
    /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
    /*public*/ readonly struct SubEnumerable<T> : IIndexable<int, T>, ICountable
    {
        /// <summary>
        /// The backing multi-list.
        /// </summary>
        readonly IMultiList<T> m_Source;

        /// <summary>
        /// The index of the subset (the sublist index in <see cref="m_Source"/>).
        /// </summary>
        readonly int m_SubsetIndex;

        /// <summary>
        /// Gets the number of items in the subset.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Gets the item at the specified index within the subset.
        /// </summary>
        /// <param name="index">The index within the subset.</param>
        /// <value>The item at the specified index.</value>
        public T this[int index] => m_Source[m_SubsetIndex, index];

        /// <summary>
        /// Initializes a new instance of the <see cref="SubEnumerable{T}"/> struct.
        /// </summary>
        /// <param name="source">The backing <see cref="IMultiList{T}"/>.</param>
        /// <param name="index">The sublist index within the backing list.</param>
        /// <param name="count">The size of the subset.</param>
        public SubEnumerable(IMultiList<T> source, int index, int count)
        {
            m_Source = source;
            m_SubsetIndex = index;
            Count = count;
        }

        /// <summary>
        /// Returns an enumerator that walks the subset and throws
        /// <see cref="InvalidOperationException"/> if the underlying sublist is mutated
        /// during iteration.
        /// </summary>
        public VersionedSublistEnumerator<T> GetEnumerator() => new(m_Source, m_SubsetIndex, Count);
    }

    /// <summary>
    /// Enumerator over a single sublist of an <see cref="IMultiList{T}"/>. Snapshots the
    /// sublist's version at construction and throws <see cref="InvalidOperationException"/> on
    /// <see cref="MoveNext"/> if the sublist has been mutated since.
    /// </summary>
    /// <typeparam name="T">The element type of the sublist.</typeparam>
    /*public*/ struct VersionedSublistEnumerator<T> : IValueEnumerator<T>
    {
        readonly IMultiList<T> m_Source;
        readonly int           m_ListIndex;
        readonly int           m_Count;
        readonly uint          m_SnapshotVersion;
        int                    m_Index;

        public VersionedSublistEnumerator(IMultiList<T> source, int listIndex, int count)
        {
            m_Source          = source;
            m_ListIndex       = listIndex;
            m_Count           = count;
            m_SnapshotVersion = source.GetListVersion(listIndex);
            m_Index           = -1;
        }

        public T Current => m_Source[m_ListIndex, m_Index];

        public bool MoveNext()
        {
            if (m_Source.GetListVersion(m_ListIndex) != m_SnapshotVersion)
                throw new InvalidOperationException(
                    $"Sublist {m_ListIndex} of IMultiList<{typeof(T).Name}> was modified during enumeration.");
            return ++m_Index < m_Count;
        }
    }

    /// <summary>
    /// Provides an indirect view into an indexed enumerable source.
    /// </summary>
    /*public*/ readonly struct IndirectEnumerable : IIndexable<int, int>, ICountable
    {
        /// <summary>
        /// The indirection source for the enumerable.
        /// </summary>
        readonly IIndexable<int, int> m_Indirection;

        /// <summary>
        /// Gets the number of items in the enumerable.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Gets the item at the specified index.
        /// </summary>
        /// <param name="index">The index in the enumerable.</param>
        /// <value>The item at the specified index.</value>
        public int this[int index] => m_Indirection[index];

        /// <summary>
        /// Initializes a new instance of the <see cref="IndirectEnumerable"/> struct.
        /// </summary>
        /// <param name="indirection">The indexed source for indirection.</param>
        /// <param name="count">The number of items in the enumerable.</param>
        public IndirectEnumerable(IIndexable<int, int> indirection, int count)
        {
            m_Indirection = indirection;
            Count = count;
        }
    }
}
