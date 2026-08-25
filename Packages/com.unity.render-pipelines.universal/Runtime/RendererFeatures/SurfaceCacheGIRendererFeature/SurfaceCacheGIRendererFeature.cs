#if SURFACE_CACHE_SUPPORTED || UNITY_EDITOR

using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.RenderPipelines.Core.Runtime.Shared;
using UnityEngine.Experimental.Rendering;
using UnityEngine.PathTracing.Core;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.UnifiedRayTracing;

namespace UnityEngine.Rendering.Universal
{
    internal struct SurfaceCacheScreenFilteringParameterSet
    {
        public uint LookupSampleCount;
        public uint DenoisingPassCount;
    }

    [Serializable]
    [SupportedOnRenderPipeline]
    [Categorization.CategoryInfo(Name = "R: Surface Cache URP Integration", Order = 1000), HideInInspector]
    sealed class SurfaceCacheRenderPipelineResourceSet : IRenderPipelineResources
    {
        [SerializeField, HideInInspector]
        int m_Version = 7;

        int IRenderPipelineGraphicsSettings.version => m_Version;

        [ResourcePath("Runtime/RendererFeatures/SurfaceCacheGIRendererFeature/FallbackMaterial.mat")]
        public Material m_FallbackMaterial;

        [ResourcePath("Runtime/RendererFeatures/SurfaceCacheGIRendererFeature/PatchAllocation.compute")]
        public ComputeShader m_AllocationShader;

        [ResourcePath("Runtime/RendererFeatures/SurfaceCacheGIRendererFeature/ScreenResolveLookup.compute")]
        public ComputeShader m_ScreenResolveLookupShader;

        [ResourcePath("Runtime/RendererFeatures/SurfaceCacheGIRendererFeature/ScreenResolveUpsampling.compute")]
        public ComputeShader m_ScreenResolveUpsamplingShader;

        [ResourcePath("Runtime/RendererFeatures/SurfaceCacheGIRendererFeature/Debug.compute")]
        public ComputeShader m_DebugShader;

        [ResourcePath("Runtime/RendererFeatures/SurfaceCacheGIRendererFeature/FlatNormalResolution.compute")]
        public ComputeShader m_FlatNormalResolutionShader;

        [ResourcePath("Runtime/RendererFeatures/SurfaceCacheGIRendererFeature/ScreenResolveDenoising.compute")]
        public ComputeShader m_ScreenResolveDenoisingShader;

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

        public ComputeShader screenResolveDenoisingShader
        {
            get => m_ScreenResolveDenoisingShader;
            set => this.SetValueAndNotify(ref m_ScreenResolveDenoisingShader, value, nameof(m_ScreenResolveDenoisingShader));
        }
    }

    /// <summary>
    /// The renderer feature for Surface Cache Global Illumination.
    /// When enabled, Surface Cache Global Illumination replaces baked global illumination, and the two are not combined.
    /// Use the <see cref="SurfaceCacheGIVolumeOverride"/> volume component to control the effect.
    /// </summary>
    [DisallowMultipleRendererFeature("Surface Cache Global Illumination")]
    public class SurfaceCacheGIRendererFeature : ScriptableRendererFeature
    {
        internal enum DebugViewMode_
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

        internal static readonly string k_StaticBatchingErrorMesssage = "Surface Cache GI cannot run because it is incompatible with Static Batching. You can disable the option in the Player Settings.";

        private const int k_UpscaleFactor = 4;

        private const GraphicsFormat k_IrradianceL0Format = GraphicsFormat.R16G16B16A16_SFloat;
        private const GraphicsFormat k_IrradianceL1Format = GraphicsFormat.R8G8B8A8_UNorm;

        // URP currently cannot render motion vectors properly in Scene View, so we disable it.
        // https://jira.unity3d.com/browse/SRP-743
        // When this is fixed, we probably want to enable this always.
        static bool UseMotionVectorPatchSeeding(CameraType camType)
        {
            return camType == CameraType.Game;
        }

        static bool TryGetSupportedRayTracingBackend(out RayTracingBackend backend)
        {
            if (RayTracingContext.IsBackendSupported(RayTracingBackend.Hardware))
            {
                backend = RayTracingBackend.Hardware;
                return true;
            }

            if (RayTracingContext.IsBackendSupported(RayTracingBackend.Compute))
            {
                backend = RayTracingBackend.Compute;
                return true;
            }

            backend = default;
            return false;
        }

        internal static bool HasSupportedRayTracingBackend()
        {
            return TryGetSupportedRayTracingBackend(out _);
        }

        static bool IsEligibleCamera(Camera camera)
        {
            if (camera.cullingMask == 0)
                return false;

            if (camera.cameraType == CameraType.Game || camera.cameraType == CameraType.SceneView)
                return true;

            // Only allow in preview cameras via the RenderRequest API (i.e. 'Cameras' overlay in scene view)
            // Don't allow regular preview cameras (i.e. thumbnails, material previews)
            if (camera.cameraType == CameraType.Preview && camera.isProcessingRenderRequest)
                return true;

            return false;
        }

        static Vector3 ResolveFocusPos(Camera camera)
        {
            GameObject focusTarget = VolumeManager.instance.stack?.GetSceneObjectReference<SurfaceCacheGIVolumeOverride>();
            return focusTarget != null ? focusTarget.transform.position : camera.transform.position;
        }

        private static VolumeParameterSet GetVolumeParameters(SurfaceCacheGIVolumeOverride volumeOverride)
        {
            Debug.Assert(volumeOverride != null);

            return new VolumeParameterSet
            {
                EstimationParams = new SurfaceCacheEstimationParameterSet
                {
                    MultiBounce = volumeOverride.lightTransportMultiBounce.value,
                    BouncePatchAllocation = volumeOverride.lightTransportBouncePatchAllocation.value,
                    SampleCount = (uint)volumeOverride.lightTransportSampleCount.value,
                    DistanceFallback = volumeOverride.volumeDistanceFallback.value
                },
                PatchFilteringParams = new SurfaceCachePatchFilteringParameterSet
                {
                    TemporalSmoothing = volumeOverride.patchFilteringTemporalSmoothing.value,
                    SpatialFilterEnabled = volumeOverride.patchFilteringSpatialFilterEnabled.value,
                    SpatialFilterSampleCount = (uint)volumeOverride.patchFilteringSpatialSampleCount.value,
                    SpatialFilterRadius = volumeOverride.patchFilteringSpatialRadius.value,
                    TemporalPostFilterEnabled = volumeOverride.patchFilteringPostTemporalEnabled.value
                },
                ScreenFilteringParams = new SurfaceCacheScreenFilteringParameterSet
                {
                    LookupSampleCount = (uint)volumeOverride.screenFilteringLookupSampleCount.value,
                    DenoisingPassCount = (uint)volumeOverride.screenFilteringDenoisingPassCount.value,
                },
                Intensity = volumeOverride.intensity.value,
                VolumeSize = volumeOverride.volumeSize.value,
                VolumeResolution = (uint)volumeOverride.volumeResolution.value,
                VolumeCascadeCount = (uint)volumeOverride.volumeCascadeCount.value,
                DefragCount = (uint)volumeOverride.advancedDefragCount.value,
                RenderingLayerMask = volumeOverride.advancedRenderingLayerMask.value,
                PatchWarpingEnabled = volumeOverride.advancedPatchWarpingEnabled.value
            };
        }

        private static uint3 DivUp(uint3 x, uint3 y) => (x + y - 1) / y;

        private static uint2 DivUp(uint2 x, uint2 y) => (x + y - 1) / y;

