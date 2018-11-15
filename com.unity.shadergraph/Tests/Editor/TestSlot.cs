using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    class TestSlot : MaterialSlot
    {
        public TestSlot() {}

        public TestSlot(int slotId, string displayName, SlotType slotType, ShaderStageCapability stageCapability = ShaderStageCapability.All, bool hidden = false)
            : base(slotId, displayName, displayName, slotType, stageCapability, hidden) {}

        public TestSlot(int slotId, string displayName, SlotType slotType, int priority, ShaderStageCapability stageCapability = ShaderStageCapability.All, bool hidden = false)
            : base(slotId, displayName, displayName, slotType, priority, stageCapability, hidden) {}

        public override SlotValueType valueType
        {
            get { return SlotValueType.Vector4; }
        }

        public override ConcreteSlotValueType concreteValueType
        {
            get { return ConcreteSlotValueType.Vector4; }
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
        }
    }
}
