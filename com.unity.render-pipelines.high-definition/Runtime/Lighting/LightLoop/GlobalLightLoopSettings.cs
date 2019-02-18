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
    public enum CookieResolution
    {
        CookieResolution64 = 64,
        CookieResolution128 = 128,
        CookieResolution256 = 256,
        CookieResolution512 = 512,
        CookieResolution1024 = 1024,
        CookieResolution2048 = 2048,
        CookieResolution4096 = 4096,
        CookieResolution8192 = 8192,
        CookieResolution16384 = 16384
    }

    [Serializable]
    public enum CubeCookieResolution
    {
        CubeCookieResolution64 = 64,
        CubeCookieResolution128 = 128,
        CubeCookieResolution256 = 256,
        CubeCookieResolution512 = 512,
        CubeCookieResolution1024 = 1024,
        CubeCookieResolution2048 = 2048,
        CubeCookieResolution4096 = 4096
    }

    [Serializable]
    public struct GlobalLightLoopSettings
    {
        /// <summary>Default GlobalDecalSettings</summary>
        public static readonly GlobalLightLoopSettings @default = new GlobalLightLoopSettings()
        {
            cookieSize = CookieResolution.CookieResolution128,
            cookieTexArraySize = 16,
            pointCookieSize = CubeCookieResolution.CubeCookieResolution128,
            cubeCookieTexArraySize = 16,

            planarReflectionProbeCacheSize = 2,
            planarReflectionTextureSize = PlanarReflectionResolution.PlanarReflectionResolution1024,
            reflectionProbeCacheSize = 64,
            reflectionCubemapSize = CubeReflectionResolution.CubeReflectionResolution256,

            skyReflectionSize = SkyResolution.SkyResolution256,
            skyLightingOverrideLayerMask = 0,

            maxDirectionalLightsOnScreen = 16,
            maxPunctualLightsOnScreen = 512,
            maxAreaLightsOnScreen = 64,
            maxEnvLightsOnScreen = 64,
            maxDecalsOnScreen = 512,
        };

        public CookieResolution cookieSize;
        public int cookieTexArraySize;
        public CubeCookieResolution pointCookieSize;
        public int cubeCookieTexArraySize;

        public int planarReflectionProbeCacheSize;
        public PlanarReflectionResolution planarReflectionTextureSize;
        public int reflectionProbeCacheSize;
        public CubeReflectionResolution reflectionCubemapSize;
        public bool reflectionCacheCompressed;
        public bool planarReflectionCacheCompressed;

        public SkyResolution skyReflectionSize;
        public LayerMask skyLightingOverrideLayerMask;
        public bool supportFabricConvolution;

        public int maxDirectionalLightsOnScreen;
        public int maxPunctualLightsOnScreen;
        public int maxAreaLightsOnScreen;
        public int maxEnvLightsOnScreen;
        public int maxDecalsOnScreen;
    }
}
