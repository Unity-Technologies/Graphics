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
        public CookieResolution cookieSize = CookieResolution.CookieResolution128;
        public int cookieTexArraySize = 16;
        public CubeCookieResolution pointCookieSize = CubeCookieResolution.CubeCookieResolution128;
        public int cubeCookieTexArraySize = 16;

        public int planarReflectionProbeCacheSize = 2;
        public PlanarReflectionResolution planarReflectionTextureSize = PlanarReflectionResolution.PlanarReflectionResolution1024;
        public int reflectionProbeCacheSize = 128;
        public CubeReflectionResolution reflectionCubemapSize = CubeReflectionResolution.CubeReflectionResolution128;
        public bool reflectionCacheCompressed = false;
        public bool planarReflectionCacheCompressed = false;
        public SkyResolution skyReflectionSize = SkyResolution.SkyResolution256;
        public LayerMask skyLightingOverrideLayerMask = 0;
    }
}



