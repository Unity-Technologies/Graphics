using UnityEditorInternal;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal class SerializedUniversalRenderPipelineAsset
    {
        public SerializedProperty rendererDataProp { get; }
        public SerializedProperty defaultRendererProp { get; }

        public SerializedProperty requireDepthTextureProp { get; }
        public SerializedProperty requireOpaqueTextureProp { get; }
        public SerializedProperty opaqueDownsamplingProp { get; }
        public SerializedProperty supportsTerrainHolesProp { get; }
        public SerializedProperty storeActionsOptimizationProperty { get; }

        public SerializedProperty hdr { get; }
        public SerializedProperty msaa { get; }
        public SerializedProperty renderScale { get; }

        public SerializedProperty mainLightRenderingModeProp { get; }
        public SerializedProperty mainLightShadowsSupportedProp { get; }
        public SerializedProperty mainLightShadowmapResolutionProp { get; }

        public SerializedProperty additionalLightsRenderingModeProp { get; }
        public SerializedProperty additionalLightsPerObjectLimitProp { get; }
        public SerializedProperty additionalLightShadowsSupportedProp { get; }
        public SerializedProperty additionalLightShadowmapResolutionProp { get; }

        public SerializedProperty additionalLightsShadowResolutionTierLowProp { get; }
        public SerializedProperty additionalLightsShadowResolutionTierMediumProp { get; }
        public SerializedProperty additionalLightsShadowResolutionTierHighProp { get; }

        public SerializedProperty additionalLightCookieResolutionProp { get; }
        public SerializedProperty additionalLightCookieFormatProp { get; }

        public SerializedProperty reflectionProbeBlendingProp { get; }
        public SerializedProperty reflectionProbeBoxProjectionProp { get; }

        public SerializedProperty shadowDistanceProp { get; }
        public SerializedProperty shadowCascadeCountProp { get; }
        public SerializedProperty shadowCascade2SplitProp { get; }
        public SerializedProperty shadowCascade3SplitProp { get; }
        public SerializedProperty shadowCascade4SplitProp { get; }
        public SerializedProperty shadowCascadeBorderProp { get; }
        public SerializedProperty shadowDepthBiasProp { get; }
        public SerializedProperty shadowNormalBiasProp { get; }
        public SerializedProperty softShadowsSupportedProp { get; }
        public SerializedProperty conservativeEnclosingSphereProp { get; }

        public SerializedProperty srpBatcher { get; }
        public SerializedProperty supportsDynamicBatching { get; }
        public SerializedProperty mixedLightingSupportedProp { get; }
        public SerializedProperty supportsLightLayers { get; }
        public SerializedProperty debugLevelProp { get; }

        public SerializedProperty shaderVariantLogLevel { get; }
        public SerializedProperty volumeFrameworkUpdateModeProp { get; }

        public SerializedProperty colorGradingMode { get; }
        public SerializedProperty colorGradingLutSize { get; }
        public SerializedProperty useFastSRGBLinearConversion { get; }

#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
        public SerializedProperty useAdaptivePerformance { get; }
#endif
        public UniversalRenderPipelineAsset asset { get; }
        public SerializedObject serializedObject { get; }

        public EditorPrefBoolFlags<EditorUtils.Unit> state;

        public SerializedUniversalRenderPipelineAsset(SerializedObject serializedObject)
        {
            asset = serializedObject.targetObject as UniversalRenderPipelineAsset;
            this.serializedObject = serializedObject;

            requireDepthTextureProp = serializedObject.FindProperty("m_RequireDepthTexture");
            requireOpaqueTextureProp = serializedObject.FindProperty("m_RequireOpaqueTexture");
            opaqueDownsamplingProp = serializedObject.FindProperty("m_OpaqueDownsampling");
            supportsTerrainHolesProp = serializedObject.FindProperty("m_SupportsTerrainHoles");

            hdr = serializedObject.FindProperty("m_SupportsHDR");
            msaa = serializedObject.FindProperty("m_MSAA");
            renderScale = serializedObject.FindProperty("m_RenderScale");

            mainLightRenderingModeProp = serializedObject.FindProperty("m_MainLightRenderingMode");
            mainLightShadowsSupportedProp = serializedObject.FindProperty("m_MainLightShadowsSupported");
            mainLightShadowmapResolutionProp = serializedObject.FindProperty("m_MainLightShadowmapResolution");

            additionalLightsRenderingModeProp = serializedObject.FindProperty("m_AdditionalLightsRenderingMode");
            additionalLightsPerObjectLimitProp = serializedObject.FindProperty("m_AdditionalLightsPerObjectLimit");
            additionalLightShadowsSupportedProp = serializedObject.FindProperty("m_AdditionalLightShadowsSupported");
            additionalLightShadowmapResolutionProp = serializedObject.FindProperty("m_AdditionalLightsShadowmapResolution");

            additionalLightsShadowResolutionTierLowProp = serializedObject.FindProperty("m_AdditionalLightsShadowResolutionTierLow");
            additionalLightsShadowResolutionTierMediumProp = serializedObject.FindProperty("m_AdditionalLightsShadowResolutionTierMedium");
            additionalLightsShadowResolutionTierHighProp = serializedObject.FindProperty("m_AdditionalLightsShadowResolutionTierHigh");

            additionalLightCookieResolutionProp = serializedObject.FindProperty("m_AdditionalLightsCookieResolution");
            additionalLightCookieFormatProp = serializedObject.FindProperty("m_AdditionalLightsCookieFormat");

            reflectionProbeBlendingProp = serializedObject.FindProperty("m_ReflectionProbeBlending");
            reflectionProbeBoxProjectionProp = serializedObject.FindProperty("m_ReflectionProbeBoxProjection");

            shadowDistanceProp = serializedObject.FindProperty("m_ShadowDistance");

            shadowCascadeCountProp = serializedObject.FindProperty("m_ShadowCascadeCount");
            shadowCascade2SplitProp = serializedObject.FindProperty("m_Cascade2Split");
            shadowCascade3SplitProp = serializedObject.FindProperty("m_Cascade3Split");
            shadowCascade4SplitProp = serializedObject.FindProperty("m_Cascade4Split");
            shadowCascadeBorderProp = serializedObject.FindProperty("m_CascadeBorder");
            shadowDepthBiasProp = serializedObject.FindProperty("m_ShadowDepthBias");
            shadowNormalBiasProp = serializedObject.FindProperty("m_ShadowNormalBias");
            softShadowsSupportedProp = serializedObject.FindProperty("m_SoftShadowsSupported");
            conservativeEnclosingSphereProp = serializedObject.FindProperty("m_ConservativeEnclosingSphere");

            srpBatcher = serializedObject.FindProperty("m_UseSRPBatcher");
            supportsDynamicBatching = serializedObject.FindProperty("m_SupportsDynamicBatching");
            mixedLightingSupportedProp = serializedObject.FindProperty("m_MixedLightingSupported");
            supportsLightLayers = serializedObject.FindProperty("m_SupportsLightLayers");
            debugLevelProp = serializedObject.FindProperty("m_DebugLevel");

            shaderVariantLogLevel = serializedObject.FindProperty("m_ShaderVariantLogLevel");
            volumeFrameworkUpdateModeProp = serializedObject.FindProperty("m_VolumeFrameworkUpdateMode");

            storeActionsOptimizationProperty = serializedObject.FindProperty("m_StoreActionsOptimization");

            colorGradingMode = serializedObject.FindProperty("m_ColorGradingMode");
            colorGradingLutSize = serializedObject.FindProperty("m_ColorGradingLutSize");

            useFastSRGBLinearConversion = serializedObject.FindProperty("m_UseFastSRGBLinearConversion");

#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
            useAdaptivePerformance = serializedObject.FindProperty("m_UseAdaptivePerformance");
#endif
            string Key = "Universal_Shadow_Setting_Unit:UI_State";
            state = new EditorPrefBoolFlags<EditorUtils.Unit>(Key);
        }

        /// <summary>
        /// Refreshes the serialized object
        /// </summary>
        public void Update()
        {
            serializedObject.Update();
        }

        /// <summary>
        /// Applies the modified properties of the serialized object
        /// </summary>
        public void Apply()
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
