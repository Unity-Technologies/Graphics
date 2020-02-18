using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    class BlockNode : AbstractMaterialNode
    {
        ContextData m_ContextData;

        // TODO: This whole class is temporary
        // TODO: Generate BlockNode from FieldDescriptors
        public BlockNode()
        {
            name = GuidEncoder.Encode(guid);
            AddSlot(new DynamicVectorMaterialSlot(0, name, name, SlotType.Input, Vector4.zero));
            RemoveSlotsNameNotMatching(new int[] {0});
        }

        public ContextData contextData
        {
            get => m_ContextData;
            set => m_ContextData = value;
        }
    }
}
