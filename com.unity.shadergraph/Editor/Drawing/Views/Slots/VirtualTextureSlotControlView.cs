using System;
using UnityEditor.Graphing;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    class VirtualTextureSlotControlView : VisualElement
    {
        VirtualTextureInputMaterialSlot m_Slot;

        public VirtualTextureSlotControlView(VirtualTextureInputMaterialSlot slot)
        {
            m_Slot = slot;
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/TextureSlotControlView"));      // TODO
            var proceduralField = new Toggle() { value = m_Slot.m_Default.value.procedural };
            proceduralField.OnToggleChanged(OnProceduralChanged);
            // proceduralField.RegisterValueChangedCallback(OnValueChanged);
            Add(proceduralField);
        }

        void OnProceduralChanged(ChangeEvent<bool> evt)
        {
            m_Slot.m_Default.value.procedural = evt.newValue;
        }
    }
}
