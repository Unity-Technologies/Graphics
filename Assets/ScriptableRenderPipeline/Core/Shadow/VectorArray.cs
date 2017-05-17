using System;

namespace UnityEngine.Experimental
{
    public struct VectorArray<T>
    {
        public const uint k_InvalidIdx = uint.MaxValue;
        private T[]  m_array;           // backing storage
        private uint m_offset;          // offset into backing storage
        private uint m_count;           // active slots, <= m_array.Length
        private bool m_clearToDefault;  // if true, freed up slots will be default initialized

        public VectorArray(uint capacity, bool clearToDefault)
        {
            m_array          = new T[capacity];
            m_offset         = 0;
            m_count          = capacity;
            m_clearToDefault = clearToDefault;
        }

        public VectorArray(T[] array, uint offset, uint count, bool clearToDefault)
        {
            m_array          = array;
            m_offset         = offset;
            m_count          = count;
            m_clearToDefault = clearToDefault;
        }

        public VectorArray(ref VectorArray<T> vec, uint offset, uint count)
        {
            m_array          = vec.m_array;
            m_offset         = vec.m_offset + offset;
            m_count          = count;
            m_clearToDefault = vec.m_clearToDefault;
        }

        // Reset fill count, but keep storage
        public void Reset()
        {
            if (m_clearToDefault)
            {
                for (uint i = 0; i < m_count; ++i)
                    this[i] = default(T);
            }
            m_count = 0;
        }

        // Reset fill count and reserve enough storage
        public void Reset(uint capacity)
        {
            if (m_array.Length < (m_offset + capacity))
            {
                m_array = new T[capacity];
                m_offset = 0;
                m_count = 0;
            }
            else
                Reset();
        }

        // Reserve additional storage
        public void Reserve(uint capacity)
        {
            if (m_offset + m_count + capacity <= m_array.Length)
                return;

            if (m_count == 0)
            {
                m_array = new T[capacity];
            }
            else
            {
                T[] tmp = new T[m_count + capacity];
                Array.Copy(m_array, m_offset, tmp, 0, m_count);
                m_array = tmp;
            }
            m_offset = 0;
        }

        // Resize array, either by cutting off at the end, or adding potentially default initialized slots
        // Resize will modify the count member as well, whereas reserve won't.
        public void Resize(uint size)
        {
            if (size > m_count)
            {
                uint residue = size - m_count;
                Reserve(residue);
            }
            else if (m_clearToDefault)
            {
                for (uint i = size; i < m_count; ++i)
                    this[i] = default(T);
            }
            m_count = size;
        }

        public delegate void Cleanup(T obj);
        // Same as Resize(), with an additional delegate to do any cleanup on the potentially freed slots
        public void Resize(uint size, Cleanup cleanupDelegate)
        {
            for (uint i = size; i < m_count; ++i)
                cleanupDelegate(this[i]);

            Resize(size);
        }

        // Same as Reset(), with an additional delegate to do any cleanup on the potentially freed slots
        public void Reset(Cleanup cleanupDelegate)
        {
            for (uint i = 0; i < m_count; ++i)
                cleanupDelegate(this[i]);

            Reset();
        }

        // Same as Reset( uint capacity ), with an additional delegate to do any cleanup on the potentially freed slots
        public void Reset(uint capacity, Cleanup cleanupDelegate)
        {
            for (uint i = 0; i < m_count; ++i)
                cleanupDelegate(this[i]);

            Reset(capacity);
        }

        // Add obj and reallocate if necessary. Returns the index where the object was added.
        public uint Add( T obj )
        {
            Reserve(1);
            uint idx = m_count;
            this[idx] = obj;
            m_count++;
            return idx;
        }

        // Add multiple objects and reallocate if necessary. Returns the index where the first object was added.
        public uint Add(T[] objs, uint count)
        {
            Reserve(count);
            return AddUnchecked(objs, count);
        }

        public uint Add(ref VectorArray<T> vec)
        {
            Reserve(vec.Count());
            return AddUnchecked(ref vec);
        }

        // Adds the object if it does not exist in the container, yet
        public uint AddUnique( T obj )
        {
            Reserve( 1 );
            return AddUniqueUnchecked( obj );
        }

        public uint AddUnique( T[] objs, uint count )
        {
            Reserve( count );
            return AddUniqueUnchecked( objs, count );
        }

        public uint AddUnique( ref VectorArray<T> vec )
        {
            Reserve( vec.Count() );
            return AddUniqueUnchecked( ref vec );
        }

        // Add an object without doing size checks. Make sure to call Reserve( capacity ) before using this.
        public uint AddUnchecked( T obj )
        {
            uint idx = m_count;
            this[idx] = obj;
            m_count++;
            return idx;
        }

        // Add multiple objects without doing size checks. Make sure to call Reserve( capacity ) before using this.
        public uint AddUnchecked(T[] objs, uint count)
        {
            uint idx = m_count;
            Array.Copy(objs, 0, m_array, m_offset + idx, count);
            m_count += count;
            return idx;
        }

        public uint AddUnchecked(ref VectorArray<T> vec)
        {
            uint cnt = vec.Count();
            uint idx = m_count;
            Array.Copy(vec.m_array, vec.m_offset, m_array, m_offset + idx, cnt);
            m_count += cnt;
            return idx;
        }

        // Adds the object if it does not exist in the container, yet
        public uint AddUniqueUnchecked( T obj )
        {
            if( !Contains( obj ) )
                return Add( obj );
            return k_InvalidIdx;
        }

