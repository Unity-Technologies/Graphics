using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Legacy
{
    [Serializable]
    class TextureShaderPropertyV0
    {
        [SerializeField]
        SerializableTexture m_Value = default;

        public SerializableTexture value => m_Value;
    }
}
