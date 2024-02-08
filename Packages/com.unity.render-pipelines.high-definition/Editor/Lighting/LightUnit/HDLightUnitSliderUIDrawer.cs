using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    internal class HDLightUnitSliderUIDrawer
    {
        static HDPiecewiseLightUnitSlider k_ExposureSlider;

        static HDLightUnitSliderUIDrawer()
        {
            // Exposure is in EV100, but we load a separate due to the different icon set.
            k_ExposureSlider = new HDPiecewiseLightUnitSlider(LightUnitSliderDescriptors.ExposureDescriptor);
        }

        // Need to cache the serialized object on the slider, to add support for the preset selection context menu (need to apply changes to serialized)
        // TODO: This slider drawer is getting kind of bloated. Break up the implementation into where it is actually used?
        public void SetSerializedObject(SerializedObject serializedObject)
        {
            k_ExposureSlider.SetSerializedObject(serializedObject);
        }

        public void DrawExposureSlider(SerializedProperty value, Rect rect)
        {
            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                float val = value.floatValue;
                k_ExposureSlider.Draw(rect, value, ref val);
                if (val != value.floatValue)
                    value.floatValue = val;
            }
        }
    }
}