        public uint AddUniqueUnchecked( T[] objs, uint count )
        {
            uint res = k_InvalidIdx;
            for( uint i = 0; i < count; ++i )
            {
                uint tmp = AddUniqueUnchecked( objs[i] );
                res = res <= tmp ? res : tmp;
            }
            return res;
        }

        public uint AddUniqueUnchecked( ref VectorArray<T> vec )
        {
            uint res = k_InvalidIdx;
            for( uint i = 0, cnt = vec.Count(); i < cnt; ++i )
            {
                uint tmp = AddUniqueUnchecked( vec[i] );
                res = res <= tmp ? res : tmp;
            }
            return res;
        }


        // Purge count number of elements from the end of the array.
        public void Purge(uint count)
        {
            Resize(count > m_count ? 0 : (m_count - count));
        }

        // Same as Purge with an additional cleanup delegate.
        public void Purge(uint count, Cleanup cleanupDelegate)
        {
            Resize(count > m_count ? 0 : (m_count - count), cleanupDelegate);
        }

        // Copies the active elements to the destination. destination.Length must be large enough to hold all values.
        public void CopyTo(T[] destination, int destinationStart, out uint count)
        {
            count = m_count;
            Array.Copy(m_array, m_offset, destination, destinationStart, count);
        }

        // Swaps two elements in the array doing bounds checks.
        public void Swap(uint first, uint second)
        {
            if (first >= m_count || second >= m_count)
                throw new System.ArgumentException("Swap indices are out of range.");

            SwapUnchecked(first, second);
        }

        // Swaps two elements without any checks.
        public void SwapUnchecked(uint first, uint second)
        {
            T tmp = this[first];
            this[first] = this[second];
            this[second] = tmp;
        }

        // Extracts information from objects contained in the array and stores them in the destination array.
        // Conversion is performed by the extractor delegate for each object.
        // The destination array must be large enough to hold all values.
        // The output parameter count will contain the number of actual objects written out to the destination array.
        public delegate U Extractor<U>(T obj);
        public void ExtractTo<U>(U[] destination, int destinationStart, out uint count, Extractor<U> extractorDelegate)
        {
            if (destination.Length < m_count)
                throw new System.ArgumentException("Destination array is too small for source array.");

            count = m_count;
            for (uint i = 0; i < m_count; ++i)
                destination[destinationStart + i] = extractorDelegate(this[i]);
        }

        public void ExtractTo<U>(ref VectorArray<U> destination, Extractor<U> extractorDelegate)
        {
            destination.Reserve(m_count);
            for (uint i = 0; i < m_count; ++i)
                destination.AddUnchecked(extractorDelegate(this[i]));
        }

        // Cast to array. The output parameter will contain the array offset to valid members and the number of active elements.
        // Accessing array elements outside of this interval will lead to undefined behavior.
        public T[] AsArray(out uint offset, out uint count)
        {
            offset = m_offset;
            count  = m_count;
            return m_array;
        }

        // Array access. No bounds checking here, make sure you know what you're doing.
        public T this[uint index]
        {
            get { return m_array[m_offset + index]; }
            set { m_array[m_offset + index] = value; }
        }

        // Returns the number of active elements in the vector.
        public uint Count()
        {
            return m_count;
        }

        // Returns the total capacity of accessible backing storage
        public uint CapacityTotal()
        {
            return (uint)m_array.Length - m_offset;
        }

        // Return the number of slots that can still be added before reallocation of the backing storage becomes necessary.
        public uint CapacityAvailable()
        {
            return (uint)m_array.Length - m_offset - m_count;
        }

        // Default sort the active array content.
        public void Sort()
        {
            Debug.Assert(m_count <= int.MaxValue && m_offset <= int.MaxValue);
            Array.Sort(m_array, (int)m_offset, (int)m_count);
        }

        // Sort according to some comparer
        public void Sort(System.Collections.Generic.IComparer<T> comparer)
        {

            Debug.Assert(m_count <= int.MaxValue && m_offset <= int.MaxValue);
            Array.Sort(m_array, (int)m_offset, (int)m_count, comparer);
        }

        // Returns true if the element matches the designator according to the comparator. idx will hold the index to the first matched object in the array.
        public delegate bool Comparator<U>(ref U designator, ref T obj);
        public bool FindFirst<U>(out uint idx, ref U designator, Comparator<U> compareDelegate)
        {
            for (idx = 0; idx < m_count; ++idx)
            {
                T obj = this[idx];
                if (compareDelegate(ref designator, ref obj))
                    return true;
            }
            idx = k_InvalidIdx;
            return false;
        }

        // Returns true if the element matches the designator using the types native compare method. idx will hold the index to the first matched object in the array.
        public bool FindFirst(out uint idx, ref T designator)
        {
            for (idx = 0; idx < m_count; ++idx)
            {
                if (this[idx].Equals(designator))
                    return true;
            }
            idx = k_InvalidIdx;
            return false;
        }
        // Returns true if the container already contains the element
        public bool Contains( T designator )
        {
            uint idx;
            return FindFirst( out idx, ref designator );
        }
        // Returns true if the container already contains the element
        public bool Contains<U>( U designator, Comparator<U> compareDelegate )
        {
            uint idx;
            return FindFirst( out idx, ref designator, compareDelegate );
        }
        // Returns a vector representing a subrange. Contents are shared, not duplicated.
        public VectorArray<T> Subrange(uint offset, uint count)
        {
            return new VectorArray<T>(m_array, m_offset + offset, count, m_clearToDefault);
        }
    }
}
