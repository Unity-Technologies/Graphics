using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Channel/Combine")]
    public class CombineChannelsNode : PropertyNode, IGeneratesBodyCode
    {
        protected const string kOutputSlot0Id = "Input0";
        protected const string kOutputSlot1Id = "Input1";
        protected const string kOutputSlot2Id = "Input2";
        protected const string kOutputSlot3Id = "Input3";

        protected const string kOutputSlotRGBAName = "RGBA";
        protected const string kOutputSlotRGBSlotName = "RGB";
        protected const string kOutputSlotRGSlotName = "RG";

        public const int InputSlot0Id = 0;
        public const int InputSlot1Id = 1;
        public const int InputSlot2Id = 2;
        public const int InputSlot3Id = 3;

        public const int OutputSlotRGBAId = 4;
        public const int OutputSlotRGBId = 5;
        public const int OutputSlotRGId = 6;

        public CombineChannelsNode()
        {
            name = "ChannelsCombine";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(InputSlot0Id, kOutputSlot0Id, kOutputSlot0Id, SlotType.Input, SlotValueType.Dynamic, Vector4.zero));
            AddSlot(new MaterialSlot(InputSlot1Id, kOutputSlot1Id, kOutputSlot1Id, SlotType.Input, SlotValueType.Dynamic, Vector4.zero));
            AddSlot(new MaterialSlot(InputSlot2Id, kOutputSlot2Id, kOutputSlot2Id, SlotType.Input, SlotValueType.Dynamic, Vector4.zero));
            AddSlot(new MaterialSlot(InputSlot3Id, kOutputSlot3Id, kOutputSlot3Id, SlotType.Input, SlotValueType.Dynamic, Vector4.zero));

            AddSlot(new MaterialSlot(OutputSlotRGBAId, kOutputSlotRGBAName, kOutputSlotRGBAName, SlotType.Output, SlotValueType.Vector4, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotRGBId, kOutputSlotRGBSlotName, kOutputSlotRGBSlotName, SlotType.Output, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotRGId, kOutputSlotRGSlotName, kOutputSlotRGSlotName, SlotType.Output, SlotValueType.Vector2, Vector4.zero));
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] { InputSlot0Id, InputSlot1Id, InputSlot2Id, InputSlot3Id, OutputSlotRGBAId, OutputSlotRGBId, OutputSlotRGId }; }
        }

        [SerializeField]
        private Vector4 m_Value;

        public Vector4 value
        {
            get { return m_Value; }
            set
            {
                if (m_Value == value)
                    return;

                m_Value = value;

                if (onModified != null)
                    onModified(this, ModificationScope.Node);
            }
        }

        public override PropertyType propertyType { get { return PropertyType.Vector4; } }

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            if (exposedState == ExposedState.Exposed)
                visitor.AddShaderProperty(new VectorPropertyChunk(propertyName, description, m_Value, PropertyChunk.HideState.Visible));
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (exposedState == ExposedState.Exposed || generationMode.IsPreview())
                visitor.AddShaderChunk(precision + "4 " + propertyName + ";", true);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            //if (exposedState == ExposedState.Exposed || generationMode.IsPreview())
            //  return;
            var inputValue = GetSlotValue(InputSlot0Id, generationMode);
            visitor.AddShaderChunk(precision + "4 " + propertyName + " = " + inputValue + ";", false);
            //visitor.AddShaderChunk(precision + "4 " + propertyName + " = " + precision + "4 (" + m_Value.x + ", " + m_Value.y + ", " + m_Value.z + ", " + m_Value.w + ");", true);
        }

        protected virtual MaterialSlot GetInputSlot()
        {
            return new MaterialSlot(InputSlot0Id, GetInputSlotName(), kOutputSlot0Id, SlotType.Input, SlotValueType.Dynamic, Vector4.zero);
        }

        protected virtual string GetInputSlotName() { return "Input"; }

        public override PreviewProperty GetPreviewProperty()
        {
            return new PreviewProperty
            {
                m_Name = propertyName,
                m_PropType = PropertyType.Vector4,
                m_Vector4 = m_Value
            };
        }

        /*public override string GetVariableNameForSlot(int slotId)
        {
            string slotOutput;
            switch (slotId)
            {
                case OutputSlotRId:
                    slotOutput = ".r";
                    break;
                case OutputSlotGId:
                    slotOutput = ".g";
                    break;
                case OutputSlotBId:
                    slotOutput = ".b";
                    break;
                case OutputSlotAId:
                    slotOutput = ".a";
                    break;
                default:
                    slotOutput = "";
                    break;
            }
            return propertyName + slotOutput;
            //return GetVariableNameForNode() + slotOutput;
        }*/

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(GetPreviewProperty());
        }
    }
}
