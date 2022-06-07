using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;

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
        High = 2,
    }

    // custom-begin:
    public
    // custom-end
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
    // custom-begin:
    public
    // custom-end
    struct HDShadowData
    {
        public Vector3      rot0;
        public Vector3      rot1;
        public Vector3      rot2;
        public Vector3      pos;
        public Vector4      proj;

        public Vector2      atlasOffset;
        public float        worldTexelSize;
        public float        normalBias;

        [SurfaceDataAttributes(precision = FieldPrecision.Real)]
        public Vector4      zBufferParam;
        public Vector4      shadowMapSize;

        public Vector4      shadowFilterParams0;

        public Vector3      cacheTranslationDelta;
        public float        isInCachedAtlas;

        public Matrix4x4    shadowToWorld;
    }

    // We use a different structure for directional light because these is a lot of data there
    // and it will add too much useless stuff for other lights
    // Note: In order to support HLSL array generation, we need to use fixed arrays and so a unsafe context for this struct
    [GenerateHLSL(needAccessors = false)]
    unsafe struct HDDirectionalShadowData
    {
        // We can't use Vector4 here because the vector4[] makes this struct non blittable
        [HLSLArray(4, typeof(Vector4))]
        public fixed float      sphereCascades[4 * 4];

        [SurfaceDataAttributes(precision = FieldPrecision.Real)]
        public Vector4          cascadeDirection;

        [HLSLArray(4, typeof(float))]
        [SurfaceDataAttributes(precision = FieldPrecision.Real)]
        public fixed float      cascadeBorders[4];
    }
    // custom-begin:
    public
        // custom-end
        struct HDShadowRequestHandle
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

    // custom-begin:
    public
        // custom-end
        struct HDShadowRequestSetHandle
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

    // custom-begin:
    public
    // custom-end
    struct HDShadowRequest
    {
        public const int maxLightShadowRequestsCount = 6;
        public const int frustumPlanesCount = 6;
        public Matrix4x4            view;
        // Use the y flipped device projection matrix as light projection matrix
        public Matrix4x4            deviceProjectionYFlip;
        public Matrix4x4            deviceProjection;
        public Matrix4x4            projection;
        public Matrix4x4            shadowToWorld;
        public Vector3              position;
        public Vector4              zBufferParam;
        // Warning: these viewport fields are updated by ProcessShadowRequests and are invalid before
        public Rect                 dynamicAtlasViewport;
        public Rect                 cachedAtlasViewport;
        public bool                 zClip;

        // Store the final shadow indice in the shadow data array
        // Warning: the index is computed during ProcessShadowRequest and so is invalid before calling this function
        public int                  shadowIndex;

        // Determine in which atlas the shadow will be rendered
        public ShadowMapType        shadowMapType;

        // TODO: Remove these field once scriptable culling is here (currently required by ScriptableRenderContext.DrawShadows)
        public int                  lightIndex;
        public ShadowSplitData      splitData;
        // end

        public float                normalBias;
        public float                worldTexelSize;
        public float                slopeBias;

        // PCSS parameters
        public float                shadowSoftness;
        public int                  blockerSampleCount;
        public int                  filterSampleCount;
        public float                minFilterSize;

        // IMS parameters
        public float                kernelSize;
        public float                lightAngle;
        public float                maxDepthBias;

        public Vector4              evsmParams;

        public HDShadowData cachedShadowData;

        public BitArray8 flags;
        public bool         shouldUseCachedShadowData
        {
            get { return flags[0]; }
            set { flags[0] = value; }
        }

        public bool shouldRenderCachedComponent
        {
            get { return flags[1]; }
            set { flags[1] = value; }
        }

        public bool isInCachedAtlas
        {
            get { return flags[2]; }
            set { flags[2] = value; }
        }

        public bool isMixedCached
        {
            get { return flags[3]; }
            set { flags[3] = value; }
        }

        public bool isValid
        {
            get { return flags[4]; }
            set { flags[4] = value; }
        }

        public void InitDefault()
        {
            view = default;

            deviceProjectionYFlip = default;
            deviceProjection = default;
            projection = default;
            shadowToWorld = default;
            position = default;
            zBufferParam = default;

            dynamicAtlasViewport = default;
            cachedAtlasViewport = default;
            zClip = default;

            shadowIndex = default;


            shadowMapType = ShadowMapType.PunctualAtlas;


            lightIndex = default;
            splitData = default;

            normalBias = default;
            worldTexelSize = default;
            slopeBias = default;

            shadowSoftness = default;
            blockerSampleCount = default;
            filterSampleCount = default;
            minFilterSize = default;

            kernelSize = default;
            lightAngle = default;
            maxDepthBias = default;

            evsmParams = default;

            shouldUseCachedShadowData = default;
            shouldRenderCachedComponent = default;

            cachedShadowData = default;

            isInCachedAtlas = default;
            isMixedCached = default;
            isValid = true;
        }
    }

    // custom-begin:
    public
    // custom-end
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
                    shadowAtlasDepthBits = k_DefaultShadowMapDepthBits,
                    useDynamicViewportRescale = true
                };
            }
        }

        internal static HDShadowInitParameters NewDefault() => new HDShadowInitParameters()
        {
            maxShadowRequests                   = k_DefaultMaxShadowRequests,
            directionalShadowsDepthBits         = k_DefaultShadowMapDepthBits,
            punctualLightShadowAtlas            = HDShadowAtlasInitParams.GetDefault(),
            areaLightShadowAtlas                = HDShadowAtlasInitParams.GetDefault(),
            cachedPunctualLightShadowAtlas      = 2048,
            cachedAreaLightShadowAtlas          = 1024,
            shadowResolutionDirectional         = new IntScalableSetting(new []{ 256, 512, 1024, 2048 }, ScalableSettingSchemaId.With4Levels),
            shadowResolutionArea                = new IntScalableSetting(new []{ 256, 512, 1024, 2048 }, ScalableSettingSchemaId.With4Levels),
            shadowResolutionPunctual            = new IntScalableSetting(new []{ 256, 512, 1024, 2048 }, ScalableSettingSchemaId.With4Levels),
            shadowFilteringQuality              = HDShadowFilteringQuality.Medium,
            supportScreenSpaceShadows           = false,
            supportHierarchicalVarianceScreenSpaceShadows = false,
            maxScreenSpaceShadowSlots           = 4,
            screenSpaceShadowBufferFormat       = ScreenSpaceShadowFormat.R16G16B16A16,
            maxDirectionalShadowMapResolution   = 2048,
            maxAreaShadowMapResolution          = 2048,
            maxPunctualShadowMapResolution      = 2048,

        };

        internal const int k_DefaultShadowAtlasResolution = 4096;
        internal const int k_DefaultMaxShadowRequests = 128;
        internal const DepthBits k_DefaultShadowMapDepthBits = DepthBits.Depth32;

        /// <summary>Maximum number of shadow requests at the same time.</summary>
        public int maxShadowRequests;
        /// <summary>Depth bits for directional shadows.</summary>
        public DepthBits directionalShadowsDepthBits;

        /// <summary>Shadow filtering quality.</summary>
        [FormerlySerializedAs("shadowQuality")]
        public HDShadowFilteringQuality shadowFilteringQuality;

        /// <summary>Initialization parameters for punctual shadows atlas.</summary>
        public HDShadowAtlasInitParams punctualLightShadowAtlas;
        /// <summary>Initialization parameters for area shadows atlas.</summary>
        public HDShadowAtlasInitParams areaLightShadowAtlas;

        /// <summary>Resolution for the punctual lights cached shadow maps atlas.</summary>
        public int cachedPunctualLightShadowAtlas;

        /// <summary>Resolution for the area lights cached shadow maps atlas.</summary>
        public int cachedAreaLightShadowAtlas;

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
        public bool supportScreenSpaceShadows;
        /// <summary>Enable support for hierarchical variance screen space shadows.</summary>
        public bool supportHierarchicalVarianceScreenSpaceShadows;
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

    internal struct HDShadowManagerUnmanagedFields
    {
        public HDDirectionalShadowData     m_DirectionalShadowData;
        public int                         m_MaxShadowRequests;
        public int                         m_ShadowRequestCount;
        public int                         m_CascadeCount;
        public int                         m_ShadowResolutionRequestCounter;

    }

    internal struct HDShadowManagerUnmanaged
    {
        public NativeArray<HDShadowRequestHandle> shadowRequests;
        [ReadOnly] public NativeList<HDShadowManagerUnmanagedFields> fields;
        public NativeList<HDShadowResolutionRequest> shadowResolutionRequestStorage;
        public HDDynamicShadowAtlasUnmanaged atlas;
        public HDDynamicShadowAtlasUnmanaged cascadeShadowAtlas;
        public HDDynamicShadowAtlasUnmanaged areaShadowAtlas;

        public HDCachedShadowManagerUnmanaged cachedShadowManager;

        public void UpdateShadowRequest(int index, HDShadowRequestHandle shadowRequest, ShadowMapUpdateType updateType, ShadowMapType shadowMapType)
        {
            if (index >= fields[0].m_ShadowRequestCount)
                return;

            shadowRequests[index] = shadowRequest;

            bool addToCached = updateType == ShadowMapUpdateType.Cached || updateType == ShadowMapUpdateType.Mixed;
            bool addDynamic = updateType == ShadowMapUpdateType.Dynamic || updateType == ShadowMapUpdateType.Mixed;

            switch (shadowMapType)
            {
                case ShadowMapType.PunctualAtlas:
                {
                    if (addToCached)
                        cachedShadowManager.punctualShadowAtlas.AddShadowRequest(shadowRequest);
                    if (addDynamic)
                    {
                        atlas.AddShadowRequest(shadowRequest);
                        if(updateType == ShadowMapUpdateType.Mixed)
                            atlas.AddRequestToPendingBlitFromCache(shadowRequest);
                    }

                    break;
                }
                case ShadowMapType.CascadedDirectional:
                {
                    cascadeShadowAtlas.AddShadowRequest(shadowRequest);
                    break;
                }
                case ShadowMapType.AreaLightAtlas:
                {
                    if (addToCached)
                    {
                        cachedShadowManager.areaShadowAtlas.AddShadowRequest(shadowRequest);
                    }
                    if (addDynamic)
                    {
                        areaShadowAtlas.AddShadowRequest(shadowRequest);
                        if (updateType == ShadowMapUpdateType.Mixed)
                            areaShadowAtlas.AddRequestToPendingBlitFromCache(shadowRequest);
                    }

                    break;
                }
            };
        }

        public void WriteShadowRequestIndex(int index, int shadowRequestCount, HDShadowRequestHandle shadowRequest)
        {
            if (index >= shadowRequestCount)
                return;

            shadowRequests[index] = shadowRequest;
        }

        public void UpdateCachedPunctualShadowRequest(HDShadowRequestHandle shadowRequest)
        {
            cachedShadowManager.punctualShadowAtlas.AddShadowRequest(shadowRequest);
        }

        public void UpdateDynamicPunctualShadowRequest(HDShadowRequestHandle shadowRequest, ShadowMapUpdateType updateType)
        {
            atlas.AddShadowRequest(shadowRequest);
            if(updateType == ShadowMapUpdateType.Mixed)
                atlas.AddRequestToPendingBlitFromCache(shadowRequest);
        }

        public void UpdateCachedAreaShadowRequest(HDShadowRequestHandle shadowRequest)
        {
            cachedShadowManager.punctualShadowAtlas.AddShadowRequest(shadowRequest);
        }

        public void UpdateDynamicAreaShadowRequest(HDShadowRequestHandle shadowRequest, ShadowMapUpdateType updateType)
        {
            atlas.AddShadowRequest(shadowRequest);
            if(updateType == ShadowMapUpdateType.Mixed)
                atlas.AddRequestToPendingBlitFromCache(shadowRequest);
        }

        public void UpdateCascadedDirectionalShadowRequest(HDShadowRequestHandle shadowRequest)
        {
            cascadeShadowAtlas.AddShadowRequest(shadowRequest);
        }

    }

    internal struct HDDynamicShadowAtlasUnmanaged
    {
        [WriteOnly] public NativeList<HDShadowRequestHandle> shadowRequests;
        [ReadOnly] public NativeList<HDShadowResolutionRequestHandle> shadowResolutionRequests;
        [WriteOnly] public NativeList<HDShadowRequestHandle> mixedRequestsPendingBlits;

        public void AddShadowRequest(HDShadowRequestHandle shadowRequest)
        {
            shadowRequests.Add(shadowRequest);
        }

        public void AddRequestToPendingBlitFromCache(HDShadowRequestHandle request)
        {
            mixedRequestsPendingBlits.Add(request);
        }
    }

    internal struct HDCachedShadowManagerUnmanaged
    {
        public NativeList<BitArray8> flagStorage;
        public HDCachedShadowAtlasUnmanaged punctualShadowAtlas;
        public HDCachedShadowAtlasUnmanaged areaShadowAtlas;
        public Vector3 cachedDirectionalAngles;

        public ref BitArray8 directionalShadowPendingUpdate => ref flagStorage.ElementAt((int)HDCachedShadowManager.FlagType.directionalShadowPendingUpdate);
        public ref BitArray8 directionalShadowHasRendered => ref flagStorage.ElementAt((int)HDCachedShadowManager.FlagType.directionalShadowHasRendered);

        public bool LightIsPendingPlacement(int lightIdxForCachedShadows, ShadowMapType shadowMapType)
        {
            if (shadowMapType == ShadowMapType.PunctualAtlas)
                return punctualShadowAtlas.LightIsPendingPlacement(lightIdxForCachedShadows);
            if (shadowMapType == ShadowMapType.AreaLightAtlas)
                return areaShadowAtlas.LightIsPendingPlacement(lightIdxForCachedShadows);

            return false;
        }

        public bool NeedRenderingDueToTransformChange(in HDAdditionalLightDataUpdateInfo updateInfo, in VisibleLight lightData, HDLightType lightType)
        {
            if(updateInfo.updateUponLightMovement)
            {
                if (lightType == HDLightType.Directional)
                {
                    float angleDiffThreshold = updateInfo.cachedShadowAngleUpdateThreshold;
                    Vector3 eulerAngles = HDShadowUtils.QuaternionToEulerZXY(new quaternion(lightData.localToWorldMatrix));
                    //Vector3 eulerAngles = ((Quaternion)new quaternion(lightData.localToWorldMatrix)).eulerAngles;
                    Vector3 angleDiff = cachedDirectionalAngles - eulerAngles;
                    return (Mathf.Abs(angleDiff.x) > angleDiffThreshold || Mathf.Abs(angleDiff.y) > angleDiffThreshold || Mathf.Abs(angleDiff.z) > angleDiffThreshold);
                }
                else if (lightType == HDLightType.Area)
                {
                    return areaShadowAtlas.NeedRenderingDueToTransformChange(in lightData, in updateInfo, lightType);
                }
                else
                {
                    return punctualShadowAtlas.NeedRenderingDueToTransformChange(in lightData, in updateInfo, lightType);
                }
            }

            return false;
        }

        public bool ShadowIsPendingUpdate(int shadowIdx, ShadowMapType shadowMapType)
        {
            if (shadowMapType == ShadowMapType.PunctualAtlas)
                return punctualShadowAtlas.ShadowIsPendingRendering(shadowIdx);
            if (shadowMapType == ShadowMapType.AreaLightAtlas)
                return areaShadowAtlas.ShadowIsPendingRendering(shadowIdx);
            if (shadowMapType == ShadowMapType.CascadedDirectional)
                return directionalShadowPendingUpdate[(uint)shadowIdx];

            return false;
        }

        public void UpdateResolutionRequest(ref HDShadowResolutionRequest request, int shadowIdx, ShadowMapType shadowMapType)
        {
            if (shadowMapType == ShadowMapType.PunctualAtlas)
                punctualShadowAtlas.UpdateResolutionRequest(ref request, shadowIdx);
            else if (shadowMapType == ShadowMapType.AreaLightAtlas)
                areaShadowAtlas.UpdateResolutionRequest(ref request, shadowIdx);
            else if (shadowMapType == ShadowMapType.CascadedDirectional)
                request.cachedAtlasViewport = request.dynamicAtlasViewport;
        }

        public void MarkShadowAsRendered(int shadowIdx, ShadowMapType shadowMapType)
        {
            if (shadowMapType == ShadowMapType.PunctualAtlas)
                punctualShadowAtlas.MarkAsRendered(shadowIdx);
            if (shadowMapType == ShadowMapType.AreaLightAtlas)
                areaShadowAtlas.MarkAsRendered(shadowIdx);
            if (shadowMapType == ShadowMapType.CascadedDirectional)
            {
                directionalShadowPendingUpdate[(uint)shadowIdx] = false;
                directionalShadowHasRendered[(uint)shadowIdx] = true;
            }
        }
    }

    internal struct HDCachedShadowAtlasUnmanaged
    {
        [WriteOnly] public NativeList<HDShadowRequestHandle> shadowRequests;
        public NativeHashMap<int, HDCachedShadowAtlas.CachedShadowRecord> placedShadows;
        public NativeHashMap<int, HDCachedShadowAtlas.CachedShadowRecord> shadowsPendingRendering;
        [WriteOnly] public NativeHashMap<int, int> shadowsWithValidData;                            // Shadows that have been placed and rendered at least once (OnDemand shadows are not rendered unless requested explicitly). It is a dictionary for fast access by shadow index.
        [ReadOnly] public NativeHashMap<int, HDLightRenderEntity> registeredLightDataPendingPlacement;
        [ReadOnly] public NativeHashMap<int, HDCachedShadowAtlas.CachedShadowRecord> recordsPendingPlacement;          // Note: this is different from m_RegisteredLightDataPendingPlacement because it contains records that were allocated in the system
        // but they lost their spot (e.g. post defrag). They don't have a light associated anymore if not by index, so we keep a separate collection.

        public NativeHashMap<int, HDCachedShadowAtlas.CachedTransform> transformCaches;
        [ReadOnly] public NativeList<HDCachedShadowAtlas.CachedShadowRecord> tempListForPlacement;

        public bool LightIsPendingPlacement(int lightIdxForCachedShadows)
        {
            return (registeredLightDataPendingPlacement.ContainsKey(lightIdxForCachedShadows) ||
                    recordsPendingPlacement.ContainsKey(lightIdxForCachedShadows));
        }

        public bool NeedRenderingDueToTransformChange(in VisibleLight lightData, in HDAdditionalLightDataUpdateInfo updateInfo, HDLightType lightType)
        {
            bool needUpdate = false;

            if (transformCaches.TryGetValue(updateInfo.lightIdxForCachedShadows, out HDCachedShadowAtlas.CachedTransform cachedTransform))
            {
                float positionThreshold = updateInfo.cachedShadowTranslationUpdateThreshold;
                float3 positionDiffVec = cachedTransform.position - lightData.GetPosition();
                float positionDiff = math.dot(positionDiffVec, positionDiffVec);
                if (positionDiff > positionThreshold * positionThreshold)
                {
                    needUpdate = true;
                }
                if(lightType != HDLightType.Point)
                {
                    float angleDiffThreshold = updateInfo.cachedShadowAngleUpdateThreshold;
                    float3 cachedAngles = cachedTransform.angles;
                    float3 angleDiff = cachedAngles - HDShadowUtils.QuaternionToEulerZXY(new quaternion(lightData.localToWorldMatrix));
                    // Any angle difference
                    if (math.abs(angleDiff.x) > angleDiffThreshold || math.abs(angleDiff.y) > angleDiffThreshold || math.abs(angleDiff.z) > angleDiffThreshold)
                    {
                        needUpdate = true;
                    }
                }

                if (needUpdate)
                {
                    // Update the record
                    cachedTransform.position = lightData.GetPosition();
                    cachedTransform.angles = HDShadowUtils.QuaternionToEulerZXY(new quaternion(lightData.localToWorldMatrix));
                    transformCaches[updateInfo.lightIdxForCachedShadows] = cachedTransform;
                }
            }

            return needUpdate;
        }

        public bool PointLightNeedRenderingDueToTransformChange(in VisibleLight lightData, in HDAdditionalLightDataUpdateInfo updateInfo)
        {
            bool needUpdate = false;

            if (transformCaches.TryGetValue(updateInfo.lightIdxForCachedShadows, out HDCachedShadowAtlas.CachedTransform cachedTransform))
            {
                float positionThreshold = updateInfo.cachedShadowTranslationUpdateThreshold;
                float3 positionDiffVec = cachedTransform.position - lightData.GetPosition();
                float positionDiff = math.dot(positionDiffVec, positionDiffVec);
                //Vector3 positionDiffVec = cachedTransform.position - lightData.GetPosition();
                //float positionDiff = Vector3.Dot(positionDiffVec, positionDiffVec);
                if (positionDiff > positionThreshold * positionThreshold)
                {
                    needUpdate = true;
                }

                if (needUpdate)
                {
                    // Update the record
                    cachedTransform.position = lightData.GetPosition();
                    //cachedTransform.angles = ((Quaternion)new quaternion(lightData.localToWorldMatrix)).eulerAngles;
                    cachedTransform.angles = HDShadowUtils.QuaternionToEulerZXY(new quaternion(lightData.localToWorldMatrix));
                    transformCaches[updateInfo.lightIdxForCachedShadows] = cachedTransform;
                }
            }

            return needUpdate;
        }

        public bool NonPointLightNeedsRenderingDueToTransformChange(in VisibleLight lightData, in HDAdditionalLightDataUpdateInfo updateInfo)
        {
            bool needUpdate = false;

            if (transformCaches.TryGetValue(updateInfo.lightIdxForCachedShadows, out HDCachedShadowAtlas.CachedTransform cachedTransform))
            {
                float positionThreshold = updateInfo.cachedShadowTranslationUpdateThreshold;
                float3 positionDiffVec = cachedTransform.position - lightData.GetPosition();
                float positionDiff = math.dot(positionDiffVec, positionDiffVec);
                if (positionDiff > positionThreshold * positionThreshold)
                {
                    needUpdate = true;
                }
                float angleDiffThreshold = updateInfo.cachedShadowAngleUpdateThreshold;
                float3 cachedAngles = cachedTransform.angles;
                float3 angleDiff = cachedAngles - HDShadowUtils.QuaternionToEulerZXY(new quaternion(lightData.localToWorldMatrix));
                // Any angle difference
                if (math.abs(angleDiff.x) > angleDiffThreshold || math.abs(angleDiff.y) > angleDiffThreshold || math.abs(angleDiff.z) > angleDiffThreshold)
                {
                    needUpdate = true;
                }

                if (needUpdate)
                {
                    // Update the record
                    cachedTransform.position = lightData.GetPosition();
                    cachedTransform.angles = HDShadowUtils.QuaternionToEulerZXY(new quaternion(lightData.localToWorldMatrix));
                    transformCaches[updateInfo.lightIdxForCachedShadows] = cachedTransform;
                }
            }

            return needUpdate;
        }

        public bool ShadowIsPendingRendering(int shadowIdx)
        {
            return shadowsPendingRendering.ContainsKey(shadowIdx);
        }

        public void UpdateResolutionRequest(ref HDShadowResolutionRequest request, int shadowIdx)
        {
            HDCachedShadowAtlas.CachedShadowRecord record;
            bool valueFound = placedShadows.TryGetValue(shadowIdx, out record);

            if (!valueFound)
            {
                Debug.LogWarning("Trying to render a cached shadow map that doesn't have a slot in the atlas yet.");
            }

            request.cachedAtlasViewport = new Rect(record.offsetInAtlas.x, record.offsetInAtlas.y, record.viewportSize, record.viewportSize);
            request.resolution = new Vector2(record.viewportSize, record.viewportSize);
        }

        public void AddShadowRequest(HDShadowRequestHandle shadowRequest)
        {
            shadowRequests.Add(shadowRequest);
        }

        public void MarkAsRendered(int shadowIdx)
        {
            if (shadowsPendingRendering.ContainsKey(shadowIdx))
            {
                shadowsPendingRendering.Remove(shadowIdx);
                shadowsWithValidData.Add(shadowIdx, shadowIdx);
            }
        }
    }

    public struct HDShadowResolutionRequestHandle
    {
        public const int k_InvalidIndex = -1;
        public int index;

        public bool valid => index != k_InvalidIndex;

        public static HDShadowResolutionRequestHandle Invalid => new HDShadowResolutionRequestHandle() { index = k_InvalidIndex };
    }

    // custom-begin:
    public
    // custom-end
    partial class HDShadowManager : IDisposable
    {
        public const int            k_DirectionalShadowCascadeCount = 4;
        public const int            k_MinShadowMapResolution = 16;
        public const int            k_MaxShadowMapResolution = 16384;

        List<HDShadowData>          m_ShadowDatas = new List<HDShadowData>();
        NativeArray<HDShadowRequestHandle>     m_ShadowRequests;
        NativeList<HDShadowResolutionRequest> m_ShadowResolutionRequestStorage;
        HDDirectionalShadowData[]   m_CachedDirectionalShadowData;

        NativeList<HDShadowManagerUnmanagedFields>    m_UnmanagedFields;

        // Structured buffer of shadow datas
        ComputeBuffer               m_ShadowDataBuffer;
        ComputeBuffer               m_DirectionalShadowDataBuffer;

        // The two shadowmaps atlases we uses, one for directional cascade (without resize) and the second for the rest of the shadows
        HDDynamicShadowAtlas               m_CascadeAtlas;
        HDDynamicShadowAtlas               m_Atlas;
        HDDynamicShadowAtlas               m_AreaLightShadowAtlas;

        Material                    m_ClearShadowMaterial;
        Material                    m_BlitShadowMaterial;
        MaterialPropertyBlock       m_BlitShadowPropertyBlock = new MaterialPropertyBlock();

        ConstantBuffer<ShaderVariablesGlobal> m_GlobalShaderVariables;

        private static HDShadowManager s_Instance = new HDShadowManager();

        public static HDShadowManager instance { get { return s_Instance; } }
        public static HDCachedShadowManager cachedShadowManager {  get { return HDCachedShadowManager.instance; } }

        internal NativeList<HDShadowResolutionRequest> shadowResolutionRequestStorage => m_ShadowResolutionRequestStorage;
        internal NativeList<HDShadowManagerUnmanagedFields> unmanagedFields => m_UnmanagedFields;


        private HDShadowManager()
        {}

        public void InitShadowManager(RenderPipelineResources renderPipelineResources, HDShadowInitParameters initParams, Shader clearShader)
        {
            if (!m_UnmanagedFields.IsCreated)
            {
                m_UnmanagedFields = new NativeList<HDShadowManagerUnmanagedFields>(1, Allocator.Persistent);
                m_UnmanagedFields.Length = 1;
            }
            else
            {
                m_UnmanagedFields[0] = default;
            }
            // Even when shadows are disabled (maxShadowRequests == 0) we need to allocate compute buffers to avoid having
            // resource not bound errors when dispatching a compute shader.
            m_ShadowDataBuffer = new ComputeBuffer(Mathf.Max(initParams.maxShadowRequests, 1), System.Runtime.InteropServices.Marshal.SizeOf(typeof(HDShadowData)));
            m_DirectionalShadowDataBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(HDDirectionalShadowData)));
            m_UnmanagedFields.ElementAt(0).m_MaxShadowRequests = initParams.maxShadowRequests;
            m_UnmanagedFields.ElementAt(0).m_ShadowRequestCount = 0;

            if (initParams.maxShadowRequests == 0)
                return;

            m_ClearShadowMaterial = CoreUtils.CreateEngineMaterial(clearShader);
            m_BlitShadowMaterial = CoreUtils.CreateEngineMaterial(renderPipelineResources.shaders.shadowBlitPS);

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
            m_ShadowRequests = new NativeArray<HDShadowRequestHandle>(initParams.maxShadowRequests, Allocator.Persistent);
            m_CachedDirectionalShadowData = new HDDirectionalShadowData[1]; // we only support directional light shadow

            m_GlobalShaderVariables = new ConstantBuffer<ShaderVariablesGlobal>();

            for (int i = 0; i < initParams.maxShadowRequests; i++)
            {
                m_ShadowResolutionRequestStorage[i] = new HDShadowResolutionRequest();
            }

            HDShadowAtlas.HDShadowAtlasInitParameters punctualAtlasInitParams = new HDShadowAtlas.HDShadowAtlasInitParameters(renderPipelineResources, initParams.punctualLightShadowAtlas.shadowAtlasResolution,
            initParams.punctualLightShadowAtlas.shadowAtlasResolution, HDShaderIDs._ShadowmapAtlas, m_ClearShadowMaterial, initParams.maxShadowRequests, initParams, m_GlobalShaderVariables);

            punctualAtlasInitParams.name = "Shadow Map Atlas";

            // The cascade atlas will be allocated only if there is a directional light
            m_Atlas = new HDDynamicShadowAtlas(punctualAtlasInitParams);

            // Cascade atlas render texture will only be allocated if there is a shadow casting directional light
            HDShadowAtlas.BlurAlgorithm cascadeBlur = GetDirectionalShadowAlgorithm() == DirectionalShadowAlgorithm.IMS ? HDShadowAtlas.BlurAlgorithm.IM : HDShadowAtlas.BlurAlgorithm.None;
            HDShadowAtlas.HDShadowAtlasInitParameters dirAtlasInitParams = punctualAtlasInitParams;
            dirAtlasInitParams.width = 1;
            dirAtlasInitParams.height = 1;
            dirAtlasInitParams.atlasShaderID = HDShaderIDs._ShadowmapCascadeAtlas;
            dirAtlasInitParams.blurAlgorithm = cascadeBlur;
            dirAtlasInitParams.depthBufferBits = initParams.directionalShadowsDepthBits;
            dirAtlasInitParams.name = "Cascade Shadow Map Atlas";

            m_CascadeAtlas = new HDDynamicShadowAtlas(dirAtlasInitParams);

            HDShadowAtlas.HDShadowAtlasInitParameters areaAtlasInitParams = punctualAtlasInitParams;
            if (ShaderConfig.s_AreaLights == 1)
            {
                areaAtlasInitParams.width = initParams.areaLightShadowAtlas.shadowAtlasResolution;
                areaAtlasInitParams.height = initParams.areaLightShadowAtlas.shadowAtlasResolution;
                areaAtlasInitParams.atlasShaderID = HDShaderIDs._ShadowmapAreaAtlas;
                areaAtlasInitParams.blurAlgorithm = HDShadowAtlas.BlurAlgorithm.EVSM;
                areaAtlasInitParams.depthBufferBits = initParams.areaLightShadowAtlas.shadowAtlasDepthBits;
                areaAtlasInitParams.name = "Area Light Shadow Map Atlas";
                m_AreaLightShadowAtlas = new HDDynamicShadowAtlas(areaAtlasInitParams);
            }

            HDShadowAtlas.HDShadowAtlasInitParameters cachedPunctualAtlasInitParams = punctualAtlasInitParams;
            cachedPunctualAtlasInitParams.width = initParams.cachedPunctualLightShadowAtlas;
            cachedPunctualAtlasInitParams.height = initParams.cachedPunctualLightShadowAtlas;
            cachedPunctualAtlasInitParams.atlasShaderID = HDShaderIDs._CachedShadowmapAtlas;
            cachedPunctualAtlasInitParams.name = "Cached Shadow Map Atlas";

            cachedShadowManager.InitPunctualShadowAtlas(cachedPunctualAtlasInitParams);

            if (ShaderConfig.s_AreaLights == 1)
            {
                HDShadowAtlas.HDShadowAtlasInitParameters cachedAreaAtlasInitParams = areaAtlasInitParams;
                cachedAreaAtlasInitParams.width = initParams.cachedAreaLightShadowAtlas;
                cachedAreaAtlasInitParams.height = initParams.cachedAreaLightShadowAtlas;
                cachedAreaAtlasInitParams.atlasShaderID = HDShaderIDs._CachedAreaLightShadowmapAtlas;
                cachedAreaAtlasInitParams.name = "Cached Area Light Shadow Map Atlas";

                cachedShadowManager.InitAreaLightShadowAtlas(cachedAreaAtlasInitParams);
            }
        }

        public void InitializeNonRenderGraphResources()
        {
            m_Atlas.AllocateRenderTexture();
            m_CascadeAtlas.AllocateRenderTexture();
            cachedShadowManager.punctualShadowAtlas.AllocateRenderTexture();
            if (ShaderConfig.s_AreaLights == 1)
            {
                m_AreaLightShadowAtlas.AllocateRenderTexture();
                cachedShadowManager.areaShadowAtlas.AllocateRenderTexture();
            }
        }

        public void CleanupNonRenderGraphResources()
        {
            m_Atlas.Release();
            m_CascadeAtlas.Release();
            cachedShadowManager.punctualShadowAtlas.Release();
            if (ShaderConfig.s_AreaLights == 1)
            {
                m_AreaLightShadowAtlas.Release();
                cachedShadowManager.areaShadowAtlas.Release();
            }
        }

        // Keep in sync with both HDShadowSampling.hlsl
        public static DirectionalShadowAlgorithm GetDirectionalShadowAlgorithm()
        {
            switch (HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.shadowFilteringQuality)
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
            };
            return DirectionalShadowAlgorithm.PCF5x5;
        }

        public void UpdateShaderVariablesGlobalCB(ref ShaderVariablesGlobal cb)
        {
            if (m_UnmanagedFields.ElementAt(0).m_MaxShadowRequests == 0)
                return;

            cb._CascadeShadowCount = (uint)(m_UnmanagedFields.ElementAt(0).m_CascadeCount + 1);
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
        }

        internal int ReserveShadowResolutions(Vector2 resolution, ShadowMapType shadowMapType, int lightID, int index, ShadowMapUpdateType updateType)
        {
            ref var unmanagedData = ref m_UnmanagedFields.ElementAt(0);
            if (unmanagedData.m_ShadowRequestCount >= unmanagedData.m_MaxShadowRequests)
            {
                Debug.LogWarning("Max shadow requests count reached, dropping all exceeding requests. You can increase this limit by changing the max requests in the HDRP asset");
                return -1;
            }

            ref var resolutionRequest = ref m_ShadowResolutionRequestStorage.ElementAt(unmanagedData.m_ShadowResolutionRequestCounter);
            var resolutionRequestHandle = new HDShadowResolutionRequestHandle() { index = unmanagedData.m_ShadowResolutionRequestCounter };
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


            unmanagedData.m_ShadowResolutionRequestCounter++;
            unmanagedData.m_ShadowRequestCount = unmanagedData.m_ShadowResolutionRequestCounter;

            return unmanagedData.m_ShadowResolutionRequestCounter - 1;
        }

        internal HDShadowResolutionRequestHandle GetResolutionRequestHandle(int index)
        {
            if (index < 0 || index >= m_UnmanagedFields.ElementAt(0).m_ShadowRequestCount)
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
            if (index < 0 || index >= m_UnmanagedFields.ElementAt(0).m_ShadowRequestCount)
                return Vector2.zero;

            return m_ShadowResolutionRequestStorage[index].resolution;
        }

        internal void UpdateShadowRequest(int index, HDShadowRequestHandle shadowRequest, ShadowMapUpdateType updateType, ShadowMapType shadowMapType, bool isMixedCache)
        {
            if (index >= m_UnmanagedFields.ElementAt(0).m_ShadowRequestCount)
                return;

            m_ShadowRequests[index] = shadowRequest;

            bool addToCached = updateType == ShadowMapUpdateType.Cached || updateType == ShadowMapUpdateType.Mixed;
            bool addDynamic = updateType == ShadowMapUpdateType.Dynamic || updateType == ShadowMapUpdateType.Mixed;

            switch (shadowMapType)
            {
                case ShadowMapType.PunctualAtlas:
                {
                    if (addToCached)
                        cachedShadowManager.punctualShadowAtlas.AddShadowRequest(shadowRequest);
                    if (addDynamic)
                    {
                        m_Atlas.AddShadowRequest(shadowRequest);
                        if(updateType == ShadowMapUpdateType.Mixed)
                            m_Atlas.AddRequestToPendingBlitFromCache(shadowRequest, isMixedCache);
                    }

                        break;
                }
                case ShadowMapType.CascadedDirectional:
                {
                    m_CascadeAtlas.AddShadowRequest(shadowRequest);
                    break;
                }
                case ShadowMapType.AreaLightAtlas:
                {
                    if (addToCached)
                    {
                        cachedShadowManager.areaShadowAtlas.AddShadowRequest(shadowRequest);
                    }
                    if (addDynamic)
                    {
                        m_AreaLightShadowAtlas.AddShadowRequest(shadowRequest);
                        if (updateType == ShadowMapUpdateType.Mixed)
                            m_AreaLightShadowAtlas.AddRequestToPendingBlitFromCache(shadowRequest, isMixedCache);
                    }

                    break;
                }
            };
        }

        public void UpdateCascade(int cascadeIndex, Vector4 cullingSphere, float border)
        {
            if (cullingSphere.w != float.NegativeInfinity)
            {
                cullingSphere.w *= cullingSphere.w;
            }

            ref var unmanagedData = ref m_UnmanagedFields.ElementAt(0);
            unmanagedData.m_CascadeCount = Mathf.Max(unmanagedData.m_CascadeCount, cascadeIndex);

            unsafe
            {
                ref HDDirectionalShadowData shadowData = ref unmanagedData.m_DirectionalShadowData;
                fixed (float * sphereCascadesBuffer = shadowData.sphereCascades)
                    ((Vector4 *)sphereCascadesBuffer)[cascadeIndex] = cullingSphere;
                fixed (float * cascadeBorders = shadowData.cascadeBorders)
                    cascadeBorders[cascadeIndex] = border;
            }
        }

        internal static void UpdateCascade(NativeList<HDDirectionalShadowData> shadowDataStorage, NativeArray<int> cascadeCountStorage, int cascadeIndex, Vector4 cullingSphere, float border)
        {
            if (cullingSphere.w != float.NegativeInfinity)
            {
                cullingSphere.w *= cullingSphere.w;
            }

            cascadeCountStorage[0] = Mathf.Max(cascadeCountStorage[0], cascadeIndex);

            unsafe
            {
                ref HDDirectionalShadowData shadowData = ref shadowDataStorage.ElementAt(0);
                fixed (float * sphereCascadesBuffer = shadowData.sphereCascades)
                    ((Vector4 *)sphereCascadesBuffer)[cascadeIndex] = cullingSphere;
                fixed (float * cascadeBorders = shadowData.cascadeBorders)
                    cascadeBorders[cascadeIndex] = border;
            }
        }

        HDShadowData CreateShadowData(ref HDShadowRequest shadowRequest, HDShadowAtlas atlas)
        {
            HDShadowData data = new HDShadowData();

            var devProj = shadowRequest.deviceProjection;
            var view = shadowRequest.view;
            data.proj = new Vector4(devProj.m00, devProj.m11, devProj.m22, devProj.m23);
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
            fixed (float * sphereCascadesBuffer = m_UnmanagedFields.ElementAt(0).m_DirectionalShadowData.sphereCascades)
            {
                return ((Vector4 *)sphereCascadesBuffer)[index];
            }
        }

        public void UpdateCullingParameters(ref ScriptableCullingParameters cullingParams, float maxShadowDistance)
        {
            cullingParams.shadowDistance = Mathf.Min(maxShadowDistance, cullingParams.shadowDistance);
        }

        public unsafe void LayoutShadowMaps(LightingDebugSettings lightingDebugSettings)
        {
            if (m_UnmanagedFields.ElementAt(0).m_MaxShadowRequests == 0)
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
            if (m_UnmanagedFields.ElementAt(0).m_MaxShadowRequests == 0)
                return;

            int shadowIndex = 0;

            m_ShadowDatas.Clear();

            NativeList<HDShadowRequest> requestStorage = HDLightRenderDatabase.instance.hdShadowRequestStorage;
            ref UnsafeList<HDShadowRequest> requestStorageUnsafe = ref *requestStorage.GetUnsafeList();

            ref var unmanagedData = ref m_UnmanagedFields.ElementAt(0);
            HDShadowRequestHandle* shadowRequestsPtr = (HDShadowRequestHandle*)m_ShadowRequests.GetUnsafePtr();

            if (!m_ShadowRequests.IsCreated || m_ShadowRequests.Length < unmanagedData.m_ShadowRequestCount)
                throw new IndexOutOfRangeException("Shadow request count is out of range of the shadow request array.");

            // Create all HDShadowDatas and update them with shadow request datas
            for (int i = 0; i < unmanagedData.m_ShadowRequestCount; i++)
            {
                //Debug.Assert(m_ShadowRequests[i] != null);

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
                shadowRequest.shadowIndex = shadowIndex++;
            }

            int first = k_DirectionalShadowCascadeCount, second = k_DirectionalShadowCascadeCount;

            fixed (float *sphereBuffer = unmanagedData.m_DirectionalShadowData.sphereCascades)
            {
                Vector4 * sphere = (Vector4 *)sphereBuffer;
                for (int i = 0; i < k_DirectionalShadowCascadeCount; i++)
                {
                    first  = (first  == k_DirectionalShadowCascadeCount                       && sphere[i].w > 0.0f) ? i : first;
                    second = ((second == k_DirectionalShadowCascadeCount || second == first)  && sphere[i].w > 0.0f) ? i : second;
                }
            }

            // Update directional datas:
            if (second != k_DirectionalShadowCascadeCount)
                unmanagedData.m_DirectionalShadowData.cascadeDirection = (GetCascadeSphereAtIndex(second) - GetCascadeSphereAtIndex(first)).normalized;
            else
                unmanagedData.m_DirectionalShadowData.cascadeDirection = Vector4.zero;

            unmanagedData.m_DirectionalShadowData.cascadeDirection.w = camera.volumeStack.GetComponent<HDShadowSettings>().cascadeShadowSplitCount.value;

            if (unmanagedData.m_ShadowRequestCount > 0)
            {
                // Upload the shadow buffers to GPU
                m_ShadowDataBuffer.SetData(m_ShadowDatas);
                m_CachedDirectionalShadowData[0] = unmanagedData.m_DirectionalShadowData;
                m_DirectionalShadowDataBuffer.SetData(m_CachedDirectionalShadowData);
            }
        }

        public void RenderShadows(ScriptableRenderContext renderContext, CommandBuffer cmd, in ShaderVariablesGlobal globalCB, CullingResults cullResults, HDCamera hdCamera)
        {
            // Avoid to do any commands if there is no shadow to draw
            if (m_UnmanagedFields.ElementAt(0).m_ShadowRequestCount == 0)
                return ;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderCachedPunctualShadowMaps)))
            {
                cachedShadowManager.punctualShadowAtlas.RenderShadows(cullResults, globalCB, hdCamera.frameSettings, renderContext, cmd);
                cachedShadowManager.punctualShadowAtlas.AddBlitRequestsForUpdatedShadows(m_Atlas);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderCachedAreaShadowMaps)))
            {
                if (ShaderConfig.s_AreaLights == 1)
                {
                    cachedShadowManager.areaShadowAtlas.RenderShadows(cullResults, globalCB, hdCamera.frameSettings, renderContext, cmd);
                    cachedShadowManager.areaShadowAtlas.AddBlitRequestsForUpdatedShadows(m_AreaLightShadowAtlas);
                }
            }

            BlitCacheIntoAtlas(cmd);

            // Clear atlas render targets and draw shadows
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderPunctualShadowMaps)))
            {
                m_Atlas.RenderShadows(cullResults, globalCB, hdCamera.frameSettings, renderContext, cmd);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderDirectionalShadowMaps)))
            {
                m_CascadeAtlas.RenderShadows(cullResults, globalCB, hdCamera.frameSettings, renderContext, cmd);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderAreaShadowMaps)))
            {
                if (ShaderConfig.s_AreaLights == 1)
                {
                    m_AreaLightShadowAtlas.RenderShadows(cullResults, globalCB, hdCamera.frameSettings, renderContext, cmd);
                }
            }
        }

        public void BlitCacheIntoAtlas(CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.BlitPunctualMixedCachedShadowMaps)))
            {

                HDDynamicShadowAtlas.BlitCachedIntoAtlas(m_Atlas.PrepareShadowBlitParameters(cachedShadowManager.punctualShadowAtlas, m_BlitShadowMaterial, m_BlitShadowPropertyBlock),
                                                        m_Atlas.renderTarget,
                                                        cachedShadowManager.punctualShadowAtlas.renderTarget,
                                                        cmd);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.BlitAreaMixedCachedShadowMaps)))
            {
                HDDynamicShadowAtlas.BlitCachedIntoAtlas(m_AreaLightShadowAtlas.PrepareShadowBlitParameters(cachedShadowManager.areaShadowAtlas, m_BlitShadowMaterial, m_BlitShadowPropertyBlock),
                                                        m_AreaLightShadowAtlas.renderTarget,
                                                        cachedShadowManager.areaShadowAtlas.renderTarget,
                                                        cmd);
            }
        }

        public void PushGlobalParameters(CommandBuffer cmd)
        {
            // This code must be in sync with HDShadowContext.hlsl
            cmd.SetGlobalBuffer(HDShaderIDs._HDShadowDatas, m_ShadowDataBuffer);
            cmd.SetGlobalBuffer(HDShaderIDs._HDDirectionalShadowData, m_DirectionalShadowDataBuffer);
        }

        public void BindResources(CommandBuffer cmd)
        {
            m_Atlas.BindResources(cmd);
            m_CascadeAtlas.BindResources(cmd);
            cachedShadowManager.punctualShadowAtlas.BindResources(cmd);
            if (ShaderConfig.s_AreaLights == 1)
            {
                m_AreaLightShadowAtlas.BindResources(cmd);
                cachedShadowManager.areaShadowAtlas.BindResources(cmd);
            }
        }

        public int GetShadowRequestCount()
        {
            return m_UnmanagedFields.ElementAt(0).m_ShadowRequestCount;
        }

        internal void GetUnmanageDataForShadowRequestJobs(ref HDShadowManagerUnmanaged unmanagedData)
        {
            unmanagedData.shadowRequests = m_ShadowRequests;
            unmanagedData.fields = m_UnmanagedFields;
            unmanagedData.shadowResolutionRequestStorage = m_ShadowResolutionRequestStorage;
            m_Atlas.GetUnmanageDataForShadowRequestJobs(ref unmanagedData.atlas);
            m_CascadeAtlas.GetUnmanageDataForShadowRequestJobs(ref unmanagedData.cascadeShadowAtlas);
            m_AreaLightShadowAtlas.GetUnmanageDataForShadowRequestJobs(ref unmanagedData.areaShadowAtlas);
            cachedShadowManager.GetUnmanageDataForShadowRequestJobs(ref unmanagedData.cachedShadowManager);
        }

        public void Clear()
        {
            if (m_UnmanagedFields.ElementAt(0).m_MaxShadowRequests == 0)
                return;

            // Clear the shadows atlas infos and requests
            m_Atlas.Clear();
            m_CascadeAtlas.Clear();
            if (ShaderConfig.s_AreaLights == 1)
                m_AreaLightShadowAtlas.Clear();

            cachedShadowManager.ClearShadowRequests();

            m_UnmanagedFields.ElementAt(0).m_ShadowResolutionRequestCounter = 0;

            m_UnmanagedFields.ElementAt(0).m_ShadowRequestCount = 0;
            m_UnmanagedFields.ElementAt(0).m_CascadeCount = 0;
        }

        public struct ShadowDebugAtlasTextures
        {
            public RTHandle punctualShadowAtlas;
            public RTHandle cascadeShadowAtlas;
            public RTHandle areaShadowAtlas;

            public RTHandle cachedPunctualShadowAtlas;
            public RTHandle cachedAreaShadowAtlas;
        }

        public ShadowDebugAtlasTextures GetDebugAtlasTextures()
        {
            var result = new ShadowDebugAtlasTextures();
            if (ShaderConfig.s_AreaLights == 1)
            {
                result.areaShadowAtlas = m_AreaLightShadowAtlas.renderTarget;
                result.cachedAreaShadowAtlas = cachedShadowManager.areaShadowAtlas.renderTarget;
            }
            result.punctualShadowAtlas = m_Atlas.renderTarget;
            result.cascadeShadowAtlas = m_CascadeAtlas.renderTarget;
            result.cachedPunctualShadowAtlas = cachedShadowManager.punctualShadowAtlas.renderTarget;
            return result;
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
        public unsafe void DisplayShadowMap(in ShadowDebugAtlasTextures atlasTextures, int shadowIndex, CommandBuffer cmd, Material debugMaterial, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, MaterialPropertyBlock mpb)
        {
            if (shadowIndex >= m_UnmanagedFields.ElementAt(0).m_ShadowRequestCount)
                return;

            NativeList<HDShadowRequest> requestStorage = HDLightRenderDatabase.instance.hdShadowRequestStorage;
            ref UnsafeList<HDShadowRequest> requestStorageUnsafe = ref *requestStorage.GetUnsafeList();

            HDShadowRequestHandle   shadowRequestHandle = m_ShadowRequests[shadowIndex];
            ref var shadowRequest = ref requestStorageUnsafe.ElementAt(shadowRequestHandle.storageIndexForShadowRequest);

            switch (shadowRequest.shadowMapType)
            {
                case ShadowMapType.PunctualAtlas:
                {
                    if (shadowRequest.isInCachedAtlas)
                        cachedShadowManager.punctualShadowAtlas.DisplayAtlas(atlasTextures.cachedPunctualShadowAtlas, cmd, debugMaterial, shadowRequest.cachedAtlasViewport, screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, mpb);
                    else
                        m_Atlas.DisplayAtlas(atlasTextures.punctualShadowAtlas, cmd, debugMaterial, shadowRequest.dynamicAtlasViewport, screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, mpb);
                    break;
                }
                case ShadowMapType.CascadedDirectional:
                {
                    m_CascadeAtlas.DisplayAtlas(atlasTextures.cascadeShadowAtlas, cmd, debugMaterial, shadowRequest.dynamicAtlasViewport, screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, mpb);
                    break;
                }
                case ShadowMapType.AreaLightAtlas:
                {
                    if (ShaderConfig.s_AreaLights == 1)
                    {
                        if (shadowRequest.isInCachedAtlas)
                            cachedShadowManager.areaShadowAtlas.DisplayAtlas(atlasTextures.cachedAreaShadowAtlas, cmd, debugMaterial, shadowRequest.cachedAtlasViewport, screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, mpb);
                        else
                            m_AreaLightShadowAtlas.DisplayAtlas(atlasTextures.areaShadowAtlas, cmd, debugMaterial, shadowRequest.dynamicAtlasViewport, screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, mpb);
                    }
                    break;
                }
            };
        }

        public void Dispose()
        {
            m_ShadowDataBuffer.Dispose();
            m_DirectionalShadowDataBuffer.Dispose();
            m_GlobalShaderVariables.Release();

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

            int maxShadowRequests = 0;

            if (m_UnmanagedFields.IsCreated)
            {
                maxShadowRequests = m_UnmanagedFields.ElementAt(0).m_MaxShadowRequests;
                m_UnmanagedFields.Dispose();
                m_UnmanagedFields = default;
            }

            if (maxShadowRequests == 0)
                return;

            m_Atlas.Release();
            m_Atlas.Dispose();
            if (ShaderConfig.s_AreaLights == 1)
                m_AreaLightShadowAtlas.Release();
            if (m_AreaLightShadowAtlas != null)
                m_AreaLightShadowAtlas.Dispose();
            m_CascadeAtlas.Release();
            m_CascadeAtlas.Dispose();

            CoreUtils.Destroy(m_ClearShadowMaterial);
            cachedShadowManager.Dispose();
        }

        // custom-begin:
        // custom accessor so that game specific rendering code can access shadow data.
        public List<HDShadowData> GetShadowDatas()
        {
            return m_ShadowDatas;
        }

        public HDShadowAtlas GetShadowAtlas()
        {
            return m_Atlas;
        }
        // custom-end
    }
}
