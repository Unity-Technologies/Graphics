using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using Unity.Collections;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        struct VBufferOutput
        {
            public TextureHandle vbuffer;
        }

        internal bool IsVisibilityPassEnabled()
        {
            return currentAsset != null && currentAsset.VisibilityMaterial != null;
        }

        class VBufferPassData
        {
            public FrameSettings frameSettings;
            public RendererListHandle rendererList;
        }

        void RenderVBuffer(RenderGraph renderGraph, HDCamera hdCamera, CullingResults cull, ref PrepassOutput output)
        {
            output.vbuffer = new VBufferOutput();

            var globalGeoPool = RenderBRG.FindGlobalGeometryPool();
            if (!IsVisibilityPassEnabled() || globalGeoPool == null)
            {
                output.vbuffer.vbuffer = renderGraph.defaultResources.blackUIntTextureXR;
                return;
            }

            var visibilityMaterial = currentAsset.VisibilityMaterial;
            var visFormat = GraphicsFormat.R32_UInt;
            using (var builder = renderGraph.AddRenderPass<VBufferPassData>("VBuffer", out var passData, ProfilingSampler.Get(HDProfileId.VBuffer)))
            {
                builder.AllowRendererListCulling(false);

                FrameSettings frameSettings = hdCamera.frameSettings;

                passData.frameSettings = frameSettings;

                output.depthBuffer = builder.UseDepthBuffer(output.depthBuffer, DepthAccess.ReadWrite);
                output.vbuffer.vbuffer = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true)
                    {
                        colorFormat = visFormat,
                        clearBuffer = true,//TODO: for now clear
                        clearColor = Color.clear,
                        name = "VisibilityBuffer"
                    }), 0);

                passData.rendererList = builder.UseRendererList(
                   renderGraph.CreateRendererList(CreateOpaqueRendererListDesc(
                        cull, hdCamera.camera,
                        HDShaderPassNames.s_VBufferName, m_CurrentRendererConfigurationBakedLighting, null, null, visibilityMaterial, excludeObjectMotionVectors: false)));

                globalGeoPool.BindResources(visibilityMaterial);

                builder.SetRenderFunc(
                    (VBufferPassData data, RenderGraphContext context) =>
                    {
                        DrawOpaqueRendererList(context, data.frameSettings, data.rendererList);
                    });
            }
        }
    }
}
