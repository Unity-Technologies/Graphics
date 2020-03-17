using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    class BlockNode : AbstractMaterialNode
        , IMayRequireNormal
        , IMayRequireTangent
        , IMayRequireBitangent
        , IMayRequireMeshUV
        , IMayRequireScreenPosition
        , IMayRequireViewDirection
        , IMayRequirePosition
        , IMayRequireVertexColor
    {
        [SerializeField]
        string m_SerializedDescriptor;

        [NonSerialized]
        ContextData m_ContextData;

        [NonSerialized]
        int m_Index;

        [NonSerialized]
        BlockFieldDescriptor m_Descriptor;

        [NonSerialized]
        ShaderGraphRequirements m_Requirements;

        public BlockNode()
        {
        }
        
        public override bool canCutNode => false;
        public override bool canCopyNode => false;

        // Because the GraphData is deserialized after its child elements
        // the descriptor list is not built (and owner is not set)
        // at the time of node deserialization
        // Therefore we need to deserialize this element at GraphData.OnAfterDeserialize
        public string serializedDescriptor => m_SerializedDescriptor;

        public ContextData contextData
        {
            get => m_ContextData;
            set => m_ContextData = value;
        }

        public int index
        {
            get => m_Index;
            set => m_Index = value;
        }

        public BlockFieldDescriptor descriptor
        {
            get => m_Descriptor;
            set => m_Descriptor = value;
        }

        public void Init(BlockFieldDescriptor fieldDescriptor)
        {
            name = $"{fieldDescriptor.tag}.{fieldDescriptor.name}";
            m_Descriptor = fieldDescriptor;
            m_Requirements = fieldDescriptor.control.GetRequirements();
            AddSlot();
        }

        void AddSlot()
        {
            var stageCapability = m_Descriptor.shaderStage.GetShaderStageCapability();
            switch(descriptor.control)
            {
                case PositionControl positionControl:
                    AddSlot(new PositionMaterialSlot(0, descriptor.name, descriptor.name, positionControl.space, stageCapability));
                    break;
                case NormalControl normalControl:
                    AddSlot(new NormalMaterialSlot(0, descriptor.name, descriptor.name, normalControl.space, stageCapability));
                    break;
                case TangentControl tangentControl:
                    AddSlot(new TangentMaterialSlot(0, descriptor.name, descriptor.name, tangentControl.space, stageCapability));
                    break;
                case ColorControl colorControl:
                    var colorMode = colorControl.hdr ? ColorMode.HDR : ColorMode.Default;
                    AddSlot(new ColorRGBMaterialSlot(0, descriptor.name, descriptor.name, SlotType.Input, colorControl.value, colorMode, stageCapability));
                    break;
                case ColorRGBAControl colorRGBAControl:
                    AddSlot(new ColorRGBAMaterialSlot(0, descriptor.name, descriptor.name, SlotType.Input, colorRGBAControl.value, stageCapability));
                    break;
                case FloatControl floatControl:
                    AddSlot(new Vector1MaterialSlot(0, descriptor.name, descriptor.name, SlotType.Input, floatControl.value, stageCapability));
                    break;
            }
            RemoveSlotsNameNotMatching(new int[] {0});
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
        {
            if(m_Descriptor == null || stageCapability != m_Descriptor.shaderStage.GetShaderStageCapability())
                return NeededCoordinateSpace.None;
            
            return m_Requirements.requiresNormal;
        }

        public NeededCoordinateSpace RequiresViewDirection(ShaderStageCapability stageCapability)
        {
            if(m_Descriptor == null || stageCapability != m_Descriptor.shaderStage.GetShaderStageCapability())
                return NeededCoordinateSpace.None;

            return m_Requirements.requiresViewDir;
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            if(m_Descriptor == null || stageCapability != m_Descriptor.shaderStage.GetShaderStageCapability())
                return NeededCoordinateSpace.None;
            
            return m_Requirements.requiresPosition;
        }

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability)
        {
            if(m_Descriptor == null || stageCapability != m_Descriptor.shaderStage.GetShaderStageCapability())
                return NeededCoordinateSpace.None;
            
            return m_Requirements.requiresTangent;
        }

        public NeededCoordinateSpace RequiresBitangent(ShaderStageCapability stageCapability)
        {
            if(m_Descriptor == null || stageCapability != m_Descriptor.shaderStage.GetShaderStageCapability())
                return NeededCoordinateSpace.None;
            
            return m_Requirements.requiresBitangent;
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            if(m_Descriptor == null || stageCapability != m_Descriptor.shaderStage.GetShaderStageCapability())
                return false;
            
            return m_Requirements.requiresMeshUVs.Contains(channel);
        }

        public bool RequiresScreenPosition(ShaderStageCapability stageCapability)
        {
            if(m_Descriptor == null || stageCapability != m_Descriptor.shaderStage.GetShaderStageCapability())
                return false;
            
            return m_Requirements.requiresScreenPosition;
        }

        public bool RequiresVertexColor(ShaderStageCapability stageCapability)
        {
            if(m_Descriptor == null || stageCapability != m_Descriptor.shaderStage.GetShaderStageCapability())
                return false;
            
            return m_Requirements.requiresVertexColor;
        }

        public override void OnBeforeSerialize()
        {
            m_SerializedDescriptor = $"{m_Descriptor.tag}.{m_Descriptor.name}";
            base.OnBeforeSerialize();
        }
    }
}