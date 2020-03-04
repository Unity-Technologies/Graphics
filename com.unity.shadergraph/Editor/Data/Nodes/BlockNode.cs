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
        ContextData m_Context;

        [NonSerialized]
        int m_Index;

        [NonSerialized]
        BlockFieldDescriptor m_Descriptor;

        public BlockNode()
        {
            // TODO: Temporary. Remove.
            RegisterCallback(OnNodeChanged);
        }

        // TODO: Temporary hack to update previews 
        // TODO: Can be removed when preview manager no longer requires Master nodes
        void OnNodeChanged(AbstractMaterialNode inNode, ModificationScope scope)
        {
            if(owner != null)
            {
                var outputNode = owner.outputNode;
                outputNode?.Dirty(scope);
            }
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

        // Because the GraphData is deserialized after its child elements
        // the descriptor list is not built (and owner is not set)
        // at the time of node deserialization
        // Therefore we need to deserialize this element at GraphData.OnAfterDeserialize
        public string serializedDescriptor => m_SerializedDescriptor;

        public BlockFieldDescriptor descriptor
        {
            get => m_Descriptor;
            set => m_Descriptor = value;
        }

        public void Init(BlockFieldDescriptor fieldDescriptor)
        {
            name = $"{fieldDescriptor.tag}.{fieldDescriptor.name}";
            m_Descriptor = fieldDescriptor;
            AddSlot();
        }

        void AddSlot()
        {
            var displayName = descriptor.name;
            var referenceName = descriptor.name;
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
            if(m_Descriptor != null && m_Descriptor.contextStage == ContextStage.Vertex)
                return ShaderStageCapability.Vertex;

            return ShaderStageCapability.Fragment;
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
        {
            if(stageCapability != GetStageCapability())
                return NeededCoordinateSpace.None;
            
            return m_Descriptor.requirements.requiresNormal;
        }

        public NeededCoordinateSpace RequiresViewDirection(ShaderStageCapability stageCapability)
        {
            if(stageCapability != GetStageCapability())
                return NeededCoordinateSpace.None;

            return m_Descriptor.requirements.requiresViewDir;
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            if(stageCapability != GetStageCapability())
                return NeededCoordinateSpace.None;
            
            return m_Descriptor.requirements.requiresPosition;
        }

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability)
        {
            if(stageCapability != GetStageCapability())
                return NeededCoordinateSpace.None;
            
            return m_Descriptor.requirements.requiresTangent;
        }

        public NeededCoordinateSpace RequiresBitangent(ShaderStageCapability stageCapability)
        {
            if(stageCapability != GetStageCapability())
                return NeededCoordinateSpace.None;
            
            return m_Descriptor.requirements.requiresBitangent;
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            if(stageCapability != GetStageCapability())
                return false;
            
            return m_Descriptor.requirements.requiresMeshUVs.Contains(channel);
        }

        public bool RequiresScreenPosition(ShaderStageCapability stageCapability)
        {
            if(stageCapability != GetStageCapability())
                return false;
            
            return m_Descriptor.requirements.requiresScreenPosition;
        }

        public bool RequiresVertexColor(ShaderStageCapability stageCapability)
        {
            if(stageCapability != GetStageCapability())
                return false;
            
            return m_Descriptor.requirements.requiresVertexColor;
        }

        public override void OnBeforeSerialize()
        {
            m_SerializedDescriptor = $"{m_Descriptor.tag}.{m_Descriptor.name}";
            base.OnBeforeSerialize();
        }
    }
}