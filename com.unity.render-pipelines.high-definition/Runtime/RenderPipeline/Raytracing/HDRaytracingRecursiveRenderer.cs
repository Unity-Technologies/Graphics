using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Intermediate buffer that stores the reflection pre-denoising
        RTHandle m_RaytracingFlagTarget = null;
        RTHandle m_DebugRaytracingTexture = null;

        // The kernel that allows us to override the color buffer
        Material m_RaytracingFlagMaterial = null;

        // String values
        const string m_RayGenShaderName = "RayGenRenderer";

        // Pass name for the flag pass
        ShaderTagId raytracingPassID = new ShaderTagId("Forward");

        RenderStateBlock m_RaytracingFlagStateBlock;

        public void InitRecursiveRenderer()
        {
            m_RaytracingFlagTarget = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R8_SNorm, enableRandomWrite: true, useMipMap: false, name: "RaytracingFlagTexture");
            m_DebugRaytracingTexture = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "DebugRaytracingBuffer");

            m_RaytracingFlagStateBlock = new RenderStateBlock
            {
                depthState = new DepthState(false, CompareFunction.LessEqual),
                mask = RenderStateMask.Depth
            };
        }

        public void ReleaseRecursiveRenderer()
        {
            RTHandles.Release(m_DebugRaytracingTexture);
            RTHandles.Release(m_RaytracingFlagTarget);

            if (m_RaytracingFlagMaterial != null)
            {
                CoreUtils.Destroy(m_RaytracingFlagMaterial);
            }
        }

        public void EvaluateRaytracingMask(CullingResults cull, HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext)
        {
            // Clear our target
            CoreUtils.SetRenderTarget(cmd, m_RaytracingFlagTarget, ClearFlag.Color, Color.black);

            // Bind out custom color texture
            CoreUtils.SetRenderTarget(cmd, m_RaytracingFlagTarget, m_SharedRTManager.GetDepthStencilBuffer());

            // This is done here because DrawRenderers API lives outside command buffers so we need to make call this before doing any DrawRenders
            renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            var sortingSettings = new SortingSettings(hdCamera.camera)
            {
                criteria = 0
            };

            var filterSettings = new FilteringSettings(HDRenderQueue.k_RenderQueue_AllOpaqueRaytracing)
            {
                excludeMotionVectorObjects = false
            };

            var drawSettings = new DrawingSettings(HDShaderPassNames.s_EmptyName, sortingSettings)
            {
                perObjectData = 0
            };

            // First let's render the opaque objects
            m_RaytracingFlagMaterial.renderQueue = (int)HDRenderQueue.Priority.RaytracingOpaque;
            drawSettings.SetShaderPassName(0, raytracingPassID);
            drawSettings.overrideMaterial = m_RaytracingFlagMaterial;
            drawSettings.overrideMaterialPassIndex = 0;
            renderContext.DrawRenderers(cull, ref drawSettings, ref filterSettings);

            // Set the render queue range for the transparent set
            filterSettings.renderQueueRange = HDRenderQueue.k_RenderQueue_AllTransparentRaytracing;

            // Then let's render the transparent objects
            m_RaytracingFlagMaterial.renderQueue = (int)HDRenderQueue.Priority.RaytracingTransparent;
            drawSettings.SetShaderPassName(0, raytracingPassID);
            drawSettings.overrideMaterial = m_RaytracingFlagMaterial;
            drawSettings.overrideMaterialPassIndex = 0;
            renderContext.DrawRenderers(cull, ref drawSettings, ref filterSettings);
        }

        public void RaytracingRecursiveRender(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, CullingResults cull)
        {
            // First thing to check is: Do we have a valid ray-tracing environment?
            RecursiveRendering recursiveSettings = VolumeManager.instance.stack.GetComponent<RecursiveRendering>();

            // Check the validity of the state before computing the effect
            bool invalidState = !hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing)
                || !recursiveSettings.enable.value
                || m_Asset.currentPlatformRenderPipelineSettings.supportedRaytracingTier == RenderPipelineSettings.RaytracingTier.Tier1;

            // If any resource or game-object is missing We stop right away
            if (invalidState)
                return;

            RayTracingShader forwardShader = m_Asset.renderPipelineRayTracingResources.forwardRaytracing;
            Shader raytracingMask = m_Asset.renderPipelineRayTracingResources.raytracingFlagMask;
            LightCluster lightClusterSettings = VolumeManager.instance.stack.GetComponent<LightCluster>();
            RayTracingSettings rtSettings = VolumeManager.instance.stack.GetComponent<RayTracingSettings>();

            // Grab the acceleration structure and the list of HD lights for the target camera
            RayTracingAccelerationStructure accelerationStructure = RequestAccelerationStructure();
            HDRaytracingLightCluster lightCluster = RequestLightCluster();

            if (m_RaytracingFlagMaterial == null)
                m_RaytracingFlagMaterial = CoreUtils.CreateEngineMaterial(raytracingMask);

            // Before going into ray tracing, we need to flag which pixels needs to be raytracing
            EvaluateRaytracingMask(cull, hdCamera, cmd, renderContext);

            // Define the shader pass to use for the reflection pass
            cmd.SetRayTracingShaderPass(forwardShader, "ForwardDXR");

            // Set the acceleration structure for the pass
            cmd.SetRayTracingAccelerationStructure(forwardShader, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

            // Inject the ray-tracing sampling data
            cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._OwenScrambledTexture, m_Asset.renderPipelineResources.textures.owenScrambledRGBATex);
            cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._ScramblingTexture, m_Asset.renderPipelineResources.textures.scramblingTex);

            // Inject the ray generation data
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayBias, rtSettings.rayBias.value);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayMaxLength, recursiveSettings.rayLength.value);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingMaxRecursion, recursiveSettings.maxDepth.value);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingCameraNearPlane, hdCamera.camera.nearClipPlane);

            // Set the data for the ray generation
            cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._RaytracingFlagMask, m_RaytracingFlagTarget);
            cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._CameraColorTextureRW, m_CameraColorBuffer);

            // Set ray count texture
            RayCountManager rayCountManager = GetRayCountManager();
            cmd.SetRayTracingIntParam(forwardShader, HDShaderIDs._RayCountEnabled, rayCountManager.RayCountIsEnabled());
            cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._RayCountTexture, rayCountManager.GetRayCountTexture());
            
            // Compute an approximate pixel spread angle value (in radians)
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingPixelSpreadAngle, GetPixelSpreadAngle(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight));

            // LightLoop data
            cmd.SetGlobalBuffer(HDShaderIDs._RaytracingLightCluster, lightCluster.GetCluster());
            cmd.SetGlobalBuffer(HDShaderIDs._LightDatasRT, lightCluster.GetLightDatas());
            cmd.SetGlobalVector(HDShaderIDs._MinClusterPos, lightCluster.GetMinClusterPos());
            cmd.SetGlobalVector(HDShaderIDs._MaxClusterPos, lightCluster.GetMaxClusterPos());
            cmd.SetGlobalInt(HDShaderIDs._LightPerCellCount, lightClusterSettings.maxNumLightsPercell.value);
            cmd.SetGlobalInt(HDShaderIDs._PunctualLightCountRT, lightCluster.GetPunctualLightCount());
            cmd.SetGlobalInt(HDShaderIDs._AreaLightCountRT, lightCluster.GetAreaLightCount());

            // Note: Just in case, we rebind the directional light data (in case they were not)
            cmd.SetGlobalBuffer(HDShaderIDs._DirectionalLightDatas, m_LightLoopLightData.directionalLightData);
            cmd.SetGlobalInt(HDShaderIDs._DirectionalLightCount, m_lightList.directionalLights.Count);

            // Set the data for the ray miss
            cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._SkyTexture, m_SkyManager.GetSkyReflection(hdCamera));

            // If this is the right debug mode and we have at least one light, write the first shadow to the de-noised texture
            cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._RaytracingPrimaryDebug, m_DebugRaytracingTexture);

            // Run the computation
            cmd.DispatchRays(forwardShader, m_RayGenShaderName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, (uint)hdCamera.viewCount);

            HDRenderPipeline hdrp = (RenderPipelineManager.currentPipeline as HDRenderPipeline);
            hdrp.PushFullScreenDebugTexture(hdCamera, cmd, m_DebugRaytracingTexture, FullScreenDebugMode.RecursiveRayTracing);
        }
    }
}
