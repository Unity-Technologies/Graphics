using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    using CED = CoreEditorDrawer<SerializedUniversalRenderPipelineGlobalSettings>;

    internal partial class UniversalRenderPipelineGlobalSettingsUI
    {
        #region Rendering Layer Names

        static readonly CED.IDrawer RenderingLayerNamesSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.renderingLayersLabel, contextAction: pos => OnContextClickRenderingLayerNames(pos, serialized))),
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
                        if (serialized.serializedObject?.targetObject is UniversalRenderPipelineGlobalSettings urpGlobalSettings)
                            urpGlobalSettings.UpdateRenderingLayerNames();
                    }
                }
            }
        }

        static void OnContextClickRenderingLayerNames(Vector2 position, SerializedUniversalRenderPipelineGlobalSettings serialized)
        {
            var menu = new GenericMenu();
            menu.AddItem(CoreEditorStyles.resetButtonLabel, false, () =>
            {
                var globalSettings = (serialized.serializedObject.targetObject as UniversalRenderPipelineGlobalSettings);
                globalSettings.ResetRenderingLayerNames();
            });
            menu.DropDown(new Rect(position, Vector2.zero));
        }

        #endregion

        #region Default Volume Profile

        static readonly CED.IDrawer DefaultVolumeProfileSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.defaultVolumeProfileHeaderLabel)),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawVolumeProfile),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );

        static void DrawVolumeProfile(SerializedUniversalRenderPipelineGlobalSettings serialized, Editor owner)
        {
            if (owner is not UniversalGlobalSettingsEditor universalGlobalSettingsEditor)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                var oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = Styles.labelWidth;

                var globalSettings = serialized.serializedObject.targetObject as UniversalRenderPipelineGlobalSettings;
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
                            asset = oldAssetValue as VolumeProfile;
                        }
                        else
                        {
                            asset = globalSettings.GetOrCreateDefaultVolumeProfile();
                        }
                    }

                    if (asset != oldAssetValue)
                        VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile(asset);

                    if (GUILayout.Button(Styles.newVolumeProfileLabel, GUILayout.Width(38), GUILayout.Height(18)))
                    {
                        if (globalSettings != null)
                        {
                            string path = $"Assets/VolumeProfile_Default.asset";
                            VolumeProfileFactory.CreateVolumeProfileWithCallback(path, (volumeProfile) =>
                            {
                                globalSettings.volumeProfile = volumeProfile;
                                VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile(volumeProfile);
                                EditorUtility.SetDirty(globalSettings);
                            });
                        }
                        else
                        {
                            Debug.LogError("Trying to create a Volume Profile but for URP Global Settings asset that is null. Operation aborted.");
                        }
                    }
                }

                if (asset != null)
                {
                    var editor = universalGlobalSettingsEditor.GetDefaultVolumeProfileEditor(asset);

                    bool oldEnabled = GUI.enabled;
                    GUI.enabled = AssetDatabase.IsOpenForEdit(asset);
                    editor.OnInspectorGUI();
                    GUI.enabled = oldEnabled;
                }

                EditorGUIUtility.labelWidth = oldWidth;
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
