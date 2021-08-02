using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    public sealed class SerializableTexture : ISerializationCallbackReceiver
    {
        [SerializeField]
        string m_SerializedTexture;

        [SerializeField]
        string m_Guid;

        [NonSerialized]
        Texture m_Texture;

        [Serializable]
        class TextureHelper
        {
#pragma warning disable 649
            public Texture texture;
#pragma warning restore 649
        }

        // used to get a Texture ref guid without loading the texture asset itself into memory
        [Serializable]
        class MinimalTextureHelper
        {
            // these variables are only ever populated by serialization, disable the C# warning that checks if they are ever assigned
            #pragma warning disable 0649
            [Serializable]
            public struct MinimalTextureRef
            {
                public string guid;
            }
            public MinimalTextureRef texture;
            #pragma warning restore 0649
        }

        internal string guid
        {
            get
            {
                if (!string.IsNullOrEmpty(m_SerializedTexture))
                {
                    var textureHelper = new MinimalTextureHelper();
                    EditorJsonUtility.FromJsonOverwrite(m_SerializedTexture, textureHelper);
                    if (!string.IsNullOrEmpty(textureHelper.texture.guid))
                        return textureHelper.texture.guid;
                }
                if (!string.IsNullOrEmpty(m_Guid))
                {
                    return m_Guid;
                }
                if (m_Texture != null)
                {
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m_Texture, out string guid, out long localId))
                        return guid;
                }
                return null;
            }
        }

        public Texture texture
        {
            get
            {
                if (!string.IsNullOrEmpty(m_SerializedTexture))
                {
                    var textureHelper = new TextureHelper();
                    EditorJsonUtility.FromJsonOverwrite(m_SerializedTexture, textureHelper);
                    m_SerializedTexture = null;
                    m_Guid = null;
                    m_Texture = textureHelper.texture;
                }
                else if (!string.IsNullOrEmpty(m_Guid) && m_Texture == null)
                {
                    m_Texture = AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(m_Guid));
                    m_Guid = null;
                }

                return m_Texture;
            }
            set
            {
                m_Texture = value;
                m_Guid = null;
                m_SerializedTexture = null;
            }
        }

        public void OnBeforeSerialize()
        {
            m_SerializedTexture = EditorJsonUtility.ToJson(new TextureHelper { texture = texture }, false);
        }

        public void OnAfterDeserialize()
        {
        }
    }
}
