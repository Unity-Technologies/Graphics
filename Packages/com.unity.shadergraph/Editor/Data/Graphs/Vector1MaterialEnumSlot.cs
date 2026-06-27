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

        // Raised when CopyDefaultValue swaps in new entries; EnumSlotControlView rebuilds the dropdown.
        [NonSerialized]
        internal Action entriesChanged;

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

        // AddSlot's modify-in-place path calls this; copy entries so Sub Graph property edits propagate.
        public override void CopyDefaultValue(MaterialSlot other)
        {
            base.CopyDefaultValue(other);
            if (other is Vector1MaterialEnumSlot enumSlot && !EntriesEqual(enumSlot))
            {
                options = enumSlot.options != null ? new List<string>(enumSlot.options) : new List<string>();
                values = enumSlot.values != null ? new List<int>(enumSlot.values) : new List<int>();
                if (!values.Contains((int)value))
                    value = values.Count > 0 ? values[0] : 0;
                entriesChanged?.Invoke();
            }
        }

        bool EntriesEqual(Vector1MaterialEnumSlot other)
        {
            if (options == null || other.options == null || values == null || other.values == null)
                return options == other.options && values == other.values;
            if (options.Count != other.options.Count || values.Count != other.values.Count)
                return false;
            for (int i = 0; i < options.Count; i++)
                if (options[i] != other.options[i])
                    return false;
            for (int i = 0; i < values.Count; i++)
                if (values[i] != other.values[i])
                    return false;
            return true;
        }

        public override VisualElement InstantiateControl()
        {
            return new EnumSlotControlView(this);
        }

        class EnumSlotControlView : VisualElement
        {
            Vector1MaterialEnumSlot m_Slot;
            DropdownField m_DropdownField;

            public EnumSlotControlView(Vector1MaterialEnumSlot slot)
            {
                m_Slot = slot;
                BuildDropdown();

                RegisterCallback<AttachToPanelEvent>(_ => m_Slot.entriesChanged += RebuildDropdown);
                RegisterCallback<DetachFromPanelEvent>(_ => m_Slot.entriesChanged -= RebuildDropdown);
            }

            void BuildDropdown()
            {
                int idx = m_Slot.values.FindIndex(e => e == (int)m_Slot.value);
                if (idx < 0 || idx >= m_Slot.options.Count)
                    idx = 0;

                m_DropdownField = m_Slot.hideConnector
                    ? new DropdownField(m_Slot.RawDisplayName(), new List<string>(m_Slot.options), idx)
                    : new DropdownField(new List<string>(m_Slot.options), idx);

                m_DropdownField.RegisterValueChangedCallback(OnValueChange);
                Add(m_DropdownField);
            }

            void RebuildDropdown()
            {
                if (m_DropdownField != null)
                    Remove(m_DropdownField);
                BuildDropdown();
            }

            void OnValueChange(ChangeEvent<string> evt)
            {
                int newIndex = m_Slot.options.FindIndex(e => e == evt.newValue);
                if (newIndex < 0 || newIndex >= m_Slot.values.Count)
                    return;

                int newValue = m_Slot.values[newIndex];

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
