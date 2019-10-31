using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Serialization
{
    struct JsonRefListEnumerable<T> : IEnumerable<T>
        where T : JsonObject
    {
        List<JsonRef<T>> m_List;

        public JsonRefListEnumerable(List<JsonRef<T>> list)
        {
            m_List = list;
        }

        public JsonRefListEnumerator<T> GetEnumerator()
        {
            return new JsonRefListEnumerator<T>(m_List.GetEnumerator());
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static implicit operator JsonRefListEnumerable<T>(List<JsonRef<T>> list)
        {
            return new JsonRefListEnumerable<T>(list);
        }
    }
}
