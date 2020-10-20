using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Serialized version of <see cref="ScalableSettingValue{T}"/>.
    /// </summary>
    internal class SerializedScalableSettingValue
    {
        public SerializedProperty level;
        public SerializedProperty useOverride;
        public SerializedProperty @override;

        /// <summary>
        /// <c>true</c> when the value evaluated has different multiple values.
        /// <c>false</c> if it has a single value.
        /// </summary>
        public bool hasMultipleValues => useOverride.hasMultipleDifferentValues
                || useOverride.boolValue && @override.hasMultipleDifferentValues
                || !useOverride.boolValue && level.hasMultipleDifferentValues;

        public SerializedScalableSettingValue(SerializedProperty property)
        {
            level = property.FindPropertyRelative("m_Level");
            useOverride = property.FindPropertyRelative("m_UseOverride");
            @override = property.FindPropertyRelative("m_Override");
        }

        /// <summary>
        /// Evaluate the value of the scalable setting value.
        /// </summary>
        /// <typeparam name="T">The type of the scalable setting.</typeparam>
        /// <param name="setting">The scalable setting to use to evaluate levels.</param>
        /// <param name="value">The evaluated value.</param>
        /// <returns><c>true</c> when the value was evaluated, <c>false</c> when the value could not be evaluated.</returns>
        public bool TryGetValue<T>(SerializedScalableSetting setting, out T value)
            where T : struct
        {
            if (hasMultipleValues)
            {
                value = default;
                return false;
            }

            if (useOverride.boolValue)
            {
                value = @override.GetInline<T>();
                return true;
            }
            else
            {
                var actualLevel = level.intValue;
                return setting.TryGetLevelValue(actualLevel, out value);
            }
        }

        /// <summary>
        /// Evaluate the value of the scalable setting value.
        /// </summary>
        /// <typeparam name="T">The type of the scalable setting.</typeparam>
        /// <param name="setting">The scalable setting to use to evaluate levels.</param>
        /// <param name="value">The evaluated value.</param>
        /// <returns><c>true</c> when the value was evaluated, <c>false</c> when the value could not be evaluated.</returns>
        public bool TryGetValue<T>(ScalableSetting<T> setting, out T value)
            where T : struct
        {
            if (hasMultipleValues)
            {
                value = default;
                return false;
            }

            if (useOverride.boolValue)
            {
                value = @override.GetInline<T>();
                return true;
            }
            else
            {
                var actualLevel = level.intValue;
                return setting.TryGet(actualLevel, out value);
            }
        }
    }

    internal static class SerializedScalableSettingValueUI
    {
        /// <summary>
        /// Draw the level enum popup for a scalable setting value.
        ///
        /// The popup displays the level available and a `custom` entry to provide an explicit value.
        /// </summary>
        /// <param name="rect">The rect to use to draw the popup.</param>
        /// <param name="label">The label to use for the popup.</param>
        /// <param name="schema">The schema of the scalable setting. This provides the number of levels availables.</param>
        /// <param name="level">The level to use, when <paramref name="useOverride"/> is <c>false</c>.</param>
        /// <param name="useOverride">Whether to use the custom value or the level's value.</param>
        /// <returns>
        /// If the field uses a level value, then <c>(level, true)</c> is returned, where <c>level</c> is the level used.
        /// Otherwise, <c>(-1, 0)</c> is returned.
        /// </returns>
        public static (int level, bool useOverride) LevelFieldGUI(
            Rect rect,
            GUIContent label,
            ScalableSettingSchema schema,
            int level,
            bool useOverride
        )
        {
            var enumValue = useOverride ? 0 : level + 1;
            var levelCount = schema.levelCount;
            var options = new GUIContent[levelCount + 1];
            options[0] = new GUIContent("Custom");
            Array.Copy(schema.levelNames, 0, options, 1, levelCount);

            var newValue = EditorGUI.Popup(rect, label, enumValue, options);

            return (newValue - 1, newValue == 0);
        }

        /// <summary>Draws the level popup and the associated value in a field style GUI with an int field.</summary>
        /// <param name="self">The scalable setting value to draw.</param>
        /// <param name="label">The label to use for the field.</param>
        /// <param name="sourceValue">The associated scalable setting. This one defines the levels for this value.</param>
        /// <param name="sourceName">A string describing the source of the scalable settings. Usually the name of the containing asset.</param>
        public static void LevelAndIntGUILayout(
            this SerializedScalableSettingValue self,
            GUIContent label,
            ScalableSetting<int> sourceValue,
            string sourceName
        ) => LevelAndGUILayout<int, IntFieldGUI>(self, label, sourceValue, sourceName);

        /// <summary>Draws the level popup and the associated value in a field style GUI with an toggle field.</summary>
        /// <param name="self">The scalable setting value to draw.</param>
        /// <param name="label">The label to use for the field.</param>
        /// <param name="sourceValue">The associated scalable setting. This one defines the levels for this value.</param>
        /// <param name="sourceName">A string describing the source of the scalable settings. Usually the name of the containing asset.</param>
        public static void LevelAndToggleGUILayout(
            this SerializedScalableSettingValue self,
            GUIContent label,
            ScalableSetting<bool> sourceValue,
            string sourceName
        ) => LevelAndGUILayout<bool, ToggleFieldGUI>(self, label, sourceValue, sourceName);

        /// <summary>Draws the level popup and the associated value in a field style GUI with an Enum field.</summary>
        /// <param name="self">The scalable setting value to draw.</param>
        /// <param name="label">The label to use for the field.</param>
        /// <param name="sourceValue">The associated scalable setting. This one defines the levels for this value.</param>
        /// <param name="sourceName">A string describing the source of the scalable settings. Usually the name of the containing asset.</param>
        public static void LevelAndEnumGUILayout<H>
        (
            this SerializedScalableSettingValue self,
            GUIContent label,
            ScalableSetting<H> sourceValue,
            string sourceName
        ) where H : Enum => LevelAndGUILayout<H, EnumFieldGUI<H>>(self, label, sourceValue, sourceName);

        /// <summary>
        /// Draw the level enum popup for a scalable setting value.
        ///
        /// The popup displays the level available and a `custom` entry to provide an explicit value.
        /// </summary>
        /// <param name = "rect" > The rect to use to draw the popup.</param>
        /// <param name="label">The label to use for the popup.</param>
        /// <param name="schema">The schema of the scalable setting. This provides the number of levels availables.</param>
        /// <returns>The rect to use to render the value of the field. (Either the custom value or the level value).</returns>
        static Rect LevelFieldGUILayout(
            SerializedScalableSettingValue self,
            GUIContent label,
            ScalableSettingSchema schema
        )
        {
            Assert.IsNotNull(schema);

            // Match const defined in EditorGUI.cs
            const int k_IndentPerLevel = 15;
            const int k_PrefixPaddingRight = 2;

            const int k_ValueUnitSeparator = 2;
            const int k_EnumWidth = 70;

            float indent = k_IndentPerLevel * EditorGUI.indentLevel;

            Rect lineRect = EditorGUILayout.GetControlRect();
            Rect labelRect = lineRect;
            Rect levelRect = lineRect;
            Rect fieldRect = lineRect;
            labelRect.width = EditorGUIUtility.labelWidth;
            // Dealing with indentation add space before the actual drawing
            // Thus resize accordingly to have a coherent aspect
            levelRect.x += labelRect.width - indent + k_PrefixPaddingRight;
            levelRect.width = k_EnumWidth + indent;
            fieldRect.x = levelRect.x + levelRect.width + k_ValueUnitSeparator - indent;
            fieldRect.width -= fieldRect.x - lineRect.x;

            label = EditorGUI.BeginProperty(labelRect, label, self.level);
            label = EditorGUI.BeginProperty(labelRect, label, self.@override);
            label = EditorGUI.BeginProperty(labelRect, label, self.useOverride);
            {
                EditorGUI.LabelField(labelRect, label);
            }
            EditorGUI.EndProperty();
            EditorGUI.EndProperty();
            EditorGUI.EndProperty();

            EditorGUI.BeginProperty(levelRect, label, self.level);
            EditorGUI.BeginProperty(levelRect, label, self.useOverride);
            {
                EditorGUI.BeginChangeCheck();
                var (level, useOverride) = LevelFieldGUI(
                    levelRect,
                    GUIContent.none,
                    schema,
                    self.level.intValue,
                    self.useOverride.boolValue
                );
                if (EditorGUI.EndChangeCheck())
                {
                    self.useOverride.boolValue = useOverride;
                    if (!self.useOverride.boolValue)
                        self.level.intValue = level;
                }
            }
            EditorGUI.EndProperty();
            EditorGUI.EndProperty();

            return fieldRect;
        }

        /// <summary>
        /// Draw the scalable setting as a popup and field GUI in a single line.
        /// 
        /// This helper statically dispatch the appropriate GUI call depending
        /// on the multiselection state of the serialized properties.
        /// </summary>
        /// <typeparam name="T">The type of the scalable property.</typeparam>
        /// <typeparam name="FieldGUI">The renderer of the property. It must implements <see cref="IFieldGUI{T}"/></typeparam>
        /// <param name="self">The scalable property to render.</param>
        /// <param name="label">The label of the scalable property field.</param>
        /// <param name="sourceValue">The source of the scalable setting.</param>
        /// <param name="sourceName">A description of the scalable setting, usually the name of its containing asset.</param>
        /// <param name="defaultSchema">
        /// The id of the schema to use when the scalable setting is null.
        /// Defaults to <see cref="ScalableSettingSchemaId.With3Levels"/>.
        /// </param>
        static void LevelAndGUILayout<T, FieldGUI>(
            this SerializedScalableSettingValue self,
            GUIContent label,
            ScalableSetting<T> sourceValue,
            string sourceName,
            ScalableSettingSchemaId defaultSchema = default
        ) where FieldGUI : IFieldGUI<T>, new()
        {
            var resolvedDefaultSchema = defaultSchema.Equals(default) ? ScalableSettingSchemaId.With3Levels : defaultSchema;
            var gui = new FieldGUI();

            var schema = ScalableSettingSchema.GetSchemaOrNull(sourceValue?.schemaId)
                ?? ScalableSettingSchema.GetSchemaOrNull(resolvedDefaultSchema);

            var fieldRect = LevelFieldGUILayout(self, label, schema);

            EditorGUI.BeginProperty(fieldRect, label, self.level);
            EditorGUI.BeginProperty(fieldRect, label, self.@override);
            EditorGUI.BeginProperty(fieldRect, label, self.useOverride);
            if (!self.useOverride.hasMultipleDifferentValues && self.useOverride.boolValue)
            {
                // All fields have custom values
                // So we show the custom value field GUI.

                var showMixedValues = EditorGUI.showMixedValue;
                EditorGUI.showMixedValue = self.@override.hasMultipleDifferentValues || showMixedValues;
                gui.CustomGUI(fieldRect, self, label, sourceValue, sourceName);
                EditorGUI.showMixedValue = showMixedValues;
            }
            else
            {
                if (self.useOverride.hasMultipleDifferentValues
                    || !self.useOverride.boolValue && self.level.hasMultipleDifferentValues)
                    // Scalable settings have either:
                    //  - custom or level values
                    //  - level values with different levels
                    gui.MixedValueDescriptionGUI(fieldRect, self, label, sourceValue, sourceName);
                else
                    // Scalable settings have all the same level value
                    gui.LevelValueDescriptionGUI(fieldRect, self, label, sourceValue, sourceName);
            }
            EditorGUI.EndProperty();
            EditorGUI.EndProperty();
            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Renderer interface for a scalable setting.
        ///
        /// Implement this in a struct so the GUI calls can be inlined in
        /// <see cref="LevelAndGUILayout{T, FieldGUI}(SerializedScalableSettingValue, GUIContent, ScalableSetting{T}, string, ScalableSettingSchemaId)"/>.
        ///
        /// For an example, <see cref="IntFieldGUI"/>.
        /// </summary>
        /// <typeparam name="T">The type of the scalable setting.</typeparam>
        interface IFieldGUI<T>
        {
            /// <summary>
            /// Draw the custom value field.
            ///
            /// Assumes that all scalable properties uses a custom value.
            /// </summary>
            /// <param name="fieldRect">The rect to use to draw the value field.</param>
            /// <param name="self">The scalable setting value to render.</param>
            /// <param name="label">The label that was used for this scalable setting value.</param>
            /// <param name="sourceValue">The scalable setting that defines the levels.</param>
            /// <param name="sourceName">A description of the scalable setting source.</param>
            void CustomGUI(
                Rect fieldRect,
                SerializedScalableSettingValue self,
                GUIContent label,
                ScalableSetting<T> sourceValue,
                string sourceName
            );

            /// <summary>
            /// Draw the value field in a mixed value state.
            ///
            /// Scalable properties have either custom and level values or levels values with different levels.
            /// </summary>
            /// <param name="fieldRect">The rect to use to draw the value field.</param>
            /// <param name="self">The scalable setting value to render.</param>
            /// <param name="label">The label that was used for this scalable setting value.</param>
            /// <param name="sourceValue">The scalable setting that defines the levels.</param>
            /// <param name="sourceName">A description of the scalable setting source.</param>
            void MixedValueDescriptionGUI(
                Rect fieldRect,
                SerializedScalableSettingValue self,
                GUIContent label,
                ScalableSetting<T> sourceValue,
                string sourceName
            );

            /// <summary>
            /// Draw the value of a level for a scalable setting value.
            ///
            /// Scalable properties have all level values with the same level.
            /// This functions is expected to draw the level value with its source description.
            /// </summary>
            /// <param name="fieldRect">The rect to use to draw the value field.</param>
            /// <param name="self">The scalable setting value to render.</param>
            /// <param name="label">The label that was used for this scalable setting value.</param>
            /// <param name="sourceValue">The scalable setting that defines the levels.</param>
            /// <param name="sourceName">A description of the scalable setting source.</param>
            void LevelValueDescriptionGUI(
                Rect fieldRect,
                SerializedScalableSettingValue self,
                GUIContent label,
                ScalableSetting<T> sourceValue,
                string sourceName
            );
        }

        #region Field Renderer Implementations
        struct IntFieldGUI : IFieldGUI<int>
        {
            public void CustomGUI(
                Rect fieldRect,
                SerializedScalableSettingValue self,
                GUIContent label,
                ScalableSetting<int> sourceValue,
                string sourceName
            )
            {
                self.@override.intValue = EditorGUI.IntField(fieldRect, self.@override.intValue);
            }

            public void LevelValueDescriptionGUI(
                Rect fieldRect,
                SerializedScalableSettingValue self,
                GUIContent label,
                ScalableSetting<int> sourceValue,
                string sourceName
            )
            {
                EditorGUI.LabelField(fieldRect, $"{(sourceValue != null ? sourceValue[self.level.intValue] : 0)} ({sourceName})");
            }

            public void MixedValueDescriptionGUI(
                Rect fieldRect,
                SerializedScalableSettingValue self,
                GUIContent label,
                ScalableSetting<int> sourceValue,
                string sourceName
            )
            {
                EditorGUI.LabelField(fieldRect, $"---");
            }
        }

        struct ToggleFieldGUI : IFieldGUI<bool>
        {
            public void CustomGUI(
                Rect fieldRect,
                SerializedScalableSettingValue self,
                GUIContent label,
                ScalableSetting<bool> sourceValue,
                string sourceName
            )
            {
                self.@override.boolValue = EditorGUI.Toggle(fieldRect, self.@override.boolValue);
            }

            public void LevelValueDescriptionGUI(
                Rect fieldRect,
                SerializedScalableSettingValue self,
                GUIContent label,
                ScalableSetting<bool> sourceValue,
                string sourceName
            )
            {
                var enabled = GUI.enabled;
                GUI.enabled = false;
                EditorGUI.Toggle(fieldRect, sourceValue != null ? sourceValue[self.level.intValue] : false);
                fieldRect.x += 25;
                fieldRect.width -= 25;
                EditorGUI.LabelField(fieldRect, $"({sourceName})");
                GUI.enabled = enabled;
            }

            public void MixedValueDescriptionGUI(
                Rect fieldRect,
                SerializedScalableSettingValue self,
                GUIContent label,
                ScalableSetting<bool> sourceValue,
                string sourceName
            )
            {
                EditorGUI.LabelField(fieldRect, $"---");
            }
        }

        struct EnumFieldGUI<H> : IFieldGUI<H>
        {
            public void CustomGUI(
                Rect fieldRect,
                SerializedScalableSettingValue self,
                GUIContent label,
                ScalableSetting<H> sourceValue,
                string sourceName
            )
            {
                // Due to a constraint in the scalability setting, we cannot simply precise the H type as an Enum in the struct declaration.
                // this shenanigans are not pretty, but we do not fall into a high complexity everytime we want to support a new enum.
                Enum data = (Enum)Enum.Parse(typeof(H), self.@override.intValue.ToString());
                self.@override.intValue = (int)(object)EditorGUI.EnumPopup(fieldRect, data);
            }

            public void LevelValueDescriptionGUI(
                Rect fieldRect,
                SerializedScalableSettingValue self,
                GUIContent label,
                ScalableSetting<H> sourceValue,
                string sourceName
            )
            {
                var enabled = GUI.enabled;
                GUI.enabled = false;
                // See the comment in the function above.
                var defaultValue = (Enum)(Enum.GetValues(typeof(H)).GetValue(0));
                EditorGUI.EnumPopup(fieldRect, sourceValue != null ? (Enum)(object)(sourceValue[self.level.intValue]) : defaultValue);
                fieldRect.x += 25;
                fieldRect.width -= 25;
                GUI.enabled = enabled;
            }

            public void MixedValueDescriptionGUI(
                Rect fieldRect,
                SerializedScalableSettingValue self,
                GUIContent label,
                ScalableSetting<H> sourceValue,
                string sourceName
            )
            {
                EditorGUI.LabelField(fieldRect, $"---");
            }
        }
        #endregion
    }
}
