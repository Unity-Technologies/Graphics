using System;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Collections;
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
            output.Forward = matrix.GetColumn(2);
            output.Up = matrix.GetColumn(1);
            output.Right = matrix.GetColumn(0);
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
        Decal,
        Count
    }

    [GenerateHLSL]
    internal enum LightFeatureFlags
    {
        // Light bit mask must match LightDefinitions.s_LightFeatureMaskFlags value
        Punctual = 1 << 12,
        Area = 1 << 13,
        Directional = 1 << 14,
        Env = 1 << 15,
        Sky = 1 << 16,
        SSRefraction = 1 << 17,
        SSReflection = 1 << 18,
        // If adding more light be sure to not overflow LightDefinitions.s_LightFeatureMaskFlags
    }

    [GenerateHLSL]
    internal enum WorldLightFlags : uint
    {
        None = 0u,
        Active = 1u,
        Raytracing = 2u,
        Pathtracing = 4u,
        ActiveRaytracing = Active | Raytracing,
        ActivePathtracing = Active | Pathtracing,
    }

    [GenerateHLSL]
    class LightDefinitions
    {
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

        // Following define the maximum number of bits use in each feature category.
        public static uint s_LightFeatureMaskFlags = 0xFFF000;
        public static uint s_LightFeatureMaskFlagsOpaque = 0xFFF000 & ~((uint)LightFeatureFlags.SSRefraction); // Opaque don't support screen space refraction
        public static uint s_LightFeatureMaskFlagsTransparent = 0xFFF000 & ~((uint)LightFeatureFlags.SSReflection); // Transparent don't support screen space reflection
        public static uint s_MaterialFeatureMaskFlags = 0x000FFF;   // don't use all bits just to be safe from signed and/or float conversions :/

        // Screen space shadow flags
        public static uint s_RayTracedScreenSpaceShadowFlag = 0x1000;
        public static uint s_ScreenSpaceColorShadowFlag = 0x100;
        public static uint s_InvalidScreenSpaceShadow = 0xff;
        public static uint s_ScreenSpaceShadowIndexMask = 0xff;

        //Contact shadow bit definitions
        public static int s_ContactShadowFadeBits = 8;
        public static int s_ContactShadowMaskBits = 32 - s_ContactShadowFadeBits;
        public static int s_ContactShadowFadeMask = (1 << s_ContactShadowFadeBits) - 1;
        public static int s_ContactShadowMaskMask = (1 << s_ContactShadowMaskBits) - 1;

    }

    [GenerateHLSL]
    struct SFiniteLightBound
    {
        public Vector3 boxAxisX; // Scaled by the extents (half-size)
        public Vector3 boxAxisY; // Scaled by the extents (half-size)
        public Vector3 boxAxisZ; // Scaled by the extents (half-size)
        public Vector3 center;   // Center of the bounds (box) in camera space
        public float scaleXY;  // Scale applied to the top of the box to turn it into a truncated pyramid (X = Y)
        public float radius;     // Circumscribed sphere for the bounds (box)
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
        public int affectVolumetric; // 1 if the volumetric dimmer of the light is above 0
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
        Punctual = (1 << LightCategory.Punctual),
        /// <summary>Area lights.</summary>
        Area = (1 << LightCategory.Area),
        /// <summary>Area and punctual lights.</summary>
        [InspectorName("Area and Punctual")]
        AreaAndPunctual = Area | Punctual,
        /// <summary>Environment lights.</summary>
        [InspectorName("Reflection Probes")]
        Environment = (1 << LightCategory.Env),
        /// <summary>Environment and punctual lights.</summary>
        [InspectorName("Reflection Probes and Punctual")]
        EnvironmentAndPunctual = Environment | Punctual,
        /// <summary>Environment and area lights.</summary>
        [InspectorName("Reflection Probes and Area")]
        EnvironmentAndArea = Environment | Area,
        /// <summary>All lights.</summary>
        [InspectorName("Reflection Probes, Area and Punctual")]
        EnvironmentAndAreaAndPunctual = Environment | Area | Punctual,
        /// <summary>Decals.</summary>
        Decal = (1 << LightCategory.Decal),
        /// <summary>Local Volumetric Fog.</summary>
        [Obsolete("Unused")]
        LocalVolumetricFog = 0,
        /// <summary>Local Volumetric Fog.</summary>
        [Obsolete("Unused", true)]
        [InspectorName("Local Volumetric Fog")]
        DensityVolumes = LocalVolumetricFog
    };

    /// <summary>
    /// Light category for cluster debug view.
    /// </summary>
    public enum ClusterLightCategoryDebug : int
    {
        /// <summary>Punctual lights.</summary>
        Punctual = LightCategory.Punctual,
        /// <summary>Area lights.</summary>
        Area = LightCategory.Area,
        /// <summary>Environment lights.</summary>
        Environment = LightCategory.Env,
        /// <summary>Decals.</summary>
        Decal = LightCategory.Decal,
        /// <summary>All.</summary>
        All = LightCategory.Count,
    };

    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesLightList
    {
        [HLSLArray(ShaderConfig.k_XRMaxViewsForCBuffer, typeof(Matrix4x4))]
        public fixed float g_mInvScrProjectionArr[ShaderConfig.k_XRMaxViewsForCBuffer * 16];
        [HLSLArray(ShaderConfig.k_XRMaxViewsForCBuffer, typeof(Matrix4x4))]
        public fixed float g_mScrProjectionArr[ShaderConfig.k_XRMaxViewsForCBuffer * 16];
        [HLSLArray(ShaderConfig.k_XRMaxViewsForCBuffer, typeof(Matrix4x4))]
        public fixed float g_mInvProjectionArr[ShaderConfig.k_XRMaxViewsForCBuffer * 16];
        [HLSLArray(ShaderConfig.k_XRMaxViewsForCBuffer, typeof(Matrix4x4))]
        public fixed float g_mProjectionArr[ShaderConfig.k_XRMaxViewsForCBuffer * 16];

        public Vector4 g_screenSize;

        public Vector2Int g_viDimensions;
        public int g_iNrVisibLights;
        public uint g_isOrthographic;

        public uint g_BaseFeatureFlags;
        public int g_iNumSamplesMSAA;
        public uint _EnvLightIndexShift;
        public uint _DecalIndexShift;
    }

    internal struct ProcessedProbeData
    {
        public HDProbe hdProbe;
        public float weight;
    }

    public partial class HDRenderPipeline
    {
        internal const int k_MaxCacheSize = 2000000000; //2 GigaByte
        internal const int k_MaxDirectionalLightsOnScreen = 512;
        internal const int k_MaxPunctualLightsOnScreen = 2048;
        internal const int k_MaxAreaLightsOnScreen = 1024;
        internal const int k_MaxDecalsOnScreen = 2048;
        internal const int k_MaxPlanarReflectionsOnScreen = 16;
        internal const int k_MaxCubeReflectionsOnScreen = 128;
        internal const int k_MaxLightsPerClusterCell = ShaderConfig.LightClusterMaxCellElementCount;
        internal static readonly Vector3 k_BoxCullingExtentThreshold = Vector3.one * 0.01f;

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
        int m_MaxPlanarReflectionsOnScreen;
        int m_MaxCubeReflectionsOnScreen;
        int m_MaxEnvLightsOnScreen;

        internal class LightLoopTextureCaches
        {
            // Structure for cookies used by directional and spotlights
            public LightCookieManager lightCookieManager { get; private set; }
            public ReflectionProbeTextureCache reflectionProbeTextureCache { get; private set; }

            public void Initialize(HDRenderPipelineAsset hdrpAsset, HDRenderPipeline renderPipeline, IBLFilterBSDF[] iBLFilterBSDFArray)
            {
                var lightLoopSettings = hdrpAsset.currentPlatformRenderPipelineSettings.lightLoopSettings;

                lightCookieManager = new LightCookieManager(hdrpAsset, renderPipeline, k_MaxCacheSize);

                // For regular reflection probes, we need to convolve with all the BSDF functions
                GraphicsFormat probeCacheFormat = lightLoopSettings.reflectionProbeFormat == ReflectionAndPlanarProbeFormat.R11G11B10 ?
                    GraphicsFormat.B10G11R11_UFloatPack32 : GraphicsFormat.R16G16B16A16_SFloat;

                // BC6H requires CPP feature not yet available
                //if (lightLoopSettings.reflectionCacheCompressed)
                //{
                //    probeCacheFormat = GraphicsFormat.RGB_BC6H_SFloat;
                //}

                Vector2Int cacheDim = GlobalLightLoopSettings.GetReflectionProbeTextureCacheDim(lightLoopSettings.reflectionProbeTexCacheSize);

                reflectionProbeTextureCache = new ReflectionProbeTextureCache(renderPipeline, iBLFilterBSDFArray, cacheDim.x, cacheDim.y, probeCacheFormat,
                    lightLoopSettings.reflectionProbeDecreaseResToFit, lightLoopSettings.reflectionProbeTexLastValidCubeMip, lightLoopSettings.reflectionProbeTexLastValidPlanarMip);
            }

            public void Cleanup()
            {
                reflectionProbeTextureCache.Release();
                lightCookieManager.Release();
            }

            public void NewFrame()
            {
                lightCookieManager.NewFrame();
                reflectionProbeTextureCache.NewFrame();
            }

            public void NewRender()
            {
                reflectionProbeTextureCache.NewRender();
            }
        }

        internal class LightLoopLightData
        {
            public GraphicsBuffer directionalLightData { get; private set; }
            public GraphicsBuffer lightData { get; private set; }
            public GraphicsBuffer envLightData { get; private set; }
            public GraphicsBuffer decalData { get; private set; }

            public void Initialize(int directionalCount, int punctualCount, int areaLightCount, int envLightCount, int decalCount)
            {
                directionalLightData = new GraphicsBuffer(GraphicsBuffer.Target.Structured, directionalCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalLightData)));
                lightData = new GraphicsBuffer(GraphicsBuffer.Target.Structured, punctualCount + areaLightCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LightData)));
                envLightData = new GraphicsBuffer(GraphicsBuffer.Target.Structured, envLightCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(EnvLightData)));
                decalData = new GraphicsBuffer(GraphicsBuffer.Target.Structured, decalCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DecalData)));
            }

            public void Cleanup()
            {
                CoreUtils.SafeRelease(directionalLightData);
                CoreUtils.SafeRelease(lightData);
                CoreUtils.SafeRelease(envLightData);
                CoreUtils.SafeRelease(decalData);
            }
        }

        // TODO RENDERGRAPH: When we remove the old path, we need to remove/refactor this class
        // With render graph it's only useful for 3 buffers and a boolean value.
        class TileAndClusterData
        {
            // Internal to light list building
            public GraphicsBuffer lightVolumeDataBuffer { get; private set; }
            public GraphicsBuffer convexBoundsBuffer { get; private set; }

            public bool listsAreClear = false;

            public bool clusterNeedsDepth { get; private set; }
            public bool hasTileBuffers { get; private set; }
            public int maxLightCount { get; private set; }

            public void Initialize(bool allocateTileBuffers, bool clusterNeedsDepth, int maxLightCount)
            {
                hasTileBuffers = allocateTileBuffers;
                this.clusterNeedsDepth = clusterNeedsDepth;
                this.maxLightCount = maxLightCount;
            }

            public void AllocateResolutionDependentBuffers(HDCamera hdCamera, int width, int height, int viewCount, int maxLightOnScreen)
            {
                convexBoundsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, viewCount * maxLightOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SFiniteLightBound)));
                lightVolumeDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, viewCount * maxLightOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LightVolumeData)));

                // Make sure to invalidate the content of the buffers
                listsAreClear = false;
            }

            public void ReleaseResolutionDependentBuffers()
            {
                CoreUtils.SafeRelease(convexBoundsBuffer);
                CoreUtils.SafeRelease(lightVolumeDataBuffer);
                convexBoundsBuffer = null;
                lightVolumeDataBuffer = null;
            }

            public void Cleanup()
            {
                ReleaseResolutionDependentBuffers();
            }
        }

        // TODO: Remove the internal
        internal LightLoopTextureCaches m_TextureCaches = new LightLoopTextureCaches();
        // TODO: Remove the internal
        internal LightLoopLightData m_LightLoopLightData = new LightLoopLightData();
        TileAndClusterData m_TileAndClusterData = new TileAndClusterData();

        EnvLightReflectionData m_EnvLightReflectionData = new EnvLightReflectionData();

        // This control if we use cascade borders for directional light by default
        static internal readonly bool s_UseCascadeBorders = true;

        // Keep sorting array around to avoid garbage
        ulong[] m_SortKeys = null;
        DynamicArray<ProcessedProbeData> m_ProcessedReflectionProbeData = new DynamicArray<ProcessedProbeData>();
        DynamicArray<ProcessedProbeData> m_ProcessedPlanarProbeData = new DynamicArray<ProcessedProbeData>();

        void UpdateSortKeysArray(int count)
        {
            if (m_SortKeys == null || count > m_SortKeys.Length)
            {
                m_SortKeys = new ulong[count];
            }
        }

        static readonly Matrix4x4 s_FlipMatrixLHSRHS = Matrix4x4.Scale(new Vector3(1, 1, -1));

        internal static Matrix4x4 GetWorldToViewMatrix(HDCamera hdCamera, int viewIndex)
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
            public List<EnvLightData> envLights;
            public void Clear()
            {
                envLights.Clear();
            }

            public void Allocate()
            {
                envLights = new List<EnvLightData>();
            }
        }

        internal LightList m_lightList;
        internal HDProcessedVisibleLightsBuilder m_ProcessedLightsBuilder;
        internal HDGpuLightsBuilder m_GpuLightsBuilder;

        internal HDGpuLightsBuilder gpuLightList => m_GpuLightsBuilder;

        int m_TotalLightCount = 0;
        bool m_EnableBakeShadowMask = false; // Track if any light require shadow mask. In this case we will need to enable the keyword shadow mask

        ComputeShader buildScreenAABBShader => runtimeShaders.buildScreenAABBCS;
        ComputeShader buildPerTileLightListShader => runtimeShaders.buildPerTileLightListCS;
        ComputeShader buildPerBigTileLightListShader => runtimeShaders.buildPerBigTileLightListCS;
        ComputeShader buildPerVoxelLightListShader => runtimeShaders.buildPerVoxelLightListCS;
        ComputeShader clearClusterAtomicIndexShader => runtimeShaders.lightListClusterClearAtomicIndexCS;
        ComputeShader buildMaterialFlagsShader => runtimeShaders.buildMaterialFlagsCS;
        ComputeShader buildDispatchIndirectShader => runtimeShaders.buildDispatchIndirectCS;
        ComputeShader clearDispatchIndirectShader => runtimeShaders.clearDispatchIndirectCS;
        ComputeShader deferredComputeShader => runtimeShaders.deferredCS;
        ComputeShader contactShadowComputeShader => runtimeShaders.contactShadowCS;
        Shader screenSpaceShadowsShader => runtimeShaders.screenSpaceShadowPS;

        ShaderVariablesLightList m_ShaderVariablesLightListCB = new ShaderVariablesLightList();

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
        const int k_Log2NumClusters = 5;     // accepted range is from 0 to 5 (NR_THREADS is set to 32). NumClusters is 1<<g_iLog2NumClusters
