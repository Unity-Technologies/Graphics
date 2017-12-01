using System;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Matrix2MaterialSlot : MaterialSlot
    {
        public Matrix2MaterialSlot()
        {
        }

        public Matrix2MaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStage shaderStage = ShaderStage.Dynamic,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden)
        {
        }

        protected override string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            return precision + "2x2 (1,0,0,1)";
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {}

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
        }

        public override SlotValueType valueType { get { return SlotValueType.Matrix2; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Matrix2; } }
    }
}
