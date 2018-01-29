using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    class DepthPyramid
    {
        const int k_DepthBlockSize = 4;

        ComputeShader m_DepthPyramidCS;
        GPUCopy m_GPUCopy;

        RenderTextureDescriptor m_RenderTextureDescriptor;
        int[] m_DepthPyramidMips = new int[0];
        int m_DepthPyramidKernel_8;
        int m_DepthPyramidKernel_1;

        public RenderTextureDescriptor renderTextureDescriptor { get { return m_RenderTextureDescriptor; } }

        public int usedMipMapCount { get { return Mathf.Min(bufferMipMapCount, m_DepthPyramidMips.Length); } }

        public int bufferMipMapCount
        {
            get
            {
                var minSize = Mathf.Min(renderTextureDescriptor.width, renderTextureDescriptor.height);
                return Mathf.FloorToInt(Mathf.Log(minSize, 2f));
            }
        }

        public DepthPyramid(ComputeShader depthPyramidCS, GPUCopy gpuCopy, int[] mipIds)
        {
            m_DepthPyramidCS = depthPyramidCS;
            m_GPUCopy = gpuCopy;

            m_DepthPyramidKernel_8 = m_DepthPyramidCS.FindKernel("KMain_8");
            m_DepthPyramidKernel_1 = m_DepthPyramidCS.FindKernel("KMain_1");
            m_DepthPyramidMips = mipIds;
        }

        public void RenderPyramidDepth
            (HDCamera hdCamera,
            CommandBuffer cmd, 
            ScriptableRenderContext renderContext,
            RenderTargetIdentifier depthTexture,
            RenderTargetIdentifier targetTexture)
        {
            using (new ProfilingSample(cmd, "Pyramid Depth", CustomSamplerId.PyramidDepth.GetSampler()))
            {
                var depthPyramidDesc = m_RenderTextureDescriptor;

                var lodCount = bufferMipMapCount;
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

                    cmd.SetComputeTextureParam(m_DepthPyramidCS, kernel, "_Source", m_DepthPyramidMips[i]);
                    cmd.SetComputeTextureParam(m_DepthPyramidCS, kernel, "_Result", m_DepthPyramidMips[i + 1]);
                    cmd.SetComputeVectorParam(m_DepthPyramidCS, "_SrcSize", new Vector4(srcMipWidth, srcMipHeight, 1f / srcMipWidth, 1f / srcMipHeight));

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
