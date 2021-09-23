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
        , IMayRequireNDCPosition
        , IMayRequirePixelPosition
        , IMayRequireViewDirection
        , IMayRequirePosition
        , IMayRequirePositionPredisplacement
        , IMayRequireVertexColor
    {
        [SerializeField]
        string m_SerializedDescriptor;

        [NonSerialized]
        ContextData m_ContextData;

        [NonSerialized]
        BlockFieldDescriptor m_Descriptor;

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

        const string k_CustomBlockDefaultName = "CustomInterpolator";

        internal enum CustomBlockType { Float = 1, Vector2 = 2, Vector3 = 3, Vector4 = 4 }

        internal bool isCustomBlock { get => m_Descriptor?.isCustom ?? false; }

        internal string customName
        {
            get => m_Descriptor.name;
            set => OnCustomBlockFieldModified(value, customWidth);
        }

        internal CustomBlockType customWidth
        {
            get => (CustomBlockType)ControlToWidth(m_Descriptor.control);
            set => OnCustomBlockFieldModified(customName, value);
        }

        public void Init(BlockFieldDescriptor fieldDescriptor)
        {
            m_Descriptor = fieldDescriptor;

            // custom blocks can be "copied" via a custom Field Descriptor, we'll use the CI name instead though.
            name = !isCustomBlock
                ? $"{fieldDescriptor.tag}.{fieldDescriptor.name}"
                : $"{BlockFields.VertexDescription.name}.{k_CustomBlockDefaultName}";


            // TODO: This exposes the MaterialSlot API
            // TODO: This needs to be removed but is currently required by HDRP for DiffusionProfileInputMaterialSlot
            if (m_Descriptor is CustomSlotBlockFieldDescriptor customSlotDescriptor)
            {
                var newSlot = customSlotDescriptor.createSlot();
                AddSlot(newSlot);
                RemoveSlotsNameNotMatching(new int[] { 0 });
                return;
            }

            AddSlotFromControlType();
        }

        internal void InitCustomDefault()
        {
            Init(MakeCustomBlockField(k_CustomBlockDefaultName, CustomBlockType.Vector4));
        }

        private void AddSlotFromControlType(bool attemptToModifyExisting = true)
        {
            // TODO: this should really just use callbacks like the CustomSlotBlockFieldDescriptor. then we wouldn't need this switch to make a copy
            var stageCapability = m_Descriptor.shaderStage.GetShaderStageCapability();
            switch (descriptor.control)
            {
                case PositionControl positionControl:
                    AddSlot(new PositionMaterialSlot(0, descriptor.displayName, descriptor.name, positionControl.space, stageCapability), attemptToModifyExisting);
                    break;
                case NormalControl normalControl:
                    AddSlot(new NormalMaterialSlot(0, descriptor.displayName, descriptor.name, normalControl.space, stageCapability), attemptToModifyExisting);
                    break;
                case TangentControl tangentControl:
                    AddSlot(new TangentMaterialSlot(0, descriptor.displayName, descriptor.name, tangentControl.space, stageCapability), attemptToModifyExisting);
                    break;
                case VertexColorControl vertexColorControl:
                    AddSlot(new VertexColorMaterialSlot(0, descriptor.displayName, descriptor.name, stageCapability), attemptToModifyExisting);
                    break;
                case ColorControl colorControl:
                    var colorMode = colorControl.hdr ? ColorMode.HDR : ColorMode.Default;
                    AddSlot(new ColorRGBMaterialSlot(0, descriptor.displayName, descriptor.name, SlotType.Input, colorControl.value, colorMode, stageCapability), attemptToModifyExisting);
                    break;
                case ColorRGBAControl colorRGBAControl:
                    AddSlot(new ColorRGBAMaterialSlot(0, descriptor.displayName, descriptor.name, SlotType.Input, colorRGBAControl.value, stageCapability), attemptToModifyExisting);
                    break;
                case FloatControl floatControl:
                    AddSlot(new Vector1MaterialSlot(0, descriptor.displayName, descriptor.name, SlotType.Input, floatControl.value, stageCapability), attemptToModifyExisting);
                    break;
                case Vector2Control vector2Control:
                    AddSlot(new Vector2MaterialSlot(0, descriptor.displayName, descriptor.name, SlotType.Input, vector2Control.value, stageCapability), attemptToModifyExisting);
                    break;
                case Vector3Control vector3Control:
                    AddSlot(new Vector3MaterialSlot(0, descriptor.displayName, descriptor.name, SlotType.Input, vector3Control.value, stageCapability), attemptToModifyExisting);
                    break;
                case Vector4Control vector4Control:
                    AddSlot(new Vector4MaterialSlot(0, descriptor.displayName, descriptor.name, SlotType.Input, vector4Control.value, stageCapability), attemptToModifyExisting);
                    break;
            }
            RemoveSlotsNameNotMatching(new int[] { 0 });
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

        public NeededCoordinateSpace RequiresPositionPredisplacement(ShaderStageCapability stageCapability)
        {
            if (stageCapability != m_Descriptor.shaderStage.GetShaderStageCapability())
                return NeededCoordinateSpace.None;

            if (m_Descriptor.control == null)
                return NeededCoordinateSpace.None;

            var requirements = m_Descriptor.control.GetRequirements();
            return requirements.requiresPositionPredisplacement;
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

        public bool RequiresNDCPosition(ShaderStageCapability stageCapability)
        {
            if (stageCapability != m_Descriptor.shaderStage.GetShaderStageCapability())
                return false;

            if (m_Descriptor.control == null)
                return false;

            var requirements = m_Descriptor.control.GetRequirements();
            return requirements.requiresNDCPosition;
        }

        public bool RequiresPixelPosition(ShaderStageCapability stageCapability)
        {
            if (stageCapability != m_Descriptor.shaderStage.GetShaderStageCapability())
                return false;

            if (m_Descriptor.control == null)
                return false;

            var requirements = m_Descriptor.control.GetRequirements();
            return requirements.requiresPixelPosition;
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

        private void OnCustomBlockFieldModified(string name, CustomBlockType width)
        {
            if (!isCustomBlock)
            {
                Debug.LogWarning(String.Format("{0} is not a custom interpolator.", this.name));
                return;
            }

            m_Descriptor = MakeCustomBlockField(name, width);

            // TODO: Preserve the original slot's value and try to reapply after the slot is updated.
            AddSlotFromControlType(false);

            owner?.ValidateGraph();
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            if (descriptor != null)
            {
                if (isCustomBlock)
                {
                    int width = ControlToWidth(m_Descriptor.control);
                    m_SerializedDescriptor = $"{m_Descriptor.tag}.{m_Descriptor.name}#{width}";
                }
                else
                {
                    m_SerializedDescriptor = $"{m_Descriptor.tag}.{m_Descriptor.name}";
                }
            }
        }

        public override void OnAfterDeserialize()
        {
            // TODO: Go find someone to tell @esme not to do this.
            if (m_SerializedDescriptor.Contains("#"))
            {
                string descName = k_CustomBlockDefaultName;
                CustomBlockType descWidth = CustomBlockType.Vector4;
                var descTag = BlockFields.VertexDescription.name;

                name = $"{descTag}.{descName}";

                var wsplit = m_SerializedDescriptor.Split(new char[] { '#', '.' });

                try
                {
                    descWidth = (CustomBlockType)int.Parse(wsplit[2]);
                }
                catch
                {
                    Debug.LogWarning(String.Format("Bad width found while deserializing custom interpolator {0}, defaulting to 4.", m_SerializedDescriptor));
                    descWidth = CustomBlockType.Vector4;
                }

                IControl control;
                try { control = (IControl)FindSlot<MaterialSlot>(0).InstantiateControl(); }
                catch { control = WidthToControl((int)descWidth); }

                descName = NodeUtils.ConvertToValidHLSLIdentifier(wsplit[1]);
                m_Descriptor = new BlockFieldDescriptor(descTag, descName, "", control, ShaderStage.Vertex, isCustom: true);
            }
        }

        #region CustomInterpolatorHelpers
        private static BlockFieldDescriptor MakeCustomBlockField(string name, CustomBlockType width)
        {
            name = NodeUtils.ConvertToValidHLSLIdentifier(name);
            var referenceName = name;
            var define = "";
            IControl control = WidthToControl((int)width);
            var tag = BlockFields.VertexDescription.name;

            return new BlockFieldDescriptor(tag, referenceName, define, control, ShaderStage.Vertex, isCustom: true);
        }

        private static IControl WidthToControl(int width)
        {
            switch (width)
            {
                case 1: return new FloatControl(default(float));
                case 2: return new Vector2Control(default(Vector2));
                case 3: return new Vector3Control(default(Vector3));
                case 4: return new Vector4Control(default(Vector4));
                default: return null;
            }
        }

        private static int ControlToWidth(IControl control)
        {
            switch (control)
            {
                case FloatControl a: return 1;
                case Vector2Control b: return 2;
                case Vector3Control c: return 3;
                case Vector4Control d: return 4;
                default: return -1;
            }
        }

        #endregion
    }
}
