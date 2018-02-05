using System.Collections.Generic;
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

        RTHandle m_ColorPyramidBuffer;
        List<RTHandle> m_ColorPyramidMips = new List<RTHandle>();
        int m_ColorPyramidKernel;

        ComputeShader m_DepthPyramidCS;
        RTHandle m_DepthPyramidBuffer;
        List<RTHandle> m_DepthPyramidMips = new List<RTHandle>();
        int m_DepthPyramidKernel_8;
        int m_DepthPyramidKernel_1;

        public RTHandle colorPyramid { get { return m_ColorPyramidBuffer; } }
        public RTHandle depthPyramid { get { return m_DepthPyramidBuffer; } }

        public BufferPyramid(
            ComputeShader colorPyramidCS,
            ComputeShader depthPyramidCS, GPUCopy gpuCopy)
        {
            m_ColorPyramidCS = colorPyramidCS;
            m_ColorPyramidKernel = m_ColorPyramidCS.FindKernel("KMain");

            m_DepthPyramidCS = depthPyramidCS;
            m_GPUCopy = gpuCopy;
            m_DepthPyramidKernel_8 = m_DepthPyramidCS.FindKernel("KMain_8");
            m_DepthPyramidKernel_1 = m_DepthPyramidCS.FindKernel("KMain_1");
        }

        public int GetPyramidLodCount(HDCamera camera)
        {
            var minSize = Mathf.Min(camera.actualWidth, camera.actualHeight);
            return Mathf.FloorToInt(Mathf.Log(minSize, 2f));
        }

        Vector2Int CalculatePyramidMipSize(Vector2Int baseMipSize, int mipIndex)
        {
            float scale = GetXRscale();
            return new Vector2Int((int)(baseMipSize.x  * scale) >> mipIndex, baseMipSize.y >> mipIndex);
        }

        void UpdatePyramidMips(HDCamera camera, RenderTextureFormat format, List<RTHandle> mipList, int lodCount)
        {
            int currentLodCount = mipList.Count;
            if (lodCount > currentLodCount)
            {
                for (int i = currentLodCount; i < lodCount; ++i)
                {
                    int mipIndexCopy = i + 1; // Don't remove this copy! It's important for the value to be correctly captured by the lambda.
                    RTHandle newMip = RTHandle.Alloc(size => CalculatePyramidMipSize(size, mipIndexCopy), colorFormat: format, sRGB: false, enableRandomWrite: true, useMipMap: false, filterMode: FilterMode.Bilinear);
                    mipList.Add(newMip);
                }
            }
        }

        public void RenderDepthPyramid(
            HDCamera hdCamera,
            CommandBuffer cmd,
            ScriptableRenderContext renderContext,
            RTHandle depthTexture)
        {
            int lodCount = GetPyramidLodCount(hdCamera);
            UpdatePyramidMips(hdCamera, m_DepthPyramidBuffer.rt.format, m_DepthPyramidMips, lodCount);

            cmd.SetGlobalVector(HDShaderIDs._DepthPyramidMipSize, new Vector4(hdCamera.actualWidth, hdCamera.actualHeight, lodCount, 0.0f));

            m_GPUCopy.SampleCopyChannel_xyzw2x(cmd, depthTexture, m_DepthPyramidBuffer, new Vector2(hdCamera.actualWidth, hdCamera.actualHeight));

            RTHandle src = m_DepthPyramidBuffer;
            for (var i = 0; i < lodCount; i++)
            {
                RTHandle dest = m_DepthPyramidMips[i];

                var srcMipWidth = hdCamera.actualWidth >> i;
                var srcMipHeight = hdCamera.actualHeight >> i;
                var dstMipWidth = srcMipWidth >> 1;
                var dstMipHeight = srcMipHeight >> 1;

                var kernel = m_DepthPyramidKernel_8;
                var kernelBlockSize = 8f;
                if (dstMipWidth < 4 * k_DepthBlockSize
                    || dstMipHeight < 4 * k_DepthBlockSize)
                {
                    kernel = m_DepthPyramidKernel_1;
                    kernelBlockSize = 1;
                }

                cmd.SetComputeTextureParam(m_DepthPyramidCS, kernel, _Source, src);
                cmd.SetComputeTextureParam(m_DepthPyramidCS, kernel, _Result, dest);
                cmd.SetComputeVectorParam(m_DepthPyramidCS, _SrcSize, new Vector4(srcMipWidth, srcMipHeight, hdCamera.scaleBias.x / srcMipWidth, hdCamera.scaleBias.y / srcMipHeight));

                cmd.DispatchCompute(
                    m_DepthPyramidCS,
                    kernel,
                    Mathf.CeilToInt(dstMipWidth / kernelBlockSize),
                    Mathf.CeilToInt(dstMipHeight / kernelBlockSize),
                    1);

                // If we could bind texture mips as UAV we could avoid this copy...(which moreover copies more than the needed viewport if not fullscreen)
                cmd.CopyTexture(m_DepthPyramidMips[i], 0, 0, m_DepthPyramidBuffer, 0, i + 1);
                src = dest;
            }

            cmd.SetGlobalTexture(HDShaderIDs._PyramidDepthTexture, m_DepthPyramidBuffer);
        }

        public void RenderColorPyramid(
            HDCamera hdCamera,
            CommandBuffer cmd, 
            ScriptableRenderContext renderContext,
            RTHandle colorTexture)
        {
            int lodCount = GetPyramidLodCount(hdCamera);
            UpdatePyramidMips(hdCamera, m_ColorPyramidBuffer.rt.format, m_ColorPyramidMips, lodCount);

            cmd.SetGlobalVector(HDShaderIDs._GaussianPyramidColorMipSize, new Vector4(hdCamera.actualWidth, hdCamera.actualHeight, lodCount, 0.0f));

            // Copy mip 0
            HDUtils.BlitCameraTexture(cmd, hdCamera, colorTexture, m_ColorPyramidBuffer); // true : bilinear

            RTHandle src = m_ColorPyramidBuffer;
            for (var i = 0; i < lodCount; i++)
            {
                RTHandle dest = m_ColorPyramidMips[i];

                var srcMipWidth = hdCamera.actualWidth >> i;
                var srcMipHeight = hdCamera.actualHeight >> i;
                var dstMipWidth = srcMipWidth >> 1;
                var dstMipHeight = srcMipHeight >> 1;

                // TODO: Add proper stereo support to the compute job

                cmd.SetComputeTextureParam(m_ColorPyramidCS, m_ColorPyramidKernel, _Source, src);
                cmd.SetComputeTextureParam(m_ColorPyramidCS, m_ColorPyramidKernel, _Result, dest);
                // _Size is used as a scale inside the whole render target so here we need to keep the full size (and not the scaled size depending on the current camera)
                cmd.SetComputeVectorParam(m_ColorPyramidCS, _Size, new Vector4(dest.rt.width, dest.rt.height, 1f / dest.rt.width, 1f / dest.rt.height));
                cmd.DispatchCompute(
                    m_ColorPyramidCS,
                    m_ColorPyramidKernel,
                    Mathf.CeilToInt(dstMipWidth / 8f),
                    Mathf.CeilToInt(dstMipHeight / 8f),
                    1);
                // If we could bind texture mips as UAV we could avoid this copy...(which moreover copies more than the needed viewport if not fullscreen)
                cmd.CopyTexture(m_ColorPyramidMips[i], 0, 0, m_ColorPyramidBuffer, 0, i + 1);

                src = dest;
            }

            cmd.SetGlobalTexture(HDShaderIDs._GaussianPyramidColorTexture, m_ColorPyramidBuffer);
        }

        float GetXRscale()
        {
            float scale = 1.0f;
            //if (m_Asset.renderPipelineSettings.supportsStereo && (desc.dimension != TextureDimension.Tex2DArray))
            //    scale = 2.0f; // double-wide
            return scale;
        }

        public void CreateBuffers()
        {
            Vector2 sizeScale = Vector2.one;
            sizeScale.x *= GetXRscale();

            m_ColorPyramidBuffer = RTHandle.Alloc(sizeScale, filterMode: FilterMode.Trilinear, colorFormat: RenderTextureFormat.ARGBHalf, sRGB: true, useMipMap: true, autoGenerateMips: false);
            m_DepthPyramidBuffer = RTHandle.Alloc(sizeScale, filterMode: FilterMode.Trilinear, colorFormat: RenderTextureFormat.RFloat, sRGB: false, useMipMap: true, autoGenerateMips: false, enableRandomWrite: true); // Need randomReadWrite because we downsample the first mip with a compute shader.
        }

        public void DestroyBuffers()
        {
            RTHandle.Release(m_ColorPyramidBuffer);
            RTHandle.Release(m_DepthPyramidBuffer);

            foreach (var rth in m_ColorPyramidMips)
            {
                RTHandle.Release(rth);
            }

            foreach (var rth in m_DepthPyramidMips)
            {
                RTHandle.Release(rth);
            }
        }
    }
}
