using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the ambient occlusion.
    /// </summary>
    [Serializable, VolumeComponentMenu("Lighting/Ambient Occlusion")]
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "Override-Ambient-Occlusion" + Documentation.endURL)]
    public sealed class AmbientOcclusion : VolumeComponentWithQuality
    {
        /// <summary>
        /// Enable ray traced ambient occlusion.
        /// </summary>
        public BoolParameter rayTracing = new BoolParameter(false);

        /// <summary>
        /// Controls the strength of the ambient occlusion effect. Increase this value to produce darker areas.
        /// </summary>
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 4f);
        /// <summary>
        /// Controls how much the ambient occlusion affects direct lighting.
        /// </summary>
        public ClampedFloatParameter directLightingStrength = new ClampedFloatParameter(0f, 0f, 1f);
        /// <summary>
        /// Sampling radius. Bigger the radius, wider AO will be achieved, risking to lose fine details and increasing cost of the effect due to increasing cache misses.
        /// </summary>
        public ClampedFloatParameter radius = new ClampedFloatParameter(2.0f, 0.25f, 5.0f);

        /// <summary>
        /// Moving this factor closer to 0 will increase the amount of accepted samples during temporal accumulation, increasing the ghosting, but reducing the temporal noise.
        /// </summary>
        public ClampedFloatParameter spatialBilateralAggressiveness = new ClampedFloatParameter(0.15f, 0.0f, 1.0f);


        /// <summary>
        /// Whether the results are accumulated over time or not. This can get higher quality results at a cheaper cost, but it can lead to temporal artifacts such as ghosting.
        /// </summary>
        public BoolParameter temporalAccumulation = new BoolParameter(true);

        // Temporal only parameters
        /// <summary>
        /// Moving this factor closer to 0 will increase the amount of accepted samples during temporal accumulation, increasing the ghosting, but reducing the temporal noise.
        /// </summary>
        public ClampedFloatParameter ghostingReduction = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        // Non-temporal only parameters
        /// <summary>
        /// Modify the non-temporal blur to change how sharp features are preserved. Lower values leads to blurrier/softer results, higher values gets a sharper result, but with the risk of noise.
        /// </summary>
        public ClampedFloatParameter blurSharpness = new ClampedFloatParameter(0.1f, 0.0f, 1.0f);

        // Ray tracing parameters
        /// <summary>
        /// Defines the layers that ray traced ambient occlusion should include.
        /// </summary>
        public LayerMaskParameter layerMask = new LayerMaskParameter(-1);

        /// <summary>
        /// Controls the length of ray traced ambient occlusion rays.
        /// </summary>
        public float rayLength
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_RayLength.value;
                else
                    return GetLightingQualitySettings().RTAORayLength[(int)quality.value];
            }
            set { m_RayLength.value = value; }
        }

        /// <summary>
        /// Number of samples for evaluating the effect.
        /// </summary>
        public int sampleCount
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_SampleCount.value;
                else
                    return GetLightingQualitySettings().RTAOSampleCount[(int)quality.value];
            }
            set { m_SampleCount.value = value; }
        }

        /// <summary>
        /// Defines if the ray traced ambient occlusion should be denoised.
        /// </summary>
        public bool denoise
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_Denoise.value;
                else
                    return GetLightingQualitySettings().RTAODenoise[(int)quality.value];
            }
            set { m_Denoise.value = value; }
        }

        /// <summary>
        /// Controls the radius of the ray traced ambient occlusion denoiser.
        /// </summary>
        public float denoiserRadius
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_DenoiserRadius.value;
                else
                    return GetLightingQualitySettings().RTAODenoiserRadius[(int)quality.value];
            }
            set { m_DenoiserRadius.value = value; }
        }

        /// <summary>
        /// Number of steps to take along one signed direction during horizon search (this is the number of steps in positive and negative direction). Increasing the value can lead to detection
        /// of finer details, but is not a guarantee of higher quality otherwise. Also note that increasing this value will lead to higher cost.
        /// </summary>
        public int stepCount
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_StepCount.value;
                else
                    return GetLightingQualitySettings().AOStepCount[(int)quality.value];
            }
            set { m_StepCount.value = value; }
        }

        /// <summary>
        /// If this option is set to true, the effect runs at full resolution. This will increases quality, but also decreases performance significantly.
        /// </summary>
        public bool fullResolution
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_FullResolution.value;
                else
                    return GetLightingQualitySettings().AOFullRes[(int)quality.value];
            }
            set { m_FullResolution.value = value; }
        }

        /// <summary>
        /// This field imposes a maximum radius in pixels that will be considered. It is very important to keep this as tight as possible to preserve good performance.
        /// Note that the pixel value specified for this field is the value used for 1080p when *not* running the effect at full resolution, it will be scaled accordingly
        /// for other resolutions.
        /// </summary>
        public int maximumRadiusInPixels
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_MaximumRadiusInPixels.value;
                else
                    return GetLightingQualitySettings().AOMaximumRadiusPixels[(int)quality.value];
            }
            set { m_MaximumRadiusInPixels.value = value; }
        }

        /// <summary>
        /// This upsample method preserves sharp edges better, however may result in visible aliasing and it is slightly more expensive.
        /// </summary>
        public bool bilateralUpsample
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_BilateralUpsample.value;
                else
                    return GetLightingQualitySettings().AOBilateralUpsample[(int)quality.value];
            }
            set { m_BilateralUpsample.value = value; }
        }

        /// <summary>
        /// Number of directions searched for occlusion at each each pixel when temporal accumulation is disabled.
        /// </summary>
        public int directionCount
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_DirectionCount.value;
                else
                    return GetLightingQualitySettings().AODirectionCount[(int)quality.value];
            }
            set { m_DirectionCount.value = value; }
        }

        // SSAO
        [SerializeField, FormerlySerializedAs("stepCount")]
        private ClampedIntParameter m_StepCount = new ClampedIntParameter(6, 2, 32);
        [SerializeField, FormerlySerializedAs("fullResolution")]
        private BoolParameter m_FullResolution = new BoolParameter(false);
        [SerializeField, FormerlySerializedAs("maximumRadiusInPixels")]
        private ClampedIntParameter m_MaximumRadiusInPixels = new ClampedIntParameter(40, 16, 256);
        // Temporal only parameter
        [SerializeField, FormerlySerializedAs("bilateralUpsample")]
        private BoolParameter m_BilateralUpsample = new BoolParameter(true);
        // Non-temporal only parameters
        [SerializeField, FormerlySerializedAs("directionCount")]
        private ClampedIntParameter m_DirectionCount = new ClampedIntParameter(2, 1, 6);

        // RTAO
        [SerializeField, FormerlySerializedAs("rayLength")]
        private MinFloatParameter m_RayLength = new MinFloatParameter(50.0f, 0.01f);
        [SerializeField, FormerlySerializedAs("sampleCount")]
        private ClampedIntParameter m_SampleCount = new ClampedIntParameter(1, 1, 64);
        [SerializeField, FormerlySerializedAs("denoise")]
        private BoolParameter m_Denoise = new BoolParameter(true);
        [SerializeField, FormerlySerializedAs("denoiserRadius")]
        private ClampedFloatParameter m_DenoiserRadius = new ClampedFloatParameter(1.0f, 0.001f, 1.0f);
    }

    partial class AmbientOcclusionSystem
    {
        RenderPipelineResources m_Resources;
        RenderPipelineSettings m_Settings;

        private bool m_HistoryReady = false;
        private RTHandle m_PackedDataTex;
        private RTHandle m_PackedDataBlurred;
        private RTHandle m_AmbientOcclusionTex;
        private RTHandle m_FinalHalfRes;

        private bool m_RunningFullRes = false;

        readonly HDRaytracingAmbientOcclusion m_RaytracingAmbientOcclusion = new HDRaytracingAmbientOcclusion();

        private void ReleaseRT()
        {
            RTHandles.Release(m_AmbientOcclusionTex);
            RTHandles.Release(m_PackedDataTex);
            RTHandles.Release(m_PackedDataBlurred);
            RTHandles.Release(m_FinalHalfRes);
        }

        void AllocRT(float scaleFactor)
        {
            m_AmbientOcclusionTex = RTHandles.Alloc(Vector2.one, TextureXR.slices, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R8_UNorm, dimension: TextureXR.dimension, useDynamicScale: true, enableRandomWrite: true, name: "Ambient Occlusion");
            m_PackedDataTex = RTHandles.Alloc(Vector2.one * scaleFactor, TextureXR.slices, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32_SFloat, dimension: TextureXR.dimension, useDynamicScale: true, enableRandomWrite: true, name: "AO Packed data");
            m_PackedDataBlurred = RTHandles.Alloc(Vector2.one * scaleFactor, TextureXR.slices, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32_SFloat, dimension: TextureXR.dimension, useDynamicScale: true, enableRandomWrite: true, name: "AO Packed blurred data");

            m_FinalHalfRes = RTHandles.Alloc(Vector2.one * 0.5f, TextureXR.slices, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32_SFloat, dimension: TextureXR.dimension, useDynamicScale: true, enableRandomWrite: true, name: "Final Half Res AO Packed");
        }

        void EnsureRTSize(AmbientOcclusion settings, HDCamera hdCamera)
        {
            float scaleFactor = m_RunningFullRes ? 1.0f : 0.5f;
            if (settings.fullResolution != m_RunningFullRes)
            {
                ReleaseRT();

                m_RunningFullRes = settings.fullResolution;
                scaleFactor = m_RunningFullRes ? 1.0f : 0.5f;
                AllocRT(scaleFactor);
            }

            hdCamera.AllocateAmbientOcclusionHistoryBuffer(scaleFactor);
        }

        internal AmbientOcclusionSystem(HDRenderPipelineAsset hdAsset, RenderPipelineResources defaultResources)
        {
            m_Settings = hdAsset.currentPlatformRenderPipelineSettings;
            m_Resources = defaultResources;

            if (!hdAsset.currentPlatformRenderPipelineSettings.supportSSAO)
                return;
        }

        internal void InitializeNonRenderGraphResources()
        {
            float scaleFactor = m_RunningFullRes ? 1.0f : 0.5f;
            AllocRT(scaleFactor);

            if (HDRenderPipeline.GatherRayTracingSupport(m_Settings))
            {
                m_RaytracingAmbientOcclusion.InitializeNonRenderGraphResources();
            }
        }

        internal void CleanupNonRenderGraphResources()
        {
            ReleaseRT();

            if (HDRenderPipeline.GatherRayTracingSupport(m_Settings))
            {
                m_RaytracingAmbientOcclusion.CleanupNonRenderGraphResources();
            }
        }

        internal void InitRaytracing(HDRenderPipeline renderPipeline)
        {
            m_RaytracingAmbientOcclusion.Init(renderPipeline);
        }

        internal float EvaluateSpecularOcclusionFlag(HDCamera hdCamera)
        {
            AmbientOcclusion ssoSettings = hdCamera.volumeStack.GetComponent<AmbientOcclusion>();
            bool enableRTAO = hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && ssoSettings.rayTracing.value;
            if (enableRTAO)
                return m_RaytracingAmbientOcclusion.EvaluateRTSpecularOcclusionFlag(hdCamera, ssoSettings);
            else
                return 1.0f;
        }

        internal bool IsActive(HDCamera camera, AmbientOcclusion settings) => camera.frameSettings.IsEnabled(FrameSettingsField.SSAO) && settings.intensity.value > 0f;

        internal void Render(CommandBuffer cmd, HDCamera camera, ScriptableRenderContext renderContext, RTHandle depthTexture, RTHandle normalBuffer, RTHandle motionVectors, in ShaderVariablesRaytracing globalRTCB, int frameCount)
        {
            var settings = camera.volumeStack.GetComponent<AmbientOcclusion>();

            if (!IsActive(camera, settings))
            {
                PostDispatchWork(cmd, camera);
                return;
            }
            else
            {
                if (camera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && settings.rayTracing.value)
                    m_RaytracingAmbientOcclusion.RenderRTAO(camera, cmd, m_AmbientOcclusionTex, globalRTCB, renderContext, frameCount);
                else
                {
                    Dispatch(cmd, camera, depthTexture, normalBuffer, motionVectors, frameCount);
                    PostDispatchWork(cmd, camera);
                }
            }
        }

        struct RenderAOParameters
        {
            public ComputeShader    gtaoCS;
            public int              gtaoKernel;
            public ComputeShader    spatialDenoiseAOCS;
            public int              denoiseKernelSpatial;
            public ComputeShader    temporalDenoiseAOCS;
            public int              denoiseKernelTemporal;
            public ComputeShader    copyHistoryAOCS;
            public int              denoiseKernelCopyHistory;
            public ComputeShader    upsampleAndBlurAOCS;
            public int              upsampleAndBlurKernel;
            public int              upsampleAOKernel;

            public Vector2          runningRes;
            public int              viewCount;
            public bool             historyReady;
            public int              outputWidth;
            public int              outputHeight;
            public bool             fullResolution;
            public bool             runAsync;
            public bool             temporalAccumulation;
            public bool             bilateralUpsample;

            public ShaderVariablesAmbientOcclusion cb;
        }

        RenderAOParameters PrepareRenderAOParameters(HDCamera camera, Vector2 historySize, in HDUtils.PackedMipChainInfo depthMipInfo)
        {
            var parameters = new RenderAOParameters();

            ref var cb = ref parameters.cb;

            // Grab current settings
            var settings = camera.volumeStack.GetComponent<AmbientOcclusion>();
            parameters.fullResolution = settings.fullResolution;

            if (parameters.fullResolution)
            {
                parameters.runningRes = new Vector2(camera.actualWidth, camera.actualHeight);
                cb._AOBufferSize = new Vector4(camera.actualWidth, camera.actualHeight, 1.0f / camera.actualWidth, 1.0f / camera.actualHeight);
            }
            else
            {
                parameters.runningRes = new Vector2(camera.actualWidth, camera.actualHeight) * 0.5f;
                cb._AOBufferSize = new Vector4(camera.actualWidth * 0.5f, camera.actualHeight * 0.5f, 2.0f / camera.actualWidth, 2.0f / camera.actualHeight);
            }

            float invHalfTanFOV = -camera.mainViewConstants.projMatrix[1, 1];
            float aspectRatio = parameters.runningRes.y / parameters.runningRes.x;
            uint frameCount = camera.GetCameraFrameCount();

            cb._AOParams0 = new Vector4(
                parameters.fullResolution ? 0.0f : 1.0f,
                parameters.runningRes.y * invHalfTanFOV * 0.25f,
                settings.radius.value,
                settings.stepCount
                );

            cb._AOParams1 = new Vector4(
                settings.intensity.value,
                1.0f / (settings.radius.value * settings.radius.value),
                (frameCount / 6) % 4,
                (frameCount % 6)
                );


            // We start from screen space position, so we bake in this factor the 1 / resolution as well.
            cb._AODepthToViewParams = new Vector4(
                2.0f / (invHalfTanFOV * aspectRatio * parameters.runningRes.x),
                2.0f / (invHalfTanFOV * parameters.runningRes.y),
                1.0f / (invHalfTanFOV * aspectRatio),
                1.0f / invHalfTanFOV
                );

            float scaleFactor = (parameters.runningRes.x * parameters.runningRes.y) / (540.0f * 960.0f);
            float radInPixels = Mathf.Max(16, settings.maximumRadiusInPixels * Mathf.Sqrt(scaleFactor));

            cb._AOParams2 = new Vector4(
                historySize.x,
                historySize.y,
                1.0f / (settings.stepCount + 1.0f),
                radInPixels
            );

            float stepSize = m_RunningFullRes ? 1 : 0.5f;

            float blurTolerance = 1.0f - settings.blurSharpness.value;
            float maxBlurTolerance = 0.25f;
            float minBlurTolerance = -2.5f;
            blurTolerance = minBlurTolerance + (blurTolerance * (maxBlurTolerance - minBlurTolerance));

            float bTolerance = 1f - Mathf.Pow(10f, blurTolerance) * stepSize;
            bTolerance *= bTolerance;
            const float upsampleTolerance = -7.0f; // TODO: Expose?
            float uTolerance = Mathf.Pow(10f, upsampleTolerance);
            float noiseFilterWeight = 1f / (Mathf.Pow(10f, 0.0f) + uTolerance);

            cb._AOParams3 = new Vector4(
                bTolerance,
                uTolerance,
                noiseFilterWeight,
                stepSize
            );

            float upperNudgeFactor = 1.0f - settings.ghostingReduction.value;
            const float maxUpperNudgeLimit = 5.0f;
            const float minUpperNudgeLimit = 0.25f;
            upperNudgeFactor = minUpperNudgeLimit + (upperNudgeFactor * (maxUpperNudgeLimit - minUpperNudgeLimit));
            cb._AOParams4 = new Vector4(
                settings.directionCount,
                upperNudgeFactor,
                minUpperNudgeLimit,
                settings.spatialBilateralAggressiveness.value * 15.0f
            );

            cb._FirstTwoDepthMipOffsets = new Vector4(depthMipInfo.mipLevelOffsets[1].x, depthMipInfo.mipLevelOffsets[1].y, depthMipInfo.mipLevelOffsets[2].x, depthMipInfo.mipLevelOffsets[2].y);

            parameters.bilateralUpsample = settings.bilateralUpsample;
            parameters.gtaoCS = m_Resources.shaders.GTAOCS;
            parameters.gtaoCS.shaderKeywords = null;
            parameters.temporalAccumulation = settings.temporalAccumulation.value && camera.frameSettings.IsEnabled(FrameSettingsField.MotionVectors);

            if (parameters.temporalAccumulation)
            {
                parameters.gtaoCS.EnableKeyword("TEMPORAL");
            }
            if(parameters.fullResolution)
            {
                parameters.gtaoCS.EnableKeyword("FULL_RES");
            }
            else
            {
                parameters.gtaoCS.EnableKeyword("HALF_RES");
            }

            parameters.gtaoKernel = parameters.gtaoCS.FindKernel("GTAOMain");

            parameters.upsampleAndBlurAOCS = m_Resources.shaders.GTAOBlurAndUpsample;

            parameters.spatialDenoiseAOCS = m_Resources.shaders.GTAOSpatialDenoiseCS;
            parameters.spatialDenoiseAOCS.shaderKeywords = null;
            if (parameters.temporalAccumulation)
                parameters.spatialDenoiseAOCS.EnableKeyword("TO_TEMPORAL");
            parameters.denoiseKernelSpatial = parameters.spatialDenoiseAOCS.FindKernel("SpatialDenoise");

            parameters.temporalDenoiseAOCS = m_Resources.shaders.GTAOTemporalDenoiseCS;
            parameters.temporalDenoiseAOCS.shaderKeywords = null;
            if (parameters.fullResolution)
                parameters.temporalDenoiseAOCS.EnableKeyword("FULL_RES");
            else
                parameters.temporalDenoiseAOCS.EnableKeyword("HALF_RES");

            parameters.denoiseKernelTemporal = parameters.temporalDenoiseAOCS.FindKernel("TemporalDenoise");

            parameters.copyHistoryAOCS = m_Resources.shaders.GTAOCopyHistoryCS;
            parameters.denoiseKernelCopyHistory = parameters.copyHistoryAOCS.FindKernel("GTAODenoise_CopyHistory");

            parameters.upsampleAndBlurKernel = parameters.upsampleAndBlurAOCS.FindKernel("BlurUpsample");
            parameters.upsampleAOKernel = parameters.upsampleAndBlurAOCS.FindKernel(settings.bilateralUpsample ? "BilateralUpsampling" : "BoxUpsampling");

            parameters.outputWidth = camera.actualWidth;
            parameters.outputHeight = camera.actualHeight;

            parameters.viewCount = camera.viewCount;
            parameters.historyReady = m_HistoryReady;
            m_HistoryReady = true; // assumes that if this is called, then render is done as well.

            parameters.runAsync = camera.frameSettings.SSAORunsAsync();

            return parameters;
        }

        static void RenderAO(in RenderAOParameters      parameters,
                                RTHandle                packedDataTexture,
                                RTHandle                depthTexture,
                                RTHandle                normalBuffer,
                                CommandBuffer           cmd)
        {
            ConstantBuffer.Push(cmd, parameters.cb, parameters.gtaoCS, HDShaderIDs._ShaderVariablesAmbientOcclusion);
            cmd.SetComputeTextureParam(parameters.gtaoCS, parameters.gtaoKernel, HDShaderIDs._AOPackedData, packedDataTexture);
            cmd.SetComputeTextureParam(parameters.gtaoCS, parameters.gtaoKernel, HDShaderIDs._NormalBufferTexture, normalBuffer);
            cmd.SetComputeTextureParam(parameters.gtaoCS, parameters.gtaoKernel, HDShaderIDs._CameraDepthTexture, depthTexture);

            const int groupSizeX = 8;
            const int groupSizeY = 8;
            int threadGroupX = ((int)parameters.runningRes.x + (groupSizeX - 1)) / groupSizeX;
            int threadGroupY = ((int)parameters.runningRes.y + (groupSizeY - 1)) / groupSizeY;

            cmd.DispatchCompute(parameters.gtaoCS, parameters.gtaoKernel, threadGroupX, threadGroupY, parameters.viewCount);
        }

        static void DenoiseAO(  in RenderAOParameters   parameters,
                                RTHandle                packedDataTex,
                                RTHandle                packedDataBlurredTex,
                                RTHandle                packedHistoryTex,
                                RTHandle                packedHistoryOutputTex,
                                RTHandle                motionVectors,
                                RTHandle                aoOutputTex,
                                CommandBuffer           cmd)
        {
            const int groupSizeX = 8;
            const int groupSizeY = 8;
            int threadGroupX = ((int)parameters.runningRes.x + (groupSizeX - 1)) / groupSizeX;
            int threadGroupY = ((int)parameters.runningRes.y + (groupSizeY - 1)) / groupSizeY;

            if (parameters.temporalAccumulation || parameters.fullResolution)
            {
                var blurCS = parameters.spatialDenoiseAOCS;
                ConstantBuffer.Set<ShaderVariablesAmbientOcclusion>(cmd, blurCS, HDShaderIDs._ShaderVariablesAmbientOcclusion);

                // Spatial
                cmd.SetComputeTextureParam(blurCS, parameters.denoiseKernelSpatial, HDShaderIDs._AOPackedData, packedDataTex);
                if (parameters.temporalAccumulation)
                {
                    cmd.SetComputeTextureParam(blurCS, parameters.denoiseKernelSpatial, HDShaderIDs._AOPackedBlurred, packedDataBlurredTex);
                }
                else
                {
                    cmd.SetComputeTextureParam(blurCS, parameters.denoiseKernelSpatial, HDShaderIDs._OcclusionTexture, aoOutputTex);
                }

                cmd.DispatchCompute(blurCS, parameters.denoiseKernelSpatial, threadGroupX, threadGroupY, parameters.viewCount);
            }

            if (parameters.temporalAccumulation)
            {
                if (!parameters.historyReady)
                {
                    cmd.SetComputeTextureParam(parameters.copyHistoryAOCS, parameters.denoiseKernelCopyHistory, HDShaderIDs._InputTexture, packedDataTex);
                    cmd.SetComputeTextureParam(parameters.copyHistoryAOCS, parameters.denoiseKernelCopyHistory, HDShaderIDs._OutputTexture, packedHistoryTex);
                    cmd.DispatchCompute(parameters.copyHistoryAOCS, parameters.denoiseKernelCopyHistory, threadGroupX, threadGroupY, parameters.viewCount);
                }

                var blurCS = parameters.temporalDenoiseAOCS;
                ConstantBuffer.Set<ShaderVariablesAmbientOcclusion>(cmd, blurCS, HDShaderIDs._ShaderVariablesAmbientOcclusion);

                // Temporal
                cmd.SetComputeTextureParam(blurCS, parameters.denoiseKernelTemporal, HDShaderIDs._AOPackedData, packedDataTex);
                cmd.SetComputeTextureParam(blurCS, parameters.denoiseKernelTemporal, HDShaderIDs._AOPackedBlurred, packedDataBlurredTex);
                cmd.SetComputeTextureParam(blurCS, parameters.denoiseKernelTemporal, HDShaderIDs._AOPackedHistory, packedHistoryTex);
                cmd.SetComputeTextureParam(blurCS, parameters.denoiseKernelTemporal, HDShaderIDs._AOOutputHistory, packedHistoryOutputTex);
                cmd.SetComputeTextureParam(blurCS, parameters.denoiseKernelTemporal, HDShaderIDs._CameraMotionVectorsTexture, motionVectors);
                cmd.SetComputeTextureParam(blurCS, parameters.denoiseKernelTemporal, HDShaderIDs._OcclusionTexture, aoOutputTex);
                cmd.DispatchCompute(blurCS, parameters.denoiseKernelTemporal, threadGroupX, threadGroupY, parameters.viewCount);
            }
        }

        static void UpsampleAO( in RenderAOParameters   parameters,
                                RTHandle                depthTexture,
                                RTHandle                input,
                                RTHandle                output,
                                CommandBuffer           cmd)
        {
            bool blurAndUpsample = !parameters.temporalAccumulation;

            ConstantBuffer.Set<ShaderVariablesAmbientOcclusion>(cmd, parameters.upsampleAndBlurAOCS, HDShaderIDs._ShaderVariablesAmbientOcclusion);

            if (blurAndUpsample)
            {
                cmd.SetComputeTextureParam(parameters.upsampleAndBlurAOCS, parameters.upsampleAndBlurKernel, HDShaderIDs._AOPackedData, input);
                cmd.SetComputeTextureParam(parameters.upsampleAndBlurAOCS, parameters.upsampleAndBlurKernel, HDShaderIDs._OcclusionTexture, output);
                cmd.SetComputeTextureParam(parameters.upsampleAndBlurAOCS, parameters.upsampleAndBlurKernel, HDShaderIDs._CameraDepthTexture, depthTexture);

                const int groupSizeX = 8;
                const int groupSizeY = 8;
                int threadGroupX = ((int)(parameters.runningRes.x) + (groupSizeX - 1)) / groupSizeX;
                int threadGroupY = ((int)(parameters.runningRes.y) + (groupSizeY - 1)) / groupSizeY;
                cmd.DispatchCompute(parameters.upsampleAndBlurAOCS, parameters.upsampleAndBlurKernel, threadGroupX, threadGroupY, parameters.viewCount);

            }
            else
            {
                cmd.SetComputeTextureParam(parameters.upsampleAndBlurAOCS, parameters.upsampleAOKernel, HDShaderIDs._AOPackedData, input);
                cmd.SetComputeTextureParam(parameters.upsampleAndBlurAOCS, parameters.upsampleAOKernel, HDShaderIDs._OcclusionTexture, output);
                cmd.SetComputeTextureParam(parameters.upsampleAndBlurAOCS, parameters.upsampleAOKernel, HDShaderIDs._CameraDepthTexture, depthTexture);

                const int groupSizeX = 8;
                const int groupSizeY = 8;
                int threadGroupX = ((int)parameters.runningRes.x + (groupSizeX - 1)) / groupSizeX;
                int threadGroupY = ((int)parameters.runningRes.y + (groupSizeY - 1)) / groupSizeY;
                cmd.DispatchCompute(parameters.upsampleAndBlurAOCS, parameters.upsampleAOKernel, threadGroupX, threadGroupY, parameters.viewCount);
            }
        }

        internal void Dispatch(CommandBuffer cmd, HDCamera camera, RTHandle depthTexture, RTHandle normalBuffer, RTHandle motionVectors, int frameCount)
        {
            var settings = camera.volumeStack.GetComponent<AmbientOcclusion>();
            if (IsActive(camera, settings))
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderSSAO)))
                {
                    EnsureRTSize(settings, camera);

                    var currentHistory = camera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.AmbientOcclusion);
                    var historyOutput = camera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.AmbientOcclusion);

                    Vector2 historySize = new Vector2(currentHistory.referenceSize.x * currentHistory.scaleFactor.x,
                                                      currentHistory.referenceSize.y * currentHistory.scaleFactor.y);
                    var rtScaleForHistory = camera.historyRTHandleProperties.rtHandleScale;

                    var hdrp = (RenderPipelineManager.currentPipeline as HDRenderPipeline);
                    var aoParameters = PrepareRenderAOParameters(camera, historySize * rtScaleForHistory, hdrp.sharedRTManager.GetDepthBufferMipChainInfo());
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.HorizonSSAO)))
                    {
                        RenderAO(aoParameters, m_PackedDataTex, depthTexture, normalBuffer, cmd);
                    }

                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DenoiseSSAO)))
                    {
                        var output = m_RunningFullRes ? m_AmbientOcclusionTex : m_FinalHalfRes;
                        DenoiseAO(aoParameters, m_PackedDataTex, m_PackedDataBlurred, currentHistory, historyOutput, motionVectors, output, cmd);
                    }

                    if (!m_RunningFullRes)
                    {
                        using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.UpSampleSSAO)))
                        {
                            UpsampleAO(aoParameters, depthTexture, aoParameters.temporalAccumulation ? m_FinalHalfRes : m_PackedDataTex, m_AmbientOcclusionTex, cmd);
                        }
                    }
                }
            }
        }

        internal void UpdateShaderVariableGlobalCB(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            var settings = hdCamera.volumeStack.GetComponent<AmbientOcclusion>();
            bool aoEnabled = false;
            if (IsActive(hdCamera, settings))
            {
                aoEnabled = true;
                // If raytraced AO is enabled but raytracing state is wrong then we disable it.
                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && settings.rayTracing.value && !HDRenderPipeline.currentPipeline.GetRayTracingState())
                {
                    aoEnabled = false;
                }
            }

            if (aoEnabled)
                cb._AmbientOcclusionParam = new Vector4(0f, 0f, 0f, settings.directLightingStrength.value);
            else
                cb._AmbientOcclusionParam = Vector4.zero;
        }

        internal void PostDispatchWork(CommandBuffer cmd, HDCamera camera)
        {
            var settings = camera.volumeStack.GetComponent<AmbientOcclusion>();
            var aoTexture = IsActive(camera, settings) ? m_AmbientOcclusionTex : TextureXR.GetBlackTexture();
            cmd.SetGlobalTexture(HDShaderIDs._AmbientOcclusionTexture, aoTexture);
            // TODO: All the push debug stuff should be centralized somewhere
            (RenderPipelineManager.currentPipeline as HDRenderPipeline).PushFullScreenDebugTexture(camera, cmd, aoTexture, FullScreenDebugMode.ScreenSpaceAmbientOcclusion);
        }
    }
}
