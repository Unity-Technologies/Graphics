using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedGPUResidentDrawerSettings
    {
        public SerializedProperty root;
        public SerializedProperty mode;
        public SerializedProperty smallMeshScreenPercentage;
        public SerializedProperty enableOcclusionCullingInCameras;
        public SerializedProperty useDepthPrepassForOccluders;
        public SerializedProperty shadowSmallMeshScreenPercentages;

        public SerializedGPUResidentDrawerSettings(SerializedProperty root)
        {
            this.root = root;
            mode = root.Find((GlobalGPUResidentDrawerSettings s) => s.mode);
            smallMeshScreenPercentage = root.Find((GlobalGPUResidentDrawerSettings s) => s.smallMeshScreenPercentage);
            enableOcclusionCullingInCameras = root.Find((GlobalGPUResidentDrawerSettings s) => s.enableOcclusionCullingInCameras);
            useDepthPrepassForOccluders = root.Find((GlobalGPUResidentDrawerSettings s) => s.useDepthPrepassForOccluders);
            shadowSmallMeshScreenPercentages = root.Find((GlobalGPUResidentDrawerSettings s) => s.shadowSmallMeshScreenPercentages);
        }
    }
}
