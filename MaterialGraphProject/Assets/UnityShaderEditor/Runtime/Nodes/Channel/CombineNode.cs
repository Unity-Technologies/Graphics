using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Channel/Combine")]
    public class CombineNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        protected const string kInputSlot0Name = "Input1";
        protected const string kInputSlot1Name = "Input2";
        protected const string kInputSlot2Name = "Input3";
        protected const string kInputSlot3Name = "Input4";

        protected const string kOutputSlotName = "Output";

        public const int InputSlot0Id = 0;
        public const int InputSlot1Id = 1;
        public const int InputSlot2Id = 2;
        public const int InputSlot3Id = 3;

        public const int OutputSlotId = 4;

        public CombineNode()
        {
            name = "Combine";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(InputSlot0Id, kInputSlot0Name, kInputSlot0Name, SlotType.Input, SlotValueType.Dynamic, Vector4.zero));
            AddSlot(new MaterialSlot(InputSlot1Id, kInputSlot1Name, kInputSlot1Name, SlotType.Input, SlotValueType.Dynamic, Vector4.zero));
            AddSlot(new MaterialSlot(InputSlot2Id, kInputSlot2Name, kInputSlot2Name, SlotType.Input, SlotValueType.Dynamic, Vector4.zero));
            AddSlot(new MaterialSlot(InputSlot3Id, kInputSlot3Name, kInputSlot3Name, SlotType.Input, SlotValueType.Dynamic, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, SlotValueType.Vector4, Vector4.zero));
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] { InputSlot0Id, InputSlot1Id, InputSlot2Id, InputSlot3Id, OutputSlotId}; }
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            NodeUtils.SlotConfigurationExceptionIfBadConfiguration(this, new[] { InputSlot0Id, InputSlot1Id, InputSlot2Id, InputSlot3Id }, new[] { OutputSlotId });
            visitor.AddShaderChunk(precision + "4 " + GetVariableNameForSlot(OutputSlotId) + " = " + GetNodeBody(generationMode) + ";", true);
        }

        private int GetChannelCountForInput(int inputSlotId)
        {
            var inputSlot = FindInputSlot<MaterialSlot>(inputSlotId);
            if (inputSlot == null)
                return 0;
            int numInputChannels = (int)inputSlot.concreteValueType;
            if (owner.GetEdges(inputSlot.slotReference).ToList().Count() == 0)
                numInputChannels = 0;

            return numInputChannels;
        }

        private void AddChannelsFromInputSlot(GenerationMode generationMode, int inputSlot, ref string outputString, ref int freeChannel)
        {
            string[] channelNames = { "r", "g", "b", "a" };

            int numChannel = GetChannelCountForInput(inputSlot);
            numChannel = Math.Min(freeChannel, numChannel);
            if (numChannel <= 0)
                return;

            string channelInputName = GetSlotValue(inputSlot, generationMode);
            outputString += channelInputName;

            freeChannel -= numChannel;
            if (freeChannel != 0)
                outputString += ",";
        }

        protected string GetNodeBody(GenerationMode generationMode)
        {
            string outputString = precision + "4(";
            int freeChannels = 4;
            AddChannelsFromInputSlot(generationMode, InputSlot0Id, ref outputString, ref freeChannels);
            AddChannelsFromInputSlot(generationMode, InputSlot1Id, ref outputString, ref freeChannels);
            AddChannelsFromInputSlot(generationMode, InputSlot2Id, ref outputString, ref freeChannels);
            AddChannelsFromInputSlot(generationMode, InputSlot3Id, ref outputString, ref freeChannels);

            for (int i = freeChannels; i > 0; --i)
            {
                outputString += "0.0";
                if (i > 1)
                    outputString += ", ";
            }
            outputString += ")";

            return outputString;
        }
    }
}
