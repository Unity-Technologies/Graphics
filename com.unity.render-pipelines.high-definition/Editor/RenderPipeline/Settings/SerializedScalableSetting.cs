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
            where T : struct
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

        public int GetSchemaLevelCount()
        {
            var schema = ScalableSettingSchema.GetSchemaOrNull(new ScalableSettingSchemaId(schemaId.stringValue))
                ?? ScalableSettingSchema.GetSchemaOrNull(ScalableSettingSchemaId.With3Levels);

            return schema.levelCount;
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

            EditorGUI.showMixedValue = self.values.hasMultipleDifferentValues;

            var count = schema.levelCount;

            if (self.values.arraySize != count)
                self.values.arraySize = count;

            LevelValuesFieldGUI<T>(label, self, count, schema);

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
            GUIContent label,
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

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                CoreEditorUtils.DrawMultipleFields(label, labels, values);
                if (scope.changed)
                {
                    for (var i = 0; i < count; ++i)
                        scalableSetting.values.GetArrayElementAtIndex(i).SetInline(values[i]);
                }
            }
        }
    }
}
