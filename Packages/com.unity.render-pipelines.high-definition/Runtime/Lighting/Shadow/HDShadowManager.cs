using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Serialization;
#if UNITY_EDITOR
// TODO @ SHADERS: Enable as many of the rules (currently commented out) as make sense
//                 once the setting asset aggregation behavior is finalized.  More fine tuning
//                 of these rules is also desirable (current rules have been interpreted from
//                 the variant stripping logic)
using ShaderKeywordFilter = UnityEditor.ShaderKeywordFilter;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Shadow Filtering Quality
    /// </summary>
    public enum HDShadowFilteringQuality
    {
        /// <summary>
        /// Low Shadow Filtering Quality
        /// </summary>
        Low = 0,
        /// <summary>
        /// Medium Shadow Filtering Quality
        /// </summary>
        Medium = 1,
        /// <summary>
        /// High Shadow Filtering Quality
        /// </summary>
        High = 2
    }

    /// <summary>
    /// Area Shadow Filtering Quality
    /// </summary>
    public enum HDAreaShadowFilteringQuality
    {
        /// <summary>
        /// Area Medium Shadow Filtering Quality
        /// </summary>
        Medium = 0,
        /// <summary>
        /// Area High Shadow Filtering Quality
        /// </summary>
        High = 1
    }

    enum ShadowMapType
    {
        CascadedDirectional,
        PunctualAtlas,
        AreaLightAtlas
    }

    enum ShadowMapUpdateType
    {
        // Fully dynamic shadow maps
        Dynamic = 0,
        // Fully cached shadow maps (nothing is rendered unless requested)
        Cached,
        // Mixed, static shadow caster are cached and updated as indicated, dynamic are drawn on top.
        Mixed
    }

    [GenerateHLSL(needAccessors = false)]
    struct HDShadowData
    {
        public Vector3 rot0;
        public Vector3 rot1;
        public Vector3 rot2;
        public Vector3 pos;
        public Vector4 proj;

        public Vector2 atlasOffset;
        public float worldTexelSize;
        public float normalBias;

        [SurfaceDataAttributes(precision = FieldPrecision.Real)]
        public Vector4 zBufferParam;
        public Vector4 shadowMapSize;

        public Vector4 shadowFilterParams0;
        public Vector4 dirLightPCSSParams0;
        public Vector4 dirLightPCSSParams1;

        public Vector3 cacheTranslationDelta;
        public float isInCachedAtlas;

        public Matrix4x4 shadowToWorld;
    }

    // We use a different structure for directional light because these is a lot of data there
    // and it will add too much useless stuff for other lights
    // Note: In order to support HLSL array generation, we need to use fixed arrays and so a unsafe context for this struct
    [GenerateHLSL(needAccessors = false)]
    unsafe struct HDDirectionalShadowData
    {
        // We can't use Vector4 here because the vector4[] makes this struct non blittable
        [HLSLArray(4, typeof(Vector4))]
        public fixed float sphereCascades[4 * 4];

        [SurfaceDataAttributes(precision = FieldPrecision.Real)]
        public Vector4 cascadeDirection;

        [HLSLArray(4, typeof(float))]
        [SurfaceDataAttributes(precision = FieldPrecision.Real)]
        public fixed float cascadeBorders[4];

        public float fadeScale;
        public float fadeBias;
    }

    struct HDShadowCullingSplit
    {
        public Matrix4x4 view;
        public Matrix4x4 deviceProjectionMatrix;
        public Matrix4x4 deviceProjectionYFlip; // Use the y flipped device projection matrix as light projection matrix
        public Matrix4x4 projection;
        public Matrix4x4 invViewProjection;
        public Vector4 deviceProjection;
        public Vector4 cullingSphere;
        public Vector2 viewportSize;
        public float forwardOffset;
        public int splitIndex;
    }

    internal struct HDShadowRequestHandle
    {
        public HDShadowRequestSetHandle setHandle;
        public int offset;

        public int storageIndexForShadowRequest => setHandle.storageIndexForShadowRequests + offset;
        public int storageIndexForRequestIndex => setHandle.storageIndexForRequestIndices + offset;
        public int storageIndexForCachedViewPosition => setHandle.storageIndexForCachedViewPositions + offset;
        public int storageIndexForFrustumPlanes => setHandle.storageIndexForCachedViewPositions  + (offset * HDShadowRequest.frustumPlanesCount);

        public HDShadowRequestHandle(HDShadowRequestSetHandle setHandle, int offset)
        {
            this.setHandle = setHandle;
            this.offset = offset;
        }
    }


    internal struct HDShadowRequestSetHandle
    {
        public const int InvalidIndex = -1;
        public int relativeDataOffset;

        public int storageIndexForShadowRequests => relativeDataOffset * HDShadowRequest.maxLightShadowRequestsCount;
        public int storageIndexForRequestIndices => relativeDataOffset * HDShadowRequest.maxLightShadowRequestsCount;
        public int storageIndexForCachedViewPositions => relativeDataOffset * HDShadowRequest.maxLightShadowRequestsCount;
        public int storageIndexForFrustumPlanes => relativeDataOffset * HDShadowRequest.maxLightShadowRequestsCount * HDShadowRequest.frustumPlanesCount;

        public bool valid => relativeDataOffset != InvalidIndex;

        public HDShadowRequestHandle this[int index]
        {
            get { return new HDShadowRequestHandle(this, index); }
        }
    }


    internal struct HDShadowRequest
    {
        public const int maxLightShadowRequestsCount = 6;
        public const int frustumPlanesCount = 6;

        private const int ShouldUseCachedShadowDataFlagIndex = 0;
        private const int ShouldRenderCachedComponentFlagIndex = 1;
        private const int IsInCachedAtlasFlagIndex = 2;
        private const int IsMixedCachedFlagIndex = 3;
        private const int IsValidFlagIndex = 4;
        private const int ZClipFlagIndex = 5;

        private const int ShadowMapTypeIndex = 0;
        private const int BatchCullingProjectionTypeIndex = 1;

        public HDShadowCullingSplit cullingSplit;
        public HDShadowData cachedShadowData;
        public Matrix4x4 shadowToWorld;
        public Vector4 zBufferParam;
        public Vector4 evsmParams;
        // Warning: these viewport fields are updated by ProcessShadowRequests and are invalid before
        public Rect dynamicAtlasViewport;
        public Rect cachedAtlasViewport;

        public Vector3 position;

        // TODO: Remove these field once scriptable culling is here (currently required by ScriptableRenderContext.DrawShadows)
        public int lightIndex;
        // end

        public float normalBias;
        public float worldTexelSize;
        public float slopeBias;

        // PCSS parameter
        public float shadowSoftness;

        public float minFilterSize;

        // IMS parameters
        public float kernelSize;

        // PCSS parameters
        public byte blockerSampleCount;
        public byte filterSampleCount;

        // Parameters specific to directional lights
        public float dirLightPCSSDepth2RadialScale;         // scales depth to light cone radius (in shadowmap space)
        public float dirLightPCSSRadial2DepthScale;         // scales radius to light cone depth (in shadowmap space)
        public float dirLightPCSSMaxBlockerDistance;        // Maximum distance of blockers, limiting blur size
        public float dirLightPCSSMaxSamplingDistance;       // Maximum sampling distance, to avoid light leaks
        public float dirLightPCSSMinFilterSizeTexels;       // Minimum filter size (in texels)
        public float dirLightPCSSMinFilterRadial2DepthScale;// Minimum filter radius to light cone depth (in shadowmap space)
        public float dirLightPCSSBlockerRadial2DepthScale;  // scales radius to light cone depth (in shadowmap space)
        public float dirLightPCSSBlockerSamplingClumpExponent; // Blocker sample clump exponent to apply to linear radial range

        public byte typeData;

        public BitArray8 flags;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTypeData(byte typeIndex, byte value)
        {
            typeData = (byte)((typeData & ~(0b11 << (typeIndex * 2))) | (value << (typeIndex * 2)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetTypeData(byte typeIndex)
        {
            return (byte)((typeData >> (typeIndex * 2)) & 0b11);
        }

        // Determine in which atlas the shadow will be rendered
        public ShadowMapType shadowMapType
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => (ShadowMapType)GetTypeData(ShadowMapTypeIndex);
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => SetTypeData(ShadowMapTypeIndex, (byte)value);
        }
        public BatchCullingProjectionType projectionType
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => (BatchCullingProjectionType)GetTypeData(BatchCullingProjectionTypeIndex);
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => SetTypeData(BatchCullingProjectionTypeIndex, (byte)value);
        }

        public bool         shouldUseCachedShadowData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => flags[ShouldUseCachedShadowDataFlagIndex];
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => flags[ShouldUseCachedShadowDataFlagIndex] = value;
        }

        public bool shouldRenderCachedComponent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => flags[ShouldRenderCachedComponentFlagIndex];
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => flags[ShouldRenderCachedComponentFlagIndex] = value;
        }

        public bool isInCachedAtlas
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => flags[IsInCachedAtlasFlagIndex];
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => flags[IsInCachedAtlasFlagIndex] = value;
        }

        public bool isMixedCached
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => flags[IsMixedCachedFlagIndex];
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => flags[IsMixedCachedFlagIndex] = value;
        }

        public bool isValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => flags[IsValidFlagIndex];
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => flags[IsValidFlagIndex] = value;
        }

        public bool zClip
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => flags[ZClipFlagIndex];
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => flags[ZClipFlagIndex] = value;
        }

        public void InitDefault()
        {
            cullingSplit = default;
            shadowToWorld = default;
            position = default;
            zBufferParam = default;
            dynamicAtlasViewport = default;
            cachedAtlasViewport = default;
            zClip = default;
            shadowMapType = ShadowMapType.PunctualAtlas;
            lightIndex = default;
            normalBias = default;
            worldTexelSize = default;
            slopeBias = default;
            shadowSoftness = default;
            blockerSampleCount = default;
            filterSampleCount = default;
            dirLightPCSSDepth2RadialScale = default;
            dirLightPCSSRadial2DepthScale = default;
            dirLightPCSSMaxBlockerDistance = default;
            dirLightPCSSMaxSamplingDistance = default;
            dirLightPCSSMinFilterSizeTexels = default;
            dirLightPCSSMinFilterRadial2DepthScale = default;
            dirLightPCSSBlockerRadial2DepthScale = default;
            dirLightPCSSBlockerSamplingClumpExponent = default;
            minFilterSize = default;
            kernelSize = default;
            evsmParams = default;
            shouldUseCachedShadowData = default;
            shouldRenderCachedComponent = default;
            cachedShadowData = default;
            isInCachedAtlas = default;
            isMixedCached = default;
            isValid = true;
        }
    }

    enum DirectionalShadowAlgorithm
    {
        PCF5x5,
        PCF7x7,
        PCSS,
        IMS
    }

    /// <summary>
    /// Screen Space Shadows format.
    /// </summary>
    public enum ScreenSpaceShadowFormat
    {
        /// <summary>R8G8B8A8 format for fastest rendering.</summary>
        R8G8B8A8 = GraphicsFormat.R8G8B8A8_UNorm,
        /// <summary>R16G16B16A16 format for better quality.</summary>
        R16G16B16A16 = GraphicsFormat.R16G16B16A16_SFloat
    }

    /// <summary>
    /// Shadows Global Settings.
    /// </summary>
    [Serializable]
    public struct HDShadowInitParameters
    {
        /// <summary>
        /// Shadow Atlases parameters.
        /// </summary>
        [Serializable]
        public struct HDShadowAtlasInitParams
        {
            /// <summary>Shadow Atlas resolution.</summary>
            public int shadowAtlasResolution;
            /// <summary>Shadow Atlas depth bits.</summary>
            public DepthBits shadowAtlasDepthBits;
            /// <summary>Enable dynamic rescale of the atlas.</summary>
            public bool useDynamicViewportRescale;

            internal static HDShadowAtlasInitParams GetDefault()
            {
                return new HDShadowAtlasInitParams()
                {
                    shadowAtlasResolution = k_DefaultShadowAtlasResolution,
                    shadowAtlasDepthBits = CoreUtils.GetDefaultDepthBufferBits(),
                    useDynamicViewportRescale = true
                };
            }
        }

        internal static HDShadowInitParameters NewDefault() => new HDShadowInitParameters()
        {
            maxShadowRequests = k_DefaultMaxShadowRequests,
            directionalShadowsDepthBits = CoreUtils.GetDefaultDepthBufferBits(),
            punctualLightShadowAtlas = HDShadowAtlasInitParams.GetDefault(),
            areaLightShadowAtlas = HDShadowAtlasInitParams.GetDefault(),
            cachedPunctualLightShadowAtlas = 2048,
            cachedAreaLightShadowAtlas = 1024,
            allowDirectionalMixedCachedShadows = false,
            shadowResolutionDirectional = new IntScalableSetting(new[] { 256, 512, 1024, 2048 }, ScalableSettingSchemaId.With4Levels),
            shadowResolutionArea = new IntScalableSetting(new[] { 256, 512, 1024, 2048 }, ScalableSettingSchemaId.With4Levels),
            shadowResolutionPunctual = new IntScalableSetting(new[] { 256, 512, 1024, 2048 }, ScalableSettingSchemaId.With4Levels),
            punctualShadowFilteringQuality = HDShadowFilteringQuality.Medium,
            directionalShadowFilteringQuality = HDShadowFilteringQuality.Medium,
            areaShadowFilteringQuality = HDAreaShadowFilteringQuality.Medium,
            supportScreenSpaceShadows = false,
            maxScreenSpaceShadowSlots = 4,
            screenSpaceShadowBufferFormat = ScreenSpaceShadowFormat.R16G16B16A16,
            maxDirectionalShadowMapResolution = 2048,
            maxAreaShadowMapResolution = 2048,
            maxPunctualShadowMapResolution = 2048,
        };

        internal const int k_DefaultShadowAtlasResolution = 4096;
        internal const int k_DefaultMaxShadowRequests = 128;

        /// <summary>Maximum number of shadow requests at the same time.</summary>
        public int maxShadowRequests;
        /// <summary>Depth bits for directional shadows.</summary>
        public DepthBits directionalShadowsDepthBits;

        /// <summary>Punctual shadow filtering quality.</summary>
#if UNITY_EDITOR // multi_compile_fragment SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH
        // [ShaderKeywordFilter.SelectIf(HDShadowFilteringQuality.Low, keywordNames: "SHADOW_LOW")]
        // [ShaderKeywordFilter.SelectIf(HDShadowFilteringQuality.Medium, keywordNames: "SHADOW_MEDIUM")]
        // [ShaderKeywordFilter.SelectIf(HDShadowFilteringQuality.High, keywordNames: "SHADOW_HIGH")]
#endif
        [FormerlySerializedAs("shadowQuality"), FormerlySerializedAs("shadowFilteringQuality")]
        public HDShadowFilteringQuality punctualShadowFilteringQuality;

        /// <summary>Directional shadow filtering quality.</summary>
        [FormerlySerializedAs("shadowQuality"), FormerlySerializedAs("shadowFilteringQuality")]
        public HDShadowFilteringQuality directionalShadowFilteringQuality;

        /// <summary>Area Shadow filtering quality.</summary>
#if UNITY_EDITOR // multi_compile_fragment AREA_SHADOW_MEDIUM AREA_SHADOW_HIGH
        // [ShaderKeywordFilter.SelectIf(HDAreaShadowFilteringQuality.Medium, keywordNames: "AREA_SHADOW_MEDIUM")]
        // [ShaderKeywordFilter.SelectIf(HDAreaShadowFilteringQuality.High, keywordNames: "AREA_SHADOW_HIGH")]
#endif
        public HDAreaShadowFilteringQuality areaShadowFilteringQuality;

        /// <summary>Initialization parameters for punctual shadows atlas.</summary>
        public HDShadowAtlasInitParams punctualLightShadowAtlas;
        /// <summary>Initialization parameters for area shadows atlas.</summary>
        public HDShadowAtlasInitParams areaLightShadowAtlas;

        /// <summary>Resolution for the punctual lights cached shadow maps atlas.</summary>
        public int cachedPunctualLightShadowAtlas;

        /// <summary>Resolution for the area lights cached shadow maps atlas.</summary>
        public int cachedAreaLightShadowAtlas;

        /// <summary>Maximum shadow map resolution for directional lights.</summary>
        public bool allowDirectionalMixedCachedShadows;


        /// <summary>Shadow scalable resolution for directional lights.</summary>
        public IntScalableSetting shadowResolutionDirectional;
        /// <summary>Shadow scalable resolution for point lights.</summary>
        public IntScalableSetting shadowResolutionPunctual;
        /// <summary>Shadow scalable resolution for area lights.</summary>
        public IntScalableSetting shadowResolutionArea;

        /// <summary>Maximum shadow map resolution for directional lights.</summary>
        public int maxDirectionalShadowMapResolution;
        /// <summary>Maximum shadow map resolution for punctual lights.</summary>
        public int maxPunctualShadowMapResolution;
        /// <summary>Maximum shadow map resolution for area lights.</summary>
        public int maxAreaShadowMapResolution;

        /// <summary>Enable support for screen space shadows.</summary>
#if UNITY_EDITOR // multi_compile_fragment SCREEN_SPACE_SHADOWS_OFF SCREEN_SPACE_SHADOWS_ON
        // [ShaderKeywordFilter.RemoveIf(true, keywordNames: "SCREEN_SPACE_SHADOWS_OFF")]
        // [ShaderKeywordFilter.RemoveIf(false, keywordNames: "SCREEN_SPACE_SHADOWS_ON")]
#endif
        public bool supportScreenSpaceShadows;
        /// <summary>Maximum number of screen space shadows.</summary>
        public int maxScreenSpaceShadowSlots;
        /// <summary>Format for screen space shadows.</summary>
        public ScreenSpaceShadowFormat screenSpaceShadowBufferFormat;
    }

    struct HDShadowResolutionRequest
    {
        public Rect             dynamicAtlasViewport;
        public Rect             cachedAtlasViewport;
        public Vector2          resolution;
        public ShadowMapType    shadowMapType;
    }

    internal struct HDShadowManagerDataForComputeCullingSplitsJob
    {
        public int requestCount;
        public float3 cachedDirectionalAngles;
        public HDCachedShadowManagerDataForShadowRequestUpdateJob cachedShadowManager;
    }

    internal struct HDShadowManagerDataForShadowRequestUpateJob
    {
        public NativeArray<HDShadowRequestHandle> shadowRequests;
        public NativeList<HDShadowResolutionRequest> shadowResolutionRequestStorage;
        public HDDynamicShadowAtlasDataForShadowRequestUpdateJob atlas;
        public HDDynamicShadowAtlasDataForShadowRequestUpdateJob cascadeShadowAtlas;
        public HDDynamicShadowAtlasDataForShadowRequestUpdateJob areaShadowAtlas;

        public HDCachedShadowManagerDataForShadowRequestUpdateJob cachedShadowManager;

        public HDAreaShadowFilteringQuality areaShadowFilteringQuality;

        public void WriteShadowRequestIndex(int index, int shadowRequestCount, HDShadowRequestHandle shadowRequest)
        {
            if (index >= shadowRequestCount)
                return;

            shadowRequests[index] = shadowRequest;
        }
    }

    internal struct ShadowResult
    {
        public TextureHandle punctualShadowResult;
        public TextureHandle cachedPunctualShadowResult;
        public TextureHandle directionalShadowResult;
        public TextureHandle areaShadowResult;
        public TextureHandle cachedAreaShadowResult;
    }

    internal struct HDDynamicShadowAtlasDataForShadowRequestUpdateJob
    {
        [WriteOnly] public NativeList<HDShadowRequestHandle> shadowRequests;
        [WriteOnly] public NativeList<HDShadowRequestHandle> mixedRequestsPendingBlits;

        public void initEmpty()
        {
            shadowRequests = new(Allocator.Persistent);
            mixedRequestsPendingBlits = new(Allocator.Persistent);
        }
        public void DisposeNativeCollections()
        {
            if (mixedRequestsPendingBlits.IsCreated)
                mixedRequestsPendingBlits.Dispose();
            if (shadowRequests.IsCreated)
                shadowRequests.Dispose();
        }

    }

    internal struct HDCachedShadowManagerDataForShadowRequestUpdateJob
    {
        public BitArray8 directionalShadowPendingUpdate;
        public HDCachedShadowAtlasDataForShadowRequestUpdateJob punctualShadowAtlas;
        public HDCachedShadowAtlasDataForShadowRequestUpdateJob areaShadowAtlas;
        public HDDirectionalLightAtlasDataForShadowRequestUpdateJob directionalLightAtlas;
        public bool directionalHasCachedAtlas;

        public bool LightIsPendingPlacement(int lightIdxForCachedShadows, ShadowMapType shadowMapType)
        {
            if (shadowMapType == ShadowMapType.PunctualAtlas)
                return punctualShadowAtlas.LightIsPendingPlacement(lightIdxForCachedShadows);
            if (shadowMapType == ShadowMapType.AreaLightAtlas)
                return areaShadowAtlas.LightIsPendingPlacement(lightIdxForCachedShadows);

            return false;
        }
    }

    internal struct HDCachedShadowAtlasDataForShadowRequestUpdateJob
    {
        [WriteOnly] public NativeList<HDShadowRequestHandle> shadowRequests;
        public NativeParallelHashMap<int, HDCachedShadowAtlas.CachedShadowRecord> shadowsPendingRendering;
        [WriteOnly] public NativeParallelHashMap<int, int> shadowsWithValidData;                            // Shadows that have been placed and rendered at least once (OnDemand shadows are not rendered unless requested explicitly). It is a dictionary for fast access by shadow index.
        [ReadOnly] public NativeParallelHashMap<int, HDLightRenderEntity> registeredLightDataPendingPlacement;
        [ReadOnly] public NativeParallelHashMap<int, HDCachedShadowAtlas.CachedShadowRecord> recordsPendingPlacement;          // Note: this is different from m_RegisteredLightDataPendingPlacement because it contains records that were allocated in the system
        // but they lost their spot (e.g. post defrag). They don't have a light associated anymore if not by index, so we keep a separate collection.

        public NativeParallelHashMap<int, HDCachedShadowAtlas.CachedTransform> transformCaches;
        internal bool LightIsPendingPlacement(int lightIdxForCachedShadows)
        {
            return (registeredLightDataPendingPlacement.ContainsKey(lightIdxForCachedShadows) ||
                    recordsPendingPlacement.ContainsKey(lightIdxForCachedShadows));
        }

        public void initEmpty()
        {
            shadowRequests = new(Allocator.Persistent);
            shadowsPendingRendering = new(1, Allocator.Persistent);
            shadowsWithValidData = new(1, Allocator.Persistent);
            registeredLightDataPendingPlacement = new(1, Allocator.Persistent);
            recordsPendingPlacement = new(1, Allocator.Persistent);
            transformCaches = new(1, Allocator.Persistent);
        }

        public void DisposeNativeCollections()
        {
            if (transformCaches.IsCreated)
                transformCaches.Dispose();
            if (recordsPendingPlacement.IsCreated)
                recordsPendingPlacement.Dispose();
            if (registeredLightDataPendingPlacement.IsCreated)
                registeredLightDataPendingPlacement.Dispose();
            if (shadowsWithValidData.IsCreated)
                shadowsWithValidData.Dispose();
            if (shadowsPendingRendering.IsCreated)
                shadowsPendingRendering.Dispose();
            if (shadowRequests.IsCreated)
                shadowRequests.Dispose();
        }
    }

    internal struct HDDirectionalLightAtlasDataForShadowRequestUpdateJob
    {
        public NativeList<HDShadowRequestHandle> shadowRequests;
    }

    internal struct HDShadowResolutionRequestHandle
    {
        public const int k_InvalidIndex = -1;
        public int index;

        public bool valid => index != k_InvalidIndex;

        public static HDShadowResolutionRequestHandle Invalid => new HDShadowResolutionRequestHandle() { index = k_InvalidIndex };
    }
    internal class HDShadowManager
    {
        public const int k_DirectionalShadowCascadeCount = 4;
        public const int k_MinShadowMapResolution = 16;
        public const int k_OffscreenShadowMapResolution = 64;
        public const int k_MaxShadowMapResolution = 16384;

        List<HDShadowData>          m_ShadowDatas = new List<HDShadowData>();
        NativeArray<HDShadowRequestHandle>     m_ShadowRequests;
        NativeList<HDShadowResolutionRequest> m_ShadowResolutionRequestStorage;
        HDDirectionalShadowData[]   m_CachedDirectionalShadowData;

        public HDDirectionalShadowData     m_DirectionalShadowData;
        public int                         m_CascadeCount;
        public int                         m_ShadowResolutionRequestCounter;

        // Structured buffer of shadow datas
        ComputeBuffer m_ShadowDataBuffer;
        ComputeBuffer m_DirectionalShadowDataBuffer;

        // The two shadowmaps atlases we uses, one for directional cascade (without resize) and the second for the rest of the shadows
        HDDynamicShadowAtlas m_CascadeAtlas;
        HDDynamicShadowAtlas m_Atlas;
        HDDynamicShadowAtlas m_AreaLightShadowAtlas;

        HDDynamicShadowAtlasDataForShadowRequestUpdateJob m_emptyAreaLightShadowAtlasJob;

        Material                    m_ClearShadowMaterial;
        Material                    m_BlitShadowMaterial;
        MaterialPropertyBlock       m_BlitShadowPropertyBlock = new MaterialPropertyBlock();

        ConstantBuffer<ShaderVariablesGlobal> m_GlobalShaderVariables;

        private int m_MaxShadowRequests;
        private int m_ShadowRequestCount;

        private static HDShadowManager s_Instance = new HDShadowManager();

        public static HDShadowManager instance { get { return s_Instance; } }
        public static HDCachedShadowManager cachedShadowManager {  get { return HDCachedShadowManager.instance; } }

        internal NativeList<HDShadowResolutionRequest> shadowResolutionRequestStorage => m_ShadowResolutionRequestStorage;

        private HDShadowManager()
        {
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += () => DisposeNativeCollections();
            UnityEditor.EditorApplication.quitting += () => DisposeNativeCollections();
#else
            Application.quitting += () => DisposeNativeCollections();
#endif
        }

        private void DisposeNativeCollections()
        {
            if (m_Atlas != null)
            {
                m_Atlas.DisposeNativeCollections();
            }

            if (m_CascadeAtlas != null)
            {
                m_CascadeAtlas.DisposeNativeCollections();
            }

            if (m_AreaLightShadowAtlas != null)
            {
                m_AreaLightShadowAtlas.DisposeNativeCollections();
            }

            m_emptyAreaLightShadowAtlasJob.DisposeNativeCollections();

            if (m_ShadowRequests.IsCreated)
            {
                m_ShadowRequests.Dispose();
                m_ShadowRequests = default;
            }

            if (m_ShadowResolutionRequestStorage.IsCreated)
            {
                m_ShadowResolutionRequestStorage.Dispose();
                m_ShadowResolutionRequestStorage = default;
            }

            if (cachedShadowManager != null)
            {
                cachedShadowManager.DisposeNativeCollections();
            }
        }

        public void InitShadowManager(HDRenderPipeline renderPipeline, HDShadowInitParameters initParams, RenderGraph renderGraph)
        {
            m_DirectionalShadowData = default;
            m_CascadeCount = 0;
            m_ShadowResolutionRequestCounter = 0;

            // Even when shadows are disabled (maxShadowRequests == 0) we need to allocate compute buffers to avoid having
            // resource not bound errors when dispatching a compute shader.
            if (initParams.maxShadowRequests > 65536)
            {
                initParams.maxShadowRequests = 65536;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("The 'Maximum Shadows on Screen' value has been clamped to 65536 in order not to exceed the maximum size of the buffer.");
#endif
            }
            m_ShadowDataBuffer = new ComputeBuffer(Mathf.Max(initParams.maxShadowRequests, 1), System.Runtime.InteropServices.Marshal.SizeOf(typeof(HDShadowData)));
            m_DirectionalShadowDataBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(HDDirectionalShadowData)));
            m_MaxShadowRequests = initParams.maxShadowRequests;
            m_ShadowRequestCount = 0;

            if (initParams.maxShadowRequests == 0)
                return;

            m_ClearShadowMaterial = CoreUtils.CreateEngineMaterial(renderPipeline.runtimeShaders.shadowClearPS);
            m_BlitShadowMaterial = CoreUtils.CreateEngineMaterial(renderPipeline.runtimeShaders.shadowBlitPS);

            // Prevent the list from resizing their internal container when we add shadow requests
            m_ShadowDatas.Capacity = Math.Max(initParams.maxShadowRequests, m_ShadowDatas.Capacity);

            if (!m_ShadowResolutionRequestStorage.IsCreated)
            {
                m_ShadowResolutionRequestStorage = new NativeList<HDShadowResolutionRequest>(initParams.maxShadowRequests, Allocator.Persistent);
                m_ShadowResolutionRequestStorage.Length = initParams.maxShadowRequests;
            }
            else
            {
                m_ShadowResolutionRequestStorage.Clear();
                m_ShadowResolutionRequestStorage.Length = initParams.maxShadowRequests;
            }

            if (m_ShadowRequests.IsCreated)
            {
                m_ShadowRequests.Dispose();
            }
            m_ShadowRequests = new NativeArray<HDShadowRequestHandle>(initParams.maxShadowRequests, Allocator.Persistent);
            m_CachedDirectionalShadowData = new HDDirectionalShadowData[1]; // we only support directional light shadow

            m_GlobalShaderVariables = new ConstantBuffer<ShaderVariablesGlobal>();

            var punctualAtlasInitParams = new HDShadowAtlas.HDShadowAtlasInitParameters(
                renderPipeline,
                renderGraph,
                useSharedTexture: false,
                initParams.punctualLightShadowAtlas.shadowAtlasResolution,
                initParams.punctualLightShadowAtlas.shadowAtlasResolution,
                m_ClearShadowMaterial,
                initParams.maxShadowRequests,
                initParams, m_GlobalShaderVariables)
            {
                name = "Shadow Map Atlas"
            };

            if (m_Atlas != null)
                m_Atlas.DisposeNativeCollections();

            // The cascade atlas will be allocated only if there is a directional light
            m_Atlas = new HDDynamicShadowAtlas(punctualAtlasInitParams);
            // Cascade atlas render texture will only be allocated if there is a shadow casting directional light
            HDShadowAtlas.BlurAlgorithm cascadeBlur = GetDirectionalShadowAlgorithm() == DirectionalShadowAlgorithm.IMS ? HDShadowAtlas.BlurAlgorithm.IM : HDShadowAtlas.BlurAlgorithm.None;

            HDShadowAtlas.HDShadowAtlasInitParameters dirAtlasInitParams = punctualAtlasInitParams;
            dirAtlasInitParams.useSharedTexture = true;
            dirAtlasInitParams.width = 1;
            dirAtlasInitParams.height = 1;
            dirAtlasInitParams.blurAlgorithm = cascadeBlur;
            dirAtlasInitParams.depthBufferBits = initParams.directionalShadowsDepthBits;
            dirAtlasInitParams.name = "Cascade Shadow Map Atlas";

            if (m_CascadeAtlas != null)
                m_CascadeAtlas.DisposeNativeCollections();

            m_CascadeAtlas = new HDDynamicShadowAtlas(dirAtlasInitParams);

            HDShadowAtlas.HDShadowAtlasInitParameters areaAtlasInitParams = punctualAtlasInitParams;
            if (ShaderConfig.s_AreaLights == 1)
            {
                areaAtlasInitParams.useSharedTexture = false;
                areaAtlasInitParams.width = initParams.areaLightShadowAtlas.shadowAtlasResolution;
                areaAtlasInitParams.height = initParams.areaLightShadowAtlas.shadowAtlasResolution;
                areaAtlasInitParams.blurAlgorithm = GetAreaLightShadowBlurAlgorithm();
                areaAtlasInitParams.depthBufferBits = initParams.areaLightShadowAtlas.shadowAtlasDepthBits;
                areaAtlasInitParams.name = "Area Light Shadow Map Atlas";

                if (m_AreaLightShadowAtlas != null)
                    m_AreaLightShadowAtlas.DisposeNativeCollections();

                m_AreaLightShadowAtlas = new HDDynamicShadowAtlas(areaAtlasInitParams);
            }
            else
            {
                m_emptyAreaLightShadowAtlasJob.DisposeNativeCollections();
                m_emptyAreaLightShadowAtlasJob.initEmpty();
            }

            HDShadowAtlas.HDShadowAtlasInitParameters cachedPunctualAtlasInitParams = punctualAtlasInitParams;
            cachedPunctualAtlasInitParams.useSharedTexture = true;
            cachedPunctualAtlasInitParams.width = initParams.cachedPunctualLightShadowAtlas;
            cachedPunctualAtlasInitParams.height = initParams.cachedPunctualLightShadowAtlas;
            cachedPunctualAtlasInitParams.name = "Cached Shadow Map Atlas";
            cachedPunctualAtlasInitParams.isShadowCache = true;

            cachedShadowManager.InitPunctualShadowAtlas(cachedPunctualAtlasInitParams);
            if (ShaderConfig.s_AreaLights == 1)
            {
                HDShadowAtlas.HDShadowAtlasInitParameters cachedAreaAtlasInitParams = areaAtlasInitParams;
                cachedAreaAtlasInitParams.useSharedTexture = true;
                cachedAreaAtlasInitParams.width = initParams.cachedAreaLightShadowAtlas;
                cachedAreaAtlasInitParams.height = initParams.cachedAreaLightShadowAtlas;
                cachedAreaAtlasInitParams.name = "Cached Area Light Shadow Map Atlas";
                cachedAreaAtlasInitParams.isShadowCache = true;

                cachedShadowManager.InitAreaLightShadowAtlas(cachedAreaAtlasInitParams);
            }

            cachedShadowManager.InitDirectionalState(dirAtlasInitParams, initParams.allowDirectionalMixedCachedShadows);
        }

        public void Cleanup(RenderGraph renderGraph)
        {
            m_ShadowDataBuffer.Dispose();
            m_DirectionalShadowDataBuffer.Dispose();

            if (m_MaxShadowRequests == 0)
                return;

            m_Atlas.Release(renderGraph);
            if (ShaderConfig.s_AreaLights == 1)
                m_AreaLightShadowAtlas.Release(renderGraph);
            m_CascadeAtlas.Release(renderGraph);

            CoreUtils.Destroy(m_ClearShadowMaterial);
            cachedShadowManager.Cleanup(renderGraph);

            m_GlobalShaderVariables.Release();

            m_ShadowDataBuffer.Dispose();
            m_DirectionalShadowDataBuffer.Dispose();
            m_GlobalShaderVariables.Release();
        }

        // Keep in sync with both HDShadowSampling.hlsl
        public static DirectionalShadowAlgorithm GetDirectionalShadowAlgorithm()
        {
            switch (HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.directionalShadowFilteringQuality)
            {
                case HDShadowFilteringQuality.Low:
                {
                    return DirectionalShadowAlgorithm.PCF5x5;
                }
                case HDShadowFilteringQuality.Medium:
                {
                    return DirectionalShadowAlgorithm.PCF7x7;
                }
                case HDShadowFilteringQuality.High:
                {
                    return DirectionalShadowAlgorithm.PCSS;
                }
            }
            ;
            return DirectionalShadowAlgorithm.PCF5x5;
        }

        public static HDShadowAtlas.BlurAlgorithm GetAreaLightShadowBlurAlgorithm()
        {
            return HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.areaShadowFilteringQuality == HDAreaShadowFilteringQuality.High ?
                HDShadowAtlas.BlurAlgorithm.None : HDShadowAtlas.BlurAlgorithm.EVSM;
        }

        public void UpdateShaderVariablesGlobalCB(ref ShaderVariablesGlobal cb)
        {
            if (m_MaxShadowRequests == 0)
                return;

            cb._CascadeShadowCount = (uint)(m_CascadeCount + 1);
            cb._ShadowAtlasSize = new Vector4(m_Atlas.width, m_Atlas.height, 1.0f / m_Atlas.width, 1.0f / m_Atlas.height);
            cb._CascadeShadowAtlasSize = new Vector4(m_CascadeAtlas.width, m_CascadeAtlas.height, 1.0f / m_CascadeAtlas.width, 1.0f / m_CascadeAtlas.height);
            cb._CachedShadowAtlasSize = new Vector4(cachedShadowManager.punctualShadowAtlas.width, cachedShadowManager.punctualShadowAtlas.height, 1.0f / cachedShadowManager.punctualShadowAtlas.width, 1.0f / cachedShadowManager.punctualShadowAtlas.height);
            if (ShaderConfig.s_AreaLights == 1)
            {
                cb._AreaShadowAtlasSize = new Vector4(m_AreaLightShadowAtlas.width, m_AreaLightShadowAtlas.height, 1.0f / m_AreaLightShadowAtlas.width, 1.0f / m_AreaLightShadowAtlas.height);
                cb._CachedAreaShadowAtlasSize = new Vector4(cachedShadowManager.areaShadowAtlas.width, cachedShadowManager.areaShadowAtlas.height, 1.0f / cachedShadowManager.areaShadowAtlas.width, 1.0f / cachedShadowManager.areaShadowAtlas.height);
            }
        }

        public void UpdateDirectionalShadowResolution(int resolution, int cascadeCount)
        {
            Vector2Int atlasResolution = new Vector2Int(resolution, resolution);

            if (cascadeCount > 1)
                atlasResolution.x *= 2;
            if (cascadeCount > 2)
                atlasResolution.y *= 2;

            m_CascadeAtlas.UpdateSize(atlasResolution);
            if (cachedShadowManager.DirectionalHasCachedAtlas())
                cachedShadowManager.directionalLightAtlas.UpdateSize(atlasResolution);
        }

        internal int ReserveShadowResolutions(Vector2 resolution, ShadowMapType shadowMapType, int lightID, int index, ShadowMapUpdateType updateType)
        {
            if (m_ShadowRequestCount >= m_MaxShadowRequests)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("Max shadow requests count reached, dropping all exceeding requests. You can increase this limit by changing the Maximum Shadows on Screen property in the HDRP asset.");
#endif
                return -1;
            }

            ref var resolutionRequest = ref m_ShadowResolutionRequestStorage.ElementAt(m_ShadowResolutionRequestCounter);
            var resolutionRequestHandle = new HDShadowResolutionRequestHandle() { index = m_ShadowResolutionRequestCounter };
            resolutionRequest.shadowMapType = shadowMapType;

            // Note: for cached shadows we manage the resolution requests directly on the CachedShadowAtlas as they need special handling. We however keep incrementing the counter for two reasons:
            //      - Maintain the limit of m_MaxShadowRequests
            //      - Avoid to refactor other parts that the shadow manager that get requests indices from here.

            if (updateType != ShadowMapUpdateType.Cached || shadowMapType == ShadowMapType.CascadedDirectional)
            {
                resolutionRequest.resolution = resolution;
                resolutionRequest.dynamicAtlasViewport.width = resolution.x;
                resolutionRequest.dynamicAtlasViewport.height = resolution.y;

                switch (shadowMapType)
                {
                    case ShadowMapType.PunctualAtlas:
                        m_Atlas.ReserveResolution(resolutionRequestHandle);
                        break;
                    case ShadowMapType.AreaLightAtlas:
                        m_AreaLightShadowAtlas.ReserveResolution(resolutionRequestHandle);
                        break;
                    case ShadowMapType.CascadedDirectional:
                        m_CascadeAtlas.ReserveResolution(resolutionRequestHandle);
                        break;
                }
            }


            m_ShadowResolutionRequestCounter++;
            m_ShadowRequestCount = m_ShadowResolutionRequestCounter;

            return m_ShadowResolutionRequestCounter - 1;
        }

        internal HDShadowResolutionRequestHandle GetResolutionRequestHandle(int index)
        {
            if (index < 0 || index >= m_ShadowRequestCount)
                return HDShadowResolutionRequestHandle.Invalid;

            return new HDShadowResolutionRequestHandle(){index = index};
        }

        internal static HDShadowResolutionRequestHandle GetResolutionRequestHandle(int index, int shadowRequestCount)
        {
            if (index < 0 || index >= shadowRequestCount)
                return HDShadowResolutionRequestHandle.Invalid;

            return new HDShadowResolutionRequestHandle(){index = index};
        }

        public Vector2 GetReservedResolution(int index)
        {
            if (index < 0 || index >= m_ShadowRequestCount)
                return Vector2.zero;

            return m_ShadowResolutionRequestStorage[index].resolution;
        }

        public void UpdateCascade(int cascadeIndex, Vector4 cullingSphere, float border)
        {
            if (cullingSphere.w != float.NegativeInfinity)
            {
                cullingSphere.w *= cullingSphere.w;
            }

            m_CascadeCount = Mathf.Max(m_CascadeCount, cascadeIndex);

            unsafe
            {
                ref HDDirectionalShadowData shadowData = ref m_DirectionalShadowData;
                fixed (float * sphereCascadesBuffer = shadowData.sphereCascades)
                    ((Vector4 *)sphereCascadesBuffer)[cascadeIndex] = cullingSphere;
                fixed (float * cascadeBorders = shadowData.cascadeBorders)
                    cascadeBorders[cascadeIndex] = border;
            }
        }

        HDShadowData CreateShadowData(ref HDShadowRequest shadowRequest, HDShadowAtlas atlas)
        {
            HDShadowData data = new HDShadowData();

            var view = shadowRequest.cullingSplit.view;
            data.proj = shadowRequest.cullingSplit.deviceProjection;
            data.pos = shadowRequest.position;
            data.rot0 = new Vector3(view.m00, view.m01, view.m02);
            data.rot1 = new Vector3(view.m10, view.m11, view.m12);
            data.rot2 = new Vector3(view.m20, view.m21, view.m22);
            data.shadowToWorld = shadowRequest.shadowToWorld;
            data.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);

            var viewport = shadowRequest.isInCachedAtlas ? shadowRequest.cachedAtlasViewport : shadowRequest.dynamicAtlasViewport;

            // Compute the scale and offset (between 0 and 1) for the atlas coordinates
            float rWidth = 1.0f / atlas.width;
            float rHeight = 1.0f / atlas.height;
            data.atlasOffset = Vector2.Scale(new Vector2(rWidth, rHeight), new Vector2(viewport.x, viewport.y));

            data.shadowMapSize = new Vector4(viewport.width, viewport.height, 1.0f / viewport.width, 1.0f / viewport.height);

            data.normalBias = shadowRequest.normalBias;
            data.worldTexelSize = shadowRequest.worldTexelSize;

            data.shadowFilterParams0.x = shadowRequest.shadowSoftness;
            data.shadowFilterParams0.y = HDShadowUtils.Asfloat(shadowRequest.blockerSampleCount);
            data.shadowFilterParams0.z = HDShadowUtils.Asfloat(shadowRequest.filterSampleCount);
            data.shadowFilterParams0.w = shadowRequest.minFilterSize;

            data.dirLightPCSSParams0.x = shadowRequest.dirLightPCSSDepth2RadialScale;
            data.dirLightPCSSParams0.y = shadowRequest.dirLightPCSSRadial2DepthScale;
            data.dirLightPCSSParams0.z = shadowRequest.dirLightPCSSMaxBlockerDistance;
            data.dirLightPCSSParams0.w = shadowRequest.dirLightPCSSMaxSamplingDistance;
            data.dirLightPCSSParams1.x = shadowRequest.dirLightPCSSMinFilterSizeTexels;
            data.dirLightPCSSParams1.y = shadowRequest.dirLightPCSSMinFilterRadial2DepthScale;
            data.dirLightPCSSParams1.z = shadowRequest.dirLightPCSSBlockerRadial2DepthScale;
            data.dirLightPCSSParams1.w = shadowRequest.dirLightPCSSBlockerSamplingClumpExponent;

            data.zBufferParam = shadowRequest.zBufferParam;
            if (atlas.HasBlurredEVSM())
            {
                data.shadowFilterParams0 = shadowRequest.evsmParams;
            }

            data.isInCachedAtlas = shadowRequest.isInCachedAtlas ? 1.0f : 0.0f;

            return data;
        }

        unsafe Vector4 GetCascadeSphereAtIndex(int index)
        {
            fixed (float * sphereCascadesBuffer = m_DirectionalShadowData.sphereCascades)
            {
                return ((Vector4*)sphereCascadesBuffer)[index];
            }
        }

        public unsafe void LayoutShadowMaps(LightingDebugSettings lightingDebugSettings)
        {
            if (m_MaxShadowRequests == 0)
                return;

            cachedShadowManager.UpdateDebugSettings(lightingDebugSettings);

            m_Atlas.UpdateDebugSettings(lightingDebugSettings);

            if (m_CascadeAtlas != null)
                m_CascadeAtlas.UpdateDebugSettings(lightingDebugSettings);

            if (ShaderConfig.s_AreaLights == 1)
                m_AreaLightShadowAtlas.UpdateDebugSettings(lightingDebugSettings);

            if (lightingDebugSettings.shadowResolutionScaleFactor != 1.0f)
            {
                ref UnsafeList<HDShadowResolutionRequest> resolutionRequests = ref *m_ShadowResolutionRequestStorage.GetUnsafeList();
                int resolutionRequestCount = m_ShadowResolutionRequestStorage.Length;
                for (int i = 0; i < resolutionRequestCount; i++)
                {
                    ref HDShadowResolutionRequest shadowResolutionRequest = ref resolutionRequests.ElementAt(i);

                    // We don't rescale the directional shadows with the global shadow scale factor
                    // because there is no dynamic atlas rescale when it overflow.
                    if (shadowResolutionRequest.shadowMapType != ShadowMapType.CascadedDirectional)
                        shadowResolutionRequest.resolution *= lightingDebugSettings.shadowResolutionScaleFactor;
                }
            }

            // Assign a position to all the shadows in the atlas, and scale shadows if needed
            if (m_CascadeAtlas != null && !m_CascadeAtlas.Layout(false))
                Debug.LogError("Cascade Shadow atlasing has failed, only one directional light can cast shadows at a time");
            m_Atlas.Layout();
            if (ShaderConfig.s_AreaLights == 1)
                m_AreaLightShadowAtlas.Layout();
        }

        unsafe public void PrepareGPUShadowDatas(CullingResults cullResults, HDCamera camera)
        {
            if (m_MaxShadowRequests == 0)
                return;

            int shadowIndex = 0;

            m_ShadowDatas.Clear();

            NativeList<HDShadowRequest> requestStorage = HDShadowRequestDatabase.instance.hdShadowRequestStorage;
            ref UnsafeList<HDShadowRequest> requestStorageUnsafe = ref *requestStorage.GetUnsafeList();

            HDShadowRequestHandle* shadowRequestsPtr = (HDShadowRequestHandle*)m_ShadowRequests.GetUnsafePtr();

            if (!m_ShadowRequests.IsCreated || m_ShadowRequests.Length < m_ShadowRequestCount)
                throw new IndexOutOfRangeException("Shadow request count is out of range of the shadow request array.");

            // Create all HDShadowDatas and update them with shadow request datas
            for (int i = 0; i < m_ShadowRequestCount; i++)
            {
                ref var shadowRequest = ref requestStorageUnsafe.ElementAt(shadowRequestsPtr[i].storageIndexForShadowRequest);

                HDShadowAtlas atlas = m_Atlas;
                if(shadowRequest.isInCachedAtlas)
                {
                    atlas = cachedShadowManager.punctualShadowAtlas;
                }

                if (shadowRequest.shadowMapType == ShadowMapType.CascadedDirectional)
                {
                    atlas = m_CascadeAtlas;
                }
                else if (shadowRequest.shadowMapType == ShadowMapType.AreaLightAtlas)
                {
                    atlas = m_AreaLightShadowAtlas;
                    if(shadowRequest.isInCachedAtlas)
                    {
                        atlas = cachedShadowManager.areaShadowAtlas;
                    }
                }

                HDShadowData shadowData;
                if (shadowRequest.shouldUseCachedShadowData)
                {
                    shadowData = shadowRequest.cachedShadowData;
                }
                else
                {
                    shadowData = CreateShadowData(ref shadowRequest, atlas);
                    shadowRequest.cachedShadowData = shadowData;
                }

                m_ShadowDatas.Add(shadowData);
                shadowIndex++;
            }

            int first = k_DirectionalShadowCascadeCount, second = k_DirectionalShadowCascadeCount;

            fixed (float *sphereBuffer = m_DirectionalShadowData.sphereCascades)
            {
                Vector4* sphere = (Vector4*)sphereBuffer;
                for (int i = 0; i < k_DirectionalShadowCascadeCount; i++)
                {
                    first = (first == k_DirectionalShadowCascadeCount && sphere[i].w > 0.0f) ? i : first;
                    second = ((second == k_DirectionalShadowCascadeCount || second == first) && sphere[i].w > 0.0f) ? i : second;
                }
            }

            // Update directional datas:
            if (second != k_DirectionalShadowCascadeCount)
                m_DirectionalShadowData.cascadeDirection = (GetCascadeSphereAtIndex(second) - GetCascadeSphereAtIndex(first)).normalized;
            else
                m_DirectionalShadowData.cascadeDirection = Vector4.zero;

            HDShadowSettings shadowSettings = camera.volumeStack.GetComponent<HDShadowSettings>();
            m_DirectionalShadowData.cascadeDirection.w = shadowSettings.cascadeShadowSplitCount.value;

            GetShadowFadeScaleAndBias(shadowSettings, out m_DirectionalShadowData.fadeScale, out m_DirectionalShadowData.fadeBias);

            if (m_ShadowRequestCount > 0)
            {
                // Upload the shadow buffers to GPU
                m_ShadowDataBuffer.SetData(m_ShadowDatas);
                m_CachedDirectionalShadowData[0] = m_DirectionalShadowData;
                m_DirectionalShadowDataBuffer.SetData(m_CachedDirectionalShadowData);
            }
        }

        void GetShadowFadeScaleAndBias(HDShadowSettings shadowSettings, out float scale, out float bias)
        {
            float maxShadowDistance = shadowSettings.maxShadowDistance.value;
            float maxShadowDistanceSq = maxShadowDistance * maxShadowDistance;
            float cascadeBorder;
            int splitCount = shadowSettings.cascadeShadowSplitCount.value;
            if (splitCount == 4)
                cascadeBorder = shadowSettings.cascadeShadowBorder3.value;
            else if (splitCount == 3)
                cascadeBorder = shadowSettings.cascadeShadowBorder2.value;
            else if (splitCount == 2)
                cascadeBorder = shadowSettings.cascadeShadowBorder1.value;
            else
                cascadeBorder = shadowSettings.cascadeShadowBorder0.value;

            GetScaleAndBiasForLinearDistanceFade(maxShadowDistanceSq, cascadeBorder, out scale, out bias);
        }

        void GetScaleAndBiasForLinearDistanceFade(float fadeDistance, float border, out float scale, out float bias)
        {
            // To avoid division from zero
            // This values ensure that fade within cascade will be 0 and outside 1
            if (border < 0.0001f)
            {
                float multiplier = 1000f; // To avoid blending if difference is in fractions
                scale = multiplier;
                bias = -fadeDistance * multiplier;
                return;
            }

            border = 1 - border;
            border *= border;

            // Fade with distance calculation is just a linear fade from 90% of fade distance to fade distance. 90% arbitrarily chosen but should work well enough.
            float distanceFadeNear = border * fadeDistance;
            scale = 1.0f / (fadeDistance - distanceFadeNear);
            bias = -distanceFadeNear / (fadeDistance - distanceFadeNear);
        }

        public void PushGlobalParameters(CommandBuffer cmd)
        {
            // This code must be in sync with HDShadowContext.hlsl
            cmd.SetGlobalBuffer(HDShaderIDs._HDShadowDatas, m_ShadowDataBuffer);
            cmd.SetGlobalBuffer(HDShaderIDs._HDDirectionalShadowData, m_DirectionalShadowDataBuffer);
        }

        public int GetShadowRequestCount()
        {
            return m_ShadowRequestCount;
        }
        
        internal void GetUnmanageDataForComputeCullingSplitsJob(ref HDShadowManagerDataForComputeCullingSplitsJob shadowManagerData)
        {
            shadowManagerData.requestCount = GetShadowRequestCount();
            shadowManagerData.cachedDirectionalAngles = cachedShadowManager.cachedDirectionalAngles;
            cachedShadowManager.GetUnmanagedDataForShadowRequestJobs(ref shadowManagerData.cachedShadowManager);
        }

        internal void GetUnmanageDataForShadowRequestJobs(ref HDShadowManagerDataForShadowRequestUpateJob shadowManagerData)
        {
            shadowManagerData.shadowRequests = m_ShadowRequests;
            shadowManagerData.shadowResolutionRequestStorage = m_ShadowResolutionRequestStorage;
            shadowManagerData.areaShadowFilteringQuality = HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.areaShadowFilteringQuality;
            m_Atlas.GetUnmanageDataForShadowRequestJobs(ref shadowManagerData.atlas);
            m_CascadeAtlas.GetUnmanageDataForShadowRequestJobs(ref shadowManagerData.cascadeShadowAtlas);
            if (ShaderConfig.s_AreaLights == 1)
                m_AreaLightShadowAtlas.GetUnmanageDataForShadowRequestJobs(ref shadowManagerData.areaShadowAtlas);
            else
                shadowManagerData.areaShadowAtlas = m_emptyAreaLightShadowAtlasJob;
            cachedShadowManager.GetUnmanagedDataForShadowRequestJobs(ref shadowManagerData.cachedShadowManager);
        }

        public void Clear()
        {
            if (m_MaxShadowRequests == 0)
                return;

            // Clear the shadows atlas infos and requests
            m_Atlas.Clear();
            m_CascadeAtlas.Clear();
            if (ShaderConfig.s_AreaLights == 1)
                m_AreaLightShadowAtlas.Clear();

            cachedShadowManager.ClearShadowRequests();

            m_ShadowResolutionRequestCounter = 0;

            m_ShadowRequestCount = 0;
            m_CascadeCount = 0;
        }

        // Warning: must be called after ProcessShadowRequests and RenderShadows to have valid informations
        public void DisplayShadowAtlas(RTHandle atlasTexture, CommandBuffer cmd, Material debugMaterial, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, MaterialPropertyBlock mpb)
        {
            m_Atlas.DisplayAtlas(atlasTexture, cmd, debugMaterial, new Rect(0, 0, m_Atlas.width, m_Atlas.height), screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, mpb);
        }

        // Warning: must be called after ProcessShadowRequests and RenderShadows to have valid informations
        public void DisplayShadowCascadeAtlas(RTHandle atlasTexture, CommandBuffer cmd, Material debugMaterial, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, MaterialPropertyBlock mpb)
        {
            m_CascadeAtlas.DisplayAtlas(atlasTexture, cmd, debugMaterial, new Rect(0, 0, m_CascadeAtlas.width, m_CascadeAtlas.height), screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, mpb);
        }

        // Warning: must be called after ProcessShadowRequests and RenderShadows to have valid informations
        public void DisplayAreaLightShadowAtlas(RTHandle atlasTexture, CommandBuffer cmd, Material debugMaterial, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, MaterialPropertyBlock mpb)
        {
            if (ShaderConfig.s_AreaLights == 1)
                m_AreaLightShadowAtlas.DisplayAtlas(atlasTexture, cmd, debugMaterial, new Rect(0, 0, m_AreaLightShadowAtlas.width, m_AreaLightShadowAtlas.height), screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, mpb);
        }

        public void DisplayCachedPunctualShadowAtlas(RTHandle atlasTexture, CommandBuffer cmd, Material debugMaterial, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, MaterialPropertyBlock mpb)
        {
            cachedShadowManager.punctualShadowAtlas.DisplayAtlas(atlasTexture, cmd, debugMaterial, new Rect(0, 0, cachedShadowManager.punctualShadowAtlas.width, cachedShadowManager.punctualShadowAtlas.height), screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, mpb);
        }

        public void DisplayCachedAreaShadowAtlas(RTHandle atlasTexture, CommandBuffer cmd, Material debugMaterial, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, MaterialPropertyBlock mpb)
        {
            if (ShaderConfig.s_AreaLights == 1)
                cachedShadowManager.areaShadowAtlas.DisplayAtlas(atlasTexture, cmd, debugMaterial, new Rect(0, 0, cachedShadowManager.areaShadowAtlas.width, cachedShadowManager.areaShadowAtlas.height), screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, mpb);
        }

        // Warning: must be called after ProcessShadowRequests and RenderShadows to have valid informations
        public unsafe void DisplayShadowMap(in ShadowResult atlasTextures, int shadowIndex, CommandBuffer cmd, Material debugMaterial, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, MaterialPropertyBlock mpb)
        {
            if (shadowIndex >= m_ShadowRequestCount)
                return;

            NativeList<HDShadowRequest> requestStorage = HDShadowRequestDatabase.instance.hdShadowRequestStorage;
            ref UnsafeList<HDShadowRequest> requestStorageUnsafe = ref *requestStorage.GetUnsafeList();

            HDShadowRequestHandle   shadowRequestHandle = m_ShadowRequests[shadowIndex];
            ref var shadowRequest = ref requestStorageUnsafe.ElementAt(shadowRequestHandle.storageIndexForShadowRequest);

            switch (shadowRequest.shadowMapType)
            {
                case ShadowMapType.PunctualAtlas:
                {
                    if (shadowRequest.isInCachedAtlas)
                        cachedShadowManager.punctualShadowAtlas.DisplayAtlas(atlasTextures.cachedPunctualShadowResult, cmd, debugMaterial, shadowRequest.cachedAtlasViewport, screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, mpb);
                    else
                        m_Atlas.DisplayAtlas(atlasTextures.punctualShadowResult, cmd, debugMaterial, shadowRequest.dynamicAtlasViewport, screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, mpb);
                    break;
                }
                case ShadowMapType.CascadedDirectional:
                {
                    m_CascadeAtlas.DisplayAtlas(atlasTextures.directionalShadowResult, cmd, debugMaterial, shadowRequest.dynamicAtlasViewport, screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, mpb);
                    break;
                }
                case ShadowMapType.AreaLightAtlas:
                {
                    if (ShaderConfig.s_AreaLights == 1)
                    {
                        if (shadowRequest.isInCachedAtlas)
                            cachedShadowManager.areaShadowAtlas.DisplayAtlas(atlasTextures.cachedAreaShadowResult, cmd, debugMaterial, shadowRequest.cachedAtlasViewport, screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, mpb);
                        else
                            m_AreaLightShadowAtlas.DisplayAtlas(atlasTextures.areaShadowResult, cmd, debugMaterial, shadowRequest.dynamicAtlasViewport, screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, mpb);
                    }
                    break;
                }
            }
            ;
        }

        internal static ShadowResult ReadShadowResult(in ShadowResult shadowResult, RenderGraphBuilder builder)
        {
            var result = new ShadowResult();

            if (shadowResult.punctualShadowResult.IsValid())
                result.punctualShadowResult = builder.ReadTexture(shadowResult.punctualShadowResult);
            if (shadowResult.directionalShadowResult.IsValid())
                result.directionalShadowResult = builder.ReadTexture(shadowResult.directionalShadowResult);
            if (shadowResult.areaShadowResult.IsValid())
                result.areaShadowResult = builder.ReadTexture(shadowResult.areaShadowResult);
            if (shadowResult.cachedPunctualShadowResult.IsValid())
                result.cachedPunctualShadowResult = builder.ReadTexture(shadowResult.cachedPunctualShadowResult);
            if (shadowResult.cachedAreaShadowResult.IsValid())
                result.cachedAreaShadowResult = builder.ReadTexture(shadowResult.cachedAreaShadowResult);

            return result;
        }

        internal void RenderShadows(RenderGraph renderGraph, in ShaderVariablesGlobal globalCB, HDCamera hdCamera, CullingResults cullResults, ref ShadowResult result)
        {
            InvalidateAtlasOutputsIfNeeded();

            // Avoid to do any commands if there is no shadow to draw
            if (m_ShadowRequestCount != 0 &&
                (hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects) || hdCamera.frameSettings.IsEnabled(FrameSettingsField.TransparentObjects)))
            {
                // Punctual
                result.cachedPunctualShadowResult = cachedShadowManager.punctualShadowAtlas.RenderShadows(renderGraph, cullResults, globalCB, hdCamera.frameSettings, "Cached Punctual Lights Shadows rendering");
                BlitCachedShadows(renderGraph, ShadowMapType.PunctualAtlas);
                result.punctualShadowResult = m_Atlas.RenderShadows(renderGraph, cullResults, globalCB, hdCamera.frameSettings, "Punctual Lights Shadows rendering");

                if (ShaderConfig.s_AreaLights == 1)
                {
                    cachedShadowManager.areaShadowAtlas.RenderShadowMaps(renderGraph, cullResults, globalCB, hdCamera.frameSettings, "Cached Area Lights Shadows rendering");
                    BlitCachedShadows(renderGraph, ShadowMapType.AreaLightAtlas);
                    m_AreaLightShadowAtlas.RenderShadowMaps(renderGraph, cullResults, globalCB, hdCamera.frameSettings, "Area Light Shadows rendering");
                    result.areaShadowResult = m_AreaLightShadowAtlas.BlurShadows(renderGraph);
                    result.cachedAreaShadowResult = cachedShadowManager.areaShadowAtlas.BlurShadows(renderGraph);
                }

                if (cachedShadowManager.DirectionalHasCachedAtlas())
                {

                    if (cachedShadowManager.directionalLightAtlas.HasShadowRequests())
                    {
                        cachedShadowManager.UpdateDirectionalCacheTexture(renderGraph);
                        cachedShadowManager.directionalLightAtlas.RenderShadows(renderGraph, cullResults, globalCB, hdCamera.frameSettings, "Cached Directional Lights Shadows rendering");
                    }
                    BlitCachedShadows(renderGraph, ShadowMapType.CascadedDirectional);
                }
                result.directionalShadowResult = m_CascadeAtlas.RenderShadows(renderGraph, cullResults, globalCB, hdCamera.frameSettings, "Directional Light Shadows rendering");
            }

            // TODO RENDERGRAPH
            // Not really good to bind things globally here (makes lifecycle of the textures fuzzy)
            // Probably better to bind it explicitly where needed (deferred lighting and forward/debug passes)
            // We can probably remove this when we have only one code path and can clean things up a bit.
            BindShadowGlobalResources(renderGraph, result);
        }

        internal void ReleaseSharedShadowAtlases(RenderGraph renderGraph)
        {
            if (cachedShadowManager.DirectionalHasCachedAtlas())
                cachedShadowManager.directionalLightAtlas.CleanupRenderGraphOutput(renderGraph);

            cachedShadowManager.punctualShadowAtlas.CleanupRenderGraphOutput(renderGraph);
            if (ShaderConfig.s_AreaLights == 1)
                cachedShadowManager.areaShadowAtlas.CleanupRenderGraphOutput(renderGraph);

            cachedShadowManager.DefragAtlas(LightType.Point);
            cachedShadowManager.DefragAtlas(LightType.Spot);
            if (ShaderConfig.s_AreaLights == 1)
                cachedShadowManager.DefragAtlas(LightType.Rectangle);
        }

        void InvalidateAtlasOutputsIfNeeded()
        {
            cachedShadowManager.punctualShadowAtlas.InvalidateOutputIfNeeded();
            m_Atlas.InvalidateOutputIfNeeded();
            m_CascadeAtlas.InvalidateOutputIfNeeded();
            if (cachedShadowManager.DirectionalHasCachedAtlas())
            {
                cachedShadowManager.directionalLightAtlas.InvalidateOutputIfNeeded();
            }
            if (ShaderConfig.s_AreaLights == 1)
            {
                cachedShadowManager.areaShadowAtlas.InvalidateOutputIfNeeded();
                m_AreaLightShadowAtlas.InvalidateOutputIfNeeded();
            }
        }

        class BindShadowGlobalResourcesPassData
        {
            public ShadowResult shadowResult;
        }


        static void BindAtlasTexture(RenderGraphContext ctx, TextureHandle texture, int shaderId)
        {
            if (texture.IsValid())
                ctx.cmd.SetGlobalTexture(shaderId, texture);
            else
                ctx.cmd.SetGlobalTexture(shaderId, ctx.defaultResources.defaultShadowTexture);
        }

        void BindShadowGlobalResources(RenderGraph renderGraph, in ShadowResult shadowResult)
        {
            using (var builder = renderGraph.AddRenderPass<BindShadowGlobalResourcesPassData>("BindShadowGlobalResources", out var passData))
            {
                passData.shadowResult = ReadShadowResult(shadowResult, builder);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(
                    (BindShadowGlobalResourcesPassData data, RenderGraphContext ctx) =>
                    {
                        BindAtlasTexture(ctx, data.shadowResult.punctualShadowResult, HDShaderIDs._ShadowmapAtlas);
                        BindAtlasTexture(ctx, data.shadowResult.directionalShadowResult, HDShaderIDs._ShadowmapCascadeAtlas);
                        BindAtlasTexture(ctx, data.shadowResult.areaShadowResult, HDShaderIDs._ShadowmapAreaAtlas);
                        BindAtlasTexture(ctx, data.shadowResult.cachedPunctualShadowResult, HDShaderIDs._CachedShadowmapAtlas);
                        BindAtlasTexture(ctx, data.shadowResult.cachedAreaShadowResult, HDShaderIDs._CachedAreaLightShadowmapAtlas);
                    });
            }
        }

        internal static void BindDefaultShadowGlobalResources(RenderGraph renderGraph)
        {
            using (var builder = renderGraph.AddRenderPass<BindShadowGlobalResourcesPassData>("BindDefaultShadowGlobalResources", out var passData))
            {
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(
                    (BindShadowGlobalResourcesPassData data, RenderGraphContext ctx) =>
                    {
                        BindAtlasTexture(ctx, ctx.defaultResources.defaultShadowTexture, HDShaderIDs._ShadowmapAtlas);
                        BindAtlasTexture(ctx, ctx.defaultResources.defaultShadowTexture, HDShaderIDs._ShadowmapCascadeAtlas);
                        BindAtlasTexture(ctx, ctx.defaultResources.defaultShadowTexture, HDShaderIDs._ShadowmapAreaAtlas);
                        BindAtlasTexture(ctx, ctx.defaultResources.defaultShadowTexture, HDShaderIDs._CachedShadowmapAtlas);
                        BindAtlasTexture(ctx, ctx.defaultResources.defaultShadowTexture, HDShaderIDs._CachedAreaLightShadowmapAtlas);
                    });
            }
        }

        void BlitCachedShadows(RenderGraph renderGraph)
        {
            m_Atlas.BlitCachedIntoAtlas(renderGraph, cachedShadowManager.punctualShadowAtlas.GetOutputTexture(renderGraph), new Vector2Int(cachedShadowManager.punctualShadowAtlas.width, cachedShadowManager.punctualShadowAtlas.height), m_BlitShadowMaterial, "Blit Punctual Mixed Cached Shadows", HDProfileId.BlitPunctualMixedCachedShadowMaps);
            if (cachedShadowManager.DirectionalHasCachedAtlas())
            {
                m_CascadeAtlas.BlitCachedIntoAtlas(renderGraph, cachedShadowManager.directionalLightAtlas.GetOutputTexture(renderGraph), new Vector2Int(cachedShadowManager.directionalLightAtlas.width, cachedShadowManager.directionalLightAtlas.height), m_BlitShadowMaterial, "Blit Directional Mixed Cached Shadows", HDProfileId.BlitDirectionalMixedCachedShadowMaps);
            }

            if (ShaderConfig.s_AreaLights == 1)
            {
                m_AreaLightShadowAtlas.BlitCachedIntoAtlas(renderGraph, cachedShadowManager.areaShadowAtlas.GetOutputTexture(renderGraph), new Vector2Int(cachedShadowManager.areaShadowAtlas.width, cachedShadowManager.areaShadowAtlas.height), m_BlitShadowMaterial, "Blit Area Mixed Cached Shadows", HDProfileId.BlitAreaMixedCachedShadowMaps);
            }
        }

        void BlitCachedShadows(RenderGraph renderGraph, ShadowMapType shadowAtlas)
        {
            if (shadowAtlas == ShadowMapType.PunctualAtlas)
                m_Atlas.BlitCachedIntoAtlas(renderGraph, cachedShadowManager.punctualShadowAtlas.GetOutputTexture(renderGraph), new Vector2Int(cachedShadowManager.punctualShadowAtlas.width, cachedShadowManager.punctualShadowAtlas.height), m_BlitShadowMaterial, "Blit Punctual Mixed Cached Shadows", HDProfileId.BlitPunctualMixedCachedShadowMaps);
            if (shadowAtlas == ShadowMapType.CascadedDirectional && cachedShadowManager.DirectionalHasCachedAtlas())
                m_CascadeAtlas.BlitCachedIntoAtlas(renderGraph, cachedShadowManager.directionalLightAtlas.GetOutputTexture(renderGraph), new Vector2Int(cachedShadowManager.directionalLightAtlas.width, cachedShadowManager.directionalLightAtlas.height), m_BlitShadowMaterial, "Blit Directional Mixed Cached Shadows", HDProfileId.BlitDirectionalMixedCachedShadowMaps);
            if (shadowAtlas == ShadowMapType.AreaLightAtlas && ShaderConfig.s_AreaLights == 1)
                m_AreaLightShadowAtlas.BlitCachedIntoAtlas(renderGraph, cachedShadowManager.areaShadowAtlas.GetShadowMapDepthTexture(renderGraph), new Vector2Int(cachedShadowManager.areaShadowAtlas.width, cachedShadowManager.areaShadowAtlas.height), m_BlitShadowMaterial, "Blit Area Mixed Cached Shadows", HDProfileId.BlitAreaMixedCachedShadowMaps);
        }
    }
}
