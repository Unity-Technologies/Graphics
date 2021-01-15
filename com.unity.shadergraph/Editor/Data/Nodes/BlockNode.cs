using System;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
        BlockFieldDescriptor m_Descriptor;


        [SerializeField]
        private uint m_TestProperty = 0;

        public uint testProperty
        {
            get => m_TestProperty;
            set => m_TestProperty = value;
        }


        ///////////////////////////////////////////////////
        // Custom block stuff.

        [SerializeField]
        private bool m_IsCustomBlock = false;

        internal bool isCustomBlock
        {
            get => m_IsCustomBlock;
            set => m_IsCustomBlock = value;
        }

        internal enum CustomBlockType { Float, Vector2, Vector3, Vector4 }

        [SerializeField]
        internal CustomBlockType customBlockType = CustomBlockType.Vector4;
        internal string m_customBlockName;

        internal string customBlockName
        {
            get => m_customBlockName ?? descriptor?.displayName ?? "CustomInterpolator";
            set => m_customBlockName = value;
        }

    internal void RenewCustomBlockFieldDescriptor()
        {
            if (!isCustomBlock)
                return;

            // todo, sanitize this.
            var referenceName = customBlockName;
            var define = "VERTEXDESCRIPTION_" + customBlockName.ToUpper();

            IControl control = null;
            // control the subset of exposed property types, for now.
            switch(customBlockType)
            {
                case CustomBlockType.Float: control = new FloatControl(default(float)); break;
                case CustomBlockType.Vector2: control = new Vector2Control(default(Vector2)); break;
                case CustomBlockType.Vector3: control = new Vector3Control(default(Vector3)); break;
                case CustomBlockType.Vector4: control = new Vector4Control(default(Vector4)); break;
            }

            // create our new block field descriptor, which drives the rest of the behavior.
            descriptor = new BlockFieldDescriptor(BlockFields.VertexDescription.name, referenceName, define, control, ShaderStage.Vertex);

            AddSlotFromControlType();

            owner?.ValidateGraph();
            // I think the only revalidation use-case is if there are CINodes that referred to our a name
            //foreach (var cibclient in owner?.GetNodes<CustomInterpolatorNode>().Where(cib => cib.customBlockNodeName == customBlockName))
            //    cibclient.ValidateNode();
        }

        public void InitCustomBlockNode()
        {
            name = $"{BlockFields.VertexDescription.name}.CustomInterpolator";
            isCustomBlock = true;
            RenewCustomBlockFieldDescriptor();
        }

        ///////////////////////////////////////////////////



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

        public int index => contextData.blocks.IndexOf(this);

        public BlockFieldDescriptor descriptor
        {
            get => m_Descriptor;
            set => m_Descriptor = value;
        }

        public void Init(BlockFieldDescriptor fieldDescriptor)
        {
            name = $"{fieldDescriptor.tag}.{(isCustomBlock ? "CustomInterpolator" : fieldDescriptor.name)}";
            m_Descriptor = fieldDescriptor;

            // TODO: This exposes the MaterialSlot API
            // TODO: This needs to be removed but is currently required by HDRP for DiffusionProfileInputMaterialSlot
            if (m_Descriptor is CustomSlotBlockFieldDescriptor customSlotDescriptor)
            {
                var newSlot = customSlotDescriptor.createSlot();
                AddSlot(newSlot);
                RemoveSlotsNameNotMatching(new int[] {0});
                return;
            }

            // if we are a custom block and we deserialized, we want to make sure we preserve the existing descriptor name.
            customBlockName = fieldDescriptor.name;
            AddSlotFromControlType();
        }

        void AddSlotFromControlType()
        {
            // TODO: this should really just use callbacks like the CustomSlotBlockFieldDescriptor.. then we wouldn't need this switch to make a copy
            var stageCapability = m_Descriptor.shaderStage.GetShaderStageCapability();
            switch (descriptor.control)
            {
                case PositionControl positionControl:
                    AddSlot(new PositionMaterialSlot(0, descriptor.displayName, descriptor.name, positionControl.space, stageCapability), false);
                    break;
                case NormalControl normalControl:
                    AddSlot(new NormalMaterialSlot(0, descriptor.displayName, descriptor.name, normalControl.space, stageCapability), false);
                    break;
                case TangentControl tangentControl:
                    AddSlot(new TangentMaterialSlot(0, descriptor.displayName, descriptor.name, tangentControl.space, stageCapability), false);
                    break;
                case VertexColorControl vertexColorControl:
                    AddSlot(new VertexColorMaterialSlot(0, descriptor.displayName, descriptor.name, stageCapability), false);
                    break;
                case ColorControl colorControl:
                    var colorMode = colorControl.hdr ? ColorMode.HDR : ColorMode.Default;
                    AddSlot(new ColorRGBMaterialSlot(0, descriptor.displayName, descriptor.name, SlotType.Input, colorControl.value, colorMode, stageCapability), false);
                    break;
                case ColorRGBAControl colorRGBAControl:
                    AddSlot(new ColorRGBAMaterialSlot(0, descriptor.displayName, descriptor.name, SlotType.Input, colorRGBAControl.value, stageCapability), false);
                    break;
                case FloatControl floatControl:
                    AddSlot(new Vector1MaterialSlot(0, descriptor.displayName, descriptor.name, SlotType.Input, floatControl.value, stageCapability), false);
                    break;
                case Vector2Control vector2Control:
                    AddSlot(new Vector2MaterialSlot(0, descriptor.displayName, descriptor.name, SlotType.Input, vector2Control.value, stageCapability), false);
                    break;
                case Vector3Control vector3Control:
                    AddSlot(new Vector3MaterialSlot(0, descriptor.displayName, descriptor.name, SlotType.Input, vector3Control.value, stageCapability), false);
                    break;
                case Vector4Control vector4Control:
                    AddSlot(new Vector4MaterialSlot(0, descriptor.displayName, descriptor.name, SlotType.Input, vector4Control.value, stageCapability), false);
                    break;
            }
            RemoveSlotsNameNotMatching(new int[] {0});
        }

        public override string GetVariableNameForNode()
        {
            // Temporary block nodes have temporary guids that cannot be used to set preview data
            // Since each block is unique anyway we just omit the guid
            return NodeUtils.GetHLSLSafeName(name);
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
        {
            if (stageCapability != m_Descriptor.shaderStage.GetShaderStageCapability())
                return NeededCoordinateSpace.None;

            if (m_Descriptor.control == null)
                return NeededCoordinateSpace.None;

            var requirements = m_Descriptor.control.GetRequirements();
            return requirements.requiresNormal;
        }

        public NeededCoordinateSpace RequiresViewDirection(ShaderStageCapability stageCapability)
        {
            if (stageCapability != m_Descriptor.shaderStage.GetShaderStageCapability())
                return NeededCoordinateSpace.None;

            if (m_Descriptor.control == null)
                return NeededCoordinateSpace.None;

            var requirements = m_Descriptor.control.GetRequirements();
            return requirements.requiresViewDir;
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            if (stageCapability != m_Descriptor.shaderStage.GetShaderStageCapability())
                return NeededCoordinateSpace.None;

            if (m_Descriptor.control == null)
                return NeededCoordinateSpace.None;

            var requirements = m_Descriptor.control.GetRequirements();
            return requirements.requiresPosition;
        }

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability)
        {
            if (stageCapability != m_Descriptor.shaderStage.GetShaderStageCapability())
                return NeededCoordinateSpace.None;

            if (m_Descriptor.control == null)
                return NeededCoordinateSpace.None;

            var requirements = m_Descriptor.control.GetRequirements();
            return requirements.requiresTangent;
        }

        public NeededCoordinateSpace RequiresBitangent(ShaderStageCapability stageCapability)
        {
            if (stageCapability != m_Descriptor.shaderStage.GetShaderStageCapability())
                return NeededCoordinateSpace.None;

            if (m_Descriptor.control == null)
                return NeededCoordinateSpace.None;

            var requirements = m_Descriptor.control.GetRequirements();
            return requirements.requiresBitangent;
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            if (stageCapability != m_Descriptor.shaderStage.GetShaderStageCapability())
                return false;

            if (m_Descriptor.control == null)
                return false;

            var requirements = m_Descriptor.control.GetRequirements();
            return requirements.requiresMeshUVs.Contains(channel);
        }

        public bool RequiresScreenPosition(ShaderStageCapability stageCapability)
        {
            if (stageCapability != m_Descriptor.shaderStage.GetShaderStageCapability())
                return false;

            if (m_Descriptor.control == null)
                return false;

            var requirements = m_Descriptor.control.GetRequirements();
            return requirements.requiresScreenPosition;
        }

        public bool RequiresVertexColor(ShaderStageCapability stageCapability)
        {
            if (stageCapability != m_Descriptor.shaderStage.GetShaderStageCapability())
                return false;

            if (m_Descriptor.control == null)
                return false;

            var requirements = m_Descriptor.control.GetRequirements();
            return requirements.requiresVertexColor;
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            if (descriptor != null)
            {
                m_SerializedDescriptor = $"{m_Descriptor.tag}.{m_Descriptor.name}";
            }
        }
    }
}
