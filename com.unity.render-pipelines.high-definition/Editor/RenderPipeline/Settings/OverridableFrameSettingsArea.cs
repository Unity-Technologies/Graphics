using System;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Reflection;
using System.Linq;

namespace UnityEditor.Rendering.HighDefinition
{
    internal struct OverridableFrameSettingsArea
    {
        static readonly GUIContent overrideTooltip = EditorGUIUtility.TrTextContent("", "Override this setting in component.");
        static readonly Dictionary<FrameSettingsField, FrameSettingsFieldAttribute> attributes;
        static Dictionary<int, IOrderedEnumerable<KeyValuePair<FrameSettingsField, FrameSettingsFieldAttribute>>> attributesGroup = new Dictionary<int, IOrderedEnumerable<KeyValuePair<FrameSettingsField, FrameSettingsFieldAttribute>>>();

        /// <summary>Enumerates the keywords corresponding to frame settings properties.</summary>
        internal static readonly string[] frameSettingsKeywords;

        FrameSettings defaultFrameSettings;
        SerializedFrameSettings serializedFrameSettings;

        static OverridableFrameSettingsArea()
        {
            attributes = new Dictionary<FrameSettingsField, FrameSettingsFieldAttribute>();
            attributesGroup = new Dictionary<int, IOrderedEnumerable<KeyValuePair<FrameSettingsField, FrameSettingsFieldAttribute>>>();
            Type type = typeof(FrameSettingsField);
            foreach (FrameSettingsField value in Enum.GetValues(type))
            {
                attributes[value] = type.GetField(Enum.GetName(type, value)).GetCustomAttribute<FrameSettingsFieldAttribute>();
            }

            frameSettingsKeywords = attributes
                .Values.Where(v => !string.IsNullOrEmpty(v?.displayedName))
                .Select(v => v.displayedName?.ToLowerInvariant()).ToArray();
        }

        private struct Field
        {
            public FrameSettingsField field;
            public Func<bool> overrideable;
            public Func<bool> customOverrideable;
            public Func<object> customGetter;
            public Action<object> customSetter;
            public object overridedDefaultValue;
            public GUIContent label => EditorGUIUtility.TrTextContent(attributes[field].displayedName, attributes[field].tooltip);
            public bool IsOverrideableWithDependencies(SerializedFrameSettings serialized, FrameSettings defaultFrameSettings)
            {
                FrameSettingsFieldAttribute attribute = attributes[field];
                bool locallyOverrideable = overrideable == null || overrideable();
                FrameSettingsField[] dependencies = attribute.dependencies;
                if (dependencies == null || !locallyOverrideable)
                    return locallyOverrideable;

                bool dependenciesOverrideable = true;
                for (int index = dependencies.Length - 1; index >= 0 && dependenciesOverrideable; --index)
                {
                    FrameSettingsField depency = dependencies[index];
                    dependenciesOverrideable &= EvaluateBoolWithOverride(depency, this, defaultFrameSettings, serialized, attribute.IsNegativeDependency(depency));
                }
                return dependenciesOverrideable;
            }
        }
        private List<Field> fields;

        public OverridableFrameSettingsArea(int capacity, FrameSettings defaultFrameSettings, SerializedFrameSettings serializedFrameSettings)
        {
            fields = new List<Field>(capacity);
            this.defaultFrameSettings = defaultFrameSettings;
            this.serializedFrameSettings = serializedFrameSettings;
        }

        public static OverridableFrameSettingsArea GetGroupContent(int groupIndex, FrameSettings defaultFrameSettings, SerializedFrameSettings serializedFrameSettings)
        {
            if (!attributesGroup.ContainsKey(groupIndex) || attributesGroup[groupIndex] == null)
                attributesGroup[groupIndex] = attributes?.Where(pair => pair.Value?.group == groupIndex)?.OrderBy(pair => pair.Value.orderInGroup);
            if (!attributesGroup.ContainsKey(groupIndex))
                throw new ArgumentException("Unknown groupIndex");

            var area = new OverridableFrameSettingsArea(attributesGroup[groupIndex].Count(), defaultFrameSettings, serializedFrameSettings);
            foreach (var field in attributesGroup[groupIndex])
            {
                area.Add(field.Key);
            }
            return area;
        }

