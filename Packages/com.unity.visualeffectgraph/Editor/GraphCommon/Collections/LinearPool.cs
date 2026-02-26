using System;
using System.Collections;
using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    struct LinearValueChunkId<T>
    {
        public int Start { get; set; }
        public int Length { get; set; }
        public int End => Start + Length; // not inclusive

        public LinearValueChunkId(int start, int length)
        {
            if (start < 0)
                throw new ArgumentException();
            if (length <= 0)
                throw new ArgumentException();

            Start = start;
            Length = length;
        }
    }

    class LinearValuePool<T> : IEnumerable<T>, ICloneable
    {
        T[] m_Items;
        List<LinearValueChunkId<T>> m_FreeChunks;
        int m_AcquiredCount;

        public int Capacity => m_Items.Length;
        public int FreeChunkCount => m_FreeChunks.Count; // allow to check for fragmentation
        public int AcquiredCount => m_AcquiredCount;
        public int FreeCount => m_Items.Length - m_AcquiredCount;

        public LinearValuePool(int capacity = 32)
        {
            m_Items = new T[capacity];
            m_FreeChunks = new();  
            if (capacity > 0)
                m_FreeChunks.Add(new(0, capacity));
            m_AcquiredCount = 0;
        }

        public LinearValuePool(LinearValuePool<T> other)
        {
            m_Items = (T[])other.m_Items.Clone();
            m_FreeChunks = new(other.m_FreeChunks);
            m_AcquiredCount = other.m_AcquiredCount;
        }

        public ref T this[int index] => ref m_Items[index];

        public Span<T> GetSpan(int index, int count) => new Span<T>(m_Items, index, count);
        public ReadOnlySpan<T> GetReadOnlySpan(int index, int count) => new ReadOnlySpan<T>(m_Items, index, count);

        public IEnumerator<T> GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        public int Acquire(int count, out Span<T> span)
        {
            int index = Acquire(count);
            span = GetSpan(index, count);
            return index;
        }

        public int Acquire(int count = 1)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException();

            int chunkIndex = GetBigEnoughFreeChunk(count);
            if (chunkIndex == -1)
            {
                Reallocate(count);
                chunkIndex = m_FreeChunks.Count - 1;
            }

            var freeChunk = m_FreeChunks[chunkIndex];
            int itemIndex = freeChunk.Start;

            if (freeChunk.Length == count)
            {
                m_FreeChunks.RemoveAt(chunkIndex);
            }
            else
            {
                freeChunk.Start += count;
                freeChunk.Length -= count;
                m_FreeChunks[chunkIndex] = freeChunk;
            }

            m_AcquiredCount += count;
            return itemIndex;
        }

        public void Release(int index, int count = 1)
        {
            Release(index, count, true);
        }

        public int AcquireAndExpand(int index, int count, int newCount)
        {
            if (newCount <= count)
                throw new ArgumentException();

            Release(index, count, false); // don't clear now
            int newIndex = Acquire(newCount);

            Array.Copy(m_Items, index, m_Items, newIndex, newCount);
            // TODO clear what's not reused

            return newIndex;
        }

        public void ReleaseAndShrink(int index, int count, int totalCount)
        {
            if (index < 0 || index >= index + totalCount)
                throw new ArgumentOutOfRangeException();
            if (totalCount <= count)
                throw new ArgumentException();

            Release(index + count, totalCount - count, false);
            Array.Copy(m_Items, index, m_Items, index + count, totalCount - index);
        }

        public void Reallocate(int minNewSlotCount = 1)
        {
            if (minNewSlotCount <= 0)
                throw new ArgumentException();

            int oldCapacity = m_Items.Length;
            int newCapacity = oldCapacity + Math.Max(oldCapacity, minNewSlotCount);

            if (m_FreeChunks.Count > 0 && m_FreeChunks[m_FreeChunks.Count - 1].End == oldCapacity)
            {
                var chunk = m_FreeChunks[m_FreeChunks.Count - 1];
                chunk.Length += newCapacity - oldCapacity;
                m_FreeChunks[m_FreeChunks.Count - 1] = chunk;
            }
            else
            {
                m_FreeChunks.Add(new(oldCapacity, newCapacity - oldCapacity));
            }

            T[] newItems = new T[newCapacity];
            Array.Copy(m_Items, newItems, m_Items.Length);
            m_Items = newItems;
        }

        public void Clear()
        {
            Array.Clear(m_Items, 0, m_Items.Length);
            m_FreeChunks.Clear();
            m_FreeChunks.Add(new(0, m_Items.Length));
            m_AcquiredCount = 0;
        }

        public object Clone()
        {
            return new LinearValuePool<T>(this);
        }

        void Release(int index, int count, bool clear)
        {
            if (index < 0 || index + count >= m_Items.Length)
                throw new ArgumentOutOfRangeException();
            if (count <= 0)
                throw new ArgumentException();

            int chunkIndex = 0;
            bool mergeInPrevChunk = false;
            bool mergeInNextChunk = false;

            var chunks = m_FreeChunks;
            int chunkCount = m_FreeChunks.Count;

            while (chunkIndex < chunkCount)
            {
                // TODO add checks for not releasing not acquired items
                var chunk = chunks[chunkIndex];
                if (chunk.End == index)
                {
                    mergeInPrevChunk = true;
                }
                else if (chunk.Start >= index + count)
                {
                    mergeInNextChunk = chunk.Start == index + count;
                    break;
                }
                ++chunkIndex;
            }

            if (mergeInPrevChunk)
            {
                var chunk = chunks[chunkIndex - 1];
                chunk.Length += count + (mergeInNextChunk ? chunks[chunkIndex].Length : 0);
                chunks[chunkIndex - 1] = chunk;
                if (mergeInNextChunk)
                    chunks.RemoveAt(chunkIndex);
            }
            else if (mergeInNextChunk)
            {
                var chunk = chunks[chunkIndex];
                chunk.Start -= count;
                chunk.Length += count;
                chunks[chunkIndex] = chunk;
            }
            else
            {
                chunks.Insert(chunkIndex, new(index, count));
            }

            if (clear)
            {
                for (int i = 0; i < count; ++i)
                    m_Items[index + i] = default(T); // Reset released object to avoid holding ref
            }
            m_AcquiredCount -= count;
        }

        int GetBigEnoughFreeChunk(int length)
        {
            var chunks = m_FreeChunks;
            int chunkCount = chunks.Count;
            for (int i = 0; i < chunkCount; ++i) // linear search but fine if fragmentation is under control
            {
                if (chunks[i].Length >= length)
                    return i;
            }
            return -1;
        }

        struct FreeChunk
        {
            public int Start { get; set; }
            public int Length { get; set; }
            public int End => Start + Length; // not inclusive

            public FreeChunk(int start, int length)
            {
                if (start < 0)
                    throw new ArgumentException();
                if (length <= 0)
                    throw new ArgumentException();

                Start = start;
                Length = length;
            }
        }

        struct Enumerator : IEnumerator<T>
        {
            LinearValuePool<T> m_Pool;
            int m_Index;
            int m_ChunkPos;

            public T Current => m_Pool.m_Items[m_Index];
            object IEnumerator.Current => Current;

            public Enumerator(LinearValuePool<T> pool)
            {
                m_Pool = pool;
                m_Index = -1;
                m_ChunkPos = 0;
            }

            public bool MoveNext()
            {
                ++m_Index;
                if (m_ChunkPos < m_Pool.m_FreeChunks.Count)
                {
                    var freeChunk = m_Pool.m_FreeChunks[m_ChunkPos];
                    if (m_Index == freeChunk.Start)
                    {
                        m_Index += freeChunk.Length;
                        ++m_ChunkPos;
                    }
                }
                return m_Index < m_Pool.m_Items.Length;
            }

            public void Reset()
            {
                m_Index = -1;
                m_ChunkPos = 0;
            }

            public void Dispose() { }
        }
    }
}
