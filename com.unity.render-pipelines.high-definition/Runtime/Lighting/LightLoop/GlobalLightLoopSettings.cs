using System;

namespace UnityEngine.Rendering.HighDefinition
{
    // RenderRenderPipelineSettings represent settings that are immutable at runtime.
    // There is a dedicated RenderRenderPipelineSettings for each platform

    /// <summary>
    /// Possible values for the cubemap texture size used for reflection probes.
    /// </summary>
    [Serializable]
    public enum CubeReflectionResolution
    {
        /// <summary>Size 128</summary>
        CubeReflectionResolution128 = 128,
        /// <summary>Size 256</summary>
        CubeReflectionResolution256 = 256,
        /// <summary>Size 512</summary>
        CubeReflectionResolution512 = 512,
        /// <summary>Size 1024</summary>
        CubeReflectionResolution1024 = 1024,
        /// <summary>Size 2048</summary>
        CubeReflectionResolution2048 = 2048,
        /// <summary>Size 4096</summary>
        CubeReflectionResolution4096 = 4096
    }

    /// <summary>
    /// Possible values for the texture 2D size used for planar reflection probes.
    /// </summary>
    [Serializable]
    public enum PlanarReflectionResolution
    {
        /// <summary>Size 64</summary>
        PlanarReflectionResolution64 = 64,
        /// <summary>Size 128</summary>
        PlanarReflectionResolution128 = 128,
        /// <summary>Size 256</summary>
        PlanarReflectionResolution256 = 256,
        /// <summary>Size 512</summary>
        PlanarReflectionResolution512 = 512,
        /// <summary>Size 1024</summary>
        PlanarReflectionResolution1024 = 1024,
        /// <summary>Size 2048</summary>
        PlanarReflectionResolution2048 = 2048,
        /// <summary>Size 4096</summary>
        PlanarReflectionResolution4096 = 4096,
        /// <summary>Size 8192</summary>
        PlanarReflectionResolution8192 = 8192,
        /// <summary>Size 16384</summary>
        PlanarReflectionResolution16384 = 16384
    }

    /// <summary>
    /// Possible values for the texture 2D size used for cookies.
    /// </summary>
    [Serializable]
    public enum CookieResolution
    {
        /// <summary>Size 64</summary>
        CookieResolution64 = 64,
        /// <summary>Size 128</summary>
        CookieResolution128 = 128,
        /// <summary>Size 256</summary>
        CookieResolution256 = 256,
        /// <summary>Size 512</summary>
        CookieResolution512 = 512,
        /// <summary>Size 1024</summary>
        CookieResolution1024 = 1024,
        /// <summary>Size 2048</summary>
        CookieResolution2048 = 2048,
        /// <summary>Size 4096</summary>
        CookieResolution4096 = 4096,
        /// <summary>Size 8192</summary>
        CookieResolution8192 = 8192,
        /// <summary>Size 16384</summary>
        CookieResolution16384 = 16384
    }

    /// <summary>
    /// Possible values for the cubemap texture size used for cookies.
    /// </summary>
    [Serializable]
    public enum CubeCookieResolution
    {
        /// <summary>Size 64</summary>
        CubeCookieResolution64 = 64,
        /// <summary>Size 128</summary>
        CubeCookieResolution128 = 128,
        /// <summary>Size 256</summary>
        CubeCookieResolution256 = 256,
        /// <summary>Size 512</summary>
        CubeCookieResolution512 = 512,
        /// <summary>Size 1024</summary>
        CubeCookieResolution1024 = 1024,
        /// <summary>Size 2048</summary>
        CubeCookieResolution2048 = 2048,
        /// <summary>Size 4096</summary>
        CubeCookieResolution4096 = 4096
    }

    [Serializable]
    public struct GlobalLightLoopSettings
    {
        /// <summary>Default GlobalDecalSettings</summary>
        [Obsolete("Since 2019.3, use GlobalLightLoopSettings.NewDefault() instead.")]
        public static readonly GlobalLightLoopSettings @default = default;
        /// <summary>Default GlobalDecalSettings</summary>
        public static GlobalLightLoopSettings NewDefault() => new GlobalLightLoopSettings()
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
