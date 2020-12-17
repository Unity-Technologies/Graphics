using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    static class VisibleLightExtensionMethods
    {
        public struct VisibleLightAxisAndPosition
        {
            public Vector3 Position;
            public Vector3 Forward;
            public Vector3 Up;
            public Vector3 Right;
        }

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

        public static VisibleLightAxisAndPosition GetAxisAndPosition(this VisibleLight value)
        {
            var matrix = value.localToWorldMatrix;
            VisibleLightAxisAndPosition output;
            output.Position = matrix.GetColumn(3);
            output.Forward  = matrix.GetColumn(2);
            output.Up       = matrix.GetColumn(1);
            output.Right    = matrix.GetColumn(0);
            return output;
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
        ProbeVolume,
        Decal,
        DensityVolume, // WARNING: Currently lightlistbuild.compute assumes density volume is the last element in the LightCategory enum. Do not append new LightCategory types after DensityVolume. TODO: Fix .compute code.
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
        SSReflection = 1 << 18,
        ProbeVolume = 1 << 19
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

        // Screen space shadow flags
        public static uint s_ScreenSpaceColorShadowFlag = 0x100;
        public static uint s_InvalidScreenSpaceShadow = 0xff;
        public static uint s_ScreenSpaceShadowIndexMask = 0xff;
    }

    [GenerateHLSL]
    struct SFiniteLightBound
    {
        public Vector3 boxAxisX; // Scaled by the extents (half-size)
        public Vector3 boxAxisY; // Scaled by the extents (half-size)
        public Vector3 boxAxisZ; // Scaled by the extents (half-size)
        public Vector3 center;   // Center of the bounds (box) in camera space
        public float   scaleXY;  // Scale applied to the top of the box to turn it into a truncated pyramid (X = Y)
        public float   radius;     // Circumscribed sphere for the bounds (box)
    };

    [GenerateHLSL]
    struct LightVolumeData
    {
        public Vector3 lightPos;     // Of light's "origin"
        public uint lightVolume;     // Type index

        public Vector3 lightAxisX;   // Normalized
        public uint lightCategory;   // Category index

        public Vector3 lightAxisY;   // Normalized
        public float radiusSq;       // Cone and sphere: light range squared

        public Vector3 lightAxisZ;   // Normalized
        public float cotan;          // Cone: cotan of the aperture (half-angle)

        public Vector3 boxInnerDist; // Box: extents (half-size) of the inner box
        public uint featureFlags;

        public Vector3 boxInvRange;  // Box: 1 / (OuterBoxExtents - InnerBoxExtents)
        public float unused2;
    };

    /// <summary>
    /// Tile and Cluster Debug Mode.
    /// </summary>
    public enum TileClusterDebug : int
    {
        /// <summary>No Tile and Cluster debug.</summary>
        None,
        /// <summary>Display lighting tiles debug.</summary>
        Tile,
        /// <summary>Display lighting clusters debug.</summary>
        Cluster,
        /// <summary>Display material feautre variants.</summary>
        MaterialFeatureVariants
    };

    /// <summary>
    /// Cluster visualization mode.
    /// </summary>
    [GenerateHLSL]
    public enum ClusterDebugMode : int
    {
        /// <summary>Visualize Cluster on opaque objects.</summary>
        VisualizeOpaque,
        /// <summary>Visualize a slice of the Cluster at a given distance.</summary>
        VisualizeSlice
    }

    /// <summary>
    /// Light Volume Debug Mode.
    /// </summary>
    public enum LightVolumeDebug : int
    {
        /// <summary>Display light volumes as a gradient.</summary>
        Gradient,
        /// <summary>Display light volumes as shapes will color and edges depending on the light type.</summary>
        ColorAndEdge
    };

    /// <summary>
    /// Tile and Cluster Debug Categories.
    /// </summary>
    public enum TileClusterCategoryDebug : int
    {
        /// <summary>Punctual lights.</summary>
        Punctual = 1,
        /// <summary>Area lights.</summary>
        Area = 2,
        /// <summary>Area and punctual lights.</summary>
        AreaAndPunctual = 3,
        /// <summary>Environment lights.</summary>
        Environment = 4,
        /// <summary>Environment and punctual lights.</summary>
        EnvironmentAndPunctual = 5,
        /// <summary>Environment and area lights.</summary>
        EnvironmentAndArea = 6,
        /// <summary>All lights.</summary>
        EnvironmentAndAreaAndPunctual = 7,
		/// <summary>Probe Volumes.</summary>
        ProbeVolumes = 8,
        /// <summary>Decals.</summary>
        Decal = 16,
        /// <summary>Density Volumes.</summary>
        DensityVolumes = 32
    };

    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesLightList
    {
        [HLSLArray(ShaderConfig.k_XRMaxViewsForCBuffer, typeof(Matrix4x4))]
        public fixed float  g_mInvScrProjectionArr[ShaderConfig.k_XRMaxViewsForCBuffer * 16];
        [HLSLArray(ShaderConfig.k_XRMaxViewsForCBuffer, typeof(Matrix4x4))]
        public fixed float  g_mScrProjectionArr[ShaderConfig.k_XRMaxViewsForCBuffer * 16];
        [HLSLArray(ShaderConfig.k_XRMaxViewsForCBuffer, typeof(Matrix4x4))]
        public fixed float  g_mInvProjectionArr[ShaderConfig.k_XRMaxViewsForCBuffer * 16];
        [HLSLArray(ShaderConfig.k_XRMaxViewsForCBuffer, typeof(Matrix4x4))]
        public fixed float  g_mProjectionArr[ShaderConfig.k_XRMaxViewsForCBuffer * 16];

        public Vector4      g_screenSize;

        public Vector2Int   g_viDimensions;
        public int          g_iNrVisibLights;
        public uint         g_isOrthographic;

        public uint         g_BaseFeatureFlags;
        public int          g_iNumSamplesMSAA;
        public uint         _EnvLightIndexShift;
        public uint         _DecalIndexShift;

        public uint         _DensityVolumeIndexShift;
        public uint         _ProbeVolumeIndexShift;
        public uint         _Pad0_SVLL;
        public uint         _Pad1_SVLL;
    }

    internal struct ProcessedLightData
    {
        public HDAdditionalLightData    additionalLightData;
        public HDLightType              lightType;
        public LightCategory            lightCategory;
        public GPULightType             gpuLightType;
        public LightVolumeType          lightVolumeType;
        public float                    distanceToCamera;
        public float                    lightDistanceFade;
        public float                    volumetricDistanceFade;
        public bool                     isBakedShadowMask;
    }

    internal struct ProcessedProbeData
    {
        public HDProbe  hdProbe;
        public float    weight;
    }

    public partial class HDRenderPipeline
    {
        internal const int k_MaxCacheSize = 2000000000; //2 GigaByte
        internal const int k_MaxDirectionalLightsOnScreen = 512;
        internal const int k_MaxPunctualLightsOnScreen    = 2048;
        internal const int k_MaxAreaLightsOnScreen        = 1024;
        internal const int k_MaxDecalsOnScreen = 2048;
        internal const int k_MaxLightsOnScreen = k_MaxDirectionalLightsOnScreen + k_MaxPunctualLightsOnScreen + k_MaxAreaLightsOnScreen + k_MaxEnvLightsOnScreen;
        internal const int k_MaxEnvLightsOnScreen = 1024;
        internal const int k_MaxLightsPerClusterCell = 24;
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
        int m_MaxPlanarReflectionOnScreen;

        Texture2DArray  m_DefaultTexture2DArray;
        Cubemap         m_DefaultTextureCube;

        internal class LightLoopTextureCaches
        {
            // Structure for cookies used by directional and spotlights
            public LightCookieManager           lightCookieManager { get; private set; }
            public ReflectionProbeCache         reflectionProbeCache { get; private set; }
            public PlanarReflectionProbeCache   reflectionPlanarProbeCache { get; private set; }
            public List<Matrix4x4>              env2DCaptureVP { get; private set; }
            public List<Vector4>                env2DCaptureForward { get; private set; }
            public List<Vector4>                env2DAtlasScaleOffset {get; private set; } = new List<Vector4>();

            Material m_CubeToPanoMaterial;

            public void Initialize(HDRenderPipelineAsset hdrpAsset, RenderPipelineResources defaultResources,  IBLFilterBSDF[] iBLFilterBSDFArray)
            {
                var lightLoopSettings = hdrpAsset.currentPlatformRenderPipelineSettings.lightLoopSettings;

                m_CubeToPanoMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.cubeToPanoPS);

                lightCookieManager = new LightCookieManager(hdrpAsset, k_MaxCacheSize);

                env2DCaptureVP = new List<Matrix4x4>();
                env2DCaptureForward = new List<Vector4>();
                for (int i = 0, c = Mathf.Max(1, lightLoopSettings.maxPlanarReflectionOnScreen); i < c; ++i)
                {
                    env2DCaptureVP.Add(Matrix4x4.identity);
                    env2DCaptureForward.Add(Vector4.zero);
                    env2DAtlasScaleOffset.Add(Vector4.zero);
                }

                // For regular reflection probes, we need to convolve with all the BSDF functions
                GraphicsFormat probeCacheFormat = lightLoopSettings.reflectionProbeFormat == ReflectionAndPlanarProbeFormat.R11G11B10 ?
                                                  GraphicsFormat.B10G11R11_UFloatPack32 : GraphicsFormat.R16G16B16A16_SFloat;

                // BC6H requires CPP feature not yet available
                //if (lightLoopSettings.reflectionCacheCompressed)
                //{
                //    probeCacheFormat = GraphicsFormat.RGB_BC6H_SFloat;
                //}

                int reflectionCubeSize = lightLoopSettings.reflectionProbeCacheSize;
                int reflectionCubeResolution = (int)lightLoopSettings.reflectionCubemapSize;
                if (ReflectionProbeCache.GetApproxCacheSizeInByte(reflectionCubeSize, reflectionCubeResolution, iBLFilterBSDFArray.Length) > k_MaxCacheSize)
                    reflectionCubeSize = ReflectionProbeCache.GetMaxCacheSizeForWeightInByte(k_MaxCacheSize, reflectionCubeResolution, iBLFilterBSDFArray.Length);
                reflectionProbeCache = new ReflectionProbeCache(defaultResources, iBLFilterBSDFArray, reflectionCubeSize, reflectionCubeResolution, probeCacheFormat, true);

                // For planar reflection we only convolve with the GGX filter, otherwise it would be too expensive
                GraphicsFormat planarProbeCacheFormat = (GraphicsFormat)hdrpAsset.currentPlatformRenderPipelineSettings.lightLoopSettings.reflectionProbeFormat;
                int reflectionPlanarResolution = (int)lightLoopSettings.planarReflectionAtlasSize;
                reflectionPlanarProbeCache = new PlanarReflectionProbeCache(defaultResources, (IBLFilterGGX)iBLFilterBSDFArray[0], reflectionPlanarResolution, planarProbeCacheFormat, true);
            }

            public void Cleanup()
            {
                reflectionProbeCache.Release();
                reflectionPlanarProbeCache.Release();
                lightCookieManager.Release();

                CoreUtils.Destroy(m_CubeToPanoMaterial);
            }

            public void NewFrame()
            {
                lightCookieManager.NewFrame();
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

        // TODO RENDERGRAPH: When we remove the old pass, we need to remove/refactor this class
        // With render graph it's only useful for 2 buffers and a boolean value.
        class TileAndClusterData
        {
            // Internal to light list building
            public ComputeBuffer lightVolumeDataBuffer { get; private set; }
            public ComputeBuffer convexBoundsBuffer { get; private set; }
            public ComputeBuffer AABBBoundsBuffer { get; private set; }
            public ComputeBuffer globalLightListAtomic { get; private set; }

            // Tile Output
            public ComputeBuffer tileFeatureFlags { get; private set; } // Deferred
            public ComputeBuffer dispatchIndirectBuffer { get; private set; } // Deferred
            public ComputeBuffer tileList { get; private set; } // Deferred
            public ComputeBuffer lightList { get; private set; } // ContactShadows, Deferred, Forward w/ fptl

            // Cluster Output
            public ComputeBuffer perVoxelOffset { get; private set; } // Cluster
            public ComputeBuffer perTileLogBaseTweak { get; private set; } // Cluster
            // used for pre-pass coarse culling on 64x64 tiles
            public ComputeBuffer bigTileLightList { get; private set; } // Volumetric
            public ComputeBuffer perVoxelLightLists { get; private set; } // Cluster

            public bool listsAreClear = false;

            public bool clusterNeedsDepth { get; private set; }
            public bool hasTileBuffers { get; private set; }
            public int maxLightCount { get; private set; }

            public void Initialize(bool allocateTileBuffers, bool clusterNeedsDepth, int maxLightCount)
            {
                hasTileBuffers = allocateTileBuffers;
                this.clusterNeedsDepth = clusterNeedsDepth;
                this.maxLightCount = maxLightCount;
                globalLightListAtomic = new ComputeBuffer(1, sizeof(uint));
            }

            public void AllocateNonRenderGraphResolutionDependentBuffers(HDCamera hdCamera, int width, int height, int viewCount, int maxLightOnScreen)
            {
                if (hasTileBuffers)
                {
                    var nrTilesX = (width + LightDefinitions.s_TileSizeFptl - 1) / LightDefinitions.s_TileSizeFptl;
                    var nrTilesY = (height + LightDefinitions.s_TileSizeFptl - 1) / LightDefinitions.s_TileSizeFptl;
                    var nrTiles = nrTilesX * nrTilesY * viewCount;
                    const int capacityUShortsPerTile = 32;
                    const int dwordsPerTile = (capacityUShortsPerTile + 1) >> 1;        // room for 31 lights and a nrLights value.

                    // note that nrTiles include the viewCount in allocation below
                    lightList = new ComputeBuffer((int)LightCategory.Count * dwordsPerTile * nrTiles, sizeof(uint));       // enough list memory for a 4k x 4k display
                    tileList = new ComputeBuffer((int)LightDefinitions.s_NumFeatureVariants * nrTiles, sizeof(uint));
                    tileFeatureFlags = new ComputeBuffer(nrTiles, sizeof(uint));

                    // DispatchIndirect: Buffer with arguments has to have three integer numbers at given argsOffset offset: number of work groups in X dimension, number of work groups in Y dimension, number of work groups in Z dimension.
                    // DrawProceduralIndirect: Buffer with arguments has to have four integer numbers at given argsOffset offset: vertex count per instance, instance count, start vertex location, and start instance location
                    // Use use max size of 4 unit for allocation
                    dispatchIndirectBuffer = new ComputeBuffer(viewCount * LightDefinitions.s_NumFeatureVariants * 4, sizeof(uint), ComputeBufferType.IndirectArguments);
                }

                // Cluster
                {
                    var nrClustersX = (width + LightDefinitions.s_TileSizeClustered - 1) / LightDefinitions.s_TileSizeClustered;
                    var nrClustersY = (height + LightDefinitions.s_TileSizeClustered - 1) / LightDefinitions.s_TileSizeClustered;
                    var nrClusterTiles = nrClustersX * nrClustersY * viewCount;

                    perVoxelOffset = new ComputeBuffer((int)LightCategory.Count * (1 << k_Log2NumClusters) * nrClusterTiles, sizeof(uint));
                    perVoxelLightLists = new ComputeBuffer(NumLightIndicesPerClusteredTile() * nrClusterTiles, sizeof(uint));

                    if (clusterNeedsDepth)
                    {
                        perTileLogBaseTweak = new ComputeBuffer(nrClusterTiles, sizeof(float));
                    }
                }

                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass))
                {
                    var nrBigTilesX = (width + 63) / 64;
                    var nrBigTilesY = (height + 63) / 64;
                    var nrBigTiles = nrBigTilesX * nrBigTilesY * viewCount;
                    // TODO: (Nick) In the case of Probe Volumes, this buffer could be trimmed down / tuned more specifically to probe volumes if we added a s_MaxNrBigTileProbeVolumesPlusOne value.
                    bigTileLightList = new ComputeBuffer(LightDefinitions.s_MaxNrBigTileLightsPlusOne * nrBigTiles, sizeof(uint));
                }

                // The bounds and light volumes are view-dependent, and AABB is additionally projection dependent.
                AABBBoundsBuffer = new ComputeBuffer(viewCount * 2 * maxLightOnScreen, 4 * sizeof(float));

                // Make sure to invalidate the content of the buffers
                listsAreClear = false;
            }


            public void AllocateResolutionDependentBuffers(HDCamera hdCamera, int width, int height, int viewCount, int maxLightOnScreen, bool renderGraphEnabled)
            {
                convexBoundsBuffer = new ComputeBuffer(viewCount * maxLightOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SFiniteLightBound)));
                lightVolumeDataBuffer = new ComputeBuffer(viewCount * maxLightOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LightVolumeData)));

                if (!renderGraphEnabled)
                    AllocateNonRenderGraphResolutionDependentBuffers(hdCamera, width, height, viewCount, maxLightOnScreen);

                // Make sure to invalidate the content of the buffers
                listsAreClear = false;
            }

            public void ReleaseNonRenderGraphResolutionDependentBuffers()
            {
                CoreUtils.SafeRelease(lightList);
                CoreUtils.SafeRelease(tileList);
                CoreUtils.SafeRelease(tileFeatureFlags);
                lightList = null;
                tileList = null;
                tileFeatureFlags = null;

                // enableClustered
                CoreUtils.SafeRelease(perVoxelLightLists);
                CoreUtils.SafeRelease(perVoxelOffset);
                CoreUtils.SafeRelease(perTileLogBaseTweak);
                perVoxelLightLists = null;
                perVoxelOffset = null;
                perTileLogBaseTweak = null;

                // enableBigTilePrepass
                CoreUtils.SafeRelease(bigTileLightList);
                bigTileLightList = null;

                // LightList building
                CoreUtils.SafeRelease(AABBBoundsBuffer);
                CoreUtils.SafeRelease(dispatchIndirectBuffer);
                AABBBoundsBuffer = null;
                dispatchIndirectBuffer = null;
            }

            public void ReleaseResolutionDependentBuffers()
            {
                CoreUtils.SafeRelease(convexBoundsBuffer);
                CoreUtils.SafeRelease(lightVolumeDataBuffer);
                convexBoundsBuffer = null;
                lightVolumeDataBuffer = null;

                ReleaseNonRenderGraphResolutionDependentBuffers();
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
        TileAndClusterData m_ProbeVolumeClusterData;

        // HDRenderPipeline needs to cache m_ProbeVolumeList as a member variable, as it cannot be passed in directly into BuildGPULightListProbeVolumesCommon() async compute
        // due to the HDGPUAsyncTask API. We could have extended HDGPUAsyncTaskParams definition to contain a ProbeVolumeList, but this seems worse, as all other async compute
        // tasks do not care about it.
        ProbeVolumeList m_ProbeVolumeList;

        // This control if we use cascade borders for directional light by default
        static internal readonly bool s_UseCascadeBorders = true;

        // Keep sorting array around to avoid garbage
        uint[] m_SortKeys = null;
        DynamicArray<ProcessedLightData> m_ProcessedLightData = new DynamicArray<ProcessedLightData>();
        DynamicArray<ProcessedProbeData> m_ProcessedReflectionProbeData = new DynamicArray<ProcessedProbeData>();
        DynamicArray<ProcessedProbeData> m_ProcessedPlanarProbeData = new DynamicArray<ProcessedProbeData>();

        void UpdateSortKeysArray(int count)
        {
            if (m_SortKeys == null ||count > m_SortKeys.Length)
            {
                m_SortKeys = new uint[count];
            }
        }

        static readonly Matrix4x4 s_FlipMatrixLHSRHS = Matrix4x4.Scale(new Vector3(1, 1, -1));

        Matrix4x4 GetWorldToViewMatrix(HDCamera hdCamera, int viewIndex)
        {
            var viewMatrix = (hdCamera.xr.enabled ? hdCamera.xr.GetViewMatrix(viewIndex) : hdCamera.camera.worldToCameraMatrix);

            // camera.worldToCameraMatrix is RHS and Unity's transforms are LHS, we need to flip it to work with transforms.
            // Note that this is equivalent to s_FlipMatrixLHSRHS * viewMatrix, but faster given that it doesn't need full matrix multiply
            // However if for some reason s_FlipMatrixLHSRHS changes from Matrix4x4.Scale(new Vector3(1, 1, -1)), this need to change as well.
            viewMatrix.m20 *= -1;
            viewMatrix.m21 *= -1;
            viewMatrix.m22 *= -1;
            viewMatrix.m23 *= -1;

            return viewMatrix;
        }

        // Matrix used for LightList building, keep them around to avoid GC
        Matrix4x4[] m_LightListProjMatrices = new Matrix4x4[ShaderConfig.s_XrMaxViews];

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
                public List<SFiniteLightBound> probeVolumesBounds;
                public List<LightVolumeData> probeVolumesLightVolumes;
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
                    lightsPerView[i].probeVolumesBounds.Clear();
                    lightsPerView[i].probeVolumesLightVolumes.Clear();
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
                    lightsPerView.Add(new LightsPerView { bounds = new List<SFiniteLightBound>(), lightVolumes = new List<LightVolumeData>(), probeVolumesBounds = new List<SFiniteLightBound>(), probeVolumesLightVolumes = new List<LightVolumeData>() });
                }
            }
        }

        internal LightList m_lightList;
        int m_TotalLightCount = 0;
        int m_DensityVolumeCount = 0;
        int m_ProbeVolumeCount = 0;
        bool m_EnableBakeShadowMask = false; // Track if any light require shadow mask. In this case we will need to enable the keyword shadow mask

        ComputeShader buildScreenAABBShader { get { return defaultResources.shaders.buildScreenAABBCS; } }
        ComputeShader buildPerTileLightListShader { get { return defaultResources.shaders.buildPerTileLightListCS; } }
        ComputeShader buildPerBigTileLightListShader { get { return defaultResources.shaders.buildPerBigTileLightListCS; } }
        ComputeShader buildPerVoxelLightListShader { get { return defaultResources.shaders.buildPerVoxelLightListCS; } }
        ComputeShader clearClusterAtomicIndexShader { get { return defaultResources.shaders.lightListClusterClearAtomicIndexCS; } }
        ComputeShader buildMaterialFlagsShader { get { return defaultResources.shaders.buildMaterialFlagsCS; } }
        ComputeShader buildDispatchIndirectShader { get { return defaultResources.shaders.buildDispatchIndirectCS; } }
        ComputeShader clearDispatchIndirectShader { get { return defaultResources.shaders.clearDispatchIndirectCS; } }
        ComputeShader deferredComputeShader { get { return defaultResources.shaders.deferredCS; } }
        ComputeShader contactShadowComputeShader { get { return defaultResources.shaders.contactShadowCS; } }
        Shader screenSpaceShadowsShader { get { return defaultResources.shaders.screenSpaceShadowPS; } }

        Shader deferredTilePixelShader { get { return defaultResources.shaders.deferredTilePS; } }

        ShaderVariablesLightList m_ShaderVariablesLightListCB = new ShaderVariablesLightList();
        ShaderVariablesLightList m_ShaderVariablesProbeVolumeLightListCB = new ShaderVariablesLightList();


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

        static int s_GenListPerTileKernel;
        static int[,] s_ClusterKernels = new int[(int)ClusterPrepassSource.Count, (int)ClusterDepthSource.Count];
        static int[,] s_ClusterObliqueKernels = new int[(int)ClusterPrepassSource.Count, (int)ClusterDepthSource.Count];
        static int s_ClearVoxelAtomicKernel;
        static int s_ClearDispatchIndirectKernel;
        static int s_BuildIndirectKernel;
        static int s_ClearDrawProceduralIndirectKernel;
        static int s_BuildMaterialFlagsWriteKernel;
        static int s_BuildMaterialFlagsOrKernel;

        static int s_shadeOpaqueDirectFptlKernel;
        static int s_shadeOpaqueDirectFptlDebugDisplayKernel;
        static int s_shadeOpaqueDirectShadowMaskFptlKernel;
        static int s_shadeOpaqueDirectShadowMaskFptlDebugDisplayKernel;

        static int[] s_shadeOpaqueIndirectFptlKernels = new int[LightDefinitions.s_NumFeatureVariants];

        static int s_deferredContactShadowKernel;

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

        ContactShadows m_ContactShadows = null;
        bool m_EnableContactShadow = false;

        IndirectLightingController m_indirectLightingController = null;

        // Following is an array of material of size eight for all combination of keyword: OUTPUT_SPLIT_LIGHTING - LIGHTLOOP_DISABLE_TILE_AND_CLUSTER - SHADOWS_SHADOWMASK - USE_FPTL_LIGHTLIST/USE_CLUSTERED_LIGHTLIST - DEBUG_DISPLAY
        Material[] m_deferredLightingMaterial;
        Material m_DebugViewTilesMaterial;
        Material m_DebugHDShadowMapMaterial;
        Material m_DebugBlitMaterial;
        Material m_DebugDisplayProbeVolumeMaterial;

        HashSet<HDAdditionalLightData> m_ScreenSpaceShadowsUnion = new HashSet<HDAdditionalLightData>();

        // Directional light
        Light m_CurrentSunLight;
        int m_CurrentShadowSortedSunLightIndex = -1;
        HDAdditionalLightData m_CurrentSunLightAdditionalLightData;
        DirectionalLightData m_CurrentSunLightDirectionalLightData;
        Light GetCurrentSunLight() { return m_CurrentSunLight; }

        // Screen space shadow data
        struct ScreenSpaceShadowData
        {
            public HDAdditionalLightData additionalLightData;
            public int lightDataIndex;
            public bool valid;
        }

        int m_ScreenSpaceShadowIndex = 0;
        int m_ScreenSpaceShadowChannelSlot = 0;
        ScreenSpaceShadowData[] m_CurrentScreenSpaceShadowData;

        // Contact shadow index reseted at the beginning of each frame, used to generate the contact shadow mask
        int m_ContactShadowIndex;

        // shadow related stuff
        HDShadowManager m_ShadowManager;
        HDShadowInitParameters m_ShadowInitParameters;

        // Used to shadow shadow maps with use selection enabled in the debug menu
        int m_DebugSelectedLightShadowIndex;
        int m_DebugSelectedLightShadowCount;

        // Data needed for the PrepareGPULightdata
        List<Matrix4x4> m_WorldToViewMatrices = new List<Matrix4x4>(ShaderConfig.s_XrMaxViews);

        static MaterialPropertyBlock m_LightLoopDebugMaterialProperties = new MaterialPropertyBlock();

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
                m_ShadowInitParameters,
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
            m_DebugBlitMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.debugBlitQuad);
            m_DebugDisplayProbeVolumeMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.debugDisplayProbeVolumePS);

            m_MaxDirectionalLightsOnScreen = lightLoopSettings.maxDirectionalLightsOnScreen;
            m_MaxPunctualLightsOnScreen = lightLoopSettings.maxPunctualLightsOnScreen;
            m_MaxAreaLightsOnScreen = lightLoopSettings.maxAreaLightsOnScreen;
            m_MaxDecalsOnScreen = lightLoopSettings.maxDecalsOnScreen;
            m_MaxEnvLightsOnScreen = lightLoopSettings.maxEnvLightsOnScreen;
            m_MaxLightsOnScreen = m_MaxDirectionalLightsOnScreen + m_MaxPunctualLightsOnScreen + m_MaxAreaLightsOnScreen + m_MaxEnvLightsOnScreen;
            m_MaxPlanarReflectionOnScreen = lightLoopSettings.maxPlanarReflectionOnScreen;

            // Cluster
            {
                s_ClearVoxelAtomicKernel = clearClusterAtomicIndexShader.FindKernel("ClearAtomic");

                for (int i = 0; i < (int)ClusterPrepassSource.Count; ++i)
                {
                    for (int j = 0; j < (int)ClusterDepthSource.Count; ++j)
                    {
                        s_ClusterKernels[i, j] = buildPerVoxelLightListShader.FindKernel(s_ClusterKernelNames[i, j]);
                        s_ClusterObliqueKernels[i, j] = buildPerVoxelLightListShader.FindKernel(s_ClusterObliqueKernelNames[i, j]);
                    }
                }
            }

            s_GenListPerTileKernel = buildPerTileLightListShader.FindKernel("TileLightListGen");

            s_GenListPerBigTileKernel = buildPerBigTileLightListShader.FindKernel("BigTileLightListGen");

            s_BuildIndirectKernel = buildDispatchIndirectShader.FindKernel("BuildIndirect");
            s_ClearDispatchIndirectKernel = clearDispatchIndirectShader.FindKernel("ClearDispatchIndirect");

            s_ClearDrawProceduralIndirectKernel = clearDispatchIndirectShader.FindKernel("ClearDrawProceduralIndirect");

            s_BuildMaterialFlagsWriteKernel = buildMaterialFlagsShader.FindKernel("MaterialFlagsGen");

            s_shadeOpaqueDirectFptlKernel = deferredComputeShader.FindKernel("Deferred_Direct_Fptl");
            s_shadeOpaqueDirectFptlDebugDisplayKernel = deferredComputeShader.FindKernel("Deferred_Direct_Fptl_DebugDisplay");

            s_deferredContactShadowKernel = contactShadowComputeShader.FindKernel("DeferredContactShadow");

            for (int variant = 0; variant < LightDefinitions.s_NumFeatureVariants; variant++)
            {
                s_shadeOpaqueIndirectFptlKernels[variant] = deferredComputeShader.FindKernel("Deferred_Indirect_Fptl_Variant" + variant);
            }

            m_TextureCaches.Initialize(asset, defaultResources, iBLFilterBSDFArray);
            // All the allocation of the compute buffers need to happened after the kernel finding in order to avoid the leak loop when a shader does not compile or is not available
            m_LightLoopLightData.Initialize(m_MaxDirectionalLightsOnScreen, m_MaxPunctualLightsOnScreen, m_MaxAreaLightsOnScreen, m_MaxEnvLightsOnScreen, m_MaxDecalsOnScreen);

            m_TileAndClusterData.Initialize(allocateTileBuffers: true, clusterNeedsDepth: k_UseDepthBuffer, maxLightCount: m_MaxLightsOnScreen);
            if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.MaterialPass)
            {
                m_ProbeVolumeClusterData = new TileAndClusterData();
                m_ProbeVolumeClusterData.Initialize(allocateTileBuffers: false, clusterNeedsDepth: false, maxLightCount: k_MaxVisibleProbeVolumeCount);
            }

            // OUTPUT_SPLIT_LIGHTING - SHADOWS_SHADOWMASK - DEBUG_DISPLAY
            m_deferredLightingMaterial = new Material[8];
            int stencilMask = (int)StencilUsage.RequiresDeferredLighting | (int)StencilUsage.SubsurfaceScattering;

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

                        int stencilRef = (int)StencilUsage.RequiresDeferredLighting;

                        if (outputSplitLighting == 1)
                        {
                            stencilRef |= (int)StencilUsage.SubsurfaceScattering;
                        }

                        m_deferredLightingMaterial[index].SetInt(HDShaderIDs._StencilMask, stencilMask);
                        m_deferredLightingMaterial[index].SetInt(HDShaderIDs._StencilRef, stencilRef);
                        m_deferredLightingMaterial[index].SetInt(HDShaderIDs._StencilCmp, (int)CompareFunction.Equal);
                    }
                }
            }

            // Stencil set to only touch "regular lighting" pixels.
            s_DeferredTileRegularLightingMat = CoreUtils.CreateEngineMaterial(deferredTilePixelShader);
            s_DeferredTileRegularLightingMat.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.RequiresDeferredLighting | (int)StencilUsage.SubsurfaceScattering);
            s_DeferredTileRegularLightingMat.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.RequiresDeferredLighting);
            s_DeferredTileRegularLightingMat.SetInt(HDShaderIDs._StencilCmp, (int)CompareFunction.Equal);

            // Stencil set to only touch "split-lighting" pixels.
            s_DeferredTileSplitLightingMat = CoreUtils.CreateEngineMaterial(deferredTilePixelShader);
            s_DeferredTileSplitLightingMat.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.SubsurfaceScattering);
            s_DeferredTileSplitLightingMat.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.SubsurfaceScattering);
            s_DeferredTileSplitLightingMat.SetInt(HDShaderIDs._StencilCmp, (int)CompareFunction.Equal);

            // Stencil set to touch all pixels excepted background/sky.
            s_DeferredTileMat = CoreUtils.CreateEngineMaterial(deferredTilePixelShader);
            s_DeferredTileMat.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.RequiresDeferredLighting);
            s_DeferredTileMat.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.Clear);
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

            // Setup screen space shadow map usage.
            // Screen space shadow map are currently only used with Raytracing and are a global keyword.
            // either we support it and then use the variant that allow to enable/disable them, or we don't
            // and use the variant that have them disabled.
            // So this mean that even if we disable screen space shadow in frame settings, the version
            // of the shader for the variant SCREEN_SPACE_SHADOWS is used, but a dynamic branch disable it.
            if (shadowParams.supportScreenSpaceShadows)
            {
                Shader.EnableKeyword("SCREEN_SPACE_SHADOWS_ON");
                Shader.DisableKeyword("SCREEN_SPACE_SHADOWS_OFF");
            }
            else
            {
                Shader.DisableKeyword("SCREEN_SPACE_SHADOWS_ON");
                Shader.EnableKeyword("SCREEN_SPACE_SHADOWS_OFF");
            }

            InitShadowSystem(asset, defaultResources);

            s_lightVolumes = new DebugLightVolumes();
            s_lightVolumes.InitData(defaultResources);

            // Screen space shadow
            int numMaxShadows = Math.Max(m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots, 1);
            m_CurrentScreenSpaceShadowData = new ScreenSpaceShadowData[numMaxShadows];
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
            if (m_ProbeVolumeClusterData != null)
                m_ProbeVolumeClusterData.Cleanup();

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
            CoreUtils.Destroy(m_DebugBlitMaterial);
            CoreUtils.Destroy(m_DebugDisplayProbeVolumeMaterial);
        }

        void LightLoopNewRender()
        {
            m_ScreenSpaceShadowsUnion.Clear();
        }

        void LightLoopNewFrame(CommandBuffer cmd, HDCamera hdCamera)
        {
            var frameSettings = hdCamera.frameSettings;

            m_ContactShadows = hdCamera.volumeStack.GetComponent<ContactShadows>();
            m_EnableContactShadow = frameSettings.IsEnabled(FrameSettingsField.ContactShadows) && m_ContactShadows.enable.value && m_ContactShadows.length.value > 0;
            m_indirectLightingController = hdCamera.volumeStack.GetComponent<IndirectLightingController>();

            m_ContactShadowIndex = 0;

            m_TextureCaches.NewFrame();

            m_WorldToViewMatrices.Clear();
            int viewCount = hdCamera.viewCount;
            for (int viewIndex = 0; viewIndex < viewCount; ++viewIndex)
            {
                m_WorldToViewMatrices.Add(GetWorldToViewMatrix(hdCamera, viewIndex));
            }

            // Clear the cookie atlas if needed at the beginning of the frame.
            if (m_DebugDisplaySettings.data.lightingDebugSettings.clearCookieAtlas)
            {
                m_TextureCaches.lightCookieManager.ResetAllocator();
                m_TextureCaches.lightCookieManager.ClearAtlasTexture(cmd);
            }
        }

        static int NumLightIndicesPerClusteredTile()
        {
            return 32 * (1 << k_Log2NumClusters);       // total footprint for all layers of the tile (measured in light index entries)
        }

        void LightLoopAllocResolutionDependentBuffers(HDCamera hdCamera, int width, int height)
        {
            m_TileAndClusterData.AllocateResolutionDependentBuffers(hdCamera, width, height, m_MaxViewCount, m_MaxLightsOnScreen, m_EnableRenderGraph);
            if (m_ProbeVolumeClusterData != null)
                m_ProbeVolumeClusterData.AllocateResolutionDependentBuffers(hdCamera, width, height, m_MaxViewCount, k_MaxVisibleProbeVolumeCount, m_EnableRenderGraph);
        }

        void LightLoopReleaseResolutionDependentBuffers()
        {
            m_TileAndClusterData.ReleaseResolutionDependentBuffers();
            if (m_ProbeVolumeClusterData != null)
                m_ProbeVolumeClusterData.ReleaseResolutionDependentBuffers();
        }

        void LightLoopCleanupNonRenderGraphResources()
        {
            m_TileAndClusterData.ReleaseNonRenderGraphResolutionDependentBuffers();
            if (m_ProbeVolumeClusterData != null)
                m_ProbeVolumeClusterData.ReleaseNonRenderGraphResolutionDependentBuffers();
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

        internal static Vector3 GetLightColor(VisibleLight light)
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

        static Vector3 ComputeAtmosphericOpticalDepth(PhysicallyBasedSky skySettings, float r, float cosTheta, bool alwaysAboveHorizon = false)
        {
            float R = skySettings.GetPlanetaryRadius();

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
        static Vector3 EvaluateAtmosphericAttenuation(PhysicallyBasedSky skySettings, Vector3 L, Vector3 X)
        {
            Vector3 C = skySettings.GetPlanetCenterPosition(X); // X = camPosWS

            float r = Vector3.Distance(X, C);
            float R = skySettings.GetPlanetaryRadius();
            float cosHoriz = ComputeCosineOfHorizonAngle(r, R);
            float cosTheta = Vector3.Dot(X - C, L) * Rcp(r);

            if (cosTheta > cosHoriz) // Above horizon
            {
                Vector3 oDepth = ComputeAtmosphericOpticalDepth(skySettings, r, cosTheta, true);
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

        internal void GetDirectionalLightData(CommandBuffer cmd, HDCamera hdCamera, VisibleLight light, Light lightComponent, int lightIndex, int shadowIndex,
            int sortedIndex, bool isPhysicallyBasedSkyActive, ref int screenSpaceShadowIndex, ref int screenSpaceShadowslot)
        {
            var processedData = m_ProcessedLightData[lightIndex];
            var additionalLightData = processedData.additionalLightData;
            var gpuLightType = processedData.gpuLightType;

            var lightData = new DirectionalLightData();

            lightData.lightLayers = hdCamera.frameSettings.IsEnabled(FrameSettingsField.LightLayers) ? additionalLightData.GetLightLayers() : uint.MaxValue;

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

            lightData.shadowIndex = -1;
            lightData.screenSpaceShadowIndex = (int)LightDefinitions.s_InvalidScreenSpaceShadow;
            lightData.isRayTracedContactShadow = 0.0f;

            if (lightComponent != null && lightComponent.cookie != null)
            {
                lightData.cookieMode = lightComponent.cookie.wrapMode == TextureWrapMode.Repeat ? CookieMode.Repeat : CookieMode.Clamp;
                lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.Fetch2DCookie(cmd, lightComponent.cookie);
            }
            else
            {
                lightData.cookieMode = CookieMode.None;
            }

            if (additionalLightData.surfaceTexture == null)
            {
                lightData.surfaceTextureScaleOffset = Vector4.zero;
            }
            else
            {
                lightData.surfaceTextureScaleOffset = m_TextureCaches.lightCookieManager.Fetch2DCookie(cmd, additionalLightData.surfaceTexture);
            }

            lightData.shadowDimmer           = additionalLightData.shadowDimmer;
            lightData.volumetricShadowDimmer = additionalLightData.volumetricShadowDimmer;
            GetContactShadowMask(additionalLightData, HDAdditionalLightData.ScalableSettings.UseContactShadow(m_Asset), hdCamera, isRasterization: true, ref lightData.contactShadowMask,ref lightData.isRayTracedContactShadow);

            // We want to have a colored penumbra if the flag is on and the color is not gray
            bool penumbraTint = additionalLightData.penumbraTint && ((additionalLightData.shadowTint.r != additionalLightData.shadowTint.g) || (additionalLightData.shadowTint.g != additionalLightData.shadowTint.b));
            lightData.penumbraTint = penumbraTint ? 1.0f : 0.0f;
            if (penumbraTint)
                lightData.shadowTint = new Vector3(additionalLightData.shadowTint.r * additionalLightData.shadowTint.r, additionalLightData.shadowTint.g * additionalLightData.shadowTint.g, additionalLightData.shadowTint.b * additionalLightData.shadowTint.b);
            else
                lightData.shadowTint = new Vector3(additionalLightData.shadowTint.r, additionalLightData.shadowTint.g, additionalLightData.shadowTint.b);

            // fix up shadow information
            lightData.shadowIndex = shadowIndex;
            if (shadowIndex != -1)
            {
                if (additionalLightData.WillRenderScreenSpaceShadow())
                {
                    lightData.screenSpaceShadowIndex = screenSpaceShadowslot;
                    if (additionalLightData.colorShadow && additionalLightData.WillRenderRayTracedShadow())
                    {
                        screenSpaceShadowslot += 3;
                        lightData.screenSpaceShadowIndex |= (int)LightDefinitions.s_ScreenSpaceColorShadowFlag;
                    }
                    else
                    {
                        screenSpaceShadowslot++;
                    }
                    screenSpaceShadowIndex++;
                    m_ScreenSpaceShadowsUnion.Add(additionalLightData);
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

            if (processedData.isBakedShadowMask)
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

            bool interactsWithSky = isPhysicallyBasedSkyActive && additionalLightData.interactsWithSky;

            lightData.distanceFromCamera = -1; // Encode 'interactsWithSky'

            if (interactsWithSky)
            {
                lightData.distanceFromCamera = additionalLightData.distance;

                if (ShaderConfig.s_PrecomputedAtmosphericAttenuation != 0)
                {
                    var skySettings = hdCamera.volumeStack.GetComponent<PhysicallyBasedSky>();

                    // Ignores distance (at infinity).
                    Vector3 transm = EvaluateAtmosphericAttenuation(skySettings, - lightData.forward, hdCamera.camera.transform.position);
                    lightData.color.x *= transm.x;
                    lightData.color.y *= transm.y;
                    lightData.color.z *= transm.z;
                }
            }

            lightData.angularDiameter = additionalLightData.angularDiameter * Mathf.Deg2Rad;
            lightData.flareSize       = Mathf.Max(additionalLightData.flareSize * Mathf.Deg2Rad, 5.960464478e-8f);
            lightData.flareFalloff    = additionalLightData.flareFalloff;
            lightData.flareTint       = (Vector3)(Vector4)additionalLightData.flareTint;
            lightData.surfaceTint     = (Vector3)(Vector4)additionalLightData.surfaceTint;

            // Fallback to the first non shadow casting directional light.
            m_CurrentSunLight = m_CurrentSunLight == null ? lightComponent : m_CurrentSunLight;

            m_lightList.directionalLights.Add(lightData);
        }

        // This function evaluates if there is currently enough screen space sahdow slots of a given light based on its light type
        bool EnoughScreenSpaceShadowSlots(GPULightType gpuLightType, int screenSpaceChannelSlot)
        {
            if(gpuLightType == GPULightType.Rectangle)
            {
                // Area lights require two shadow slots
                return (screenSpaceChannelSlot + 1) < m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots;
            }
            else
            {
                return screenSpaceChannelSlot < m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots;
            }
        }

        internal void GetLightData(CommandBuffer cmd, HDCamera hdCamera, HDShadowSettings shadowSettings, VisibleLight light, Light lightComponent,
            in ProcessedLightData processedData, int shadowIndex, BoolScalableSetting contactShadowsScalableSetting, bool isRasterization, ref Vector3 lightDimensions, ref int screenSpaceShadowIndex, ref int screenSpaceChannelSlot, ref LightData lightData)
        {
            var additionalLightData = processedData.additionalLightData;
            var gpuLightType = processedData.gpuLightType;
            var lightType = processedData.lightType;

            var visibleLightAxisAndPosition = light.GetAxisAndPosition();
            lightData.lightLayers = hdCamera.frameSettings.IsEnabled(FrameSettingsField.LightLayers) ? additionalLightData.GetLightLayers() : uint.MaxValue;

            lightData.lightType = gpuLightType;

            lightData.positionRWS = visibleLightAxisAndPosition.Position;

            lightData.range = light.range;

            if (additionalLightData.applyRangeAttenuation)
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

            lightData.forward = visibleLightAxisAndPosition.Forward;
            lightData.up = visibleLightAxisAndPosition.Up;
            lightData.right = visibleLightAxisAndPosition.Right;

            lightDimensions.x = additionalLightData.shapeWidth;
            lightDimensions.y = additionalLightData.shapeHeight;
            lightDimensions.z = light.range;

            lightData.boxLightSafeExtent = 1.0f;
            if (lightData.lightType == GPULightType.ProjectorBox)
            {
                // Rescale for cookies and windowing.
                lightData.right *= 2.0f / Mathf.Max(additionalLightData.shapeWidth, 0.001f);
                lightData.up    *= 2.0f / Mathf.Max(additionalLightData.shapeHeight, 0.001f);

                // If we have shadows, we need to shrink the valid range so that we don't leak light due to filtering going out of bounds.
                if (shadowIndex >= 0)
                {
                    // We subtract a bit from the safe extent depending on shadow resolution
                    float shadowRes = additionalLightData.shadowResolution.Value(m_ShadowInitParameters.shadowResolutionPunctual);
                    shadowRes = Mathf.Clamp(shadowRes, 128.0f, 2048.0f); // Clamp in a somewhat plausible range.
                    // The idea is to subtract as much as 0.05 for small resolutions.
                    float shadowResFactor = Mathf.Lerp(0.05f, 0.01f, Mathf.Max(shadowRes / 2048.0f, 0.0f));
                    lightData.boxLightSafeExtent = 1.0f - shadowResFactor;
                }
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
                lightData.angleScale  = 1.0f / val;
                lightData.angleOffset = -cosSpotOuterHalfAngle * lightData.angleScale;
                lightData.iesCut      = additionalLightData.spotIESCutoffPercent01;

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
                lightData.iesCut = 1.0f;
            }

            if (lightData.lightType != GPULightType.Directional && lightData.lightType != GPULightType.ProjectorBox)
            {
                // Store the squared radius of the light to simulate a fill light.
                lightData.size = new Vector4(additionalLightData.shapeRadius * additionalLightData.shapeRadius, 0, 0, 0);
            }

            if (lightData.lightType == GPULightType.Rectangle || lightData.lightType == GPULightType.Tube)
            {
                lightData.size = new Vector4(additionalLightData.shapeWidth, additionalLightData.shapeHeight, Mathf.Cos(additionalLightData.barnDoorAngle * Mathf.PI / 180.0f), additionalLightData.barnDoorLength);
            }

            lightData.lightDimmer           = processedData.lightDistanceFade * (additionalLightData.lightDimmer);
            lightData.diffuseDimmer         = processedData.lightDistanceFade * (additionalLightData.affectDiffuse  ? additionalLightData.lightDimmer : 0);
            lightData.specularDimmer        = processedData.lightDistanceFade * (additionalLightData.affectSpecular ? additionalLightData.lightDimmer * hdCamera.frameSettings.specularGlobalDimmer : 0);
            lightData.volumetricLightDimmer = Mathf.Min(processedData.volumetricDistanceFade, processedData.lightDistanceFade) * (additionalLightData.volumetricDimmer);

            lightData.cookieMode = CookieMode.None;
            lightData.shadowIndex = -1;
            lightData.screenSpaceShadowIndex = (int)LightDefinitions.s_InvalidScreenSpaceShadow;
            lightData.isRayTracedContactShadow = 0.0f;

            if (lightComponent != null && additionalLightData != null &&
                (
                    (lightType == HDLightType.Spot && (lightComponent.cookie != null || additionalLightData.IESPoint != null)) ||
                    ((lightType == HDLightType.Area && lightData.lightType == GPULightType.Rectangle) && (lightComponent.cookie != null || additionalLightData.IESSpot != null)) ||
                    (lightType == HDLightType.Point && (lightComponent.cookie != null || additionalLightData.IESPoint != null))
                ))
            {
                switch (lightType)
                {
                    case HDLightType.Spot:
                        lightData.cookieMode = (lightComponent.cookie?.wrapMode == TextureWrapMode.Repeat) ? CookieMode.Repeat : CookieMode.Clamp;
                        if (additionalLightData.IESSpot != null && lightComponent.cookie != null && additionalLightData.IESSpot != lightComponent.cookie)
                            lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.Fetch2DCookie(cmd, lightComponent.cookie, additionalLightData.IESSpot);
                        else if (lightComponent.cookie != null)
                            lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.Fetch2DCookie(cmd, lightComponent.cookie);
                        else if (additionalLightData.IESSpot != null)
                            lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.Fetch2DCookie(cmd, additionalLightData.IESSpot);
                        else
                            lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.Fetch2DCookie(cmd, Texture2D.whiteTexture);
                        break;
                    case HDLightType.Point:
                        lightData.cookieMode = CookieMode.Repeat;
                        if (additionalLightData.IESPoint != null && lightComponent.cookie != null && additionalLightData.IESPoint != lightComponent.cookie)
                            lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.FetchCubeCookie(cmd, lightComponent.cookie, additionalLightData.IESPoint);
                        else if (lightComponent.cookie != null)
                            lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.FetchCubeCookie(cmd, lightComponent.cookie);
                        else if (additionalLightData.IESPoint != null)
                            lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.FetchCubeCookie(cmd, additionalLightData.IESPoint);
                        break;
                    case HDLightType.Area:
                        lightData.cookieMode = CookieMode.Clamp;
                        if (additionalLightData.areaLightCookie != null && additionalLightData.IESSpot != null && additionalLightData.areaLightCookie != additionalLightData.IESSpot)
                            lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.FetchAreaCookie(cmd, additionalLightData.areaLightCookie, additionalLightData.IESSpot);
                        else if (additionalLightData.IESSpot != null)
                            lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.FetchAreaCookie(cmd, additionalLightData.IESSpot);
                        else if (additionalLightData.areaLightCookie != null)
                            lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.FetchAreaCookie(cmd, additionalLightData.areaLightCookie);
                        break;
                }
            }
            else if (lightType == HDLightType.Spot && additionalLightData.spotLightShape != SpotLightShape.Cone)
            {
                // Projectors lights must always have a cookie texture.
                // As long as the cache is a texture array and not an atlas, the 4x4 white texture will be rescaled to 128
                lightData.cookieMode = CookieMode.Clamp;
                lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.Fetch2DCookie(cmd, Texture2D.whiteTexture);
            }
            else if (lightData.lightType == GPULightType.Rectangle)
            {
                if (additionalLightData.areaLightCookie != null || additionalLightData.IESPoint != null)
                {
                    lightData.cookieMode = CookieMode.Clamp;
                    if (additionalLightData.areaLightCookie != null && additionalLightData.IESSpot != null && additionalLightData.areaLightCookie != additionalLightData.IESSpot)
                        lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.FetchAreaCookie(cmd, additionalLightData.areaLightCookie, additionalLightData.IESSpot);
                    else if (additionalLightData.IESSpot != null)
                        lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.FetchAreaCookie(cmd, additionalLightData.IESSpot);
                    else if (additionalLightData.areaLightCookie != null)
                        lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.FetchAreaCookie(cmd, additionalLightData.areaLightCookie);
                }
            }

            float shadowDistanceFade         = HDUtils.ComputeLinearDistanceFade(processedData.distanceToCamera, Mathf.Min(shadowSettings.maxShadowDistance.value, additionalLightData.shadowFadeDistance));
            lightData.shadowDimmer           = shadowDistanceFade * additionalLightData.shadowDimmer;
            lightData.volumetricShadowDimmer = shadowDistanceFade * additionalLightData.volumetricShadowDimmer;
            GetContactShadowMask(additionalLightData, contactShadowsScalableSetting, hdCamera, isRasterization: isRasterization, ref lightData.contactShadowMask, ref lightData.isRayTracedContactShadow);

            // We want to have a colored penumbra if the flag is on and the color is not gray
            bool penumbraTint = additionalLightData.penumbraTint && ((additionalLightData.shadowTint.r != additionalLightData.shadowTint.g) || (additionalLightData.shadowTint.g != additionalLightData.shadowTint.b));
            lightData.penumbraTint = penumbraTint ? 1.0f : 0.0f;
            if (penumbraTint)
                lightData.shadowTint = new Vector3(Mathf.Pow(additionalLightData.shadowTint.r, 2.2f), Mathf.Pow(additionalLightData.shadowTint.g, 2.2f), Mathf.Pow(additionalLightData.shadowTint.b, 2.2f));
            else
                lightData.shadowTint = new Vector3(additionalLightData.shadowTint.r, additionalLightData.shadowTint.g, additionalLightData.shadowTint.b);

            // If there is still a free slot in the screen space shadow array and this needs to render a screen space shadow
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing)
                && EnoughScreenSpaceShadowSlots(lightData.lightType, screenSpaceChannelSlot)
                && additionalLightData.WillRenderScreenSpaceShadow()
                && isRasterization)
            {
                if (lightData.lightType == GPULightType.Rectangle)
                {
                    // Rectangle area lights require 2 consecutive slots.
                    // Meaning if (screenSpaceChannelSlot % 4 ==3), we'll need to skip a slot
                    // so that the area shadow gets the first two slots of the next following texture
                    if (screenSpaceChannelSlot % 4 == 3)
                    {
                        screenSpaceChannelSlot++;
                    }
                }

                // Bind the next available slot to the light
                lightData.screenSpaceShadowIndex = screenSpaceChannelSlot;

                // Keep track of the screen space shadow data
                m_CurrentScreenSpaceShadowData[screenSpaceShadowIndex].additionalLightData = additionalLightData;
                m_CurrentScreenSpaceShadowData[screenSpaceShadowIndex].lightDataIndex = m_lightList.lights.Count;
                m_CurrentScreenSpaceShadowData[screenSpaceShadowIndex].valid = true;
                m_ScreenSpaceShadowsUnion.Add(additionalLightData);

                // increment the number of screen space shadows
                screenSpaceShadowIndex++;

                // Based on the light type, increment the slot usage
                if (lightData.lightType == GPULightType.Rectangle)
                    screenSpaceChannelSlot += 2;
                else
                    screenSpaceChannelSlot++;
            }

            lightData.shadowIndex = shadowIndex;

            if (isRasterization)
            {
                // Keep track of the shadow map (for indirect lighting and transparents)
                additionalLightData.shadowIndex = shadowIndex;
            }


            //Value of max smoothness is derived from Radius. Formula results from eyeballing. Radius of 0 results in 1 and radius of 2.5 results in 0.
            float maxSmoothness = Mathf.Clamp01(1.1725f / (1.01f + Mathf.Pow(1.0f * (additionalLightData.shapeRadius + 0.1f), 2f)) - 0.15f);
            // Value of max smoothness is from artists point of view, need to convert from perceptual smoothness to roughness
            lightData.minRoughness = (1.0f - maxSmoothness) * (1.0f - maxSmoothness);

            lightData.shadowMaskSelector = Vector4.zero;

            if (processedData.isBakedShadowMask)
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

            Vector3 xAxisVS = worldToView.MultiplyVector(lightToWorld.GetColumn(0));
            Vector3 yAxisVS = worldToView.MultiplyVector(lightToWorld.GetColumn(1));
            Vector3 zAxisVS = worldToView.MultiplyVector(lightToWorld.GetColumn(2));

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

                var sa = light.spotAngle;
                var cs = Mathf.Cos(0.5f * sa * Mathf.Deg2Rad);
                var si = Mathf.Sin(0.5f * sa * Mathf.Deg2Rad);

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
                var altDist   = Mathf.Sqrt(fAltDy * fAltDy + (true ? 1.0f : 2.0f) * fAltDx * fAltDx);
                bound.radius  = altDist > (0.5f * range) ? altDist : (0.5f * range);       // will always pick fAltDist
                bound.scaleXY = squeeze ? 0.01f : 1.0f;

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
                // Construct a view-space axis-aligned bounding cube around the bounding sphere.
                // This allows us to utilize the same polygon clipping technique for all lights.
                // Non-axis-aligned vectors may result in a larger screen-space AABB.
                Vector3 vx = new Vector3(1, 0, 0);
                Vector3 vy = new Vector3(0, 1, 0);
                Vector3 vz = new Vector3(0, 0, 1);

                bound.center = positionVS;
                bound.boxAxisX = vx * range;
                bound.boxAxisY = vy * range;
                bound.boxAxisZ = vz * range;
                bound.scaleXY  = 1.0f;
                bound.radius   = range;

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
                Vector3 extents    = 0.5f * dimensions;
                Vector3 centerVS   = positionVS;

                bound.center   = centerVS;
                bound.boxAxisX = extents.x * xAxisVS;
                bound.boxAxisY = extents.y * yAxisVS;
                bound.boxAxisZ = extents.z * zAxisVS;
                bound.radius   = extents.x;
                bound.scaleXY  = 1.0f;

                lightVolumeData.lightPos   = centerVS;
                lightVolumeData.lightAxisX = xAxisVS;
                lightVolumeData.lightAxisY = yAxisVS;
                lightVolumeData.lightAxisZ = zAxisVS;
                lightVolumeData.boxInvRange.Set(1.0f / extents.x, 1.0f / extents.y, 1.0f / extents.z);
                lightVolumeData.featureFlags = (uint)LightFeatureFlags.Area;
            }
            else if (gpuLightType == GPULightType.Rectangle)
            {
                Vector3 dimensions = new Vector3(lightDimensions.x + 2 * range, lightDimensions.y + 2 * range, range); // One-sided
                Vector3 extents    = 0.5f * dimensions;
                Vector3 centerVS   = positionVS + extents.z * zAxisVS;

                float d = range + 0.5f * Mathf.Sqrt(lightDimensions.x * lightDimensions.x + lightDimensions.y * lightDimensions.y);

                bound.center   = centerVS;
                bound.boxAxisX = extents.x * xAxisVS;
                bound.boxAxisY = extents.y * yAxisVS;
                bound.boxAxisZ = extents.z * zAxisVS;
                bound.radius   = Mathf.Sqrt(d * d + (0.5f * range) * (0.5f * range));
                bound.scaleXY  = 1.0f;

                lightVolumeData.lightPos   = centerVS;
                lightVolumeData.lightAxisX = xAxisVS;
                lightVolumeData.lightAxisY = yAxisVS;
                lightVolumeData.lightAxisZ = zAxisVS;
                lightVolumeData.boxInvRange.Set(1.0f / extents.x, 1.0f / extents.y, 1.0f / extents.z);
                lightVolumeData.featureFlags = (uint)LightFeatureFlags.Area;
            }
            else if (gpuLightType == GPULightType.ProjectorBox)
            {
                Vector3 dimensions = new Vector3(lightDimensions.x, lightDimensions.y, range);  // One-sided
                Vector3 extents    = 0.5f * dimensions;
                Vector3 centerVS   = positionVS + extents.z * zAxisVS;

                bound.center   = centerVS;
                bound.boxAxisX = extents.x * xAxisVS;
                bound.boxAxisY = extents.y * yAxisVS;
                bound.boxAxisZ = extents.z * zAxisVS;
                bound.radius   = extents.magnitude;
                bound.scaleXY  = 1.0f;

                lightVolumeData.lightPos   = centerVS;
                lightVolumeData.lightAxisX = xAxisVS;
                lightVolumeData.lightAxisY = yAxisVS;
                lightVolumeData.lightAxisZ = zAxisVS;
                lightVolumeData.boxInvRange.Set(1.0f / extents.x, 1.0f / extents.y, 1.0f / extents.z);
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

        internal bool GetEnvLightData(CommandBuffer cmd, HDCamera hdCamera, in ProcessedProbeData processedProbe, ref EnvLightData envLightData)
        {
            // By default, rough reflections are enabled for both types of probes.
            envLightData.roughReflections = 1.0f;
            envLightData.distanceBasedRoughness = 0.0f;

            Camera camera = hdCamera.camera;
            HDProbe probe = processedProbe.hdProbe;

            // Skip the probe if the probe has never rendered (in realtime cases) or if texture is null
            if (!probe.HasValidRenderedData()) return false;

            var capturePosition = Vector3.zero;
            var influenceToWorld = probe.influenceToWorld;
            Vector4 atlasScaleOffset = Vector4.zero;

            // 31 bits index, 1 bit cache type
            var envIndex = int.MinValue;
            switch (probe)
            {
                case PlanarReflectionProbe planarProbe:
                    {
                        if (probe.mode == ProbeSettings.Mode.Realtime
                            && !hdCamera.frameSettings.IsEnabled(FrameSettingsField.PlanarProbe))
                            break;

                        // Grab the render data that was used to render the probe
                        var renderData = planarProbe.renderData;
                        // Grab the world to camera matrix of the capture camera
                        var worldToCameraRHSMatrix = renderData.worldToCameraRHS;
                        // Grab the projection matrix that was used to render
                        var projectionMatrix = renderData.projectionMatrix;
                        // Build an alternative matrix for projection that is not oblique
                        var projectionMatrixNonOblique = Matrix4x4.Perspective(renderData.fieldOfView, probe.texture.width / probe.texture.height, probe.settings.cameraSettings.frustum.nearClipPlaneRaw, probe.settings.cameraSettings.frustum.farClipPlane);

                        // Convert the projection matrices to their GPU version
                        var gpuProj = GL.GetGPUProjectionMatrix(projectionMatrix, true);
                        var gpuProjNonOblique = GL.GetGPUProjectionMatrix(projectionMatrixNonOblique, true);

                        // Build the oblique and non oblique view projection matrices
                        var vp = gpuProj * worldToCameraRHSMatrix;
                        var vpNonOblique = gpuProjNonOblique * worldToCameraRHSMatrix;

                        // We need to collect the set of parameters required for the filtering
                        IBLFilterBSDF.PlanarTextureFilteringParameters planarTextureFilteringParameters = new IBLFilterBSDF.PlanarTextureFilteringParameters();
                        planarTextureFilteringParameters.smoothPlanarReflection = !probe.settings.roughReflections;
                        planarTextureFilteringParameters.probeNormal = Vector3.Normalize(hdCamera.camera.transform.position - renderData.capturePosition);
                        planarTextureFilteringParameters.probePosition = probe.gameObject.transform.position;
                        planarTextureFilteringParameters.captureCameraDepthBuffer = planarProbe.realtimeDepthTexture;
                        planarTextureFilteringParameters.captureCameraScreenSize = new Vector4(probe.texture.width, probe.texture.height, 1.0f / probe.texture.width, 1.0f / probe.texture.height);
                        planarTextureFilteringParameters.captureCameraIVP = vp.inverse;
                        planarTextureFilteringParameters.captureCameraIVP_NonOblique = vpNonOblique.inverse;
                        planarTextureFilteringParameters.captureCameraVP_NonOblique = vpNonOblique;
                        planarTextureFilteringParameters.captureCameraPosition = renderData.capturePosition;
                        planarTextureFilteringParameters.captureFOV = renderData.fieldOfView;
                        planarTextureFilteringParameters.captureNearPlane = probe.settings.cameraSettings.frustum.nearClipPlaneRaw;
                        planarTextureFilteringParameters.captureFarPlane = probe.settings.cameraSettings.frustum.farClipPlane;

                        // Fetch the slice and do the filtering
                        var scaleOffset = m_TextureCaches.reflectionPlanarProbeCache.FetchSlice(cmd, probe.texture, ref planarTextureFilteringParameters, out int fetchIndex);

                        // We don't need to provide the capture position
                        // It is already encoded in the 'worldToCameraRHSMatrix'
                        capturePosition = Vector3.zero;

                        // Indices start at 1, because -0 == 0, we can know from the bit sign which cache to use
                        envIndex = scaleOffset == Vector4.zero ? int.MinValue : -(fetchIndex + 1);

                        // If the max number of planar on screen is reached
                        if (fetchIndex >= m_MaxPlanarReflectionOnScreen)
                        {
                            Debug.LogWarning("Maximum planar reflection probe on screen reached. To fix this error, increase the maximum number of planar reflections on screen in the HDRP asset.");
                            break;
                        }

                        atlasScaleOffset = scaleOffset;

                        m_TextureCaches.env2DAtlasScaleOffset[fetchIndex] = scaleOffset;
                        m_TextureCaches.env2DCaptureVP[fetchIndex] = vp;

                        // Propagate the smoothness information to the env light data
                        envLightData.roughReflections = probe.settings.roughReflections ? 1.0f : 0.0f;

                        var capturedForwardWS = renderData.captureRotation * Vector3.forward;
                        //capturedForwardWS.z *= -1; // Transform to RHS standard
                        m_TextureCaches.env2DCaptureForward[fetchIndex] = new Vector4(capturedForwardWS.x, capturedForwardWS.y, capturedForwardWS.z, 0.0f);
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

                    // Propagate the distance based information to the env light data (only if we are not an infinite projection)
                    envLightData.distanceBasedRoughness = probe.settings.distanceBasedRoughness && !probe.isProjectionInfinite ? 1.0f : 0.0f;

                    break;
                }
            }
            // int.MinValue means that the texture is not ready yet (ie not convolved/compressed yet)
            if (envIndex == int.MinValue)
                return false;

            InfluenceVolume influence = probe.influenceVolume;
            envLightData.lightLayers = hdCamera.frameSettings.IsEnabled(FrameSettingsField.LightLayers) ? probe.lightLayersAsUInt : uint.MaxValue;
            envLightData.influenceShapeType = influence.envShape;
            envLightData.weight = processedProbe.weight;
            envLightData.multiplier = probe.multiplier * m_indirectLightingController.reflectionProbeIntensityMultiplier.value;
            envLightData.rangeCompressionFactorCompensation = Mathf.Max(probe.rangeCompressionFactor, 1e-6f);
            envLightData.influenceExtents = influence.extents;
            switch (influence.envShape)
            {
                case EnvShapeType.Box:
                    envLightData.blendDistancePositive = influence.boxBlendDistancePositive;
                    envLightData.blendDistanceNegative = influence.boxBlendDistanceNegative;
                    if (envIndex >= 0) // Reflection Probes
                    {
                        envLightData.blendNormalDistancePositive = influence.boxBlendNormalDistancePositive;
                        envLightData.blendNormalDistanceNegative = influence.boxBlendNormalDistanceNegative;
                        envLightData.boxSideFadePositive = influence.boxSideFadePositive;
                        envLightData.boxSideFadeNegative = influence.boxSideFadeNegative;
                    }
                    else // Planar Probes
                    {
                        envLightData.blendNormalDistancePositive = Vector3.zero;
                        envLightData.blendNormalDistanceNegative = Vector3.zero;
                        envLightData.boxSideFadePositive = Vector3.one;
                        envLightData.boxSideFadeNegative = Vector3.one;
                    }
                    break;
                case EnvShapeType.Sphere:
                    envLightData.blendDistancePositive.x = influence.sphereBlendDistance;
                    if (envIndex >= 0) // Reflection Probes
                        envLightData.blendNormalDistancePositive.x = influence.sphereBlendNormalDistance;
                    else // Planar Probes
                        envLightData.blendNormalDistancePositive.x = 0;
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
                    bound.scaleXY  = 1.0f;
                    bound.radius   = influenceExtents.x;
                    break;
                }
                case LightVolumeType.Box:
                {
                    bound.center = influencePositionVS;
                    bound.boxAxisX = influenceExtents.x * influenceRightVS;
                    bound.boxAxisY = influenceExtents.y * influenceUpVS;
                    bound.boxAxisZ = influenceExtents.z * influenceForwardVS;
                    bound.scaleXY  = 1.0f;
                    bound.radius   = influenceExtents.magnitude;

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

        void CreateBoxVolumeDataAndBound(OrientedBBox obb, LightCategory category, LightFeatureFlags featureFlags, Matrix4x4 worldToView, float normalBiasDilation, out LightVolumeData volumeData, out SFiniteLightBound bound)
        {
            volumeData = new LightVolumeData();
            bound = new SFiniteLightBound();

            // Used in Probe Volumes:
            // Conservatively dilate bounds used for tile / cluster assignment by normal bias.
            // Otherwise, surfaces could bias outside of valid data within a tile.
            var extentConservativeX = obb.extentX + normalBiasDilation;
            var extentConservativeY = obb.extentY + normalBiasDilation;
            var extentConservativeZ = obb.extentZ + normalBiasDilation;
            var extentConservativeMagnitude = Mathf.Sqrt(extentConservativeX * extentConservativeX + extentConservativeY * extentConservativeY + extentConservativeZ * extentConservativeZ);

            // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
            var positionVS = worldToView.MultiplyPoint(obb.center);
            var rightVS    = worldToView.MultiplyVector(obb.right);
            var upVS       = worldToView.MultiplyVector(obb.up);
            var forwardVS  = Vector3.Cross(upVS, rightVS);
            var extents    = new Vector3(extentConservativeX, extentConservativeY, extentConservativeZ);

            volumeData.lightVolume   = (uint)LightVolumeType.Box;
            volumeData.lightCategory = (uint)category;
            volumeData.featureFlags  = (uint)featureFlags;

            bound.center   = positionVS;
            bound.boxAxisX = extentConservativeX * rightVS;
            bound.boxAxisY = extentConservativeY * upVS;
            bound.boxAxisZ = extentConservativeZ * forwardVS;
            bound.radius   = extentConservativeMagnitude;
            bound.scaleXY  = 1.0f;

            // The culling system culls pixels that are further
            //   than a threshold to the box influence extents.
            // So we use an arbitrary threshold here (k_BoxCullingExtentOffset)
            volumeData.lightPos     = positionVS;
            volumeData.lightAxisX   = rightVS;
            volumeData.lightAxisY   = upVS;
            volumeData.lightAxisZ   = forwardVS;
            volumeData.boxInnerDist = extents - k_BoxCullingExtentThreshold; // We have no blend range, but the culling code needs a small EPS value for some reason???
            volumeData.boxInvRange.Set(1.0f / k_BoxCullingExtentThreshold.x, 1.0f / k_BoxCullingExtentThreshold.y, 1.0f / k_BoxCullingExtentThreshold.z);
        }

        internal int GetCurrentShadowCount()
        {
            return m_ShadowManager.GetShadowRequestCount();
        }

        void LightLoopUpdateCullingParameters(ref ScriptableCullingParameters cullingParams, HDCamera hdCamera)
        {
            var shadowMaxDistance = hdCamera.volumeStack.GetComponent<HDShadowSettings>().maxShadowDistance.value;
            m_ShadowManager.UpdateCullingParameters(ref cullingParams, shadowMaxDistance);

            // In HDRP we don't need per object light/probe info so we disable the native code that handles it.
            cullingParams.cullingOptions |= CullingOptions.DisablePerObjectCulling;
        }

        internal static bool IsBakedShadowMaskLight(Light light)
        {
            // This can happen for particle lights.
            if (light == null)
                return false;

            return light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed &&
                light.bakingOutput.mixedLightingMode == MixedLightingMode.Shadowmask &&
                light.bakingOutput.occlusionMaskChannel != -1;     // We need to have an occlusion mask channel assign, else we have no shadow mask
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

        bool TrivialRejectLight(VisibleLight light, HDCamera hdCamera, in AOVRequestData aovRequest)
        {
            // We can skip the processing of lights that are so small to not affect at least a pixel on screen.
            // TODO: The minimum pixel size on screen should really be exposed as parameter, to allow small lights to be culled to user's taste.
            const int minimumPixelAreaOnScreen = 1;
            if ((light.screenRect.height * hdCamera.actualHeight) * (light.screenRect.width * hdCamera.actualWidth) < minimumPixelAreaOnScreen)
            {
                return true;
            }

            if (light.light != null && !aovRequest.IsLightEnabled(light.light.gameObject))
                return true;

            return false;
        }

        // Compute data that will be used during the light loop for a particular light.
        void PreprocessLightData(ref ProcessedLightData processedData, VisibleLight light, HDCamera hdCamera)
        {
            Light lightComponent = light.light;
            HDAdditionalLightData additionalLightData = GetHDAdditionalLightData(lightComponent);

            processedData.additionalLightData = additionalLightData;
            processedData.lightType = additionalLightData.ComputeLightType(lightComponent);
            processedData.distanceToCamera = (additionalLightData.transform.position - hdCamera.camera.transform.position).magnitude;

            // Evaluate the types that define the current light
            processedData.lightCategory = LightCategory.Count;
            processedData.gpuLightType = GPULightType.Point;
            processedData.lightVolumeType = LightVolumeType.Count;
            HDRenderPipeline.EvaluateGPULightType(processedData.lightType, processedData.additionalLightData.spotLightShape, processedData.additionalLightData.areaLightShape,
                                                    ref processedData.lightCategory, ref processedData.gpuLightType, ref processedData.lightVolumeType);

            processedData.lightDistanceFade = processedData.gpuLightType == GPULightType.Directional ? 1.0f : HDUtils.ComputeLinearDistanceFade(processedData.distanceToCamera, additionalLightData.fadeDistance);
            processedData.volumetricDistanceFade = processedData.gpuLightType == GPULightType.Directional ? 1.0f : HDUtils.ComputeLinearDistanceFade(processedData.distanceToCamera, additionalLightData.volumetricFadeDistance);
            processedData.isBakedShadowMask = IsBakedShadowMaskLight(lightComponent);
        }

        // This will go through the list of all visible light and do two main things:
        // - Precompute data that will be reused through the light loop
        // - Discard all lights considered unnecessary (too far away, explicitly discarded by type, ...)
        int PreprocessVisibleLights(HDCamera hdCamera, CullingResults cullResults, DebugDisplaySettings debugDisplaySettings, in AOVRequestData aovRequest)
        {
            var hdShadowSettings = hdCamera.volumeStack.GetComponent<HDShadowSettings>();

            var debugLightFilter = debugDisplaySettings.GetDebugLightFilterMode();
            var hasDebugLightFilter = debugLightFilter != DebugLightFilterMode.None;

            // 1. Count the number of lights and sort all lights by category, type and volume - This is required for the fptl/cluster shader code
            // If we reach maximum of lights available on screen, then we discard the light.
            // Lights are processed in order, so we don't discards light based on their importance but based on their ordering in visible lights list.
            int directionalLightcount = 0;
            int punctualLightcount = 0;
            int areaLightCount = 0;

            m_ProcessedLightData.Resize(cullResults.visibleLights.Length);

            int lightCount = Math.Min(cullResults.visibleLights.Length, m_MaxLightsOnScreen);
            UpdateSortKeysArray(lightCount);
            int sortCount = 0;
            for (int lightIndex = 0, numLights = cullResults.visibleLights.Length; (lightIndex < numLights) && (sortCount < lightCount); ++lightIndex)
            {
                var light = cullResults.visibleLights[lightIndex];

                // First we do all the trivial rejects.
                if (TrivialRejectLight(light, hdCamera, aovRequest))
                    continue;

                // Then we compute all light data that will be reused for the rest of the light loop.
                ref ProcessedLightData processedData = ref m_ProcessedLightData[lightIndex];
                PreprocessLightData(ref processedData, light, hdCamera);

                // Then we can reject lights based on processed data.
                var additionalData = processedData.additionalLightData;

                // If the camera is in ray tracing mode and the light is disabled in ray tracing mode, we skip this light.
                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && !additionalData.includeForRayTracing)
                    continue;

                var lightType = processedData.lightType;
                if (ShaderConfig.s_AreaLights == 0 && (lightType == HDLightType.Area && (additionalData.areaLightShape == AreaLightShape.Rectangle || additionalData.areaLightShape == AreaLightShape.Tube)))
                    continue;

                bool contributesToLighting = ((additionalData.lightDimmer > 0) && (additionalData.affectDiffuse || additionalData.affectSpecular)) || (additionalData.volumetricDimmer > 0);
                contributesToLighting = contributesToLighting && (processedData.lightDistanceFade > 0);

                if (!contributesToLighting)
                    continue;

                // Do NOT process lights beyond the specified limit!
                switch (processedData.lightCategory)
                {
                    case LightCategory.Punctual:
                        if (processedData.gpuLightType == GPULightType.Directional) // Our directional lights are "punctual"...
                        {
                            if (!debugDisplaySettings.data.lightingDebugSettings.showDirectionalLight || directionalLightcount >= m_MaxDirectionalLightsOnScreen) continue;
                            directionalLightcount++;
                            break;
                        }
                        if (!debugDisplaySettings.data.lightingDebugSettings.showPunctualLight || punctualLightcount >= m_MaxPunctualLightsOnScreen) continue;
                        punctualLightcount++;
                        break;
                    case LightCategory.Area:
                        if (!debugDisplaySettings.data.lightingDebugSettings.showAreaLight || areaLightCount >= m_MaxAreaLightsOnScreen) continue;
                        areaLightCount++;
                        break;
                    default:
                        break;
                }

                // First we should evaluate the shadow information for this frame
                additionalData.EvaluateShadowState(hdCamera, processedData, cullResults, hdCamera.frameSettings, lightIndex);

                // Reserve shadow map resolutions and check if light needs to render shadows
                if (additionalData.WillRenderShadowMap())
                {
                    additionalData.ReserveShadowMap(hdCamera.camera, m_ShadowManager, hdShadowSettings, m_ShadowInitParameters, light, lightType);
                }

                // Reserve the cookie resolution in the 2D atlas
                ReserveCookieAtlasTexture(additionalData, light.light, lightType);

                if (hasDebugLightFilter
                    && !debugLightFilter.IsEnabledFor(processedData.gpuLightType, additionalData.spotLightShape))
                    continue;

                // 5 bit (0x1F) light category, 5 bit (0x1F) GPULightType, 5 bit (0x1F) lightVolume, 1 bit for shadow casting, 16 bit index
                m_SortKeys[sortCount++] = (uint)processedData.lightCategory << 27 | (uint)processedData.gpuLightType << 22 | (uint)processedData.lightVolumeType << 17 | (uint)lightIndex;
            }

            CoreUnsafeUtils.QuickSort(m_SortKeys, 0, sortCount - 1); // Call our own quicksort instead of Array.Sort(sortKeys, 0, sortCount) so we don't allocate memory (note the SortCount-1 that is different from original call).
            return sortCount;
        }

        void PrepareGPULightdata(CommandBuffer cmd, HDCamera hdCamera, CullingResults cullResults, int processedLightCount)
        {
            Vector3 camPosWS = hdCamera.mainViewConstants.worldSpaceCameraPos;

            int directionalLightcount = 0;
            int punctualLightcount = 0;
            int areaLightCount = 0;

            // Now that all the lights have requested a shadow resolution, we can layout them in the atlas
            // And if needed rescale the whole atlas
            m_ShadowManager.LayoutShadowMaps(m_CurrentDebugDisplaySettings.data.lightingDebugSettings);

            // Using the same pattern than shadowmaps, light have requested space in the atlas for their
            // cookies and now we can layout the atlas (re-insert all entries by order of size) if needed
            m_TextureCaches.lightCookieManager.LayoutIfNeeded();

            var visualEnvironment = hdCamera.volumeStack.GetComponent<VisualEnvironment>();
            Debug.Assert(visualEnvironment != null);

            bool isPbrSkyActive = visualEnvironment.skyType.value == (int)SkyType.PhysicallyBased;

            var hdShadowSettings = hdCamera.volumeStack.GetComponent<HDShadowSettings>();

            // TODO: Refactor shadow management
            // The good way of managing shadow:
            // Here we sort everyone and we decide which light is important or not (this is the responsibility of the lightloop)
            // we allocate shadow slot based on maximum shadow allowed on screen and attribute slot by bigger solid angle
            // THEN we ask to the ShadowRender to render the shadow, not the reverse as it is today (i.e render shadow than expect they
            // will be use...)
            // The lightLoop is in charge, not the shadow pass.
            // For now we will still apply the maximum of shadow here but we don't apply the sorting by priority + slot allocation yet

            BoolScalableSetting contactShadowScalableSetting = HDAdditionalLightData.ScalableSettings.UseContactShadow(m_Asset);
            var shadowFilteringQuality = HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.shadowFilteringQuality;

            // 2. Go through all lights, convert them to GPU format.
            // Simultaneously create data for culling (LightVolumeData and SFiniteLightBound)

            for (int sortIndex = 0; sortIndex < processedLightCount; ++sortIndex)
            {
                // In 1. we have already classify and sorted the light, we need to use this sorted order here
                uint sortKey = m_SortKeys[sortIndex];
                LightCategory lightCategory = (LightCategory)((sortKey >> 27) & 0x1F);
                GPULightType gpuLightType = (GPULightType)((sortKey >> 22) & 0x1F);
                LightVolumeType lightVolumeType = (LightVolumeType)((sortKey >> 17) & 0x1F);
                int lightIndex = (int)(sortKey & 0xFFFF);

                var light = cullResults.visibleLights[lightIndex];
                var lightComponent = light.light;
                ProcessedLightData processedData = m_ProcessedLightData[lightIndex];

                m_EnableBakeShadowMask = m_EnableBakeShadowMask || processedData.isBakedShadowMask;

                // Light should always have additional data, however preview light right don't have, so we must handle the case by assigning HDUtils.s_DefaultHDAdditionalLightData
                var additionalLightData = processedData.additionalLightData;

                int shadowIndex = -1;

                // Manage shadow requests
                if (additionalLightData.WillRenderShadowMap())
                {
                    int shadowRequestCount;
                    shadowIndex = additionalLightData.UpdateShadowRequest(hdCamera, m_ShadowManager, hdShadowSettings, light, cullResults, lightIndex, m_CurrentDebugDisplaySettings.data.lightingDebugSettings, shadowFilteringQuality, out shadowRequestCount);

#if UNITY_EDITOR
                    if ((m_CurrentDebugDisplaySettings.data.lightingDebugSettings.shadowDebugUseSelection
                            || m_CurrentDebugDisplaySettings.data.lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.SingleShadow)
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
                    GetDirectionalLightData(cmd, hdCamera, light, lightComponent, lightIndex, shadowIndex, directionalLightcount, isPbrSkyActive, ref m_ScreenSpaceShadowIndex, ref m_ScreenSpaceShadowChannelSlot);

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
                else
                {
                    Vector3 lightDimensions = new Vector3(); // X = length or width, Y = height, Z = range (depth)

                    // Allocate a light data
                    LightData lightData = new LightData();

                    // Punctual, area, projector lights - the rendering side.
                    GetLightData(cmd, hdCamera, hdShadowSettings, light, lightComponent, in m_ProcessedLightData[lightIndex], shadowIndex, contactShadowScalableSetting, isRasterization: true, ref lightDimensions, ref m_ScreenSpaceShadowIndex, ref m_ScreenSpaceShadowChannelSlot, ref lightData);

                    // Add the previously created light data
                    m_lightList.lights.Add(lightData);

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
                        GetLightVolumeDataAndBound(lightCategory, gpuLightType, lightVolumeType, light, m_lightList.lights[m_lightList.lights.Count - 1], lightDimensions, m_WorldToViewMatrices[viewIndex], viewIndex);
                    }

                    // We make the light position camera-relative as late as possible in order
                    // to allow the preceding code to work with the absolute world space coordinates.
                    if (ShaderConfig.s_CameraRelativeRendering != 0)
                    {
                        // Caution: 'LightData.positionWS' is camera-relative after this point.
                        int last = m_lightList.lights.Count - 1;
                        lightData = m_lightList.lights[last];
                        lightData.positionRWS -= camPosWS;
                        m_lightList.lights[last] = lightData;
                    }
                }
            }

            // Sanity check
            Debug.Assert(m_lightList.directionalLights.Count == directionalLightcount);
            Debug.Assert(m_lightList.lights.Count == areaLightCount + punctualLightcount);

            m_lightList.punctualLightCount = punctualLightcount;
            m_lightList.areaLightCount = areaLightCount;
        }

        bool TrivialRejectProbe(in ProcessedProbeData processedProbe, HDCamera hdCamera)
        {
            // For now we won't display real time probe when rendering one.
            // TODO: We may want to display last frame result but in this case we need to be careful not to update the atlas before all realtime probes are rendered (for frame coherency).
            // Unfortunately we don't have this information at the moment.
            if (processedProbe.hdProbe.mode == ProbeSettings.Mode.Realtime && hdCamera.camera.cameraType == CameraType.Reflection)
                return true;

            // Discard probe if disabled in debug menu
            if (!m_CurrentDebugDisplaySettings.data.lightingDebugSettings.showReflectionProbe)
                return true;

            // Discard probe if its distance is too far or if its weight is at 0
            if (processedProbe.weight <= 0f)
                return true;

            // Exclude env lights based on hdCamera.probeLayerMask
            if ((hdCamera.probeLayerMask.value & (1 << processedProbe.hdProbe.gameObject.layer)) == 0)
                return true;

            // probe.texture can be null when we are adding a reflection probe in the editor
            if (processedProbe.hdProbe.texture == null)
                return true;

            return false;
        }

        internal static void PreprocessReflectionProbeData(ref ProcessedProbeData processedData, VisibleReflectionProbe probe, HDCamera hdCamera)
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

            PreprocessProbeData(ref processedData, add, hdCamera);
        }

        internal static void PreprocessProbeData(ref ProcessedProbeData processedData, HDProbe probe, HDCamera hdCamera)
        {
            processedData.hdProbe = probe;
            processedData.weight = HDUtils.ComputeWeightedLinearFadeDistance(processedData.hdProbe.transform.position, hdCamera.camera.transform.position, processedData.hdProbe.weight, processedData.hdProbe.fadeDistance);
        }

        int PreprocessVisibleProbes(HDCamera hdCamera, CullingResults cullResults, HDProbeCullingResults hdProbeCullingResults, in AOVRequestData aovRequest)
        {
            var debugLightFilter = m_CurrentDebugDisplaySettings.GetDebugLightFilterMode();
            var hasDebugLightFilter = debugLightFilter != DebugLightFilterMode.None;

            // Redo everything but this time with envLights
            Debug.Assert(m_MaxEnvLightsOnScreen <= 256); //for key construction
            int envLightCount = 0;

            var totalProbes = cullResults.visibleReflectionProbes.Length + hdProbeCullingResults.visibleProbes.Count;

            m_ProcessedReflectionProbeData.Resize(cullResults.visibleReflectionProbes.Length);
            m_ProcessedPlanarProbeData.Resize(hdProbeCullingResults.visibleProbes.Count);

            int maxProbeCount = Math.Min(totalProbes, m_MaxEnvLightsOnScreen);
            UpdateSortKeysArray(maxProbeCount);

            var enableReflectionProbes = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ReflectionProbe) &&
                                            (!hasDebugLightFilter || debugLightFilter.IsEnabledFor(ProbeSettings.ProbeType.ReflectionProbe));

            var enablePlanarProbes = hdCamera.frameSettings.IsEnabled(FrameSettingsField.PlanarProbe) &&
                                        (!hasDebugLightFilter || debugLightFilter.IsEnabledFor(ProbeSettings.ProbeType.PlanarProbe));

            if (enableReflectionProbes)
            {
                for (int probeIndex = 0; probeIndex < cullResults.visibleReflectionProbes.Length; probeIndex++)
                {
                    var probe = cullResults.visibleReflectionProbes[probeIndex];

                    if (probe.reflectionProbe == null
                        || probe.reflectionProbe.Equals(null) || !probe.reflectionProbe.isActiveAndEnabled
                        || !aovRequest.IsLightEnabled(probe.reflectionProbe.gameObject))
                        continue;

                    ref ProcessedProbeData processedData = ref m_ProcessedReflectionProbeData[probeIndex];
                    PreprocessReflectionProbeData(ref processedData, probe, hdCamera);

                    if (TrivialRejectProbe(processedData, hdCamera))
                        continue;

                    // Work around the data issues.
                    if (probe.localToWorldMatrix.determinant == 0)
                    {
                        Debug.LogError("Reflection probe " + probe.reflectionProbe.name + " has an invalid local frame and needs to be fixed.");
                        continue;
                    }

                    // This test needs to be the last one otherwise we may consume an available slot and then discard the probe.
                    if (envLightCount >= maxProbeCount)
                        continue;

                    LightVolumeType lightVolumeType = LightVolumeType.Box;
                    if (processedData.hdProbe != null && processedData.hdProbe.influenceVolume.shape == InfluenceShape.Sphere)
                        lightVolumeType = LightVolumeType.Sphere;

                    var logVolume = CalculateProbeLogVolume(probe.bounds);

                    m_SortKeys[envLightCount++] = PackProbeKey(logVolume, lightVolumeType, 0u, probeIndex); // Sort by volume
                }
            }

            if (enablePlanarProbes)
            {
                for (int planarProbeIndex = 0; planarProbeIndex < hdProbeCullingResults.visibleProbes.Count; planarProbeIndex++)
                {
                    var probe = hdProbeCullingResults.visibleProbes[planarProbeIndex];

                    ref ProcessedProbeData processedData = ref m_ProcessedPlanarProbeData[planarProbeIndex];
                    PreprocessProbeData(ref processedData, probe, hdCamera);

                    if (!aovRequest.IsLightEnabled(probe.gameObject))
                        continue;

                    // This test needs to be the last one otherwise we may consume an available slot and then discard the probe.
                    if (envLightCount >= maxProbeCount)
                        continue;

                    var lightVolumeType = LightVolumeType.Box;
                    if (probe.influenceVolume.shape == InfluenceShape.Sphere)
                        lightVolumeType = LightVolumeType.Sphere;

                    var logVolume = CalculateProbeLogVolume(probe.bounds);

                    m_SortKeys[envLightCount++] = PackProbeKey(logVolume, lightVolumeType, 1u, planarProbeIndex); // Sort by volume
                }
            }

            // Not necessary yet but call it for future modification with sphere influence volume
            CoreUnsafeUtils.QuickSort(m_SortKeys, 0, envLightCount - 1); // Call our own quicksort instead of Array.Sort(sortKeys, 0, sortCount) so we don't allocate memory (note the SortCount-1 that is different from original call).
            return envLightCount;
        }

        void PrepareGPUProbeData(CommandBuffer cmd, HDCamera hdCamera, CullingResults cullResults, HDProbeCullingResults hdProbeCullingResults, int processedLightCount)
        {
            Vector3 camPosWS = hdCamera.mainViewConstants.worldSpaceCameraPos;

            for (int sortIndex = 0; sortIndex < processedLightCount; ++sortIndex)
            {
                // In 1. we have already classify and sorted the light, we need to use this sorted order here
                uint sortKey = m_SortKeys[sortIndex];
                LightVolumeType lightVolumeType;
                int probeIndex;
                int listType;
                UnpackProbeSortKey(sortKey, out lightVolumeType, out probeIndex, out listType);

                ProcessedProbeData processedProbe = (listType == 0) ? m_ProcessedReflectionProbeData[probeIndex] : m_ProcessedPlanarProbeData[probeIndex];

                EnvLightData envLightData = new EnvLightData();

                if (GetEnvLightData(cmd, hdCamera, processedProbe, ref envLightData))
                {
                    // it has been filled
                    m_lightList.envLights.Add(envLightData);

                    for (int viewIndex = 0; viewIndex < hdCamera.viewCount; ++viewIndex)
                    {
                        var worldToView = GetWorldToViewMatrix(hdCamera, viewIndex);
                        GetEnvLightVolumeDataAndBound(processedProbe.hdProbe, lightVolumeType, worldToView, viewIndex);
                    }

                    // We make the light position camera-relative as late as possible in order
                    // to allow the preceding code to work with the absolute world space coordinates.
                    UpdateEnvLighCameraRelativetData(ref envLightData, camPosWS);

                    int last = m_lightList.envLights.Count - 1;
                    m_lightList.envLights[last] = envLightData;
                }
            }
        }

        // Return true if BakedShadowMask are enabled
        bool PrepareLightsForGPU(CommandBuffer cmd, HDCamera hdCamera, CullingResults cullResults,
        HDProbeCullingResults hdProbeCullingResults, DensityVolumeList densityVolumes, ProbeVolumeList probeVolumes, DebugDisplaySettings debugDisplaySettings, AOVRequestData aovRequest)
        {
            var debugLightFilter = debugDisplaySettings.GetDebugLightFilterMode();
            var hasDebugLightFilter = debugLightFilter != DebugLightFilterMode.None;

            HDShadowManager.cachedShadowManager.AssignSlotsInAtlases();

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.PrepareLightsForGPU)))
            {
                Camera camera = hdCamera.camera;

                // If any light require it, we need to enabled bake shadow mask feature
                m_EnableBakeShadowMask = false;

                m_lightList.Clear();

                // We need to properly reset this here otherwise if we go from 1 light to no visible light we would keep the old reference active.
                m_CurrentSunLight = null;
                m_CurrentSunLightAdditionalLightData = null;
                m_CurrentShadowSortedSunLightIndex = -1;
                m_DebugSelectedLightShadowIndex = -1;
                m_DebugSelectedLightShadowCount = 0;

                int decalDatasCount = Math.Min(DecalSystem.m_DecalDatasCount, m_MaxDecalsOnScreen);

                // We must clear the shadow requests before checking if they are any visible light because we would have requests from the last frame executed in the case where we don't see any lights
                m_ShadowManager.Clear();

                // Because we don't support baking planar reflection probe, we can clear the atlas.
                // Every visible probe will be blitted again.
                m_TextureCaches.reflectionPlanarProbeCache.ClearAtlasAllocator();

                m_ScreenSpaceShadowIndex = 0;
                m_ScreenSpaceShadowChannelSlot = 0;
                // Set all the light data to invalid
                for (int i = 0; i < m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots; ++i)
                {
                    m_CurrentScreenSpaceShadowData[i].additionalLightData = null;
                    m_CurrentScreenSpaceShadowData[i].lightDataIndex = -1;
                    m_CurrentScreenSpaceShadowData[i].valid = false;
                }

                // Note: Light with null intensity/Color are culled by the C++, no need to test it here
                if (cullResults.visibleLights.Length != 0)
                {
                    int processedLightCount = PreprocessVisibleLights(hdCamera, cullResults, debugDisplaySettings, aovRequest);

                    // In case ray tracing supported and a light cluster is built, we need to make sure to reserve all the cookie slots we need
                    if (m_RayTracingSupported)
                        ReserveRayTracingCookieAtlasSlots();

                    PrepareGPULightdata(cmd, hdCamera, cullResults, processedLightCount);

                    // Update the compute buffer with the shadow request datas
                    m_ShadowManager.PrepareGPUShadowDatas(cullResults, hdCamera);
                }
                else if (m_RayTracingSupported)
                {
                    // In case there is no rasterization lights, we stil need to do it for ray tracing
                    ReserveRayTracingCookieAtlasSlots();
                    m_TextureCaches.lightCookieManager.LayoutIfNeeded();
                }

                if (cullResults.visibleReflectionProbes.Length != 0 || hdProbeCullingResults.visibleProbes.Count != 0)
                {
                    int processedProbesCount = PreprocessVisibleProbes(hdCamera, cullResults, hdProbeCullingResults, aovRequest);
                    PrepareGPUProbeData(cmd, hdCamera, cullResults, hdProbeCullingResults, processedProbesCount);
                }

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
                m_DensityVolumeCount = densityVolumes.bounds != null ? densityVolumes.bounds.Count : 0;
                m_ProbeVolumeCount = probeVolumes.bounds != null ? probeVolumes.bounds.Count : 0;

                bool probeVolumeNormalBiasEnabled = false;
                if (ShaderConfig.s_ProbeVolumesEvaluationMode != ProbeVolumesEvaluationModes.Disabled)
                {
                    var settings = hdCamera.volumeStack.GetComponent<ProbeVolumeController>();
                    probeVolumeNormalBiasEnabled = !(settings == null || (settings.leakMitigationMode.value != LeakMitigationMode.NormalBias && settings.leakMitigationMode.value != LeakMitigationMode.OctahedralDepthOcclusionFilter));
                }

                for (int viewIndex = 0; viewIndex < hdCamera.viewCount; ++viewIndex)
                {
                    Matrix4x4 worldToViewCR = GetWorldToViewMatrix(hdCamera, viewIndex);

                    if (ShaderConfig.s_CameraRelativeRendering != 0)
                    {
                        // The OBBs are camera-relative, the matrix is not. Fix it.
                        worldToViewCR.SetColumn(3, new Vector4(0, 0, 0, 1));
                    }

                    for (int i = 0, n = m_DensityVolumeCount; i < n; i++)
                    {
                        // Density volumes are not lights and therefore should not affect light classification.
                        LightFeatureFlags featureFlags = 0;
                        CreateBoxVolumeDataAndBound(densityVolumes.bounds[i], LightCategory.DensityVolume, featureFlags, worldToViewCR, 0.0f, out LightVolumeData volumeData, out SFiniteLightBound bound);
                        m_lightList.lightsPerView[viewIndex].lightVolumes.Add(volumeData);
                        m_lightList.lightsPerView[viewIndex].bounds.Add(bound);
                    }

                    for (int i = 0, n = m_ProbeVolumeCount; i < n; i++)
                    {
                        // Probe volumes are not lights and therefore should not affect light classification.
                        LightFeatureFlags featureFlags = 0;
                        float probeVolumeNormalBiasWS = probeVolumeNormalBiasEnabled ? probeVolumes.data[i].normalBiasWS : 0.0f;
                        CreateBoxVolumeDataAndBound(probeVolumes.bounds[i], LightCategory.ProbeVolume, featureFlags, worldToViewCR, probeVolumeNormalBiasWS, out LightVolumeData volumeData, out SFiniteLightBound bound);
                        if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.MaterialPass)
                        {
                            // Only probe volume evaluation in the material pass use these custom probe volume specific lists.
                            // Probe volumes evaluated in the light loop, as well as other volume data such as Decals get folded into the standard list data.
                            m_lightList.lightsPerView[viewIndex].probeVolumesLightVolumes.Add(volumeData);
                            m_lightList.lightsPerView[viewIndex].probeVolumesBounds.Add(bound);
                        }
                        else
                        {

                            m_lightList.lightsPerView[viewIndex].lightVolumes.Add(volumeData);
                            m_lightList.lightsPerView[viewIndex].bounds.Add(bound);
                        }
                    }
                }

                m_TotalLightCount = m_lightList.lights.Count + m_lightList.envLights.Count + decalDatasCount + m_DensityVolumeCount;
                if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.LightLoop)
                {
                    m_TotalLightCount += m_ProbeVolumeCount;
                }

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

                if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.MaterialPass)
                {
                    // Aggregate the remaining probe volume views into the first entry of the list (view 0)
                    for (int viewIndex = 1; viewIndex < hdCamera.viewCount; ++viewIndex)
                    {
                        Debug.Assert(m_lightList.lightsPerView[viewIndex].probeVolumesBounds.Count == m_ProbeVolumeCount);
                        m_lightList.lightsPerView[0].probeVolumesBounds.AddRange(m_lightList.lightsPerView[viewIndex].probeVolumesBounds);

                        Debug.Assert(m_lightList.lightsPerView[viewIndex].probeVolumesLightVolumes.Count == m_ProbeVolumeCount);
                        m_lightList.lightsPerView[0].probeVolumesLightVolumes.AddRange(m_lightList.lightsPerView[viewIndex].probeVolumesLightVolumes);
                    }
                }

                PushLightDataGlobalParams(cmd);
                PushShadowGlobalParams(cmd);
            }

            m_EnableBakeShadowMask = m_EnableBakeShadowMask && hdCamera.frameSettings.IsEnabled(FrameSettingsField.Shadowmask);
            return m_EnableBakeShadowMask;
        }

        internal void ReserveCookieAtlasTexture(HDAdditionalLightData hdLightData, Light light, HDLightType lightType)
        {
            // Note: light component can be null if a Light is used for shuriken particle lighting.
            lightType = light == null ? HDLightType.Point : lightType;
            switch (lightType)
            {
                case HDLightType.Directional:
                    m_TextureCaches.lightCookieManager.ReserveSpace(hdLightData.surfaceTexture);
                    m_TextureCaches.lightCookieManager.ReserveSpace(light?.cookie);
                    break;
                case HDLightType.Point:
                    if (light?.cookie != null && hdLightData.IESPoint != null && light.cookie != hdLightData.IESPoint)
                        m_TextureCaches.lightCookieManager.ReserveSpaceCube(light.cookie, hdLightData.IESPoint);
                    else if (light?.cookie != null)
                        m_TextureCaches.lightCookieManager.ReserveSpaceCube(light.cookie);
                    else if (hdLightData.IESPoint != null)
                        m_TextureCaches.lightCookieManager.ReserveSpaceCube(hdLightData.IESPoint);
                    break;
                case HDLightType.Spot:
                    if (light?.cookie != null && hdLightData.IESSpot != null && light.cookie != hdLightData.IESSpot)
                        m_TextureCaches.lightCookieManager.ReserveSpace(light.cookie, hdLightData.IESSpot);
                    else if (light?.cookie != null)
                        m_TextureCaches.lightCookieManager.ReserveSpace(light.cookie);
                    else if (hdLightData.IESSpot != null)
                        m_TextureCaches.lightCookieManager.ReserveSpace(hdLightData.IESSpot);
                    // Projectors lights must always have a cookie texture.
                    else if (hdLightData.spotLightShape != SpotLightShape.Cone)
                        m_TextureCaches.lightCookieManager.ReserveSpace(Texture2D.whiteTexture);
                    break;
                case HDLightType.Area:
                    // Only rectangle can have cookies
                    if (hdLightData.areaLightShape == AreaLightShape.Rectangle)
                    {
                        if (hdLightData.IESSpot != null && hdLightData.areaLightCookie != null && hdLightData.IESSpot != hdLightData.areaLightCookie)
                            m_TextureCaches.lightCookieManager.ReserveSpace(hdLightData.areaLightCookie, hdLightData.IESSpot);
                        else if (hdLightData.IESSpot != null)
                            m_TextureCaches.lightCookieManager.ReserveSpace(hdLightData.IESSpot);
                        else if (hdLightData.areaLightCookie != null)
                            m_TextureCaches.lightCookieManager.ReserveSpace(hdLightData.areaLightCookie);
                    }
                    break;
            }
        }

        internal static void UpdateLightCameraRelativetData(ref LightData lightData, Vector3 camPosWS)
        {
            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                lightData.positionRWS -= camPosWS;
            }
        }

        internal static void UpdateEnvLighCameraRelativetData(ref EnvLightData envLightData, Vector3 camPosWS)
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
            public int viewCount;
            public bool runLightList;
            public bool clearLightLists;
            public bool enableFeatureVariants;
            public bool computeMaterialVariants;
            public bool computeLightVariants;
            public bool skyEnabled;
            public bool probeVolumeEnabled;
            public LightList lightList;

            // Clear Light lists
            public ComputeShader clearLightListCS;
            public int clearLightListKernel;

            // Screen Space AABBs
            public ComputeShader screenSpaceAABBShader;
            public int screenSpaceAABBKernel;

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
            public ComputeShader clearClusterAtomicIndexShader;
            public int buildPerVoxelLightListKernel;
            public int numTilesClusterX;
            public int numTilesClusterY;
            public bool clusterNeedsDepth;

            // Build dispatch indirect
            public ComputeShader buildMaterialFlagsShader;
            public ComputeShader clearDispatchIndirectShader;
            public ComputeShader buildDispatchIndirectShader;
            public bool useComputeAsPixel;

            public ShaderVariablesLightList lightListCB;
        }

        struct BuildGPULightListResources
        {
            public RTHandle depthBuffer;
            public RTHandle stencilTexture;
            public RTHandle[] gBuffer;

            // Internal to light list building
            public ComputeBuffer lightVolumeDataBuffer;
            public ComputeBuffer convexBoundsBuffer;
            public ComputeBuffer AABBBoundsBuffer;
            public ComputeBuffer globalLightListAtomic;

            // Output
            public ComputeBuffer tileFeatureFlags; // Deferred
            public ComputeBuffer dispatchIndirectBuffer; // Deferred
            public ComputeBuffer perVoxelOffset; // Cluster
            public ComputeBuffer perTileLogBaseTweak; // Cluster
            public ComputeBuffer tileList; // Deferred
            // used for pre-pass coarse culling on 64x64 tiles
            public ComputeBuffer bigTileLightList; // Volumetrics
            public ComputeBuffer perVoxelLightLists; // Cluster
            public ComputeBuffer lightList; // ContactShadows, Deferred, Forward w/ fptl
        }

        BuildGPULightListResources PrepareBuildGPULightListResources(TileAndClusterData tileAndClusterData, RTHandle depthBuffer, RTHandle stencilTexture, bool isGBufferNeeded)
        {
            var resources = new BuildGPULightListResources();

            resources.depthBuffer = depthBuffer;
            resources.stencilTexture = stencilTexture;
            resources.gBuffer = isGBufferNeeded ? m_GbufferManager.GetBuffers() : null;

            resources.bigTileLightList = tileAndClusterData.bigTileLightList;
            resources.lightList = tileAndClusterData.lightList;
            resources.perVoxelOffset = tileAndClusterData.perVoxelOffset;
            resources.convexBoundsBuffer = tileAndClusterData.convexBoundsBuffer;
            resources.AABBBoundsBuffer = tileAndClusterData.AABBBoundsBuffer;
            resources.lightVolumeDataBuffer = tileAndClusterData.lightVolumeDataBuffer;
            resources.tileFeatureFlags = tileAndClusterData.tileFeatureFlags;
            resources.globalLightListAtomic = tileAndClusterData.globalLightListAtomic;
            resources.perVoxelLightLists = tileAndClusterData.perVoxelLightLists;
            resources.perTileLogBaseTweak = tileAndClusterData.perTileLogBaseTweak;
            resources.dispatchIndirectBuffer = tileAndClusterData.dispatchIndirectBuffer;
            resources.tileList = tileAndClusterData.tileList;

            return resources;
        }

        static void ClearLightList(in BuildGPULightListParameters parameters, CommandBuffer cmd, ComputeBuffer bufferToClear)
        {
            cmd.SetComputeBufferParam(parameters.clearLightListCS, parameters.clearLightListKernel, HDShaderIDs._LightListToClear, bufferToClear);
            Vector2 countAndOffset = new Vector2Int(bufferToClear.count, 0);

            int groupSize = 64;
            int totalNumberOfGroupsNeeded = (bufferToClear.count + groupSize - 1) / groupSize;

            const int maxAllowedGroups = 65535;
            // On higher resolutions we might end up with more than 65535 group which is not allowed, so we need to to have multiple dispatches.
            int i = 0;
            while(totalNumberOfGroupsNeeded > 0)
            {
                countAndOffset.y = maxAllowedGroups * i;                
                cmd.SetComputeVectorParam(parameters.clearLightListCS, HDShaderIDs._LightListEntriesAndOffset, countAndOffset);

                int currGroupCount = Math.Min(maxAllowedGroups, totalNumberOfGroupsNeeded);

                cmd.DispatchCompute(parameters.clearLightListCS, parameters.clearLightListKernel, currGroupCount, 1, 1);

                totalNumberOfGroupsNeeded -= currGroupCount;
                i++;
            }
        }

        static void ClearLightLists(    in BuildGPULightListParameters parameters,
                                        in BuildGPULightListResources resources,
                                        CommandBuffer cmd)
        {
            if (parameters.clearLightLists)
            {
                // Note we clear the whole content and not just the header since it is fast enough, happens only in one frame and is a bit more robust
                // to changes to the inner workings of the lists.
                // Also, we clear all the lists and to be resilient to changes in pipeline.
                if (parameters.runBigTilePrepass)
                    ClearLightList(parameters, cmd, resources.bigTileLightList);
                if (resources.lightList != null) // This can happen for probe volume light list build where we only generate clusters.
                    ClearLightList(parameters, cmd, resources.lightList);
                ClearLightList(parameters, cmd, resources.perVoxelOffset);
            }
        }

        // generate screen-space AABBs (used for both fptl and clustered).
        static void GenerateLightsScreenSpaceAABBs(in BuildGPULightListParameters parameters, in BuildGPULightListResources resources, CommandBuffer cmd)
        {
            if (parameters.totalLightCount != 0)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.GenerateLightAABBs)))
                {
                    // With XR single-pass, we have one set of light bounds per view to iterate over (bounds are in view space for each view)
                    cmd.SetComputeBufferParam(parameters.screenSpaceAABBShader, parameters.screenSpaceAABBKernel, HDShaderIDs.g_data, resources.convexBoundsBuffer);
                    cmd.SetComputeBufferParam(parameters.screenSpaceAABBShader, parameters.screenSpaceAABBKernel, HDShaderIDs.g_vBoundsBuffer, resources.AABBBoundsBuffer);

                    ConstantBuffer.Push(cmd, parameters.lightListCB, parameters.screenSpaceAABBShader, HDShaderIDs._ShaderVariablesLightList);

                    const int threadsPerLight = 4;  // Shader: THREADS_PER_LIGHT (4)
                    const int threadsPerGroup = 64; // Shader: THREADS_PER_GROUP (64)

                    int groupCount = HDUtils.DivRoundUp(parameters.totalLightCount * threadsPerLight, threadsPerGroup);

                    cmd.DispatchCompute(parameters.screenSpaceAABBShader, parameters.screenSpaceAABBKernel, groupCount, parameters.viewCount, 1);
                }
            }
        }

        // enable coarse 2D pass on 64x64 tiles (used for both fptl and clustered).
        static void BigTilePrepass(in BuildGPULightListParameters parameters, in BuildGPULightListResources resources, CommandBuffer cmd)
        {
            if (parameters.runLightList && parameters.runBigTilePrepass)
            {
                cmd.SetComputeBufferParam(parameters.bigTilePrepassShader, parameters.bigTilePrepassKernel, HDShaderIDs.g_vLightList, resources.bigTileLightList);
                cmd.SetComputeBufferParam(parameters.bigTilePrepassShader, parameters.bigTilePrepassKernel, HDShaderIDs.g_vBoundsBuffer, resources.AABBBoundsBuffer);
                cmd.SetComputeBufferParam(parameters.bigTilePrepassShader, parameters.bigTilePrepassKernel, HDShaderIDs._LightVolumeData, resources.lightVolumeDataBuffer);
                cmd.SetComputeBufferParam(parameters.bigTilePrepassShader, parameters.bigTilePrepassKernel, HDShaderIDs.g_data, resources.convexBoundsBuffer);

                ConstantBuffer.Push(cmd, parameters.lightListCB, parameters.bigTilePrepassShader, HDShaderIDs._ShaderVariablesLightList);

                cmd.DispatchCompute(parameters.bigTilePrepassShader, parameters.bigTilePrepassKernel, parameters.numBigTilesX, parameters.numBigTilesY, parameters.viewCount);
            }
        }

        static void BuildPerTileLightList(in BuildGPULightListParameters parameters, in BuildGPULightListResources resources, ref bool tileFlagsWritten, CommandBuffer cmd)
        {
            // optimized for opaques only
            if (parameters.runLightList && parameters.runFPTL)
            {
                cmd.SetComputeBufferParam(parameters.buildPerTileLightListShader, parameters.buildPerTileLightListKernel, HDShaderIDs.g_vBoundsBuffer, resources.AABBBoundsBuffer);
                cmd.SetComputeBufferParam(parameters.buildPerTileLightListShader, parameters.buildPerTileLightListKernel, HDShaderIDs._LightVolumeData, resources.lightVolumeDataBuffer);
                cmd.SetComputeBufferParam(parameters.buildPerTileLightListShader, parameters.buildPerTileLightListKernel, HDShaderIDs.g_data, resources.convexBoundsBuffer);

                cmd.SetComputeTextureParam(parameters.buildPerTileLightListShader, parameters.buildPerTileLightListKernel, HDShaderIDs.g_depth_tex, resources.depthBuffer);
                cmd.SetComputeBufferParam(parameters.buildPerTileLightListShader, parameters.buildPerTileLightListKernel, HDShaderIDs.g_vLightList, resources.lightList);
                if (parameters.runBigTilePrepass)
                    cmd.SetComputeBufferParam(parameters.buildPerTileLightListShader, parameters.buildPerTileLightListKernel, HDShaderIDs.g_vBigTileLightList, resources.bigTileLightList);

                var localLightListCB = parameters.lightListCB;

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

                    if (parameters.probeVolumeEnabled)
                    {
                        // If probe volume feature is enabled, we toggle this feature on for all tiles.
                        // This is necessary because all tiles must sample ambient probe fallback.
                        // It is possible we could save a little bit of work by having 2x feature flags for probe volumes:
                        // one specifiying which tiles contain probe volumes,
                        // and another triggered for all tiles to handle fallback.
                        baseFeatureFlags |= (uint)LightFeatureFlags.ProbeVolume;
                    }

                    localLightListCB.g_BaseFeatureFlags = baseFeatureFlags;

                    cmd.SetComputeBufferParam(parameters.buildPerTileLightListShader, parameters.buildPerTileLightListKernel, HDShaderIDs.g_TileFeatureFlags, resources.tileFeatureFlags);
                    tileFlagsWritten = true;
                }

                ConstantBuffer.Push(cmd, localLightListCB, parameters.buildPerTileLightListShader, HDShaderIDs._ShaderVariablesLightList);

                cmd.DispatchCompute(parameters.buildPerTileLightListShader, parameters.buildPerTileLightListKernel, parameters.numTilesFPTLX, parameters.numTilesFPTLY, parameters.viewCount);
            }
        }
        static void VoxelLightListGeneration(in BuildGPULightListParameters parameters, in BuildGPULightListResources resources, CommandBuffer cmd)
        {
            if (parameters.runLightList)
            {
                // clear atomic offset index
                cmd.SetComputeBufferParam(parameters.clearClusterAtomicIndexShader, s_ClearVoxelAtomicKernel, HDShaderIDs.g_LayeredSingleIdxBuffer, resources.globalLightListAtomic);
                cmd.DispatchCompute(parameters.clearClusterAtomicIndexShader, s_ClearVoxelAtomicKernel, 1, 1, 1);

                cmd.SetComputeBufferParam(parameters.buildPerVoxelLightListShader, s_ClearVoxelAtomicKernel, HDShaderIDs.g_LayeredSingleIdxBuffer, resources.globalLightListAtomic);
                cmd.SetComputeBufferParam(parameters.buildPerVoxelLightListShader, parameters.buildPerVoxelLightListKernel, HDShaderIDs.g_vLayeredLightList, resources.perVoxelLightLists);
                cmd.SetComputeBufferParam(parameters.buildPerVoxelLightListShader, parameters.buildPerVoxelLightListKernel, HDShaderIDs.g_LayeredOffset, resources.perVoxelOffset);
                cmd.SetComputeBufferParam(parameters.buildPerVoxelLightListShader, parameters.buildPerVoxelLightListKernel, HDShaderIDs.g_LayeredSingleIdxBuffer, resources.globalLightListAtomic);

                if (parameters.runBigTilePrepass)
                    cmd.SetComputeBufferParam(parameters.buildPerVoxelLightListShader, parameters.buildPerVoxelLightListKernel, HDShaderIDs.g_vBigTileLightList, resources.bigTileLightList);

                if (parameters.clusterNeedsDepth)
                {
                    cmd.SetComputeTextureParam(parameters.buildPerVoxelLightListShader, parameters.buildPerVoxelLightListKernel, HDShaderIDs.g_depth_tex, resources.depthBuffer);
                    cmd.SetComputeBufferParam(parameters.buildPerVoxelLightListShader, parameters.buildPerVoxelLightListKernel, HDShaderIDs.g_logBaseBuffer, resources.perTileLogBaseTweak);
                }

                cmd.SetComputeBufferParam(parameters.buildPerVoxelLightListShader, parameters.buildPerVoxelLightListKernel, HDShaderIDs.g_vBoundsBuffer, resources.AABBBoundsBuffer);
                cmd.SetComputeBufferParam(parameters.buildPerVoxelLightListShader, parameters.buildPerVoxelLightListKernel, HDShaderIDs._LightVolumeData, resources.lightVolumeDataBuffer);
                cmd.SetComputeBufferParam(parameters.buildPerVoxelLightListShader, parameters.buildPerVoxelLightListKernel, HDShaderIDs.g_data, resources.convexBoundsBuffer);

                ConstantBuffer.Push(cmd, parameters.lightListCB, parameters.buildPerVoxelLightListShader, HDShaderIDs._ShaderVariablesLightList);

                cmd.DispatchCompute(parameters.buildPerVoxelLightListShader, parameters.buildPerVoxelLightListKernel, parameters.numTilesClusterX, parameters.numTilesClusterY, parameters.viewCount);
            }
        }

        static void BuildDispatchIndirectArguments(in BuildGPULightListParameters parameters, in BuildGPULightListResources resources, bool tileFlagsWritten, CommandBuffer cmd)
        {
            if (parameters.enableFeatureVariants)
            {
                // We need to touch up the tile flags if we need material classification or, if disabled, to patch up for missing flags during the skipped light tile gen
                bool needModifyingTileFeatures = !tileFlagsWritten || parameters.computeMaterialVariants;
                if (needModifyingTileFeatures)
                {
                    int buildMaterialFlagsKernel = s_BuildMaterialFlagsWriteKernel;
                    parameters.buildMaterialFlagsShader.shaderKeywords = null;
                    if (tileFlagsWritten && parameters.computeLightVariants)
                    {
                        parameters.buildMaterialFlagsShader.EnableKeyword("USE_OR");
                    }

                    uint baseFeatureFlags = 0;
                    if (!parameters.computeLightVariants)
                    {
                        baseFeatureFlags |= LightDefinitions.s_LightFeatureMaskFlags;
                    }
                    if (parameters.probeVolumeEnabled)
                    {
                        // TODO: Verify that we should be globally enabling ProbeVolume feature for all tiles here, or if we should be using per-tile culling.
                        baseFeatureFlags |= (uint)LightFeatureFlags.ProbeVolume;
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

                    var localLightListCB = parameters.lightListCB;
                    localLightListCB.g_BaseFeatureFlags = baseFeatureFlags;

                    cmd.SetComputeBufferParam(parameters.buildMaterialFlagsShader, buildMaterialFlagsKernel, HDShaderIDs.g_TileFeatureFlags, resources.tileFeatureFlags);

                    for (int i = 0; i < resources.gBuffer.Length; ++i)
                        cmd.SetComputeTextureParam(parameters.buildMaterialFlagsShader, buildMaterialFlagsKernel, HDShaderIDs._GBufferTexture[i], resources.gBuffer[i]);

                    if (resources.stencilTexture.rt == null ||
                        resources.stencilTexture.rt.stencilFormat == GraphicsFormat.None) // We are accessing MSAA resolved version and not the depth stencil buffer directly.
                    {
                        cmd.SetComputeTextureParam(parameters.buildMaterialFlagsShader, buildMaterialFlagsKernel, HDShaderIDs._StencilTexture, resources.stencilTexture);
                    }
                    else
                    {
                        cmd.SetComputeTextureParam(parameters.buildMaterialFlagsShader, buildMaterialFlagsKernel, HDShaderIDs._StencilTexture, resources.stencilTexture, 0, RenderTextureSubElement.Stencil);
                    }

                    ConstantBuffer.Push(cmd, localLightListCB, parameters.buildMaterialFlagsShader, HDShaderIDs._ShaderVariablesLightList);

                    cmd.DispatchCompute(parameters.buildMaterialFlagsShader, buildMaterialFlagsKernel, parameters.numTilesFPTLX, parameters.numTilesFPTLY, parameters.viewCount);
                }

                // clear dispatch indirect buffer
                if (parameters.useComputeAsPixel)
                {
                    cmd.SetComputeBufferParam(parameters.clearDispatchIndirectShader, s_ClearDrawProceduralIndirectKernel, HDShaderIDs.g_DispatchIndirectBuffer, resources.dispatchIndirectBuffer);
                    cmd.SetComputeIntParam(parameters.clearDispatchIndirectShader, HDShaderIDs.g_NumTiles, parameters.numTilesFPTL);
                    cmd.SetComputeIntParam(parameters.clearDispatchIndirectShader, HDShaderIDs.g_VertexPerTile, k_HasNativeQuadSupport ? 4 : 6);
                    cmd.DispatchCompute(parameters.clearDispatchIndirectShader, s_ClearDrawProceduralIndirectKernel, 1, 1, 1);

                }
                else
                {
                    cmd.SetComputeBufferParam(parameters.clearDispatchIndirectShader, s_ClearDispatchIndirectKernel, HDShaderIDs.g_DispatchIndirectBuffer, resources.dispatchIndirectBuffer);
                    cmd.DispatchCompute(parameters.clearDispatchIndirectShader, s_ClearDispatchIndirectKernel, 1, 1, 1);
                }

                // add tiles to indirect buffer
                cmd.SetComputeBufferParam(parameters.buildDispatchIndirectShader, s_BuildIndirectKernel, HDShaderIDs.g_DispatchIndirectBuffer, resources.dispatchIndirectBuffer);
                cmd.SetComputeBufferParam(parameters.buildDispatchIndirectShader, s_BuildIndirectKernel, HDShaderIDs.g_TileList, resources.tileList);
                cmd.SetComputeBufferParam(parameters.buildDispatchIndirectShader, s_BuildIndirectKernel, HDShaderIDs.g_TileFeatureFlags, resources.tileFeatureFlags);
                cmd.SetComputeIntParam(parameters.buildDispatchIndirectShader, HDShaderIDs.g_NumTiles, parameters.numTilesFPTL);
                cmd.SetComputeIntParam(parameters.buildDispatchIndirectShader, HDShaderIDs.g_NumTilesX, parameters.numTilesFPTLX);
                // Round on k_ThreadGroupOptimalSize so we have optimal thread for buildDispatchIndirectShader kernel
                cmd.DispatchCompute(parameters.buildDispatchIndirectShader, s_BuildIndirectKernel, (parameters.numTilesFPTL + k_ThreadGroupOptimalSize - 1) / k_ThreadGroupOptimalSize, 1, parameters.viewCount);
            }
        }

        static bool DeferredUseComputeAsPixel(FrameSettings frameSettings)
        {
            return frameSettings.IsEnabled(FrameSettingsField.DeferredTile) && (!frameSettings.IsEnabled(FrameSettingsField.ComputeLightEvaluation) || k_PreferFragment);
        }

        unsafe BuildGPULightListParameters PrepareBuildGPULightListParameters(  HDCamera                        hdCamera,
                                                                                TileAndClusterData              tileAndClusterData,
                                                                                ref ShaderVariablesLightList    constantBuffer,
                                                                                int                             totalLightCount)
        {
            BuildGPULightListParameters parameters = new BuildGPULightListParameters();

            var camera = hdCamera.camera;

            var w = (int)hdCamera.screenSize.x;
            var h = (int)hdCamera.screenSize.y;

            // Fill the shared constant buffer.
            // We don't fill directly the one in the parameter struct because we will need those parameters for volumetric lighting as well.
            ref var cb = ref constantBuffer;
            var temp = new Matrix4x4();
            temp.SetRow(0, new Vector4(0.5f * w, 0.0f, 0.0f, 0.5f * w));
            temp.SetRow(1, new Vector4(0.0f, 0.5f * h, 0.0f, 0.5f * h));
            temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
            temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

            // camera to screen matrix (and it's inverse)
            for (int viewIndex = 0; viewIndex < hdCamera.viewCount; ++viewIndex)
            {
                var proj = hdCamera.xr.enabled ? hdCamera.xr.GetProjMatrix(viewIndex) : camera.projectionMatrix;
                m_LightListProjMatrices[viewIndex] = proj * s_FlipMatrixLHSRHS;

                for (int i = 0; i < 16; ++i)
                {
                    var tempMatrix = temp * m_LightListProjMatrices[viewIndex];
                    var invTempMatrix = tempMatrix.inverse;
                    cb.g_mScrProjectionArr[viewIndex * 16 + i] = tempMatrix[i];
                    cb.g_mInvScrProjectionArr[viewIndex * 16 + i] = invTempMatrix[i];
                }
            }

            // camera to screen matrix (and it's inverse)
            for (int viewIndex = 0; viewIndex < hdCamera.viewCount; ++viewIndex)
            {
                temp.SetRow(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
                temp.SetRow(1, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
                temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
                temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

                for (int i = 0; i < 16; ++i)
                {
                    var tempMatrix = temp * m_LightListProjMatrices[viewIndex];
                    var invTempMatrix = tempMatrix.inverse;
                    cb.g_mProjectionArr[viewIndex * 16 + i] = tempMatrix[i];
                    cb.g_mInvProjectionArr[viewIndex * 16 + i] = invTempMatrix[i];
                }
            }

            var decalDatasCount = Math.Min(DecalSystem.m_DecalDatasCount, m_MaxDecalsOnScreen);

            cb.g_iNrVisibLights = totalLightCount;
            cb.g_screenSize = hdCamera.screenSize; // TODO remove and use global one.
            cb.g_viDimensions = new Vector2Int((int)hdCamera.screenSize.x, (int)hdCamera.screenSize.y);
            cb.g_isOrthographic = camera.orthographic ? 1u : 0u;
            cb.g_BaseFeatureFlags = 0; // Filled for each individual pass.
            cb.g_iNumSamplesMSAA = (int)hdCamera.msaaSamples;
            cb._EnvLightIndexShift = (uint)m_lightList.lights.Count;
            cb._DecalIndexShift = (uint)(m_lightList.lights.Count + m_lightList.envLights.Count);
            cb._DensityVolumeIndexShift = (uint)(m_lightList.lights.Count + m_lightList.envLights.Count + decalDatasCount);

            int probeVolumeIndexShift = (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.LightLoop)
                    ? (m_lightList.lights.Count + m_lightList.envLights.Count + decalDatasCount + m_DensityVolumeCount)
                    : 0;
            cb._ProbeVolumeIndexShift = (uint)probeVolumeIndexShift;

            // Copy the constant buffer into the parameter struct.
            parameters.lightListCB = cb;

            parameters.totalLightCount = totalLightCount;
            parameters.runLightList = parameters.totalLightCount > 0;
            parameters.clearLightLists = false;

            // TODO RENDERGRAPH: This logic is flawed with Render Graph.
            // In theory buffers memory might be reused from another usage entirely so keeping track of its "cleared" state does not represent the truth of their content.
            // In practice though, when resolution stays the same, buffers will be the same reused from one frame to another
            // because for now buffers are pooled based on their parameters. When we do proper aliasing though, we might end up with any random chunk of memory.

            // Always build the light list in XR mode to avoid issues with multi-pass
            if (hdCamera.xr.enabled)
            {
                parameters.runLightList = true;
            }
            else if (!parameters.runLightList && !tileAndClusterData.listsAreClear)
            {
                parameters.clearLightLists = true;
                // After that, No need to clear it anymore until we start and stop running light list building.
                tileAndClusterData.listsAreClear = true;
            }
            else if (parameters.runLightList)
            {
                tileAndClusterData.listsAreClear = false;
            }

            parameters.viewCount = hdCamera.viewCount;
            parameters.enableFeatureVariants = GetFeatureVariantsEnabled(hdCamera.frameSettings) && tileAndClusterData.hasTileBuffers;
            parameters.computeMaterialVariants = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ComputeMaterialVariants);
            parameters.computeLightVariants = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ComputeLightVariants);
            parameters.lightList = m_lightList;
            parameters.skyEnabled = m_SkyManager.IsLightingSkyValid(hdCamera);
            parameters.useComputeAsPixel = DeferredUseComputeAsPixel(hdCamera.frameSettings);
            parameters.probeVolumeEnabled = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume) && m_ProbeVolumeCount > 0;

            bool isProjectionOblique = GeometryUtils.IsProjectionMatrixOblique(m_LightListProjMatrices[0]);

            // Clear light lsts
            parameters.clearLightListCS = defaultResources.shaders.clearLightListsCS;
            parameters.clearLightListKernel = parameters.clearLightListCS.FindKernel("ClearList");

            // Screen space AABB
            parameters.screenSpaceAABBShader = buildScreenAABBShader;
            parameters.screenSpaceAABBKernel = 0;

            // Big tile prepass
            parameters.runBigTilePrepass = hdCamera.frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass);
            parameters.bigTilePrepassShader = buildPerBigTileLightListShader;
            parameters.bigTilePrepassKernel = s_GenListPerBigTileKernel;
            parameters.numBigTilesX = (w + 63) / 64;
            parameters.numBigTilesY = (h + 63) / 64;

            // Fptl
            parameters.runFPTL = hdCamera.frameSettings.fptl && tileAndClusterData.hasTileBuffers;
            parameters.buildPerTileLightListShader = buildPerTileLightListShader;
            parameters.buildPerTileLightListShader.shaderKeywords = null;
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass))
            {
                parameters.buildPerTileLightListShader.EnableKeyword("USE_TWO_PASS_TILED_LIGHTING");
            }
            if (isProjectionOblique)
            {
                parameters.buildPerTileLightListShader.EnableKeyword("USE_OBLIQUE_MODE");
            }
            if (GetFeatureVariantsEnabled(hdCamera.frameSettings))
            {
                parameters.buildPerTileLightListShader.EnableKeyword("USE_FEATURE_FLAGS");
            }
            parameters.buildPerTileLightListKernel = s_GenListPerTileKernel;

            parameters.numTilesFPTLX = GetNumTileFtplX(hdCamera);
            parameters.numTilesFPTLY = GetNumTileFtplY(hdCamera);
            parameters.numTilesFPTL = parameters.numTilesFPTLX * parameters.numTilesFPTLY;

            // Cluster
            bool msaa = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA);
            var clustPrepassSourceIdx = hdCamera.frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass) ? ClusterPrepassSource.BigTile : ClusterPrepassSource.None;
            var clustDepthSourceIdx = ClusterDepthSource.NoDepth;
            if (tileAndClusterData.clusterNeedsDepth)
                clustDepthSourceIdx = msaa ? ClusterDepthSource.MSAA_Depth : ClusterDepthSource.Depth;

            parameters.buildPerVoxelLightListShader = buildPerVoxelLightListShader;
            parameters.clearClusterAtomicIndexShader = clearClusterAtomicIndexShader;
            parameters.buildPerVoxelLightListKernel = isProjectionOblique ? s_ClusterObliqueKernels[(int)clustPrepassSourceIdx, (int)clustDepthSourceIdx] : s_ClusterKernels[(int)clustPrepassSourceIdx, (int)clustDepthSourceIdx];
            parameters.numTilesClusterX = GetNumTileClusteredX(hdCamera);
            parameters.numTilesClusterY = GetNumTileClusteredY(hdCamera);
            parameters.clusterNeedsDepth = tileAndClusterData.clusterNeedsDepth;

            // Build dispatch indirect
            parameters.buildMaterialFlagsShader = buildMaterialFlagsShader;
            parameters.clearDispatchIndirectShader = clearDispatchIndirectShader;
            parameters.buildDispatchIndirectShader = buildDispatchIndirectShader;
            parameters.buildDispatchIndirectShader.shaderKeywords = null;
            if (parameters.useComputeAsPixel)
            {
                parameters.buildDispatchIndirectShader.EnableKeyword("IS_DRAWPROCEDURALINDIRECT");
            }


            return parameters;
        }

        // Note: This is a trivial setter and could be removed if we do not like that style.
        // I exposed it as a function, even though it will only ever be set by HDRenderPipeline just to make it more clear that
        // m_ProbeVolumeList is not set inside of LightLoop.cs
        void SetProbeVolumeList(ProbeVolumeList probeVolumeList)
        {
            m_ProbeVolumeList = probeVolumeList;
        }

        static void PushProbeVolumeLightListGlobalParams(in LightLoopGlobalParameters param, CommandBuffer cmd)
        {
            Debug.Assert(ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.MaterialPass);

            if (!param.hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume))
                return;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.PushProbeVolumeLightListGlobalParameters)))
            {
                Camera camera = param.hdCamera.camera;

                if (param.hdCamera.frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass))
                    cmd.SetGlobalBuffer(HDShaderIDs.g_vBigTileLightList, param.tileAndClusterData.bigTileLightList);

                // int useDepthBuffer = 0;
                // cmd.SetGlobalInt(HDShaderIDs.g_isLogBaseBufferEnabled, useDepthBuffer);
                cmd.SetGlobalBuffer(HDShaderIDs.g_vProbeVolumesLayeredOffsetsBuffer, param.tileAndClusterData.perVoxelOffset);
                cmd.SetGlobalBuffer(HDShaderIDs.g_vProbeVolumesLightListGlobal, param.tileAndClusterData.perVoxelLightLists);
            }
        }

        void BuildGPULightListProbeVolumesCommon(HDCamera hdCamera, CommandBuffer cmd)
        {
            // Custom probe volume only light list is only needed if we are evaluating probe volumes early, in the GBuffer phase.
            // If probe volumes are evaluated in the Light Loop, they are folded into the standard light lists.
            if (ShaderConfig.s_ProbeVolumesEvaluationMode != ProbeVolumesEvaluationModes.MaterialPass)
                return;

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume))
                return;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.BuildGPULightListProbeVolumes)))
            {
                var parameters = PrepareBuildGPULightListParameters(hdCamera, m_ProbeVolumeClusterData, ref m_ShaderVariablesProbeVolumeLightListCB, m_ProbeVolumeCount);
                var resources = PrepareBuildGPULightListResources(
                    m_ProbeVolumeClusterData,
                    m_SharedRTManager.GetDepthStencilBuffer(hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA)),
                    m_SharedRTManager.GetStencilBuffer(hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA)),
                    isGBufferNeeded: false
                );

                ClearLightLists(parameters, resources, cmd);
                GenerateLightsScreenSpaceAABBs(parameters, resources, cmd);
                BigTilePrepass(parameters, resources, cmd);
                VoxelLightListGeneration(parameters, resources, cmd);
            }
        }

        void BuildGPULightListsCommon(HDCamera hdCamera, CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.BuildLightList)))
            {
                var parameters = PrepareBuildGPULightListParameters(hdCamera, m_TileAndClusterData, ref m_ShaderVariablesLightListCB, m_TotalLightCount);
                var resources = PrepareBuildGPULightListResources(
                    m_TileAndClusterData,
                    m_SharedRTManager.GetDepthStencilBuffer(hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA)),
                    m_SharedRTManager.GetStencilBuffer(hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA)),
                    isGBufferNeeded: true
                );

                bool tileFlagsWritten = false;

                ClearLightLists(parameters, resources, cmd);
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

            var globalParams = PrepareLightLoopGlobalParameters(hdCamera, m_TileAndClusterData);
            PushLightLoopGlobalParams(globalParams, cmd);
        }

        HDAdditionalLightData GetHDAdditionalLightData(Light light)
        {
            HDAdditionalLightData add = null;

            // Light reference can be null for particle lights.
            if (light != null)
                light.TryGetComponent<HDAdditionalLightData>(out add);

            // Light should always have additional data, however preview light right don't have, so we must handle the case by assigning HDUtils.s_DefaultHDAdditionalLightData
            if (add == null)
                add = HDUtils.s_DefaultHDAdditionalLightData;

            return add;
        }

        struct LightLoopGlobalParameters
        {
            public HDCamera                 hdCamera;
            public TileAndClusterData       tileAndClusterData;
        }

        LightLoopGlobalParameters PrepareLightLoopGlobalParameters(HDCamera hdCamera, TileAndClusterData tileAndClusterData)
        {
            LightLoopGlobalParameters parameters = new LightLoopGlobalParameters();
            parameters.hdCamera = hdCamera;
            parameters.tileAndClusterData = tileAndClusterData;
            return parameters;
        }

        unsafe void UpdateShaderVariablesGlobalLightLoop(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            // Atlases
            cb._CookieAtlasSize = m_TextureCaches.lightCookieManager.GetCookieAtlasSize();
            cb._CookieAtlasData = m_TextureCaches.lightCookieManager.GetCookieAtlasDatas();
            cb._PlanarAtlasData = m_TextureCaches.reflectionPlanarProbeCache.GetAtlasDatas();
            cb._EnvSliceSize = m_TextureCaches.reflectionProbeCache.GetEnvSliceSize();

            // Planar reflections
            for (int i = 0; i < asset.currentPlatformRenderPipelineSettings.lightLoopSettings.maxPlanarReflectionOnScreen; ++i)
            {
                for (int j = 0; j < 16; ++j)
                    cb._Env2DCaptureVP[i * 16 + j] = m_TextureCaches.env2DCaptureVP[i][j];

                for (int j = 0; j < 4; ++j)
                    cb._Env2DCaptureForward[i * 4 + j] = m_TextureCaches.env2DCaptureForward[i][j];

                for (int j = 0; j < 4; ++j)
                    cb._Env2DAtlasScaleOffset[i * 4 + j] = m_TextureCaches.env2DAtlasScaleOffset[i][j];
            }

            // Light info
            cb._PunctualLightCount = (uint)m_lightList.punctualLightCount;
            cb._AreaLightCount = (uint)m_lightList.areaLightCount;
            cb._EnvLightCount = (uint)m_lightList.envLights.Count;
            cb._DirectionalLightCount = (uint)m_lightList.directionalLights.Count;
            cb._DecalCount = (uint)DecalSystem.m_DecalDatasCount;
            HDAdditionalLightData sunLightData = GetHDAdditionalLightData(m_CurrentSunLight);
            bool sunLightShadow = sunLightData != null && m_CurrentShadowSortedSunLightIndex >= 0;
            cb._DirectionalShadowIndex = sunLightShadow ? m_CurrentShadowSortedSunLightIndex : -1;
            cb._EnableLightLayers = hdCamera.frameSettings.IsEnabled(FrameSettingsField.LightLayers) ? 1u : 0u;
            cb._EnableDecalLayers = hdCamera.frameSettings.IsEnabled(FrameSettingsField.DecalLayers) ? 1u : 0u;
            cb._EnvLightSkyEnabled = m_SkyManager.IsLightingSkyValid(hdCamera) ? 1 : 0;

            const float C = (float)(1 << k_Log2NumClusters);
            var geomSeries = (1.0 - Mathf.Pow(k_ClustLogBase, C)) / (1 - k_ClustLogBase); // geometric series: sum_k=0^{C-1} base^k

            // Tile/Cluster
            cb._NumTileFtplX = (uint)GetNumTileFtplX(hdCamera);
            cb._NumTileFtplY = (uint)GetNumTileFtplY(hdCamera);
            cb.g_fClustScale = (float)(geomSeries / (hdCamera.camera.farClipPlane - hdCamera.camera.nearClipPlane)); ;
            cb.g_fClustBase = k_ClustLogBase;
            cb.g_fNearPlane = hdCamera.camera.nearClipPlane;
            cb.g_fFarPlane = hdCamera.camera.farClipPlane;
            cb.g_iLog2NumClusters = k_Log2NumClusters;
            cb.g_isLogBaseBufferEnabled = k_UseDepthBuffer ? 1 : 0;
            cb._NumTileClusteredX = (uint)GetNumTileClusteredX(hdCamera);
            cb._NumTileClusteredY = (uint)GetNumTileClusteredY(hdCamera);

            // Misc
            cb._EnableSSRefraction = hdCamera.frameSettings.IsEnabled(FrameSettingsField.Refraction) ? 1u : 0u;
        }

        void PushLightDataGlobalParams(CommandBuffer cmd)
        {
            m_LightLoopLightData.directionalLightData.SetData(m_lightList.directionalLights);
            m_LightLoopLightData.lightData.SetData(m_lightList.lights);
            m_LightLoopLightData.envLightData.SetData(m_lightList.envLights);
            m_LightLoopLightData.decalData.SetData(DecalSystem.m_DecalDatas, 0, 0, Math.Min(DecalSystem.m_DecalDatasCount, m_MaxDecalsOnScreen)); // don't add more than the size of the buffer

            // These two buffers have been set in Rebuild(). At this point, view 0 contains combined data from all views
            m_TileAndClusterData.convexBoundsBuffer.SetData(m_lightList.lightsPerView[0].bounds);
            m_TileAndClusterData.lightVolumeDataBuffer.SetData(m_lightList.lightsPerView[0].lightVolumes);

            if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.MaterialPass)
            {
                m_ProbeVolumeClusterData.convexBoundsBuffer.SetData(m_lightList.lightsPerView[0].probeVolumesBounds);
                m_ProbeVolumeClusterData.lightVolumeDataBuffer.SetData(m_lightList.lightsPerView[0].probeVolumesLightVolumes);
            }

            cmd.SetGlobalTexture(HDShaderIDs._CookieAtlas, m_TextureCaches.lightCookieManager.atlasTexture);
            cmd.SetGlobalTexture(HDShaderIDs._EnvCubemapTextures, m_TextureCaches.reflectionProbeCache.GetTexCache());
            cmd.SetGlobalTexture(HDShaderIDs._Env2DTextures, m_TextureCaches.reflectionPlanarProbeCache.GetTexCache());

            cmd.SetGlobalBuffer(HDShaderIDs._LightDatas, m_LightLoopLightData.lightData);
            cmd.SetGlobalBuffer(HDShaderIDs._EnvLightDatas, m_LightLoopLightData.envLightData);
            cmd.SetGlobalBuffer(HDShaderIDs._DecalDatas, m_LightLoopLightData.decalData);
            cmd.SetGlobalBuffer(HDShaderIDs._DirectionalLightDatas, m_LightLoopLightData.directionalLightData);
        }

        void PushShadowGlobalParams(CommandBuffer cmd)
        {
            m_ShadowManager.PushGlobalParameters(cmd);
        }

        static void PushLightLoopGlobalParams(in LightLoopGlobalParameters param, CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.PushGlobalParameters)))
            {
                if (param.hdCamera.frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass))
                    cmd.SetGlobalBuffer(HDShaderIDs.g_vBigTileLightList, param.tileAndClusterData.bigTileLightList);

                // Cluster
                {
                    cmd.SetGlobalBuffer(HDShaderIDs.g_vLayeredOffsetsBuffer, param.tileAndClusterData.perVoxelOffset);
                    if (k_UseDepthBuffer)
                    {
                        cmd.SetGlobalBuffer(HDShaderIDs.g_logBaseBuffer, param.tileAndClusterData.perTileLogBaseTweak);
                    }

                    // Set up clustered lighting for volumetrics.
                    cmd.SetGlobalBuffer(HDShaderIDs.g_vLightListGlobal, param.tileAndClusterData.perVoxelLightLists);
                }
            }
        }


        void RenderShadowMaps(ScriptableRenderContext renderContext, CommandBuffer cmd, in ShaderVariablesGlobal globalCB, CullingResults cullResults, HDCamera hdCamera)
        {
            // kick off the shadow jobs here
            m_ShadowManager.RenderShadows(renderContext, cmd, globalCB, cullResults, hdCamera);

            // Bind the shadow data
            m_ShadowManager.BindResources(cmd);
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
        void GetContactShadowMask(HDAdditionalLightData hdAdditionalLightData, BoolScalableSetting contactShadowEnabled, HDCamera hdCamera, bool isRasterization, ref int contactShadowMask, ref float rayTracingShadowFlag)
        {
            contactShadowMask = 0;
            rayTracingShadowFlag = 0.0f;
            // If contact shadows are not enabled or we already reached the manimal number of contact shadows
            // or this is not rasterization
            if ((!hdAdditionalLightData.useContactShadow.Value(contactShadowEnabled))
                || m_ContactShadowIndex >= LightDefinitions.s_LightListMaxPrunedEntries
                || !isRasterization)
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
            public Vector4          params3;

            public int              numTilesX;
            public int              numTilesY;
            public int              viewCount;

            public bool             rayTracingEnabled;
            public RayTracingShader contactShadowsRTS;
            public RayTracingAccelerationStructure accelerationStructure;
            public int              actualWidth;
            public int              actualHeight;
            public int              depthTextureParameterName;
        }

        ContactShadowsParameters PrepareContactShadowsParameters(HDCamera hdCamera, float firstMipOffsetY)
        {
            var parameters = new ContactShadowsParameters();

            parameters.contactShadowsCS = contactShadowComputeShader;
            parameters.contactShadowsCS.shaderKeywords = null;
            if(hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA))
            {
                parameters.contactShadowsCS.EnableKeyword("ENABLE_MSAA");
            }

            parameters.rayTracingEnabled = RayTracedContactShadowsRequired();
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing))
            {
                parameters.contactShadowsRTS = m_Asset.renderPipelineRayTracingResources.contactShadowRayTracingRT;
                parameters.accelerationStructure = RequestAccelerationStructure();

                parameters.actualWidth = hdCamera.actualWidth;
                parameters.actualHeight = hdCamera.actualHeight;
            }

            parameters.kernel = s_deferredContactShadowKernel;

            float contactShadowRange = Mathf.Clamp(m_ContactShadows.fadeDistance.value, 0.0f, m_ContactShadows.maxDistance.value);
            float contactShadowFadeEnd = m_ContactShadows.maxDistance.value;
            float contactShadowOneOverFadeRange = 1.0f / Math.Max(1e-6f, contactShadowRange);

            float contactShadowMinDist = Mathf.Min(m_ContactShadows.minDistance.value, contactShadowFadeEnd);
            float contactShadowFadeIn = Mathf.Clamp(m_ContactShadows.fadeInDistance.value, 1e-6f, contactShadowFadeEnd);

            parameters.params1 = new Vector4(m_ContactShadows.length.value, m_ContactShadows.distanceScaleFactor.value, contactShadowFadeEnd, contactShadowOneOverFadeRange);
            parameters.params2 = new Vector4(firstMipOffsetY, contactShadowMinDist, contactShadowFadeIn, m_ContactShadows.rayBias.value * 0.01f);
            parameters.params3 = new Vector4(m_ContactShadows.sampleCount, m_ContactShadows.thicknessScale.value * 10.0f , 0.0f, 0.0f);

            int deferredShadowTileSize = 8; // Must match ContactShadows.compute
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
                                            ComputeBuffer               lightList,
                                            CommandBuffer               cmd)
        {

            cmd.SetComputeVectorParam(parameters.contactShadowsCS, HDShaderIDs._ContactShadowParamsParameters, parameters.params1);
            cmd.SetComputeVectorParam(parameters.contactShadowsCS, HDShaderIDs._ContactShadowParamsParameters2, parameters.params2);
            cmd.SetComputeVectorParam(parameters.contactShadowsCS, HDShaderIDs._ContactShadowParamsParameters3, parameters.params3);
            cmd.SetComputeBufferParam(parameters.contactShadowsCS, parameters.kernel, HDShaderIDs._DirectionalLightDatas, lightLoopLightData.directionalLightData);

            // Send light list to the compute
            cmd.SetComputeBufferParam(parameters.contactShadowsCS, parameters.kernel, HDShaderIDs._LightDatas, lightLoopLightData.lightData);
            cmd.SetComputeBufferParam(parameters.contactShadowsCS, parameters.kernel, HDShaderIDs.g_vLightListGlobal, lightList);

            cmd.SetComputeTextureParam(parameters.contactShadowsCS, parameters.kernel, parameters.depthTextureParameterName, depthTexture);
            cmd.SetComputeTextureParam(parameters.contactShadowsCS, parameters.kernel, HDShaderIDs._ContactShadowTextureUAV, contactShadowRT);

            cmd.DispatchCompute(parameters.contactShadowsCS, parameters.kernel, parameters.numTilesX, parameters.numTilesY, parameters.viewCount);

            if (parameters.rayTracingEnabled)
            {
                cmd.SetRayTracingShaderPass(parameters.contactShadowsRTS, "VisibilityDXR");
                cmd.SetRayTracingAccelerationStructure(parameters.contactShadowsRTS, HDShaderIDs._RaytracingAccelerationStructureName, parameters.accelerationStructure);

                cmd.SetRayTracingVectorParam(parameters.contactShadowsRTS, HDShaderIDs._ContactShadowParamsParameters, parameters.params1);
                cmd.SetRayTracingVectorParam(parameters.contactShadowsRTS, HDShaderIDs._ContactShadowParamsParameters2, parameters.params2);
                cmd.SetRayTracingBufferParam(parameters.contactShadowsRTS, HDShaderIDs._DirectionalLightDatas, lightLoopLightData.directionalLightData);

                // Send light list to the compute
                cmd.SetRayTracingBufferParam(parameters.contactShadowsRTS, HDShaderIDs._LightDatas, lightLoopLightData.lightData);
                cmd.SetRayTracingBufferParam(parameters.contactShadowsRTS, HDShaderIDs.g_vLightListGlobal, lightList);

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

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ContactShadows)))
            {
                m_ShadowManager.BindResources(cmd);

                var depthTexture = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA) ? m_SharedRTManager.GetDepthValuesTexture() : m_SharedRTManager.GetDepthTexture();
                int firstMipOffsetY = m_SharedRTManager.GetDepthBufferMipChainInfo().mipLevelOffsets[1].y;
                var parameters = PrepareContactShadowsParameters(hdCamera, firstMipOffsetY);
                RenderContactShadows(parameters, m_ContactShadowBuffer, depthTexture, m_LightLoopLightData, m_TileAndClusterData.lightList, cmd);
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
            parameters.enableShadowMasks = m_EnableBakeShadowMask;
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

            var parameters = PrepareDeferredLightingParameters(hdCamera, m_CurrentDebugDisplaySettings);
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
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderDeferredLightingCompute)))
            {
                cmd.SetGlobalBuffer(HDShaderIDs.g_vLightListGlobal, resources.lightListBuffer);
                parameters.deferredComputeShader.shaderKeywords = null;

                switch (HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.shadowFilteringQuality)
                {
                    case HDShadowFilteringQuality.Low:
                        parameters.deferredComputeShader.EnableKeyword("SHADOW_LOW");
                        break;
                    case HDShadowFilteringQuality.Medium:
                        parameters.deferredComputeShader.EnableKeyword("SHADOW_MEDIUM");
                        break;
                    case HDShadowFilteringQuality.High:
                        parameters.deferredComputeShader.EnableKeyword("SHADOW_HIGH");
                        break;
                    default:
                        parameters.deferredComputeShader.EnableKeyword("SHADOW_MEDIUM");
                        break;
                }

                if (parameters.enableShadowMasks)
                {
                    parameters.deferredComputeShader.EnableKeyword("SHADOWS_SHADOWMASK");
                }

                for (int variant = 0; variant < parameters.numVariants; variant++)
                {
                    int kernel;

                    if (parameters.enableFeatureVariants)
                    {
                        kernel = s_shadeOpaqueIndirectFptlKernels[variant];
                    }
                    else
                    {
                        kernel = parameters.debugDisplaySettings.IsDebugDisplayEnabled() ? s_shadeOpaqueDirectFptlDebugDisplayKernel : s_shadeOpaqueDirectFptlKernel;
                    }

                    cmd.SetComputeTextureParam(parameters.deferredComputeShader, kernel, HDShaderIDs._CameraDepthTexture, resources.depthTexture);

                    // TODO: Is it possible to setup this outside the loop ? Can figure out how, get this: Property (specularLightingUAV) at kernel index (21) is not set
                    cmd.SetComputeTextureParam(parameters.deferredComputeShader, kernel, HDShaderIDs.specularLightingUAV, resources.colorBuffers[0]);
                    cmd.SetComputeTextureParam(parameters.deferredComputeShader, kernel, HDShaderIDs.diffuseLightingUAV, resources.colorBuffers[1]);

                    cmd.SetComputeTextureParam(parameters.deferredComputeShader, kernel, HDShaderIDs._StencilTexture, resources.depthStencilBuffer, 0, RenderTextureSubElement.Stencil);

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
                    CoreUtils.SetRenderTarget(cmd, resources.colorBuffers, resources.depthStencilBuffer);
                else
                    CoreUtils.SetRenderTarget(cmd, resources.colorBuffers[0], resources.depthStencilBuffer);

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
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderDeferredLightingComputeAsPixel)))
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
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderDeferredLightingSinglePassMRT)))
                {
                    CoreUtils.DrawFullScreen(cmd, parameters.splitLightingMat, resources.colorBuffers, resources.depthStencilBuffer);
                }
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderDeferredLightingSinglePass)))
            {
                var currentLightingMaterial = parameters.regularLightingMat;
                // If SSS is disable, do lighting for both split lighting and no split lighting
                // This is for debug purpose, so fine to use immediate material mode here to modify render state
                if (!parameters.outputSplitLighting)
                {
                    currentLightingMaterial.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.Clear);
                    currentLightingMaterial.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.RequiresDeferredLighting | (int)StencilUsage.SubsurfaceScattering);
                    currentLightingMaterial.SetInt(HDShaderIDs._StencilCmp, (int)CompareFunction.NotEqual);
                }
                else
                {
                    currentLightingMaterial.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.RequiresDeferredLighting);
                    currentLightingMaterial.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.RequiresDeferredLighting);
                    currentLightingMaterial.SetInt(HDShaderIDs._StencilCmp, (int)CompareFunction.Equal);
                }

                CoreUtils.DrawFullScreen(cmd, currentLightingMaterial, resources.colorBuffers[0], resources.depthStencilBuffer);
            }
        }

        struct LightLoopDebugOverlayParameters
        {
            public Material                     debugViewTilesMaterial;
            public HDShadowManager              shadowManager;
            public int                          debugSelectedLightShadowIndex;
            public int                          debugSelectedLightShadowCount;
            public Material                     debugShadowMapMaterial;
            public Material                     debugBlitMaterial;
            public LightCookieManager           cookieManager;
            public PlanarReflectionProbeCache   planarProbeCache;
        }

        LightLoopDebugOverlayParameters PrepareLightLoopDebugOverlayParameters()
        {
            var parameters = new LightLoopDebugOverlayParameters();

            parameters.debugViewTilesMaterial = m_DebugViewTilesMaterial;
            parameters.shadowManager = m_ShadowManager;
            parameters.debugSelectedLightShadowIndex = m_DebugSelectedLightShadowIndex;
            parameters.debugSelectedLightShadowCount = m_DebugSelectedLightShadowCount;
            parameters.debugShadowMapMaterial = m_DebugHDShadowMapMaterial;
            parameters.debugBlitMaterial = m_DebugBlitMaterial;
            parameters.cookieManager = m_TextureCaches.lightCookieManager;
            parameters.planarProbeCache = m_TextureCaches.reflectionPlanarProbeCache;

            return parameters;
        }

        static void RenderLightLoopDebugOverlay(in DebugParameters  debugParameters,
                                                CommandBuffer       cmd,
                                                ComputeBuffer       tileBuffer,
                                                ComputeBuffer       lightListBuffer,
                                                ComputeBuffer       perVoxelLightListBuffer,
                                                ComputeBuffer       dispatchIndirectBuffer,
                                                RTHandle            depthTexture)
        {
            var hdCamera = debugParameters.hdCamera;
            var parameters = debugParameters.lightingOverlayParameters;
            LightingDebugSettings lightingDebug = debugParameters.debugDisplaySettings.data.lightingDebugSettings;
            if (lightingDebug.tileClusterDebug != TileClusterDebug.None)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.TileClusterLightingDebug)))
                {

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
                            parameters.debugViewTilesMaterial.SetBuffer(HDShaderIDs.g_TileList, tileBuffer);
                            parameters.debugViewTilesMaterial.SetBuffer(HDShaderIDs.g_DispatchIndirectBuffer, dispatchIndirectBuffer);
                            parameters.debugViewTilesMaterial.EnableKeyword("USE_FPTL_LIGHTLIST");
                            parameters.debugViewTilesMaterial.DisableKeyword("USE_CLUSTERED_LIGHTLIST");
                            parameters.debugViewTilesMaterial.DisableKeyword("SHOW_LIGHT_CATEGORIES");
                            parameters.debugViewTilesMaterial.EnableKeyword("SHOW_FEATURE_VARIANTS");
                            if (DeferredUseComputeAsPixel(hdCamera.frameSettings))
                                parameters.debugViewTilesMaterial.EnableKeyword("IS_DRAWPROCEDURALINDIRECT");
                            else
                                parameters.debugViewTilesMaterial.DisableKeyword("IS_DRAWPROCEDURALINDIRECT");
                            cmd.DrawProcedural(Matrix4x4.identity, parameters.debugViewTilesMaterial, 0, MeshTopology.Triangles, numTiles * 6);
                        }
                    }
                    else // tile or cluster
                    {
                        bool bUseClustered = lightingDebug.tileClusterDebug == TileClusterDebug.Cluster;

                        // lightCategories
                        parameters.debugViewTilesMaterial.SetInt(HDShaderIDs._ViewTilesFlags, (int)lightingDebug.tileClusterDebugByCategory);
                        parameters.debugViewTilesMaterial.SetInt(HDShaderIDs._ClusterDebugMode, bUseClustered ? (int)lightingDebug.clusterDebugMode : (int)ClusterDebugMode.VisualizeOpaque);
                        parameters.debugViewTilesMaterial.SetFloat(HDShaderIDs._ClusterDebugDistance, lightingDebug.clusterDebugDistance);
                        parameters.debugViewTilesMaterial.SetVector(HDShaderIDs._MousePixelCoord, HDUtils.GetMouseCoordinates(hdCamera));
                        parameters.debugViewTilesMaterial.SetVector(HDShaderIDs._MouseClickPixelCoord, HDUtils.GetMouseClickCoordinates(hdCamera));
                        parameters.debugViewTilesMaterial.SetBuffer(HDShaderIDs.g_vLightListGlobal, bUseClustered ? perVoxelLightListBuffer : lightListBuffer);
                        parameters.debugViewTilesMaterial.SetTexture(HDShaderIDs._CameraDepthTexture, depthTexture);
                        parameters.debugViewTilesMaterial.EnableKeyword(bUseClustered ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");
                        parameters.debugViewTilesMaterial.DisableKeyword(!bUseClustered ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");
                        parameters.debugViewTilesMaterial.EnableKeyword("SHOW_LIGHT_CATEGORIES");
                        parameters.debugViewTilesMaterial.DisableKeyword("SHOW_FEATURE_VARIANTS");
                        if (!bUseClustered && hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA))
                            parameters.debugViewTilesMaterial.EnableKeyword("DISABLE_TILE_MODE");
                        else
                            parameters.debugViewTilesMaterial.DisableKeyword("DISABLE_TILE_MODE");

                        CoreUtils.DrawFullScreen(cmd, parameters.debugViewTilesMaterial, 0);
                    }
                }
            }

            if (lightingDebug.displayCookieAtlas)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DisplayCookieAtlas)))
                {
                    m_LightLoopDebugMaterialProperties.SetFloat(HDShaderIDs._ApplyExposure, 0.0f);
                    m_LightLoopDebugMaterialProperties.SetFloat(HDShaderIDs._Mipmap, lightingDebug.cookieAtlasMipLevel);
                    m_LightLoopDebugMaterialProperties.SetTexture(HDShaderIDs._InputTexture, parameters.cookieManager.atlasTexture);
                    debugParameters.debugOverlay.SetViewport(cmd);
                    cmd.DrawProcedural(Matrix4x4.identity, parameters.debugBlitMaterial, 0, MeshTopology.Triangles, 3, 1, m_LightLoopDebugMaterialProperties);
                    debugParameters.debugOverlay.Next();
                }
            }

            if (lightingDebug.clearPlanarReflectionProbeAtlas)
            {
                parameters.planarProbeCache.Clear(cmd);
            }

            if (lightingDebug.displayPlanarReflectionProbeAtlas)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DisplayPlanarReflectionProbeAtlas)))
                {
                    m_LightLoopDebugMaterialProperties.SetFloat(HDShaderIDs._ApplyExposure, 1.0f);
                    m_LightLoopDebugMaterialProperties.SetFloat(HDShaderIDs._Mipmap, lightingDebug.planarReflectionProbeMipLevel);
                    m_LightLoopDebugMaterialProperties.SetTexture(HDShaderIDs._InputTexture, parameters.planarProbeCache.GetTexCache());
                    debugParameters.debugOverlay.SetViewport(cmd);
                    cmd.DrawProcedural(Matrix4x4.identity, parameters.debugBlitMaterial, 0, MeshTopology.Triangles, 3, 1, m_LightLoopDebugMaterialProperties);
                    debugParameters.debugOverlay.Next();
                }
            }
        }

        static void RenderShadowsDebugOverlay(in DebugParameters debugParameters, in HDShadowManager.ShadowDebugAtlasTextures atlasTextures, CommandBuffer cmd, MaterialPropertyBlock mpb)
        {
            if (HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxShadowRequests == 0)
                return;

            LightingDebugSettings lightingDebug = debugParameters.debugDisplaySettings.data.lightingDebugSettings;
            if (lightingDebug.shadowDebugMode != ShadowMapDebugMode.None)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DisplayShadows)))
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
                                parameters.shadowManager.DisplayShadowMap(atlasTextures, shadowIndex, cmd, parameters.debugShadowMapMaterial, debugParameters.debugOverlay.x, debugParameters.debugOverlay.y, debugParameters.debugOverlay.overlaySize, debugParameters.debugOverlay.overlaySize, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, mpb);
                                debugParameters.debugOverlay.Next();
                            }
                            break;
                        case ShadowMapDebugMode.VisualizePunctualLightAtlas:
                            parameters.shadowManager.DisplayShadowAtlas(atlasTextures.punctualShadowAtlas, cmd, parameters.debugShadowMapMaterial, debugParameters.debugOverlay.x, debugParameters.debugOverlay.y, debugParameters.debugOverlay.overlaySize, debugParameters.debugOverlay.overlaySize, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, mpb);
                            debugParameters.debugOverlay.Next();
                            break;
                        case ShadowMapDebugMode.VisualizeCachedPunctualLightAtlas:
                            parameters.shadowManager.DisplayCachedPunctualShadowAtlas(atlasTextures.cachedPunctualShadowAtlas, cmd, parameters.debugShadowMapMaterial, debugParameters.debugOverlay.x, debugParameters.debugOverlay.y, debugParameters.debugOverlay.overlaySize, debugParameters.debugOverlay.overlaySize, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, mpb);
                            debugParameters.debugOverlay.Next();
                            break;
                        case ShadowMapDebugMode.VisualizeDirectionalLightAtlas:
                            parameters.shadowManager.DisplayShadowCascadeAtlas(atlasTextures.cascadeShadowAtlas, cmd, parameters.debugShadowMapMaterial, debugParameters.debugOverlay.x, debugParameters.debugOverlay.y, debugParameters.debugOverlay.overlaySize, debugParameters.debugOverlay.overlaySize, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, mpb);
                            debugParameters.debugOverlay.Next();
                            break;
                        case ShadowMapDebugMode.VisualizeAreaLightAtlas:
                            parameters.shadowManager.DisplayAreaLightShadowAtlas(atlasTextures.areaShadowAtlas, cmd, parameters.debugShadowMapMaterial, debugParameters.debugOverlay.x, debugParameters.debugOverlay.y, debugParameters.debugOverlay.overlaySize, debugParameters.debugOverlay.overlaySize, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, mpb);
                            debugParameters.debugOverlay.Next();
                            break;
                        case ShadowMapDebugMode.VisualizeCachedAreaLightAtlas:
                            parameters.shadowManager.DisplayCachedAreaShadowAtlas(atlasTextures.cachedAreaShadowAtlas, cmd, parameters.debugShadowMapMaterial, debugParameters.debugOverlay.x, debugParameters.debugOverlay.y, debugParameters.debugOverlay.overlaySize, debugParameters.debugOverlay.overlaySize, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, mpb);
                            debugParameters.debugOverlay.Next();
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}

