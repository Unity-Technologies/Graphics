using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    internal struct GlobalProbeVolumeSettings
    {
        /// <summary>Default GlobalProbeVolumeSettings</summary>
        internal static readonly GlobalProbeVolumeSettings @default = new GlobalProbeVolumeSettings()
        {
            atlasResolution = 128,
            atlasOctahedralDepthResolution = 2048
        };

        [SerializeField] internal int atlasResolution;
        [SerializeField] internal int atlasOctahedralDepthResolution;
    }
}
