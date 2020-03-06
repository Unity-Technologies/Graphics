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
            occlusionMesh = true
        };

        /// <summary>Use single pass.</summary>
        public bool singlePass;
        /// <summary>Use occlusion mesh.</summary>
        public bool occlusionMesh;
    }
}