        public void AmmendInfo(FrameSettingsField field, Func<bool> overrideable = null, Func<object> customGetter = null, Action<object> customSetter = null, object overridedDefaultValue = null, Func<bool> customOverrideable = null, string labelOverride = null)
        {
            var matchIndex = fields.FindIndex(f => f.field == field);

            if (matchIndex == -1)
                throw new FrameSettingsNotFoundInGroupException("This FrameSettings' group do not contain this field. Be sure that the group parameter of the FrameSettingsFieldAttribute match this OverridableFrameSettingsArea groupIndex.");

            var match = fields[matchIndex];
            if (overrideable != null)
                match.overrideable = overrideable;
            if (customOverrideable != null)
                match.customOverrideable = customOverrideable;
            if (customGetter != null)
                match.customGetter = customGetter;
            if (customSetter != null)
                match.customSetter = customSetter;
            if (overridedDefaultValue != null)
                match.overridedDefaultValue = overridedDefaultValue;
            if (labelOverride != null)
                match.label.text = labelOverride;
            fields[matchIndex] = match;
        }

        static bool EvaluateBoolWithOverride(FrameSettingsField field, Field forField, FrameSettings defaultFrameSettings, SerializedFrameSettings serializedFrameSettings, bool negative)
        {
            bool value;
            if (forField.customOverrideable != null)
                return forField.customOverrideable() ^ negative;

            if (serializedFrameSettings.GetOverrides(field))
                value = serializedFrameSettings.IsEnabled(field) ?? false;
            else
                value = defaultFrameSettings.IsEnabled(field);
            return value ^ negative;
        }

        /// <summary>Add an overrideable field to be draw when Draw(bool) will be called.</summary>
        /// <param name="serializedFrameSettings">The overrideable property to draw in inspector</param>
        /// <param name="field">The field drawn</param>
        /// <param name="overrideable">The enabler will be used to check if this field could be overrided. If null or have a return value at true, it will be overrided.</param>
        /// <param name="overridedDefaultValue">The value to display when the property is not overrided. If null, use the actual value of it.</param>
        /// <param name="indent">Add this value number of indent when drawing this field.</param>
        void Add(FrameSettingsField field, Func<bool> overrideable = null, Func<object> customGetter = null, Action<object> customSetter = null, object overridedDefaultValue = null)
            => fields.Add(new Field { field = field, overrideable = overrideable, overridedDefaultValue = overridedDefaultValue, customGetter = customGetter, customSetter = customSetter });

        public void Draw(bool withOverride)
        {
            if (fields == null)
                throw new ArgumentOutOfRangeException("Cannot be used without using the constructor with a capacity initializer.");
            if (withOverride & GUI.enabled)
                OverridesHeaders();
            for (int i = 0; i< fields.Count; ++i)
                DrawField(fields[i], withOverride);
        }

