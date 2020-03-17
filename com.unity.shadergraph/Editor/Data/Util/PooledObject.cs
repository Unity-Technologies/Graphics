using System;

namespace UnityEditor.Graphing
{
    struct PooledObject<T> : IDisposable where T : new()
    {
        private ObjectPool<T> m_ObjectPool;

        public T value { get; private set; }

        internal PooledObject(ObjectPool<T> objectPool, T value)
        {
            m_ObjectPool = objectPool;
            this.value = value;
        }

        public void Dispose()
        {
            m_ObjectPool.Release(value);
        }
    }
}
