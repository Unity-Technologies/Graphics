using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    internal static class LightIntensitySlider
    {
        // Note: To have the right icons along the skin, we do not use the editor resource loading mechanism at the moment. This could be revisited once this is converted to UITK.
        static Texture2D GetLightUnitIcon(string name)
        {
            return CoreEditorUtils.LoadIcon(@"Packages/com.unity.render-pipelines.core/Editor/Lighting/Icons/LightUnitIcons", name, ".png");
        }

        // TODO: Move all light unit icons from the package into the built-in resources.
        static Texture2D Candlelight = GetLightUnitIcon("Candlelight");
        static Texture2D DecorativeLight = GetLightUnitIcon("DecorativeLight");
        static Texture2D ExteriorLight = GetLightUnitIcon("ExteriorLight");
        static Texture2D InteriorLight = GetLightUnitIcon("InteriorLight");
        static Texture2D Moonlight = GetLightUnitIcon("Moonlight");
        static Texture2D Overcast = GetLightUnitIcon("Overcast");
        static Texture2D SunriseSunset = GetLightUnitIcon("SunriseSunset");
        static Texture2D BrightSky = GetLightUnitIcon("BrightSky");

        static GUIStyle k_IconButton = new ("IconButton");

        private static readonly LightUnitSliderUIRange[] LumenRanges =
        {
            new (Candlelight, "Candle", new Vector2(0, 15), 12.5f),
            new (DecorativeLight, "Decorative", new Vector2(15, 300), 100),
            new (InteriorLight, "Interior", new Vector2(300, 3000), 1000),
            new (ExteriorLight, "Exterior", new Vector2(3000, 40000), 10000),
        };
        private static readonly float[] LumenDistribution = { 0f, 0.25f, 0.5f, 0.75f, 1f };

        private static readonly LightUnitSliderUIRange[] LuxRanges =
        {
            new (Moonlight, "Moon", new Vector2(0, 1), 0.5f),
            new (SunriseSunset, "Low Sun", new Vector2(1, 10000), 5000),
            new (Overcast, "Cloudy", new Vector2(10000, 80000), 20000),
            new (BrightSky, "High Sun", new Vector2(80000, 130000), 100000),
        };
        private static readonly float[] LuxDistribution = { 0.0f, 0.05f, 0.5f, 0.9f, 1.0f };

        private const float ConstantNitsToLumenArea = 200.0f;

        internal static void Draw(ISerializedLight serialized, Editor owner, Rect baseRect)
        {
            // Calculate UI rects
            Rect sliderRect, iconRect;
            {
                sliderRect = baseRect;
                sliderRect.width -= EditorGUIUtility.singleLineHeight;

                iconRect = baseRect;
                iconRect.x += sliderRect.width;
                iconRect.width = EditorGUIUtility.singleLineHeight;
            }

            Light light = serialized.settings.light;
            LightType lightType = serialized.settings.lightType.GetEnumValue<LightType>();
            LightUnit nativeUnit = LightUnitUtils.GetNativeLightUnit(lightType);
            LightUnit lightUnit = serialized.settings.lightUnit.GetEnumValue<LightUnit>();
            bool usesLuxBasedRange = lightType == LightType.Directional;

            LightUnitSliderUIRange[] ranges = usesLuxBasedRange ? LuxRanges : LumenRanges;
            float[] distribution = usesLuxBasedRange ? LuxDistribution : LumenDistribution;

            // Verify that ui light unit is in fact supported or revert to native.
            lightUnit = LightUnitUtils.IsLightUnitSupported(lightType, lightUnit) ? lightUnit : nativeUnit;

            Debug.Assert(ranges.Length == distribution.Length - 1);

            // This intensity is in the native light unit for the light's type
            float nativeIntensity = serialized.settings.intensity.floatValue;
            // This is the intensity above converted to the unit (either lux or lumen) that's the basis of the ranges/distribution for this light type
            float convertedIntensity;
            bool isSpotReflectorRelevant = (lightType == LightType.Pyramid || lightType == LightType.Spot) &&
                                           lightUnit == LightUnit.Lumen &&
                                           serialized.settings.enableSpotReflector.boolValue;

            if (lightType == LightType.Pyramid || lightType == LightType.Spot || lightType == LightType.Box)
            {
                // For Box light, we want to use the Lumen style ranges,
                // but Lumen is not defined for Box lights,
                // so we just pretend its native type is Candela.
                float solidAngle = LightUnitUtils.SphereSolidAngle;
                if (isSpotReflectorRelevant)
                {
                    // If spot reflector matters for this type of light,
                    // calculate lumen as if spot reflector is on;
                    // This prevents the slider from moving around when solid angle params change.
                    solidAngle = LightUnitUtils.GetSolidAngle(lightType, true, light.spotAngle, light.areaSize.x);
                }
                convertedIntensity = LightUnitUtils.CandelaToLumen(nativeIntensity, solidAngle);
            }
            else if (nativeUnit == LightUnit.Nits && lightUnit != LightUnit.Lumen)
            {
                convertedIntensity = LightUnitUtils.NitsToLumen(nativeIntensity, ConstantNitsToLumenArea);
            }
            else
            {
                LightUnit toUnit = usesLuxBasedRange ? LightUnit.Lux : LightUnit.Lumen;
                convertedIntensity = LightUnitUtils.ConvertIntensity(light, nativeIntensity, nativeUnit, toUnit);
            }

            // Check which preset level we are in. If we're within a preset range,
            // this index will contain the index of that preset. If we're below all
            // preset ranges, the value will be -2, and if we're above all preset
            // ranges, the value will be -1. Also calculate the min and max values
            // of the slider.
            int rangeIndex = -3;
            float minValue = float.MaxValue;
            float maxValue = float.MinValue;
            for (int i = 0; i < ranges.Length; i++)
            {
                var l = ranges[i];
                if (convertedIntensity >= l.value.x && convertedIntensity <= l.value.y)
                {
                    rangeIndex = i;
                }

                minValue = Mathf.Min(minValue, l.value.x);
                maxValue = Mathf.Max(maxValue, l.value.y);
            }

            if (rangeIndex < 0)
            {
                // ^ The current value doesn't lie within a preset range.
                // If it is less than the minimum preset, it is below the
                // whole slider's range, otherwise, it is above it.
                rangeIndex = (convertedIntensity < minValue) ? -2 : -1;
            }

            // Draw the slider
            float sliderValue;
            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                if (rangeIndex == -2)
                {
                    // ^ The current value is below the slider's range
                    sliderValue = 0f;
                }
                else if (rangeIndex == -1)
                {
                    // ^ The current value is above the slider's range
                    sliderValue = 1f;
                }
                else
                {
                    // Map the intensity value into the [0, 1] range via a non-linear piecewise mapping
                    Vector2 r = ranges[rangeIndex].value;
                    Vector2 d = new Vector2(distribution[rangeIndex], distribution[rangeIndex + 1]);
                    sliderValue = (d.x - d.y) / (r.x - r.y) * (convertedIntensity - r.x) + d.x;
                }

                EditorGUI.BeginChangeCheck();
                float newSliderValue = GUI.HorizontalSlider(sliderRect, sliderValue, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    bool newRangeFound = false;
                    for (int i = 0; i < ranges.Length; i++)
                    {
                        if (newSliderValue >= distribution[i] && newSliderValue <= distribution[i + 1])
                        {
                            rangeIndex = i;
                            newRangeFound = true;
                            break;
                        }
                    }

                    Debug.Assert(newRangeFound);
                    // Map the slider value in the [0, 1] range to the intensity value via a non-linear piecewise
                    // mapping
                    Vector2 r = ranges[rangeIndex].value;
                    Vector2 d = new Vector2(distribution[rangeIndex], distribution[rangeIndex + 1]);
                    float newConvertedIntensity = (r.x - r.y) / (d.x - d.y) * (newSliderValue - d.x) + r.x;

                    if (lightType == LightType.Pyramid || lightType == LightType.Spot || lightType == LightType.Box)
                    {
                        float solidAngle = LightUnitUtils.SphereSolidAngle;
                        if (isSpotReflectorRelevant)
                        {
                            solidAngle = LightUnitUtils.GetSolidAngle(lightType, true, light.spotAngle, light.areaSize.x);
                        }
                        serialized.settings.intensity.floatValue = LightUnitUtils.LumenToCandela(newConvertedIntensity, solidAngle);
                    }
                    else if (nativeUnit == LightUnit.Nits && lightUnit != LightUnit.Lumen)
                    {
                        serialized.settings.intensity.floatValue = LightUnitUtils.LumenToNits(newConvertedIntensity, ConstantNitsToLumenArea);
                    }
                    else
                    {
                        LightUnit fromUnit = usesLuxBasedRange ? LightUnit.Lux : LightUnit.Lumen;
                        serialized.settings.intensity.floatValue = LightUnitUtils.ConvertIntensity(light, newConvertedIntensity, fromUnit, nativeUnit);
                    }
                }
            }

            GUIContent GetTooltip(string rangeName, float intensity)
            {
                float uiIntensity;

                if (lightType == LightType.Box)
                {
                    float candelaIntensity = LightUnitUtils.LumenToCandela(intensity, LightUnitUtils.SphereSolidAngle);
                    uiIntensity = candelaIntensity;
                }
                else if (nativeUnit == LightUnit.Nits && lightUnit != LightUnit.Lumen)
                {
                    uiIntensity = LightUnitUtils.LumenToNits(intensity, ConstantNitsToLumenArea);
                }
                else
                {
                    LightUnit fromUnit = usesLuxBasedRange ? LightUnit.Lux : LightUnit.Lumen;
                    uiIntensity = LightUnitUtils.ConvertIntensity(light, intensity, fromUnit, lightUnit);
                }

                string formatValue = uiIntensity < 100 ? $"{uiIntensity:n}" : $"{uiIntensity:n0}";
                return new GUIContent(string.Empty, $"{rangeName} | {formatValue} {lightUnit.ToString()}");
            }

            // Draw the markers on the slider
            for (int i = 0; i < ranges.Length; i++)
            {
                const float kMarkerWidth = 2f;
                const float kMarkerHeight = 2f;
                const float kMarkerTooltipSize = 16f;

                var markerRect = new Rect(
                    sliderRect.x + distribution[i + 1] * sliderRect.width - kMarkerWidth * 0.5f,
                    sliderRect.y + (EditorGUIUtility.singleLineHeight / 2f) - 1,
                    kMarkerWidth,
                    kMarkerHeight
                );

                // Draw marker by manually drawing the rect, and an empty label with the tooltip.
                Color kDarkThemeColor = new Color32(153, 153, 153, 255);
                Color kLiteThemeColor = new Color32(97, 97, 97, 255);
                EditorGUI.DrawRect(markerRect, EditorGUIUtility.isProSkin ? kDarkThemeColor : kLiteThemeColor);

                // Scale the marker tooltip for easier discovery
                Rect markerTooltipRect = new(
                    markerRect.x - kMarkerTooltipSize * 0.5f,
                    markerRect.y - kMarkerTooltipSize * 0.5f,
                    kMarkerTooltipSize * (i < ranges.Length - 1 ? 1f : 0.5f),
                    kMarkerTooltipSize
                );

                // Temporarily remove indent level, otherwise our custom-positioned tooltip label field will also be
                // indented
                int indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                EditorGUI.LabelField(markerTooltipRect, GetTooltip(ranges[i].content.tooltip, ranges[i].value.y));
                EditorGUI.indentLevel = indent;
            }

            GUIContent content;
            Vector2 range;
            if (rangeIndex < 0)
            {
                string tooltip = usesLuxBasedRange ? "Higher than Sunlight" : "Very high intensity light";
                content = new GUIContent(EditorGUIUtility.TrIconContent("console.warnicon").image, tooltip);
                float minOrMaxValue = (convertedIntensity < minValue) ? minValue : maxValue;
                range = new Vector2(-1, minOrMaxValue);
            }
            else
            {
                content = ranges[rangeIndex].content;
                range = ranges[rangeIndex].value;
            }
            // Draw the context menu feedback before the icon
            GUI.Box(iconRect, GUIContent.none, k_IconButton);
            // Draw the icon
            {
                var oldColor = GUI.color;
                GUI.color = Color.clear;
                EditorGUI.DrawTextureTransparent(iconRect, content.image);
                GUI.color = oldColor;
            }
            // Draw the thumbnail tooltip and the knob tooltip
            {
                // Temporarily remove indent level, otherwise our custom-positioned tooltip label field will also be
                // indented
                int indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                EditorGUI.LabelField(iconRect, GetTooltip(content.tooltip, range.y));

                const float knobSize = 10f;
                Rect knobRect = new(
                    sliderRect.x + (sliderRect.width - knobSize) * sliderValue,
                    sliderRect.y + (sliderRect.height - knobSize) * 0.5f,
                    knobSize,
                    knobSize
                );
                EditorGUI.LabelField(knobRect, GetTooltip(content.tooltip, convertedIntensity));

                EditorGUI.indentLevel = indent;
            }
            // Handle events for context menu
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (iconRect.Contains(e.mousePosition))
                {
                    var menuPosition = iconRect.position + iconRect.size;
                    var menu = new GenericMenu();

                    for (int i = ranges.Length - 1; i >= 0; --i)
                    {
                        // Indicate a checkmark if the value is within this preset range.
                        LightUnitSliderUIRange preset = ranges[i];
                        float nativePresetValue;
                        if (lightType == LightType.Pyramid || lightType == LightType.Spot || lightType == LightType.Box)
                        {
                            float solidAngle = LightUnitUtils.SphereSolidAngle;
                            if (isSpotReflectorRelevant)
                            {
                                solidAngle = LightUnitUtils.GetSolidAngle(lightType, true, light.spotAngle, light.areaSize.x);
                            }
                            nativePresetValue = LightUnitUtils.LumenToCandela(preset.presetValue, solidAngle);
                        }
                        else if (nativeUnit == LightUnit.Nits && lightUnit != LightUnit.Lumen)
                        {
                            nativePresetValue = LightUnitUtils.LumenToNits(preset.presetValue, ConstantNitsToLumenArea);
                        }
                        else
                        {
                            LightUnit fromUnit = usesLuxBasedRange ? LightUnit.Lux : LightUnit.Lumen;
                            nativePresetValue = LightUnitUtils.ConvertIntensity(light, preset.presetValue, fromUnit, nativeUnit);
                        }

                        menu.AddItem(
                            EditorGUIUtility.TrTextContent(preset.content.tooltip),
                           rangeIndex == i,
                           () => SetIntensityValue(serialized, nativePresetValue)
                        );
                    }

                    menu.DropDown(new Rect(menuPosition, Vector2.zero));
                    e.Use();
                }
            }
        }

        static void SetIntensityValue(ISerializedLight serialized, float intensity)
        {
            serialized.Update();
            serialized.settings.intensity.floatValue = intensity;
            serialized.Apply();
        }
    }
}
