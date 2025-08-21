using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.RenderGraphModule
{
    /// <summary>
    /// Helper class provided in the RenderGraphContext to all Render Passes.
    /// It allows you to do temporary allocations of various objects during a Render Pass.
    /// </summary>
    public sealed class RenderGraphObjectPool
    {
        // Only used to clear all existing pools at once from here when needed 
        static DynamicArray<SharedObjectPoolBase> s_AllocatedPools = new DynamicArray<SharedObjectPoolBase>();

        // Non abstract class instead of an interface to store it in a DynamicArray
        class SharedObjectPoolBase
        {        
            public SharedObjectPoolBase() {}
            public virtual void Clear() {}
        }

        class SharedObjectPool<T> : SharedObjectPoolBase where T : class, new()
        {
            private static readonly Pool.ObjectPool<T> s_Pool = AllocatePool();
            
            private static Pool.ObjectPool<T> AllocatePool()
            {
                var newPool = new Pool.ObjectPool<T>(() => new T(), null, null);
                // Storing instance to clear the static pool of the same type if needed
                s_AllocatedPools.Add(new SharedObjectPool<T>());
                return newPool;
            }

            /// <summary>
            /// Clear the pool using SharedObjectPool instance.
            /// </summary>
            /// <returns></returns>
            public override void Clear()
            {
                s_Pool.Clear();
            }
            
            /// <summary>
            /// Get a new instance from the pool.
            /// </summary>
            /// <returns></returns>
            public static T Get() => s_Pool.Get();

            /// <summary>
            /// Release an object to the pool.
            /// </summary>
            /// <param name="toRelease">instance to release.</param>
            public static void Release(T toRelease) => s_Pool.Release(toRelease);
        }


        Dictionary<(Type, int), Stack<object>> m_ArrayPool = new Dictionary<(Type, int), Stack<object>>();
        List<(object, (Type, int))> m_AllocatedArrays = new List<(object, (Type, int))>();
        List<MaterialPropertyBlock> m_AllocatedMaterialPropertyBlocks = new List<MaterialPropertyBlock>();

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
            var result = SharedObjectPool<MaterialPropertyBlock>.Get();
            result.Clear();
            m_AllocatedMaterialPropertyBlocks.Add(result);
            return result;
        }

        internal void ReleaseAllTempAlloc()
        {
            foreach (var arrayDesc in m_AllocatedArrays)
            {
                bool result = m_ArrayPool.TryGetValue(arrayDesc.Item2, out var stack);
                Debug.Assert(result, "Correct stack type should always be allocated.");
                stack.Push(arrayDesc.Item1);
            }

            m_AllocatedArrays.Clear();

            foreach (var mpb in m_AllocatedMaterialPropertyBlocks)
            {
                SharedObjectPool<MaterialPropertyBlock>.Release(mpb);
            }

            m_AllocatedMaterialPropertyBlocks.Clear();
        }
        
        internal bool IsEmpty()
        {
            return m_AllocatedArrays.Count == 0 && m_AllocatedMaterialPropertyBlocks.Count == 0;
        }
        
        // Regular pooling API. Only internal use for now
        internal T Get<T>() where T : class, new()
        {
            return SharedObjectPool<T>.Get();
        }

        internal void Release<T>(T value) where T : class, new()
        {
            SharedObjectPool<T>.Release(value);
        }

        internal void Cleanup()
        {
            m_AllocatedArrays.Clear();
            m_AllocatedMaterialPropertyBlocks.Clear();
            m_ArrayPool.Clear();

            // Removing all objects in the pools
            foreach (var pool in s_AllocatedPools)
                pool.Clear();
        }
    }
}
