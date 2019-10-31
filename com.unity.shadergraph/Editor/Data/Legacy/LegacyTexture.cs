using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Legacy
{
    [Serializable]
    class LegacyTexture
    {
        [SerializeField]
        SerializableTexture m_Texture = default;

        public Texture texture => m_Texture.texture;
    }
}
