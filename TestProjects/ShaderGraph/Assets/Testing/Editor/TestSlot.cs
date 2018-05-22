using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public class TestSlot : MaterialSlot
    {
        public TestSlot() {}

        public TestSlot(int slotId, string displayName, SlotType slotType, ShaderStage shaderStage = ShaderStage.Dynamic, bool hidden = false)
            : base(slotId, displayName, displayName, slotType, shaderStage, hidden) {}

        public TestSlot(int slotId, string displayName, SlotType slotType, int priority, ShaderStage shaderStage = ShaderStage.Dynamic, bool hidden = false)
            : base(slotId, displayName, displayName, slotType, priority, shaderStage, hidden) {}

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
