using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    // Ground Truth Ambient Occlusion (GTAO) Pass - Handles GTAO mode with compute shader and raster-fragment fallback.
    internal class GTAOPass : ScriptableRenderPass, IDisposable
    {
        // Shared state
        private readonly bool m_SupportsR8RenderTextureFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8);
        private readonly bool m_SupportsComputeShader = SystemInfo.supportsComputeShaders;
        private int m_BlueNoiseTextureIndex = 0;
        private Material m_Material;
        private Texture2D[] m_BlueNoiseTextures;
        private SSAOUtils.CameraViewData m_CameraViewData = SSAOUtils.CameraViewData.Create();
        private SSAOUtils.BlurTypes m_BlurType = SSAOUtils.BlurTypes.Bilateral;
        private ProfilingSampler m_ProfilingSampler = URPProfilingSamplers.SSAO;
        private ScreenSpaceAmbientOcclusionSettings m_CurrentSettings;
        private bool m_WarnedMissingTextures;
        private bool m_WarnedDepthPyramidUnavailable;

        // Raster path state
        private SSAOUtils.SSAOMaterialParams m_SSAOParamsPrev = new SSAOUtils.SSAOMaterialParams();

        // Compute path state
        private ComputePathState m_ComputePathState;
        private DepthPrefilterState m_DepthPrefilterState;

        // Constants
        private const float k_DegreesPerRotation              = 360.0f;
        private const int   k_TemporalOffsetCount             = 4;
        private const int   k_DepthPrefilterTileSize          = 16;
        private const float k_DepthFilterRadiusScale          = 0.75f;    // XeGTAO
        private const float k_DepthFilterFalloffRange         = 0.615f;   // XeGTAO
        private const float k_DepthMipSamplingOffset          = 3.3f;     // XeGTAO
        private const float k_MaxMipFootprintFractionOfRadius = 0.25f;    // keeps the pyramid from averaging over the kernel
        private const float k_HalfMaxDepth                    = 65504.0f;
        private const float k_DepthFilterMinFalloffRange      = 1.0e-6f;

        // GTAO-only Shader Property IDs
        internal static class ShaderIDs
        {
            // Compute kernel I/O bindings
            public static readonly int _CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
            public static readonly int _AOOutput = Shader.PropertyToID("_AOOutput");
            public static readonly int _BlurOutput = Shader.PropertyToID("_BlurOutput");
            public static readonly int _OcclusionTexture = Shader.PropertyToID("_OcclusionTexture");
            public static readonly int _AOOutputHistory = Shader.PropertyToID("_AOOutputHistory");
            public static readonly int _FinalOutput = Shader.PropertyToID("_FinalOutput");

            // Temporal filter
            public static readonly int _AOPackedHistory = Shader.PropertyToID("_AOPackedHistory");
            public static readonly int _MotionVectorTexture = Shader.PropertyToID("_MotionVectorTexture");
            public static readonly int _SSAOHistoryLength = Shader.PropertyToID("_SSAOHistoryLength");
            public static readonly int _SSAOGhostingMitigation = Shader.PropertyToID("_SSAOGhostingMitigation");
            public static readonly int _SSAOTemporalRotation = Shader.PropertyToID("_SSAOTemporalRotation");
            public static readonly int _SSAOTemporalOffset = Shader.PropertyToID("_SSAOTemporalOffset");

            // GTAO compute parameters
            public static readonly int _GTAODirectionCount = Shader.PropertyToID("_GTAODirectionCount");
            public static readonly int _GTAOStepCount = Shader.PropertyToID("_GTAOStepCount");
            public static readonly int _GTAOSpatialFilter = Shader.PropertyToID("_GTAOSpatialFilter");
            public static readonly int _GTAOBoxFilterStep = Shader.PropertyToID("_GTAOBoxFilterStep");

            // Depth Pyramid
            public static readonly int _GTAOPrefilterSize = Shader.PropertyToID("_GTAOPrefilterSize");
            public static readonly int _GTAOPrefilterSourceSize = Shader.PropertyToID("_GTAOPrefilterSourceSize");
            public static readonly int _GTAOPrefilterFalloff = Shader.PropertyToID("_GTAOPrefilterFalloff");
            public static readonly int _GTAOPrefilterOrthoRemap = Shader.PropertyToID("_GTAOPrefilterOrthoRemap");
            public static readonly int _GTAOPrefilterDownsample = Shader.PropertyToID("_GTAOPrefilterDownsample");
            public static readonly int _GTAOPrefilterMipCount = Shader.PropertyToID("_GTAOPrefilterMipCount");
        }

        // Temporal rotation/offset arrays for GTAO temporal filtering
        private static readonly float[] s_TemporalRotations = { 60.0f, 300.0f, 180.0f, 240.0f, 120.0f, 0.0f };

        // Structs
        private struct LocalKeywordSet
        {
            public LocalKeyword blueNoise;
            public LocalKeyword interleavedGradient;
            public LocalKeyword temporalFiltering;
            public LocalKeyword orthographic;

            public void Init(ComputeShader cs)
            {
                blueNoise = new(cs, ScreenSpaceAmbientOcclusionKeywords.k_AOBlueNoiseKeyword);
                interleavedGradient = new(cs, ScreenSpaceAmbientOcclusionKeywords.k_AOInterleavedGradientKeyword);
                temporalFiltering = new(cs, ScreenSpaceAmbientOcclusionKeywords.k_TemporalFilteringKeyword);
                orthographic = new(cs, ScreenSpaceAmbientOcclusionKeywords.k_OrthographicCameraKeyword);
            }
        }

        private struct ComputePathState
        {
            public ComputeShader shader;
            public int gtaoKernel;
            public int gtaoDepthPyramidKernel;
            public int blurHKernel;
            public int blurVKernel;
            public int boxSpatialFilterKernel;
            public int temporalKernel;
            public int copyHistoryKernel;
            public int finalBlitKernel;
            public LocalKeywordSet keywords;
            public RTHandle blueNoiseRTHandle;

            public void Init(ComputeShader cs)
            {
                Debug.Assert(SystemInfo.supportsComputeShaders, "ComputePathState must not be initialized when the platform does not support compute shaders.");

                shader = cs;

                if (shader == null)
                    return;

                Debug.Assert(
                    shader.HasKernel("ComputeAO") &&
                    shader.HasKernel("ComputeAODepthPyramid") &&
                    shader.HasKernel("BilateralBlurH") &&
                    shader.HasKernel("BilateralBlurV") &&
                    shader.HasKernel("BoxSpatialFilter") &&
                    shader.HasKernel("TemporalFilter") &&
                    shader.HasKernel("CopyHistory") &&
                    shader.HasKernel("FinalBlit"),
                    "The GTAO compute shader does not have the expected kernels.");

                gtaoKernel = shader.FindKernel("ComputeAO");
                gtaoDepthPyramidKernel = shader.FindKernel("ComputeAODepthPyramid");
                blurHKernel = shader.FindKernel("BilateralBlurH");
                blurVKernel = shader.FindKernel("BilateralBlurV");
                boxSpatialFilterKernel = shader.FindKernel("BoxSpatialFilter");
                temporalKernel = shader.FindKernel("TemporalFilter");
                copyHistoryKernel = shader.FindKernel("CopyHistory");
                finalBlitKernel = shader.FindKernel("FinalBlit");
                keywords.Init(shader);
            }

            public void Release()
            {
                blueNoiseRTHandle?.Release();
                blueNoiseRTHandle = null;
            }
        }

        private struct DepthPrefilterState
        {
            public ComputeShader shader;
            public int kernel;
            public LocalKeyword orthographic;
            public LocalKeyword disableTexture2DArray;

            public readonly bool IsValid => shader != null && kernel >= 0;

            public void Init(ComputeShader cs)
            {
                shader = null;
                kernel = -1;

                if (cs == null)
                    return;

                // A shader that failed to compile has no kernels. Stay invalid so the pass falls back instead of throwing.
                if (!cs.HasKernel("PrefilterDepths"))
                    return;

                int prefilterKernel = cs.FindKernel("PrefilterDepths");
                if (!cs.IsSupported(prefilterKernel))
                    return;

                shader = cs;
                kernel = prefilterKernel;

                shader.GetKernelThreadGroupSizes(kernel, out uint groupSizeX, out uint groupSizeY, out _);
                Debug.Assert(groupSizeX * 2 == k_DepthPrefilterTileSize && groupSizeY * 2 == k_DepthPrefilterTileSize, "The tile size must match the group size times the 2x2 block each thread owns.");

                orthographic = new(shader, ScreenSpaceAmbientOcclusionKeywords.k_OrthographicCameraKeyword);
                disableTexture2DArray = new(shader, ShaderKeywordStrings.DisableTexture2DXArray);
            }
        }

        private struct GTAOComputeParams
        {
            public Vector4 ssaoParams;
            public Vector4 ssaoParams2;
            public Vector4 depthToViewParams;
            public Vector4 sourceSize;
            public Vector4 blueNoiseParams;
            public float historyLength;
            public float ghostingMitigation;
            public Vector4 projectionParams2;
            public bool orthographicCamera;
            public float temporalRotation;
            public int temporalOffset;
        }

        private struct TemporalHistoryState
        {
            public TextureHandle historyReadTexture;
            public TextureHandle historyWriteTexture;
            public SSAOHistory ssaoHistory;
            public bool historyReady;
            public bool isNewFrame;
        }

        // Pass Data Classes
        private class GTAODepthPrefilterPassData
        {
            public ComputeShader cs;
            public int kernel;
            public LocalKeyword orthographicKeyword;
            public bool orthographicCamera;
            public LocalKeyword disableTexture2DArrayKeyword;
            public bool pyramidIsArray;
            public Vector2Int dispatchSize;
            public int sliceCount;
            public Vector4 prefilterSize;    // xy = MIP 0 viewport size, zw = max source depth coordinate
            public Vector4 prefilterSourceSize; // xy = source depth allocation size, zw = its reciprocal
            public Vector4 prefilterFalloff; // x = falloff multiplier, y = falloff addend, z = max storable depth
            public Vector4 orthoRemap;       // xy = orthographic eye depth remap, x + y * rawDepth
            public Vector4[] depthPyramidMipLevelOffsets = new Vector4[SSAOUtils.k_GTAODepthPyramidMipCount]; // xy = where each MIP sits in the packed pyramid
            public int downsample;
            public int mipCount;
            public TextureHandle depthTexture;
            public TextureHandle depthPyramidTexture;
        }

        private class GTAOComputePassData
        {
            public ComputeShader cs;
            public int kernel;
            public Vector2Int dispatchSize;
            public bool useBlueNoise;
            public LocalKeywordSet localKeywords;
            public bool willRunTemporalFilter;
            public int directionCount;
            public int stepCount;
            public ScreenSpaceAmbientOcclusionSpatialFilter spatialFilter;
            public GTAOComputeParams computeParams;
            public Matrix4x4[] cameraViewProjections = new Matrix4x4[2];
            public Vector4[] cameraTopLeftCorner = new Vector4[2];
            public Vector4[] cameraXExtent = new Vector4[2];
            public Vector4[] cameraYExtent = new Vector4[2];
            public Vector4[] cameraZExtent = new Vector4[2];
            public TextureHandle depthTexture;
            public TextureHandle normalsTexture;
            public TextureHandle blueNoiseTexture;
            public TextureHandle aoTexture;
            public SSAOUtils.GTAODepthPyramidBinding depthPyramid;
            public Vector4[] depthPyramidMipLevelOffsets = new Vector4[SSAOUtils.k_GTAODepthPyramidMipCount];
        }

        private class GTAOSingleDispatchPassData
        {
            public ComputeShader cs;
            public int kernel;
            public int dstPropertyId;
            public Vector2Int dispatchSize;
            public TextureHandle srcTexture;
            public TextureHandle depthTexture;
            public TextureHandle dstTexture;
            public float boxFilterStep;
        }

        private class GTAOCopyHistoryPassData
        {
            public ComputeShader cs;
            public int kernel;
            public Vector2Int dispatchSize;
            public TextureHandle srcTexture;
            public TextureHandle depthTexture;
            public TextureHandle historyTexture;
        }

        private class GTAOTemporalPassData
        {
            public ComputeShader cs;
            public int kernel;
            public Vector2Int dispatchSize;
            public TextureHandle aoTexture;
            public TextureHandle depthTexture;
            public TextureHandle historyReadTexture;
            public TextureHandle historyWriteTexture;
            public TextureHandle motionVectorTexture;
            public TextureHandle temporalTexture;
            public ScreenSpaceAmbientOcclusionSpatialFilter spatialFilter;
        }

        private class GTAOFinalBlitPassData
        {
            public ComputeShader cs;
            public int kernel;
            public Vector2Int dispatchSize;
            public float directLightingStrength;
            public TextureHandle srcTexture;
            public TextureHandle finalTexture;
        }

        private class GTAOAfterOpaqueBlitPassData
        {
            public Material material;
            public Vector4 sourceSize;
            public Vector4 ssaoParams;
            public int shaderPass;
            public TextureHandle sourceTexture;
            public TextureHandle targetTexture;
        }

        internal GTAOPass(Shader shader, Texture2D[] blueNoiseTextures)
        {
            m_CurrentSettings = new ScreenSpaceAmbientOcclusionSettings();
            m_Material = CoreUtils.CreateEngineMaterial(shader);
            m_BlueNoiseTextures = blueNoiseTextures;

            if (m_SupportsComputeShader && GraphicsSettings.TryGetRenderPipelineSettings<ScreenSpaceAmbientOcclusionCoreResources>(out var ssaoCoreResources))
            {
                m_ComputePathState.Init(ssaoCoreResources.GTAOComputeShader);
                m_DepthPrefilterState.Init(ssaoCoreResources.GTAODepthPrefilterComputeShader);
            }
        }

        // Sets up GTAO pass using active volume. Returns false if SSAO should be skipped.
        internal bool Setup(ScreenSpaceAmbientOcclusionVolumeOverride ssaoVolume)
        {
            ApplyVolumeSettings(m_CurrentSettings, ssaoVolume);
            m_BlurType = SSAOUtils.GetBlurType(m_CurrentSettings.BlurQuality);
            return ssaoVolume.IsActive();
        }

        private static void ApplyVolumeSettings(ScreenSpaceAmbientOcclusionSettings settings, ScreenSpaceAmbientOcclusionVolumeOverride volume)
        {
            // Common parameters
            settings.Mode = ScreenSpaceAmbientOcclusionMode.GTAO;
            settings.UseComputeShader = volume.useComputeShader;
            settings.Intensity = volume.intensity;
            settings.Radius = volume.radius;
            settings.DirectLightingStrength = volume.directLightingStrength;
            settings.Falloff = volume.falloffDistance;
            settings.Downsample = volume.downsample;
            settings.AfterOpaque = volume.afterOpaque;
            settings.AOMethod = volume.method == ScreenSpaceAmbientOcclusionNoiseMethod.BlueNoise ? ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise : ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.InterleavedGradient;
            settings.Samples = volume.sampleCount switch
            {
                ScreenSpaceAmbientOcclusionSampleCount.Low => ScreenSpaceAmbientOcclusionSettings.AOSampleOption.Low,
                ScreenSpaceAmbientOcclusionSampleCount.High => ScreenSpaceAmbientOcclusionSettings.AOSampleOption.High,
                _ => ScreenSpaceAmbientOcclusionSettings.AOSampleOption.Medium
            };

            // GTAO mode parameters
            settings.GTAOMinimumRadiusInPixels = volume.minimumRadiusInPixels;
            settings.Source = ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals;
            settings.BlurQuality = ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.High;
            settings.NormalSamples = ScreenSpaceAmbientOcclusionSettings.NormalQuality.High;

            settings.GTAODepthSampling = volume.depthSampling;

            if (volume.useComputeShader)
            {
                settings.GTAODirectionCount = volume.directionCount;
                settings.GTAOStepCount = volume.stepCount;
                settings.GTAOSpatialFilter = volume.spatialFilter;
                settings.GTAOTemporalFilterEnabled = volume.temporalFilter;
                settings.GTAOGhostingMitigation = volume.ghostingMitigation;
                settings.GTAOHistoryLength = volume.historyLength;
            }
        }

        // ---- GTAO-specific helpers ----

        internal static void CalculateGTAOViewParams(ScreenSpaceAmbientOcclusionSettings settings, UniversalCameraData cameraData,
            bool orthographicCamera, float radius, in TextureDesc cameraColorDesc, out Vector4 ssaoParams2, out Vector4 depthToViewParams)
        {
            if (settings.Mode == ScreenSpaceAmbientOcclusionMode.SSAO)
            {
                ssaoParams2 = Vector4.zero;
                depthToViewParams = Vector4.zero;
                return;
            }

            float invHalfTanFOV = cameraData.camera.projectionMatrix.m11;
            int downsampleDivider = settings.Downsample ? 2 : 1;
            Vector2 runningRes = new Vector2(cameraColorDesc.width / downsampleDivider, cameraColorDesc.height / downsampleDivider);
            float aspectRatio = runningRes.y / runningRes.x;
            float fovCorrection = runningRes.y * invHalfTanFOV * 0.5f;

            ssaoParams2 = new Vector4(settings.GTAOMinimumRadiusInPixels / (float)downsampleDivider, fovCorrection, runningRes.x, runningRes.y);
            depthToViewParams = new Vector4(
                2.0f / (invHalfTanFOV * aspectRatio * runningRes.x),
                2.0f / (invHalfTanFOV * runningRes.y),
                1.0f / (invHalfTanFOV * aspectRatio),
                1.0f / invHalfTanFOV
            );
        }

        private static void CalculateTemporalParams(ScreenSpaceAmbientOcclusionSettings settings, out float temporalRotation, out int temporalOffset)
        {
            temporalRotation = 0.0f;
            temporalOffset = 0;

            if (!settings.IsTemporalFilterActive)
                return;

            uint frameIndex = (uint)GetTemporalFrameIndex();
            uint temporalRotationCount = (uint)s_TemporalRotations.Length;
            temporalRotation = s_TemporalRotations[frameIndex % temporalRotationCount] / k_DegreesPerRotation;
            temporalOffset = (int)((frameIndex / temporalRotationCount) % k_TemporalOffsetCount);
        }

        private static int GetTemporalFrameIndex()
        {
            // Time.frameCount does not advance on Scene View repaints.
            return Unity.RenderPipelines.Core.Runtime.Shared.RendererUpdateManagerBridge.GetMotionVectorFrameIndex();
        }

        private static Vector2Int CalculateDispatchSize(Vector2Int textureSize, ComputeShader cs, int kernelIndex)
        {
            cs.GetKernelThreadGroupSizes(kernelIndex, out uint groupX, out uint groupY, out _);
            return new Vector2Int(
                Mathf.CeilToInt(textureSize.x / (float)groupX),
                Mathf.CeilToInt(textureSize.y / (float)groupY)
            );
        }

        private static GTAOComputeParams CreateGTAOComputeParams(ScreenSpaceAmbientOcclusionSettings settings, UniversalCameraData cameraData, in TextureDesc cameraColorDesc, Texture2D blueNoiseTexture)
        {
            GTAOComputeParams computeParams = new GTAOComputeParams();
            float radius = SSAOUtils.CalculateRadius(settings);

            computeParams.orthographicCamera = cameraData.camera.orthographic;
            computeParams.ssaoParams = SSAOUtils.CalculateCommonParams(settings, radius);
            CalculateGTAOViewParams(settings, cameraData, computeParams.orthographicCamera, radius, cameraColorDesc, out computeParams.ssaoParams2, out computeParams.depthToViewParams);

            computeParams.sourceSize = PostProcessUtils.CalcShaderSourceSize(cameraColorDesc.width, cameraColorDesc.height, cameraColorDesc.useDynamicScale);
            computeParams.projectionParams2 = SSAOUtils.CalculateProjectionParams2(cameraData);

            if (settings.AOMethod == ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise && blueNoiseTexture != null)
                computeParams.blueNoiseParams = SSAOUtils.CalculateBlueNoiseParams(cameraData, blueNoiseTexture);

            computeParams.historyLength = settings.GTAOHistoryLength;
            computeParams.ghostingMitigation = settings.GTAOGhostingMitigation;
            CalculateTemporalParams(settings, out computeParams.temporalRotation, out computeParams.temporalOffset);
            return computeParams;
        }

        private static TemporalHistoryState GetTemporalHistoryState(RenderGraph renderGraph, ScreenSpaceAmbientOcclusionSettings settings, UniversalCameraData cameraData, bool useComputeShader)
        {
            var state = new TemporalHistoryState
            {
                historyReadTexture = TextureHandle.nullHandle,
                historyWriteTexture = TextureHandle.nullHandle,
            };

            if (!settings.IsTemporalFilterActive || cameraData.historyManager == null)
                return state;

            cameraData.historyManager.RequestAccess<SSAOHistory>();
            state.ssaoHistory = cameraData.historyManager.GetHistoryForWrite<SSAOHistory>();
            if (state.ssaoHistory == null)
                return state;

            bool xrMultipassEnabled = false;
#if ENABLE_VR && ENABLE_XR_MODULE
            xrMultipassEnabled = cameraData.xr.enabled && !cameraData.xr.singlePassEnabled;
#endif
            bool wasReallocated = state.ssaoHistory.Update(cameraData, settings.Downsample, useComputeShader, xrMultipassEnabled);

            int multipassId = 0;
#if ENABLE_VR && ENABLE_XR_MODULE
            multipassId = cameraData.xr.multipassId;
#endif
            int accumulationVersion = state.ssaoHistory.GetAccumulationVersion(multipassId);
            bool isPreview = cameraData.camera.cameraType == CameraType.Preview;
            bool isRenderRequest = cameraData.camera.isProcessingRenderRequest;
            state.isNewFrame = !isPreview && !isRenderRequest && accumulationVersion != GetTemporalFrameIndex();
            RTHandle historyWrite = state.ssaoHistory.GetCurrentTexture(multipassId);
            RTHandle historyRead = state.ssaoHistory.GetPreviousTexture(multipassId);
            Debug.Assert(historyWrite != null && historyRead != null, "SSAO temporal history textures should be allocated here.");

            state.historyWriteTexture = renderGraph.ImportTexture(historyWrite);
            state.historyReadTexture = renderGraph.ImportTexture(historyRead);
            state.historyReady = !wasReallocated && accumulationVersion >= 0;

            return state;
        }

        private TextureHandle ImportBlueNoiseTexture(RenderGraph renderGraph, Texture2D blueNoiseTexture)
        {
            if (blueNoiseTexture == null)
                return TextureHandle.nullHandle;

            if (m_ComputePathState.blueNoiseRTHandle == null || m_ComputePathState.blueNoiseRTHandle.externalTexture != blueNoiseTexture)
            {
                m_ComputePathState.blueNoiseRTHandle?.Release();
                m_ComputePathState.blueNoiseRTHandle = RTHandles.Alloc(blueNoiseTexture);
            }

            return renderGraph.ImportTexture(m_ComputePathState.blueNoiseRTHandle);
        }

        // ---- RecordRenderGraph ----

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            if (!resourceData.cameraDepthTexture.IsValid() || !resourceData.cameraNormalsTexture.IsValid())
            {
                if (Debug.isDebugBuild && !m_WarnedMissingTextures)
                {
                    m_WarnedMissingTextures = true;
                    Debug.LogWarning("Screen Space Ambient Occlusion: skipped because the active renderer does not provide the required camera depth/normals textures.");
                }
                return;
            }

            if (Debug.isDebugBuild) // Saw valid textures: reset so a later renderer without them warns again.
                m_WarnedMissingTextures = false;

            // Builds a TextureDesc compatible with the active camera color target, safe for both intermediate render textures and imported back-buffer handles.
            TextureDesc cameraColorDesc = SSAOUtils.GetCameraColorDescriptor(renderGraph, cameraData, resourceData.activeColorTexture);

            bool useComputeShader = (m_CurrentSettings.NeedsComputeShader && m_SupportsComputeShader) && m_ComputePathState.shader != null && m_ComputePathState.gtaoKernel >= 0;

            SSAOUtils.CreateRenderTextureHandles(renderGraph, resourceData, cameraColorDesc,
                m_CurrentSettings, m_SupportsR8RenderTextureFormat, m_BlurType, enableRandomWrite: useComputeShader,
                out TextureHandle aoTexture, out TextureHandle blurTexture, out TextureHandle temporalTexture, out TextureHandle finalTexture);

            var temporalHistory = GetTemporalHistoryState(renderGraph, m_CurrentSettings, cameraData, useComputeShader);

            SSAOUtils.SetupCameraViewMatrices(cameraData, ref m_CameraViewData);
            m_BlueNoiseTextureIndex = SSAOUtils.AdvanceBlueNoiseIndex(m_CurrentSettings, m_BlueNoiseTextures, m_BlueNoiseTextureIndex);

            RecordDepthPyramid(renderGraph, cameraData, resourceData, cameraColorDesc, out SSAOUtils.GTAODepthPyramidBinding depthPyramid);

            if (useComputeShader)
            {
                RecordGTAOComputePass(renderGraph, aoTexture, blurTexture, temporalTexture, finalTexture, temporalHistory, cameraData, resourceData, cameraColorDesc, in depthPyramid);
            }
            else
            {
                // GTAO raster-fragment fallback - reuse shared raster recording
                TextureHandle cameraDepthTexture = resourceData.cameraDepthTexture;
                TextureHandle cameraNormalsTexture = resourceData.cameraNormalsTexture;

                SSAOUtils.SetupKeywordsAndParameters(m_Material, m_CurrentSettings, cameraData, cameraColorDesc, ref m_CameraViewData, m_BlueNoiseTextures, m_BlueNoiseTextureIndex, depthPyramid.enabled, ref m_SSAOParamsPrev);
                SSAOUtils.RecordRasterAOPass(renderGraph, m_ProfilingSampler, m_Material, m_CurrentSettings, aoTexture, cameraDepthTexture, cameraNormalsTexture, cameraColorDesc, in depthPyramid);
                SSAOUtils.RecordBlurChain(renderGraph, m_ProfilingSampler, m_Material, m_CurrentSettings, cameraData, aoTexture, blurTexture, finalTexture);

                if (!m_CurrentSettings.AfterOpaque)
                    SSAOUtils.RecordCleanupPass(renderGraph, m_ProfilingSampler, m_CurrentSettings.DirectLightingStrength, finalTexture);
            }
        }

        private void RecordDepthPyramid(RenderGraph renderGraph, UniversalCameraData cameraData, UniversalResourceData resourceData, in TextureDesc cameraColorDesc, out SSAOUtils.GTAODepthPyramidBinding depthPyramid)
        {
            depthPyramid = default;

            if (!m_CurrentSettings.NeedsDepthPyramid)
                return;

            if (!m_DepthPrefilterState.IsValid)
            {
                if (Debug.isDebugBuild && !m_WarnedDepthPyramidUnavailable)
                {
                    m_WarnedDepthPyramidUnavailable = true;
                    Debug.LogWarning("Screen Space Ambient Occlusion: Depth Sampling is set to Depth Pyramid, but the compute shaders it needs are unavailable on this platform. Falling back to Camera Depth.");
                }
                return;
            }

            // Half's relative error is too coarse for orthographic, where the radius does not shrink with depth.
            bool orthographicCamera = cameraData.camera.orthographic;
            bool halfSupported = !orthographicCamera && SystemInfo.IsFormatSupported(GraphicsFormat.R16_SFloat, GraphicsFormatUsage.LoadStore);
            GraphicsFormat pyramidFormat = halfSupported ? GraphicsFormat.R16_SFloat : GraphicsFormat.R32_SFloat;
            bool pyramidFormatSupported = halfSupported || SystemInfo.IsFormatSupported(pyramidFormat, GraphicsFormatUsage.LoadStore);

            if (!pyramidFormatSupported)
            {
                if (Debug.isDebugBuild && !m_WarnedDepthPyramidUnavailable)
                {
                    m_WarnedDepthPyramidUnavailable = true;
                    Debug.LogWarning("Screen Space Ambient Occlusion: Depth Sampling is set to Depth Pyramid, but this platform cannot write the single channel 32-bit float format it needs. Falling back to Camera Depth.");
                }
                return;
            }

#if ENABLE_VR && ENABLE_XR_MODULE
            // The prefilter reduces on a flat grid, so coarse MIPs would average across the foveation warp.
            if (cameraData.xr.supportsFoveatedRendering && (XRSystem.foveatedRenderingCaps & FoveatedRenderingCaps.NonUniformRaster) != 0)
            {
                if (Debug.isDebugBuild && !m_WarnedDepthPyramidUnavailable)
                {
                    m_WarnedDepthPyramidUnavailable = true;
                    Debug.LogWarning("Screen Space Ambient Occlusion: Depth Sampling is set to Depth Pyramid, but it is not supported with non-uniform foveated rendering. Falling back to Camera Depth.");
                }
                return;
            }
#endif

            Vector2Int pyramidAllocationSize = SSAOUtils.CalculateAOAllocationResolution(cameraColorDesc, m_CurrentSettings);
            Vector2Int pyramidViewportSize = SSAOUtils.CalculateAOViewportResolution(cameraColorDesc, m_CurrentSettings);
            int mipCount = Mathf.Clamp(CoreUtils.GetMipCount(Mathf.Max(pyramidViewportSize.x, pyramidViewportSize.y)), 1, SSAOUtils.k_GTAODepthPyramidMipCount);

            SSAOUtils.GTAODepthPyramidLayout allocationLayout = SSAOUtils.CalculateGTAODepthPyramidLayout(pyramidAllocationSize);
            SSAOUtils.GTAODepthPyramidLayout viewportLayout = SSAOUtils.CalculateGTAODepthPyramidLayout(pyramidViewportSize);
            Debug.Assert(viewportLayout.packedSize.x <= allocationLayout.packedSize.x && viewportLayout.packedSize.y <= allocationLayout.packedSize.y,
                "The packed layout must grow with its MIP 0 size, so that the allocation holds the layout of any viewport that fits in it.");

            TextureDesc pyramidDesc = PostProcessUtils.GetCompatibleDescriptor(cameraColorDesc, allocationLayout.packedSize.x, allocationLayout.packedSize.y, pyramidFormat);
            pyramidDesc.enableRandomWrite = true;

            // The packed layout cannot be scaled uniformly.
            // The allocation stays full size and a downscaled frame uses the smaller layout inside it.
            pyramidDesc.useDynamicScale = false;

            depthPyramid.texture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, pyramidDesc, "_GTAO_DepthPyramid", false, Color.clear, FilterMode.Point);
            depthPyramid.size = new Vector4(pyramidViewportSize.x, pyramidViewportSize.y, 0.0f, 0.0f);
            depthPyramid.parameters = new Vector4(k_DepthMipSamplingOffset, mipCount - 1, k_MaxMipFootprintFractionOfRadius, 0.0f);
            depthPyramid.layout = viewportLayout;
            depthPyramid.enabled = true;

            RecordDepthPrefilterPass(renderGraph, resourceData.cameraDepthTexture, in depthPyramid, cameraData, cameraColorDesc, pyramidViewportSize, mipCount, pyramidFormat, pyramidDesc.slices, pyramidDesc.dimension == TextureDimension.Tex2DArray);
        }

        // ---- Compute Pass Recording ----

        private void RecordGTAOComputePass(RenderGraph renderGraph, TextureHandle aoTexture, TextureHandle blurTexture, TextureHandle temporalTexture, TextureHandle finalTexture,
            in TemporalHistoryState temporalHistory, UniversalCameraData cameraData, UniversalResourceData resourceData, in TextureDesc cameraColorDesc, in SSAOUtils.GTAODepthPyramidBinding depthPyramid)
        {
            TextureHandle cameraDepthTexture = resourceData.cameraDepthTexture;
            TextureHandle cameraNormalsTexture = resourceData.cameraNormalsTexture;
            TextureHandle motionVectorTexture = resourceData.motionVectorColor;
            bool temporalEnabled = m_CurrentSettings.IsTemporalFilterActive;
            bool isTemporalTextureValid = temporalTexture.IsValid();
            bool isHistoryReadTextureValid = temporalHistory.historyReadTexture.IsValid();
            bool isHistoryWriteTextureValid = temporalHistory.historyWriteTexture.IsValid();
            bool hasTemporalResources = temporalEnabled && isTemporalTextureValid && isHistoryReadTextureValid && isHistoryWriteTextureValid;
            bool shouldSeedHistory = hasTemporalResources && temporalHistory.isNewFrame && !temporalHistory.historyReady;
            bool willRunTemporalFilter = hasTemporalResources && (temporalHistory.historyReady || shouldSeedHistory);

            TextureDesc aoDesc = aoTexture.GetDescriptor(renderGraph);
            Vector2Int aoSize = new Vector2Int(aoDesc.width, aoDesc.height);
            int traceKernel = depthPyramid.enabled ? m_ComputePathState.gtaoDepthPyramidKernel : m_ComputePathState.gtaoKernel;
            Vector2Int aoDispatchSize = CalculateDispatchSize(aoSize, m_ComputePathState.shader, traceKernel);

            Texture2D blueNoiseTexture = SSAOUtils.GetBlueNoiseTexture(m_BlueNoiseTextures, m_BlueNoiseTextureIndex);
            if (m_CurrentSettings.AOMethod == ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise)
                Debug.Assert(blueNoiseTexture != null, "Blue noise texture is null. Blue noise mode requires a valid blue noise texture.");
            TextureHandle blueNoiseHandle = ImportBlueNoiseTexture(renderGraph, blueNoiseTexture ?? Texture2D.blackTexture);

            var computeParams = CreateGTAOComputeParams(m_CurrentSettings, cameraData, cameraColorDesc, blueNoiseTexture);

            // GTAO
            RecordGTAOMainPass(renderGraph, cameraData, computeParams, blueNoiseHandle, cameraDepthTexture, cameraNormalsTexture, aoTexture, aoDispatchSize, willRunTemporalFilter, in depthPyramid, traceKernel);

            // Spatial filter
            TextureHandle spatialFilteredTexture = aoTexture;
            Debug.Assert(blurTexture.IsValid(), "GTAO compute path requires an allocated blur texture.");

            if (m_CurrentSettings.GTAOSpatialFilter == ScreenSpaceAmbientOcclusionSpatialFilter.Box)
            {
                RecordComputeDispatchPass(renderGraph, "GTAO Box Spatial Filter 1", m_ComputePathState.boxSpatialFilterKernel, aoDispatchSize, aoTexture, blurTexture, ShaderIDs._BlurOutput, cameraDepthTexture, 4.0f);
                RecordComputeDispatchPass(renderGraph, "GTAO Box Spatial Filter 2", m_ComputePathState.boxSpatialFilterKernel, aoDispatchSize, blurTexture, aoTexture, ShaderIDs._BlurOutput, cameraDepthTexture, 2.0f);
                RecordComputeDispatchPass(renderGraph, "GTAO Box Spatial Filter 3", m_ComputePathState.boxSpatialFilterKernel, aoDispatchSize, aoTexture, blurTexture, ShaderIDs._BlurOutput, cameraDepthTexture, 1.0f);
                spatialFilteredTexture = blurTexture;
            }
            else
            {
                RecordComputeDispatchPass(renderGraph, "GTAO BlurH", m_ComputePathState.blurHKernel, aoDispatchSize, aoTexture, blurTexture, ShaderIDs._BlurOutput);
                RecordComputeDispatchPass(renderGraph, "GTAO BlurV", m_ComputePathState.blurVKernel, aoDispatchSize, blurTexture, aoTexture, ShaderIDs._BlurOutput);
                spatialFilteredTexture = aoTexture;
            }

            // Temporal filter and history update
            if (willRunTemporalFilter)
            {
                if (shouldSeedHistory)
                    RecordCopyHistoryPass(renderGraph, "GTAO SeedHistory", aoDispatchSize, spatialFilteredTexture, cameraDepthTexture, temporalHistory.historyReadTexture);

                // Duplicate renders of the same frame can come after history has been advanced.
                // Keep resolving against history, but stop reprojection motion for those redraws.
                TextureHandle activeMotionVectorTexture = temporalHistory.isNewFrame ? motionVectorTexture : renderGraph.defaultResources.blackTexture;
                RecordTemporalFilterPass(renderGraph, aoDispatchSize, spatialFilteredTexture, cameraDepthTexture, temporalHistory.historyReadTexture, temporalHistory.historyWriteTexture, activeMotionVectorTexture, temporalTexture);
            }

            // Determine the final source for downstream passes
            TextureHandle resultTexture = willRunTemporalFilter ? temporalTexture : spatialFilteredTexture;

            // Final / AfterOpaque blit
            if (m_CurrentSettings.AfterOpaque)
            {
                bool useBlitAfterOpaquePass = m_CurrentSettings.GTAOSpatialFilter == ScreenSpaceAmbientOcclusionSpatialFilter.Box;
                int afterOpaqueShaderPass = useBlitAfterOpaquePass
                    ? (int)SSAOUtils.ShaderPasses.BoxAfterOpaque
                    : (int)SSAOUtils.ShaderPasses.BilateralAfterOpaque;
                RecordAfterOpaqueBlitPass(renderGraph, computeParams, resultTexture, resourceData.activeColorTexture, afterOpaqueShaderPass);
            }
            else if (finalTexture.IsValid())
            {
                TextureDesc finalDesc = finalTexture.GetDescriptor(renderGraph);
                Vector2Int finalSize = new Vector2Int(finalDesc.width, finalDesc.height);
                Vector2Int finalDispatchSize = CalculateDispatchSize(finalSize, m_ComputePathState.shader, m_ComputePathState.finalBlitKernel);
                RecordFinalBlitPass(renderGraph, resultTexture, finalTexture, m_CurrentSettings.DirectLightingStrength, finalDispatchSize);
            }

            if (willRunTemporalFilter && temporalHistory.isNewFrame && temporalHistory.ssaoHistory != null)
            {
                int multipassId = 0;
#if ENABLE_VR && ENABLE_XR_MODULE
                multipassId = cameraData.xr.multipassId;
#endif
                temporalHistory.ssaoHistory.SetAccumulationVersion(multipassId, GetTemporalFrameIndex());
            }
        }

        private void RecordDepthPrefilterPass(RenderGraph renderGraph, TextureHandle depthTexture, in SSAOUtils.GTAODepthPyramidBinding depthPyramid, UniversalCameraData cameraData,
            in TextureDesc cameraColorDesc, Vector2Int pyramidViewportSize, int mipCount, GraphicsFormat pyramidFormat, int pyramidSliceCount, bool pyramidIsArray)
        {
            using (var builder = renderGraph.AddComputePass<GTAODepthPrefilterPassData>("GTAO Depth Prefilter", out var passData, m_ProfilingSampler))
            {
                Vector4 sourceSize = PostProcessUtils.CalcShaderSourceSize(cameraColorDesc.width, cameraColorDesc.height, cameraColorDesc.useDynamicScale);

                // Depth filter weight ramp
                float effectRadius = k_DepthFilterRadiusScale * SSAOUtils.CalculateRadius(m_CurrentSettings);
                float falloffRange = Mathf.Max(k_DepthFilterFalloffRange * effectRadius, k_DepthFilterMinFalloffRange);
                float falloffFrom = effectRadius * (1.0f - k_DepthFilterFalloffRange);

                float maxStorableDepth = pyramidFormat == GraphicsFormat.R16_SFloat ? k_HalfMaxDepth : float.MaxValue;

                passData.cs = m_DepthPrefilterState.shader;
                passData.kernel = m_DepthPrefilterState.kernel;
                passData.orthographicKeyword = m_DepthPrefilterState.orthographic;
                passData.orthographicCamera = cameraData.camera.orthographic;

                // The prefilter has no access to URP's depth helpers, so the orthographic conversion comes from here.
                float nearPlane = cameraData.camera.nearClipPlane;
                float farPlane = cameraData.camera.farClipPlane;
                passData.orthoRemap = SystemInfo.usesReversedZBuffer ? new Vector4(farPlane, nearPlane - farPlane, 0.0f, 0.0f) : new Vector4(nearPlane, farPlane - nearPlane, 0.0f, 0.0f);

                passData.disableTexture2DArrayKeyword = m_DepthPrefilterState.disableTexture2DArray;
                passData.pyramidIsArray = pyramidIsArray;
                passData.downsample = m_CurrentSettings.Downsample ? 2 : 1;
                passData.prefilterSize = new Vector4(pyramidViewportSize.x, pyramidViewportSize.y, sourceSize.x - 1.0f, sourceSize.y - 1.0f);

                // The gather addresses the allocation, not the dynamic resolution viewport.
                passData.prefilterSourceSize = PostProcessUtils.CalcShaderSourceSize(cameraColorDesc.width, cameraColorDesc.height, false);
                passData.prefilterFalloff = new Vector4(-1.0f / falloffRange, falloffFrom / falloffRange + 1.0f, maxStorableDepth, 0.0f);

                // One group per k_DepthPrefilterTileSize tile of MIP 0.
                passData.dispatchSize = new Vector2Int(CoreUtils.DivRoundUp(pyramidViewportSize.x, k_DepthPrefilterTileSize), CoreUtils.DivRoundUp(pyramidViewportSize.y, k_DepthPrefilterTileSize));

                passData.sliceCount = pyramidSliceCount;

                depthPyramid.layout.WriteMipLevelOffsets(passData.depthPyramidMipLevelOffsets);
                passData.mipCount = mipCount;
                passData.depthTexture = depthTexture;
                passData.depthPyramidTexture = depthPyramid.texture;

                builder.UseTexture(depthTexture, AccessFlags.Read);
                builder.UseTexture(depthPyramid.texture, AccessFlags.Write);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (GTAODepthPrefilterPassData data, ComputeGraphContext ctx) =>
                {
                    var cmd = ctx.cmd;
                    var cs = data.cs;

                    cmd.SetKeyword(cs, data.orthographicKeyword, data.orthographicCamera);
                    cmd.SetKeyword(cs, data.disableTexture2DArrayKeyword, !data.pyramidIsArray);

                    cmd.SetComputeVectorParam(cs, ShaderIDs._GTAOPrefilterSize, data.prefilterSize);
                    cmd.SetComputeVectorParam(cs, ShaderIDs._GTAOPrefilterSourceSize, data.prefilterSourceSize);
                    cmd.SetComputeVectorParam(cs, ShaderIDs._GTAOPrefilterFalloff, data.prefilterFalloff);
                    cmd.SetComputeVectorParam(cs, ShaderIDs._GTAOPrefilterOrthoRemap, data.orthoRemap);
                    cmd.SetComputeVectorArrayParam(cs, SSAOUtils.ShaderConstants._GTAODepthPyramidMipLevelOffsets, data.depthPyramidMipLevelOffsets);
                    cmd.SetComputeIntParam(cs, ShaderIDs._GTAOPrefilterDownsample, data.downsample);
                    cmd.SetComputeIntParam(cs, ShaderIDs._GTAOPrefilterMipCount, data.mipCount);
                    cmd.SetComputeTextureParam(cs, data.kernel, ShaderIDs._CameraDepthTexture, data.depthTexture);
                    cmd.SetComputeTextureParam(cs, data.kernel, SSAOUtils.ShaderConstants._GTAODepthPyramid, data.depthPyramidTexture);

                    cmd.DispatchCompute(cs, data.kernel, data.dispatchSize.x, data.dispatchSize.y, data.sliceCount);
                });
            }
        }

        private void RecordGTAOMainPass(RenderGraph renderGraph, UniversalCameraData cameraData, GTAOComputeParams computeParams,
            TextureHandle blueNoiseTexture, TextureHandle depthTexture, TextureHandle normalsTexture, TextureHandle aoTexture, Vector2Int dispatchSize, bool willRunTemporalFilter,
            in SSAOUtils.GTAODepthPyramidBinding depthPyramid, int traceKernel)
        {
            using (var builder = renderGraph.AddComputePass<GTAOComputePassData>("GTAO Compute", out var passData, m_ProfilingSampler))
            {
                passData.cs = m_ComputePathState.shader;
                passData.kernel = traceKernel;
                passData.dispatchSize = dispatchSize;
                passData.computeParams = computeParams;
                passData.useBlueNoise = m_CurrentSettings.AOMethod == ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise;
                passData.localKeywords = m_ComputePathState.keywords;
                passData.willRunTemporalFilter = willRunTemporalFilter;
                passData.directionCount = m_CurrentSettings.GTAODirectionCount;
                passData.stepCount = m_CurrentSettings.GTAOStepCount;
                passData.spatialFilter = m_CurrentSettings.GTAOSpatialFilter;

#if ENABLE_VR && ENABLE_XR_MODULE
                int eyeCount = cameraData.xr.enabled && cameraData.xr.singlePassEnabled ? 2 : 1;
#else
                int eyeCount = 1;
#endif
                Array.Copy(m_CameraViewData.viewProjections, passData.cameraViewProjections, eyeCount);
                Array.Copy(m_CameraViewData.topLeftCorner, passData.cameraTopLeftCorner, eyeCount);
                Array.Copy(m_CameraViewData.xExtent, passData.cameraXExtent, eyeCount);
                Array.Copy(m_CameraViewData.yExtent, passData.cameraYExtent, eyeCount);
                Array.Copy(m_CameraViewData.zExtent, passData.cameraZExtent, eyeCount);

                passData.aoTexture = aoTexture;
                builder.UseTexture(aoTexture, AccessFlags.Write);

                passData.depthTexture = depthTexture;
                builder.UseTexture(depthTexture, AccessFlags.Read);

                passData.normalsTexture = normalsTexture;
                builder.UseTexture(normalsTexture, AccessFlags.Read);

                passData.blueNoiseTexture = blueNoiseTexture;
                builder.UseTexture(blueNoiseTexture, AccessFlags.Read);

                passData.depthPyramid = depthPyramid;
                if (depthPyramid.enabled)
                {
                    depthPyramid.layout.WriteMipLevelOffsets(passData.depthPyramidMipLevelOffsets);
                    builder.UseTexture(depthPyramid.texture, AccessFlags.Read);
                }

                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (GTAOComputePassData data, ComputeGraphContext ctx) =>
                {
                    var cmd = ctx.cmd;
                    var cs = data.cs;
                    ref var computeParams = ref data.computeParams;

                    cmd.SetKeyword(cs, data.localKeywords.blueNoise, data.useBlueNoise);
                    cmd.SetKeyword(cs, data.localKeywords.interleavedGradient, !data.useBlueNoise);
                    cmd.SetKeyword(cs, data.localKeywords.temporalFiltering, data.willRunTemporalFilter);
                    cmd.SetKeyword(cs, data.localKeywords.orthographic, computeParams.orthographicCamera);

                    cmd.SetComputeVectorParam(cs, SSAOUtils.ShaderConstants._SSAOParams, computeParams.ssaoParams);
                    cmd.SetComputeVectorParam(cs, SSAOUtils.ShaderConstants._SSAOParams2, computeParams.ssaoParams2);
                    cmd.SetComputeVectorParam(cs, SSAOUtils.ShaderConstants._AODepthToViewParams, computeParams.depthToViewParams);
                    cmd.SetComputeVectorParam(cs, SSAOUtils.ShaderConstants._SourceSize, computeParams.sourceSize);
                    cmd.SetComputeVectorParam(cs, SSAOUtils.ShaderConstants._ProjectionParams2, computeParams.projectionParams2);
                    cmd.SetComputeMatrixArrayParam(cs, SSAOUtils.ShaderConstants._CameraViewProjections, data.cameraViewProjections);
                    cmd.SetComputeVectorArrayParam(cs, SSAOUtils.ShaderConstants._CameraViewTopLeftCorner, data.cameraTopLeftCorner);
                    cmd.SetComputeVectorArrayParam(cs, SSAOUtils.ShaderConstants._CameraViewXExtent, data.cameraXExtent);
                    cmd.SetComputeVectorArrayParam(cs, SSAOUtils.ShaderConstants._CameraViewYExtent, data.cameraYExtent);
                    cmd.SetComputeVectorArrayParam(cs, SSAOUtils.ShaderConstants._CameraViewZExtent, data.cameraZExtent);
                    cmd.SetComputeIntParam(cs, ShaderIDs._GTAODirectionCount, data.directionCount);
                    cmd.SetComputeIntParam(cs, ShaderIDs._GTAOStepCount, data.stepCount);
                    cmd.SetComputeIntParam(cs, ShaderIDs._GTAOSpatialFilter, (int)data.spatialFilter);

                    cmd.SetComputeVectorParam(cs, SSAOUtils.ShaderConstants._SSAOBlueNoiseParams, computeParams.blueNoiseParams);
                    cmd.SetComputeTextureParam(cs, data.kernel, SSAOUtils.ShaderConstants._BlueNoiseTexture, data.blueNoiseTexture);

                    if (data.willRunTemporalFilter)
                    {
                        cmd.SetComputeFloatParam(cs, ShaderIDs._SSAOHistoryLength, computeParams.historyLength);
                        cmd.SetComputeFloatParam(cs, ShaderIDs._SSAOGhostingMitigation, computeParams.ghostingMitigation);
                        cmd.SetComputeFloatParam(cs, ShaderIDs._SSAOTemporalRotation, computeParams.temporalRotation);
                        cmd.SetComputeIntParam(cs, ShaderIDs._SSAOTemporalOffset, computeParams.temporalOffset);
                    }

                    cmd.SetComputeTextureParam(cs, data.kernel, ShaderIDs._CameraDepthTexture, data.depthTexture);
                    cmd.SetComputeTextureParam(cs, data.kernel, SSAOUtils.ShaderConstants._CameraNormalsTexture, data.normalsTexture);
                    if (data.depthPyramid.enabled)
                    {
                        cmd.SetComputeTextureParam(cs, data.kernel, SSAOUtils.ShaderConstants._GTAODepthPyramid, data.depthPyramid.texture);
                        cmd.SetComputeVectorParam(cs, SSAOUtils.ShaderConstants._GTAODepthPyramidSize, data.depthPyramid.size);
                        cmd.SetComputeVectorParam(cs, SSAOUtils.ShaderConstants._GTAODepthPyramidParams, data.depthPyramid.parameters);
                        cmd.SetComputeVectorArrayParam(cs, SSAOUtils.ShaderConstants._GTAODepthPyramidMipLevelOffsets, data.depthPyramidMipLevelOffsets);
                    }
                    cmd.SetComputeTextureParam(cs, data.kernel, ShaderIDs._AOOutput, data.aoTexture);
                    cmd.DispatchCompute(cs, data.kernel, data.dispatchSize.x, data.dispatchSize.y, 1);
                });
            }
        }

        private void RecordComputeDispatchPass(RenderGraph renderGraph, string passName, int kernel, Vector2Int dispatchSize, TextureHandle srcTexture, TextureHandle dstTexture, int dstPropertyId, TextureHandle depthTexture = default, float boxFilterStep = 0.0f)
        {
            using (var builder = renderGraph.AddComputePass<GTAOSingleDispatchPassData>(passName, out var passData, m_ProfilingSampler))
            {
                passData.cs = m_ComputePathState.shader;
                passData.kernel = kernel;
                passData.dispatchSize = dispatchSize;
                passData.srcTexture = srcTexture;
                passData.depthTexture = depthTexture;
                passData.dstTexture = dstTexture;
                passData.dstPropertyId = dstPropertyId;
                passData.boxFilterStep = boxFilterStep;

                builder.UseTexture(srcTexture, AccessFlags.Read);
                if (depthTexture.IsValid())
                    builder.UseTexture(depthTexture, AccessFlags.Read);
                builder.UseTexture(dstTexture, AccessFlags.Write);

                builder.SetRenderFunc(static (GTAOSingleDispatchPassData data, ComputeGraphContext ctx) =>
                {
                    ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, SSAOUtils.ShaderConstants._BlitTexture, data.srcTexture);
                    if (data.depthTexture.IsValid())
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, ShaderIDs._CameraDepthTexture, data.depthTexture);
                    ctx.cmd.SetComputeFloatParam(data.cs, ShaderIDs._GTAOBoxFilterStep, data.boxFilterStep);
                    ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, data.dstPropertyId, data.dstTexture);
                    ctx.cmd.DispatchCompute(data.cs, data.kernel, data.dispatchSize.x, data.dispatchSize.y, 1);
                });
            }
        }

        private void RecordCopyHistoryPass(RenderGraph renderGraph, string passName, Vector2Int dispatchSize, TextureHandle srcTexture, TextureHandle depthTexture, TextureHandle historyTexture)
        {
            using (var builder = renderGraph.AddComputePass<GTAOCopyHistoryPassData>(passName, out var passData, m_ProfilingSampler))
            {
                passData.cs = m_ComputePathState.shader;
                passData.kernel = m_ComputePathState.copyHistoryKernel;
                passData.dispatchSize = dispatchSize;
                passData.srcTexture = srcTexture;
                passData.depthTexture = depthTexture;
                passData.historyTexture = historyTexture;

                builder.UseTexture(srcTexture, AccessFlags.Read);
                builder.UseTexture(depthTexture, AccessFlags.Read);
                builder.UseTexture(historyTexture, AccessFlags.Write);

                builder.SetRenderFunc(static (GTAOCopyHistoryPassData data, ComputeGraphContext ctx) =>
                {
                    var cmd = ctx.cmd;
                    cmd.SetComputeTextureParam(data.cs, data.kernel, SSAOUtils.ShaderConstants._BlitTexture, data.srcTexture);
                    cmd.SetComputeTextureParam(data.cs, data.kernel, ShaderIDs._CameraDepthTexture, data.depthTexture);
                    cmd.SetComputeTextureParam(data.cs, data.kernel, ShaderIDs._AOOutputHistory, data.historyTexture);
                    cmd.DispatchCompute(data.cs, data.kernel, data.dispatchSize.x, data.dispatchSize.y, 1);
                });
            }
        }

        private void RecordTemporalFilterPass(RenderGraph renderGraph, Vector2Int dispatchSize, TextureHandle aoTexture, TextureHandle depthTexture, TextureHandle historyReadTexture, TextureHandle historyWriteTexture, TextureHandle motionVectorTexture, TextureHandle temporalTexture)
        {
            using (var builder = renderGraph.AddComputePass<GTAOTemporalPassData>("GTAO Temporal", out var passData, m_ProfilingSampler))
            {
                passData.cs = m_ComputePathState.shader;
                passData.kernel = m_ComputePathState.temporalKernel;
                passData.dispatchSize = dispatchSize;
                passData.aoTexture = aoTexture;
                passData.depthTexture = depthTexture;
                passData.historyReadTexture = historyReadTexture;
                passData.historyWriteTexture = historyWriteTexture;
                passData.temporalTexture = temporalTexture;
                passData.spatialFilter = m_CurrentSettings.GTAOSpatialFilter;

                builder.UseTexture(aoTexture, AccessFlags.Read);
                builder.UseTexture(depthTexture, AccessFlags.Read);
                builder.UseTexture(historyReadTexture, AccessFlags.Read);
                builder.UseTexture(historyWriteTexture, AccessFlags.Write);
                builder.UseTexture(temporalTexture, AccessFlags.Write);

                Debug.Assert(motionVectorTexture.IsValid(), "Motion vector texture is invalid. GTAO temporal filter requires a motion vector texture.");
                passData.motionVectorTexture = motionVectorTexture;
                builder.UseTexture(motionVectorTexture, AccessFlags.Read);

                builder.SetRenderFunc(static (GTAOTemporalPassData data, ComputeGraphContext ctx) =>
                {
                    var cmd = ctx.cmd;
                    cmd.SetComputeTextureParam(data.cs, data.kernel, SSAOUtils.ShaderConstants._BlitTexture, data.aoTexture);
                    cmd.SetComputeTextureParam(data.cs, data.kernel, ShaderIDs._CameraDepthTexture, data.depthTexture);
                    cmd.SetComputeTextureParam(data.cs, data.kernel, ShaderIDs._AOPackedHistory, data.historyReadTexture);
                    cmd.SetComputeTextureParam(data.cs, data.kernel, ShaderIDs._AOOutputHistory, data.historyWriteTexture);
                    cmd.SetComputeTextureParam(data.cs, data.kernel, ShaderIDs._MotionVectorTexture, data.motionVectorTexture);
                    cmd.SetComputeTextureParam(data.cs, data.kernel, ShaderIDs._OcclusionTexture, data.temporalTexture);
                    cmd.SetComputeIntParam(data.cs, ShaderIDs._GTAOSpatialFilter, (int)data.spatialFilter);
                    cmd.DispatchCompute(data.cs, data.kernel, data.dispatchSize.x, data.dispatchSize.y, 1);
                });
            }
        }

        private void RecordFinalBlitPass(RenderGraph renderGraph, TextureHandle srcTexture, TextureHandle finalTexture, float directLightingStrength, Vector2Int finalDispatchSize)
        {
            using (var builder = renderGraph.AddComputePass<GTAOFinalBlitPassData>("GTAO FinalBlit", out var passData, m_ProfilingSampler))
            {
                passData.cs = m_ComputePathState.shader;
                passData.kernel = m_ComputePathState.finalBlitKernel;
                passData.dispatchSize = finalDispatchSize;
                passData.directLightingStrength = directLightingStrength;
                passData.srcTexture = srcTexture;
                passData.finalTexture = finalTexture;

                builder.UseTexture(srcTexture, AccessFlags.Read);
                builder.UseTexture(finalTexture, AccessFlags.Write);
                builder.SetGlobalTextureAfterPass(finalTexture, SSAOUtils.ShaderConstants._SSAOFinalTexture);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (GTAOFinalBlitPassData data, ComputeGraphContext ctx) =>
                {
                    ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, SSAOUtils.ShaderConstants._BlitTexture, data.srcTexture);
                    ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, ShaderIDs._FinalOutput, data.finalTexture);
                    ctx.cmd.DispatchCompute(data.cs, data.kernel, data.dispatchSize.x, data.dispatchSize.y, 1);
                    ctx.cmd.SetKeyword(ShaderGlobalKeywords.ScreenSpaceOcclusion, true);
                    ctx.cmd.SetGlobalVector(SSAOUtils.ShaderConstants._AmbientOcclusionParam, new Vector4(1f, 0f, 0f, data.directLightingStrength));
                });
            }
        }

        private void RecordAfterOpaqueBlitPass(RenderGraph renderGraph, GTAOComputeParams computeParams, TextureHandle sourceTexture, TextureHandle targetTexture, int shaderPass)
        {
            using (var builder = renderGraph.AddRasterRenderPass<GTAOAfterOpaqueBlitPassData>("GTAO AfterOpaque Blit", out var passData, m_ProfilingSampler))
            {
                passData.material = m_Material;
                passData.sourceSize = computeParams.sourceSize;
                passData.ssaoParams = computeParams.ssaoParams;
                passData.shaderPass = shaderPass;
                passData.sourceTexture = sourceTexture;
                passData.targetTexture = targetTexture;

                builder.UseTexture(passData.sourceTexture, AccessFlags.Read);
                builder.SetRenderAttachment(passData.targetTexture, 0, AccessFlags.Write);

                builder.SetRenderFunc(static (GTAOAfterOpaqueBlitPassData data, RasterGraphContext ctx) =>
                {
                    Vector4 viewScaleBias = SSAOUtils.ComputeScaleBias(data.sourceTexture, SSAOUtils.IsYFlip(ctx, in data.sourceTexture, in data.targetTexture));
                    data.material.SetVector(SSAOUtils.ShaderConstants._SourceSize, data.sourceSize);
                    data.material.SetVector(SSAOUtils.ShaderConstants._SSAOParams, data.ssaoParams);
                    Blitter.BlitTexture(ctx.cmd, data.sourceTexture, viewScaleBias, data.material, data.shaderPass);
                });
            }
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            if (!m_CurrentSettings.AfterOpaque)
                cmd.SetKeyword(ShaderGlobalKeywords.ScreenSpaceOcclusion, false);
        }

        public void Dispose()
        {
            m_ComputePathState.Release();
            CoreUtils.Destroy(m_Material);
            m_Material = null;
            m_SSAOParamsPrev = default;
        }
    }
}
