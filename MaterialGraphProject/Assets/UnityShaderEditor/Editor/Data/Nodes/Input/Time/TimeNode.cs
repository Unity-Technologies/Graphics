using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input/Time/Time")]
    public class TimeNode : AbstractMaterialNode, IMayRequireTime
    {
        private const string kOutputSlotName = "Time";
        private const string kOutputSlotNameX = "Time/20";
        private const string kOutputSlotNameY = "Time";
        private const string kOutputSlotNameZ = "Time * 2";
        private const string kOutputSlotNameW = "Time * 3";

        public const int OutputSlotId = 0;
        public const int OutputSlotIdX = 1;
        public const int OutputSlotIdY = 2;
        public const int OutputSlotIdZ = 3;
        public const int OutputSlotIdW = 4;

        public TimeNode()
        {
            name = "Time";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new Vector1MaterialSlot(OutputSlotIdX, kOutputSlotNameX, kOutputSlotNameX, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSlotIdY, kOutputSlotNameY, kOutputSlotNameY, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSlotIdZ, kOutputSlotNameZ, kOutputSlotNameZ, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSlotIdW, kOutputSlotNameW, kOutputSlotNameW, SlotType.Output, 0));
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] {OutputSlotId, OutputSlotIdX, OutputSlotIdY, OutputSlotIdZ, OutputSlotIdW}; }
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            switch (slotId)
            {
                case OutputSlotIdX:
                    return "_Time.x";
                case OutputSlotIdY:
                    return "_Time.y";
                case OutputSlotIdZ:
                    return "_Time.z";
                case OutputSlotIdW:
                    return "_Time.w";
                default:
                    return "_Time";
            }
        }

        public bool RequiresTime()
        {
            return true;
        }
    }
}
