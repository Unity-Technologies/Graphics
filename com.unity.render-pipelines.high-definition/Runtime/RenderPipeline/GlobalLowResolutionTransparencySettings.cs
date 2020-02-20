using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Low resolution transparency upsample type..
    /// </summary>
    public enum LowResTransparentUpsample : byte
    {
        /// <summary>Bilinear upsample.</summary>
        Bilinear,
        /// <summary>Nearest depth upsample.</summary>
        NearestDepth
    }

    /// <summary>
    /// Global Low Resolution Transparency Settings.
    /// </summary>
    [Serializable]
    public struct GlobalLowResolutionTransparencySettings
    {
        internal static GlobalLowResolutionTransparencySettings NewDefault() => new GlobalLowResolutionTransparencySettings()
        {
            enabled = true,
            checkerboardDepthBuffer = true,
            upsampleType = LowResTransparentUpsample.NearestDepth
        };

        /// <summary>
        /// Enable low resolution transparency upsample.
        /// </summary>
        public bool enabled;
        /// <summary>
        /// Enable checkerboard depth buffer.
        /// </summary>
        public bool checkerboardDepthBuffer;
        /// <summary>Low resolution transparency upsample type.</summary>
        public LowResTransparentUpsample upsampleType;
    }
}
