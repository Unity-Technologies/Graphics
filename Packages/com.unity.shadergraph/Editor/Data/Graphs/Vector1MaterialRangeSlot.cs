using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class Vector1MaterialRangeSlot : Vector1MaterialSlot
    {
        [SerializeField]
        Vector2 m_sliderRange = new Vector2(0.0f, 1.0f);

        [SerializeField]
        SliderType m_SliderType = SliderType.Default;

        [SerializeField]
        float m_SliderPower = 3.0f;

        internal Vector1MaterialRangeSlot() { }

        public Vector1MaterialRangeSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            float value,
            Vector2 range,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, value, stageCapability: stageCapability, hidden: hidden)
        {
            this.m_sliderRange = range;
        }


        internal Vector1MaterialRangeSlot(int slotId, Vector1ShaderProperty fromProperty)
            : base(slotId, fromProperty)
        {
            m_sliderRange = fromProperty.rangeValues;
            m_SliderType = fromProperty.sliderType;
            m_SliderPower = fromProperty.sliderPower;
        }

        public override VisualElement InstantiateControl()
        {
            return new SliderSlotControlView(this);
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            base.CopyValuesFrom(foundSlot);
            value = Mathf.Clamp(value, m_sliderRange.x, m_sliderRange.y);
        }

        class SliderSlotControlView : VisualElement
        {
            Vector1MaterialRangeSlot m_Slot;

            public SliderSlotControlView(Vector1MaterialRangeSlot slot)
            {
                m_Slot = slot;

                if (!slot.hideConnector)
                    styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/SliderSlotControlView"));
                else styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/SliderSlotControlView"));


                var sliderField = slot.hideConnector
                    ? new Slider(m_Slot.RawDisplayName(), m_Slot.m_sliderRange.x, m_Slot.m_sliderRange.y)
                    : new Slider(m_Slot.m_sliderRange.x, m_Slot.m_sliderRange.y);

                sliderField.value = slot.value;

                sliderField.RegisterValueChangedCallback(OnValueChange);

                
                Add(sliderField);
            }

            void OnValueChange(ChangeEvent<float> evt)
            {
                if (evt.newValue != m_Slot.value)
                {
                    m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Slider Change");
                    m_Slot.value = evt.newValue;
                    m_Slot.owner.Dirty(ModificationScope.Node);
                }
            }
        }
    }
}
