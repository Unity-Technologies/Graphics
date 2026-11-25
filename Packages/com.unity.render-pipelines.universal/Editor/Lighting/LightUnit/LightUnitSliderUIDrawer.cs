using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    class LightUnitSliderUIDrawer
    {
        PiecewiseLightUnitSlider m_Slider;

        public LightUnitSliderUIDrawer()
        {
            // Exposure is in EV100, but we load a separate due to the different icon set.
            m_Slider = new PiecewiseLightUnitSlider(LightUnitSliderDescriptors.ExposureDescriptor);
        }

        // Need to cache the serialized object on the slider, to add support for the preset selection context menu (need to apply changes to serialized)
        public void SetSerializedObject(SerializedObject serializedObject)
        {
            m_Slider.SetSerializedObject(serializedObject);
        }

        public void DrawExposureSlider(SerializedProperty value, Rect rect)
        {
            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                float val = value.floatValue;
                m_Slider.Draw(rect, value, ref val);
                if (val != value.floatValue)
                    value.floatValue = val;
            }
        }
    }
}
