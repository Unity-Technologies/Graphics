using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    using RTHandle = RTHandleSystem.RTHandle;

    public class MipGenerator
    {
        RTHandle m_TempFullscreenTarget;

        ComputeShader m_DepthPyramidCS;
        ComputeShader m_ColorPyramidCS;

        int m_DepthDownsampleKernel;
        int m_ColorDownsampleKernel;
        int m_ColorGaussianKernel;

        public MipGenerator(HDRenderPipelineAsset asset)
        {
            m_DepthPyramidCS = asset.renderPipelineResources.depthPyramidCS;
            m_ColorPyramidCS = asset.renderPipelineResources.colorPyramidCS;

            m_DepthDownsampleKernel = m_DepthPyramidCS.FindKernel("KDepthDownsample8DualUav");
            m_ColorDownsampleKernel = m_ColorPyramidCS.FindKernel("KColorDownsample");
            m_ColorGaussianKernel = m_ColorPyramidCS.FindKernel("KColorGaussian");
        }

        public void Release()
        {
            RTHandles.Release(m_TempFullscreenTarget);
            m_TempFullscreenTarget = null;
        }

        // Generates an in-place depth pyramid
        // Returns the number of generated mips
        // TODO: Mip-mapping depth is problematic for precision at lower mips, generate a packed atlas instead
        public int RenderMinDepthPyramid(CommandBuffer cmd, Vector2Int size, RTHandle texture)
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
        
        // Generates an in-place gaussian pyramid
        // Returns the number of mips
        public int RenderColorGaussianPyramid(CommandBuffer cmd, Vector2Int size, RTHandle texture)
        {
            return RenderColorGaussianPyramid(cmd, size, texture.rt);
        }

        // Need this RenderTexture variant because some of the code in HDRP still hasn't switched to
        // the RTHandle system :(
        public int RenderColorGaussianPyramid(CommandBuffer cmd, Vector2Int size, RenderTexture texture)
        {
            // Only create the temporary target on-demand in case the game doesn't actually need it
            if (m_TempFullscreenTarget == null)
            {
                m_TempFullscreenTarget = RTHandles.Alloc(
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
            int gaussianKernel = m_ColorGaussianKernel;
            int srcMipLevel  = 0;
            int srcMipWidth  = size.x;
            int srcMipHeight = size.y;

            // Note: smaller mips are excluded as we don't need them and the gaussian compute works
            // on 8x8 blocks
            // TODO: Could be further optimized by merging the smaller mips to reduce the amount of dispatches
            while (srcMipWidth >= 2 || srcMipHeight >= 2)
            {
                int dstMipWidth  = Mathf.Max(1, srcMipWidth  >> 1);
                int dstMipHeight = Mathf.Max(1, srcMipHeight >> 1);

                cmd.SetComputeVectorParam(cs, HDShaderIDs._Size, new Vector4(srcMipWidth, srcMipHeight, 0f, 0f));
                cmd.SetComputeTextureParam(cs, downsampleKernel, HDShaderIDs._Source, texture, srcMipLevel);
                cmd.SetComputeTextureParam(cs, downsampleKernel, HDShaderIDs._Destination, m_TempFullscreenTarget);
                cmd.DispatchCompute(cs, downsampleKernel, (dstMipWidth + 7) / 8, (dstMipHeight + 7) / 8, 1);

                cmd.SetComputeVectorParam(cs, HDShaderIDs._Size, new Vector4(dstMipWidth, dstMipHeight, 0f, 0f));
                cmd.SetComputeTextureParam(cs, gaussianKernel, HDShaderIDs._Source, m_TempFullscreenTarget);
                cmd.SetComputeTextureParam(cs, gaussianKernel, HDShaderIDs._Destination, texture, srcMipLevel + 1);
                cmd.DispatchCompute(cs, gaussianKernel, (dstMipWidth + 7) / 8, (dstMipHeight + 7) / 8, 1);

                srcMipLevel++;
                srcMipWidth  = srcMipWidth  >> 1;
                srcMipHeight = srcMipHeight >> 1;
            }

            return srcMipLevel - 1;
        }
    }
}
