using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class SerializableTexture
    {
        [SerializeField]
        private string m_SerializedTexture;

        [Serializable]
        private class TextureHelper
        {
            public Texture texture;
        }

        public Texture texture
        {
            get
            {
                if (string.IsNullOrEmpty(m_SerializedTexture))
                    return null;
                var tex = new TextureHelper();
                EditorJsonUtility.FromJsonOverwrite(m_SerializedTexture, tex);
                return tex.texture;
            }
            set
            {
                if (texture == value)
                    return;

                var textureHelper = new TextureHelper();
                textureHelper.texture = value;
                m_SerializedTexture = EditorJsonUtility.ToJson(textureHelper, true);
            }
        }
    }
}
