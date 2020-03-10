using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    [Serializable]
    public class JsonObject : ISerializationCallbackReceiver
    {
        // We have some fields in here to match the output of ScriptableObject.
        // This is to prevent introducing big changes to Shader Graph files
        // when we make JsonObject inherit ScriptableObject in the future.
#pragma warning disable 414
        [SerializeField]
        bool m_Enabled = true;

        [SerializeField]
        int m_EditorHideFlags = 0;

        [SerializeField]
        string m_Name = default;

        [SerializeField]
        string m_EditorClassIdentifier = default;
#pragma warning restore 414

        [SerializeField]
        string m_Type;

        [SerializeField]
        string m_Id = Guid.NewGuid().ToString("N");

        public string id => m_Id;

        public string name
        {
            get => m_Name;
            set => m_Name = value;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            m_EditorClassIdentifier = $"{GetType().Assembly.GetName().Name}:{GetType().Namespace}:{GetType().Name}";
            m_Type = $"{GetType().FullName}";
            OnBeforeSerialize();
        }

        public virtual void OnBeforeSerialize() { }

        public virtual void OnAfterDeserialize() { }

        public virtual void OnAfterDeserialize(string json) { }

        public virtual void OnAfterMultiDeserialize(string json) { }
    }
}
