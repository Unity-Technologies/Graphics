using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    class JsonContext : ScriptableObject
    {
        [SerializeField]
        uint m_SerializedVersion = default;

        [NonSerialized]
        uint m_LiveVersion;

        [NonSerialized]
        Dictionary<string, JsonObject> m_ObjectMap;

        public TRoot LoadText<TRoot>(string text)
        {
            return default;
        }

        public string GetText(JsonObject rootObject)
        {
            return default;
        }

        public void Serialize(JsonObject rootObject) { }

        public void Deserialize(JsonObject rootObject) { }

        public void Update()
        {
            if (m_SerializedVersion != m_LiveVersion)
            {

            }
        }

        public void RegisterCompleteObjectUndo(string actionName)
        {
            // TODO: Serialize
            // An undo action means that new changes are incoming, so we mark them up.
            m_LiveVersion++;
        }
    }
}
