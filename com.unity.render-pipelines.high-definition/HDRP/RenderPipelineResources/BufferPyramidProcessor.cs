using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class BufferPyramidProcessor
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

        List<RenderTexture> m_RenderColorPyramid_CastTmp = new List<RenderTexture>();

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
            RTHandleSystem.RTHandle sourceTexture,
            RTHandleSystem.RTHandle targetTexture,
            List<RTHandleSystem.RTHandle> mips,
            int lodCount,
            Vector2 scale
            )
        {
            m_GPUCopy.SampleCopyChannel_xyzw2x(cmd, sourceTexture, targetTexture, new RectInt(0, 0, width, height));

            var src = targetTexture;
            for (var i = 0; i < lodCount; i++)
            {
                var dest = mips[i];

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
                // The compute shader work in texture space
                // So we must provide the texture's size
                cmd.SetComputeVectorParam(m_DepthPyramidCS, _SrcSize, new Vector4(
                        src.rt.width, src.rt.height,
                        (1.0f / src.rt.width), (1.0f / src.rt.height))
                    );

                cmd.DispatchCompute(
                    m_DepthPyramidCS,
                    kernel,
                    Mathf.CeilToInt(dstWorkMip.width / (float)kernelSize),
                    Mathf.CeilToInt(dstWorkMip.height / (float)kernelSize),
                    1
                    );

                var dstMipWidthToCopy = Mathf.Min(Mathf.Min(targetTexture.rt.width >> (i + 1), dstWorkMip.width), mips[i].rt.width);
                var dstMipHeightToCopy = Mathf.Min(Mathf.Min(targetTexture.rt.height >> (i + 1), dstWorkMip.height), mips[i].rt.height);

                // If we could bind texture mips as UAV we could avoid this copy...(which moreover copies more than the needed viewport if not fullscreen)
                cmd.CopyTexture(mips[i], 0, 0, 0, 0, dstMipWidthToCopy, dstMipHeightToCopy, targetTexture, 0, i + 1, 0, 0);
                src = dest;
            }
        }

        public void RenderColorPyramid(
            HDCamera hdCamera,
            CommandBuffer cmd,
            RTHandleSystem.RTHandle sourceTexture,
            RTHandleSystem.RTHandle targetTexture,
            List<RTHandleSystem.RTHandle> mips,
            int lodCount,
            Vector2 scale
            )
        {
            // Copy mip 0
            // Here we blit a "camera space" texture into a square texture but we want to keep the original viewport.
            // Other BlitCameraTexture version will setup the viewport based on the destination RT scale (square here) so we need override it here.
            HDUtils.BlitCameraTexture(cmd, hdCamera, sourceTexture, targetTexture, new Rect(0.0f, 0.0f, hdCamera.actualWidth, hdCamera.actualHeight));

            m_RenderColorPyramid_CastTmp.Clear();
            for (var i = 0; i < mips.Count; ++i)
                m_RenderColorPyramid_CastTmp.Add(mips[i]);
            RenderColorPyramidMips(
                new RectInt(0, 0, hdCamera.actualWidth, hdCamera.actualHeight),
                cmd,
                targetTexture,
                m_RenderColorPyramid_CastTmp,
                lodCount,
                scale
                );
        }

        public void RenderColorPyramid(
            RectInt srcRect,
            CommandBuffer cmd,
            Texture sourceTexture,
            RenderTexture targetTexture,
            List<RenderTexture> mips,
            int lodCount
            )
        {
            Assert.AreEqual(0, srcRect.x, "Offset are not supported");
            Assert.AreEqual(0, srcRect.y, "Offset are not supported");
            Assert.IsTrue(srcRect.width > 0);
            Assert.IsTrue(srcRect.height > 0);

            var scale = new Vector2(
                    sourceTexture.width / (float)srcRect.width,
                    sourceTexture.height / (float)srcRect.height
                    );

            cmd.Blit(sourceTexture, targetTexture, scale, Vector2.zero);

            RenderColorPyramidMips(
                srcRect,
                cmd,
                targetTexture,
                mips,
                lodCount,
                scale
                );
        }

        void RenderColorPyramidMips(
            RectInt srcRect,
            CommandBuffer cmd,
            RenderTexture targetTexture,
            List<RenderTexture> mips,
            int lodCount,
            Vector2 scale
            )
        {
            Assert.AreEqual(0, srcRect.x, "Offset are not supported");
            Assert.AreEqual(0, srcRect.y, "Offset are not supported");
            Assert.IsTrue(srcRect.width > 0);
            Assert.IsTrue(srcRect.height > 0);

            var src = targetTexture;
            for (var i = 0; i < lodCount; i++)
            {
                var dest = mips[i];

                var srcMip = new RectInt(0, 0, srcRect.width >> i, srcRect.height >> i);
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
                    new Vector4(src.width >> 1, src.height >> 1, 1f / (src.width >> 1), 1f / (src.height >> 1))
                    );
                cmd.DispatchCompute(
                    m_ColorPyramidCS,
                    m_ColorPyramidKernel,
                    dstWorkMip.width / 8,
                    dstWorkMip.height / 8,
                    1
                    );

                var dstMipWidthToCopy = Mathf.Min(Mathf.Min(targetTexture.width >> (i + 1), dstWorkMip.width), mips[i].width);
                var dstMipHeightToCopy = Mathf.Min(Mathf.Min(targetTexture.height >> (i + 1), dstWorkMip.height), mips[i].height);

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
