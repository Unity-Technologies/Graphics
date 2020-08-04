using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    internal class HDLightUnitSliderUIDrawer
    {
        private struct LightUnitTooltips
        {
            public static string s_Empty = "";
        }

        private class LightUnitPresets
        {
            public void AddPreset(Texture2D icon, string tooltip, Vector2 range)
            {
                presets.Add(new GUIContent(icon, tooltip));
                ranges.Add(range);
            }

            public void GetPreset(float value, out GUIContent preset)
            {
                // TODO: Take arbitrary value as input and fill the appropriate icon & tooltip, with respect to range
                preset = GUIContent.none;
                preset = presets[4];
            }

            private List<GUIContent> presets = new List<GUIContent>();
            private List<Vector2> ranges = new List<Vector2>();
        }

        private static readonly Dictionary<LightUnit, LightUnitPresets> s_LightUnitPresets = new Dictionary<LightUnit, LightUnitPresets>();

        static HDLightUnitSliderUIDrawer()
        {
            // Load light unit icons from editor resources
            var editorTextures = HDRenderPipeline.defaultAsset.renderPipelineEditorResources.textures;

            // TODO: Fill a table of light unit presets, containing their icon, tooltip, and range
            LightUnitPresets luxPresets = new LightUnitPresets();
            luxPresets.AddPreset(editorTextures.lightUnitVeryBrightSun,   LightUnitTooltips.s_Empty, Vector2.zero);
            luxPresets.AddPreset(editorTextures.lightUnitOvercastSky,     LightUnitTooltips.s_Empty, Vector2.zero);
            luxPresets.AddPreset(editorTextures.lightUnitSunriseOrSunset, LightUnitTooltips.s_Empty, Vector2.zero);
            luxPresets.AddPreset(editorTextures.lightUnitMoonLight,       LightUnitTooltips.s_Empty, Vector2.zero);
            luxPresets.AddPreset(editorTextures.lightUnitCautionValue,    LightUnitTooltips.s_Empty, Vector2.zero);
            s_LightUnitPresets.Add(LightUnit.Lux, luxPresets);
        }

        void DrawLightUnitSlider(Rect rect, SerializedProperty value)
        {
            // TODO: Look into compiling a lambda to access internal slider function for background (markers) + logarithmic values.
            value.floatValue = GUI.HorizontalSlider(rect, value.floatValue, 0f, 20000f, GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb);
        }

        public void OnGUI(LightUnit unit, SerializedProperty value)
        {
            OnGUI(unit, value, EditorGUILayout.GetControlRect());
        }

        public void OnGUI(LightUnit unit, SerializedProperty value, Rect rect)
        {
            if (!s_LightUnitPresets.TryGetValue(unit, out var unitPreset))
                return;

            // Fetch the presets for this light unit
            unitPreset.GetPreset(value.floatValue, out var preset);

            const int kCenterIcon = 7;
            const int kIconWidth = 33;

            var sliderRect = rect;
            sliderRect.width -= kIconWidth;
            DrawLightUnitSlider(sliderRect, value);

            var iconRect = rect;
            iconRect.x += iconRect.width - kIconWidth - kCenterIcon;
            iconRect.width = kIconWidth;
            EditorGUI.LabelField(iconRect, preset);
        }
    }
}
