using UnityEditor.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    public class ColorSlotControlView : VisualElement
    {
        ColorMaterialSlot m_Slot;

        public ColorSlotControlView(ColorMaterialSlot slot)
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
            if (m_Slot.owner.onModified != null)
                m_Slot.owner.onModified(m_Slot.owner, ModificationScope.Node);
        }
    }
}
