using System;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(URPDefaultVolumeProfileSettings))]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    class URPDefaultVolumeProfileSettingsPropertyDrawer : DefaultVolumeProfileSettingsPropertyDrawer
    {
        GUIContent defaultVolumeProfileAssetLabel => EditorGUIUtility.TrTextContent("Default Profile",
            "Settings that will be applied project-wide to all Volumes by default when URP is active.");

        protected override GUIContent volumeInfoBoxLabel => EditorGUIUtility.TrTextContent(
            "The values in the Default Volume can be overridden by a Volume Profile assigned to URP asset and Volumes inside scenes.");

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
                        if (RenderPipelineManager.currentPipeline is not UniversalRenderPipeline)
                            return null;

                        // When the built-in Reset context action is used, the asset becomes null outside of this scope.
                        // This is required to apply the new value to the serialized property.
                        GUI.changed = true;

                        return UniversalRenderPipelineGlobalSettings.GetOrCreateDefaultVolumeProfile(null);
                    },
                    ref expanded
                );
                m_DefaultVolumeProfileFoldoutExpanded.value = expanded;

                if (changeScope.changed && defaultVolumeProfileAsset != previousDefaultVolumeProfileAsset)
                {
                    if (RenderPipelineManager.currentPipeline is not UniversalRenderPipeline)
                    {
                        Debug.Log("Cannot change Default Volume Profile when URP is not active. Rolling back to previous value.");
                        m_VolumeProfileSerializedProperty.objectReferenceValue = previousDefaultVolumeProfileAsset;
                    }
                    else if (previousDefaultVolumeProfileAsset == null)
                    {
                        VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile<UniversalRenderPipeline>(defaultVolumeProfileAsset);
                        m_VolumeProfileSerializedProperty.objectReferenceValue = defaultVolumeProfileAsset;
                    }
                    else
                    {
                        bool confirmed = VolumeProfileUtils.UpdateGlobalDefaultVolumeProfileWithConfirmation<UniversalRenderPipeline>(defaultVolumeProfileAsset);
                        if (!confirmed)
                            m_VolumeProfileSerializedProperty.objectReferenceValue = previousDefaultVolumeProfileAsset;
                    }

                    m_SettingsSerializedObject.ApplyModifiedProperties();
                    m_VolumeProfileSerializedProperty.serializedObject.Update();

                    DestroyDefaultVolumeProfileEditor();
                    CreateDefaultVolumeProfileEditor();
                }

                // Propagate foldout expander state from IMGUI to UITK
                m_EditorContainer.style.display = m_DefaultVolumeProfileFoldoutExpanded.value ? DisplayStyle.Flex : DisplayStyle.None;
            };
            return container;
        }

        public class URPDefaultVolumeProfileSettingsContextMenu : DefaultVolumeProfileSettingsContextMenu<URPDefaultVolumeProfileSettings, UniversalRenderPipeline>
        {
            protected override string defaultVolumeProfilePath => "Assets/VolumeProfile_Default.asset";
        }
    }
}
