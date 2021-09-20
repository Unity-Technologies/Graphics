using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Contains a set of methods to help render the inspectors of Lights across SRP's
    /// </summary>
    public partial class LightUI
    {
        public static void DrawColor(ISerializedLight serialized, Editor owner)
        {
            if (GraphicsSettings.lightsUseLinearIntensity && GraphicsSettings.lightsUseColorTemperature)
            {
                // Use the color temperature bool to create a popup dropdown to choose between the two modes.
                var colorTemperaturePopupValue = Convert.ToInt32(serialized.settings.useColorTemperature.boolValue);
                colorTemperaturePopupValue = EditorGUILayout.Popup(Styles.lightAppearance, colorTemperaturePopupValue, Styles.lightAppearanceOptions);
                serialized.settings.useColorTemperature.boolValue = Convert.ToBoolean(colorTemperaturePopupValue);

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
                    unitRect.width = k_UnitWidth + .5f;

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
    }
}
