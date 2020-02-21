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

        [NonSerialized]
        ContextStage m_ContextStage;

        public BlockNode()
        {
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

        public ContextStage contextStage
        {
            get => m_ContextStage;
            set => m_ContextStage = value;
        }

        public void Init(BlockFieldDescriptor fieldDescriptor)
        {
            name = fieldDescriptor.name;
            m_ContextStage = fieldDescriptor.contextStage;
            AddSlot(fieldDescriptor.control);
        }

        void AddSlot(IControl control)
        {
            switch(control)
            {
                case ObjectSpacePositionControl objectSpacePositionControl:
                    AddSlot(new PositionMaterialSlot(0, name, name, CoordinateSpace.Object, GetStageCapability()));
                    break;
                case ObjectSpaceNormalControl objectSpaceNormalControl:
                    AddSlot(new NormalMaterialSlot(0, name, name, CoordinateSpace.Object, GetStageCapability()));
                    break;
                case ObjectSpaceTangentControl objectSpaceTangentControl:
                    AddSlot(new TangentMaterialSlot(0, name, name, CoordinateSpace.Object, GetStageCapability()));
                    break;
                case TangentSpaceNormalControl tangentSpaceNormalControl:
                    AddSlot(new NormalMaterialSlot(0, name, name, CoordinateSpace.Tangent, GetStageCapability()));
                    break;
                case ColorControl colorControl:
                    var colorMode = colorControl.hdr ? ColorMode.HDR : ColorMode.Default;
                    AddSlot(new ColorRGBMaterialSlot(0, name, name, SlotType.Input, colorControl.value, colorMode, GetStageCapability()));
                    break;
                case FloatControl floatControl:
                    AddSlot(new Vector1MaterialSlot(0, name, name, SlotType.Input, floatControl.value, GetStageCapability()));
                    break;
            }
            RemoveSlotsNameNotMatching(new int[] {0});
        }

        ShaderStageCapability GetStageCapability()
        {
            switch(m_ContextStage)
            {
                case ContextStage.Vertex:
                    return ShaderStageCapability.Vertex;
                default:
                    return ShaderStageCapability.Fragment;
            }
        }
    }
}
