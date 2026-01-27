using System;
using UnityEditor.Graphing;
using UnityEngine;

using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    class BooleanSlotControlView : VisualElement
    {
        BooleanMaterialSlot m_Slot;

        public BooleanSlotControlView(BooleanMaterialSlot slot)
        {
            m_Slot = slot;
            if (!slot.hideConnector)
                styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/BooleanSlotControlView"));
            var toggleField = new Toggle() { label = m_Slot.hideConnector ? m_Slot.RawDisplayName() : null, value = m_Slot.value };

            toggleField.OnToggleChanged(OnChangeToggle);

            Add(toggleField);
        }

        void OnChangeToggle(ChangeEvent<bool> evt)
        {
            if (evt.newValue != m_Slot.value)
            {
                m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Toggle Change");
                m_Slot.value = evt.newValue;
                m_Slot.owner.Dirty(ModificationScope.Node);
            }
        }
    }
}
