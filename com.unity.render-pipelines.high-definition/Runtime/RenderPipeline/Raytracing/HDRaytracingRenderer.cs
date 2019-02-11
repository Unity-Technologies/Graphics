using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
#if ENABLE_RAYTRACING
    public class HDRaytracingRenderer
    {
        // External structures
        HDRenderPipelineAsset m_PipelineAsset = null;
        RenderPipelineResources m_PipelineResources = null;
        SkyManager m_SkyManager = null;
        HDRaytracingManager m_RaytracingManager = null;
        SharedRTManager m_SharedRTManager = null;

        // Intermediate buffer that stores the reflection pre-denoising
        RTHandleSystem.RTHandle m_RaytracingFlagTarget = null;

        // The kernel that allows us to override the color buffer
        Material m_RaytracingFlagMaterial = null;

        // Light cluster structure
        public HDRaytracingLightCluster m_LightCluster = null;

        // String values
        const string m_RayGenShaderName = "RayGenRenderer";
        const string m_MissShaderName = "MissShaderRenderer";
        const string m_ClosestHitShaderName = "ClosestHitMain";

        // Pass name for the flag pass
        ShaderTagId raytracingPassID = new ShaderTagId("Forward");

        RenderStateBlock m_RaytracingFlagStateBlock;

        public HDRaytracingRenderer()
        {
        }

        public void Init(HDRenderPipelineAsset asset, SkyManager skyManager, HDRaytracingManager raytracingManager, SharedRTManager sharedRTManager)
        {
            // Keep track of the pipeline asset
            m_PipelineAsset = asset;
            m_PipelineResources = asset.renderPipelineResources;

            // Keep track of the sky manager
            m_SkyManager = skyManager;

            // keep track of the ray tracing manager
            m_RaytracingManager = raytracingManager;

            // Keep track of the shared rt manager
            m_SharedRTManager = sharedRTManager;

            m_RaytracingFlagTarget = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R8_SNorm, enableRandomWrite: true, useMipMap: false, name: "RaytracingFlagTexture");

            // Allocate the light cluster
            m_LightCluster = new HDRaytracingLightCluster();
            m_LightCluster.Initialize(asset, raytracingManager);

            m_RaytracingFlagStateBlock = new RenderStateBlock
            {
                depthState = new DepthState(false, CompareFunction.LessEqual),
                mask = RenderStateMask.Depth
            };
        }

        public void Release()
        {
            m_LightCluster.ReleaseResources();
            m_LightCluster = null;

            RTHandles.Release(m_RaytracingFlagTarget);

            if (m_RaytracingFlagMaterial != null)
            {
                CoreUtils.Destroy(m_RaytracingFlagMaterial);
            }
        }

        public void EvaluateRaytracingMask(CullingResults cull, HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext)
        {
            // Clear our target
            HDUtils.SetRenderTarget(cmd, hdCamera, m_RaytracingFlagTarget, ClearFlag.Color, Color.black);

            // Bind out custom color texture
            HDUtils.SetRenderTarget(cmd, hdCamera, m_RaytracingFlagTarget, m_SharedRTManager.GetDepthStencilBuffer());

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

            // Set the renderqueue range for the transparent set
            filterSettings.renderQueueRange = HDRenderQueue.k_RenderQueue_AllTransparentRaytracing;

            // Then let's render the transparent objects
            m_RaytracingFlagMaterial.renderQueue = (int)HDRenderQueue.Priority.RaytracingTransparent;
            drawSettings.SetShaderPassName(0, raytracingPassID);
            drawSettings.overrideMaterial = m_RaytracingFlagMaterial;
            drawSettings.overrideMaterialPassIndex = 0;
            renderContext.DrawRenderers(cull, ref drawSettings, ref filterSettings);
        }

        public void Render(HDCamera hdCamera, CommandBuffer cmd, RTHandleSystem.RTHandle outputTexture, ScriptableRenderContext renderContext, CullingResults cull)
        {
            // First thing to check is: Do we have a valid ray-tracing environment?
            HDRaytracingEnvironment rtEnvironement = m_RaytracingManager.CurrentEnvironment();
            BlueNoise blueNoise = m_RaytracingManager.GetBlueNoiseManager();
            RaytracingShader forwardShader = m_PipelineAsset.renderPipelineResources.shaders.forwardRaytracing;
            Shader raytracingMask = m_PipelineAsset.renderPipelineResources.shaders.raytracingFlagMask;

            // Try to grab the acceleration structure and the list of HD lights for the target camera
            RaytracingAccelerationStructure accelerationStructure = m_RaytracingManager.RequestAccelerationStructure(hdCamera);
            List<HDAdditionalLightData> lightData = m_RaytracingManager.RequestHDLightList(hdCamera);

            bool missingResources = rtEnvironement == null || blueNoise == null || forwardShader == null || raytracingMask == null || accelerationStructure == null || lightData == null 
                                    || m_PipelineResources.textures.owenScrambledTex == null || m_PipelineResources.textures.scramblingTex == null;

            // If any resource or game-object is missing We stop right away
            if (missingResources || !rtEnvironement.raytracedObjects)
                return;

            if (m_RaytracingFlagMaterial == null)
                m_RaytracingFlagMaterial = CoreUtils.CreateEngineMaterial(raytracingMask);

            // Before going into raytracing, we need to flag which pixels needs to be raytracing
            EvaluateRaytracingMask(cull, hdCamera, cmd, renderContext);

            // Evaluate the light cluster
            m_LightCluster.EvaluateLightClusters(cmd, hdCamera, lightData);

            // Define the shader pass to use for the reflection pass
            cmd.SetRaytracingShaderPass(forwardShader, "ForwardDXR");

            // Set the acceleration structure for the pass
            cmd.SetRaytracingAccelerationStructure(forwardShader, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

            // Inject the ray-tracing sampling data
            cmd.SetRaytracingTextureParam(forwardShader, m_RayGenShaderName, HDShaderIDs._OwenScrambledTexture, m_PipelineResources.textures.owenScrambledTex);
            cmd.SetRaytracingTextureParam(forwardShader, m_RayGenShaderName, HDShaderIDs._ScramblingTexture, m_PipelineResources.textures.scramblingTex);
            
            // Inject the ray generation data
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayBias, rtEnvironement.rayBias);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayMaxLength, rtEnvironement.raytracingRayLength);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingMaxRecursion, rtEnvironement.rayMaxDepth);

            // Set the data for the ray generation
            cmd.SetRaytracingTextureParam(forwardShader, m_RayGenShaderName, HDShaderIDs._RaytracingFlagMask, m_RaytracingFlagTarget);
            cmd.SetRaytracingTextureParam(forwardShader, m_RayGenShaderName, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetRaytracingTextureParam(forwardShader, m_RayGenShaderName, HDShaderIDs._CameraColorTextureRW, outputTexture);

            // Compute the pixel spread value
            float pixelSpreadAngle = Mathf.Atan(2.0f * Mathf.Tan(hdCamera.camera.fieldOfView * Mathf.PI / 360.0f) / Mathf.Min(hdCamera.actualWidth, hdCamera.actualHeight));
            cmd.SetRaytracingFloatParam(forwardShader, HDShaderIDs._PixelSpreadAngle, pixelSpreadAngle);

            if(lightData.Count != 0)
            {
                // LightLoop data
                cmd.SetGlobalBuffer(HDShaderIDs._RaytracingLightCluster, m_LightCluster.GetCluster());
                cmd.SetGlobalBuffer(HDShaderIDs._LightDatasRT, m_LightCluster.GetLightDatas());
                cmd.SetGlobalVector(HDShaderIDs._MinClusterPos, m_LightCluster.GetMinClusterPos());
                cmd.SetGlobalVector(HDShaderIDs._MaxClusterPos, m_LightCluster.GetMaxClusterPos());
                cmd.SetGlobalInt(HDShaderIDs._LightPerCellCount, rtEnvironement.maxNumLightsPercell);
                cmd.SetGlobalInt(HDShaderIDs._PunctualLightCountRT, m_LightCluster.GetPunctualLightCount());
                cmd.SetGlobalInt(HDShaderIDs._AreaLightCountRT, m_LightCluster.GetAreaLightCount());
            }

            // Set the data for the ray miss
            cmd.SetRaytracingTextureParam(forwardShader, m_MissShaderName, HDShaderIDs._SkyTexture, m_SkyManager.skyReflection);

            // Run the calculus
            cmd.DispatchRays(forwardShader, m_RayGenShaderName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, 1);
        }
    }
#endif
}
