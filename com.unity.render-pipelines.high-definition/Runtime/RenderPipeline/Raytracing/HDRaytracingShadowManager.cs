using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
#if ENABLE_RAYTRACING
    public class HDRaytracingShadowManager
    {
        HDRenderPipelineAsset m_PipelineAsset = null;
        RenderPipelineResources m_PipelineResources = null;
        HDRaytracingManager m_RaytracingManager = null;
        SharedRTManager m_SharedRTManager = null;
        LightLoop m_LightLoop = null;
        GBufferManager m_GbufferManager = null;
        static int m_KernelFilter;

        // Buffers that hold the intermediate data of the shadow algorithm
        RTHandleSystem.RTHandle m_SNBuffer = null;
        RTHandleSystem.RTHandle m_UNBuffer = null;
        RTHandleSystem.RTHandle m_UBuffer = null;

        // Array that holds the shadow textures for the area lights
        RTHandleSystem.RTHandle m_AreaShadowTextureArray = null;

        // String values
        const string m_RayGenShaderName = "RayGenShadows";
        const string m_MissShaderName = "MissShaderShadows";

        public static readonly int _SNBuffer = Shader.PropertyToID("_SNTextureUAV");
        public static readonly int _UNBuffer = Shader.PropertyToID("_UNTextureUAV");
        public static readonly int _UBuffer = Shader.PropertyToID("_UTextureUAV");

        // Denoising data
        public static readonly int _DenoiseRadius = Shader.PropertyToID("_DenoiseRadius");
        public static readonly int _GaussianSigma = Shader.PropertyToID("_GaussianSigma");

        // Temporary variable that allows us to store the world to local matrix
        Matrix4x4 worldToLocalArea = new Matrix4x4();

        public HDRaytracingShadowManager()
        {
        }

        public void Init(HDRenderPipelineAsset asset, HDRaytracingManager raytracingManager, SharedRTManager sharedRTManager, LightLoop lightLoop, GBufferManager gbufferManager)
        {
            // Keep track of the pipeline asset
            m_PipelineAsset = asset;
            m_PipelineResources = asset.renderPipelineResources;

            // keep track of the ray tracing manager
            m_RaytracingManager = raytracingManager;

            // Keep track of the shared rt manager
            m_SharedRTManager = sharedRTManager;

            // The lightloop that holds all the lights of the scene
            m_LightLoop = lightLoop;

            // GBuffer manager that holds all the data for shading the samples
            m_GbufferManager = gbufferManager;

            // Allocate the intermediate buffers
            m_SNBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "SNBuffer");
            m_UNBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "UNBuffer");
            m_UBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "UBuffer");
            m_AreaShadowTextureArray = RTHandles.Alloc(Vector2.one, slices:4, dimension:TextureDimension.Tex2DArray, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "AreaShadowArrayBuffer");
        }

        public RTHandleSystem.RTHandle GetShadowedIntegrationTexture()
        {
            return m_SNBuffer;
        }

        public RTHandleSystem.RTHandle GetUnShadowedIntegrationTexture()
        {
            return m_UNBuffer;
        }

        public void Release()
        {
            RTHandles.Release(m_AreaShadowTextureArray);
            RTHandles.Release(m_UBuffer);
            RTHandles.Release(m_UNBuffer);
            RTHandles.Release(m_SNBuffer);
        }

        void BindShadowTexture(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._AreaShadowTexture, m_AreaShadowTextureArray);
        }

        public bool RenderAreaShadows(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, uint frameCount)
        {
            // NOTE: Here we cannot clear the area shadow texture because it is a texture array. So we need to bind it and make sure no material will try to read it in the shaders
            BindShadowTexture(cmd);

            // Let's check all the resources and states to see if we should render the effect
            HDRaytracingEnvironment rtEnvironement = m_RaytracingManager.CurrentEnvironment();
            BlueNoise blueNoise = m_RaytracingManager.GetBlueNoiseManager();
            RaytracingShader shadowsShader = m_PipelineAsset.renderPipelineResources.shaders.shadowsRaytracing;
            ComputeShader bilateralFilter = m_PipelineAsset.renderPipelineResources.shaders.areaBillateralFilterCS;
            bool invalidState = rtEnvironement == null || blueNoise == null || !rtEnvironement.raytracedShadows || shadowsShader  == null || bilateralFilter == null || hdCamera.frameSettings.litShaderMode != LitShaderMode.Deferred
            || m_PipelineResources.textures.owenScrambledTex == null || m_PipelineResources.textures.scramblingTex == null;

            // Try to grab the acceleration structure for the target camera
            RaytracingAccelerationStructure accelerationStructure = m_RaytracingManager.RequestAccelerationStructure(hdCamera);

            // If invalid state or ray-tracing acceleration structure, we stop right away
            if (invalidState || accelerationStructure == null)
                return false;

            // Fetch the filter kernel
            m_KernelFilter = bilateralFilter.FindKernel("AreaBilateralShadow");

            // Define the shader pass to use for the reflection pass
            cmd.SetRaytracingShaderPass(shadowsShader, "VisibilityDXR");

            // Set the acceleration structure for the pass
            cmd.SetRaytracingAccelerationStructure(shadowsShader, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

            // Inject the ray-tracing sampling data
            cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._OwenScrambledTexture, m_PipelineResources.textures.owenScrambledTex);
            cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._ScramblingTexture, m_PipelineResources.textures.scramblingTex);

            int frameIndex = hdCamera.IsTAAEnabled() ? hdCamera.taaFrameIndex : (int)frameCount % 8;
            cmd.SetGlobalInt(HDShaderIDs._RaytracingFrameIndex, frameIndex);

            // Inject the ray generation data
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayBias, rtEnvironement.rayBias);

            int numLights = m_LightLoop.m_lightList.lights.Count;

            for(int lightIdx = 0; lightIdx < numLights; ++lightIdx)
            {
                // If this is not a rectangular area light or it won't have shadows, skip it
                if(m_LightLoop.m_lightList.lights[lightIdx].lightType != GPULightType.Rectangle || m_LightLoop.m_lightList.lights[lightIdx].shadowIndex == -1) continue;
                using (new ProfilingSample(cmd, "Raytrace Area Shadow", CustomSamplerId.RaytracingShadowIntegration.GetSampler()))
                {
                    LightData currentLight = m_LightLoop.m_lightList.lights[lightIdx];
                    
                    // We need to build the world to area light matrix
                    worldToLocalArea.SetColumn(0, currentLight.right);
                    worldToLocalArea.SetColumn(1, currentLight.up);
                    worldToLocalArea.SetColumn(2, currentLight.forward);

                    // Compensate the  relative rendering if active
                    Vector3 lightPositionWS = currentLight.positionRWS;
                    if (ShaderConfig.s_CameraRelativeRendering != 0)
                    {
                        lightPositionWS += hdCamera.camera.transform.position;
                    }
                    worldToLocalArea.SetColumn(3, lightPositionWS);
                    worldToLocalArea.m33 = 1.0f;
                    worldToLocalArea =  worldToLocalArea.inverse;

                    // Inject the light data
                    cmd.SetRaytracingBufferParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._LightDatas, m_LightLoop.lightDatas);
                    cmd.SetRaytracingIntParam(shadowsShader, HDShaderIDs._RaytracingTargetAreaLight, lightIdx);
                    cmd.SetRaytracingIntParam(shadowsShader, HDShaderIDs._RaytracingNumSamples, rtEnvironement.shadowNumSamples);
                    cmd.SetRaytracingMatrixParam(shadowsShader, HDShaderIDs._RaytracingAreaWorldToLocal, worldToLocalArea);
                    
                    // Set the data for the ray generation
                    cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                    cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._GBufferTexture[0], m_GbufferManager.GetBuffer(0));
                    cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._GBufferTexture[1], m_GbufferManager.GetBuffer(1));
                    cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._GBufferTexture[2], m_GbufferManager.GetBuffer(2));
                    cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._GBufferTexture[3], m_GbufferManager.GetBuffer(3));
                    cmd.SetRaytracingIntParam(shadowsShader, HDShaderIDs._RayCountEnabled, m_RaytracingManager.rayCountManager.rayCountEnabled);
                    cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._RayCountTexture, m_RaytracingManager.rayCountManager.rayCountTex);

                    // Set the output textures
                    cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, _SNBuffer, m_SNBuffer);
                    cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, _UNBuffer, m_UNBuffer);
                    cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, _UBuffer, m_UBuffer);


                    // Run the shadow evaluation
                    cmd.DispatchRays(shadowsShader, m_RayGenShaderName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, 1);
                }

                using (new ProfilingSample(cmd, "Combine Area Shadow", CustomSamplerId.RaytracingShadowCombination.GetSampler()))
                {
                    // Inject all the parameters for the compute
                    cmd.SetComputeTextureParam(bilateralFilter, m_KernelFilter, _SNBuffer, m_SNBuffer);
                    cmd.SetComputeTextureParam(bilateralFilter, m_KernelFilter, _UNBuffer, m_UNBuffer);
                    cmd.SetComputeTextureParam(bilateralFilter, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetComputeTextureParam(bilateralFilter, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                    cmd.SetComputeIntParam(bilateralFilter, _DenoiseRadius, rtEnvironement.shadowFilterRadius);
                    cmd.SetComputeFloatParam(bilateralFilter, _GaussianSigma, rtEnvironement.shadowFilterSigma);
                    cmd.SetComputeIntParam(bilateralFilter, HDShaderIDs._RaytracingShadowSlot, m_LightLoop.m_lightList.lights[lightIdx].shadowIndex);

                    // Set the output slot
                    cmd.SetComputeTextureParam(bilateralFilter, m_KernelFilter, HDShaderIDs._AreaShadowTexture, m_AreaShadowTextureArray);

                    // Texture dimensions
                    int texWidth = m_AreaShadowTextureArray.rt.width;
                    int texHeight = m_AreaShadowTextureArray.rt.width;

                    // Evaluate the dispatch parameters
                    int areaTileSize = 8;
                    int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
                    int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

                    // Compute the texture
                    cmd.DispatchCompute(bilateralFilter, m_KernelFilter, numTilesX, numTilesY, 1);
                }
            }
            return true;
        }
    }
#endif
}
