using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    internal class HDLightUnitSliderUIDrawer
    {
        private class LightUnitPresets
        {
            public void AddPreset(Texture2D icon, string tooltip, Vector2 range)
            {
                presets.Add(new GUIContent(icon, tooltip));
                ranges.Add(range);
            }

            public void GetPreset(float value, out GUIContent preset)
            {
                for (int i = 0; i < ranges.Count; ++i)
                {
                    var range = ranges[i];

                    if (value >= range.x && value <= range.y)
                    {
                        preset = presets[i];
                        return;
                    }
                }

                // If value out of range, indicate caution. (For now assume caution feedback is last)
                preset = presets[presets.Count - 1];
            }

            private List<GUIContent> presets = new List<GUIContent>();
            private List<Vector2> ranges = new List<Vector2>();
        }

        private static readonly Dictionary<LightUnit, LightUnitPresets> s_LightUnitPresets = new Dictionary<LightUnit, LightUnitPresets>();
        private static GUIContent s_MarkerContent;

        static HDLightUnitSliderUIDrawer()
        {
            // Load light unit icons from editor resources
            var editorTextures = HDRenderPipeline.defaultAsset.renderPipelineEditorResources.textures;

            // TODO: Fill a table of light unit presets, containing their icon, tooltip, and range
            var luxPresets = new LightUnitPresets();
            luxPresets.AddPreset(editorTextures.lightUnitVeryBrightSun,   "Very Bright Sun",   new Vector2(80000, 120000));
            luxPresets.AddPreset(editorTextures.lightUnitOvercastSky,     "Overcast Sky",      new Vector2(10000, 80000));
            luxPresets.AddPreset(editorTextures.lightUnitSunriseOrSunset, "Sunrise or Sunset", new Vector2(1,     10000));
            luxPresets.AddPreset(editorTextures.lightUnitMoonLight,       "Moon Light",        new Vector2(0,     1));;
            luxPresets.AddPreset(editorTextures.lightUnitCautionValue,    "Higher than Sunlight", Vector2.positiveInfinity);
            s_LightUnitPresets.Add(LightUnit.Lux, luxPresets);

            s_MarkerContent = new GUIContent(string.Empty);
        }

        private static void GetLightUnitRects(Rect baseRect, out Rect sliderRect, out Rect iconRect)
        {
            sliderRect = baseRect;
            sliderRect.width -= EditorGUIUtility.singleLineHeight;

            iconRect = baseRect;
            iconRect.x += sliderRect.width;
            iconRect.width = EditorGUIUtility.singleLineHeight;
        }

        private static void DrawLightUnitSlider(Rect rect, SerializedProperty value)
        {
            // TODO: Look into compiling a lambda to access internal slider function for background (markers) + logarithmic values.
            value.floatValue = GUI.HorizontalSlider(rect, value.floatValue, 0f, 150000f, GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb);
        }

        private static void DrawLightUnitMarker(Rect sliderRect, float x, string tooltip, float width = 4, float height = 2)
        {
            var markerRect = sliderRect;
            markerRect.width  = width;
            markerRect.height = height;

            // Align with slider.
            markerRect.y += (EditorGUIUtility.singleLineHeight / 2f) - 1;

            // Place on slider.
            markerRect.x = sliderRect.x + sliderRect.width * x;

            // Center the marker on value.
            markerRect.x -= markerRect.width / 2;

            EditorGUI.DrawRect(markerRect, Color.white);

            // TODO: Consider enlarging this tooltip rect so that it's easier to discover?
            s_MarkerContent.tooltip = tooltip;
            EditorGUI.LabelField(markerRect, s_MarkerContent, EditorStyles.inspectorDefaultMargins);
        }

        private static void DrawLightUnitIcon(Rect iconRect, GUIContent icon)
        {
            var oldColor = GUI.color;
            GUI.color = Color.clear;
            EditorGUI.DrawTextureTransparent(iconRect, icon.image);
            GUI.color = oldColor;

            EditorGUI.LabelField(iconRect, new GUIContent(string.Empty, icon.tooltip));
        }

        public void OnGUI(LightUnit unit, SerializedProperty value)
        {
            OnGUI(unit, value, EditorGUILayout.GetControlRect());
        }

        public void OnGUI(LightUnit unit, SerializedProperty value, Rect rect)
        {
            if (!s_LightUnitPresets.TryGetValue(unit, out var unitPreset))
                return;

            // Disable indentation (causes issues with manually rect management).
            var prevLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Fetch the presets for this light unit
            unitPreset.GetPreset(value.floatValue, out var preset);

            // Fetch the rects
            GetLightUnitRects(rect, out var sliderRect, out var iconRect);

            // Draw
            DrawLightUnitSlider(sliderRect, value);
            DrawLightUnitMarker(sliderRect,   0f, "Here's a Marker");
            DrawLightUnitMarker(sliderRect, 0.5f, "Here's another one");
            DrawLightUnitIcon(iconRect, preset);

            // Restore indentation
            EditorGUI.indentLevel = prevLevel;
        }
    }
}
