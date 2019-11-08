using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    static class VisibleLightExtensionMethods
    {
        public static Vector3 GetPosition(this VisibleLight value)
        {
            return value.localToWorldMatrix.GetColumn(3);
        }

        public static Vector3 GetForward(this VisibleLight value)
        {
            return value.localToWorldMatrix.GetColumn(2);
        }

        public static Vector3 GetUp(this VisibleLight value)
        {
            return value.localToWorldMatrix.GetColumn(1);
        }

        public static Vector3 GetRight(this VisibleLight value)
        {
            return value.localToWorldMatrix.GetColumn(0);
        }
    }

    //-----------------------------------------------------------------------------
    // structure definition
    //-----------------------------------------------------------------------------

    [GenerateHLSL]
    internal enum LightVolumeType
    {
        Cone,
        Sphere,
        Box,
        Count
    }

    [GenerateHLSL]
    internal enum LightCategory
    {
        Punctual,
        Area,
        Env,
        Decal,
        DensityVolume,
        Count
    }

    [GenerateHLSL]
    internal enum LightFeatureFlags
    {
        // Light bit mask must match LightDefinitions.s_LightFeatureMaskFlags value
        Punctual    = 1 << 12,
        Area        = 1 << 13,
        Directional = 1 << 14,
        Env         = 1 << 15,
        Sky         = 1 << 16,
        SSRefraction = 1 << 17,
        SSReflection = 1 << 18
            // If adding more light be sure to not overflow LightDefinitions.s_LightFeatureMaskFlags
    }

    [GenerateHLSL]
    class LightDefinitions
    {
        public static int s_MaxNrBigTileLightsPlusOne = 512;      // may be overkill but the footprint is 2 bits per pixel using uint16.
        public static float s_ViewportScaleZ = 1.0f;
        public static int s_UseLeftHandCameraSpace = 1;

        public static int s_TileSizeFptl = 16;
        public static int s_TileSizeClustered = 32;
        public static int s_TileSizeBigTile = 64;

        // Tile indexing constants for indirect dispatch deferred pass : [2 bits for eye index | 15 bits for tileX | 15 bits for tileY]
        public static int s_TileIndexMask = 0x7FFF;
        public static int s_TileIndexShiftX = 0;
        public static int s_TileIndexShiftY = 15;
        public static int s_TileIndexShiftEye = 30;

        // feature variants
        public static int s_NumFeatureVariants = 29;

        // light list limits
        public static int s_LightListMaxCoarseEntries = 64;
        public static int s_LightListMaxPrunedEntries = 24;
        public static int s_LightClusterMaxCoarseEntries = 128;

        // Following define the maximum number of bits use in each feature category.
        public static uint s_LightFeatureMaskFlags = 0xFFF000;
        public static uint s_LightFeatureMaskFlagsOpaque = 0xFFF000 & ~((uint)LightFeatureFlags.SSRefraction); // Opaque don't support screen space refraction
        public static uint s_LightFeatureMaskFlagsTransparent = 0xFFF000 & ~((uint)LightFeatureFlags.SSReflection); // Transparent don't support screen space reflection
        public static uint s_MaterialFeatureMaskFlags = 0x000FFF;   // don't use all bits just to be safe from signed and/or float conversions :/
    }

    [GenerateHLSL]
    struct SFiniteLightBound
    {
        public Vector3 boxAxisX;
        public Vector3 boxAxisY;
        public Vector3 boxAxisZ;
        public Vector3 center;        // a center in camera space inside the bounding volume of the light source.
        public Vector2 scaleXY;
        public float radius;
    };

    [GenerateHLSL]
    struct LightVolumeData
    {
        public Vector3 lightPos;
        public uint lightVolume;

        public Vector3 lightAxisX;
        public uint lightCategory;

        public Vector3 lightAxisY;
        public float radiusSq;

        public Vector3 lightAxisZ;      // spot +Z axis
        public float cotan;

        public Vector3 boxInnerDist;
        public uint featureFlags;

        public Vector3 boxInvRange;
        public float unused2;
    };

        public enum TileClusterDebug : int
        {
            None,
            Tile,
            Cluster,
            MaterialFeatureVariants
        };

        public enum LightVolumeDebug : int
        {
            Gradient,
            ColorAndEdge
        };

        public enum TileClusterCategoryDebug : int
        {
            Punctual = 1,
            Area = 2,
            AreaAndPunctual = 3,
            Environment = 4,
            EnvironmentAndPunctual = 5,
            EnvironmentAndArea = 6,
            EnvironmentAndAreaAndPunctual = 7,
            Decal = 8,
            DensityVolumes = 16
        };

    public partial class HDRenderPipeline
    {
        internal const int k_MaxCacheSize = 2000000000; //2 GigaByte
        internal const int k_MaxDirectionalLightsOnScreen = 16;
        internal const int k_MaxPunctualLightsOnScreen    = 512;
        internal const int k_MaxAreaLightsOnScreen        = 128;
        internal const int k_MaxDecalsOnScreen = 512;
        internal const int k_MaxLightsOnScreen = k_MaxDirectionalLightsOnScreen + k_MaxPunctualLightsOnScreen + k_MaxAreaLightsOnScreen + k_MaxEnvLightsOnScreen;
        internal const int k_MaxEnvLightsOnScreen = 128;
        internal static readonly Vector3 k_BoxCullingExtentThreshold = Vector3.one * 0.01f;

        #if UNITY_SWITCH
        static bool k_PreferFragment = true;
        #else
        static bool k_PreferFragment = false;
        #endif
        #if !UNITY_EDITOR && UNITY_SWITCH
        const bool k_HasNativeQuadSupport = true;
        #else
        const bool k_HasNativeQuadSupport = false;
        #endif

        #if !UNITY_EDITOR && UNITY_SWITCH
        const int k_ThreadGroupOptimalSize = 32;
        #else
        const int k_ThreadGroupOptimalSize = 64;
        #endif

        int m_MaxDirectionalLightsOnScreen;
        int m_MaxPunctualLightsOnScreen;
        int m_MaxAreaLightsOnScreen;
        int m_MaxDecalsOnScreen;
        int m_MaxLightsOnScreen;
        int m_MaxEnvLightsOnScreen;

        Texture2DArray  m_DefaultTexture2DArray;
        Cubemap         m_DefaultTextureCube;

        internal class LightLoopTextureCaches
        {
            // Structure for cookies used by directional and spotlights
            public TextureCache2D               cookieTexArray { get; private set; }
            public LTCAreaLightCookieManager    areaLightCookieManager { get; private set; }
            // Structure for cookies used by point lights
            public TextureCacheCubemap          cubeCookieTexArray { get; private set; }
            public ReflectionProbeCache         reflectionProbeCache { get; private set; }
            public PlanarReflectionProbeCache   reflectionPlanarProbeCache { get; private set; }
            public List<Matrix4x4>              env2DCaptureVP { get; private set; }
            public List<float>                  env2DCaptureForward { get; private set; }

            Material m_CubeToPanoMaterial;

            public void Initialize(HDRenderPipelineAsset hdrpAsset, RenderPipelineResources defaultResources,  IBLFilterBSDF[] iBLFilterBSDFArray)
            {
                var lightLoopSettings = hdrpAsset.currentPlatformRenderPipelineSettings.lightLoopSettings;

                m_CubeToPanoMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.cubeToPanoPS);

                areaLightCookieManager = new LTCAreaLightCookieManager(hdrpAsset, defaultResources, k_MaxCacheSize);

                env2DCaptureVP = new List<Matrix4x4>();
                env2DCaptureForward = new List<float>();
                for (int i = 0, c = Mathf.Max(1, lightLoopSettings.planarReflectionProbeCacheSize); i < c; ++i)
                {
                    env2DCaptureVP.Add(Matrix4x4.identity);
                    env2DCaptureForward.Add(0);
                    env2DCaptureForward.Add(0);
                    env2DCaptureForward.Add(0);
                }

                cookieTexArray = new TextureCache2D("Cookie");
                int coockieSize = lightLoopSettings.cookieTexArraySize;
                int coockieResolution = (int)lightLoopSettings.cookieSize;
                if (TextureCache2D.GetApproxCacheSizeInByte(coockieSize, coockieResolution, 1) > k_MaxCacheSize)
                    coockieSize = TextureCache2D.GetMaxCacheSizeForWeightInByte(k_MaxCacheSize, coockieResolution, 1);
                cookieTexArray.AllocTextureArray(coockieSize, coockieResolution, coockieResolution, TextureFormat.RGBA32, true);
                cubeCookieTexArray = new TextureCacheCubemap("Cookie");
                int coockieCubeSize = lightLoopSettings.cubeCookieTexArraySize;
                int coockieCubeResolution = (int)lightLoopSettings.pointCookieSize;
                if (TextureCacheCubemap.GetApproxCacheSizeInByte(coockieCubeSize, coockieCubeResolution, 1) > k_MaxCacheSize)
                    coockieCubeSize = TextureCacheCubemap.GetMaxCacheSizeForWeightInByte(k_MaxCacheSize, coockieCubeResolution, 1);
                cubeCookieTexArray.AllocTextureArray(coockieCubeSize, coockieCubeResolution, TextureFormat.RGBA32, true, m_CubeToPanoMaterial);

                // For regular reflection probes, we need to convolve with all the BSDF functions
                TextureFormat probeCacheFormat = lightLoopSettings.reflectionCacheCompressed ? TextureFormat.BC6H : TextureFormat.RGBAHalf;
                int reflectionCubeSize = lightLoopSettings.reflectionProbeCacheSize;
                int reflectionCubeResolution = (int)lightLoopSettings.reflectionCubemapSize;
                if (ReflectionProbeCache.GetApproxCacheSizeInByte(reflectionCubeSize, reflectionCubeResolution, iBLFilterBSDFArray.Length) > k_MaxCacheSize)
                    reflectionCubeSize = ReflectionProbeCache.GetMaxCacheSizeForWeightInByte(k_MaxCacheSize, reflectionCubeResolution, iBLFilterBSDFArray.Length);
                reflectionProbeCache = new ReflectionProbeCache(defaultResources, iBLFilterBSDFArray, reflectionCubeSize, reflectionCubeResolution, probeCacheFormat, true);

                // For planar reflection we only convolve with the GGX filter, otherwise it would be too expensive
                TextureFormat planarProbeCacheFormat = lightLoopSettings.planarReflectionCacheCompressed ? TextureFormat.BC6H : TextureFormat.RGBAHalf;
                int reflectionPlanarSize = lightLoopSettings.planarReflectionProbeCacheSize;
                int reflectionPlanarResolution = (int)lightLoopSettings.planarReflectionTextureSize;
                if (ReflectionProbeCache.GetApproxCacheSizeInByte(reflectionPlanarSize, reflectionPlanarResolution, 1) > k_MaxCacheSize)
                    reflectionPlanarSize = ReflectionProbeCache.GetMaxCacheSizeForWeightInByte(k_MaxCacheSize, reflectionPlanarResolution, 1);
                reflectionPlanarProbeCache = new PlanarReflectionProbeCache(defaultResources, (IBLFilterGGX)iBLFilterBSDFArray[0], reflectionPlanarSize, reflectionPlanarResolution, planarProbeCacheFormat, true);
            }

            public void Cleanup()
            {
                reflectionProbeCache.Release();
                reflectionPlanarProbeCache.Release();
                cookieTexArray.Release();
                cubeCookieTexArray.Release();
                areaLightCookieManager.ReleaseResources();

                CoreUtils.Destroy(m_CubeToPanoMaterial);
            }

            public void NewFrame()
            {
                areaLightCookieManager.NewFrame();
                cookieTexArray.NewFrame();
                cubeCookieTexArray.NewFrame();
                reflectionProbeCache.NewFrame();
                reflectionPlanarProbeCache.NewFrame();
            }
        }

        internal class LightLoopLightData
        {
            public ComputeBuffer    directionalLightData { get; private set; }
            public ComputeBuffer    lightData { get; private set; }
            public ComputeBuffer    envLightData { get; private set; }
            public ComputeBuffer    decalData { get; private set; }

            public void Initialize(int directionalCount, int punctualCount, int areaLightCount, int envLightCount, int decalCount)
            {
                directionalLightData = new ComputeBuffer(directionalCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalLightData)));
                lightData = new ComputeBuffer(punctualCount + areaLightCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LightData)));
                envLightData = new ComputeBuffer(envLightCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(EnvLightData)));
                decalData = new ComputeBuffer(decalCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DecalData)));
            }

            public void Cleanup()
            {
                CoreUtils.SafeRelease(directionalLightData);
                CoreUtils.SafeRelease(lightData);
                CoreUtils.SafeRelease(envLightData);
                CoreUtils.SafeRelease(decalData);
            }
        }

        class TileAndClusterData
        {
            public ComputeBuffer lightVolumeDataBuffer;
            public ComputeBuffer convexBoundsBuffer;
            public ComputeBuffer AABBBoundsBuffer;
            public ComputeBuffer lightList;
            public ComputeBuffer tileList;
            public ComputeBuffer tileFeatureFlags;
            public ComputeBuffer dispatchIndirectBuffer;
            public ComputeBuffer bigTileLightList;        // used for pre-pass coarse culling on 64x64 tiles
            public ComputeBuffer perVoxelLightLists;
            public ComputeBuffer perVoxelOffset;
            public ComputeBuffer perTileLogBaseTweak;
            public ComputeBuffer globalLightListAtomic;

            public void Initialize()
            {
                globalLightListAtomic = new ComputeBuffer(1, sizeof(uint));
            }

            public void AllocateResolutionDependentBuffers(HDCamera hdCamera, int width, int height, int viewCount, int maxLightOnScreen)
            {
                var nrTilesX = (width + LightDefinitions.s_TileSizeFptl - 1) / LightDefinitions.s_TileSizeFptl;
                var nrTilesY = (height + LightDefinitions.s_TileSizeFptl - 1) / LightDefinitions.s_TileSizeFptl;
                var nrTiles = nrTilesX * nrTilesY * viewCount;
                const int capacityUShortsPerTile = 32;
                const int dwordsPerTile = (capacityUShortsPerTile + 1) >> 1;        // room for 31 lights and a nrLights value.

                lightList = new ComputeBuffer((int)LightCategory.Count * dwordsPerTile * nrTiles, sizeof(uint));       // enough list memory for a 4k x 4k display
                tileList = new ComputeBuffer((int)LightDefinitions.s_NumFeatureVariants * nrTiles, sizeof(uint));
                tileFeatureFlags = new ComputeBuffer(nrTiles, sizeof(uint));

                // Cluster
                {
                    var nrClustersX = (width + LightDefinitions.s_TileSizeClustered - 1) / LightDefinitions.s_TileSizeClustered;
                    var nrClustersY = (height + LightDefinitions.s_TileSizeClustered - 1) / LightDefinitions.s_TileSizeClustered;
                    var nrClusterTiles = nrClustersX * nrClustersY * viewCount;

                    perVoxelOffset = new ComputeBuffer((int)LightCategory.Count * (1 << k_Log2NumClusters) * nrClusterTiles, sizeof(uint));
                    perVoxelLightLists = new ComputeBuffer(NumLightIndicesPerClusteredTile() * nrClusterTiles, sizeof(uint));

                    if (k_UseDepthBuffer)
                    {
                        perTileLogBaseTweak = new ComputeBuffer(nrClusterTiles, sizeof(float));
                    }
                }

                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass))
                {
                    var nrBigTilesX = (width + 63) / 64;
                    var nrBigTilesY = (height + 63) / 64;
                    var nrBigTiles = nrBigTilesX * nrBigTilesY * viewCount;
                    bigTileLightList = new ComputeBuffer(LightDefinitions.s_MaxNrBigTileLightsPlusOne * nrBigTiles, sizeof(uint));
                }

                // The bounds and light volumes are view-dependent, and AABB is additionally projection dependent.
                // TODO: I don't think k_MaxLightsOnScreen corresponds to the actual correct light count for cullable light types (punctual, area, env, decal)
                AABBBoundsBuffer = new ComputeBuffer(viewCount * 2 * maxLightOnScreen, 4 * sizeof(float));
                convexBoundsBuffer = new ComputeBuffer(viewCount * maxLightOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SFiniteLightBound)));
                lightVolumeDataBuffer = new ComputeBuffer(viewCount * maxLightOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LightVolumeData)));

                // Need 3 ints for DispatchIndirect, but need 4 ints for DrawInstancedIndirect.
                dispatchIndirectBuffer = new ComputeBuffer(viewCount * LightDefinitions.s_NumFeatureVariants * 4, sizeof(uint), ComputeBufferType.IndirectArguments);
            }

            public void ReleaseResolutionDependentBuffers()
            {
                CoreUtils.SafeRelease(lightList);
                CoreUtils.SafeRelease(tileList);
                CoreUtils.SafeRelease(tileFeatureFlags);

                // enableClustered
                CoreUtils.SafeRelease(perVoxelLightLists);
                CoreUtils.SafeRelease(perVoxelOffset);
                CoreUtils.SafeRelease(perTileLogBaseTweak);

                // enableBigTilePrepass
                CoreUtils.SafeRelease(bigTileLightList);

                // LightList building
                CoreUtils.SafeRelease(AABBBoundsBuffer);
                CoreUtils.SafeRelease(convexBoundsBuffer);
                CoreUtils.SafeRelease(lightVolumeDataBuffer);
                CoreUtils.SafeRelease(dispatchIndirectBuffer);
            }

            public void Cleanup()
            {
                CoreUtils.SafeRelease(globalLightListAtomic);

                ReleaseResolutionDependentBuffers();
            }
        }

        // TODO: Remove the internal
        internal LightLoopTextureCaches m_TextureCaches = new LightLoopTextureCaches();
        // TODO: Remove the internal
        internal LightLoopLightData m_LightLoopLightData = new LightLoopLightData();
        TileAndClusterData m_TileAndClusterData = new TileAndClusterData();

        // This control if we use cascade borders for directional light by default
        static internal readonly bool s_UseCascadeBorders = true;

        // Keep sorting array around to avoid garbage
        uint[] m_SortKeys = null;

        void UpdateSortKeysArray(int count)
        {
            if (m_SortKeys == null ||count > m_SortKeys.Length)
            {
                m_SortKeys = new uint[count];
            }
        }

        static readonly Matrix4x4 s_FlipMatrixLHSRHS = Matrix4x4.Scale(new Vector3(1, 1, -1));

        public Matrix4x4 GetWorldToViewMatrix(HDCamera hdCamera, int viewIndex)
        {
            var viewMatrix = (hdCamera.xr.enabled ? hdCamera.xr.GetViewMatrix(viewIndex) : hdCamera.camera.worldToCameraMatrix);

            // camera.worldToCameraMatrix is RHS and Unity's transforms are LHS, we need to flip it to work with transforms
            return s_FlipMatrixLHSRHS * viewMatrix;
        }

        // Keep track of the maximum number of XR instanced views
        int m_MaxViewCount = 1;

        // Matrix used for LightList building, keep them around to avoid GC
        Matrix4x4[] m_LightListProjMatrices = new Matrix4x4[ShaderConfig.s_XrMaxViews];
        Matrix4x4[] m_LightListProjscrMatrices = new Matrix4x4[ShaderConfig.s_XrMaxViews];
        Matrix4x4[] m_LightListInvProjscrMatrices = new Matrix4x4[ShaderConfig.s_XrMaxViews];
        Matrix4x4[] m_LightListProjHMatrices = new Matrix4x4[ShaderConfig.s_XrMaxViews];
        Matrix4x4[] m_LightListInvProjHMatrices = new Matrix4x4[ShaderConfig.s_XrMaxViews];

        internal class LightList
        {
            public List<DirectionalLightData> directionalLights;
            public List<LightData> lights;
            public List<EnvLightData> envLights;
            public int punctualLightCount;
            public int areaLightCount;

            public struct LightsPerView
            {
                public List<SFiniteLightBound> bounds;
                public List<LightVolumeData> lightVolumes;
            }

            public List<LightsPerView> lightsPerView;

            public void Clear()
            {
                directionalLights.Clear();
                lights.Clear();
                envLights.Clear();
                punctualLightCount = 0;
                areaLightCount = 0;

                for (int i = 0; i < lightsPerView.Count; ++i)
                {
                    lightsPerView[i].bounds.Clear();
                    lightsPerView[i].lightVolumes.Clear();
                }
            }

            public void Allocate()
            {
                directionalLights = new List<DirectionalLightData>();
                lights = new List<LightData>();
                envLights = new List<EnvLightData>();

                lightsPerView = new List<LightsPerView>();
                for (int i = 0; i < TextureXR.slices; ++i)
                {
                    lightsPerView.Add(new LightsPerView { bounds = new List<SFiniteLightBound>(), lightVolumes = new List<LightVolumeData>() });
                }
            }
        }

        internal LightList m_lightList;
        int m_TotalLightCount = 0;
        int m_densityVolumeCount = 0;
        bool m_enableBakeShadowMask = false; // Track if any light require shadow mask. In this case we will need to enable the keyword shadow mask
        bool m_hasRunLightListPrevFrame = false;

        ComputeShader buildScreenAABBShader { get { return defaultResources.shaders.buildScreenAABBCS; } }
        ComputeShader buildPerTileLightListShader { get { return defaultResources.shaders.buildPerTileLightListCS; } }
        ComputeShader buildPerBigTileLightListShader { get { return defaultResources.shaders.buildPerBigTileLightListCS; } }
        ComputeShader buildPerVoxelLightListShader { get { return defaultResources.shaders.buildPerVoxelLightListCS; } }

        ComputeShader buildMaterialFlagsShader { get { return defaultResources.shaders.buildMaterialFlagsCS; } }
        ComputeShader buildDispatchIndirectShader { get { return defaultResources.shaders.buildDispatchIndirectCS; } }
        ComputeShader clearDispatchIndirectShader { get { return defaultResources.shaders.clearDispatchIndirectCS; } }
        ComputeShader deferredComputeShader { get { return defaultResources.shaders.deferredCS; } }
        ComputeShader contactShadowComputeShader { get { return defaultResources.shaders.contactShadowCS; } }
        Shader screenSpaceShadowsShader { get { return defaultResources.shaders.screenSpaceShadowPS; } }

        Shader deferredTilePixelShader { get { return defaultResources.shaders.deferredTilePS; } }


        static int s_GenAABBKernel;
        static int s_GenAABBKernel_Oblique;
        static int s_GenListPerTileKernel;
        static int s_GenListPerTileKernel_Oblique;
        static int s_GenListPerVoxelKernel;
        static int s_GenListPerVoxelKernelOblique;
        static int s_ClearVoxelAtomicKernel;
        static int s_ClearDispatchIndirectKernel;
        static int s_BuildDispatchIndirectKernel;
        static int s_ClearDrawInstancedIndirectKernel;
        static int s_BuildDrawInstancedIndirectKernel;
        static int s_BuildMaterialFlagsWriteKernel;
        static int s_BuildMaterialFlagsOrKernel;

        static int s_shadeOpaqueDirectFptlKernel;
        static int s_shadeOpaqueDirectFptlDebugDisplayKernel;
        static int s_shadeOpaqueDirectShadowMaskFptlKernel;
        static int s_shadeOpaqueDirectShadowMaskFptlDebugDisplayKernel;

        static int[] s_shadeOpaqueIndirectFptlKernels = new int[LightDefinitions.s_NumFeatureVariants];
        static int[] s_shadeOpaqueIndirectShadowMaskFptlKernels = new int[LightDefinitions.s_NumFeatureVariants];

        static int s_deferredContactShadowKernel;
        static int s_deferredContactShadowKernelMSAA;

        static int s_GenListPerBigTileKernel;

        const bool k_UseDepthBuffer = true;      // only has an impact when EnableClustered is true (requires a depth-prepass)

