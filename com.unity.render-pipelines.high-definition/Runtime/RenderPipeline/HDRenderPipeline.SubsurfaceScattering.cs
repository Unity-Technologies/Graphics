using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        TextureHandle CreateSSSBuffer(RenderGraph renderGraph, HDCamera hdCamera, bool msaa)
        {
#if UNITY_2020_2_OR_NEWER
            FastMemoryDesc fastMemDesc;
            fastMemDesc.inFastMemory = true;
            fastMemDesc.residencyFraction = 1.0f;
            fastMemDesc.flags = FastMemoryFlags.SpillTop;
#endif

            return renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            {
                colorFormat = GraphicsFormat.R8G8B8A8_SRGB,
                enableRandomWrite = !msaa,
                bindTextureMS = msaa,
                enableMSAA = msaa,
                clearBuffer = NeedClearGBuffer(hdCamera),
                clearColor = Color.clear,
                name = msaa ? "SSSBufferMSAA" : "SSSBuffer"
#if UNITY_2020_2_OR_NEWER
                , fastMemoryDesc = fastMemDesc
#endif
            });
        }

        class SubsurfaceScaterringPassData
        {
            public SubsurfaceScatteringParameters parameters;
            public TextureHandle colorBuffer;
            public TextureHandle diffuseBuffer;
            public TextureHandle depthStencilBuffer;
            public TextureHandle depthTexture;
            public TextureHandle cameraFilteringBuffer;
            public TextureHandle sssBuffer;
            public ComputeBufferHandle coarseStencilBuffer;
        }

        void RenderSubsurfaceScattering(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer,
            in LightingBuffers lightingBuffers, ref PrepassOutput prepassOutput)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.SubsurfaceScattering))
                return;

            BuildCoarseStencilAndResolveIfNeeded(renderGraph, hdCamera, resolveOnly: false, ref prepassOutput);

            TextureHandle depthStencilBuffer = prepassOutput.depthBuffer;
            TextureHandle depthTexture = prepassOutput.depthPyramidTexture;

            using (var builder = renderGraph.AddRenderPass<SubsurfaceScaterringPassData>("Subsurface Scattering", out var passData, ProfilingSampler.Get(HDProfileId.SubsurfaceScattering)))
            {
                passData.parameters = PrepareSubsurfaceScatteringParameters(hdCamera);
                passData.colorBuffer = builder.WriteTexture(colorBuffer);
                passData.diffuseBuffer = builder.ReadTexture(lightingBuffers.diffuseLightingBuffer);
                passData.depthStencilBuffer = builder.ReadTexture(depthStencilBuffer);
                passData.depthTexture = builder.ReadTexture(depthTexture);
                passData.sssBuffer = builder.ReadTexture(lightingBuffers.sssBuffer);
                passData.coarseStencilBuffer = builder.ReadComputeBuffer(prepassOutput.coarseStencilBuffer);
                if (passData.parameters.needTemporaryBuffer)
                {
                    passData.cameraFilteringBuffer = builder.CreateTransientTexture(
                                            new TextureDesc(Vector2.one, true, true)
                                            { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true, clearBuffer = true, clearColor = Color.clear, name = "SSSCameraFiltering" });
                }
                else
                {
                    // We need to set this as otherwise it will still be using an handle that is potentially coming from another rendergraph execution.
                    // For example if we have two cameras, if  NeedTemporarySubsurfaceBuffer() is false, but one camera has MSAA and one hasn't, only one camera
                    // will have passData.parameters.needTemporaryBuffer true and the other that doesn't, without explicit setting to null handle will try to use handle of the other camera.
                    passData.cameraFilteringBuffer = TextureHandle.nullHandle;
                }

                builder.SetRenderFunc(
                (SubsurfaceScaterringPassData data, RenderGraphContext context) =>
                {
                    var resources = new SubsurfaceScatteringResources();
                    resources.colorBuffer = data.colorBuffer;
                    resources.diffuseBuffer = data.diffuseBuffer;
                    resources.depthStencilBuffer = data.depthStencilBuffer;
                    resources.depthTexture = data.depthTexture;
                    resources.cameraFilteringBuffer = data.cameraFilteringBuffer;
                    resources.sssBuffer = data.sssBuffer;
                    resources.coarseStencilBuffer = data.coarseStencilBuffer;

                    RenderSubsurfaceScattering(data.parameters, resources, context.cmd);
                });
            }
        }
    }
}
