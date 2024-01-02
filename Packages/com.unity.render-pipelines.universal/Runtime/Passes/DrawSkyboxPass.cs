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
        private PassData m_PassData;
        private RendererList m_SkyRendererList;

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
            m_PassData = new PassData();
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

            InitSkyboxRendererList(context, cameraData);
            InitPassData(ref m_PassData, cameraData.xr);
            ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), m_PassData.xr, m_PassData.skyRendererList);
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

        private class PassData
        {
            internal XRPass xr;
            internal RendererList skyRendererList;
        }

        private void InitPassData(ref PassData passData, XRPass xr)
        {
            passData.xr = xr;
            passData.skyRendererList = m_SkyRendererList;
        }

        private void InitSkyboxRendererList(ScriptableRenderContext context, UniversalCameraData cameraData)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                // Setup Legacy XR buffer states
                if (cameraData.xr.singlePassEnabled)
                {
                    m_SkyRendererList = context.CreateSkyboxRendererList(cameraData.camera,
                        cameraData.GetProjectionMatrix(0), cameraData.GetViewMatrix(0),
                        cameraData.GetProjectionMatrix(1), cameraData.GetViewMatrix(1));
                }
                else
                {
                    m_SkyRendererList = context.CreateSkyboxRendererList(cameraData.camera, cameraData.GetProjectionMatrix(0), cameraData.GetViewMatrix(0));
                }
            }
            else
#endif
            {
                m_SkyRendererList = context.CreateSkyboxRendererList(cameraData.camera);
            }
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
                InitSkyboxRendererList(context, cameraData);
                InitPassData(ref passData, cameraData.xr);
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
                builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    ExecutePass(context.cmd, data.xr, data.skyRendererList);
                });
            }
        }
    }
}