#if !UNITY_EDITOR && UNITY_SWITCH
        const int k_Log2NumClusters = 5;     // accepted range is from 0 to 5 (NR_THREADS is set to 32 on Switch). NumClusters is 1<<g_iLog2NumClusters
#else
        const int k_Log2NumClusters = 6;     // accepted range is from 0 to 6 (NR_THREADS is set to 64 on other platforms). NumClusters is 1<<g_iLog2NumClusters
#endif
        const float k_ClustLogBase = 1.02f;     // each slice 2% bigger than the previous
        float m_ClusterScale;

        static DebugLightVolumes s_lightVolumes = null;


        static Material s_DeferredTileRegularLightingMat;   // stencil-test set to touch regular pixels only
        static Material s_DeferredTileSplitLightingMat;     // stencil-test set to touch split-lighting pixels only
        static Material s_DeferredTileMat;                  // fallback when regular and split-lighting pixels must be touch
        static String[] s_variantNames = new String[LightDefinitions.s_NumFeatureVariants];

        enum ClusterPrepassSource : int
        {
            None = 0,
            BigTile = 1,
            Count = 2,
        }

        enum ClusterDepthSource : int
        {
            NoDepth = 0,
            Depth = 1,
            MSAA_Depth = 2,
            Count = 3,
        }

        static string[,] s_ClusterKernelNames = new string[(int)ClusterPrepassSource.Count, (int)ClusterDepthSource.Count]
        {
            { "TileLightListGen_NoDepthRT", "TileLightListGen_DepthRT", "TileLightListGen_DepthRT_MSAA" },
            { "TileLightListGen_NoDepthRT_SrcBigTile", "TileLightListGen_DepthRT_SrcBigTile", "TileLightListGen_DepthRT_MSAA_SrcBigTile" }
        };
        static string[,] s_ClusterObliqueKernelNames = new string[(int)ClusterPrepassSource.Count, (int)ClusterDepthSource.Count]
        {
            { "TileLightListGen_NoDepthRT", "TileLightListGen_DepthRT_Oblique", "TileLightListGen_DepthRT_MSAA_Oblique" },
            { "TileLightListGen_NoDepthRT_SrcBigTile", "TileLightListGen_DepthRT_SrcBigTile_Oblique", "TileLightListGen_DepthRT_MSAA_SrcBigTile_Oblique" }
        };
        // clustered light list specific buffers and data end

        static int[] s_TempScreenDimArray = new int[2]; // Used to avoid GC stress when calling SetComputeIntParams

        ContactShadows m_ContactShadows = null;
        bool m_EnableContactShadow = false;

        IndirectLightingController m_indirectLightingController = null;

        // Following is an array of material of size eight for all combination of keyword: OUTPUT_SPLIT_LIGHTING - LIGHTLOOP_DISABLE_TILE_AND_CLUSTER - SHADOWS_SHADOWMASK - USE_FPTL_LIGHTLIST/USE_CLUSTERED_LIGHTLIST - DEBUG_DISPLAY
        Material[] m_deferredLightingMaterial;
        Material m_DebugViewTilesMaterial;
        Material m_DebugHDShadowMapMaterial;

        // Directional light
        Light m_CurrentSunLight;
        int m_CurrentShadowSortedSunLightIndex = -1;
        HDAdditionalLightData m_CurrentSunLightAdditionalLightData;
        DirectionalLightData m_CurrentSunLightDirectionalLightData;
        Light GetCurrentSunLight() { return m_CurrentSunLight; }

        // Screen space shadow data
        public struct ScreenSpaceShadowData
        {
            public HDAdditionalLightData additionalLightData;
            public int lightDataIndex;
            public bool valid;
        }

        int m_ScreenSpaceShadowIndex = 0;
        ScreenSpaceShadowData[] m_CurrentScreenSpaceShadowData;
        public ScreenSpaceShadowData GetScreenSpaceShadowData(int screenSpaceShadowIndex) { return m_CurrentScreenSpaceShadowData[screenSpaceShadowIndex]; }

        // Contact shadow index reseted at the beginning of each frame, used to generate the contact shadow mask
        int m_ContactShadowIndex;

        // shadow related stuff
        HDShadowManager m_ShadowManager;
        HDShadowInitParameters m_ShadowInitParameters;

        Material m_CopyStencil;
        // We need a copy for SSR because setting render states through uniform constants does not work with MaterialPropertyBlocks so it would override values set for the regular copy
        Material m_CopyStencilForSSR;

        // Used to shadow shadow maps with use selection enabled in the debug menu
        int m_DebugSelectedLightShadowIndex;
        int m_DebugSelectedLightShadowCount;

        bool HasLightToCull()
        {
            return m_TotalLightCount > 0;
        }

        static int GetNumTileBigTileX(HDCamera hdCamera)
        {
            return HDUtils.DivRoundUp((int)hdCamera.screenSize.x, LightDefinitions.s_TileSizeBigTile);
        }

        static int GetNumTileBigTileY(HDCamera hdCamera)
        {
            return HDUtils.DivRoundUp((int)hdCamera.screenSize.y, LightDefinitions.s_TileSizeBigTile);
        }

        static int GetNumTileFtplX(HDCamera hdCamera)
        {
            return HDUtils.DivRoundUp((int)hdCamera.screenSize.x, LightDefinitions.s_TileSizeFptl);
        }

        static int GetNumTileFtplY(HDCamera hdCamera)
        {
            return HDUtils.DivRoundUp((int)hdCamera.screenSize.y, LightDefinitions.s_TileSizeFptl);
        }

        static int GetNumTileClusteredX(HDCamera hdCamera)
        {
            return HDUtils.DivRoundUp((int)hdCamera.screenSize.x, LightDefinitions.s_TileSizeClustered);
        }

        static int GetNumTileClusteredY(HDCamera hdCamera)
        {
            return HDUtils.DivRoundUp((int)hdCamera.screenSize.y, LightDefinitions.s_TileSizeClustered);
        }

        void InitShadowSystem(HDRenderPipelineAsset hdAsset, RenderPipelineResources defaultResources)
        {
            m_ShadowInitParameters = hdAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams;
            m_ShadowManager = HDShadowManager.instance;
            m_ShadowManager.InitShadowManager(
                defaultResources,
                m_ShadowInitParameters.directionalShadowsDepthBits,
                m_ShadowInitParameters.punctualLightShadowAtlas,
                m_ShadowInitParameters.areaLightShadowAtlas,
                m_ShadowInitParameters.maxShadowRequests,
                defaultResources.shaders.shadowClearPS
            );
        }

        void DeinitShadowSystem()
        {
            if(m_ShadowManager != null)
            {
                m_ShadowManager.Dispose();
                m_ShadowManager = null;
            }
        }

        static bool GetFeatureVariantsEnabled(FrameSettings frameSettings) =>
            frameSettings.litShaderMode == LitShaderMode.Deferred
            && frameSettings.IsEnabled(FrameSettingsField.DeferredTile)
            && (frameSettings.IsEnabled(FrameSettingsField.ComputeLightVariants) || frameSettings.IsEnabled(FrameSettingsField.ComputeMaterialVariants));

        int GetDeferredLightingMaterialIndex(int outputSplitLighting, int shadowMask, int debugDisplay)
        {
            return (outputSplitLighting) | (shadowMask << 1) | (debugDisplay << 2);
        }

        Material GetDeferredLightingMaterial(bool outputSplitLighting, bool shadowMask, bool debugDisplayEnabled)
        {
            int index = GetDeferredLightingMaterialIndex(outputSplitLighting ? 1 : 0,
                shadowMask ? 1 : 0,
                debugDisplayEnabled ? 1 : 0);

            return m_deferredLightingMaterial[index];
        }

        void InitializeLightLoop(IBLFilterBSDF[] iBLFilterBSDFArray)
        {
            var lightLoopSettings = asset.currentPlatformRenderPipelineSettings.lightLoopSettings;

            m_lightList = new LightList();
            m_lightList.Allocate();

            m_DebugViewTilesMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.debugViewTilesPS);
            m_DebugHDShadowMapMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.debugHDShadowMapPS);

            m_MaxDirectionalLightsOnScreen = lightLoopSettings.maxDirectionalLightsOnScreen;
            m_MaxPunctualLightsOnScreen = lightLoopSettings.maxPunctualLightsOnScreen;
            m_MaxAreaLightsOnScreen = lightLoopSettings.maxAreaLightsOnScreen;
            m_MaxDecalsOnScreen = lightLoopSettings.maxDecalsOnScreen;
            m_MaxEnvLightsOnScreen = lightLoopSettings.maxEnvLightsOnScreen;
            m_MaxLightsOnScreen = m_MaxDirectionalLightsOnScreen + m_MaxPunctualLightsOnScreen + m_MaxAreaLightsOnScreen + m_MaxEnvLightsOnScreen;

            s_GenAABBKernel = buildScreenAABBShader.FindKernel("ScreenBoundsAABB");
            s_GenAABBKernel_Oblique = buildScreenAABBShader.FindKernel("ScreenBoundsAABB_Oblique");

            // Cluster
            {
                s_ClearVoxelAtomicKernel = buildPerVoxelLightListShader.FindKernel("ClearAtomic");
            }

            s_GenListPerBigTileKernel = buildPerBigTileLightListShader.FindKernel("BigTileLightListGen");

            s_BuildDispatchIndirectKernel = buildDispatchIndirectShader.FindKernel("BuildDispatchIndirect");
            s_ClearDispatchIndirectKernel = clearDispatchIndirectShader.FindKernel("ClearDispatchIndirect");

            s_BuildDrawInstancedIndirectKernel = buildDispatchIndirectShader.FindKernel("BuildDrawInstancedIndirect");
            s_ClearDrawInstancedIndirectKernel = clearDispatchIndirectShader.FindKernel("ClearDrawInstancedIndirect");

            s_BuildMaterialFlagsOrKernel = buildMaterialFlagsShader.FindKernel("MaterialFlagsGen_Or");
            s_BuildMaterialFlagsWriteKernel = buildMaterialFlagsShader.FindKernel("MaterialFlagsGen_Write");

            s_shadeOpaqueDirectFptlKernel = deferredComputeShader.FindKernel("Deferred_Direct_Fptl");
            s_shadeOpaqueDirectFptlDebugDisplayKernel = deferredComputeShader.FindKernel("Deferred_Direct_Fptl_DebugDisplay");

            s_shadeOpaqueDirectShadowMaskFptlKernel = deferredComputeShader.FindKernel("Deferred_Direct_ShadowMask_Fptl");
            s_shadeOpaqueDirectShadowMaskFptlDebugDisplayKernel = deferredComputeShader.FindKernel("Deferred_Direct_ShadowMask_Fptl_DebugDisplay");

            s_deferredContactShadowKernel = contactShadowComputeShader.FindKernel("DeferredContactShadow");
            s_deferredContactShadowKernelMSAA = contactShadowComputeShader.FindKernel("DeferredContactShadowMSAA");

            for (int variant = 0; variant < LightDefinitions.s_NumFeatureVariants; variant++)
            {
                s_shadeOpaqueIndirectFptlKernels[variant] = deferredComputeShader.FindKernel("Deferred_Indirect_Fptl_Variant" + variant);
                s_shadeOpaqueIndirectShadowMaskFptlKernels[variant] = deferredComputeShader.FindKernel("Deferred_Indirect_ShadowMask_Fptl_Variant" + variant);
            }

            m_TextureCaches.Initialize(asset, defaultResources, iBLFilterBSDFArray);
            // All the allocation of the compute buffers need to happened after the kernel finding in order to avoid the leak loop when a shader does not compile or is not available
            m_LightLoopLightData.Initialize(m_MaxDirectionalLightsOnScreen, m_MaxPunctualLightsOnScreen, m_MaxAreaLightsOnScreen, m_MaxEnvLightsOnScreen, m_MaxDecalsOnScreen);
            m_TileAndClusterData.Initialize();

            // OUTPUT_SPLIT_LIGHTING - SHADOWS_SHADOWMASK - DEBUG_DISPLAY
            m_deferredLightingMaterial = new Material[8];

            for (int outputSplitLighting = 0; outputSplitLighting < 2; ++outputSplitLighting)
            {
                for (int shadowMask = 0; shadowMask < 2; ++shadowMask)
                {
                    for (int debugDisplay = 0; debugDisplay < 2; ++debugDisplay)
                    {
                        int index = GetDeferredLightingMaterialIndex(outputSplitLighting, shadowMask, debugDisplay);

                        m_deferredLightingMaterial[index] = CoreUtils.CreateEngineMaterial(defaultResources.shaders.deferredPS);
                        m_deferredLightingMaterial[index].name = string.Format("{0}_{1}", defaultResources.shaders.deferredPS.name, index);
                        CoreUtils.SetKeyword(m_deferredLightingMaterial[index], "OUTPUT_SPLIT_LIGHTING", outputSplitLighting == 1);
                        CoreUtils.SetKeyword(m_deferredLightingMaterial[index], "SHADOWS_SHADOWMASK", shadowMask == 1);
                        CoreUtils.SetKeyword(m_deferredLightingMaterial[index], "DEBUG_DISPLAY", debugDisplay == 1);

                        m_deferredLightingMaterial[index].SetInt(HDShaderIDs._StencilMask, (int)HDRenderPipeline.StencilBitMask.LightingMask);
                        m_deferredLightingMaterial[index].SetInt(HDShaderIDs._StencilRef, outputSplitLighting == 1 ? (int)StencilLightingUsage.SplitLighting : (int)StencilLightingUsage.RegularLighting);
                        m_deferredLightingMaterial[index].SetInt(HDShaderIDs._StencilCmp, (int)CompareFunction.Equal);
                    }
                }
            }

            // Stencil set to only touch "regular lighting" pixels.
            s_DeferredTileRegularLightingMat = CoreUtils.CreateEngineMaterial(deferredTilePixelShader);
            s_DeferredTileRegularLightingMat.SetInt(HDShaderIDs._StencilRef, (int)StencilLightingUsage.RegularLighting);
            s_DeferredTileRegularLightingMat.SetInt(HDShaderIDs._StencilCmp, (int)CompareFunction.Equal);

            // Stencil set to only touch "split-lighting" pixels.
            s_DeferredTileSplitLightingMat = CoreUtils.CreateEngineMaterial(deferredTilePixelShader);
            s_DeferredTileSplitLightingMat.SetInt(HDShaderIDs._StencilRef, (int)StencilLightingUsage.SplitLighting);
            s_DeferredTileSplitLightingMat.SetInt(HDShaderIDs._StencilCmp, (int)CompareFunction.Equal);

            // Stencil set to touch all pixels excepted background/sky.
            s_DeferredTileMat = CoreUtils.CreateEngineMaterial(deferredTilePixelShader);
            s_DeferredTileMat.SetInt(HDShaderIDs._StencilRef, (int)StencilLightingUsage.NoLighting);
            s_DeferredTileMat.SetInt(HDShaderIDs._StencilCmp, (int)CompareFunction.NotEqual);

            for (int i = 0; i < LightDefinitions.s_NumFeatureVariants; ++i)
                s_variantNames[i] = "VARIANT" + i;

            m_DefaultTexture2DArray = new Texture2DArray(1, 1, 1, TextureFormat.ARGB32, false);
            m_DefaultTexture2DArray.hideFlags = HideFlags.HideAndDontSave;
            m_DefaultTexture2DArray.name = CoreUtils.GetTextureAutoName(1, 1, TextureFormat.ARGB32, depth: 1, dim: TextureDimension.Tex2DArray, name: "LightLoopDefault");
            m_DefaultTexture2DArray.SetPixels32(new Color32[1] { new Color32(128, 128, 128, 128) }, 0);
            m_DefaultTexture2DArray.Apply();

            m_DefaultTextureCube = new Cubemap(16, TextureFormat.ARGB32, false);
            m_DefaultTextureCube.Apply();

            // Setup shadow algorithms
            var shadowParams = asset.currentPlatformRenderPipelineSettings.hdShadowInitParams;
            var shadowKeywords = new[]{"SHADOW_LOW", "SHADOW_MEDIUM", "SHADOW_HIGH"};
            foreach (var p in shadowKeywords)
                Shader.DisableKeyword(p);
            Shader.EnableKeyword(shadowKeywords[(int)shadowParams.shadowFilteringQuality]);

            InitShadowSystem(asset, defaultResources);

            s_lightVolumes = new DebugLightVolumes();
            s_lightVolumes.InitData(defaultResources);

            // Screen space shadow
            int numMaxShadows = Math.Max(m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadows, 1);
            m_CurrentScreenSpaceShadowData = new ScreenSpaceShadowData[numMaxShadows];

            m_CopyStencil = CoreUtils.CreateEngineMaterial(defaultResources.shaders.copyStencilBufferPS);
            m_CopyStencilForSSR = CoreUtils.CreateEngineMaterial(defaultResources.shaders.copyStencilBufferPS);
        }

        void CleanupLightLoop()
        {
            s_lightVolumes.ReleaseData();

            DeinitShadowSystem();

            CoreUtils.Destroy(m_DefaultTexture2DArray);
            CoreUtils.Destroy(m_DefaultTextureCube);

            m_TextureCaches.Cleanup();
            m_LightLoopLightData.Cleanup();
            m_TileAndClusterData.Cleanup();

            LightLoopReleaseResolutionDependentBuffers();

            for (int outputSplitLighting = 0; outputSplitLighting < 2; ++outputSplitLighting)
            {
                for (int shadowMask = 0; shadowMask < 2; ++shadowMask)
                {
                    for (int debugDisplay = 0; debugDisplay < 2; ++debugDisplay)
                    {
                        int index = GetDeferredLightingMaterialIndex(outputSplitLighting, shadowMask, debugDisplay);
                        CoreUtils.Destroy(m_deferredLightingMaterial[index]);
                    }
                }
            }

            CoreUtils.Destroy(s_DeferredTileRegularLightingMat);
            CoreUtils.Destroy(s_DeferredTileSplitLightingMat);
            CoreUtils.Destroy(s_DeferredTileMat);

            CoreUtils.Destroy(m_DebugViewTilesMaterial);
            CoreUtils.Destroy(m_DebugHDShadowMapMaterial);

            CoreUtils.Destroy(m_CopyStencil);
            CoreUtils.Destroy(m_CopyStencilForSSR);
        }

        void LightLoopNewFrame(FrameSettings frameSettings)
        {
            m_ContactShadows = VolumeManager.instance.stack.GetComponent<ContactShadows>();
            m_EnableContactShadow = frameSettings.IsEnabled(FrameSettingsField.ContactShadows) && m_ContactShadows.enable.value && m_ContactShadows.length.value > 0;
            m_indirectLightingController = VolumeManager.instance.stack.GetComponent<IndirectLightingController>();

            m_ContactShadowIndex = 0;

            // Cluster
            {
                var clustPrepassSourceIdx = frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass) ? ClusterPrepassSource.BigTile : ClusterPrepassSource.None;
                var clustDepthSourceIdx = ClusterDepthSource.NoDepth;
                if (k_UseDepthBuffer)
                {
                    if (frameSettings.IsEnabled(FrameSettingsField.MSAA))
                        clustDepthSourceIdx = ClusterDepthSource.MSAA_Depth;
                    else
                        clustDepthSourceIdx = ClusterDepthSource.Depth;
                }
                var kernelName = s_ClusterKernelNames[(int)clustPrepassSourceIdx, (int)clustDepthSourceIdx];
                var kernelObliqueName = s_ClusterObliqueKernelNames[(int)clustPrepassSourceIdx, (int)clustDepthSourceIdx];

                s_GenListPerVoxelKernel = buildPerVoxelLightListShader.FindKernel(kernelName);
                s_GenListPerVoxelKernelOblique = buildPerVoxelLightListShader.FindKernel(kernelObliqueName);
            }

            if (GetFeatureVariantsEnabled(frameSettings))
            {
                s_GenListPerTileKernel = buildPerTileLightListShader.FindKernel(frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass) ? "TileLightListGen_SrcBigTile_FeatureFlags" : "TileLightListGen_FeatureFlags");
                s_GenListPerTileKernel_Oblique = buildPerTileLightListShader.FindKernel(frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass) ? "TileLightListGen_SrcBigTile_FeatureFlags_Oblique" : "TileLightListGen_FeatureFlags_Oblique");

            }
            else
            {
                s_GenListPerTileKernel = buildPerTileLightListShader.FindKernel(frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass) ? "TileLightListGen_SrcBigTile" : "TileLightListGen");
                s_GenListPerTileKernel_Oblique = buildPerTileLightListShader.FindKernel(frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass) ? "TileLightListGen_SrcBigTile_Oblique" : "TileLightListGen_Oblique");
            }

            m_TextureCaches.NewFrame();
        }

        bool LightLoopNeedResize(HDCamera hdCamera, TileAndClusterData tileAndClusterData)
        {
            return tileAndClusterData.lightList == null || tileAndClusterData.tileList == null || tileAndClusterData.tileFeatureFlags == null ||
                tileAndClusterData.AABBBoundsBuffer == null || tileAndClusterData.convexBoundsBuffer == null || tileAndClusterData.lightVolumeDataBuffer == null ||
                (tileAndClusterData.bigTileLightList == null && hdCamera.frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass)) ||
                (tileAndClusterData.dispatchIndirectBuffer == null && hdCamera.frameSettings.IsEnabled(FrameSettingsField.DeferredTile)) ||
                (tileAndClusterData.perVoxelLightLists == null) || (hdCamera.viewCount > m_MaxViewCount);
        }

        void LightLoopReleaseResolutionDependentBuffers()
        {
            m_MaxViewCount = 1;
            m_TileAndClusterData.ReleaseResolutionDependentBuffers();
        }

        static int NumLightIndicesPerClusteredTile()
        {
            return 32 * (1 << k_Log2NumClusters);       // total footprint for all layers of the tile (measured in light index entries)
        }

        void LightLoopAllocResolutionDependentBuffers(HDCamera hdCamera, int width, int height)
        {
            m_MaxViewCount = Math.Max(hdCamera.viewCount, m_MaxViewCount);
            m_TileAndClusterData.AllocateResolutionDependentBuffers(hdCamera, width, height, m_MaxViewCount, m_MaxLightsOnScreen);
        }

        internal static Matrix4x4 WorldToCamera(Camera camera)
        {
            // camera.worldToCameraMatrix is RHS and Unity's transforms are LHS
            // We need to flip it to work with transforms
            return s_FlipMatrixLHSRHS * camera.worldToCameraMatrix;
        }

        // For light culling system, we need non oblique projection matrices
        static Matrix4x4 CameraProjectionNonObliqueLHS(HDCamera camera)
        {
            // camera.projectionMatrix expect RHS data and Unity's transforms are LHS
            // We need to flip it to work with transforms
            return camera.nonObliqueProjMatrix * s_FlipMatrixLHSRHS;
        }

        Vector3 GetLightColor(VisibleLight light)
        {
            return new Vector3(light.finalColor.r, light.finalColor.g, light.finalColor.b);
        }

        static float Saturate(float x)
        {
            return Mathf.Max(0, Mathf.Min(x, 1));
        }

        static float Rcp(float x)
        {
            return 1.0f / x;
        }

        static float Rsqrt(float x)
        {
            return Rcp(Mathf.Sqrt(x));
        }

        static float ComputeCosineOfHorizonAngle(float r, float R)
        {
            float sinHoriz = R * Rcp(r);
            return -Mathf.Sqrt(Saturate(1 - sinHoriz * sinHoriz));
        }

        static float ChapmanUpperApprox(float z, float cosTheta)
        {
            float c = cosTheta;
            float n = 0.761643f * ((1 + 2 * z) - (c * c * z));
            float d = c * z + Mathf.Sqrt(z * (1.47721f + 0.273828f * (c * c * z)));

            return 0.5f * c + (n * Rcp(d));
        }

        static float ChapmanHorizontal(float z)
        {
            float r = Rsqrt(z);
            float s = z * r; // sqrt(z)

            return 0.626657f * (r + 2 * s);
        }

        static Vector3 ComputeAtmosphericOpticalDepth(float r, float cosTheta, bool alwaysAboveHorizon = false)
        {
            var skySettings = VolumeManager.instance.stack.GetComponent<PhysicallyBasedSky>();
            Debug.Assert(skySettings != null);

            float R = skySettings.planetaryRadius.value;

            Vector2 H    = new Vector2(skySettings.GetAirScaleHeight(), skySettings.GetAerosolScaleHeight());
            Vector2 rcpH = new Vector2(Rcp(H.x), Rcp(H.y));

            Vector2 z = r * rcpH;
            Vector2 Z = R * rcpH;

            float cosHoriz = ComputeCosineOfHorizonAngle(r, R);
	        float sinTheta = Mathf.Sqrt(Saturate(1 - cosTheta * cosTheta));

            Vector2 ch;
            ch.x = ChapmanUpperApprox(z.x, Mathf.Abs(cosTheta)) * Mathf.Exp(Z.x - z.x); // Rescaling adds 'exp'
            ch.y = ChapmanUpperApprox(z.y, Mathf.Abs(cosTheta)) * Mathf.Exp(Z.y - z.y); // Rescaling adds 'exp'

            if ((!alwaysAboveHorizon) && (cosTheta < cosHoriz)) // Below horizon, intersect sphere
	        {
		        float sinGamma = (r / R) * sinTheta;
		        float cosGamma = Mathf.Sqrt(Saturate(1 - sinGamma * sinGamma));

		        Vector2 ch_2;
                ch_2.x = ChapmanUpperApprox(Z.x, cosGamma); // No need to rescale
                ch_2.y = ChapmanUpperApprox(Z.y, cosGamma); // No need to rescale

		        ch = ch_2 - ch;
            }
            else if (cosTheta < 0)   // Above horizon, lower hemisphere
            {
    	        // z_0 = n * r_0 = (n * r) * sin(theta) = z * sin(theta).
                // Ch(z, theta) = 2 * exp(z - z_0) * Ch(z_0, Pi/2) - Ch(z, Pi - theta).
                Vector2 z_0  = z * sinTheta;
                Vector2 b    = new Vector2(Mathf.Exp(Z.x - z_0.x), Mathf.Exp(Z.x - z_0.x)); // Rescaling cancels out 'z' and adds 'Z'
                Vector2 a;
                a.x         = 2 * ChapmanHorizontal(z_0.x);
                a.y         = 2 * ChapmanHorizontal(z_0.y);
                Vector2 ch_2 = a * b;

                ch = ch_2 - ch;
            }

            Vector2 optDepth = ch * H;

            Vector3 airExtinction     = skySettings.GetAirExtinctionCoefficient();
            float   aerosolExtinction = skySettings.GetAerosolExtinctionCoefficient();

            return new Vector3(optDepth.x * airExtinction.x + optDepth.y * aerosolExtinction,
                               optDepth.x * airExtinction.y + optDepth.y * aerosolExtinction,
                               optDepth.x * airExtinction.z + optDepth.y * aerosolExtinction);
        }

        // Computes transmittance along the light path segment.
        static Vector3 EvaluateAtmosphericAttenuation(Vector3 L, Vector3 positionWS)
        {
            var skySettings = VolumeManager.instance.stack.GetComponent<PhysicallyBasedSky>();
            Debug.Assert(skySettings != null);

            Vector3 X = positionWS * 0.001f; // Convert m to km
            Vector3 C = skySettings.planetCenterPosition.value;

            float r        = Vector3.Distance(X, C);
            float R        = skySettings.planetaryRadius.value;
            float cosHoriz = ComputeCosineOfHorizonAngle(r, R);
            float cosTheta = Vector3.Dot(X - C, L) * Rcp(r);

            if (cosTheta > cosHoriz) // Above horizon
            {
                Vector3 oDepth = ComputeAtmosphericOpticalDepth(r, cosTheta, true);
                Vector3 transm;

                transm.x = Mathf.Exp(-oDepth.x);
                transm.y = Mathf.Exp(-oDepth.y);
                transm.z = Mathf.Exp(-oDepth.z);

                return transm;
            }
            else
            {
                return Vector3.zero;
            }
        }

        internal bool GetDirectionalLightData(CommandBuffer cmd, HDCamera hdCamera, GPULightType gpuLightType, VisibleLight light,
            Light lightComponent, HDAdditionalLightData additionalLightData, int lightIndex, int shadowIndex,
            DebugDisplaySettings debugDisplaySettings, int sortedIndex, ref int screenSpaceShadowIndex, bool isPysicallyBasedSkyActive)
        {
            // Clamp light list to the maximum allowed lights on screen to avoid ComputeBuffer overflow
            if (m_lightList.directionalLights.Count >= m_MaxDirectionalLightsOnScreen)
                return false;

            bool contributesToLighting = ((additionalLightData.lightDimmer > 0) && (additionalLightData.affectDiffuse || additionalLightData.affectSpecular)) || (additionalLightData.volumetricDimmer > 0);

            if (!contributesToLighting)
                return false;

            // Discard light if disabled in debug display settings
            if (!debugDisplaySettings.data.lightingDebugSettings.showDirectionalLight)
                return false;

            var lightData = new DirectionalLightData();

            lightData.lightLayers = additionalLightData.GetLightLayers();

            // Light direction for directional is opposite to the forward direction
            lightData.forward = light.GetForward();
            // Rescale for cookies and windowing.
            lightData.right      = light.GetRight() * 2 / Mathf.Max(additionalLightData.shapeWidth, 0.001f);
            lightData.up         = light.GetUp() * 2 / Mathf.Max(additionalLightData.shapeHeight, 0.001f);
            lightData.positionRWS = light.GetPosition();
            lightData.color = GetLightColor(light);

            // Caution: This is bad but if additionalData == HDUtils.s_DefaultHDAdditionalLightData it mean we are trying to promote legacy lights, which is the case for the preview for example, so we need to multiply by PI as legacy Unity do implicit divide by PI for direct intensity.
            // So we expect that all light with additionalData == HDUtils.s_DefaultHDAdditionalLightData are currently the one from the preview, light in scene MUST have additionalData
            lightData.color *= (HDUtils.s_DefaultHDAdditionalLightData == additionalLightData) ? Mathf.PI : 1.0f;

            lightData.lightDimmer           = additionalLightData.lightDimmer;
            lightData.diffuseDimmer         = additionalLightData.affectDiffuse  ? additionalLightData.lightDimmer : 0;
            lightData.specularDimmer        = additionalLightData.affectSpecular ? additionalLightData.lightDimmer * hdCamera.frameSettings.specularGlobalDimmer : 0;
            lightData.volumetricLightDimmer = additionalLightData.volumetricDimmer;

            lightData.shadowIndex = lightData.cookieIndex = -1;
            lightData.screenSpaceShadowIndex = -1;
            lightData.isRayTracedContactShadow = 0.0f;


            if (lightComponent != null && lightComponent.cookie != null)
            {
                lightData.tileCookie = lightComponent.cookie.wrapMode == TextureWrapMode.Repeat ? 1 : 0;
                lightData.cookieIndex = m_TextureCaches.cookieTexArray.FetchSlice(cmd, lightComponent.cookie);
            }

            lightData.shadowDimmer           = additionalLightData.shadowDimmer;
            lightData.volumetricShadowDimmer = additionalLightData.volumetricShadowDimmer;
            GetContactShadowMask(additionalLightData, HDAdditionalLightData.ScalableSettings.UseContactShadow(m_Asset), hdCamera, ref lightData.contactShadowMask,ref lightData.isRayTracedContactShadow);
            lightData.shadowTint             = new Vector3(additionalLightData.shadowTint.r, additionalLightData.shadowTint.g, additionalLightData.shadowTint.b);

            // fix up shadow information
            lightData.shadowIndex = shadowIndex;
            if (shadowIndex != -1)
            {
                if (additionalLightData.WillRenderScreenSpaceShadow())
                {
                    lightData.screenSpaceShadowIndex = screenSpaceShadowIndex;
                    screenSpaceShadowIndex++;
                }
                m_CurrentSunLight = lightComponent;
                m_CurrentSunLightAdditionalLightData = additionalLightData;
                m_CurrentSunLightDirectionalLightData = lightData;
                m_CurrentShadowSortedSunLightIndex = sortedIndex;

            }
            //Value of max smoothness is derived from AngularDiameter. Formula results from eyeballing. Angular diameter of 0 results in 1 and angular diameter of 80 results in 0.
            float maxSmoothness = Mathf.Clamp01(1.35f / (1.0f + Mathf.Pow(1.15f * (0.0315f * additionalLightData.angularDiameter + 0.4f),2f)) - 0.11f);
            // Value of max smoothness is from artists point of view, need to convert from perceptual smoothness to roughness
            lightData.minRoughness = (1.0f - maxSmoothness) * (1.0f - maxSmoothness);

            lightData.shadowMaskSelector = Vector4.zero;

            if (IsBakedShadowMaskLight(lightComponent))
            {
                lightData.shadowMaskSelector[lightComponent.bakingOutput.occlusionMaskChannel] = 1.0f;
                lightData.nonLightMappedOnly = lightComponent.lightShadowCasterMode == LightShadowCasterMode.NonLightmappedOnly ? 1 : 0;
            }
            else
            {
                // use -1 to say that we don't use shadow mask
                lightData.shadowMaskSelector.x = -1.0f;
                lightData.nonLightMappedOnly = 0;
            }

            bool interactsWithSky = isPysicallyBasedSkyActive && additionalLightData.interactsWithSky;

            lightData.distanceFromCamera = -1; // Encode 'interactsWithSky'

            if (interactsWithSky)
            {
                lightData.distanceFromCamera = additionalLightData.distance;

                if (ShaderConfig.s_PrecomputedAtmosphericAttenuation != 0)
                {
                    // Ignores distance (at infinity).
                    Vector3 transm = EvaluateAtmosphericAttenuation(-lightData.forward, hdCamera.camera.transform.position);
                    lightData.color.x *= transm.x;
                    lightData.color.y *= transm.y;
                    lightData.color.z *= transm.z;
                }
            }

            lightData.angularDiameter = additionalLightData.angularDiameter * Mathf.Deg2Rad;

            // Fallback to the first non shadow casting directional light.
            m_CurrentSunLight = m_CurrentSunLight == null ? lightComponent : m_CurrentSunLight;

            m_lightList.directionalLights.Add(lightData);

            return true;
        }

        internal bool GetLightData(CommandBuffer cmd, HDCamera hdCamera, HDShadowSettings shadowSettings, GPULightType gpuLightType,
            VisibleLight light, Light lightComponent, HDAdditionalLightData additionalLightData,
            int lightIndex, int shadowIndex, ref Vector3 lightDimensions, DebugDisplaySettings debugDisplaySettings, ref int screenSpaceShadowIndex)
        {
            // Clamp light list to the maximum allowed lights on screen to avoid ComputeBuffer overflow
            if (m_lightList.lights.Count >= m_MaxPunctualLightsOnScreen + m_MaxAreaLightsOnScreen)
            return false;

            // Both of these positions are non-camera-relative.
            float distanceToCamera  = (light.GetPosition() - hdCamera.camera.transform.position).magnitude;
            float lightDistanceFade = HDUtils.ComputeLinearDistanceFade(distanceToCamera, additionalLightData.fadeDistance);

            bool contributesToLighting = ((additionalLightData.lightDimmer > 0) && (additionalLightData.affectDiffuse || additionalLightData.affectSpecular)) || (additionalLightData.volumetricDimmer > 0);
                 contributesToLighting = contributesToLighting && (lightDistanceFade > 0);

            if (!contributesToLighting)
                return false;

            var lightData = new LightData();

            lightData.lightLayers = additionalLightData.GetLightLayers();

            lightData.lightType = gpuLightType;

            lightData.positionRWS = light.GetPosition();

            bool applyRangeAttenuation = additionalLightData.applyRangeAttenuation && (gpuLightType != GPULightType.ProjectorBox);

            // Discard light if disabled in debug display settings
            if (lightData.lightType.IsAreaLight())
            {
                if (!debugDisplaySettings.data.lightingDebugSettings.showAreaLight)
                    return false;
            }
            else
            {
                if (!debugDisplaySettings.data.lightingDebugSettings.showPunctualLight)
                    return false;
            }

            lightData.range = light.range;

            if (applyRangeAttenuation)
            {
                lightData.rangeAttenuationScale = 1.0f / (light.range * light.range);
                lightData.rangeAttenuationBias  = 1.0f;

                if (lightData.lightType == GPULightType.Rectangle)
                {
                    // Rect lights are currently a special case because they use the normalized
                    // [0, 1] attenuation range rather than the regular [0, r] one.
                    lightData.rangeAttenuationScale = 1.0f;
                }
            }
            else // Don't apply any attenuation but do a 'step' at range
            {
                // Solve f(x) = b - (a * x)^2 where x = (d/r)^2.
                // f(0) = huge -> b = huge.
                // f(1) = 0    -> huge - a^2 = 0 -> a = sqrt(huge).
                const float hugeValue = 16777216.0f;
                const float sqrtHuge  = 4096.0f;
                lightData.rangeAttenuationScale = sqrtHuge / (light.range * light.range);
                lightData.rangeAttenuationBias  = hugeValue;

                if (lightData.lightType == GPULightType.Rectangle)
                {
                    // Rect lights are currently a special case because they use the normalized
                    // [0, 1] attenuation range rather than the regular [0, r] one.
                    lightData.rangeAttenuationScale = sqrtHuge;
                }
            }

            lightData.color = GetLightColor(light);

            lightData.forward = light.GetForward();
            lightData.up = light.GetUp();
            lightData.right = light.GetRight();

            lightDimensions.x = additionalLightData.shapeWidth;
            lightDimensions.y = additionalLightData.shapeHeight;
            lightDimensions.z = light.range;

            if (lightData.lightType == GPULightType.ProjectorBox)
            {
                // Rescale for cookies and windowing.
                lightData.right *= 2.0f / Mathf.Max(additionalLightData.shapeWidth, 0.001f);
                lightData.up    *= 2.0f / Mathf.Max(additionalLightData.shapeHeight, 0.001f);
            }
            else if (lightData.lightType == GPULightType.ProjectorPyramid)
            {
                // Get width and height for the current frustum
                var spotAngle = light.spotAngle;

                float frustumWidth, frustumHeight;

                if (additionalLightData.aspectRatio >= 1.0f)
                {
                    frustumHeight = 2.0f * Mathf.Tan(spotAngle * 0.5f * Mathf.Deg2Rad);
                    frustumWidth = frustumHeight * additionalLightData.aspectRatio;
                }
                else
                {
                    frustumWidth = 2.0f * Mathf.Tan(spotAngle * 0.5f * Mathf.Deg2Rad);
                    frustumHeight = frustumWidth / additionalLightData.aspectRatio;
                }

                // Adjust based on the new parametrization.
                lightDimensions.x = frustumWidth;
                lightDimensions.y = frustumHeight;

                // Rescale for cookies and windowing.
                lightData.right *= 2.0f / frustumWidth;
                lightData.up *= 2.0f / frustumHeight;
            }

            if (lightData.lightType == GPULightType.Spot)
            {
                var spotAngle = light.spotAngle;

                var innerConePercent = additionalLightData.innerSpotPercent01;
                var cosSpotOuterHalfAngle = Mathf.Clamp(Mathf.Cos(spotAngle * 0.5f * Mathf.Deg2Rad), 0.0f, 1.0f);
                var sinSpotOuterHalfAngle = Mathf.Sqrt(1.0f - cosSpotOuterHalfAngle * cosSpotOuterHalfAngle);
                var cosSpotInnerHalfAngle = Mathf.Clamp(Mathf.Cos(spotAngle * 0.5f * innerConePercent * Mathf.Deg2Rad), 0.0f, 1.0f); // inner cone

                var val = Mathf.Max(0.0001f, (cosSpotInnerHalfAngle - cosSpotOuterHalfAngle));
                lightData.angleScale = 1.0f / val;
                lightData.angleOffset = -cosSpotOuterHalfAngle * lightData.angleScale;

                // Rescale for cookies and windowing.
                float cotOuterHalfAngle = cosSpotOuterHalfAngle / sinSpotOuterHalfAngle;
                lightData.up    *= cotOuterHalfAngle;
                lightData.right *= cotOuterHalfAngle;
            }
            else
            {
                // These are the neutral values allowing GetAngleAnttenuation in shader code to return 1.0
                lightData.angleScale = 0.0f;
                lightData.angleOffset = 1.0f;
            }

            if (lightData.lightType != GPULightType.Directional && lightData.lightType != GPULightType.ProjectorBox)
            {
                // Store the squared radius of the light to simulate a fill light.
                lightData.size = new Vector2(additionalLightData.shapeRadius * additionalLightData.shapeRadius, 0);
            }

            if (lightData.lightType == GPULightType.Rectangle || lightData.lightType == GPULightType.Tube)
            {
                lightData.size = new Vector2(additionalLightData.shapeWidth, additionalLightData.shapeHeight);
            }

            lightData.lightDimmer           = lightDistanceFade * (additionalLightData.lightDimmer);
            lightData.diffuseDimmer         = lightDistanceFade * (additionalLightData.affectDiffuse  ? additionalLightData.lightDimmer : 0);
            lightData.specularDimmer        = lightDistanceFade * (additionalLightData.affectSpecular ? additionalLightData.lightDimmer * hdCamera.frameSettings.specularGlobalDimmer : 0);
            lightData.volumetricLightDimmer = lightDistanceFade * (additionalLightData.volumetricDimmer);

            lightData.cookieIndex = -1;
            lightData.shadowIndex = -1;
            lightData.screenSpaceShadowIndex = -1;
            lightData.isRayTracedContactShadow = 0.0f;

            HDLightType lightType = additionalLightData.ComputeLightType(lightComponent);

            if (lightComponent != null && lightComponent.cookie != null)
            {
                // TODO: add texture atlas support for cookie textures.
                switch (lightType)
                {
                    case HDLightType.Spot:
                        lightData.cookieIndex = m_TextureCaches.cookieTexArray.FetchSlice(cmd, lightComponent.cookie);
                        break;
                    case HDLightType.Point:
                        lightData.cookieIndex = m_TextureCaches.cubeCookieTexArray.FetchSlice(cmd, lightComponent.cookie);
                        break;
                }
            }
            else if (lightType == HDLightType.Spot && additionalLightData.spotLightShape != SpotLightShape.Cone)
            {
                // Projectors lights must always have a cookie texture.
                // As long as the cache is a texture array and not an atlas, the 4x4 white texture will be rescaled to 128
                lightData.cookieIndex = m_TextureCaches.cookieTexArray.FetchSlice(cmd, Texture2D.whiteTexture);
            }
            else if (lightData.lightType == GPULightType.Rectangle && additionalLightData.areaLightCookie != null)
            {
                lightData.cookieIndex = m_TextureCaches.areaLightCookieManager.FetchSlice(cmd, additionalLightData.areaLightCookie);
            }

            float shadowDistanceFade         = HDUtils.ComputeLinearDistanceFade(distanceToCamera, Mathf.Min(shadowSettings.maxShadowDistance.value, additionalLightData.shadowFadeDistance));
            lightData.shadowDimmer           = shadowDistanceFade * additionalLightData.shadowDimmer;
            lightData.volumetricShadowDimmer = shadowDistanceFade * additionalLightData.volumetricShadowDimmer;
            GetContactShadowMask(additionalLightData, HDAdditionalLightData.ScalableSettings.UseContactShadow(m_Asset), hdCamera, ref lightData.contactShadowMask, ref lightData.isRayTracedContactShadow);
            lightData.shadowTint             = new Vector3(additionalLightData.shadowTint.r, additionalLightData.shadowTint.g, additionalLightData.shadowTint.b);

            // If there is still a free slot in the screen space shadow array and this needs to render a screen space shadow
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing)
                && screenSpaceShadowIndex < m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadows
                && additionalLightData.WillRenderScreenSpaceShadow())
            {
                // Keep track of the shadow map (for indirect lighting and transparents)
                lightData.shadowIndex = shadowIndex;
                additionalLightData.shadowIndex = shadowIndex;

                // Keep track of the screen space shadow data
                lightData.screenSpaceShadowIndex = screenSpaceShadowIndex;
                m_CurrentScreenSpaceShadowData[screenSpaceShadowIndex].additionalLightData = additionalLightData;
                m_CurrentScreenSpaceShadowData[screenSpaceShadowIndex].lightDataIndex = m_lightList.lights.Count;
                m_CurrentScreenSpaceShadowData[screenSpaceShadowIndex].valid = true;
                screenSpaceShadowIndex++;
            }
            else
            {
                // fix up shadow information
                lightData.shadowIndex = shadowIndex;
                additionalLightData.shadowIndex = shadowIndex;
            }

            //Value of max smoothness is derived from Radius. Formula results from eyeballing. Radius of 0 results in 1 and radius of 2.5 results in 0.
            float maxSmoothness = Mathf.Clamp01(1.1725f / (1.01f + Mathf.Pow(1.0f * (additionalLightData.shapeRadius + 0.1f), 2f)) - 0.15f);
            // Value of max smoothness is from artists point of view, need to convert from perceptual smoothness to roughness
            lightData.minRoughness = (1.0f - maxSmoothness) * (1.0f - maxSmoothness);

            lightData.shadowMaskSelector = Vector4.zero;

            if (IsBakedShadowMaskLight(lightComponent))
            {
                lightData.shadowMaskSelector[lightComponent.bakingOutput.occlusionMaskChannel] = 1.0f;
                lightData.nonLightMappedOnly = lightComponent.lightShadowCasterMode == LightShadowCasterMode.NonLightmappedOnly ? 1 : 0;
            }
            else
            {
                // use -1 to say that we don't use shadow mask
                lightData.shadowMaskSelector.x = -1.0f;
                lightData.nonLightMappedOnly = 0;
            }

            m_lightList.lights.Add(lightData);

            return true;
        }

        // TODO: we should be able to do this calculation only with LightData without VisibleLight light, but for now pass both
        void GetLightVolumeDataAndBound(LightCategory lightCategory, GPULightType gpuLightType, LightVolumeType lightVolumeType,
            VisibleLight light, LightData lightData, Vector3 lightDimensions, Matrix4x4 worldToView, int viewIndex)
        {
            // Then Culling side
            var range = lightDimensions.z;
            var lightToWorld = light.localToWorldMatrix;
            Vector3 positionWS = lightData.positionRWS;
            Vector3 positionVS = worldToView.MultiplyPoint(positionWS);

            Matrix4x4 lightToView = worldToView * lightToWorld;
            Vector3 xAxisVS = lightToView.GetColumn(0);
            Vector3 yAxisVS = lightToView.GetColumn(1);
            Vector3 zAxisVS = lightToView.GetColumn(2);

            // Fill bounds
            var bound = new SFiniteLightBound();
            var lightVolumeData = new LightVolumeData();

            lightVolumeData.lightCategory = (uint)lightCategory;
            lightVolumeData.lightVolume = (uint)lightVolumeType;

            if (gpuLightType == GPULightType.Spot || gpuLightType == GPULightType.ProjectorPyramid)
            {
                Vector3 lightDir = lightToWorld.GetColumn(2);

                // represents a left hand coordinate system in world space since det(worldToView)<0
                Vector3 vx = xAxisVS;
                Vector3 vy = yAxisVS;
                Vector3 vz = zAxisVS;

                const float pi = 3.1415926535897932384626433832795f;
                const float degToRad = (float)(pi / 180.0);

                var sa = light.spotAngle;
                var cs = Mathf.Cos(0.5f * sa * degToRad);
                var si = Mathf.Sin(0.5f * sa * degToRad);

                if (gpuLightType == GPULightType.ProjectorPyramid)
                {
                    Vector3 lightPosToProjWindowCorner = (0.5f * lightDimensions.x) * vx + (0.5f * lightDimensions.y) * vy + 1.0f * vz;
                    cs = Vector3.Dot(vz, Vector3.Normalize(lightPosToProjWindowCorner));
                    si = Mathf.Sqrt(1.0f - cs * cs);
                }

                const float FltMax = 3.402823466e+38F;
                var ta = cs > 0.0f ? (si / cs) : FltMax;
                var cota = si > 0.0f ? (cs / si) : FltMax;

                //const float cotasa = l.GetCotanHalfSpotAngle();

                // apply nonuniform scale to OBB of spot light
                var squeeze = true;//sa < 0.7f * 90.0f;      // arb heuristic
                var fS = squeeze ? ta : si;
                bound.center = worldToView.MultiplyPoint(positionWS + ((0.5f * range) * lightDir));    // use mid point of the spot as the center of the bounding volume for building screen-space AABB for tiled lighting.

                // scale axis to match box or base of pyramid
                bound.boxAxisX = (fS * range) * vx;
                bound.boxAxisY = (fS * range) * vy;
                bound.boxAxisZ = (0.5f * range) * vz;

                // generate bounding sphere radius
                var fAltDx = si;
                var fAltDy = cs;
                fAltDy = fAltDy - 0.5f;
                //if(fAltDy<0) fAltDy=-fAltDy;

                fAltDx *= range; fAltDy *= range;

                // Handle case of pyramid with this select (currently unused)
                var altDist = Mathf.Sqrt(fAltDy * fAltDy + (true ? 1.0f : 2.0f) * fAltDx * fAltDx);
                bound.radius = altDist > (0.5f * range) ? altDist : (0.5f * range);       // will always pick fAltDist
                bound.scaleXY = squeeze ? new Vector2(0.01f, 0.01f) : new Vector2(1.0f, 1.0f);

                lightVolumeData.lightAxisX = vx;
                lightVolumeData.lightAxisY = vy;
                lightVolumeData.lightAxisZ = vz;
                lightVolumeData.lightPos = positionVS;
                lightVolumeData.radiusSq = range * range;
                lightVolumeData.cotan = cota;
                lightVolumeData.featureFlags = (uint)LightFeatureFlags.Punctual;
            }
            else if (gpuLightType == GPULightType.Point)
            {
                Vector3 vx = xAxisVS;
                Vector3 vy = yAxisVS;
                Vector3 vz = zAxisVS;

                bound.center = positionVS;
                bound.boxAxisX = vx * range;
                bound.boxAxisY = vy * range;
                bound.boxAxisZ = vz * range;
                bound.scaleXY.Set(1.0f, 1.0f);
                bound.radius = range;

                // fill up ldata
                lightVolumeData.lightAxisX = vx;
                lightVolumeData.lightAxisY = vy;
                lightVolumeData.lightAxisZ = vz;
                lightVolumeData.lightPos = bound.center;
                lightVolumeData.radiusSq = range * range;
                lightVolumeData.featureFlags = (uint)LightFeatureFlags.Punctual;
            }
            else if (gpuLightType == GPULightType.Tube)
            {
                Vector3 dimensions = new Vector3(lightDimensions.x + 2 * range, 2 * range, 2 * range); // Omni-directional
                Vector3 extents = 0.5f * dimensions;

                bound.center = positionVS;
                bound.boxAxisX = extents.x * xAxisVS;
                bound.boxAxisY = extents.y * yAxisVS;
                bound.boxAxisZ = extents.z * zAxisVS;
                bound.scaleXY.Set(1.0f, 1.0f);
                bound.radius = extents.magnitude;

                lightVolumeData.lightPos = positionVS;
                lightVolumeData.lightAxisX = xAxisVS;
                lightVolumeData.lightAxisY = yAxisVS;
                lightVolumeData.lightAxisZ = zAxisVS;
                lightVolumeData.boxInnerDist = new Vector3(lightDimensions.x, 0, 0);
                lightVolumeData.boxInvRange.Set(1.0f / range, 1.0f / range, 1.0f / range);
                lightVolumeData.featureFlags = (uint)LightFeatureFlags.Area;
            }
            else if (gpuLightType == GPULightType.Rectangle)
            {
                Vector3 dimensions = new Vector3(lightDimensions.x + 2 * range, lightDimensions.y + 2 * range, range); // One-sided
                Vector3 extents = 0.5f * dimensions;
                Vector3 centerVS = positionVS + extents.z * zAxisVS;

                bound.center = centerVS;
                bound.boxAxisX = extents.x * xAxisVS;
                bound.boxAxisY = extents.y * yAxisVS;
                bound.boxAxisZ = extents.z * zAxisVS;
                bound.scaleXY.Set(1.0f, 1.0f);
                bound.radius = extents.magnitude;

                lightVolumeData.lightPos = centerVS;
                lightVolumeData.lightAxisX = xAxisVS;
                lightVolumeData.lightAxisY = yAxisVS;
                lightVolumeData.lightAxisZ = zAxisVS;
                lightVolumeData.boxInnerDist = extents;
                lightVolumeData.boxInvRange.Set(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
                lightVolumeData.featureFlags = (uint)LightFeatureFlags.Area;
            }
            else if (gpuLightType == GPULightType.ProjectorBox)
            {
                Vector3 dimensions = new Vector3(lightDimensions.x, lightDimensions.y, range);  // One-sided
                Vector3 extents = 0.5f * dimensions;
                Vector3 centerVS = positionVS + extents.z * zAxisVS;

                bound.center = centerVS;
                bound.boxAxisX = extents.x * xAxisVS;
                bound.boxAxisY = extents.y * yAxisVS;
                bound.boxAxisZ = extents.z * zAxisVS;
                bound.radius = extents.magnitude;
                bound.scaleXY.Set(1.0f, 1.0f);

                lightVolumeData.lightPos = centerVS;
                lightVolumeData.lightAxisX = xAxisVS;
                lightVolumeData.lightAxisY = yAxisVS;
                lightVolumeData.lightAxisZ = zAxisVS;
                lightVolumeData.boxInnerDist = extents;
                lightVolumeData.boxInvRange.Set(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
                lightVolumeData.featureFlags = (uint)LightFeatureFlags.Punctual;
            }
            else if (gpuLightType == GPULightType.Disc)
            {
                //not supported at real time at the moment
            }
            else
            {
                Debug.Assert(false, "TODO: encountered an unknown GPULightType.");
            }

            m_lightList.lightsPerView[viewIndex].bounds.Add(bound);
            m_lightList.lightsPerView[viewIndex].lightVolumes.Add(lightVolumeData);
        }

        internal bool GetEnvLightData(CommandBuffer cmd, HDCamera hdCamera, HDProbe probe, DebugDisplaySettings debugDisplaySettings, ref EnvLightData envLightData)
        {
            Camera camera = hdCamera.camera;

            // For now we won't display real time probe when rendering one.
            // TODO: We may want to display last frame result but in this case we need to be careful not to update the atlas before all realtime probes are rendered (for frame coherency).
            // Unfortunately we don't have this information at the moment.
            if (probe.mode == ProbeSettings.Mode.Realtime && camera.cameraType == CameraType.Reflection)
                return false;

            // Discard probe if disabled in debug menu
            if (!debugDisplaySettings.data.lightingDebugSettings.showReflectionProbe)
                return false;

            // Discard probe if its distance is too far or if its weight is at 0
            float weight = HDUtils.ComputeWeightedLinearFadeDistance(probe.transform.position, camera.transform.position, probe.weight, probe.fadeDistance);
            if (weight <= 0f)
                return false;

            var capturePosition = Vector3.zero;
            var influenceToWorld = probe.influenceToWorld;

            // 31 bits index, 1 bit cache type
            var envIndex = int.MinValue;
            switch (probe)
            {
                case PlanarReflectionProbe planarProbe:
                    {
                        if (probe.mode == ProbeSettings.Mode.Realtime
                            && !hdCamera.frameSettings.IsEnabled(FrameSettingsField.RealtimePlanarReflection))
                            break;

                        var fetchIndex = m_TextureCaches.reflectionPlanarProbeCache.FetchSlice(cmd, probe.texture);
                        // Indices start at 1, because -0 == 0, we can know from the bit sign which cache to use
                        envIndex = fetchIndex == -1 ? int.MinValue : -(fetchIndex + 1);

                        var renderData = planarProbe.renderData;
                        var worldToCameraRHSMatrix = renderData.worldToCameraRHS;
                        var projectionMatrix = renderData.projectionMatrix;

                        // We don't need to provide the capture position
                        // It is already encoded in the 'worldToCameraRHSMatrix'
                        capturePosition = Vector3.zero;

                        // get the device dependent projection matrix
                        var gpuProj = GL.GetGPUProjectionMatrix(projectionMatrix, true);
                        var gpuView = worldToCameraRHSMatrix;
                        var vp = gpuProj * gpuView;
                        m_TextureCaches.env2DCaptureVP[fetchIndex] = vp;

                        var capturedForwardWS = renderData.captureRotation * Vector3.forward;
                        //capturedForwardWS.z *= -1; // Transform to RHS standard
                        m_TextureCaches.env2DCaptureForward[fetchIndex * 3 + 0] = capturedForwardWS.x;
                        m_TextureCaches.env2DCaptureForward[fetchIndex * 3 + 1] = capturedForwardWS.y;
                        m_TextureCaches.env2DCaptureForward[fetchIndex * 3 + 2] = capturedForwardWS.z;
                        break;
                    }
                case HDAdditionalReflectionData _:
                    {
                        envIndex = m_TextureCaches.reflectionProbeCache.FetchSlice(cmd, probe.texture);
                        // Indices start at 1, because -0 == 0, we can know from the bit sign which cache to use
                        envIndex = envIndex == -1 ? int.MinValue : (envIndex + 1);

                        // Calculate settings to use for the probe
                        var probePositionSettings = ProbeCapturePositionSettings.ComputeFrom(probe, camera.transform);
                        HDRenderUtilities.ComputeCameraSettingsFromProbeSettings(
                            probe.settings, probePositionSettings,
                            out _, out var cameraPositionSettings, 0
                        );
                        capturePosition = cameraPositionSettings.position;

                        break;
                    }
            }
            // int.MinValue means that the texture is not ready yet (ie not convolved/compressed yet)
            if (envIndex == int.MinValue)
                return false;

            InfluenceVolume influence = probe.influenceVolume;
            envLightData.lightLayers = probe.lightLayersAsUInt;
            envLightData.influenceShapeType = influence.envShape;
            envLightData.weight = weight;
            envLightData.multiplier = probe.multiplier * m_indirectLightingController.indirectSpecularIntensity.value;
            envLightData.rangeCompressionFactorCompensation = Mathf.Max(probe.rangeCompressionFactor, 1e-6f);
            envLightData.influenceExtents = influence.extents;
            switch (influence.envShape)
            {
                case EnvShapeType.Box:
                    envLightData.blendNormalDistancePositive = influence.boxBlendNormalDistancePositive;
                    envLightData.blendNormalDistanceNegative = influence.boxBlendNormalDistanceNegative;
                    envLightData.blendDistancePositive = influence.boxBlendDistancePositive;
                    envLightData.blendDistanceNegative = influence.boxBlendDistanceNegative;
                    envLightData.boxSideFadePositive = influence.boxSideFadePositive;
                    envLightData.boxSideFadeNegative = influence.boxSideFadeNegative;
                    break;
                case EnvShapeType.Sphere:
                    envLightData.blendNormalDistancePositive.x = influence.sphereBlendNormalDistance;
                    envLightData.blendDistancePositive.x = influence.sphereBlendDistance;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unknown EnvShapeType");
            }

            envLightData.influenceRight = influenceToWorld.GetColumn(0).normalized;
            envLightData.influenceUp = influenceToWorld.GetColumn(1).normalized;
            envLightData.influenceForward = influenceToWorld.GetColumn(2).normalized;
            envLightData.capturePositionRWS = capturePosition;
            envLightData.influencePositionRWS = influenceToWorld.GetColumn(3);

            envLightData.envIndex = envIndex;

            // Proxy data
            var proxyToWorld = probe.proxyToWorld;
            envLightData.proxyExtents = probe.proxyExtents;
            envLightData.minProjectionDistance = probe.isProjectionInfinite ? 65504f : 0;
            envLightData.proxyRight = proxyToWorld.GetColumn(0).normalized;
            envLightData.proxyUp = proxyToWorld.GetColumn(1).normalized;
            envLightData.proxyForward = proxyToWorld.GetColumn(2).normalized;
            envLightData.proxyPositionRWS = proxyToWorld.GetColumn(3);

            return true;
        }

        void GetEnvLightVolumeDataAndBound(HDProbe probe, LightVolumeType lightVolumeType, Matrix4x4 worldToView, int viewIndex)
        {
            var bound = new SFiniteLightBound();
            var lightVolumeData = new LightVolumeData();

            // C is reflection volume center in world space (NOT same as cube map capture point)
            var influenceExtents = probe.influenceExtents;       // 0.5f * Vector3.Max(-boxSizes[p], boxSizes[p]);

            var influenceToWorld = probe.influenceToWorld;

            // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
            var influenceRightVS = worldToView.MultiplyVector(influenceToWorld.GetColumn(0).normalized);
            var influenceUpVS = worldToView.MultiplyVector(influenceToWorld.GetColumn(1).normalized);
            var influenceForwardVS = worldToView.MultiplyVector(influenceToWorld.GetColumn(2).normalized);
            var influencePositionVS = worldToView.MultiplyPoint(influenceToWorld.GetColumn(3));

            lightVolumeData.lightCategory = (uint)LightCategory.Env;
            lightVolumeData.lightVolume = (uint)lightVolumeType;
            lightVolumeData.featureFlags = (uint)LightFeatureFlags.Env;

            switch (lightVolumeType)
            {
                case LightVolumeType.Sphere:
                {
                    lightVolumeData.lightPos = influencePositionVS;
                    lightVolumeData.radiusSq = influenceExtents.x * influenceExtents.x;
                    lightVolumeData.lightAxisX = influenceRightVS;
                    lightVolumeData.lightAxisY = influenceUpVS;
                    lightVolumeData.lightAxisZ = influenceForwardVS;

                    bound.center = influencePositionVS;
                    bound.boxAxisX = influenceRightVS * influenceExtents.x;
                    bound.boxAxisY = influenceUpVS * influenceExtents.x;
                    bound.boxAxisZ = influenceForwardVS * influenceExtents.x;
                    bound.scaleXY.Set(1.0f, 1.0f);
                    bound.radius = influenceExtents.x;
                    break;
                }
                case LightVolumeType.Box:
                {
                    bound.center = influencePositionVS;
                    bound.boxAxisX = influenceExtents.x * influenceRightVS;
                    bound.boxAxisY = influenceExtents.y * influenceUpVS;
                    bound.boxAxisZ = influenceExtents.z * influenceForwardVS;
                    bound.scaleXY.Set(1.0f, 1.0f);
                    bound.radius = influenceExtents.magnitude;

                    // The culling system culls pixels that are further
                    //   than a threshold to the box influence extents.
                    // So we use an arbitrary threshold here (k_BoxCullingExtentOffset)
                    lightVolumeData.lightPos = influencePositionVS;
                    lightVolumeData.lightAxisX = influenceRightVS;
                    lightVolumeData.lightAxisY = influenceUpVS;
                    lightVolumeData.lightAxisZ = influenceForwardVS;
                    lightVolumeData.boxInnerDist = influenceExtents - k_BoxCullingExtentThreshold;
                    lightVolumeData.boxInvRange.Set(1.0f / k_BoxCullingExtentThreshold.x, 1.0f / k_BoxCullingExtentThreshold.y, 1.0f / k_BoxCullingExtentThreshold.z);
                    break;
                }
            }

            m_lightList.lightsPerView[viewIndex].bounds.Add(bound);
            m_lightList.lightsPerView[viewIndex].lightVolumes.Add(lightVolumeData);
        }

        void AddBoxVolumeDataAndBound(OrientedBBox obb, LightCategory category, LightFeatureFlags featureFlags, Matrix4x4 worldToView, int viewIndex)
        {
            var bound      = new SFiniteLightBound();
            var volumeData = new LightVolumeData();

            // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
            var positionVS = worldToView.MultiplyPoint(obb.center);
            var rightVS    = worldToView.MultiplyVector(obb.right);
            var upVS       = worldToView.MultiplyVector(obb.up);
            var forwardVS  = Vector3.Cross(upVS, rightVS);
            var extents    = new Vector3(obb.extentX, obb.extentY, obb.extentZ);

            volumeData.lightVolume   = (uint)LightVolumeType.Box;
            volumeData.lightCategory = (uint)category;
            volumeData.featureFlags  = (uint)featureFlags;

            bound.center   = positionVS;
            bound.boxAxisX = obb.extentX * rightVS;
            bound.boxAxisY = obb.extentY * upVS;
            bound.boxAxisZ = obb.extentZ * forwardVS;
            bound.radius   = extents.magnitude;
            bound.scaleXY.Set(1.0f, 1.0f);

            // The culling system culls pixels that are further
            //   than a threshold to the box influence extents.
            // So we use an arbitrary threshold here (k_BoxCullingExtentOffset)
            volumeData.lightPos     = positionVS;
            volumeData.lightAxisX   = rightVS;
            volumeData.lightAxisY   = upVS;
            volumeData.lightAxisZ   = forwardVS;
            volumeData.boxInnerDist = extents - k_BoxCullingExtentThreshold; // We have no blend range, but the culling code needs a small EPS value for some reason???
            volumeData.boxInvRange.Set(1.0f / k_BoxCullingExtentThreshold.x, 1.0f / k_BoxCullingExtentThreshold.y, 1.0f / k_BoxCullingExtentThreshold.z);

            m_lightList.lightsPerView[viewIndex].bounds.Add(bound);
            m_lightList.lightsPerView[viewIndex].lightVolumes.Add(volumeData);
            }

        internal int GetCurrentShadowCount()
        {
            return m_ShadowManager.GetShadowRequestCount();
        }

        void LightLoopUpdateCullingParameters(ref ScriptableCullingParameters cullingParams, HDCamera hdCamera)
        {
            // Note we are using hdCamera.shadowMaxDistance instead of the value coming from the volume stack.
            // Check comment on hdCamera.shadowMaxDistance for more info.
            m_ShadowManager.UpdateCullingParameters(ref cullingParams, hdCamera.shadowMaxDistance);

            // In HDRP we don't need per object light/probe info so we disable the native code that handles it.
            cullingParams.cullingOptions |= CullingOptions.DisablePerObjectCulling;
        }

        bool IsBakedShadowMaskLight(Light light)
        {
            // This can happen for particle lights.
            if (light == null)
                return false;

            return light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed &&
                light.bakingOutput.mixedLightingMode == MixedLightingMode.Shadowmask &&
                light.bakingOutput.occlusionMaskChannel != -1;     // We need to have an occlusion mask channel assign, else we have no shadow mask
        }

        HDProbe SelectProbe(VisibleReflectionProbe probe, PlanarReflectionProbe planarProbe)
        {
            if (probe.reflectionProbe != null)
            {
                var add = probe.reflectionProbe.GetComponent<HDAdditionalReflectionData>();
                if (add == null)
                {
                    add = HDUtils.s_DefaultHDAdditionalReflectionData;
                    Vector3 distance = Vector3.one * probe.blendDistance;
                    add.influenceVolume.boxBlendDistancePositive = distance;
                    add.influenceVolume.boxBlendDistanceNegative = distance;
                    add.influenceVolume.shape = InfluenceShape.Box;
                }
                return add;
            }
            if (planarProbe != null)
                return planarProbe;

            throw new ArgumentException();
        }

        internal static void EvaluateGPULightType(HDLightType lightType, SpotLightShape spotLightShape, AreaLightShape areaLightShape,
            ref LightCategory lightCategory, ref GPULightType gpuLightType, ref LightVolumeType lightVolumeType)
        {
            lightCategory = LightCategory.Count;
            gpuLightType = GPULightType.Point;
            lightVolumeType = LightVolumeType.Count;

            switch (lightType)
            {
                case HDLightType.Spot:
                    lightCategory = LightCategory.Punctual;

                    switch (spotLightShape)
                    {
                        case SpotLightShape.Cone:
                            gpuLightType = GPULightType.Spot;
                            lightVolumeType = LightVolumeType.Cone;
                            break;
                        case SpotLightShape.Pyramid:
                            gpuLightType = GPULightType.ProjectorPyramid;
                            lightVolumeType = LightVolumeType.Cone;
                            break;
                        case SpotLightShape.Box:
                            gpuLightType = GPULightType.ProjectorBox;
                            lightVolumeType = LightVolumeType.Box;
                            break;
                        default:
                            Debug.Assert(false, "Encountered an unknown SpotLightShape.");
                            break;
                    }
                    break;

                case HDLightType.Directional:
                    lightCategory = LightCategory.Punctual;
                    gpuLightType = GPULightType.Directional;
                    // No need to add volume, always visible
                    lightVolumeType = LightVolumeType.Count; // Count is none
                    break;

                case HDLightType.Point:
                    lightCategory = LightCategory.Punctual;
                    gpuLightType = GPULightType.Point;
                    lightVolumeType = LightVolumeType.Sphere;
                    break;

                case HDLightType.Area:
                    lightCategory = LightCategory.Area;

                    switch (areaLightShape)
                    {
                        case AreaLightShape.Rectangle:
                            gpuLightType = GPULightType.Rectangle;
                            lightVolumeType = LightVolumeType.Box;
                            break;

                        case AreaLightShape.Tube:
                            gpuLightType = GPULightType.Tube;
                            lightVolumeType = LightVolumeType.Box;
                            break;

                        case AreaLightShape.Disc:
                            //not used in real-time at the moment anyway
                            gpuLightType = GPULightType.Disc;
                            lightVolumeType = LightVolumeType.Sphere;
                            break;

                        default:
                            Debug.Assert(false, "Encountered an unknown AreaLightShape.");
                            break;
                    }
                    break;

                default:
                    Debug.Assert(false, "Encountered an unknown LightType.");
                    break;
            }
        }

        // Return true if BakedShadowMask are enabled
        bool PrepareLightsForGPU(CommandBuffer cmd, HDCamera hdCamera, CullingResults cullResults,
            HDProbeCullingResults hdProbeCullingResults, DensityVolumeList densityVolumes, DebugDisplaySettings debugDisplaySettings, AOVRequestData aovRequest)
        {
            var debugLightFilter = debugDisplaySettings.GetDebugLightFilterMode();
            var hasDebugLightFilter = debugLightFilter != DebugLightFilterMode.None;

            using (new ProfilingSample(cmd, "Prepare Lights For GPU"))
            {
                Camera camera = hdCamera.camera;

                // If any light require it, we need to enabled bake shadow mask feature
                m_enableBakeShadowMask = false;

                m_lightList.Clear();

                // We need to properly reset this here otherwise if we go from 1 light to no visible light we would keep the old reference active.
                m_CurrentSunLight = null;
                m_CurrentSunLightAdditionalLightData = null;
                m_CurrentShadowSortedSunLightIndex = -1;
                m_DebugSelectedLightShadowIndex = -1;
                m_DebugSelectedLightShadowCount = 0;

                int decalDatasCount = Math.Min(DecalSystem.m_DecalDatasCount, m_MaxDecalsOnScreen);

                var hdShadowSettings = VolumeManager.instance.stack.GetComponent<HDShadowSettings>();

                Vector3 camPosWS = hdCamera.mainViewConstants.worldSpaceCameraPos;

                // We must clear the shadow requests before checking if they are any visible light because we would have requests from the last frame executed in the case where we don't see any lights
                m_ShadowManager.Clear();

                m_ScreenSpaceShadowIndex = 0;
                // Set all the light data to invalid
                for (int i = 0; i < m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadows; ++i)
                {
                    m_CurrentScreenSpaceShadowData[i].additionalLightData = null;
                    m_CurrentScreenSpaceShadowData[i].lightDataIndex = -1;
                    m_CurrentScreenSpaceShadowData[i].valid = false;
                }

                // Note: Light with null intensity/Color are culled by the C++, no need to test it here
                if (cullResults.visibleLights.Length != 0 || cullResults.visibleReflectionProbes.Length != 0 || hdProbeCullingResults.visibleProbes.Count != 0)
                {
                    // 1. Count the number of lights and sort all lights by category, type and volume - This is required for the fptl/cluster shader code
                    // If we reach maximum of lights available on screen, then we discard the light.
                    // Lights are processed in order, so we don't discards light based on their importance but based on their ordering in visible lights list.
                    int directionalLightcount = 0;
                    int punctualLightcount = 0;
                    int areaLightCount = 0;

                    int lightCount = Math.Min(cullResults.visibleLights.Length, m_MaxLightsOnScreen);
                    UpdateSortKeysArray(lightCount);
                    int sortCount = 0;
                    for (int lightIndex = 0, numLights = cullResults.visibleLights.Length; (lightIndex < numLights) && (sortCount < lightCount); ++lightIndex)
                    {
                        var light = cullResults.visibleLights[lightIndex];

                        // We can skip the processing of lights that are so small to not affect at least a pixel on screen.
                        // TODO: The minimum pixel size on screen should really be exposed as parameter, to allow small lights to be culled to user's taste.
                        const int minimumPixelAreaOnScreen = 1;
                        if ((light.screenRect.height * hdCamera.actualHeight) * (light.screenRect.width * hdCamera.actualWidth) < minimumPixelAreaOnScreen)
                        {
                            continue;
                        }

                        if (light.light != null && !aovRequest.IsLightEnabled(light.light.gameObject))
                            continue;

                        var lightComponent = light.light;

                        // Light should always have additional data, however preview light right don't have, so we must handle the case by assigning HDUtils.s_DefaultHDAdditionalLightData
                        var additionalData = GetHDAdditionalLightData(lightComponent);
                        HDLightType lightType = additionalData.ComputeLightType(lightComponent);

                        if (ShaderConfig.s_AreaLights == 0 && (lightType == HDLightType.Area && (additionalData.areaLightShape == AreaLightShape.Rectangle || additionalData.areaLightShape == AreaLightShape.Tube)))
                            continue;

                        // First we should evaluate the shadow information for this frame
                        additionalData.EvaluateShadowState(hdCamera, cullResults, hdCamera.frameSettings, lightIndex);

                        // Reserve shadow map resolutions and check if light needs to render shadows
                        if(additionalData.WillRenderShadowMap())
                        {
                            additionalData.ReserveShadowMap(camera, m_ShadowManager, m_ShadowInitParameters, light.screenRect);
                        }

                        // Evaluate the types that define the current light
                        LightCategory lightCategory = LightCategory.Count;
                        GPULightType gpuLightType = GPULightType.Point;
                        LightVolumeType lightVolumeType = LightVolumeType.Count;
                        HDRenderPipeline.EvaluateGPULightType(lightType, additionalData.spotLightShape, additionalData.areaLightShape, 
                                                                ref lightCategory, ref gpuLightType, ref lightVolumeType);

                        if (hasDebugLightFilter
                            && !debugLightFilter.IsEnabledFor(gpuLightType, additionalData.spotLightShape))
                            continue;

                        // 5 bit (0x1F) light category, 5 bit (0x1F) GPULightType, 5 bit (0x1F) lightVolume, 1 bit for shadow casting, 16 bit index
                        m_SortKeys[sortCount++] = (uint)lightCategory << 27 | (uint)gpuLightType << 22 | (uint)lightVolumeType << 17 | (uint)lightIndex;
                    }

                    CoreUnsafeUtils.QuickSort(m_SortKeys, 0, sortCount - 1); // Call our own quicksort instead of Array.Sort(sortKeys, 0, sortCount) so we don't allocate memory (note the SortCount-1 that is different from original call).

                    // Now that all the lights have requested a shadow resolution, we can layout them in the atlas
                    // And if needed rescale the whole atlas
                    m_ShadowManager.LayoutShadowMaps(debugDisplaySettings.data.lightingDebugSettings);

                    var visualEnvironment = VolumeManager.instance.stack.GetComponent<VisualEnvironment>();
                    Debug.Assert(visualEnvironment != null);

                    bool isPbrSkyActive = visualEnvironment.skyType.value == (int)SkyType.PhysicallyBased;

                    // TODO: Refactor shadow management
                    // The good way of managing shadow:
                    // Here we sort everyone and we decide which light is important or not (this is the responsibility of the lightloop)
                    // we allocate shadow slot based on maximum shadow allowed on screen and attribute slot by bigger solid angle
                    // THEN we ask to the ShadowRender to render the shadow, not the reverse as it is today (i.e render shadow than expect they
                    // will be use...)
                    // The lightLoop is in charge, not the shadow pass.
                    // For now we will still apply the maximum of shadow here but we don't apply the sorting by priority + slot allocation yet

                    // 2. Go through all lights, convert them to GPU format.
                    // Simultaneously create data for culling (LightVolumeData and SFiniteLightBound)

                    for (int sortIndex = 0; sortIndex < sortCount; ++sortIndex)
                    {
                        // In 1. we have already classify and sorted the light, we need to use this sorted order here
                        uint sortKey = m_SortKeys[sortIndex];
                        LightCategory lightCategory = (LightCategory)((sortKey >> 27) & 0x1F);
                        GPULightType gpuLightType = (GPULightType)((sortKey >> 22) & 0x1F);
                        LightVolumeType lightVolumeType = (LightVolumeType)((sortKey >> 17) & 0x1F);
                        int lightIndex = (int)(sortKey & 0xFFFF);

                        var light = cullResults.visibleLights[lightIndex];
                        var lightComponent = light.light;

                        switch(lightCategory)
                        {
                            case LightCategory.Punctual:
                                if (punctualLightcount >= m_MaxPunctualLightsOnScreen)
                                    continue;
                                break;
                            case LightCategory.Area:
                                if (areaLightCount >= m_MaxAreaLightsOnScreen)
                                    continue;
                                break;
                            default:
                                break;
                        }

                        m_enableBakeShadowMask = m_enableBakeShadowMask || IsBakedShadowMaskLight(lightComponent);

                        // Light should always have additional data, however preview light right don't have, so we must handle the case by assigning HDUtils.s_DefaultHDAdditionalLightData
                        var additionalLightData = GetHDAdditionalLightData(lightComponent);

                        int shadowIndex = -1;

                        // Manage shadow requests
                        if (additionalLightData.WillRenderShadowMap())
                        {
                            int shadowRequestCount;
                            shadowIndex = additionalLightData.UpdateShadowRequest(hdCamera, m_ShadowManager, light, cullResults, lightIndex, debugDisplaySettings.data.lightingDebugSettings, out shadowRequestCount);

#if UNITY_EDITOR
                            if ((debugDisplaySettings.data.lightingDebugSettings.shadowDebugUseSelection
                                    || debugDisplaySettings.data.lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.SingleShadow)
                                && UnityEditor.Selection.activeGameObject == lightComponent.gameObject)
                            {
                                m_DebugSelectedLightShadowIndex = shadowIndex;
                                m_DebugSelectedLightShadowCount = shadowRequestCount;
                            }
#endif
                        }

                        // Directional rendering side, it is separated as it is always visible so no volume to handle here
                        if (gpuLightType == GPULightType.Directional)
                        {
                            if (GetDirectionalLightData(cmd, hdCamera, gpuLightType, light, lightComponent, additionalLightData, lightIndex, shadowIndex, debugDisplaySettings, directionalLightcount, ref m_ScreenSpaceShadowIndex, isPbrSkyActive))
                            {
                                directionalLightcount++;

                                // We make the light position camera-relative as late as possible in order
                                // to allow the preceding code to work with the absolute world space coordinates.
                                if (ShaderConfig.s_CameraRelativeRendering != 0)
                                {
                                    // Caution: 'DirectionalLightData.positionWS' is camera-relative after this point.
                                    int last = m_lightList.directionalLights.Count - 1;
                                    DirectionalLightData lightData = m_lightList.directionalLights[last];
                                    lightData.positionRWS -= camPosWS;
                                    m_lightList.directionalLights[last] = lightData;
                                }
                            }
                            continue;
                        }

                        Vector3 lightDimensions = new Vector3(); // X = length or width, Y = height, Z = range (depth)

                        // Punctual, area, projector lights - the rendering side.
                        if (GetLightData(cmd, hdCamera, hdShadowSettings, gpuLightType, light, lightComponent, additionalLightData, lightIndex, shadowIndex, ref lightDimensions, debugDisplaySettings, ref m_ScreenSpaceShadowIndex))
                        {
                            switch (lightCategory)
                            {
                                case LightCategory.Punctual:
                                    punctualLightcount++;
                                    break;
                                case LightCategory.Area:
                                    areaLightCount++;
                                    break;
                                default:
                                    Debug.Assert(false, "TODO: encountered an unknown LightCategory.");
                                    break;
                            }

                            // Then culling side. Must be call in this order as we pass the created Light data to the function
                            for (int viewIndex = 0; viewIndex < hdCamera.viewCount; ++viewIndex)
                            {
                                var worldToView = GetWorldToViewMatrix(hdCamera, viewIndex);
                                GetLightVolumeDataAndBound(lightCategory, gpuLightType, lightVolumeType, light, m_lightList.lights[m_lightList.lights.Count - 1], lightDimensions, worldToView, viewIndex);
                            }

                            // We make the light position camera-relative as late as possible in order
                            // to allow the preceding code to work with the absolute world space coordinates.
                            if (ShaderConfig.s_CameraRelativeRendering != 0)
                            {
                                // Caution: 'LightData.positionWS' is camera-relative after this point.
                                int last = m_lightList.lights.Count - 1;
                                LightData lightData = m_lightList.lights[last];
                                lightData.positionRWS -= camPosWS;
                                m_lightList.lights[last] = lightData;
                            }
                        }
                    }

                    // Update the compute buffer with the shadow request datas
                    m_ShadowManager.PrepareGPUShadowDatas(cullResults, hdCamera);

                    // Sanity check
                    Debug.Assert(m_lightList.directionalLights.Count == directionalLightcount);
                    Debug.Assert(m_lightList.lights.Count == areaLightCount + punctualLightcount);

                    m_lightList.punctualLightCount = punctualLightcount;
                    m_lightList.areaLightCount = areaLightCount;

                    // Redo everything but this time with envLights
                    Debug.Assert(m_MaxEnvLightsOnScreen <= 256); //for key construction
                    int envLightCount = 0;

                    var totalProbes = cullResults.visibleReflectionProbes.Length + hdProbeCullingResults.visibleProbes.Count;
                    int probeCount = Math.Min(totalProbes, m_MaxEnvLightsOnScreen);
                    UpdateSortKeysArray(probeCount);
                    sortCount = 0;

                    var enableReflectionProbes =    hdCamera.frameSettings.IsEnabled(FrameSettingsField.ReflectionProbe) &&
                                                    (!hasDebugLightFilter || debugLightFilter.IsEnabledFor(ProbeSettings.ProbeType.ReflectionProbe));

                    var enablePlanarProbes =    hdCamera.frameSettings.IsEnabled(FrameSettingsField.PlanarProbe) &&
                                                (!hasDebugLightFilter || debugLightFilter.IsEnabledFor(ProbeSettings.ProbeType.PlanarProbe));

                    for (int probeIndex = 0, numProbes = totalProbes; (probeIndex < numProbes) && (sortCount < probeCount); probeIndex++)
                    {
                        if (probeIndex < cullResults.visibleReflectionProbes.Length)
                        {
                            if (!enableReflectionProbes)
                            {
                                // Skip directly to planar probes
                                probeIndex = cullResults.visibleReflectionProbes.Length - 1;
                                continue;
                            }

                            var probe = cullResults.visibleReflectionProbes[probeIndex];
                            if (probe.reflectionProbe == null
                                || probe.reflectionProbe.Equals(null) || !probe.reflectionProbe.isActiveAndEnabled
                                || !aovRequest.IsLightEnabled(probe.reflectionProbe.gameObject))
                                continue;

                            // Exclude env lights based on hdCamera.probeLayerMask
                            if ((hdCamera.probeLayerMask.value & (1 << probe.reflectionProbe.gameObject.layer)) == 0)
                                continue;

                            var additional = probe.reflectionProbe.GetComponent<HDAdditionalReflectionData>();

                            // probe.texture can be null when we are adding a reflection probe in the editor
                            if (additional.texture == null || envLightCount >= m_MaxEnvLightsOnScreen)
                                continue;

                            // Work around the data issues.
                            if (probe.localToWorldMatrix.determinant == 0)
                            {
                                Debug.LogError("Reflection probe " + probe.reflectionProbe.name + " has an invalid local frame and needs to be fixed.");
                                continue;
                            }

                            LightVolumeType lightVolumeType = LightVolumeType.Box;
                            if (additional != null && additional.influenceVolume.shape == InfluenceShape.Sphere)
                                lightVolumeType = LightVolumeType.Sphere;
                            ++envLightCount;

                            var logVolume = CalculateProbeLogVolume(probe.bounds);

                            m_SortKeys[sortCount++] = PackProbeKey(logVolume, lightVolumeType, 0u, probeIndex); // Sort by volume
                        }
                        else
                        {
                            if (!enablePlanarProbes)
                                // skip planar probes
                                break;

                            var planarProbeIndex = probeIndex - cullResults.visibleReflectionProbes.Length;
                            var probe = hdProbeCullingResults.visibleProbes[planarProbeIndex];
                            if (!aovRequest.IsLightEnabled(probe.gameObject))
                                continue;

                            // probe.texture can be null when we are adding a reflection probe in the editor
                            if (probe.texture == null || envLightCount >= k_MaxEnvLightsOnScreen)
                                continue;

                            // Exclude env lights based on hdCamera.probeLayerMask
                            if ((hdCamera.probeLayerMask.value & (1 << probe.gameObject.layer)) == 0)
                                continue;

                            var lightVolumeType = LightVolumeType.Box;
                            if (probe.influenceVolume.shape == InfluenceShape.Sphere)
                                lightVolumeType = LightVolumeType.Sphere;
                            ++envLightCount;

                            var logVolume = CalculateProbeLogVolume(probe.bounds);

                            m_SortKeys[sortCount++] = PackProbeKey(logVolume, lightVolumeType, 1u, planarProbeIndex); // Sort by volume
                        }
                    }

                    // Not necessary yet but call it for future modification with sphere influence volume
                    CoreUnsafeUtils.QuickSort(m_SortKeys, 0, sortCount - 1); // Call our own quicksort instead of Array.Sort(sortKeys, 0, sortCount) so we don't allocate memory (note the SortCount-1 that is different from original call).

                    for (int sortIndex = 0; sortIndex < sortCount; ++sortIndex)
                    {
                        // In 1. we have already classify and sorted the light, we need to use this sorted order here
                        uint sortKey = m_SortKeys[sortIndex];
                        LightVolumeType lightVolumeType;
                        int probeIndex;
                        int listType;
                        UnpackProbeSortKey(sortKey, out lightVolumeType, out probeIndex, out listType);

                        PlanarReflectionProbe planarProbe = null;
                        VisibleReflectionProbe probe = default(VisibleReflectionProbe);
                        if (listType == 0)
                            probe = cullResults.visibleReflectionProbes[probeIndex];
                        else
                            planarProbe = (PlanarReflectionProbe)hdProbeCullingResults.visibleProbes[probeIndex];

                        var probeWrapper = SelectProbe(probe, planarProbe);

                        EnvLightData envLightData = new EnvLightData();

                        if (GetEnvLightData(cmd, hdCamera, probeWrapper, debugDisplaySettings, ref envLightData))
                        {
                            // it has been filled
                            m_lightList.envLights.Add(envLightData);

                            for (int viewIndex = 0; viewIndex < hdCamera.viewCount; ++viewIndex)
                            {
                                var worldToView = GetWorldToViewMatrix(hdCamera, viewIndex);
                                GetEnvLightVolumeDataAndBound(probeWrapper, lightVolumeType, worldToView, viewIndex);
                            }

                            // We make the light position camera-relative as late as possible in order
                            // to allow the preceding code to work with the absolute world space coordinates.
                            UpdateEnvLighCameraRelativetData(ref envLightData, camPosWS);

                            int last = m_lightList.envLights.Count - 1;
                            m_lightList.envLights[last] = envLightData;
                        }
                    }
                }

                HDShadowManager.instance.CheckForCulledCachedShadows();

                if (decalDatasCount > 0)
                {
                    for (int i = 0; i < decalDatasCount; i++)
                    {
                        for (int viewIndex = 0; viewIndex < hdCamera.viewCount; ++viewIndex)
                        {
                            m_lightList.lightsPerView[viewIndex].bounds.Add(DecalSystem.m_Bounds[i]);
                            m_lightList.lightsPerView[viewIndex].lightVolumes.Add(DecalSystem.m_LightVolumes[i]);
                        }
                    }
                }

                // Inject density volumes into the clustered data structure for efficient look up.
                m_densityVolumeCount = densityVolumes.bounds != null ? densityVolumes.bounds.Count : 0;

                for (int viewIndex = 0; viewIndex < hdCamera.viewCount; ++viewIndex)
                {
                    Matrix4x4 worldToViewCR = GetWorldToViewMatrix(hdCamera, viewIndex);

                    if (ShaderConfig.s_CameraRelativeRendering != 0)
                    {
                        // The OBBs are camera-relative, the matrix is not. Fix it.
                        worldToViewCR.SetColumn(3, new Vector4(0, 0, 0, 1));
                    }

                    for (int i = 0, n = m_densityVolumeCount; i < n; i++)
                    {
                        // Density volumes are not lights and therefore should not affect light classification.
                        LightFeatureFlags featureFlags = 0;
                        AddBoxVolumeDataAndBound(densityVolumes.bounds[i], LightCategory.DensityVolume, featureFlags, worldToViewCR, viewIndex);
                    }
                }

                m_TotalLightCount = m_lightList.lights.Count + m_lightList.envLights.Count + decalDatasCount + m_densityVolumeCount;
                Debug.Assert(m_TotalLightCount == m_lightList.lightsPerView[0].bounds.Count);
                Debug.Assert(m_TotalLightCount == m_lightList.lightsPerView[0].lightVolumes.Count);

                // Aggregate the remaining views into the first entry of the list (view 0)
                for (int viewIndex = 1; viewIndex < hdCamera.viewCount; ++viewIndex)
                {
                    Debug.Assert(m_lightList.lightsPerView[viewIndex].bounds.Count == m_TotalLightCount);
                    m_lightList.lightsPerView[0].bounds.AddRange(m_lightList.lightsPerView[viewIndex].bounds);

                    Debug.Assert(m_lightList.lightsPerView[viewIndex].lightVolumes.Count == m_TotalLightCount);
                    m_lightList.lightsPerView[0].lightVolumes.AddRange(m_lightList.lightsPerView[viewIndex].lightVolumes);
                }

                UpdateDataBuffers();

                cmd.SetGlobalInt(HDShaderIDs._EnvLightIndexShift, m_lightList.lights.Count);
                cmd.SetGlobalInt(HDShaderIDs._DecalIndexShift, m_lightList.lights.Count + m_lightList.envLights.Count);
                cmd.SetGlobalInt(HDShaderIDs._DensityVolumeIndexShift, m_lightList.lights.Count + m_lightList.envLights.Count + decalDatasCount);
            }

            m_enableBakeShadowMask = m_enableBakeShadowMask && hdCamera.frameSettings.IsEnabled(FrameSettingsField.Shadowmask);

            // We push this parameter here because we know that normal/deferred shadows are not yet rendered
            if (debugDisplaySettings.data.lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.SingleShadow)
            {
                int shadowIndex = (int)debugDisplaySettings.data.lightingDebugSettings.shadowMapIndex;

                if (debugDisplaySettings.data.lightingDebugSettings.shadowDebugUseSelection)
                    shadowIndex = m_DebugSelectedLightShadowIndex;
                cmd.SetGlobalInt(HDShaderIDs._DebugSingleShadowIndex, shadowIndex);
            }

            return m_enableBakeShadowMask;
        }

        internal void UpdateEnvLighCameraRelativetData(ref EnvLightData envLightData, Vector3 camPosWS)
        {
            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                // Caution: 'EnvLightData.positionRWS' is camera-relative after this point.
                envLightData.capturePositionRWS -= camPosWS;
                envLightData.influencePositionRWS -= camPosWS;
                envLightData.proxyPositionRWS -= camPosWS;
            }
        }

        static float CalculateProbeLogVolume(Bounds bounds)
        {
            //Notes:
            // - 1+ term is to prevent having negative values in the log result
            // - 1000* is too keep 3 digit after the dot while we truncate the result later
            // - 1048575 is 2^20-1 as we pack the result on 20bit later
            float boxVolume = 8f* bounds.extents.x * bounds.extents.y * bounds.extents.z;
            float logVolume = Mathf.Clamp(Mathf.Log(1 + boxVolume, 1.05f)*1000, 0, 1048575);
            return logVolume;
        }

        static void UnpackProbeSortKey(uint sortKey, out LightVolumeType lightVolumeType, out int probeIndex, out int listType)
        {
            lightVolumeType = (LightVolumeType)((sortKey >> 9) & 0x3);
            probeIndex = (int)(sortKey & 0xFF);
            listType = (int)((sortKey >> 8) & 1);
        }

        static uint PackProbeKey(float logVolume, LightVolumeType lightVolumeType, uint listType, int probeIndex)
        {
            // 20 bit volume, 3 bit LightVolumeType, 1 bit list type, 8 bit index
            return (uint)logVolume << 12 | (uint)lightVolumeType << 9 | listType << 8 | ((uint)probeIndex & 0xFF);
        }

        struct BuildGPULightListParameters
        {
            // Common
            public int totalLightCount; // Regular + Env + Decal + Density Volumes
            public bool isOrthographic;
            public int viewCount;
            public bool runLightList;
            public bool enableFeatureVariants;
            public bool computeMaterialVariants;
            public bool computeLightVariants;
            public bool skyEnabled;
            public LightList lightList;
            public Matrix4x4[] lightListProjscrMatrices;
            public Matrix4x4[] lightListInvProjscrMatrices;
            public float nearClipPlane, farClipPlane;
            public Vector4 screenSize;
            public int msaaSamples;

            // Screen Space AABBs
            public ComputeShader screenSpaceAABBShader;
            public int screenSpaceAABBKernel;
            public Matrix4x4[] lightListProjHMatrices;
            public Matrix4x4[] lightListInvProjHMatrices;

            // Big Tile
            public ComputeShader bigTilePrepassShader;
            public int bigTilePrepassKernel;
            public bool runBigTilePrepass;
            public int numBigTilesX, numBigTilesY;

            // FPTL
            public ComputeShader buildPerTileLightListShader;
            public int buildPerTileLightListKernel;
            public bool runFPTL;
            public int numTilesFPTLX;
            public int numTilesFPTLY;
            public int numTilesFPTL;

            // Cluster
            public ComputeShader buildPerVoxelLightListShader;
            public int buildPerVoxelLightListKernel;
            public int numTilesClusterX;
            public int numTilesClusterY;
            public float clusterScale;

            // Build dispatch indirect
            public ComputeShader buildMaterialFlagsShader;
            public ComputeShader clearDispatchIndirectShader;
            public ComputeShader buildDispatchIndirectShader;
            public bool useComputeAsPixel;
        }

        struct BuildGPULightListResources
        {
            public TileAndClusterData tileAndClusterData;
            public RTHandle depthBuffer;
            public RTHandle stencilTexture;
            public RTHandle[] gBuffer;
        }

        BuildGPULightListResources PrepareBuildGPULightListResources(TileAndClusterData tileAndClusterData, RTHandle depthBuffer, RTHandle stencilTexture)
        {
            var resources = new BuildGPULightListResources();

            resources.tileAndClusterData = tileAndClusterData;
            resources.depthBuffer = depthBuffer;
            resources.stencilTexture = stencilTexture;
            resources.gBuffer = m_GbufferManager.GetBuffers();

            return resources;
        }

        // generate screen-space AABBs (used for both fptl and clustered).
        static void GenerateLightsScreenSpaceAABBs(in BuildGPULightListParameters parameters, in BuildGPULightListResources resources, CommandBuffer cmd)
        {
            if (parameters.totalLightCount != 0)
            {
                var tileAndCluster = resources.tileAndClusterData;

                cmd.SetComputeIntParam(parameters.screenSpaceAABBShader, HDShaderIDs.g_isOrthographic, parameters.isOrthographic ? 1 : 0);

                // With XR single-pass, we have one set of light bounds per view to iterate over (bounds are in view space for each view)
                cmd.SetComputeIntParam(parameters.screenSpaceAABBShader, HDShaderIDs.g_iNrVisibLights, parameters.totalLightCount);
                cmd.SetComputeBufferParam(parameters.screenSpaceAABBShader, parameters.screenSpaceAABBKernel, HDShaderIDs.g_data, tileAndCluster.convexBoundsBuffer);
                cmd.SetComputeBufferParam(parameters.screenSpaceAABBShader, parameters.screenSpaceAABBKernel, HDShaderIDs.g_vBoundsBuffer, tileAndCluster.AABBBoundsBuffer);

                cmd.SetComputeMatrixArrayParam(parameters.screenSpaceAABBShader, HDShaderIDs.g_mProjectionArr, parameters.lightListProjHMatrices);
                cmd.SetComputeMatrixArrayParam(parameters.screenSpaceAABBShader, HDShaderIDs.g_mInvProjectionArr, parameters.lightListInvProjHMatrices);

                cmd.DispatchCompute(parameters.screenSpaceAABBShader, parameters.screenSpaceAABBKernel, (parameters.totalLightCount + 7) / 8, parameters.viewCount, 1);
            }
        }

        // enable coarse 2D pass on 64x64 tiles (used for both fptl and clustered).
        static void BigTilePrepass(in BuildGPULightListParameters parameters, in BuildGPULightListResources resources, CommandBuffer cmd)
        {
            if (parameters.runLightList && parameters.runBigTilePrepass)
            {
                var tileAndCluster = resources.tileAndClusterData;

                cmd.SetComputeIntParam(parameters.bigTilePrepassShader, HDShaderIDs.g_iNrVisibLights, parameters.totalLightCount);
                cmd.SetComputeIntParam(parameters.bigTilePrepassShader, HDShaderIDs.g_isOrthographic, parameters.isOrthographic ? 1 : 0);
                cmd.SetComputeIntParams(parameters.bigTilePrepassShader, HDShaderIDs.g_viDimensions, s_TempScreenDimArray);

                // TODO: These two aren't actually used...
                cmd.SetComputeIntParam(parameters.bigTilePrepassShader, HDShaderIDs._EnvLightIndexShift, parameters.lightList.lights.Count);
                cmd.SetComputeIntParam(parameters.bigTilePrepassShader, HDShaderIDs._DecalIndexShift, parameters.lightList.lights.Count + parameters.lightList.envLights.Count);

                cmd.SetComputeMatrixArrayParam(parameters.bigTilePrepassShader, HDShaderIDs.g_mScrProjectionArr, parameters.lightListProjscrMatrices);
                cmd.SetComputeMatrixArrayParam(parameters.bigTilePrepassShader, HDShaderIDs.g_mInvScrProjectionArr, parameters.lightListInvProjscrMatrices);

                cmd.SetComputeFloatParam(parameters.bigTilePrepassShader, HDShaderIDs.g_fNearPlane, parameters.nearClipPlane);
                cmd.SetComputeFloatParam(parameters.bigTilePrepassShader, HDShaderIDs.g_fFarPlane, parameters.farClipPlane);
                cmd.SetComputeBufferParam(parameters.bigTilePrepassShader, parameters.bigTilePrepassKernel, HDShaderIDs.g_vLightList, tileAndCluster.bigTileLightList);
                cmd.SetComputeBufferParam(parameters.bigTilePrepassShader, parameters.bigTilePrepassKernel, HDShaderIDs.g_vBoundsBuffer, tileAndCluster.AABBBoundsBuffer);
                cmd.SetComputeBufferParam(parameters.bigTilePrepassShader, parameters.bigTilePrepassKernel, HDShaderIDs._LightVolumeData, tileAndCluster.lightVolumeDataBuffer);
                cmd.SetComputeBufferParam(parameters.bigTilePrepassShader, parameters.bigTilePrepassKernel, HDShaderIDs.g_data, tileAndCluster.convexBoundsBuffer);

                cmd.DispatchCompute(parameters.bigTilePrepassShader, parameters.bigTilePrepassKernel, parameters.numBigTilesX, parameters.numBigTilesY, parameters.viewCount);
            }
        }

        static void BuildPerTileLightList(in BuildGPULightListParameters parameters, in BuildGPULightListResources resources, ref bool tileFlagsWritten, CommandBuffer cmd)
        {
            // optimized for opaques only
            if (parameters.runLightList && parameters.runFPTL)
            {
                var tileAndCluster = resources.tileAndClusterData;

                cmd.SetComputeIntParam(parameters.buildPerTileLightListShader, HDShaderIDs.g_isOrthographic, parameters.isOrthographic ? 1 : 0);
                cmd.SetComputeIntParams(parameters.buildPerTileLightListShader, HDShaderIDs.g_viDimensions, s_TempScreenDimArray);
                cmd.SetComputeIntParam(parameters.buildPerTileLightListShader, HDShaderIDs._EnvLightIndexShift, parameters.lightList.lights.Count);
                cmd.SetComputeIntParam(parameters.buildPerTileLightListShader, HDShaderIDs._DecalIndexShift, parameters.lightList.lights.Count + parameters.lightList.envLights.Count);
                cmd.SetComputeIntParam(parameters.buildPerTileLightListShader, HDShaderIDs.g_iNrVisibLights, parameters.totalLightCount);

                cmd.SetComputeBufferParam(parameters.buildPerTileLightListShader, parameters.buildPerTileLightListKernel, HDShaderIDs.g_vBoundsBuffer, tileAndCluster.AABBBoundsBuffer);
                cmd.SetComputeBufferParam(parameters.buildPerTileLightListShader, parameters.buildPerTileLightListKernel, HDShaderIDs._LightVolumeData, tileAndCluster.lightVolumeDataBuffer);
                cmd.SetComputeBufferParam(parameters.buildPerTileLightListShader, parameters.buildPerTileLightListKernel, HDShaderIDs.g_data, tileAndCluster.convexBoundsBuffer);

                cmd.SetComputeMatrixArrayParam(parameters.buildPerTileLightListShader, HDShaderIDs.g_mScrProjectionArr, parameters.lightListProjscrMatrices);
                cmd.SetComputeMatrixArrayParam(parameters.buildPerTileLightListShader, HDShaderIDs.g_mInvScrProjectionArr, parameters.lightListInvProjscrMatrices);

                cmd.SetComputeTextureParam(parameters.buildPerTileLightListShader, parameters.buildPerTileLightListKernel, HDShaderIDs.g_depth_tex, resources.depthBuffer);
                cmd.SetComputeBufferParam(parameters.buildPerTileLightListShader, parameters.buildPerTileLightListKernel, HDShaderIDs.g_vLightList, tileAndCluster.lightList);
                if (parameters.runBigTilePrepass)
                    cmd.SetComputeBufferParam(parameters.buildPerTileLightListShader, parameters.buildPerTileLightListKernel, HDShaderIDs.g_vBigTileLightList, tileAndCluster.bigTileLightList);

                if (parameters.enableFeatureVariants)
                {
                    uint baseFeatureFlags = 0;
                    if (parameters.lightList.directionalLights.Count > 0)
                    {
                        baseFeatureFlags |= (uint)LightFeatureFlags.Directional;
                    }
                    if (parameters.skyEnabled)
                    {
                        baseFeatureFlags |= (uint)LightFeatureFlags.Sky;
                    }
                    if (!parameters.computeMaterialVariants)
                    {
                        baseFeatureFlags |= LightDefinitions.s_MaterialFeatureMaskFlags;
                    }
                    cmd.SetComputeIntParam(parameters.buildPerTileLightListShader, HDShaderIDs.g_BaseFeatureFlags, (int)baseFeatureFlags);
                    cmd.SetComputeBufferParam(parameters.buildPerTileLightListShader, parameters.buildPerTileLightListKernel, HDShaderIDs.g_TileFeatureFlags, tileAndCluster.tileFeatureFlags);
                    tileFlagsWritten = true;
                }

                cmd.DispatchCompute(parameters.buildPerTileLightListShader, parameters.buildPerTileLightListKernel, parameters.numTilesFPTLX, parameters.numTilesFPTLY, parameters.viewCount);
            }
        }
        static void VoxelLightListGeneration(in BuildGPULightListParameters parameters, in BuildGPULightListResources resources, CommandBuffer cmd)
        {
            if (parameters.runLightList)
            {
                var tileAndCluster = resources.tileAndClusterData;

                // clear atomic offset index
                cmd.SetComputeBufferParam(parameters.buildPerVoxelLightListShader, s_ClearVoxelAtomicKernel, HDShaderIDs.g_LayeredSingleIdxBuffer, tileAndCluster.globalLightListAtomic);
                cmd.DispatchCompute(parameters.buildPerVoxelLightListShader, s_ClearVoxelAtomicKernel, 1, 1, 1);

                cmd.SetComputeIntParam(parameters.buildPerVoxelLightListShader, HDShaderIDs.g_isOrthographic, parameters.isOrthographic ? 1 : 0);
                cmd.SetComputeIntParam(parameters.buildPerVoxelLightListShader, HDShaderIDs.g_iNrVisibLights, parameters.totalLightCount);
                cmd.SetComputeMatrixArrayParam(parameters.buildPerVoxelLightListShader, HDShaderIDs.g_mScrProjectionArr, parameters.lightListProjscrMatrices);
                cmd.SetComputeMatrixArrayParam(parameters.buildPerVoxelLightListShader, HDShaderIDs.g_mInvScrProjectionArr, parameters.lightListInvProjscrMatrices);

                cmd.SetComputeIntParam(parameters.buildPerVoxelLightListShader, HDShaderIDs.g_iLog2NumClusters, k_Log2NumClusters);

                cmd.SetComputeVectorParam(parameters.buildPerVoxelLightListShader, HDShaderIDs.g_screenSize, parameters.screenSize);
                cmd.SetComputeIntParam(parameters.buildPerVoxelLightListShader, HDShaderIDs.g_iNumSamplesMSAA, parameters.msaaSamples);

                cmd.SetComputeFloatParam(parameters.buildPerVoxelLightListShader, HDShaderIDs.g_fNearPlane, parameters.nearClipPlane);
                cmd.SetComputeFloatParam(parameters.buildPerVoxelLightListShader, HDShaderIDs.g_fFarPlane, parameters.farClipPlane);

                cmd.SetComputeFloatParam(parameters.buildPerVoxelLightListShader, HDShaderIDs.g_fClustScale, parameters.clusterScale);
                cmd.SetComputeFloatParam(parameters.buildPerVoxelLightListShader, HDShaderIDs.g_fClustBase, k_ClustLogBase);

                cmd.SetComputeTextureParam(parameters.buildPerVoxelLightListShader, parameters.buildPerVoxelLightListKernel, HDShaderIDs.g_depth_tex, resources.depthBuffer);
                cmd.SetComputeBufferParam(parameters.buildPerVoxelLightListShader, parameters.buildPerVoxelLightListKernel, HDShaderIDs.g_vLayeredLightList, tileAndCluster.perVoxelLightLists);
                cmd.SetComputeBufferParam(parameters.buildPerVoxelLightListShader, parameters.buildPerVoxelLightListKernel, HDShaderIDs.g_LayeredOffset, tileAndCluster.perVoxelOffset);
                cmd.SetComputeBufferParam(parameters.buildPerVoxelLightListShader, parameters.buildPerVoxelLightListKernel, HDShaderIDs.g_LayeredSingleIdxBuffer, tileAndCluster.globalLightListAtomic);
                if (parameters.runBigTilePrepass)
                    cmd.SetComputeBufferParam(parameters.buildPerVoxelLightListShader, parameters.buildPerVoxelLightListKernel, HDShaderIDs.g_vBigTileLightList, tileAndCluster.bigTileLightList);

                if (k_UseDepthBuffer)
                {
                    cmd.SetComputeBufferParam(parameters.buildPerVoxelLightListShader, parameters.buildPerVoxelLightListKernel, HDShaderIDs.g_logBaseBuffer, tileAndCluster.perTileLogBaseTweak);
                }

                cmd.SetComputeBufferParam(parameters.buildPerVoxelLightListShader, parameters.buildPerVoxelLightListKernel, HDShaderIDs.g_vBoundsBuffer, tileAndCluster.AABBBoundsBuffer);
                cmd.SetComputeBufferParam(parameters.buildPerVoxelLightListShader, parameters.buildPerVoxelLightListKernel, HDShaderIDs._LightVolumeData, tileAndCluster.lightVolumeDataBuffer);
                cmd.SetComputeBufferParam(parameters.buildPerVoxelLightListShader, parameters.buildPerVoxelLightListKernel, HDShaderIDs.g_data, tileAndCluster.convexBoundsBuffer);

                cmd.DispatchCompute(parameters.buildPerVoxelLightListShader, parameters.buildPerVoxelLightListKernel, parameters.numTilesClusterX, parameters.numTilesClusterY, parameters.viewCount);
            }
        }

        static void BuildDispatchIndirectArguments(in BuildGPULightListParameters parameters, in BuildGPULightListResources resources, bool tileFlagsWritten, CommandBuffer cmd)
        {
            if (parameters.enableFeatureVariants)
            {
                var tileAndCluster = resources.tileAndClusterData;

                // We need to touch up the tile flags if we need material classification or, if disabled, to patch up for missing flags during the skipped light tile gen
                bool needModifyingTileFeatures = !tileFlagsWritten || parameters.computeMaterialVariants;
                if (needModifyingTileFeatures)
                {
                    int buildMaterialFlagsKernel = (!tileFlagsWritten || !parameters.computeLightVariants) ? s_BuildMaterialFlagsWriteKernel : s_BuildMaterialFlagsOrKernel;

                    uint baseFeatureFlags = 0;
                    if (!parameters.computeLightVariants)
                    {
                        baseFeatureFlags |= LightDefinitions.s_LightFeatureMaskFlags;
                    }

                    // If we haven't run the light list building, we are missing some basic lighting flags.
                    if (!tileFlagsWritten)
                    {
                        if (parameters.lightList.directionalLights.Count > 0)
                        {
                            baseFeatureFlags |= (uint)LightFeatureFlags.Directional;
                        }
                        if (parameters.skyEnabled)
                        {
                            baseFeatureFlags |= (uint)LightFeatureFlags.Sky;
                        }
                        if (!parameters.computeMaterialVariants)
                        {
                            baseFeatureFlags |= LightDefinitions.s_MaterialFeatureMaskFlags;
                        }
                    }

                    cmd.SetComputeIntParam(parameters.buildMaterialFlagsShader, HDShaderIDs.g_BaseFeatureFlags, (int)baseFeatureFlags);
                    cmd.SetComputeIntParams(parameters.buildMaterialFlagsShader, HDShaderIDs.g_viDimensions, s_TempScreenDimArray);
                    cmd.SetComputeBufferParam(parameters.buildMaterialFlagsShader, buildMaterialFlagsKernel, HDShaderIDs.g_TileFeatureFlags, tileAndCluster.tileFeatureFlags);

                    for (int i = 0; i < resources.gBuffer.Length; ++i)
                        cmd.SetComputeTextureParam(parameters.buildMaterialFlagsShader, buildMaterialFlagsKernel, HDShaderIDs._GBufferTexture[i], resources.gBuffer[i]);
                    cmd.SetComputeTextureParam(parameters.buildMaterialFlagsShader, buildMaterialFlagsKernel, HDShaderIDs._StencilTexture, resources.stencilTexture);

                    cmd.DispatchCompute(parameters.buildMaterialFlagsShader, buildMaterialFlagsKernel, parameters.numTilesFPTLX, parameters.numTilesFPTLY, parameters.viewCount);
                }

                // clear dispatch indirect buffer
                if (parameters.useComputeAsPixel)
                {
                    cmd.SetComputeBufferParam(parameters.clearDispatchIndirectShader, s_ClearDrawInstancedIndirectKernel, HDShaderIDs.g_DispatchIndirectBuffer, tileAndCluster.dispatchIndirectBuffer);
                    cmd.SetComputeIntParam(parameters.clearDispatchIndirectShader, HDShaderIDs.g_NumTiles, parameters.numTilesFPTL);
                    cmd.SetComputeIntParam(parameters.clearDispatchIndirectShader, HDShaderIDs.g_VertexPerTile, k_HasNativeQuadSupport ? 4 : 6);
                    cmd.DispatchCompute(parameters.clearDispatchIndirectShader, s_ClearDrawInstancedIndirectKernel, 1, 1, 1);

                    // add tiles to indirect buffer
                    cmd.SetComputeBufferParam(parameters.buildDispatchIndirectShader, s_BuildDrawInstancedIndirectKernel, HDShaderIDs.g_DispatchIndirectBuffer, tileAndCluster.dispatchIndirectBuffer);
                    cmd.SetComputeBufferParam(parameters.buildDispatchIndirectShader, s_BuildDrawInstancedIndirectKernel, HDShaderIDs.g_TileList, tileAndCluster.tileList);
                    cmd.SetComputeBufferParam(parameters.buildDispatchIndirectShader, s_BuildDrawInstancedIndirectKernel, HDShaderIDs.g_TileFeatureFlags, tileAndCluster.tileFeatureFlags);
                    cmd.SetComputeIntParam(parameters.buildDispatchIndirectShader, HDShaderIDs.g_NumTiles, parameters.numTilesFPTL);
                    cmd.SetComputeIntParam(parameters.buildDispatchIndirectShader, HDShaderIDs.g_NumTilesX, parameters.numTilesFPTLX);
                    cmd.DispatchCompute(parameters.buildDispatchIndirectShader, s_BuildDrawInstancedIndirectKernel, (parameters.numTilesFPTL + k_ThreadGroupOptimalSize - 1) / k_ThreadGroupOptimalSize, 1, parameters.viewCount);
                }
                else
                {
                    cmd.SetComputeBufferParam(parameters.clearDispatchIndirectShader, s_ClearDispatchIndirectKernel, HDShaderIDs.g_DispatchIndirectBuffer, tileAndCluster.dispatchIndirectBuffer);
                    cmd.DispatchCompute(parameters.clearDispatchIndirectShader, s_ClearDispatchIndirectKernel, 1, 1, 1);

                    // add tiles to indirect buffer
                    cmd.SetComputeBufferParam(parameters.buildDispatchIndirectShader, s_BuildDispatchIndirectKernel, HDShaderIDs.g_DispatchIndirectBuffer, tileAndCluster.dispatchIndirectBuffer);
                    cmd.SetComputeBufferParam(parameters.buildDispatchIndirectShader, s_BuildDispatchIndirectKernel, HDShaderIDs.g_TileList, tileAndCluster.tileList);
                    cmd.SetComputeBufferParam(parameters.buildDispatchIndirectShader, s_BuildDispatchIndirectKernel, HDShaderIDs.g_TileFeatureFlags, tileAndCluster.tileFeatureFlags);
                    cmd.SetComputeIntParam(parameters.buildDispatchIndirectShader, HDShaderIDs.g_NumTiles, parameters.numTilesFPTL);
                    cmd.SetComputeIntParam(parameters.buildDispatchIndirectShader, HDShaderIDs.g_NumTilesX, parameters.numTilesFPTLX);
                    cmd.DispatchCompute(parameters.buildDispatchIndirectShader, s_BuildDispatchIndirectKernel, (parameters.numTilesFPTL + k_ThreadGroupOptimalSize - 1) / k_ThreadGroupOptimalSize, 1, parameters.viewCount);
                }
            }
        }

        static bool DeferredUseComputeAsPixel(FrameSettings frameSettings)
        {
            return frameSettings.IsEnabled(FrameSettingsField.DeferredTile) && (!frameSettings.IsEnabled(FrameSettingsField.ComputeLightEvaluation) || k_PreferFragment);
        }

        BuildGPULightListParameters PrepareBuildGPULightListParameters(HDCamera hdCamera)
        {
            BuildGPULightListParameters parameters = new BuildGPULightListParameters();

            var camera = hdCamera.camera;

            var w = (int)hdCamera.screenSize.x;
            var h = (int)hdCamera.screenSize.y;

            s_TempScreenDimArray[0] = w;
            s_TempScreenDimArray[1] = h;

            parameters.runLightList = m_TotalLightCount > 0;

            // If we don't need to run the light list, we still run it for the first frame that is not needed in order to keep the lists in a clean state.
            if (!parameters.runLightList && m_hasRunLightListPrevFrame)
            {
                m_hasRunLightListPrevFrame = false;
                parameters.runLightList = true;
            }
            else
            {
                m_hasRunLightListPrevFrame = parameters.runLightList;
            }

            // Always build the light list in XR mode to avoid issues with multi-pass
            if (hdCamera.xr.enabled)
                parameters.runLightList = true;

            var temp = new Matrix4x4();
            temp.SetRow(0, new Vector4(0.5f * w, 0.0f, 0.0f, 0.5f * w));
            temp.SetRow(1, new Vector4(0.0f, 0.5f * h, 0.0f, 0.5f * h));
            temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
            temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

            parameters.lightListProjscrMatrices = m_LightListProjscrMatrices;
            parameters.lightListInvProjscrMatrices = m_LightListInvProjscrMatrices;
            parameters.lightListProjHMatrices = m_LightListProjHMatrices;
            parameters.lightListInvProjHMatrices = m_LightListInvProjHMatrices;

            // camera to screen matrix (and it's inverse)
            for (int viewIndex = 0; viewIndex < hdCamera.viewCount; ++viewIndex)
            {
                var proj = hdCamera.xr.enabled ? hdCamera.xr.GetProjMatrix(viewIndex) : camera.projectionMatrix;

                m_LightListProjMatrices[viewIndex] = proj * s_FlipMatrixLHSRHS;
                parameters.lightListProjscrMatrices[viewIndex] = temp * m_LightListProjMatrices[viewIndex];
                parameters.lightListInvProjscrMatrices[viewIndex] = parameters.lightListProjscrMatrices[viewIndex].inverse;
            }

            parameters.totalLightCount = m_TotalLightCount;
            parameters.isOrthographic = camera.orthographic;
            parameters.viewCount = hdCamera.viewCount;
            parameters.enableFeatureVariants = GetFeatureVariantsEnabled(hdCamera.frameSettings);
            parameters.computeMaterialVariants = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ComputeMaterialVariants);
            parameters.computeLightVariants = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ComputeLightVariants);
            parameters.nearClipPlane = camera.nearClipPlane;
            parameters.farClipPlane = camera.farClipPlane;
            parameters.lightList = m_lightList;
            parameters.skyEnabled = m_SkyManager.IsLightingSkyValid(hdCamera);
            parameters.screenSize = hdCamera.screenSize;
            parameters.msaaSamples = (int)hdCamera.msaaSamples;
            parameters.useComputeAsPixel = DeferredUseComputeAsPixel(hdCamera.frameSettings);

            bool isProjectionOblique = GeometryUtils.IsProjectionMatrixOblique(m_LightListProjMatrices[0]);

            // Screen space AABB
            parameters.screenSpaceAABBShader = buildScreenAABBShader;
            parameters.screenSpaceAABBKernel = isProjectionOblique ? s_GenAABBKernel_Oblique : s_GenAABBKernel;
            // camera to screen matrix (and it's inverse)
            for (int viewIndex = 0; viewIndex < hdCamera.viewCount; ++viewIndex)
            {
                temp.SetRow(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
                temp.SetRow(1, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
                temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
                temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

                parameters.lightListProjHMatrices[viewIndex] = temp * m_LightListProjMatrices[viewIndex];
                parameters.lightListInvProjHMatrices[viewIndex] = parameters.lightListProjHMatrices[viewIndex].inverse;
            }

            // Big tile prepass
            parameters.runBigTilePrepass = hdCamera.frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass);
            parameters.bigTilePrepassShader = buildPerBigTileLightListShader;
            parameters.bigTilePrepassKernel = s_GenListPerBigTileKernel;
            parameters.numBigTilesX = (w + 63) / 64;
            parameters.numBigTilesY = (h + 63) / 64;

            // Fptl
            parameters.runFPTL = hdCamera.frameSettings.fptl;
            parameters.buildPerTileLightListShader = buildPerTileLightListShader;
            parameters.buildPerTileLightListKernel = isProjectionOblique ? s_GenListPerTileKernel_Oblique : s_GenListPerTileKernel;
            parameters.numTilesFPTLX = GetNumTileFtplX(hdCamera);
            parameters.numTilesFPTLY = GetNumTileFtplY(hdCamera);
            parameters.numTilesFPTL = parameters.numTilesFPTLX * parameters.numTilesFPTLY;

            // Cluster
            parameters.buildPerVoxelLightListShader = buildPerVoxelLightListShader;
            parameters.buildPerVoxelLightListKernel = isProjectionOblique ? s_GenListPerVoxelKernelOblique : s_GenListPerVoxelKernel;
            parameters.numTilesClusterX = GetNumTileClusteredX(hdCamera);
            parameters.numTilesClusterY = GetNumTileClusteredY(hdCamera);

            const float C = (float)(1 << k_Log2NumClusters);
            var geomSeries = (1.0 - Mathf.Pow(k_ClustLogBase, C)) / (1 - k_ClustLogBase); // geometric series: sum_k=0^{C-1} base^k
            // TODO: This is computed here and then passed later on as a global shader parameters.
            // This will be dangerous when running RenderGraph as execution will be delayed and this can change before it's executed
            m_ClusterScale = (float)(geomSeries / (parameters.farClipPlane - parameters.nearClipPlane));

            parameters.clusterScale = m_ClusterScale;

            // Build dispatch indirect
            parameters.buildMaterialFlagsShader = buildMaterialFlagsShader;
            parameters.clearDispatchIndirectShader = clearDispatchIndirectShader;
            parameters.buildDispatchIndirectShader = buildDispatchIndirectShader;

            return parameters;
        }

        void BuildGPULightListsCommon(HDCamera hdCamera, CommandBuffer cmd)
        {
            using (new ProfilingSample(cmd, "Build Light List"))
            {
                var parameters = PrepareBuildGPULightListParameters(hdCamera);
                var resources = PrepareBuildGPULightListResources(
                    m_TileAndClusterData,
                    m_SharedRTManager.GetDepthStencilBuffer(hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA)),
                    m_SharedRTManager.GetStencilBufferCopy()
                );

                bool tileFlagsWritten = false;

                GenerateLightsScreenSpaceAABBs(parameters, resources, cmd);
                BigTilePrepass(parameters, resources, cmd);
                BuildPerTileLightList(parameters, resources, ref tileFlagsWritten, cmd);
                VoxelLightListGeneration(parameters, resources, cmd);

                BuildDispatchIndirectArguments(parameters, resources, tileFlagsWritten, cmd);
            }
        }

        void BuildGPULightLists(HDCamera hdCamera, CommandBuffer cmd)
        {
            cmd.SetRenderTarget(BuiltinRenderTextureType.None);

            BuildGPULightListsCommon(hdCamera, cmd);

            var globalParams = PrepareLightLoopGlobalParameters(hdCamera);
            PushLightLoopGlobalParams(globalParams, cmd);
        }

        void UpdateDataBuffers()
        {
            m_LightLoopLightData.directionalLightData.SetData(m_lightList.directionalLights);
            m_LightLoopLightData.lightData.SetData(m_lightList.lights);
            m_LightLoopLightData.envLightData.SetData(m_lightList.envLights);
            m_LightLoopLightData.decalData.SetData(DecalSystem.m_DecalDatas, 0, 0, Math.Min(DecalSystem.m_DecalDatasCount, m_MaxDecalsOnScreen)); // don't add more than the size of the buffer

            // These two buffers have been set in Rebuild(). At this point, view 0 contains combined data from all views
            m_TileAndClusterData.convexBoundsBuffer.SetData(m_lightList.lightsPerView[0].bounds);
            m_TileAndClusterData.lightVolumeDataBuffer.SetData(m_lightList.lightsPerView[0].lightVolumes);
        }

        HDAdditionalLightData GetHDAdditionalLightData(Light light)
        {
            // Light reference can be null for particle lights.
            var add = light != null ? light.GetComponent<HDAdditionalLightData>() : null;
            if (add == null)
            {
                add = HDUtils.s_DefaultHDAdditionalLightData;
            }
            return add;
        }

        struct LightLoopGlobalParameters
        {
            public HDCamera                 hdCamera;
            public HDShadowManager          shadowManager;
            public LightList                lightList;
            public LightLoopTextureCaches   textureCaches;
            public LightLoopLightData       lightData;
            public TileAndClusterData       tileAndClusterData;
            public float                    clusterScale;
            public int                      sunLightIndex;
            public int                      maxScreenSpaceShadows;
        }

        LightLoopGlobalParameters PrepareLightLoopGlobalParameters(HDCamera hdCamera)
        {
            LightLoopGlobalParameters parameters = new LightLoopGlobalParameters();
            parameters.hdCamera = hdCamera;
            parameters.shadowManager = m_ShadowManager;
            parameters.lightList = m_lightList;
            parameters.textureCaches = m_TextureCaches;
            parameters.lightData = m_LightLoopLightData;
            parameters.tileAndClusterData = m_TileAndClusterData;
            parameters.clusterScale = m_ClusterScale;
            parameters.maxScreenSpaceShadows = m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadows;

            HDAdditionalLightData sunLightData = m_CurrentSunLight != null ? m_CurrentSunLight.GetComponent<HDAdditionalLightData>() : null;
            bool sunLightShadow = sunLightData != null && m_CurrentShadowSortedSunLightIndex >= 0;
            parameters.sunLightIndex = sunLightShadow ? m_CurrentShadowSortedSunLightIndex : -1;

            return parameters;
        }

        static void PushLightLoopGlobalParams(in LightLoopGlobalParameters param, CommandBuffer cmd)
        {
            using (new ProfilingSample(cmd, "Push Global Parameters", CustomSamplerId.TPPushGlobalParameters.GetSampler()))
            {
                Camera camera = param.hdCamera.camera;

                // Shadows
                param.shadowManager.SyncData();
                param.shadowManager.BindResources(cmd);

                cmd.SetGlobalTexture(HDShaderIDs._CookieTextures, param.textureCaches.cookieTexArray.GetTexCache());
                cmd.SetGlobalTexture(HDShaderIDs._AreaCookieTextures, param.textureCaches.areaLightCookieManager.GetTexCache());
                cmd.SetGlobalTexture(HDShaderIDs._CookieCubeTextures, param.textureCaches.cubeCookieTexArray.GetTexCache());
                cmd.SetGlobalTexture(HDShaderIDs._EnvCubemapTextures, param.textureCaches.reflectionProbeCache.GetTexCache());
                cmd.SetGlobalInt(HDShaderIDs._EnvSliceSize, param.textureCaches.reflectionProbeCache.GetEnvSliceSize());
                // Compute the power of 2 size of the texture
                int pot = Mathf.RoundToInt( 1.4426950408889634073599246810019f * Mathf.Log(param.textureCaches.cookieTexArray.GetTexCache().width ) );
                cmd.SetGlobalInt(HDShaderIDs._CookieSizePOT, pot);
                cmd.SetGlobalTexture(HDShaderIDs._Env2DTextures, param.textureCaches.reflectionPlanarProbeCache.GetTexCache());
                cmd.SetGlobalMatrixArray(HDShaderIDs._Env2DCaptureVP, param.textureCaches.env2DCaptureVP);
                cmd.SetGlobalFloatArray(HDShaderIDs._Env2DCaptureForward, param.textureCaches.env2DCaptureForward);

                // Directional lights are made available immediately after PrepareLightsForGPU for the PBR sky.
                // cmd.SetGlobalBuffer(HDShaderIDs._DirectionalLightDatas, param.lightData.directionalLightData);
                // cmd.SetGlobalInt(HDShaderIDs._DirectionalLightCount, param.lightList.directionalLights.Count);
                cmd.SetGlobalBuffer(HDShaderIDs._LightDatas, param.lightData.lightData);
                cmd.SetGlobalInt(HDShaderIDs._PunctualLightCount, param.lightList.punctualLightCount);
                cmd.SetGlobalInt(HDShaderIDs._AreaLightCount, param.lightList.areaLightCount);
                cmd.SetGlobalBuffer(HDShaderIDs._EnvLightDatas, param.lightData.envLightData);
                cmd.SetGlobalInt(HDShaderIDs._EnvLightCount, param.lightList.envLights.Count);
                cmd.SetGlobalBuffer(HDShaderIDs._DecalDatas, param.lightData.decalData);
                cmd.SetGlobalInt(HDShaderIDs._DecalCount, DecalSystem.m_DecalDatasCount);

                cmd.SetGlobalInt(HDShaderIDs._NumTileBigTileX, GetNumTileBigTileX(param.hdCamera));
                cmd.SetGlobalInt(HDShaderIDs._NumTileBigTileY, GetNumTileBigTileY(param.hdCamera));

                cmd.SetGlobalInt(HDShaderIDs._NumTileFtplX, GetNumTileFtplX(param.hdCamera));
                cmd.SetGlobalInt(HDShaderIDs._NumTileFtplY, GetNumTileFtplY(param.hdCamera));

                cmd.SetGlobalInt(HDShaderIDs._NumTileClusteredX, GetNumTileClusteredX(param.hdCamera));
                cmd.SetGlobalInt(HDShaderIDs._NumTileClusteredY, GetNumTileClusteredY(param.hdCamera));

                cmd.SetGlobalInt(HDShaderIDs._EnableSSRefraction, param.hdCamera.frameSettings.IsEnabled(FrameSettingsField.RoughRefraction) ? 1 : 0);

                if (param.hdCamera.frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass))
                    cmd.SetGlobalBuffer(HDShaderIDs.g_vBigTileLightList, param.tileAndClusterData.bigTileLightList);

                // Cluster
                {
                    cmd.SetGlobalFloat(HDShaderIDs.g_fClustScale, param.clusterScale);
                    cmd.SetGlobalFloat(HDShaderIDs.g_fClustBase, k_ClustLogBase);
                    cmd.SetGlobalFloat(HDShaderIDs.g_fNearPlane, camera.nearClipPlane);
                    cmd.SetGlobalFloat(HDShaderIDs.g_fFarPlane, camera.farClipPlane);
                    cmd.SetGlobalInt(HDShaderIDs.g_iLog2NumClusters, k_Log2NumClusters);

                    cmd.SetGlobalInt(HDShaderIDs.g_isLogBaseBufferEnabled, k_UseDepthBuffer ? 1 : 0);

                    cmd.SetGlobalBuffer(HDShaderIDs.g_vLayeredOffsetsBuffer, param.tileAndClusterData.perVoxelOffset);
                    if (k_UseDepthBuffer)
                    {
                        cmd.SetGlobalBuffer(HDShaderIDs.g_logBaseBuffer, param.tileAndClusterData.perTileLogBaseTweak);
                    }

                    // Set up clustered lighting for volumetrics.
                    cmd.SetGlobalBuffer(HDShaderIDs.g_vLightListGlobal, param.tileAndClusterData.perVoxelLightLists);
                }

                cmd.SetGlobalInt(HDShaderIDs._DirectionalShadowIndex, param.sunLightIndex);
            }
        }

        void RenderShadowMaps(ScriptableRenderContext renderContext, CommandBuffer cmd, CullingResults cullResults, HDCamera hdCamera)
        {
            // kick off the shadow jobs here
            m_ShadowManager.RenderShadows(renderContext, cmd, cullResults, hdCamera);
        }

        bool WillRenderContactShadow()
        {
            // When contact shadow index is 0, then there is no light casting contact shadow in the view
            return m_EnableContactShadow && m_ContactShadowIndex != 0;
        }

        void SetContactShadowsTexture(HDCamera hdCamera, RTHandle contactShadowsRT, CommandBuffer cmd)
        {
            if (!WillRenderContactShadow())
            {
                cmd.SetGlobalTexture(HDShaderIDs._ContactShadowTexture, TextureXR.GetBlackUIntTexture());
                return;
            }
            cmd.SetGlobalTexture(HDShaderIDs._ContactShadowTexture, contactShadowsRT);
        }

        // The first rendered 24 lights that have contact shadow enabled have a mask used to select the bit that contains
        // the contact shadow shadowed information (occluded or not). Otherwise -1 is written
        void GetContactShadowMask(HDAdditionalLightData hdAdditionalLightData, BoolScalableSetting contactShadowEnabled, HDCamera hdCamera, ref int contactShadowMask, ref float rayTracingShadowFlag)
        {
            contactShadowMask = 0;
            rayTracingShadowFlag = 0.0f;
            // If contact shadows are not enabled or we already reached the manimal number of contact shadows
            if ((!hdAdditionalLightData.useContactShadow.Value(contactShadowEnabled))
                || m_ContactShadowIndex >= LightDefinitions.s_LightListMaxPrunedEntries)
                return;

            // Evaluate the contact shadow index of this light
            contactShadowMask = 1 << m_ContactShadowIndex++;

            // If this light has ray traced contact shadow
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && hdAdditionalLightData.rayTraceContactShadow)
                rayTracingShadowFlag = 1.0f;
        }

        struct ContactShadowsParameters
        {
            public ComputeShader    contactShadowsCS;
            public int              kernel;

            public Vector4          params1;
            public Vector4          params2;
            public int              sampleCount;

            public int              numTilesX;
            public int              numTilesY;
            public int              viewCount;

            public bool             rayTracingEnabled;
            public RayTracingShader contactShadowsRTS;
            public RayTracingAccelerationStructure accelerationStructure;
            public float            rayTracingBias;
            public int              actualWidth;
            public int              actualHeight;
            public int              depthTextureParameterName;
        }

        ContactShadowsParameters PrepareContactShadowsParameters(HDCamera hdCamera, float firstMipOffsetY)
        {
            var parameters = new ContactShadowsParameters();

            parameters.contactShadowsCS = contactShadowComputeShader;
            parameters.rayTracingEnabled = hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing);
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing))
            {
                RayTracingSettings raySettings = VolumeManager.instance.stack.GetComponent<RayTracingSettings>();
                parameters.contactShadowsRTS = m_Asset.renderPipelineRayTracingResources.shadowRaytracingRT;
                parameters.rayTracingBias = raySettings.rayBias.value;
                parameters.accelerationStructure = RequestAccelerationStructure();

                parameters.actualWidth = hdCamera.actualWidth;
                parameters.actualHeight = hdCamera.actualHeight;
            }

            parameters.kernel = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA) ? s_deferredContactShadowKernelMSAA : s_deferredContactShadowKernel;

            float contactShadowRange = Mathf.Clamp(m_ContactShadows.fadeDistance.value, 0.0f, m_ContactShadows.maxDistance.value);
            float contactShadowFadeEnd = m_ContactShadows.maxDistance.value;
            float contactShadowOneOverFadeRange = 1.0f / Math.Max(1e-6f, contactShadowRange);
            parameters.params1 = new Vector4(m_ContactShadows.length.value, m_ContactShadows.distanceScaleFactor.value, contactShadowFadeEnd, contactShadowOneOverFadeRange);
            parameters.params2 = new Vector4(firstMipOffsetY, 0.0f, 0.0f, 0.0f);
            parameters.sampleCount = m_ContactShadows.sampleCount.value;

            int deferredShadowTileSize = 16; // Must match DeferreDirectionalShadow.compute
            parameters.numTilesX = (hdCamera.actualWidth + (deferredShadowTileSize - 1)) / deferredShadowTileSize;
            parameters.numTilesY = (hdCamera.actualHeight + (deferredShadowTileSize - 1)) / deferredShadowTileSize;
            parameters.viewCount = hdCamera.viewCount;

            // TODO: Remove once we switch fully to render graph (auto binding of textures)
            parameters.depthTextureParameterName = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA) ? HDShaderIDs._CameraDepthValuesTexture : HDShaderIDs._CameraDepthTexture;

            return parameters;
        }

        static void RenderContactShadows(   in ContactShadowsParameters parameters,
                                            RTHandle                    contactShadowRT,
                                            RTHandle                    depthTexture,
                                            LightLoopLightData          lightLoopLightData,
                                            TileAndClusterData          tileAndClusterData,
                                            CommandBuffer               cmd)
        {

            cmd.SetComputeVectorParam(parameters.contactShadowsCS, HDShaderIDs._ContactShadowParamsParameters, parameters.params1);
            cmd.SetComputeVectorParam(parameters.contactShadowsCS, HDShaderIDs._ContactShadowParamsParameters2, parameters.params2);
            cmd.SetComputeIntParam(parameters.contactShadowsCS, HDShaderIDs._DirectionalContactShadowSampleCount, parameters.sampleCount);
            cmd.SetComputeBufferParam(parameters.contactShadowsCS, parameters.kernel, HDShaderIDs._DirectionalLightDatas, lightLoopLightData.directionalLightData);

            // Send light list to the compute
            cmd.SetComputeBufferParam(parameters.contactShadowsCS, parameters.kernel, HDShaderIDs._LightDatas, lightLoopLightData.lightData);
            cmd.SetComputeBufferParam(parameters.contactShadowsCS, parameters.kernel, HDShaderIDs.g_vLightListGlobal, tileAndClusterData.lightList);

            cmd.SetComputeTextureParam(parameters.contactShadowsCS, parameters.kernel, parameters.depthTextureParameterName, depthTexture);
            cmd.SetComputeTextureParam(parameters.contactShadowsCS, parameters.kernel, HDShaderIDs._ContactShadowTextureUAV, contactShadowRT);

            cmd.DispatchCompute(parameters.contactShadowsCS, parameters.kernel, parameters.numTilesX, parameters.numTilesY, parameters.viewCount);

            if (parameters.rayTracingEnabled)
            {
                cmd.SetRayTracingShaderPass(parameters.contactShadowsRTS, "VisibilityDXR");
                cmd.SetRayTracingFloatParam(parameters.contactShadowsRTS, HDShaderIDs._RaytracingRayBias, parameters.rayTracingBias);
                cmd.SetRayTracingAccelerationStructure(parameters.contactShadowsRTS, HDShaderIDs._RaytracingAccelerationStructureName, parameters.accelerationStructure);

                cmd.SetRayTracingVectorParam(parameters.contactShadowsRTS, HDShaderIDs._ContactShadowParamsParameters, parameters.params1);
                cmd.SetRayTracingVectorParam(parameters.contactShadowsRTS, HDShaderIDs._ContactShadowParamsParameters2, parameters.params2);
                cmd.SetRayTracingIntParam(parameters.contactShadowsRTS, HDShaderIDs._DirectionalContactShadowSampleCount, parameters.sampleCount);
                cmd.SetRayTracingBufferParam(parameters.contactShadowsRTS, HDShaderIDs._DirectionalLightDatas, lightLoopLightData.directionalLightData);

                // Send light list to the compute
                cmd.SetRayTracingBufferParam(parameters.contactShadowsRTS, HDShaderIDs._LightDatas, lightLoopLightData.lightData);
                cmd.SetRayTracingBufferParam(parameters.contactShadowsRTS, HDShaderIDs.g_vLightListGlobal, tileAndClusterData.lightList);

                cmd.SetRayTracingTextureParam(parameters.contactShadowsRTS, HDShaderIDs._DepthTexture, depthTexture);
                cmd.SetRayTracingTextureParam(parameters.contactShadowsRTS, HDShaderIDs._ContactShadowTextureUAV, contactShadowRT);

                cmd.DispatchRays(parameters.contactShadowsRTS, "RayGenContactShadows", (uint)parameters.actualWidth, (uint)parameters.actualHeight, (uint)parameters.viewCount);
            }
        }

        void RenderContactShadows(HDCamera hdCamera, CommandBuffer cmd)
        {
            // if there is no need to compute contact shadows, we just quit
            if (!WillRenderContactShadow())
                return;

            using (new ProfilingSample(cmd, "Contact Shadows", CustomSamplerId.TPScreenSpaceShadows.GetSampler()))
            {
                m_ShadowManager.BindResources(cmd);

                var depthTexture = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA) ? m_SharedRTManager.GetDepthValuesTexture() : m_SharedRTManager.GetDepthTexture();
                int firstMipOffsetY = m_SharedRTManager.GetDepthBufferMipChainInfo().mipLevelOffsets[1].y;
                var parameters = PrepareContactShadowsParameters(hdCamera, firstMipOffsetY);
                RenderContactShadows(parameters, m_ContactShadowBuffer, depthTexture, m_LightLoopLightData, m_TileAndClusterData, cmd);
            }
        }

        struct DeferredLightingParameters
        {
            public int                  numTilesX;
            public int                  numTilesY;
            public int                  numTiles;
            public bool                 enableTile;
            public bool                 outputSplitLighting;
            public bool                 useComputeLightingEvaluation;
            public bool                 enableFeatureVariants;
            public bool                 enableShadowMasks;
            public int                  numVariants;
            public DebugDisplaySettings debugDisplaySettings;

            // Compute Lighting
            public ComputeShader        deferredComputeShader;
            public int                  viewCount;

            // Full Screen Pixel (debug)
            public Material             splitLightingMat;
            public Material             regularLightingMat;
        }

        DeferredLightingParameters PrepareDeferredLightingParameters(HDCamera hdCamera, DebugDisplaySettings debugDisplaySettings)
        {
            var parameters = new DeferredLightingParameters();

            bool debugDisplayOrSceneLightOff = CoreUtils.IsSceneLightingDisabled(hdCamera.camera) || debugDisplaySettings.IsDebugDisplayEnabled();

            int w = hdCamera.actualWidth;
            int h = hdCamera.actualHeight;
            parameters.numTilesX = (w + 15) / 16;
            parameters.numTilesY = (h + 15) / 16;
            parameters.numTiles = parameters.numTilesX * parameters.numTilesY;
            parameters.enableTile = hdCamera.frameSettings.IsEnabled(FrameSettingsField.DeferredTile);
            parameters.outputSplitLighting = hdCamera.frameSettings.IsEnabled(FrameSettingsField.SubsurfaceScattering);
            parameters.useComputeLightingEvaluation = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ComputeLightEvaluation);
            parameters.enableFeatureVariants = GetFeatureVariantsEnabled(hdCamera.frameSettings) && !debugDisplayOrSceneLightOff;
            parameters.enableShadowMasks = m_enableBakeShadowMask;
            parameters.numVariants = LightDefinitions.s_NumFeatureVariants;
            parameters.debugDisplaySettings = debugDisplaySettings;

            // Compute Lighting
            parameters.deferredComputeShader = deferredComputeShader;
            parameters.viewCount = hdCamera.viewCount;

            // Full Screen Pixel (debug)
            parameters.splitLightingMat = GetDeferredLightingMaterial(true /*split lighting*/, parameters.enableShadowMasks, debugDisplayOrSceneLightOff);
            parameters.regularLightingMat = GetDeferredLightingMaterial(false /*split lighting*/, parameters.enableShadowMasks, debugDisplayOrSceneLightOff);

            return parameters;
        }

        struct DeferredLightingResources
        {
            public RenderTargetIdentifier[] colorBuffers;
            public RTHandle depthStencilBuffer;
            public RTHandle depthTexture;
            public ComputeBuffer lightListBuffer;
            public ComputeBuffer tileFeatureFlagsBuffer;
            public ComputeBuffer tileListBuffer;
            public ComputeBuffer dispatchIndirectBuffer;
        }

        DeferredLightingResources PrepareDeferredLightingResources()
        {
            var resources = new DeferredLightingResources();

            resources.colorBuffers = m_MRTCache2;
            resources.colorBuffers[0] = m_CameraColorBuffer;
            resources.colorBuffers[1] = m_CameraSssDiffuseLightingBuffer;
            resources.depthStencilBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            resources.depthTexture = m_SharedRTManager.GetDepthTexture();
            resources.lightListBuffer = m_TileAndClusterData.lightList;
            resources.tileFeatureFlagsBuffer = m_TileAndClusterData.tileFeatureFlags;
            resources.tileListBuffer = m_TileAndClusterData.tileList;
            resources.dispatchIndirectBuffer = m_TileAndClusterData.dispatchIndirectBuffer;

            return resources;
        }

        void RenderDeferredLighting(HDCamera hdCamera, CommandBuffer cmd)
        {
            if (hdCamera.frameSettings.litShaderMode != LitShaderMode.Deferred)
                return;

            var parameters = PrepareDeferredLightingParameters(hdCamera, debugDisplaySettings);
            var resources = PrepareDeferredLightingResources();

            if (parameters.enableTile)
            {
                bool useCompute = parameters.useComputeLightingEvaluation && !k_PreferFragment;
                if (useCompute)
                    RenderComputeDeferredLighting(parameters, resources, cmd);
                else
                    RenderComputeAsPixelDeferredLighting(parameters, resources, cmd);
            }
            else
            {
                RenderPixelDeferredLighting(parameters, resources, cmd);
            }
        }

        static void RenderComputeDeferredLighting(in DeferredLightingParameters parameters, in DeferredLightingResources resources, CommandBuffer cmd)
        {
            using (new ProfilingSample(cmd, "TilePass - Compute Deferred Lighting Pass", CustomSamplerId.TPRenderDeferredLighting.GetSampler()))
            {
                cmd.SetGlobalBuffer(HDShaderIDs.g_vLightListGlobal, resources.lightListBuffer);

                for (int variant = 0; variant < parameters.numVariants; variant++)
                {
                    int kernel;

                    if (parameters.enableFeatureVariants)
                    {
                        kernel = parameters.enableShadowMasks ? s_shadeOpaqueIndirectShadowMaskFptlKernels[variant] : s_shadeOpaqueIndirectFptlKernels[variant];
                    }
                    else
                    {
                        if (parameters.enableShadowMasks)
                        {
                            kernel = parameters.debugDisplaySettings.IsDebugDisplayEnabled() ? s_shadeOpaqueDirectShadowMaskFptlDebugDisplayKernel : s_shadeOpaqueDirectShadowMaskFptlKernel;
                        }
                        else
                        {
                            kernel = parameters.debugDisplaySettings.IsDebugDisplayEnabled() ? s_shadeOpaqueDirectFptlDebugDisplayKernel : s_shadeOpaqueDirectFptlKernel;
                        }
                    }

                    cmd.SetComputeTextureParam(parameters.deferredComputeShader, kernel, HDShaderIDs._CameraDepthTexture, resources.depthTexture);

                    // TODO: Is it possible to setup this outside the loop ? Can figure out how, get this: Property (specularLightingUAV) at kernel index (21) is not set
                    cmd.SetComputeTextureParam(parameters.deferredComputeShader, kernel, HDShaderIDs.specularLightingUAV, resources.colorBuffers[0]);
                    cmd.SetComputeTextureParam(parameters.deferredComputeShader, kernel, HDShaderIDs.diffuseLightingUAV, resources.colorBuffers[1]);

                    // always do deferred lighting in blocks of 16x16 (not same as tiled light size)
                    if (parameters.enableFeatureVariants)
                    {
                        cmd.SetComputeBufferParam(parameters.deferredComputeShader, kernel, HDShaderIDs.g_TileFeatureFlags, resources.tileFeatureFlagsBuffer);
                        cmd.SetComputeIntParam(parameters.deferredComputeShader, HDShaderIDs.g_TileListOffset, variant * parameters.numTiles * parameters.viewCount);
                        cmd.SetComputeBufferParam(parameters.deferredComputeShader, kernel, HDShaderIDs.g_TileList, resources.tileListBuffer);
                        cmd.DispatchCompute(parameters.deferredComputeShader, kernel, resources.dispatchIndirectBuffer, (uint)variant * 3 * sizeof(uint));
                    }
                    else
                    {
                        // 4x 8x8 groups per a 16x16 tile.
                        cmd.DispatchCompute(parameters.deferredComputeShader, kernel, parameters.numTilesX * 2, parameters.numTilesY * 2, parameters.viewCount);
                    }
                }
            }
        }

        static void RenderComputeAsPixelDeferredLighting(in DeferredLightingParameters parameters, in DeferredLightingResources resources, Material deferredMat, bool outputSplitLighting, CommandBuffer cmd)
        {
            CoreUtils.SetKeyword(cmd, "OUTPUT_SPLIT_LIGHTING", outputSplitLighting);
            CoreUtils.SetKeyword(cmd, "SHADOWS_SHADOWMASK", parameters.enableShadowMasks);

            if (parameters.enableFeatureVariants)
            {
                if (outputSplitLighting)
                    cmd.SetRenderTarget(resources.colorBuffers, resources.depthStencilBuffer);
                else
                    cmd.SetRenderTarget(resources.colorBuffers[0], resources.depthStencilBuffer);

                for (int variant = 0; variant < parameters.numVariants; variant++)
                {
                    cmd.SetGlobalInt(HDShaderIDs.g_TileListOffset, variant * parameters.numTiles);

                    cmd.EnableShaderKeyword(s_variantNames[variant]);

                    MeshTopology topology = k_HasNativeQuadSupport ? MeshTopology.Quads : MeshTopology.Triangles;
                    cmd.DrawProceduralIndirect(Matrix4x4.identity, deferredMat, 0, topology, resources.dispatchIndirectBuffer, variant * 4 * sizeof(uint), null);

                    // Must disable variant keyword because it will not get overridden.
                    cmd.DisableShaderKeyword(s_variantNames[variant]);
                }
            }
            else
            {
                CoreUtils.SetKeyword(cmd, "DEBUG_DISPLAY", parameters.debugDisplaySettings.IsDebugDisplayEnabled());

                if (outputSplitLighting)
                    CoreUtils.DrawFullScreen(cmd, deferredMat, resources.colorBuffers, resources.depthStencilBuffer, null, 1);
                else
                    CoreUtils.DrawFullScreen(cmd, deferredMat, resources.colorBuffers[0], resources.depthStencilBuffer, null, 1);
            }
        }

        static void RenderComputeAsPixelDeferredLighting(in DeferredLightingParameters parameters, in DeferredLightingResources resources, CommandBuffer cmd)
        {
            using (new ProfilingSample(cmd, "TilePass - Compute as Pixel Deferred Lighting Pass", CustomSamplerId.TPRenderDeferredLighting.GetSampler()))
            {
                cmd.SetGlobalBuffer(HDShaderIDs.g_vLightListGlobal, resources.lightListBuffer);

                cmd.SetGlobalTexture(HDShaderIDs._CameraDepthTexture, resources.depthTexture);
                cmd.SetGlobalBuffer(HDShaderIDs.g_TileFeatureFlags, resources.tileFeatureFlagsBuffer);
                cmd.SetGlobalBuffer(HDShaderIDs.g_TileList, resources.tileListBuffer);

                // If SSS is disabled, do lighting for both split lighting and no split lighting
                // Must set stencil parameters through Material.
                if (parameters.outputSplitLighting)
                {
                    RenderComputeAsPixelDeferredLighting(parameters, resources, s_DeferredTileSplitLightingMat, true, cmd);
                    RenderComputeAsPixelDeferredLighting(parameters, resources, s_DeferredTileRegularLightingMat, false, cmd);
                }
                else
                {
                    RenderComputeAsPixelDeferredLighting(parameters, resources, s_DeferredTileMat, false, cmd);
                }
            }
        }

        static void RenderPixelDeferredLighting(in DeferredLightingParameters parameters, in DeferredLightingResources resources, CommandBuffer cmd)
        {
            cmd.SetGlobalBuffer(HDShaderIDs.g_vLightListGlobal, resources.lightListBuffer);

            // First, render split lighting.
            if (parameters.outputSplitLighting)
            {
                using (new ProfilingSample(cmd, "SinglePass - Deferred Lighting Pass MRT", CustomSamplerId.TPRenderDeferredLighting.GetSampler()))
                {
                    CoreUtils.DrawFullScreen(cmd, parameters.splitLightingMat, resources.colorBuffers, resources.depthStencilBuffer);
                }
            }

            using (new ProfilingSample(cmd, "SinglePass - Deferred Lighting Pass", CustomSamplerId.TPRenderDeferredLighting.GetSampler()))
            {
                var currentLightingMaterial = parameters.regularLightingMat;
                // If SSS is disable, do lighting for both split lighting and no split lighting
                // This is for debug purpose, so fine to use immediate material mode here to modify render state
                if (!parameters.outputSplitLighting)
                {
                    currentLightingMaterial.SetInt(HDShaderIDs._StencilRef, (int)StencilLightingUsage.NoLighting);
                    currentLightingMaterial.SetInt(HDShaderIDs._StencilCmp, (int)CompareFunction.NotEqual);
                }
                else
                {
                    currentLightingMaterial.SetInt(HDShaderIDs._StencilRef, (int)StencilLightingUsage.RegularLighting);
                    currentLightingMaterial.SetInt(HDShaderIDs._StencilCmp, (int)CompareFunction.Equal);
                }

                CoreUtils.DrawFullScreen(cmd, currentLightingMaterial, resources.colorBuffers[0], resources.depthStencilBuffer);
            }
        }

        static void CopyStencilBufferForMaterialClassification(CommandBuffer cmd, RTHandle depthStencilBuffer, RTHandle stencilCopyBuffer, Material copyStencilMaterial)
        {
#if (UNITY_SWITCH || UNITY_IPHONE || UNITY_STANDALONE_OSX)
            // Faster on Switch.
            CoreUtils.SetRenderTarget(cmd, stencilCopyBuffer, depthStencilBuffer, ClearFlag.Color, Color.clear);

            copyStencilMaterial.SetInt(HDShaderIDs._StencilRef, (int)StencilLightingUsage.NoLighting);
            copyStencilMaterial.SetInt(HDShaderIDs._StencilMask, (int)HDRenderPipeline.StencilBitMask.LightingMask);

            // Use ShaderPassID 1 => "Pass 1 - Write 1 if value different from stencilRef to output"
            CoreUtils.DrawFullScreen(cmd, copyStencilMaterial, null, 1);
#else
            CoreUtils.SetRenderTarget(cmd, stencilCopyBuffer, ClearFlag.Color, Color.clear);
            CoreUtils.SetRenderTarget(cmd, depthStencilBuffer);
            cmd.SetRandomWriteTarget(1, stencilCopyBuffer);

            copyStencilMaterial.SetInt(HDShaderIDs._StencilRef, (int)StencilLightingUsage.NoLighting);
            copyStencilMaterial.SetInt(HDShaderIDs._StencilMask, (int)HDRenderPipeline.StencilBitMask.LightingMask);

            // Use ShaderPassID 3 => "Pass 3 - Initialize Stencil UAV copy with 1 if value different from stencilRef to output"
            CoreUtils.DrawFullScreen(cmd, copyStencilMaterial, null, 3);
            cmd.ClearRandomWriteTargets();
#endif
        }

        static void UpdateStencilBufferForSSRExclusion(CommandBuffer cmd, RTHandle depthStencilBuffer, RTHandle stencilCopyBuffer, Material copyStencilMaterial)
        {
            CoreUtils.SetRenderTarget(cmd, depthStencilBuffer);
            cmd.SetRandomWriteTarget(1, stencilCopyBuffer);

            copyStencilMaterial.SetInt(HDShaderIDs._StencilRef, (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR);
            copyStencilMaterial.SetInt(HDShaderIDs._StencilMask, (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR);

            // Pass 4 performs an OR between the already present content of the copy and the stencil ref, if stencil test passes.
            CoreUtils.DrawFullScreen(cmd, copyStencilMaterial, null, 4);
            cmd.ClearRandomWriteTargets();
        }

        static void CopyStencilBufferIfNeeded(CommandBuffer cmd, HDCamera hdCamera, RTHandle depthStencilBuffer, RTHandle stencilBufferCopy, Material copyStencil, Material copyStencilForSSR)
        {
            // Clear and copy the stencil texture needs to be moved to before we invoke the async light list build,
            // otherwise the async compute queue can end up using that texture before the graphics queue is done with it.
            // For the SSR we need the lighting flags to be copied into the stencil texture (it is use to discard object that have no lighting)
            if (GetFeatureVariantsEnabled(hdCamera.frameSettings) || hdCamera.frameSettings.IsEnabled(FrameSettingsField.SSR))
            {
                // For material classification we use compute shader and so can't read into the stencil, so prepare it.
                using (new ProfilingSample(cmd, "Clear and copy stencil texture for material classification", CustomSamplerId.ClearAndCopyStencilTexture.GetSampler()))
                {
                    CopyStencilBufferForMaterialClassification(cmd, depthStencilBuffer, stencilBufferCopy, copyStencil);
                }

                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.SSR))
                {
                    using (new ProfilingSample(cmd, "Update stencil copy for SSR Exclusion", CustomSamplerId.UpdateStencilCopyForSSRExclusion.GetSampler()))
                    {
                        UpdateStencilBufferForSSRExclusion(cmd, depthStencilBuffer, stencilBufferCopy, copyStencilForSSR);
                    }
                }
            }
        }

        struct LightLoopDebugOverlayParameters
        {
            public Material                 debugViewTilesMaterial;
            public TileAndClusterData       tileAndClusterData;
            public HDShadowManager          shadowManager;
            public int                      debugSelectedLightShadowIndex;
            public int                      debugSelectedLightShadowCount;
            public Material                 debugShadowMapMaterial;
        }

        LightLoopDebugOverlayParameters PrepareLightLoopDebugOverlayParameters()
        {
            var parameters = new LightLoopDebugOverlayParameters();

            parameters.debugViewTilesMaterial = m_DebugViewTilesMaterial;
            parameters.tileAndClusterData = m_TileAndClusterData;
            parameters.shadowManager = m_ShadowManager;
            parameters.debugSelectedLightShadowIndex = m_DebugSelectedLightShadowIndex;
            parameters.debugSelectedLightShadowCount = m_DebugSelectedLightShadowCount;
            parameters.debugShadowMapMaterial = m_DebugHDShadowMapMaterial;

            return parameters;
        }

        static void RenderLightLoopDebugOverlay(in DebugParameters debugParameters, CommandBuffer cmd, ref float x, ref float y, float overlaySize, RTHandle depthTexture)
        {
            LightingDebugSettings lightingDebug = debugParameters.debugDisplaySettings.data.lightingDebugSettings;
            if (lightingDebug.tileClusterDebug != TileClusterDebug.None)
            {
                using (new ProfilingSample(cmd, "Tiled/cluster Lighting Debug", CustomSamplerId.TPTiledLightingDebug.GetSampler()))
                {
                    var hdCamera = debugParameters.hdCamera;
                    var parameters = debugParameters.lightingOverlayParameters;

                    int w = hdCamera.actualWidth;
                    int h = hdCamera.actualHeight;
                    int numTilesX = (w + 15) / 16;
                    int numTilesY = (h + 15) / 16;
                    int numTiles = numTilesX * numTilesY;

                    // Debug tiles
                    if (lightingDebug.tileClusterDebug == TileClusterDebug.MaterialFeatureVariants)
                    {
                        if (GetFeatureVariantsEnabled(hdCamera.frameSettings))
                        {
                            // featureVariants
                            parameters.debugViewTilesMaterial.SetInt(HDShaderIDs._NumTiles, numTiles);
                            parameters.debugViewTilesMaterial.SetInt(HDShaderIDs._ViewTilesFlags, (int)lightingDebug.tileClusterDebugByCategory);
                            parameters.debugViewTilesMaterial.SetVector(HDShaderIDs._MousePixelCoord, HDUtils.GetMouseCoordinates(hdCamera));
                            parameters.debugViewTilesMaterial.SetVector(HDShaderIDs._MouseClickPixelCoord, HDUtils.GetMouseClickCoordinates(hdCamera));
                            parameters.debugViewTilesMaterial.SetBuffer(HDShaderIDs.g_TileList, parameters.tileAndClusterData.tileList);
                            parameters.debugViewTilesMaterial.SetBuffer(HDShaderIDs.g_DispatchIndirectBuffer, parameters.tileAndClusterData.dispatchIndirectBuffer);
                            parameters.debugViewTilesMaterial.EnableKeyword("USE_FPTL_LIGHTLIST");
                            parameters.debugViewTilesMaterial.DisableKeyword("USE_CLUSTERED_LIGHTLIST");
                            parameters.debugViewTilesMaterial.DisableKeyword("SHOW_LIGHT_CATEGORIES");
                            parameters.debugViewTilesMaterial.EnableKeyword("SHOW_FEATURE_VARIANTS");
                            if (DeferredUseComputeAsPixel(hdCamera.frameSettings))
                                parameters.debugViewTilesMaterial.EnableKeyword("IS_DRAWINSTANCEDINDIRECT");
                            else
                                parameters.debugViewTilesMaterial.DisableKeyword("IS_DRAWINSTANCEDINDIRECT");
                            cmd.DrawProcedural(Matrix4x4.identity, parameters.debugViewTilesMaterial, 0, MeshTopology.Triangles, numTiles * 6);
                        }
                    }
                    else // tile or cluster
                    {
                        bool bUseClustered = lightingDebug.tileClusterDebug == TileClusterDebug.Cluster;

                        // lightCategories
                        parameters.debugViewTilesMaterial.SetInt(HDShaderIDs._ViewTilesFlags, (int)lightingDebug.tileClusterDebugByCategory);
                        parameters.debugViewTilesMaterial.SetVector(HDShaderIDs._MousePixelCoord, HDUtils.GetMouseCoordinates(hdCamera));
                        parameters.debugViewTilesMaterial.SetVector(HDShaderIDs._MouseClickPixelCoord, HDUtils.GetMouseClickCoordinates(hdCamera));
                        parameters.debugViewTilesMaterial.SetBuffer(HDShaderIDs.g_vLightListGlobal, bUseClustered ? parameters.tileAndClusterData.perVoxelLightLists : parameters.tileAndClusterData.lightList);
                        parameters.debugViewTilesMaterial.SetTexture(HDShaderIDs._CameraDepthTexture, depthTexture);
                        parameters.debugViewTilesMaterial.EnableKeyword(bUseClustered ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");
                        parameters.debugViewTilesMaterial.DisableKeyword(!bUseClustered ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");
                        parameters.debugViewTilesMaterial.EnableKeyword("SHOW_LIGHT_CATEGORIES");
                        parameters.debugViewTilesMaterial.DisableKeyword("SHOW_FEATURE_VARIANTS");

                        CoreUtils.DrawFullScreen(cmd, parameters.debugViewTilesMaterial, 0);
                    }
                    }
                }
            }

        static void RenderShadowsDebugOverlay(in DebugParameters debugParameters, in HDShadowManager.ShadowDebugAtlasTextures atlasTextures, CommandBuffer cmd, ref float x, ref float y, float overlaySize, MaterialPropertyBlock mpb)
        {
            LightingDebugSettings lightingDebug = debugParameters.debugDisplaySettings.data.lightingDebugSettings;
            if (lightingDebug.shadowDebugMode != ShadowMapDebugMode.None)
            {
                using (new ProfilingSample(cmd, "Display Shadows", CustomSamplerId.TPDisplayShadows.GetSampler()))
                {
                    var hdCamera = debugParameters.hdCamera;
                    var parameters = debugParameters.lightingOverlayParameters;

                    switch (lightingDebug.shadowDebugMode)
                    {
                        case ShadowMapDebugMode.VisualizeShadowMap:
                            int startShadowIndex = (int)lightingDebug.shadowMapIndex;
                            int shadowRequestCount = 1;

#if UNITY_EDITOR
                            if (lightingDebug.shadowDebugUseSelection)
                            {
                                if (parameters.debugSelectedLightShadowIndex != -1 && parameters.debugSelectedLightShadowCount != 0)
                                {
                                    startShadowIndex = parameters.debugSelectedLightShadowIndex;
                                    shadowRequestCount = parameters.debugSelectedLightShadowCount;
                                }
                                else
                                {
                                    // We don't display any shadow map if the selected object is not a light
                                    shadowRequestCount = 0;
                                }
                            }
#endif

                            for (int shadowIndex = startShadowIndex; shadowIndex < startShadowIndex + shadowRequestCount; shadowIndex++)
                            {
                                parameters.shadowManager.DisplayShadowMap(atlasTextures, shadowIndex, cmd, parameters.debugShadowMapMaterial, x, y, overlaySize, overlaySize, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, mpb);
                                HDUtils.NextOverlayCoord(ref x, ref y, overlaySize, overlaySize, hdCamera);
                            }
                            break;
                        case ShadowMapDebugMode.VisualizePunctualLightAtlas:
                            parameters.shadowManager.DisplayShadowAtlas(atlasTextures.punctualShadowAtlas, cmd, parameters.debugShadowMapMaterial, x, y, overlaySize, overlaySize, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, mpb);
                            HDUtils.NextOverlayCoord(ref x, ref y, overlaySize, overlaySize, hdCamera);
                            break;
                        case ShadowMapDebugMode.VisualizeDirectionalLightAtlas:
                            parameters.shadowManager.DisplayShadowCascadeAtlas(atlasTextures.cascadeShadowAtlas, cmd, parameters.debugShadowMapMaterial, x, y, overlaySize, overlaySize, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, mpb);
                            HDUtils.NextOverlayCoord(ref x, ref y, overlaySize, overlaySize, hdCamera);
                            break;
                        case ShadowMapDebugMode.VisualizeAreaLightAtlas:
                            parameters.shadowManager.DisplayAreaLightShadowAtlas(atlasTextures.areaShadowAtlas, cmd, parameters.debugShadowMapMaterial, x, y, overlaySize, overlaySize, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, mpb);
                            HDUtils.NextOverlayCoord(ref x, ref y, overlaySize, overlaySize, hdCamera);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}
