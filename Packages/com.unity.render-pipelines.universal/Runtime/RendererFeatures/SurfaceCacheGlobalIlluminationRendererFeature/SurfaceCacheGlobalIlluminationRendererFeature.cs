#if SURFACE_CACHE

using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.PathTracing.Core;
using UnityEngine.Rendering.LiveGI;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.UnifiedRayTracing;
using InstanceHandle = UnityEngine.PathTracing.Core.Handle<UnityEngine.Rendering.SurfaceCacheWorld.Instance>;
using LightHandle = UnityEngine.PathTracing.Core.Handle<UnityEngine.Rendering.SurfaceCacheWorld.Light>;
using MaterialHandle = UnityEngine.PathTracing.Core.Handle<UnityEngine.PathTracing.Core.MaterialPool.MaterialDescriptor>;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    [SupportedOnRenderPipeline]
    [Categorization.CategoryInfo(Name = "R: Surface Cache URP Integration", Order = 1000), HideInInspector]
    sealed class SurfaceCacheRenderPipelineResourceSet : IRenderPipelineResources
    {
        [SerializeField, HideInInspector]
        int m_Version = 6;

        int IRenderPipelineGraphicsSettings.version => m_Version;

        [ResourcePath("Runtime/RendererFeatures/SurfaceCacheGlobalIlluminationRendererFeature/FallbackMaterial.mat")]
        public Material m_FallbackMaterial;

        [ResourcePath("Runtime/RendererFeatures/SurfaceCacheGlobalIlluminationRendererFeature/PatchAllocation.compute")]
        public ComputeShader m_AllocationShader;

        [ResourcePath("Runtime/RendererFeatures/SurfaceCacheGlobalIlluminationRendererFeature/ScreenResolveLookup.compute")]
        public ComputeShader m_ScreenResolveLookupShader;

        [ResourcePath("Runtime/RendererFeatures/SurfaceCacheGlobalIlluminationRendererFeature/ScreenResolveUpsampling.compute")]
        public ComputeShader m_ScreenResolveUpsamplingShader;

        [ResourcePath("Runtime/RendererFeatures/SurfaceCacheGlobalIlluminationRendererFeature/Debug.compute")]
        public ComputeShader m_DebugShader;

        [ResourcePath("Runtime/RendererFeatures/SurfaceCacheGlobalIlluminationRendererFeature/FlatNormalResolution.compute")]
        public ComputeShader m_FlatNormalResolutionShader;

        public Material fallbackMaterial
        {
            get => m_FallbackMaterial;
            set => this.SetValueAndNotify(ref m_FallbackMaterial, value, nameof(m_FallbackMaterial));
        }

        public ComputeShader allocationShader
        {
            get => m_AllocationShader;
            set => this.SetValueAndNotify(ref m_AllocationShader, value, nameof(m_AllocationShader));
        }

        public ComputeShader screenResolveLookupShader
        {
            get => m_ScreenResolveLookupShader;
            set => this.SetValueAndNotify(ref m_ScreenResolveLookupShader, value, nameof(m_ScreenResolveLookupShader));
        }

        public ComputeShader screenResolveUpsamplingShader
        {
            get => m_ScreenResolveUpsamplingShader;
            set => this.SetValueAndNotify(ref m_ScreenResolveUpsamplingShader, value, nameof(m_ScreenResolveUpsamplingShader));
        }

        public ComputeShader debugShader
        {
            get => m_DebugShader;
            set => this.SetValueAndNotify(ref m_DebugShader, value, nameof(m_DebugShader));
        }

        public ComputeShader flatNormalResolutionShader
        {
            get => m_FlatNormalResolutionShader;
            set => this.SetValueAndNotify(ref m_FlatNormalResolutionShader, value, nameof(m_FlatNormalResolutionShader));
        }
    }

    [DisallowMultipleRendererFeature("Surface Cache Global Illumination")]
    public class SurfaceCacheGlobalIlluminationRendererFeature : ScriptableRendererFeature
    {
        public enum DebugViewMode_
        {
            CellIndex,
            StableIrradiance,
            FastIrradiance,
            CoefficientOfVariation,
            Drift,
            StdDev,
            UpdateCount,
            FlatNormal
        }

        // URP currently cannot render motion vectors properly in Scene View, so we disable it.
        // https://jira.unity3d.com/browse/SRP-743
        // When this is fixed, we probably want to enable this always.
        static bool UseMotionVectorPatchSeeding(CameraType camType)
        {
            return camType == CameraType.Game;
        }

        internal static class ShaderIDs
        {
            public static readonly int _RingConfigBuffer = Shader.PropertyToID("_RingConfigBuffer");
            public static readonly int _PatchIrradiances = Shader.PropertyToID("_PatchIrradiances");
            public static readonly int _PatchStatistics = Shader.PropertyToID("_PatchStatistics");
            public static readonly int _PatchGeometries = Shader.PropertyToID("_PatchGeometries");
            public static readonly int _PatchIrradiances0 = Shader.PropertyToID("_PatchIrradiances0");
            public static readonly int _PatchIrradiances1 = Shader.PropertyToID("_PatchIrradiances1");
            public static readonly int _PatchCounterSets = Shader.PropertyToID("_PatchCounterSets");
            public static readonly int _CellAllocationMarks = Shader.PropertyToID("_CellAllocationMarks");
            public static readonly int _CellPatchIndices = Shader.PropertyToID("_CellPatchIndices");
            public static readonly int _Result = Shader.PropertyToID("_Result");
            public static readonly int _FullResDepths = Shader.PropertyToID("_FullResDepths");
            public static readonly int _FullResIrradiances = Shader.PropertyToID("_FullResIrradiances");
            public static readonly int _FullResFlatNormals = Shader.PropertyToID("_FullResFlatNormals");
            public static readonly int _FullResShadedNormals = Shader.PropertyToID("_FullResShadedNormals");
            public static readonly int _UseMotionVectorSeeding = Shader.PropertyToID("_UseMotionVectorSeeding");
            public static readonly int _ResultL0 = Shader.PropertyToID("_ResultL0");
            public static readonly int _ResultL10 = Shader.PropertyToID("_ResultL10");
            public static readonly int _ResultL11 = Shader.PropertyToID("_ResultL11");
            public static readonly int _ResultL12 = Shader.PropertyToID("_ResultL12");
            public static readonly int _ResultNdcDepths = Shader.PropertyToID("_ResultNdcDepths");
            public static readonly int _ScreenDepths = Shader.PropertyToID("_ScreenDepths");
            public static readonly int _ScreenFlatNormals = Shader.PropertyToID("_ScreenFlatNormals");
            public static readonly int _CurrentFullResScreenDepths = Shader.PropertyToID("_CurrentFullResScreenDepths");
            public static readonly int _CurrentFullResScreenFlatNormals = Shader.PropertyToID("_CurrentFullResScreenFlatNormals");
            public static readonly int _CurrentFullResScreenMotionVectors = Shader.PropertyToID("_CurrentFullResScreenMotionVectors");
            public static readonly int _ScreenShadedNormals = Shader.PropertyToID("_ScreenShadedNormals");
            public static readonly int _LowResIrradiancesL0 = Shader.PropertyToID("_LowResIrradiancesL0");
            public static readonly int _LowResIrradiancesL10 = Shader.PropertyToID("_LowResIrradiancesL10");
            public static readonly int _LowResIrradiancesL11 = Shader.PropertyToID("_LowResIrradiancesL11");
            public static readonly int _LowResIrradiancesL12 = Shader.PropertyToID("_LowResIrradiancesL12");
            public static readonly int _PreviousLowResScreenIrradiancesL0 = Shader.PropertyToID("_PreviousLowResScreenIrradiancesL0");
            public static readonly int _PreviousLowResScreenIrradiancesL10 = Shader.PropertyToID("_PreviousLowResScreenIrradiancesL10");
            public static readonly int _PreviousLowResScreenIrradiancesL11 = Shader.PropertyToID("_PreviousLowResScreenIrradiancesL11");
            public static readonly int _PreviousLowResScreenIrradiancesL12 = Shader.PropertyToID("_PreviousLowResScreenIrradiancesL12");
            public static readonly int _PreviousLowResScreenNdcDepths = Shader.PropertyToID("_PreviousLowResScreenNdcDepths");
            public static readonly int _FilterRadius = Shader.PropertyToID("_FilterRadius");
            public static readonly int _ViewMode = Shader.PropertyToID("_ViewMode");
            public static readonly int _ShowSamplePosition = Shader.PropertyToID("_ShowSamplePosition");
            public static readonly int _SampleCount = Shader.PropertyToID("_SampleCount");
            public static readonly int _ClipToWorldTransform = Shader.PropertyToID("_ClipToWorldTransform");
            public static readonly int _CurrentClipToWorldTransform = Shader.PropertyToID("_CurrentClipToWorldTransform");
            public static readonly int _PreviousClipToWorldTransform = Shader.PropertyToID("_PreviousClipToWorldTransform");
            public static readonly int _FrameIdx = Shader.PropertyToID("_FrameIdx");
            public static readonly int _VolumeSpatialResolution = Shader.PropertyToID("_VolumeSpatialResolution");
            public static readonly int _RingConfigOffset = Shader.PropertyToID("_RingConfigOffset");
            public static readonly int _VolumeTargetPos = Shader.PropertyToID("_VolumeTargetPos");
            public static readonly int _FullResPixelOffset = Shader.PropertyToID("_FullResPixelOffset");
            public static readonly int _LowResScreenSize = Shader.PropertyToID("_LowResScreenSize");
            public static readonly int _CascadeCount = Shader.PropertyToID("_CascadeCount");
            public static readonly int _VolumeVoxelMinSize = Shader.PropertyToID("_VolumeVoxelMinSize");
            public static readonly int _CascadeOffsets = Shader.PropertyToID("_CascadeOffsets");
            public static readonly int _PatchCellIndices = Shader.PropertyToID("_PatchCellIndices");
        }

        class SurfaceCachePass : ScriptableRenderPass, IDisposable
        {
            private class WorldUpdatePassData
            {
                internal SurfaceCacheWorld World;
                internal uint EnvCubemapResolution;
                internal Light Sun;
            }

            private class DebugPassData
            {
                internal ComputeShader Shader;
                internal int KernelIndex;
                internal uint3 ThreadGroupSize;
                internal uint3 ThreadCount;
                internal TextureHandle ScreenDepths;
                internal TextureHandle ScreenShadedNormals;
                internal TextureHandle ScreenFlatNormals;
                internal TextureHandle ScreenIrradiances;
                internal GraphicsBuffer CellPatchIndices;
                internal GraphicsBuffer RingConfigBuffer;
                internal GraphicsBuffer PatchIrradiances;
                internal GraphicsBuffer PatchGeometries;
                internal GraphicsBuffer PatchCellIndices;
                internal GraphicsBuffer PatchStatistics;
                internal GraphicsBuffer PatchCounterSets;
                internal DebugViewMode_ ViewMode;
                internal uint FrameIndex;
                internal bool ShowSamplePosition;
                internal uint VolumeSpatialResolution;
                internal float VolumeVoxelMinSize;
                internal uint VolumeCascadeCount;
                internal GraphicsBuffer CascadeOffsets;
                internal uint RingConfigOffset;
                internal Matrix4x4 ClipToWorldTransform;
                internal Vector3 VolumeTargetPos;
            }

            private class FlatNormalResolutionPassData
            {
                internal ComputeShader Shader;
                internal int KernelIndex;
                internal uint3 ThreadGroupSize;
                internal uint3 ThreadCount;
                internal TextureHandle ScreenDepths;
                internal TextureHandle ScreenFlatNormals;
                internal Matrix4x4 ClipToWorldTransform;
            }

            private class PatchAllocationPassData
            {
                internal ComputeShader Shader;
                internal int KernelIndex;
                internal uint3 ThreadGroupSize;
                internal uint3 ThreadCount;
                internal TextureHandle ScreenDepths;
                internal TextureHandle ScreenFlatNormals;
                internal TextureHandle ScreenMotionVectors;
                internal TextureHandle LowResScreenIrradiancesL0;
                internal TextureHandle LowResScreenIrradiancesL10;
                internal TextureHandle LowResScreenIrradiancesL11;
                internal TextureHandle LowResScreenIrradiancesL12;
                internal TextureHandle LowResScreenNdcDepths;
                internal GraphicsBuffer CellAllocationMarks;
                internal GraphicsBuffer CellPatchIndices;
                internal GraphicsBuffer RingConfigBuffer;
                internal GraphicsBuffer PatchIrradiances0;
                internal GraphicsBuffer PatchIrradiances1;
                internal GraphicsBuffer PatchGeometries;
                internal GraphicsBuffer PatchCellIndices;
                internal GraphicsBuffer PatchCounterSets;
                internal uint FrameIdx;
                internal uint VolumeSpatialResolution;
                internal uint VolumeCascadeCount;
                internal uint RingConfigOffset;
                internal bool UseMotionVectorSeeding;
                internal GraphicsBuffer CascadeOffsets;
                internal float VoxelMinSize;
                internal Matrix4x4 CurrentClipToWorldTransform;
                internal Matrix4x4 PreviousClipToWorldTransform;
                internal Vector3 VolumeTargetPos;
                internal uint2 FullResPixelOffset;
                internal uint2 LowResScreenSize;
            }

            private class ScreenIrradianceLookupPassData
            {
                internal ComputeShader Shader;
                internal int KernelIndex;
                internal uint3 ThreadGroupSize;
                internal uint3 ThreadCount;
                internal TextureHandle FullResDepths;
                internal TextureHandle FullResFlatNormals;
                internal TextureHandle LowResScreenIrradiancesL0;
                internal TextureHandle LowResScreenIrradiancesL10;
                internal TextureHandle LowResScreenIrradiancesL11;
                internal TextureHandle LowResScreenIrradiancesL12;
                internal TextureHandle LowResScreenNdcDepths;
                internal GraphicsBuffer CellPatchIndices;
                internal GraphicsBuffer PatchIrradiances;
                internal GraphicsBuffer PatchCounterSets;
                internal GraphicsBuffer CascadeOffsets;
                internal uint VolumeSpatialResolution;
                internal uint VolumeCascadeCount;
                internal uint SampleCount;
                internal uint FrameIndex;
                internal float VolumeVoxelMinSize;
                internal Matrix4x4 ClipToWorldTransform;
                internal Vector3 VolumeTargetPos;
            }

            private class ScreenIrradianceUpsamplingPassData
            {
                internal ComputeShader Shader;
                internal int KernelIndex;
                internal uint3 ThreadGroupSize;
                internal uint3 FullResThreadCount;
                internal TextureHandle FullResDepths;
                internal TextureHandle FullResShadedNormals;
                internal TextureHandle FullResFlatNormals;
                internal TextureHandle LowResScreenIrradiancesL0;
                internal TextureHandle LowResScreenIrradiancesL10;
                internal TextureHandle LowResScreenIrradiancesL11;
                internal TextureHandle LowResScreenIrradiancesL12;
                internal TextureHandle FullResIrradiances;
                internal float FilterRadius;
                internal uint SampleCount;
                internal Matrix4x4 ClipToWorldTransform;
            }

            private RTHandle _fullResScreenIrradiances;
            private RTHandle _fullResScreenFlatNormals;
            private RTHandle _lowResScreenIrradiancesL0;
            private RTHandle _lowResScreenIrradiancesL10;
            private RTHandle _lowResScreenIrradiancesL11;
            private RTHandle _lowResScreenIrradiancesL12;
            private RTHandle _lowResScreenNdcDepths;
            private GraphicsBuffer _worldUpdateScratch;

            private readonly SceneUpdatesTracker _sceneTracker;
            private readonly WorldAdapter _worldAdapter;
            private readonly SurfaceCacheWorld _world;
            private readonly Material _fallbackMaterial;

            private readonly ComputeShader _screenResolveLookupShader;
            private readonly ComputeShader _screenResolveUpsamplingShader;
            private readonly ComputeShader _patchAllocationShader;
            private readonly ComputeShader _debugShader;
            private readonly ComputeShader _flatNormalResolutionShader;

            private readonly RayTracingContext _rtContext;

            private readonly int _screenResolveLookupKernel;
            private readonly int _screenResolveUpsamplingKernel;
            private readonly int _patchAllocationKernel;
            private readonly int _debugKernel;
            private readonly int _flatNormalResolutionKernel;

            private uint3 _screenResolveLookupKernelGroupSize;
            private uint3 _screenResolveUpsamplingKernelGroupSize;
            private uint3 _patchAllocationKernelGroupSize;
            private uint3 _debugKernelGroupSize;
            private uint3 _flatNormalResolutionKernelGroupSize;

            private uint _frameIdx;
            private bool _cascadeMovement;

            // Debug
            private readonly bool _debugEnabled;
            private readonly DebugViewMode_ _debugViewMode;
            private readonly bool _debugShowSamplePosition;

            // Screen Filtering
            private readonly uint _lookupSampleCount;
            private readonly float _upsamplingKernelSize;
            private readonly uint _upsamplingSampleCount;

            private SurfaceCache _cache;

            private Matrix4x4 _prevClipToWorldTransform = Matrix4x4.identity;

            readonly private uint _environmentCubemapResolution = 32;

            public SurfaceCachePass(
                RayTracingContext rtContext,
                UnityEngine.Rendering.SurfaceCacheResourceSet resourceSet,
                WorldResourceSet worldResources,
                ComputeShader patchAllocationShader,
                ComputeShader screenResolveLookupShader,
                ComputeShader screenResolveUpsamplingShader,
                ComputeShader debugShader,
                ComputeShader flatNormalResolutionShader,
                Material fallbackMaterial,
                bool debugEnabled,
                DebugViewMode_ debugViewMode,
                bool debugShowSamplePosition,
                uint lookupSampleCount,
                float upsamplingKernelSize,
                uint upsamplingSampleCount,
                uint defragCount,
                SurfaceCacheVolumeParameterSet volParams,
                SurfaceCacheEstimationParameterSet estimationParams,
                SurfaceCachePatchFilteringParameterSet patchFilteringParams,
                bool cascadeMovement)
            {
                Debug.Assert(volParams.CascadeCount != 0);
                Debug.Assert(volParams.CascadeCount <= SurfaceCache.CascadeMax);

                _screenResolveLookupShader = screenResolveLookupShader;
                _screenResolveUpsamplingShader = screenResolveUpsamplingShader;
                _debugShader = debugShader;
                _flatNormalResolutionShader = flatNormalResolutionShader;
                _patchAllocationShader = patchAllocationShader;
                _rtContext = rtContext;

                _screenResolveLookupKernel = _screenResolveLookupShader.FindKernel("Lookup");
                _screenResolveUpsamplingKernel = _screenResolveUpsamplingShader.FindKernel("Upsample");
                _patchAllocationKernel = _patchAllocationShader.FindKernel("Allocate");
                _debugKernel = _debugShader.FindKernel("Visualize");
                _flatNormalResolutionKernel = _flatNormalResolutionShader.FindKernel("ResolveFlatNormals");

                _cascadeMovement = cascadeMovement;

                _screenResolveLookupShader.GetKernelThreadGroupSizes(_screenResolveLookupKernel, out _screenResolveLookupKernelGroupSize.x, out _screenResolveLookupKernelGroupSize.y, out _screenResolveLookupKernelGroupSize.z);
                _screenResolveUpsamplingShader.GetKernelThreadGroupSizes(_screenResolveUpsamplingKernel, out _screenResolveUpsamplingKernelGroupSize.x, out _screenResolveUpsamplingKernelGroupSize.y, out _screenResolveUpsamplingKernelGroupSize.z);
                _patchAllocationShader.GetKernelThreadGroupSizes(_patchAllocationKernel, out _patchAllocationKernelGroupSize.x, out _patchAllocationKernelGroupSize.y, out _patchAllocationKernelGroupSize.z);
                _debugShader.GetKernelThreadGroupSizes(_debugKernel, out _debugKernelGroupSize.x, out _debugKernelGroupSize.y, out _debugKernelGroupSize.z);
                _flatNormalResolutionShader.GetKernelThreadGroupSizes(_flatNormalResolutionKernel, out _flatNormalResolutionKernelGroupSize.x, out _flatNormalResolutionKernelGroupSize.y, out _flatNormalResolutionKernelGroupSize.z);

                _frameIdx = 0;

                _debugEnabled = debugEnabled;
                _debugViewMode = debugViewMode;
                _debugShowSamplePosition = debugShowSamplePosition;

                _upsamplingKernelSize = upsamplingKernelSize;
                _upsamplingSampleCount = upsamplingSampleCount;
                _lookupSampleCount = lookupSampleCount;

                _cache = new SurfaceCache(resourceSet, defragCount, volParams, estimationParams, patchFilteringParams);
                _sceneTracker = new SceneUpdatesTracker();

                _world = new SurfaceCacheWorld();
                _world.Init(_rtContext, worldResources);

                _fallbackMaterial = fallbackMaterial;
                _worldAdapter = new WorldAdapter(_world, _fallbackMaterial);
            }

            public void Dispose()
            {
                _fullResScreenIrradiances?.Release();
                _fullResScreenFlatNormals?.Release();
                _lowResScreenIrradiancesL0?.Release();
                _lowResScreenIrradiancesL10?.Release();
                _lowResScreenIrradiancesL11?.Release();
                _lowResScreenIrradiancesL12?.Release();
                _lowResScreenNdcDepths?.Release();
                _sceneTracker.Dispose();
                _cache.Dispose();
                _world.Dispose();
                _worldAdapter.Dispose();
                _worldUpdateScratch?.Dispose();
            }

            const int k_UpscaleFactor = 4;

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                Debug.Assert(resourceData.cameraDepthTexture.IsValid());
                Debug.Assert(resourceData.cameraNormalsTexture.IsValid());
                Debug.Assert(resourceData.motionVectorColor.IsValid());

                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                var worldToViewTransform = cameraData.GetViewMatrix();
                var viewToClipTransform = cameraData.GetGPUProjectionMatrix(true);
                var worldToClipTransform = viewToClipTransform * worldToViewTransform;
                var clipToWorldTransform = worldToClipTransform.inverse;

                var screenResolution = new int2(cameraData.pixelWidth, cameraData.pixelHeight);

                // This avoids applying surface cache to e.g. preview cameras.
                if (cameraData.cameraType != CameraType.Game && cameraData.cameraType != CameraType.SceneView)
                    return;

                bool useMotionVectorPatchSeeding = UseMotionVectorPatchSeeding(cameraData.cameraType);

                if (_fullResScreenIrradiances == null || _fullResScreenIrradiances.GetScaledSize() != new Vector2Int(screenResolution.x, screenResolution.y))
                {
                    int lowResWidth = (screenResolution.x + k_UpscaleFactor - 1) / k_UpscaleFactor;
                    int lowResHeight = (screenResolution.y + k_UpscaleFactor - 1) / k_UpscaleFactor;

                    _fullResScreenIrradiances?.Release();
                    _fullResScreenIrradiances = RTHandles.Alloc((int)screenResolution.x, (int)screenResolution.y, 1, DepthBits.None, GraphicsFormat.R16G16B16A16_SFloat, FilterMode.Point, TextureWrapMode.Clamp, TextureDimension.Tex2D, true, name: "_fullResScreenIrradiances");

                    _fullResScreenFlatNormals?.Release();
                    _fullResScreenFlatNormals = RTHandles.Alloc((int)screenResolution.x, (int)screenResolution.y, 1, DepthBits.None, GraphicsFormat.R8G8B8A8_SNorm, FilterMode.Point, TextureWrapMode.Clamp, TextureDimension.Tex2D, true, name: "_fullResScreenFlatNormals");

                    var l0Format = GraphicsFormat.R16G16B16A16_SFloat;
                    var l1Format = GraphicsFormat.R8G8B8A8_UNorm;
                    _lowResScreenIrradiancesL0?.Release();
                    _lowResScreenIrradiancesL0 = RTHandles.Alloc(lowResWidth, lowResHeight, 1, DepthBits.None, l0Format, FilterMode.Point, TextureWrapMode.Clamp, TextureDimension.Tex2D, true, name: "_lowResScreenIrradiancesL0");
                    _lowResScreenIrradiancesL10?.Release();
                    _lowResScreenIrradiancesL10 = RTHandles.Alloc(lowResWidth, lowResHeight, 1, DepthBits.None, l1Format, FilterMode.Point, TextureWrapMode.Clamp, TextureDimension.Tex2D, true, name: "_lowResScreenIrradiancesL10");
                    _lowResScreenIrradiancesL11?.Release();
                    _lowResScreenIrradiancesL11 = RTHandles.Alloc(lowResWidth, lowResHeight, 1, DepthBits.None, l1Format, FilterMode.Point, TextureWrapMode.Clamp, TextureDimension.Tex2D, true, name: "_lowResScreenIrradiancesL11");
                    _lowResScreenIrradiancesL12?.Release();
                    _lowResScreenIrradiancesL12 = RTHandles.Alloc(lowResWidth, lowResHeight, 1, DepthBits.None, l1Format, FilterMode.Point, TextureWrapMode.Clamp, TextureDimension.Tex2D, true, name: "_lowResScreenIrradiancesL12");
                    _lowResScreenNdcDepths?.Release();
                    _lowResScreenNdcDepths = RTHandles.Alloc(lowResWidth, lowResHeight, 1, DepthBits.None, GraphicsFormat.R16_UNorm, FilterMode.Point, TextureWrapMode.Clamp, TextureDimension.Tex2D, true, name: "_lowResScreenNdcDepths");
                }

                if (_cascadeMovement || _frameIdx == 0)
                {
                    _cache.Volume.TargetPos = cameraData.camera.transform.position;
                }

                _cache.RecordPreparation(renderGraph, _frameIdx);

                var fullResScreenIrradiancesHandle = renderGraph.ImportTexture(_fullResScreenIrradiances);
                var fullResScreenFlatNormalsHandle = renderGraph.ImportTexture(_fullResScreenFlatNormals);
                var lowResScreenIrradiancesL0Handle = renderGraph.ImportTexture(_lowResScreenIrradiancesL0);
                var lowResScreenIrradiancesL10Handle = renderGraph.ImportTexture(_lowResScreenIrradiancesL10);
                var lowResScreenIrradiancesL11Handle = renderGraph.ImportTexture(_lowResScreenIrradiancesL11);
                var lowResScreenIrradiancesL12Handle = renderGraph.ImportTexture(_lowResScreenIrradiancesL12);
                var lowResScreenNdcDepthsHandle = renderGraph.ImportTexture(_lowResScreenNdcDepths);
                var cellAllocationMarkHandle = renderGraph.ImportBuffer(_cache.Volume.CellAllocationMarks);

                using (var builder = renderGraph.AddComputePass("Surface Cache Flat Normal Resolution", out FlatNormalResolutionPassData passData))
                {
                    passData.ThreadCount = new uint3((uint)screenResolution.x, (uint)screenResolution.y, 1);
                    passData.Shader = _flatNormalResolutionShader;
                    passData.KernelIndex = _flatNormalResolutionKernel;
                    passData.ThreadGroupSize = _flatNormalResolutionKernelGroupSize;
                    passData.ScreenDepths = resourceData.cameraDepthTexture;
                    passData.ScreenFlatNormals = fullResScreenFlatNormalsHandle;
                    passData.ClipToWorldTransform = clipToWorldTransform;

                    builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                    builder.UseTexture(fullResScreenFlatNormalsHandle, AccessFlags.Write);
                    builder.SetRenderFunc((FlatNormalResolutionPassData data, ComputeGraphContext cgContext) => ResolveFlatNormals(data, cgContext));
                }

                using (var builder = renderGraph.AddComputePass("Surface Cache Patch Allocation", out PatchAllocationPassData passData))
                {
                    uint2 fullResScreenSize = new uint2((uint)screenResolution.x, (uint)screenResolution.y);
                    uint2 lowResScreenSize = DivUp(fullResScreenSize, new uint2(k_UpscaleFactor, k_UpscaleFactor));

                    passData.ThreadCount = new uint3(lowResScreenSize, 1);
                    passData.Shader = _patchAllocationShader;
                    passData.KernelIndex = _patchAllocationKernel;
                    passData.ThreadGroupSize = _patchAllocationKernelGroupSize;
                    passData.ScreenDepths = resourceData.cameraDepthTexture;
                    passData.ScreenFlatNormals = fullResScreenFlatNormalsHandle;
                    passData.ScreenMotionVectors = resourceData.motionVectorColor;
                    passData.LowResScreenIrradiancesL0 = lowResScreenIrradiancesL0Handle;
                    passData.LowResScreenIrradiancesL10 = lowResScreenIrradiancesL10Handle;
                    passData.LowResScreenIrradiancesL11 = lowResScreenIrradiancesL11Handle;
                    passData.LowResScreenIrradiancesL12 = lowResScreenIrradiancesL12Handle;
                    passData.LowResScreenNdcDepths = lowResScreenNdcDepthsHandle;
                    passData.CellAllocationMarks = _cache.Volume.CellAllocationMarks;
                    passData.CellPatchIndices = _cache.Volume.CellPatchIndices;
                    passData.RingConfigBuffer = _cache.RingConfig.Buffer;
                    passData.PatchIrradiances0 = _cache.Patches.Irradiances[0];
                    passData.PatchIrradiances1 = _cache.Patches.Irradiances[2];
                    passData.PatchGeometries = _cache.Patches.Geometries;
                    passData.PatchCellIndices = _cache.Patches.CellIndices;
                    passData.PatchCounterSets = _cache.Patches.CounterSets;
                    passData.FrameIdx = _frameIdx;
                    passData.VolumeSpatialResolution = _cache.Volume.SpatialResolution;
                    passData.VolumeCascadeCount = _cache.Volume.CascadeCount;
                    passData.RingConfigOffset = _cache.RingConfig.OffsetA;

                    {
                        Debug.Assert(k_UpscaleFactor == 4);
                        uint cycleIndex = _frameIdx % 4;
                        uint shuffledCycleIndex = cycleIndex * 7 % 16;
                        passData.FullResPixelOffset = new uint2(shuffledCycleIndex / 4, shuffledCycleIndex % 4);
                    }

                    passData.LowResScreenSize = lowResScreenSize;
                    passData.UseMotionVectorSeeding = useMotionVectorPatchSeeding;
                    passData.CascadeOffsets = _cache.Volume.CascadeOffsetBuffer;
                    passData.VoxelMinSize = _cache.Volume.VoxelMinSize;
                    passData.CurrentClipToWorldTransform = clipToWorldTransform;
                    passData.PreviousClipToWorldTransform = _prevClipToWorldTransform;
                    passData.VolumeTargetPos = _cache.Volume.TargetPos;

                    builder.UseBuffer(cellAllocationMarkHandle, AccessFlags.Write);
                    builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                    builder.UseTexture(fullResScreenFlatNormalsHandle, AccessFlags.Read);
                    builder.UseTexture(resourceData.motionVectorColor, AccessFlags.Read);
                    builder.UseTexture(lowResScreenIrradiancesL0Handle, AccessFlags.Read);
                    builder.UseTexture(lowResScreenIrradiancesL10Handle, AccessFlags.Read);
                    builder.UseTexture(lowResScreenIrradiancesL11Handle, AccessFlags.Read);
                    builder.UseTexture(lowResScreenIrradiancesL12Handle, AccessFlags.Read);
                    builder.UseTexture(lowResScreenNdcDepthsHandle, AccessFlags.Read);

                    builder.SetRenderFunc((PatchAllocationPassData data, ComputeGraphContext cgContext) => AllocatePatches(data, cgContext));
                }

                using (var builder = renderGraph.AddUnsafePass("Surface Cache World Update", out WorldUpdatePassData passData))
                {
                    const bool filterBakedLights = true;
                    var changes = _sceneTracker.GetChanges(filterBakedLights);

                    _worldAdapter.UpdateMaterials(_world, changes.addedMaterials, changes.removedMaterials, changes.changedMaterials);
                    _worldAdapter.UpdateInstances(_world, changes.addedInstances, changes.changedInstances, changes.removedInstances, _fallbackMaterial);
                    const bool multiplyPunctualLightIntensityByPI = false;
                    _worldAdapter.UpdateLights(_world, changes.addedLights, changes.removedLights, changes.changedLights, multiplyPunctualLightIntensityByPI);

                    if (RenderSettings.ambientMode == AmbientMode.Skybox)
                    {
                        _world.SetEnvironmentMode(CubemapRender.Mode.Material);
                        _world.SetEnvironmentMaterial(RenderSettings.skybox);
                    }
                    else if (RenderSettings.ambientMode == AmbientMode.Flat)
                    {
                        _world.SetEnvironmentMode(CubemapRender.Mode.Color);
                        _world.SetEnvironmentColor(RenderSettings.ambientSkyColor);
                    }

                    passData.World = _world;
                    passData.EnvCubemapResolution = _environmentCubemapResolution;
                    passData.Sun = RenderSettings.sun;



                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc((WorldUpdatePassData data, UnsafeGraphContext graphCtx) => UpdateWorld(data, graphCtx, ref _worldUpdateScratch));
                }

                uint outputIrradianceBufferIdx = _cache.RecordPatchUpdate(renderGraph, _frameIdx, _world);

                using (var builder = renderGraph.AddComputePass("Surface Cache Screen Lookup", out ScreenIrradianceLookupPassData data))
                {
                    data.VolumeTargetPos = _cache.Volume.TargetPos;
                    data.ClipToWorldTransform = clipToWorldTransform;
                    data.ThreadCount = new uint3((uint)_lowResScreenIrradiancesL0.rt.width, (uint)_lowResScreenIrradiancesL0.rt.height, 1);
                    data.Shader = _screenResolveLookupShader;
                    data.KernelIndex = _screenResolveLookupKernel;
                    data.ThreadGroupSize = _screenResolveLookupKernelGroupSize;
                    data.FullResDepths = resourceData.cameraDepthTexture;
                    data.FullResFlatNormals = fullResScreenFlatNormalsHandle;
                    data.LowResScreenIrradiancesL0 = lowResScreenIrradiancesL0Handle;
                    data.LowResScreenIrradiancesL10 = lowResScreenIrradiancesL10Handle;
                    data.LowResScreenIrradiancesL11 = lowResScreenIrradiancesL11Handle;
                    data.LowResScreenIrradiancesL12 = lowResScreenIrradiancesL12Handle;
                    data.LowResScreenNdcDepths = lowResScreenNdcDepthsHandle;
                    data.CellPatchIndices = _cache.Volume.CellPatchIndices;
                    data.PatchIrradiances = _cache.Patches.Irradiances[outputIrradianceBufferIdx];
                    data.PatchCounterSets = _cache.Patches.CounterSets;
                    data.CascadeOffsets = _cache.Volume.CascadeOffsetBuffer;
                    data.VolumeSpatialResolution = _cache.Volume.SpatialResolution;
                    data.VolumeVoxelMinSize = _cache.Volume.VoxelMinSize;
                    data.VolumeCascadeCount = _cache.Volume.CascadeCount;
                    data.SampleCount = _lookupSampleCount;
                    data.FrameIndex = _frameIdx;

                    builder.UseBuffer(cellAllocationMarkHandle, AccessFlags.Read);
                    builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                    builder.UseTexture(fullResScreenFlatNormalsHandle, AccessFlags.Read);
                    builder.UseTexture(resourceData.motionVectorColor, AccessFlags.Read);
                    builder.UseTexture(data.LowResScreenIrradiancesL0, AccessFlags.Write);
                    builder.UseTexture(data.LowResScreenIrradiancesL10, AccessFlags.Write);
                    builder.UseTexture(data.LowResScreenIrradiancesL11, AccessFlags.Write);
                    builder.UseTexture(data.LowResScreenIrradiancesL12, AccessFlags.Write);
                    builder.UseTexture(data.LowResScreenNdcDepths, AccessFlags.Write);

                    builder.SetRenderFunc((ScreenIrradianceLookupPassData data, ComputeGraphContext cgContext) => LookupScreenIrradiance(data, cgContext));
                }

                if (_debugEnabled)
                {
                    using (var builder = renderGraph.AddComputePass("Surface Cache Debug", out DebugPassData passData))
                    {
                        passData.ThreadCount = new uint3((uint)screenResolution.x, (uint)screenResolution.y, 1);
                        passData.Shader = _debugShader;
                        passData.KernelIndex = _debugKernel;
                        passData.ThreadGroupSize = _debugKernelGroupSize;
                        passData.ScreenDepths = resourceData.cameraDepthTexture;
                        passData.ScreenShadedNormals = resourceData.cameraNormalsTexture;
                        passData.ScreenFlatNormals = fullResScreenFlatNormalsHandle;
                        passData.ScreenIrradiances = fullResScreenIrradiancesHandle;
                        passData.PatchCellIndices = _cache.Patches.CellIndices;
                        passData.CellPatchIndices = _cache.Volume.CellPatchIndices;
                        passData.RingConfigBuffer = _cache.RingConfig.Buffer;
                        passData.RingConfigOffset = _cache.RingConfig.OffsetA;
                        passData.VolumeTargetPos = _cache.Volume.TargetPos;
                        passData.PatchIrradiances = _cache.Patches.Irradiances[outputIrradianceBufferIdx];
                        passData.PatchGeometries = _cache.Patches.Geometries;
                        passData.PatchStatistics = _cache.Patches.Statistics;
                        passData.PatchCounterSets = _cache.Patches.CounterSets;
                        passData.VolumeSpatialResolution = _cache.Volume.SpatialResolution;
                        passData.VolumeVoxelMinSize = _cache.Volume.VoxelMinSize;
                        passData.VolumeCascadeCount = _cache.Volume.CascadeCount;
                        passData.CascadeOffsets = _cache.Volume.CascadeOffsetBuffer;
                        passData.ViewMode = _debugViewMode;
                        passData.FrameIndex = _frameIdx;
                        passData.ShowSamplePosition = _debugShowSamplePosition;
                        passData.ClipToWorldTransform = clipToWorldTransform;

                        builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                        builder.UseTexture(resourceData.cameraNormalsTexture, AccessFlags.Read);
                        builder.UseTexture(fullResScreenFlatNormalsHandle, AccessFlags.Read);
                        builder.UseTexture(passData.ScreenIrradiances, AccessFlags.Write);

                        builder.SetRenderFunc((DebugPassData data, ComputeGraphContext cgContext) => RenderDebug(data, cgContext));
                    }
                }
                else
                {
                    using (var builder = renderGraph.AddComputePass("Surface Cache Screen Upsampling", out ScreenIrradianceUpsamplingPassData data))
                    {
                        data.ClipToWorldTransform = clipToWorldTransform;
                        data.FullResThreadCount = new uint3((uint)screenResolution.x, (uint)screenResolution.y, 1);
                        data.Shader = _screenResolveUpsamplingShader;
                        data.KernelIndex = _screenResolveUpsamplingKernel;
                        data.ThreadGroupSize = _screenResolveUpsamplingKernelGroupSize;
                        data.FullResDepths = resourceData.cameraDepthTexture;
                        data.FullResShadedNormals = resourceData.cameraNormalsTexture;
                        data.FullResFlatNormals = fullResScreenFlatNormalsHandle;
                        data.LowResScreenIrradiancesL0 = lowResScreenIrradiancesL0Handle;
                        data.LowResScreenIrradiancesL10 = lowResScreenIrradiancesL10Handle;
                        data.LowResScreenIrradiancesL11 = lowResScreenIrradiancesL11Handle;
                        data.LowResScreenIrradiancesL12 = lowResScreenIrradiancesL12Handle;
                        data.FullResIrradiances = fullResScreenIrradiancesHandle;
                        data.FilterRadius = _upsamplingKernelSize;
                        data.SampleCount = _upsamplingSampleCount;

                        builder.UseTexture(data.FullResIrradiances, AccessFlags.Write);
                        builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                        builder.UseTexture(resourceData.cameraNormalsTexture, AccessFlags.Read);
                        builder.UseTexture(fullResScreenFlatNormalsHandle, AccessFlags.Read);
                        builder.UseTexture(resourceData.motionVectorColor, AccessFlags.Read);
                        builder.UseTexture(data.LowResScreenIrradiancesL0, AccessFlags.Read);
                        builder.UseTexture(data.LowResScreenIrradiancesL10, AccessFlags.Read);
                        builder.UseTexture(data.LowResScreenIrradiancesL11, AccessFlags.Read);
                        builder.UseTexture(data.LowResScreenIrradiancesL12, AccessFlags.Read);
                        builder.UseBuffer(cellAllocationMarkHandle, AccessFlags.Read);

                        builder.SetRenderFunc((ScreenIrradianceUpsamplingPassData data, ComputeGraphContext cgContext) => UpsampleScreenIrradiance(data, cgContext));
                    }
                }

                resourceData.irradianceTexture = fullResScreenIrradiancesHandle;
                _frameIdx++;
                _prevClipToWorldTransform = clipToWorldTransform;
            }

            static void UpdateWorld(WorldUpdatePassData data, UnsafeGraphContext graphCtx, ref GraphicsBuffer scratch)
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(graphCtx.cmd);
                data.World.Commit(cmd, ref scratch, data.EnvCubemapResolution, data.Sun);
            }

            static void LookupScreenIrradiance(ScreenIrradianceLookupPassData data, ComputeGraphContext cgContext)
            {
                var cmd = cgContext.cmd;
                var shader = data.Shader;
                var kernelIndex = data.KernelIndex;
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ResultL0, data.LowResScreenIrradiancesL0);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ResultL10, data.LowResScreenIrradiancesL10);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ResultL11, data.LowResScreenIrradiancesL11);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ResultL12, data.LowResScreenIrradiancesL12);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ResultNdcDepths, data.LowResScreenNdcDepths);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ScreenDepths, data.FullResDepths);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ScreenFlatNormals, data.FullResFlatNormals);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._CellPatchIndices, data.CellPatchIndices);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchIrradiances, data.PatchIrradiances);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._CascadeOffsets, data.CascadeOffsets);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchCounterSets, data.PatchCounterSets);
                cmd.SetComputeIntParam(shader, ShaderIDs._VolumeSpatialResolution, (int)data.VolumeSpatialResolution);
                cmd.SetComputeIntParam(shader, ShaderIDs._CascadeCount, (int)data.VolumeCascadeCount);
                cmd.SetComputeIntParam(shader, ShaderIDs._SampleCount, (int)data.SampleCount);
                cmd.SetComputeIntParam(shader, ShaderIDs._FrameIdx, (int)data.FrameIndex);
                cmd.SetComputeFloatParam(shader, ShaderIDs._VolumeVoxelMinSize, data.VolumeVoxelMinSize);
                cmd.SetComputeMatrixParam(shader, ShaderIDs._ClipToWorldTransform, data.ClipToWorldTransform);
                cmd.SetComputeVectorParam(shader, ShaderIDs._VolumeTargetPos, data.VolumeTargetPos);

                uint3 groupCount = DivUp(data.ThreadCount, data.ThreadGroupSize);
                cmd.DispatchCompute(shader, kernelIndex, (int)groupCount.x, (int)groupCount.y, 1);
            }

            static void UpsampleScreenIrradiance(ScreenIrradianceUpsamplingPassData passData, ComputeGraphContext cgContext)
            {
                var cmd = cgContext.cmd;

                var shader = passData.Shader;
                var kernelIndex = passData.KernelIndex;
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._FullResIrradiances, passData.FullResIrradiances);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._FullResDepths, passData.FullResDepths);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._FullResFlatNormals, passData.FullResFlatNormals);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._FullResShadedNormals, passData.FullResShadedNormals);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._LowResIrradiancesL0, passData.LowResScreenIrradiancesL0);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._LowResIrradiancesL10, passData.LowResScreenIrradiancesL10);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._LowResIrradiancesL11, passData.LowResScreenIrradiancesL11);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._LowResIrradiancesL12, passData.LowResScreenIrradiancesL12);
                cmd.SetComputeFloatParam(shader, ShaderIDs._FilterRadius, passData.FilterRadius);
                cmd.SetComputeIntParam(shader, ShaderIDs._SampleCount, (int)passData.SampleCount);
                cmd.SetComputeMatrixParam(shader, ShaderIDs._ClipToWorldTransform, passData.ClipToWorldTransform);

                uint3 groupCount = DivUp(passData.FullResThreadCount, passData.ThreadGroupSize);
                cmd.DispatchCompute(shader, kernelIndex, (int)groupCount.x, (int)groupCount.y, 1);
            }

            static void ResolveFlatNormals(FlatNormalResolutionPassData data, ComputeGraphContext cgContext)
            {
                var cmd = cgContext.cmd;
                var shader = data.Shader;
                var kernelIndex = data.KernelIndex;
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ScreenDepths, data.ScreenDepths);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ScreenFlatNormals, data.ScreenFlatNormals);
                cmd.SetComputeMatrixParam(shader, ShaderIDs._ClipToWorldTransform, data.ClipToWorldTransform);

                uint3 groupCount = DivUp(data.ThreadCount, data.ThreadGroupSize);
                cmd.DispatchCompute(shader, kernelIndex, (int)groupCount.x, (int)groupCount.y, 1);
            }

            static void AllocatePatches(PatchAllocationPassData data, ComputeGraphContext cgContext)
            {
                var cmd = cgContext.cmd;
                var shader = data.Shader;
                var kernelIndex = data.KernelIndex;
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._CurrentFullResScreenDepths, data.ScreenDepths);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._CurrentFullResScreenFlatNormals, data.ScreenFlatNormals);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._CurrentFullResScreenMotionVectors, data.ScreenMotionVectors);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._PreviousLowResScreenIrradiancesL0, data.LowResScreenIrradiancesL0);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._PreviousLowResScreenIrradiancesL10, data.LowResScreenIrradiancesL10);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._PreviousLowResScreenIrradiancesL11, data.LowResScreenIrradiancesL11);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._PreviousLowResScreenIrradiancesL12, data.LowResScreenIrradiancesL12);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._PreviousLowResScreenNdcDepths, data.LowResScreenNdcDepths);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._CellAllocationMarks, data.CellAllocationMarks);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._CellPatchIndices, data.CellPatchIndices);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._RingConfigBuffer, data.RingConfigBuffer);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchIrradiances0, data.PatchIrradiances0);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchIrradiances1, data.PatchIrradiances1);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchGeometries, data.PatchGeometries);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchCellIndices, data.PatchCellIndices);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchCounterSets, data.PatchCounterSets);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._CascadeOffsets, data.CascadeOffsets);
                cmd.SetComputeIntParam(shader, ShaderIDs._FrameIdx, (int)data.FrameIdx);
                cmd.SetComputeIntParam(shader, ShaderIDs._VolumeSpatialResolution, (int)data.VolumeSpatialResolution);
                cmd.SetComputeIntParam(shader, ShaderIDs._CascadeCount, (int)data.VolumeCascadeCount);
                cmd.SetComputeIntParam(shader, ShaderIDs._RingConfigOffset, (int)data.RingConfigOffset);
                cmd.SetComputeIntParams(shader, ShaderIDs._FullResPixelOffset, (int)data.FullResPixelOffset.x, (int)data.FullResPixelOffset.y);
                cmd.SetComputeIntParams(shader, ShaderIDs._LowResScreenSize, (int)data.LowResScreenSize.x, (int)data.LowResScreenSize.y);
                cmd.SetComputeIntParam(shader, ShaderIDs._UseMotionVectorSeeding, data.UseMotionVectorSeeding ? 1 : 0);
                cmd.SetComputeFloatParam(shader, ShaderIDs._VolumeVoxelMinSize, data.VoxelMinSize);
                cmd.SetComputeMatrixParam(shader, ShaderIDs._CurrentClipToWorldTransform, data.CurrentClipToWorldTransform);
                cmd.SetComputeMatrixParam(shader, ShaderIDs._PreviousClipToWorldTransform, data.PreviousClipToWorldTransform);
                cmd.SetComputeVectorParam(shader, ShaderIDs._VolumeTargetPos, data.VolumeTargetPos);

                uint3 groupCount = DivUp(data.ThreadCount, data.ThreadGroupSize);
                cmd.DispatchCompute(shader, kernelIndex, (int)groupCount.x, (int)groupCount.y, 1);
            }

            static void RenderDebug(DebugPassData data, ComputeGraphContext cgContext)
            {
                var cmd = cgContext.cmd;
                var shader = data.Shader;
                var kernelIndex = data.KernelIndex;
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._Result, data.ScreenIrradiances);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ScreenDepths, data.ScreenDepths);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ScreenShadedNormals, data.ScreenShadedNormals);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ScreenFlatNormals, data.ScreenFlatNormals);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._CellPatchIndices, data.CellPatchIndices);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._RingConfigBuffer, data.RingConfigBuffer);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchIrradiances, data.PatchIrradiances);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchGeometries, data.PatchGeometries);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._CascadeOffsets, data.CascadeOffsets);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchCellIndices, data.PatchCellIndices);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchStatistics, data.PatchStatistics);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchCounterSets, data.PatchCounterSets);

                cmd.SetComputeIntParam(shader, ShaderIDs._VolumeSpatialResolution, (int)data.VolumeSpatialResolution);
                cmd.SetComputeIntParam(shader, ShaderIDs._CascadeCount, (int)data.VolumeCascadeCount);
                cmd.SetComputeIntParam(shader, ShaderIDs._ViewMode, (int)data.ViewMode);
                cmd.SetComputeIntParam(shader, ShaderIDs._FrameIdx, (int)data.FrameIndex);
                cmd.SetComputeIntParam(shader, ShaderIDs._ShowSamplePosition, data.ShowSamplePosition ? 1 : 0);
                cmd.SetComputeFloatParam(shader, ShaderIDs._VolumeVoxelMinSize, data.VolumeVoxelMinSize);
                cmd.SetComputeFloatParam(shader, ShaderIDs._RingConfigOffset, data.RingConfigOffset);
                cmd.SetComputeVectorParam(shader, ShaderIDs._VolumeTargetPos, data.VolumeTargetPos);
                cmd.SetComputeMatrixParam(shader, ShaderIDs._ClipToWorldTransform, data.ClipToWorldTransform);

                uint3 groupCount = DivUp(data.ThreadCount, data.ThreadGroupSize);
                cmd.DispatchCompute(shader, kernelIndex, (int)groupCount.x, (int)groupCount.y, 1);
            }

            private static uint3 DivUp(uint3 x, uint3 y) => (x + y - 1) / y;

            private static uint2 DivUp(uint2 x, uint2 y) => (x + y - 1) / y;
        }

        class WorldAdapter : IDisposable
        {
            // This dictionary maps from Unity EntityID for MeshRenderer or Terrain, to corresponding InstanceHandle for accessing World.
            private readonly Dictionary<EntityId, InstanceHandle> _entityIDToWorldInstanceHandles = new();

            // Same as above but for Lights
            private readonly Dictionary<EntityId, LightHandle> _entityIDToWorldLightHandles = new();

            // Same as above but for Materials
            private Dictionary<EntityId, MaterialHandle> _entityIDToWorldMaterialHandles = new();

            // We also keep track of associated material descriptors, so we can free temporary temporary textures when a material is removed
            private Dictionary<EntityId, MaterialPool.MaterialDescriptor> _entityIDToWorldMaterialDescriptors = new();

            private MaterialPool.MaterialDescriptor _fallbackMaterialDescriptor;
            private MaterialHandle _fallbackMaterialHandle;

            public WorldAdapter(SurfaceCacheWorld world, Material fallbackMaterial)
            {
                _fallbackMaterialDescriptor = MaterialPool.ConvertUnityMaterialToMaterialDescriptor(fallbackMaterial);
                _fallbackMaterialHandle = world.AddMaterial(in _fallbackMaterialDescriptor, UVChannel.UV0);
                _entityIDToWorldMaterialHandles.Add(fallbackMaterial.GetEntityId(), _fallbackMaterialHandle);
                _entityIDToWorldMaterialDescriptors.Add(fallbackMaterial.GetEntityId(), _fallbackMaterialDescriptor);
            }

            public void UpdateMaterials(SurfaceCacheWorld world, List<Material> addedMaterials, List<EntityId> removedMaterials, List<Material> changedMaterials)
            {
                UpdateMaterials(world, _entityIDToWorldMaterialHandles, _entityIDToWorldMaterialDescriptors, addedMaterials, removedMaterials, changedMaterials);
            }

            private static void UpdateMaterials(SurfaceCacheWorld world, Dictionary<EntityId, MaterialHandle> entityIDToHandle, Dictionary<EntityId, MaterialPool.MaterialDescriptor> entityIDToDescriptor, List<Material> addedMaterials, List<EntityId> removedMaterials, List<Material> changedMaterials)
            {
                static void DeleteTemporaryTextures(ref MaterialPool.MaterialDescriptor desc)
                {
                    CoreUtils.Destroy(desc.Albedo);
                    CoreUtils.Destroy(desc.Emission);
                    CoreUtils.Destroy(desc.Transmission);
                }

                foreach (var entityID in removedMaterials)
                {
                    // Clean up temporary textures in the descriptor
                    Debug.Assert(entityIDToDescriptor.ContainsKey(entityID));
                    var descriptor = entityIDToDescriptor[entityID];
                    DeleteTemporaryTextures(ref descriptor);
                    entityIDToDescriptor.Remove(entityID);

                    // Remove the material from the world
                    Debug.Assert(entityIDToHandle.ContainsKey(entityID));
                    world.RemoveMaterial(entityIDToHandle[entityID]);
                    entityIDToHandle.Remove(entityID);
                }

                foreach (var material in addedMaterials)
                {
                    // Add material to the world
                    var descriptor = MaterialPool.ConvertUnityMaterialToMaterialDescriptor(material);
                    var handle = world.AddMaterial(in descriptor, UVChannel.UV0);
                    entityIDToHandle.Add(material.GetEntityId(), handle);

                    // Keep track of the descriptor
                    entityIDToDescriptor.Add(material.GetEntityId(), descriptor);
                }

                foreach (var material in changedMaterials)
                {
                    // Clean up temporary textures in the old descriptor
                    Debug.Assert(entityIDToDescriptor.ContainsKey(material.GetEntityId()));
                    var oldDescriptor = entityIDToDescriptor[material.GetEntityId()];
                    DeleteTemporaryTextures(ref oldDescriptor);

                    // Update the material in the world using the new descriptor
                    Debug.Assert(entityIDToHandle.ContainsKey(material.GetEntityId()));
                    var newDescriptor = MaterialPool.ConvertUnityMaterialToMaterialDescriptor(material);
                    world.UpdateMaterial(entityIDToHandle[material.GetEntityId()], in newDescriptor, UVChannel.UV0);
                    entityIDToDescriptor[material.GetEntityId()] = newDescriptor;
                }
            }

            internal void UpdateLights(SurfaceCacheWorld world, List<Light> addedLights, List<EntityId> removedLights,
                List<Light> changedLights, bool multiplyPunctualLightIntensityByPI)
            {
                UpdateLights(world, _entityIDToWorldLightHandles, addedLights, removedLights, changedLights, multiplyPunctualLightIntensityByPI);
            }

            private static void UpdateLights(
                SurfaceCacheWorld world,
                Dictionary<EntityId, LightHandle> entityIDToHandle, List<Light> addedLights, List<EntityId> removedLights,
                List<Light> changedLights,
                bool multiplyPunctualLightIntensityByPI)
            {
                // Remove deleted lights
                LightHandle[] handlesToRemove = new LightHandle[removedLights.Count];
                for (int i = 0; i < removedLights.Count; i++)
                {
                    var lightEntityID = removedLights[i];
                    handlesToRemove[i] = entityIDToHandle[lightEntityID];
                    entityIDToHandle.Remove(lightEntityID);
                }
                world.RemoveLights(handlesToRemove);

                // Add new lights
                var lightDescriptors = ConvertUnityLightsToLightDescriptors(addedLights.ToArray(), multiplyPunctualLightIntensityByPI);
                LightHandle[] addedHandles = world.AddLights(lightDescriptors);
                for (int i = 0; i < addedLights.Count; ++i)
                    entityIDToHandle.Add(addedLights[i].GetEntityId(), addedHandles[i]);

                // Update changed lights
                LightHandle[] handlesToUpdate = new LightHandle[changedLights.Count];
                for (int i = 0; i < changedLights.Count; i++)
                    handlesToUpdate[i] = entityIDToHandle[changedLights[i].GetEntityId()];

                world.UpdateLights(handlesToUpdate, ConvertUnityLightsToLightDescriptors(changedLights.ToArray(), multiplyPunctualLightIntensityByPI));
            }

            internal void UpdateInstances(
                SurfaceCacheWorld world,
                List<MeshRenderer> addedInstances,
                List<InstanceChanges> changedInstances,
                List<EntityId> removedInstances,
                Material fallbackMaterial)
            {
                UpdateInstances(world, _entityIDToWorldInstanceHandles, _entityIDToWorldMaterialHandles, addedInstances, changedInstances, removedInstances, fallbackMaterial);
            }

            private static void UpdateInstances(
                SurfaceCacheWorld world,
                Dictionary<EntityId, InstanceHandle> entityIDToInstanceHandle,
                Dictionary<EntityId, MaterialHandle> entityIDToMaterialHandle,
                List<MeshRenderer> addedInstances,
                List<InstanceChanges> changedInstances,
                List<EntityId> removedInstances,
                Material fallbackMaterial)
            {
                foreach (var meshRendererEntityID in removedInstances)
                {
                    if (entityIDToInstanceHandle.TryGetValue(meshRendererEntityID, out var instanceHandle))
                    {
                        world.RemoveInstance(instanceHandle);
                        entityIDToInstanceHandle.Remove(meshRendererEntityID);
                    }
                }

                foreach (var meshRenderer in addedInstances)
                {
                    Debug.Assert(!meshRenderer.isPartOfStaticBatch);

                    var mesh = meshRenderer.GetComponent<MeshFilter>().sharedMesh;

                    if (mesh == null || mesh.vertexCount == 0)
                        continue;

                    var localToWorldMatrix = meshRenderer.transform.localToWorldMatrix;

                    var materials = Util.GetMaterials(meshRenderer);
                    var materialHandles = new MaterialHandle[materials.Length];
                    for (int i = 0; i < materials.Length; i++)
                    {
                        var matEntityId = materials[i] == null ? fallbackMaterial.GetEntityId() : materials[i].GetEntityId();
                        materialHandles[i] = entityIDToMaterialHandle[matEntityId];
                    }
                    uint[] masks = new uint[materials.Length];
                    for (int i = 0; i < masks.Length; i++)
                    {
                        masks[i] = materials[i] != null ? 1u : 0u;
                    }

                    InstanceHandle instance = world.AddInstance(mesh, materialHandles, masks, in localToWorldMatrix);
                    var entityID = meshRenderer.GetEntityId();
                    Debug.Assert(!entityIDToInstanceHandle.ContainsKey(entityID));
                    entityIDToInstanceHandle.Add(entityID, instance);
                }

                foreach (var instanceUpdate in changedInstances)
                {
                    var meshRenderer = instanceUpdate.meshRenderer;
                    var gameObject = meshRenderer.gameObject;

                    Debug.Assert(entityIDToInstanceHandle.ContainsKey(meshRenderer.GetEntityId()));
                    var instanceHandle = entityIDToInstanceHandle[meshRenderer.GetEntityId()];

                    if ((instanceUpdate.changes & ModifiedProperties.Transform) != 0)
                    {
                        world.UpdateInstanceTransform(instanceHandle, gameObject.transform.localToWorldMatrix);
                    }

                    if ((instanceUpdate.changes & ModifiedProperties.Material) != 0)
                    {
                        var materials = Util.GetMaterials(meshRenderer);
                        var materialHandles = new MaterialHandle[materials.Length];
                        for (int i = 0; i < materials.Length; i++)
                        {
                            var matEntityId = materials[i] == null ? fallbackMaterial.GetEntityId() : materials[i].GetEntityId();
                            materialHandles[i] = entityIDToMaterialHandle[matEntityId];
                        }

                        world.UpdateInstanceMaterials(instanceHandle, materialHandles);

                        uint[] masks = new uint[materials.Length];
                        for (int i = 0; i < masks.Length; i++)
                        {
                            masks[i] = materials[i] != null ? 1u : 0u;
                        }

                        world.UpdateInstanceMask(instanceHandle, masks);
                    }
                }
            }

            public void Dispose()
            {
                CoreUtils.Destroy(_fallbackMaterialDescriptor.Albedo);
                CoreUtils.Destroy(_fallbackMaterialDescriptor.Emission);
                CoreUtils.Destroy(_fallbackMaterialDescriptor.Transmission);
            }

            internal static SurfaceCacheWorld.LightDescriptor[] ConvertUnityLightsToLightDescriptors(Light[] lights, bool multiplyPunctualLightIntensityByPI)
            {
                var descriptors = new SurfaceCacheWorld.LightDescriptor[lights.Length];
                for (int i = 0; i < lights.Length; i++)
                {
                    Light light = lights[i];
                    ref SurfaceCacheWorld.LightDescriptor descriptor = ref descriptors[i];
                    descriptor.Type = light.type;
                    descriptor.LinearLightColor = Util.GetLinearLightColor(light) * light.bounceIntensity;
                    if (multiplyPunctualLightIntensityByPI && Util.IsPunctualLightType(light.type))
                        descriptor.LinearLightColor *= Mathf.PI;
                    descriptor.Transform = light.transform.localToWorldMatrix;
                    descriptor.ColorTemperature = light.colorTemperature;
                    descriptor.SpotAngle = light.spotAngle;
                    descriptor.InnerSpotAngle = light.innerSpotAngle;
                    descriptor.Range = light.range;
                }
                return descriptors;
            }
        }

        private SurfaceCachePass _pass;
        private RayTracingContext _rtContext;
        [SerializeField] private ParameterSet _parameterSet = new ParameterSet();

        [Serializable]
        class UniformEstimationParameterSet
        {
            public uint SampleCount = 2;
        }

        [Serializable]
        class RestirEstimationParameterSet
        {
            public uint ConfidenceCap = 30;
            public uint SpatialSampleCount = 4;
            public float SpatialFilterSize = 2.0f;
            public uint ValidationFrameInterval = 4;
        }

        [Serializable]
        class RisEstimationParameterSet
        {
            public uint CandidateCount = 8;
            public float TargetFunctionUpdateWeight = 0.8f;
        }

        [Serializable]
        class PatchFilteringParameterSet
        {
            public float TemporalSmoothing = 0.8f;
            public bool SpatialFilterEnabled = true;
            public uint SpatialFilterSampleCount = 4;
            public float SpatialFilterRadius = 1.0f;
            public bool TemporalPostFilterEnabled = true;
        }

        [Serializable]
        class ScreenFilteringParameterSet
        {
            public uint LookupSampleCount = 8;
            public float UpsamplingKernelSize = 5.0f;
            public uint UpsamplingSampleCount = 3;
        }

        [Serializable]
        class VolumeParameterSet
        {
            public uint Resolution = 32;
            public float Size = 128.0f;
            public uint CascadeCount = 4;
            public bool Movement = true;
        }

        [Serializable]
        class AdvancedParameterSet
        {
            public uint DefragCount = 2;
        }

        [Serializable]
        class ParameterSet
        {
            public SurfaceCacheEstimationMethod EstimationMethod = SurfaceCacheEstimationMethod.Uniform;
            public bool MultiBounce = true;

            [SerializeField] public UniformEstimationParameterSet UniformEstimationParams = new UniformEstimationParameterSet();
            [SerializeField] public RestirEstimationParameterSet RestirEstimationParams = new RestirEstimationParameterSet();
            [SerializeField] public RisEstimationParameterSet RisEstimationParams = new RisEstimationParameterSet();
            public PatchFilteringParameterSet PatchFilteringParams = new PatchFilteringParameterSet();
            [SerializeField] public ScreenFilteringParameterSet ScreenFilteringParams = new ScreenFilteringParameterSet();
            [SerializeField] public VolumeParameterSet VolumeParams = new VolumeParameterSet();
            [SerializeField] public AdvancedParameterSet AdvancedParams = new AdvancedParameterSet();

            public bool DebugEnabled = false;
            public DebugViewMode_ DebugViewMode = DebugViewMode_.CellIndex;
            public bool DebugShowSamplePosition = false;
        }

        void ClearResources()
        {
            _pass?.Dispose();
            _pass = null;
            _rtContext?.Dispose();
            _rtContext = null;
        }

        public override void Create()
        {
            ClearResources();

            if (isActive)
            {
                var rtBackend = RayTracingBackend.Compute;

                {
                    var resources = new RayTracingResources();
#if UNITY_EDITOR
                    resources.Load();
#else
                    resources.LoadFromRenderPipelineResources();
#endif
                    _rtContext = new RayTracingContext(rtBackend, resources);
                }

                var universalRenderPipelineResources = GraphicsSettings.GetRenderPipelineSettings<SurfaceCacheRenderPipelineResourceSet>();
                Debug.Assert(universalRenderPipelineResources != null);

                var worldResources = new WorldResourceSet();
                var worldLoadResult = worldResources.LoadFromRenderPipelineResources();
                Debug.Assert(worldLoadResult);

                var coreResources = new Rendering.SurfaceCacheResourceSet((uint)SystemInfo.computeSubGroupSize);
                var coreResourceLoadResult = coreResources.LoadFromRenderPipelineResources(_rtContext);
                Debug.Assert(coreResourceLoadResult);

                var volParams = new SurfaceCacheVolumeParameterSet
                {
                    Resolution = _parameterSet.VolumeParams.Resolution,
                    Size = _parameterSet.VolumeParams.Size,
                    CascadeCount = _parameterSet.VolumeParams.CascadeCount
                };

                var estimationParams = new SurfaceCacheEstimationParameterSet
                {
                    Method = _parameterSet.EstimationMethod,
                    MultiBounce = _parameterSet.MultiBounce,
                    RestirEstimationConfidenceCap = _parameterSet.RestirEstimationParams.ConfidenceCap,
                    RestirEstimationSpatialSampleCount = _parameterSet.RestirEstimationParams.SpatialSampleCount,
                    RestirEstimationSpatialFilterSize = _parameterSet.RestirEstimationParams.SpatialFilterSize,
                    RestirEstimationValidationFrameInterval = _parameterSet.RestirEstimationParams.ValidationFrameInterval,
                    UniformEstimationSampleCount = _parameterSet.UniformEstimationParams.SampleCount,
                    RisEstimationCandidateCount = _parameterSet.RisEstimationParams.CandidateCount,
                    RisEstimationTargetFunctionUpdateWeight = _parameterSet.RisEstimationParams.TargetFunctionUpdateWeight
                };

                var patchFilteringParams = new SurfaceCachePatchFilteringParameterSet
                {
                    TemporalSmoothing = _parameterSet.PatchFilteringParams.TemporalSmoothing,
                    SpatialFilterEnabled = _parameterSet.PatchFilteringParams.SpatialFilterEnabled,
                    SpatialFilterSampleCount = _parameterSet.PatchFilteringParams.SpatialFilterSampleCount,
                    SpatialFilterRadius = _parameterSet.PatchFilteringParams.SpatialFilterRadius,
                    TemporalPostFilterEnabled = _parameterSet.PatchFilteringParams.TemporalPostFilterEnabled
                };

                _pass = new SurfaceCachePass(
                    _rtContext,
                    coreResources,
                    worldResources,
                    universalRenderPipelineResources.allocationShader,
                    universalRenderPipelineResources.screenResolveLookupShader,
                    universalRenderPipelineResources.screenResolveUpsamplingShader,
                    universalRenderPipelineResources.debugShader,
                    universalRenderPipelineResources.flatNormalResolutionShader,
                    universalRenderPipelineResources.fallbackMaterial,
                    _parameterSet.DebugEnabled,
                    _parameterSet.DebugViewMode,
                    _parameterSet.DebugShowSamplePosition,
                    _parameterSet.ScreenFilteringParams.LookupSampleCount,
                    _parameterSet.ScreenFilteringParams.UpsamplingKernelSize,
                    _parameterSet.ScreenFilteringParams.UpsamplingSampleCount,
                    _parameterSet.AdvancedParams.DefragCount,
                    volParams,
                    estimationParams,
                    patchFilteringParams,
                    _parameterSet.VolumeParams.Movement);

                _pass.renderPassEvent = RenderPassEvent.AfterRenderingPrePasses + 1;
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            ScriptableRenderPassInput inputs = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
            if (UseMotionVectorPatchSeeding(renderingData.cameraData.cameraType))
            {
                inputs |= ScriptableRenderPassInput.Motion;
            }
            _pass.ConfigureInput(inputs);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            ClearResources();
            base.Dispose(disposing);
        }
    }
}

#endif
