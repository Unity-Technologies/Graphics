using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    class ColorRGBSlotControlView : VisualElement
    {
        ColorRGBMaterialSlot m_Slot;

        public ColorRGBSlotControlView(ColorRGBMaterialSlot slot)
        {
            if (!slot.hideConnector)
                styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/ColorRGBSlotControlView"));
            else styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/ColorControlView"));

            m_Slot = slot;
            var colorField = new ColorField
            {
                value = new Color(slot.value.x, slot.value.y, slot.value.z, 1),
                showEyeDropper = false,
                showAlpha = false,
                hdr = (slot.colorMode == ColorMode.HDR)
            };

            if (slot.hideConnector)
                colorField.label = slot.RawDisplayName();

            colorField.RegisterValueChangedCallback(OnValueChanged);
            Add(colorField);
        }

        void OnValueChanged(ChangeEvent<Color> evt)
        {
            m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Color Change");
            m_Slot.value = new Vector3(evt.newValue.r, evt.newValue.g, evt.newValue.b);
            m_Slot.owner.Dirty(ModificationScope.Node);
        }
    }
}
