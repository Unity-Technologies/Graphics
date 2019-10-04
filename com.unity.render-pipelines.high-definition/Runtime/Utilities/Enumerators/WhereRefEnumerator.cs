namespace UnityEditor.Rendering.HighDefinition
{
    using System;

    /// <summary>
    /// EXPERIMENTAL: An enumerator by reference thats skip values based on a where clause.
    /// </summary>
    /// <typeparam name="T">Type of the enumerated values.</typeparam>
    /// <typeparam name="En">Type of the enumerator.</typeparam>
    /// <typeparam name="Wh">Type of the where clause.</typeparam>
    public struct WhereRefIterator<T, En, Wh> : IRefEnumerator<T>
        where Wh : struct, IInFunc<T, bool>
        where En : struct, IRefEnumerator<T>
    {
        class Data
        {
            public En enumerator;
            public Wh whereClause;
        }

        Data m_Data;

        public WhereRefIterator(En enumerator, Wh whereClause)
        {
            m_Data = new Data
            {
                enumerator = enumerator,
                whereClause = whereClause
            };
        }

        public ref readonly T current
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
                ref readonly var value = ref m_Data.enumerator.current;
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
