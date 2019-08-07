using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class SplatConditionsInputMaterialSlot : Vector1MaterialSlot
    {
        [SerializeField]
        private int m_BlendWeightSlotId = -1;

        public SplatConditionsInputMaterialSlot()
        { }

        public SplatConditionsInputMaterialSlot(int slotId, string displayName, string shaderOutputName, int blendWeightSlot)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment)
        {
            m_BlendWeightSlotId = blendWeightSlot;
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            var blendWeightSlot = owner.FindInputSlot<Vector1MaterialSlot>(m_BlendWeightSlotId);
            if (blendWeightSlot != null)
                return owner.GetSlotValue(m_BlendWeightSlotId, generationMode);
            return base.GetDefaultValue(generationMode);
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            if (foundSlot is SplatConditionsInputMaterialSlot splatConditionsSlot)
                m_BlendWeightSlotId = splatConditionsSlot.m_BlendWeightSlotId;
        }
    }
}
