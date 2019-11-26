using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    class UVSlotControlView : VisualElement
    {
        UVMaterialSlot m_Slot;

        public UVSlotControlView(UVMaterialSlot slot)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/UVSlotControlView"));
            m_Slot = slot;
            var enumField = new EnumField(slot.channel);
            enumField.RegisterValueChangedCallback(OnValueChanged);
            Add(enumField);
        }

        void OnValueChanged(ChangeEvent<Enum> evt)
        {
            var channel = (UVChannel)evt.newValue;
            if (channel != m_Slot.channel)
            {
                m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Change UV Channel");
                m_Slot.channel = channel;
                m_Slot.owner.Dirty(ModificationScope.Graph);
            }
        }
    }
}
