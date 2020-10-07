using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    [Serializable]
    public class JsonObject : ISerializationCallbackReceiver
    {


        public virtual int latestVersion { get; } = 0;

        [SerializeField]
        protected int m_SGVersion = 0;
        public virtual int sgVersion { get => m_SGVersion; protected set => m_SGVersion = value; }

        internal protected delegate void VersionChange(int newVersion);
        internal protected VersionChange onBeforeVersionChange;
        internal protected Action onAfterVersionChange;

        internal void ChangeVersion(int newVersion)
        {
            if (newVersion == sgVersion)
            {
                return;
            }
            if (newVersion < 0)
            {
                Debug.LogError("Cant downgrade past version 0");
                return;
            }
            if (newVersion > latestVersion)
            {
                Debug.LogError("Cant upgrade to a version >= the current latest version");
                return;
            }

            onBeforeVersionChange?.Invoke(newVersion);
            sgVersion = newVersion;
            onAfterVersionChange?.Invoke();
        }

        public JsonObject()
        {
            sgVersion = latestVersion;
        }

        public static readonly string emptyObjectId = Guid.Empty.ToString("N");

        [SerializeField]
        string m_Type;

        [SerializeField]
        string m_ObjectId = Guid.NewGuid().ToString("N");

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
