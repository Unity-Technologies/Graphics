using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    internal struct DensityVolumeSettings
    {
        internal static readonly DensityVolumeSettings @default = new DensityVolumeSettings()
        {
            atlasResolution = new Vector3Int(64, 64, 256),
        };

        [SerializeField] internal Vector3Int atlasResolution;
    }
}
