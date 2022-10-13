using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    internal struct GlobalMaskVolumeSettings
    {
        /// <summary>Default GlobalMaskVolumeSettings</summary>
        internal static readonly GlobalMaskVolumeSettings @default = new GlobalMaskVolumeSettings()
        {
            atlasResolution = 128,
        };

        [SerializeField] internal int atlasResolution;
    }
}
