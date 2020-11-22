using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    sealed class ContextData
    {
        [SerializeField]
        Vector2 m_Position;

        [SerializeField]
        List<JsonRef<BlockNode>> m_Blocks = new List<JsonRef<BlockNode>>();

        [NonSerialized]
        ShaderStage m_ShaderStage;

        public ContextData()
        {
        }

        public List<JsonRef<BlockNode>> blocks => m_Blocks;

        public Vector2 position
        {
            get => m_Position;
            set => m_Position = value;
        }

        public ShaderStage shaderStage
        {
            get => m_ShaderStage;
            set => m_ShaderStage = value;
        }
    }
}
