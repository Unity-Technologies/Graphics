using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDRenderPipelineGlobalSettings>;

    internal partial class HDRenderPipelineGlobalSettingsUI
    {
        public class DocumentationUrls
        {
            public static readonly string k_Volumes = "Volume-Profile";
            public static readonly string k_FrameSettings = "Frame-Settings";
            public static readonly string k_LightLayers = "Light-Layers";
            public static readonly string k_DecalLayers = "Decal";
            public static readonly string k_CustomPostProcesses = "Custom-Post-Process";
        }

        #region Resources

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
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField(Styles.frameSettingsLabel_Camera, CoreEditorStyles.subSectionHeaderStyle);
                DrawFrameSettingsSubsection(0, serialized.defaultCameraFrameSettings, owner);
                EditorGUILayout.Space();

                EditorGUILayout.LabelField(Styles.frameSettingsLabel_RTProbe, CoreEditorStyles.subSectionHeaderStyle);
                DrawFrameSettingsSubsection(1, serialized.defaultRealtimeReflectionFrameSettings, owner);
                EditorGUILayout.Space();

                EditorGUILayout.LabelField(Styles.frameSettingsLabel_BakedProbe, CoreEditorStyles.subSectionHeaderStyle);
                DrawFrameSettingsSubsection(2, serialized.defaultBakedOrCustomReflectionFrameSettings, owner);
                EditorGUILayout.Space();
            }
        }

        static private bool[] m_ShowFrameSettings_Rendering = { false, false, false };
        static private bool[] m_ShowFrameSettings_Lighting = { false, false, false };
        static private bool[] m_ShowFrameSettings_AsyncCompute = { false, false, false };
        static private bool[] m_ShowFrameSettings_LightLoopDebug = { false, false, false };

        static void DrawFrameSettingsSubsection(int index, SerializedFrameSettings serialized, Editor owner)
        {
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;

            CoreEditorUtils.DrawSplitter();
            m_ShowFrameSettings_Rendering[index] = CoreEditorUtils.DrawHeaderFoldout(Styles.renderingSettingsHeaderContent, m_ShowFrameSettings_Rendering[index]);
            if (m_ShowFrameSettings_Rendering[index])
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    FrameSettingsUI.Drawer_SectionRenderingSettings(serialized, owner, withOverride: false);
                }
            }

            CoreEditorUtils.DrawSplitter();
            m_ShowFrameSettings_Lighting[index] = CoreEditorUtils.DrawHeaderFoldout(Styles.lightSettingsHeaderContent, m_ShowFrameSettings_Lighting[index]);
            if (m_ShowFrameSettings_Lighting[index])
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    FrameSettingsUI.Drawer_SectionLightingSettings(serialized, owner, withOverride: false);
                }
            }

            CoreEditorUtils.DrawSplitter();
            m_ShowFrameSettings_AsyncCompute[index] = CoreEditorUtils.DrawHeaderFoldout(Styles.asyncComputeSettingsHeaderContent, m_ShowFrameSettings_AsyncCompute[index]);
            if (m_ShowFrameSettings_AsyncCompute[index])
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    FrameSettingsUI.Drawer_SectionAsyncComputeSettings(serialized, owner, withOverride: false);
                }
            }

            CoreEditorUtils.DrawSplitter();
            m_ShowFrameSettings_LightLoopDebug[index] = CoreEditorUtils.DrawHeaderFoldout(Styles.lightLoopSettingsHeaderContent, m_ShowFrameSettings_LightLoopDebug[index]);
            if (m_ShowFrameSettings_LightLoopDebug[index])
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    FrameSettingsUI.Drawer_SectionLightLoopSettings(serialized, owner, withOverride: false);
                }
            }
            CoreEditorUtils.DrawSplitter();
            EditorGUIUtility.labelWidth = oldWidth;
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

        #region Volume Profiles

        static readonly CED.IDrawer VolumeSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.volumeComponentsLabel, Documentation.GetPageLink(DocumentationUrls.k_Volumes))),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawVolumeSection),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );

        static void DrawVolumeSection(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            if (owner is not HDRenderPipelineGlobalSettingsEditor hdGlobalSettingsEditor)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                var oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = Styles.labelWidth;

                HDRenderPipelineGlobalSettings globalSettings = serialized.serializedObject.targetObject as HDRenderPipelineGlobalSettings;
                VolumeProfile asset = null;
                using (new EditorGUILayout.HorizontalScope())
                {
                    var oldAssetValue = serialized.defaultVolumeProfile.objectReferenceValue;
                    EditorGUILayout.PropertyField(serialized.defaultVolumeProfile, Styles.defaultVolumeProfileLabel);
                    asset = serialized.defaultVolumeProfile.objectReferenceValue as VolumeProfile;
                    if (asset == null)
                    {
                        if (oldAssetValue != null)
                        {
                            Debug.Log("Default Volume Profile Asset cannot be null. Rolling back to previous value.");
                            serialized.defaultVolumeProfile.objectReferenceValue = oldAssetValue;
                        }
                        else
                        {
                            asset = globalSettings.GetOrCreateDefaultVolumeProfile();
                        }
                    }

                    if (GUILayout.Button(Styles.newVolumeProfileLabel, GUILayout.Width(38), GUILayout.Height(18)))
                    {
                        HDAssetFactory.VolumeProfileCreator.CreateAndAssign(HDAssetFactory.VolumeProfileCreator.Kind.Default, globalSettings);
                    }
                }
                if (asset != null)
                {
                    var editor = hdGlobalSettingsEditor.GetDefaultVolumeProfileEditor(asset);

                    bool oldEnabled = GUI.enabled;
                    GUI.enabled = AssetDatabase.IsOpenForEdit(asset);
                    editor.OnInspectorGUI();
                    GUI.enabled = oldEnabled;
                }

                EditorGUILayout.Space();

                VolumeProfile lookDevAsset = null;
                using (new EditorGUILayout.HorizontalScope())
                {
                    var oldAssetValue = serialized.lookDevVolumeProfile.objectReferenceValue;
                    EditorGUILayout.PropertyField(serialized.lookDevVolumeProfile, Styles.lookDevVolumeProfileLabel);
                    lookDevAsset = serialized.lookDevVolumeProfile.objectReferenceValue as VolumeProfile;
                    if (lookDevAsset == null)
                    {
                        if (oldAssetValue != null)
                        {
                            Debug.Log("LookDev Volume Profile Asset cannot be null. Rolling back to previous value.");
                            serialized.lookDevVolumeProfile.objectReferenceValue = oldAssetValue;
                        }
                        else
                        {
                            lookDevAsset = globalSettings.GetOrAssignLookDevVolumeProfile();
                        }
                    }

                    if (GUILayout.Button(Styles.newVolumeProfileLabel, GUILayout.Width(38), GUILayout.Height(18)))
                    {
                        HDAssetFactory.VolumeProfileCreator.CreateAndAssign(HDAssetFactory.VolumeProfileCreator.Kind.LookDev, globalSettings);
                    }
                }
                if (lookDevAsset != null)
                {
                    var editor = hdGlobalSettingsEditor.GetLookDevDefaultVolumeProfileEditor(lookDevAsset);

                    bool oldEnabled = GUI.enabled;
                    GUI.enabled = AssetDatabase.IsOpenForEdit(lookDevAsset);
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

        #endregion // Volume Profiles

        #region Misc Settings

        static MethodInfo s_CleanupRenderPipelineMethod = typeof(RenderPipelineManager).GetMethod("CleanupRenderPipeline", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        static readonly CED.IDrawer MiscSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.generalSettingsLabel)),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawMiscSettings),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group((serialized, owner) => RenderPipelineGlobalSettingsUI.DrawShaderStrippingSettings(serialized, owner))
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

