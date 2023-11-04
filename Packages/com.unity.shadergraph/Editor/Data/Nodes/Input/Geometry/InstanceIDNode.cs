using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Geometry", "Instance ID")]
    class InstanceIDNode : AbstractMaterialNode, IMayRequireInstanceID
    {
        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public override bool hasPreview => false;

        public InstanceIDNode()
        {
            name = "Instance ID";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, (int)0, ShaderStageCapability.All));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return string.Format("IN.{0}", ShaderGeneratorNames.InstanceID);
        }

        public bool RequiresInstanceID(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
