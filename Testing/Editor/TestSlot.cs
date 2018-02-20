using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public class TestSlot : MaterialSlot
    {
        public TestSlot(int slotId, string displayName, SlotType slotType, ShaderStage shaderStage = ShaderStage.Dynamic, bool hidden = false)
            : base(slotId, displayName, displayName, slotType, shaderStage, hidden) {}

        public TestSlot(int slotId, string displayName, SlotType slotType, int priority, ShaderStage shaderStage = ShaderStage.Dynamic, bool hidden = false)
            : base(slotId, displayName, displayName, slotType, priority, shaderStage, hidden) {}

        public override SlotValueType valueType
        {
            get { throw new System.NotImplementedException(); }
        }

        public override ConcreteSlotValueType concreteValueType
        {
            get { throw new System.NotImplementedException(); }
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            throw new System.NotImplementedException();
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            throw new System.NotImplementedException();
        }
    }
}
