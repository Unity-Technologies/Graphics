using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class SplatConditionsInputMaterialSlot : Vector4MaterialSlot
    {
        [SerializeField]
        private int m_BlendWeightSlotId = -1;

        public SplatConditionsInputMaterialSlot()
        { }

        public SplatConditionsInputMaterialSlot(int slotId, string displayName, string shaderOutputName, int blendWeightSlot)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment)
        {
            m_BlendWeightSlotId = blendWeightSlot;
        }

        public override VisualElement InstantiateControl()
        {
            return new LabelSlotControlView("Blend Weights > 0");
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            var blendWeightSlot = owner.FindInputSlot<Vector4MaterialSlot>(m_BlendWeightSlotId);
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
