using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    class BufferPyramid
    {
        RTHandle m_ColorPyramidBuffer;
        List<RTHandle> m_ColorPyramidMips = new List<RTHandle>();

        RTHandle m_DepthPyramidBuffer;
        List<RTHandle> m_DepthPyramidMips = new List<RTHandle>();

        public RTHandle colorPyramid { get { return m_ColorPyramidBuffer; } }
        public RTHandle depthPyramid { get { return m_DepthPyramidBuffer; } }

        BufferPyramidProcessor m_Processor;

        public BufferPyramid(BufferPyramidProcessor processor)
            {
            m_Processor = processor;
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

            m_Processor.RenderDepthPyramid(
                hdCamera.actualWidth, hdCamera.actualHeight,
                cmd,
                depthTexture,
                m_DepthPyramidBuffer,
                m_DepthPyramidMips,
                lodCount,
                scale
                );

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

            m_Processor.RenderColorPyramid(
                hdCamera,
                cmd,
                colorTexture,
                m_ColorPyramidBuffer,
                m_ColorPyramidMips,
                lodCount,
                scale
                );

            cmd.SetGlobalTexture(HDShaderIDs._ColorPyramidTexture, m_ColorPyramidBuffer);
        }
    }
}
