using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    using CED = CoreEditorDrawer<SerializedUniversalRenderPipelineGlobalSettings>;

    internal partial class UniversalRenderPipelineGlobalSettingsUI
    {
        public class DocumentationUrls
        {
            public static readonly string k_LightLayers = "Light-Layers";
        }

        #region Rendering Layer Names

        static readonly CED.IDrawer LightLayerNamesSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.lightLayersLabel, contextAction: pos => OnContextClickLightLayerNames(pos, serialized))),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawLightLayerNames),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );

        static void DrawLightLayerNames(SerializedUniversalRenderPipelineGlobalSettings serialized, Editor owner)
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
                        if (serialized.serializedObject?.targetObject is UniversalRenderPipelineGlobalSettings urpGlobalSettings)
                            urpGlobalSettings.UpdateRenderingLayerNames();
                    }
                }
            }
        }

        static void OnContextClickLightLayerNames(Vector2 position, SerializedUniversalRenderPipelineGlobalSettings serialized)
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
                    EditorGUILayout.PropertyField(universalRenderPipelineGlobalSettings.stripDebugVariants, Styles.stripDebugVariantsLabel);
                    EditorGUILayout.PropertyField(universalRenderPipelineGlobalSettings.stripUnusedPostProcessingVariants, Styles.stripUnusedPostProcessingVariantsLabel);
                    EditorGUILayout.PropertyField(universalRenderPipelineGlobalSettings.stripUnusedVariants, Styles.stripUnusedVariantsLabel);
                }
            }))),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );

        #endregion

        public static readonly CED.IDrawer Inspector = CED.Group(
                LightLayerNamesSection,
                MiscSection
            );
    }
}
