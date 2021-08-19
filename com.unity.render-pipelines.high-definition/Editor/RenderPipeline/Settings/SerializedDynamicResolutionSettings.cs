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
        public SerializedProperty DLSSUseOptimalSettings;
        public SerializedProperty DLSSSharpness;
        public SerializedProperty maxPercentage;
        public SerializedProperty minPercentage;
        public SerializedProperty dynamicResType;
        public SerializedProperty softwareUpsamplingFilter;
        public SerializedProperty forcePercentage;
        public SerializedProperty forcedPercentage;
        public SerializedProperty lowResTransparencyMinimumThreshold;
        public SerializedProperty rayTracingHalfResThreshold;

        public SerializedDynamicResolutionSettings(SerializedProperty root)
        {
            this.root = root;

            enabled = root.Find((GlobalDynamicResolutionSettings s) => s.enabled);
            enableDLSS = root.Find((GlobalDynamicResolutionSettings s) => s.enableDLSS);
            useMipBias = root.Find((GlobalDynamicResolutionSettings s) => s.useMipBias);
            DLSSPerfQualitySetting = root.Find((GlobalDynamicResolutionSettings s) => s.DLSSPerfQualitySetting);
            DLSSUseOptimalSettings = root.Find((GlobalDynamicResolutionSettings s) => s.DLSSUseOptimalSettings);
            DLSSSharpness = root.Find((GlobalDynamicResolutionSettings s) => s.DLSSSharpness);
            maxPercentage = root.Find((GlobalDynamicResolutionSettings s) => s.maxPercentage);
            minPercentage = root.Find((GlobalDynamicResolutionSettings s) => s.minPercentage);
            dynamicResType = root.Find((GlobalDynamicResolutionSettings s) => s.dynResType);
            softwareUpsamplingFilter = root.Find((GlobalDynamicResolutionSettings s) => s.upsampleFilter);
            forcePercentage = root.Find((GlobalDynamicResolutionSettings s) => s.forceResolution);
            forcedPercentage = root.Find((GlobalDynamicResolutionSettings s) => s.forcedPercentage);
            lowResTransparencyMinimumThreshold = root.Find((GlobalDynamicResolutionSettings s) => s.lowResTransparencyMinimumThreshold);
            rayTracingHalfResThreshold = root.Find((GlobalDynamicResolutionSettings s) => s.rayTracingHalfResThreshold);
        }
    }
}
