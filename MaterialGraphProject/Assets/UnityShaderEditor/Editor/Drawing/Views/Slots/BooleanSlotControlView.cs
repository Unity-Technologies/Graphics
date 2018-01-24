using System;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    public class BooleanSlotControlView : VisualElement
    {
        BooleanMaterialSlot m_Slot;

        public BooleanSlotControlView(BooleanMaterialSlot slot)
        {
            m_Slot = slot;
            Action changedToggle = () => { OnChangeToggle(); };
            var toggleField = new UnityEngine.Experimental.UIElements.Toggle(changedToggle);
            Add(toggleField);
        }

        void OnChangeToggle()
        {
            m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Toggle Change");
            var value = m_Slot.value;
            value = !value;
            m_Slot.value = value;
            m_Slot.owner.Dirty(ModificationScope.Node);
        }
    }
}
