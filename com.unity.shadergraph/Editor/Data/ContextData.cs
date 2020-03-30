using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    sealed class ContextData : ISerializationCallbackReceiver
    {
        [SerializeField]
        List<string> m_SerializableBlockGuids = new List<string>();

        [SerializeField]
        Vector2 m_Position;

        [NonSerialized]
        List<BlockNode> m_Blocks = new List<BlockNode>();

        [NonSerialized]
        ShaderStage m_ShaderStage;

        public ContextData()
        {
        }

        public List<string> serializeableBlockGuids => m_SerializableBlockGuids;

        public List<BlockNode> blocks => m_Blocks;

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

        public void OnBeforeSerialize()
        {
            m_SerializableBlockGuids = new List<string>();
            foreach(var block in blocks)
            {
                m_SerializableBlockGuids.Add(block.guid.ToString());
            }
        }

        public void OnAfterDeserialize()
        {
        }
    }
}
