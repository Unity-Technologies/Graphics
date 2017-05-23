using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Channel/Split")]
    public class SplitNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        protected const string kInputSlotName = "Input";
        protected const string kOutputSlotRName = "R";
        protected const string kOutputSlotGName = "G";
        protected const string kOutputSlotBName = "B";
        protected const string kOutputSlotAName = "A";
        protected const string kOutputSlotRGBName = "RGB";
        protected const string kOutputSlotRGName = "RG";

        public const int InputSlotId = 0;
        public const int OutputSlotRId = 1;
        public const int OutputSlotGId = 2;
        public const int OutputSlotBId = 3;
        public const int OutputSlotAId = 4;
        public const int OutputSlotRGBId = 5;
        public const int OutputSlotRGId = 6;
        
        public SplitNode()
        {
            name = "Split";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, SlotValueType.Dynamic, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotRId, kOutputSlotRName, kOutputSlotRName, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotGId, kOutputSlotGName, kOutputSlotGName, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotBId, kOutputSlotBName, kOutputSlotBName, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotAId, kOutputSlotAName, kOutputSlotAName, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotRGBId, kOutputSlotRGBName, kOutputSlotRGBName, SlotType.Output, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotRGId, kOutputSlotRGName, kOutputSlotRGName, SlotType.Output, SlotValueType.Vector2, Vector4.zero));
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] { InputSlotId, OutputSlotRId, OutputSlotGId, OutputSlotBId, OutputSlotAId, OutputSlotRGBId, OutputSlotRGId }; }
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            NodeUtils.SlotConfigurationExceptionIfBadConfiguration(this, new[] { InputSlotId }, new[] { OutputSlotRId, OutputSlotGId, OutputSlotBId, OutputSlotRGBId, OutputSlotRGId });
            string inputValue = GetSlotValue(InputSlotId, generationMode);
            visitor.AddShaderChunk(precision +"4 "+ GetVariableNameForNode() + " = " + GetNodeBody(inputValue) + ";", true);
        }

        protected string GetNodeBody(string inputValue)
        {
            string[] channelNames = { "r", "g", "b", "a" };
            var inputSlot = FindInputSlot<MaterialSlot>(InputSlotId);
            if (inputSlot == null)
                return "1.0";

            int numInputChannels = (int)inputSlot.concreteValueType;
            if (owner.GetEdges(inputSlot.slotReference).ToList().Count() == 0)
                numInputChannels = 0;

            string outputString = precision + "4(";
            if (numInputChannels == 0)
            {
                outputString += "1.0, 1.0, 1.0, 1.0)";
            }
            else
            {
                //float4(arg1, 1.0, 1.0)
                outputString += inputValue;
                for (int i = numInputChannels; i < 4; i++)
                {
                    outputString += ", 1.0";
                }
                outputString += ")";
            }
            return outputString;
        }

        public override string GetVariableNameForSlot(int slotId)
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
                case OutputSlotRGBId:
                    slotOutput = ".rgb";
                    break;
                case OutputSlotRGId:
                    slotOutput = ".rg";
                    break;
                default:
                    slotOutput = "";
                    break;
            } 
            return GetVariableNameForNode() + slotOutput;
        }
    }
}
