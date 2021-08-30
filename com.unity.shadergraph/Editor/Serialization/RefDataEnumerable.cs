using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Serialization
{
    struct DataValueEnumerable<T> : IEnumerable<T>
        where T : JsonObject
    {
        List<JsonData<T>> m_List;

        public DataValueEnumerable(List<JsonData<T>> list)
        {
            m_List = list;
        }

        public void Sort(System.Comparison<T> comparison)
        {
            m_List?.Sort((a, b) => comparison(a.value, b.value));
        }

        public Enumerator GetEnumerator() => new Enumerator(m_List.GetEnumerator());

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            List<JsonData<T>>.Enumerator m_Enumerator;

            public Enumerator(List<JsonData<T>>.Enumerator enumerator)
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
