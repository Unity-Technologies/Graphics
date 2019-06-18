using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    using RTHandle = RTHandleSystem.RTHandle;

    [Serializable, VolumeComponentMenu("Lighting/Ambient Occlusion")]
    public sealed class AmbientOcclusion : VolumeComponent
    {
        [Tooltip("Controls the strength of the ambient occlusion effect. Increase this value to produce darker areas.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 4f);

        [Tooltip("Number of steps to take along one signed direction during horizon search (this is the number of steps in positive and negative direction).")]
        public ClampedIntParameter stepCount = new ClampedIntParameter(6, 2, 32);

        [Tooltip("Sampling radius. Bigger the radius, wider AO will be achieved, risking to lose fine details and increasing cost of the effect due to increasing cache misses.")]
        public ClampedFloatParameter radius = new ClampedFloatParameter(2.0f, 0.25f, 5.0f);

        [Tooltip("The effect runs at full resolution. This increases quality, but also decreases performance significantly.")]
        public BoolParameter fullResolution = new BoolParameter(false);

        [Tooltip("This poses a maximum radius in pixels that we consider. It is very important to keep this as tight as possible to preserve good performance. Note that this is the value used for 1080p when *not* running the effect at full resolution, it will be scaled accordingly for other resolutions.")]
        public ClampedIntParameter maximumRadiusInPixels = new ClampedIntParameter(40, 16, 256);


        [Tooltip("Controls how much the ambient light affects occlusion.")]
        public ClampedFloatParameter directLightingStrength = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Enable raytraced ambient occlusion.")]
        public BoolParameter enableRaytracing = new BoolParameter(false);

        [Tooltip("Controls the length of ambient occlusion rays.")]
        public ClampedFloatParameter rayLength = new ClampedFloatParameter(0.5f, 0f, 50f);

        [Tooltip("Enable Filtering on the raytraced ambient occlusion.")]
        public BoolParameter enableFilter = new BoolParameter(false);

        [Tooltip("Controls the length of ambient occlusion rays.")]
        public ClampedIntParameter numSamples = new ClampedIntParameter(4, 1, 64);

        [Tooltip("Controls the length of ambient occlusion rays.")]
        public ClampedIntParameter filterRadius = new ClampedIntParameter(16, 1, 32);

    }

    public class AmbientOcclusionSystem
    {
        RenderPipelineResources m_Resources;
#if ENABLE_RAYTRACING
        HDRenderPipelineRayTracingResources m_RTResources;
#endif
        RenderPipelineSettings m_Settings;

        private bool m_HistoryReady = false;
        private RTHandle m_PackedDataTex;
        private RTHandle m_PackedDataBlurred;
        private RTHandle[] m_PackedHistory;
        private RTHandle m_AmbientOcclusionTex;
        private RTHandle m_FinalHalfRes;

        private RTHandle m_BentNormalTex;

        private bool m_RunningFullRes = false;
        private int m_HistoryIndex = 0;

#if ENABLE_RAYTRACING
        public HDRaytracingManager m_RayTracingManager;
        readonly HDRaytracingAmbientOcclusion m_RaytracingAmbientOcclusion = new HDRaytracingAmbientOcclusion();
#endif

        private void ReleaseRT()
        {
            RTHandles.Release(m_AmbientOcclusionTex);
            RTHandles.Release(m_BentNormalTex);
            RTHandles.Release(m_PackedDataTex);
            RTHandles.Release(m_PackedDataBlurred);
            for (int i = 0; i < m_PackedHistory.Length; ++i)
            {
                RTHandles.Release(m_PackedHistory[i]);
            }

            if (m_FinalHalfRes != null)
                RTHandles.Release(m_FinalHalfRes);
        }

        void AllocRT(float scaleFactor)
        {
            m_AmbientOcclusionTex = RTHandles.Alloc(Vector2.one, TextureXR.slices, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R8_UNorm, dimension: TextureXR.dimension, useDynamicScale: true, enableRandomWrite: true, name: "Ambient Occlusion");
            m_BentNormalTex = RTHandles.Alloc(Vector2.one * scaleFactor, TextureXR.slices, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R8G8B8A8_SNorm, dimension: TextureXR.dimension, useDynamicScale: true, enableRandomWrite: true, name: "Bent normals");
            m_PackedDataTex = RTHandles.Alloc(Vector2.one * scaleFactor, TextureXR.slices, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32_UInt, dimension: TextureXR.dimension, useDynamicScale: true, enableRandomWrite: true, name: "AO Packed data");
            m_PackedDataBlurred = RTHandles.Alloc(Vector2.one * scaleFactor, TextureXR.slices, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32_UInt, dimension: TextureXR.dimension, useDynamicScale: true, enableRandomWrite: true, name: "AO Packed blurred data");
            m_PackedHistory[0] = RTHandles.Alloc(Vector2.one * scaleFactor, TextureXR.slices, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32_UInt, dimension: TextureXR.dimension, useDynamicScale: true, enableRandomWrite: true, name: "AO Packed history_1");
            m_PackedHistory[1] = RTHandles.Alloc(Vector2.one * scaleFactor, TextureXR.slices, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32_UInt, dimension: TextureXR.dimension, useDynamicScale: true, enableRandomWrite: true, name: "AO Packed history_2");

            m_FinalHalfRes = RTHandles.Alloc(Vector2.one * 0.5f, TextureXR.slices, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32_UInt, dimension: TextureXR.dimension, useDynamicScale: true, enableRandomWrite: true, name: "Final Half Res AO Packed");
        }

        void EnsureRTSize(AmbientOcclusion settings)
        {
            if (settings.fullResolution != m_RunningFullRes)
            {
                ReleaseRT();

                m_RunningFullRes = settings.fullResolution.value;
                float scaleFactor = m_RunningFullRes ? 1.0f : 0.5f;
                AllocRT(scaleFactor);
            }
        }

        public AmbientOcclusionSystem(HDRenderPipelineAsset hdAsset)
        {
            m_Settings = hdAsset.currentPlatformRenderPipelineSettings;
            m_Resources = hdAsset.renderPipelineResources;
#if ENABLE_RAYTRACING
            m_RTResources = hdAsset.renderPipelineRayTracingResources;
#endif

            if (!hdAsset.currentPlatformRenderPipelineSettings.supportSSAO)
                return;

            m_PackedHistory = new RTHandle[2];
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

        public void Render(CommandBuffer cmd, HDCamera camera, SharedRTManager sharedRTManager, ScriptableRenderContext renderContext, int frameCount)
        {

            var settings = VolumeManager.instance.stack.GetComponent<AmbientOcclusion>();

            if (!IsActive(camera, settings))
            {
                // No AO applied - neutral is black, see the comment in the shaders
                cmd.SetGlobalTexture(HDShaderIDs._AmbientOcclusionTexture, TextureXR.GetBlackTexture());
                cmd.SetGlobalVector(HDShaderIDs._AmbientOcclusionParam, Vector4.zero);
                return;
            }
            else
            {
#if ENABLE_RAYTRACING
                HDRaytracingEnvironment rtEnvironement = m_RayTracingManager.CurrentEnvironment();
                if (rtEnvironement != null && settings.enableRaytracing.value)
                    m_RaytracingAmbientOcclusion.RenderAO(camera, cmd, m_AmbientOcclusionTex, renderContext, frameCount);
                else
#endif
                {
                    Dispatch(cmd, camera, sharedRTManager, frameCount);
                    PostDispatchWork(cmd, camera, sharedRTManager);
                }
            }
        }

        private void RenderAO(CommandBuffer cmd, HDCamera camera, SharedRTManager sharedRTManager, int frameCount)
        {
            // Grab current settings
            var settings = VolumeManager.instance.stack.GetComponent<AmbientOcclusion>();

            EnsureRTSize(settings);

            Vector4 aoBufferInfo;
            Vector2 runningRes;

            if (settings.fullResolution.value)
            {
                runningRes = new Vector2(camera.actualWidth, camera.actualHeight);
                aoBufferInfo = new Vector4(camera.actualWidth, camera.actualHeight, 1.0f / camera.actualWidth, 1.0f / camera.actualHeight);
            }
            else
            {
                runningRes = new Vector2(camera.actualWidth, camera.actualHeight) * 0.5f;
                aoBufferInfo = new Vector4(camera.actualWidth * 0.5f, camera.actualHeight * 0.5f, 2.0f / camera.actualWidth, 2.0f / camera.actualHeight);
            }

            float invHalfTanFOV = -camera.mainViewConstants.projMatrix[1, 1];
            float aspectRatio = runningRes.y / runningRes.x;

            Vector4 aoParams0 = new Vector4(
                settings.fullResolution.value ? 0.0f : 1.0f,
                runningRes.y * invHalfTanFOV * 0.25f,
                settings.radius.value,
                settings.stepCount.value
                );


            Vector4 aoParams1 = new Vector4(
                settings.intensity.value,
                1.0f / (settings.radius.value * settings.radius.value),
                (frameCount / 6) % 4,
                (frameCount % 6)
                );


            // We start from screen space position, so we bake in this factor the 1 / resolution as well. 
            Vector4 toViewSpaceProj = new Vector4(
                2.0f / (invHalfTanFOV * aspectRatio * runningRes.x),
                2.0f / (invHalfTanFOV * runningRes.y),
                1.0f / (invHalfTanFOV * aspectRatio),
                1.0f / invHalfTanFOV
                );

            float radInPixels = Mathf.Max(16, settings.maximumRadiusInPixels.value * ((runningRes.x * runningRes.y) /  (540.0f * 960.0f)));

            Vector4 aoParams2 = new Vector4(
                RTHandles.rtHandleProperties.currentRenderTargetSize.x,
                RTHandles.rtHandleProperties.currentRenderTargetSize.y,
                1.0f / ((float)settings.stepCount.value + 1.0f),
                 radInPixels
            );

            var cs = m_Resources.shaders.GTAOCS;
            var kernel = cs.FindKernel("GTAOMain_HalfRes");
            if(m_RunningFullRes)
            {
                kernel = cs.FindKernel("GTAOMain_FullRes");
            }

            cmd.SetComputeVectorParam(cs, HDShaderIDs._AOBufferSize, aoBufferInfo);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._AODepthToViewParams, toViewSpaceProj);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._AOParams0, aoParams0);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._AOParams1, aoParams1);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._AOParams2, aoParams2);

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OcclusionTexture, m_AmbientOcclusionTex);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._BentNormalsTexture, m_BentNormalTex);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._AOPackedData, m_PackedDataTex);

            const int groupSizeX = 8;
            const int groupSizeY = 8;
            int threadGroupX = ((int)runningRes.x + (groupSizeX - 1)) / groupSizeX;
            int threadGroupY = ((int)runningRes.y + (groupSizeY - 1)) / groupSizeY;
            using (new ProfilingSample(cmd, "GTAO Horizon search and integration", CustomSamplerId.RenderSSAO.GetSampler()))
            {
                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, camera.viewCount);
            }
        }

        private void DenoiseAO(CommandBuffer cmd, HDCamera camera, SharedRTManager sharedRTManager)
        {
            var settings = VolumeManager.instance.stack.GetComponent<AmbientOcclusion>();

            if (!IsActive(camera, settings))
                return;

            var cs = m_Resources.shaders.GTAODenoiseCS;

            Vector4 aoBufferInfo;
            Vector2 runningRes;

            if (m_RunningFullRes)
            {
                runningRes = new Vector2(camera.actualWidth, camera.actualHeight);
                aoBufferInfo = new Vector4(camera.actualWidth, camera.actualHeight, 1.0f / camera.actualWidth, 1.0f / camera.actualHeight);
            }
            else
            {
                runningRes = new Vector2(camera.actualWidth, camera.actualHeight) * 0.5f;
                aoBufferInfo = new Vector4(camera.actualWidth * 0.5f, camera.actualHeight * 0.5f, 2.0f / camera.actualWidth, 2.0f / camera.actualHeight);
            }

            Vector4 aoParams0 = new Vector4(
                settings.fullResolution.value ? 0.0f : 1.0f,
                0, // not needed
                settings.radius.value,
                settings.stepCount.value
            );


            Vector4 aoParams1 = new Vector4(
                settings.intensity.value,
                1.0f / (settings.radius.value * settings.radius.value),
                0,
                0
            );

            cmd.SetComputeVectorParam(cs, HDShaderIDs._AOParams0, aoParams0);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._AOParams1, aoParams1);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._AOBufferSize, aoBufferInfo);

            // Spatial
            using (new ProfilingSample(cmd, "Spatial Denoise GTAO", CustomSamplerId.ResolveSSAO.GetSampler()))
            {
                var kernel = cs.FindKernel("GTAODenoise_Spatial");

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._AOPackedData, m_PackedDataTex);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._AOPackedBlurred, m_PackedDataBlurred);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OcclusionTexture, m_AmbientOcclusionTex);

                const int groupSizeX = 8;
                const int groupSizeY = 8;
                int threadGroupX = ((int)runningRes.x + (groupSizeX - 1)) / groupSizeX;
                int threadGroupY = ((int)runningRes.y + (groupSizeY - 1)) / groupSizeY;
                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, camera.viewCount);
            }

            if (!m_HistoryReady)
            {
                var kernel = cs.FindKernel("GTAODenoise_CopyHistory");
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, m_PackedDataTex);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, m_PackedHistory[m_HistoryIndex]);
                const int groupSizeX = 8;
                const int groupSizeY = 8;
                int threadGroupX = ((int)runningRes.x + (groupSizeX - 1)) / groupSizeX;
                int threadGroupY = ((int)runningRes.y + (groupSizeY - 1)) / groupSizeY;
                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, camera.viewCount);

                m_HistoryReady = true;
            }

            // Temporal
            using (new ProfilingSample(cmd, "Temporal GTAO", CustomSamplerId.ResolveSSAO.GetSampler()))
            {
                int outputIndex = (m_HistoryIndex + 1) & 1;

                int kernel;
                if (m_RunningFullRes)
                {
                    kernel = cs.FindKernel("GTAODenoise_Temporal_FullRes");
                }
                else
                {
                    kernel = cs.FindKernel("GTAODenoise_Temporal");
                }

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._AOPackedData, m_PackedDataTex);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._AOPackedBlurred, m_PackedDataBlurred);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._AOPackedHistory, m_PackedHistory[m_HistoryIndex]);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._AOOutputHistory, m_PackedHistory[outputIndex]);
                if (m_RunningFullRes)
                {
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OcclusionTexture, m_AmbientOcclusionTex);
                }
                else
                {
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OcclusionTexture, m_FinalHalfRes);
                }

                const int groupSizeX = 8;
                const int groupSizeY = 8;
                int threadGroupX = ((int)runningRes.x + (groupSizeX - 1)) / groupSizeX;
                int threadGroupY = ((int)runningRes.y + (groupSizeY - 1)) / groupSizeY;
                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, camera.viewCount);

                m_HistoryIndex = outputIndex;
            }

            // Need upsample
            if (!m_RunningFullRes)
            {
                using (new ProfilingSample(cmd, "Upsample GTAO", CustomSamplerId.ResolveSSAO.GetSampler()))
                {
                    cs = m_Resources.shaders.GTAOUpsampleCS;
                    var kernel = cs.FindKernel("AOUpsample");

                    cmd.SetComputeVectorParam(cs, HDShaderIDs._AOParams0, aoParams0);
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._AOParams1, aoParams1);
                    cmd.SetComputeVectorParam(cs, HDShaderIDs._AOBufferSize, aoBufferInfo);

                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._AOPackedData, m_FinalHalfRes);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OcclusionTexture, m_AmbientOcclusionTex);

                    const int groupSizeX = 8;
                    const int groupSizeY = 8;
                    int threadGroupX = ((int)camera.actualWidth + (groupSizeX - 1)) / groupSizeX;
                    int threadGroupY = ((int)camera.actualHeight + (groupSizeY - 1)) / groupSizeY;
                    cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, camera.viewCount);
                }
            }
        }

        public void Dispatch(CommandBuffer cmd, HDCamera camera, SharedRTManager sharedRTManager, int frameCount)
        {
            using (new ProfilingSample(cmd, "GTAO", CustomSamplerId.RenderSSAO.GetSampler()))
            {
                RenderAO(cmd, camera, sharedRTManager, frameCount);
                DenoiseAO(cmd, camera, sharedRTManager);
            }
        }

        public void PostDispatchWork(CommandBuffer cmd, HDCamera camera, SharedRTManager sharedRTManager)
        {
            // Grab current settings
            var settings = VolumeManager.instance.stack.GetComponent<AmbientOcclusion>();

            cmd.SetGlobalTexture(HDShaderIDs._AmbientOcclusionTexture, m_AmbientOcclusionTex);
            cmd.SetGlobalVector(HDShaderIDs._AmbientOcclusionParam, new Vector4(0f, 0f, 0f, settings.directLightingStrength.value));

            // TODO: All the push debug stuff should be centralized somewhere
            (RenderPipelineManager.currentPipeline as HDRenderPipeline).PushFullScreenDebugTexture(camera, cmd, m_AmbientOcclusionTex, FullScreenDebugMode.SSAO);
        }
    }
}
