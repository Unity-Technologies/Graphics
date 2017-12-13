using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // RenderRenderPipelineSettings represent settings that are immutable at runtime.
    // There is a dedicated RenderRenderPipelineSettings for each platform

    [Serializable]
    public struct GlobalLightLoopSettings
    {
        public int spotCookieSize;
        public int cookieTexArraySize;
        public int pointCookieSize;
        public int cubeCookieTexArraySize;

        public int reflectionProbeCacheSize;
        public int reflectionCubemapSize;
        public bool reflectionCacheCompressed;

        public GlobalLightLoopSettings()
        {
            spotCookieSize              = 128;
            cookieTexArraySize          = 16;

            pointCookieSize             = 512;
            cubeCookieTexArraySize      = 16;

            reflectionProbeCacheSize    = 128;
            reflectionCubemapSize       = 128;
            reflectionCacheCompressed   = false;
        }
    }
}



