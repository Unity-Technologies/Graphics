using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Legacy
{
    [Serializable]
    class LegacyShaderInput
    {
        [SerializeField]
        SerializableGuid m_Guid = default;

        public SerializableGuid guid => m_Guid;
    }
}
