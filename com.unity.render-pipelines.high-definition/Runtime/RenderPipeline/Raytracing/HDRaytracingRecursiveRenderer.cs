using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // The kernel that allows us to override the color buffer
        Material m_RaytracingFlagMaterial = null;

        // String values
        const string m_RayGenShaderName = "RayGenRenderer";

        // Pass name for the flag pass
        ShaderTagId raytracingPassID = new ShaderTagId("Forward");
        RenderStateBlock m_RaytracingFlagStateBlock;

        void InitRecursiveRenderer()
        {
            m_RaytracingFlagStateBlock = new RenderStateBlock
            {
                depthState = new DepthState(false, CompareFunction.LessEqual),
                mask = RenderStateMask.Depth
            };
        }

        void ReleaseRecursiveRenderer()
        {
            if (m_RaytracingFlagMaterial != null)
            {
                CoreUtils.Destroy(m_RaytracingFlagMaterial);
            }
        }

        void EvaluateRaytracingMask(CullingResults cull, HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, RTHandle flagBuffer)
        {

            // Clear our target
            CoreUtils.SetRenderTarget(cmd, flagBuffer, ClearFlag.Color, Color.black);

            // Bind out custom color texture
            CoreUtils.SetRenderTarget(cmd, flagBuffer, m_SharedRTManager.GetDepthStencilBuffer());

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

        void RaytracingRecursiveRender(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, CullingResults cull)
        {
            // First thing to check is: Do we have a valid ray-tracing environment?
            RecursiveRendering recursiveSettings = hdCamera.volumeStack.GetComponent<RecursiveRendering>();

            // Check the validity of the state before computing the effect
            bool invalidState = !hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing)
                || !recursiveSettings.enable.value;

            // If any resource or game-object is missing We stop right away
            if (invalidState)
                return;

            RayTracingShader forwardShader = m_Asset.renderPipelineRayTracingResources.forwardRaytracing;
            Shader raytracingMask = m_Asset.renderPipelineRayTracingResources.raytracingFlagMask;
            LightCluster lightClusterSettings = hdCamera.volumeStack.GetComponent<LightCluster>();
            RayTracingSettings rtSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();

            // Grab the acceleration structure and the list of HD lights for the target camera
            RayTracingAccelerationStructure accelerationStructure = RequestAccelerationStructure();
            HDRaytracingLightCluster lightCluster = RequestLightCluster();

            // Fecth the temporary buffers we shall be using
            RTHandle flagBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.R0);

            if (m_RaytracingFlagMaterial == null)
                m_RaytracingFlagMaterial = CoreUtils.CreateEngineMaterial(raytracingMask);

            // Before going into ray tracing, we need to flag which pixels needs to be raytracing
            EvaluateRaytracingMask(cull, hdCamera, cmd, renderContext, flagBuffer);

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
            cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._RaytracingFlagMask, flagBuffer);
            cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._CameraColorTextureRW, m_CameraColorBuffer);

            // Set ray count texture
            RayCountManager rayCountManager = GetRayCountManager();
            cmd.SetRayTracingIntParam(forwardShader, HDShaderIDs._RayCountEnabled, rayCountManager.RayCountIsEnabled());
            cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._RayCountTexture, rayCountManager.GetRayCountTexture());
            
            // Compute an approximate pixel spread angle value (in radians)
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingPixelSpreadAngle, GetPixelSpreadAngle(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight));

            // LightLoop data
            lightCluster.BindLightClusterData(cmd);
            
            // Note: Just in case, we rebind the directional light data (in case they were not)
            cmd.SetGlobalBuffer(HDShaderIDs._DirectionalLightDatas, m_LightLoopLightData.directionalLightData);
            cmd.SetGlobalInt(HDShaderIDs._DirectionalLightCount, m_lightList.directionalLights.Count);

            // Set the data for the ray miss
            cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._SkyTexture, m_SkyManager.GetSkyReflection(hdCamera));

            // If this is the right debug mode and we have at least one light, write the first shadow to the de-noised texture
            RTHandle debugBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);
            cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._RaytracingPrimaryDebug, debugBuffer);

            // Run the computation
            cmd.DispatchRays(forwardShader, m_RayGenShaderName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, (uint)hdCamera.viewCount);

            HDRenderPipeline hdrp = (RenderPipelineManager.currentPipeline as HDRenderPipeline);
            hdrp.PushFullScreenDebugTexture(hdCamera, cmd, debugBuffer, FullScreenDebugMode.RecursiveRayTracing);
        }
    }
}
