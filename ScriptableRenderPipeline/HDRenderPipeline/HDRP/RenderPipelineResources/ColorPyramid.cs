using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    class ColorPyramid
    {
        const int k_ColorBlockSize = 4;

        ComputeShader m_ColorPyramidCS;
        GPUCopy m_GPUCopy;
        Material m_Blit;
        int m_BlitTextureId;

        RenderTextureDescriptor m_RenderTextureDescriptor;
        int[] m_ColorPyramidMips = new int[0];
        int m_ColorPyramidKernel;

        public RenderTextureDescriptor renderTextureDescriptor { get { return m_RenderTextureDescriptor; } }

        public int usedMipMapCount { get { return Mathf.Min(bufferMipMapCount, m_ColorPyramidMips.Length); } }

        public int bufferMipMapCount
        {
            get
            {
                var minSize = Mathf.Min(renderTextureDescriptor.width, renderTextureDescriptor.height);
                return Mathf.FloorToInt(Mathf.Log(minSize, 2f));
            }
        }

        public ColorPyramid(ComputeShader colorPyramidCS, GPUCopy gpuCopy, Material blit, int blitTextureId, int[] mipIds)
        {
            m_ColorPyramidCS = colorPyramidCS;
            m_GPUCopy = gpuCopy;
            m_Blit = blit;
            m_BlitTextureId = blitTextureId;

            m_ColorPyramidKernel = m_ColorPyramidCS.FindKernel("KMain");
            m_ColorPyramidMips = mipIds;
        }

        public void RenderPyramidColor(
            HDCamera hdCamera,
            CommandBuffer cmd, 
            ScriptableRenderContext renderContext,
            RenderTargetIdentifier colorTexture,
            RenderTargetIdentifier targetTexture)
        {
            var colorPyramidDesc = renderTextureDescriptor;

            var lodCount = bufferMipMapCount;
            if (lodCount > m_ColorPyramidMips.Length)
            {
                Debug.LogWarningFormat("Cannot compute all mipmaps of the color pyramid, max texture size supported: {0}", (2 << m_ColorPyramidMips.Length).ToString());
                lodCount = m_ColorPyramidMips.Length;
            }

            cmd.SetGlobalTexture(m_BlitTextureId, colorTexture);
            CoreUtils.DrawFullScreen(cmd, m_Blit, colorTexture, null, 1); // Bilinear filtering

            var last = colorTexture;

            colorPyramidDesc.sRGB = false;
            colorPyramidDesc.enableRandomWrite = true;
            colorPyramidDesc.useMipMap = false;

            for (var i = 0; i < lodCount; i++)
            {
                colorPyramidDesc.width = colorPyramidDesc.width >> 1;
                colorPyramidDesc.height = colorPyramidDesc.height >> 1;

                // TODO: Add proper stereo support to the compute job

                cmd.ReleaseTemporaryRT(m_ColorPyramidMips[i + 1]);
                cmd.GetTemporaryRT(m_ColorPyramidMips[i + 1], colorPyramidDesc, FilterMode.Bilinear);
                cmd.SetComputeTextureParam(m_ColorPyramidCS, m_ColorPyramidKernel, "_Source", last);
                cmd.SetComputeTextureParam(m_ColorPyramidCS, m_ColorPyramidKernel, "_Result", m_ColorPyramidMips[i + 1]);
                cmd.SetComputeVectorParam(m_ColorPyramidCS, "_Size", new Vector4(colorPyramidDesc.width, colorPyramidDesc.height, 1f / colorPyramidDesc.width, 1f / colorPyramidDesc.height));
                cmd.DispatchCompute(m_ColorPyramidCS, m_ColorPyramidKernel, colorPyramidDesc.width / 8, colorPyramidDesc.height / 8, 1);
                cmd.CopyTexture(m_ColorPyramidMips[i + 1], 0, 0, targetTexture, 0, i + 1);

                last = m_ColorPyramidMips[i + 1];
            }

            for (int i = 0; i < lodCount; i++)
                cmd.ReleaseTemporaryRT(m_ColorPyramidMips[i + 1]);
        }

        public void Initialize(HDCamera hdCamera, bool enableStereo)
        {
            var desc = hdCamera.renderTextureDesc;
            desc.colorFormat = RenderTextureFormat.RFloat;
            desc.depthBufferBits = 0;
            desc.useMipMap = true;
            desc.autoGenerateMips = false;

            desc.msaaSamples = 1; // These are approximation textures, they don't need MSAA

            // for stereo double-wide, each half of the texture will represent a single eye's pyramid
            //var widthModifier = 1;
            //if (stereoEnabled && (desc.dimension != TextureDimension.Tex2DArray))
            //    widthModifier = 2; // double-wide

            //desc.width = pyramidSize * widthModifier;
            desc.width = (int)hdCamera.screenSize.x;
            desc.height = (int)hdCamera.screenSize.y;

            m_RenderTextureDescriptor = desc;
        }
    }
}