        internal static class ShaderIDs
        {
            public static readonly int _RingConfigBuffer = Shader.PropertyToID("_RingConfigBuffer");
            public static readonly int _PatchIrradiances = Shader.PropertyToID("_PatchIrradiances");
            public static readonly int _PatchStatistics = Shader.PropertyToID("_PatchStatistics");
            public static readonly int _PatchGeometries = Shader.PropertyToID("_PatchGeometries");
            public static readonly int _PatchIrradiances0 = Shader.PropertyToID("_PatchIrradiances0");
            public static readonly int _PatchIrradiances1 = Shader.PropertyToID("_PatchIrradiances1");
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
            public static readonly int _ExposureMultiplier = Shader.PropertyToID("_ExposureMultiplier");
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
            public static readonly int _PassIndex = Shader.PropertyToID("_PassIndex");
            public static readonly int _Intensity = Shader.PropertyToID("_Intensity");
            public static readonly int _ViewMode = Shader.PropertyToID("_ViewMode");
            public static readonly int _ShowSamplePosition = Shader.PropertyToID("_ShowSamplePosition");
            public static readonly int _SampleCount = Shader.PropertyToID("_SampleCount");
            public static readonly int _ClipToWorldTransform = Shader.PropertyToID("_ClipToWorldTransform");
            public static readonly int _CameraWorldPos = Shader.PropertyToID("_CameraWorldPos");
            public static readonly int _CurrentClipToWorldTransform = Shader.PropertyToID("_CurrentClipToWorldTransform");
            public static readonly int _PreviousClipToWorldTransform = Shader.PropertyToID("_PreviousClipToWorldTransform");
            public static readonly int _FrameIdx = Shader.PropertyToID("_FrameIdx");
            public static readonly int _VolumeSpatialResolution = Shader.PropertyToID("_VolumeSpatialResolution");
            public static readonly int _RingConfigOffset = Shader.PropertyToID("_RingConfigOffset");
            public static readonly int _VolumeTargetPos = Shader.PropertyToID("_VolumeTargetPos");
            public static readonly int _FullResPixelOffset = Shader.PropertyToID("_FullResPixelOffset");
            public static readonly int _LowResScreenSize = Shader.PropertyToID("_LowResScreenSize");
            public static readonly int _VolumeCascadeCount = Shader.PropertyToID("_VolumeCascadeCount");
            public static readonly int _VolumeVoxelMinSize = Shader.PropertyToID("_VolumeVoxelMinSize");
            public static readonly int _PatchWarping = Shader.PropertyToID("_PatchWarping");
            public static readonly int _VolumeCascadeOffsets = Shader.PropertyToID("_VolumeCascadeOffsets");
            public static readonly int _PatchCellIndices = Shader.PropertyToID("_PatchCellIndices");
            public static readonly int _GlobalProbe = Shader.PropertyToID("_GlobalProbe");
            public static readonly int _DistanceFallback = Shader.PropertyToID("_DistanceFallback");
            public static readonly int _InputL0 = Shader.PropertyToID("_InputL0");
            public static readonly int _InputL10 = Shader.PropertyToID("_InputL10");
            public static readonly int _InputL11 = Shader.PropertyToID("_InputL11");
            public static readonly int _InputL12 = Shader.PropertyToID("_InputL12");
            public static readonly int _LowResNdcDepths = Shader.PropertyToID("_LowResNdcDepths");
        }

        internal readonly struct CameraInfo
        {
            public readonly Matrix4x4 ClipToWorld;
            public readonly Vector3 WorldPos;
            public readonly int2 ScreenResolution;

            public CameraInfo(Matrix4x4 clipToWorld, Vector3 worldPos, int2 screenResolution)
            {
                ClipToWorld = clipToWorld;
                WorldPos = worldPos;
                ScreenResolution = screenResolution;
            }
        }

        internal static CameraInfo ComputeCameraInfo(UniversalCameraData cameraData)
        {
            var worldToView = cameraData.GetViewMatrix();
            var viewToClip = cameraData.GetGPUProjectionMatrix(true);
            var clipToWorld = (viewToClip * worldToView).inverse;
            return new CameraInfo(clipToWorld, cameraData.worldSpaceCameraPos, new int2(cameraData.pixelWidth, cameraData.pixelHeight));
        }

        class SurfaceCacheWorldPass : ScriptableRenderPass, IDisposable
        {
            private class WorldUpdatePassData
            {
                internal SurfaceCacheWorld World;
                internal uint EnvCubemapResolution;
                internal Light Sun;
                internal Matrix4x4 RestoreViewMatrix;
                internal Matrix4x4 RestoreProjectionMatrix;
            }

            private readonly SurfaceCacheWorld _world;
            private readonly SurfaceCacheWorldAdapter _worldAdapter;
            private readonly ObjectDispatcherChangeSource _objectChangeSource;
            private readonly EntityChangeSource _entityChangeSource;
            private GraphicsBuffer _worldUpdateScratch;
            private readonly uint _environmentCubemapResolution = 32;
            private readonly bool _conformToUnityGIFormat;

            internal SurfaceCacheWorld World => _world;
            internal uint RenderingLayerMask { get; set; }

            public SurfaceCacheWorldPass(
                RayTracingContext rtContext,
                WorldResourceSet worldResources,
                ComputeShader emissiveTriangleAdditionShader,
                ComputeShader emissiveTriangleRemovalShader,
                Material fallbackMaterial,
                bool conformToUnityGIFormat)
            {
                _conformToUnityGIFormat = conformToUnityGIFormat;
                _world = new SurfaceCacheWorld();
                _world.Init(rtContext, worldResources, emissiveTriangleAdditionShader, emissiveTriangleRemovalShader);
                _objectChangeSource = new ObjectDispatcherChangeSource();
                _entityChangeSource = new EntityChangeSource();
                _worldAdapter = new SurfaceCacheWorldAdapter(fallbackMaterial);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                // RenderSettings.ambientIntensity is used directly as a linear value for Surface Cache, which is not currently
                // the case for the standard ambient probe lighting, which is assumed to be in gamma space and then converted to
                // linear space. We will make this more coherent for the ambient probe in the future.
                // Similarly, the ambient colors are all defined in sRGB space and must be converted to linear.
                using (var changeSet = _objectChangeSource.CollectChanges())
                {
                    _entityChangeSource.CollectChanges(changeSet.WorldChangeSet);

                    _worldAdapter.Update(
                        changeSet.WorldChangeSet,
                        RenderSettings.ambientMode,
                        RenderSettings.skybox,
                        RenderSettings.ambientSkyColor.linear,
                        RenderSettings.ambientEquatorColor.linear,
                        RenderSettings.ambientGroundColor.linear,
                        RenderSettings.ambientIntensity,
                        _conformToUnityGIFormat,
                        RenderingLayerMask,
                        _world);

                    _entityChangeSource.EndCollectChanges();
                }

                using (var builder = renderGraph.AddUnsafePass("Surface Cache World Update", out WorldUpdatePassData passData))
                {
                    passData.World = _world;
                    passData.EnvCubemapResolution = _environmentCubemapResolution;
                    passData.Sun = RenderSettings.sun;
                    passData.RestoreProjectionMatrix = cameraData.GetProjectionMatrix();
                    passData.RestoreViewMatrix = cameraData.GetViewMatrix();

                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc((WorldUpdatePassData data, UnsafeGraphContext graphCtx) => UpdateWorld(data, graphCtx, ref _worldUpdateScratch));
                }
            }

            static void UpdateWorld(WorldUpdatePassData data, UnsafeGraphContext graphCtx, ref GraphicsBuffer scratch)
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(graphCtx.cmd);
                data.World.Commit(cmd, ref scratch, data.EnvCubemapResolution, data.Sun, out bool viewAndProjectionMatricesChanged);
                if (viewAndProjectionMatricesChanged)
                {
                    cmd.SetViewProjectionMatrices(data.RestoreViewMatrix, data.RestoreProjectionMatrix);
                }
            }

            public void Dispose()
            {
                _entityChangeSource.Dispose();
                _worldAdapter.CleanUp(_world);
                _objectChangeSource.Dispose();
                _world.Dispose();
                _worldUpdateScratch?.Dispose();
            }
        }

        class PerCameraState : IDisposable
        {
            internal SurfaceCache Cache;
            internal RTHandle LowResIrradiancesL0;
            internal RTHandle LowResIrradiancesL10;
            internal RTHandle LowResIrradiancesL11;
            internal RTHandle LowResIrradiancesL12;
            internal RTHandle LowResNdcDepths;
            internal Matrix4x4 PrevClipToWorldTransform;
            internal uint FrameIndex;
            internal uint RenderingLayerMask;
            internal int LastUsedFrame;

            public PerCameraState(SurfaceCache cache)
            {
                Cache = cache;
                PrevClipToWorldTransform = Matrix4x4.identity;
            }

