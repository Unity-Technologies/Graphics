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
            name = $"{fieldDescriptor.tag}.{fieldDescriptor.name}";
            m_ContextStage = fieldDescriptor.contextStage;
            AddSlot(fieldDescriptor);
        }

        void AddSlot(BlockFieldDescriptor descriptor)
        {
            var displayName = descriptor.name;
            var referenceName = $"{descriptor.tag}.{descriptor.name}";
            switch(descriptor.control)
            {
                case ObjectSpacePositionControl objectSpacePositionControl:
                    AddSlot(new PositionMaterialSlot(0, displayName, referenceName, CoordinateSpace.Object, GetStageCapability()));
                    break;
                case ObjectSpaceNormalControl objectSpaceNormalControl:
                    AddSlot(new NormalMaterialSlot(0, displayName, referenceName, CoordinateSpace.Object, GetStageCapability()));
                    break;
                case ObjectSpaceTangentControl objectSpaceTangentControl:
                    AddSlot(new TangentMaterialSlot(0, displayName, referenceName, CoordinateSpace.Object, GetStageCapability()));
                    break;
                case TangentSpaceNormalControl tangentSpaceNormalControl:
                    AddSlot(new NormalMaterialSlot(0, displayName, referenceName, CoordinateSpace.Tangent, GetStageCapability()));
                    break;
                case ColorControl colorControl:
                    var colorMode = colorControl.hdr ? ColorMode.HDR : ColorMode.Default;
                    AddSlot(new ColorRGBMaterialSlot(0, displayName, referenceName, SlotType.Input, colorControl.value, colorMode, GetStageCapability()));
                    break;
                case ColorRGBAControl colorRGBAControl:
                    AddSlot(new ColorRGBAMaterialSlot(0, displayName, referenceName, SlotType.Input, colorRGBAControl.value, GetStageCapability()));
                    break;
                case FloatControl floatControl:
                    AddSlot(new Vector1MaterialSlot(0, displayName, referenceName, SlotType.Input, floatControl.value, GetStageCapability()));
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
