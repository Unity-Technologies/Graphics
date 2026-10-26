using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>User-facing settings for GPU Resident Drawer draw submission settings.</summary>
    [Serializable]
    public struct GlobalGPUResidentDrawerSettings
    {
        /// <summary>Default GlobalImprovedDrawSubmissionSettings</summary>
        /// <returns>Default value for GlobalImprovedDrawSubmissionSettings</returns>
        public static GlobalGPUResidentDrawerSettings NewDefault() => new GlobalGPUResidentDrawerSettings()
        {
            mode = GPUResidentDrawerMode.Disabled,
            smallMeshScreenPercentage = 0.0f,
            enableOcclusionCullingInCameras = false,
            useDepthPrepassForOccluders = true,
            shadowSmallMeshScreenPercentages = Vector4.zero
        };

        /// <summary>batching mode of macro batcher.</summary>
        public GPUResidentDrawerMode mode;

        /// <summary>
        /// Default minimum screen percentage (0-20%) gpu-driven Renderers can cover before getting culled.
        /// </summary>
        public float smallMeshScreenPercentage;

        /// <summary>Enables occlusion culling in cameras</summary>
        public bool enableOcclusionCullingInCameras;

        /// <summary>Uses the depth prepass for occluders</summary>
        public bool useDepthPrepassForOccluders;

        /// <summary> Default minimum screen percentages (0-50%) gpu-driven Renderers can cover before getting culled in shadow cascades</summary>
        public Vector4 shadowSmallMeshScreenPercentages;

    }
}
