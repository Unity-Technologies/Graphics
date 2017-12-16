using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // All the structures here represent global engine settings.
    // It means that they are supposed to be setup once and not changed during the game.
    // All of these will be serialized in the HDRenderPipelineInstance used for the project.
    [Serializable]
    public class GlobalTextureSettings
    {
        public const int kHDDefaultSpotCookieSize = 128;
        public const int kHDDefaultPointCookieSize = 512;
        public const int kHDDefaultReflectionCubemapSize = 128;

        public int spotCookieSize = kHDDefaultSpotCookieSize;
        public int pointCookieSize = kHDDefaultPointCookieSize;
        public int reflectionCubemapSize = kHDDefaultReflectionCubemapSize;
        public bool reflectionCacheCompressed = false;
    }

    [Serializable]
    public class GlobalRenderingSettings
    {
        public bool supportShadowMask;

        public bool supportSSSAndTransmission;
        public bool supportDBuffer;

        public bool supportSSR;
        public bool supportSSAO;
    }
}
