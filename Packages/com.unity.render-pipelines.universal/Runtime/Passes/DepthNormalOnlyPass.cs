using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Render all objects that have a 'DepthNormals' and/or 'DepthNormalsOnly' pass into the given depth and normal buffers.
    /// </summary>
    public partial class DepthNormalOnlyPass : ScriptableRenderPass
    {
        internal List<ShaderTagId> shaderTagIds { get; set; }
        internal bool enableRenderingLayers { get; set; } = false;
        internal RenderingLayerUtils.MaskSize renderingLayersMaskSize { get; set; }
        private FilteringSettings m_FilteringSettings;

        // Statics
        private static readonly List<ShaderTagId> k_DepthNormals = new List<ShaderTagId> { new ShaderTagId("DepthNormals"), new ShaderTagId("DepthNormalsOnly") };
        private static readonly List<ShaderTagId> k_DepthNormalsOnly = new List<ShaderTagId> { new ShaderTagId("DepthNormalsOnly") };

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
        /// <param name="sampler">The ProfilingSampler to use in the profiler and frame debugger for this pass.</param>
        /// <seealso cref="RenderPassEvent"/>
        /// <seealso cref="RenderQueueRange"/>
        /// <seealso cref="LayerMask"/>
        public DepthNormalOnlyPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, ProfilingSampler sampler = null)
        {
            profilingSampler = sampler ?? URPProfilingSamplers.DrawDepthNormalPrepass;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            renderPassEvent = evt;
            shaderTagIds = k_DepthNormals;
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
            enableRenderingLayers = true;
        }

        private static void ExecutePass(RasterCommandBuffer cmd, PassData passData, RendererList rendererList)
        {
            // Enable Rendering Layers
            if (passData.enableRenderingLayers)
                cmd.SetKeyword(ShaderGlobalKeywords.WriteRenderingLayers, true);
            if (passData.outputSmoothness)
                cmd.SetKeyword(ShaderGlobalKeywords.WriteSmoothness, true);

            // Draw
            cmd.DrawRendererList(rendererList);

            // Clean up
            if (passData.enableRenderingLayers)
                cmd.SetKeyword(ShaderGlobalKeywords.WriteRenderingLayers, false);
            if (passData.outputSmoothness)
                cmd.SetKeyword(ShaderGlobalKeywords.WriteSmoothness, false);
        }

        /// <inheritdoc/>
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
            {
                throw new ArgumentNullException("cmd");
            }

            // This needs to be reset as the renderer might change this in runtime (UUM-36069)
            shaderTagIds = k_DepthNormals;
        }

        /// <summary>
        /// Shared pass data
        /// </summary>
        private class PassData
        {
            internal bool enableRenderingLayers;
            internal bool outputSmoothness;
            internal RenderingLayerUtils.MaskSize maskSize;
            internal RendererListHandle rendererList;
        }

        private RendererListParams InitRendererListParams(UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData)
        {
            var drawSettings = RenderingUtils.CreateDrawingSettings(this.shaderTagIds, renderingData, cameraData, lightData, GetSortingCriteria(cameraData));
            drawSettings.perObjectData = PerObjectData.None;
            return new RendererListParams(renderingData.cullResults, drawSettings, m_FilteringSettings);
        }

        internal void Render(RenderGraph renderGraph, ContextContainer frameData, in TextureHandle cameraNormalsTexture, in TextureHandle depthTexture, in TextureHandle renderingLayersTexture, uint batchLayerMask, bool setGlobalDepth, bool setGlobalNormalAndRenderingLayers, bool allowPartialPass)
        {
            if (allowPartialPass)
            {
                this.shaderTagIds = k_DepthNormalsOnly;
            }
            else
            {
                this.shaderTagIds = k_DepthNormals;
            }

            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                builder.SetRenderAttachment(cameraNormalsTexture, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(depthTexture, AccessFlags.ReadWrite);

                passData.enableRenderingLayers = enableRenderingLayers;
#if URP_SCREEN_SPACE_REFLECTION
                passData.outputSmoothness = renderingData.writesSmoothnessToDepthNormalsAlpha;
#else
                passData.outputSmoothness = false;
#endif

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
                {
                    builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering && cameraData.xrUniversal.canFoveateIntermediatePasses);
                    // Multiview render regions are incompatible with the inner (foveal) pass in Quad View
                    if (!cameraData.xr.isQuadViewInnerPass)
                    {
                        builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.MultiviewRenderRegionsCompatible);
                    }
                }

                if (setGlobalNormalAndRenderingLayers)
                {
                    builder.SetGlobalTextureAfterPass(cameraNormalsTexture, s_CameraNormalsTextureID);

                    if (passData.enableRenderingLayers)
                        builder.SetGlobalTextureAfterPass(renderingLayersTexture, s_CameraRenderingLayersTextureID);
                }

                if (setGlobalDepth)
                    builder.SetGlobalTextureAfterPass(depthTexture, s_CameraDepthTextureID);

                // Required here because of RenderingLayerUtils.SetupProperties, and for setting keywords in ExecutePass
                if (passData.enableRenderingLayers || passData.outputSmoothness)
                    builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    if (data.enableRenderingLayers)
                        RenderingLayerUtils.SetupProperties(context.cmd, data.maskSize);
                    ExecutePass(context.cmd, data, data.rendererList);
                });
            }
        }

        /// <summary>
        /// Gets the sorting criteria to use for a given camera. Override to customize behavior.
        /// </summary>
        /// <param name="cameraData">The UniversalCameraData of the camera to get sorting criteria for.</param>
        /// <returns>The sorting criteria to use.</returns>
        protected virtual SortingCriteria GetSortingCriteria(UniversalCameraData cameraData)
        {
            return cameraData.defaultOpaqueSortFlags;
        }
    }
}
