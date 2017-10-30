using System;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class GradientMaterialSlot : MaterialSlot
    {
        public GradientMaterialSlot()
        {
        }

        public GradientMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStage shaderStage = ShaderStage.Dynamic,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden)
        {
        }

        public static readonly string DefaultGradientName = "ShaderGraph_DefaultGradient()";

        public override SlotValueType valueType { get { return SlotValueType.Gradient; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Gradient; } }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            return DefaultGradientName;
        }
    }
}
