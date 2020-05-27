using System;
using UnityEngine.Experimental.Rendering;

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

        void ReleaseRecursiveRenderer()
        {
        }

        void RaytracingRecursiveRender(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, CullingResults cull)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing))
                return ;

            RecursiveRendering recursiveSettings = hdCamera.volumeStack.GetComponent<RecursiveRendering>();
            if (!recursiveSettings.enable.value)
                return;

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

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RayTracingRecursiveRendering)))
            {
                RayTracingShader forwardShader = m_Asset.renderPipelineRayTracingResources.forwardRaytracing;
                LightCluster lightClusterSettings = hdCamera.volumeStack.GetComponent<LightCluster>();
                RayTracingSettings rtSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();

                // Grab the acceleration structure and the list of HD lights for the target camera
                RayTracingAccelerationStructure accelerationStructure = RequestAccelerationStructure();
                HDRaytracingLightCluster lightCluster = RequestLightCluster();

                // Define the shader pass to use for the reflection pass
                cmd.SetRayTracingShaderPass(forwardShader, "ForwardDXR");

                // Set the acceleration structure for the pass
                cmd.SetRayTracingAccelerationStructure(forwardShader, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

                // Inject the ray-tracing sampling data
                cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._OwenScrambledTexture, m_Asset.renderPipelineResources.textures.owenScrambledRGBATex);
                cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._ScramblingTexture, m_Asset.renderPipelineResources.textures.scramblingTex);

                // Update Global Constant Buffer.
                m_ShaderVariablesRayTracingCB._RaytracingRayMaxLength = recursiveSettings.rayLength.value;
                m_ShaderVariablesRayTracingCB._RaytracingMaxRecursion = recursiveSettings.maxDepth.value;
                ConstantBuffer.PushGlobal(cmd, m_ShaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                // Fecth the temporary buffers we shall be using
                RTHandle flagBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.R0);
                cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._RaytracingFlagMask, flagBuffer);
                cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._CameraColorTextureRW, m_CameraColorBuffer);

                // Set ray count texture
                RayCountManager rayCountManager = GetRayCountManager();
                cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._RayCountTexture, rayCountManager.GetRayCountTexture());

                // LightLoop data
                lightCluster.BindLightClusterData(cmd);

                // Note: Just in case, we rebind the directional light data (in case they were not)
                cmd.SetGlobalBuffer(HDShaderIDs._DirectionalLightDatas, m_LightLoopLightData.directionalLightData);

                // Set the data for the ray miss
                cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._SkyTexture, m_SkyManager.GetSkyReflection(hdCamera));

                // If this is the right debug mode and we have at least one light, write the first shadow to the de-noised texture
                RTHandle debugBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);
                cmd.SetRayTracingTextureParam(forwardShader, HDShaderIDs._RaytracingPrimaryDebug, debugBuffer);

                // Run the computation
                cmd.DispatchRays(forwardShader, m_RayGenShaderName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, (uint)hdCamera.viewCount);

                HDRenderPipeline hdrp = (RenderPipelineManager.currentPipeline as HDRenderPipeline);
                hdrp.PushFullScreenDebugTexture(hdCamera, cmd, debugBuffer, FullScreenDebugMode.RecursiveRayTracing);
            }
        }
    }
}
