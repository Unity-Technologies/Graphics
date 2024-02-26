using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedFrameSettings>;

    // Mirrors MaterialQuality enum and adds `FromQualitySettings`
    enum MaterialQualityMode
    {
        Low,
        Medium,
        High,
        FromQualitySettings,
    }

    static class MaterialQualityModeExtensions
    {
        public static MaterialQuality Into(this MaterialQualityMode quality)
        {
            switch (quality)
            {
                case MaterialQualityMode.High: return MaterialQuality.High;
                case MaterialQualityMode.Medium: return MaterialQuality.Medium;
                case MaterialQualityMode.Low: return MaterialQuality.Low;
                case MaterialQualityMode.FromQualitySettings: return (MaterialQuality)0;
                default: throw new ArgumentOutOfRangeException(nameof(quality));
            }
        }

        public static MaterialQualityMode Into(this MaterialQuality quality)
        {
            if (quality == (MaterialQuality)0)
                return MaterialQualityMode.FromQualitySettings;
            switch (quality)
            {
                case MaterialQuality.High: return MaterialQualityMode.High;
                case MaterialQuality.Medium: return MaterialQualityMode.Medium;
                case MaterialQuality.Low: return MaterialQualityMode.Low;
                default: throw new ArgumentOutOfRangeException(nameof(quality));
            }
        }
    }

    interface IDefaultFrameSettingsType
    {
        FrameSettingsRenderType GetFrameSettingsType();
    }

    partial class FrameSettingsUI
    {
        enum Expandable
        {
            RenderingPasses = 1 << 0,
            RenderingSettings = 1 << 1,
            LightingSettings = 1 << 2,
            AsynComputeSettings = 1 << 3,
            LightLoop = 1 << 4,
        }

        readonly static ExpandedState<Expandable, FrameSettings> k_ExpandedState = new ExpandedState<Expandable, FrameSettings>(~(-1), "HDRP");

        static Rect lastBoxRect;
        internal static CED.IDrawer Inspector(bool withOverride = true) => CED.Group(
            CED.Group((serialized, owner) =>
            {
                lastBoxRect = EditorGUILayout.BeginVertical("box");

                // Add dedicated scope here and on each FrameSettings field to have the contextual menu on everything
                Rect rect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
                using (new FrameSettingsAreaImGUI.TitleDrawingScope(rect, FrameSettingsUI.frameSettingsHeaderContent, serialized))
                {
                    EditorGUI.LabelField(rect, FrameSettingsUI.frameSettingsHeaderContent, EditorStyles.boldLabel);
                }
            }),
            InspectorInnerbox(withOverride),
            CED.Group((serialized, owner) =>
            {
                EditorGUILayout.EndVertical();
                using (new FrameSettingsAreaImGUI.TitleDrawingScope(lastBoxRect, FrameSettingsUI.frameSettingsHeaderContent, serialized))
                {
                    //Nothing to draw.
                    //We just want to have a big blue bar at left that match the whole framesetting box.
                    //This is because framesettings will be considered as one big block from prefab point
                    //of view as there is no way to separate it bit per bit in serialization and Prefab
                    //override API rely on SerializedProperty.
                }
            })
        );

        //separated to add enum popup on default frame settings
        internal static CED.IDrawer InspectorInnerbox(bool withOverride = true, bool isBoxed = true) => CED.Group(
            CED.FoldoutGroup(renderingSettingsHeaderContent, Expandable.RenderingPasses, k_ExpandedState, isBoxed ? FoldoutOption.Indent | FoldoutOption.Boxed : FoldoutOption.Indent,
                CED.Group(206, (serialized, owner) => Drawer_Section(0, serialized.data, serialized.mask, owner, withOverride))
                ),
            CED.FoldoutGroup(lightSettingsHeaderContent, Expandable.LightingSettings, k_ExpandedState, isBoxed ? FoldoutOption.Indent | FoldoutOption.Boxed : FoldoutOption.Indent,
                CED.Group(206, (serialized, owner) => Drawer_Section(1, serialized.data, serialized.mask, owner, withOverride))
                ),
            CED.FoldoutGroup(asyncComputeSettingsHeaderContent, Expandable.AsynComputeSettings, k_ExpandedState, isBoxed ? FoldoutOption.Indent | FoldoutOption.Boxed : FoldoutOption.Indent,
                CED.Group(206, (serialized, owner) => Drawer_Section(2, serialized.data, serialized.mask, owner, withOverride))
                ),
            CED.FoldoutGroup(lightLoopSettingsHeaderContent, Expandable.LightLoop, k_ExpandedState, isBoxed ? FoldoutOption.Indent | FoldoutOption.Boxed : FoldoutOption.Indent,
                CED.Group(206, (serialized, owner) => Drawer_Section(3, serialized.data, serialized.mask, owner, withOverride))
                )
			);

        static HDRenderPipelineAsset GetHDRPAssetFor(Editor owner)
        {
            HDRenderPipelineAsset hdrpAsset;
            if (owner is HDRenderPipelineEditor)
            {
                // When drawing the inspector of a selected HDRPAsset in Project windows, access HDRP by owner drawing itself
                hdrpAsset = (owner as HDRenderPipelineEditor).target as HDRenderPipelineAsset;
            }
            else
            {
                // Else rely on GraphicsSettings are you should be in hdrp and owner could be probe or camera.
                hdrpAsset = HDRenderPipeline.currentAsset;
            }
            return hdrpAsset;
        }

        static FrameSettingsRenderType? GetDefaultFrameSettingsFor(Editor owner)
        {
            if (owner is IHDProbeEditor)
                return (owner as IDefaultFrameSettingsType).GetFrameSettingsType();
            else if (owner is HDCameraEditor)
                return FrameSettingsRenderType.Camera;
            return null;
        }

        private static void Drawer_Section(int index, SerializedFrameSettings.Data data, SerializedFrameSettings.Mask mask, Editor owner, bool withOverride)
        {
            FrameSettingsAreaImGUI.DrawWithOverride(
                FrameSettingsExtractedDatas.CreateBoundInstance(data, GetDefaultFrameSettingsFor(owner), GetHDRPAssetFor(owner)),
                index,
                mask);
        }
    }

    
    // Drawing methods with ImGUI
    static class FrameSettingsAreaImGUI
    {
        internal const int k_IndentPerLevel = 15;
        const int k_CheckBoxWidth = 15;
        const int k_CheckboxLabelSeparator = 5;
        const int k_LabelFieldSeparator = 2;
        const int k_OverridesHeadersHeight = 17;
        const int k_OverridesHeadersPadding = 3;
        const int k_OverridesHeadersAllWidth = 17;
        const int k_OverridesHeadersNoneWidth = 50;

        static readonly GUIContent overrideTooltip = EditorGUIUtility.TrTextContent("", "Override this setting in component.");

        public static void DrawWithOverride(FrameSettingsExtractedDatas.DataLinked boundInstance, int groupIndex, SerializedFrameSettings.Mask serializedMask)
        {
            EditorGUI.BeginChangeCheck();
            var oldState = GUI.enabled;
            if (GUI.enabled)
            {
                Rect rect = GUILayoutUtility.GetRect(0f, k_OverridesHeadersHeight, GUILayout.ExpandWidth(false));
                OverridesHeaders(rect, boundInstance, serializedMask, groupIndex);
            }
            foreach(var field in boundInstance.GetDescriptorForGroup(groupIndex))
            {
                var line = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
                var borderedLine = line;
                borderedLine.x -= 1;
                borderedLine.width += 2;
                var remainingsOnLine = DrawOverridePart(line, field, serializedMask);

                var currentIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = field.indentLevel;

                GUIContent label = EditorGUIUtility.TrTextContent(field.displayedName, field.tooltip);
                using (new TitleDrawingScope(remainingsOnLine, label, field.boundData.boundData, serializedMask))
                    remainingsOnLine = DrawLabel(remainingsOnLine, label);
                DrawFieldPart(remainingsOnLine, field, true);

                EditorGUI.indentLevel = currentIndent;
                GUI.enabled = oldState;
            }
            if (EditorGUI.EndChangeCheck())
            {
                boundInstance.boundData.ApplyModifiedProperties();
                serializedMask.ApplyModifiedProperties();
            }
        }

        public static void DrawWithOverride(Rect rect, FrameSettingsExtractedDatas.DataLinked boundInstance, int groupIndex, SerializedFrameSettings.Mask serializedMask)
        {
            EditorGUI.BeginChangeCheck();
            var oldState = GUI.enabled;
            if (GUI.enabled)
            {
                Rect overridesHeadersRect = new Rect(rect.x, rect.y, rect.width, k_OverridesHeadersHeight);
                OverridesHeaders(overridesHeadersRect, boundInstance, serializedMask, groupIndex);
                rect.y += k_OverridesHeadersHeight;
                rect.height -= k_OverridesHeadersHeight;
            }
            var linePosition = GetLines(rect, groupIndex).GetEnumerator();
            foreach(var field in boundInstance.GetDescriptorForGroup(groupIndex))
            {
                linePosition.MoveNext();
                var remainingsOnLine = DrawOverridePart(linePosition.Current, field, serializedMask);
                
                var currentIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = field.indentLevel;

                GUIContent label = EditorGUIUtility.TrTextContent(field.displayedName, field.tooltip);
                using (new TitleDrawingScope(remainingsOnLine, label, field.boundData.boundData, serializedMask))
                    remainingsOnLine = DrawLabel(remainingsOnLine, label);
                DrawFieldPart(remainingsOnLine, field, true);

                EditorGUI.indentLevel = currentIndent;
                GUI.enabled = oldState;
            }
            if (EditorGUI.EndChangeCheck())
            {
                boundInstance.boundData.ApplyModifiedProperties();
                serializedMask.ApplyModifiedProperties();
            }
        }

        static public void DrawWithoutOverride(FrameSettingsExtractedDatas.DataLinked boundInstance, int groupIndex)
        {
            EditorGUI.BeginChangeCheck();
            var oldState = GUI.enabled;
            foreach(var field in boundInstance.GetDescriptorForGroup(groupIndex))
            {
                var remainingsOnLine = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);

                var currentIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = field.indentLevel;

                GUIContent label = EditorGUIUtility.TrTextContent(field.displayedName, field.tooltip);
                using (new TitleDrawingScope(remainingsOnLine, label, field.boundData.boundData))
                    remainingsOnLine = DrawLabel(remainingsOnLine, label);
                DrawFieldPart(remainingsOnLine, field, false);

                EditorGUI.indentLevel = currentIndent;
                GUI.enabled = oldState;
            }
            if (EditorGUI.EndChangeCheck())
                boundInstance.boundData.ApplyModifiedProperties();
        }

        static public void DrawWithoutOverride(Rect rect, FrameSettingsExtractedDatas.DataLinked boundInstance, int groupIndex)
        {
            EditorGUI.BeginChangeCheck();
            var oldState = GUI.enabled;
            var linePosition = GetLines(rect, groupIndex).GetEnumerator();
            foreach (var field in boundInstance.GetDescriptorForGroup(groupIndex))
            {
                linePosition.MoveNext();
                var remainingsOnLine = linePosition.Current;

                var currentIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = field.indentLevel;

                GUIContent label = EditorGUIUtility.TrTextContent(field.displayedName, field.tooltip);
                using (new TitleDrawingScope(remainingsOnLine, label, field.boundData.boundData))
                    remainingsOnLine = DrawLabel(remainingsOnLine, label);
                DrawFieldPart(remainingsOnLine, field, false);

                EditorGUI.indentLevel = currentIndent;
                GUI.enabled = oldState;
            }
            if (EditorGUI.EndChangeCheck())
                boundInstance.boundData.ApplyModifiedProperties();
        }

        static public float CalcHeightWithoutOverride(int groupIndex)
        {
            var lineAmount = FrameSettingsExtractedDatas.GetGroupLength(groupIndex);
            return lineAmount * EditorGUIUtility.singleLineHeight + (lineAmount - 1) * EditorGUIUtility.standardVerticalSpacing;
        }
        
        static public float CalcHeightWithOverride(int groupIndex)
        {
            return k_OverridesHeadersHeight + CalcHeightWithoutOverride(groupIndex);
        }

        static public IEnumerable<Rect> GetLines(Rect encompassingRect, int groupIndex)
        {
            Rect line = encompassingRect;
            line.height = EditorGUIUtility.singleLineHeight;

            for(int i = FrameSettingsExtractedDatas.GetGroupLength(groupIndex); i > 0; --i)
            {
                yield return line;
                line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
        }
        
        static Rect DrawLabel(Rect rect, GUIContent label)
        {
            Rect labelRect = rect;
            labelRect.width = EditorGUIUtility.labelWidth - k_IndentPerLevel * EditorGUI.indentLevel;
            labelRect.x += EditorGUI.indentLevel * k_IndentPerLevel;

            Rect fieldRect = rect;
            fieldRect.x = labelRect.xMax + k_LabelFieldSeparator;
            fieldRect.width -= fieldRect.x - rect.x;
            
            EditorGUI.HandlePrefixLabel(rect, labelRect, label);
            return fieldRect;
        }

        static Rect DrawOverridePart(Rect rect, FrameSettingsExtractedDatas.DataLinked.FieldDescriptor descriptor, SerializedFrameSettings.Mask mask)
        {
            int currentIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            Rect overrideRect = rect;
            overrideRect.width = k_CheckBoxWidth;

            Rect fieldRect = rect;
            fieldRect.x += k_CheckBoxWidth + k_CheckboxLabelSeparator;
            fieldRect.width -= fieldRect.x - rect.x;

            var overrideInterface = descriptor.GetOverrideInterface(mask);
            var val = overrideInterface.overrided;
            bool originalValue = val ?? false;
            overrideRect.yMin += 4f;

            var overrideable = overrideInterface.IsOverrideableWithDependencies();
            GUI.enabled &= overrideable;

            if (overrideable)
            {
                bool modifiedValue = EditorGUI.Toggle(overrideRect, overrideTooltip, originalValue, val.HasValue ? CoreEditorStyles.smallTickbox : CoreEditorStyles.smallMixedTickbox);
                if (originalValue ^ modifiedValue)
                    overrideInterface.overrided = modifiedValue;

                GUI.enabled &= modifiedValue;
            }
            EditorGUI.indentLevel = currentIndent;
            return fieldRect;
        }
        
        static void DrawFieldPart(Rect fieldRect, FrameSettingsExtractedDatas.DataLinked.FieldDescriptor descriptor, bool haveOverride)
        {
            int currentIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            if (haveOverride && !GUI.enabled)
            {
                //When disabled, show default values instead
                DrawFieldPartWithDefaultValues(fieldRect, descriptor);
            }
            else
            {
                //When enabled, show serialized ones for this instance
                DrawFieldPartWithCurrentValues(fieldRect, descriptor);
            }
            EditorGUI.indentLevel = currentIndent;
        }
        
        static void DrawFieldPartWithDefaultValues(Rect fieldRect, FrameSettingsExtractedDatas.DataLinked.FieldDescriptor descriptor)
        {
            if (descriptor.overridedDefaultValue == null)
            {
                switch (descriptor.displayType)
                {
                    case FrameSettingsFieldAttribute.DisplayType.BoolAsCheckbox:
                        DrawFieldShape(fieldRect, descriptor.enabledInDefault ?? false);
                        break;
                    case FrameSettingsFieldAttribute.DisplayType.BoolAsEnumPopup:
                        //shame but it is not possible to use Convert.ChangeType to convert int into enum in current C#
                        //rely on string parsing for the moment
                        var oldEnumValue = Enum.Parse(descriptor.targetType, (descriptor.enabledInDefault ?? false) ? "1" : "0");
                        DrawFieldShape(fieldRect, oldEnumValue);
                        break;
                    case FrameSettingsFieldAttribute.DisplayType.Others:
                        if (descriptor.overridedGetter == null)
                            throw new Exception($"{nameof(FrameSettingsFieldAttribute.DisplayType.Others)} can only be used with an overridedGetter in {nameof(FrameSettingsExtractedDatas.DataLinked)}.AddDynamicOverrides()");
                        var oldValue = descriptor.overridedGetter();
                        DrawFieldShape(fieldRect, oldValue);
                        break;
                    default:
                        throw new ArgumentException("Unknown FrameSettingsFieldAttribute");
                }
            }
            else
                DrawFieldShape(fieldRect, descriptor.overridedDefaultValue());
        }
        
        static void DrawFieldPartWithCurrentValues(Rect fieldRect, FrameSettingsExtractedDatas.DataLinked.FieldDescriptor descriptor)
        {
            EditorGUI.showMixedValue = descriptor.hasMultipleDifferentValues;
            switch (descriptor.displayType)
            {
                case FrameSettingsFieldAttribute.DisplayType.BoolAsCheckbox:
                    bool oldBool = descriptor.enabled ?? false;
                    bool newBool = (bool)DrawFieldShape(fieldRect, oldBool);
                    if (oldBool ^ newBool)
                    {
                        Undo.RecordObjects(descriptor.targetObjects, $"Changed FrameSettings {descriptor.displayedName}");
                        descriptor.enabled = newBool;
                        descriptor.callbackOnChange?.Invoke(oldBool, newBool);
                    }
                    break;
                case FrameSettingsFieldAttribute.DisplayType.BoolAsEnumPopup:
                    //shame but it is not possible to use Convert.ChangeType to convert int into enum in current C#
                    //Also, Enum.Equals and Enum operator!= always send true here. As it seams to compare object reference instead of value.
                    var oldBoolValue = descriptor.enabled;
                    int oldEnumIntValue = -1;
                    int newEnumIntValue;
                    object newEnumValue;
                    if (oldBoolValue.HasValue)
                    {
                        var oldEnumValue = Enum.GetValues(descriptor.targetType).GetValue(oldBoolValue.Value ? 1 : 0);
                        newEnumValue = Convert.ChangeType(DrawFieldShape(fieldRect, oldEnumValue), descriptor.targetType);
                        oldEnumIntValue = ((IConvertible)oldEnumValue).ToInt32(System.Globalization.CultureInfo.CurrentCulture);
                        newEnumIntValue = ((IConvertible)newEnumValue).ToInt32(System.Globalization.CultureInfo.CurrentCulture);
                    }
                    else //in multi edition, do not assume any previous value
                    {
                        newEnumIntValue = EditorGUI.Popup(fieldRect, -1, Enum.GetNames(descriptor.targetType));
                        newEnumValue = newEnumIntValue < 0 ? null : Enum.GetValues(descriptor.targetType).GetValue(newEnumIntValue);
                    }
                    if (oldEnumIntValue != newEnumIntValue)
                    {
                        Undo.RecordObjects(descriptor.targetObjects, $"Changed FrameSettings {descriptor.displayedName}");
                        descriptor.enabled = Convert.ToInt32(newEnumValue) == 1;
                        descriptor.callbackOnChange?.Invoke(oldEnumIntValue, newEnumIntValue);
                    }
                    break;
                case FrameSettingsFieldAttribute.DisplayType.Others:
                    if (descriptor.overridedGetter == null || descriptor.overridedSetter == null)
                            throw new Exception($"{nameof(FrameSettingsFieldAttribute.DisplayType.Others)} can only be used with an overridedGetter and a overridedSetter in {nameof(FrameSettingsExtractedDatas.DataLinked)}.AddDynamicOverrides()");
                    var oldValue = descriptor.overridedGetter();
                    EditorGUI.BeginChangeCheck();
                    var newValue = DrawFieldShape(fieldRect, oldValue);
                    // We need an extensive check here, otherwise in some case with boxing or polymorphism
                    // the != operator won't be accurate. (This is the case for enum types).
                    var valuesAreEquals = oldValue == null && newValue == null || oldValue != null && oldValue.Equals(newValue);
                    // If the UI reported a change, we also assign values.
                    // When assigning to a multiple selection, the equals check may fail while there was indeed a change.
                    if (EditorGUI.EndChangeCheck() || !valuesAreEquals)
                    {
                        Undo.RecordObjects(descriptor.targetObjects, $"Changed FrameSettings {descriptor.displayedName}");
                        descriptor.overridedSetter(newValue);
                        descriptor.callbackOnChange?.Invoke(oldValue, newValue);
                    }
                    break;
                default:
                    throw new ArgumentException("Unknown FrameSettingsFieldAttribute");
            }
            EditorGUI.showMixedValue = false;
        }

        static object DrawFieldShape(Rect rect, object field)
        {
            switch (field)
            {
                case bool boolean:
                    return EditorGUI.Toggle(rect, boolean);
                case int integer:
                    return EditorGUI.IntField(rect, integer);
                case float floatValue:
                    return EditorGUI.FloatField(rect, floatValue);
                case Enum enumeration:
                    return EditorGUI.EnumPopup(rect, enumeration);
                default:
                    EditorGUI.LabelField(rect, new GUIContent("Unsupported type"));
                    Debug.LogError($"Unsupported format {field.GetType()} in OverridableSettingsArea.cs. Please add it!");
                    return null;
            }
        }

        static void OverridesHeaders(Rect rect, FrameSettingsExtractedDatas.DataLinked boundInstance, SerializedFrameSettings.Mask mask, int groupIndex)
        {
            if (GUI.Button(new Rect(rect.x + k_OverridesHeadersPadding, rect.y, k_OverridesHeadersAllWidth, rect.height), EditorGUIUtility.TrTextContent("All", "Toggle all overrides on. To maximize performances you should only toggle overrides that you actually need."), CoreEditorStyles.miniLabelButton))
            {
                boundInstance.SetAllAllowedOverridesTo(mask, groupIndex, true);
                GUI.changed = true;
            }
                
            if (GUI.Button(new Rect(rect.x + k_OverridesHeadersAllWidth + k_OverridesHeadersPadding*2, rect.y, k_OverridesHeadersNoneWidth, rect.height), EditorGUIUtility.TrTextContent("None", "Toggle all overrides off."), CoreEditorStyles.miniLabelButton))
            {
                boundInstance.SetAllAllowedOverridesTo(mask, groupIndex, false);
                GUI.changed = true;
            }
        }
        
        public struct TitleDrawingScope : IDisposable
        {
            bool m_HasOverride;

            public TitleDrawingScope(Rect rect, GUIContent label, SerializedFrameSettings serialized) 
                : this(rect, label, serialized.data, serialized.mask) { }
            public TitleDrawingScope(Rect rect, GUIContent label, SerializedFrameSettings.Data data, SerializedFrameSettings.Mask mask = null)
            {
                EditorGUI.BeginProperty(rect, label, data.root);

                m_HasOverride = mask != null;
                if (m_HasOverride)
                    EditorGUI.BeginProperty(rect, label, mask.root);
            }

            void IDisposable.Dispose()
            {
                EditorGUI.EndProperty();
                if (m_HasOverride)
                    EditorGUI.EndProperty();
            }
        }
    }


    // Drawing methods with UITK
    class FrameSettingsArea : VisualElement
    {
        internal const string k_StylesheetPathFormat = "Packages/com.unity.render-pipelines.high-definition/Editor/USS/FrameSettings{0}.uss";
        const string k_FieldClass = "frame-settings-field";

        protected Dictionary<FrameSettingsField, LineField> m_QuickAccess = new();
        Dictionary<FrameSettingsField, Queue<Action<VisualElement>>> m_LateInitQueues = new();
        protected FrameSettingsExtractedDatas.DataLinked m_BoundInstance;
        protected int m_GroupIndex;
        
        protected FrameSettingsArea(int groupIndex)
        {
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(string.Format(k_StylesheetPathFormat, "")));
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(string.Format(k_StylesheetPathFormat, EditorGUIUtility.isProSkin ? "Dark" : "Light")));
            m_GroupIndex = groupIndex;
        }

        public FrameSettingsArea(FrameSettingsExtractedDatas.DataLinked boundInstance, int groupIndex) : this(groupIndex)
        {
            if (boundInstance == null)
                throw new ArgumentNullException(nameof(boundInstance));
            if (!FrameSettingsExtractedDatas.Exists(groupIndex))
                throw new ArgumentOutOfRangeException(nameof(groupIndex));
            m_BoundInstance = boundInstance;
            GenerateLayoutForGroup();
            FinishDelayedInit();
        }

        public void GenerateLayoutForGroup()
        {
            VisualElement header = CreateHeader();
            if (header != null)
                Add(header);
            foreach (var descriptor in m_BoundInstance.GetDescriptorForGroup(m_GroupIndex))
                Add(CreateFieldLayout(descriptor), descriptor.field);
        }

        protected virtual VisualElement CreateHeader() => null;

        protected virtual LineField CreateFieldLayout(FrameSettingsExtractedDatas.DataLinked.FieldDescriptor descriptor)
            => new LineField(descriptor.field, CreateInnerFieldLayout(descriptor));

        VisualElement CreateInnerFieldLayout(FrameSettingsExtractedDatas.DataLinked.FieldDescriptor descriptor)
        {
            switch (descriptor.displayType)
            {
                case FrameSettingsFieldAttribute.DisplayType.BoolAsCheckbox:
                    return CreateField(descriptor.enabledUnchecked, descriptor, v => descriptor.enabledUnchecked = (bool)v);
                case FrameSettingsFieldAttribute.DisplayType.BoolAsEnumPopup:
                    var val = (Enum)Enum.GetValues(descriptor.targetType).GetValue(descriptor.enabledUnchecked ? 1 : 0);
                    Action<object> setter = v => descriptor.enabledUnchecked = ((IConvertible)v).ToInt32(System.Globalization.CultureInfo.InvariantCulture) == 1;
                    return CreateField(val, descriptor, setter);
                case FrameSettingsFieldAttribute.DisplayType.Others:
                    {
                        if (descriptor.overridedGetter == null || descriptor.overridedSetter == null)
                            throw new Exception($"{nameof(FrameSettingsFieldAttribute.DisplayType.Others)} can only be used with an overridedGetter and a overridedSetter in {nameof(FrameSettingsExtractedDatas.DataLinked)}.AddDynamicOverrides()");

                        return CreateField(descriptor.overridedGetter(), descriptor);
                    }
                default:
                    throw new ArgumentException($"Unknown {nameof(FrameSettingsFieldAttribute.DisplayType)}");
            }
        }

        VisualElement CreateField<T>(T value, FrameSettingsExtractedDatas.DataLinked.FieldDescriptor descriptor, Action<object> registerChange = null)
        {
            switch (value) //only kept bool, int, float and enum as nothing else is used
            {
                case bool boolean:
                    return SetUp(new Toggle() { value = boolean }, descriptor, registerChange);
                case int integer:
                    return SetUp(new IntegerField() { value = integer }, descriptor, registerChange);
                case float floatValue:
                    return SetUp(new FloatField() { value = floatValue }, descriptor, registerChange);
                case Enum enumeration:
                    return SetUp(new EnumField(enumeration), descriptor, registerChange);
                default:
                    throw new ArgumentException($"Unsupported format {typeof(T)} in OverridableFrameSettingsArea.cs. Please add it!");
            }
        }

        VisualElement SetUp<T>(BaseField<T> field, FrameSettingsExtractedDatas.DataLinked.FieldDescriptor descriptor, Action<object> registerChange = null)
        {
            //initilization styling
            field.label = descriptor.displayedName;
            field.tooltip = descriptor.tooltip;
            field.showMixedValue = descriptor.hasMultipleDifferentValues;

            //dynamic styling
            field.labelElement.style.paddingLeft = 1 + descriptor.indentLevel * FrameSettingsAreaImGUI.k_IndentPerLevel;
            field.AddToClassList("unity-base-field__aligned"); //Align with other BaseField<T>
            field.AddToClassList(k_FieldClass);
            if (descriptor.fieldDependentLabel != FrameSettingsField.None)
            {
                // workaround because IPrefixLabel is not public
                var encapsulatedDescriptor = descriptor;
                void RegisterValueChanged<K>(BaseField<K> otherField)
                {
                    otherField.RegisterValueChangedCallback(evt =>
                    {
                        field.labelElement.text = encapsulatedDescriptor.displayedName;
                    });
                }
                AddDelayedInit(descriptor.fieldDependentLabel, elt =>
                {
                    switch (elt)
                    {
                        case Toggle toggle:
                            RegisterValueChanged(toggle);
                            return;
                        case IntegerField integreField:
                            RegisterValueChanged(integreField);
                            return;
                        case FloatField floatField:
                            RegisterValueChanged(floatField);
                            return;
                        case EnumField enumField:
                            RegisterValueChanged(enumField);
                            return;
                        default:
                            throw new ArgumentException($"Unsupported format BaseField<T> in {nameof(FrameSettingsArea)}. Please add it! Found {elt.GetType()}");
                    }
                });
            }

            //callback on change
            field.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObjects(descriptor.targetObjects, $"Changed FrameSettings {descriptor.displayedName}");
                if (descriptor.overridedSetter != null)
                    descriptor.overridedSetter(evt.newValue);
                else
                    registerChange(evt.newValue);
                descriptor.callbackOnChange?.Invoke(evt.previousValue, evt.newValue);
                m_BoundInstance.boundData.ApplyModifiedProperties();
            });
            return field;
        }

        // Usefull to add dependency between bits. This is mainly used to rename labels on other bit change.
        protected void AddDelayedInit(FrameSettingsField forField, Action<VisualElement> action)
        {
            if (m_QuickAccess.ContainsKey(forField))
                action?.Invoke(m_QuickAccess[forField].field);
            else if (action != null)
            {
                if (!m_LateInitQueues.ContainsKey(forField))
                    m_LateInitQueues[forField] = new();
                m_LateInitQueues[forField].Enqueue(action);
            }
        }

        void Add(LineField element, FrameSettingsField field)
        {
            Add(m_QuickAccess[field] = element);
            if (!m_LateInitQueues.ContainsKey(field))
                return;
            while (m_LateInitQueues[field].TryDequeue(out var action))
                action(element.field);
        }

        protected void FinishDelayedInit()
        {
            foreach (var kpv in m_LateInitQueues)
            {
                if (kpv.Value == null)
                    continue;

                while (kpv.Value.TryDequeue(out var action))
                    action(m_QuickAccess[kpv.Key].field);
            }
        }

        internal class LineField : VisualElement
        {
            FrameSettingsField m_FrameSettingsField;
            VisualElement m_Field;
            List<FrameSettingsField> m_FieldsDependingOnThisOne;

            public VisualElement field => m_Field;
            public FrameSettingsField frameSettingsField => m_FrameSettingsField;

            public T GetFistChildAs<T>()
                where T : VisualElement
            {
                var e = Children().GetEnumerator();
                e.MoveNext();
                return e.Current as T;
            }

            public LineField(FrameSettingsField frameSettingsField, VisualElement field)
            {
                name = $"line-field-{frameSettingsField}";
                m_FrameSettingsField = frameSettingsField;
                Add(m_Field = field);
                style.flexDirection = FlexDirection.Row;
                m_FieldsDependingOnThisOne = new();
            }

            internal void AddDependence(FrameSettingsField dependentField)
                => m_FieldsDependingOnThisOne.Add(dependentField);

            internal void PropagateActionToDependenteLineFields(FrameSettingsArea host, Action<LineField> action)
            {
                if (action == null)
                    return;

                foreach (var field in m_FieldsDependingOnThisOne)
                    action(host.m_QuickAccess[field]);
            }
        }
    }

    class FrameSettingsAreaWithOverrides : FrameSettingsArea
    {
        const string k_OverrideHeaderClass = "frame-settings-override-header";
        const string k_OverrideCheckboxClass = "frame-settings-override-checkbox";

        SerializedFrameSettings.Mask m_Mask;

        public FrameSettingsAreaWithOverrides(FrameSettingsExtractedDatas.DataLinked boundInstance, SerializedFrameSettings.Mask serializedMask, int groupIndex)
            : base(groupIndex)
        {
            if (boundInstance == null)
                throw new ArgumentNullException(nameof(boundInstance));
            if (serializedMask == null)
                throw new ArgumentNullException(nameof(serializedMask));
            if (!FrameSettingsExtractedDatas.Exists(groupIndex))
                throw new ArgumentOutOfRangeException(nameof(groupIndex));
            m_BoundInstance = boundInstance;
            m_Mask = serializedMask;
            GenerateLayoutForGroup();
            FinishDelayedInit();
        }

        protected override VisualElement CreateHeader()
        {
            void UpdateDisplayFromSerializedValues()
            {
                foreach ( var line in m_QuickAccess.Values)
                {
                    var descriptor = m_BoundInstance.GetFieldDescriptor(line.frameSettingsField);
                    var overrideInterface = descriptor.GetOverrideInterface(m_Mask);
                    var toggle = line.GetFistChildAs<Toggle>();
                    toggle.SetValueWithoutNotify(overrideInterface.overridedUnchecked);
                    toggle.showMixedValue = overrideInterface.hasMultipleDifferentOverrides;
                    UpdateFieldAlongOverrides(toggle, line.field, descriptor);
                }
            }

            var line = new VisualElement();
            line.Add(new Button(() =>
            {
                Undo.RecordObjects(m_Mask.root.serializedObject.targetObjects, $"Changed all FrameSettings override");
                m_BoundInstance.SetAllAllowedOverridesTo(m_Mask, m_GroupIndex, true);
                m_Mask.ApplyModifiedProperties();
                UpdateDisplayFromSerializedValues();
            })
            {
                text = "All",
                tooltip = "Toggle all overrides on. To maximize performances you should only toggle overrides that you actually need.",
                name = "All",
            });
            line.Add(new Button(() =>
            {
                Undo.RecordObjects(m_Mask.root.serializedObject.targetObjects, $"Changed all FrameSettings override");
                m_BoundInstance.SetAllAllowedOverridesTo(m_Mask, m_GroupIndex, false);
                m_Mask.ApplyModifiedProperties();
                UpdateDisplayFromSerializedValues();
            })
            {
                text = "None", 
                tooltip = "Toggle all overrides off.",
                name = "None",
            });
            line.AddToClassList(k_OverrideHeaderClass);
            return line;
        }

        protected override LineField CreateFieldLayout(FrameSettingsExtractedDatas.DataLinked.FieldDescriptor descriptor)
        {
            var line = base.CreateFieldLayout(descriptor);
            var overrideCheckbox = CreateOverride(descriptor);
            line.Insert(0, overrideCheckbox);
            return line;
        }
        
        VisualElement GetSiblingFieldOnLine(Toggle t) => (t.hierarchy.parent as LineField).field;

        void UpdateFieldAlongOverrides(Toggle overrideToggle, VisualElement field, FrameSettingsExtractedDatas.DataLinked.FieldDescriptor descriptor)
        {
            void SetToDefaultValue()
            {
                var overrided = descriptor.overridedDefaultValue?.Invoke();
                switch (field)
                {
                    case Toggle toggleField:
                        toggleField.SetValueWithoutNotify((bool)(overrided ?? descriptor.enabledInDefault.Value));
                        return;
                    case IntegerField integerField:
                        integerField.SetValueWithoutNotify((int)(overrided ?? descriptor.overridedGetter()));
                        return;
                    case FloatField floatField:
                        floatField.SetValueWithoutNotify((float)(overrided ?? descriptor.overridedGetter()));
                        return;
                    case EnumField enumField:
                        object oldEnumValue;
                        switch (descriptor.displayType)
                        {
                            case FrameSettingsFieldAttribute.DisplayType.BoolAsEnumPopup:
                                oldEnumValue = overrided ?? Enum.Parse(descriptor.targetType, (descriptor.enabledInDefault ?? false) ? "1" : "0");
                                break;
                            default:
                                oldEnumValue = overrided ?? descriptor.overridedGetter();
                                break;
                        }
                        enumField.SetValueWithoutNotify(oldEnumValue as Enum);
                        return;
                    default:
                        throw new ArgumentException($"Unsupported format BaseField<T> in {nameof(FrameSettingsArea)}. Please add it! Found {field.GetType()}");
                }
            }

            void SetToSerializedValueBack()
            {
                switch (field)
                {
                    case Toggle toggleField:
                        toggleField.SetValueWithoutNotify(descriptor.enabledUnchecked);
                        toggleField.showMixedValue = descriptor.hasMultipleDifferentValues;
                        break;
                    case IntegerField integerField:
                        integerField.SetValueWithoutNotify((int)descriptor.overridedGetter());
                        integerField.showMixedValue = descriptor.hasMultipleDifferentValues;
                        break;
                    case FloatField floatField:
                        floatField.SetValueWithoutNotify((float)descriptor.overridedGetter());
                        floatField.showMixedValue = descriptor.hasMultipleDifferentValues;
                        break;
                    case EnumField enumField:
                        Enum value;
                        switch (descriptor.displayType)
                        {
                            case FrameSettingsFieldAttribute.DisplayType.BoolAsEnumPopup:
                                value = (Enum)Enum.GetValues(descriptor.targetType).GetValue(descriptor.enabledUnchecked ? 1 : 0);
                                break;
                            default:
                                value = (Enum)descriptor.overridedGetter();
                                break;
                        }
                        enumField.SetValueWithoutNotify(value);
                        enumField.showMixedValue = descriptor.hasMultipleDifferentValues;
                        break;
                    default:
                        throw new ArgumentException($"Unsupported format BaseField<T> in {nameof(FrameSettingsArea)}. Please add it! Found {field.GetType()}");
                }
            }

            var overrideInterface = descriptor.GetOverrideInterface(m_Mask);
            bool visibleToggle = overrideInterface.IsOverrideableWithDependencies();
            overrideToggle.style.visibility = visibleToggle ? Visibility.Visible : Visibility.Hidden;

            bool locallyOverrided = overrideInterface.overridedUnchecked;
            if (visibleToggle && locallyOverrided)
            {
                SetToSerializedValueBack();
                field.SetEnabled(true);
            }
            else
            {
                field.SetEnabled(false);
                SetToDefaultValue();
            }
        }

        Toggle CreateOverride(FrameSettingsExtractedDatas.DataLinked.FieldDescriptor descriptor)
        {
            //Initialize override toggle
            var o = descriptor.GetOverrideInterface(m_Mask);
            var toggle = new Toggle()
            {
                value = o.overridedUnchecked,
                showMixedValue = o.hasMultipleDifferentOverrides,
            };
            toggle.AddToClassList(k_OverrideCheckboxClass);
                        
            //Handle direct impact of override toggle on value to display
            //Line is not fully constructed yet. So we must delay
            AddDelayedInit(descriptor.field, elt =>
            {
                //Line not fully constructed yet -> Delaying
                UpdateFieldAlongOverrides(toggle, GetSiblingFieldOnLine(toggle), descriptor);

                toggle.RegisterValueChangedCallback(evt =>
                {
                    //Serialization of override mask
                    Undo.RecordObjects(descriptor.targetObjects, $"Changed FrameSettings {descriptor.displayedName} override");
                    o.overridedUnchecked = evt.newValue;
                    m_Mask.ApplyModifiedProperties();

                    //Display of field
                    LineField line = toggle.hierarchy.parent as LineField;
                    UpdateFieldAlongOverrides(toggle, line.field, descriptor);
                    line.PropagateActionToDependenteLineFields(this, l 
                        => UpdateFieldAlongOverrides(l.GetFistChildAs<Toggle>(), l.field, descriptor.boundData.GetFieldDescriptor(l.frameSettingsField)));
                });
            });

            //Handle dependent fields impact on value to display
            foreach (var field in descriptor.dependencies)
            {
                AddDelayedInit(field, elt =>
                {
                    m_QuickAccess[field].AddDependence(descriptor.field);

                    //As there is no common interface allowing to RegisterValueChangedCallback
                    //without knowing the type first, only handling supported types.
                    switch (elt)
                    {
                        case Toggle toggle:
                            RegisterValueChanged(toggle);
                            return;
                        case IntegerField integreField:
                            RegisterValueChanged(integreField);
                            return;
                        case FloatField floatField:
                            RegisterValueChanged(floatField);
                            return;
                        case EnumField enumField:
                            RegisterValueChanged(enumField);
                            return;
                        default:
                            throw new ArgumentException($"Unsupported format BaseField<T> in {nameof(FrameSettingsArea)}. Please add it! Found {elt.GetType()}");
                    }
                });
            }
            void RegisterValueChanged<K>(BaseField<K> otherField)
            {
                otherField.RegisterValueChangedCallback(evt 
                    => UpdateFieldAlongOverrides(toggle, GetSiblingFieldOnLine(toggle), descriptor));
            }

            return toggle;
        }
    }
}
