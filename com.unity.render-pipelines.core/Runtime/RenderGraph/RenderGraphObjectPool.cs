using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    /// <summary>
    /// Helper class provided in the RenderGraphContext to all Render Passes.
    /// It allows you to do temporary allocations of various objects during a Render Pass.
    /// </summary>
    public sealed class RenderGraphObjectPool
    {
        abstract class SharedObjectPoolBase
        {
            protected static List<SharedObjectPoolBase> s_AllocatedPools = new List<SharedObjectPoolBase>();

            protected abstract void Clear();

            public static void ClearAll()
            {
                foreach (var pool in s_AllocatedPools)
                    pool.Clear();
            }
        }

        class SharedObjectPool<T> : SharedObjectPoolBase where T : new()
        {
            Stack<T> m_Pool = new Stack<T>();

            public T Get()
            {
                var result = m_Pool.Count == 0 ? new T() : m_Pool.Pop();
                return result;
            }

            public void Release(T value)
            {
                m_Pool.Push(value);
            }

            static SharedObjectPool<T> AllocatePool()
            {
                var pool = new SharedObjectPool<T>();
                s_AllocatedPools.Add(pool);
                return pool;
            }

            override protected void Clear()
            {
                m_Pool.Clear();
            }

            static readonly Lazy<SharedObjectPool<T>> s_Instance = new Lazy<SharedObjectPool<T>>(AllocatePool);
            public static SharedObjectPool<T> sharedPool => s_Instance.Value;
        }

        Dictionary<(Type, int), Stack<object>>  m_ArrayPool = new Dictionary<(Type, int), Stack<object>>();
        List<(object, (Type, int))>             m_AllocatedArrays = new List<(object, (Type, int))>();
        List<MaterialPropertyBlock>             m_AllocatedMaterialPropertyBlocks = new List<MaterialPropertyBlock>();

        internal RenderGraphObjectPool() { }

        /// <summary>
        /// Allocate a temporary typed array of a specific size.
        /// Unity releases the array at the end of the Render Pass.
        /// </summary>
        /// <typeparam name="T">Type of the array to be allocated.</typeparam>
        /// <param name="size">Number of element in the array.</param>
        /// <returns>A new array of type T with size number of elements.</returns>
        public T[] GetTempArray<T>(int size)
        {
            if (!m_ArrayPool.TryGetValue((typeof(T), size), out var stack))
            {
                stack = new Stack<object>();
                m_ArrayPool.Add((typeof(T), size), stack);
            }

            var result = stack.Count > 0 ? (T[])stack.Pop() : new T[size];
            m_AllocatedArrays.Add((result, (typeof(T), size)));
            return result;
        }

        /// <summary>
        /// Allocate a temporary MaterialPropertyBlock for the Render Pass.
        /// </summary>
        /// <returns>A new clean MaterialPropertyBlock.</returns>
        public MaterialPropertyBlock GetTempMaterialPropertyBlock()
        {
            var result = SharedObjectPool<MaterialPropertyBlock>.sharedPool.Get();
            result.Clear();
            m_AllocatedMaterialPropertyBlocks.Add(result);
            return result;
        }

        internal void ReleaseAllTempAlloc()
        {
            foreach(var arrayDesc in m_AllocatedArrays)
            {
                bool result = m_ArrayPool.TryGetValue(arrayDesc.Item2, out var stack);
                Debug.Assert(result, "Correct stack type should always be allocated.");
                stack.Push(arrayDesc.Item1);
            }

            m_AllocatedArrays.Clear();

            foreach(var mpb in m_AllocatedMaterialPropertyBlocks)
            {
                SharedObjectPool<MaterialPropertyBlock>.sharedPool.Release(mpb);
            }

            m_AllocatedMaterialPropertyBlocks.Clear();
        }

        // Regular pooling API. Only internal use for now
        internal T Get<T>() where T : new()
        {
            var pool = SharedObjectPool<T>.sharedPool;
            return pool.Get();
        }

        internal void Release<T>(T value) where T : new()
        {
            var pool = SharedObjectPool<T>.sharedPool;
            pool.Release(value);
        }

        internal void Cleanup()
        {
            m_AllocatedArrays.Clear();
            m_AllocatedMaterialPropertyBlocks.Clear();
            m_ArrayPool.Clear();
            SharedObjectPoolBase.ClearAll();
        }
    }
}
