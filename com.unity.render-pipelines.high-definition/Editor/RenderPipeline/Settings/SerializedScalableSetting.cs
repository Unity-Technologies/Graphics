using System;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    public class SerializedScalableSetting
    {
        public SerializedProperty low;
        public SerializedProperty medium;
        public SerializedProperty high;

        public SerializedScalableSetting(SerializedProperty property)
        {
            low = property.FindPropertyRelative("m_Low");
            medium = property.FindPropertyRelative("m_Medium");
            high = property.FindPropertyRelative("m_High");
        }
    }

    public static class SerializedScalableSettingUI
    {
        private static readonly GUIContent k_ShortLow = new GUIContent("L", "Low");
        private static readonly GUIContent k_ShortMed = new GUIContent("M", "Medium");
        private static readonly GUIContent k_ShortHigh = new GUIContent("H", "High");

        private static readonly GUIContent k_Low = new GUIContent("Low", "Low");
        private static readonly GUIContent k_Med = new GUIContent("Medium", "Medium");
        private static readonly GUIContent k_High = new GUIContent("High", "High");

        public static void ValueGUI<T>(this SerializedScalableSetting self, GUIContent label)
        {
            var rect = GUILayoutUtility.GetRect(0, float.Epsilon, 0, EditorGUIUtility.singleLineHeight);
            // Magic Number !!
            rect.x += 3;
            rect.width -= 6;
            // Magic Number !!

            var contentRect = EditorGUI.PrefixLabel(rect, label);
            EditorGUI.showMixedValue = self.low.hasMultipleDifferentValues
                                       || self.medium.hasMultipleDifferentValues
                                       || self.high.hasMultipleDifferentValues;

            if (typeof(T) == typeof(bool))
            {
                GUIContent[] labels = {k_Low, k_Med, k_High};
                bool[] values =
                {
                    self.low.boolValue,
                    self.medium.boolValue,
                    self.high.boolValue
                };
                EditorGUI.BeginChangeCheck();
                MultiField(contentRect, labels, values);
                if(EditorGUI.EndChangeCheck())
                {
                    self.low.boolValue = values[0];
                    self.medium.boolValue = values[1];
                    self.high.boolValue = values[2];
                }
            }
            else if (typeof(T) == typeof(int))
            {
                GUIContent[] labels = {k_ShortLow, k_ShortMed, k_ShortHigh};
                int[] values =
                {
                    self.low.intValue,
                    self.medium.intValue,
                    self.high.intValue
                };
                EditorGUI.BeginChangeCheck();
                MultiField(contentRect, labels, values);
                if(EditorGUI.EndChangeCheck())
                {
                    self.low.intValue = values[0];
                    self.medium.intValue = values[1];
                    self.high.intValue = values[2];
                }
            }
            else if (typeof(T) == typeof(float))
            {
                GUIContent[] labels = {k_ShortLow, k_ShortMed, k_ShortHigh};
                float[] values =
                {
                    self.low.floatValue,
                    self.medium.floatValue,
                    self.high.floatValue
                };
                EditorGUI.BeginChangeCheck();
                MultiField(contentRect, labels, values);
                if(EditorGUI.EndChangeCheck())
                {
                    self.low.floatValue = values[0];
                    self.medium.floatValue = values[1];
                    self.high.floatValue = values[2];
                }
            }

            EditorGUI.showMixedValue = false;
        }

        internal static void MultiField<T>(Rect position, GUIContent[] subLabels, T[] values)
        {
            var length = values.Length;
            var num = (position.width - (float) (length - 1) * 3f) / (float) length;
            var position1 = new Rect(position)
            {
                width = num
            };
            var labelWidth = EditorGUIUtility.labelWidth;
            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            for (var index = 0; index < values.Length; ++index)
            {
                EditorGUIUtility.labelWidth = CalcPrefixLabelWidth(subLabels[index], (GUIStyle) null);
                if (typeof(T) == typeof(int))
                    values[index] = (T)(object)EditorGUI.IntField(position1, subLabels[index], (int)(object)values[index]);
                else if (typeof(T) == typeof(bool))
                    values[index] = (T)(object)EditorGUI.Toggle(position1, subLabels[index], (bool)(object)values[index]);
                else if (typeof(T) == typeof(float))
                    values[index] = (T)(object)EditorGUI.FloatField(position1, subLabels[index], (float)(object)values[index]);
                position1.x += num + 4f;
            }
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.indentLevel = indentLevel;
        }

        internal static float CalcPrefixLabelWidth(GUIContent label, GUIStyle style = null)
        {
            if (style == null)
                style = EditorStyles.label;
            return style.CalcSize(label).x;
        }
    }
}
