using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Universal
{
    using CED = CoreEditorDrawer<SerializedUniversalRenderPipelineGlobalSettings>;

    internal partial class UniversalRenderPipelineGlobalSettingsUI
    {
        // Temporary labelWidth indentation scope while we still use IMGUI
        class ImguiLabelWidthGUIScope : GUI.Scope
        {
            float m_LabelWidth;

            public ImguiLabelWidthGUIScope()
            {
                m_LabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 260;
            }

            protected override void CloseScope()
            {
                EditorGUIUtility.labelWidth = m_LabelWidth;
            }
        }

        public class DocumentationUrls
        {
            public static readonly string k_Volumes = "Volumes";
        }

        #region Rendering Layer Names

        public static VisualElement CreateImguiSections(SerializedUniversalRenderPipelineGlobalSettings serialized, Editor owner)
        {
            return new IMGUIContainer(() =>
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.BeginVertical();

                using (new ImguiLabelWidthGUIScope())
                using (var changedScope = new EditorGUI.ChangeCheckScope())
                {
                    RenderingLayerNamesSection.Draw(serialized, owner);
                    MiscSection.Draw(serialized, owner);

                    if (changedScope.changed)
                        serialized.serializedObject.ApplyModifiedProperties();
                }

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            });
        }

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


        #region Misc Settings

        private static readonly CED.IDrawer MiscSection =
            CED.Group((s, owner) =>
            {
#pragma warning disable 618 // Obsolete warning
                CoreEditorUtils.DrawSectionHeader(RenderPipelineGlobalSettingsUI.Styles.shaderStrippingSettingsLabel);
#pragma warning restore 618 // Obsolete warning
                using var indentScope = new EditorGUI.IndentLevelScope();
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(s.serializedObject.FindProperty("m_ShaderStrippingSetting"));
                EditorGUILayout.PropertyField(s.serializedObject.FindProperty("m_URPShaderStrippingSetting"));
                EditorGUILayout.Space();
            });

        #endregion
    }
}
