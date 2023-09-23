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
            allowInEditMode = false
        };

        /// <summary>batching mode of macro batcher.</summary>
        public GPUResidentDrawerMode mode;

        /// <summary>
        /// Allows the GPU Resident Drawer to run in edit mode
        /// </summary>
        public bool allowInEditMode;
    }
}
