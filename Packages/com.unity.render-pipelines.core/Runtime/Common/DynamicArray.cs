using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Generic growable array.
    /// </summary>
    /// <typeparam name="T">Type of the array.</typeparam>
    [DebuggerDisplay("Size = {size} Capacity = {capacity}")]
    public class DynamicArray<T> where T : new()
    {
        /// <summary>
        /// The C# array memory used to store the DynamicArray values in. This array's Length may be longer than the DynamicArrayLength. Objects beyond the length should not be referenced.
        /// </summary>
        protected T[] m_Array = null;

        /// <summary>
        /// Number of elements in the array. There may be more elements allocated. Use `capacity` to query the number of allocated items.
        /// </summary>
        public int size { get; protected set; }

        /// <summary>
        /// Allocated size of the array.
        /// </summary>
        public int capacity { get { return m_Array.Length; } }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        /// <summary>
        ///  This keeps track of structural modifications to this array and allows us to raise exceptions when modifying during enumeration
        /// </summary>
        protected internal int version { get; protected set; }
#endif

        /// <summary>
        /// Constructor.
        /// Defaults to a capacity of 32 elements. The size will be 0.
        /// </summary>
        public DynamicArray()
        {
            m_Array = new T[32];
            size = 0;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            version = 0;
#endif
        }

        /// <summary>
        /// Constructor. This constructor allocates memory and sets the size of the array to the specified number of elements.
        /// </summary>
        /// <param name="size">Number of elements. The elements are initialized to the default value of the element type, 0 for integers.</param>
        public DynamicArray(int size)
        {
            m_Array = new T[size];
            this.size = size;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            version = 0;
#endif
        }

        /// <summary>
        /// Constructor. This overload allows you to only allocate memory without setting the size.
        /// </summary>
        /// <param name="capacity">The number of elements to allocate.</param>
        /// <param name="resize">If true, also set the size of the array to the passed in capacity. If false, only allocate data but keep the size at 0.</param>
        public DynamicArray(int capacity, bool resize)
        {
            m_Array = new T[capacity];
            this.size = (resize) ? capacity : 0;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            version = 0;
#endif
        }

        /// <summary>
        /// Constructor. This constructor allocates memory and does a deep copy of the provided array.
        /// </summary>
        /// <param name="deepCopy">Array to be copied</param>
        public DynamicArray(DynamicArray<T> deepCopy)
        {
            m_Array = new T[deepCopy.size];
            size = deepCopy.size;
            Array.Copy(deepCopy.m_Array, m_Array, size);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            version = 0;
#endif
        }

        /// <summary>
        /// Clear the array of all elements.
        /// </summary>
        public void Clear()
        {
            size = 0;
        }

        /// <summary>
        /// Determines whether the DynamicArray contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the DynamicArray.</param>
        /// <returns>true if item is found in the DynamicArray; otherwise, false.</returns>
        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        /// <summary>
        /// Add an element to the array.
        /// </summary>
        /// <param name="value">Element to add to the array.</param>
        /// <returns>The index of the element.</returns>
        public int Add(in T value)
        {
            int index = size;

            // Grow array if needed;
            if (index >= m_Array.Length)
            {
                var newArray = new T[Math.Max(m_Array.Length * 2,1)];
                Array.Copy(m_Array, newArray, m_Array.Length);
                m_Array = newArray;
            }

            m_Array[index] = value;
            size++;
            BumpVersion();
            return index;
        }

        /// <summary>
        /// Adds the elements of the specified collection to the end of the DynamicArray.
        /// </summary>
        /// <param name="array">The array whose elements should be added to the end of the DynamicArray. The array itself cannot be null, but it can contain elements that are null, if type T is a reference type.</param>
        public void AddRange(DynamicArray<T> array)
        {
            // Save the size before reserve. Otherwise things break when self-appending i.e. `a.AddRange(a)`
            var addedSize = array.size;

            Reserve(size + addedSize, true);
            for (int i = 0; i < addedSize; ++i)
                m_Array[size++] = array[i];
            BumpVersion();
        }

        /// <summary>
        /// Insert an item in the DynamicArray.
        /// </summary>
        /// <param name="index">Index where the item should be inserted.</param>
        /// <param name="item">Item to be inserted in the DynamicArray.</param>
        public void Insert(int index, T item)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (index < 0 || index > size)
                throw new IndexOutOfRangeException();
#endif
            if (index == size)
                Add(item);
            else
            {
                Resize(size + 1, true);
                Array.Copy(m_Array, index, m_Array, index + 1, size - index);
                m_Array[index] = item;
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the DynamicArray.
        /// </summary>
        /// <param name="item">The object to remove from the DynamicArray. The value can be null for reference types.</param>
        /// <returns>true if item is successfully removed; otherwise, false. This method also returns false if item was not found in the DynamicArray.</returns>
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index != -1)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes the element at the specified index of the DynamicArray.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        public void RemoveAt(int index)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (index < 0 || index >= size)
                throw new IndexOutOfRangeException();
#endif

            if (index != size - 1)
                Array.Copy(m_Array, index + 1, m_Array, index, size - index - 1);

            size--;
            BumpVersion();
        }

        /// <summary>
        /// Removes a range of elements from the DynamicArray.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range of elements to remove.</param>
        /// <param name="count">The number of elements to remove.</param>
        public void RemoveRange(int index, int count)
        {
            if (count == 0)
                return;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (index < 0 || index >= size || count < 0 || index + count > size)
                throw new ArgumentOutOfRangeException();
#endif

            Array.Copy(m_Array, index + count, m_Array, index, size - index - count);
            size -= count;
            BumpVersion();
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, and returns the zero-based index of the first occurrence within the range of elements in the DynamicArray that starts at the specified index and contains the specified number of elements.
        /// </summary>
        /// <param name="startIndex">The zero-based starting index of the search.</param>
        /// <param name="count">The number of elements in the section to search.</param>
        /// <param name="match">The Predicate delegate that defines the conditions of the element to search for.</param>
        /// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by match, if found; otherwise, -1.</returns>
        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            for (int i = startIndex; i < size; ++i)
            {
                if (match(m_Array[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first occurrence within the range of elements in the DynamicArray that starts at the specified index and contains the specified number of elements.
        /// </summary>
        /// <param name="item">The object to locate in the DynamicArray. The value can be null for reference types.</param>
        /// <param name="index">The zero-based starting index of the search. 0 (zero) is valid in an empty list.</param>
        /// <param name="count">The number of elements in the section to search.</param>
        /// <returns>The index of the first occurrence of the object within the range of elements, or -1 if not found.</returns>
        public int IndexOf(T item, int index, int count)
        {
            for (int i = index; i < size && count > 0; ++i, --count)
            {
                if (m_Array[i].Equals(item))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first occurrence within the range of elements in the DynamicArray that extends from the specified index to the last element.
        /// </summary>
        /// <param name="item">The object to locate in the DynamicArray. The value can be null for reference types.</param>
        /// <param name="index">The zero-based starting index of the search. 0 (zero) is valid in an empty list.</param>
        /// <returns>The zero-based index of the first occurrence of item within the range of elements in the DynamicArray that extends from index to the last element, if found; otherwise, -1.</returns>
        public int IndexOf(T item, int index)
        {
            for (int i = index; i < size; ++i)
            {
                if (m_Array[i].Equals(item))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first occurrence within the entire DynamicArray.
        /// </summary>
        /// <param name="item">The object to locate in the DynamicArray. The value can be null for reference types.</param>
        /// <returns>he zero-based index of the first occurrence of item within the entire DynamicArray, if found; otherwise, -1.</returns>
        public int IndexOf(T item)
        {
            return IndexOf(item, 0);
        }

        /// <summary>
        /// Resize the Dynamic Array.
        /// This will reallocate memory if necessary and set the current size of the array to the provided size.
        /// Note: The memory is not cleared so the elements may contain invalid data.
        /// </summary>
        /// <param name="newSize">New size for the array.</param>
        /// <param name="keepContent">Set to true if you want the current content of the array to be kept.</param>
        public void Resize(int newSize, bool keepContent = false)
        {
            Reserve(newSize, keepContent);
            size = newSize;
            BumpVersion();
        }

        /// <summary>
        /// Resize the Dynamic Array.
        /// This will reallocate memory if necessary and set the current size of the array to the provided size.
        /// The elements are initialized to the default value of the element type, e.g. 0 for integers.
        /// </summary>
        /// <param name="newSize">New size for the array.</param>
        public void ResizeAndClear(int newSize)
        {
            if (newSize > m_Array.Length)
            {
                // Reserve will allocate a whole new array that is cleared as part of the allocation
                Reserve(newSize, false);
            }
            else
            {
                // We're not reallocating anything we need to clear the old values to the default.
                Array.Clear(m_Array, 0, newSize);
            }
            size = newSize;
            BumpVersion();
        }

        /// <summary>
        /// Sets the total number of elements the internal data structure can hold without resizing.
        /// </summary>
        /// <param name="newCapacity">New capacity for the array.</param>
        /// <param name="keepContent">Set to true if you want the current content of the array to be kept.</param>
        public void Reserve(int newCapacity, bool keepContent = false)
        {
            if (newCapacity > m_Array.Length)
            {
                if (keepContent)
                {
                    var newArray = new T[newCapacity];
                    Array.Copy(m_Array, newArray, m_Array.Length);
                    m_Array = newArray;
                }
                else
                {
                    m_Array = new T[newCapacity];
                }
            }
        }

        /// <summary>
        /// ref access to an element.
        /// </summary>
        /// <param name="index">Element index</param>
        /// <value>The requested element.</value>
        public ref T this[int index]
        {
            get
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (index < 0 || index >= size)
                    throw new IndexOutOfRangeException();
#endif
                return ref m_Array[index];
            }
        }

        /// <summary>
        /// Implicit conversion to regular array.
        /// </summary>
        /// <param name="array">Input DynamicArray.</param>
        /// <returns>The internal array.</returns>
        [Obsolete("This is deprecated because it returns an incorrect value. It may returns an array with elements beyond the size. Please use Span/ReadOnly if you want safe raw access to the DynamicArray memory.",false)]
        public static implicit operator T[](DynamicArray<T> array) => array.m_Array;

        /// <summary>
        /// Implicit conversion to ReadOnlySpan.
        /// </summary>
        /// <param name="array">Input DynamicArray.</param>
        /// <returns>The internal array.</returns>
        public static implicit operator ReadOnlySpan<T>(DynamicArray<T> array) => new ReadOnlySpan<T>(array.m_Array, 0, array.size);

        /// <summary>
        /// Implicit conversion to Span.
        /// </summary>
        /// <param name="array">Input DynamicArray.</param>
        /// <returns>The internal array.</returns>
        public static implicit operator Span<T>(DynamicArray<T> array) => new Span<T>(array.m_Array, 0, array.size);

        /// <summary>
        /// IEnumerator-like struct used to loop over this entire array. See the IEnumerator docs for more info:
        /// <a href="https://docs.microsoft.com/en-us/dotnet/api/system.collections.ienumerator">IEnumerator</a>
        /// </summary>
        /// <remarks>
        /// This struct intentionally does not explicitly implement the IEnumarable/IEnumerator interfaces it just follows
        /// the same function signatures. This means the duck typing used by <c>foreach</c> on the compiler level will
        /// pick it up as IEnumerable but at the same time avoids generating Garbage.
        /// For more info, see the C# language specification of the <c>foreach</c> statement.
        /// </remarks>
        /// <seealso cref="RangeIterator"/>
        public struct Iterator
        {
            private readonly DynamicArray<T> owner;
            private int index;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            private int localVersion;
#endif

            /// <summary>
            /// Creates an iterator to iterate over an array.
            /// </summary>
            /// <param name="setOwner">The array to iterate over.</param>
            /// <exception cref="ArgumentNullException">Thrown if the array is null.</exception>
            public Iterator(DynamicArray<T> setOwner)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (setOwner == null)
                    throw new ArgumentNullException();
#endif
                owner = setOwner;
                index = -1;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                localVersion = owner.version;
#endif
            }

            /// <summary>
            /// Gets the element in the DynamicArray at the current position of the iterator.
            /// </summary>
            public ref T Current
            {
                get
                {
                    return ref owner[index];
                }
            }

            /// <summary>
            /// Advances the iterator to the next element of the DynamicArray.
            /// </summary>
            /// <returns>Returns <c>true</c> if the iterator has successfully advanced to the next element; <c>false</c> if the iterator has passed the end of the DynamicArray.</returns>
            /// <exception cref="InvalidOperationException">An operation changed the DynamicArray after the creation of this iterator.</exception>
            public bool MoveNext()
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (owner.version != localVersion)
                {
                    throw  new InvalidOperationException("DynamicArray was modified during enumeration");
                }
#endif
                index++;
                return index < owner.size;
            }

            /// <summary>
            /// Sets the iterator to its initial position, which is before the first element in the DynamicArray.
            /// </summary>
            public void Reset()
            {
                index = -1;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through of this array.
        /// See the IEnumerable docs for more info: <a href="https://docs.microsoft.com/en-us/dotnet/api/system.collections.ienumerable" >IEnumerable</a>
        /// </summary>
        /// <remarks>
        /// The returned struct intentionally does not explicitly implement the IEnumarable/IEnumerator interfaces it just follows
        /// the same function signatures. This means the duck typing used by <c>foreach</c> on the compiler level will
        /// pick it up as IEnumerable but at the same time avoids generating Garbage.
        /// For more info, see the C# language specification of the <c>foreach</c> statement.
        /// </remarks>
        /// <returns>Iterator pointing before the first element in the array.</returns>
        public Iterator GetEnumerator()
        {
            return new Iterator(this);
        }

        /// <summary>
        /// IEnumerable-like struct used to iterate through a subsection of this array.
        /// See the IEnumerable docs for more info: <a href="https://docs.microsoft.com/en-us/dotnet/api/system.collections.ienumerable">IEnumerable</a>
        /// </summary>
        /// <remarks>
        /// This struct intentionally does not explicitly implement the IEnumarable/IEnumerator interfaces it just follows
        /// the same function signatures. This means the duck typing used by <c>foreach</c> on the compiler level will
        /// pick it up as IEnumerable but at the same time avoids generating Garbage.
        /// For more info, see the C# language specification of the <c>foreach</c> statement.
        /// </remarks>
        /// <seealso cref="SubRange"/>
        public struct RangeEnumerable
        {
            /// <summary>
            /// IEnumerator-like struct used to iterate through a subsection of this array.
            /// See the IEnumerator docs for more info: <a href="https://docs.microsoft.com/en-us/dotnet/api/system.collections.ienumerator">IEnumerator</a>
            /// </summary>
            /// <remarks>
            /// This struct intentionally does not explicitly implement the IEnumarable/IEnumerator interfaces it just follows
            /// the same function signatures. This means the duck typing used by <c>foreach</c> on the compiler level will
            /// pick it up as <c>IEnumarable</c> but at the same time avoids generating Garbage.
            /// For more info, see the C# language specification of the <c>foreach</c> statement.
            /// </remarks>
            /// <seealso cref="SubRange"/>
            public struct RangeIterator
            {
                private readonly DynamicArray<T> owner;
                private int index;
                private int first;
                private int last;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                private int localVersion;
#endif


                /// <summary>
                /// Create an iterator to iterate over the given range in the array.
                /// </summary>
                /// <param name="setOwner">The array to iterate over.</param>
                /// <param name="first">The index of the first item in the array.</param>
                /// <param name="numItems">The number of array members to iterate through.</param>
                /// <exception cref="ArgumentNullException">Thrown if the array is null.</exception>
                public RangeIterator(DynamicArray<T> setOwner, int first, int numItems)
                {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    if (setOwner == null)
                        throw new ArgumentNullException();
                    if (first < 0 || first > setOwner.size || (first + numItems) > setOwner.size)
                        throw new IndexOutOfRangeException();
#endif
                    owner = setOwner;
                    this.first = first;
                    index = first-1;
                    last = first + numItems;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    localVersion = owner.version;
#endif
                }

                /// <summary>
                /// Gets the element in the DynamicArray at the current position of the iterator.
                /// </summary>
                public ref T Current
                {
                    get
                    {
                        return ref owner[index];
                    }
                }

                /// <summary>
                /// Advances the iterator to the next element of the DynamicArray.
                /// </summary>
                /// <returns>Returs <c>true</c> if the iterator successfully advanced to the next element; returns <c>false</c> if the iterator has passed the end of the range.</returns>
                /// <exception cref="InvalidOperationException">The DynamicArray was modified after the iterator was created.</exception>
                public bool MoveNext()
                {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    if (owner.version != localVersion)
                    {
                        throw  new InvalidOperationException("DynamicArray was modified during enumeration");
                    }
#endif
                    index++;
                    return index < last;
                }

                /// <summary>
                /// Sets the iterator to its initial position, which is before the first element in the range.
                /// </summary>
                public void Reset()
                {
                    index = first-1;
                }
            }

            /// <summary>
            /// The iterator associated with this Enumerable.
            /// </summary>
            public RangeIterator iterator;

            /// <summary>
            /// Returns an enumerator that iterates through this array.
            /// </summary>
            /// <remarks>
            /// The returned struct intentionally does not explicitly implement the IEnumarable/IEnumerator interfaces it just follows
            /// the same function signatures. This means the duck typing used by <c>foreach</c> on the compiler level will
            /// pick it up as IEnumerable but at the same time avoids generating Garbage.
            /// For more info, see the C# language specification of the <c>foreach</c> statement.
            /// </remarks>
            /// <returns>Iterator pointing before the first element in the range.</returns>
            public RangeIterator GetEnumerator()
            {
                return iterator;
            }
        }

        /// <summary>
        /// Returns an IEnumeralbe-Like object that iterates through a subsection of this array.
        /// </summary>
        /// <remarks>
        /// The returned struct intentionally does not explicitly implement the IEnumarable/IEnumerator interfaces it just follows
        /// the same function signatures. This means the duck typing used by <c>foreach</c> on the compiler level will
        /// pick it up as IEnumerable but at the same time avoids generating Garbage.
        /// For more info, see the C# language specification of the <c>foreach</c> statement.
        /// </remarks>
        /// <param name="first">The index of the first item</param>
        /// <param name="numItems">The number of items to iterate</param>
        /// <returns><c>RangeEnumerable</c> that can be used to enumerate the given range.</returns>
        /// <seealso cref="RangeIterator"/>
        public RangeEnumerable SubRange(int first, int numItems)
        {
            RangeEnumerable r = new RangeEnumerable { iterator = new RangeEnumerable.RangeIterator(this, first, numItems) };
            return r;
        }

        /// <summary>
        /// Increments the internal version counter.
        /// </summary>
        protected internal void BumpVersion()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            version++;
#endif
        }

        /// <summary>
        /// Delegate for custom sorting comparison.
        /// </summary>
        /// <param name="x">First object.</param>
        /// <param name="y">Second object.</param>
        /// <returns>-1 if x smaller than y, 1 if x bigger than y and 0 otherwise.</returns>
        public delegate int SortComparer(T x, T y);
    }

    /// <summary>
    /// Extension class for DynamicArray
    /// </summary>
    public static class DynamicArrayExtensions
    {
        static int Partition<T>(Span<T> data, int left, int right) where T : IComparable<T>, new()
        {
            var pivot = data[left];

            --left;
            ++right;
            while (true)
            {
                var c = 0;
                var lvalue = default(T);
                do
                {
                    ++left;
                    lvalue = data[left];
                    c = lvalue.CompareTo(pivot);
                }
                while (c < 0);

                var rvalue = default(T);
                do
                {
                    --right;
                    rvalue = data[right];
                    c = rvalue.CompareTo(pivot);
                }
                while (c > 0);

                if (left < right)
                {
                    data[right] = lvalue;
                    data[left] = rvalue;
                }
                else
                {
                    return right;
                }
            }
        }

        static void QuickSort<T>(Span<T> data, int left, int right) where T : IComparable<T>, new()
        {
            if (left < right)
            {
                int pivot = Partition(data, left, right);

                if (pivot >= 1)
                    QuickSort(data, left, pivot);

                if (pivot + 1 < right)
                    QuickSort(data, pivot + 1, right);
            }
        }

        // C# SUCKS
        // Had to copy paste because it's apparently impossible to pass a sort delegate where T is Comparable<T>, otherwise some boxing happens and allocates...
        // So two identical versions of the function, one with delegate but no Comparable and the other with just the comparable.
        static int Partition<T>(Span<T> data, int left, int right, DynamicArray<T>.SortComparer comparer) where T : new()
        {
            var pivot = data[left];

            --left;
            ++right;
            while (true)
            {
                var c = 0;
                var lvalue = default(T);
                do
                {
                    ++left;
                    lvalue = data[left];
                    c = comparer(lvalue, pivot);
                }
                while (c < 0);

                var rvalue = default(T);
                do
                {
                    --right;
                    rvalue = data[right];
                    c = comparer(rvalue, pivot);
                }
                while (c > 0);

                if (left < right)
                {
                    data[right] = lvalue;
                    data[left] = rvalue;
                }
                else
                {
                    return right;
                }
            }
        }

        static void QuickSort<T>(Span<T> data, int left, int right, DynamicArray<T>.SortComparer comparer) where T : new()
        {
            if (left < right)
            {
                int pivot = Partition(data, left, right, comparer);

                if (pivot >= 1)
                    QuickSort(data, left, pivot, comparer);

                if (pivot + 1 < right)
                    QuickSort(data, pivot + 1, right, comparer);
            }
        }

        /// <summary>
        /// Perform a quick sort on the DynamicArray
        /// </summary>
        /// <typeparam name="T">Type of the array.</typeparam>
        /// <param name="array">Array on which to perform the quick sort.</param>
        public static void QuickSort<T>(this DynamicArray<T> array) where T : IComparable<T>, new()
        {
            QuickSort<T>(array, 0, array.size - 1);
            array.BumpVersion();
        }

        /// <summary>
        /// Perform a quick sort on the DynamicArray
        /// </summary>
        /// <typeparam name="T">Type of the array.</typeparam>
        /// <param name="array">Array on which to perform the quick sort.</param>
        /// <param name="comparer">Comparer used for sorting.</param>
        public static void QuickSort<T>(this DynamicArray<T> array, DynamicArray<T>.SortComparer comparer) where T : new()
        {
            QuickSort<T>(array, 0, array.size - 1, comparer);
            array.BumpVersion();
        }
    }
}
