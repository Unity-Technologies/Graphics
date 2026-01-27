using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class Vector1MaterialEnumSlot : Vector1MaterialSlot
    {
        [SerializeField]
        List<string> options;

        [SerializeField]
        List<int> values;

        internal Vector1MaterialEnumSlot() { }

        internal Vector1MaterialEnumSlot(int slotId, Vector1ShaderProperty fromProperty)
            : base(slotId, fromProperty)
        {
            options = new();
            values = new();

            options.AddRange(fromProperty.enumNames);
            values.AddRange(fromProperty.enumValues);
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            base.CopyValuesFrom(foundSlot);
            if (!values.Contains((int)value))
                value = 0;
        }

        public override VisualElement InstantiateControl()
        {
            return new EnumSlotControlView(this);
        }

        class EnumSlotControlView : VisualElement
        {
            Vector1MaterialEnumSlot m_Slot;

            public EnumSlotControlView(Vector1MaterialEnumSlot slot)
            {
                m_Slot = slot;

                var dropdownField = slot.hideConnector
                    ? new DropdownField(slot.RawDisplayName(), slot.options, 0)
                    : new DropdownField(slot.options, 0);

                dropdownField.RegisterValueChangedCallback(OnValueChange);
                Add(dropdownField);
            }

            void OnValueChange(ChangeEvent<string> evt)
            {
                int newIndex = m_Slot.options.FindIndex(e => e == evt.newValue);

                int newValue = m_Slot.values[newIndex]; // TODO Safety

                if (newValue != m_Slot.value)
                {
                    m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Dropdown Change");
                    m_Slot.value = newValue;
                    m_Slot.owner.Dirty(ModificationScope.Node);
                }
            }
        }
    }
}
