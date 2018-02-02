using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    class BufferPyramid
    {
        static readonly int _Size = Shader.PropertyToID("_Size");
        static readonly int _Source = Shader.PropertyToID("_Source");
        static readonly int _Result = Shader.PropertyToID("_Result");
        static readonly int _SrcSize = Shader.PropertyToID("_SrcSize");
        const int k_DepthBlockSize = 4;

        GPUCopy m_GPUCopy;
        ComputeShader m_ColorPyramidCS;

        RenderTextureDescriptor m_ColorRenderTextureDescriptor;
        int[] m_ColorPyramidMips = new int[0];
        int m_ColorPyramidKernel;

        ComputeShader m_DepthPyramidCS;
        RenderTextureDescriptor m_DepthRenderTextureDescriptor;
        int[] m_DepthPyramidMips = new int[0];
        int m_DepthPyramidKernel_8;
        int m_DepthPyramidKernel_1;

        public RenderTextureDescriptor colorRenderTextureDescriptor { get { return m_ColorRenderTextureDescriptor; } }
        public int colorUsedMipMapCount { get { return Mathf.Min(colorBufferMipMapCount, m_ColorPyramidMips.Length); } }
        public int colorBufferMipMapCount
        {
            get
            {
                var minSize = Mathf.Min(colorRenderTextureDescriptor.width, colorRenderTextureDescriptor.height);
                return Mathf.FloorToInt(Mathf.Log(minSize, 2f));
            }
        }

        public RenderTextureDescriptor depthRenderTextureDescriptor { get { return m_DepthRenderTextureDescriptor; } }
        public int depthUsedMipMapCount { get { return Mathf.Min(depthBufferMipMapCount, m_DepthPyramidMips.Length); } }
        public int depthBufferMipMapCount
        {
            get
            {
                var minSize = Mathf.Min(depthRenderTextureDescriptor.width, depthRenderTextureDescriptor.height);
                return Mathf.FloorToInt(Mathf.Log(minSize, 2f));
            }
        }

        public BufferPyramid(
            ComputeShader colorPyramidCS, int[] colorMipIds,
            ComputeShader depthPyramidCS, GPUCopy gpuCopy, int[] depthMipIds)
        {
            m_ColorPyramidCS = colorPyramidCS;
            m_ColorPyramidKernel = m_ColorPyramidCS.FindKernel("KMain");
            m_ColorPyramidMips = colorMipIds;

            m_DepthPyramidCS = depthPyramidCS;
            m_GPUCopy = gpuCopy;
            m_DepthPyramidMips = depthMipIds;
            m_DepthPyramidKernel_8 = m_DepthPyramidCS.FindKernel("KMain_8");
            m_DepthPyramidKernel_1 = m_DepthPyramidCS.FindKernel("KMain_1");
        }

        public void RenderDepthPyramid(
            HDCamera hdCamera,
            CommandBuffer cmd,
            ScriptableRenderContext renderContext,
            RenderTargetIdentifier depthTexture,
            RenderTargetIdentifier targetTexture)
        {
            var depthPyramidDesc = m_DepthRenderTextureDescriptor;

            var lodCount = depthBufferMipMapCount;
            if (lodCount > m_DepthPyramidMips.Length)
            {
                Debug.LogWarningFormat("Cannot compute all mipmaps of the depth pyramid, max texture size supported: {0}", (2 << m_DepthPyramidMips.Length).ToString());
                lodCount = m_DepthPyramidMips.Length;
            }

            cmd.ReleaseTemporaryRT(m_DepthPyramidMips[0]);

            depthPyramidDesc.sRGB = false;
            depthPyramidDesc.enableRandomWrite = true;
            depthPyramidDesc.useMipMap = false;

            cmd.GetTemporaryRT(m_DepthPyramidMips[0], depthPyramidDesc, FilterMode.Bilinear);
            m_GPUCopy.SampleCopyChannel_xyzw2x(cmd, depthTexture, m_DepthPyramidMips[0], new Vector2(depthPyramidDesc.width, depthPyramidDesc.height));
            cmd.CopyTexture(m_DepthPyramidMips[0], 0, 0, targetTexture, 0, 0);

            for (var i = 0; i < lodCount; i++)
            {
                var srcMipWidth = depthPyramidDesc.width;
                var srcMipHeight = depthPyramidDesc.height;
                depthPyramidDesc.width = srcMipWidth >> 1;
                depthPyramidDesc.height = srcMipHeight >> 1;

                var kernel = m_DepthPyramidKernel_8;
                var kernelBlockSize = 8f;
                if (depthPyramidDesc.width < 4 * k_DepthBlockSize
                    || depthPyramidDesc.height < 4 * k_DepthBlockSize)
                {
                    kernel = m_DepthPyramidKernel_1;
                    kernelBlockSize = 1;
                }

                cmd.ReleaseTemporaryRT(m_DepthPyramidMips[i + 1]);
                cmd.GetTemporaryRT(m_DepthPyramidMips[i + 1], depthPyramidDesc, FilterMode.Bilinear);

                cmd.SetComputeTextureParam(m_DepthPyramidCS, kernel, _Source, m_DepthPyramidMips[i]);
                cmd.SetComputeTextureParam(m_DepthPyramidCS, kernel, _Result, m_DepthPyramidMips[i + 1]);
                cmd.SetComputeVectorParam(m_DepthPyramidCS, _SrcSize, new Vector4(srcMipWidth, srcMipHeight, 1f / srcMipWidth, 1f / srcMipHeight));

                cmd.DispatchCompute(
                    m_DepthPyramidCS,
                    kernel,
                    Mathf.CeilToInt(depthPyramidDesc.width / kernelBlockSize),
                    Mathf.CeilToInt(depthPyramidDesc.height / kernelBlockSize),
                    1);

                cmd.CopyTexture(m_DepthPyramidMips[i + 1], 0, 0, targetTexture, 0, i + 1);
            }

            for (int i = 0; i < lodCount + 1; i++)
                cmd.ReleaseTemporaryRT(m_DepthPyramidMips[i]);
        }

        public void RenderColorPyramid(
            HDCamera hdCamera,
            CommandBuffer cmd, 
            ScriptableRenderContext renderContext,
            RenderTargetIdentifier colorTexture,
            RenderTargetIdentifier targetTexture)
        {
            var colorPyramidDesc = colorRenderTextureDescriptor;

            var lodCount = colorBufferMipMapCount;
            if (lodCount > m_ColorPyramidMips.Length)
            {
                Debug.LogWarningFormat("Cannot compute all mipmaps of the color pyramid, max texture size supported: {0}", (2 << m_ColorPyramidMips.Length).ToString());
                lodCount = m_ColorPyramidMips.Length;
            }

            // Copy mip 0
            cmd.CopyTexture(colorTexture, 0, 0, targetTexture, 0, 0);
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
                cmd.SetComputeTextureParam(m_ColorPyramidCS, m_ColorPyramidKernel, _Source, last);
                cmd.SetComputeTextureParam(m_ColorPyramidCS, m_ColorPyramidKernel, _Result, m_ColorPyramidMips[i + 1]);
                cmd.SetComputeVectorParam(m_ColorPyramidCS, _Size, new Vector4(colorPyramidDesc.width, colorPyramidDesc.height, 1f / colorPyramidDesc.width, 1f / colorPyramidDesc.height));
                cmd.DispatchCompute(
                    m_ColorPyramidCS,
                    m_ColorPyramidKernel,
                    Mathf.CeilToInt(colorPyramidDesc.width / 8f),
                    Mathf.CeilToInt(colorPyramidDesc.height / 8f),
                    1);
                cmd.CopyTexture(m_ColorPyramidMips[i + 1], 0, 0, targetTexture, 0, i + 1);

                last = m_ColorPyramidMips[i + 1];
            }

            for (int i = 0; i < lodCount; i++)
                cmd.ReleaseTemporaryRT(m_ColorPyramidMips[i + 1]);
        }

        public void Initialize(HDCamera hdCamera, bool enableStereo)
        {
            var colorDesc = CalculateRenderTextureDescriptor(hdCamera, enableStereo);
            colorDesc.colorFormat = RenderTextureFormat.ARGBHalf;
            m_ColorRenderTextureDescriptor = colorDesc;

            var depthDesc = CalculateRenderTextureDescriptor(hdCamera, enableStereo);
            depthDesc.colorFormat = RenderTextureFormat.RFloat;
            m_DepthRenderTextureDescriptor = depthDesc;
        }

        public static RenderTextureDescriptor CalculateRenderTextureDescriptor(HDCamera hdCamera, bool enableStereo)
        {
            var desc = hdCamera.renderTextureDesc;
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

            return desc;
        }
    }
}