            internal void EnsureScreenResources(int2 screenResolution)
            {
                int lowResWidth = (screenResolution.x + k_UpscaleFactor - 1) / k_UpscaleFactor;
                int lowResHeight = (screenResolution.y + k_UpscaleFactor - 1) / k_UpscaleFactor;

                if (LowResIrradiancesL0 != null && LowResIrradiancesL0.GetScaledSize() == new Vector2Int(lowResWidth, lowResHeight))
                    return;

                LowResIrradiancesL0?.Release();
                LowResIrradiancesL0 = RTHandles.Alloc(lowResWidth, lowResHeight, 1, DepthBits.None, k_IrradianceL0Format, FilterMode.Point, TextureWrapMode.Clamp, TextureDimension.Tex2D, true, name: "_lowResScreenIrradiancesL0");
                LowResIrradiancesL10?.Release();
                LowResIrradiancesL10 = RTHandles.Alloc(lowResWidth, lowResHeight, 1, DepthBits.None, k_IrradianceL1Format, FilterMode.Point, TextureWrapMode.Clamp, TextureDimension.Tex2D, true, name: "_lowResScreenIrradiancesL10");
                LowResIrradiancesL11?.Release();
                LowResIrradiancesL11 = RTHandles.Alloc(lowResWidth, lowResHeight, 1, DepthBits.None, k_IrradianceL1Format, FilterMode.Point, TextureWrapMode.Clamp, TextureDimension.Tex2D, true, name: "_lowResScreenIrradiancesL11");
                LowResIrradiancesL12?.Release();
                LowResIrradiancesL12 = RTHandles.Alloc(lowResWidth, lowResHeight, 1, DepthBits.None, k_IrradianceL1Format, FilterMode.Point, TextureWrapMode.Clamp, TextureDimension.Tex2D, true, name: "_lowResScreenIrradiancesL12");
                LowResNdcDepths?.Release();
                LowResNdcDepths = RTHandles.Alloc(lowResWidth, lowResHeight, 1, DepthBits.None, GraphicsFormat.R16_UNorm, FilterMode.Point, TextureWrapMode.Clamp, TextureDimension.Tex2D, true, name: "_lowResScreenNdcDepths");
            }

            public void Dispose()
            {
                LowResIrradiancesL0?.Release();
                LowResIrradiancesL10?.Release();
                LowResIrradiancesL11?.Release();
                LowResIrradiancesL12?.Release();
                LowResNdcDepths?.Release();
                Cache?.Dispose();
            }
        }

        class SurfaceCacheCameraPass : ScriptableRenderPass
        {
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
                internal GraphicsBuffer PatchStatistics;
                internal uint FrameIdx;
                internal uint VolumeSpatialResolution;
                internal uint VolumeCascadeCount;
                internal uint RingConfigOffset;
                internal bool UseMotionVectorSeeding;
                internal GraphicsBuffer CascadeOffsets;
                internal float VoxelMinSize;
                internal bool PatchWarpingEnabled;
                internal Matrix4x4 CurrentClipToWorldTransform;
                internal Matrix4x4 PreviousClipToWorldTransform;
                internal Vector3 VolumeTargetPos;
                internal uint2 FullResPixelOffset;
                internal uint2 LowResScreenSize;
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
                internal DebugViewMode_ ViewMode;
                internal uint FrameIndex;
                internal bool ShowSamplePosition;
                internal uint VolumeSpatialResolution;
                internal float VolumeVoxelMinSize;
                internal bool PatchWarpingEnabled;
                internal uint VolumeCascadeCount;
                internal GraphicsBuffer CascadeOffsets;
                internal uint RingConfigOffset;
                internal Matrix4x4 ClipToWorldTransform;
                internal Vector3 VolumeTargetPos;
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
                internal TextureHandle ExposureMultiplier;
                internal GraphicsBuffer CellPatchIndices;
                internal GraphicsBuffer PatchIrradiances;
                internal GraphicsBuffer PatchStatistics;
                internal GraphicsBuffer CascadeOffsets;
                internal GraphicsBuffer GlobalProbeBuffer;
                internal bool DistanceFallback;
                internal uint VolumeSpatialResolution;
                internal uint VolumeCascadeCount;
                internal uint SampleCount;
                internal float VolumeVoxelMinSize;
                internal bool PatchWarpingEnabled;
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
                internal TextureHandle LowResScreenNdcDepths;
                internal TextureHandle FullResIrradiances;
                internal float Intensity;
                internal Matrix4x4 ClipToWorldTransform;
                internal Vector3 CameraWorldPos;
            }

            private class ScreenResolveDenoisingPassData
            {
                internal ComputeShader Shader;
                internal int KernelIndex;
                internal uint3 ThreadGroupSize;
                internal uint3 ThreadCount;
                internal TextureHandle InputL0;
                internal TextureHandle InputL10;
                internal TextureHandle InputL11;
                internal TextureHandle InputL12;
                internal TextureHandle DenoisedL0;
                internal TextureHandle DenoisedL10;
                internal TextureHandle DenoisedL11;
                internal TextureHandle DenoisedL12;
                internal TextureHandle ScratchL0;
                internal TextureHandle ScratchL10;
                internal TextureHandle ScratchL11;
                internal TextureHandle ScratchL12;
                internal TextureHandle InputNdcDepths;
                internal TextureHandle ScreenFlatNormals;
                internal Matrix4x4 ClipToWorldTransform;
                internal Vector3 CameraWorldPos;
                internal int PassCount;
            }

            private SurfaceCacheWorld _world;

            private readonly ComputeShader _patchAllocationShader;
            private readonly ComputeShader _flatNormalResolutionShader;
            private readonly int _patchAllocationKernel;
            private readonly int _flatNormalResolutionKernel;
            private uint3 _patchAllocationKernelGroupSize;
            private uint3 _flatNormalResolutionKernelGroupSize;

            private readonly ComputeShader _screenResolveLookupShader;
            private readonly ComputeShader _screenResolveUpsamplingShader;
            private readonly ComputeShader _screenResolveDenoisingShader;
            private readonly ComputeShader _debugShader;
            private readonly int _screenResolveLookupKernel;
            private readonly int _screenResolveUpsamplingKernel;
            private readonly int _screenResolveDenoisingKernel;
            private readonly int _debugKernel;
            private uint3 _screenResolveLookupKernelGroupSize;
            private uint3 _screenResolveUpsamplingKernelGroupSize;
            private uint3 _screenResolveDenoisingKernelGroupSize;
            private uint3 _debugKernelGroupSize;

            private readonly bool _debugEnabled;
            private readonly DebugViewMode_ _debugViewMode;
            private readonly bool _debugShowSamplePosition;

            internal Vector3 FocusPos { get; set; }
            internal VolumeParameterSet VolumeParams { get; set; }
            internal PerCameraState CurrentPerCameraState { get; set; }
            internal void SetWorld(SurfaceCacheWorld world) => _world = world;

            public SurfaceCacheCameraPass(
                ComputeShader patchAllocationShader,
                ComputeShader flatNormalResolutionShader,
                ComputeShader screenResolveLookupShader,
                ComputeShader screenResolveUpsamplingShader,
                ComputeShader screenResolveDenoisingShader,
                ComputeShader debugShader,
                bool debugEnabled,
                DebugViewMode_ debugViewMode,
                bool debugShowSamplePosition)
            {
                _flatNormalResolutionShader = flatNormalResolutionShader;
                _patchAllocationShader = patchAllocationShader;

                _patchAllocationKernel = _patchAllocationShader.FindKernel("Allocate");
                _flatNormalResolutionKernel = _flatNormalResolutionShader.FindKernel("ResolveFlatNormals");

                _patchAllocationShader.GetKernelThreadGroupSizes(_patchAllocationKernel, out _patchAllocationKernelGroupSize.x, out _patchAllocationKernelGroupSize.y, out _patchAllocationKernelGroupSize.z);
                _flatNormalResolutionShader.GetKernelThreadGroupSizes(_flatNormalResolutionKernel, out _flatNormalResolutionKernelGroupSize.x, out _flatNormalResolutionKernelGroupSize.y, out _flatNormalResolutionKernelGroupSize.z);

                _screenResolveLookupShader = screenResolveLookupShader;
                _screenResolveUpsamplingShader = screenResolveUpsamplingShader;
                _screenResolveDenoisingShader = screenResolveDenoisingShader;
                _debugShader = debugShader;

                _screenResolveLookupKernel = _screenResolveLookupShader.FindKernel("Lookup");
                _screenResolveUpsamplingKernel = _screenResolveUpsamplingShader.FindKernel("Upsample");
                _screenResolveDenoisingKernel = _screenResolveDenoisingShader.FindKernel("Denoise");
                _debugKernel = _debugShader.FindKernel("Visualize");

                _screenResolveLookupShader.GetKernelThreadGroupSizes(_screenResolveLookupKernel, out _screenResolveLookupKernelGroupSize.x, out _screenResolveLookupKernelGroupSize.y, out _screenResolveLookupKernelGroupSize.z);
                _screenResolveUpsamplingShader.GetKernelThreadGroupSizes(_screenResolveUpsamplingKernel, out _screenResolveUpsamplingKernelGroupSize.x, out _screenResolveUpsamplingKernelGroupSize.y, out _screenResolveUpsamplingKernelGroupSize.z);
                _screenResolveDenoisingShader.GetKernelThreadGroupSizes(_screenResolveDenoisingKernel, out _screenResolveDenoisingKernelGroupSize.x, out _screenResolveDenoisingKernelGroupSize.y, out _screenResolveDenoisingKernelGroupSize.z);
                _debugShader.GetKernelThreadGroupSizes(_debugKernel, out _debugKernelGroupSize.x, out _debugKernelGroupSize.y, out _debugKernelGroupSize.z);

                _debugEnabled = debugEnabled;
                _debugViewMode = debugViewMode;
                _debugShowSamplePosition = debugShowSamplePosition;
            }

