using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
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
        public static readonly GlobalLowResolutionTransparencySettings @default = new GlobalLowResolutionTransparencySettings()
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
