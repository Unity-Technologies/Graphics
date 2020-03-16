using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    [Serializable]
    public class JsonObject : ISerializationCallbackReceiver
    {
        [SerializeField]
        string m_Type;

        [SerializeField]
        string m_Id = Guid.NewGuid().ToString("N");

        public string id => m_Id;

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            m_Type = $"{GetType().FullName}";
            OnBeforeSerialize();
        }

        public virtual void OnBeforeSerialize() { }

        public virtual void OnAfterDeserialize() { }

        public virtual void OnAfterDeserialize(string json) { }

        public virtual void OnAfterMultiDeserialize(string json) { }
    }
}
