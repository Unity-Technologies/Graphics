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

        static readonly CED.IDrawer MiscSection = CED.Group(
            CED.Group((serialized, owner) => RenderPipelineGlobalSettingsUI.DrawShaderStrippingSettings(serialized, owner, CoreEditorDrawer<ISerializedRenderPipelineGlobalSettings>.Group((s, e) =>
            {
                if (s is SerializedUniversalRenderPipelineGlobalSettings universalRenderPipelineGlobalSettings)
                {
                    EditorGUILayout.PropertyField(universalRenderPipelineGlobalSettings.stripUnusedPostProcessingVariants, Styles.stripUnusedPostProcessingVariantsLabel);
                    EditorGUILayout.PropertyField(universalRenderPipelineGlobalSettings.stripUnusedVariants, Styles.stripUnusedVariantsLabel);
                    EditorGUILayout.PropertyField(serialized.stripScreenCoordOverrideVariants, Styles.stripScreenCoordOverrideVariants);
                }
            }))),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );
        #endregion

        public static readonly CED.IDrawer Inspector = CED.Group(
                RenderingLayerNamesSection,
                MiscSection
        );
    }
}