        void DrawField(Field field, bool withOverride)
        {
            int indentLevel = attributes[field.field].indentLevel;
            if (indentLevel == 0)
                --EditorGUI.indentLevel;    //alignment provided by the space for override checkbox
            else
            {
                for (int i = indentLevel - 1; i > 0; --i)
                    ++EditorGUI.indentLevel;
            }
            bool enabled = field.IsOverrideableWithDependencies(serializedFrameSettings, defaultFrameSettings);
            withOverride &= enabled & GUI.enabled;
            bool shouldBeDisabled = withOverride || !enabled || !GUI.enabled;

            const int k_IndentPerLevel = 15;
            const int k_CheckBoxWidth = 15;
            const int k_CheckboxLabelSeparator = 5;
            const int k_LabelFieldSeparator = 2;
            float indentValue = k_IndentPerLevel * EditorGUI.indentLevel;

            Rect lineRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
            Rect overrideRect = lineRect;
            overrideRect.width = k_CheckBoxWidth;
            Rect labelRect = lineRect;
            labelRect.x += k_CheckBoxWidth + k_CheckboxLabelSeparator;
            labelRect.width = EditorGUIUtility.labelWidth - indentValue;
            Rect fieldRect = lineRect;
            fieldRect.x = labelRect.xMax + k_LabelFieldSeparator;
            fieldRect.width -= fieldRect.x - lineRect.x;

            if (withOverride)
            {
                int currentIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                bool mixedValue = serializedFrameSettings.HaveMultipleOverride(field.field);
                bool originalValue = serializedFrameSettings.GetOverrides(field.field) && !mixedValue;
                overrideRect.yMin += 4f;

                // MixedValueState is handled by style for small tickbox for strange reason
                //EditorGUI.showMixedValue = mixedValue;
                bool modifiedValue = EditorGUI.Toggle(overrideRect, overrideTooltip, originalValue, mixedValue? CoreEditorStyles.smallMixedTickbox : CoreEditorStyles.smallTickbox);
                //EditorGUI.showMixedValue = false;

                if (originalValue ^ modifiedValue)
                    serializedFrameSettings.SetOverrides(field.field, modifiedValue);

                shouldBeDisabled = !modifiedValue;
                EditorGUI.indentLevel = currentIndent;
            }

            using(new SerializedFrameSettings.TitleDrawingScope(labelRect, field.label, serializedFrameSettings))
            {
                HDEditorUtils.HandlePrefixLabelWithIndent(lineRect, labelRect, field.label);
            }

            using (new EditorGUI.DisabledScope(shouldBeDisabled))
            {
                EditorGUI.showMixedValue = serializedFrameSettings.HaveMultipleValue(field.field);
                using (new EditorGUILayout.VerticalScope())
                {
                    //the following block will display a default value if provided instead of actual value (case if(true))
                    if (shouldBeDisabled)
                    {
                        if (field.overridedDefaultValue == null)
                        {
                            switch (attributes[field.field].type)
                            {
                                case FrameSettingsFieldAttribute.DisplayType.BoolAsCheckbox:
                                    DrawFieldShape(fieldRect, defaultFrameSettings.IsEnabled(field.field));
                                    break;
                                case FrameSettingsFieldAttribute.DisplayType.BoolAsEnumPopup:
                                    //shame but it is not possible to use Convert.ChangeType to convert int into enum in current C#
                                    //rely on string parsing for the moment
                                    var oldEnumValue = Enum.Parse(attributes[field.field].targetType, defaultFrameSettings.IsEnabled(field.field) ? "1" : "0");
                                    DrawFieldShape(fieldRect, oldEnumValue);
                                    break;
                                case FrameSettingsFieldAttribute.DisplayType.Others:
                                    var oldValue = field.customGetter();
                                    DrawFieldShape(fieldRect, oldValue);
                                    break;
                                default:
                                    throw new ArgumentException("Unknown FrameSettingsFieldAttribute");
                            }
                        }
                        else
                            DrawFieldShape(fieldRect, field.overridedDefaultValue);
                    }
                    else
                    {
                        switch (attributes[field.field].type)
                        {
                            case FrameSettingsFieldAttribute.DisplayType.BoolAsCheckbox:
                                bool oldBool = serializedFrameSettings.IsEnabled(field.field) ?? false;
                                bool newBool = (bool)DrawFieldShape(fieldRect, oldBool);
                                if (oldBool ^ newBool)
                                {
                                    Undo.RecordObject(serializedFrameSettings.serializedObject.targetObject, "Changed FrameSettings " + field.field);
                                    serializedFrameSettings.SetEnabled(field.field, newBool);
                                }
                                break;
                            case FrameSettingsFieldAttribute.DisplayType.BoolAsEnumPopup:
                                //shame but it is not possible to use Convert.ChangeType to convert int into enum in current C#
                                //Also, Enum.Equals and Enum operator!= always send true here. As it seams to compare object reference instead of value.
                                var oldBoolValue = serializedFrameSettings.IsEnabled(field.field);
                                int oldEnumIntValue = -1;
                                int newEnumIntValue;
                                object newEnumValue;
                                if (oldBoolValue.HasValue)
                                {
                                    var oldEnumValue = Enum.GetValues(attributes[field.field].targetType).GetValue(oldBoolValue.Value ? 1 : 0);
                                    newEnumValue = Convert.ChangeType(DrawFieldShape(fieldRect, oldEnumValue), attributes[field.field].targetType);
                                    oldEnumIntValue = ((IConvertible)oldEnumValue).ToInt32(System.Globalization.CultureInfo.CurrentCulture);
                                    newEnumIntValue = ((IConvertible)newEnumValue).ToInt32(System.Globalization.CultureInfo.CurrentCulture);
                                }
                                else //in multi edition, do not assume any previous value
                                {
                                    newEnumIntValue = EditorGUI.Popup(fieldRect, -1, Enum.GetNames(attributes[field.field].targetType));
                                    newEnumValue = newEnumIntValue < 0 ? null : Enum.GetValues(attributes[field.field].targetType).GetValue(newEnumIntValue);
                                }
                                if (oldEnumIntValue != newEnumIntValue)
                                {
                                    Undo.RecordObject(serializedFrameSettings.serializedObject.targetObject, "Changed FrameSettings " + field.field);
                                    serializedFrameSettings.SetEnabled(field.field, Convert.ToInt32(newEnumValue) == 1);
                                }
                                break;
                            case FrameSettingsFieldAttribute.DisplayType.Others:
                                var oldValue = field.customGetter();
                                var newValue = DrawFieldShape(fieldRect, oldValue);
                                if (oldValue != newValue)
                                {
                                    Undo.RecordObject(serializedFrameSettings.serializedObject.targetObject, "Changed FrameSettings " + field.field);
                                    field.customSetter(newValue);
                                }
                                break;
                            default:
                                throw new ArgumentException("Unknown FrameSettingsFieldAttribute");
                        }

                    }
                }
                EditorGUI.showMixedValue = false;
            }

            if (indentLevel == 0)
            {
                ++EditorGUI.indentLevel;
            }
            else
            {
                for (int i = indentLevel - 1; i > 0; --i)
                {
                    --EditorGUI.indentLevel;
                }
            }
        }

