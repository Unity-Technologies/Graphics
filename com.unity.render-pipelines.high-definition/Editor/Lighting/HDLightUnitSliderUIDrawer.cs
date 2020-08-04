using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    internal class HDLightUnitSliderUIDrawer
    {
        internal struct LightUnitTooltips
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
            }

            private readonly List<GUIContent> presets = new List<GUIContent>();
            private readonly List<Vector2> ranges = new List<Vector2>();
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

        public void OnGUI(LightUnit unit, float value, Rect rect)
        {
            // TODO: Function that takes as input a LightUnit & value, and outputs a slider & icon.
            if (s_LightUnitPresets.TryGetValue(unit, out LightUnitPresets unitPreset))
            {
                unitPreset.GetPreset(value, out var preset);

                // TODO: Draw the custom slider + icon.
                EditorGUI.LabelField(rect, preset);
            }
        }
    }
}
