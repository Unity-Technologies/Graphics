using System;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Draw the skybox into the given color buffer using the given depth buffer for depth testing.
    ///
    /// This pass renders the standard Unity skybox.
    /// </summary>
    public class DrawSkyboxPass : ScriptableRenderPass
    {
        static readonly int s_CameraDepthTextureID = Shader.PropertyToID("_CameraDepthTexture");

        /// <summary>
        /// Creates a new <c>DrawSkyboxPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <seealso cref="RenderPassEvent"/>
        public DrawSkyboxPass(RenderPassEvent evt)
        {
            base.profilingSampler = new ProfilingSampler(nameof(DrawSkyboxPass));

            renderPassEvent = evt;
        }

        /// <inheritdoc/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            UniversalCameraData cameraData = renderingData.frameData.Get<UniversalCameraData>();
            var activeDebugHandler = GetActiveDebugHandler(cameraData);
            if (activeDebugHandler != null)
            {
                // TODO: The skybox needs to work the same as the other shaders, but until it does we'll not render it
                // when certain debug modes are active (e.g. wireframe/overdraw modes)
                if (activeDebugHandler.IsScreenClearNeeded)
                {
                    return;
                }
            }

            var skyRendererList = CreateSkyboxRendererList(context, cameraData);
            ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), cameraData.xr, skyRendererList);
        }

        // For non-RG path
        private RendererList CreateSkyboxRendererList(ScriptableRenderContext context, UniversalCameraData cameraData)
        {
            var skyRendererList = new RendererList();

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                // Setup Legacy XR buffer states
                if (cameraData.xr.singlePassEnabled)
                {
                    skyRendererList = context.CreateSkyboxRendererList(cameraData.camera,
                        cameraData.GetProjectionMatrix(0), cameraData.GetViewMatrix(0),
                        cameraData.GetProjectionMatrix(1), cameraData.GetViewMatrix(1));
                }
                else
                {
                    skyRendererList = context.CreateSkyboxRendererList(cameraData.camera, cameraData.GetProjectionMatrix(0), cameraData.GetViewMatrix(0));
                }
            }
            else
#endif
            {
                skyRendererList = context.CreateSkyboxRendererList(cameraData.camera);
            }

            return skyRendererList;
        }

        // For RG path
        private RendererListHandle CreateSkyBoxRendererList(RenderGraph renderGraph, UniversalCameraData cameraData)
        {
            var skyRendererListHandle = new RendererListHandle();

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                // Setup Legacy XR buffer states
                if (cameraData.xr.singlePassEnabled)
                {
                    skyRendererListHandle = renderGraph.CreateSkyboxRendererList(cameraData.camera,
                        cameraData.GetProjectionMatrix(0), cameraData.GetViewMatrix(0),
                        cameraData.GetProjectionMatrix(1), cameraData.GetViewMatrix(1));
                }
                else
                {
                    skyRendererListHandle = renderGraph.CreateSkyboxRendererList(cameraData.camera, cameraData.GetProjectionMatrix(0), cameraData.GetViewMatrix(0));
                }
            }
            else
#endif
            {
                skyRendererListHandle = renderGraph.CreateSkyboxRendererList(cameraData.camera);
            }

            return skyRendererListHandle;
        }

        private static void ExecutePass(RasterCommandBuffer cmd, XRPass xr, RendererList rendererList)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (xr.enabled && xr.singlePassEnabled)
                cmd.SetSinglePassStereo(SystemInfo.supportsMultiview ? SinglePassStereoMode.Multiview : SinglePassStereoMode.Instancing);
#endif
            cmd.DrawRendererList(rendererList);

#if ENABLE_VR && ENABLE_XR_MODULE
            if (xr.enabled && xr.singlePassEnabled)
                cmd.SetSinglePassStereo(SinglePassStereoMode.None);
#endif
        }

        // All the rest below is Render Graph specific
        private class PassData
        {
            internal XRPass xr;
            internal RendererListHandle skyRendererListHandle;
        }

        private void InitPassData(ref PassData passData, in XRPass xr, in RendererListHandle handle)
        {
            passData.xr = xr;
            passData.skyRendererListHandle = handle;
        }

        internal void Render(RenderGraph renderGraph, ContextContainer frameData, ScriptableRenderContext context, TextureHandle colorTarget, TextureHandle depthTarget, bool hasDepthCopy = false)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            var activeDebugHandler = GetActiveDebugHandler(cameraData);
            if (activeDebugHandler != null)
            {
                // TODO: The skybox needs to work the same as the other shaders, but until it does we'll not render it
                // when certain debug modes are active (e.g. wireframe/overdraw modes)
                if (activeDebugHandler.IsScreenClearNeeded)
                {
                    return;
                }
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Draw Skybox Pass", out var passData,
                base.profilingSampler))
            {
                var skyRendererListHandle = CreateSkyBoxRendererList(renderGraph, cameraData);
                InitPassData(ref passData, cameraData.xr, skyRendererListHandle);
                builder.UseRendererList(skyRendererListHandle);
                builder.SetRenderAttachment(colorTarget, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(depthTarget, AccessFlags.Write);

                UniversalRenderer renderer = cameraData.renderer as UniversalRenderer;
                if (hasDepthCopy && resourceData.cameraDepthTexture.IsValid())
                {
                    if (renderer.renderingModeActual != RenderingMode.Deferred)
                        builder.UseGlobalTexture(s_CameraDepthTextureID);
                    else if (renderer.deferredLights.GbufferDepthIndex != -1)
                        builder.UseGlobalTexture(DeferredLights.k_GBufferShaderPropertyIDs[renderer.deferredLights.GbufferDepthIndex]);
                }

                builder.AllowPassCulling(false);
                if (cameraData.xr.enabled)
                {
                    bool passSupportsFoveation = cameraData.xrUniversal.canFoveateIntermediatePasses || resourceData.isActiveTargetBackBuffer;
                    builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering && passSupportsFoveation);
                }

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    ExecutePass(context.cmd, data.xr, data.skyRendererListHandle);
                });
            }
        }
    }
}
