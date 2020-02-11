using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    public struct GlobalProbeVolumeSettings
    {
        /// <summary>Default GlobalDecalSettings</summary>
        public static readonly GlobalProbeVolumeSettings @default = new GlobalProbeVolumeSettings()
        {
            atlasWidth = 1024,
            atlasHeight = 1024,
            atlasOctahedralDepthWidth = 2048,
            atlasOctahedralDepthHeight = 2048
        };

        public int atlasWidth;
        public int atlasHeight;
        public int atlasOctahedralDepthWidth;
        public int atlasOctahedralDepthHeight;
    }
}
