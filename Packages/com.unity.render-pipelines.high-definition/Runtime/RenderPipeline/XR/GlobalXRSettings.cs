using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Global XR Settings.
    /// </summary>
    [Serializable]
    public struct GlobalXRSettings
    {
        internal static GlobalXRSettings NewDefault() => new GlobalXRSettings()
        {
            singlePass = true,
            occlusionMesh = true,
            cameraJitter = false,
            allowMotionBlur = false
        };

        /// <summary>Use single pass.</summary>
        public bool singlePass;
        /// <summary>Use occlusion mesh.</summary>
        public bool occlusionMesh;
        /// <summary>Add jitter to camera for temporal effects.</summary>
        public bool cameraJitter;
        /// <summary>Allow motion blur when in XR.</summary>
        public bool allowMotionBlur;
    }
}