#else
        const int k_Log2NumClusters = 6;     // accepted range is from 0 to 6 (NR_THREADS is set to 64). NumClusters is 1<<g_iLog2NumClusters
#endif
        const float k_ClustLogBase = 1.02f;     // each slice 2% bigger than the previous
        float m_ClusterScale;

        static DebugLightVolumes s_lightVolumes = null;


        ContactShadows m_ContactShadows = null;
        bool m_EnableContactShadow = false;

        IndirectLightingController m_indirectLightingController = null;

        HashSet<HDAdditionalLightData> m_ScreenSpaceShadowsUnion = new HashSet<HDAdditionalLightData>();

        // Directional light
        Light m_CurrentSunLight;
        int m_CurrentSunLightDataIndex = -1;
        int m_CurrentShadowSortedSunLightIndex = -1;
        HDAdditionalLightData m_CurrentSunLightAdditionalLightData;
        HDProcessedVisibleLightsBuilder.ShadowMapFlags m_CurrentSunShadowMapFlags = HDProcessedVisibleLightsBuilder.ShadowMapFlags.None;
        DirectionalLightData m_CurrentSunLightDirectionalLightData;

        /// <summary>
        /// Main directional Light for the HD Render Pipeline.
        /// </summary>
        /// <returns>The main directional Light.</returns>
        public Light GetMainLight() { return m_CurrentSunLight; }
        internal HDAdditionalLightData GetMainLightAdditionalData() { return m_CurrentSunLightAdditionalLightData; }

        // Screen space shadow data
        internal struct ScreenSpaceShadowData
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

        void InitShadowSystem(HDRenderPipelineAsset hdAsset)
        {
            m_ShadowInitParameters = hdAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams;
            m_ShadowManager = HDShadowManager.instance;
            m_ShadowManager.InitShadowManager(this, m_ShadowInitParameters, m_RenderGraph);
        }

        void DeinitShadowSystem()
        {
            if (m_ShadowManager != null)
            {
                m_ShadowManager.Cleanup(m_RenderGraph);
                m_ShadowManager = null;
            }
        }

        static bool GetFeatureVariantsEnabled(FrameSettings frameSettings) =>
            frameSettings.litShaderMode == LitShaderMode.Deferred
            && (frameSettings.IsEnabled(FrameSettingsField.ComputeLightVariants) || frameSettings.IsEnabled(FrameSettingsField.ComputeMaterialVariants));

        void InitializeLightLoop(IBLFilterBSDF[] iBLFilterBSDFArray)
        {
            var lightLoopSettings = asset.currentPlatformRenderPipelineSettings.lightLoopSettings;

            m_lightList = new LightList();
            m_lightList.Allocate();

            m_ProcessedLightsBuilder = new HDProcessedVisibleLightsBuilder();
            m_GpuLightsBuilder = new HDGpuLightsBuilder();

            m_MaxDirectionalLightsOnScreen = lightLoopSettings.maxDirectionalLightsOnScreen;
            m_MaxPunctualLightsOnScreen = lightLoopSettings.maxPunctualLightsOnScreen;
            m_MaxAreaLightsOnScreen = lightLoopSettings.maxAreaLightsOnScreen;
            m_MaxDecalsOnScreen = lightLoopSettings.maxDecalsOnScreen;
            m_MaxPlanarReflectionsOnScreen = Math.Min(lightLoopSettings.maxPlanarReflectionOnScreen, HDRenderPipeline.k_MaxPlanarReflectionsOnScreen);
            m_MaxCubeReflectionsOnScreen = Math.Min(lightLoopSettings.maxCubeReflectionOnScreen, HDRenderPipeline.k_MaxCubeReflectionsOnScreen);
            m_MaxEnvLightsOnScreen = m_MaxPlanarReflectionsOnScreen + m_MaxCubeReflectionsOnScreen;
            m_MaxLightsOnScreen = m_MaxDirectionalLightsOnScreen + m_MaxPunctualLightsOnScreen + m_MaxAreaLightsOnScreen + m_MaxEnvLightsOnScreen;

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

            s_BuildMaterialFlagsWriteKernel = buildMaterialFlagsShader.FindKernel("MaterialFlagsGen");

            s_shadeOpaqueDirectFptlKernel = deferredComputeShader.FindKernel("Deferred_Direct_Fptl");
            s_shadeOpaqueDirectFptlDebugDisplayKernel = deferredComputeShader.FindKernel("Deferred_Direct_Fptl_DebugDisplay");

            s_deferredContactShadowKernel = contactShadowComputeShader.FindKernel("DeferredContactShadow");

            for (int variant = 0; variant < LightDefinitions.s_NumFeatureVariants; variant++)
            {
                s_shadeOpaqueIndirectFptlKernels[variant] = deferredComputeShader.FindKernel("Deferred_Indirect_Fptl_Variant" + variant);
            }

            m_TextureCaches.Initialize(asset, this, iBLFilterBSDFArray);

            // All the allocation of the compute buffers need to happened after the kernel finding in order to avoid the leak loop when a shader does not compile or is not available
            m_LightLoopLightData.Initialize(m_MaxDirectionalLightsOnScreen, m_MaxPunctualLightsOnScreen, m_MaxAreaLightsOnScreen, m_MaxEnvLightsOnScreen, m_MaxDecalsOnScreen);

            m_TileAndClusterData.Initialize(allocateTileBuffers: true, clusterNeedsDepth: k_UseDepthBuffer, maxLightCount: m_MaxLightsOnScreen + m_MaxDecalsOnScreen); // both light and decals are stored in the cluster

            // Setup shadow algorithms
            var shadowParams = asset.currentPlatformRenderPipelineSettings.hdShadowInitParams;
            var punctualShadowKeywords = new[] { "PUNCTUAL_SHADOW_LOW", "PUNCTUAL_SHADOW_MEDIUM", "PUNCTUAL_SHADOW_HIGH" };
            foreach (var p in punctualShadowKeywords)
                Shader.DisableKeyword(p);
            Shader.EnableKeyword(punctualShadowKeywords[(int)shadowParams.punctualShadowFilteringQuality]);

            var directionalSadowKeywords = new[] { "DIRECTIONAL_SHADOW_LOW", "DIRECTIONAL_SHADOW_MEDIUM", "DIRECTIONAL_SHADOW_HIGH" };
            foreach (var p in directionalSadowKeywords)
                Shader.DisableKeyword(p);
            Shader.EnableKeyword(directionalSadowKeywords[(int)shadowParams.directionalShadowFilteringQuality]);

            var areaShadowKeywords = new[] { "AREA_SHADOW_MEDIUM", "AREA_SHADOW_HIGH" };
            foreach (var p in areaShadowKeywords)
                Shader.DisableKeyword(p);
            Shader.EnableKeyword(areaShadowKeywords[(int)shadowParams.areaShadowFilteringQuality]);

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

            InitShadowSystem(asset);

            m_GpuLightsBuilder.Initialize(m_Asset, m_ShadowManager, m_TextureCaches);

            s_lightVolumes = new DebugLightVolumes();
            s_lightVolumes.InitData(this);

            // Screen space shadow
            int numMaxShadows = Math.Max(m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots, 1);
            m_CurrentScreenSpaceShadowData = new ScreenSpaceShadowData[numMaxShadows];

            // Surface gradient decal blending
            if (asset.currentPlatformRenderPipelineSettings.supportDecals && asset.currentPlatformRenderPipelineSettings.supportSurfaceGradient)
                Shader.EnableKeyword("DECAL_SURFACE_GRADIENT");
            else
                Shader.DisableKeyword("DECAL_SURFACE_GRADIENT");

            s_BigTileVolumetricLightListKeyword = new LocalKeyword(buildPerBigTileLightListShader, "GENERATE_VOLUMETRIC_BIGTILE");
        }

        void CleanupLightLoop()
        {
            s_lightVolumes.ReleaseData();

            DeinitShadowSystem();

            m_TextureCaches.Cleanup();
            m_LightLoopLightData.Cleanup();
            m_TileAndClusterData.Cleanup();

            LightLoopReleaseResolutionDependentBuffers();

            m_ProcessedLightsBuilder.Cleanup();
            m_GpuLightsBuilder.Cleanup();
        }

        void LightLoopNewRender()
        {
            m_ScreenSpaceShadowsUnion.Clear();

            m_TextureCaches.NewRender();
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

            if (m_DebugDisplaySettings.data.lightingDebugSettings.clearReflectionProbeAtlas)
            {
                m_TextureCaches.reflectionProbeTextureCache.Clear(cmd);
            }

            bool supportProbeVolume = m_Asset != null && m_Asset.currentPlatformRenderPipelineSettings.supportProbeVolume;
            ProbeReferenceVolume.instance.SetEnableStateFromSRP(supportProbeVolume);
            // We need to verify and flush any pending asset loading for probe volume.
            if (supportProbeVolume && ProbeReferenceVolume.instance.isInitialized)
            {
                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.AdaptiveProbeVolume))
                {
                    ProbeReferenceVolume.instance.PerformPendingOperations();
                    if (hdCamera.camera.cameraType != CameraType.Reflection &&
                        hdCamera.camera.cameraType != CameraType.Preview)
                    {
                        // TODO: Move this to one call for all cameras
                        ProbeReferenceVolume.instance.UpdateCellStreaming(cmd, hdCamera.camera, hdCamera.volumeStack.GetComponent<ProbeVolumesOptions>());
                    }
                }
            }
        }

        static int NumLightIndicesPerClusteredTile()
        {
            return ShaderConfig.FPTLMaxLightCount * (1 << k_Log2NumClusters);       // total footprint for all layers of the tile (measured in light index entries)
        }

        void LightLoopAllocResolutionDependentBuffers(HDCamera hdCamera, int width, int height)
        {
            m_TileAndClusterData.AllocateResolutionDependentBuffers(hdCamera, width, height, m_MaxViewCount, m_MaxLightsOnScreen + m_MaxDecalsOnScreen);
        }

        void LightLoopReleaseResolutionDependentBuffers()
        {
            m_TileAndClusterData.ReleaseResolutionDependentBuffers();
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

        // This function evaluates if there is currently enough screen space sahdow slots of a given light based on its light type
        bool EnoughScreenSpaceShadowSlots(GPULightType gpuLightType, int screenSpaceChannelSlot)
        {
            if (gpuLightType == GPULightType.Rectangle)
            {
                // Area lights require two shadow slots
                return (screenSpaceChannelSlot + 1) < m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots;
            }
            else
            {
                return screenSpaceChannelSlot < m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots;
            }
        }

        internal bool GetEnvLightData(CommandBuffer cmd, HDCamera hdCamera, in ProcessedProbeData processedProbe, ref EnvLightData envLightData,
            out int fetchIndex, out Vector4 scaleOffset, out Matrix4x4 vp)
        {
            // Initialize the fetch index
            fetchIndex = -1;
            scaleOffset = Vector4.zero;
            vp = Matrix4x4.identity;

            // By default, rough reflections are enabled for both types of probes.
            envLightData.roughReflections = 1.0f;
            envLightData.distanceBasedRoughness = 0.0f;

            Camera camera = hdCamera.camera;
            HDProbe probe = processedProbe.hdProbe;

            // Skip the probe if the probe has never rendered (in realtime cases) or if texture is null
            if (!probe.HasValidRenderedData() || probe.IsTurnedOff()) return false;

            var capturePosition = Vector3.zero;
            var influenceToWorld = probe.influenceToWorld;

            // 31 bits index, 1 bit cache type
            var envIndex = int.MinValue;
            switch (probe)
            {
                case PlanarReflectionProbe planarProbe:
                {
                    if ((probe.mode == ProbeSettings.Mode.Realtime
                        && !hdCamera.frameSettings.IsEnabled(FrameSettingsField.PlanarProbe)))
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
                    vp = gpuProj * worldToCameraRHSMatrix;
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
                    scaleOffset = m_TextureCaches.reflectionProbeTextureCache.FetchPlanarReflectionProbe(cmd, probe, ref planarTextureFilteringParameters, out fetchIndex);

                    // We don't need to provide the capture position
                    // It is already encoded in the 'worldToCameraRHSMatrix'
                    capturePosition = Vector3.zero;

                    // Indices start at 1, because -0 == 0, we can know from the bit sign which cache to use
                    envIndex = scaleOffset == Vector4.zero ? int.MinValue : -(fetchIndex + 1);

                    // If the max number of planar on screen is reached
                    if (fetchIndex >= m_MaxPlanarReflectionsOnScreen)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.LogWarning("Maximum planar reflection probes on screen reached. To fix this error, increase the 'Maximum Planar Reflection Probes on Screen' property in the HDRP asset.");
#endif
                        break;
                    }

                    // Propagate the smoothness information to the env light data
                    envLightData.roughReflections = probe.settings.roughReflections ? 1.0f : 0.0f;

                    //We must use the setting resolved from the probe, not from the frameSettings.
                    //Using the frmaeSettings from the probe is wrong because it can be disabled (not ticking on using custom frame settings in the probe reflection component)
                    if (probe.ExposureControlEnabled)
                        envLightData.rangeCompressionFactorCompensation = 1.0f / probe.ProbeExposureValue();
                    else
                        envLightData.rangeCompressionFactorCompensation = Mathf.Max(probe.rangeCompressionFactor, 1e-6f);
                    break;
                }
                case HDAdditionalReflectionData reflectionData:
                {
                    // Fetch the slice and do the filtering
                    scaleOffset = m_TextureCaches.reflectionProbeTextureCache.FetchCubeReflectionProbe(cmd, probe, out fetchIndex);

                    // Indices start at 1, because -0 == 0, we can know from the bit sign which cache to use
                    envIndex = scaleOffset == Vector4.zero ? int.MinValue : (fetchIndex + 1);

                    // If the max number of reflection probes on screen is reached
                    if (fetchIndex >= m_MaxCubeReflectionsOnScreen)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.LogWarning("Maximum reflection probes on screen reached. To fix this error, increase the 'Maximum Cube Reflection Probes on Screen' property in the HDRP asset.");
#endif
                        break;
                    }

                    // Calculate settings to use for the probe
                    var probePositionSettings = ProbeCapturePositionSettings.ComputeFrom(probe, camera.transform);
                    HDRenderUtilities.ComputeCameraSettingsFromProbeSettings(
                        probe.settings, probePositionSettings,
                        out _, out var cameraPositionSettings, 0
                    );
                    capturePosition = cameraPositionSettings.position;
                    envLightData.rangeCompressionFactorCompensation = Mathf.Max(probe.rangeCompressionFactor, 1e-6f);

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

            envLightData.normalizeWithAPV = hdCamera.frameSettings.IsEnabled(FrameSettingsField.NormalizeReflectionProbeWithProbeVolume) ? 1 : 0;
            if (envLightData.normalizeWithAPV > 0)
            {
                if (!probe.GetSHForNormalization(out envLightData.L0L1, out envLightData.L2_1, out envLightData.L2_2))
                {
                    // We don't have valid data, hence we disable the feature.
                    envLightData.normalizeWithAPV = 0;
                }
            }

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
                    bound.scaleXY = 1.0f;
                    bound.radius = influenceExtents.x;
                    break;
                }
                case LightVolumeType.Box:
                {
                    bound.center = influencePositionVS;
                    bound.boxAxisX = influenceExtents.x * influenceRightVS;
                    bound.boxAxisY = influenceExtents.y * influenceUpVS;
                    bound.boxAxisZ = influenceExtents.z * influenceForwardVS;
                    bound.scaleXY = 1.0f;
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

            m_GpuLightsBuilder.AddLightBounds(viewIndex, bound, lightVolumeData);
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
            var rightVS = worldToView.MultiplyVector(obb.right);
            var upVS = worldToView.MultiplyVector(obb.up);
            var forwardVS = Vector3.Cross(upVS, rightVS);
            var extents = new Vector3(extentConservativeX, extentConservativeY, extentConservativeZ);

            volumeData.lightVolume = (uint)LightVolumeType.Box;
            volumeData.lightCategory = (uint)category;
            volumeData.featureFlags = (uint)featureFlags;

            bound.center = positionVS;
            bound.boxAxisX = extentConservativeX * rightVS;
            bound.boxAxisY = extentConservativeY * upVS;
            bound.boxAxisZ = extentConservativeZ * forwardVS;
            bound.radius = extentConservativeMagnitude;
            bound.scaleXY = 1.0f;

            // The culling system culls pixels that are further
            //   than a threshold to the box influence extents.
            // So we use an arbitrary threshold here (k_BoxCullingExtentOffset)
            volumeData.lightPos = positionVS;
            volumeData.lightAxisX = rightVS;
            volumeData.lightAxisY = upVS;
            volumeData.lightAxisZ = forwardVS;
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

            // In HDRP we don't need per object light/probe info so we disable the native code that handles it.
            cullingParams.cullingOptions |= CullingOptions.DisablePerObjectCulling;
            cullingParams.shadowDistance = Mathf.Min(shadowMaxDistance, cullingParams.shadowDistance);
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

        internal static void EvaluateGPULightType(LightType lightType, ref LightCategory lightCategory,
            ref GPULightType gpuLightType, ref LightVolumeType lightVolumeType)
        {
            lightCategory = LightCategory.Count;
            gpuLightType = GPULightType.Point;
            lightVolumeType = LightVolumeType.Count;

            switch (lightType)
            {
                case LightType.Spot:
                    lightCategory = LightCategory.Punctual;
                    gpuLightType = GPULightType.Spot;
                    lightVolumeType = LightVolumeType.Cone;
                    break;

                case LightType.Pyramid:
                    lightCategory = LightCategory.Punctual;
                    gpuLightType = GPULightType.ProjectorPyramid;
                    lightVolumeType = LightVolumeType.Cone;
                    break;

                case LightType.Box:
                    lightCategory = LightCategory.Punctual;
                    gpuLightType = GPULightType.ProjectorBox;
                    lightVolumeType = LightVolumeType.Box;
                    break;

                case LightType.Directional:
                    lightCategory = LightCategory.Punctual;
                    gpuLightType = GPULightType.Directional;
                    // No need to add volume, always visible
                    lightVolumeType = LightVolumeType.Count; // Count is none
                    break;

                case LightType.Point:
                    lightCategory = LightCategory.Punctual;
                    gpuLightType = GPULightType.Point;
                    lightVolumeType = LightVolumeType.Sphere;
                    break;

                case LightType.Rectangle:
                    lightCategory = LightCategory.Area;
                    gpuLightType = GPULightType.Rectangle;
                    lightVolumeType = LightVolumeType.Box;
                    break;

                case LightType.Tube:
                    lightCategory = LightCategory.Area;
                    gpuLightType = GPULightType.Tube;
                    lightVolumeType = LightVolumeType.Box;
                    break;

                case LightType.Disc:
                    lightCategory = LightCategory.Area;
                    //not used in real-time at the moment anyway, except for path tracing
                    gpuLightType = GPULightType.Disc;
                    lightVolumeType = LightVolumeType.Sphere;
                    break;

                default:
                    Debug.Assert(false, "Encountered an unknown LightType.");
                    break;
            }
        }

        private static void CullShadowCasters(ScriptableRenderContext renderContext,
            in HDShadowInitParameters hdShadowInitParams,
            HDShadowManager shadowManager,
            HDCamera hdCamera,
            HDProcessedVisibleLightsBuilder processedVisibleLights,
            in CullingResults cullingResult)
        {
            HDLightRenderDatabase lightRenderDatabase = HDLightRenderDatabase.instance;

            HDShadowRequestDatabase shadowRequestDatabase = HDShadowRequestDatabase.instance;
            if (!shadowRequestDatabase.IsCreated)
                return;

            HDShadowSettings shadowSettings = hdCamera.volumeStack.GetComponent<HDShadowSettings>();
            if (shadowSettings == null)
                return;

            int shadowLightCount = processedVisibleLights.shadowLightCount;
            if (shadowLightCount == 0)
                return;

            NativeArray<LightShadowCasterCullingInfo> perLightShadowCullingInfos = new NativeArray<LightShadowCasterCullingInfo>(cullingResult.visibleLights.Length, Allocator.TempJob);
            NativeArray<ShadowSplitData> splitBuffer = new NativeArray<ShadowSplitData>(shadowLightCount * HDShadowUtils.k_MaxShadowSplitCount, Allocator.TempJob);
            int totalSplitCount;

            HDShadowCullingUtils.ComputeCullingSplits(hdShadowInitParams,
                lightRenderDatabase,
                shadowRequestDatabase,
                shadowManager,
                shadowSettings,
                cullingResult,
                processedVisibleLights,
                perLightShadowCullingInfos,
                splitBuffer,
                out totalSplitCount);

            ShadowCastersCullingInfos cullingInfos = default;
            cullingInfos.splitBuffer = splitBuffer.GetSubArray(0, totalSplitCount);
            cullingInfos.perLightInfos = perLightShadowCullingInfos;
            renderContext.CullShadowCasters(cullingResult, cullingInfos);

            splitBuffer.Dispose();
            perLightShadowCullingInfos.Dispose();
        }

        // Compute data that will be used during the light loop for a particular light.
        void PreprocessVisibleLights(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, in CullingResults cullResults, DebugDisplaySettings debugDisplaySettings, in AOVRequestData aovRequest)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ProcessVisibleLights)))
            {
                var lightLoopSettings = asset.currentPlatformRenderPipelineSettings.lightLoopSettings;
                m_ProcessedLightsBuilder.Build(
                    hdCamera,
                    cullResults,
                    GetRayTracingState(),
                    m_ShadowManager,
                    m_ShadowInitParameters,
                    aovRequest,
                    lightLoopSettings,
                    m_CurrentDebugDisplaySettings);

                CullShadowCasters(renderContext, m_ShadowInitParameters, m_ShadowManager, hdCamera, m_ProcessedLightsBuilder, cullResults);

                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ProcessDirectionalAndCookies)))
                {
                    int visibleLightCounts = m_ProcessedLightsBuilder.sortedLightCounts;
                    var lightEntities = HDLightRenderDatabase.instance;
                    for (int i = 0; i < visibleLightCounts; ++i)
                    {
                        uint sortKey = m_ProcessedLightsBuilder.sortKeys[i];
                        HDGpuLightsBuilder.UnpackLightSortKey(sortKey, out var _, out var _, out var _, out var lightIndex, out var _);
                        HDProcessedVisibleLight processedLightEntity = m_ProcessedLightsBuilder.processedEntities[lightIndex];
                        HDAdditionalLightData additionalLightData = lightEntities.hdAdditionalLightData[processedLightEntity.dataIndex];
                        if (additionalLightData == null)
                            continue;

                        if (processedLightEntity.gpuLightType == GPULightType.Directional)
                        {
                            // Sunlight is the directional casting shadows
                            // Fallback to the first non shadow casting directional light.
                            if (additionalLightData.ShadowsEnabled() || m_CurrentSunLight == null)
                            {
                                m_CurrentSunLightDataIndex = i;
                                m_CurrentSunLight = additionalLightData.legacyLight;
                                m_CurrentSunLightAdditionalLightData = additionalLightData;
                            }
                        }

                        ReserveCookieAtlasTexture(additionalLightData, additionalLightData.legacyLight, processedLightEntity.lightType);
                    }

                    if (hdCamera.visualSky.skyRenderer?.GetType() == typeof(PhysicallyBasedSkyRenderer))
                    {
                        // Allocate space in the cookie atlas for PBR sky surface textures
                        foreach (var directional in lightEntities.directionalLights)
                        {
                            if (directional.interactsWithSky)
                                m_TextureCaches.lightCookieManager.ReserveSpace(directional.surfaceTexture);
                        }
                    }
                }

                // Also we need to allocate space for the volumetric clouds texture if necessary
                if (m_SkyManager.TryGetCloudSettings(hdCamera, out var cloudSettings, out var cloudRenderer))
                {
                    CookieParameters cookieParams = new CookieParameters();
                    if (cloudRenderer.GetSunLightCookieParameters(cloudSettings, ref cookieParams))
                        m_TextureCaches.lightCookieManager.ReserveSpace(cookieParams.texture);
                }
            }
        }

        void PrepareGPULightdata(CommandBuffer cmd, HDCamera hdCamera, CullingResults cullResults)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.PrepareGPULightdata)))
            {
                // 2. Go through all lights, convert them to GPU format.
                // Simultaneously create data for culling (LightVolumeData and SFiniteLightBound)
                m_GpuLightsBuilder.Build(cmd, hdCamera, cullResults, m_ProcessedLightsBuilder, HDLightRenderDatabase.instance, m_ShadowInitParameters, m_CurrentDebugDisplaySettings);

                m_EnableBakeShadowMask = m_EnableBakeShadowMask || m_ProcessedLightsBuilder.bakedShadowsCount > 0;
                m_CurrentShadowSortedSunLightIndex = m_GpuLightsBuilder.currentShadowSortedSunLightIndex;
                m_CurrentSunShadowMapFlags = m_GpuLightsBuilder.currentSunShadowMapFlags;
                m_CurrentSunLightDirectionalLightData = m_GpuLightsBuilder.currentSunLightDirectionalLightData;

                m_ContactShadowIndex = m_GpuLightsBuilder.contactShadowIndex;
                m_ScreenSpaceShadowIndex = m_GpuLightsBuilder.screenSpaceShadowIndex;
                m_ScreenSpaceShadowChannelSlot = m_GpuLightsBuilder.screenSpaceShadowChannelSlot;
                m_DebugSelectedLightShadowIndex = m_GpuLightsBuilder.debugSelectedLightShadowIndex;
                m_DebugSelectedLightShadowCount = m_GpuLightsBuilder.debugSelectedLightShadowCount;
                m_CurrentScreenSpaceShadowData = m_GpuLightsBuilder.currentScreenSpaceShadowData;
            }
        }

        void ClearUnusedProcessedReferences(CullingResults cullResults, HDProbeCullingResults hdProbeCullingResults)
        {
            for (int i = cullResults.visibleReflectionProbes.Length; i < m_ProcessedReflectionProbeData.size; i++)
                m_ProcessedReflectionProbeData[i].hdProbe = null;
            for (int i = hdProbeCullingResults.visibleProbes.Count; i < m_ProcessedPlanarProbeData.size; i++)
                m_ProcessedPlanarProbeData[i].hdProbe = null;
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
            if (!probe.reflectionProbe.TryGetComponent<HDAdditionalReflectionData>(out var add))
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

            int maxProbeCount = Math.Min(totalProbes, m_MaxEnvLightsOnScreen);
            UpdateSortKeysArray(maxProbeCount);

            var enableReflectionProbes = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ReflectionProbe) &&
                (!hasDebugLightFilter || debugLightFilter.IsEnabledFor(ProbeSettings.ProbeType.ReflectionProbe));

            var enablePlanarProbes = hdCamera.frameSettings.IsEnabled(FrameSettingsField.PlanarProbe) &&
                (!hasDebugLightFilter || debugLightFilter.IsEnabledFor(ProbeSettings.ProbeType.PlanarProbe));

            if (enableReflectionProbes)
            {
                m_ProcessedReflectionProbeData.Resize(cullResults.visibleReflectionProbes.Length);

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

                    var priority = CalculateProbePriority(probe.bounds, probe.importance);

                    m_SortKeys[envLightCount++] = PackProbeKey(priority, lightVolumeType, 0u, probeIndex); // Sort by priority
                }
            }

            if (enablePlanarProbes)
            {
                m_ProcessedPlanarProbeData.Resize(hdProbeCullingResults.visibleProbes.Count);

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

                    var priority = CalculateProbePriority(probe.bounds, probe.importance);

                    m_SortKeys[envLightCount++] = PackProbeKey(priority, lightVolumeType, 1u, planarProbeIndex); // Sort by priority
                }
            }

            // Not necessary yet but call it for future modification with sphere influence volume
            CoreUnsafeUtils.QuickSort(m_SortKeys, 0, envLightCount - 1); // Call our own quicksort instead of Array.Sort(sortKeys, 0, sortCount) so we don't allocate memory (note the SortCount-1 that is different from original call).
            return envLightCount;
        }

        void PrepareGPUProbeData(CommandBuffer cmd, HDCamera hdCamera, CullingResults cullResults, HDProbeCullingResults hdProbeCullingResults, int processedLightCount)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.PrepareGPUProbeData)))
            {
                Vector3 camPosWS = hdCamera.mainViewConstants.worldSpaceCameraPos;

                for (int sortIndex = 0; sortIndex < processedLightCount; ++sortIndex)
                {
                    ProcessedProbeData processedProbe = GetSortedProcessedProbe(sortIndex);

                    if (processedProbe.hdProbe.HasValidRenderedData())
                        m_TextureCaches.reflectionProbeTextureCache.ReserveReflectionProbeSlot(processedProbe.hdProbe);
                }

                for (int sortIndex = 0; sortIndex < processedLightCount; ++sortIndex)
                {
                    // In 1. we have already classify and sorted the light, we need to use this sorted order here
                    ProcessedProbeData processedProbe = GetSortedProcessedProbe(sortIndex);

                    // Output data of the function
                    int fetchIndex;
                    Vector4 scaleOffset;
                    Matrix4x4 vp;
                    EnvLightData envLightData = new EnvLightData();

                    if (GetEnvLightData(cmd, hdCamera, processedProbe, ref envLightData, out fetchIndex, out scaleOffset, out vp))
                    {
                        switch (processedProbe.hdProbe)
                        {
                            case PlanarReflectionProbe planarProbe:
                                SetPlanarReflectionData(fetchIndex, ref vp, ref scaleOffset);
                            break;
                            case HDAdditionalReflectionData reflectionData:
                                SetCubeReflectionData(fetchIndex, ref scaleOffset);
                            break;
                        };

                        // it has been filled
                        m_lightList.envLights.Add(envLightData);

                        for (int viewIndex = 0; viewIndex < hdCamera.viewCount; ++viewIndex)
                        {
                            var worldToView = GetWorldToViewMatrix(hdCamera, viewIndex);
                            LightVolumeType lightVolumeType = GetSortedLightVolumeType(sortIndex);
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
        }

        ProcessedProbeData GetSortedProcessedProbe(int sortIndex)
        {
            UnpackProbeSortKey(m_SortKeys[sortIndex], out _, out int probeIndex, out int listType);
            return (listType == 0) ? m_ProcessedReflectionProbeData[probeIndex] : m_ProcessedPlanarProbeData[probeIndex];
        }

        LightVolumeType GetSortedLightVolumeType(int sortIndex)
        {
            UnpackProbeSortKey(m_SortKeys[sortIndex], out LightVolumeType lightVolumeType, out _, out _);
            return lightVolumeType;
        }

        // Return true if BakedShadowMask are enabled
        bool PrepareLightsForGPU(ScriptableRenderContext renderContext,
            CommandBuffer cmd,
            HDCamera hdCamera,
            CullingResults cullResults,
            HDProbeCullingResults hdProbeCullingResults,
            DebugDisplaySettings debugDisplaySettings,
            AOVRequestData aovRequest)
        {
            var debugLightFilter = debugDisplaySettings.GetDebugLightFilterMode();
            var hasDebugLightFilter = debugLightFilter != DebugLightFilterMode.None;

            HDShadowManager.cachedShadowManager.AssignSlotsInAtlases();

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.PrepareLightsForGPU)))
            {
                Camera camera = hdCamera.camera;
                ClearUnusedProcessedReferences(cullResults, hdProbeCullingResults);

                // If any light require it, we need to enabled bake shadow mask feature
                m_EnableBakeShadowMask = false;

                m_lightList.Clear();

                // We need to properly reset this here otherwise if we go from 1 light to no visible light we would keep the old reference active.
                m_CurrentSunLight = null;
                m_CurrentSunLightDataIndex = -1;
                m_CurrentSunLightAdditionalLightData = null;
                m_CurrentShadowSortedSunLightIndex = -1;
                m_DebugSelectedLightShadowIndex = -1;
                m_DebugSelectedLightShadowCount = 0;

                int decalDatasCount = Math.Min(DecalSystem.m_DecalDatasCount, m_MaxDecalsOnScreen);

                // We must clear the shadow requests before checking if they are any visible light because we would have requests from the last frame executed in the case where we don't see any lights
                m_ShadowManager.Clear();

                m_ScreenSpaceShadowIndex = 0;
                m_ScreenSpaceShadowChannelSlot = 0;
                // Set all the light data to invalid
                for (int i = 0; i < m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots; ++i)
                {
                    m_CurrentScreenSpaceShadowData[i].additionalLightData = null;
                    m_CurrentScreenSpaceShadowData[i].lightDataIndex = -1;
                    m_CurrentScreenSpaceShadowData[i].valid = false;
                }

                m_GpuLightsBuilder.NewFrame(
                    hdCamera,
                    cullResults.visibleLights.Length + cullResults.visibleReflectionProbes.Length + hdProbeCullingResults.visibleProbes.Count
                    + decalDatasCount);

                // Note: Light with null intensity/Color are culled by the C++, no need to test it here
                if (cullResults.visibleLights.Length != 0)
                {
                    PreprocessVisibleLights(renderContext, cmd, hdCamera, cullResults, debugDisplaySettings, aovRequest);

                    // In case ray tracing supported and a light cluster is built, we need to make sure to reserve all the cookie slots we need
                    if (m_RayTracingSupported)
                        ReserveRayTracingCookieAtlasSlots();

                    PrepareGPULightdata(cmd, hdCamera, cullResults);

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
                            m_GpuLightsBuilder.AddLightBounds(viewIndex, DecalSystem.m_Bounds[i], DecalSystem.m_LightVolumes[i]);
                        }
                    }
                }

                for (int viewIndex = 0; viewIndex < hdCamera.viewCount; ++viewIndex)
                {
                    Matrix4x4 worldToViewCR = GetWorldToViewMatrix(hdCamera, viewIndex);

                    if (ShaderConfig.s_CameraRelativeRendering != 0)
                    {
                        // The OBBs are camera-relative, the matrix is not. Fix it.
                        worldToViewCR.SetColumn(3, new Vector4(0, 0, 0, 1));
                    }
                }

                m_TotalLightCount = m_GpuLightsBuilder.lightsCount + m_lightList.envLights.Count + decalDatasCount;

                Debug.Assert(m_TotalLightCount == m_GpuLightsBuilder.lightsPerView[0].boundsCount);

                PushLightDataGlobalParams(cmd);
                PushShadowGlobalParams(cmd);
            }

            m_ProcessedLightsBuilder.Reset();
            m_TextureCaches?.reflectionProbeTextureCache?.GarbageCollectTmpResources();

            m_EnableBakeShadowMask = m_EnableBakeShadowMask && hdCamera.frameSettings.IsEnabled(FrameSettingsField.Shadowmask);
            return m_EnableBakeShadowMask;
        }

        internal void ReserveCookieAtlasTexture(HDAdditionalLightData hdLightData, Light light, LightType lightType)
        {
            // Note: light component can be null if a Light is used for shuriken particle lighting.
            lightType = light == null ? LightType.Point : lightType;
            switch (lightType)
            {
                case LightType.Directional:
                {
                    m_TextureCaches.lightCookieManager.ReserveSpace(light?.cookie);
                    break;
                }
                case LightType.Point:
                    if (light?.cookie != null && hdLightData.IESPoint != null && light.cookie != hdLightData.IESPoint)
                        m_TextureCaches.lightCookieManager.ReserveSpaceCube(light.cookie, hdLightData.IESPoint);
                    else if (light?.cookie != null)
                        m_TextureCaches.lightCookieManager.ReserveSpaceCube(light.cookie);
                    else if (hdLightData.IESPoint != null)
                        m_TextureCaches.lightCookieManager.ReserveSpaceCube(hdLightData.IESPoint);
                    break;
                case LightType.Spot:
                case LightType.Pyramid:
                case LightType.Box:
                    if (light?.cookie != null && hdLightData.IESSpot != null && light.cookie != hdLightData.IESSpot)
                        m_TextureCaches.lightCookieManager.ReserveSpace(light.cookie, hdLightData.IESSpot);
                    else if (light?.cookie != null)
                        m_TextureCaches.lightCookieManager.ReserveSpace(light.cookie);
                    else if (hdLightData.IESSpot != null)
                        m_TextureCaches.lightCookieManager.ReserveSpace(hdLightData.IESSpot);
                    // Projectors lights must always have a cookie texture.
                    else if (lightType != LightType.Spot)
                        m_TextureCaches.lightCookieManager.ReserveSpace(Texture2D.whiteTexture);
                    break;
                case LightType.Rectangle:
                case LightType.Disc:
                    // Only rectangle can have cookies
                    if (hdLightData.IESSpot != null && hdLightData.areaLightCookie != null && hdLightData.IESSpot != hdLightData.areaLightCookie)
                        m_TextureCaches.lightCookieManager.ReserveSpace(hdLightData.areaLightCookie, hdLightData.IESSpot);
                    else if (hdLightData.IESSpot != null)
                        m_TextureCaches.lightCookieManager.ReserveSpace(hdLightData.IESSpot);
                    else if (hdLightData.areaLightCookie != null)
                        m_TextureCaches.lightCookieManager.ReserveSpace(hdLightData.areaLightCookie);
                    break;
                case LightType.Tube:
                    // These light types can't have cookies
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

        const uint k_floatSignMask = (1u << 31)-1;

        // 52 bits total stored when packing, 6 bits unused, 15 bits importance, 31 bits volume
        static ulong CalculateProbePriority(Bounds bounds, int importance = 1)
        {
            float boxVolume = 8f * bounds.extents.x * bounds.extents.y * bounds.extents.z;

            ulong priority;
            uint uintVolume;

            // Use unsafe to point uintVolume to the same bits as boxVolume
            unsafe
            {
                uintVolume = *((uint*)&boxVolume);
            }

            // The sign bit should always be positive (0), but just in case, force it to 0
            uintVolume &= k_floatSignMask;

            // Copy the bits to the 64 bits unsigned integer
            priority = uintVolume;

            // Store the importance value in bits of higher weight, inverted so higher importances result in lower priority value and are displayed on top.
            priority |= (ulong)(32767 - importance) << 31;

             return priority;
        }

        static void UnpackProbeSortKey(ulong sortKey, out LightVolumeType lightVolumeType, out int probeIndex, out int listType)
        {
            lightVolumeType = (LightVolumeType)((sortKey >> 9) & 0x3);
            probeIndex = (int)(sortKey & 0xFF);
            listType = (int)((sortKey >> 8) & 1);
        }

        static ulong PackProbeKey(ulong priority, LightVolumeType lightVolumeType, uint listType, int probeIndex)
        {
            // 52 bit priority, 3 bit LightVolumeType, 1 bit list type, 8 bit index
            return priority << 12 | (uint)lightVolumeType << 9 | listType << 8 | ((uint)probeIndex & 0xFF);
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
            public HDCamera hdCamera;
            public TileAndClusterData tileAndClusterData;
        }

        unsafe void UpdateShaderVariablesGlobalLightLoop(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            // Atlases
            cb._CookieAtlasSize = m_TextureCaches.lightCookieManager.GetCookieAtlasSize();
            cb._CookieAtlasData = m_TextureCaches.lightCookieManager.GetCookieAtlasDatas();
            cb._ReflectionAtlasCubeData = m_TextureCaches.reflectionProbeTextureCache.GetTextureAtlasCubeData();
            cb._ReflectionAtlasPlanarData = m_TextureCaches.reflectionProbeTextureCache.GetTextureAtlasPlanarData();
            cb._EnvSliceSize = m_TextureCaches.reflectionProbeTextureCache.GetEnvSliceSize();

            // Light info
            cb._DirectionalLightCount = (uint)m_GpuLightsBuilder.directionalLightCount;
            cb._PunctualLightCount = (uint)m_GpuLightsBuilder.punctualLightCount;
            cb._AreaLightCount = (uint)m_GpuLightsBuilder.areaLightCount;
            cb._EnvLightCount = (uint)m_lightList.envLights.Count;

            // TODO-WL: Directional lights?
            cb._WorldDirectionalLightCount = 0u;
            cb._WorldPunctualLightCount = (uint)m_WorldLights.pointLightCount;
            cb._WorldAreaLightCount = (uint)(m_WorldLights.rectLightCount + m_WorldLights.lineLightCount + m_WorldLights.discLightCount);
            cb._WorldEnvLightCount = (uint)m_WorldLights.envLightCount;

            cb._DecalCount = (uint)DecalSystem.m_DecalDatasCount;
            HDAdditionalLightData sunLightData = GetHDAdditionalLightData(m_CurrentSunLight);
            bool sunLightShadow = sunLightData != null && m_CurrentShadowSortedSunLightIndex >= 0;
            cb._DirectionalShadowIndex = sunLightShadow ? m_CurrentShadowSortedSunLightIndex : -1;
            cb._EnableLightLayers = hdCamera.frameSettings.IsEnabled(FrameSettingsField.LightLayers) ? 1u : 0u;
            cb._EnableDecalLayers = hdCamera.frameSettings.IsEnabled(FrameSettingsField.DecalLayers) ? 1u : 0u;
            cb._EnableRenderingLayers = hdCamera.frameSettings.IsEnabled(FrameSettingsField.RenderingLayerMaskBuffer) ? 1u : 0u;
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
            cb._SpecularFade       = m_SpecularFadeSettings.enabled ? 1 : 0;
        }

        void PushLightDataGlobalParams(CommandBuffer cmd)
        {
            m_LightLoopLightData.directionalLightData.SetData(m_GpuLightsBuilder.directionalLights, 0, 0, m_GpuLightsBuilder.directionalLightCount);
            m_LightLoopLightData.lightData.SetData(m_GpuLightsBuilder.lights, 0, 0, m_GpuLightsBuilder.lightsCount);
            m_LightLoopLightData.envLightData.SetData(m_lightList.envLights);
            m_LightLoopLightData.decalData.SetData(DecalSystem.m_DecalDatas, 0, 0, Math.Min(DecalSystem.m_DecalDatasCount, m_MaxDecalsOnScreen)); // don't add more than the size of the buffer

            for (int viewId = 0; viewId < m_GpuLightsBuilder.lightsPerViewCount; ++viewId)
            {
                HDGpuLightsBuilder.LightsPerView lightsPerView = m_GpuLightsBuilder.lightsPerView[viewId];

                bool validLightCount = lightsPerView.boundsCount <= m_TileAndClusterData.maxLightCount;
                Debug.Assert(validLightCount, "Encountered bounds counts that are greater than the total light count.");

                /// In the CPU we have stored the left and right eye in one single array, offset by the LightsPerView.boundsOffset. This is before trivial rejection.
                /// In the GPU we compact them, and access each eye by the actual m_TotalLightCount, which contains the post trivial rejection offset.
                int inputStartIndex = lightsPerView.boundsOffset;
                int outputStartIndex = viewId * m_TotalLightCount;
                int maxLightCount = (validLightCount) ? lightsPerView.boundsCount : m_TileAndClusterData.maxLightCount;

                // These two buffers have been set in Rebuild(). At this point, view 0 contains combined data from all views
                m_TileAndClusterData.convexBoundsBuffer.SetData(m_GpuLightsBuilder.lightBounds, inputStartIndex, outputStartIndex, maxLightCount);
                m_TileAndClusterData.lightVolumeDataBuffer.SetData(m_GpuLightsBuilder.lightVolumes, inputStartIndex, outputStartIndex, maxLightCount);
            }

            ConstantBuffer.PushGlobal(cmd, m_EnvLightReflectionData, HDShaderIDs._EnvLightReflectionData);

            cmd.SetGlobalTexture(HDShaderIDs._CookieAtlas, m_TextureCaches.lightCookieManager.atlasTexture);
            cmd.SetGlobalTexture(HDShaderIDs._ReflectionAtlas, m_TextureCaches.reflectionProbeTextureCache.GetAtlasTexture());

            cmd.SetGlobalBuffer(HDShaderIDs._LightDatas, m_LightLoopLightData.lightData);
            cmd.SetGlobalBuffer(HDShaderIDs._EnvLightDatas, m_LightLoopLightData.envLightData);
            cmd.SetGlobalBuffer(HDShaderIDs._DecalDatas, m_LightLoopLightData.decalData);
            cmd.SetGlobalBuffer(HDShaderIDs._DirectionalLightDatas, m_LightLoopLightData.directionalLightData);
        }

        void PushShadowGlobalParams(CommandBuffer cmd)
        {
            m_ShadowManager.PushGlobalParameters(cmd);
        }

        bool WillRenderContactShadow()
        {
            // When contact shadow index is 0, then there is no light casting contact shadow in the view
            return m_EnableContactShadow && m_ContactShadowIndex != 0;
        }

        // The first rendered 24 lights that have contact shadow enabled have a mask used to select the bit that contains
        // the contact shadow shadowed information (occluded or not). Otherwise -1 is written
        // 8 bits are reserved for the fading.
        void GetContactShadowMask(HDAdditionalLightData hdAdditionalLightData, BoolScalableSetting contactShadowEnabled, HDCamera hdCamera, bool isRasterization, ref int contactShadowMask, ref float rayTracingShadowFlag)
        {
            contactShadowMask = 0;
            rayTracingShadowFlag = 0.0f;
            // If contact shadows are not enabled or we already reached the manimal number of contact shadows
            // or this is not rasterization
            if ((!hdAdditionalLightData.useContactShadow.Value(contactShadowEnabled))
                || m_ContactShadowIndex >= LightDefinitions.s_ContactShadowMaskMask
                || !isRasterization)
                return;

            // Evaluate the contact shadow index of this light
            contactShadowMask = 1 << m_ContactShadowIndex++;

            // If this light has ray traced contact shadow
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && hdAdditionalLightData.rayTraceContactShadow)
                rayTracingShadowFlag = 1.0f;
        }

        unsafe void SetPlanarReflectionData(int index, ref Matrix4x4 vp, ref Vector4 scaleOffset)
        {
            Debug.Assert(index < k_MaxPlanarReflectionsOnScreen);

            for (int j = 0; j < 16; ++j)
                m_EnvLightReflectionData._PlanarCaptureVP[index * 16 + j] = vp[j];

            for (int j = 0; j < 4; ++j)
                m_EnvLightReflectionData._PlanarScaleOffset[index * 4 + j] = scaleOffset[j];
        }

        unsafe void SetCubeReflectionData(int index, ref Vector4 scaleOffset)
        {
            Debug.Assert(index < k_MaxCubeReflectionsOnScreen);

            for (int j = 0; j < 4; ++j)
                m_EnvLightReflectionData._CubeScaleOffset[index * 4 + j] = scaleOffset[j];
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
