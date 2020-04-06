using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    internal struct GlobalProbeVolumeSettings
    {
        /// <summary>Default GlobalProbeVolumeSettings</summary>
        internal static readonly GlobalProbeVolumeSettings @default = new GlobalProbeVolumeSettings()
        {
            atlasWidth = 128,
            atlasHeight = 128,
            atlasDepth = 512,
            atlasOctahedralDepthWidth = 2048,
            atlasOctahedralDepthHeight = 2048
        };

        [SerializeField] internal int atlasWidth;
        [SerializeField] internal int atlasHeight;
        [SerializeField] internal int atlasDepth;
        [SerializeField] internal int atlasOctahedralDepthWidth;
        [SerializeField] internal int atlasOctahedralDepthHeight;
    }
}
