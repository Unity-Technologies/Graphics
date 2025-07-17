using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

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
            // Grab the number of view we'll need to support
            int numViews = hdCamera.viewCount;

            // Evaluate the number of tiles
            int numTilesRayBinX = (hdCamera.actualWidth + (binningTileSize - 1)) / binningTileSize;
            int numTilesRayBinY = (hdCamera.actualHeight + (binningTileSize - 1)) / binningTileSize;
            int minimalTileBufferSize = numViews * numTilesRayBinX * numTilesRayBinY;

            // Evaluate the global size
            int bufferSizeX = numTilesRayBinX * binningTileSize;
            int bufferSizeY = numTilesRayBinY * binningTileSize;
            int minimalBufferSize = numViews * bufferSizeX * bufferSizeY;

            //  Resize the binning buffers if required
            if (minimalBufferSize > m_RayBinResult.count)
            {
                if (m_RayBinResult != null)
                {
                    CoreUtils.SafeRelease(m_RayBinResult);
                    CoreUtils.SafeRelease(m_RayBinSizeResult);
                    m_RayBinResult = null;
                    m_RayBinSizeResult = null;
                }

                if (minimalBufferSize > 0)
                {
                    m_RayBinResult = new ComputeBuffer(minimalBufferSize, sizeof(uint));
                    m_RayBinSizeResult = new ComputeBuffer(minimalTileBufferSize, sizeof(uint));
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
            public int rayMiss;
            public int lastBounceFallbackHierarchy;

            // Ray marching attributes
            public bool mixedTracing;
            public int raySteps;
            public float nearClipPlane;
            public float farClipPlane;
            public bool transparent;

            // Camera data
            public int width;
            public int height;
            public int viewCount;

            // Compute buffers
            public ComputeBuffer rayBinResult;
            public ComputeBuffer rayBinSizeResult;
            public ComputeBuffer mipChainBuffer;
            public RayTracingAccelerationStructure accelerationStructure;
            public HDRaytracingLightCluster lightCluster;

            // Shaders
            public ComputeShader rayMarchingCS;
            public RayTracingShader gBufferRaytracingRT;
            public ComputeShader deferredRaytracingCS;
            public ComputeShader rayBinningCS;

            public ShaderVariablesRaytracing raytracingCB;
        }

        class DeferredLightingRTRPassData
        {
            public DeferredLightingRTParameters parameters;

            // Prepass textures
            public TextureHandle depthPyramid;
            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle ssgbuffer0;
            public TextureHandle ssgbuffer1;
            public TextureHandle ssgbuffer2;
            public TextureHandle ssgbuffer3;

            // Input textures
            public TextureHandle directionBuffer;
            public TextureHandle renderingLayersTexture;

            // Intermediate textures
            public TextureHandle gbuffer0;
            public TextureHandle gbuffer1;
            public TextureHandle gbuffer2;
            public TextureHandle gbuffer3;
            public TextureHandle tmpDistanceBuffer;

            // Output textures
            public TextureHandle litBuffer;
            public TextureHandle distanceBuffer;
            public TextureHandle rayCountTexture;

            // Data textures
            public Texture skyTexture;

            public bool enableDecals;
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

            // Inject the data for the additional views
            cmd.SetComputeIntParam(config.rayBinningCS, HDShaderIDs._RayBinViewOffset, bufferSizeX * bufferSizeY);
            cmd.SetComputeIntParam(config.rayBinningCS, HDShaderIDs._RayBinTileViewOffset, numTilesRayBinX * numTilesRayBinY);

            // Run the binning
            cmd.DispatchCompute(config.rayBinningCS, currentKernel, numTilesRayBinX, numTilesRayBinY, config.viewCount);
        }

        static void RayMarchGBuffer(CommandBuffer cmd, in DeferredLightingRTRPassData data, int texWidth, int texHeight)
        {
            // Now let's do the deferred shading pass on the samples
            int marchingKernel = data.parameters.rayMarchingCS.FindKernel(data.parameters.halfResolution ? "RayMarchHalfKernel" : "RayMarchKernel");

            // Evaluate the dispatch parameters
            int numTilesRayBinX = (texWidth + (8 - 1)) / 8;
            int numTilesRayBinY = (texHeight + (8 - 1)) / 8;

            // Prepass textures
            if (data.parameters.transparent)
            {
                // Use the transparent pre-pass depth for ray start position, and do not filter using stencil
                cmd.SetComputeTextureParam(data.parameters.rayMarchingCS, marchingKernel, HDShaderIDs._InputDepthTexture, data.depthStencilBuffer, 0, RenderTextureSubElement.Depth);
                cmd.SetComputeIntParam(data.parameters.rayMarchingCS, HDShaderIDs._DeferredStencilBit, 0);
            }
            else
            {
                // Use the depth pyramid for ray start position (since it is already bound for hit testing), filter pixels using stencil
                cmd.SetComputeTextureParam(data.parameters.rayMarchingCS, marchingKernel, HDShaderIDs._InputDepthTexture, data.depthPyramid);
                cmd.SetComputeIntParam(data.parameters.rayMarchingCS, HDShaderIDs._DeferredStencilBit, (int)StencilUsage.RequiresDeferredLighting);
            }
            cmd.SetComputeTextureParam(data.parameters.rayMarchingCS, marchingKernel, HDShaderIDs._DepthTexture, data.depthPyramid);
            cmd.SetComputeTextureParam(data.parameters.rayMarchingCS, marchingKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
            cmd.SetComputeTextureParam(data.parameters.rayMarchingCS, marchingKernel, HDShaderIDs._StencilTexture, data.depthStencilBuffer, 0, RenderTextureSubElement.Stencil);
            cmd.SetComputeBufferParam(data.parameters.rayMarchingCS, marchingKernel, HDShaderIDs._DepthPyramidMipLevelOffsets, data.parameters.mipChainBuffer);

            // Bind the input parameters
            float n = data.parameters.nearClipPlane;
            float f = data.parameters.farClipPlane;
            float thicknessScale = 1.0f / (1.0f + 0.01f);
            float thicknessBias = -n / (f - n) * (0.01f * thicknessScale);
            cmd.SetComputeFloatParam(data.parameters.rayMarchingCS, HDShaderIDs._RayMarchingThicknessScale, thicknessScale);
            cmd.SetComputeFloatParam(data.parameters.rayMarchingCS, HDShaderIDs._RayMarchingThicknessBias, thicknessBias);
            cmd.SetComputeIntParam(data.parameters.rayMarchingCS, HDShaderIDs._RayMarchingSteps, data.parameters.raySteps);
            cmd.SetComputeIntParam(data.parameters.rayMarchingCS, HDShaderIDs._RayMarchingReflectSky, 0);
            cmd.SetComputeTextureParam(data.parameters.rayMarchingCS, marchingKernel, HDShaderIDs._RaytracingDirectionBuffer, data.directionBuffer);
            cmd.SetComputeTextureParam(data.parameters.rayMarchingCS, marchingKernel, HDShaderIDs._GBufferTexture[0], data.ssgbuffer0);
            cmd.SetComputeTextureParam(data.parameters.rayMarchingCS, marchingKernel, HDShaderIDs._GBufferTexture[1], data.ssgbuffer1);
            cmd.SetComputeTextureParam(data.parameters.rayMarchingCS, marchingKernel, HDShaderIDs._GBufferTexture[2], data.ssgbuffer2);
            cmd.SetComputeTextureParam(data.parameters.rayMarchingCS, marchingKernel, HDShaderIDs._GBufferTexture[3], data.ssgbuffer3);

            // Bind the output textures
            cmd.SetComputeTextureParam(data.parameters.rayMarchingCS, marchingKernel, HDShaderIDs._GBufferTextureRW[0], data.gbuffer0);
            cmd.SetComputeTextureParam(data.parameters.rayMarchingCS, marchingKernel, HDShaderIDs._GBufferTextureRW[1], data.gbuffer1);
            cmd.SetComputeTextureParam(data.parameters.rayMarchingCS, marchingKernel, HDShaderIDs._GBufferTextureRW[2], data.gbuffer2);
            cmd.SetComputeTextureParam(data.parameters.rayMarchingCS, marchingKernel, HDShaderIDs._GBufferTextureRW[3], data.gbuffer3);
            cmd.SetComputeTextureParam(data.parameters.rayMarchingCS, marchingKernel, HDShaderIDs._RaytracingDistanceBuffer, data.tmpDistanceBuffer);

            // Run the ray marching
            cmd.DispatchCompute(data.parameters.rayMarchingCS, marchingKernel, numTilesRayBinX, numTilesRayBinY, data.parameters.viewCount);
        }

        struct RayTracingDefferedLightLoopOutput
        {
            public TextureHandle lightingBuffer;
            public TextureHandle distanceBuffer;
        }

        RayTracingDefferedLightLoopOutput DeferredLightingRT(RenderGraph renderGraph,
            HDCamera hdCamera,
            in DeferredLightingRTParameters parameters,
            TextureHandle directionBuffer,
            in PrepassOutput prepassOutput,
            Texture skyTexture,
            TextureHandle rayCountTexture)
        {
            RayTracingDefferedLightLoopOutput output = new RayTracingDefferedLightLoopOutput();
            using (var builder = renderGraph.AddUnsafePass<DeferredLightingRTRPassData>("Deferred Lighting Ray Tracing", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingDeferredLighting)))
            {
                passData.parameters = parameters;

                // Input Buffers
                passData.directionBuffer = directionBuffer;
                builder.UseTexture(passData.directionBuffer, AccessFlags.Read);
                passData.depthStencilBuffer = prepassOutput.depthBuffer;
                builder.UseTexture(passData.depthStencilBuffer, AccessFlags.Read);
                passData.depthPyramid = prepassOutput.depthPyramidTexture;
                builder.UseTexture(passData.depthPyramid, AccessFlags.Read);
                passData.normalBuffer = prepassOutput.normalBuffer;
                builder.UseTexture(passData.normalBuffer, AccessFlags.Read);
                passData.renderingLayersTexture = renderGraph.defaultResources.whiteTextureXR;
                builder.UseTexture(passData.renderingLayersTexture, AccessFlags.Read);

                if (passData.parameters.mixedTracing)
                {
                    passData.ssgbuffer0 = prepassOutput.gbuffer.mrt[0];
                    passData.ssgbuffer1 = prepassOutput.gbuffer.mrt[1];
                    passData.ssgbuffer2 = prepassOutput.gbuffer.mrt[2];
                    passData.ssgbuffer3 = prepassOutput.gbuffer.mrt[3];
                }
                else
                {
                    passData.ssgbuffer0 = renderGraph.defaultResources.blackTextureXR;
                    passData.ssgbuffer1 = renderGraph.defaultResources.blackTextureXR;
                    passData.ssgbuffer2 = renderGraph.defaultResources.blackTextureXR;
                    passData.ssgbuffer3 = renderGraph.defaultResources.blackTextureXR;
                }
                builder.UseTexture(passData.ssgbuffer0, AccessFlags.Read);
                builder.UseTexture(passData.ssgbuffer1, AccessFlags.Read);
                builder.UseTexture(passData.ssgbuffer2, AccessFlags.Read);
                builder.UseTexture(passData.ssgbuffer3, AccessFlags.Read);

                passData.skyTexture = skyTexture;

                // Temporary buffers
                passData.gbuffer0 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R8G8B8A8_SRGB, enableRandomWrite = true, name = "GBuffer0" });
                passData.gbuffer1 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite = true, name = "GBuffer1" });
                passData.gbuffer2 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite = true, name = "GBuffer2" });
                passData.gbuffer3 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { format = Builtin.GetLightingBufferFormat(), enableRandomWrite = true, name = "GBuffer3" });
                passData.tmpDistanceBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "TMP Distance Buffer" });

                // Output buffers
                passData.rayCountTexture = rayCountTexture;
                builder.UseTexture(passData.rayCountTexture, AccessFlags.ReadWrite);
                passData.litBuffer = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Deferred Lighting Result" });
                builder.UseTexture(passData.litBuffer, AccessFlags.Write);
                passData.distanceBuffer = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Distance Buffer" });
                builder.UseTexture(passData.distanceBuffer, AccessFlags.Write);

                passData.enableDecals = hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals);

                builder.SetRenderFunc(
                    (DeferredLightingRTRPassData data, UnsafeGraphContext ctx) =>
                    {
                        var natCmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                        // Compute the input texture dimension
                        int texWidth = data.parameters.width;
                        int texHeight = data.parameters.height;
                        if (data.parameters.halfResolution)
                        {
                            // Given that the up-sampling if half resolution potentially needs the last/extra pixel (if the resolution is odd), we need to make sure
                            // that the ray tracing and lighting is evaluated with that in mind.
                            texWidth = (texWidth + 1) / 2;
                            texHeight = (texHeight + 1) / 2;
                        }

                        // Inject the global parameters
                        ConstantBuffer.PushGlobal(natCmd, data.parameters.raytracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                        // If mixed ray tracing was enabled for this pass, we need to try to resolve as many pixels as possible
                        // using the depth buffer ray marching
                        if (data.parameters.mixedTracing)
                        {
                            RayMarchGBuffer(natCmd, data, texWidth, texHeight);
                        }

                        if (data.parameters.rayBinning)
                        {
                            BinRays(natCmd, data.parameters, data.directionBuffer, texWidth, texHeight);
                        }

                        // Inject the global parameters
                        data.parameters.raytracingCB._RayTracingLodBias = data.parameters.lodBias;
                        ConstantBuffer.PushGlobal(natCmd, data.parameters.raytracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                        // Define the shader pass to use for the reflection pass
                        natCmd.SetRayTracingShaderPass(data.parameters.gBufferRaytracingRT, "GBufferDXR");

                        if (data.parameters.rayBinning)
                        {
                            int numTilesRayBinX = (texWidth + (binningTileSize - 1)) / binningTileSize;
                            natCmd.SetGlobalBuffer(HDShaderIDs._RayBinResult, data.parameters.rayBinResult);
                            natCmd.SetGlobalBuffer(HDShaderIDs._RayBinSizeResult, data.parameters.rayBinSizeResult);
                            natCmd.SetRayTracingIntParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._RayBinTileCountX, numTilesRayBinX);
                        }

                        // Set the acceleration structure for the pass
                        natCmd.SetRayTracingAccelerationStructure(data.parameters.gBufferRaytracingRT, HDShaderIDs._RaytracingAccelerationStructureName, data.parameters.accelerationStructure);

                        // Set ray count texture
                        natCmd.SetRayTracingIntParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._RayCountType, data.parameters.rayCountType);
                        natCmd.SetRayTracingTextureParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._RayCountTexture, data.rayCountTexture);

                        // Bind all input parameter
                        natCmd.SetRayTracingIntParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._RayTracingLayerMask, data.parameters.layerMask);
                        natCmd.SetRayTracingTextureParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                        natCmd.SetRayTracingTextureParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        natCmd.SetRayTracingTextureParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._RaytracingDirectionBuffer, data.directionBuffer);
                        natCmd.SetRayTracingIntParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._RaytracingHalfResolution, data.parameters.halfResolution ? 1 : 0);

                        // Bind the output textures
                        natCmd.SetRayTracingTextureParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._GBufferTextureRW[0], data.gbuffer0);
                        natCmd.SetRayTracingTextureParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._GBufferTextureRW[1], data.gbuffer1);
                        natCmd.SetRayTracingTextureParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._GBufferTextureRW[2], data.gbuffer2);
                        natCmd.SetRayTracingTextureParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._GBufferTextureRW[3], data.gbuffer3);
                        natCmd.SetRayTracingTextureParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._RaytracingDistanceBuffer, data.tmpDistanceBuffer);

                        // Compute the actual resolution that is needed base on the resolution
                        uint widthResolution = (uint)data.parameters.width;
                        uint heightResolution = (uint)data.parameters.height;

                        // Include the sky if required
                        natCmd.SetRayTracingTextureParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._SkyTexture, data.skyTexture);

                        // Only compute diffuse lighting if required
                        CoreUtils.SetKeyword(natCmd, "MINIMAL_GBUFFER", data.parameters.diffuseLightingOnly);

                        if (data.enableDecals)
                        {
                            DecalSystem.instance.SetAtlas(natCmd);
                            data.parameters.lightCluster.BindLightClusterData(natCmd);
                        }

                        if (data.parameters.rayBinning)
                        {
                            // Evaluate the dispatch data.parameters
                            int numTilesRayBinX = (texWidth + (binningTileSize - 1)) / binningTileSize;
                            int numTilesRayBinY = (texHeight + (binningTileSize - 1)) / binningTileSize;
                            int bufferSizeX = numTilesRayBinX * binningTileSize;
                            int bufferSizeY = numTilesRayBinY * binningTileSize;
                            natCmd.SetRayTracingIntParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._BufferSizeX, bufferSizeX);

                            // Inject the data for the additional views
                            natCmd.SetRayTracingIntParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._RayBinViewOffset, bufferSizeX * bufferSizeY);
                            natCmd.SetRayTracingIntParam(data.parameters.gBufferRaytracingRT, HDShaderIDs._RayBinTileViewOffset, numTilesRayBinX * numTilesRayBinY);

                            // A really nice tip is to dispatch the rays as a 1D array instead of 2D, the performance difference has been measured.
                            uint dispatchSize = (uint)(bufferSizeX * bufferSizeY);

                            natCmd.DispatchRays(data.parameters.gBufferRaytracingRT, m_RayGenGBufferBinned, dispatchSize, 1, (uint)data.parameters.viewCount, null);
                        }
                        else
                        {
                            natCmd.DispatchRays(data.parameters.gBufferRaytracingRT, m_RayGenGBuffer, widthResolution, heightResolution, (uint)data.parameters.viewCount, null);
                        }

                        CoreUtils.SetKeyword(natCmd, "MINIMAL_GBUFFER", false);

                        // Now let's do the deferred shading pass on the samples
                        int currentKernel = data.parameters.deferredRaytracingCS.FindKernel(data.parameters.halfResolution ? "RaytracingDeferredHalf" : "RaytracingDeferred");

                        // Bind the lightLoop data
                        data.parameters.lightCluster.BindLightClusterData(natCmd);

                        // Bind the input textures
                        natCmd.SetComputeTextureParam(data.parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                        natCmd.SetComputeTextureParam(data.parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        natCmd.SetComputeTextureParam(data.parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._RaytracingDirectionBuffer, data.directionBuffer);
                        natCmd.SetComputeTextureParam(data.parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._RaytracingDistanceBuffer, data.tmpDistanceBuffer);
                        natCmd.SetComputeTextureParam(data.parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._GBufferTexture[0], data.gbuffer0);
                        natCmd.SetComputeTextureParam(data.parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._GBufferTexture[1], data.gbuffer1);
                        natCmd.SetComputeTextureParam(data.parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._GBufferTexture[2], data.gbuffer2);
                        natCmd.SetComputeTextureParam(data.parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._GBufferTexture[3], data.gbuffer3);
                        natCmd.SetComputeTextureParam(data.parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._RenderingLayersTexture, data.renderingLayersTexture);

                        // Bind the output texture
                        natCmd.SetComputeTextureParam(data.parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._RaytracingLitBufferRW, data.litBuffer);
                        natCmd.SetComputeTextureParam(data.parameters.deferredRaytracingCS, currentKernel, HDShaderIDs._RaytracingDistanceBufferRW, data.distanceBuffer);

                        // Evaluate the dispatch parameters
                        int areaTileSize = 8;
                        int numTilesXHR = (texWidth + (areaTileSize - 1)) / areaTileSize;
                        int numTilesYHR = (texHeight + (areaTileSize - 1)) / areaTileSize;

                        // Compute the texture
                        natCmd.DispatchCompute(data.parameters.deferredRaytracingCS, currentKernel, numTilesXHR, numTilesYHR, data.parameters.viewCount);
                    });

                // Output the two buffers we need
                output.lightingBuffer = passData.litBuffer;
                output.distanceBuffer = passData.distanceBuffer;
                return output;
            }
        }
    }
}
