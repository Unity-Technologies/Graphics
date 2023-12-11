#if ENABLE_VIRTUALTEXTURES
using VirtualTexturing = UnityEngine.Rendering.VirtualTexturing;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace  UnityEngine.Rendering.HighDefinition
{
    class VTBufferManager
    {
        public static TextureHandle CreateVTFeedbackBuffer(RenderGraph renderGraph, MSAASamples msaaSamples)
        {
            bool msaa = msaaSamples != MSAASamples.None;
#if UNITY_2020_2_OR_NEWER
            FastMemoryDesc colorFastMemDesc;
            colorFastMemDesc.inFastMemory = true;
            colorFastMemDesc.residencyFraction = 1.0f;
            colorFastMemDesc.flags = FastMemoryFlags.SpillTop;
#endif

            return renderGraph.CreateTexture(
                new TextureDesc(Vector2.one, true, true)
                {
                    colorFormat = GetFeedbackBufferFormat(), enableRandomWrite = !msaa, bindTextureMS = msaa, msaaSamples = msaa ? msaaSamples : MSAASamples.None, clearBuffer = true, clearColor = Color.white, name = msaa ? "VTFeedbackMSAA" : "VTFeedback", fallBackToBlackTexture = true
#if UNITY_2020_2_OR_NEWER
                    ,
                    fastMemoryDesc = colorFastMemDesc
#endif
                });
        }

        public static GraphicsFormat GetFeedbackBufferFormat()
        {
            return GraphicsFormat.R16G16_UNorm;
        }

        const int kResolveScaleFactor = 16;

        VirtualTexturing.Resolver   m_Resolver = new VirtualTexturing.Resolver();
        VirtualTexturing.Resolver   m_ResolverMsaa = new VirtualTexturing.Resolver();
        Vector2                     m_ResolverScale = new Vector2(1.0f / (float)kResolveScaleFactor, 1.0f / (float)kResolveScaleFactor);
        RTHandle                    m_LowresResolver; // This texture needs to be persistent because we do async gpu readback on it.
        ComputeShader               m_DownSampleCS = null;
        int                         m_DownsampleKernel;
        int                         m_DownsampleKernelMSAA;

        public VTBufferManager()
        {
        }

        public void Cleanup()
        {
            m_Resolver.Dispose();
            m_ResolverMsaa.Dispose();

            RTHandles.Release(m_LowresResolver);
            m_LowresResolver = null;
        }

        public void BeginRender(HDCamera hdCamera)
        {
            // Lazy creation because creating this texture in the constructor can happen outside of proper RTHandleSystem initialization
            // This can happen for example when cinemachine gets the HDCamera and ends up constructing VTBufferManager before HDRP is even initialized.
            if (m_LowresResolver == null)
                m_LowresResolver = RTHandles.Alloc(m_ResolverScale, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite: true, autoGenerateMips: false, name: "VTFeedback lowres");

            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.VirtualTexturing))
            {
                int width = hdCamera.actualWidth;
                int height = hdCamera.actualHeight;
                bool msaa = hdCamera.msaaEnabled;
                GetResolveDimensions(ref width, ref height);

                if (msaa)
                {
                    CalculateResolverDimensions(ref width, ref height, m_ResolverMsaa);
                    m_ResolverMsaa.UpdateSize(width, height);
                }
                else
                {
                    CalculateResolverDimensions(ref width, ref height, m_Resolver);
                    m_Resolver.UpdateSize(width, height);
                }
            }
        }

        class ResolveVTData
        {
            public int width, height;
            public int lowresWidth, lowresHeight;
            public VirtualTexturing.Resolver resolver;
            public ComputeShader downsampleCS;
            public int downsampleKernel;

            public TextureHandle input;
            public TextureHandle lowres;
        }

        public void Resolve(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle input)
        {
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.VirtualTexturing))
            {
                if (m_DownSampleCS == null)
                {
                    m_DownSampleCS = GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineRuntimeShaders>().VTFeedbackDownsample;
                    m_DownsampleKernel = m_DownSampleCS.FindKernel("KMain");
                    m_DownsampleKernelMSAA = m_DownSampleCS.FindKernel("KMainMSAA");
                }

                using (var builder = renderGraph.AddRenderPass<ResolveVTData>("Resolve VT", out var passData, ProfilingSampler.Get(HDProfileId.VTFeedbackDownsample)))
                {
                    // The output is never read outside the pass but is still useful for the VT system so we can't cull this pass.
                    builder.AllowPassCulling(false);

                    bool msaa = hdCamera.msaaEnabled;
                    passData.width = hdCamera.actualWidth;
                    passData.height = hdCamera.actualHeight;
                    passData.lowresWidth = passData.width;
                    passData.lowresHeight = passData.height;
                    GetResolveDimensions(ref passData.lowresWidth, ref passData.lowresHeight);
                    passData.resolver = msaa ? m_ResolverMsaa : m_Resolver;
                    passData.downsampleCS = m_DownSampleCS;
                    passData.downsampleKernel = msaa ? m_DownsampleKernelMSAA : m_DownsampleKernel;

                    passData.input = builder.ReadTexture(input);
                    passData.lowres = builder.WriteTexture(renderGraph.ImportTexture(m_LowresResolver));

                    builder.SetRenderFunc(
                        (ResolveVTData data, RenderGraphContext ctx) =>
                        {
                            RTHandle lowresBuffer = data.lowres;
                            RTHandle buffer = data.input;

                            Debug.Assert(data.lowresWidth <= data.resolver.CurrentWidth && data.lowresHeight <= data.resolver.CurrentHeight);
                            Debug.Assert(data.lowresWidth <= lowresBuffer.referenceSize.x && data.lowresHeight <= lowresBuffer.referenceSize.y);

                            int inputID = (buffer.isMSAAEnabled) ? HDShaderIDs._InputTextureMSAA : HDShaderIDs._InputTexture;

                            ctx.cmd.SetComputeTextureParam(data.downsampleCS, data.downsampleKernel, inputID, buffer);
                            ctx.cmd.SetComputeTextureParam(data.downsampleCS, data.downsampleKernel, HDShaderIDs._OutputTexture, lowresBuffer);
                            var resolveCounter = 0;
                            var startOffsetX = (resolveCounter % kResolveScaleFactor);
                            var startOffsetY = (resolveCounter / kResolveScaleFactor) % kResolveScaleFactor;
                            ctx.cmd.SetComputeVectorParam(data.downsampleCS, HDShaderIDs._Params, new Vector4(kResolveScaleFactor, startOffsetX, startOffsetY, /*unused*/ -1));
                            ctx.cmd.SetComputeVectorParam(data.downsampleCS, HDShaderIDs._Params1, new Vector4(data.width, data.height, data.lowresWidth, data.lowresHeight));
                            var TGSize = 8; //Match shader
                            ctx.cmd.DispatchCompute(data.downsampleCS, data.downsampleKernel, ((int)data.lowresWidth + (TGSize - 1)) / TGSize, ((int)data.lowresHeight + (TGSize - 1)) / TGSize, 1);

                            data.resolver.Process(ctx.cmd, lowresBuffer, 0, data.lowresWidth, 0, data.lowresHeight, 0, 0);

                            VirtualTexturing.System.Update();
                        });
                }
            }
        }

        void GetResolveDimensions(ref int w, ref int h)
        {
            w = Mathf.Max(Mathf.RoundToInt(m_ResolverScale.x * w), 1);
            h = Mathf.Max(Mathf.RoundToInt(m_ResolverScale.y * h), 1);
        }

        void CalculateResolverDimensions(ref int w, ref int h, VirtualTexturing.Resolver resolver)
        {
            //RT handles don't downscale in the editor but render with subrects.
#if UNITY_EDITOR
            int currentWidth = resolver.CurrentWidth;
            int currentHeight = resolver.CurrentHeight;

            if (currentWidth < w || currentHeight < h)
            {
                w = currentWidth < w ? w : currentWidth;
                h = currentHeight < h ? h : currentHeight;
            }
#endif
        }
    }
}
#endif
