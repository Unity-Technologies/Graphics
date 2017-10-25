using System;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class Matrix3MaterialSlot : MaterialSlot
    {

        public Matrix3MaterialSlot()
        {
        }

        public Matrix3MaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStage shaderStage = ShaderStage.Dynamic,
            bool hidden = false)
            :base(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden)
        {
        }

        protected override string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            return precision + "3x3 (1,0,0,0,1,0,0,0,1)";
        }

        public override SlotValueType valueType { get { return SlotValueType.Matrix3; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Matrix3; } }
    }
}