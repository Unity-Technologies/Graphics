using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class SerializableTexture : ISerializationCallbackReceiver
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
