using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Serialization
{
    struct JsonRefListEnumerator<T> : IEnumerator<T>
        where T : JsonObject
    {
        List<JsonRef<T>>.Enumerator m_Enumerator;

        public JsonRefListEnumerator(List<JsonRef<T>>.Enumerator enumerator)
        {
            m_Enumerator = enumerator;
        }

        public bool MoveNext()
        {
            return m_Enumerator.MoveNext();
        }

        void IEnumerator.Reset()
        {
            ((IEnumerator)m_Enumerator).Reset();
        }

        public T Current => m_Enumerator.Current;

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            m_Enumerator.Dispose();
        }
    }
}
