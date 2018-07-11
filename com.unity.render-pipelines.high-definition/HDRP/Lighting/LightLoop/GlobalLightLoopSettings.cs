using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // RenderRenderPipelineSettings represent settings that are immutable at runtime.
    // There is a dedicated RenderRenderPipelineSettings for each platform

    [Serializable]
    public enum CubeReflectionResolution
    {
        CubeReflectionResolution128 = 128,
        CubeReflectionResolution256 = 256,
        CubeReflectionResolution512 = 512,
        CubeReflectionResolution1024 = 1024,
        CubeReflectionResolution2048 = 2048,
        CubeReflectionResolution4096 = 4096
    }

    [Serializable]
    public enum PlanarReflectionResolution
    {
        PlanarReflectionResolution64 = 64,
        PlanarReflectionResolution128 = 128,
        PlanarReflectionResolution256 = 256,
        PlanarReflectionResolution512 = 512,
        PlanarReflectionResolution1024 = 1024,
        PlanarReflectionResolution2048 = 2048,
        PlanarReflectionResolution4096 = 4096,
        PlanarReflectionResolution8192 = 8192,
        PlanarReflectionResolution16384 = 16384
    }

    [Serializable]
    public class GlobalLightLoopSettings
    {
        public int cookieAtlasSize = 1024;

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
