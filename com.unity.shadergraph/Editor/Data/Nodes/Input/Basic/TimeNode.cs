using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Basic", "Time")]
    class TimeNode : AbstractMaterialNode, IMayRequireTime
    {
        private const string kOutputSlotName = "Time";
        private const string kOutputSlot1Name = "Sine Time";
        private const string kOutputSlot2Name = "Cosine Time";
        private const string kOutputSlot3Name = "Delta Time";
        private const string kOutputSlot4Name = "Smooth Delta";

        public const int OutputSlotId = 0;
        public const int OutputSlot1Id = 1;
        public const int OutputSlot2Id = 2;
        public const int OutputSlot3Id = 3;
        public const int OutputSlot4Id = 4;

        public TimeNode()
        {
            name = "Time";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSlot1Id, kOutputSlot1Name, kOutputSlot1Name, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSlot2Id, kOutputSlot2Name, kOutputSlot2Name, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSlot3Id, kOutputSlot3Name, kOutputSlot3Name, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSlot4Id, kOutputSlot4Name, kOutputSlot4Name, SlotType.Output, 0));
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] { OutputSlotId, OutputSlot1Id, OutputSlot2Id, OutputSlot3Id, OutputSlot4Id }; }
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            switch (slotId)
            {
                case OutputSlot1Id:
                    return "IN.TimeParameters.y";
                case OutputSlot2Id:
                    return "IN.TimeParameters.z";
                case OutputSlot3Id:
                    return "unity_DeltaTime.x";
                case OutputSlot4Id:
                    return "unity_DeltaTime.z";
                default:
                    return "IN.TimeParameters.x";
            }
        }

        public bool RequiresTime()
        {
            return true;
        }
    }
}
