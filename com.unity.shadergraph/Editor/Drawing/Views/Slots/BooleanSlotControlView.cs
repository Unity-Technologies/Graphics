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
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/BooleanSlotControlView"));
            m_Slot = slot;
            var toggleField = new Toggle() { value = m_Slot.value };
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
