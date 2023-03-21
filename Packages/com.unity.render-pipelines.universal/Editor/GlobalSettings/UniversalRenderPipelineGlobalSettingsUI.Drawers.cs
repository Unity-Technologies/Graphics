using UnityEngine;
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
                RenderingLayerNamesSection,
                MiscSection
        );
    }
}
