using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    class HDDiffuseShadowDenoiser
    {
        // The resources quired by this component
        ComputeShader m_ShadowDenoiser;

        // Kernels that we are using
        int m_BilateralFilterHSingleDirectionalKernel;
        int m_BilateralFilterVSingleDirectionalKernel;

        int m_BilateralFilterHColorDirectionalKernel;
        int m_BilateralFilterVColorDirectionalKernel;

        int m_BilateralFilterHSinglePointKernel;
        int m_BilateralFilterVSinglePointKernel;

        int m_BilateralFilterHSingleSpotKernel;
        int m_BilateralFilterVSingleSpotKernel;

        public HDDiffuseShadowDenoiser()
        {
        }

        public void Init(HDRenderPipelineRayTracingResources rpRTResources)
        {
            m_ShadowDenoiser = rpRTResources.diffuseShadowDenoiserCS;

            m_BilateralFilterHSingleDirectionalKernel = m_ShadowDenoiser.FindKernel("BilateralFilterHSingleDirectional");
            m_BilateralFilterVSingleDirectionalKernel = m_ShadowDenoiser.FindKernel("BilateralFilterVSingleDirectional");

            m_BilateralFilterHColorDirectionalKernel = m_ShadowDenoiser.FindKernel("BilateralFilterHColorDirectional");
            m_BilateralFilterVColorDirectionalKernel = m_ShadowDenoiser.FindKernel("BilateralFilterVColorDirectional");

            m_BilateralFilterHSinglePointKernel = m_ShadowDenoiser.FindKernel("BilateralFilterHSinglePoint");
            m_BilateralFilterVSinglePointKernel = m_ShadowDenoiser.FindKernel("BilateralFilterVSinglePoint");

            m_BilateralFilterHSingleSpotKernel = m_ShadowDenoiser.FindKernel("BilateralFilterHSingleSpot");
            m_BilateralFilterVSingleSpotKernel = m_ShadowDenoiser.FindKernel("BilateralFilterVSingleSpot");
        }

        public void Release()
        {
        }

        class DiffuseShadowDenoiserDirectionalPassData
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Evaluation parameters
            public float lightAngle;
            public float cameraFov;
            public int kernelSize;

            // Kernels
            public int bilateralHKernel;
            public int bilateralVKernel;

            // Other parameters
            public ComputeShader diffuseShadowDenoiserCS;

            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle distanceBuffer;
            public TextureHandle noisyBuffer;
            public TextureHandle intermediateBuffer;
            public TextureHandle outputBuffer;
        }

        public TextureHandle DenoiseBufferDirectional(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle depthBuffer, TextureHandle normalBuffer,
            TextureHandle noisyBuffer, TextureHandle distanceBuffer,
            int kernelSize, float angularDiameter, bool singleChannel = true)
        {
            using (var builder = renderGraph.AddRenderPass<DiffuseShadowDenoiserDirectionalPassData>("TemporalDenoiser", out var passData, ProfilingSampler.Get(HDProfileId.DiffuseFilter)))
            {
                // Cannot run in async
                builder.EnableAsyncCompute(false);

                // Fetch all the resources
                // Set the camera parameters
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Evaluation parameters
                passData.cameraFov = hdCamera.camera.fieldOfView * Mathf.PI / 180.0f;
                // Convert the angular diameter of the directional light to radians (from degrees)
                passData.lightAngle = angularDiameter * Mathf.PI / 180.0f;
                passData.kernelSize = kernelSize;

                // Kernels
                passData.bilateralHKernel = singleChannel ? m_BilateralFilterHSingleDirectionalKernel : m_BilateralFilterHColorDirectionalKernel;
                passData.bilateralVKernel = singleChannel ? m_BilateralFilterVSingleDirectionalKernel : m_BilateralFilterVColorDirectionalKernel;

                // Other parameters
                passData.diffuseShadowDenoiserCS = m_ShadowDenoiser;

                // Input buffers
                passData.depthStencilBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.Read);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.distanceBuffer = builder.ReadTexture(distanceBuffer);
                passData.noisyBuffer = builder.ReadTexture(noisyBuffer);

                // Temporary buffers
                passData.intermediateBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Intermediate buffer" });

                // Output buffer
                passData.outputBuffer = builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Denoised Buffer" }));


                builder.SetRenderFunc(
                    (DiffuseShadowDenoiserDirectionalPassData data, RenderGraphContext ctx) =>
                    {
                        // Raise the distance based denoiser keyword
                        CoreUtils.SetKeyword(ctx.cmd, "DISTANCE_BASED_DENOISER", true);

                        // Evaluate the dispatch parameters
                        int denoiserTileSize = 8;
                        int numTilesX = (data.texWidth + (denoiserTileSize - 1)) / denoiserTileSize;
                        int numTilesY = (data.texHeight + (denoiserTileSize - 1)) / denoiserTileSize;

                        // Bind input uniforms for both dispatches
                        ctx.cmd.SetComputeFloatParam(data.diffuseShadowDenoiserCS, HDShaderIDs._RaytracingLightAngle, data.lightAngle);
                        ctx.cmd.SetComputeIntParam(data.diffuseShadowDenoiserCS, HDShaderIDs._DenoiserFilterRadius, data.kernelSize);
                        ctx.cmd.SetComputeFloatParam(data.diffuseShadowDenoiserCS, HDShaderIDs._CameraFOV, data.cameraFov);

                        // Bind Input Textures
                        ctx.cmd.SetComputeTextureParam(data.diffuseShadowDenoiserCS, data.bilateralHKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                        ctx.cmd.SetComputeTextureParam(data.diffuseShadowDenoiserCS, data.bilateralHKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.diffuseShadowDenoiserCS, data.bilateralHKernel, HDShaderIDs._DenoiseInputTexture, data.noisyBuffer);
                        ctx.cmd.SetComputeTextureParam(data.diffuseShadowDenoiserCS, data.bilateralHKernel, HDShaderIDs._DistanceTexture, data.distanceBuffer);

                        // Bind output textures
                        ctx.cmd.SetComputeTextureParam(data.diffuseShadowDenoiserCS, data.bilateralHKernel, HDShaderIDs._DenoiseOutputTextureRW, data.intermediateBuffer);

                        // Do the Horizontal pass
                        ctx.cmd.DispatchCompute(data.diffuseShadowDenoiserCS, data.bilateralHKernel, numTilesX, numTilesY, data.viewCount);

                        // Bind Input Textures
                        ctx.cmd.SetComputeTextureParam(data.diffuseShadowDenoiserCS, data.bilateralVKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                        ctx.cmd.SetComputeTextureParam(data.diffuseShadowDenoiserCS, data.bilateralVKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.diffuseShadowDenoiserCS, data.bilateralVKernel, HDShaderIDs._DenoiseInputTexture, data.intermediateBuffer);
                        ctx.cmd.SetComputeTextureParam(data.diffuseShadowDenoiserCS, data.bilateralVKernel, HDShaderIDs._DistanceTexture, data.distanceBuffer);

                        // Bind output textures
                        ctx.cmd.SetComputeTextureParam(data.diffuseShadowDenoiserCS, data.bilateralVKernel, HDShaderIDs._DenoiseOutputTextureRW, data.outputBuffer);

                        // Do the Vertical pass
                        ctx.cmd.DispatchCompute(data.diffuseShadowDenoiserCS, data.bilateralVKernel, numTilesX, numTilesY, data.viewCount);

                        CoreUtils.SetKeyword(ctx.cmd, "DISTANCE_BASED_DENOISER", false);
                    });
                return passData.outputBuffer;
            }
        }

        class DiffuseShadowDenoiserSpherePassData
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Evaluation parameters
            public float cameraFov;
            public PunctualShadowProperties properties;

            // Kernels
            public int bilateralHKernel;
            public int bilateralVKernel;

            // Other parameters
            public ComputeShader diffuseShadowDenoiserCS;

            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle distanceBuffer;
            public TextureHandle noisyBuffer;
            public TextureHandle intermediateBuffer;
            public TextureHandle outputBuffer;
        }

        public TextureHandle DenoiseBufferSphere(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle depthBuffer, TextureHandle normalBuffer,
            TextureHandle noisyBuffer, TextureHandle distanceBuffer,
            PunctualShadowProperties properties)
        {
            using (var builder = renderGraph.AddRenderPass<DiffuseShadowDenoiserSpherePassData>("DiffuseDenoiser", out var passData, ProfilingSampler.Get(HDProfileId.DiffuseFilter)))
            {
                // Cannot run in async
                builder.EnableAsyncCompute(false);

                // Set the camera parameters
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Evaluation parameters
                passData.cameraFov = hdCamera.camera.fieldOfView * Mathf.PI / 180.0f;
                passData.properties = properties;

                // Make sure the position is in the right space before injecting it
                if (ShaderConfig.s_CameraRelativeRendering != 0)
                    passData.properties.lightPosition -= hdCamera.camera.transform.position;

                // Kernels
                passData.bilateralHKernel = properties.isSpot ? m_BilateralFilterHSingleSpotKernel : m_BilateralFilterHSinglePointKernel;
                passData.bilateralVKernel = properties.isSpot ? m_BilateralFilterVSingleSpotKernel : m_BilateralFilterVSinglePointKernel;  

                // Other parameters
                passData.diffuseShadowDenoiserCS = m_ShadowDenoiser;

                // Input buffers
                passData.depthStencilBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.Read);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                if (properties.distanceBasedDenoiser)
                    passData.distanceBuffer = builder.ReadTexture(distanceBuffer);
                passData.noisyBuffer = builder.ReadTexture(noisyBuffer);

                // Temporary buffers
                passData.intermediateBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Intermediate buffer" });

                // Output buffer
                passData.outputBuffer = builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Denoised Buffer" }));


                builder.SetRenderFunc(
                    (DiffuseShadowDenoiserSpherePassData data, RenderGraphContext ctx) =>
                    {
                        // Evaluate the dispatch parameters
                        int shadowTileSize = 8;
                        int numTilesX = (data.texWidth + (shadowTileSize - 1)) / shadowTileSize;
                        int numTilesY = (data.texHeight + (shadowTileSize - 1)) / shadowTileSize;

                        // Raise the distance based denoiser keyword
                        CoreUtils.SetKeyword(ctx.cmd, "DISTANCE_BASED_DENOISER", data.properties.distanceBasedDenoiser);

                        // Bind input uniforms for both dispatches
                        ctx.cmd.SetComputeIntParam(data.diffuseShadowDenoiserCS, HDShaderIDs._RaytracingTargetLight, data.properties.lightIndex);
                        ctx.cmd.SetComputeFloatParam(data.diffuseShadowDenoiserCS, HDShaderIDs._RaytracingLightAngle, data.properties.lightConeAngle);
                        ctx.cmd.SetComputeFloatParam(data.diffuseShadowDenoiserCS, HDShaderIDs._RaytracingLightRadius, data.properties.lightRadius);
                        ctx.cmd.SetComputeIntParam(data.diffuseShadowDenoiserCS, HDShaderIDs._DenoiserFilterRadius, data.properties.kernelSize);
                        ctx.cmd.SetComputeVectorParam(data.diffuseShadowDenoiserCS, HDShaderIDs._SphereLightPosition, data.properties.lightPosition);
                        ctx.cmd.SetComputeFloatParam(data.diffuseShadowDenoiserCS, HDShaderIDs._CameraFOV, data.cameraFov);

                        // Bind Input Textures
                        ctx.cmd.SetComputeTextureParam(data.diffuseShadowDenoiserCS, data.bilateralHKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                        ctx.cmd.SetComputeTextureParam(data.diffuseShadowDenoiserCS, data.bilateralHKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.diffuseShadowDenoiserCS, data.bilateralHKernel, HDShaderIDs._DenoiseInputTexture, data.noisyBuffer);
                        if (data.properties.distanceBasedDenoiser)
                            ctx.cmd.SetComputeTextureParam(data.diffuseShadowDenoiserCS, data.bilateralHKernel, HDShaderIDs._DistanceTexture, data.distanceBuffer);

                        // Bind output textures
                        ctx.cmd.SetComputeTextureParam(data.diffuseShadowDenoiserCS, data.bilateralHKernel, HDShaderIDs._DenoiseOutputTextureRW, data.intermediateBuffer);

                        // Do the Horizontal pass
                        ctx.cmd.DispatchCompute(data.diffuseShadowDenoiserCS, data.bilateralHKernel, numTilesX, numTilesY, data.viewCount);

                        ctx.cmd.SetComputeTextureParam(data.diffuseShadowDenoiserCS, data.bilateralVKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                        ctx.cmd.SetComputeTextureParam(data.diffuseShadowDenoiserCS, data.bilateralVKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.diffuseShadowDenoiserCS, data.bilateralVKernel, HDShaderIDs._DenoiseInputTexture, data.intermediateBuffer);
                        if (data.properties.distanceBasedDenoiser)
                            ctx.cmd.SetComputeTextureParam(data.diffuseShadowDenoiserCS, data.bilateralVKernel, HDShaderIDs._DistanceTexture, data.distanceBuffer);

                        // Bind output textures
                        ctx.cmd.SetComputeTextureParam(data.diffuseShadowDenoiserCS, data.bilateralVKernel, HDShaderIDs._DenoiseOutputTextureRW, data.outputBuffer);

                        // Do the Vertical pass
                        ctx.cmd.DispatchCompute(data.diffuseShadowDenoiserCS, data.bilateralVKernel, numTilesX, numTilesY, data.viewCount);

                        CoreUtils.SetKeyword(ctx.cmd, "DISTANCE_BASED_DENOISER", false);
                    });
                return passData.outputBuffer;
            }
        }
    }
}
