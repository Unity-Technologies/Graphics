using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    class BufferPyramidProcessor
    {
        static readonly int _Size = Shader.PropertyToID("_Size");
        static readonly int _Source = Shader.PropertyToID("_Source");
        static readonly int _Result = Shader.PropertyToID("_Result");
        static readonly int _SrcSize = Shader.PropertyToID("_SrcSize");
        const int k_DepthBlockSize = 4;

        GPUCopy m_GPUCopy;
        TexturePadding m_TexturePadding;
        ComputeShader m_ColorPyramidCS;
        int m_ColorPyramidKernel;

        ComputeShader m_DepthPyramidCS;
        int[] m_DepthKernels = null;
        int depthKernel8 { get { return m_DepthKernels[0]; } }
        int depthKernel1 { get { return m_DepthKernels[1]; } }

        public BufferPyramidProcessor(
            ComputeShader colorPyramidCS,
            ComputeShader depthPyramidCS, 
            GPUCopy gpuCopy,
            TexturePadding texturePadding
        )
        {
            m_ColorPyramidCS = colorPyramidCS;
            m_ColorPyramidKernel = m_ColorPyramidCS.FindKernel("KMain");

            m_DepthPyramidCS = depthPyramidCS;
            m_GPUCopy = gpuCopy;
            m_DepthKernels = new int[]
            {
                m_DepthPyramidCS.FindKernel("KDepthDownSample8"),
                m_DepthPyramidCS.FindKernel("KDepthDownSample1")
            };

            m_TexturePadding = texturePadding;
        }

        public void RenderDepthPyramid(
            int width, int height,
            CommandBuffer cmd,
            RTHandle sourceTexture,
            RTHandle targetTexture,
            List<RTHandle> mips,
            int lodCount,
            Vector2 scale
        )
        {
            m_GPUCopy.SampleCopyChannel_xyzw2x(cmd, sourceTexture, targetTexture, new RectInt(0, 0, width, height));

            RTHandle src = targetTexture;
            for (var i = 0; i < lodCount; i++)
            {
                RTHandle dest = mips[i];

                var srcMip = new RectInt(0, 0, width >> i, height >> i);
                var dstMip = new RectInt(0, 0, srcMip.width >> 1, srcMip.height >> 1);

                var kernel = depthKernel1;
                var kernelSize = 1;
                var srcWorkMip = srcMip;
                var dstWorkMip = dstMip;

                if (dstWorkMip.width >= 8 && dstWorkMip.height >= 8)
                {
                    srcWorkMip.width = Mathf.CeilToInt(srcWorkMip.width / 16.0f) * 16;
                    srcWorkMip.height = Mathf.CeilToInt(srcWorkMip.height / 16.0f) * 16;
                    dstWorkMip.width = srcWorkMip.width >> 1;
                    dstWorkMip.height = srcWorkMip.height >> 1;

                    m_TexturePadding.Pad(cmd, src, srcMip, srcWorkMip);
                    kernel = depthKernel8;
                    kernelSize = 8;
                }
                else
                {
                    m_TexturePadding.Pad(cmd, src, srcMip, new RectInt(0, 0, src.rt.width, src.rt.height));
                }

                cmd.SetComputeTextureParam(m_DepthPyramidCS, kernel, _Source, src);
                cmd.SetComputeTextureParam(m_DepthPyramidCS, kernel, _Result, dest);
                cmd.SetComputeVectorParam(m_DepthPyramidCS, _SrcSize, new Vector4(
                    srcWorkMip.width, srcWorkMip.height, 
                    (1.0f / srcWorkMip.width) * scale.x, (1.0f / srcWorkMip.height) * scale.y)
                );

                cmd.DispatchCompute(
                    m_DepthPyramidCS,
                    kernel,
                    Mathf.CeilToInt(dstWorkMip.width / (float)kernelSize),
                    Mathf.CeilToInt(dstWorkMip.height / (float)kernelSize),
                    1
                );

                var dstMipWidthToCopy = Mathf.Min(dest.rt.width, dstWorkMip.width);
                var dstMipHeightToCopy = Mathf.Min(dest.rt.height, dstWorkMip.height);

                // If we could bind texture mips as UAV we could avoid this copy...(which moreover copies more than the needed viewport if not fullscreen)
                cmd.CopyTexture(mips[i], 0, 0, 0, 0, dstMipWidthToCopy, dstMipHeightToCopy, targetTexture, 0, i + 1, 0, 0);
                src = dest;
            }
        }

        public void RenderColorPyramid(
            HDCamera hdCamera,
            CommandBuffer cmd, 
            RTHandle sourceTexture,
            RTHandle targetTexture,
            List<RTHandle> mips,
            int lodCount,
            Vector2 scale
        )
        {
            // Copy mip 0
            // Here we blit a "camera space" texture into a square texture but we want to keep the original viewport.
            // Other BlitCameraTexture version will setup the viewport based on the destination RT scale (square here) so we need override it here.
            HDUtils.BlitCameraTexture(cmd, hdCamera, sourceTexture, targetTexture, new Rect(0.0f, 0.0f, hdCamera.actualWidth, hdCamera.actualHeight));

            RenderColorPyramidMips(
                hdCamera.actualWidth, hdCamera.actualHeight,
                cmd,
                sourceTexture,
                targetTexture,
                mips,
                lodCount,
                scale
            );
        }

        public void RenderColorPyramid(
            int width, int height,
            CommandBuffer cmd, 
            RTHandle sourceTexture,
            RTHandle targetTexture,
            List<RTHandle> mips,
            int lodCount,
            Vector2 scale
        )
        {
            // Copy mip 0
            HDUtils.BlitTexture(cmd, sourceTexture, targetTexture, scale, 0, false);

            RenderColorPyramidMips(
                width, height,
                cmd,
                sourceTexture,
                targetTexture,
                mips,
                lodCount,
                scale
            );
        }

        void RenderColorPyramidMips(
            int width, int height,
            CommandBuffer cmd, 
            RTHandle sourceTexture,
            RTHandle targetTexture,
            List<RTHandle> mips,
            int lodCount,
            Vector2 scale
        )
        {
            RTHandle src = targetTexture;
            for (var i = 0; i < lodCount; i++)
            {
                RTHandle dest = mips[i];

                var srcMip = new RectInt(0, 0, width >> i, height >> i);
                var dstMip = new RectInt(0, 0, srcMip.width >> 1, srcMip.height >> 1);
                var srcWorkMip = new RectInt(
                    0, 
                    0, 
                    Mathf.CeilToInt(srcMip.width / 16.0f) * 16,
                    Mathf.CeilToInt(srcMip.height / 16.0f) * 16
                );
                var dstWorkMip = new RectInt(0, 0, srcWorkMip.width >> 1, srcWorkMip.height >> 1);

                m_TexturePadding.Pad(cmd, src, srcMip, srcWorkMip);

                // TODO: Add proper stereo support to the compute job

                cmd.SetComputeTextureParam(m_ColorPyramidCS, m_ColorPyramidKernel, _Source, src);
                cmd.SetComputeTextureParam(m_ColorPyramidCS, m_ColorPyramidKernel, _Result, dest);
                // _Size is used as a scale inside the whole render target so here we need to keep the full size (and not the scaled size depending on the current camera)
                cmd.SetComputeVectorParam(
                    m_ColorPyramidCS, 
                    _Size, 
                    new Vector4(dest.rt.width, dest.rt.height, 1f / dest.rt.width, 1f / dest.rt.height)
                );
                cmd.DispatchCompute(
                    m_ColorPyramidCS,
                    m_ColorPyramidKernel,
                    dstWorkMip.width / 8,
                    dstWorkMip.height / 8,
                    1
                );

                var dstMipWidthToCopy = Mathf.Min(dest.rt.width, dstWorkMip.width);
                var dstMipHeightToCopy = Mathf.Min(dest.rt.height, dstWorkMip.height);

                // If we could bind texture mips as UAV we could avoid this copy...(which moreover copies more than the needed viewport if not fullscreen)
                cmd.CopyTexture(
                    mips[i], 
                    0, 0, 0, 0, 
                    dstMipWidthToCopy, dstMipHeightToCopy, targetTexture, 0, i + 1, 0, 0
                );

                src = dest;
            }
        }
    }
}
