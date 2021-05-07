using System;
using UnityEngine;
using Unity.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    // This is a metadata object attached to ShaderGraph import asset results by the Universal Target
    // it contains any additional information that we might want to know about the Universal shader
    [Serializable]
    sealed class UniversalMetadata : ScriptableObject
    {
        [SerializeField]
        ShaderUtils.ShaderID m_ShaderID;

        public ShaderUtils.ShaderID shaderID
        {
            get => m_ShaderID;
            set => m_ShaderID = value;
        }
    }
}
