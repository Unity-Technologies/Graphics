using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // The set of parameters that define our ray tracing deferred lighting pass
        struct DeferredLightingRTParameters
        {
            // Generic attributes
            public bool rayBinning;
            public LayerMask layerMask;
            public float rayBias;
            public float maxRayLength;
            public float clampValue;
            public bool includeSky;
            public bool diffuseLightingOnly;
            public bool halfResolution;
            public int rayCountFlag;
            public int rayCountType;
            public bool preExpose;

            // Camera data
            public int width;
            public int height;
            public int viewCount;
            public float fov;

            // Compute buffers
            public ComputeBuffer rayBinResult;
            public ComputeBuffer rayBinSizeResult;
            public RayTracingAccelerationStructure accelerationStructure;
            public HDRaytracingLightCluster lightCluster;

            // Shaders
            public RayTracingShader gBufferRaytracingRT;
            public ComputeShader deferredRaytracingCS;
            public ComputeShader rayBinningCS;
        }

        public struct DeferredLightingRTResources
        {
            // Input Buffer
            public RTHandle directionBuffer;
            public RTHandle depthStencilBuffer;
            public RTHandle normalBuffer;
            public Texture skyTexture;

            // Temporary buffers
            public RTHandle gbuffer0;
            public RTHandle gbuffer1;
            public RTHandle gbuffer2;
            public RTHandle gbuffer3;
            public RTHandle distanceBuffer;

            // Debug textures
            public RTHandle rayCountTexture;

            // Output Buffer
            public RTHandle litBuffer;
        }

        // Ray Direction/Distance buffers
        RTHandle m_RaytracingDirectionBuffer;
        RTHandle m_RaytracingDistanceBuffer;

        // Ray binning buffers
        ComputeBuffer m_RayBinResult = null;
        ComputeBuffer m_RayBinSizeResult = null;

        // Structure that holds the g buffers
        GBufferManager m_RaytracingGBufferManager;

        // The set of ray tracing shader names
        const string m_RayGenGBuffer = "RayGenGBuffer";
        const string m_RayGenGBufferHalfRes = "RayGenGBufferHalfRes";
        const string m_RayGenGBufferBinned = "RayGenGBufferBinned";
        const string m_RayGenGBufferHalfResBinned = "RayGenGBufferHalfResBinned";
        const string m_MissShaderNameGBuffer = "MissShaderGBuffer";

        // Resolution of the binning tile
        const int binningTileSize = 16;

        void InitRaytracingDeferred()
        {
            m_RayBinResult = new ComputeBuffer(1, sizeof(uint));
            m_RayBinSizeResult = new ComputeBuffer(1, sizeof(uint));

            m_RaytracingDirectionBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true,useMipMap: false, name: "RaytracingDirectionBuffer");
            m_RaytracingDistanceBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R32_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "RaytracingDistanceBuffer");

            m_RaytracingGBufferManager = new GBufferManager(asset, m_DeferredMaterial);
            m_RaytracingGBufferManager.CreateBuffers();
        }

        void ReleaseRayTracingDeferred()
        {
            CoreUtils.SafeRelease(m_RayBinResult);
            CoreUtils.SafeRelease(m_RayBinSizeResult);

            RTHandles.Release(m_RaytracingDistanceBuffer);
            RTHandles.Release(m_RaytracingDirectionBuffer);
            
            m_RaytracingGBufferManager.DestroyBuffers();
        }

        DeferredLightingRTResources PrepareDeferredLightingRTResources(HDCamera hdCamera, RTHandle directionBuffer, RTHandle ouputBuffer)
        {
            DeferredLightingRTResources deferredResources = new DeferredLightingRTResources();

            deferredResources.directionBuffer = directionBuffer;
            deferredResources.depthStencilBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            deferredResources.normalBuffer = m_SharedRTManager.GetNormalBuffer();
            deferredResources.skyTexture = m_SkyManager.GetSkyReflection(hdCamera);

            // Temporary buffers
            deferredResources.gbuffer0 = m_RaytracingGBufferManager.GetBuffer(0);
            deferredResources.gbuffer1 = m_RaytracingGBufferManager.GetBuffer(1);
            deferredResources.gbuffer2 = m_RaytracingGBufferManager.GetBuffer(2);
            deferredResources.gbuffer3 = m_RaytracingGBufferManager.GetBuffer(3);
            deferredResources.distanceBuffer = m_RaytracingDistanceBuffer;

            // Debug textures
            deferredResources.rayCountTexture = m_RayCountManager.GetRayCountTexture();

            // Output Buffer
            deferredResources.litBuffer = ouputBuffer;

            return deferredResources;
        }

        void CheckBinningBuffersSize(HDCamera hdCamera)
        {
            // Evaluate the dispatch parameters
            int numTilesRayBinX = (hdCamera.actualWidth + (binningTileSize - 1)) / binningTileSize;
            int numTilesRayBinY = (hdCamera.actualHeight + (binningTileSize - 1)) / binningTileSize;

            int bufferSizeX = numTilesRayBinX * binningTileSize;
            int bufferSizeY = numTilesRayBinY * binningTileSize;

            //  Resize the binning buffers if required
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
        }

        static void BinRays(CommandBuffer cmd, in DeferredLightingRTParameters config, RTHandle directionBuffer, int texWidth, int texHeight)
        {
            // We need to go through the ray binning pass (if required)
            int currentKernel = config.rayBinningCS.FindKernel(config.halfResolution ? "RayBinningHalf" : "RayBinning");

            // Evaluate the dispatch parameters
            int numTilesRayBinX = (texWidth + (binningTileSize - 1)) / binningTileSize;
            int numTilesRayBinY = (texHeight + (binningTileSize - 1)) / binningTileSize;

            int bufferSizeX = numTilesRayBinX * binningTileSize;
            int bufferSizeY = numTilesRayBinY * binningTileSize;

            // Bind the resources
            cmd.SetComputeTextureParam(config.rayBinningCS, currentKernel, HDShaderIDs._RaytracingDirectionBuffer, directionBuffer);
            cmd.SetComputeBufferParam(config.rayBinningCS, currentKernel, HDShaderIDs._RayBinResult, config.rayBinResult);
            cmd.SetComputeBufferParam(config.rayBinningCS, currentKernel, HDShaderIDs._RayBinSizeResult, config.rayBinSizeResult);
            cmd.SetComputeIntParam(config.rayBinningCS, HDShaderIDs._RayBinTileCountX, numTilesRayBinX);

            // Run the binning
            cmd.DispatchCompute(config.rayBinningCS, currentKernel, numTilesRayBinX, numTilesRayBinY, config.viewCount);
        }

        static void RenderRaytracingDeferredLighting(CommandBuffer cmd, in DeferredLightingRTParameters parameters, in DeferredLightingRTResources buffers)
        {
            // Compute the input texture dimension
            int texWidth = parameters.width;
            int texHeight = parameters.height;
            if (parameters.halfResolution)
            {
                texWidth /= 2;
                texHeight /= 2;
            }

            if (parameters.rayBinning)
            {
                BinRays(cmd, parameters, buffers.directionBuffer,  texWidth, texHeight);
            }

            // Define the shader pass to use for the reflection pass
            cmd.SetRayTracingShaderPass(parameters.gBufferRaytracingRT, "GBufferDXR");

            if (parameters.rayBinning)
            {
                int numTilesRayBinX = (texWidth + (binningTileSize - 1)) / binningTileSize;
                cmd.SetGlobalBuffer(HDShaderIDs._RayBinResult, parameters.rayBinResult);
                cmd.SetGlobalBuffer(HDShaderIDs._RayBinSizeResult, parameters.rayBinSizeResult);
                cmd.SetRayTracingIntParam(parameters.gBufferRaytracingRT, HDShaderIDs._RayBinTileCountX, numTilesRayBinX);
            }

            // Set the acceleration structure for the pass
            cmd.SetRayTracingAccelerationStructure(parameters.gBufferRaytracingRT, HDShaderIDs._RaytracingAccelerationStructureName, parameters.accelerationStructure);

            // Set ray count tex
            cmd.SetRayTracingIntParam(parameters.gBufferRaytracingRT, HDShaderIDs._RayCountEnabled, parameters.rayCountFlag);
            cmd.SetRayTracingIntParam(parameters.gBufferRaytracingRT, HDShaderIDs._RayCountType, parameters.rayCountType);
            cmd.SetRayTracingTextureParam(parameters.gBufferRaytracingRT, HDShaderIDs._RayCountTexture, buffers.rayCountTexture);
            
            // Bind all input parameter
            cmd.SetRayTracingFloatParams(parameters.gBufferRaytracingRT, HDShaderIDs._RaytracingRayBias, parameters.rayBias);
            cmd.SetRayTracingIntParams(parameters.gBufferRaytracingRT, HDShaderIDs._RayTracingLayerMask, parameters.layerMask);
            cmd.SetRayTracingFloatParams(parameters.gBufferRaytracingRT, HDShaderIDs._RaytracingRayMaxLength, parameters.maxRayLength);
            cmd.SetRayTracingTextureParam(parameters.gBufferRaytracingRT, HDShaderIDs._DepthTexture, buffers.depthStencilBuffer);
            cmd.SetRayTracingTextureParam(parameters.gBufferRaytracingRT, HDShaderIDs._NormalBufferTexture, buffers.normalBuffer);
            cmd.SetRayTracingTextureParam(parameters.gBufferRaytracingRT, HDShaderIDs._RaytracingDirectionBuffer, buffers.directionBuffer);
            cmd.SetRayTracingFloatParams(parameters.gBufferRaytracingRT, HDShaderIDs._RaytracingPixelSpreadAngle, HDRenderPipeline.GetPixelSpreadAngle(parameters.fov, parameters.width, parameters.height));

            // Bind the output textures
            cmd.SetRayTracingTextureParam(parameters.gBufferRaytracingRT, HDShaderIDs._GBufferTextureRW[0], buffers.gbuffer0);
            cmd.SetRayTracingTextureParam(parameters.gBufferRaytracingRT, HDShaderIDs._GBufferTextureRW[1], buffers.gbuffer1);
            cmd.SetRayTracingTextureParam(parameters.gBufferRaytracingRT, HDShaderIDs._GBufferTextureRW[2], buffers.gbuffer2);
            cmd.SetRayTracingTextureParam(parameters.gBufferRaytracingRT, HDShaderIDs._GBufferTextureRW[3], buffers.gbuffer3);
            cmd.SetRayTracingTextureParam(parameters.gBufferRaytracingRT, HDShaderIDs._RaytracingDistanceBuffer, buffers.distanceBuffer);

            // Compute the actual resolution that is needed base on the resolution
            uint widthResolution = (uint)parameters.width;
            uint heightResolution = (uint)parameters.height;

            // Include the sky if required
            cmd.SetRayTracingIntParam(parameters.gBufferRaytracingRT, HDShaderIDs._RaytracingIncludeSky, parameters.includeSky ? 1 : 0);
            cmd.SetRayTracingTextureParam(parameters.gBufferRaytracingRT, HDShaderIDs._SkyTexture, buffers.skyTexture);

            // Only compute diffuse lighting if required
            CoreUtils.SetKeyword(cmd, "DIFFUSE_LIGHTING_ONLY", parameters.diffuseLightingOnly);
            CoreUtils.SetKeyword(cmd, "MULTI_BOUNCE_INDIRECT", false);

            // All rays are diffuse if we are evaluating diffuse lighting only
            cmd.SetRayTracingIntParams(parameters.gBufferRaytracingRT, HDShaderIDs._RaytracingDiffuseRay, parameters.diffuseLightingOnly ? 1 : 0);

            if (parameters.rayBinning)
            {
                // Evaluate the dispatch parameters
                int numTilesRayBinX = (texWidth + (binningTileSize - 1)) / binningTileSize;
                int numTilesRayBinY = (texHeight + (binningTileSize - 1)) / binningTileSize;

                int bufferSizeX = numTilesRayBinX * binningTileSize;
                int bufferSizeY = numTilesRayBinY * binningTileSize;

                cmd.DispatchRays(parameters.gBufferRaytracingRT, m_RayGenGBufferBinned, (uint)bufferSizeX, (uint)bufferSizeY, 1);
            }
            else
            {
                cmd.SetRayTracingIntParams(parameters.gBufferRaytracingRT, "_RaytracingHalfResolution", parameters.halfResolution ? 1 : 0);
                cmd.DispatchRays(parameters.gBufferRaytracingRT, m_RayGenGBuffer, widthResolution, heightResolution, (uint)parameters.viewCount);
            }

            // Disable the diffuse lighting only flag
            CoreUtils.SetKeyword(cmd, "DIFFUSE_LIGHTING_ONLY", false);

            // Now let's do the deferred shading pass on the samples
            int currentKernel = parameters.deferredRaytracingCS.FindKernel(parameters.halfResolution ? "RaytracingDeferredHalf" : "RaytracingDeferred");

            // Bind the lightLoop data
            parameters.lightCluster.BindLightClusterData(cmd);

            // Bind the input textures
            cmd.SetComputeTextureParam(parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._DepthTexture, buffers.depthStencilBuffer);
            cmd.SetComputeTextureParam(parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._RaytracingDirectionBuffer, buffers.directionBuffer);
            cmd.SetComputeTextureParam(parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._RaytracingDistanceBuffer, buffers.distanceBuffer);
            cmd.SetComputeTextureParam(parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._GBufferTexture[0], buffers.gbuffer0);
            cmd.SetComputeTextureParam(parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._GBufferTexture[1], buffers.gbuffer1);
            cmd.SetComputeTextureParam(parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._GBufferTexture[2], buffers.gbuffer2);
            cmd.SetComputeTextureParam(parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._GBufferTexture[3], buffers.gbuffer3);
            cmd.SetComputeTextureParam(parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._LightLayersTexture, TextureXR.GetWhiteTexture());

            // Inject the other parameters
            cmd.SetComputeFloatParam(parameters.deferredRaytracingCS, HDShaderIDs._RaytracingIntensityClamp, parameters.clampValue);
            cmd.SetComputeIntParam(parameters.deferredRaytracingCS, HDShaderIDs._RaytracingPreExposition, parameters.preExpose ? 1 : 0);

            // Bind the output texture
            cmd.SetComputeTextureParam(parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._RaytracingLitBufferRW, buffers.litBuffer);

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesXHR = (texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesYHR = (texHeight + (areaTileSize - 1)) / areaTileSize;

            // Compute the texture
            cmd.DispatchCompute(parameters.deferredRaytracingCS, currentKernel, numTilesXHR, numTilesYHR, parameters.viewCount);
        }
    }
}
