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
        internal static GlobalLowResolutionTransparencySettings NewDefault() => new GlobalLowResolutionTransparencySettings()
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
