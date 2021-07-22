using System;
using UnityEngine;
using UnityEditor.Rendering.BuiltIn;

namespace UnityEditor.Rendering.BuiltIn.ShaderGraph
{
    [Serializable]
    sealed class BuiltInMetadata : ScriptableObject
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
