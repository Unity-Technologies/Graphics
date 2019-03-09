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

        // Buffers that hold the intermediate data of the shadow algorithm
        RTHandleSystem.RTHandle m_AnalyticBuffer = null;
        RTHandleSystem.RTHandle m_DenoiseBuffer0 = null;
        RTHandleSystem.RTHandle m_DenoiseBuffer1 = null;

        // Array that holds the shadow textures for the area lights
        RTHandleSystem.RTHandle m_AreaShadowTextureArray = null;

        // String values
        const string m_RayGenShaderName = "RayGenShadows";
        const string m_MissShaderName = "MissShaderShadows";

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
            m_AnalyticBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "AnalyticBuffer");
            m_DenoiseBuffer0 = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "DenoiseBuffer0");
            m_DenoiseBuffer1 = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "DenoiseBuffer1");
            m_AreaShadowTextureArray = RTHandles.Alloc(Vector2.one, slices:4, dimension:TextureDimension.Tex2DArray, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "AreaShadowArrayBuffer");
        }

        public RTHandleSystem.RTHandle GetIntegrationTexture()
        {
            return m_DenoiseBuffer0;
        }

        public void Release()
        {
            RTHandles.Release(m_AreaShadowTextureArray);
            RTHandles.Release(m_DenoiseBuffer0);
            RTHandles.Release(m_DenoiseBuffer1);
            RTHandles.Release(m_AnalyticBuffer);
        }

        void BindShadowTexture(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._AreaShadowTexture, m_AreaShadowTextureArray);
        }

        static RTHandleSystem.RTHandle AreaShadowHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, slices:4, dimension:TextureDimension.Tex2DArray, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16G16_SFloat,
                enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: string.Format("AreaShadowHistoryBuffer{0}", frameIndex));
        }

        public bool RenderAreaShadows(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, uint frameCount)
        {
            // NOTE: Here we cannot clear the area shadow texture because it is a texture array. So we need to bind it and make sure no material will try to read it in the shaders
            BindShadowTexture(cmd);

            // Let's check all the resources and states to see if we should render the effect
            HDRaytracingEnvironment rtEnvironement = m_RaytracingManager.CurrentEnvironment();
            RaytracingShader shadowsShader = m_PipelineAsset.renderPipelineResources.shaders.shadowsRaytracing;
            ComputeShader shadowFilter = m_PipelineAsset.renderPipelineResources.shaders.areaBillateralFilterCS;
            bool invalidState = rtEnvironement == null || rtEnvironement.raytracedShadows == false || 
					hdCamera.frameSettings.litShaderMode != LitShaderMode.Deferred || shadowsShader == null || 
					shadowFilter == null || m_PipelineResources.textures.owenScrambledTex == null || 
					m_PipelineResources.textures.scramblingTex == null || 
					((hdCamera.camera.cameraType == CameraType.SceneView) && !rtEnvironement.raytraceSceneCamera);

            // If invalid state or ray-tracing acceleration structure, we stop right away
            if (invalidState)
                return false;

            // Grab the TAA history buffer
            RTHandleSystem.RTHandle areaShadowHistoryArray = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedAreaShadow)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedAreaShadow, AreaShadowHistoryBufferAllocatorFunction, 1);

            // Grab the acceleration structure for the target camera
            RaytracingAccelerationStructure accelerationStructure = m_RaytracingManager.RequestAccelerationStructure(rtEnvironement.shadowLayerMask);

            // Define the shader pass to use for the reflection pass
            cmd.SetRaytracingShaderPass(shadowsShader, "VisibilityDXR");

            // Set the acceleration structure for the pass
            cmd.SetRaytracingAccelerationStructure(shadowsShader, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

            // Inject the ray-tracing sampling data
            cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._OwenScrambledTexture, m_PipelineResources.textures.owenScrambledTex);
            cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._ScramblingTexture, m_PipelineResources.textures.scramblingTex);

            int frameIndex = hdCamera.IsTAAEnabled() ? hdCamera.taaFrameIndex : (int)frameCount % 8;
            cmd.SetGlobalInt(HDShaderIDs._RaytracingFrameIndex, frameIndex);

            // Grab the kernels
            int copyTAAHistoryKernel = shadowFilter.FindKernel("AreaShadowCopyTAAHistory");
            int applyTAAKernel       = shadowFilter.FindKernel("AreaShadowApplyTAA");
            int estimateNoiseKernel  = shadowFilter.FindKernel("AreaShadowEstimateNoise");
            int firstDenoiseKernel   = shadowFilter.FindKernel("AreaShadowDenoiseFirstPass");
            int secondDenoiseKernel  = shadowFilter.FindKernel("AreaShadowDenoiseSecondPass");

            // Inject the ray generation data
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayBias, rtEnvironement.rayBias);

            int numLights = m_LightLoop.m_lightList.lights.Count;

            for(int lightIdx = 0; lightIdx < numLights; ++lightIdx)
            {
                // If this is not a rectangular area light or it won't have shadows, skip it
                if(m_LightLoop.m_lightList.lights[lightIdx].lightType != GPULightType.Rectangle || m_LightLoop.m_lightList.lights[lightIdx].rayTracedAreaShadowIndex == -1) continue;
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
                    cmd.SetRaytracingIntParam(shadowsShader, HDShaderIDs._RayCountEnabled, m_RaytracingManager.rayCountManager.RayCountIsEnabled());
                    cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._RayCountTexture, m_RaytracingManager.rayCountManager.rayCountTexture);

                    // Bind the area cookie textures to the raytracing shader
                    cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._AreaCookieTextures, m_LightLoop.areaLightCookieManager.GetTexCache());

                    // Set the output textures
                    cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._RaytracedAnalyticBuffer, m_AnalyticBuffer);
                    cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._RaytracedAreaShadowOutput, m_DenoiseBuffer0);

                    // Run the shadow evaluation
                    cmd.DispatchRays(shadowsShader, m_RayGenShaderName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, 1);
                }

                using (new ProfilingSample(cmd, "Combine Area Shadow", CustomSamplerId.RaytracingShadowCombination.GetSampler()))
                {
                    // Texture dimensions
                    int texWidth = hdCamera.actualWidth;
                    int texHeight = hdCamera.actualHeight;

                    // Evaluate the dispatch parameters
                    int areaTileSize = 8;
                    int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
                    int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

                    // Global parameters
                    cmd.SetComputeIntParam(shadowFilter, HDShaderIDs._RaytracingDenoiseRadius, rtEnvironement.shadowFilterRadius);
                    cmd.SetComputeIntParam(shadowFilter, HDShaderIDs._RaytracingShadowSlot, m_LightLoop.m_lightList.lights[lightIdx].rayTracedAreaShadowIndex);

                    // Given that we can't read and write into the same buffer, we store the current frame value and the history in the denoisebuffer1
                    cmd.SetComputeTextureParam(shadowFilter, copyTAAHistoryKernel, HDShaderIDs._AreaShadowHistoryRW, areaShadowHistoryArray);
                    cmd.SetComputeTextureParam(shadowFilter, copyTAAHistoryKernel, HDShaderIDs._DenoiseInputTexture, m_DenoiseBuffer0);
                    cmd.SetComputeTextureParam(shadowFilter, copyTAAHistoryKernel, HDShaderIDs._DenoiseOutputTextureRW, m_DenoiseBuffer1);
                    cmd.DispatchCompute(shadowFilter, copyTAAHistoryKernel, numTilesX, numTilesY, 1);

                    // Apply a vectorized temporal filtering pass and store it back in the denoisebuffer0 with the analytic value in the third channel
                    var historyScale = new Vector2(hdCamera.actualWidth / (float)areaShadowHistoryArray.rt.width, hdCamera.actualHeight / (float)areaShadowHistoryArray.rt.height);
                    cmd.SetComputeVectorParam(shadowFilter, HDShaderIDs._ScreenToTargetScaleHistory, historyScale);
                    cmd.SetComputeTextureParam(shadowFilter, applyTAAKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetComputeTextureParam(shadowFilter, applyTAAKernel, HDShaderIDs._RaytracedAnalyticBuffer, m_AnalyticBuffer);
                    cmd.SetComputeTextureParam(shadowFilter, applyTAAKernel, HDShaderIDs._DenoiseInputTexture, m_DenoiseBuffer1);
                    cmd.SetComputeTextureParam(shadowFilter, applyTAAKernel, HDShaderIDs._DenoiseOutputTextureRW, m_DenoiseBuffer0);
                    cmd.SetComputeTextureParam(shadowFilter, applyTAAKernel, HDShaderIDs._AreaShadowHistoryRW, areaShadowHistoryArray);
                    cmd.DispatchCompute(shadowFilter, applyTAAKernel, numTilesX, numTilesY, 1);

                    if (rtEnvironement.shadowFilterRadius > 0)
                    {
                        // Inject parameters for noise estimation
                        cmd.SetComputeTextureParam(shadowFilter, estimateNoiseKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                        cmd.SetComputeTextureParam(shadowFilter, estimateNoiseKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                        cmd.SetComputeTextureParam(shadowFilter, estimateNoiseKernel, HDShaderIDs._ScramblingTexture, m_PipelineResources.textures.scramblingTex);

                        // Noise estimation pre-pass
                        cmd.SetComputeTextureParam(shadowFilter, estimateNoiseKernel, HDShaderIDs._DenoiseInputTexture, m_DenoiseBuffer0);
                        cmd.SetComputeTextureParam(shadowFilter, estimateNoiseKernel, HDShaderIDs._DenoiseOutputTextureRW, m_DenoiseBuffer1);
                        cmd.DispatchCompute(shadowFilter, estimateNoiseKernel, numTilesX, numTilesY, 1);

                        // Reinject parameters for denoising
                        cmd.SetComputeTextureParam(shadowFilter, firstDenoiseKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                        cmd.SetComputeTextureParam(shadowFilter, firstDenoiseKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                        cmd.SetComputeTextureParam(shadowFilter, firstDenoiseKernel, HDShaderIDs._AreaShadowTextureRW, m_AreaShadowTextureArray);

                        // First denoising pass
                        cmd.SetComputeTextureParam(shadowFilter, firstDenoiseKernel, HDShaderIDs._DenoiseInputTexture, m_DenoiseBuffer1);
                        cmd.SetComputeTextureParam(shadowFilter, firstDenoiseKernel, HDShaderIDs._DenoiseOutputTextureRW, m_DenoiseBuffer0);
                        cmd.DispatchCompute(shadowFilter, firstDenoiseKernel, numTilesX, numTilesY, 1);
                    }

                    // Reinject parameters for denoising
                    cmd.SetComputeTextureParam(shadowFilter, secondDenoiseKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetComputeTextureParam(shadowFilter, secondDenoiseKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                    cmd.SetComputeTextureParam(shadowFilter, secondDenoiseKernel, HDShaderIDs._AreaShadowTextureRW, m_AreaShadowTextureArray);

                    // Second (and final) denoising pass
                    cmd.SetComputeTextureParam(shadowFilter, secondDenoiseKernel, HDShaderIDs._DenoiseInputTexture, m_DenoiseBuffer0);
                    cmd.DispatchCompute(shadowFilter, secondDenoiseKernel, numTilesX, numTilesY, 1);
                }
            }
            return true;
        }
    }
#endif
}