#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
                EditorGUILayout.PropertyField(serialized.useDLSSCustomProjectId, Styles.useDLSSCustomProjectIdLabel);
                if (serialized.useDLSSCustomProjectId.boolValue)
                    EditorGUILayout.PropertyField(serialized.DLSSProjectId, Styles.DLSSProjectIdLabel);
#endif
                EditorGUILayout.PropertyField(serialized.supportRuntimeDebugDisplay, Styles.supportRuntimeDebugDisplayContentLabel);
                EditorGUILayout.PropertyField(serialized.autoRegisterDiffusionProfiles, Styles.autoRegisterDiffusionProfilesContentLabel);
            }
            EditorGUIUtility.labelWidth = oldWidth;
        }

        #endregion // Misc Settings

        #region Rendering Layer Names

        static readonly CED.IDrawer LayerNamesSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.layerNamesLabel, contextAction: pos => OnContextClickRenderingLayerNames(pos, serialized), documentationURL: Documentation.GetPageLink(DocumentationUrls.k_LightLayers))),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawLayerNamesSettings),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );

        static void DrawLayerNamesSettings(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;

            CoreEditorUtils.DrawSplitter();
            DrawLightLayerNames(serialized, owner);
            CoreEditorUtils.DrawSplitter();
            DrawDecalLayerNames(serialized, owner);
            CoreEditorUtils.DrawSplitter();
            EditorGUILayout.Space();

            EditorGUIUtility.labelWidth = oldWidth;
        }

        static private bool m_ShowLightLayerNames = false;
        static private bool m_ShowDecalLayerNames = false;
        static void DrawLightLayerNames(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            m_ShowLightLayerNames = CoreEditorUtils.DrawHeaderFoldout(Styles.lightLayersLabel,
                m_ShowLightLayerNames,
                documentationURL: Documentation.GetPageLink(DocumentationUrls.k_LightLayers),
                contextAction: pos => OnContextClickRenderingLayerNames(pos, serialized, section: 1)
            );
            if (m_ShowLightLayerNames)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    using (var changed = new EditorGUI.ChangeCheckScope())
                    {
                        EditorGUILayout.DelayedTextField(serialized.lightLayerName0, Styles.lightLayerName0);
                        EditorGUILayout.DelayedTextField(serialized.lightLayerName1, Styles.lightLayerName1);
                        EditorGUILayout.DelayedTextField(serialized.lightLayerName2, Styles.lightLayerName2);
                        EditorGUILayout.DelayedTextField(serialized.lightLayerName3, Styles.lightLayerName3);
                        EditorGUILayout.DelayedTextField(serialized.lightLayerName4, Styles.lightLayerName4);
                        EditorGUILayout.DelayedTextField(serialized.lightLayerName5, Styles.lightLayerName5);
                        EditorGUILayout.DelayedTextField(serialized.lightLayerName6, Styles.lightLayerName6);
                        EditorGUILayout.DelayedTextField(serialized.lightLayerName7, Styles.lightLayerName7);
                        if (changed.changed)
                        {
                            serialized.serializedObject?.ApplyModifiedProperties();
                            (serialized.serializedObject.targetObject as HDRenderPipelineGlobalSettings).UpdateRenderingLayerNames();
                        }
                    }
                }
            }
        }

        static void DrawDecalLayerNames(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            m_ShowDecalLayerNames = CoreEditorUtils.DrawHeaderFoldout(Styles.decalLayersLabel, m_ShowDecalLayerNames,
                documentationURL: Documentation.GetPageLink(DocumentationUrls.k_DecalLayers),
                contextAction: pos => OnContextClickRenderingLayerNames(pos, serialized, section: 2));
            if (m_ShowDecalLayerNames)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    using (var changed = new EditorGUI.ChangeCheckScope())
                    {
                        EditorGUILayout.DelayedTextField(serialized.decalLayerName0, Styles.decalLayerName0);
                        EditorGUILayout.DelayedTextField(serialized.decalLayerName1, Styles.decalLayerName1);
                        EditorGUILayout.DelayedTextField(serialized.decalLayerName2, Styles.decalLayerName2);
                        EditorGUILayout.DelayedTextField(serialized.decalLayerName3, Styles.decalLayerName3);
                        EditorGUILayout.DelayedTextField(serialized.decalLayerName4, Styles.decalLayerName4);
                        EditorGUILayout.DelayedTextField(serialized.decalLayerName5, Styles.decalLayerName5);
                        EditorGUILayout.DelayedTextField(serialized.decalLayerName6, Styles.decalLayerName6);
                        EditorGUILayout.DelayedTextField(serialized.decalLayerName7, Styles.decalLayerName7);
                        if (changed.changed)
                        {
                            serialized.serializedObject?.ApplyModifiedProperties();
                            (serialized.serializedObject.targetObject as HDRenderPipelineGlobalSettings).UpdateRenderingLayerNames();
                        }
                    }
                }
            }
        }

        static void OnContextClickRenderingLayerNames(Vector2 position, SerializedHDRenderPipelineGlobalSettings serialized, int section = 0)
        {
            var menu = new GenericMenu();
            menu.AddItem(section == 0 ? CoreEditorStyles.resetAllButtonLabel : CoreEditorStyles.resetButtonLabel, false, () =>
            {
                var globalSettings = (serialized.serializedObject.targetObject as HDRenderPipelineGlobalSettings);
                globalSettings.ResetRenderingLayerNames(lightLayers: section < 2, decalLayers: section != 1);
            });
            menu.DropDown(new Rect(position, Vector2.zero));
        }

        #endregion

        public static readonly CED.IDrawer Inspector = CED.Group(
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