            internal TextureHandle RecordFlatNormalResolution(RenderGraph renderGraph, in CameraInfo cameraInfo, TextureHandle screenDepths)
            {
                var flatNormalsDesc = new TextureDesc(cameraInfo.ScreenResolution.x, cameraInfo.ScreenResolution.y)
                {
                    format = GraphicsFormat.R8G8B8A8_SNorm,
                    enableRandomWrite = true,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "_fullResScreenFlatNormals"
                };
                var flatNormalsHandle = renderGraph.CreateTexture(flatNormalsDesc);
                using (var builder = renderGraph.AddComputePass("Surface Cache Flat Normal Resolution", out FlatNormalResolutionPassData passData))
                {
                    passData.ThreadCount = new uint3((uint)cameraInfo.ScreenResolution.x, (uint)cameraInfo.ScreenResolution.y, 1);
                    passData.Shader = _flatNormalResolutionShader;
                    passData.KernelIndex = _flatNormalResolutionKernel;
                    passData.ThreadGroupSize = _flatNormalResolutionKernelGroupSize;
                    passData.ScreenDepths = screenDepths;
                    passData.ScreenFlatNormals = flatNormalsHandle;
                    passData.ClipToWorldTransform = cameraInfo.ClipToWorld;

                    builder.UseTexture(screenDepths, AccessFlags.Read);
                    builder.UseTexture(flatNormalsHandle, AccessFlags.Write);
                    builder.SetRenderFunc((FlatNormalResolutionPassData data, ComputeGraphContext cgContext) => ResolveFlatNormals(data, cgContext));
                }
                return flatNormalsHandle;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                Debug.Assert(resourceData.cameraDepthTexture.IsValid());
                Debug.Assert(resourceData.cameraNormalsTexture.IsValid());
                Debug.Assert(resourceData.motionVectorColor.IsValid());

                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                var cameraInfo = ComputeCameraInfo(cameraData);

                var cache = CurrentPerCameraState.Cache;
                cache.SetEstimationParams(VolumeParams.EstimationParams);
                cache.SetPatchFilteringParams(VolumeParams.PatchFilteringParams);
                cache.SetVolumeSize(VolumeParams.VolumeSize);
                cache.SetDefragCount(VolumeParams.DefragCount);
                cache.SetPatchWarpingEnabled(VolumeParams.PatchWarpingEnabled);

                CurrentPerCameraState.EnsureScreenResources(cameraInfo.ScreenResolution);

                var flatNormalsHandle = RecordFlatNormalResolution(renderGraph, cameraInfo, resourceData.cameraDepthTexture);

                cache.Volume.TargetPos = FocusPos;

                cache.RecordPreparation(renderGraph, CurrentPerCameraState.FrameIndex);

                var lowResScreenIrradiancesL0Handle = renderGraph.ImportTexture(CurrentPerCameraState.LowResIrradiancesL0);
                var lowResScreenIrradiancesL10Handle = renderGraph.ImportTexture(CurrentPerCameraState.LowResIrradiancesL10);
                var lowResScreenIrradiancesL11Handle = renderGraph.ImportTexture(CurrentPerCameraState.LowResIrradiancesL11);
                var lowResScreenIrradiancesL12Handle = renderGraph.ImportTexture(CurrentPerCameraState.LowResIrradiancesL12);
                var lowResScreenNdcDepthsHandle = renderGraph.ImportTexture(CurrentPerCameraState.LowResNdcDepths);
                var cellAllocationMarkHandle = renderGraph.ImportBuffer(cache.Volume.CellAllocationMarks);

                bool useMotionVectorPatchSeeding = UseMotionVectorPatchSeeding(cameraData.cameraType);

                using (var builder = renderGraph.AddComputePass("Surface Cache Patch Allocation", out PatchAllocationPassData passData))
                {
                    uint2 fullResScreenSize = new uint2((uint)cameraInfo.ScreenResolution.x, (uint)cameraInfo.ScreenResolution.y);
                    uint2 lowResScreenSize = DivUp(fullResScreenSize, new uint2(k_UpscaleFactor, k_UpscaleFactor));

                    passData.ThreadCount = new uint3(lowResScreenSize, 1);
                    passData.Shader = _patchAllocationShader;
                    passData.KernelIndex = _patchAllocationKernel;
                    passData.ThreadGroupSize = _patchAllocationKernelGroupSize;
                    passData.ScreenDepths = resourceData.cameraDepthTexture;
                    passData.ScreenFlatNormals = flatNormalsHandle;
                    passData.ScreenMotionVectors = resourceData.motionVectorColor;
                    passData.LowResScreenIrradiancesL0 = lowResScreenIrradiancesL0Handle;
                    passData.LowResScreenIrradiancesL10 = lowResScreenIrradiancesL10Handle;
                    passData.LowResScreenIrradiancesL11 = lowResScreenIrradiancesL11Handle;
                    passData.LowResScreenIrradiancesL12 = lowResScreenIrradiancesL12Handle;
                    passData.LowResScreenNdcDepths = lowResScreenNdcDepthsHandle;
                    passData.CellAllocationMarks = cache.Volume.CellAllocationMarks;
                    passData.CellPatchIndices = cache.Volume.CellPatchIndices;
                    passData.RingConfigBuffer = cache.RingConfig.Buffer;
                    passData.PatchIrradiances0 = cache.Patches.Irradiances[0];
                    passData.PatchIrradiances1 = cache.Patches.Irradiances[2];
                    passData.PatchGeometries = cache.Patches.Geometries;
                    passData.PatchCellIndices = cache.Patches.CellIndices;
                    passData.PatchStatistics = cache.Patches.Statistics;
                    passData.FrameIdx = CurrentPerCameraState.FrameIndex;
                    passData.VolumeSpatialResolution = cache.Volume.SpatialResolution;
                    passData.VolumeCascadeCount = cache.Volume.CascadeCount;
                    passData.RingConfigOffset = cache.RingConfig.OffsetA;

                    {
                        Debug.Assert(k_UpscaleFactor == 4);
                        uint cycleIndex = CurrentPerCameraState.FrameIndex % 4;
                        uint shuffledCycleIndex = cycleIndex * 7 % 16;
                        passData.FullResPixelOffset = new uint2(shuffledCycleIndex / 4, shuffledCycleIndex % 4);
                    }

                    passData.LowResScreenSize = lowResScreenSize;
                    passData.UseMotionVectorSeeding = useMotionVectorPatchSeeding;
                    passData.CascadeOffsets = cache.Volume.CascadeOffsetBuffer;
                    passData.VoxelMinSize = cache.Volume.VoxelMinSize;
                    passData.PatchWarpingEnabled = cache.Volume.PatchWarpingEnabled;
                    passData.CurrentClipToWorldTransform = cameraInfo.ClipToWorld;
                    passData.PreviousClipToWorldTransform = CurrentPerCameraState.PrevClipToWorldTransform;
                    passData.VolumeTargetPos = cache.Volume.TargetPos;

                    builder.UseBuffer(cellAllocationMarkHandle, AccessFlags.Write);
                    builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                    builder.UseTexture(flatNormalsHandle, AccessFlags.Read);
                    builder.UseTexture(resourceData.motionVectorColor, AccessFlags.Read);
                    builder.UseTexture(lowResScreenIrradiancesL0Handle, AccessFlags.Read);
                    builder.UseTexture(lowResScreenIrradiancesL10Handle, AccessFlags.Read);
                    builder.UseTexture(lowResScreenIrradiancesL11Handle, AccessFlags.Read);
                    builder.UseTexture(lowResScreenIrradiancesL12Handle, AccessFlags.Read);
                    builder.UseTexture(lowResScreenNdcDepthsHandle, AccessFlags.Read);

                    builder.SetRenderFunc((PatchAllocationPassData data, ComputeGraphContext cgContext) => AllocatePatches(data, cgContext));
                }

                uint outputIrradianceBufferIdx = cache.RecordUpdate(renderGraph, CurrentPerCameraState.FrameIndex, _world);

                var fullResIrradiancesDesc = new TextureDesc(cameraInfo.ScreenResolution.x, cameraInfo.ScreenResolution.y)
                {
                    format = GraphicsFormat.R16G16B16A16_SFloat,
                    enableRandomWrite = true,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "_fullResScreenIrradiances"
                };
                var fullResScreenIrradiancesHandle = renderGraph.CreateTexture(fullResIrradiancesDesc);

                using (var builder = renderGraph.AddComputePass("Surface Cache Screen Lookup", out ScreenIrradianceLookupPassData data))
                {
                    data.VolumeTargetPos = cache.Volume.TargetPos;
                    data.ClipToWorldTransform = cameraInfo.ClipToWorld;
                    data.ThreadCount = new uint3((uint)CurrentPerCameraState.LowResIrradiancesL0.rt.width, (uint)CurrentPerCameraState.LowResIrradiancesL0.rt.height, 1);
                    data.Shader = _screenResolveLookupShader;
                    data.KernelIndex = _screenResolveLookupKernel;
                    data.ThreadGroupSize = _screenResolveLookupKernelGroupSize;
                    data.ExposureMultiplier = resourceData.exposureMultiplier;
                    data.FullResDepths = resourceData.cameraDepthTexture;
                    data.FullResFlatNormals = flatNormalsHandle;
                    data.LowResScreenIrradiancesL0 = lowResScreenIrradiancesL0Handle;
                    data.LowResScreenIrradiancesL10 = lowResScreenIrradiancesL10Handle;
                    data.LowResScreenIrradiancesL11 = lowResScreenIrradiancesL11Handle;
                    data.LowResScreenIrradiancesL12 = lowResScreenIrradiancesL12Handle;
                    data.LowResScreenNdcDepths = lowResScreenNdcDepthsHandle;
                    data.CellPatchIndices = cache.Volume.CellPatchIndices;
                    data.PatchIrradiances = cache.Patches.Irradiances[outputIrradianceBufferIdx];
                    data.PatchStatistics = cache.Patches.Statistics;
                    data.CascadeOffsets = cache.Volume.CascadeOffsetBuffer;
                    data.GlobalProbeBuffer = cache.GlobalProbe;
                    data.DistanceFallback = VolumeParams.EstimationParams.DistanceFallback;
                    data.VolumeSpatialResolution = cache.Volume.SpatialResolution;
                    data.VolumeVoxelMinSize = cache.Volume.VoxelMinSize;
                    data.PatchWarpingEnabled = cache.Volume.PatchWarpingEnabled;
                    data.VolumeCascadeCount = cache.Volume.CascadeCount;
                    data.SampleCount = VolumeParams.ScreenFilteringParams.LookupSampleCount;

                    builder.UseBuffer(cellAllocationMarkHandle, AccessFlags.Read);
                    builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                    builder.UseTexture(flatNormalsHandle, AccessFlags.Read);
                    builder.UseTexture(data.LowResScreenIrradiancesL0, AccessFlags.Write);
                    builder.UseTexture(data.LowResScreenIrradiancesL10, AccessFlags.Write);
                    builder.UseTexture(data.LowResScreenIrradiancesL11, AccessFlags.Write);
                    builder.UseTexture(data.LowResScreenIrradiancesL12, AccessFlags.Write);
                    builder.UseTexture(data.LowResScreenNdcDepths, AccessFlags.Write);
                    if (data.ExposureMultiplier.IsValid())
                    {
                        builder.UseTexture(data.ExposureMultiplier, AccessFlags.Read);
                    }

                    builder.SetRenderFunc((ScreenIrradianceLookupPassData data, ComputeGraphContext cgContext) => LookupScreenIrradiance(data, cgContext));
                }

                var denoisedL0 = lowResScreenIrradiancesL0Handle;
                var denoisedL10 = lowResScreenIrradiancesL10Handle;
                var denoisedL11 = lowResScreenIrradiancesL11Handle;
                var denoisedL12 = lowResScreenIrradiancesL12Handle;

                int denoisingPassCount = (int)VolumeParams.ScreenFilteringParams.DenoisingPassCount;
                if (denoisingPassCount > 0)
                {
                    int lowResWidth = CurrentPerCameraState.LowResIrradiancesL0.rt.width;
                    int lowResHeight = CurrentPerCameraState.LowResIrradiancesL0.rt.height;

                    TextureHandle CreateDenoisingTargetTexture(GraphicsFormat format, string name)
                    {
                        return renderGraph.CreateTexture(new TextureDesc(lowResWidth, lowResHeight)
                        {
                            format = format,
                            enableRandomWrite = true,
                            filterMode = FilterMode.Point,
                            wrapMode = TextureWrapMode.Clamp,
                            name = name
                        });
                    }

                    denoisedL0 = CreateDenoisingTargetTexture(k_IrradianceL0Format, "_lowResScreenDenoisedIrradiancesL0");
                    denoisedL10 = CreateDenoisingTargetTexture(k_IrradianceL1Format, "_lowResScreenDenoisedIrradiancesL10");
                    denoisedL11 = CreateDenoisingTargetTexture(k_IrradianceL1Format, "_lowResScreenDenoisedIrradiancesL11");
                    denoisedL12 = CreateDenoisingTargetTexture(k_IrradianceL1Format, "_lowResScreenDenoisedIrradiancesL12");

                    var scratchL0 = TextureHandle.nullHandle;
                    var scratchL10 = TextureHandle.nullHandle;
                    var scratchL11 = TextureHandle.nullHandle;
                    var scratchL12 = TextureHandle.nullHandle;
                    if (denoisingPassCount > 1)
                    {
                        scratchL0 = CreateDenoisingTargetTexture(k_IrradianceL0Format, "_lowResScreenDenoisedIrradiancesScratchL0");
                        scratchL10 = CreateDenoisingTargetTexture(k_IrradianceL1Format, "_lowResScreenDenoisedIrradiancesScratchL10");
                        scratchL11 = CreateDenoisingTargetTexture(k_IrradianceL1Format, "_lowResScreenDenoisedIrradiancesScratchL11");
                        scratchL12 = CreateDenoisingTargetTexture(k_IrradianceL1Format, "_lowResScreenDenoisedIrradiancesScratchL12");
                    }

                    using (var builder = renderGraph.AddComputePass("Surface Cache Screen Denoising", out ScreenResolveDenoisingPassData denoisingData))
                    {
                        denoisingData.ClipToWorldTransform = cameraInfo.ClipToWorld;
                        denoisingData.CameraWorldPos = cameraInfo.WorldPos;
                        denoisingData.ThreadCount = new uint3((uint)lowResWidth, (uint)lowResHeight, 1);
                        denoisingData.Shader = _screenResolveDenoisingShader;
                        denoisingData.KernelIndex = _screenResolveDenoisingKernel;
                        denoisingData.ThreadGroupSize = _screenResolveDenoisingKernelGroupSize;
                        denoisingData.InputL0 = lowResScreenIrradiancesL0Handle;
                        denoisingData.InputL10 = lowResScreenIrradiancesL10Handle;
                        denoisingData.InputL11 = lowResScreenIrradiancesL11Handle;
                        denoisingData.InputL12 = lowResScreenIrradiancesL12Handle;
                        denoisingData.DenoisedL0 = denoisedL0;
                        denoisingData.DenoisedL10 = denoisedL10;
                        denoisingData.DenoisedL11 = denoisedL11;
                        denoisingData.DenoisedL12 = denoisedL12;
                        denoisingData.ScratchL0 = scratchL0;
                        denoisingData.ScratchL10 = scratchL10;
                        denoisingData.ScratchL11 = scratchL11;
                        denoisingData.ScratchL12 = scratchL12;
                        denoisingData.InputNdcDepths = lowResScreenNdcDepthsHandle;
                        denoisingData.ScreenFlatNormals = flatNormalsHandle;
                        denoisingData.PassCount = denoisingPassCount;

                        builder.UseTexture(lowResScreenIrradiancesL0Handle, AccessFlags.Read);
                        builder.UseTexture(lowResScreenIrradiancesL10Handle, AccessFlags.Read);
                        builder.UseTexture(lowResScreenIrradiancesL11Handle, AccessFlags.Read);
                        builder.UseTexture(lowResScreenIrradiancesL12Handle, AccessFlags.Read);
                        builder.UseTexture(lowResScreenNdcDepthsHandle, AccessFlags.Read);
                        builder.UseTexture(flatNormalsHandle, AccessFlags.Read);
                        builder.UseTexture(denoisedL0, AccessFlags.ReadWrite);
                        builder.UseTexture(denoisedL10, AccessFlags.ReadWrite);
                        builder.UseTexture(denoisedL11, AccessFlags.ReadWrite);
                        builder.UseTexture(denoisedL12, AccessFlags.ReadWrite);
                        if (denoisingPassCount > 1)
                        {
                            builder.UseTexture(scratchL0, AccessFlags.ReadWrite);
                            builder.UseTexture(scratchL10, AccessFlags.ReadWrite);
                            builder.UseTexture(scratchL11, AccessFlags.ReadWrite);
                            builder.UseTexture(scratchL12, AccessFlags.ReadWrite);
                        }

                        builder.SetRenderFunc((ScreenResolveDenoisingPassData data, ComputeGraphContext cgContext) => DenoiseScreenIrradiance(data, cgContext));
                    }
                }

                if (_debugEnabled)
                {
                    using (var builder = renderGraph.AddComputePass("Surface Cache Debug", out DebugPassData passData))
                    {
                        passData.ThreadCount = new uint3((uint)cameraInfo.ScreenResolution.x, (uint)cameraInfo.ScreenResolution.y, 1);
                        passData.Shader = _debugShader;
                        passData.KernelIndex = _debugKernel;
                        passData.ThreadGroupSize = _debugKernelGroupSize;
                        passData.ScreenDepths = resourceData.cameraDepthTexture;
                        passData.ScreenShadedNormals = resourceData.cameraNormalsTexture;
                        passData.ScreenFlatNormals = flatNormalsHandle;
                        passData.ScreenIrradiances = fullResScreenIrradiancesHandle;
                        passData.PatchCellIndices = cache.Patches.CellIndices;
                        passData.CellPatchIndices = cache.Volume.CellPatchIndices;
                        passData.RingConfigBuffer = cache.RingConfig.Buffer;
                        passData.RingConfigOffset = cache.RingConfig.OffsetA;
                        passData.VolumeTargetPos = cache.Volume.TargetPos;
                        passData.PatchIrradiances = cache.Patches.Irradiances[outputIrradianceBufferIdx];
                        passData.PatchGeometries = cache.Patches.Geometries;
                        passData.PatchStatistics = cache.Patches.Statistics;
                        passData.VolumeSpatialResolution = cache.Volume.SpatialResolution;
                        passData.VolumeVoxelMinSize = cache.Volume.VoxelMinSize;
                        passData.PatchWarpingEnabled = cache.Volume.PatchWarpingEnabled;
                        passData.VolumeCascadeCount = cache.Volume.CascadeCount;
                        passData.CascadeOffsets = cache.Volume.CascadeOffsetBuffer;
                        passData.ViewMode = _debugViewMode;
                        passData.FrameIndex = CurrentPerCameraState.FrameIndex;
                        passData.ShowSamplePosition = _debugShowSamplePosition;
                        passData.ClipToWorldTransform = cameraInfo.ClipToWorld;

                        builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                        builder.UseTexture(resourceData.cameraNormalsTexture, AccessFlags.Read);
                        builder.UseTexture(flatNormalsHandle, AccessFlags.Read);
                        builder.UseTexture(passData.ScreenIrradiances, AccessFlags.Write);

                        builder.SetRenderFunc((DebugPassData data, ComputeGraphContext cgContext) => RenderDebug(data, cgContext));
                    }
                }
                else
                {
                    using (var builder = renderGraph.AddComputePass("Surface Cache Screen Upsampling", out ScreenIrradianceUpsamplingPassData data))
                    {
                        data.ClipToWorldTransform = cameraInfo.ClipToWorld;
                        data.CameraWorldPos = cameraInfo.WorldPos;
                        data.FullResThreadCount = new uint3((uint)cameraInfo.ScreenResolution.x, (uint)cameraInfo.ScreenResolution.y, 1);
                        data.Shader = _screenResolveUpsamplingShader;
                        data.KernelIndex = _screenResolveUpsamplingKernel;
                        data.ThreadGroupSize = _screenResolveUpsamplingKernelGroupSize;
                        data.FullResDepths = resourceData.cameraDepthTexture;
                        data.FullResShadedNormals = resourceData.cameraNormalsTexture;
                        data.FullResFlatNormals = flatNormalsHandle;
                        data.LowResScreenIrradiancesL0 = denoisedL0;
                        data.LowResScreenIrradiancesL10 = denoisedL10;
                        data.LowResScreenIrradiancesL11 = denoisedL11;
                        data.LowResScreenIrradiancesL12 = denoisedL12;
                        data.LowResScreenNdcDepths = lowResScreenNdcDepthsHandle;
                        data.FullResIrradiances = fullResScreenIrradiancesHandle;
                        data.Intensity = VolumeParams.Intensity;

                        builder.UseTexture(data.FullResIrradiances, AccessFlags.Write);
                        builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                        builder.UseTexture(resourceData.cameraNormalsTexture, AccessFlags.Read);
                        builder.UseTexture(flatNormalsHandle, AccessFlags.Read);
                        builder.UseTexture(data.LowResScreenIrradiancesL0, AccessFlags.Read);
                        builder.UseTexture(data.LowResScreenIrradiancesL10, AccessFlags.Read);
                        builder.UseTexture(data.LowResScreenIrradiancesL11, AccessFlags.Read);
                        builder.UseTexture(data.LowResScreenIrradiancesL12, AccessFlags.Read);
                        builder.UseTexture(data.LowResScreenNdcDepths, AccessFlags.Read);
                        builder.UseBuffer(cellAllocationMarkHandle, AccessFlags.Read);

                        builder.SetRenderFunc((ScreenIrradianceUpsamplingPassData data, ComputeGraphContext cgContext) => UpsampleScreenIrradiance(data, cgContext));
                    }
                }

                resourceData.irradianceTexture = fullResScreenIrradiancesHandle;

                CurrentPerCameraState.PrevClipToWorldTransform = cameraInfo.ClipToWorld;
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
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchStatistics, data.PatchStatistics);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._VolumeCascadeOffsets, data.CascadeOffsets);
                cmd.SetComputeIntParam(shader, ShaderIDs._FrameIdx, (int)data.FrameIdx);
                cmd.SetComputeIntParam(shader, ShaderIDs._VolumeSpatialResolution, (int)data.VolumeSpatialResolution);
                cmd.SetComputeIntParam(shader, ShaderIDs._VolumeCascadeCount, (int)data.VolumeCascadeCount);
                cmd.SetComputeIntParam(shader, ShaderIDs._RingConfigOffset, (int)data.RingConfigOffset);
                cmd.SetComputeIntParams(shader, ShaderIDs._FullResPixelOffset, (int)data.FullResPixelOffset.x, (int)data.FullResPixelOffset.y);
                cmd.SetComputeIntParams(shader, ShaderIDs._LowResScreenSize, (int)data.LowResScreenSize.x, (int)data.LowResScreenSize.y);
                cmd.SetComputeIntParam(shader, ShaderIDs._UseMotionVectorSeeding, data.UseMotionVectorSeeding ? 1 : 0);
                cmd.SetComputeFloatParam(shader, ShaderIDs._VolumeVoxelMinSize, data.VoxelMinSize);
                cmd.SetComputeIntParam(shader, ShaderIDs._PatchWarping, data.PatchWarpingEnabled ? 1 : 0);
                cmd.SetComputeMatrixParam(shader, ShaderIDs._CurrentClipToWorldTransform, data.CurrentClipToWorldTransform);
                cmd.SetComputeMatrixParam(shader, ShaderIDs._PreviousClipToWorldTransform, data.PreviousClipToWorldTransform);
                cmd.SetComputeVectorParam(shader, ShaderIDs._VolumeTargetPos, data.VolumeTargetPos);

                uint3 groupCount = DivUp(data.ThreadCount, data.ThreadGroupSize);
                cmd.DispatchCompute(shader, kernelIndex, (int)groupCount.x, (int)groupCount.y, 1);
            }

            static void LookupScreenIrradiance(ScreenIrradianceLookupPassData data, ComputeGraphContext cgContext)
            {
                var cmd = cgContext.cmd;
                var shader = data.Shader;
                var kernelIndex = data.KernelIndex;
                if (data.ExposureMultiplier.IsValid())
                {
                    cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ExposureMultiplier, data.ExposureMultiplier);
                }
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ResultL0, data.LowResScreenIrradiancesL0);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ResultL10, data.LowResScreenIrradiancesL10);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ResultL11, data.LowResScreenIrradiancesL11);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ResultL12, data.LowResScreenIrradiancesL12);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ResultNdcDepths, data.LowResScreenNdcDepths);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ScreenDepths, data.FullResDepths);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ScreenFlatNormals, data.FullResFlatNormals);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._CellPatchIndices, data.CellPatchIndices);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchIrradiances, data.PatchIrradiances);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._VolumeCascadeOffsets, data.CascadeOffsets);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchStatistics, data.PatchStatistics);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._GlobalProbe, data.GlobalProbeBuffer);
                cmd.SetComputeIntParam(shader, ShaderIDs._DistanceFallback, data.DistanceFallback ? 1 : 0);
                cmd.SetComputeIntParam(shader, ShaderIDs._VolumeSpatialResolution, (int)data.VolumeSpatialResolution);
                cmd.SetComputeIntParam(shader, ShaderIDs._VolumeCascadeCount, (int)data.VolumeCascadeCount);
                cmd.SetComputeIntParam(shader, ShaderIDs._SampleCount, (int)data.SampleCount);
                cmd.SetComputeFloatParam(shader, ShaderIDs._VolumeVoxelMinSize, data.VolumeVoxelMinSize);
                cmd.SetComputeIntParam(shader, ShaderIDs._PatchWarping, data.PatchWarpingEnabled ? 1 : 0);
                cmd.SetComputeMatrixParam(shader, ShaderIDs._ClipToWorldTransform, data.ClipToWorldTransform);
                cmd.SetComputeVectorParam(shader, ShaderIDs._VolumeTargetPos, data.VolumeTargetPos);

                uint3 groupCount = DivUp(data.ThreadCount, data.ThreadGroupSize);
                cmd.DispatchCompute(shader, kernelIndex, (int)groupCount.x, (int)groupCount.y, 1);
            }

            static void DenoiseScreenIrradiance(ScreenResolveDenoisingPassData data, ComputeGraphContext cgContext)
            {
                var cmd = cgContext.cmd;
                var shader = data.Shader;
                var kernelIndex = data.KernelIndex;
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._LowResNdcDepths, data.InputNdcDepths);
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ScreenFlatNormals, data.ScreenFlatNormals);
                cmd.SetComputeMatrixParam(shader, ShaderIDs._ClipToWorldTransform, data.ClipToWorldTransform);
                cmd.SetComputeVectorParam(shader, ShaderIDs._CameraWorldPos, data.CameraWorldPos);

                uint3 groupCount = DivUp(data.ThreadCount, data.ThreadGroupSize);

                var inputL0 = data.InputL0;
                var inputL10 = data.InputL10;
                var inputL11 = data.InputL11;
                var inputL12 = data.InputL12;

                for (int passIndex = 0; passIndex < data.PassCount; passIndex++)
                {
                    bool writeToScratch = ((data.PassCount - 1 - passIndex) % 2) != 0;
                    var outputL0 = writeToScratch ? data.ScratchL0 : data.DenoisedL0;
                    var outputL10 = writeToScratch ? data.ScratchL10 : data.DenoisedL10;
                    var outputL11 = writeToScratch ? data.ScratchL11 : data.DenoisedL11;
                    var outputL12 = writeToScratch ? data.ScratchL12 : data.DenoisedL12;

                    cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._InputL0, inputL0);
                    cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._InputL10, inputL10);
                    cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._InputL11, inputL11);
                    cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._InputL12, inputL12);
                    cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ResultL0, outputL0);
                    cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ResultL10, outputL10);
                    cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ResultL11, outputL11);
                    cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ResultL12, outputL12);
                    cmd.SetComputeIntParam(shader, ShaderIDs._PassIndex, passIndex);

                    cmd.DispatchCompute(shader, kernelIndex, (int)groupCount.x, (int)groupCount.y, 1);

                    inputL0 = outputL0;
                    inputL10 = outputL10;
                    inputL11 = outputL11;
                    inputL12 = outputL12;
                }
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
                cmd.SetComputeTextureParam(shader, kernelIndex, ShaderIDs._ResultNdcDepths, passData.LowResScreenNdcDepths);
                cmd.SetComputeFloatParam(shader, ShaderIDs._Intensity, passData.Intensity);
                cmd.SetComputeMatrixParam(shader, ShaderIDs._ClipToWorldTransform, passData.ClipToWorldTransform);
                cmd.SetComputeVectorParam(shader, ShaderIDs._CameraWorldPos, passData.CameraWorldPos);

                uint3 groupCount = DivUp(passData.FullResThreadCount, passData.ThreadGroupSize);
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
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._VolumeCascadeOffsets, data.CascadeOffsets);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchCellIndices, data.PatchCellIndices);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchStatistics, data.PatchStatistics);

                cmd.SetComputeIntParam(shader, ShaderIDs._VolumeSpatialResolution, (int)data.VolumeSpatialResolution);
                cmd.SetComputeIntParam(shader, ShaderIDs._VolumeCascadeCount, (int)data.VolumeCascadeCount);
                cmd.SetComputeIntParam(shader, ShaderIDs._ViewMode, (int)data.ViewMode);
                cmd.SetComputeIntParam(shader, ShaderIDs._FrameIdx, (int)data.FrameIndex);
                cmd.SetComputeIntParam(shader, ShaderIDs._ShowSamplePosition, data.ShowSamplePosition ? 1 : 0);
                cmd.SetComputeFloatParam(shader, ShaderIDs._VolumeVoxelMinSize, data.VolumeVoxelMinSize);
                cmd.SetComputeIntParam(shader, ShaderIDs._PatchWarping, data.PatchWarpingEnabled ? 1 : 0);
                cmd.SetComputeFloatParam(shader, ShaderIDs._RingConfigOffset, data.RingConfigOffset);
                cmd.SetComputeVectorParam(shader, ShaderIDs._VolumeTargetPos, data.VolumeTargetPos);
                cmd.SetComputeMatrixParam(shader, ShaderIDs._ClipToWorldTransform, data.ClipToWorldTransform);

                uint3 groupCount = DivUp(data.ThreadCount, data.ThreadGroupSize);
                cmd.DispatchCompute(shader, kernelIndex, (int)groupCount.x, (int)groupCount.y, 1);
            }
        }

        private SurfaceCacheWorldPass _worldPass;
        private SurfaceCacheCameraPass _cameraPass;
        private RayTracingContext _rtContext;
        [SerializeField] private ParameterSet _parameterSet = new ParameterSet();

        private readonly Dictionary<Camera, PerCameraState> _cameraStates = new Dictionary<Camera, PerCameraState>();
        private readonly List<Camera> _cacheEvictionScratch = new List<Camera>();
        private Rendering.SurfaceCacheResourceSet _coreResources;

        internal int ActiveCacheCount => _cameraStates.Count;
        internal uint WorldRenderingLayerMask => _worldPass.RenderingLayerMask;

        // Historically, GI systems in Unity have, for historical reasons, 1) multiplied punctual light intensity
        // inputs with PI, and 2) divided all GI output with PI. To match this, Surface Cache for now does
        // something mathematically equivalent: It divides environment light and triangle emission by PI.
        // Ideally, we'd get rid of this behavior across all Unity GI systems in the future.
        // Note that this is distinct from the separate issue that URP is generally off by PI in its output.
        private const bool k_ConformToUnityGIFormat = true;
        private const float k_EmissiveTriangleIntensityMultiplier = k_ConformToUnityGIFormat ? 1.0f / Mathf.PI : 1.0f;

        private int _lastFrameCount = -1;

        // Main parameter set containing advanced and debug settings.
        [Serializable]
        class ParameterSet
        {
            // Debug settings (will be moved to rendering debugger in the future)
            public bool DebugEnabled = false;
            public DebugViewMode_ DebugViewMode = DebugViewMode_.CellIndex;
            public bool DebugShowSamplePosition = false;
        }

        void ClearResources()
        {
            _cameraPass = null;
            _worldPass?.Dispose();
            _worldPass = null;
            foreach (var perCameraState in _cameraStates.Values)
                perCameraState.Dispose();
            _cameraStates.Clear();
            _rtContext?.Dispose();
            _rtContext = null;
        }

        // Parameters extracted from Volume override each frame
        internal struct VolumeParameterSet
        {
            public SurfaceCacheEstimationParameterSet EstimationParams;
            public SurfaceCachePatchFilteringParameterSet PatchFilteringParams;
            public SurfaceCacheScreenFilteringParameterSet ScreenFilteringParams;
            public float Intensity;
            public float VolumeSize;
            public uint VolumeResolution;
            public uint VolumeCascadeCount;
            public uint DefragCount;
            public uint RenderingLayerMask;
            public bool PatchWarpingEnabled;
        }

        SurfaceCache CreateCache(in VolumeParameterSet vp)
        {
            Debug.Assert(vp.VolumeCascadeCount != 0 && vp.VolumeCascadeCount <= SurfaceCache.CascadeMax);

            var cache = new SurfaceCache(_coreResources, new SurfaceCacheVolumeParameterSet
            {
                Resolution = vp.VolumeResolution,
                Size = vp.VolumeSize,
                CascadeCount = vp.VolumeCascadeCount
            });
            cache.SetEmissiveTriangleIntensityMultiplier(k_EmissiveTriangleIntensityMultiplier);
            return cache;
        }

        void EvictStaleCameraStates()
        {
            const int maxUnusedFrames = 2;
            _cacheEvictionScratch.Clear();
            foreach (var kvp in _cameraStates)
            {
                if (kvp.Key == null || Time.frameCount - kvp.Value.LastUsedFrame >= maxUnusedFrames)
                    _cacheEvictionScratch.Add(kvp.Key);
            }
            foreach (var camera in _cacheEvictionScratch)
            {
                _cameraStates[camera].Dispose();
                _cameraStates.Remove(camera);
            }
        }

        bool ResourcesCreated()
        {
            return _cameraPass != null;
        }

        internal bool IsValidForDebugging()
        {
            return _worldPass != null && _cameraPass != null;
        }

        /// <inheritdoc/>
        public override void Create()
        {
            ClearResources();

            _lastFrameCount = -1;

            if (!isActive)
                return;

#if UNITY_EDITOR
            if (CheckStaticBatchingStatus())
                return;
#endif

            if (!TryGetSupportedRayTracingBackend(out var rtBackend))
                return;

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

            var coreRpResources = GraphicsSettings.GetRenderPipelineSettings<Rendering.SurfaceCacheRenderPipelineResourceSet>();
            Debug.Assert(coreRpResources != null);

            var coreResources = new Rendering.SurfaceCacheResourceSet((uint)SystemInfo.computeSubGroupSize);

            Object punctualLightSamplingUnifiedObj;
            Object estimationUnifiedObj;
            if (_rtContext.BackendType == RayTracingBackend.Compute)
            {
                punctualLightSamplingUnifiedObj = coreRpResources.punctualLightSamplingComputeShader;
                estimationUnifiedObj = coreRpResources.estimationComputeShader;
            }
            else
            {
                punctualLightSamplingUnifiedObj = coreRpResources.punctualLightSamplingRayTracingShader;
                estimationUnifiedObj = coreRpResources.estimationRayTracingShader;
            }
            IRayTracingShader punctualLightSamplingShader = _rtContext.CreateRayTracingShader(punctualLightSamplingUnifiedObj);
            IRayTracingShader estimationShader = _rtContext.CreateRayTracingShader(estimationUnifiedObj);

            coreResources.Load(
                coreRpResources.globalProbeShader,
                coreRpResources.scrollingShader,
                coreRpResources.evictionShader,
                coreRpResources.patchAllocationShader,
                coreRpResources.spatialFilteringShader,
                coreRpResources.temporalFilteringShader,
                coreRpResources.defragShader,
                punctualLightSamplingShader,
                estimationShader);

            _coreResources = coreResources;

            _worldPass = new SurfaceCacheWorldPass(
                _rtContext,
                worldResources,
                coreRpResources.emissiveTriangleAdditionComputeShader,
                coreRpResources.emissiveTriangleRemovalComputeShader,
                universalRenderPipelineResources.fallbackMaterial,
                k_ConformToUnityGIFormat);

            _cameraPass = new SurfaceCacheCameraPass(
                universalRenderPipelineResources.allocationShader,
                universalRenderPipelineResources.flatNormalResolutionShader,
                universalRenderPipelineResources.screenResolveLookupShader,
                universalRenderPipelineResources.screenResolveUpsamplingShader,
                universalRenderPipelineResources.screenResolveDenoisingShader,
                universalRenderPipelineResources.debugShader,
                _parameterSet.DebugEnabled,
                _parameterSet.DebugViewMode,
                _parameterSet.DebugShowSamplePosition);

            _cameraPass.SetWorld(_worldPass.World);

            // The world update runs first, then the surface cache pass.
            _worldPass.renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
            _cameraPass.renderPassEvent = RenderPassEvent.AfterRenderingPrePasses + 1;
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            if (CheckStaticBatchingStatus())
                return;
#endif

            var camera = renderingData.cameraData.camera;

            if (!IsEligibleCamera(camera))
                return;

            var surfaceCacheVolumeOverride = VolumeManager.instance.stack.GetComponent<SurfaceCacheGIVolumeOverride>();
            Debug.Assert(surfaceCacheVolumeOverride != null);

            if (!surfaceCacheVolumeOverride.enabled.value)
            {
                if (_cameraStates.TryGetValue(camera, out var disabledState))
                {
                    disabledState.Dispose();
                    _cameraStates.Remove(camera);
                }
                if (_cameraStates.Count == 0 && ResourcesCreated())
                    ClearResources();
                return;
            }

            if (!ResourcesCreated())
                Create();

            // Create() does not create the resources when the feature is inactive.
            if (!ResourcesCreated())
                return;

            var volumeParams = GetVolumeParameters(surfaceCacheVolumeOverride);

            bool newEntry = !_cameraStates.TryGetValue(camera, out var perCameraState);
            if (newEntry)
            {
                perCameraState = new PerCameraState(CreateCache(volumeParams));
                _cameraStates.Add(camera, perCameraState);
            }

            bool reconfigured = newEntry
                                || volumeParams.VolumeResolution != perCameraState.Cache.Volume.SpatialResolution
                                || volumeParams.VolumeCascadeCount != perCameraState.Cache.Volume.CascadeCount;
            if (reconfigured && !newEntry)
            {
                perCameraState.Cache.Dispose();
                perCameraState.Cache = CreateCache(volumeParams);
            }
            perCameraState.FrameIndex = reconfigured ? 0u : perCameraState.FrameIndex + 1u;
            perCameraState.LastUsedFrame = Time.frameCount;
            perCameraState.RenderingLayerMask = volumeParams.RenderingLayerMask;

            if (Time.frameCount != _lastFrameCount)
            {
                _lastFrameCount = Time.frameCount;
                EvictStaleCameraStates();

                uint combinedMask = 0;
                foreach (var kvp in _cameraStates)
                    combinedMask |= kvp.Value.RenderingLayerMask;

                _worldPass.RenderingLayerMask = combinedMask;
                renderer.EnqueuePass(_worldPass);
            }

            ScriptableRenderPassInput passInputs = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
            if (UseMotionVectorPatchSeeding(camera.cameraType))
                passInputs |= ScriptableRenderPassInput.Motion;

            _cameraPass.CurrentPerCameraState = perCameraState;
            _cameraPass.VolumeParams = volumeParams;
            _cameraPass.FocusPos = ResolveFocusPos(camera);
            _cameraPass.ConfigureInput(passInputs);
            renderer.EnqueuePass(_cameraPass);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            ClearResources();
            base.Dispose(disposing);
        }

#if UNITY_EDITOR
        bool _staticBatchingEnabled;
        bool CheckStaticBatchingStatus()
        {
            bool previousStatus = _staticBatchingEnabled;
            _staticBatchingEnabled = UnityEditor.PlayerSettings.GetStaticBatchingForPlatform(UnityEditor.EditorUserBuildSettings.activeBuildTarget);

            if (_staticBatchingEnabled && _staticBatchingEnabled != previousStatus)
                Debug.LogError(k_StaticBatchingErrorMesssage);

            return _staticBatchingEnabled;
        }
#endif
    }
}

#endif
