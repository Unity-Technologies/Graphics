using System;
using UnityEngine;
using UnityEditor.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    [Serializable]
    sealed class HDMetadata : ScriptableObject
    {
        [SerializeField]
        HDShaderUtils.ShaderID m_ShaderID;

        public HDShaderUtils.ShaderID shaderID
        {
            get => m_ShaderID;
            set => m_ShaderID = value;
        }
    }
}
