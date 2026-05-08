#if MODERN_SSAO
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

        // Raster path state
        private SSAOUtils.SSAOMaterialParams m_SSAOParamsPrev = new SSAOUtils.SSAOMaterialParams();

        // Compute path state
        private ComputePathState m_ComputePathState;

        // Constants
        private const float k_GTAOMaxRadiusReferencePixelCount = 960.0f * 540.0f;
        private const float k_DegreesPerRotation = 360.0f;
        private const int k_TemporalOffsetCount = 4;

        // GTAO-only Shader Property IDs
        internal static class ShaderIDs
        {
            // Compute kernel I/O bindings
            public static readonly int _CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
            public static readonly int _AOOutput = Shader.PropertyToID("_AOOutput");
            public static readonly int _BlurOutput = Shader.PropertyToID("_BlurOutput");
            public static readonly int _TemporalOutput = Shader.PropertyToID("_TemporalOutput");
            public static readonly int _HistoryOutput = Shader.PropertyToID("_HistoryOutput");
            public static readonly int _FinalOutput = Shader.PropertyToID("_FinalOutput");

            // Temporal filter
            public static readonly int _SSAOHistoryTexture = Shader.PropertyToID("_SSAOHistoryTexture");
            public static readonly int _MotionVectorTexture = Shader.PropertyToID("_MotionVectorTexture");
            public static readonly int _SSAOTemporalParams = Shader.PropertyToID("_SSAOTemporalParams");
            public static readonly int _SSAOTemporalRotation = Shader.PropertyToID("_SSAOTemporalRotation");
            public static readonly int _SSAOTemporalOffset = Shader.PropertyToID("_SSAOTemporalOffset");

            // GTAO compute parameters
            public static readonly int _GTAODirectionCount = Shader.PropertyToID("_GTAODirectionCount");
            public static readonly int _GTAOStepCount = Shader.PropertyToID("_GTAOStepCount");
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
            public int blurHKernel;
            public int blurVKernel;
            public int temporalKernel;
            public int copyHistoryKernel;
            public int finalBlitKernel;
            public LocalKeywordSet keywords;
            public RTHandle blueNoiseRTHandle;

            public void Init(ComputeShader cs)
            {
                shader = cs;
                if (shader == null)
                    return;

                gtaoKernel = shader.FindKernel("GTAOCompute");
                blurHKernel = shader.FindKernel("BilateralBlurH");
                blurVKernel = shader.FindKernel("BilateralBlurV");
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

        private struct GTAOComputeParams
        {
            public Vector4 ssaoParams;
            public Vector4 ssaoParams2;
            public Vector4 depthToViewParams;
            public Vector4 sourceSize;
            public Vector4 blueNoiseParams;
            public Vector4 temporalParams;
            public Vector4 projectionParams2;
            public bool orthographicCamera;
            public float temporalRotation;
            public int temporalOffset;
        }

        private struct TemporalHistoryState
        {
            public TextureHandle historyTexture;
            public SSAOHistory ssaoHistory;
            public bool historyReady;
            public bool isNewFrame;
        }

        // Pass Data Classes
        private class GTAOComputePassData
        {
            public ComputeShader cs;
            public int kernel;
            public Vector2Int dispatchSize;
            public bool useBlueNoise;
            public LocalKeywordSet localKeywords;
            public bool temporalEnabled;
            public int directionCount;
            public int stepCount;
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
        }

        private class GTAOSingleDispatchPassData
        {
            public ComputeShader cs;
            public int kernel;
            public int dstPropertyId;
            public Vector2Int dispatchSize;
            public TextureHandle srcTexture;
            public TextureHandle dstTexture;
        }

        private class GTAOTemporalPassData
        {
            public ComputeShader cs;
            public int kernel;
            public Vector2Int dispatchSize;
            public TextureHandle aoTexture;
            public TextureHandle historyTexture;
            public TextureHandle motionVectorTexture;
            public TextureHandle temporalTexture;
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
            public TextureHandle sourceTexture;
            public TextureHandle targetTexture;
        }

        internal GTAOPass(Shader shader, Texture2D[] blueNoiseTextures)
        {
            m_CurrentSettings = new ScreenSpaceAmbientOcclusionSettings();
            m_Material = CoreUtils.CreateEngineMaterial(shader);
            m_BlueNoiseTextures = blueNoiseTextures;

            if (GraphicsSettings.TryGetRenderPipelineSettings<ScreenSpaceAmbientOcclusionCoreResources>(out var ssaoCoreResources))
                m_ComputePathState.Init(ssaoCoreResources.GTAOComputeShader);
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
            settings.GTAOMaxRadiusPixels = volume.maximumRadiusInPixels;
            settings.Source = ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals;
            settings.BlurQuality = ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.High;
            settings.NormalSamples = ScreenSpaceAmbientOcclusionSettings.NormalQuality.High;

            if (volume.useComputeShader)
            {
                settings.GTAODirectionCount = volume.directionCount;
                settings.GTAOStepCount = volume.stepCount;
                settings.GTAOTemporalFilterEnabled = volume.temporalFilter;
                settings.GTAOTemporalScale = volume.temporalScale;
                settings.GTAOTemporalResponse = volume.temporalResponse;
            }
        }

        // ---- GTAO-specific helpers ----

        internal static void CalculateGTAOViewParams(ScreenSpaceAmbientOcclusionSettings settings, UniversalCameraData cameraData,
            bool orthographicCamera, float radius, in TextureDesc cameraColorDesc, out Vector4 ssaoParams2, out Vector4 depthToViewParams)
        {
            if (settings.Mode == ScreenSpaceAmbientOcclusionMode.Standard)
            {
                ssaoParams2 = Vector4.zero;
                depthToViewParams = Vector4.zero;
                return;
            }

            float invHalfTanFOV = cameraData.camera.projectionMatrix.m11;
            int downsampleDivider = settings.Downsample ? 2 : 1;
            Vector2 runningRes = new Vector2(cameraColorDesc.width / (float)downsampleDivider, cameraColorDesc.height / (float)downsampleDivider);
            float aspectRatio = runningRes.y / runningRes.x;
            float fovCorrection = orthographicCamera
                ? runningRes.y * invHalfTanFOV * 0.5f   // if orthographic, m11 = (1 / orthoSize). So pixelsPerWorldUnit = resY / (2 * orthoSize) = resY * m11 * 0.5.
                : runningRes.y * invHalfTanFOV * 0.25f;

            float scaleFactor = (runningRes.x * runningRes.y) / k_GTAOMaxRadiusReferencePixelCount;
            float radInPixels = Mathf.Max(16, settings.GTAOMaxRadiusPixels * Mathf.Sqrt(scaleFactor));

            ssaoParams2 = new Vector4(radInPixels, 1.0f / (radius * radius), fovCorrection, 0.0f);
            depthToViewParams = new Vector4(
                2.0f / (invHalfTanFOV * aspectRatio * runningRes.x),
                2.0f / (invHalfTanFOV * runningRes.y),
                1.0f / (invHalfTanFOV * aspectRatio),
                1.0f / invHalfTanFOV
            );
        }

        private static void CalculateTemporalParams(ScreenSpaceAmbientOcclusionSettings settings, out Vector4 temporalParams, out float temporalRotation, out int temporalOffset)
        {
            temporalParams = new Vector4(settings.GTAOTemporalScale, settings.GTAOTemporalResponse, 0.0f, 0.0f);
            temporalRotation = 0.0f;
            temporalOffset = 0;

            if (!settings.IsTemporalFilterActive)
                return;

            uint temporalRotationCount = (uint)s_TemporalRotations.Length;
            temporalRotation = s_TemporalRotations[(uint)Time.frameCount % temporalRotationCount] / k_DegreesPerRotation;
            temporalOffset = (int)(((uint)Time.frameCount / temporalRotationCount) % k_TemporalOffsetCount);
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

            CalculateTemporalParams(settings, out computeParams.temporalParams, out computeParams.temporalRotation, out computeParams.temporalOffset);
            return computeParams;
        }

        private static TemporalHistoryState GetTemporalHistoryState(RenderGraph renderGraph, ScreenSpaceAmbientOcclusionSettings settings, UniversalCameraData cameraData, bool supportsR8RenderTextureFormat, bool useComputeShader)
        {
            var state = new TemporalHistoryState { historyTexture = TextureHandle.nullHandle };

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
            bool wasReallocated = state.ssaoHistory.Update(cameraData, settings.Downsample, supportsR8RenderTextureFormat, useComputeShader, xrMultipassEnabled);

            int multipassId = 0;
#if ENABLE_VR && ENABLE_XR_MODULE
            multipassId = cameraData.xr.multipassId;
#endif
            int accumulationVersion = state.ssaoHistory.GetAccumulationVersion(multipassId);
            bool isPreview = cameraData.camera.cameraType == CameraType.Preview;
            bool isRenderRequest = cameraData.camera.isProcessingRenderRequest;
            state.isNewFrame = !isPreview && !isRenderRequest && accumulationVersion != Time.frameCount;
            RTHandle accumulationTexture = state.ssaoHistory.GetAccumulationTexture(multipassId);
            if (accumulationTexture != null)
            {
                state.historyTexture = renderGraph.ImportTexture(accumulationTexture);
                state.historyReady = !wasReallocated && state.isNewFrame && accumulationVersion >= 0;
            }

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

            // Builds a TextureDesc compatible with the active camera color target, safe for both intermediate render textures and imported back-buffer handles.
            TextureDesc cameraColorDesc = SSAOUtils.GetCameraColorDescriptor(renderGraph, resourceData.activeColorTexture, cameraData.camera.allowDynamicResolution);

            bool useComputeShader = (m_CurrentSettings.NeedsComputeShader && m_SupportsComputeShader) && m_ComputePathState.shader != null && m_ComputePathState.gtaoKernel >= 0;

            SSAOUtils.CreateRenderTextureHandles(renderGraph, resourceData, cameraColorDesc,
                m_CurrentSettings, m_SupportsR8RenderTextureFormat, m_BlurType, enableRandomWrite: useComputeShader,
                out TextureHandle aoTexture, out TextureHandle blurTexture, out TextureHandle temporalTexture, out TextureHandle finalTexture);

            var temporalHistory = GetTemporalHistoryState(renderGraph, m_CurrentSettings, cameraData, m_SupportsR8RenderTextureFormat, useComputeShader);

            SSAOUtils.SetupCameraViewMatrices(cameraData, ref m_CameraViewData);
            m_BlueNoiseTextureIndex = SSAOUtils.AdvanceBlueNoiseIndex(m_CurrentSettings, m_BlueNoiseTextures, m_BlueNoiseTextureIndex);

            if (useComputeShader)
            {
                RecordGTAOComputePass(renderGraph, aoTexture, blurTexture, temporalTexture, finalTexture, temporalHistory, cameraData, resourceData, cameraColorDesc);
            }
            else
            {
                // GTAO raster-fragment fallback - reuse shared raster recording
                TextureHandle cameraDepthTexture = resourceData.cameraDepthTexture;
                TextureHandle cameraNormalsTexture = resourceData.cameraNormalsTexture;

                SSAOUtils.SetupKeywordsAndParameters(m_Material, m_CurrentSettings, cameraData, cameraColorDesc, ref m_CameraViewData, m_BlueNoiseTextures, m_BlueNoiseTextureIndex, ref m_SSAOParamsPrev);
                SSAOUtils.RecordRasterAOPass(renderGraph, m_ProfilingSampler, m_Material, m_CurrentSettings, aoTexture, cameraDepthTexture, cameraNormalsTexture, cameraColorDesc);
                SSAOUtils.RecordBlurChain(renderGraph, m_ProfilingSampler, m_Material, m_CurrentSettings, cameraData, aoTexture, blurTexture, finalTexture);

                if (!m_CurrentSettings.AfterOpaque)
                    SSAOUtils.RecordCleanupPass(renderGraph, m_ProfilingSampler, m_CurrentSettings.DirectLightingStrength, finalTexture);
            }
        }

        // ---- Compute Pass Recording ----

        private void RecordGTAOComputePass(RenderGraph renderGraph, TextureHandle aoTexture, TextureHandle blurTexture, TextureHandle temporalTexture, TextureHandle finalTexture,
            in TemporalHistoryState temporalHistory, UniversalCameraData cameraData, UniversalResourceData resourceData, in TextureDesc cameraColorDesc)
        {
            TextureHandle cameraDepthTexture = resourceData.cameraDepthTexture;
            TextureHandle cameraNormalsTexture = resourceData.cameraNormalsTexture;
            TextureHandle motionVectorTexture = resourceData.motionVectorColor;
            bool temporalEnabled = m_CurrentSettings.IsTemporalFilterActive;
            bool isTemporalTextureValid = temporalTexture.IsValid();
            bool isHistoryTextureValid = temporalHistory.historyTexture.IsValid();
            bool shouldWriteHistory = temporalEnabled && temporalHistory.isNewFrame && isHistoryTextureValid;

            TextureDesc aoDesc = aoTexture.GetDescriptor(renderGraph);
            Vector2Int aoSize = new Vector2Int(aoDesc.width, aoDesc.height);
            Vector2Int aoDispatchSize = CalculateDispatchSize(aoSize, m_ComputePathState.shader, m_ComputePathState.gtaoKernel);

            Texture2D blueNoiseTexture = SSAOUtils.GetBlueNoiseTexture(m_BlueNoiseTextures, m_BlueNoiseTextureIndex);
            if (m_CurrentSettings.AOMethod == ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise)
                Debug.Assert(blueNoiseTexture != null, "Blue noise texture is null. Blue noise mode requires a valid blue noise texture.");
            TextureHandle blueNoiseHandle = ImportBlueNoiseTexture(renderGraph, blueNoiseTexture ?? Texture2D.blackTexture);

            var computeParams = CreateGTAOComputeParams(m_CurrentSettings, cameraData, cameraColorDesc, blueNoiseTexture);

            // GTAO
            RecordGTAOMainPass(renderGraph, cameraData, computeParams, blueNoiseHandle, cameraDepthTexture, cameraNormalsTexture, aoTexture, aoDispatchSize);

            // Spatial filter (BlurH, BlurV)
            if (blurTexture.IsValid())
            {
                RecordComputeDispatchPass(renderGraph, "GTAO BlurH", m_ComputePathState.blurHKernel, aoDispatchSize, aoTexture, blurTexture, ShaderIDs._BlurOutput);
                RecordComputeDispatchPass(renderGraph, "GTAO BlurV", m_ComputePathState.blurVKernel, aoDispatchSize, blurTexture, aoTexture, ShaderIDs._BlurOutput);
            }

            // Temporal filter and history update
            if (isHistoryTextureValid)
            {
                if (temporalEnabled && temporalHistory.historyReady && isTemporalTextureValid)
                    RecordTemporalFilterPass(renderGraph, aoDispatchSize, aoTexture, temporalHistory.historyTexture, motionVectorTexture, temporalTexture);

                if (shouldWriteHistory)
                {
                    TextureHandle historySource = (temporalHistory.historyReady && isTemporalTextureValid) ? temporalTexture : aoTexture;
                    RecordComputeDispatchPass(renderGraph, "GTAO CopyHistory", m_ComputePathState.copyHistoryKernel, aoDispatchSize, historySource, temporalHistory.historyTexture, ShaderIDs._HistoryOutput);
                }
            }

            // Determine the final source for downstream passes
            TextureHandle resultTexture = (temporalEnabled && temporalHistory.historyReady && isTemporalTextureValid) ? temporalTexture : aoTexture;

            // Final / AfterOpaque blit
            if (m_CurrentSettings.AfterOpaque)
            {
                RecordAfterOpaqueBlitPass(renderGraph, computeParams, resultTexture, resourceData.activeColorTexture);
            }
            else if (finalTexture.IsValid())
            {
                TextureDesc finalDesc = finalTexture.GetDescriptor(renderGraph);
                Vector2Int finalSize = new Vector2Int(finalDesc.width, finalDesc.height);
                Vector2Int finalDispatchSize = CalculateDispatchSize(finalSize, m_ComputePathState.shader, m_ComputePathState.finalBlitKernel);
                RecordFinalBlitPass(renderGraph, resultTexture, finalTexture, m_CurrentSettings.DirectLightingStrength, finalDispatchSize);
            }

            if (shouldWriteHistory && temporalHistory.ssaoHistory != null)
            {
                int multipassId = 0;
#if ENABLE_VR && ENABLE_XR_MODULE
                multipassId = cameraData.xr.multipassId;
#endif
                temporalHistory.ssaoHistory.SetAccumulationVersion(multipassId, Time.frameCount);
            }
        }

        private void RecordGTAOMainPass(RenderGraph renderGraph, UniversalCameraData cameraData, GTAOComputeParams computeParams,
            TextureHandle blueNoiseTexture, TextureHandle depthTexture, TextureHandle normalsTexture, TextureHandle aoTexture, Vector2Int dispatchSize)
        {
            using (var builder = renderGraph.AddComputePass<GTAOComputePassData>("GTAO Compute", out var passData, m_ProfilingSampler))
            {
                passData.cs = m_ComputePathState.shader;
                passData.kernel = m_ComputePathState.gtaoKernel;
                passData.dispatchSize = dispatchSize;
                passData.computeParams = computeParams;
                passData.useBlueNoise = m_CurrentSettings.AOMethod == ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise;
                passData.localKeywords = m_ComputePathState.keywords;
                passData.temporalEnabled = m_CurrentSettings.IsTemporalFilterActive;
                passData.directionCount = m_CurrentSettings.GTAODirectionCount;
                passData.stepCount = m_CurrentSettings.GTAOStepCount;

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

                Debug.Assert(depthTexture.IsValid(), "Camera depth texture is invalid. GTAO compute requires a depth texture.");
                passData.depthTexture = depthTexture;
                builder.UseTexture(depthTexture, AccessFlags.Read);

                Debug.Assert(normalsTexture.IsValid(), "Camera normals texture is invalid. GTAO compute requires a normals texture.");
                passData.normalsTexture = normalsTexture;
                builder.UseTexture(normalsTexture, AccessFlags.Read);

                passData.blueNoiseTexture = blueNoiseTexture;
                builder.UseTexture(blueNoiseTexture, AccessFlags.Read);

                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (GTAOComputePassData data, ComputeGraphContext ctx) =>
                {
                    var cmd = ctx.cmd;
                    var cs = data.cs;
                    ref var computeParams = ref data.computeParams;

                    cmd.SetKeyword(cs, data.localKeywords.blueNoise, data.useBlueNoise);
                    cmd.SetKeyword(cs, data.localKeywords.interleavedGradient, !data.useBlueNoise);
                    cmd.SetKeyword(cs, data.localKeywords.temporalFiltering, data.temporalEnabled);
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

                    cmd.SetComputeVectorParam(cs, SSAOUtils.ShaderConstants._SSAOBlueNoiseParams, computeParams.blueNoiseParams);
                    cmd.SetComputeTextureParam(cs, data.kernel, SSAOUtils.ShaderConstants._BlueNoiseTexture, data.blueNoiseTexture);

                    if (data.temporalEnabled)
                    {
                        cmd.SetComputeVectorParam(cs, ShaderIDs._SSAOTemporalParams, computeParams.temporalParams);
                        cmd.SetComputeFloatParam(cs, ShaderIDs._SSAOTemporalRotation, computeParams.temporalRotation);
                        cmd.SetComputeIntParam(cs, ShaderIDs._SSAOTemporalOffset, computeParams.temporalOffset);
                    }

                    cmd.SetComputeTextureParam(cs, data.kernel, ShaderIDs._CameraDepthTexture, data.depthTexture);
                    cmd.SetComputeTextureParam(cs, data.kernel, SSAOUtils.ShaderConstants._CameraNormalsTexture, data.normalsTexture);
                    cmd.SetComputeTextureParam(cs, data.kernel, ShaderIDs._AOOutput, data.aoTexture);
                    cmd.DispatchCompute(cs, data.kernel, data.dispatchSize.x, data.dispatchSize.y, 1);
                });
            }
        }

        private void RecordComputeDispatchPass(RenderGraph renderGraph, string passName, int kernel, Vector2Int dispatchSize, TextureHandle srcTexture, TextureHandle dstTexture, int dstPropertyId)
        {
            using (var builder = renderGraph.AddComputePass<GTAOSingleDispatchPassData>(passName, out var passData, m_ProfilingSampler))
            {
                passData.cs = m_ComputePathState.shader;
                passData.kernel = kernel;
                passData.dispatchSize = dispatchSize;
                passData.srcTexture = srcTexture;
                passData.dstTexture = dstTexture;
                passData.dstPropertyId = dstPropertyId;

                builder.UseTexture(srcTexture, AccessFlags.Read);
                builder.UseTexture(dstTexture, AccessFlags.Write);

                builder.SetRenderFunc(static (GTAOSingleDispatchPassData data, ComputeGraphContext ctx) =>
                {
                    ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, SSAOUtils.ShaderConstants._BlitTexture, data.srcTexture);
                    ctx.cmd.SetComputeTextureParam(data.cs, data.kernel, data.dstPropertyId, data.dstTexture);
                    ctx.cmd.DispatchCompute(data.cs, data.kernel, data.dispatchSize.x, data.dispatchSize.y, 1);
                });
            }
        }

        private void RecordTemporalFilterPass(RenderGraph renderGraph, Vector2Int dispatchSize, TextureHandle aoTexture, TextureHandle historyTexture, TextureHandle motionVectorTexture, TextureHandle temporalTexture)
        {
            using (var builder = renderGraph.AddComputePass<GTAOTemporalPassData>("GTAO Temporal", out var passData, m_ProfilingSampler))
            {
                passData.cs = m_ComputePathState.shader;
                passData.kernel = m_ComputePathState.temporalKernel;
                passData.dispatchSize = dispatchSize;
                passData.aoTexture = aoTexture;
                passData.historyTexture = historyTexture;
                passData.temporalTexture = temporalTexture;

                builder.UseTexture(aoTexture, AccessFlags.Read);
                builder.UseTexture(historyTexture, AccessFlags.Read);
                builder.UseTexture(temporalTexture, AccessFlags.Write);

                Debug.Assert(motionVectorTexture.IsValid(), "Motion vector texture is invalid. GTAO temporal filter requires a motion vector texture.");
                passData.motionVectorTexture = motionVectorTexture;
                builder.UseTexture(motionVectorTexture, AccessFlags.Read);

                builder.SetRenderFunc(static (GTAOTemporalPassData data, ComputeGraphContext ctx) =>
                {
                    var cmd = ctx.cmd;
                    cmd.SetComputeTextureParam(data.cs, data.kernel, SSAOUtils.ShaderConstants._BlitTexture, data.aoTexture);
                    cmd.SetComputeTextureParam(data.cs, data.kernel, ShaderIDs._SSAOHistoryTexture, data.historyTexture);
                    cmd.SetComputeTextureParam(data.cs, data.kernel, ShaderIDs._MotionVectorTexture, data.motionVectorTexture);
                    cmd.SetComputeTextureParam(data.cs, data.kernel, ShaderIDs._TemporalOutput, data.temporalTexture);
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

        private void RecordAfterOpaqueBlitPass(RenderGraph renderGraph, GTAOComputeParams computeParams, TextureHandle sourceTexture, TextureHandle targetTexture)
        {
            using (var builder = renderGraph.AddRasterRenderPass<GTAOAfterOpaqueBlitPassData>("GTAO AfterOpaque Blit", out var passData, m_ProfilingSampler))
            {
                passData.material = m_Material;
                passData.sourceSize = computeParams.sourceSize;
                passData.ssaoParams = computeParams.ssaoParams;
                passData.sourceTexture = sourceTexture;
                passData.targetTexture = targetTexture;

                builder.UseTexture(passData.sourceTexture, AccessFlags.Read);
                builder.SetRenderAttachment(passData.targetTexture, 0, AccessFlags.Write);

                builder.SetRenderFunc(static (GTAOAfterOpaqueBlitPassData data, RasterGraphContext ctx) =>
                {
                    Vector4 viewScaleBias = SSAOUtils.ComputeScaleBias(data.sourceTexture, SSAOUtils.IsYFlip(ctx, in data.sourceTexture, in data.targetTexture));
                    data.material.SetVector(SSAOUtils.ShaderConstants._SourceSize, data.sourceSize);
                    data.material.SetVector(SSAOUtils.ShaderConstants._SSAOParams, data.ssaoParams);
                    Blitter.BlitTexture(ctx.cmd, data.sourceTexture, viewScaleBias, data.material, (int)SSAOUtils.ShaderPasses.BilateralAfterOpaque);
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
#endif
