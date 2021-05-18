using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Ray binning buffers
        ComputeBuffer m_RayBinResult = null;
        ComputeBuffer m_RayBinSizeResult = null;

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
        }

        void ReleaseRayTracingDeferred()
        {
            CoreUtils.SafeRelease(m_RayBinResult);
            CoreUtils.SafeRelease(m_RayBinSizeResult);
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
                if (m_RayBinResult != null)
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

        // The set of parameters that define our ray tracing deferred lighting pass
        struct DeferredLightingRTParameters
        {
            // Generic attributes
            public bool rayBinning;
            public LayerMask layerMask;
            public bool diffuseLightingOnly;
            public bool halfResolution;
            public int rayCountType;
            public float lodBias;

            // Camera data
            public int width;
            public int height;
            public int viewCount;

            // Compute buffers
            public ComputeBuffer rayBinResult;
            public ComputeBuffer rayBinSizeResult;
            public RayTracingAccelerationStructure accelerationStructure;
            public HDRaytracingLightCluster lightCluster;

            // Shaders
            public RayTracingShader gBufferRaytracingRT;
            public ComputeShader deferredRaytracingCS;
            public ComputeShader rayBinningCS;

            public ShaderVariablesRaytracing raytracingCB;
        }

        class DeferredLightingRTRPassData
        {
            public DeferredLightingRTParameters parameters;
            public TextureHandle directionBuffer;
            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public Texture skyTexture;
            public TextureHandle gbuffer0;
            public TextureHandle gbuffer1;
            public TextureHandle gbuffer2;
            public TextureHandle gbuffer3;
            public TextureHandle distanceBuffer;
            public TextureHandle rayCountTexture;
            public TextureHandle litBuffer;
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

        TextureHandle DeferredLightingRT(RenderGraph renderGraph, in DeferredLightingRTParameters parameters, TextureHandle directionBuffer, TextureHandle depthPyramid, TextureHandle normalBuffer, Texture skyTexture, TextureHandle rayCountTexture)
        {
            using (var builder = renderGraph.AddRenderPass<DeferredLightingRTRPassData>("Deferred Lighting Ray Tracing", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingDeferredLighting)))
            {
                builder.EnableAsyncCompute(false);

                passData.parameters = parameters;
                // Input Buffers
                passData.directionBuffer = builder.ReadTexture(directionBuffer);
                passData.depthStencilBuffer = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.skyTexture = skyTexture;

                // Temporary buffers
                passData.gbuffer0 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R8G8B8A8_SRGB, enableRandomWrite = true, name = "GBuffer0" });
                passData.gbuffer1 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite = true, name = "GBuffer1" });
                passData.gbuffer2 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite = true, name = "GBuffer2" });
                passData.gbuffer3 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = Builtin.GetLightingBufferFormat(), enableRandomWrite = true, name = "GBuffer3" });
                passData.distanceBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Distance Buffer" });

                // Output buffers
                passData.rayCountTexture = builder.ReadWriteTexture(rayCountTexture);
                passData.litBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Deferred Lighting Result" }));

                builder.SetRenderFunc(
                    (DeferredLightingRTRPassData data, RenderGraphContext ctx) =>
                    {
                        // Compute the input texture dimension
                        int texWidth = data.parameters.width;
                        int texHeight = data.parameters.height;
                        if (data.parameters.halfResolution)
                        {
                            texWidth /= 2;
                            texHeight /= 2;
                        }

                        if (data.parameters.rayBinning)
                        {
                            BinRays(ctx.cmd, data.parameters, data.directionBuffer, texWidth, texHeight);
                        }

                        // Inject the global parameters
                        data.parameters.raytracingCB._RayTracingLodBias = data.parameters.lodBias;
                        ConstantBuffer.PushGlobal(ctx.cmd, data.parameters.raytracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                        // Define the shader pass to use for the reflection pass
                        ctx.cmd.SetRayTracingShaderPass(data.parameters.gBufferRaytracingRT, "GBufferDXR");

                        if (data.parameters.rayBinning)
                        {
                            int numTilesRayBinX = (texWidth + (binningTileSize - 1)) / binningTileSize;
                            ctx.cmd.SetGlobalBuffer(HDShaderIDs._RayBinResult, data.parameters.rayBinResult);
                            ctx.cmd.SetGlobalBuffer(HDShaderIDs._RayBinSizeResult, data.parameters.rayBinSizeResult);
                            ctx.cmd.SetRayTracingIntParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._RayBinTileCountX, numTilesRayBinX);
                        }

                        // Set the acceleration structure for the pass
                        ctx.cmd.SetRayTracingAccelerationStructure(data.parameters.gBufferRaytracingRT, HDShaderIDs._RaytracingAccelerationStructureName, data.parameters.accelerationStructure);

                        // Set ray count texture
                        ctx.cmd.SetRayTracingIntParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._RayCountType, data.parameters.rayCountType);
                        ctx.cmd.SetRayTracingTextureParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._RayCountTexture, data.rayCountTexture);

                        // Bind all input parameter
                        ctx.cmd.SetRayTracingIntParams(data.parameters.gBufferRaytracingRT, HDShaderIDs._RayTracingLayerMask, data.parameters.layerMask);
                        ctx.cmd.SetRayTracingTextureParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                        ctx.cmd.SetRayTracingTextureParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetRayTracingTextureParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._RaytracingDirectionBuffer, data.directionBuffer);
                        ctx.cmd.SetRayTracingIntParams(data.parameters.gBufferRaytracingRT, HDShaderIDs._RaytracingHalfResolution, data.parameters.halfResolution ? 1 : 0);

                        // Bind the output textures
                        ctx.cmd.SetRayTracingTextureParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._GBufferTextureRW[0], data.gbuffer0);
                        ctx.cmd.SetRayTracingTextureParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._GBufferTextureRW[1], data.gbuffer1);
                        ctx.cmd.SetRayTracingTextureParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._GBufferTextureRW[2], data.gbuffer2);
                        ctx.cmd.SetRayTracingTextureParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._GBufferTextureRW[3], data.gbuffer3);
                        ctx.cmd.SetRayTracingTextureParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._RaytracingDistanceBuffer, data.distanceBuffer);

                        // Compute the actual resolution that is needed base on the resolution
                        uint widthResolution = (uint)data.parameters.width;
                        uint heightResolution = (uint)data.parameters.height;

                        // Include the sky if required
                        ctx.cmd.SetRayTracingTextureParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._SkyTexture, data.skyTexture);

                        // Only compute diffuse lighting if required
                        CoreUtils.SetKeyword(ctx.cmd, "MINIMAL_GBUFFER", data.parameters.diffuseLightingOnly);

                        if (data.parameters.rayBinning)
                        {
                            // Evaluate the dispatch data.parameters
                            int numTilesRayBinX = (texWidth + (binningTileSize - 1)) / binningTileSize;
                            int numTilesRayBinY = (texHeight + (binningTileSize - 1)) / binningTileSize;
                            int bufferSizeX = numTilesRayBinX * binningTileSize;
                            int bufferSizeY = numTilesRayBinY * binningTileSize;
                            ctx.cmd.SetRayTracingIntParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._BufferSizeX, bufferSizeX);

                            // A really nice tip is to dispatch the rays as a 1D array instead of 2D, the performance difference has been measured.
                            uint dispatchSize = (uint)(bufferSizeX * bufferSizeY);
                            ctx.cmd.DispatchRays(data.parameters.gBufferRaytracingRT, m_RayGenGBufferBinned, dispatchSize, 1, 1);
                        }
                        else
                        {
                            ctx.cmd.DispatchRays(data.parameters.gBufferRaytracingRT, m_RayGenGBuffer, widthResolution, heightResolution, (uint)data.parameters.viewCount);
                        }

                        CoreUtils.SetKeyword(ctx.cmd, "MINIMAL_GBUFFER", false);

                        // Now let's do the deferred shading pass on the samples
                        int currentKernel = data.parameters.deferredRaytracingCS.FindKernel(data.parameters.halfResolution ? "RaytracingDeferredHalf" : "RaytracingDeferred");

                        // Bind the lightLoop data
                        data.parameters.lightCluster.BindLightClusterData(ctx.cmd);

                        // Bind the input textures
                        ctx.cmd.SetComputeTextureParam(data.parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                        ctx.cmd.SetComputeTextureParam(data.parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._RaytracingDirectionBuffer, data.directionBuffer);
                        ctx.cmd.SetComputeTextureParam(data.parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._RaytracingDistanceBuffer, data.distanceBuffer);
                        ctx.cmd.SetComputeTextureParam(data.parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._GBufferTexture[0], data.gbuffer0);
                        ctx.cmd.SetComputeTextureParam(data.parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._GBufferTexture[1], data.gbuffer1);
                        ctx.cmd.SetComputeTextureParam(data.parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._GBufferTexture[2], data.gbuffer2);
                        ctx.cmd.SetComputeTextureParam(data.parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._GBufferTexture[3], data.gbuffer3);
                        ctx.cmd.SetComputeTextureParam(data.parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._LightLayersTexture, TextureXR.GetWhiteTexture());

                        // Bind the output texture
                        ctx.cmd.SetComputeTextureParam(data.parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._RaytracingLitBufferRW, data.litBuffer);

                        // Evaluate the dispatch parameters
                        int areaTileSize = 8;
                        int numTilesXHR = (texWidth + (areaTileSize - 1)) / areaTileSize;
                        int numTilesYHR = (texHeight + (areaTileSize - 1)) / areaTileSize;

                        // Compute the texture
                        ctx.cmd.DispatchCompute(data.parameters.deferredRaytracingCS, currentKernel, numTilesXHR, numTilesYHR, data.parameters.viewCount);
                    });

                return passData.litBuffer;
            }
        }
    }
}
