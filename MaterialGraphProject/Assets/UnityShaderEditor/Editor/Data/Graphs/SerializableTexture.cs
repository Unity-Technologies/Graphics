using System;
using UnityEditor;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class SerializableTexture
    {
        [SerializeField] private string m_SerializedTexture;

        [Serializable]
        private class TextureHelper
        {
            public Texture texture;
        }

#if UNITY_EDITOR
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

                var tex = new TextureHelper();
                tex.texture = value;
                m_SerializedTexture = EditorJsonUtility.ToJson(tex, true);
            }
        }
#else
        public Texture defaultTexture {get; set; }
#endif

    }
}