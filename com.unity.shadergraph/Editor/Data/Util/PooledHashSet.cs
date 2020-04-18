using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEditor.ShaderGraph
{
    class PooledHashSet<T> : HashSet<T>, IDisposable
    {
        static Stack<PooledHashSet<T>> s_Pool = new Stack<PooledHashSet<T>>();
        bool m_Active;

        PooledHashSet() {}

        public static PooledHashSet<T> Get()
        {
            if (s_Pool.Count == 0)
            {
                return new PooledHashSet<T> { m_Active = true };
            }

            var list = s_Pool.Pop();
            list.m_Active = true;
#if DEBUG
            GC.ReRegisterForFinalize(list);
#endif
            return list;
        }

        public void Dispose()
        {
            Assert.IsTrue(m_Active);
            m_Active = false;
            Clear();
            s_Pool.Push(this);
#if DEBUG
            GC.SuppressFinalize(this);
#endif
        }

// Destructor causes some GC alloc so only do this sanity check in debug build
#if DEBUG
        ~PooledHashSet()
        {
            throw new InvalidOperationException($"{nameof(PooledHashSet<T>)} must be disposed manually.");
        }
#endif
    }
}
