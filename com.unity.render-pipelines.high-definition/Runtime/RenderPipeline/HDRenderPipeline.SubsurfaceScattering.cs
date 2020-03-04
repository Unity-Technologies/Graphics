using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Albedo + SSS Profile and mask / Specular occlusion (when no SSS)
        // This will be used during GBuffer and/or forward passes.
        RenderGraphMutableResource CreateSSSBuffer(RenderGraph renderGraph, bool msaa)
        {
            return renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            {
                colorFormat = GraphicsFormat.R8G8B8A8_SRGB,
                enableRandomWrite = !msaa,
                bindTextureMS = msaa,
                enableMSAA = msaa,
                clearBuffer = NeedClearGBuffer(),
                clearColor = Color.clear,
                name = string.Format("SSSBuffer{0}", msaa ? "MSAA" : "" ) }
            );
        }

        class SubsurfaceScaterringPassData
        {
            public SubsurfaceScatteringParameters parameters;
            public RenderGraphResource colorBuffer;
            public RenderGraphResource diffuseBuffer;
            public RenderGraphResource depthStencilBuffer;
            public RenderGraphResource depthTexture;
            public RenderGraphMutableResource cameraFilteringBuffer;
            public RenderGraphResource sssBuffer;
        }

        void RenderSubsurfaceScattering(RenderGraph renderGraph, HDCamera hdCamera, RenderGraphMutableResource colorBuffer,
            in LightingBuffers lightingBuffers, RenderGraphResource depthStencilBuffer, RenderGraphResource depthTexture)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.SubsurfaceScattering))
                return;

            using (var builder = renderGraph.AddRenderPass<SubsurfaceScaterringPassData>("Subsurface Scattering", out var passData, ProfilingSampler.Get(HDProfileId.SubsurfaceScattering)))
            {
                passData.parameters = PrepareSubsurfaceScatteringParameters(hdCamera);
                passData.colorBuffer = builder.WriteTexture(colorBuffer);
                passData.diffuseBuffer = builder.ReadTexture(lightingBuffers.diffuseLightingBuffer);
                passData.depthStencilBuffer = builder.ReadTexture(depthStencilBuffer);
                passData.depthTexture = builder.ReadTexture(depthTexture);
                passData.sssBuffer = builder.ReadTexture(lightingBuffers.sssBuffer);
                if (passData.parameters.needTemporaryBuffer)
                {
                    passData.cameraFilteringBuffer = builder.WriteTexture(renderGraph.CreateTexture(
                                            new TextureDesc(Vector2.one, true, true)
                                            { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true, clearBuffer = true, clearColor = Color.clear, name = "SSSCameraFiltering" }));
                }

                builder.SetRenderFunc(
                (SubsurfaceScaterringPassData data, RenderGraphContext context) =>
                {
                    var resources = new SubsurfaceScatteringResources();
                    resources.colorBuffer = context.resources.GetTexture(data.colorBuffer);
                    resources.diffuseBuffer = context.resources.GetTexture(data.diffuseBuffer);
                    resources.depthStencilBuffer = context.resources.GetTexture(data.depthStencilBuffer);
                    resources.depthTexture = context.resources.GetTexture(data.depthTexture);
                    resources.cameraFilteringBuffer = context.resources.GetTexture(data.cameraFilteringBuffer);
                    resources.sssBuffer = context.resources.GetTexture(data.sssBuffer);

                    RenderSubsurfaceScattering(data.parameters, resources, context.cmd);
                });
            }
        }
    }
}
