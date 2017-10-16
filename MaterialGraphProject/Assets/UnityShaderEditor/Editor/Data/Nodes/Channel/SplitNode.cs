using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Channel/Split")]
    public class SplitNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        const string kInputSlotName = "Input";
        const string kOutputSlotRName = "R";
        const string kOutputSlotGName = "G";
        const string kOutputSlotBName = "B";
        const string kOutputSlotAName = "A";

        public const int InputSlotId = 0;
        public const int OutputSlotRId = 1;
        public const int OutputSlotGId = 2;
        public const int OutputSlotBId = 3;
        public const int OutputSlotAId = 4;

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
            RemoveSlotsNameNotMatching(s_ValidSlots);
        }

        static int[] s_ValidSlots = { InputSlotId, OutputSlotRId, OutputSlotGId, OutputSlotBId, OutputSlotAId };
        static int[] s_OutputSlots = {OutputSlotRId, OutputSlotGId, OutputSlotBId, OutputSlotAId};

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            NodeUtils.SlotConfigurationExceptionIfBadConfiguration(this, new[] { InputSlotId }, new[] { OutputSlotRId, OutputSlotGId, OutputSlotBId });
            var inputValue = GetSlotValue(InputSlotId, generationMode);

            var inputSlot = FindInputSlot<MaterialSlot>(InputSlotId);
            var numInputChannels = 0;
            if (inputSlot != null)
            {
                numInputChannels = (int)SlotValueHelper.GetChannelCount(inputSlot.concreteValueType);
                if (numInputChannels > 4)
                    numInputChannels = 0;

                if (!owner.GetEdges(inputSlot.slotReference).Any())
                    numInputChannels = 0;
            }

            for (var i = 0; i < 4; i++)
            {
                var outputValue = i >= numInputChannels ? "1.0" : string.Format("{0}[{1}]", inputValue, i);
                visitor.AddShaderChunk(string.Format("{0} {1} = {2};", precision, GetVariableNameForSlot(s_OutputSlots[i]), outputValue), true);
            }
        }
    }
}
