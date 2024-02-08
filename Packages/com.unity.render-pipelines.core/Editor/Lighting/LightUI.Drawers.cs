using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    internal enum DirectionalLightUnit
    {
        Lux = LightUnit.Lux,
    }

    internal enum AreaLightUnit
    {
        Lumen = LightUnit.Lumen,
        Nits = LightUnit.Nits,
        Ev100 = LightUnit.Ev100,
    }

    internal enum PunctualLightUnit
    {
        Lumen = LightUnit.Lumen,
        Candela = LightUnit.Candela,
        Lux = LightUnit.Lux,
        Ev100 = LightUnit.Ev100
    }

    /// <summary>
    /// Contains a set of methods to help render the inspectors of Lights across SRP's
    /// </summary>
    public partial class LightUI
    {
        /// <summary>
        /// Draws the color temperature for a serialized light
        /// </summary>
        /// <param name="serialized">The serizalized light</param>
        /// <param name="owner">The editor</param>
        public static void DrawColor(ISerializedLight serialized, Editor owner)
        {
            if (GraphicsSettings.lightsUseLinearIntensity && GraphicsSettings.lightsUseColorTemperature)
            {
                // Use the color temperature bool to create a popup dropdown to choose between the two modes.

                var serializedUseColorTemperature = serialized.settings.useColorTemperature;
                using (var check = new EditorGUI.ChangeCheckScope())
                using (new EditorGUI.MixedValueScope(serializedUseColorTemperature.hasMultipleDifferentValues))
                {
                    var colorTemperaturePopupValue = Convert.ToInt32(serializedUseColorTemperature.boolValue);
                    colorTemperaturePopupValue = EditorGUILayout.Popup(Styles.lightAppearance, colorTemperaturePopupValue, Styles.lightAppearanceOptions);
                    if(check.changed)
                        serializedUseColorTemperature.boolValue = Convert.ToBoolean(colorTemperaturePopupValue);
                }

                if (serialized.settings.useColorTemperature.boolValue)
                {
                    EditorGUI.indentLevel += 1;
                    EditorGUILayout.PropertyField(serialized.settings.color, Styles.colorFilter);

                    // Light unit slider
                    const int k_ValueUnitSeparator = 2;
                    var lineRect = EditorGUILayout.GetControlRect();
                    var labelRect = lineRect;
                    labelRect.width = EditorGUIUtility.labelWidth;
                    EditorGUI.LabelField(labelRect, Styles.colorTemperature);

                    var temperatureSliderRect = lineRect;
                    temperatureSliderRect.x += EditorGUIUtility.labelWidth + k_ValueUnitSeparator;
                    temperatureSliderRect.width -= EditorGUIUtility.labelWidth + k_ValueUnitSeparator;
                    TemperatureSliderUIDrawer.Draw(serialized.settings, serialized.serializedObject, serialized.settings.colorTemperature, temperatureSliderRect);

                    // Value and unit label
                    // Match const defined in EditorGUI.cs
                    const int k_IndentPerLevel = 15;
                    const int k_UnitWidth = 60 + k_IndentPerLevel;
                    int indent = k_IndentPerLevel * EditorGUI.indentLevel;
                    Rect valueRect = EditorGUILayout.GetControlRect();
                    valueRect.width += indent - k_ValueUnitSeparator - k_UnitWidth;
                    Rect unitRect = valueRect;
                    unitRect.x += valueRect.width - indent + k_ValueUnitSeparator;
                    unitRect.width = k_UnitWidth + k_ValueUnitSeparator;

                    EditorGUI.PropertyField(valueRect, serialized.settings.colorTemperature, CoreEditorStyles.empty);
                    EditorGUI.LabelField(unitRect, Styles.lightAppearanceUnits[0]);

                    EditorGUI.indentLevel -= 1;
                }
                else
                    EditorGUILayout.PropertyField(serialized.settings.color, Styles.color);
            }
            else
                EditorGUILayout.PropertyField(serialized.settings.color, Styles.color);
        }

        /// <summary>
        /// Draws the intensity field and slider, including the light unit dropdown for a serialized light.
        /// </summary>
        /// <param name="serialized">The serialized light.</param>
        /// <param name="owner">The editor.</param>
        public static void DrawIntensity(ISerializedLight serialized, Editor owner)
        {
            // Match const defined in EditorGUI.cs
            const int k_IndentPerLevel = 15;
            const int k_ValueUnitSeparator = 2;
            const int k_UnitWidth = 100;

            float indent = k_IndentPerLevel * EditorGUI.indentLevel;

            Rect lineRect = EditorGUILayout.GetControlRect();
            Rect labelRect = lineRect;
            labelRect.width = EditorGUIUtility.labelWidth;

            // Expand to reach both lines of the intensity field.
            var interlineOffset = EditorGUIUtility.singleLineHeight + 2f;
            labelRect.height += interlineOffset;

            //handling of prefab overrides in a parent label
            GUIContent parentLabel = Styles.lightIntensity;
            parentLabel = EditorGUI.BeginProperty(labelRect, parentLabel, serialized.settings.lightUnit);
            parentLabel = EditorGUI.BeginProperty(labelRect, parentLabel, serialized.settings.intensity);
            {
                // Restore the original rect for actually drawing the label.
                labelRect.height -= interlineOffset;

                EditorGUI.LabelField(labelRect, parentLabel);
            }
            EditorGUI.EndProperty();
            EditorGUI.EndProperty();

            Light light = serialized.settings.light;
            LightType lightType = serialized.settings.lightType.GetEnumValue<LightType>();
            LightUnit nativeUnit = LightUnitUtils.GetNativeLightUnit(lightType);
            LightUnit lightUnit = serialized.settings.lightUnit.GetEnumValue<LightUnit>();
            float nativeIntensity = serialized.settings.intensity.floatValue;

            // Verify that ui light unit is in fact supported or revert to native.
            lightUnit = LightUnitUtils.IsLightUnitSupported(lightType, lightUnit) ? lightUnit : nativeUnit;

            // Draw the light unit slider + icon + tooltip
            Rect lightUnitSliderRect = lineRect; // TODO: Move the value and unit rects to new line
            lightUnitSliderRect.x += EditorGUIUtility.labelWidth + k_ValueUnitSeparator;
            lightUnitSliderRect.width -= EditorGUIUtility.labelWidth + k_ValueUnitSeparator;
            LightIntensitySlider.Draw(serialized, owner, lightUnitSliderRect);

            // We use PropertyField to draw the value to keep the handle at left of the field
            // This will apply the indent again thus we need to remove it time for alignment
            Rect valueRect = EditorGUILayout.GetControlRect();
            labelRect.width = EditorGUIUtility.labelWidth;
            valueRect.width += indent - k_ValueUnitSeparator - k_UnitWidth;
            Rect unitRect = valueRect;
            unitRect.x += valueRect.width - indent + k_ValueUnitSeparator;
            unitRect.width = k_UnitWidth + .5f;

            // Draw the intensity float field
            EditorGUI.BeginChangeCheck();
            float curIntensity = LightUnitUtils.ConvertIntensity(light, nativeIntensity, nativeUnit, lightUnit);
            EditorGUI.showMixedValue = serialized.settings.lightUnit.hasMultipleDifferentValues;
            float newIntensity = EditorGUI.FloatField(valueRect, CoreEditorStyles.empty, curIntensity);
            if (EditorGUI.EndChangeCheck())
            {
                serialized.settings.intensity.floatValue = Mathf.Max(
                    0f,
                    LightUnitUtils.ConvertIntensity(light, newIntensity, lightUnit, nativeUnit)
                );
            }
            EditorGUI.showMixedValue = false;

            // Draw the light unit dropdown
            {
                EditorGUI.BeginChangeCheck();

                EditorGUI.BeginProperty(unitRect, GUIContent.none, serialized.settings.lightUnit);
                EditorGUI.showMixedValue = serialized.settings.lightUnit.hasMultipleDifferentValues;

                LightUnit selectedLightUnit = DrawLightIntensityUnitPopup(
                    unitRect,
                    serialized.settings.lightUnit.GetEnumValue<LightUnit>(),
                    lightType
                );

                EditorGUI.showMixedValue = false;
                EditorGUI.EndProperty();

                if (EditorGUI.EndChangeCheck())
                {
                    serialized.settings.lightUnit.SetEnumValue(selectedLightUnit);
                }
            }

        }

        /// <summary>
        /// Draws additional light intensity modifiers, depending on light type/unit for a serialized light.
        /// </summary>
        /// <param name="serialized">The serialized light.</param>
        /// <param name="hideReflector">If true, the reflector checkbox will be hidden.</param>
        public static void DrawIntensityModifiers(ISerializedLight serialized, bool hideReflector = false)
        {
            LightType lightType = serialized.settings.lightType.GetEnumValue<LightType>();
            LightUnit lightUnit = serialized.settings.lightUnit.GetEnumValue<LightUnit>();

            // Draw the "Lux At Distance" field
            if (lightType != LightType.Directional && lightType != LightType.Box && lightUnit == LightUnit.Lux)
            {
                // Box and directional lights shouldn't display this widget, since their light source are considered to
                // be at infinity, and the distance is always infinity. So we only display this widget for light types
                // that support the Lux unit, and whose sources aren't positioned at infinity.
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();

                float oldLuxAtDistance = serialized.settings.luxAtDistance.floatValue;
                EditorGUILayout.PropertyField(serialized.settings.luxAtDistance, Styles.luxAtDistance);

                if (EditorGUI.EndChangeCheck())
                {
                    serialized.settings.luxAtDistance.floatValue = Mathf.Max(serialized.settings.luxAtDistance.floatValue, 0.01f);
                    // Derive the lux from intensity, which is in Candela, and the old distance value
                    float lux = LightUnitUtils.CandelaToLux(serialized.settings.intensity.floatValue, oldLuxAtDistance);
                    // Calculate the new intensity in Candela from the lux value, and the new distance
                    serialized.settings.intensity.floatValue = LightUnitUtils.LuxToCandela(lux, serialized.settings.luxAtDistance.floatValue);
                }
                EditorGUI.indentLevel--;
            }

            // Draw the "Reflector" checkbox
            if ((lightType == LightType.Spot || lightType == LightType.Pyramid) && lightUnit == (int)LightUnit.Lumen && !hideReflector)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serialized.settings.enableSpotReflector, Styles.enableSpotReflector);
                if (EditorGUI.EndChangeCheck())
                {
                    // ^ The reflector bool has changed, and the light unit is set to Lumen. Update the intensity (in Candela).
                    float oldCandela = serialized.settings.intensity.floatValue;
                    float spotAngle = serialized.settings.spotAngle.floatValue;
                    float aspectRatio = serialized.settings.areaSizeX.floatValue;
                    bool enableSpotReflector = serialized.settings.enableSpotReflector.boolValue;

                    float oldSolidAngle = LightUnitUtils.GetSolidAngle(lightType, !enableSpotReflector, spotAngle, aspectRatio);
                    float oldLumen = LightUnitUtils.CandelaToLumen(oldCandela, oldSolidAngle);
                    float newSolidAngle = LightUnitUtils.GetSolidAngle(lightType, enableSpotReflector, spotAngle, aspectRatio);
                    serialized.settings.intensity.floatValue = LightUnitUtils.LumenToCandela(oldLumen, newSolidAngle);
                }
                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// Draws a light unit dropdown.
        /// </summary>
        /// <param name="position">Rectangle on the screen to use for the field.</param>
        /// <param name="selected">The light unit the field shows.</param>
        /// <param name="type">The type of the light. This determines which options are available.</param>
        /// <returns>The light unit that has been selected by the user. </returns>
        public static LightUnit DrawLightIntensityUnitPopup(Rect position, LightUnit selected, LightType type)
        {
            switch (type)
            {
                case LightType.Box:
                case LightType.Directional:
                    return (LightUnit)EditorGUI.EnumPopup(position, (DirectionalLightUnit)selected);

                case LightType.Point:
                case LightType.Spot:
                case LightType.Pyramid:
                        return (LightUnit)EditorGUI.EnumPopup(position, (PunctualLightUnit)selected);

                default:
                    return (LightUnit)EditorGUI.EnumPopup(position, (AreaLightUnit)selected);
            }
        }
    }
}
