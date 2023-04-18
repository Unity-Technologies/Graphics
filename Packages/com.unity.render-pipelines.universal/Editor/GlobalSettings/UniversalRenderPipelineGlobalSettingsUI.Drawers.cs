using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    using CED = CoreEditorDrawer<SerializedUniversalRenderPipelineGlobalSettings>;

    internal partial class UniversalRenderPipelineGlobalSettingsUI
    {
        public class DocumentationUrls
        {
            public static readonly string k_Volumes = "Volumes";
        }

        #region Rendering Layer Names

        static readonly CED.IDrawer RenderingLayerNamesSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(
                Styles.renderingLayersLabel,
                contextAction: pos => OnContextClickRenderingLayerNames(pos, serialized))),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawRenderingLayerNames),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );

        static void DrawRenderingLayerNames(SerializedUniversalRenderPipelineGlobalSettings serialized, Editor owner)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                using (var changed = new EditorGUI.ChangeCheckScope())
                {
                    serialized.renderingLayerNameList.DoLayoutList();

                    if (changed.changed)
                    {
                        serialized.serializedObject?.ApplyModifiedProperties();
                        if (serialized.serializedObject?.targetObject is UniversalRenderPipelineGlobalSettings
                            urpGlobalSettings)
                            urpGlobalSettings.UpdateRenderingLayerNames();
                    }
                }
            }
        }

        static void OnContextClickRenderingLayerNames(
            Vector2 position,
            SerializedUniversalRenderPipelineGlobalSettings serialized)
        {
            var menu = new GenericMenu();
            menu.AddItem(CoreEditorStyles.resetButtonLabel, false, () =>
            {
                var globalSettings =
                    (serialized.serializedObject.targetObject as UniversalRenderPipelineGlobalSettings);
                globalSettings.ResetRenderingLayerNames();
            });
            menu.DropDown(new Rect(position, Vector2.zero));
        }

        #endregion

        #region Default Volume Profile

        private static readonly CED.IDrawer DefaultVolumeProfileSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(
                Styles.defaultVolumeProfileHeaderLabel,
                Documentation.GetPageLink(DocumentationUrls.k_Volumes),
                pos => OnVolumeProfileSectionContextClick(pos, serialized, owner))),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawVolumeSection),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );

        private static bool s_DefaultVolumeProfileFoldoutExpanded = true;

        static void DrawVolumeSection(SerializedUniversalRenderPipelineGlobalSettings serialized, Editor owner)
        {
            if (owner is not UniversalGlobalSettingsEditor universalGlobalSettingsEditor)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                var oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = Styles.defaultVolumeLabelWidth;

                var globalSettings = serialized.serializedObject.targetObject as UniversalRenderPipelineGlobalSettings;

                var previousDefaultVolumeProfileAsset = serialized.defaultVolumeProfile.objectReferenceValue;
                VolumeProfile defaultVolumeProfileAsset = RenderPipelineGlobalSettingsUI.DrawVolumeProfileAssetField(
                    serialized.defaultVolumeProfile,
                    Styles.defaultVolumeProfileLabel,
                    getOrCreateVolumeProfile: () => globalSettings.GetOrCreateDefaultVolumeProfile(),
                    ref s_DefaultVolumeProfileFoldoutExpanded
                );
                EditorGUIUtility.labelWidth = Styles.volumeProfileEditorLabelWidth;

                if (defaultVolumeProfileAsset != previousDefaultVolumeProfileAsset)
                {
                    bool confirmed = VolumeProfileUtils.UpdateGlobalDefaultVolumeProfileWithConfirmation(defaultVolumeProfileAsset);
                    if (!confirmed)
                        serialized.defaultVolumeProfile.objectReferenceValue = previousDefaultVolumeProfileAsset;
                }

                if (defaultVolumeProfileAsset != null && s_DefaultVolumeProfileFoldoutExpanded)
                {
                    var editor =
                        universalGlobalSettingsEditor.GetDefaultVolumeProfileEditor(defaultVolumeProfileAsset) as
                            VolumeProfileEditor;

                    bool oldEnabled = GUI.enabled;
                    GUI.enabled = AssetDatabase.IsOpenForEdit(defaultVolumeProfileAsset);
                    GUILayout.Space(4);
                    editor.OnInspectorGUI();
                    GUI.enabled = oldEnabled;
                }

                EditorGUIUtility.labelWidth = oldWidth;
            }
        }

        static void OnVolumeProfileSectionContextClick(
            Vector2 position,
            SerializedUniversalRenderPipelineGlobalSettings serialized,
            Editor owner)
        {
            if (owner is UniversalGlobalSettingsEditor universalGlobalSettingsEditor)
            {
                var editor = universalGlobalSettingsEditor.GetDefaultVolumeProfileEditor(
                    serialized.defaultVolumeProfile.objectReferenceValue as VolumeProfile) as VolumeProfileEditor;

                VolumeProfileUtils.OnVolumeProfileContextClick(position, editor,
                    defaultVolumeProfilePath: "Assets/VolumeProfile_Default.asset",
                    onNewVolumeProfileCreated: volumeProfile =>
                    {
                        var globalSettings =
                            serialized.serializedObject.targetObject as UniversalRenderPipelineGlobalSettings;
                        Undo.RecordObject(globalSettings, "Set Global Settings Volume Profile");
                        globalSettings.volumeProfile = volumeProfile;
                        VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile(volumeProfile);
                        EditorUtility.SetDirty(globalSettings);
                    });
            }
        }

        #endregion

        #region Misc Settings

        static readonly CED.IDrawer MiscSection =
            CED.Group((s, owner) =>
            {
#pragma warning disable 618 // Obsolete warning
                CoreEditorUtils.DrawSectionHeader(RenderPipelineGlobalSettingsUI.Styles.shaderStrippingSettingsLabel);
#pragma warning restore 618 // Obsolete warning
                EditorGUI.indentLevel++;
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(s.serializedObject.FindProperty("m_ShaderStrippingSetting"));
                EditorGUILayout.PropertyField(s.serializedObject.FindProperty("m_URPShaderStrippingSetting"));
                EditorGUI.indentLevel--;
            });
        #endregion

        public static readonly CED.IDrawer Inspector = CED.Group(
            DefaultVolumeProfileSection,
            RenderingLayerNamesSection,
            MiscSection
        );
    }
}
