using UnityEditor.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    public class ColorRGBASlotControlView : VisualElement
    {
        ColorRGBAMaterialSlot m_Slot;

        public ColorRGBASlotControlView(ColorRGBAMaterialSlot slot)
        {
            m_Slot = slot;
            var colorField = new ColorField { value = slot.value };
            colorField.OnValueChanged(OnValueChanged);
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
