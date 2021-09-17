using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;
using UnityEditorInternal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(UniversalRenderPipelineAsset)), CanEditMultipleObjects]
    [MovedFrom("UnityEditor.Rendering.LWRP")] public class UniversalRenderPipelineAssetEditor : Editor
    {
        internal class Styles
        {
            // Groups
            public static GUIContent generalSettingsText = EditorGUIUtility.TrTextContent("General");
            public static GUIContent qualitySettingsText = EditorGUIUtility.TrTextContent("Quality");
            public static GUIContent lightingSettingsText = EditorGUIUtility.TrTextContent("Lighting");
            public static GUIContent shadowSettingsText = EditorGUIUtility.TrTextContent("Shadows");
            public static GUIContent postProcessingSettingsText = EditorGUIUtility.TrTextContent("Post-processing");
            public static GUIContent advancedSettingsText = EditorGUIUtility.TrTextContent("Advanced");
            public static GUIContent adaptivePerformanceText = EditorGUIUtility.TrTextContent("Adaptive Performance");

            // General
            public static GUIContent rendererHeaderText = EditorGUIUtility.TrTextContent("Renderer List", "Lists all the renderers available to this Render Pipeline Asset.");
            public static GUIContent rendererDefaultText = EditorGUIUtility.TrTextContent("Default", "This renderer is currently the default for the render pipeline.");
            public static GUIContent rendererSetDefaultText = EditorGUIUtility.TrTextContent("Set Default", "Makes this renderer the default for the render pipeline.");
            public static GUIContent rendererSettingsText = EditorGUIUtility.TrIconContent("_Menu", "Opens settings for this renderer.");
            public static GUIContent rendererMissingText = EditorGUIUtility.TrIconContent("console.warnicon.sml", "Renderer missing. Click this to select a new renderer.");
            public static GUIContent rendererDefaultMissingText = EditorGUIUtility.TrIconContent("console.erroricon.sml", "Default renderer missing. Click this to select a new renderer.");
            public static GUIContent requireDepthTextureText = EditorGUIUtility.TrTextContent("Depth Texture", "If enabled the pipeline will generate camera's depth that can be bound in shaders as _CameraDepthTexture.");
            public static GUIContent requireOpaqueTextureText = EditorGUIUtility.TrTextContent("Opaque Texture", "If enabled the pipeline will copy the screen to texture after opaque objects are drawn. For transparent objects this can be bound in shaders as _CameraOpaqueTexture.");
            public static GUIContent opaqueDownsamplingText = EditorGUIUtility.TrTextContent("Opaque Downsampling", "The downsampling method that is used for the opaque texture");
            public static GUIContent supportsTerrainHolesText = EditorGUIUtility.TrTextContent("Terrain Holes", "When disabled, Universal Rendering Pipeline removes all Terrain hole Shader variants when you build for the Unity Player. This decreases build time.");

            // Quality
            public static GUIContent hdrText = EditorGUIUtility.TrTextContent("HDR", "Controls the global HDR settings.");
            public static GUIContent msaaText = EditorGUIUtility.TrTextContent("Anti Aliasing (MSAA)", "Controls the global anti aliasing settings.");
            public static GUIContent renderScaleText = EditorGUIUtility.TrTextContent("Render Scale", "Scales the camera render target allowing the game to render at a resolution different than native resolution. UI is always rendered at native resolution.");

            // Main light
            public static GUIContent mainLightRenderingModeText = EditorGUIUtility.TrTextContent("Main Light", "Main light is the brightest directional light.");
            public static GUIContent supportsMainLightShadowsText = EditorGUIUtility.TrTextContent("Cast Shadows", "If enabled the main light can be a shadow casting light.");
            public static GUIContent mainLightShadowmapResolutionText = EditorGUIUtility.TrTextContent("Shadow Resolution", "Resolution of the main light shadowmap texture. If cascades are enabled, cascades will be packed into an atlas and this setting controls the maximum shadows atlas resolution.");

            // Additional lights
            public static GUIContent addditionalLightsRenderingModeText = EditorGUIUtility.TrTextContent("Additional Lights", "Additional lights support.");
            public static GUIContent perObjectLimit = EditorGUIUtility.TrTextContent("Per Object Limit", "Maximum amount of additional lights. These lights are sorted and culled per-object.");
            public static GUIContent supportsAdditionalShadowsText = EditorGUIUtility.TrTextContent("Cast Shadows", "If enabled shadows will be supported for spot lights.\n");
            public static GUIContent additionalLightsShadowmapResolution = EditorGUIUtility.TrTextContent("Shadow Resolution", "All additional lights are packed into a single shadowmap atlas. This setting controls the atlas size.");

            // Shadow settings
            public static GUIContent shadowDistanceText = EditorGUIUtility.TrTextContent("Max Distance", "Maximum shadow rendering distance.");
            public static GUIContent shadowCascadesText = EditorGUIUtility.TrTextContent("Cascade Count", "Number of cascade splits used for directional shadows.");
            public static GUIContent shadowDepthBias = EditorGUIUtility.TrTextContent("Depth Bias", "Controls the distance at which the shadows will be pushed away from the light. Useful for avoiding false self-shadowing artifacts.");
            public static GUIContent shadowNormalBias = EditorGUIUtility.TrTextContent("Normal Bias", "Controls distance at which the shadow casting surfaces will be shrunk along the surface normal. Useful for avoiding false self-shadowing artifacts.");
            public static GUIContent supportsSoftShadows = EditorGUIUtility.TrTextContent("Soft Shadows", "If enabled pipeline will perform shadow filtering. Otherwise all lights that cast shadows will fallback to perform a single shadow sample.");

            // Post-processing
            public static GUIContent colorGradingMode = EditorGUIUtility.TrTextContent("Grading Mode", "Defines how color grading will be applied. Operators will react differently depending on the mode.");
            public static GUIContent colorGradingLutSize = EditorGUIUtility.TrTextContent("LUT size", "Sets the size of the internal and external color grading lookup textures (LUTs).");
            public static string colorGradingModeWarning = "HDR rendering is required to use the high dynamic range color grading mode. The low dynamic range will be used instead.";
            public static string colorGradingModeSpecInfo = "The high dynamic range color grading mode works best on platforms that support floating point textures.";
            public static string colorGradingLutSizeWarning = "The minimal recommended LUT size for the high dynamic range color grading mode is 32. Using lower values will potentially result in color banding and posterization effects.";

            // Advanced settings
            public static GUIContent srpBatcher = EditorGUIUtility.TrTextContent("SRP Batcher", "If enabled, the render pipeline uses the SRP batcher.");
            public static GUIContent dynamicBatching = EditorGUIUtility.TrTextContent("Dynamic Batching", "If enabled, the render pipeline will batch drawcalls with few triangles together by copying their vertex buffers into a shared buffer on a per-frame basis.");
            public static GUIContent mixedLightingSupportLabel = EditorGUIUtility.TrTextContent("Mixed Lighting", "Makes the render pipeline include mixed-lighting Shader Variants in the build.");
            public static GUIContent debugLevel = EditorGUIUtility.TrTextContent("Debug Level", "Controls the level of debug information generated by the render pipeline. When Profiling is selected, the pipeline provides detailed profiling tags.");
            public static GUIContent shaderVariantLogLevel = EditorGUIUtility.TrTextContent("Shader Variant Log Level", "Controls the level logging in of shader variants information is outputted when a build is performed. Information will appear in the Unity console when the build finishes.");
            public static GUIContent volumeFrameworkUpdateMode = EditorGUIUtility.TrTextContent("Volume Update Mode", "Select how Unity updates Volumes: every frame or when triggered via scripting. In the Editor, Unity updates Volumes every frame when not in the Play mode.");

            // Adaptive performance settings
            public static GUIContent useAdaptivePerformance = EditorGUIUtility.TrTextContent("Use adaptive performance", "Allows Adaptive Performance to adjust rendering quality during runtime");

            // Renderer List Messages
            public static GUIContent rendererListDefaultMessage =
                EditorGUIUtility.TrTextContent("Cannot remove Default Renderer",
                    "Removal of the Default Renderer is not allowed. To remove, set another Renderer to be the new Default and then remove.");

            public static GUIContent rendererMissingDefaultMessage =
                EditorGUIUtility.TrTextContent("Missing Default Renderer\nThere is no default renderer assigned, so Unity can’t perform any rendering. Set another renderer to be the new Default, or assign a renderer to the Default slot.");
            public static GUIContent rendererMissingMessage =
                EditorGUIUtility.TrTextContent("Missing Renderer(s)\nOne or more renderers are either missing or unassigned.  Switching to these renderers at runtime can cause issues.");
            public static GUIContent rendererUnsupportedAPIMessage =
                EditorGUIUtility.TrTextContent("Some Renderer(s) in the Renderer List are incompatible with the Player Graphics APIs list.  Switching to these renderers at runtime can cause issues.\n\n");

            // Dropdown menu options
            public static string[] mainLightOptions = { "Disabled", "Per Pixel" };
            public static string[] volumeFrameworkUpdateOptions = { "Every Frame", "Via Scripting" };
            public static string[] opaqueDownsamplingOptions = {"None", "2x (Bilinear)", "4x (Box)", "4x (Bilinear)"};
        }

        SavedBool m_GeneralSettingsFoldout;
        SavedBool m_QualitySettingsFoldout;
        SavedBool m_LightingSettingsFoldout;
        SavedBool m_ShadowSettingsFoldout;
        SavedBool m_PostProcessingSettingsFoldout;
        SavedBool m_AdvancedSettingsFoldout;
        SavedBool m_AdaptivePerformanceFoldout;

        SerializedProperty m_RendererDataProp;
        SerializedProperty m_DefaultRendererProp;
        ReorderableList m_RendererDataList;

        SerializedProperty m_RequireDepthTextureProp;
        SerializedProperty m_RequireOpaqueTextureProp;
        SerializedProperty m_OpaqueDownsamplingProp;
        SerializedProperty m_SupportsTerrainHolesProp;

        SerializedProperty m_HDR;
        SerializedProperty m_MSAA;
        SerializedProperty m_RenderScale;

        SerializedProperty m_MainLightRenderingModeProp;
        SerializedProperty m_MainLightShadowsSupportedProp;
        SerializedProperty m_MainLightShadowmapResolutionProp;

        SerializedProperty m_AdditionalLightsRenderingModeProp;
        SerializedProperty m_AdditionalLightsPerObjectLimitProp;
        SerializedProperty m_AdditionalLightShadowsSupportedProp;
        SerializedProperty m_AdditionalLightShadowmapResolutionProp;

        SerializedProperty m_ShadowDistanceProp;
        SerializedProperty m_ShadowCascadeCountProp;
        SerializedProperty m_ShadowCascade2SplitProp;
        SerializedProperty m_ShadowCascade3SplitProp;
        SerializedProperty m_ShadowCascade4SplitProp;
        SerializedProperty m_ShadowDepthBiasProp;
        SerializedProperty m_ShadowNormalBiasProp;

        SerializedProperty m_SoftShadowsSupportedProp;

        SerializedProperty m_SRPBatcher;
        SerializedProperty m_SupportsDynamicBatching;
        SerializedProperty m_MixedLightingSupportedProp;
        SerializedProperty m_DebugLevelProp;

        SerializedProperty m_ShaderVariantLogLevel;
        SerializedProperty m_VolumeFrameworkUpdateModeProp;

        LightRenderingMode selectedLightRenderingMode;
        SerializedProperty m_ColorGradingMode;
        SerializedProperty m_ColorGradingLutSize;

        SerializedProperty m_UseAdaptivePerformance;
        EditorPrefBoolFlags<EditorUtils.Unit> m_State;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawGeneralSettings();
            DrawQualitySettings();
            DrawLightingSettings();
            DrawShadowSettings();
            DrawPostProcessingSettings();
            DrawAdvancedSettings();
#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
            DrawAdaptivePerformance();
#endif

            serializedObject.ApplyModifiedProperties();
        }

        void OnEnable()
        {
            m_GeneralSettingsFoldout = new SavedBool($"{target.GetType()}.GeneralSettingsFoldout", false);
            m_QualitySettingsFoldout = new SavedBool($"{target.GetType()}.QualitySettingsFoldout", false);
            m_LightingSettingsFoldout = new SavedBool($"{target.GetType()}.LightingSettingsFoldout", false);
            m_ShadowSettingsFoldout = new SavedBool($"{target.GetType()}.ShadowSettingsFoldout", false);
            m_PostProcessingSettingsFoldout = new SavedBool($"{target.GetType()}.PostProcessingSettingsFoldout", false);
            m_AdvancedSettingsFoldout = new SavedBool($"{target.GetType()}.AdvancedSettingsFoldout", false);
            m_AdaptivePerformanceFoldout = new SavedBool($"{target.GetType()}.AdaptivePerformanceFoldout", false);

            m_RendererDataProp = serializedObject.FindProperty("m_RendererDataList");
            m_DefaultRendererProp = serializedObject.FindProperty("m_DefaultRendererIndex");
            m_RendererDataList = new ReorderableList(serializedObject, m_RendererDataProp, true, true, true, true);

            DrawRendererListLayout(m_RendererDataList, m_RendererDataProp);

            m_RequireDepthTextureProp = serializedObject.FindProperty("m_RequireDepthTexture");
            m_RequireOpaqueTextureProp = serializedObject.FindProperty("m_RequireOpaqueTexture");
            m_OpaqueDownsamplingProp = serializedObject.FindProperty("m_OpaqueDownsampling");
            m_SupportsTerrainHolesProp = serializedObject.FindProperty("m_SupportsTerrainHoles");

            m_HDR = serializedObject.FindProperty("m_SupportsHDR");
            m_MSAA = serializedObject.FindProperty("m_MSAA");
            m_RenderScale = serializedObject.FindProperty("m_RenderScale");

            m_MainLightRenderingModeProp = serializedObject.FindProperty("m_MainLightRenderingMode");
            m_MainLightShadowsSupportedProp = serializedObject.FindProperty("m_MainLightShadowsSupported");
            m_MainLightShadowmapResolutionProp = serializedObject.FindProperty("m_MainLightShadowmapResolution");

            m_AdditionalLightsRenderingModeProp = serializedObject.FindProperty("m_AdditionalLightsRenderingMode");
            m_AdditionalLightsPerObjectLimitProp = serializedObject.FindProperty("m_AdditionalLightsPerObjectLimit");
            m_AdditionalLightShadowsSupportedProp = serializedObject.FindProperty("m_AdditionalLightShadowsSupported");
            m_AdditionalLightShadowmapResolutionProp = serializedObject.FindProperty("m_AdditionalLightsShadowmapResolution");

            m_ShadowDistanceProp = serializedObject.FindProperty("m_ShadowDistance");

            m_ShadowCascadeCountProp = serializedObject.FindProperty("m_ShadowCascadeCount");
            m_ShadowCascade2SplitProp = serializedObject.FindProperty("m_Cascade2Split");
            m_ShadowCascade3SplitProp = serializedObject.FindProperty("m_Cascade3Split");
            m_ShadowCascade4SplitProp = serializedObject.FindProperty("m_Cascade4Split");
            m_ShadowDepthBiasProp = serializedObject.FindProperty("m_ShadowDepthBias");
            m_ShadowNormalBiasProp = serializedObject.FindProperty("m_ShadowNormalBias");
            m_SoftShadowsSupportedProp = serializedObject.FindProperty("m_SoftShadowsSupported");

            m_SRPBatcher = serializedObject.FindProperty("m_UseSRPBatcher");
            m_SupportsDynamicBatching = serializedObject.FindProperty("m_SupportsDynamicBatching");
            m_MixedLightingSupportedProp = serializedObject.FindProperty("m_MixedLightingSupported");
            m_DebugLevelProp = serializedObject.FindProperty("m_DebugLevel");

            m_ShaderVariantLogLevel = serializedObject.FindProperty("m_ShaderVariantLogLevel");
            m_VolumeFrameworkUpdateModeProp = serializedObject.FindProperty("m_VolumeFrameworkUpdateMode");

            m_ColorGradingMode = serializedObject.FindProperty("m_ColorGradingMode");
            m_ColorGradingLutSize = serializedObject.FindProperty("m_ColorGradingLutSize");

            m_UseAdaptivePerformance = serializedObject.FindProperty("m_UseAdaptivePerformance");

            selectedLightRenderingMode = (LightRenderingMode)m_AdditionalLightsRenderingModeProp.intValue;

            string Key = "Universal_Shadow_Setting_Unit:UI_State";
            m_State = new EditorPrefBoolFlags<EditorUtils.Unit>(Key);
        }

        void DrawGeneralSettings()
        {
            m_GeneralSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_GeneralSettingsFoldout.value, Styles.generalSettingsText);
            if (m_GeneralSettingsFoldout.value)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.Space();
                EditorGUI.indentLevel--;
                m_RendererDataList.DoLayoutList();
                EditorGUI.indentLevel++;

                UniversalRenderPipelineAsset asset = target as UniversalRenderPipelineAsset;
                string unsupportedGraphicsApisMessage;

                if (!asset.ValidateRendererData(-1))
                    EditorGUILayout.HelpBox(Styles.rendererMissingDefaultMessage.text, MessageType.Error, true);
                else if (!asset.ValidateRendererDataList(true))
                    EditorGUILayout.HelpBox(Styles.rendererMissingMessage.text, MessageType.Warning, true);
                else if (!ValidateRendererGraphicsAPIs(asset, out unsupportedGraphicsApisMessage))
                    EditorGUILayout.HelpBox(Styles.rendererUnsupportedAPIMessage.text + unsupportedGraphicsApisMessage, MessageType.Warning, true);

                EditorGUILayout.PropertyField(m_RequireDepthTextureProp, Styles.requireDepthTextureText);
                EditorGUILayout.PropertyField(m_RequireOpaqueTextureProp, Styles.requireOpaqueTextureText);
                EditorGUI.indentLevel++;
                EditorGUI.BeginDisabledGroup(!m_RequireOpaqueTextureProp.boolValue);
                EditorGUILayout.PropertyField(m_OpaqueDownsamplingProp, Styles.opaqueDownsamplingText);
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
                EditorGUILayout.PropertyField(m_SupportsTerrainHolesProp, Styles.supportsTerrainHolesText);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawQualitySettings()
        {
            m_QualitySettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_QualitySettingsFoldout.value, Styles.qualitySettingsText);
            if (m_QualitySettingsFoldout.value)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_HDR, Styles.hdrText);
                EditorGUILayout.PropertyField(m_MSAA, Styles.msaaText);
                m_RenderScale.floatValue = EditorGUILayout.Slider(Styles.renderScaleText, m_RenderScale.floatValue, UniversalRenderPipeline.minRenderScale, UniversalRenderPipeline.maxRenderScale);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawLightingSettings()
        {
            m_LightingSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_LightingSettingsFoldout.value, Styles.lightingSettingsText);
            if (m_LightingSettingsFoldout.value)
            {
                EditorGUI.indentLevel++;

                // Main Light
                bool disableGroup = false;
                EditorGUI.BeginDisabledGroup(disableGroup);
                CoreEditorUtils.DrawPopup(Styles.mainLightRenderingModeText, m_MainLightRenderingModeProp, Styles.mainLightOptions);
                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel++;
                disableGroup |= !m_MainLightRenderingModeProp.boolValue;

                EditorGUI.BeginDisabledGroup(disableGroup);
                EditorGUILayout.PropertyField(m_MainLightShadowsSupportedProp, Styles.supportsMainLightShadowsText);
                EditorGUI.EndDisabledGroup();

                disableGroup |= !m_MainLightShadowsSupportedProp.boolValue;
                EditorGUI.BeginDisabledGroup(disableGroup);
                EditorGUILayout.PropertyField(m_MainLightShadowmapResolutionProp, Styles.mainLightShadowmapResolutionText);
                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                // Additional light
                selectedLightRenderingMode = (LightRenderingMode)EditorGUILayout.EnumPopup(Styles.addditionalLightsRenderingModeText, selectedLightRenderingMode);
                m_AdditionalLightsRenderingModeProp.intValue = (int)selectedLightRenderingMode;
                EditorGUI.indentLevel++;

                disableGroup = m_AdditionalLightsRenderingModeProp.intValue == (int)LightRenderingMode.Disabled;
                EditorGUI.BeginDisabledGroup(disableGroup);
                m_AdditionalLightsPerObjectLimitProp.intValue = EditorGUILayout.IntSlider(Styles.perObjectLimit, m_AdditionalLightsPerObjectLimitProp.intValue, 0, UniversalRenderPipeline.maxPerObjectLights);
                EditorGUI.EndDisabledGroup();

                disableGroup |= (m_AdditionalLightsPerObjectLimitProp.intValue == 0 || m_AdditionalLightsRenderingModeProp.intValue != (int)LightRenderingMode.PerPixel);
                EditorGUI.BeginDisabledGroup(disableGroup);
                EditorGUILayout.PropertyField(m_AdditionalLightShadowsSupportedProp, Styles.supportsAdditionalShadowsText);
                EditorGUI.EndDisabledGroup();

                disableGroup |= !m_AdditionalLightShadowsSupportedProp.boolValue;
                EditorGUI.BeginDisabledGroup(disableGroup);
                EditorGUILayout.PropertyField(m_AdditionalLightShadowmapResolutionProp, Styles.additionalLightsShadowmapResolution);
                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel--;
                EditorGUI.indentLevel--;

                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawShadowSettings()
        {
            m_ShadowSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShadowSettingsFoldout.value, Styles.shadowSettingsText);
            if (m_ShadowSettingsFoldout.value)
            {
                EditorGUI.indentLevel++;
                m_ShadowDistanceProp.floatValue = Mathf.Max(0.0f, EditorGUILayout.FloatField(Styles.shadowDistanceText, m_ShadowDistanceProp.floatValue));
                EditorUtils.Unit unit = EditorUtils.Unit.Metric;
                if (m_ShadowCascadeCountProp.intValue != 0)
                {
                    EditorGUI.BeginChangeCheck();
                    unit = (EditorUtils.Unit)EditorGUILayout.EnumPopup(EditorGUIUtility.TrTextContent("Working Unit", "Except Max Distance which will be still in meter."), m_State.value);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_State.value = unit;
                    }
                }

                UniversalRenderPipelineAsset asset = target as UniversalRenderPipelineAsset;
                EditorGUILayout.IntSlider(m_ShadowCascadeCountProp, UniversalRenderPipelineAsset.k_ShadowCascadeMinCount, UniversalRenderPipelineAsset.k_ShadowCascadeMaxCount, Styles.shadowCascadesText);

                int cascadeCount = m_ShadowCascadeCountProp.intValue;
                EditorGUI.indentLevel++;
                if (cascadeCount == 4)
                    EditorUtils.DrawCascadeSplitGUI<Vector3>(ref m_ShadowCascade4SplitProp, m_ShadowDistanceProp.floatValue, cascadeCount, unit);
                else if (cascadeCount == 3)
                    EditorUtils.DrawCascadeSplitGUI<Vector2>(ref m_ShadowCascade3SplitProp, m_ShadowDistanceProp.floatValue, cascadeCount, unit);
                else if (cascadeCount == 2)
                    EditorUtils.DrawCascadeSplitGUI<float>(ref m_ShadowCascade2SplitProp, m_ShadowDistanceProp.floatValue, cascadeCount, unit);
                else if (cascadeCount == 1)
                    EditorUtils.DrawCascadeSplitGUI<float>(ref m_ShadowCascade2SplitProp, m_ShadowDistanceProp.floatValue, cascadeCount, unit);

                m_ShadowDepthBiasProp.floatValue = EditorGUILayout.Slider(Styles.shadowDepthBias, m_ShadowDepthBiasProp.floatValue, 0.0f, UniversalRenderPipeline.maxShadowBias);
                m_ShadowNormalBiasProp.floatValue = EditorGUILayout.Slider(Styles.shadowNormalBias, m_ShadowNormalBiasProp.floatValue, 0.0f, UniversalRenderPipeline.maxShadowBias);
                EditorGUILayout.PropertyField(m_SoftShadowsSupportedProp, Styles.supportsSoftShadows);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawPostProcessingSettings()
        {
            m_PostProcessingSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_PostProcessingSettingsFoldout.value, Styles.postProcessingSettingsText);
            if (m_PostProcessingSettingsFoldout.value)
            {
                bool isHdrOn = m_HDR.boolValue;

                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(m_ColorGradingMode, Styles.colorGradingMode);
                if (!isHdrOn && m_ColorGradingMode.intValue == (int)ColorGradingMode.HighDynamicRange)
                    EditorGUILayout.HelpBox(Styles.colorGradingModeWarning, MessageType.Warning);
                else if (isHdrOn && m_ColorGradingMode.intValue == (int)ColorGradingMode.HighDynamicRange)
                    EditorGUILayout.HelpBox(Styles.colorGradingModeSpecInfo, MessageType.Info);

                EditorGUILayout.DelayedIntField(m_ColorGradingLutSize, Styles.colorGradingLutSize);
                m_ColorGradingLutSize.intValue = Mathf.Clamp(m_ColorGradingLutSize.intValue, UniversalRenderPipelineAsset.k_MinLutSize, UniversalRenderPipelineAsset.k_MaxLutSize);
                if (isHdrOn && m_ColorGradingMode.intValue == (int)ColorGradingMode.HighDynamicRange && m_ColorGradingLutSize.intValue < 32)
                    EditorGUILayout.HelpBox(Styles.colorGradingLutSizeWarning, MessageType.Warning);

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawAdvancedSettings()
        {
            m_AdvancedSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_AdvancedSettingsFoldout.value, Styles.advancedSettingsText);
            if (m_AdvancedSettingsFoldout.value)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_SRPBatcher, Styles.srpBatcher);
                EditorGUILayout.PropertyField(m_SupportsDynamicBatching, Styles.dynamicBatching);
                EditorGUILayout.PropertyField(m_MixedLightingSupportedProp, Styles.mixedLightingSupportLabel);
                EditorGUILayout.PropertyField(m_DebugLevelProp, Styles.debugLevel);
                EditorGUILayout.PropertyField(m_ShaderVariantLogLevel, Styles.shaderVariantLogLevel);
                CoreEditorUtils.DrawPopup(Styles.volumeFrameworkUpdateMode, m_VolumeFrameworkUpdateModeProp, Styles.volumeFrameworkUpdateOptions);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawAdaptivePerformance()
        {
            m_AdaptivePerformanceFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_AdaptivePerformanceFoldout.value, Styles.adaptivePerformanceText);
            if (m_AdaptivePerformanceFoldout.value)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_UseAdaptivePerformance, Styles.useAdaptivePerformance);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawRendererListLayout(ReorderableList list, SerializedProperty prop)
        {
           list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                rect.y += 2;
                Rect indexRect = new Rect(rect.x, rect.y, 14, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(indexRect, index.ToString());
                Rect objRect = new Rect(rect.x + indexRect.width, rect.y, rect.width - 134, EditorGUIUtility.singleLineHeight);

                EditorGUI.BeginChangeCheck();
                EditorGUI.ObjectField(objRect, prop.GetArrayElementAtIndex(index), GUIContent.none);
                if (EditorGUI.EndChangeCheck())
                    EditorUtility.SetDirty(target);

                Rect defaultButton = new Rect(rect.width - 75, rect.y, 86, EditorGUIUtility.singleLineHeight);
                var defaultRenderer = m_DefaultRendererProp.intValue;
                GUI.enabled = index != defaultRenderer;
                if (GUI.Button(defaultButton, !GUI.enabled ? Styles.rendererDefaultText : Styles.rendererSetDefaultText))
                {
                    m_DefaultRendererProp.intValue = index;
                    EditorUtility.SetDirty(target);
                }
                GUI.enabled = true;

                Rect selectRect = new Rect(rect.x + rect.width - 24, rect.y, 24, EditorGUIUtility.singleLineHeight);

                UniversalRenderPipelineAsset asset = target as UniversalRenderPipelineAsset;

                if (asset.ValidateRendererData(index))
                {
                    if (GUI.Button(selectRect, Styles.rendererSettingsText))
                    {
                        Selection.SetActiveObjectWithContext(prop.GetArrayElementAtIndex(index).objectReferenceValue,
                            null);
                    }
                }
                else // Missing ScriptableRendererData
                {
                    if (GUI.Button(selectRect, index == defaultRenderer ? Styles.rendererDefaultMissingText : Styles.rendererMissingText))
                    {
                        EditorGUIUtility.ShowObjectPicker<ScriptableRendererData>(null, false, null, index);
                    }
                }

                // If object selector chose an object, assign it to the correct ScriptableRendererData slot.
                if (Event.current.commandName == "ObjectSelectorUpdated" && EditorGUIUtility.GetObjectPickerControlID() == index)
                {
                    prop.GetArrayElementAtIndex(index).objectReferenceValue = EditorGUIUtility.GetObjectPickerObject();
                }
            };

            list.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, Styles.rendererHeaderText);
            };

            list.onCanRemoveCallback = li => { return li.count > 1; };

            list.onRemoveCallback = li =>
            {
                bool shouldUpdateIndex = false;
                // Checking so that the user is not deleting  the default renderer
                if (li.index != m_DefaultRendererProp.intValue)
                {
                    // Need to add the undo to the removal of our assets here, for it to work properly.
                    Undo.RecordObject(target, $"Deleting renderer at index {li.index}");

                    if (prop.GetArrayElementAtIndex(li.index).objectReferenceValue == null)
                    {
                        shouldUpdateIndex = true;
                    }
                    prop.DeleteArrayElementAtIndex(li.index);
                }
                else
                {
                    EditorUtility.DisplayDialog(Styles.rendererListDefaultMessage.text, Styles.rendererListDefaultMessage.tooltip,
                        "Close");
                }

                if (shouldUpdateIndex)
                {
                    UpdateDefaultRendererValue(li.index);
                }

                EditorUtility.SetDirty(target);
            };

            list.onReorderCallbackWithDetails += (reorderableList, index, newIndex) =>
            {
                // Need to update the default renderer index
                UpdateDefaultRendererValue(index, newIndex);
            };
        }

        void UpdateDefaultRendererValue(int index)
        {
            // If the index that is being removed is lower than the default renderer value,
            // the default prop value needs to be one lower.
            if (index < m_DefaultRendererProp.intValue)
            {
                m_DefaultRendererProp.intValue--;
            }
        }

        void UpdateDefaultRendererValue(int prevIndex, int newIndex)
        {
            // If we are moving the index that is the same as the default renderer we need to update that
            if (prevIndex == m_DefaultRendererProp.intValue)
            {
                m_DefaultRendererProp.intValue = newIndex;
            }
            // If newIndex is the same as default
            // then we need to know if newIndex is above or below the default index
            else if (newIndex == m_DefaultRendererProp.intValue)
            {
                m_DefaultRendererProp.intValue += prevIndex > newIndex ? 1 : -1;
            }
            // If the old index is lower than default renderer and
            // the new index is higher then we need to move the default renderer index one lower
            else if (prevIndex < m_DefaultRendererProp.intValue && newIndex > m_DefaultRendererProp.intValue)
            {
                m_DefaultRendererProp.intValue--;
            }
            else if (newIndex < m_DefaultRendererProp.intValue && prevIndex > m_DefaultRendererProp.intValue)
            {
                m_DefaultRendererProp.intValue++;
            }
        }
        bool ValidateRendererGraphicsAPIs(UniversalRenderPipelineAsset pipelineAsset, out string unsupportedGraphicsApisMessage)
        {
            // Check the list of Renderers against all Graphics APIs the player is built with.
            unsupportedGraphicsApisMessage = null;

            BuildTarget platform = EditorUserBuildSettings.activeBuildTarget;
            GraphicsDeviceType[] graphicsAPIs = PlayerSettings.GetGraphicsAPIs(platform);
            int rendererCount = pipelineAsset.m_RendererDataList.Length;

            for (int i = 0; i < rendererCount; i++)
            {
                ScriptableRenderer renderer = pipelineAsset.GetRenderer(i);
                if (renderer == null)
                    continue;

                GraphicsDeviceType[] unsupportedAPIs = renderer.unsupportedGraphicsDeviceTypes;

                for (int apiIndex = 0; apiIndex < unsupportedAPIs.Length; apiIndex++)
                {
                    if (System.Array.FindIndex(graphicsAPIs, element => element == unsupportedAPIs[apiIndex]) >= 0)
                        unsupportedGraphicsApisMessage += System.String.Format("{0} at index {1} does not support {2}.\n", renderer, i, unsupportedAPIs[apiIndex]);
                }
            }

            return unsupportedGraphicsApisMessage == null;
        }
    }
}
