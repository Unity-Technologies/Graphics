using UnityEditor.Graphing;
using UnityEngine;

using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    class ColorRGBASlotControlView : VisualElement
    {
        ColorRGBAMaterialSlot m_Slot;

        public ColorRGBASlotControlView(ColorRGBAMaterialSlot slot)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/ColorRGBASlotControlView"));
            m_Slot = slot;
            var colorField = new ColorField { value = slot.value, showEyeDropper = false };
            colorField.RegisterValueChangedCallback(OnValueChanged);
            Add(colorField);
        }

        void OnValueChanged(ChangeEvent<Color> evt)
        {
            m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Color Change");
            m_Slot.value = evt.newValue;
            m_Slot.owner.Dirty(ModificationScope.Node);
        }
    }
}
