using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
#if ENABLE_RAYTRACING
    using RTHandle = RTHandleSystem.RTHandle;

    public partial class HDRenderPipeline
    {
        // Direction/Distance buffers
        ComputeBuffer m_RayBinResult = null;
        ComputeBuffer m_RayBinSizeResult = null;

        // Structure that holds the buffers
        GBufferManager m_RaytracingGBufferManager;

        const string m_RayGenGBuffer = "RayGenGBuffer";
        const string m_RayGenGBufferHalfRes = "RayGenGBufferHalfRes";
        const string m_RayGenGBufferBinned = "RayGenGBufferBinned";
        const string m_RayGenGBufferHalfResBinned = "RayGenGBufferHalfResBinned";
        const string m_MissShaderNameGBuffer = "MissShaderGBuffer";

        public void InitRaytracingDeferred()
        {
            m_RayBinResult = new ComputeBuffer(1, sizeof(uint));
            m_RayBinSizeResult = new ComputeBuffer(1, sizeof(uint));

            // Buffer manager used to do the split integration
            m_RaytracingGBufferManager = new GBufferManager(asset, m_DeferredMaterial);
            m_RaytracingGBufferManager.CreateBuffers();
        }

        public void ReleaseRayTracingDeferred()
        {
            CoreUtils.SafeRelease(m_RayBinResult);
            CoreUtils.SafeRelease(m_RayBinSizeResult);

            m_RaytracingGBufferManager.DestroyBuffers();
        }

        public void RenderRaytracingDeferredLighting(CommandBuffer cmd, HDCamera hdCamera, HDRaytracingEnvironment rtEnvironment,
         RTHandle directionBuffer, bool rayBinning, LayerMask layerMask, float maxRayLength, RTHandle outputBuffer, bool disableSpecularLighting = false, bool halfResolution = false)
        {
            ComputeShader rayBinningCS = m_Asset.renderPipelineRayTracingResources.rayBinningCS;
            RayTracingShader gBufferRaytracingRT = m_Asset.renderPipelineRayTracingResources.gBufferRaytracingRT;
            ComputeShader deferredRaytracingCS = m_Asset.renderPipelineRayTracingResources.deferredRaytracingCS;

            // Texture dimensions
            int texWidth = hdCamera.actualWidth;
            int texHeight = hdCamera.actualHeight;

            if (halfResolution)
            {
                texWidth /= 2;
                texHeight /= 2;
            }
            // Evaluate the dispatch parameters
            int rayTileSize = 16;
            int numTilesRayBinX = (texWidth + (rayTileSize - 1)) / rayTileSize;
            int numTilesRayBinY = (texHeight + (rayTileSize - 1)) / rayTileSize;

            int bufferSizeX = numTilesRayBinX * rayTileSize;
            int bufferSizeY = numTilesRayBinY * rayTileSize;

            int currentKernel = 0;
            if (rayBinning)
            {
                // We need to go through the ray binning pass (if required)
                currentKernel = rayBinningCS.FindKernel(halfResolution? "RayBinningHalf" : "RayBinning");

                if (bufferSizeX * bufferSizeY > m_RayBinResult.count)
                {
                    if(m_RayBinResult != null)
                    {
                        CoreUtils.SafeRelease(m_RayBinResult);
                        CoreUtils.SafeRelease(m_RayBinSizeResult);
                        m_RayBinResult = null;
                        m_RayBinSizeResult = null;
                    }

                    if (bufferSizeX * bufferSizeY > 0)
                    {
                        m_RayBinResult = new ComputeBuffer(bufferSizeX * bufferSizeY, sizeof(uint));
                        m_RayBinSizeResult = new ComputeBuffer(numTilesRayBinX * numTilesRayBinY, sizeof(uint));
                    }
                }

                cmd.SetComputeTextureParam(rayBinningCS, currentKernel, HDShaderIDs._RaytracingDirectionBuffer, directionBuffer);
                cmd.SetComputeBufferParam(rayBinningCS, currentKernel, HDShaderIDs._RayBinResult, m_RayBinResult);
                cmd.SetComputeBufferParam(rayBinningCS, currentKernel, HDShaderIDs._RayBinSizeResult, m_RayBinSizeResult);
                cmd.SetComputeIntParam(rayBinningCS, HDShaderIDs._RayBinTileCountX, numTilesRayBinX);
                cmd.DispatchCompute(rayBinningCS, currentKernel, numTilesRayBinX, numTilesRayBinY, 1);
            }

            // Define the shader pass to use for the reflection pass
            cmd.SetRayTracingShaderPass(gBufferRaytracingRT, "GBufferDXR");

            if(rayBinning)
            {
                cmd.SetGlobalBuffer(HDShaderIDs._RayBinResult, m_RayBinResult);
                cmd.SetGlobalBuffer(HDShaderIDs._RayBinSizeResult, m_RayBinSizeResult);
                cmd.SetRayTracingIntParam(gBufferRaytracingRT, HDShaderIDs._RayBinTileCountX, numTilesRayBinX);
            }

            // Grab the acceleration structures and the light cluster to use
            RayTracingAccelerationStructure accelerationStructure = m_RayTracingManager.RequestAccelerationStructure(layerMask);
            HDRaytracingLightCluster lightCluster = m_RayTracingManager.RequestLightCluster(layerMask);

            // Set the acceleration structure for the pass
            cmd.SetRayTracingAccelerationStructure(gBufferRaytracingRT, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

            // Bind the textures required for the ray launching
            cmd.SetRayTracingTextureParam(gBufferRaytracingRT, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetRayTracingTextureParam(gBufferRaytracingRT, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetRayTracingTextureParam(gBufferRaytracingRT, HDShaderIDs._RaytracingDirectionBuffer, directionBuffer);

            // Compute the pixel spread value
            float pixelSpreadAngle = hdCamera.camera.fieldOfView * (Mathf.PI / 180.0f) / Mathf.Min(hdCamera.actualWidth, hdCamera.actualHeight);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingPixelSpreadAngle, pixelSpreadAngle);

            // Additional ray launch values
            cmd.SetRayTracingFloatParams(gBufferRaytracingRT, HDShaderIDs._RaytracingRayBias, rtEnvironment.rayBias);
            cmd.SetRayTracingFloatParams(gBufferRaytracingRT, HDShaderIDs._RaytracingRayMaxLength, maxRayLength);

            // Bind the output textures
            cmd.SetRayTracingTextureParam(gBufferRaytracingRT, HDShaderIDs._GBufferTextureRW[0], m_RaytracingGBufferManager.GetBuffer(0));
            cmd.SetRayTracingTextureParam(gBufferRaytracingRT, HDShaderIDs._GBufferTextureRW[1], m_RaytracingGBufferManager.GetBuffer(1));
            cmd.SetRayTracingTextureParam(gBufferRaytracingRT, HDShaderIDs._GBufferTextureRW[2], m_RaytracingGBufferManager.GetBuffer(2));
            cmd.SetRayTracingTextureParam(gBufferRaytracingRT, HDShaderIDs._GBufferTextureRW[3], m_RaytracingGBufferManager.GetBuffer(3));
            // cmd.SetRaytracingTextureParam(gBufferRaytracingRT, rayGenGBuffer, HDShaderIDs._GBufferTextureRW[4], m_LocalGBufferManager.GetBuffer(4));
            // cmd.SetRaytracingTextureParam(gBufferRaytracingRT, rayGenGBuffer, HDShaderIDs._GBufferTextureRW[5], m_LocalGBufferManager.GetBuffer(5));
            cmd.SetRayTracingTextureParam(gBufferRaytracingRT, HDShaderIDs._RaytracingDistanceBuffer, m_RaytracingDistanceBuffer);

            // Compute the actual resolution that is needed base on the quality
            uint widthResolution = (uint)hdCamera.actualWidth;
            uint heightResolution = (uint)hdCamera.actualHeight;

            if (disableSpecularLighting)
            {
                cmd.SetGlobalInt(HDShaderIDs._EnableSpecularLighting, 0);
            }

            if (rayBinning)
            {
                cmd.DispatchRays(gBufferRaytracingRT, m_RayGenGBufferBinned, (uint)bufferSizeX, (uint)bufferSizeY, 1);
            }
            else
            {
                cmd.SetRayTracingIntParams(gBufferRaytracingRT, "_RaytracingHalfResolution", halfResolution? 1 : 0);
                cmd.DispatchRays(gBufferRaytracingRT, m_RayGenGBuffer, widthResolution, heightResolution, 1);
            }

            // Now let's do the deferred shading pass on the samples
            currentKernel = deferredRaytracingCS.FindKernel(halfResolution ? "RaytracingDeferredHalf" : "RaytracingDeferred");

            LightCluster lightClusterSettings = VolumeManager.instance.stack.GetComponent<LightCluster>();

            cmd.SetComputeBufferParam(deferredRaytracingCS, currentKernel, HDShaderIDs._RaytracingLightCluster, lightCluster.GetCluster());
            cmd.SetComputeBufferParam(deferredRaytracingCS, currentKernel, HDShaderIDs._LightDatasRT, lightCluster.GetLightDatas());
            cmd.SetComputeVectorParam(deferredRaytracingCS, HDShaderIDs._MinClusterPos, lightCluster.GetMinClusterPos());
            cmd.SetComputeVectorParam(deferredRaytracingCS, HDShaderIDs._MaxClusterPos, lightCluster.GetMaxClusterPos());
            cmd.SetComputeIntParam(deferredRaytracingCS, HDShaderIDs._LightPerCellCount, lightClusterSettings.maxNumLightsPercell.value);
            cmd.SetComputeIntParam(deferredRaytracingCS, HDShaderIDs._PunctualLightCountRT, lightCluster.GetPunctualLightCount());
            cmd.SetComputeIntParam(deferredRaytracingCS, HDShaderIDs._AreaLightCountRT, lightCluster.GetAreaLightCount());

            cmd.SetComputeTextureParam(deferredRaytracingCS, currentKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());

            cmd.SetComputeTextureParam(deferredRaytracingCS, currentKernel, HDShaderIDs._RaytracingDirectionBuffer, directionBuffer);
            cmd.SetComputeTextureParam(deferredRaytracingCS, currentKernel, HDShaderIDs._RaytracingDistanceBuffer, m_RaytracingDistanceBuffer);

            cmd.SetComputeTextureParam(deferredRaytracingCS, currentKernel, HDShaderIDs._GBufferTexture[0], m_RaytracingGBufferManager.GetBuffer(0));
            cmd.SetComputeTextureParam(deferredRaytracingCS, currentKernel, HDShaderIDs._GBufferTexture[1], m_RaytracingGBufferManager.GetBuffer(1));
            cmd.SetComputeTextureParam(deferredRaytracingCS, currentKernel, HDShaderIDs._GBufferTexture[2], m_RaytracingGBufferManager.GetBuffer(2));
            cmd.SetComputeTextureParam(deferredRaytracingCS, currentKernel, HDShaderIDs._GBufferTexture[3], m_RaytracingGBufferManager.GetBuffer(3));
            cmd.SetComputeTextureParam(deferredRaytracingCS, currentKernel, HDShaderIDs._LightLayersTexture, TextureXR.GetWhiteTexture());
            cmd.SetComputeTextureParam(deferredRaytracingCS, currentKernel, HDShaderIDs._RaytracingLitBufferRW, outputBuffer);

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesXHR = (texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesYHR = (texHeight + (areaTileSize - 1)) / areaTileSize;

            // Compute the texture
            cmd.DispatchCompute(deferredRaytracingCS, currentKernel, numTilesXHR, numTilesYHR, 1);

            if (disableSpecularLighting)
            {
                cmd.SetGlobalInt(HDShaderIDs._EnableSpecularLighting, hdCamera.frameSettings.IsEnabled(FrameSettingsField.SpecularLighting) ? 1 : 0);
            }
        }
    }
#endif
}
