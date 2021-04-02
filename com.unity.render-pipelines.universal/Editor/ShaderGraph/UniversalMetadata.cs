using System;
using UnityEngine;
using Unity.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
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
