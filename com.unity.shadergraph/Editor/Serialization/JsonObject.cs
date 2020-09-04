using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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

        internal void OverrideObjectId(string namespaceUid, string newObjectId) { m_ObjectId = GenerateNamespaceUUID(namespaceUid, newObjectId).ToString("N"); }

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
        
        internal static Guid GenerateNamespaceUUID(string Namespace, string Name)
        {            
            Guid namespaceGuid;
            if (!Guid.TryParse(Namespace, out namespaceGuid))
            {
                // Fallback namespace in case the one provided is invalid.
                // If an object ID was used as the namespace, this shouldn't normally be reachable.
                namespaceGuid = new Guid("6ba7b812-9dad-11d1-80b4-00c04fd430c8");
            }
            return GenerateNamespaceUUID(namespaceGuid, Name);
        }

        internal static Guid GenerateNamespaceUUID(Guid Namespace, string Name)
        {
            // Generate a deterministic guid using namespace guids: RFC 4122 §4.3 version 5.
            void FlipByNetworkOrder(byte[] bytes)
            { bytes = new byte[] { bytes[3], bytes[2], bytes[1], bytes[0], bytes[5], bytes[4], bytes[7], bytes[6] }; }

            var namespaceBytes = Namespace.ToByteArray();
            FlipByNetworkOrder(namespaceBytes);
            var nameBytes = Encoding.UTF8.GetBytes(Name);
            var hash = SHA1.Create().ComputeHash(namespaceBytes.Concat(nameBytes).ToArray());
            byte[] newguid = new byte[16];
            Array.Copy(hash, newguid, 16);
            newguid[6] = (byte)((newguid[6] & 0x0F) | 0x80);
            newguid[8] = (byte)((newguid[8] & 0x3F) | 0x80);
            FlipByNetworkOrder(newguid);
            return new Guid(newguid);
        }
    }
}
