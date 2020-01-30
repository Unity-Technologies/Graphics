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
        const int resolveScaleFactor = 16;
        Vector2 resolverScale = new Vector2(1.0f / (float)resolveScaleFactor, 1.0f / (float)resolveScaleFactor);
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

            lowresResolver = RTHandles.Alloc(resolverScale, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite: true, autoGenerateMips: false, name: "VTFeedback lowres");
        }

        public void BeginRender(int width, int height)
        {
            GetResolveDimensions(ref width, ref height);
            m_Resolver.UpdateSize(width, height);
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
            // We allow only resolving a sub-rectangle of a larger allocated buffer but not the other way around.
            Debug.Assert(width <= buffer.referenceSize.x && height <= buffer.referenceSize.y);

            int lowResWidth = width;
            int lowResHeight = height;
            GetResolveDimensions(ref lowResWidth, ref lowResHeight);
            Debug.Assert(lowResWidth <= m_Resolver.CurrentWidth && lowResHeight <= m_Resolver.CurrentHeight);
            Debug.Assert(lowResWidth <= lowresResolver.referenceSize.x && lowResHeight <= lowresResolver.referenceSize.y);

            string mainFunction = (buffer.enableMSAA) ? "KMainMSAA" : "KMain";
            int inputID = (buffer.enableMSAA) ? HDShaderIDs._InputTextureMSAA : HDShaderIDs._InputTexture;

            int kernel = downSampleCS.FindKernel(mainFunction);
            cmd.SetComputeTextureParam(downSampleCS, kernel, inputID, buffer.nameID);
            cmd.SetComputeTextureParam(downSampleCS, kernel, HDShaderIDs._OutputTexture, lowresResolver.nameID);
            var resolveCounter = 0;
            var startOffsetX = (resolveCounter % resolveScaleFactor);
            var startOffsetY = (resolveCounter / resolveScaleFactor) % resolveScaleFactor;
            cmd.SetComputeVectorParam(downSampleCS, HDShaderIDs._Params, new Vector4(resolveScaleFactor, startOffsetX, startOffsetY, /*unused*/-1));
            cmd.SetComputeVectorParam(downSampleCS, HDShaderIDs._Params1, new Vector4(width, height, lowResWidth, lowResHeight));
            var TGSize = 8; //Match shader
            cmd.DispatchCompute(downSampleCS, kernel, ((int)lowResWidth + (TGSize - 1)) / TGSize, ((int)lowResHeight + (TGSize - 1)) / TGSize, 1);

            m_Resolver.Process(cmd, lowresResolver.nameID, 0, lowResWidth, 0, lowResHeight, 0, 0);
        }

        void GetResolveDimensions(ref int w, ref int h)
        {
            w = Mathf.Max(Mathf.RoundToInt(resolverScale.x * w), 1);
            h = Mathf.Max(Mathf.RoundToInt(resolverScale.y * h), 1);
        }

        public void DestroyBuffers()
        {
            RTHandles.Release(m_VTFeedbackBuffer);
            RTHandles.Release(lowresResolver);
            m_VTFeedbackBuffer = null;
            lowresResolver = null;
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
