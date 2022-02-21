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

        #region 2D/Sprite Settings
        static readonly CED.IDrawer SpriteSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.spriteSettingsLabel)),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group((serialized, owner) =>
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    using (var changed = new EditorGUI.ChangeCheckScope())
                    {
                        EditorGUILayout.PropertyField(serialized.transparencySortMode, Styles.transparencySortMode);

                        using (new EditorGUI.DisabledGroupScope(serialized.transparencySortMode.intValue != (int)TransparencySortMode.CustomAxis))
                            EditorGUILayout.PropertyField(serialized.transparencySortAxis, Styles.transparencySortAxis);

                        EditorGUILayout.PropertyField(serialized.defaultSpriteMaterialType, Styles.defaultMaterialType);
                        if (serialized.defaultSpriteMaterialType.intValue == (int)UniversalRenderPipelineGlobalSettings.SpriteDefaultMaterialType.Custom)
                            EditorGUILayout.PropertyField(serialized.defaultSpriteCustomMaterial, Styles.defaultCustomMaterial);
                        if (changed.changed)
                        {
                            serialized.serializedObject?.ApplyModifiedProperties();
                            (serialized.serializedObject.targetObject as UniversalRenderPipelineGlobalSettings).UpdateRenderingLayerNames();
                        }
                    }
                }
            }),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );

        #endregion

        #region Terrain Settings
        static readonly CED.IDrawer TerrainSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.terrainSettingsLabel)),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group((serialized, owner) =>
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    using (var changed = new EditorGUI.ChangeCheckScope())
                    {
                        EditorGUILayout.PropertyField(serialized.supportsTerrainHolesProp, Styles.supportsTerrainHolesText);
                        if (changed.changed)
                        {
                            serialized.serializedObject?.ApplyModifiedProperties();
                            (serialized.serializedObject.targetObject as UniversalRenderPipelineGlobalSettings).UpdateRenderingLayerNames();
                        }
                    }
                }
            }),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );
        #endregion

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

        #region PostProcessing Settings

        static readonly CED.IDrawer PostProcessingSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.PostProcessingSettingsLabel)),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group((serialized, owner) =>
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    using (var changed = new EditorGUI.ChangeCheckScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        var postProcessIncluded = EditorGUILayout.Toggle(Styles.PostProcessIncluded, serialized.postProcessData.objectReferenceValue != null);
                        if (EditorGUI.EndChangeCheck())
                        {
                            serialized.postProcessData.objectReferenceValue = postProcessIncluded ? PostProcessData.GetDefaultPostProcessData() : null;
                        }
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(serialized.postProcessData, Styles.PostProcessLabel);
                        EditorGUI.indentLevel--;

                        EditorGUILayout.PropertyField(serialized.colorGradingMode, Styles.colorGradingMode);

                        EditorGUILayout.DelayedIntField(serialized.colorGradingLutSize, Styles.colorGradingLutSize);
                        serialized.colorGradingLutSize.intValue = Mathf.Clamp(serialized.colorGradingLutSize.intValue, UniversalRenderPipelineGlobalSettings.k_MinLutSize, UniversalRenderPipelineGlobalSettings.k_MaxLutSize);

                        EditorGUILayout.PropertyField(serialized.useFastSRGBLinearConversion, Styles.useFastSRGBLinearConversion);
                        CoreEditorUtils.DrawPopup(Styles.volumeFrameworkUpdateMode, serialized.volumeFrameworkUpdateModeProp, Styles.volumeFrameworkUpdateOptions);
                        if (changed.changed)
                        {
                            serialized.serializedObject?.ApplyModifiedProperties();
                            (serialized.serializedObject.targetObject as UniversalRenderPipelineGlobalSettings).UpdateRenderingLayerNames();
                        }
                    }
                }
            }),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );

        #endregion

        #region Misc Settings

        static readonly CED.IDrawer MiscSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.miscSettingsLabel)),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group((serialized, owner) =>
            {
                using (var changed = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUILayout.PropertyField(serialized.useNativeRenderPass, Styles.RenderPassLabel);
                    EditorGUILayout.PropertyField(serialized.storeActionsOptimizationProperty, Styles.storeActionsOptimizationText);
                    EditorGUILayout.Space();
                    if (changed.changed)
                    {
                        serialized.serializedObject?.ApplyModifiedProperties();
                        (serialized.serializedObject.targetObject as UniversalRenderPipelineGlobalSettings).UpdateRenderingLayerNames();
                    }
                }
            }),
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
                SpriteSection,
                TerrainSection,
                LightLayerNamesSection,
                PostProcessingSection,
                MiscSection
            );
    }
}
