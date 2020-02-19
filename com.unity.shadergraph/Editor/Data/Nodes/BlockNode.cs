using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    class BlockNode : AbstractMaterialNode
    {
        [NonSerialized]
        ContextData m_Context;

        [NonSerialized]
        int m_Index;

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
            get => m_Context;
            set => m_Context = value;
        }

        public int index
        {
            get => m_Index;
            set => m_Index = value;
        }
    }
}
