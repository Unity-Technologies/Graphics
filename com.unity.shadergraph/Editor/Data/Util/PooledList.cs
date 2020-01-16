using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEditor.ShaderGraph
{
    class PooledList<T> : List<T>, IDisposable
    {
        static Stack<PooledList<T>> s_Pool = new Stack<PooledList<T>>();
        bool m_Active;

        PooledList() {}

        public static PooledList<T> Get()
        {
            if (s_Pool.Count == 0)
            {
                return new PooledList<T> { m_Active = true };
            }

            var list = s_Pool.Pop();
            list.m_Active = true;
            GC.ReRegisterForFinalize(list);
            return list;
        }

        public void Dispose()
        {
            Assert.IsTrue(m_Active);
            Clear();
            s_Pool.Push(this);
            GC.SuppressFinalize(this);
        }

        ~PooledList()
        {
            throw new InvalidOperationException($"{nameof(PooledList<T>)} must be disposed manually.");
        }
    }
}
