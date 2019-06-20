using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable, VolumeComponentMenu("Ray Tracing/Recursive Rendering")]
    public sealed class RecursiveRendering : VolumeComponent
    {
        [Tooltip("Enable. Enables recursive rendering.")]
        public BoolParameter enable = new BoolParameter(false);

        [Tooltip("Max Depth. Defines the maximal recursion for rays.")]
        public ClampedIntParameter maxDepth = new ClampedIntParameter(4, 1, 10);

        [Tooltip("Ray Length. This defines the maximal travel distance of rays.")]
        public ClampedFloatParameter rayLength = new ClampedFloatParameter(10f, 0f, 50f);
    }

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
        RTHandleSystem.RTHandle m_DebugRaytracingTexture = null;

        // The kernel that allows us to override the color buffer
        Material m_RaytracingFlagMaterial = null;

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
            m_DebugRaytracingTexture = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "DebugRaytracingBuffer");

            m_RaytracingFlagStateBlock = new RenderStateBlock
            {
                depthState = new DepthState(false, CompareFunction.LessEqual),
                mask = RenderStateMask.Depth
            };
        }

        public void Release()
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
            HDUtils.SetRenderTarget(cmd, m_RaytracingFlagTarget, ClearFlag.Color, Color.black);

            // Bind out custom color texture
            HDUtils.SetRenderTarget(cmd, m_RaytracingFlagTarget, m_SharedRTManager.GetDepthStencilBuffer());

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

        public void Render(HDCamera hdCamera, CommandBuffer cmd, RTHandleSystem.RTHandle outputTexture, ScriptableRenderContext renderContext, CullingResults cull)
        {
            // First thing to check is: Do we have a valid ray-tracing environment?
            HDRaytracingEnvironment rtEnvironment = m_RaytracingManager.CurrentEnvironment();
            HDRenderPipeline renderPipeline = m_RaytracingManager.GetRenderPipeline();
            RayTracingShader forwardShader = m_PipelineAsset.renderPipelineRayTracingResources.forwardRaytracing;
            Shader raytracingMask = m_PipelineAsset.renderPipelineRayTracingResources.raytracingFlagMask;

            RecursiveRendering recursiveSettings = VolumeManager.instance.stack.GetComponent<RecursiveRendering>();
            LightCluster lightClusterSettings = VolumeManager.instance.stack.GetComponent<LightCluster>();

            // Check the validity of the state before computing the effect
            bool invalidState = rtEnvironment == null || !recursiveSettings.enable.value
                || forwardShader == null || raytracingMask == null
                || m_PipelineResources.textures.owenScrambledTex == null || m_PipelineResources.textures.scramblingTex == null;

            // If any resource or game-object is missing We stop right away
            if (invalidState)
                return;

            // Grab the acceleration structure and the list of HD lights for the target camera
            RayTracingAccelerationStructure accelerationStructure = m_RaytracingManager.RequestAccelerationStructure(rtEnvironment.raytracedLayerMask);
            HDRaytracingLightCluster lightCluster = m_RaytracingManager.RequestLightCluster(rtEnvironment.raytracedLayerMask);

            if (m_RaytracingFlagMaterial == null)
                m_RaytracingFlagMaterial = CoreUtils.CreateEngineMaterial(raytracingMask);

            // Before going into raytracing, we need to flag which pixels needs to be raytracing
            EvaluateRaytracingMask(cull, hdCamera, cmd, renderContext);

            // Define the shader pass to use for the reflection pass
            cmd.SetRayTracingShaderPass(forwardShader, "ForwardDXR");

            // Set the acceleration structure for the pass
            cmd.SetRayTracingAccelerationStructure(forwardShader, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

            // Inject the ray-tracing sampling data
            cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._OwenScrambledTexture, m_PipelineResources.textures.owenScrambledTex);
            cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._ScramblingTexture, m_PipelineResources.textures.scramblingTex);

            // Inject the ray generation data
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayBias, rtEnvironment.rayBias);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayMaxLength, recursiveSettings.rayLength.value);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingMaxRecursion, recursiveSettings.maxDepth.value);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingCameraNearPlane, hdCamera.camera.nearClipPlane);

            // Set the data for the ray generation
            cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._RaytracingFlagMask, m_RaytracingFlagTarget);
            cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._CameraColorTextureRW, outputTexture);

            // Compute an approximate pixel spread angle value (in radians)
            float pixelSpreadAngle = hdCamera.camera.fieldOfView * (Mathf.PI / 180.0f) / Mathf.Min(hdCamera.actualWidth, hdCamera.actualHeight);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingPixelSpreadAngle, pixelSpreadAngle);

            // LightLoop data
            cmd.SetGlobalBuffer(HDShaderIDs._RaytracingLightCluster, lightCluster.GetCluster());
            cmd.SetGlobalBuffer(HDShaderIDs._LightDatasRT, lightCluster.GetLightDatas());
            cmd.SetGlobalVector(HDShaderIDs._MinClusterPos, lightCluster.GetMinClusterPos());
            cmd.SetGlobalVector(HDShaderIDs._MaxClusterPos, lightCluster.GetMaxClusterPos());
            cmd.SetGlobalInt(HDShaderIDs._LightPerCellCount, lightClusterSettings.maxNumLightsPercell.value);
            cmd.SetGlobalInt(HDShaderIDs._PunctualLightCountRT, lightCluster.GetPunctualLightCount());
            cmd.SetGlobalInt(HDShaderIDs._AreaLightCountRT, lightCluster.GetAreaLightCount());

            // Note: Just in case, we rebind the directional light data (in case they were not)
            cmd.SetGlobalBuffer(HDShaderIDs._DirectionalLightDatas, renderPipeline.m_LightLoopLightData.directionalLightData);
            cmd.SetGlobalInt(HDShaderIDs._DirectionalLightCount, renderPipeline.m_lightList.directionalLights.Count);

            // Set the data for the ray miss
            cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._SkyTexture, m_SkyManager.skyReflection);

            // If this is the right debug mode and we have at least one light, write the first shadow to the denoise texture
            HDRenderPipeline hdrp = (RenderPipelineManager.currentPipeline as HDRenderPipeline);
            cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._RaytracingPrimaryDebug, m_DebugRaytracingTexture);
            hdrp.PushFullScreenDebugTexture(hdCamera, cmd, m_DebugRaytracingTexture, FullScreenDebugMode.PrimaryVisibility);

            // Run the computation
            cmd.DispatchRays(forwardShader, m_RayGenShaderName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, 1);
        }
    }
#endif
}
