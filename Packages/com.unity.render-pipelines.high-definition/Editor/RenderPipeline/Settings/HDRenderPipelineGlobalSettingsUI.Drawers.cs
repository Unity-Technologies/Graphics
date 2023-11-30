using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDRenderPipelineGlobalSettings>;

    internal partial class HDRenderPipelineGlobalSettingsUI
    {
        static void RenderPipelineGraphicsSettings_Drawer<T>(SerializedProperty property)
        {
            if (property == null)
                EditorGUILayout.HelpBox($"Unable to find {typeof(T)}", MessageType.Error);
            else
                EditorGUILayout.PropertyField(property);
        }

        public class DocumentationUrls
        {
            public static readonly string k_Volumes = "Volumes";
            public static readonly string k_LookDev = "Look-Dev";
            public static readonly string k_FrameSettings = "Frame-Settings";
            public static readonly string k_RenderingLayers = "Rendering-Layers";
            public static readonly string k_DecalLayers = "Decal";
            public static readonly string k_CustomPostProcesses = "Custom-Post-Process";
        }

        #region Resources

        public static VisualElement CreateImguiSections(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            return new IMGUIContainer(() =>
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.BeginVertical();

                using (var changedScope = new EditorGUI.ChangeCheckScope())
                {
                    Inspector.Draw(serialized, owner);
                    if (changedScope.changed)
                        serialized.serializedObject.ApplyModifiedProperties();
                }

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            });
        }

        static readonly CED.IDrawer ResourcesSection = CED.Conditional(
            (s,o) => Unsupported.IsDeveloperMode(),
            CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.resourceLabel)),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawResourcesSection),
            CED.Group((serialized, owner) => EditorGUILayout.Space()))
        );

        static void DrawResourcesSection(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                var oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = Styles.labelWidth;

                EditorGUILayout.PropertyField(serialized.renderPipelineResources, Styles.renderPipelineResourcesContent);
                bool oldGuiEnabled = GUI.enabled;
                GUI.enabled = false;

                EditorGUI.showMixedValue = false;

                GUI.enabled = oldGuiEnabled;
                EditorGUIUtility.labelWidth = oldWidth;
            }
        }

        #endregion // Resources

        #region Frame Settings

        static readonly CED.IDrawer FrameSettingsSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.frameSettingsLabel, Documentation.GetPageLink(DocumentationUrls.k_FrameSettings))),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawFrameSettings),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );

        static void DrawFrameSettings(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            RenderPipelineGraphicsSettings_Drawer<RenderingPathFrameSettings>(serialized.serializedRenderingPathProperty);
        }

        #endregion // Frame Settings

        #region Custom Post Processes

        static readonly CED.IDrawer CustomPostProcessesSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.customPostProcessOrderLabel, Documentation.GetPageLink(DocumentationUrls.k_CustomPostProcesses))),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawCustomPostProcess)
        );
        static void DrawCustomPostProcess(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            RenderPipelineGraphicsSettings_Drawer<CustomPostProcessOrdersSettings>(serialized.serializedCustomPostProcessOrdersSettings);
        }

        #endregion // Custom Post Processes

        #region LookDev Volume Profile

        static readonly CED.IDrawer VolumeSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(
                Styles.lookDevVolumeProfileSectionLabel,
                Documentation.GetPageLink(DocumentationUrls.k_LookDev),
                pos => OnLookDevVolumeProfileSectionContextClick(pos, serialized, owner))),
            CED.Group((_, _) => EditorGUILayout.Space()),
            CED.Group(DrawLookDevVolumeSection)
        );

        private static bool s_LookDevVolumeProfileFoldoutExpanded = true;

        static void DrawLookDevVolumeSection(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            if (owner is not HDRenderPipelineGlobalSettingsEditor hdGlobalSettingsEditor)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                var oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = Styles.defaultVolumeLabelWidth;

                var lookDevVolumeProfileSettings = GraphicsSettings.GetRenderPipelineSettings<LookDevVolumeProfileSettings>();

                VolumeProfile lookDevAsset = RenderPipelineGlobalSettingsUI.DrawVolumeProfileAssetField(
                    serialized.lookDevVolumeProfile,
                    Styles.lookDevVolumeProfileAssetLabel,
                    getOrCreateVolumeProfile: () =>
                    {
                        if (lookDevVolumeProfileSettings.volumeProfile == null)
                        {
                            lookDevVolumeProfileSettings.volumeProfile = VolumeUtils.CopyVolumeProfileFromResourcesToAssets(GraphicsSettings
                                .GetRenderPipelineSettings<HDRenderPipelineEditorAssets>().lookDevVolumeProfile);
                        }
                        return lookDevVolumeProfileSettings.volumeProfile;
                    },
                    ref s_LookDevVolumeProfileFoldoutExpanded
                );

                if (lookDevAsset != null && s_LookDevVolumeProfileFoldoutExpanded)
                {
                    var editor = hdGlobalSettingsEditor.GetLookDevDefaultVolumeProfileEditor(lookDevAsset) as VolumeProfileEditor;;

                    bool oldEnabled = GUI.enabled;
                    GUI.enabled = AssetDatabase.IsOpenForEdit(lookDevAsset);
                    GUILayout.Space(4);
                    editor.OnInspectorGUI();
                    GUI.enabled = oldEnabled;

                    if (lookDevAsset.Has<VisualEnvironment>())
                        EditorGUILayout.HelpBox("VisualEnvironment is not modifiable and will be overridden by the LookDev", MessageType.Warning);
                    if (lookDevAsset.Has<HDRISky>())
                        EditorGUILayout.HelpBox("HDRISky is not modifiable and will be overridden by the LookDev", MessageType.Warning);
                }
                EditorGUIUtility.labelWidth = oldWidth;
            }
        }

        static void OnLookDevVolumeProfileSectionContextClick(Vector2 pos, SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            if (owner is not HDRenderPipelineGlobalSettingsEditor hdGlobalSettingsEditor)
                return;

            var lookDevVolumeProfile = serialized.lookDevVolumeProfile.objectReferenceValue as VolumeProfile;
            var editor = hdGlobalSettingsEditor.GetLookDevDefaultVolumeProfileEditor(lookDevVolumeProfile) as VolumeProfileEditor;
			var componentEditors = editor != null ? editor.componentList.editors : null;

#pragma warning disable 618 // Obsolete warning
            VolumeProfileUtils.OnVolumeProfileContextClick(pos, lookDevVolumeProfile, componentEditors,
                overrideStateOnReset: false,
                defaultVolumeProfilePath: $"Assets/{HDProjectSettings.projectSettingsFolderPath}/LookDevProfile_Default.asset",
                onNewVolumeProfileCreated: volumeProfile =>
                {
                    serialized.lookDevVolumeProfile.objectReferenceValue = volumeProfile;
                    serialized.serializedObject.ApplyModifiedProperties();
                });
#pragma warning restore 618 // Obsolete warning
        }

        #endregion // Volume Profiles

        #region Misc Settings

        static MethodInfo s_CleanupRenderPipelineMethod = typeof(RenderPipelineManager).GetMethod("CleanupRenderPipeline", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        private static readonly CED.IDrawer MiscSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.generalSettingsLabel)),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawMiscSettings),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group((s, owner) =>
            {
#pragma warning disable 618 // Obsolete warning
                CoreEditorUtils.DrawSectionHeader(RenderPipelineGlobalSettingsUI.Styles.shaderStrippingSettingsLabel);
#pragma warning restore 618 // Obsolete warning
                var oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = Styles.labelWidth;

                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.Space();
                    RenderPipelineGraphicsSettings_Drawer<ShaderStrippingSetting>(s.serializedShaderStrippingSettings);
                }
                EditorGUIUtility.labelWidth = oldWidth;
            })
        );
        static void DrawMiscSettings(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var p in serialized.miscSectionSerializedProperties)
                {
                    EditorGUILayout.PropertyField(p.serializedProperty, EditorGUIUtility.TrTextContent(p.displayName, p.tooltip));
                }
            }
            EditorGUIUtility.labelWidth = oldWidth;
            EditorGUILayout.Space(5);
        }

        #endregion // Misc Settings

        static readonly CED.IDrawer Inspector = CED.Group(
            VolumeSection,
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            FrameSettingsSection,
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CustomPostProcessesSection,
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            MiscSection,
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            ResourcesSection);
    }
}
