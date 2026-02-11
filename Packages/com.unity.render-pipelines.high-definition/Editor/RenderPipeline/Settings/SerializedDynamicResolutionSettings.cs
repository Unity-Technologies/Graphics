using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedDynamicResolutionSettings
    {
        public SerializedProperty root;

        public SerializedProperty enabled;
        public SerializedProperty useMipBias;
        public SerializedProperty DLSSPerfQualitySetting;
        public SerializedProperty DLSSInjectionPoint;
        public SerializedProperty DLSSUseOptimalSettings;
        public SerializedProperty DLSSSharpness;
        public SerializedProperty DLSSRenderPresetForQuality;
        public SerializedProperty DLSSRenderPresetForBalanced;
        public SerializedProperty DLSSRenderPresetForPerformance;
        public SerializedProperty DLSSRenderPresetForUltraPerformance;
        public SerializedProperty DLSSRenderPresetForDLAA;
        public SerializedProperty FSR2EnableSharpness;
        public SerializedProperty FSR2Sharpness;
        public SerializedProperty FSR2UseOptimalSettings;
        public SerializedProperty FSR2QualitySetting;
        public SerializedProperty FSR2InjectionPoint;
        public SerializedProperty fsrOverrideSharpness;
        public SerializedProperty fsrSharpness;
        public SerializedProperty maxPercentage;
        public SerializedProperty minPercentage;
        public SerializedProperty dynamicResType;
        public SerializedProperty softwareUpsamplingFilter;
        public SerializedProperty forcePercentage;
        public SerializedProperty forcedPercentage;
        public SerializedProperty lowResTransparencyMinimumThreshold;
        public SerializedProperty rayTracingHalfResThreshold;
        public SerializedProperty lowResSSGIMinimumThreshold;
        public SerializedProperty lowResVolumetricCloudsMinimumThreshold;
        public SerializedProperty advancedUpscalerNames;
        public SerializedProperty TAAUInjectionPoint;
        public SerializedProperty STPInjectionPoint;
        public SerializedProperty defaultInjectionPoint;
#if ENABLE_UPSCALER_FRAMEWORK
        public SerializedProperty IUpscalerOptions;
#endif
        public SerializedDynamicResolutionSettings(SerializedProperty root)
        {
            this.root = root;

            enabled = root.Find((GlobalDynamicResolutionSettings s) => s.enabled);
            useMipBias = root.Find((GlobalDynamicResolutionSettings s) => s.useMipBias);
            TAAUInjectionPoint = root.Find((GlobalDynamicResolutionSettings s) => s.TAAUInjectionPoint);
            STPInjectionPoint = root.Find((GlobalDynamicResolutionSettings s) => s.STPInjectionPoint);
            defaultInjectionPoint = root.Find((GlobalDynamicResolutionSettings s) => s.defaultInjectionPoint);
            advancedUpscalerNames = root.Find((GlobalDynamicResolutionSettings s) => s.advancedUpscalerNames);

            DLSSInjectionPoint = root.Find((GlobalDynamicResolutionSettings s) => s.DLSSInjectionPoint);
            DLSSPerfQualitySetting = root.Find((GlobalDynamicResolutionSettings s) => s.DLSSPerfQualitySetting);
            DLSSUseOptimalSettings = root.Find((GlobalDynamicResolutionSettings s) => s.DLSSUseOptimalSettings);
            DLSSSharpness = root.Find((GlobalDynamicResolutionSettings s) => s.DLSSSharpness);
            DLSSRenderPresetForQuality = root.Find((GlobalDynamicResolutionSettings s) => s.DLSSRenderPresetForQuality);
            DLSSRenderPresetForBalanced = root.Find((GlobalDynamicResolutionSettings s) => s.DLSSRenderPresetForBalanced);
            DLSSRenderPresetForPerformance = root.Find((GlobalDynamicResolutionSettings s) => s.DLSSRenderPresetForPerformance);
            DLSSRenderPresetForUltraPerformance = root.Find((GlobalDynamicResolutionSettings s) => s.DLSSRenderPresetForUltraPerformance);
            DLSSRenderPresetForDLAA = root.Find((GlobalDynamicResolutionSettings s) => s.DLSSRenderPresetForDLAA);

            FSR2EnableSharpness = root.Find((GlobalDynamicResolutionSettings s) => s.FSR2EnableSharpness);
            FSR2Sharpness = root.Find((GlobalDynamicResolutionSettings s) => s.FSR2Sharpness);
            FSR2UseOptimalSettings = root.Find((GlobalDynamicResolutionSettings s) => s.FSR2UseOptimalSettings);
            FSR2QualitySetting = root.Find((GlobalDynamicResolutionSettings s) => s.FSR2QualitySetting);
            FSR2InjectionPoint = root.Find((GlobalDynamicResolutionSettings s) => s.FSR2InjectionPoint);

            fsrOverrideSharpness = root.Find((GlobalDynamicResolutionSettings s) => s.fsrOverrideSharpness);
            fsrSharpness = root.Find((GlobalDynamicResolutionSettings s) => s.fsrSharpness);

#if ENABLE_UPSCALER_FRAMEWORK
            IUpscalerOptions = root.Find((GlobalDynamicResolutionSettings s) => s.upscalerOptions);
#endif

            maxPercentage = root.Find((GlobalDynamicResolutionSettings s) => s.maxPercentage);
            minPercentage = root.Find((GlobalDynamicResolutionSettings s) => s.minPercentage);
            dynamicResType = root.Find((GlobalDynamicResolutionSettings s) => s.dynResType);
            softwareUpsamplingFilter = root.Find((GlobalDynamicResolutionSettings s) => s.upsampleFilter);
            forcePercentage = root.Find((GlobalDynamicResolutionSettings s) => s.forceResolution);
            forcedPercentage = root.Find((GlobalDynamicResolutionSettings s) => s.forcedPercentage);
            lowResTransparencyMinimumThreshold = root.Find((GlobalDynamicResolutionSettings s) => s.lowResTransparencyMinimumThreshold);
            rayTracingHalfResThreshold = root.Find((GlobalDynamicResolutionSettings s) => s.rayTracingHalfResThreshold);
            lowResSSGIMinimumThreshold = root.Find((GlobalDynamicResolutionSettings s) => s.lowResSSGIMinimumThreshold);
            lowResVolumetricCloudsMinimumThreshold = root.Find((GlobalDynamicResolutionSettings s) => s.lowResVolumetricCloudsMinimumThreshold);
        }
    }
}
