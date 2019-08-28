using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable, VolumeComponentMenu("Lighting/Ambient Occlusion")]
    public sealed class AmbientOcclusion : VolumeComponent
    {
        public BoolParameter rayTracing = new BoolParameter(false);

        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 4f);
        public ClampedFloatParameter directLightingStrength = new ClampedFloatParameter(0f, 0f, 1f);

        public ClampedIntParameter stepCount = new ClampedIntParameter(6, 2, 32);
        public ClampedFloatParameter radius = new ClampedFloatParameter(2.0f, 0.25f, 5.0f);
        public BoolParameter fullResolution = new BoolParameter(false);
        public ClampedIntParameter maximumRadiusInPixels = new ClampedIntParameter(40, 16, 256);

        public ClampedFloatParameter rayLength = new ClampedFloatParameter(0.5f, 0f, 50f);
        public ClampedIntParameter sampleCount = new ClampedIntParameter(4, 1, 64);
        public BoolParameter denoise = new BoolParameter(false);
        public ClampedFloatParameter denoiserRadius = new ClampedFloatParameter(0.5f, 0.001f, 1.0f);
    }

    partial class AmbientOcclusionSystem
    {
        RenderPipelineResources m_Resources;
#if ENABLE_RAYTRACING
        HDRenderPipelineRayTracingResources m_RTResources;
#endif
        RenderPipelineSettings m_Settings;

        private bool m_HistoryReady = false;
        private RTHandle m_PackedDataTex;
        private RTHandle m_PackedDataBlurred;
        private RTHandle m_AmbientOcclusionTex;
        private RTHandle m_FinalHalfRes;

        private bool m_RunningFullRes = false;

#if ENABLE_RAYTRACING
        public HDRaytracingManager m_RayTracingManager;
        readonly HDRaytracingAmbientOcclusion m_RaytracingAmbientOcclusion = new HDRaytracingAmbientOcclusion();
#endif

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
            m_PackedDataTex = RTHandles.Alloc(Vector2.one * scaleFactor, TextureXR.slices, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32_UInt, dimension: TextureXR.dimension, useDynamicScale: true, enableRandomWrite: true, name: "AO Packed data");
            m_PackedDataBlurred = RTHandles.Alloc(Vector2.one * scaleFactor, TextureXR.slices, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32_UInt, dimension: TextureXR.dimension, useDynamicScale: true, enableRandomWrite: true, name: "AO Packed blurred data");

            m_FinalHalfRes = RTHandles.Alloc(Vector2.one * 0.5f, TextureXR.slices, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32_UInt, dimension: TextureXR.dimension, useDynamicScale: true, enableRandomWrite: true, name: "Final Half Res AO Packed");
        }

        void EnsureRTSize(AmbientOcclusion settings, HDCamera hdCamera)
        {
            float scaleFactor = m_RunningFullRes ? 1.0f : 0.5f;
            if (settings.fullResolution != m_RunningFullRes)
            {
                ReleaseRT();

                m_RunningFullRes = settings.fullResolution.value;
                scaleFactor = m_RunningFullRes ? 1.0f : 0.5f;
                AllocRT(scaleFactor);
            }

            hdCamera.AllocateAmbientOcclusionHistoryBuffer(scaleFactor);
        }

        public AmbientOcclusionSystem(HDRenderPipelineAsset hdAsset, RenderPipelineResources defaultResources)
        {
            m_Settings = hdAsset.currentPlatformRenderPipelineSettings;
            m_Resources = defaultResources;
#if ENABLE_RAYTRACING
            m_RTResources = hdAsset.renderPipelineRayTracingResources;
#endif

            if (!hdAsset.currentPlatformRenderPipelineSettings.supportSSAO)
                return;

            AllocRT(0.5f);
        }

        public void Cleanup()
        {
#if ENABLE_RAYTRACING
            m_RaytracingAmbientOcclusion.Release();
#endif

            ReleaseRT();
        }

#if ENABLE_RAYTRACING
        public void InitRaytracing(HDRaytracingManager raytracingManager, SharedRTManager sharedRTManager)
        {
            m_RayTracingManager = raytracingManager;
            m_RaytracingAmbientOcclusion.Init(m_Resources, m_RTResources, m_Settings, m_RayTracingManager, sharedRTManager);
        }
