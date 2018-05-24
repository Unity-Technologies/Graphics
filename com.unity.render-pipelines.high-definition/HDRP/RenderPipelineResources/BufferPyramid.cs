using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    class BufferPyramid
    {
        List<RTHandleSystem.RTHandle> m_ColorPyramidMips = new List<RTHandleSystem.RTHandle>();
        List<RTHandleSystem.RTHandle> m_DepthPyramidMips = new List<RTHandleSystem.RTHandle>();

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

        public void DestroyBuffers()
        {
            foreach (var rth in m_ColorPyramidMips)
                RTHandles.Release(rth);

            foreach (var rth in m_DepthPyramidMips)
                RTHandles.Release(rth);
        }

        public int GetPyramidLodCount(Vector2Int size)
        {
            var minSize = Mathf.Min(size.x, size.y);
            return Mathf.Max(0, Mathf.FloorToInt(Mathf.Log(minSize, 2f)));
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

        void UpdatePyramidMips(HDCamera camera, RenderTextureFormat format, List<RTHandleSystem.RTHandle> mipList, int lodCount)
        {
            int currentLodCount = mipList.Count;
            if (lodCount > currentLodCount)
            {
                for (int i = currentLodCount; i < lodCount; ++i)
                {
                    int mipIndexCopy = i + 1; // Don't remove this copy! It's important for the value to be correctly captured by the lambda.
                    var newMip = RTHandles.Alloc(size => CalculatePyramidMipSize(CalculatePyramidSize(size), mipIndexCopy), colorFormat: format, sRGB: false, enableRandomWrite: true, useMipMap: false, filterMode: FilterMode.Bilinear, name: string.Format("PyramidMip{0}", i));
                    mipList.Add(newMip);
                }
            }
        }

        public Vector2 GetPyramidToScreenScale(HDCamera camera, RTHandleSystem.RTHandle rth)
        {
            return new Vector2((float)camera.actualWidth / rth.rt.width, (float)camera.actualHeight / rth.rt.height);
        }

        public void RenderDepthPyramid(
            HDCamera hdCamera,
            CommandBuffer cmd,
            ScriptableRenderContext renderContext,
            RTHandleSystem.RTHandle sourceDepthTexture,
            RTHandleSystem.RTHandle targetDepthTexture)
        {
            int lodCount = Mathf.Min(
                    GetPyramidLodCount(targetDepthTexture.referenceSize),
                    GetPyramidLodCount(new Vector2Int(hdCamera.actualWidth, hdCamera.actualHeight))
                    );
            if (lodCount == 0)
            {
                Debug.LogWarning("The target for the pyramid buffer has an invalid size. Skipping DepthPyramid calculation.");
                return;
            }

            UpdatePyramidMips(hdCamera, targetDepthTexture.rt.format, m_DepthPyramidMips, lodCount);

            Vector2 scale = GetPyramidToScreenScale(hdCamera, targetDepthTexture);
            cmd.SetGlobalVector(HDShaderIDs._DepthPyramidSize, new Vector4(hdCamera.actualWidth, hdCamera.actualHeight, 1f / hdCamera.actualWidth, 1f / hdCamera.actualHeight));
            cmd.SetGlobalVector(HDShaderIDs._DepthPyramidScale, new Vector4(scale.x, scale.y, lodCount, 0.0f));

            m_Processor.RenderDepthPyramid(
                hdCamera.actualWidth, hdCamera.actualHeight,
                cmd,
                sourceDepthTexture,
                targetDepthTexture,
                m_DepthPyramidMips,
                lodCount,
                scale
                );

            cmd.SetGlobalTexture(HDShaderIDs._DepthPyramidTexture, targetDepthTexture);
        }

        public void RenderColorPyramid(
            HDCamera hdCamera,
            CommandBuffer cmd,
            ScriptableRenderContext renderContext,
            RTHandleSystem.RTHandle sourceColorTexture,
            RTHandleSystem.RTHandle targetColorTexture)
        {
            int lodCount = Mathf.Min(
                    GetPyramidLodCount(targetColorTexture.referenceSize),
                    GetPyramidLodCount(new Vector2Int(hdCamera.actualWidth, hdCamera.actualHeight))
                    );
            if (lodCount == 0)
            {
                Debug.LogWarning("The target for the pyramid buffer has an invalid size. Skipping ColorPyramid calculation.");
                return;
            }

            UpdatePyramidMips(hdCamera, targetColorTexture.rt.format, m_ColorPyramidMips, lodCount);

            Vector2 scale = GetPyramidToScreenScale(hdCamera, targetColorTexture);
            cmd.SetGlobalVector(HDShaderIDs._ColorPyramidSize, new Vector4(hdCamera.actualWidth, hdCamera.actualHeight, 1f / hdCamera.actualWidth, 1f / hdCamera.actualHeight));
            cmd.SetGlobalVector(HDShaderIDs._ColorPyramidScale, new Vector4(scale.x, scale.y, lodCount, 0.0f));

            m_Processor.RenderColorPyramid(
                hdCamera,
                cmd,
                sourceColorTexture,
                targetColorTexture,
                m_ColorPyramidMips,
                lodCount,
                scale
                );

            cmd.SetGlobalTexture(HDShaderIDs._ColorPyramidTexture, targetColorTexture);
        }

        public RTHandleSystem.RTHandle AllocColorRT(string id, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(
                size => CalculatePyramidSize(size),
                filterMode: FilterMode.Trilinear,
                colorFormat: RenderTextureFormat.ARGBHalf,
                sRGB: false,
                useMipMap: true,
                autoGenerateMips: false,
                enableRandomWrite: true,
                name: string.Format("ColorPyramid-{0}-{1}", id, frameIndex)
                );
        }

        public RTHandleSystem.RTHandle AllocDepthRT(string id, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(
                size => CalculatePyramidSize(size),
                filterMode: FilterMode.Trilinear,
                colorFormat: RenderTextureFormat.RGFloat,
                sRGB: false,
                useMipMap: true,
                autoGenerateMips: false,
                enableRandomWrite: true, // Need randomReadWrite because we downsample the first mip with a compute shader.
                name: string.Format("DepthPyramid-{0}-{1}", id, frameIndex)
                );
        }
    }
}
