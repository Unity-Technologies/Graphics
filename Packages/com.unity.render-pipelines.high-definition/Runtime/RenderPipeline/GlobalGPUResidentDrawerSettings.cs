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
            mode = GPUResidentDrawerMode.Disabled
        };

        /// <summary>batching mode of macro batcher.</summary>
        public GPUResidentDrawerMode mode;
    }
}
