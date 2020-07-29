using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    [Serializable]
    public class JsonObject : ISerializationCallbackReceiver
    {
        public static readonly string emptyObjectId = Guid.Empty.ToString("N");

        [SerializeField]
        string m_Type;

        [SerializeField]
        string m_ObjectId = Guid.NewGuid().ToString("N");

        public string ObjectIdUnsafe
        {
            get { return m_ObjectId; }
            set { m_ObjectId = value; } // TODO: Maybe throw exception if the assigned Id is not a valid guid.
        }

        public string objectId => m_ObjectId;

        public bool objectIdIsEmpty => m_ObjectId.Equals(emptyObjectId);
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            m_Type = $"{GetType().FullName}";
            OnBeforeSerialize();
        }

        public virtual T CastTo<T>() where T : JsonObject { return (T)this; }
        public virtual string Serialize() { return EditorJsonUtility.ToJson(this, true); }
        public virtual void Deserailize(string typeInfo, string jsonData) { EditorJsonUtility.FromJsonOverwrite(jsonData, this); }

        public virtual void OnBeforeSerialize() { }

        public virtual void OnAfterDeserialize() { }

        public virtual void OnAfterDeserialize(string json) { }

        public virtual void OnAfterMultiDeserialize(string json) { }
    }
}
