using System;
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

                EditorGUILayout.PropertyField(serialized.renderPipelineRayTracingResources, Styles.renderPipelineRayTracingResourcesContent);

                // Not serialized as editor only datas... Retrieve them in data
                EditorGUI.showMixedValue = serialized.editorResourceHasMultipleDifferentValues;
                var editorResources = EditorGUILayout.ObjectField(Styles.renderPipelineEditorResourcesContent, serialized.firstEditorResources, typeof(HDRenderPipelineEditorResources), allowSceneObjects: false) as HDRenderPipelineEditorResources;

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
            EditorGUILayout.PropertyField(serialized.serializedObject.FindProperty("m_RenderingPath"));
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
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(5);
                serialized.uiBeforeTransparentCustomPostProcesses.DoLayoutList();
            }
            GUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(5);
                serialized.uiBeforeTAACustomPostProcesses.DoLayoutList();
            }
            GUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(5);
                serialized.uiBeforePostProcessCustomPostProcesses.DoLayoutList();
            }
            GUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(5);
                serialized.uiAfterPostProcessBlursCustomPostProcesses.DoLayoutList();
            }
            GUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(5);
                serialized.uiAfterPostProcessCustomPostProcesses.DoLayoutList();
            }
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

                HDRenderPipelineGlobalSettings globalSettings = serialized.serializedObject.targetObject as HDRenderPipelineGlobalSettings;
                VolumeProfile lookDevAsset = RenderPipelineGlobalSettingsUI.DrawVolumeProfileAssetField(
                    serialized.lookDevVolumeProfile,
                    Styles.lookDevVolumeProfileAssetLabel,
                    getOrCreateVolumeProfile: () => globalSettings.GetOrAssignLookDevVolumeProfile(),
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

            var editor = hdGlobalSettingsEditor.GetLookDevDefaultVolumeProfileEditor(
                serialized.lookDevVolumeProfile.objectReferenceValue as VolumeProfile) as VolumeProfileEditor;
            var componentEditors = editor != null ? editor.componentList.editors : null;
            var globalSettings = serialized.serializedObject.targetObject as HDRenderPipelineGlobalSettings;

            VolumeProfileUtils.OnVolumeProfileContextClick(pos, globalSettings.lookDevVolumeProfile, componentEditors,
                overrideStateOnReset: false,
                defaultVolumeProfilePath: $"Assets/{HDProjectSettings.projectSettingsFolderPath}/LookDevProfile_Default.asset",
                onNewVolumeProfileCreated: volumeProfile =>
                {

                    Undo.RecordObject(globalSettings, "Set Global Settings LookDev Profile");
                    globalSettings.lookDevVolumeProfile = volumeProfile;
                    EditorUtility.SetDirty(globalSettings);
                });
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
                    EditorGUILayout.PropertyField(s.serializedObject.FindProperty("m_ShaderStrippingSetting"));
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
                EditorGUILayout.PropertyField(serialized.lensAttenuation, Styles.lensAttenuationModeContentLabel);
                EditorGUILayout.PropertyField(serialized.colorGradingSpace, Styles.colorGradingSpaceContentLabel);
                EditorGUILayout.PropertyField(serialized.rendererListCulling, Styles.rendererListCulling);
                EditorGUILayout.PropertyField(serialized.specularFade, Styles.specularFade);

                EditorGUILayout.PropertyField(serialized.autoRegisterDiffusionProfiles, Styles.autoRegisterDiffusionProfilesContentLabel);

                EditorGUILayout.PropertyField(serialized.analyticDerivativeEmulation, Styles.analyticDerivativeEmulationContentLabel);
                EditorGUILayout.PropertyField(serialized.analyticDerivativeDebugOutput, Styles.analyticDerivativeDebugOutputContentLabel);

            }
            EditorGUIUtility.labelWidth = oldWidth;
        }

        #endregion // Misc Settings

        #region Rendering Layer Names

        static readonly CED.IDrawer LayerNamesSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.renderingLayersLabel, documentationURL: Documentation.GetPageLink(DocumentationUrls.k_RenderingLayers))),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawLayerNamesSettings),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );

        static private bool m_ShowLayerNames = false;
        static void DrawLayerNamesSettings(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            if (HDRenderPipelineGlobalSettings.instance == null)
                return;

            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;

            EditorGUI.BeginChangeCheck();
            int value = EditorGUILayout.MaskField(Styles.defaultRenderingLayerMaskLabel, serialized.defaultRenderingLayerMask.intValue, HDRenderPipelineGlobalSettings.instance.prefixedRenderingLayerNames);
            if (EditorGUI.EndChangeCheck())
            {
                serialized.defaultRenderingLayerMask.intValue = value;
                GraphicsSettings.defaultRenderingLayerMask = (uint)value;

            }

            EditorGUILayout.Space();
            CoreEditorUtils.DrawSplitter();
            m_ShowLayerNames = CoreEditorUtils.DrawHeaderFoldout(Styles.renderingLayerNamesLabel,
                m_ShowLayerNames,
                contextAction: pos => OnContextClickRenderingLayerNames(pos, serialized, section: 1)
            );
            if (m_ShowLayerNames)
            {
                EditorGUILayout.Space();
                EditorGUI.BeginChangeCheck();
                serialized.renderingLayerNamesList.DoLayoutList();
                if (EditorGUI.EndChangeCheck())
                {
                    serialized.serializedObject?.ApplyModifiedProperties();
                    (serialized.serializedObject.targetObject as HDRenderPipelineGlobalSettings).UpdateRenderingLayerNames();
                }

                EditorGUILayout.Space();
            }
            CoreEditorUtils.DrawSplitter();

            EditorGUIUtility.labelWidth = oldWidth;
        }

        static void OnContextClickRenderingLayerNames(Vector2 position, SerializedHDRenderPipelineGlobalSettings serialized, int section = 0)
        {
            var menu = new GenericMenu();
            menu.AddItem(section == 0 ? CoreEditorStyles.resetAllButtonLabel : CoreEditorStyles.resetButtonLabel, false, () =>
            {
                var globalSettings = (serialized.serializedObject.targetObject as HDRenderPipelineGlobalSettings);
                Undo.RecordObject(globalSettings, "Reset rendering layer names");
                globalSettings.ResetRenderingLayerNames();
            });
            menu.DropDown(new Rect(position, Vector2.zero));
        }

        #endregion



        static readonly CED.IDrawer Inspector = CED.Group(
            VolumeSection,
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            FrameSettingsSection,
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            LayerNamesSection,
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CustomPostProcessesSection,
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            MiscSection,
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            ResourcesSection);
    }
}
