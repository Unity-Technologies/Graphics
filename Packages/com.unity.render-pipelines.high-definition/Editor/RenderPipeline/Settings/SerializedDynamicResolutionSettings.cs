using UnityEditor.Rendering;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedDynamicResolutionSettings
    {
        public SerializedProperty root;

        public SerializedProperty enabled;
        public SerializedProperty enableDLSS;
        public SerializedProperty useMipBias;
        public SerializedProperty DLSSPerfQualitySetting;
        public SerializedProperty DLSSInjectionPoint;
        public SerializedProperty DLSSUseOptimalSettings;
        public SerializedProperty DLSSSharpness;
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

        public SerializedDynamicResolutionSettings(SerializedProperty root)
        {
            this.root = root;

            enabled = root.Find((GlobalDynamicResolutionSettings s) => s.enabled);
            enableDLSS = root.Find((GlobalDynamicResolutionSettings s) => s.enableDLSS);
            useMipBias = root.Find((GlobalDynamicResolutionSettings s) => s.useMipBias);
            DLSSPerfQualitySetting = root.Find((GlobalDynamicResolutionSettings s) => s.DLSSPerfQualitySetting);
            DLSSInjectionPoint = root.Find((GlobalDynamicResolutionSettings s) => s.DLSSInjectionPoint);
            DLSSUseOptimalSettings = root.Find((GlobalDynamicResolutionSettings s) => s.DLSSUseOptimalSettings);
            DLSSSharpness = root.Find((GlobalDynamicResolutionSettings s) => s.DLSSSharpness);
            fsrOverrideSharpness = root.Find((GlobalDynamicResolutionSettings s) => s.fsrOverrideSharpness);
            fsrSharpness = root.Find((GlobalDynamicResolutionSettings s) => s.fsrSharpness);
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
