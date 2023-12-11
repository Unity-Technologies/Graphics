using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedGPUResidentDrawerSettings
    {
        public SerializedProperty root;
        public SerializedProperty mode;
        public SerializedProperty useLegacyLightmaps;
        public SerializedProperty smallMeshScreenPercentage;
        public SerializedProperty enableOcclusionCullingInCameras;
        public SerializedProperty useDepthPrepassForOccluders;

        public SerializedGPUResidentDrawerSettings(SerializedProperty root)
        {
            this.root = root;
            mode = root.Find((GlobalGPUResidentDrawerSettings s) => s.mode);
            useLegacyLightmaps = root.Find((GlobalGPUResidentDrawerSettings s) => s.useLegacyLightmaps);
            smallMeshScreenPercentage = root.Find((GlobalGPUResidentDrawerSettings s) => s.smallMeshScreenPercentage);
            enableOcclusionCullingInCameras = root.Find((GlobalGPUResidentDrawerSettings s) => s.enableOcclusionCullingInCameras);
            useDepthPrepassForOccluders = root.Find((GlobalGPUResidentDrawerSettings s) => s.useDepthPrepassForOccluders);
        }
    }
}
