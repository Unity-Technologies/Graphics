using System;
using UnityEditor.Graphing;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    class ScreenPositionSlotControlView : VisualElement
    {
        ScreenPositionMaterialSlot m_Slot;

        public ScreenPositionSlotControlView(ScreenPositionMaterialSlot slot)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/ScreenPositionSlotControlView"));
            m_Slot = slot;
            var enumField = new EnumField(slot.screenSpaceType);
            enumField.RegisterValueChangedCallback(OnValueChanged);
            Add(enumField);
        }

        void OnValueChanged(ChangeEvent<Enum> evt)
        {
            var screenSpaceType = (ScreenSpaceType)evt.newValue;
            if (screenSpaceType != m_Slot.screenSpaceType)
            {
                m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Change Screen Space Type");
                m_Slot.screenSpaceType = screenSpaceType;
                m_Slot.owner.Dirty(ModificationScope.Graph);
            }
        }
    }
}