#endif

        public bool IsActive(HDCamera camera, AmbientOcclusion settings) => camera.frameSettings.IsEnabled(FrameSettingsField.SSAO) && settings.intensity.value > 0f;

        public void Render(CommandBuffer cmd, HDCamera camera, ScriptableRenderContext renderContext, int frameCount)
        {

            var settings = VolumeManager.instance.stack.GetComponent<AmbientOcclusion>();

            if (!IsActive(camera, settings))
            {
                // No AO applied - neutral is black, see the comment in the shaders
                cmd.SetGlobalTexture(HDShaderIDs._AmbientOcclusionTexture, TextureXR.GetBlackTexture());
                return;
            }
            else
            {
#if ENABLE_RAYTRACING
                HDRaytracingEnvironment rtEnvironement = m_RayTracingManager.CurrentEnvironment();
                if (camera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && rtEnvironement != null && settings.rayTracing.value)
                    m_RaytracingAmbientOcclusion.RenderAO(camera, cmd, m_AmbientOcclusionTex, renderContext, frameCount);
                else
#endif
                {
                    Dispatch(cmd, camera, frameCount);
                    PostDispatchWork(cmd, camera);
                }
            }
        }

        struct RenderAOParameters
        {
            public ComputeShader    gtaoCS;
            public int              gtaoKernel;
            public ComputeShader    denoiseAOCS;
            public int              denoiseKernelSpatial;
            public int              denoiseKernelTemporal;
            public int              denoiseKernelCopyHistory;
            public ComputeShader    upsampleAOCS;
            public int              upsampleAOKernel;
            public Vector4          aoParams0;
            public Vector4          aoParams1;
            public Vector4          aoParams2;
            public Vector4          aoBufferInfo;
            public Vector4          toViewSpaceProj;
            public Vector2          runningRes;
            public int              viewCount;
            public bool             historyReady;
            public int              outputWidth;
            public int              outputHeight;
            public bool             fullResolution;
            public bool             runAsync;
        }

        RenderAOParameters PrepareRenderAOParameters(HDCamera camera, RTHandleProperties rtHandleProperties, int frameCount)
        {
            var parameters = new RenderAOParameters();

            // Grab current settings
            var settings = VolumeManager.instance.stack.GetComponent<AmbientOcclusion>();
            parameters.fullResolution = settings.fullResolution.value;

            if (parameters.fullResolution)
            {
                parameters.runningRes = new Vector2(camera.actualWidth, camera.actualHeight);
                parameters.aoBufferInfo = new Vector4(camera.actualWidth, camera.actualHeight, 1.0f / camera.actualWidth, 1.0f / camera.actualHeight);
            }
            else
            {
                parameters.runningRes = new Vector2(camera.actualWidth, camera.actualHeight) * 0.5f;
                parameters.aoBufferInfo = new Vector4(camera.actualWidth * 0.5f, camera.actualHeight * 0.5f, 2.0f / camera.actualWidth, 2.0f / camera.actualHeight);
            }

            float invHalfTanFOV = -camera.mainViewConstants.projMatrix[1, 1];
            float aspectRatio = parameters.runningRes.y / parameters.runningRes.x;

            parameters.aoParams0 = new Vector4(
                parameters.fullResolution ? 0.0f : 1.0f,
                parameters.runningRes.y * invHalfTanFOV * 0.25f,
                settings.radius.value,
                settings.stepCount.value
                );


            parameters.aoParams1 = new Vector4(
                settings.intensity.value,
                1.0f / (settings.radius.value * settings.radius.value),
                (frameCount / 6) % 4,
                (frameCount % 6)
                );


            // We start from screen space position, so we bake in this factor the 1 / resolution as well.
            parameters.toViewSpaceProj = new Vector4(
                2.0f / (invHalfTanFOV * aspectRatio * parameters.runningRes.x),
                2.0f / (invHalfTanFOV * parameters.runningRes.y),
                1.0f / (invHalfTanFOV * aspectRatio),
                1.0f / invHalfTanFOV
                );

            float radInPixels = Mathf.Max(16, settings.maximumRadiusInPixels.value * ((parameters.runningRes.x * parameters.runningRes.y) / (540.0f * 960.0f)));

            parameters.aoParams2 = new Vector4(
                rtHandleProperties.currentRenderTargetSize.x,
                rtHandleProperties.currentRenderTargetSize.y,
                1.0f / (settings.stepCount.value + 1.0f),
                 radInPixels
            );

            parameters.gtaoCS = m_Resources.shaders.GTAOCS;
            parameters.gtaoKernel = parameters.gtaoCS.FindKernel("GTAOMain_HalfRes");
            if (parameters.fullResolution)
            {
                parameters.gtaoKernel = parameters.gtaoCS.FindKernel("GTAOMain_FullRes");
            }

            parameters.denoiseAOCS = m_Resources.shaders.GTAODenoiseCS;
            parameters.denoiseKernelSpatial = parameters.denoiseAOCS.FindKernel("GTAODenoise_Spatial");
            parameters.denoiseKernelTemporal = parameters.denoiseAOCS.FindKernel(parameters.fullResolution ? "GTAODenoise_Temporal_FullRes" : "GTAODenoise_Temporal");
            parameters.denoiseKernelCopyHistory = parameters.denoiseAOCS.FindKernel("GTAODenoise_CopyHistory");

            parameters.upsampleAOCS = m_Resources.shaders.GTAOUpsampleCS;
            parameters.upsampleAOKernel = parameters.upsampleAOCS.FindKernel("AOUpsample");
            parameters.outputWidth = camera.actualWidth;
            parameters.outputHeight = camera.actualHeight;

            parameters.viewCount = camera.viewCount;
            parameters.historyReady = m_HistoryReady;
            m_HistoryReady = true; // assumes that if this is called, then render is done as well.

            parameters.runAsync = camera.frameSettings.SSAORunsAsync();

            return parameters;
        }

        static void RenderAO(in RenderAOParameters  parameters,
                                RTHandle            packedDataTexture,
                                CommandBuffer       cmd)
        {
            cmd.SetComputeVectorParam(parameters.gtaoCS, HDShaderIDs._AOBufferSize, parameters.aoBufferInfo);
            cmd.SetComputeVectorParam(parameters.gtaoCS, HDShaderIDs._AODepthToViewParams, parameters.toViewSpaceProj);
            cmd.SetComputeVectorParam(parameters.gtaoCS, HDShaderIDs._AOParams0, parameters.aoParams0);
            cmd.SetComputeVectorParam(parameters.gtaoCS, HDShaderIDs._AOParams1, parameters.aoParams1);
            cmd.SetComputeVectorParam(parameters.gtaoCS, HDShaderIDs._AOParams2, parameters.aoParams2);

            cmd.SetComputeTextureParam(parameters.gtaoCS, parameters.gtaoKernel, HDShaderIDs._AOPackedData, packedDataTexture);

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
                                RTHandle                aoOutputTex,
                                CommandBuffer           cmd)
        {
            cmd.SetComputeVectorParam(parameters.denoiseAOCS, HDShaderIDs._AOParams0, parameters.aoParams0);
            cmd.SetComputeVectorParam(parameters.denoiseAOCS, HDShaderIDs._AOParams1, parameters.aoParams1);
            cmd.SetComputeVectorParam(parameters.denoiseAOCS, HDShaderIDs._AOBufferSize, parameters.aoBufferInfo);

            // Spatial
            using (new ProfilingSample(cmd, "Spatial Denoise GTAO", CustomSamplerId.ResolveSSAO.GetSampler()))
            {
                cmd.SetComputeTextureParam(parameters.denoiseAOCS, parameters.denoiseKernelSpatial, HDShaderIDs._AOPackedData, packedDataTex);
                cmd.SetComputeTextureParam(parameters.denoiseAOCS, parameters.denoiseKernelSpatial, HDShaderIDs._AOPackedBlurred, packedDataBlurredTex);

                const int groupSizeX = 8;
                const int groupSizeY = 8;
                int threadGroupX = ((int)parameters.runningRes.x + (groupSizeX - 1)) / groupSizeX;
                int threadGroupY = ((int)parameters.runningRes.y + (groupSizeY - 1)) / groupSizeY;
                cmd.DispatchCompute(parameters.denoiseAOCS, parameters.denoiseKernelSpatial, threadGroupX, threadGroupY, parameters.viewCount);
            }

            if (!parameters.historyReady)
            {
                cmd.SetComputeTextureParam(parameters.denoiseAOCS, parameters.denoiseKernelCopyHistory, HDShaderIDs._InputTexture, packedDataTex);
                cmd.SetComputeTextureParam(parameters.denoiseAOCS, parameters.denoiseKernelCopyHistory, HDShaderIDs._OutputTexture, packedHistoryTex);
                const int groupSizeX = 8;
                const int groupSizeY = 8;
                int threadGroupX = ((int)parameters.runningRes.x + (groupSizeX - 1)) / groupSizeX;
                int threadGroupY = ((int)parameters.runningRes.y + (groupSizeY - 1)) / groupSizeY;
                cmd.DispatchCompute(parameters.denoiseAOCS, parameters.denoiseKernelCopyHistory, threadGroupX, threadGroupY, parameters.viewCount);
            }

            // Temporal
            using (new ProfilingSample(cmd, "Temporal GTAO", CustomSamplerId.ResolveSSAO.GetSampler()))
            {
                cmd.SetComputeTextureParam(parameters.denoiseAOCS, parameters.denoiseKernelTemporal, HDShaderIDs._AOPackedData, packedDataTex);
                cmd.SetComputeTextureParam(parameters.denoiseAOCS, parameters.denoiseKernelTemporal, HDShaderIDs._AOPackedBlurred, packedDataBlurredTex);
                cmd.SetComputeTextureParam(parameters.denoiseAOCS, parameters.denoiseKernelTemporal, HDShaderIDs._AOPackedHistory, packedHistoryTex);
                cmd.SetComputeTextureParam(parameters.denoiseAOCS, parameters.denoiseKernelTemporal, HDShaderIDs._AOOutputHistory, packedHistoryOutputTex);
                cmd.SetComputeTextureParam(parameters.denoiseAOCS, parameters.denoiseKernelTemporal, HDShaderIDs._OcclusionTexture, aoOutputTex);

                const int groupSizeX = 8;
                const int groupSizeY = 8;
                int threadGroupX = ((int)parameters.runningRes.x + (groupSizeX - 1)) / groupSizeX;
                int threadGroupY = ((int)parameters.runningRes.y + (groupSizeY - 1)) / groupSizeY;
                cmd.DispatchCompute(parameters.denoiseAOCS, parameters.denoiseKernelTemporal, threadGroupX, threadGroupY, parameters.viewCount);
            }
        }

        static void UpsampleAO( in RenderAOParameters   parameters,
                                RTHandle                input,
                                RTHandle                output,
                                CommandBuffer           cmd)
        {
            cmd.SetComputeVectorParam(parameters.upsampleAOCS, HDShaderIDs._AOParams0, parameters.aoParams0);
            cmd.SetComputeVectorParam(parameters.upsampleAOCS, HDShaderIDs._AOParams1, parameters.aoParams1);
            cmd.SetComputeVectorParam(parameters.upsampleAOCS, HDShaderIDs._AOBufferSize, parameters.aoBufferInfo);

            cmd.SetComputeTextureParam(parameters.upsampleAOCS, parameters.upsampleAOKernel, HDShaderIDs._AOPackedData, input);
            cmd.SetComputeTextureParam(parameters.upsampleAOCS, parameters.upsampleAOKernel, HDShaderIDs._OcclusionTexture, output);

            const int groupSizeX = 8;
            const int groupSizeY = 8;
            int threadGroupX = ((int)parameters.outputWidth + (groupSizeX - 1)) / groupSizeX;
            int threadGroupY = ((int)parameters.outputHeight + (groupSizeY - 1)) / groupSizeY;
            cmd.DispatchCompute(parameters.upsampleAOCS, parameters.upsampleAOKernel, threadGroupX, threadGroupY, parameters.viewCount);
        }

        public void Dispatch(CommandBuffer cmd, HDCamera camera, int frameCount)
        {
            using (new ProfilingSample(cmd, "GTAO", CustomSamplerId.RenderSSAO.GetSampler()))
            {
                var settings = VolumeManager.instance.stack.GetComponent<AmbientOcclusion>();
                EnsureRTSize(settings, camera);

                var currentHistory = camera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.AmbientOcclusion);
                var historyOutput = camera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.AmbientOcclusion);

                var aoParameters = PrepareRenderAOParameters(camera, RTHandles.rtHandleProperties, frameCount);
                using (new ProfilingSample(cmd, "GTAO Horizon search and integration", CustomSamplerId.RenderSSAO.GetSampler()))
                {
                    RenderAO(aoParameters, m_PackedDataTex, cmd);
                }

                using (new ProfilingSample(cmd, "Denoise GTAO"))
                {
                    var output = m_RunningFullRes ? m_AmbientOcclusionTex : m_FinalHalfRes;
                    DenoiseAO(aoParameters, m_PackedDataTex, m_PackedDataBlurred, currentHistory, historyOutput, output, cmd);
                }

                if (!m_RunningFullRes)
                {
                    using (new ProfilingSample(cmd, "Upsample GTAO", CustomSamplerId.ResolveSSAO.GetSampler()))
                    {
                        UpsampleAO(aoParameters, m_FinalHalfRes, m_AmbientOcclusionTex, cmd);
                    }
                }
            }
        }

        public void PushGlobalParameters(HDCamera hdCamera, CommandBuffer cmd)
        {
            var settings = VolumeManager.instance.stack.GetComponent<AmbientOcclusion>();
            if (IsActive(hdCamera, settings))
                cmd.SetGlobalVector(HDShaderIDs._AmbientOcclusionParam, new Vector4(0f, 0f, 0f, settings.directLightingStrength.value));
            else
                cmd.SetGlobalVector(HDShaderIDs._AmbientOcclusionParam, Vector4.zero);
        }

        public void PostDispatchWork(CommandBuffer cmd, HDCamera camera)
        {
            cmd.SetGlobalTexture(HDShaderIDs._AmbientOcclusionTexture, m_AmbientOcclusionTex);
            // TODO: All the push debug stuff should be centralized somewhere
            (RenderPipelineManager.currentPipeline as HDRenderPipeline).PushFullScreenDebugTexture(camera, cmd, m_AmbientOcclusionTex, FullScreenDebugMode.SSAO);
        }
    }
}
