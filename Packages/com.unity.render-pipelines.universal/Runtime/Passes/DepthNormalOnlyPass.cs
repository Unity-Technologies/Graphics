using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Render all objects that have a 'DepthNormals' and/or 'DepthNormalsOnly' pass into the given depth and normal buffers.
    /// </summary>
    public class DepthNormalOnlyPass : ScriptableRenderPass
    {
        internal List<ShaderTagId> shaderTagIds { get; set; }

        private RTHandle depthHandle { get; set; }
        private RTHandle normalHandle { get; set; }
        private RTHandle renderingLayersHandle { get; set; }
        internal bool enableRenderingLayers { get; set; } = false;
        internal RenderingLayerUtils.MaskSize renderingLayersMaskSize { get; set; }
        private FilteringSettings m_FilteringSettings;
        private PassData m_PassData;

        // Statics
        private static readonly List<ShaderTagId> k_DepthNormals = new List<ShaderTagId> { new ShaderTagId("DepthNormals"), new ShaderTagId("DepthNormalsOnly") };
        private static readonly RTHandle[] k_ColorAttachment1 = new RTHandle[1];
        private static readonly RTHandle[] k_ColorAttachment2 = new RTHandle[2];
        internal static readonly string k_CameraNormalsTextureName = "_CameraNormalsTexture";
        private static readonly int s_CameraDepthTextureID = Shader.PropertyToID("_CameraDepthTexture");
        private static readonly int s_CameraNormalsTextureID = Shader.PropertyToID(k_CameraNormalsTextureName);
        private static readonly int s_CameraRenderingLayersTextureID = Shader.PropertyToID("_CameraRenderingLayersTexture");

        /// <summary>
        /// Creates a new <c>DepthNormalOnlyPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <param name="renderQueueRange">The <c>RenderQueueRange</c> to use for creating filtering settings that control what objects get rendered.</param>
        /// <param name="layerMask">The layer mask to use for creating filtering settings that control what objects get rendered.</param>
        /// <seealso cref="RenderPassEvent"/>
        /// <seealso cref="RenderQueueRange"/>
        /// <seealso cref="LayerMask"/>
        public DepthNormalOnlyPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask)
        {
            profilingSampler = ProfilingSampler.Get(URPProfileId.DrawDepthNormalPrepass);
            m_PassData = new PassData();
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            renderPassEvent = evt;
            useNativeRenderPass = false;
            this.shaderTagIds = k_DepthNormals;
        }

        /// <summary>
        /// Finds the format to use for the normals texture.
        /// </summary>
        /// <returns>The GraphicsFormat to use with the Normals texture.</returns>
        public static GraphicsFormat GetGraphicsFormat()
        {
            if (SystemInfo.IsFormatSupported(GraphicsFormat.R8G8B8A8_SNorm, GraphicsFormatUsage.Render))
                return GraphicsFormat.R8G8B8A8_SNorm; // Preferred format
            else if (SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, GraphicsFormatUsage.Render))
                return GraphicsFormat.R16G16B16A16_SFloat; // fallback
            else
                return GraphicsFormat.R32G32B32A32_SFloat; // fallback
        }

        /// <summary>
        /// Configures the pass.
        /// </summary>
        /// <param name="depthHandle">The <c>RTHandle</c> used to render depth to.</param>
        /// <param name="normalHandle">The <c>RTHandle</c> used to render normals.</param>
        /// <seealso cref="RTHandle"/>
        public void Setup(RTHandle depthHandle, RTHandle normalHandle)
        {
            this.depthHandle = depthHandle;
            this.normalHandle = normalHandle;
            enableRenderingLayers = false;
        }

        /// <summary>
        /// Configure the pass
        /// </summary>
        /// <param name="depthHandle">The <c>RTHandle</c> used to render depth to.</param>
        /// <param name="normalHandle">The <c>RTHandle</c> used to render normals.</param>
        /// <param name="decalLayerHandle">The <c>RTHandle</c> used to render decals.</param>
        public void Setup(RTHandle depthHandle, RTHandle normalHandle, RTHandle decalLayerHandle)
        {
            Setup(depthHandle, normalHandle);
            renderingLayersHandle = decalLayerHandle;
            enableRenderingLayers = true;
        }


        /// <inheritdoc/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RTHandle[] colorHandles;
            if (enableRenderingLayers)
            {
                k_ColorAttachment2[0] = normalHandle;
                k_ColorAttachment2[1] = renderingLayersHandle;
                colorHandles = k_ColorAttachment2;
            }
            else
            {
                k_ColorAttachment1[0] = normalHandle;
                colorHandles = k_ColorAttachment1;
            }

            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            if (renderingData.cameraData.renderer.useDepthPriming && (renderingData.cameraData.renderType == CameraRenderType.Base || renderingData.cameraData.clearDepth))
                ConfigureTarget(colorHandles, renderingData.cameraData.renderer.cameraDepthTargetHandle);
            else
                ConfigureTarget(colorHandles, depthHandle);

            ConfigureClear(ClearFlag.All, Color.black);
            #pragma warning restore CS0618
        }

        private static void ExecutePass(RasterCommandBuffer cmd, PassData passData, RendererList rendererList)
        {            
            // Enable Rendering Layers
            if (passData.enableRenderingLayers)
                cmd.SetKeyword(ShaderGlobalKeywords.WriteRenderingLayers, true);

            // Draw
            cmd.DrawRendererList(rendererList);

            // Clean up
            if (passData.enableRenderingLayers)
                cmd.SetKeyword(ShaderGlobalKeywords.WriteRenderingLayers, false);            
        }

        /// <inheritdoc/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ContextContainer frameData = renderingData.frameData;
            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            m_PassData.enableRenderingLayers = enableRenderingLayers;
            var param = InitRendererListParams(universalRenderingData, cameraData,lightData);
            var rendererList = context.CreateRendererList(ref param);

            var cmd = CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer);

            using (new ProfilingScope(cmd, profilingSampler))
            {
                ExecutePass(cmd, m_PassData, rendererList);
            }
        }

        /// <inheritdoc/>
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
            {
                throw new ArgumentNullException("cmd");
            }
            normalHandle = null;
            depthHandle = null;
            renderingLayersHandle = null;

            // This needs to be reset as the renderer might change this in runtime (UUM-36069)
            shaderTagIds = k_DepthNormals;
        }

        /// <summary>
        /// Shared pass data
        /// </summary>
        private class PassData
        {
            internal TextureHandle cameraDepthTexture;
            internal TextureHandle cameraNormalsTexture;
            internal bool enableRenderingLayers;
            internal RenderingLayerUtils.MaskSize maskSize;
            internal RendererListHandle rendererList;
        }

        private RendererListParams InitRendererListParams(UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData)
        {
            var sortFlags = cameraData.defaultOpaqueSortFlags;
            var drawSettings = RenderingUtils.CreateDrawingSettings(this.shaderTagIds, renderingData, cameraData, lightData, sortFlags);
            drawSettings.perObjectData = PerObjectData.None;
            return new RendererListParams(renderingData.cullResults, drawSettings, m_FilteringSettings);
        }

        internal void Render(RenderGraph renderGraph, ContextContainer frameData, TextureHandle cameraNormalsTexture, TextureHandle cameraDepthTexture, TextureHandle renderingLayersTexture, uint batchLayerMask, bool setGlobalDepth, bool setGlobalTextures)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                passData.cameraNormalsTexture = cameraNormalsTexture;
                builder.SetRenderAttachment(cameraNormalsTexture, 0, AccessFlags.Write);
                passData.cameraDepthTexture = cameraDepthTexture;
                builder.SetRenderAttachmentDepth(cameraDepthTexture, AccessFlags.Write);

                passData.enableRenderingLayers = enableRenderingLayers;

                if (passData.enableRenderingLayers)
                {
                    builder.SetRenderAttachment(renderingLayersTexture, 1, AccessFlags.Write);
                    passData.maskSize = renderingLayersMaskSize;
                }

                var param = InitRendererListParams(renderingData, cameraData, lightData);
                param.filteringSettings.batchLayerMask = batchLayerMask;
                passData.rendererList = renderGraph.CreateRendererList(param);
                builder.UseRendererList(passData.rendererList);
                if (cameraData.xr.enabled)
                    builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering && cameraData.xrUniversal.canFoveateIntermediatePasses);

                if (setGlobalTextures)
                {
                    builder.SetGlobalTextureAfterPass(cameraNormalsTexture, s_CameraNormalsTextureID);

                    if (passData.enableRenderingLayers)
                        builder.SetGlobalTextureAfterPass(renderingLayersTexture, s_CameraRenderingLayersTextureID);
                }

                if (setGlobalDepth)
                    builder.SetGlobalTextureAfterPass(cameraDepthTexture, s_CameraDepthTextureID);

                //  TODO RENDERGRAPH: culling? force culling off for testing
                builder.AllowPassCulling(false);
                // Required here because of RenderingLayerUtils.SetupProperties
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    RenderingLayerUtils.SetupProperties(context.cmd, data.maskSize);
                    ExecutePass(context.cmd, data, data.rendererList);
                });
            }
        }
    }
}
