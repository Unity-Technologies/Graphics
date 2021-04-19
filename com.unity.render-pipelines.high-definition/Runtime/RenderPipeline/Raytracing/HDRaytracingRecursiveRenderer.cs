using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // String values
        const string m_RayGenShaderName = "RayGenRenderer";

        // Pass name for the flag pass
        ShaderTagId raytracingPassID = new ShaderTagId("Forward");
        RenderStateBlock m_RaytracingFlagStateBlock;

        // Texture that is used to flag which pixels should be evaluated with recursive rendering
        RTHandle m_FlagMaskTextureRT;

        void InitRecursiveRenderer()
        {
            m_RaytracingFlagStateBlock = new RenderStateBlock
            {
                depthState = new DepthState(false, CompareFunction.LessEqual),
                mask = RenderStateMask.Depth
            };
        }

        internal TextureHandle CreateFlagMaskTexture(RenderGraph renderGraph)
        {
            return renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            {
                colorFormat = GraphicsFormat.R8_SNorm,
                dimension = TextureXR.dimension,
                enableRandomWrite = true,
                useMipMap = true,
                name = "FlagMaskTexture"
            });
        }

        struct RecursiveRendererParameters
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Effect parameters
            public float rayLength;
            public int maxDepth;
            public float minSmoothness;

            // Other data
            public RayTracingAccelerationStructure accelerationStructure;
            public HDRaytracingLightCluster lightCluster;
            public RayTracingShader recursiveRenderingRT;
            public Texture skyTexture;
            public ShaderVariablesRaytracing shaderVariablesRayTracingCB;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
        }

        RecursiveRendererParameters PrepareRecursiveRendererParameters(HDCamera hdCamera, RecursiveRendering recursiveRendering)
        {
            RecursiveRendererParameters rrParams = new RecursiveRendererParameters();

            // Camera parameters
            rrParams.texWidth = hdCamera.actualWidth;
            rrParams.texHeight = hdCamera.actualHeight;
            rrParams.viewCount = hdCamera.viewCount;

            // Effect parameters
            rrParams.rayLength = recursiveRendering.rayLength.value;
            rrParams.maxDepth = recursiveRendering.maxDepth.value;
            rrParams.minSmoothness = recursiveRendering.minSmoothness.value;

            // Other data
            rrParams.accelerationStructure = RequestAccelerationStructure();
            rrParams.lightCluster = RequestLightCluster();
            rrParams.recursiveRenderingRT = m_Asset.renderPipelineRayTracingResources.forwardRaytracing;
            rrParams.skyTexture = m_SkyManager.GetSkyReflection(hdCamera);
            rrParams.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;
            BlueNoise blueNoise = GetBlueNoiseManager();
            rrParams.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();

            return rrParams;
        }

        struct RecursiveRendererResources
        {
            // Input buffers
            public RTHandle depthStencilBuffer;
            public RTHandle flagMask;

            // Debug buffer
            public RTHandle debugBuffer;
            public RTHandle rayCountTexture;

            // Output buffer
            public RTHandle outputBuffer;
        }

        RecursiveRendererResources PrepareRecursiveRendererResources(RTHandle debugBuffer)
        {
            RecursiveRendererResources rrResources = new RecursiveRendererResources();

            // Input buffers
            rrResources.depthStencilBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            rrResources.flagMask = m_FlagMaskTextureRT;

            // Debug buffer
            rrResources.debugBuffer = debugBuffer;
            RayCountManager rayCountManager = GetRayCountManager();
            rrResources.rayCountTexture = rayCountManager.GetRayCountTexture();

            // Output buffer
            rrResources.outputBuffer = m_CameraColorBuffer;

            return rrResources;
        }

        // Recursive rendering works as follow:
        // - Shader have a _RayTracing property
        // When this property is setup to true, a RayTracingPrepass pass on the material is enabled (otherwise it is disabled)
        // - Before prepass we render all object with a RayTracingPrepass pass enabled into the depth buffer for performance saving.
        // Note that we will exclude from the rendering of DepthPrepass, GBuffer and Forward pass the raytraced objects but not from
        // motion vector pass, so we can still benefit from motion vector. This is handled in VertMesh.hlsl (see below).
        // However currently when rendering motion vector this will tag the stencil for deferred lighting, and thus could produce overshading.
        // - After Transparent Depth pass we render all object with a RayTracingPrepass pass enabled into output a mask buffer (need to depth test but not to write depth)
        // Note: we render two times: one to save performance and the other to write the mask, otherwise if we write the mask in the first pass it
        // will not take into account the objects which could render on top of the raytracing one (If we want to do that we need to perform the pass after that
        // the depth buffer is ready, which is after the Gbuffer pass, so we can't save performance).
        // - During RaytracingRecursiveRender we perform a RayTracingRendering.raytrace call on all pixel tag in the mask
        // It is require to exclude mesh from regular pass to save performance (for opaque) and get correct result (for transparent)
        // For this we cull the mesh by setuping their position to NaN if _RayTracing is true and _EnableRecursiveRayTracing true.
        // We use this method to avoid to have to deal with RenderQueue and it allow to dynamically disabled Recursive rendering
        // and fallback to classic rasterize transparent this way. The code for the culling is in VertMesh()
        // If raytracing is disable _EnableRecursiveRayTracing is set to false and no culling happen.
        // Objects are still render in shadow and motion vector pass to keep their properties.

        // We render Recursive render object before transparent, so transparent object can be overlayed on top
        // like lens flare on top of headlight. We write the depth, so it correctly z-test object behind as recursive rendering
        // re-render everything (Mean we should also support fog and sky into it).

        void RaytracingRecursiveRender(HDCamera hdCamera, CommandBuffer cmd)
        {
            // If ray tracing is disabled in the frame settings or the effect is not enabled
            RecursiveRendering recursiveSettings = hdCamera.volumeStack.GetComponent<RecursiveRendering>();
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) || !recursiveSettings.enable.value)
                return;


            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RayTracingRecursiveRendering)))
            {
                RTHandle debugBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);

                RecursiveRendererParameters rrParams = PrepareRecursiveRendererParameters(hdCamera, recursiveSettings);
                RecursiveRendererResources rrResources = PrepareRecursiveRendererResources(debugBuffer);
                ExecuteRecursiveRendering(cmd, rrParams, rrResources);
                PushFullScreenDebugTexture(hdCamera, cmd, debugBuffer, FullScreenDebugMode.RecursiveRayTracing);
            }
        }

        class RecursiveRenderingPassData
        {
            public RecursiveRendererParameters parameters;
            public TextureHandle depthStencilBuffer;
            public TextureHandle flagMask;
            public TextureHandle debugBuffer;
            public TextureHandle rayCountTexture;
            public TextureHandle outputBuffer;
        }

        TextureHandle RaytracingRecursiveRender(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthPyramid, TextureHandle flagMask, TextureHandle rayCountTexture)
        {
            // If ray tracing is disabled in the frame settings or the effect is not enabled
            RecursiveRendering recursiveSettings = hdCamera.volumeStack.GetComponent<RecursiveRendering>();
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) || !recursiveSettings.enable.value)
                return colorBuffer;

            // Build the parameter structure
            RecursiveRendererParameters rrParams = PrepareRecursiveRendererParameters(hdCamera, recursiveSettings);

            using (var builder = renderGraph.AddRenderPass<RecursiveRenderingPassData>("Recursive Rendering Evaluation", out var passData, ProfilingSampler.Get(HDProfileId.RayTracingRecursiveRendering)))
            {
                builder.EnableAsyncCompute(false);

                passData.parameters = rrParams;
                passData.depthStencilBuffer = builder.ReadTexture(depthPyramid);
                passData.flagMask = builder.ReadTexture(flagMask);
                passData.rayCountTexture = builder.ReadWriteTexture(rayCountTexture);
                passData.outputBuffer = builder.ReadWriteTexture(colorBuffer);
                // Right now the debug buffer is written to independently of what is happening. This must be changed
                // TODO RENDERGRAPH
                passData.debugBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Recursive Rendering Debug Texture" }));

                builder.SetRenderFunc(
                (RecursiveRenderingPassData data, RenderGraphContext ctx) =>
                {
                    RecursiveRendererResources rrResources = new RecursiveRendererResources();
                    rrResources.depthStencilBuffer = data.depthStencilBuffer;
                    rrResources.flagMask = data.flagMask;
                    rrResources.debugBuffer = data.debugBuffer;
                    rrResources.rayCountTexture = data.rayCountTexture;
                    rrResources.outputBuffer = data.outputBuffer;
                    ExecuteRecursiveRendering(ctx.cmd, data.parameters, rrResources);
                });

                PushFullScreenDebugTexture(m_RenderGraph, passData.debugBuffer, FullScreenDebugMode.RecursiveRayTracing);

                return passData.outputBuffer;
            }
        }

        static void ExecuteRecursiveRendering(CommandBuffer cmd, RecursiveRendererParameters rrParams, RecursiveRendererResources rrResources)
        {
            // Define the shader pass to use for the reflection pass
            cmd.SetRayTracingShaderPass(rrParams.recursiveRenderingRT, "ForwardDXR");

            // Set the acceleration structure for the pass
            cmd.SetRayTracingAccelerationStructure(rrParams.recursiveRenderingRT, HDShaderIDs._RaytracingAccelerationStructureName, rrParams.accelerationStructure);

            // Inject the ray-tracing sampling data
            BlueNoise.BindDitheredTextureSet(cmd, rrParams.ditheredTextureSet);

            // Update Global Constant Buffer.
            rrParams.shaderVariablesRayTracingCB._RaytracingRayMaxLength = rrParams.rayLength;
            rrParams.shaderVariablesRayTracingCB._RaytracingMaxRecursion = rrParams.maxDepth;
            rrParams.shaderVariablesRayTracingCB._RaytracingReflectionMinSmoothness = rrParams.minSmoothness;
            ConstantBuffer.PushGlobal(cmd, rrParams.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

            // Fecth the temporary buffers we shall be using
            cmd.SetRayTracingTextureParam(rrParams.recursiveRenderingRT, HDShaderIDs._RaytracingFlagMask, rrResources.flagMask);
            cmd.SetRayTracingTextureParam(rrParams.recursiveRenderingRT, HDShaderIDs._DepthTexture, rrResources.depthStencilBuffer);
            cmd.SetRayTracingTextureParam(rrParams.recursiveRenderingRT, HDShaderIDs._CameraColorTextureRW, rrResources.outputBuffer);

            // Set ray count texture
            cmd.SetRayTracingTextureParam(rrParams.recursiveRenderingRT, HDShaderIDs._RayCountTexture, rrResources.rayCountTexture);

            // LightLoop data
            rrParams.lightCluster.BindLightClusterData(cmd);

            // Set the data for the ray miss
            cmd.SetRayTracingTextureParam(rrParams.recursiveRenderingRT, HDShaderIDs._SkyTexture, rrParams.skyTexture);

            // If this is the right debug mode and we have at least one light, write the first shadow to the de-noised texture
            cmd.SetRayTracingTextureParam(rrParams.recursiveRenderingRT, HDShaderIDs._RaytracingPrimaryDebug, rrResources.debugBuffer);

            // Run the computation
            cmd.DispatchRays(rrParams.recursiveRenderingRT, m_RayGenShaderName, (uint)rrParams.texWidth, (uint)rrParams.texHeight, (uint)rrParams.viewCount);
        }
    }
}
