
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
    /// Provides a subset view into an indexed enumerable source.
    /// </summary>
    /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
    /*public*/ readonly struct SubEnumerable<T> : IIndexable<int, T>, ICountable
    {
        /// <summary>
        /// The indexed source this enumerable is referencing.
        /// </summary>
        readonly IIndexable<int, int, T> m_Source;

        /// <summary>
        /// The index of the subset.
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
        /// <param name="source">The indexed source.</param>
        /// <param name="index">The index of the subset.</param>
        /// <param name="count">The size of the subset.</param>
        public SubEnumerable(IIndexable<int, int, T> source, int index, int count)
        {
            m_Source = source;
            m_SubsetIndex = index;
            Count = count;
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

    /// <summary>
    /// Provides a linear enumerator for a <see cref="IIndexable{TIndex, TValue}"/> and <see cref="ICountable"/>.
    /// </summary>
    /// <typeparam name="TEnumerable">The type of the enumerable being enumerated.</typeparam>
    /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
    /*public*/ struct LinearEnumerator<TEnumerable, T>
        where TEnumerable : IIndexable<int, T>, ICountable
    {
        /// <summary>
        /// The enumerable being enumerated.
        /// </summary>
        readonly TEnumerable m_Enumerable;

        /// <summary>
        /// The current index of the enumeration.
        /// </summary>
        int m_Index;

        /// <summary>
        /// Gets the current value in the enumeration.
        /// </summary>
        public T Current => m_Enumerable[m_Index];

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearEnumerator{TEnumerable, T}"/> struct.
        /// </summary>
        /// <param name="enumerable">The enumerable to iterate over.</param>
        public LinearEnumerator(TEnumerable enumerable)
        {
            m_Enumerable = enumerable;
            m_Index = -1;
        }

        /// <summary>
        /// Moves to the next item in the enumeration.
        /// </summary>
        /// <returns><see langword="true"/> if there are more items; otherwise, <see langword="false"/>.</returns>
        public bool MoveNext() => ++m_Index < m_Enumerable.Count;
    }
}
