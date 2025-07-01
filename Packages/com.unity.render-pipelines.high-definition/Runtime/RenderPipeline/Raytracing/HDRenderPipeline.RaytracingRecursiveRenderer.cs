using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // String values
        const string m_RayGenShaderName = "RayGenRenderer";

        // Pass name for the flag pass
        ShaderTagId raytracingPassID = new ShaderTagId("Forward");
        RenderStateBlock m_RaytracingFlagStateBlock;

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
                format = GraphicsFormat.R8_SNorm,
                dimension = TextureXR.dimension,
                enableRandomWrite = true,
                useMipMap = true,
                clearBuffer = true,
                clearColor = Color.black,
                name = "FlagMaskTexture"
            });
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
        class RecursiveRenderingPassData
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Effect parameters
            public float rayLength;
            public int maxDepth;
            public float minSmoothness;
            public int rayMissFallbackHiearchy;
            public int lastBounceFallbackHiearchy;
            public float ambientProbeDimmer;

            // Other data
            public RayTracingAccelerationStructure accelerationStructure;
            public HDRaytracingLightCluster lightCluster;
            public RayTracingShader recursiveRenderingRT;
            public Texture skyTexture;
            public ShaderVariablesRaytracing shaderVariablesRayTracingCB;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;

            public TextureHandle depthStencilBuffer;
            public TextureHandle flagMask;
            public TextureHandle debugBuffer;
            public TextureHandle rayCountTexture;
            public TextureHandle outputBuffer;

            public bool enableDecals;
        }

        TextureHandle RaytracingRecursiveRender(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthPyramid, TextureHandle flagMask, TextureHandle rayCountTexture)
        {
            // If ray tracing is disabled in the frame settings or the effect is not enabled
            RecursiveRendering recursiveSettings = hdCamera.volumeStack.GetComponent<RecursiveRendering>();

            // Make sure all the requirements are there to render the effect
            bool validEffect = hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing)
                && recursiveSettings.enable.value
                && GetRayTracingState() && GetRayTracingClusterState();
            if (!validEffect)
                return colorBuffer;

            RecursiveRenderingPassData passData;

            using (var builder = renderGraph.AddUnsafePass<RecursiveRenderingPassData>("Recursive Rendering Evaluation", out passData, ProfilingSampler.Get(HDProfileId.RayTracingRecursiveRendering)))
            {
                // Camera parameters
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Effect parameters
                passData.rayLength = recursiveSettings.rayLength.value;
                passData.maxDepth = recursiveSettings.maxDepth.value;
                passData.minSmoothness = recursiveSettings.minSmoothness.value;
                passData.rayMissFallbackHiearchy = (int)recursiveSettings.rayMiss.value;
                passData.lastBounceFallbackHiearchy = (int)recursiveSettings.lastBounce.value;
                passData.ambientProbeDimmer = recursiveSettings.ambientProbeDimmer.value;

                // Other data
                passData.accelerationStructure = RequestAccelerationStructure(hdCamera);
                passData.lightCluster = RequestLightCluster();
                passData.recursiveRenderingRT = rayTracingResources.forwardRayTracing;
                passData.skyTexture = m_SkyManager.GetSkyReflection(hdCamera);
                passData.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;
                passData.ditheredTextureSet = GetBlueNoiseManager().DitheredTextureSet8SPP();

                passData.depthStencilBuffer = depthPyramid;
                builder.UseTexture(passData.depthStencilBuffer, AccessFlags.Read);
                passData.flagMask = flagMask;
                builder.UseTexture(passData.flagMask, AccessFlags.Read);
                passData.rayCountTexture = rayCountTexture;
                builder.UseTexture(passData.rayCountTexture, AccessFlags.ReadWrite);
                passData.outputBuffer = colorBuffer;
                builder.UseTexture(passData.outputBuffer, AccessFlags.ReadWrite);
                // Right now the debug buffer is written to independently of what is happening. This must be changed
                // TODO RENDERGRAPH
                passData.debugBuffer = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Recursive Rendering Debug Texture" });
                builder.UseTexture(passData.debugBuffer, AccessFlags.Write);

                passData.enableDecals = hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals);

                builder.SetRenderFunc(
                    (RecursiveRenderingPassData data, UnsafeGraphContext ctx) =>
                    {
                        var natCmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                        // Define the shader pass to use for the reflection pass
                        natCmd.SetRayTracingShaderPass(data.recursiveRenderingRT, "ForwardDXR");

                        // Set the acceleration structure for the pass
                        natCmd.SetRayTracingAccelerationStructure(data.recursiveRenderingRT, HDShaderIDs._RaytracingAccelerationStructureName, data.accelerationStructure);

                        // Inject the ray-tracing sampling data
                        BlueNoise.BindDitheredTextureSet(natCmd, data.ditheredTextureSet);

                        // Update Global Constant Buffer.
                        data.shaderVariablesRayTracingCB._RaytracingRayMaxLength = data.rayLength;
#if NO_RAY_RECURSION
                        data.shaderVariablesRayTracingCB._RaytracingMaxRecursion = 1;
#else
                        data.shaderVariablesRayTracingCB._RaytracingMaxRecursion = data.maxDepth;
#endif
                        data.shaderVariablesRayTracingCB._RaytracingReflectionMinSmoothness = data.minSmoothness;
                        data.shaderVariablesRayTracingCB._RayTracingRayMissFallbackHierarchy = data.rayMissFallbackHiearchy;
                        data.shaderVariablesRayTracingCB._RayTracingLastBounceFallbackHierarchy = data.lastBounceFallbackHiearchy;
                        data.shaderVariablesRayTracingCB._RayTracingAmbientProbeDimmer = data.ambientProbeDimmer;
                        ConstantBuffer.PushGlobal(natCmd, data.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                        // Fecth the temporary buffers we shall be using
                        natCmd.SetRayTracingTextureParam(data.recursiveRenderingRT, HDShaderIDs._RaytracingFlagMask, data.flagMask);
                        natCmd.SetRayTracingTextureParam(data.recursiveRenderingRT, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                        natCmd.SetRayTracingTextureParam(data.recursiveRenderingRT, HDShaderIDs._CameraColorTextureRW, data.outputBuffer);

                        // Set ray count texture
                        natCmd.SetRayTracingTextureParam(data.recursiveRenderingRT, HDShaderIDs._RayCountTexture, data.rayCountTexture);

                        // LightLoop data
                        data.lightCluster.BindLightClusterData(natCmd);

                        // Set the data for the ray miss
                        natCmd.SetRayTracingTextureParam(data.recursiveRenderingRT, HDShaderIDs._SkyTexture, data.skyTexture);

                        // If this is the right debug mode and we have at least one light, write the first shadow to the de-noised texture
                        natCmd.SetRayTracingTextureParam(data.recursiveRenderingRT, HDShaderIDs._RaytracingPrimaryDebug, data.debugBuffer);

                        if (data.enableDecals)
                        {
                            DecalSystem.instance.SetAtlas(natCmd);
                        }

                        // Run the computation
                        natCmd.DispatchRays(data.recursiveRenderingRT, m_RayGenShaderName, (uint)data.texWidth, (uint)data.texHeight, (uint)data.viewCount, null);
                    });
            }

            PushFullScreenDebugTexture(m_RenderGraph, passData.debugBuffer, FullScreenDebugMode.RecursiveRayTracing);

            return passData.outputBuffer;
        }
    }
}
