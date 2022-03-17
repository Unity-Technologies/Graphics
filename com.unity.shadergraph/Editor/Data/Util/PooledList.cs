using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEditor.ShaderGraph
{
    class PooledList<T> : List<T>, IDisposable
    {
        static Stack<PooledList<T>> s_Pool = new Stack<PooledList<T>>();
        bool m_Active;

        PooledList() { }

        public static PooledList<T> Get()
        {
            if (s_Pool.Count == 0)
            {
                return new PooledList<T> { m_Active = true };
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
        ~PooledList()
        {
            throw new InvalidOperationException($"{nameof(PooledList<T>)} must be disposed manually.");
        }

#endif
    }
}
