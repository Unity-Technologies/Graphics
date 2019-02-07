using System;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Reflection;
using System.Linq;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal struct OverridableFrameSettingsArea
    {
        static readonly GUIContent overrideTooltip = EditorGUIUtility.TrTextContent("", "Override this setting in component.");
        static readonly Dictionary<FrameSettingsField, FrameSettingsFieldAttribute> attributes;
        static Dictionary<int, IOrderedEnumerable<KeyValuePair<FrameSettingsField, FrameSettingsFieldAttribute>>> attributesGroup = new Dictionary<int, IOrderedEnumerable<KeyValuePair<FrameSettingsField, FrameSettingsFieldAttribute>>>();

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
        }
        
        private struct Field
        {
            public FrameSettingsField field;
            public Func<bool> overrideable;
            public Func<object> customGetter;
            public Action<object> customSetter;
            public object overridedDefaultValue;
            public GUIContent label => EditorGUIUtility.TrTextContent(attributes[field].displayedName);
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
                    dependenciesOverrideable &= EvaluateBoolWithOverride(depency, defaultFrameSettings, serialized, attribute.IsNegativeDependency(depency));
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

        public void AmmendInfo(FrameSettingsField field, Func<bool> overrideable = null, Func<object> customGetter = null, Action<object> customSetter = null, object overridedDefaultValue = null)
        {
            var match = fields.Find(f => f.field == field);
            if (overrideable != null)
                match.overrideable = overrideable;
            if (customGetter != null)
                match.customGetter = customGetter;
            if (customSetter != null)
                match.customSetter = customSetter;
            if (overridedDefaultValue != null)
                match.overridedDefaultValue = overridedDefaultValue;
        }

        static bool EvaluateBoolWithOverride(FrameSettingsField field, FrameSettings defaultFrameSettings, SerializedFrameSettings serializedFrameSettings, bool negative)
            => (serializedFrameSettings.GetOverrides(field) ? serializedFrameSettings.IsEnabled(field) ?? false : defaultFrameSettings.IsEnabled(field)) ^ negative;

        /// <summary>Add an overrideable field to be draw when Draw(bool) will be called.</summary>
        /// <param name="serializedFrameSettings">The overrideable property to draw in inspector</param>
        /// <param name="field">The field drawn</param>
        /// <param name="overrideable">The enabler will be used to check if this field could be overrided. If null or have a return value at true, it will be overrided.</param>
        /// <param name="overridedDefaultValue">The value to display when the property is not overrided. If null, use the actual value of it.</param>
        /// <param name="indent">Add this value number of indent when drawing this field.</param>
        public void Add(FrameSettingsField field, Func<bool> overrideable = null, Func<object> customGetter = null, Action<object> customSetter = null, object overridedDefaultValue = null)
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
            using (new EditorGUILayout.HorizontalScope())
            {
                var overrideRect = GUILayoutUtility.GetRect(15f, 17f, GUILayout.ExpandWidth(false)); //15 = kIndentPerLevel
                if (withOverride)
                {
                    bool originalValue = serializedFrameSettings.GetOverrides(field.field);
                    overrideRect.yMin += 4f;
                    EditorGUI.showMixedValue = serializedFrameSettings.HaveMultipleOverride(field.field);
                    bool modifiedValue = GUI.Toggle(overrideRect, originalValue, overrideTooltip, CoreEditorStyles.smallTickbox);
                    EditorGUI.showMixedValue = false;

                    if (originalValue ^ modifiedValue)
                        serializedFrameSettings.SetOverrides(field.field, modifiedValue);

                    shouldBeDisabled = !modifiedValue;
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
                                        DrawFieldShape(field.label, defaultFrameSettings.IsEnabled(field.field));
                                        break;
                                    case FrameSettingsFieldAttribute.DisplayType.BoolAsEnumPopup:
                                        //shame but it is not possible to use Convert.ChangeType to convert int into enum in current C#
                                        //rely on string parsing for the moment
                                        var oldEnumValue = Enum.Parse(attributes[field.field].targetType, defaultFrameSettings.IsEnabled(field.field) ? "1" : "0");
                                        DrawFieldShape(field.label, oldEnumValue);
                                        break;
                                    case FrameSettingsFieldAttribute.DisplayType.Others:
                                        var oldValue = field.customGetter();
                                        DrawFieldShape(field.label, oldValue);
                                        break;
                                    default:
                                        throw new ArgumentException("Unknown FrameSettingsFieldAttribute");
                                }
                            }
                            else
                                DrawFieldShape(field.label, field.overridedDefaultValue);
                        }
                        else
                        {
                            switch (attributes[field.field].type)
                            {
                                case FrameSettingsFieldAttribute.DisplayType.BoolAsCheckbox:
                                    bool oldBool = serializedFrameSettings.IsEnabled(field.field) ?? false;
                                    bool newBool = (bool)DrawFieldShape(field.label, oldBool);
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
                                        newEnumValue = Convert.ChangeType(DrawFieldShape(field.label, oldEnumValue), attributes[field.field].targetType);
                                        oldEnumIntValue = ((IConvertible)oldEnumValue).ToInt32(System.Globalization.CultureInfo.CurrentCulture);
                                        newEnumIntValue = ((IConvertible)newEnumValue).ToInt32(System.Globalization.CultureInfo.CurrentCulture);
                                    }
                                    else //in multi edition, do not assume any previous value
                                    {
                                        newEnumIntValue = EditorGUILayout.Popup(field.label, -1, Enum.GetNames(attributes[field.field].targetType));
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
                                    var newValue = DrawFieldShape(field.label, oldValue);
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

        object DrawFieldShape(GUIContent label, object field)
        {
            if (field is GUIContent)
            {
                EditorGUILayout.LabelField(label, (GUIContent)field);
                return null;
            }
            else if (field is string)
                return EditorGUILayout.TextField(label, (string)field);
            else if (field is bool)
                return EditorGUILayout.Toggle(label, (bool)field);
            else if (field is int)
                return EditorGUILayout.IntField(label, (int)field);
            else if (field is float)
                return EditorGUILayout.FloatField(label, (float)field);
            else if (field is Color)
                return EditorGUILayout.ColorField(label, (Color)field);
            else if (field is Enum)
                return EditorGUILayout.EnumPopup(label, (Enum)field);
            else if (field is LayerMask)
                return EditorGUILayout.MaskField(label, (LayerMask)field, GraphicsSettings.renderPipelineAsset.renderingLayerMaskNames);
            else if (field is UnityEngine.Object)
                return EditorGUILayout.ObjectField(label, (UnityEngine.Object)field, field.GetType(), true);
            else if (field is SerializedProperty)
                return EditorGUILayout.PropertyField((SerializedProperty)field, label, includeChildren: true);
            else
            {
                EditorGUILayout.LabelField(label, new GUIContent("Unsupported type"));
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
}
