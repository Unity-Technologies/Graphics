using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    internal class HDLightUnitSliderUIDrawer
    {
        static HDPiecewiseLightUnitSlider k_DirectionalLightUnitSlider;
        static HDPunctualLightUnitSlider k_PunctualLightUnitSlider;
        static HDPiecewiseLightUnitSlider k_ExposureSlider;

        static HDLightUnitSliderUIDrawer()
        {
            // Maintain a unique slider for directional/lux.
            k_DirectionalLightUnitSlider = new HDPiecewiseLightUnitSlider(LightUnitSliderDescriptors.LuxDescriptor);

            // Internally, slider is always in terms of lumens, so that the slider is uniform for all light units.
            k_PunctualLightUnitSlider = new HDPunctualLightUnitSlider(LightUnitSliderDescriptors.LumenDescriptor);

            // Exposure is in EV100, but we load a separate due to the different icon set.
            k_ExposureSlider = new HDPiecewiseLightUnitSlider(LightUnitSliderDescriptors.ExposureDescriptor);
        }

        // Need to cache the serialized object on the slider, to add support for the preset selection context menu (need to apply changes to serialized)
        // TODO: This slider drawer is getting kind of bloated. Break up the implementation into where it is actually used?
        public void SetSerializedObject(SerializedObject serializedObject)
        {
            k_DirectionalLightUnitSlider.SetSerializedObject(serializedObject);
            k_PunctualLightUnitSlider.SetSerializedObject(serializedObject);
            k_ExposureSlider.SetSerializedObject(serializedObject);
        }

        public void Draw(HDLightType type, LightUnit lightUnit, SerializedProperty value, Rect rect, SerializedHDLight light, Editor owner)
        {
            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                if (type == HDLightType.Directional)
                    DrawDirectionalUnitSlider(value, rect);
                else
                    DrawPunctualLightUnitSlider(lightUnit, value, rect, light, owner);
            }
        }

        void DrawDirectionalUnitSlider(SerializedProperty value, Rect rect)
        {
            float val = value.floatValue;
            k_DirectionalLightUnitSlider.Draw(rect, value, ref val);
            if (val != value.floatValue)
                value.floatValue = val;
        }

        void DrawPunctualLightUnitSlider(LightUnit lightUnit, SerializedProperty value, Rect rect, SerializedHDLight light, Editor owner)
        {
            k_PunctualLightUnitSlider.Setup(lightUnit, light, owner);

            float val = value.floatValue;
            k_PunctualLightUnitSlider.Draw(rect, value, ref val);
            if (val != value.floatValue)
                value.floatValue = val;
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
