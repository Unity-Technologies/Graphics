using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Serialization
{
    struct RefValueEnumerable<T> : IEnumerable<T>
        where T : JsonObject
    {
        List<JsonRef<T>> m_List;

        public RefValueEnumerable(List<JsonRef<T>> list)
        {
            m_List = list;
        }

        public Enumerator GetEnumerator() => new Enumerator(m_List.GetEnumerator());

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            List<JsonRef<T>>.Enumerator m_Enumerator;

            public Enumerator(List<JsonRef<T>>.Enumerator enumerator)
            {
                m_Enumerator = enumerator;
            }

            public bool MoveNext() => m_Enumerator.MoveNext();

            void IEnumerator.Reset() => ((IEnumerator)m_Enumerator).Reset();

            public T Current => m_Enumerator.Current.value;

            object IEnumerator.Current => Current;

            public void Dispose() => m_Enumerator.Dispose();
        }
    }
}
