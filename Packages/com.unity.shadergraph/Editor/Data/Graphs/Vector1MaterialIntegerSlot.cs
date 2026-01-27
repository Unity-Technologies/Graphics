using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class Vector1MaterialIntegerSlot : Vector1MaterialSlot
    {
        internal Vector1MaterialIntegerSlot() { }

        internal Vector1MaterialIntegerSlot(int slotId, Vector1ShaderProperty property) : base(slotId, property) { }

        public override VisualElement InstantiateControl()
        {
            return new IntegerSlotControlView(this);
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            base.CopyValuesFrom(foundSlot);
            value = Mathf.RoundToInt(value);
        }

        class IntegerSlotControlView : VisualElement
        {
            Vector1MaterialIntegerSlot m_Slot;

            public IntegerSlotControlView(Vector1MaterialIntegerSlot slot)
            {
                m_Slot = slot;
                var integerField = slot.hideConnector
                    ? new IntegerField(slot.RawDisplayName())
                    : new IntegerField();

                integerField.value = (int)slot.value;
                integerField.RegisterValueChangedCallback(OnValueChange);
                Add(integerField);
            }

            void OnValueChange(ChangeEvent<int> evt)
            {
                if (evt.newValue != m_Slot.value)
                {
                    m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Integer Change");
                    m_Slot.value = evt.newValue;
                    m_Slot.owner.Dirty(ModificationScope.Node);
                }
            }
        }
    }
}
