using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class SerializableStack : ISerializationCallbackReceiver
    {
        [SerializeField]
        string m_SerializedTextureStack;

        [SerializeField]
        string m_Guid;

        [NonSerialized]
        TextureStack m_TextureStack;

        [Serializable]
        class TextureStackHelper
        {
#pragma warning disable 649
            public TextureStack stack;
#pragma warning restore 649
        }

        public TextureStack textureStack
        {
            get
            {
                if (!string.IsNullOrEmpty(m_SerializedTextureStack))
                {
                    var textureStackHelper = new TextureStackHelper();
                    EditorJsonUtility.FromJsonOverwrite(m_SerializedTextureStack, textureStackHelper);
                    m_SerializedTextureStack = null;
                    m_Guid = null;
                    m_TextureStack = textureStackHelper.stack;
                }
                else if (!string.IsNullOrEmpty(m_Guid) && m_TextureStack == null)
                {
                    m_TextureStack = AssetDatabase.LoadAssetAtPath<TextureStack>(AssetDatabase.GUIDToAssetPath(m_Guid));
                    m_Guid = null;
                }

                return m_TextureStack;
            }
            set
            {
                m_TextureStack = value;
                m_Guid = null;
                m_SerializedTextureStack = null;
            }
        }

        public void OnBeforeSerialize()
        {
            m_SerializedTextureStack = EditorJsonUtility.ToJson(new TextureStackHelper { stack = textureStack }, false);
        }

        public void OnAfterDeserialize()
        {
        }
    }
}
