using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    sealed class ContextData : ISerializationCallbackReceiver
    {
        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableBlocks = new List<SerializationHelper.JSONSerializedElement>();

        [SerializeField]
        Vector2 m_Position;

        [NonSerialized]
        List<BlockNode> m_Blocks;
        
        [NonSerialized]
        List<BlockNode> m_RemovedBlocks;

        public ContextData()
        {
            m_Blocks = new List<BlockNode>();
            m_RemovedBlocks = new List<BlockNode>();
        }

        public List<BlockNode> blocks => m_Blocks;
        public List<BlockNode> removedBlocks => m_RemovedBlocks;

        public Vector2 position
        {
            get => m_Position;
            set => m_Position = value;
        }

        public void AddBlock(BlockNode blockNode, int index)
        {
            blockNode.contextData = this;
            if(index == -1)
            {
                blocks.Add(blockNode);
            }
            else
            {
                blocks.Insert(index, blockNode);
            }
        }

        public void RemoveBlock(BlockNode blockNode)
        {
            blocks.Remove(blockNode);
            removedBlocks.Add(blockNode);
        }

        public void OnBeforeSerialize()
        {
            m_SerializableBlocks = SerializationHelper.Serialize<BlockNode>(m_Blocks);
        }

        public void OnAfterDeserialize()
        {
            m_Blocks = SerializationHelper.Deserialize<BlockNode>(m_SerializableBlocks, GraphUtil.GetLegacyTypeRemapping());
            m_SerializableBlocks = null;
        }
    }
}
