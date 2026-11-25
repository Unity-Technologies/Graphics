using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    public sealed class SerializableCubemap : ISerializationCallbackReceiver
    {
        [SerializeField]
        string m_SerializedCubemap;

        [SerializeField]
        string m_Guid;

        [NonSerialized]
        Cubemap m_Cubemap;

        [Serializable]
        class CubemapHelper
        {
#pragma warning disable 649
            public Cubemap cubemap;
#pragma warning restore 649
        }

        // used to get a Cubemap ref guid without loading the cubemap asset itself into memory
        [Serializable]
        class MinimalCubemapHelper
        {
            // these variables are only ever populated by serialization, disable the C# warning that checks if they are ever assigned
#pragma warning disable 0649
            [Serializable]
            public struct MinimalTextureRef
            {
                public string guid;
            }
            public MinimalTextureRef cubemap;
#pragma warning restore 0649
        }

        internal string guid
        {
            get
            {
                if (!string.IsNullOrEmpty(m_SerializedCubemap))
                {
                    var textureHelper = new MinimalCubemapHelper();
                    EditorJsonUtility.FromJsonOverwrite(m_SerializedCubemap, textureHelper);
                    if (!string.IsNullOrEmpty(textureHelper.cubemap.guid))
                        return textureHelper.cubemap.guid;
                }
                if (!string.IsNullOrEmpty(m_Guid))
                {
                    return m_Guid;
                }
                if (m_Cubemap != null)
                {
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m_Cubemap, out string guid, out long localId))
                        return guid;
                }
                return null;
            }
        }

        public Cubemap cubemap
        {
            get
            {
                if (!string.IsNullOrEmpty(m_SerializedCubemap))
                {
                    var textureHelper = new CubemapHelper();
                    EditorJsonUtility.FromJsonOverwrite(m_SerializedCubemap, textureHelper);
                    m_SerializedCubemap = null;
                    m_Guid = null;
                    m_Cubemap = textureHelper.cubemap;
                }
                else if (!string.IsNullOrEmpty(m_Guid) && m_Cubemap == null)
                {
                    m_Cubemap = AssetDatabase.LoadAssetAtPath<Cubemap>(AssetDatabase.GUIDToAssetPath(m_Guid));
                    m_Guid = null;
                }

                return m_Cubemap;
            }
            set
            {
                m_Cubemap = value;
                m_Guid = null;
                m_SerializedCubemap = null;
            }
        }

        public void OnBeforeSerialize()
        {
            // If m_Cubemap is null, the data is still in serialized form
            // (m_SerializedCubemap or m_Guid) and doesn't need to be re-serialized.
            // We must not use the cubemap property getter here because it would
            // force-load the asset, which is not safe in all contexts (e.g. domain backup).
            if ((object)m_Cubemap != null && string.IsNullOrEmpty(m_SerializedCubemap))
                m_SerializedCubemap = EditorJsonUtility.ToJson(new CubemapHelper { cubemap = m_Cubemap }, false);
        }

        public void OnAfterDeserialize()
        {
        }
    }
}
