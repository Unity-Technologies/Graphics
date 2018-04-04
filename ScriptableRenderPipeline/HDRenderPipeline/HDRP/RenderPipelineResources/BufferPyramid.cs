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
        TexturePadding m_TexturePadding;
        ComputeShader m_ColorPyramidCS;

        RTHandle m_ColorPyramidBuffer;
        List<RTHandle> m_ColorPyramidMips = new List<RTHandle>();
        int m_ColorPyramidKernel;

        ComputeShader m_DepthPyramidCS;
        RTHandle m_DepthPyramidBuffer;
        List<RTHandle> m_DepthPyramidMips = new List<RTHandle>();
        int[] m_DepthKernels = null;
        int depthKernel8 { get { return m_DepthKernels[0]; } }
        int depthKernel1 { get { return m_DepthKernels[1]; } }

        public RTHandle colorPyramid { get { return m_ColorPyramidBuffer; } }
        public RTHandle depthPyramid { get { return m_DepthPyramidBuffer; } }

        public BufferPyramid(
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

        float GetXRscale()
        {
            // for stereo double-wide, each half of the texture will represent a single eye's pyramid
            float scale = 1.0f;
            //if (m_Asset.renderPipelineSettings.supportsStereo && (desc.dimension != TextureDimension.Tex2DArray))
            //    scale = 2.0f; // double-wide
            return scale;
        }

        public void CreateBuffers()
        {
            m_ColorPyramidBuffer = RTHandle.Alloc(size => CalculatePyramidSize(size), filterMode: FilterMode.Trilinear, colorFormat: RenderTextureFormat.ARGBHalf, sRGB: false, useMipMap: true, autoGenerateMips: false, enableRandomWrite: true, name: "ColorPyramid");
            m_DepthPyramidBuffer = RTHandle.Alloc(size => CalculatePyramidSize(size), filterMode: FilterMode.Trilinear, colorFormat: RenderTextureFormat.RGFloat, sRGB: false, useMipMap: true, autoGenerateMips: false, enableRandomWrite: true, name: "DepthPyramid"); // Need randomReadWrite because we downsample the first mip with a compute shader.
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

        public int GetPyramidLodCount(HDCamera camera)
        {
            var minSize = Mathf.Min(camera.actualWidth, camera.actualHeight);
            return Mathf.FloorToInt(Mathf.Log(minSize, 2f));
        }

        Vector2Int CalculatePyramidMipSize(Vector2Int baseMipSize, int mipIndex)
        {
            return new Vector2Int(baseMipSize.x >> mipIndex, baseMipSize.y >> mipIndex);
        }

        Vector2Int CalculatePyramidSize(Vector2Int size)
        {
            // Instead of using the screen size, we round up to the next power of 2 because currently some platforms don't support NPOT Render Texture with mip maps (PS4 for example)
            // Then we render in a Screen Sized viewport.
            // Note that even if PS4 supported POT Mips, the buffers would be padded to the next power of 2 anyway (TODO: check with other platforms...)
            int pyramidSize = (int)Mathf.NextPowerOfTwo(Mathf.Max(size.x, size.y));
            return new Vector2Int((int)(pyramidSize * GetXRscale()), pyramidSize);
        }

        void UpdatePyramidMips(HDCamera camera, RenderTextureFormat format, List<RTHandle> mipList, int lodCount)
        {
            int currentLodCount = mipList.Count;
            if (lodCount > currentLodCount)
            {
                for (int i = currentLodCount; i < lodCount; ++i)
                {
                    int mipIndexCopy = i + 1; // Don't remove this copy! It's important for the value to be correctly captured by the lambda.
                    RTHandle newMip = RTHandle.Alloc(size => CalculatePyramidMipSize(CalculatePyramidSize(size), mipIndexCopy), colorFormat: format, sRGB: false, enableRandomWrite: true, useMipMap: false, filterMode: FilterMode.Bilinear, name: string.Format("PyramidMip{0}", i));
                    mipList.Add(newMip);
                }
            }
        }

        public Vector2 GetPyramidToScreenScale(HDCamera camera)
        {
            return new Vector2((float)camera.actualWidth / m_DepthPyramidBuffer.rt.width, (float)camera.actualHeight / m_DepthPyramidBuffer.rt.height);
        }

        public void RenderDepthPyramid(
            HDCamera hdCamera,
            CommandBuffer cmd,
            ScriptableRenderContext renderContext,
            RTHandle depthTexture)
        {
            int lodCount = GetPyramidLodCount(hdCamera);
            UpdatePyramidMips(hdCamera, m_DepthPyramidBuffer.rt.format, m_DepthPyramidMips, lodCount);

            Vector2 scale = GetPyramidToScreenScale(hdCamera);
            cmd.SetGlobalVector(HDShaderIDs._DepthPyramidSize, new Vector4(hdCamera.actualWidth, hdCamera.actualHeight, 1f / hdCamera.actualWidth, 1f / hdCamera.actualHeight));
            cmd.SetGlobalVector(HDShaderIDs._DepthPyramidScale, new Vector4(scale.x, scale.y, lodCount, 0.0f));

            m_GPUCopy.SampleCopyChannel_xyzw2x(cmd, depthTexture, m_DepthPyramidBuffer, new RectInt(0, 0, hdCamera.actualWidth, hdCamera.actualHeight));


            RTHandle src = m_DepthPyramidBuffer;
            for (var i = 0; i < lodCount; i++)
            {
                RTHandle dest = m_DepthPyramidMips[i];

                var srcMip = new RectInt(0, 0, hdCamera.actualWidth >> i, hdCamera.actualHeight >> i);
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
                cmd.CopyTexture(m_DepthPyramidMips[i], 0, 0, 0, 0, dstMipWidthToCopy, dstMipHeightToCopy, m_DepthPyramidBuffer, 0, i + 1, 0, 0);
                src = dest;
            }

            cmd.SetGlobalTexture(HDShaderIDs._DepthPyramidTexture, m_DepthPyramidBuffer);
        }

        public void RenderColorPyramid(
            HDCamera hdCamera,
            CommandBuffer cmd, 
            ScriptableRenderContext renderContext,
            RTHandle colorTexture)
        {
            int lodCount = GetPyramidLodCount(hdCamera);
            UpdatePyramidMips(hdCamera, m_ColorPyramidBuffer.rt.format, m_ColorPyramidMips, lodCount);

            Vector2 scale = GetPyramidToScreenScale(hdCamera);
            cmd.SetGlobalVector(HDShaderIDs._ColorPyramidSize, new Vector4(hdCamera.actualWidth, hdCamera.actualHeight, 1f / hdCamera.actualWidth, 1f / hdCamera.actualHeight));
            cmd.SetGlobalVector(HDShaderIDs._ColorPyramidScale, new Vector4(scale.x, scale.y, lodCount, 0.0f));

            // Copy mip 0
            // Here we blit a "camera space" texture into a square texture but we want to keep the original viewport.
            // Other BlitCameraTexture version will setup the viewport based on the destination RT scale (square here) so we need override it here.
            HDUtils.BlitCameraTexture(cmd, hdCamera, colorTexture, m_ColorPyramidBuffer, new Rect(0.0f, 0.0f, hdCamera.actualWidth, hdCamera.actualHeight));

            RTHandle src = m_ColorPyramidBuffer;
            for (var i = 0; i < lodCount; i++)
            {
                RTHandle dest = m_ColorPyramidMips[i];

                var srcMip = new RectInt(0, 0, hdCamera.actualWidth >> i, hdCamera.actualHeight >> i);
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
                    m_ColorPyramidMips[i], 
                    0, 0, 0, 0, 
                    dstMipWidthToCopy, dstMipHeightToCopy, m_ColorPyramidBuffer, 0, i + 1, 0, 0
                );

                src = dest;
            }

            cmd.SetGlobalTexture(HDShaderIDs._ColorPyramidTexture, m_ColorPyramidBuffer);
        }
    }
}
