namespace UnityEditor.Rendering.HighDefinition
{
    using System;

    /// <summary>
    /// EXPERIMENTAL: An enumerator by mutable reference thats skip values based on a where clause.
    /// </summary>
    /// <typeparam name="T">Type of the enumerated values.</typeparam>
    /// <typeparam name="En">Type of the enumerator.</typeparam>
    /// <typeparam name="Wh">Type of the where clause.</typeparam>
    public struct WhereMutIterator<T, En, Wh> : IMutEnumerator<T>
        where Wh : struct, IInFunc<T, bool>
        where En : struct, IMutEnumerator<T>
    {
        class Data
        {
            public En enumerator;
            public Wh whereClause;
        }

        Data m_Data;

        public WhereMutIterator(En enumerator, Wh whereClause)
        {
            m_Data = new Data
            {
                enumerator = enumerator,
                whereClause = whereClause
            };
        }

        public ref T current
        {
            get
            {
                if (m_Data == null)
                    throw new InvalidOperationException("Enumerator not initialized.");

                return ref m_Data.enumerator.current;
            }
        }

        public bool MoveNext()
        {
            if (m_Data == null)
                return false;

            while (m_Data.enumerator.MoveNext())
            {
                ref var value = ref m_Data.enumerator.current;
                if (m_Data.whereClause.Execute(value))
                    return true;
            }

            return false;
        }

        public void Reset()
        {
            if (m_Data == null)
                throw new InvalidOperationException("Enumerator not initialized.");

            m_Data.enumerator.Reset();
        }
    }
}
