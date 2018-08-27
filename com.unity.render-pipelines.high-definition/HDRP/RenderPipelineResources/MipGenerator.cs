using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    using RTHandle = RTHandleSystem.RTHandle;

    public class MipGenerator
    {
        RTHandle m_TempColorTarget;

        ComputeShader m_DepthPyramidCS;
        ComputeShader m_ColorPyramidCS;

        int m_DepthDownsampleKernel;
        int m_ColorDownsampleKernel;
        int m_ColorDownsampleKernelCopyMip0;
        int m_ColorGaussianKernel;

        public MipGenerator(HDRenderPipelineAsset asset)
        {
            m_DepthPyramidCS = asset.renderPipelineResources.depthPyramidCS;
            m_ColorPyramidCS = asset.renderPipelineResources.colorPyramidCS;

            m_DepthDownsampleKernel = m_DepthPyramidCS.FindKernel("KDepthDownsample8DualUav");
            m_ColorDownsampleKernel = m_ColorPyramidCS.FindKernel("KColorDownsample");
            m_ColorDownsampleKernelCopyMip0 = m_ColorPyramidCS.FindKernel("KColorDownsampleCopyMip0");
            m_ColorGaussianKernel = m_ColorPyramidCS.FindKernel("KColorGaussian");
        }

        public void Release()
        {
            RTHandles.Release(m_TempColorTarget);
            m_TempColorTarget = null;
        }

        // Generates an in-place depth pyramid
        // Returns the number of generated mips
        // TODO: Mip-mapping depth is problematic for precision at lower mips, generate a packed atlas instead
        public int RenderMinDepthPyramid(CommandBuffer cmd, Vector2Int size, RenderTexture texture)
        {
            var cs = m_DepthPyramidCS;
            int kernel = m_DepthDownsampleKernel;
            int srcMipLevel  = 0;
            int srcMipWidth  = size.x;
            int srcMipHeight = size.y;

            // TODO: Do it 1x MIP at a time for now. In the future, do 4x MIPs per pass, or even use a single pass.
            // Note: Gather() doesn't take a LOD parameter and we cannot bind an SRV of a MIP level,
            // and we don't support Min samplers either. So we are forced to perform 4x loads.
            while (srcMipWidth >= 2 || srcMipHeight >= 2)
            {
                int dstMipWidth  = Mathf.Max(1, srcMipWidth  >> 1);
                int dstMipHeight = Mathf.Max(1, srcMipHeight >> 1);

                cmd.SetComputeVectorParam(cs, HDShaderIDs._Size, new Vector4(srcMipWidth, srcMipHeight, 0f, 0f));
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._Source, texture, srcMipLevel);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._Destination, texture, srcMipLevel + 1);
                cmd.DispatchCompute(cs, kernel, (dstMipWidth + 7) / 8, (dstMipHeight + 7) / 8, 1);

                srcMipLevel++;
                srcMipWidth  = srcMipWidth  >> 1;
                srcMipHeight = srcMipHeight >> 1;
            }

            return srcMipLevel - 1;
        }
        
        // Generates the gaussian pyramid of source into destination
        // We can't do it in place as the color pyramid has to be read while writing to the color
        // buffer in some cases (e.g. refraction, distortion)
        // Returns the number of mips
        public int RenderColorGaussianPyramid(CommandBuffer cmd, Vector2Int size, Texture source, RenderTexture destination)
        {
            // Only create the temporary target on-demand in case the game doesn't actually need it
            if (m_TempColorTarget == null)
            {
                m_TempColorTarget = RTHandles.Alloc(
                    Vector2.one * 0.5f,
                    filterMode: FilterMode.Bilinear,
                    colorFormat: RenderTextureFormat.ARGBHalf,
                    sRGB: false,
                    enableRandomWrite: true,
                    useMipMap: false,
                    enableMSAA: false,
                    name: "Temp Gaussian Pyramid Target"
                );
            }

            var cs = m_ColorPyramidCS;
            int downsampleKernel = m_ColorDownsampleKernel;
            int downsampleKernelMip0 = m_ColorDownsampleKernelCopyMip0;
            int gaussianKernel = m_ColorGaussianKernel;
            int srcMipLevel  = 0;
            int srcMipWidth  = size.x;
            int srcMipHeight = size.y;


            // Note: smaller mips are excluded as we don't need them and the gaussian compute works
            // on 8x8 blocks
            // TODO: Could be further optimized by merging the smaller mips to reduce the amount of dispatches
            while (srcMipWidth >= 8 || srcMipHeight >= 8)
            {
                int dstMipWidth  = Mathf.Max(1, srcMipWidth  >> 1);
                int dstMipHeight = Mathf.Max(1, srcMipHeight >> 1);
                
                cmd.SetComputeVectorParam(cs, HDShaderIDs._Size, new Vector4(srcMipWidth, srcMipHeight, 0f, 0f));

                // First dispatch also copies src to dst mip0
                if (srcMipLevel == 0)
                {
                    cmd.SetComputeTextureParam(cs, downsampleKernelMip0, HDShaderIDs._Source, source, 0);
                    cmd.SetComputeTextureParam(cs, downsampleKernelMip0, HDShaderIDs._Mip0, destination, 0);
                    cmd.SetComputeTextureParam(cs, downsampleKernelMip0, HDShaderIDs._Destination, m_TempColorTarget);
                    cmd.DispatchCompute(cs, downsampleKernelMip0, (dstMipWidth + 7) / 8, (dstMipHeight + 7) / 8, 1);
                }
                else
                {
                    cmd.SetComputeTextureParam(cs, downsampleKernel, HDShaderIDs._Source, destination, srcMipLevel);
                    cmd.SetComputeTextureParam(cs, downsampleKernel, HDShaderIDs._Destination, m_TempColorTarget);
                    cmd.DispatchCompute(cs, downsampleKernel, (dstMipWidth + 7) / 8, (dstMipHeight + 7) / 8, 1);
                }

                cmd.SetComputeVectorParam(cs, HDShaderIDs._Size, new Vector4(dstMipWidth, dstMipHeight, 0f, 0f));
                cmd.SetComputeTextureParam(cs, gaussianKernel, HDShaderIDs._Source, m_TempColorTarget);
                cmd.SetComputeTextureParam(cs, gaussianKernel, HDShaderIDs._Destination, destination, srcMipLevel + 1);
                cmd.DispatchCompute(cs, gaussianKernel, (dstMipWidth + 7) / 8, (dstMipHeight + 7) / 8, 1);

                srcMipLevel++;
                srcMipWidth  = srcMipWidth  >> 1;
                srcMipHeight = srcMipHeight >> 1;
            }

            return srcMipLevel - 1;
        }
    }
}
