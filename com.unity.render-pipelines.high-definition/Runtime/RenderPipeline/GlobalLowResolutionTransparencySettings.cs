using System;

namespace UnityEngine.Rendering.HighDefinition
{

    public enum LowResTransparentUpsample : byte
    {
        Bilinear,
        NearestDepth
    }

    [Serializable]
    public struct GlobalLowResolutionTransparencySettings
    {
        /// <summary>Default GlobalLowResolutionTransparencySettings</summary>
        [Obsolete("Since 2019.3, use GlobalLowResolutionTransparencySettings.NewDefault() instead.")]
        public static readonly GlobalLowResolutionTransparencySettings @default = default;
        /// <summary>Default GlobalLowResolutionTransparencySettings</summary>
        public static GlobalLowResolutionTransparencySettings NewDefault() => new GlobalLowResolutionTransparencySettings()
        {
            enabled = true,
            checkerboardDepthBuffer = true,
            upsampleType = LowResTransparentUpsample.NearestDepth
        };

        public bool enabled;
        public bool checkerboardDepthBuffer;


        public LowResTransparentUpsample upsampleType;
    }
}
