using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // RenderRenderPipelineSettings represent settings that are immutable at runtime.
    // There is a dedicated RenderRenderPipelineSettings for each platform

    [Serializable]
    public class GlobalLightLoopSettings
    {
        public int cookieSize = 128;
        public int cookieTexArraySize = 16;
        public int pointCookieSize = 128;
        public int cubeCookieTexArraySize = 16;

        public int reflectionProbeCacheSize = 2;
        public int planarReflectionProbeCacheSize = 1024;
        public int reflectionCubemapSize = 128;
        public int planarReflectionTextureSize = 128;
        public bool reflectionCacheCompressed = false;
        public bool planarReflectionCacheCompressed = false;
        public int maxPlanarReflectionProbes = 128;
        public SkyResolution skyReflectionSize = SkyResolution.SkyResolution256;
        public LayerMask skyLightingOverrideLayerMask = 0;
    }
}



