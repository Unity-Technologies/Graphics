using UnityEditor.Rendering;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedDynamicResolutionSettings
    {
        public SerializedProperty root;

        public SerializedProperty enabled;
        public SerializedProperty maxPercentage;
        public SerializedProperty minPercentage;
        public SerializedProperty dynamicResType;
        public SerializedProperty softwareUpsamplingFilter;
        public SerializedProperty forcePercentage;
        public SerializedProperty forcedPercentage;

        public SerializedDynamicResolutionSettings(SerializedProperty root)
        {
            this.root = root;

            enabled                  = root.Find((GlobalDynamicResolutionSettings s) => s.enabled);
            maxPercentage            = root.Find((GlobalDynamicResolutionSettings s) => s.maxPercentage);
            minPercentage            = root.Find((GlobalDynamicResolutionSettings s) => s.minPercentage);
            dynamicResType           = root.Find((GlobalDynamicResolutionSettings s) => s.dynResType);
            softwareUpsamplingFilter = root.Find((GlobalDynamicResolutionSettings s) => s.upsampleFilter);
            forcePercentage          = root.Find((GlobalDynamicResolutionSettings s) => s.forceResolution);
            forcedPercentage         = root.Find((GlobalDynamicResolutionSettings s) => s.forcedPercentage);
        }
    }
}
