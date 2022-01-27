using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace UnityEngine.Rendering.UIGen
{
    public struct PooledList<TValue> : IDisposable
    {
        List<TValue> m_List;

        [MustUseReturnValue]
        public bool TryGet(
            [NotNullWhen(true)] out List<TValue> thisList,
            [NotNullWhen(false)] out Exception error
        )
        {
            if (m_List == null)
            {
                error = new ObjectDisposedException(nameof(PooledList<TValue>));
                thisList = null;
                return false;
            }

            thisList = m_List;
            error = null;
            return true;
        }

        public List<TValue> list
        {
            get
            {
                if (!TryGet(out var thisList, out var error))
                    throw error;
                return thisList;
            }
        }

        public unsafe List<TValue> listUnsafe => m_List;

        public void Dispose()
        {
            if (m_List == null)
                return;
            ListPool<TValue>.Release(m_List);
            m_List = null;
        }
    }
}
