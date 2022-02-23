using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    using CED = CoreEditorDrawer<SerializedUniversalRenderPipelineGlobalSettings>;

    internal partial class UniversalRenderPipelineGlobalSettingsUI
    {
        class DocumentationUrls
        {
            public static readonly string k_LightLayers = "Light-Layers";
        }

        enum Expandable
        {
            PostProcessing = 1 << 0,
            LightLayers = 1 << 1,
            Rendering = 1 << 2,
            Terrain = 1 << 3,
            Sprite = 1 << 4,
        }
        static readonly ExpandedState<Expandable, UniversalRenderPipelineGlobalSettings> k_ExpandedState = new(0, "URP");

        #region 2D/Sprite Settings
        static readonly CED.IDrawer SpriteSection =
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
            }
        );

        #endregion

        #region PostProcessing Settings

        static readonly CED.IDrawer PostProcessingSection =
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
            }
        );

        #endregion

        #region Rendering Layers

        static readonly CED.IDrawer LightLayersSection =
            CED.Group((serialized, owner) =>
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
        );

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

        #region Rendering Settings

        static readonly CED.IDrawer RenderingSection =
            CED.Group((serialized, owner) =>
            {
                using (var changed = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUILayout.PropertyField(serialized.useNativeRenderPass, Styles.RenderPassLabel);
                    EditorGUILayout.PropertyField(serialized.storeActionsOptimizationProperty, Styles.storeActionsOptimizationText);
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(serialized.shaderVariantLogLevel, RenderPipelineGlobalSettingsUI.Styles.shaderVariantLogLevelLabel);
                    EditorGUILayout.PropertyField(serialized.exportShaderVariants, RenderPipelineGlobalSettingsUI.Styles.exportShaderVariantsLabel);
                    EditorGUILayout.PropertyField(serialized.stripDebugVariants, Styles.stripDebugVariantsLabel);
                    EditorGUILayout.PropertyField(serialized.stripUnusedPostProcessingVariants, Styles.stripUnusedPostProcessingVariantsLabel);
                    EditorGUILayout.PropertyField(serialized.stripUnusedVariants, Styles.stripUnusedVariantsLabel);
                    if (changed.changed)
                    {
                        serialized.serializedObject?.ApplyModifiedProperties();
                        (serialized.serializedObject.targetObject as UniversalRenderPipelineGlobalSettings).UpdateRenderingLayerNames();
                    }
                }
            }
        );

        #endregion

        #region Terrain Settings
        static readonly CED.IDrawer TerrainSection =
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
            }
        );
        #endregion

        public static readonly CED.IDrawer Inspector = CED.Group(
                CED.FoldoutGroup(Styles.PostProcessingSettingsLabel, Expandable.PostProcessing, k_ExpandedState, FoldoutOption.None, PostProcessingSection),
                CED.FoldoutGroup(Styles.lightLayersLabel, Expandable.LightLayers, k_ExpandedState, FoldoutOption.None, OnContextClickLightLayerNames, LightLayersSection),
                CED.FoldoutGroup(Styles.renderingSettingsLabel, Expandable.Rendering, k_ExpandedState, FoldoutOption.None, RenderingSection),
                CED.FoldoutGroup(Styles.terrainSettingsLabel, Expandable.Terrain, k_ExpandedState, FoldoutOption.None, TerrainSection),
                CED.FoldoutGroup(Styles.spriteSettingsLabel, Expandable.Sprite, k_ExpandedState, FoldoutOption.None, SpriteSection)
            );
    }
}
