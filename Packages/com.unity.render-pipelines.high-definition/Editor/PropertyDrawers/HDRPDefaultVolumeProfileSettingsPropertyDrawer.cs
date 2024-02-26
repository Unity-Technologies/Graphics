using System;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomPropertyDrawer(typeof(HDRPDefaultVolumeProfileSettings))]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    class HDRPDefaultVolumeProfileSettingsPropertyDrawer : DefaultVolumeProfileSettingsPropertyDrawer
    {
        GUIContent defaultVolumeProfileAssetLabel => EditorGUIUtility.TrTextContent("Default Profile",
            "Settings that will be applied project-wide to all Volumes by default when HDRP is active.");

        protected override GUIContent volumeInfoBoxLabel => EditorGUIUtility.TrTextContent(
            "The values in the Default Volume can be overridden by a Volume Profile assigned to HDRP asset and Volumes inside scenes.");

        protected override VisualElement CreateHeader()
        {
            var label = new Label("Default");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            return label;
        }

        protected override VisualElement CreateAssetFieldUI()
        {
            Action redraw = null;
            var container = new IMGUIContainer(() => redraw());
            redraw = () =>
            {
                using var indentLevelScope = new EditorGUI.IndentLevelScope();
                using var changeScope = new EditorGUI.ChangeCheckScope();

                /* values adapted to the ProjectSettings > Graphics */
                var minWidth = 91;
                var indent = 94;
                var ratio = 0.45f;
                EditorGUIUtility.labelWidth = Mathf.Max(minWidth, (int)((container.worldBound.width - indent) * ratio));

                bool expanded = m_DefaultVolumeProfileFoldoutExpanded.value;
                var previousDefaultVolumeProfileAsset = m_VolumeProfileSerializedProperty.objectReferenceValue;

                VolumeProfile defaultVolumeProfileAsset = RenderPipelineGlobalSettingsUI.DrawVolumeProfileAssetField(
                    m_VolumeProfileSerializedProperty,
                    defaultVolumeProfileAssetLabel,
                    getOrCreateVolumeProfile: () =>
                    {
                        if (RenderPipelineManager.currentPipeline is not HDRenderPipeline)
                            return null;

                        var defaultVolumeProfileSettings = GraphicsSettings.GetRenderPipelineSettings<HDRPDefaultVolumeProfileSettings>();
                        if (defaultVolumeProfileSettings.volumeProfile == null)
                        {
                            defaultVolumeProfileSettings.volumeProfile = VolumeUtils.CopyVolumeProfileFromResourcesToAssets(
                                GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineEditorAssets>().defaultVolumeProfile);
                        }

                        // When the built-in Reset context action is used, the asset becomes null outside of this scope.
                        // This is required to apply the new value to the serialized property.
                        GUI.changed = true;

                        return defaultVolumeProfileSettings.volumeProfile;
                    },
                    ref expanded
                );
                m_DefaultVolumeProfileFoldoutExpanded.value = expanded;

                if (changeScope.changed && defaultVolumeProfileAsset != previousDefaultVolumeProfileAsset)
                {
                    var defaultValuesAsset = GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineEditorAssets>()?.defaultVolumeProfile;
                    if (RenderPipelineManager.currentPipeline is not HDRenderPipeline)
                    {
                        Debug.Log("Cannot change Default Volume Profile when HDRP is not active. Rolling back to previous value.");
                        m_VolumeProfileSerializedProperty.objectReferenceValue = previousDefaultVolumeProfileAsset;
                    }
                    else if (previousDefaultVolumeProfileAsset == null)
                    {
                        VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile<HDRenderPipeline>(defaultVolumeProfileAsset, defaultValuesAsset);
                        m_VolumeProfileSerializedProperty.objectReferenceValue = defaultVolumeProfileAsset;
                    }
                    else
                    {
                        bool confirmed = VolumeProfileUtils.UpdateGlobalDefaultVolumeProfileWithConfirmation<HDRenderPipeline>(defaultVolumeProfileAsset, defaultValuesAsset);
                        if (!confirmed)
                            m_VolumeProfileSerializedProperty.objectReferenceValue = previousDefaultVolumeProfileAsset;
                    }

                    m_SettingsSerializedObject.ApplyModifiedProperties();
                    m_VolumeProfileSerializedProperty.serializedObject.Update();

                    DestroyDefaultVolumeProfileEditor();
                    CreateDefaultVolumeProfileEditor();
                }

                // Propagate foldout expander state from IMGUI to UITK
                m_EditorContainer.style.display = m_DefaultVolumeProfileFoldoutExpanded.value
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            };
            return container;
        }

        public class HDRPDefaultVolumeProfileSettingsContextMenu : DefaultVolumeProfileSettingsContextMenu<HDRPDefaultVolumeProfileSettings, HDRenderPipeline>
        {
            protected override string defaultVolumeProfilePath =>
                VolumeUtils.GetDefaultNameForVolumeProfile(
                    GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineEditorAssets>().defaultVolumeProfile);
        }
    }
}
