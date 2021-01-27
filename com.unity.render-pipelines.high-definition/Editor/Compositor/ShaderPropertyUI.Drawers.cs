using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

using System.Collections.Generic;

namespace UnityEditor.Rendering.HighDefinition.Compositor
{
    internal class ShaderPropertyUI
    {
        public static void Draw(List<SerializedShaderProperty> propertyList)
        {
            int index = propertyList.FindIndex(x => x.propertyType.GetEnumValue<ShaderPropertyType>() != ShaderPropertyType.Texture);
            if (index >= 0)
            {
                EditorGUILayout.Separator();
                var headerStyle = EditorStyles.helpBox;
                headerStyle.fontSize = 14;
                EditorGUILayout.LabelField("Composition Parameters", headerStyle);
            }

            foreach (var property in propertyList)
            {
                Draw(property);
            }
        }

        public static void Draw(SerializedShaderProperty prop)
        {
            int columnWidth = (int)EditorGUIUtility.labelWidth; // Set a fixed length for all labels, so everything in the UI is nicely aligned

            var propertNameWithTooltip = new GUIContent(prop.propertyName.stringValue, prop.propertyName.stringValue);
            switch ((ShaderPropertyType)prop.propertyType.intValue)
            {
                case ShaderPropertyType.Range:
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(propertNameWithTooltip, GUILayout.Width(columnWidth));
                    Vector2 rangeLimits = prop.rangeLimits.vector2Value;
                    float val = EditorGUILayout.Slider(prop.propertyValue.vector4Value.x, rangeLimits.x, rangeLimits.y);
                    prop.propertyValue.vector4Value = new Vector4(val, 0, 0, 0);
                    EditorGUILayout.EndHorizontal();
                }
                break;
                case ShaderPropertyType.Float:
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(propertNameWithTooltip, GUILayout.Width(columnWidth));
                    float val = EditorGUILayout.FloatField(prop.propertyValue.vector4Value.x);
                    prop.propertyValue.vector4Value = new Vector4(val, 0, 0, 0);
                    EditorGUILayout.EndHorizontal();
                }
                break;
                case ShaderPropertyType.Vector:
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(propertNameWithTooltip, GUILayout.Width(columnWidth));
                    Vector4 val = EditorGUILayout.Vector4Field(GUIContent.none, prop.propertyValue.vector4Value);
                    prop.propertyValue.vector4Value = val;
                    EditorGUILayout.EndHorizontal();
                }
                break;
                case ShaderPropertyType.Color:
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(propertNameWithTooltip, GUILayout.Width(columnWidth));
                    Color val = prop.propertyValue.vector4Value;
                    val = EditorGUILayout.ColorField(GUIContent.none, val);
                    prop.propertyValue.vector4Value = val;
                    EditorGUILayout.EndHorizontal();
                }
                break;
            }
        }
    }
}
