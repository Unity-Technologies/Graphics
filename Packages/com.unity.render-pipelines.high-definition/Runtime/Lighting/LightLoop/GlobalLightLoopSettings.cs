using System;
using System.ComponentModel;
using UnityEngine.Serialization;
using UnityEngine.Experimental.Rendering;

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
    /// Available graphic formats for the cube and planar reflection probes.
    /// </summary>
    [System.Serializable]
    public enum ReflectionAndPlanarProbeFormat
    {
        /// <summary>Faster sampling and rendering but at the cost of precision.</summary>
        R11G11B10 = GraphicsFormat.B10G11R11_UFloatPack32,
        /// <summary>Better precision, but uses twice as much memory compared to R11G11B10.</summary>
        R16G16B16A16 = GraphicsFormat.R16G16B16A16_SFloat,
    }

    /// <summary>
    /// Available values for the reflection probe texture cache size.
    /// </summary>
    [Serializable]
    public enum ReflectionProbeTextureCacheResolution
    {
        /// <summary>Resolution 512x512</summary>
        Resolution512x512 = 512,
        /// <summary>Resolution 1024x512</summary>
        Resolution1024x512 = 1024 << 16 | 512,
        /// <summary>Resolution 1024x1024</summary>
        Resolution1024x1024 = 1024,
        /// <summary>Resolution 2048x1024</summary>
        Resolution2048x1024 = 2048 << 16 | 1024,
        /// <summary>Resolution 2048x2048</summary>
        Resolution2048x2048 = 2048,
        /// <summary>Resolution 4096x2048</summary>
        Resolution4096x2048 = 4096 << 16 | 2048,
        /// <summary>Resolution 4096x4096</summary>
        Resolution4096x4096 = 4096,
        /// <summary>Resolution 8192x4096</summary>
        Resolution8192x4096 = 8192 << 16 | 4096,
        /// <summary>Resolution 8192x8192</summary>
        Resolution8192x8192 = 8192,
        /// <summary>Resolution 16384x8192</summary>
        Resolution16384x8192 = 16384 << 16 | 8192,
        /// <summary>Resolution 16384</summary>
        Resolution16384x16384 = 16384,
    }

    /// <summary>
    /// Possible values for the texture 2D size used for planar reflection probes.
    /// </summary>
    [Serializable]
    public enum PlanarReflectionAtlasResolution
    {
        /// <summary>Size 64</summary>
        Resolution64 = 64,
        /// <summary>Size 128</summary>
        Resolution128 = 128,
        /// <summary>Size 256</summary>
        Resolution256 = 256,
        /// <summary>Size 512</summary>
        Resolution512 = 512,
        /// <summary>Size 1024</summary>
        Resolution1024 = 1024,
        /// <summary>Size 2048</summary>
        Resolution2048 = 2048,
        /// <summary>Size 4096</summary>
        Resolution4096 = 4096,
        /// <summary>Size 8192</summary>
        Resolution8192 = 8192,
        /// <summary>Size 16384</summary>
        Resolution16384 = 16384
    }

    /// <summary>
    /// Possible values for the texture 2D size used for cookies.
    /// </summary>
    [Serializable]
    public enum CookieAtlasResolution
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

    /// <summary>
    /// Possible values for one element of the Local Volumetric Fog atlas.
    /// </summary>
    [Serializable]
    [Obsolete("The texture resolution limit in volumetric fogs have been removed. This enum is unused.")]
    public enum LocalVolumetricFogResolution
    {
        /// <summary>3D volume of 32x32x32 voxels.</summary>
        [InspectorName("32x32x32")]
        Resolution32 = 32,
        /// <summary>3D volume of 64x64x64 voxels.</summary>
        [InspectorName("64x64x64")]
        Resolution64 = 64,
        /// <summary>3D volume of 128x128x128 voxels.</summary>
        [InspectorName("128x128x128")]
        Resolution128 = 128,
        /// <summary>3D volume of 256x256x256 voxels.</summary>
        [InspectorName("256x256x256")]
        Resolution256 = 256,
    }

    /// <summary>
    /// Global Light Loop Settings.
    /// </summary>
    [Serializable]
    public struct GlobalLightLoopSettings
    {
        internal static readonly GlobalLightLoopSettings @default = default;
        /// <summary>Default GlobalDecalSettings</summary>
        internal static GlobalLightLoopSettings NewDefault() => new GlobalLightLoopSettings()
        {
            cookieAtlasSize = CookieAtlasResolution.CookieResolution2048,
            cookieFormat = CookieAtlasGraphicsFormat.R11G11B10,

            cookieAtlasLastValidMip = 0,

            // We must keep this value for migration purpose (when we create a new HDRP asset it is migrated to the last version)
#pragma warning disable 618 // Type or member is obsolete
            cookieTexArraySize = 1,
#pragma warning restore 618

            reflectionProbeFormat = ReflectionAndPlanarProbeFormat.R11G11B10,
            reflectionProbeTexCacheSize = ReflectionProbeTextureCacheResolution.Resolution4096x4096,
            reflectionProbeTexLastValidCubeMip = 3,
            reflectionProbeTexLastValidPlanarMip = 0,
            reflectionProbeDecreaseResToFit = true,

            skyReflectionSize = SkyResolution.SkyResolution256,
            skyLightingOverrideLayerMask = 0,

            maxDirectionalLightsOnScreen = 16,
            maxPunctualLightsOnScreen = 512,
            maxAreaLightsOnScreen = 64,
            maxCubeReflectionOnScreen = HDRenderPipeline.k_MaxCubeReflectionsOnScreen / 4,
            maxPlanarReflectionOnScreen = HDRenderPipeline.k_MaxPlanarReflectionsOnScreen / 2,
            maxDecalsOnScreen = 512,
            maxLightsPerClusterCell = 8,
            maxLocalVolumetricFogOnScreen = 256,
        };

        internal static Vector2Int GetReflectionProbeTextureCacheDim(ReflectionProbeTextureCacheResolution resolution)
        {
            if(resolution <= ReflectionProbeTextureCacheResolution.Resolution16384x16384)
                return new Vector2Int((int)resolution, (int)resolution);

            return new Vector2Int(((int)resolution >> 16), (int)resolution & 0xFFFF);
        }

        /// <summary>Cookie atlas resolution.</summary>
        [FormerlySerializedAs("cookieSize")]
        public CookieAtlasResolution cookieAtlasSize;
        /// <summary>Cookie atlas graphics format.</summary>
        public CookieAtlasGraphicsFormat cookieFormat;
#if UNITY_2020_1_OR_NEWER
#else
        /// <summary>Cookie atlas resolution for point lights.</summary>
        public CubeCookieResolution pointCookieSize;
#endif
        /// <summary>Last valid mip for cookie atlas.</summary>
        public int cookieAtlasLastValidMip;

        // We keep this property for the migration code (we need to know how many cookies we could have before).
        [SerializeField, Obsolete("There is no more texture array for cookies, use cookie atlases properties instead.", false)]
        internal int cookieTexArraySize;

        // We keep this property for the migration code
        /// <summary>Obsolete: Use planar reflection atlas.</summary>
        [FormerlySerializedAs("planarReflectionTextureSize")]
        [SerializeField, Obsolete("There is no more planar reflection atlas, use reflection probe atlases instead.", false)]
        public PlanarReflectionAtlasResolution planarReflectionAtlasSize;

        // We keep this property for the migration code
        [SerializeField, Obsolete("There is no more texture array for cube reflection probes, use reflection probe atlases properties instead.", false)]
        internal int reflectionProbeCacheSize;

        // We keep this property for the migration code
        [SerializeField, Obsolete("There is no more cube reflection probe size, use cube reflection probe size tiers instead.", false)]
        internal CubeReflectionResolution reflectionCubemapSize;

        // We keep this property for the migration code
        [SerializeField, Obsolete("There is no more max env light on screen, use max planar and cube reflection probes on screen instead.", false)]
        internal int maxEnvLightsOnScreen;

        /// <summary>Enable reflection probe cache compression.</summary>
        public bool reflectionCacheCompressed;
        /// <summary>Reflection probes format.</summary>
        public ReflectionAndPlanarProbeFormat reflectionProbeFormat;
        /// <summary>Reflection probes texture cache size.</summary>
        public ReflectionProbeTextureCacheResolution reflectionProbeTexCacheSize;
        /// <summary>Last valid mip for cube reflection probes in the reflection probe's texture cache.</summary>
        public int reflectionProbeTexLastValidCubeMip;
        /// <summary>Last valid mip for planar reflection probes in the reflection probe's texture cache.</summary>
        public int reflectionProbeTexLastValidPlanarMip;
        /// <summary>Downsize the reflection probe resolution if the probe doesn't fit in the reflection probe's texture cache.</summary>
        public bool reflectionProbeDecreaseResToFit;

        /// <summary>Resolution of the sky reflection cubemap.</summary>
        public SkyResolution skyReflectionSize;
        /// <summary>LayerMask used for sky lighting override.</summary>
        public LayerMask skyLightingOverrideLayerMask;
        /// <summary>Enable fabric specific convolution for probes and sky lighting.</summary>
        public bool supportFabricConvolution;

        /// <summary>Maximum number of directional lights at the same time on screen.</summary>
        public int maxDirectionalLightsOnScreen;
        /// <summary>Maximum number of punctual lights at the same time on screen.</summary>
        public int maxPunctualLightsOnScreen;
        /// <summary>Maximum number of area lights at the same time on screen.</summary>
        public int maxAreaLightsOnScreen;
        /// <summary>Maximum number of environment lights at the same time on screen.</summary>
        public int maxCubeReflectionOnScreen;
        /// <summary>Maximum number of planar lights at the same time on screen.</summary>
        public int maxPlanarReflectionOnScreen;
        /// <summary>Maximum number of decals at the same time on screen.</summary>
        public int maxDecalsOnScreen;
        /// <summary>Maximum number of lights per ray tracing light cluster cell.</summary>
        public int maxLightsPerClusterCell;

        /// <summary>Maximum size of one Local Volumetric Fog texture.</summary>
        [Obsolete("The texture resolution limit in volumetric fogs have been removed. This field is unused.")]
        public LocalVolumetricFogResolution maxLocalVolumetricFogSize;

        /// <summary>Maximum number of Local Volumetric Fog at the same time on screen.</summary>
        [Range(1, HDRenderPipeline.k_MaxVisibleLocalVolumetricFogCount)]
        public int maxLocalVolumetricFogOnScreen;
    }
}
