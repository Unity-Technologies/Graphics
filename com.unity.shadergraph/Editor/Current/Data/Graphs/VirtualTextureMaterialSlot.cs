using System;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class VirtualTextureMaterialSlot : MaterialSlot
    {
        public VirtualTextureMaterialSlot()
        { }

        public VirtualTextureMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, stageCapability, hidden)
        { }

        public override SlotValueType valueType { get { return SlotValueType.VirtualTexture; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.VirtualTexture; } }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
        }

        public override bool isDefaultValue => throw new Exception();
    }
}
