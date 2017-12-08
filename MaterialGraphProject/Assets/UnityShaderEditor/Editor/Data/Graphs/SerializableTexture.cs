using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class SerializableTexture : ISerializationCallbackReceiver
    {
        [SerializeField]
        private string m_SerializedTexture;

        [Serializable]
        private class TextureHelper
        {
            public Texture texture;
        }

        Texture m_Texture;

        public Texture texture
        {
            get
            {
                if (m_Texture == null && !string.IsNullOrEmpty(m_SerializedTexture))
                {
                    var tex = new TextureHelper();
                    EditorJsonUtility.FromJsonOverwrite(m_SerializedTexture, tex);
                    m_Texture = tex.texture;
                    m_SerializedTexture = null;
                }
                return m_Texture;
            }
            set { m_Texture = value; }
        }

        public void OnBeforeSerialize()
        {
            var tex = new TextureHelper { texture = texture };
            m_SerializedTexture = EditorJsonUtility.ToJson(tex, true);
        }

        public void OnAfterDeserialize()
        {
        }
    }
}