// Memo used when debugging to remember order of dispatch and value
// GenerateLightsScreenSpaceAABBs
// cmd.DispatchCompute(parameters.screenSpaceAABBShader, parameters.screenSpaceAABBKernel, (parameters.totalLightCount + 7) / 8, parameters.viewCount, 1);
// BigTilePrepass(ScreenX / 64, ScreenY / 64)
// cmd.DispatchCompute(parameters.bigTilePrepassShader, parameters.bigTilePrepassKernel, parameters.numBigTilesX, parameters.numBigTilesY, parameters.viewCount);
// BuildPerTileLightList(ScreenX / 16, ScreenY / 16)
// cmd.DispatchCompute(parameters.buildPerTileLightListShader, parameters.buildPerTileLightListKernel, parameters.numTilesFPTLX, parameters.numTilesFPTLY, parameters.viewCount);
// VoxelLightListGeneration(ScreenX / 32, ScreenY / 32)
// cmd.DispatchCompute(parameters.buildPerVoxelLightListShader, s_ClearVoxelAtomicKernel, 1, 1, 1);
// cmd.DispatchCompute(parameters.buildPerVoxelLightListShader, parameters.buildPerVoxelLightListKernel, parameters.numTilesClusterX, parameters.numTilesClusterY, parameters.viewCount);
// buildMaterialFlags (ScreenX / 16, ScreenY / 16)
// cmd.DispatchCompute(parameters.buildMaterialFlagsShader, buildMaterialFlagsKernel, parameters.numTilesFPTLX, parameters.numTilesFPTLY, parameters.viewCount);
// cmd.DispatchCompute(parameters.clearDispatchIndirectShader, s_ClearDispatchIndirectKernel, 1, 1, 1);
// BuildDispatchIndirectArguments
// cmd.DispatchCompute(parameters.buildDispatchIndirectShader, s_BuildDispatchIndirectKernel, (parameters.numTilesFPTL + k_ThreadGroupOptimalSize - 1) / k_ThreadGroupOptimalSize, 1, parameters.viewCount);
// Then dispatch indirect will trigger the number of tile for a variant x4 as we process by wavefront of 64 (16x16 => 4 x 8x8)
