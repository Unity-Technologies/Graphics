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
            int index = propertyList.FindIndex(x => x.PropertyType.intValue != (int)ShaderPropertyType.Texture);
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

            switch ((ShaderPropertyType)prop.PropertyType.intValue)
            {
                case ShaderPropertyType.Range:
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(prop.PropertyName.stringValue, GUILayout.Width(columnWidth));
                        Vector2 rangeLimits = prop.RangeLimits.vector2Value;
                        float val = EditorGUILayout.Slider(prop.PropertyValue.vector4Value.x, rangeLimits.x, rangeLimits.y);
                        prop.PropertyValue.vector4Value = new Vector4(val, 0, 0, 0);
                        EditorGUILayout.EndHorizontal();
                    }
                    break;
                case ShaderPropertyType.Float:
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(prop.PropertyName.stringValue, GUILayout.Width(columnWidth));
                        float val = EditorGUILayout.FloatField(prop.PropertyValue.vector4Value.x);
                        prop.PropertyValue.vector4Value = new Vector4(val, 0, 0, 0);
                        EditorGUILayout.EndHorizontal();
                    }
                    break;
                case ShaderPropertyType.Vector:
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(prop.PropertyName.stringValue, GUILayout.Width(columnWidth));
                        Vector4 val = EditorGUILayout.Vector4Field(GUIContent.none, prop.PropertyValue.vector4Value);
                        prop.PropertyValue.vector4Value = val;
                        EditorGUILayout.EndHorizontal();
                    }
                    break;
                case ShaderPropertyType.Color:
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(prop.PropertyName.stringValue, GUILayout.Width(columnWidth));
                        Color val = prop.PropertyValue.vector4Value;
                        val = EditorGUILayout.ColorField(GUIContent.none, val);
                        prop.PropertyValue.vector4Value = val;
                        EditorGUILayout.EndHorizontal();
                    }
                    break;
            }
        }
    }
}
