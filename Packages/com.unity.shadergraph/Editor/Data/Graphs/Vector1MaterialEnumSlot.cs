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

        public Vector1MaterialEnumSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            IEnumerable<string> options,
            float value, // should match a value in the options.
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, value, stageCapability: stageCapability, hidden: hidden)
        {
            this.options = new(options);
            this.values = new();
            for (int i = 0; i < this.options.Count; ++i)
                values.Add(i);
        }

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

                int idx = m_Slot.values.FindIndex(e => e == (int)slot.value);
                if (idx < 0 || idx >= slot.options.Count)
                    idx = 0;

                var dropdownField = slot.hideConnector
                    ? new DropdownField(slot.RawDisplayName(), slot.options, idx)
                    : new DropdownField(slot.options, idx);

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
