using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomPropertyDrawer(typeof(FrameSettings))]
    class FrameSettingsPropertyDrawer : PropertyDrawer
    {
        static class Styles
        {
            public const int labelWidth = 220;

            public static readonly GUIContent[] headerContents = new GUIContent[]
            {
                FrameSettingsUI.renderingSettingsHeaderContent,
                FrameSettingsUI.lightSettingsHeaderContent,
                FrameSettingsUI.asyncComputeSettingsHeaderContent,
                FrameSettingsUI.lightLoopSettingsHeaderContent
            };
        }

        internal static string GetFrameSettingsFieldName(string name, int group) => $"UI_State_{nameof(FrameSettingsPropertyDrawer)}_{name}_{group}";

        internal static bool IsExpended(string name, int group)
            => EditorPrefs.GetBool(GetFrameSettingsFieldName(name, group));

        internal static void SetExpended(string name, int group, bool value)
            => EditorPrefs.SetBool(GetFrameSettingsFieldName(name, group), value);

        float m_TotalHeight = 0f;


        bool TryExtractUsageInfosFromAttribute(SerializedProperty dataProperty, out SerializedProperty maskProperty, out FrameSettingsRenderType defaultValues,
            out HDRenderPipelineAsset hdrpAssetUsedForOverrideChecks)
        {
            maskProperty = null;
            defaultValues = default;
            hdrpAssetUsedForOverrideChecks = null;

            Type hostType;
            var serialisedObjectType = dataProperty.serializedObject.targetObject.GetType();
            var baseLength = dataProperty.propertyPath.LastIndexOf('.');
            string basePath = null;
            string fieldName;
            if (baseLength == -1)
            {
                fieldName = dataProperty.propertyPath;
                hostType = serialisedObjectType;
            }
            else
            {
                basePath = dataProperty.propertyPath.Substring(0, baseLength);
                fieldName = dataProperty.propertyPath.Substring(baseLength + 1);
                hostType = dataProperty.serializedObject.FindProperty(basePath).boxedValue.GetType();
            }

            var attributes = hostType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .GetCustomAttributes(typeof(UseOverrideMaskAttribute), true);
            if (attributes.Length == 0)
                return false;

            var customPath = ((UseOverrideMaskAttribute)attributes[0]).pathToOverrideMask;
            maskProperty = customPath == null ? null : dataProperty.serializedObject.FindProperty($"{(basePath == null ? "" : $"{basePath}.")}{customPath}");
            if (maskProperty == null)
            {
                Debug.LogError($"Wrong serialization path set: [{nameof(UseOverrideMaskAttribute)}({customPath})]. Ignoring mask");
                return false;
            }

            defaultValues = ((UseOverrideMaskAttribute)attributes[0]).defaultValuesToUse;
            if (serialisedObjectType == typeof(HDRenderPipelineAsset))
            {
                // When drawing the inspector of a selected HDRPAsset in Project windows, use itself for checks
                hdrpAssetUsedForOverrideChecks = dataProperty.serializedObject.targetObject as HDRenderPipelineAsset;
            }
            else
            {
                // Else rely on GraphicsSettings are you should be in hdrp
                hdrpAssetUsedForOverrideChecks = HDRenderPipeline.currentAsset;
            }

            return true;
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var data = new SerializedFrameSettings.Data(property);
            FrameSettingsExtractedDatas.DataLinked boundInstance;
            SerializedFrameSettings.Mask mask = null;
            if (TryExtractUsageInfosFromAttribute(property, out var maskProperty, out var defaultValues, out var hdrpAssetUsedForOverrideChecks))
            {
                boundInstance = FrameSettingsExtractedDatas.CreateBoundInstance(data, defaultValues, hdrpAssetUsedForOverrideChecks);
                mask = new SerializedFrameSettings.Mask(maskProperty, data);
            }
            else
            {
                boundInstance = FrameSettingsExtractedDatas.CreateBoundInstance(data, null, null);
            }

            var root = new VisualElement() { name = "frame-settings" };

            var labelHeader = new Label(property.displayName) { tooltip = property.tooltip };
            labelHeader.AddToClassList("frame-settings-header");
            root.Add(labelHeader);

            for (int i = 0; i < Styles.headerContents.Length; ++i)
            {
                var header = new HeaderFoldout()
                {
                    text = Styles.headerContents[i].text,
                    tooltip = Styles.headerContents[i].tooltip,
                    name = GetFrameSettingsFieldName(property.displayName, i),
                };
                header.AddToClassList("frame-settings-section-header");
                header.value = IsExpended(property.displayName, i);
                var encapsulatedIndex = i;
                header.RegisterValueChangedCallback(evt => SetExpended(property.displayName, encapsulatedIndex, evt.newValue));
                header.contentContainer.Add(mask == null
                    ? new FrameSettingsArea(boundInstance, i)
                    : new FrameSettingsAreaWithOverrides(boundInstance, mask, i));
                root.Add(header);
            }

            root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(string.Format(FrameSettingsArea.k_StylesheetPathFormat, "")));

            return root;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var data = new SerializedFrameSettings.Data(property);
            FrameSettingsExtractedDatas.DataLinked boundInstance;
            SerializedFrameSettings.Mask mask = null;
            if (TryExtractUsageInfosFromAttribute(property, out var maskProperty, out var defaultValues, out var hdrpAssetUsedForOverrideChecks))
            {
                boundInstance = FrameSettingsExtractedDatas.CreateBoundInstance(data, defaultValues, hdrpAssetUsedForOverrideChecks);
                mask = new SerializedFrameSettings.Mask(maskProperty, data);
            }
            else
            {
                boundInstance = FrameSettingsExtractedDatas.CreateBoundInstance(data, null, null);
            }

            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;
            EditorGUI.BeginProperty(position, label, property);

            float y = position.y;
            var subHeaderHeight = CoreEditorStyles.subSectionHeaderStyle.CalcHeight(label, position.width);

            EditorGUI.LabelField(new Rect(position.x, y, position.width, subHeaderHeight), label, CoreEditorStyles.subSectionHeaderStyle);
            y += subHeaderHeight + EditorGUIUtility.standardVerticalSpacing;

            for (int i = 0; i < Styles.headerContents.Length; ++i)
            {
                CoreEditorUtils.DrawSplitter(new Rect(position.x, y, position.width, 1f));
                y += 1;

                var oldExpendedState = IsExpended(property.displayName, i);
                var newExpendedState = CoreEditorUtils.DrawHeaderFoldout(new Rect(position.x, y, position.width, subHeaderHeight), Styles.headerContents[i], oldExpendedState);
                y += subHeaderHeight;

                if (newExpendedState ^ oldExpendedState)
                    SetExpended(property.displayName, i, newExpendedState);

                if (newExpendedState)
                {
                    y += EditorGUIUtility.standardVerticalSpacing;

                    if (mask == null)
                        FrameSettingsAreaImGUI.DrawWithoutOverride(new Rect(position.x, y, position.width, 0), boundInstance, i);
                    else
                        FrameSettingsAreaImGUI.DrawWithOverride(new Rect(position.x, y, position.width, 0), boundInstance, i, mask);
                    float areaHeight = FrameSettingsAreaImGUI.CalcHeightWithoutOverride(i);
                    y += areaHeight + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            CoreEditorUtils.DrawSplitter(new Rect(position.x, y, position.width, 1f));
            y += 1;

            m_TotalHeight = y - position.y;

            EditorGUI.EndProperty();

            EditorGUIUtility.labelWidth = oldWidth;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return m_TotalHeight;
        }
    }


    [CustomPropertyDrawer(typeof(FrameSettingsOverrideMask))]
    class FrameSettingsOverrideMaskPropertyDrawer : PropertyDrawer
    {
        static readonly GUIContent k_Label = EditorGUIUtility.TrTextContent("Unavailable",
            $"{nameof(FrameSettingsOverrideMask)} should not be displayed directly. Use [{nameof(UseOverrideMaskAttribute)}(pathToMask)] instead.");

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var label2 = new Label($"{property.displayName} {k_Label.text}") { tooltip = k_Label.tooltip };
            label2.SetEnabled(false);
            return label2;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool guiState = GUI.enabled;
            GUI.enabled = false;
            EditorGUI.LabelField(position, label, k_Label);
            GUI.enabled = guiState;
        }
    }
}