        object DrawFieldShape(Rect rect, object field)
        {
            if (field is GUIContent)
            {
                EditorGUI.LabelField(rect, (GUIContent)field);
                return null;
            }
            else if (field is string)
                return EditorGUI.TextField(rect, (string)field);
            else if (field is bool)
                return EditorGUI.Toggle(rect, (bool)field);
            else if (field is int)
                return EditorGUI.IntField(rect, (int)field);
            else if (field is float)
                return EditorGUI.FloatField(rect, (float)field);
            else if (field is Color)
                return EditorGUI.ColorField(rect, (Color)field);
            else if (field is Enum)
                return EditorGUI.EnumPopup(rect, (Enum)field);
            else if (field is LayerMask)
                return EditorGUI.MaskField(rect, (LayerMask)field, GraphicsSettings.currentRenderPipeline.renderingLayerMaskNames);
            else if (field is UnityEngine.Object)
                return EditorGUI.ObjectField(rect, (UnityEngine.Object)field, field.GetType(), true);
            else if (field is SerializedProperty)
                return EditorGUI.PropertyField(rect, (SerializedProperty)field, includeChildren: true);
            else
            {
                EditorGUI.LabelField(rect, new GUIContent("Unsupported type"));
                Debug.LogError("Unsupported format " + field.GetType() + " in OverridableSettingsArea.cs. Please add it!");
                return null;
            }
        }

        void OverridesHeaders()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayoutUtility.GetRect(0f, 17f, GUILayout.ExpandWidth(false));
                if (GUILayout.Button(EditorGUIUtility.TrTextContent("All", "Toggle all overrides on. To maximize performances you should only toggle overrides that you actually need."), CoreEditorStyles.miniLabelButton, GUILayout.Width(17f), GUILayout.ExpandWidth(false)))
                {
                    foreach (var field in fields)
                    {
                        if (field.IsOverrideableWithDependencies(serializedFrameSettings, defaultFrameSettings))
                            serializedFrameSettings.SetOverrides(field.field, true);
                    }
                }

                if (GUILayout.Button(EditorGUIUtility.TrTextContent("None", "Toggle all overrides off."), CoreEditorStyles.miniLabelButton, GUILayout.Width(32f), GUILayout.ExpandWidth(false)))
                {
                    foreach (var field in fields)
                    {
                        if (field.IsOverrideableWithDependencies(serializedFrameSettings, defaultFrameSettings))
                            serializedFrameSettings.SetOverrides(field.field, false);
                    }
                }

                GUILayout.FlexibleSpace();
            }
        }
    }

    class FrameSettingsNotFoundInGroupException : Exception
    {
        public FrameSettingsNotFoundInGroupException(string message)
            : base(message)
        { }
    }
}
