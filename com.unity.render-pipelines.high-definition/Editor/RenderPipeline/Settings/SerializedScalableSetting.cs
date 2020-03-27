using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Serialized version of <see cref="ScalableSetting{T}"/>.
    /// </summary>
    internal class SerializedScalableSetting
    {
        public SerializedProperty values;
        public SerializedProperty schemaId;

        public SerializedScalableSetting(SerializedProperty property)
        {
            values = property.FindPropertyRelative("m_Values");
            schemaId = property.FindPropertyRelative("m_SchemaId.m_Id");
        }

        /// <summary>Get the value of level <paramref name="level"/>.</summary>
        /// <typeparam name="T">The type of the scalable setting.</typeparam>
        /// <param name="level">The level to get.</param>
        /// <param name="value">
        /// The value of the level if the level was found.
        ///
        /// <c>default(T)</c> when:
        ///  - The level does not exists (level index is out of range)
        ///  - The level value has multiple different values
        /// </param>
        /// <returns><c>true</c> when the value was evaluated, <c>false</c> when the value could not be evaluated.</returns>
        public bool TryGetLevelValue<T>(int level, out T value)
            where T: struct
        {
            if (level < values.arraySize && level >= 0)
            {
                var levelValue = values.GetArrayElementAtIndex(level);
                if (levelValue.hasMultipleDifferentValues)
                {
                    value = default;
                    return false;
                }
                else
                {
                    value = levelValue.GetInline<T>();
                    return true;
                }
            }
            else
            {
                value = default;
                return false;
            }
        }
    }

    internal static class SerializedScalableSettingUI
    {
        /// <summary>
        /// Draw the scalable setting as a single line field with multiple values.
        ///
        /// There will be one value per level.
        /// </summary>
        /// <typeparam name="T">The type of the scalable setting.</typeparam>
        /// <param name="self">The scalable setting to draw.</param>
        /// <param name="label">The label of the field.</param>
        public static void ValueGUI<T>(this SerializedScalableSetting self, GUIContent label)
            where T : struct
        {
            var schema = ScalableSettingSchema.GetSchemaOrNull(new ScalableSettingSchemaId(self.schemaId.stringValue))
                ?? ScalableSettingSchema.GetSchemaOrNull(ScalableSettingSchemaId.With3Levels);

            var rect = GUILayoutUtility.GetRect(0, float.Epsilon, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight);
            // Magic Number !!
            rect.x += 3;
            rect.width -= 6;
            // Magic Number !!

            var contentRect = EditorGUI.PrefixLabel(rect, label);
            EditorGUI.showMixedValue = self.values.hasMultipleDifferentValues;

            var count = schema.levelCount;

            if (self.values.arraySize != count)
                self.values.arraySize = count;

            if (typeof(T) == typeof(bool))
                LevelValuesFieldGUI<bool>(contentRect, self, count, schema);
            else if (typeof(T) == typeof(int))
                LevelValuesFieldGUI<int>(contentRect, self, count, schema);
            else if (typeof(T) == typeof(float))
                LevelValuesFieldGUI<float>(contentRect, self, count, schema);

            EditorGUI.showMixedValue = false;
        }

        /// <summary>
        /// Draw the value fields for each levels of the scalable setting.
        ///
        /// Assumes that the generic type is the type stored in the <see cref="SerializedScalableSetting"/>.
        /// </summary>
        /// <typeparam name="T">The type of the scalable setting.</typeparam>
        /// <param name="rect">Rect used to draw the GUI.</param>
        /// <param name="scalableSetting">The scalable setting to draw.</param>
        /// <param name="count">The number of level to draw.</param>
        /// <param name="schema">The schema to use when drawing the levels.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void LevelValuesFieldGUI<T>(
            Rect rect,
            SerializedScalableSetting scalableSetting,
            int count,
            ScalableSettingSchema schema
        )
            where T : struct
        {
            var labels = new GUIContent[count];
            Array.Copy(schema.levelNames, labels, count);
            var values = new T[count];
            for (var i = 0; i < count; ++i)
                values[i] = scalableSetting.values.GetArrayElementAtIndex(i).GetInline<T>();
            EditorGUI.BeginChangeCheck();
            MultiField(rect, labels, values);
            if (EditorGUI.EndChangeCheck())
            {
                for (var i = 0; i < count; ++i)
                    scalableSetting.values.GetArrayElementAtIndex(i).SetInline(values[i]);
            }
        }

        /// <summary>Draw multiple fields in a single line.</summary>
        /// <typeparam name="T">The type to render.</typeparam>
        /// <param name="position">The rect to use to draw the GUI.</param>
        /// <param name="subLabels">The labels for each sub value field.</param>
        /// <param name="values">The current values of the fields.</param>
        static void MultiField<T>(Rect position, GUIContent[] subLabels, T[] values)
            where T: struct
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
                    values[index] = (T)(object)EditorGUI.DelayedIntField(position1, subLabels[index], (int)(object)values[index]);
                else if (typeof(T) == typeof(bool))
                    values[index] = (T)(object)EditorGUI.Toggle(position1, subLabels[index], (bool)(object)values[index]);
                else if (typeof(T) == typeof(float))
                    values[index] = (T)(object)EditorGUI.FloatField(position1, subLabels[index], (float)(object)values[index]);
                else
                    throw new ArgumentOutOfRangeException($"<{typeof(T)}> is not a supported type for multi field");
                position1.x += num + 4f;
            }
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.indentLevel = indentLevel;
        }

        static float CalcPrefixLabelWidth(GUIContent label, GUIStyle style = null)
        {
            if (style == null)
                style = EditorStyles.label;
            return style.CalcSize(label).x;
        }
    }
}
