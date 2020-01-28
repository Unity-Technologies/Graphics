#if ENABLE_VIRTUALTEXTURES
using VirtualTexturing = UnityEngine.Rendering.VirtualTexturing;
using UnityEngine.Experimental.Rendering;

namespace  UnityEngine.Rendering.HighDefinition
{
    public class VTBufferManager
    {
        public static GraphicsFormat GetFeedbackBufferFormat()
        {
            return GraphicsFormat.R8G8B8A8_UNorm;
        }

        RTHandle m_VTFeedbackBuffer;
        VirtualTexturing.Resolver m_Resolver = new VirtualTexturing.Resolver();
        public static int AdditionalForwardRT = 1;
        int resolveScale = 16;
        RTHandle lowresResolver;
        ComputeShader downSampleCS;

        public VTBufferManager(HDRenderPipelineAsset asset)
        {
            downSampleCS = asset.renderPipelineResources.shaders.VTFeedbackDownsample;
        }

        public void CreateBuffers(RenderPipelineSettings settings)
        {
            if (settings.supportMSAA || settings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly)
            {
                // Our processing handles both MSAA and regular buffers so we don't need to explicitly resolve here saving a buffer
                m_VTFeedbackBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, bindTextureMS: true,
                    enableMSAA: settings.supportMSAA, useDynamicScale: true, name: "VTFeedbackForwardMSAA");
            }
        }

        public void BeginRender(int width, int height)
        {
            GetResolveDimensions(ref width, ref height);

            if (width != m_Resolver.CurrentWidth || width != m_Resolver.CurrentHeight)
            {
                RTHandles.Release(lowresResolver);
            }
            lowresResolver = RTHandles.Alloc(width, height, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite: true, autoGenerateMips: false, name: "VTFeedback lowres");


            m_Resolver.UpdateSize((uint)width, (uint)height);
        }

        public void Resolve(CommandBuffer cmd, RTHandle rt, int width, int height)
        {
            if (rt != null)
            {
                ResolveVTDispatch(cmd, rt, width, height );
            }

            if (m_VTFeedbackBuffer != null)
            {
                ResolveVTDispatch(cmd, m_VTFeedbackBuffer, width, height );
            }
        }

        void ResolveVTDispatch(CommandBuffer cmd, RTHandle buffer, int width, int height)
        {
            string mainFunction = (buffer.enableMSAA) ? "KMainMSAA" : "KMain";
            int inputID = (buffer.enableMSAA) ? HDShaderIDs._InputTextureMSAA : HDShaderIDs._InputTexture;

            int kernel = downSampleCS.FindKernel(mainFunction);
            cmd.SetComputeTextureParam(downSampleCS, kernel, inputID, buffer.nameID);
            cmd.SetComputeTextureParam(downSampleCS, kernel, HDShaderIDs._OutputTexture, lowresResolver.nameID);
            var resolveCounter = 0;
            var startOffsetX = (resolveCounter % resolveScale);
            var startOffsetY = (resolveCounter / resolveScale) % resolveScale;
            cmd.SetComputeVectorParam(downSampleCS, HDShaderIDs._Params, new Vector4(resolveScale, startOffsetX, startOffsetY, /*unused*/-1));
            var TGSize = 8;
            cmd.DispatchCompute(downSampleCS, kernel, ((int)width + (TGSize - 1)) / TGSize, ((int)height + (TGSize - 1)) / TGSize, 1);

            GetResolveDimensions(ref width, ref height);
            m_Resolver.Process(cmd, lowresResolver.nameID, 0, (uint)width, 0, (uint)height, 0, 0);
        }

        void GetResolveDimensions(ref int w, ref int h)
        {
            w = (w + (resolveScale - 1)) / resolveScale;
            h = (h + (resolveScale - 1)) / resolveScale;
        }

        public void DestroyBuffers()
        {
            RTHandles.Release(m_VTFeedbackBuffer);
            m_VTFeedbackBuffer = null;
            m_Resolver.Dispose();
        }

        // This may be null in some cases where this buffer can be shared with the one used by the Gbuffer
        public RTHandle GetFeedbackBuffer()
        {
            return m_VTFeedbackBuffer;
        }


    }
}
#endif
