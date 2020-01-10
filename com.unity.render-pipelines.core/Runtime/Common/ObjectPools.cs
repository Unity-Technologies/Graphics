using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Generic object pool.
    /// </summary>
    /// <typeparam name="T">Type of the object pool.</typeparam>
    public class ObjectPool<T> where T : new()
    {
        readonly Stack<T> m_Stack = new Stack<T>();
        readonly UnityAction<T> m_ActionOnGet;
        readonly UnityAction<T> m_ActionOnRelease;
        readonly bool m_CollectionCheck = true;

        /// <summary>
        /// Number of objects in the pool.
        /// </summary>
        public int countAll { get; private set; }
        /// <summary>
        /// Number of active objects in the pool.
        /// </summary>
        public int countActive { get { return countAll - countInactive; } }
        /// <summary>
        /// Number of inactive objects in the pool.
        /// </summary>
        public int countInactive { get { return m_Stack.Count; } }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="actionOnGet">Action on get.</param>
        /// <param name="actionOnRelease">Action on release.</param>
        /// <param name="collectionCheck">True if collection integrity should be checked.</param>
        public ObjectPool(UnityAction<T> actionOnGet, UnityAction<T> actionOnRelease, bool collectionCheck = true)
        {
            m_ActionOnGet = actionOnGet;
            m_ActionOnRelease = actionOnRelease;
            m_CollectionCheck = collectionCheck;
        }

        /// <summary>
        /// Get an object from the pool.
        /// </summary>
        /// <returns>A new object from the pool.</returns>
        public T Get()
        {
            T element;
            if (m_Stack.Count == 0)
            {
                element = new T();
                countAll++;
            }
            else
            {
                element = m_Stack.Pop();
            }
            if (m_ActionOnGet != null)
                m_ActionOnGet(element);
            return element;
        }

        /// <summary>
        /// Pooled object.
        /// </summary>
        public struct PooledObject : IDisposable
        {
            readonly T m_ToReturn;
            readonly ObjectPool<T> m_Pool;

            internal PooledObject(T value, ObjectPool<T> pool)
            {
                m_ToReturn = value;
                m_Pool = pool;
            }

            /// <summary>
            /// Disposable pattern implementation.
            /// </summary>
            void IDisposable.Dispose() => m_Pool.Release(m_ToReturn);
        }

        /// <summary>
        /// Get et new PooledObject.
        /// </summary>
        /// <param name="v">Output new typed object.</param>
        /// <returns>New PooledObject</returns>
        public PooledObject Get(out T v) => new PooledObject(v = Get(), this);

        /// <summary>
        /// Release an object to the pool.
        /// </summary>
        /// <param name="element">Object to release.</param>
        public void Release(T element)
        {
#if UNITY_EDITOR // keep heavy checks in editor
            if (m_CollectionCheck && m_Stack.Count > 0)
            {
                if (m_Stack.Contains(element))
                    Debug.LogError("Internal error. Trying to destroy object that is already released to pool.");
            }
#endif
            if (m_ActionOnRelease != null)
                m_ActionOnRelease(element);
            m_Stack.Push(element);
        }
    }

    /// <summary>
    /// Generic pool.
    /// </summary>
    /// <typeparam name="T">Type of the objects in the pull.</typeparam>
    public static class GenericPool<T>
        where T : new()
    {
        // Object pool to avoid allocations.
        static readonly ObjectPool<T> s_Pool = new ObjectPool<T>(null, null);

        /// <summary>
        /// Get a new object.
        /// </summary>
        /// <returns>A new object from the pool.</returns>
        public static T Get() => s_Pool.Get();

        /// <summary>
        /// Get a new PooledObject
        /// </summary>
        /// <param name="value">Output typed object.</param>
        /// <returns>A new PooledObject.</returns>
        public static ObjectPool<T>.PooledObject Get(out T value) => s_Pool.Get(out value);

        /// <summary>
        /// Release an object to the pool.
        /// </summary>
        /// <param name="toRelease">Object to release.</param>
        public static void Release(T toRelease) => s_Pool.Release(toRelease);
    }

    /// <summary>
    /// Generic pool without collection checks.
    /// This class is an alternative for the GenericPool for object that allocate memory when they are being compared.
    /// It is the case for the CullingResult class from Unity, and because of this in HDRP HDCullingResults generates garbage whenever we use ==, .Equals or ReferenceEquals.
    /// This pool doesn't do any of these comparison because we don't check if the stack already contains the element before releasing it.
    /// </summary>
    /// <typeparam name="T">Type of the objects in the pull.</typeparam>
    public static class UnsafeGenericPool<T>
        where T : new()
    {
        // Object pool to avoid allocations.
        static readonly ObjectPool<T> s_Pool = new ObjectPool<T>(null, null, false);

        /// <summary>
        /// Get a new object.
        /// </summary>
        /// <returns>A new object from the pool.</returns>
        public static T Get() => s_Pool.Get();

        /// <summary>
        /// Get a new PooledObject
        /// </summary>
        /// <param name="value">Output typed object.</param>
        /// <returns>A new PooledObject.</returns>
        public static ObjectPool<T>.PooledObject Get(out T value) => s_Pool.Get(out value);

        /// <summary>
        /// Release an object to the pool.
        /// </summary>
        /// <param name="toRelease">Object to release.</param>
        public static void Release(T toRelease) => s_Pool.Release(toRelease);
    }

    /// <summary>
    /// List Pool.
    /// </summary>
    /// <typeparam name="T">Type of the objects in the pooled lists.</typeparam>
    public static class ListPool<T>
    {
        // Object pool to avoid allocations.
        static readonly ObjectPool<List<T>> s_Pool = new ObjectPool<List<T>>(null, l => l.Clear());

        /// <summary>
        /// Get a new List
        /// </summary>
        /// <returns>A new List</returns>
        public static List<T> Get() => s_Pool.Get();

        /// <summary>
        /// Get a new list PooledObject.
        /// </summary>
        /// <param name="value">Output typed List.</param>
        /// <returns>A new List PooledObject.</returns>
        public static ObjectPool<List<T>>.PooledObject Get(out List<T> value) => s_Pool.Get(out value);

        /// <summary>
        /// Release an object to the pool.
        /// </summary>
        /// <param name="toRelease">List to release.</param>
        public static void Release(List<T> toRelease) => s_Pool.Release(toRelease);
    }

    /// <summary>
    /// HashSet Pool.
    /// </summary>
    /// <typeparam name="T">Type of the objects in the pooled hashsets.</typeparam>
    public static class HashSetPool<T>
    {
        // Object pool to avoid allocations.
        static readonly ObjectPool<HashSet<T>> s_Pool = new ObjectPool<HashSet<T>>(null, l => l.Clear());

        /// <summary>
        /// Get a new HashSet
        /// </summary>
        /// <returns>A new HashSet</returns>
        public static HashSet<T> Get() => s_Pool.Get();

        /// <summary>
        /// Get a new list PooledObject.
        /// </summary>
        /// <param name="value">Output typed HashSet.</param>
        /// <returns>A new HashSet PooledObject.</returns>
        public static ObjectPool<HashSet<T>>.PooledObject Get(out HashSet<T> value) => s_Pool.Get(out value);

        /// <summary>
        /// Release an object to the pool.
        /// </summary>
        /// <param name="toRelease">hashSet to release.</param>
        public static void Release(HashSet<T> toRelease) => s_Pool.Release(toRelease);
    }

    /// <summary>
    /// Dictionary Pool.
    /// </summary>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    public static class DictionaryPool<TKey, TValue>
    {
        // Object pool to avoid allocations.
        static readonly ObjectPool<Dictionary<TKey, TValue>> s_Pool
            = new ObjectPool<Dictionary<TKey, TValue>>(null, l => l.Clear());

        /// <summary>
        /// Get a new Dictionary
        /// </summary>
        /// <returns>A new Dictionary</returns>
        public static Dictionary<TKey, TValue> Get() => s_Pool.Get();

        /// <summary>
        /// Get a new dictionary PooledObject.
        /// </summary>
        /// <param name="value">Output typed Dictionary.</param>
        /// <returns>A new Dictionary PooledObject.</returns>
        public static ObjectPool<Dictionary<TKey, TValue>>.PooledObject Get(out Dictionary<TKey, TValue> value)
            => s_Pool.Get(out value);

        /// <summary>
        /// Release an object to the pool.
        /// </summary>
        /// <param name="toRelease">Dictionary to release.</param>
        public static void Release(Dictionary<TKey, TValue> toRelease) => s_Pool.Release(toRelease);
    }
}
