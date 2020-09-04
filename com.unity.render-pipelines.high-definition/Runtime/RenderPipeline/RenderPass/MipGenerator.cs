namespace UnityEngine.Rendering.HighDefinition
{
    class MipGenerator
    {
        RTHandle[] m_TempColorTargets;
        RTHandle[] m_TempDownsamplePyramid;

        ComputeShader m_DepthPyramidCS;
        Shader m_ColorPyramidPS;
        Material m_ColorPyramidPSMat;
        MaterialPropertyBlock m_PropertyBlock;

        int m_DepthDownsampleKernel;

        int[] m_SrcOffset;
        int[] m_DstOffset;

        public MipGenerator(RenderPipelineResources defaultResources)
        {
            m_TempColorTargets = new RTHandle[tmpTargetCount];
            m_TempDownsamplePyramid = new RTHandle[tmpTargetCount];
            m_DepthPyramidCS = defaultResources.shaders.depthPyramidCS;

            m_DepthDownsampleKernel = m_DepthPyramidCS.FindKernel("KDepthDownsample8DualUav");

            m_SrcOffset = new int[4];
            m_DstOffset = new int[4];
            m_ColorPyramidPS = defaultResources.shaders.colorPyramidPS;
            m_ColorPyramidPSMat = CoreUtils.CreateEngineMaterial(m_ColorPyramidPS);
            m_PropertyBlock = new MaterialPropertyBlock();
        }

        public void Release()
        {
            for (int i = 0; i < tmpTargetCount; ++i)
            {
                RTHandles.Release(m_TempColorTargets[i]);
                m_TempColorTargets[i] = null;
                RTHandles.Release(m_TempDownsamplePyramid[i]);
                m_TempDownsamplePyramid[i] = null;
            }

            CoreUtils.Destroy(m_ColorPyramidPSMat);
        }

        private int tmpTargetCount
        {
            get
            {
                if (TextureXR.useTexArray)
                    return 2;

                return 1;
            }
        }

        // Generates an in-place depth pyramid
        // TODO: Mip-mapping depth is problematic for precision at lower mips, generate a packed atlas instead
        public void RenderMinDepthPyramid(CommandBuffer cmd, RenderTexture texture, HDUtils.PackedMipChainInfo info)
        {
            HDUtils.CheckRTCreated(texture);

            var cs     = m_DepthPyramidCS;
            int kernel = m_DepthDownsampleKernel;

            // TODO: Do it 1x MIP at a time for now. In the future, do 4x MIPs per pass, or even use a single pass.
            // Note: Gather() doesn't take a LOD parameter and we cannot bind an SRV of a MIP level,
            // and we don't support Min samplers either. So we are forced to perform 4x loads.
            for (int i = 1; i < info.mipLevelCount; i++)
            {
                Vector2Int dstSize   = info.mipLevelSizes[i];
                Vector2Int dstOffset = info.mipLevelOffsets[i];
                Vector2Int srcSize   = info.mipLevelSizes[i - 1];
                Vector2Int srcOffset = info.mipLevelOffsets[i - 1];
                Vector2Int srcLimit  = srcOffset + srcSize - Vector2Int.one;

                m_SrcOffset[0] = srcOffset.x;
                m_SrcOffset[1] = srcOffset.y;
                m_SrcOffset[2] = srcLimit.x;
                m_SrcOffset[3] = srcLimit.y;

                m_DstOffset[0] = dstOffset.x;
                m_DstOffset[1] = dstOffset.y;
                m_DstOffset[2] = 0;
                m_DstOffset[3] = 0;

                cmd.SetComputeIntParams(   cs,         HDShaderIDs._SrcOffsetAndLimit, m_SrcOffset);
                cmd.SetComputeIntParams(   cs,         HDShaderIDs._DstOffset,         m_DstOffset);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._DepthMipChain,     texture);

                cmd.DispatchCompute(cs, kernel, HDUtils.DivRoundUp(dstSize.x, 8), HDUtils.DivRoundUp(dstSize.y, 8), texture.volumeDepth);
            }
        }

        // Generates the gaussian pyramid of source into destination
        // We can't do it in place as the color pyramid has to be read while writing to the color
        // buffer in some cases (e.g. refraction, distortion)
        // Returns the number of mips
        public int RenderColorGaussianPyramid(CommandBuffer cmd, Vector2Int size, Texture source, RenderTexture destination)
        {
            // Select between Tex2D and Tex2DArray versions of the kernels
            bool sourceIsArray = (source.dimension == TextureDimension.Tex2DArray);
            int rtIndex = sourceIsArray ? 1 : 0;
            // Sanity check
            if (sourceIsArray)
            {
                Debug.Assert(source.dimension == destination.dimension, "MipGenerator source texture does not match dimension of destination!");
            }

            // Only create the temporary target on-demand in case the game doesn't actually need it
            if (m_TempColorTargets[rtIndex] == null)
            {
                m_TempColorTargets[rtIndex] = RTHandles.Alloc(
                    Vector2.one * 0.5f,
                    sourceIsArray ? TextureXR.slices : 1,
                    dimension: source.dimension,
                    filterMode: FilterMode.Bilinear,
                    colorFormat: destination.graphicsFormat,
                    enableRandomWrite: true,
                    useMipMap: false,
                    enableMSAA: false,
                    useDynamicScale: true,
                    name: "Temp Gaussian Pyramid Target"
                );
            }

            int srcMipLevel  = 0;
            int srcMipWidth  = size.x;
            int srcMipHeight = size.y;
            int slices = destination.volumeDepth;

            int tempTargetWidth = srcMipWidth >> 1;
            int tempTargetHeight = srcMipHeight >> 1;

            if (m_TempDownsamplePyramid[rtIndex] == null)
            {
                m_TempDownsamplePyramid[rtIndex] = RTHandles.Alloc(
                Vector2.one * 0.5f,
                sourceIsArray ? TextureXR.slices : 1,
                dimension: source.dimension,
                filterMode: FilterMode.Bilinear,
                colorFormat: destination.graphicsFormat,
                enableRandomWrite: false,
                useMipMap: false,
                enableMSAA: false,
                useDynamicScale: true,
                name: "Temporary Downsampled Pyramid"
                );

                cmd.SetRenderTarget(m_TempDownsamplePyramid[rtIndex]);
                cmd.ClearRenderTarget(false, true, Color.black);
            }

            float sourceScaleX = (float)size.x / source.width;
            float sourceScaleY = (float)size.y / source.height;

            // Copies src mip0 to dst mip0
            m_PropertyBlock.SetTexture(HDShaderIDs._BlitTexture, source);
            m_PropertyBlock.SetVector(HDShaderIDs._BlitScaleBias, new Vector4(sourceScaleX, sourceScaleY, 0f, 0f));
            m_PropertyBlock.SetFloat(HDShaderIDs._BlitMipLevel, 0f);
            cmd.SetRenderTarget(destination, 0, CubemapFace.Unknown, -1);
            cmd.SetViewport(new Rect(0, 0, srcMipWidth, srcMipHeight));
            cmd.DrawProcedural(Matrix4x4.identity, HDUtils.GetBlitMaterial(source.dimension), 0, MeshTopology.Triangles, 3, 1, m_PropertyBlock);

            int finalTargetMipWidth = destination.width;
            int finalTargetMipHeight = destination.height;

            // Note: smaller mips are excluded as we don't need them and the gaussian compute works
            // on 8x8 blocks
            while (srcMipWidth >= 8 || srcMipHeight >= 8)
            {
                int dstMipWidth  = Mathf.Max(1, srcMipWidth  >> 1);
                int dstMipHeight = Mathf.Max(1, srcMipHeight >> 1);

                // Scale for downsample
                float scaleX = ((float)srcMipWidth / finalTargetMipWidth);
                float scaleY = ((float)srcMipHeight / finalTargetMipHeight);

                // Downsample.
                m_PropertyBlock.SetTexture(HDShaderIDs._BlitTexture, destination);
                m_PropertyBlock.SetVector(HDShaderIDs._BlitScaleBias, new Vector4(scaleX, scaleY, 0f, 0f));
                m_PropertyBlock.SetFloat(HDShaderIDs._BlitMipLevel, srcMipLevel);
                cmd.SetRenderTarget(m_TempDownsamplePyramid[rtIndex], 0, CubemapFace.Unknown, -1);
                cmd.SetViewport(new Rect(0, 0, dstMipWidth, dstMipHeight));
                cmd.DrawProcedural(Matrix4x4.identity, HDUtils.GetBlitMaterial(source.dimension), 1, MeshTopology.Triangles, 3, 1, m_PropertyBlock);

                // In this mip generation process, source viewport can be smaller than the source render target itself because of the RTHandle system
                // We are not using the scale provided by the RTHandle system for two reasons:
                // - Source might be a planar probe which will not be scaled by the system (since it's actually the final target of probe rendering at the exact size)
                // - When computing mip size, depending on even/odd sizes, the scale computed for mip 0 might miss a texel at the border.
                //   This can result in a shift in the mip map downscale that depends on the render target size rather than the actual viewport
                //   (Two rendering at the same viewport size but with different RTHandle reference size would yield different results which can break automated testing)
                // So in the end we compute a specific scale for downscale and blur passes at each mip level.

                // Scales for Blur
                float blurSourceTextureWidth = (float)m_TempDownsamplePyramid[rtIndex].rt.width; // Same size as m_TempColorTargets which is the source for vertical blur
                float blurSourceTextureHeight = (float)m_TempDownsamplePyramid[rtIndex].rt.height;
                scaleX = ((float)dstMipWidth / blurSourceTextureWidth);
                scaleY = ((float)dstMipHeight / blurSourceTextureHeight);

                // Blur horizontal.
                m_PropertyBlock.SetTexture(HDShaderIDs._Source, m_TempDownsamplePyramid[rtIndex]);
                m_PropertyBlock.SetVector(HDShaderIDs._SrcScaleBias, new Vector4(scaleX, scaleY, 0f, 0f));
                m_PropertyBlock.SetVector(HDShaderIDs._SrcUvLimits, new Vector4((dstMipWidth - 0.5f) / blurSourceTextureWidth, (dstMipHeight - 0.5f) / blurSourceTextureHeight, 1.0f / blurSourceTextureWidth, 0f));
                m_PropertyBlock.SetFloat(HDShaderIDs._SourceMip, 0);
                cmd.SetRenderTarget(m_TempColorTargets[rtIndex], 0, CubemapFace.Unknown, -1);
                cmd.SetViewport(new Rect(0, 0, dstMipWidth, dstMipHeight));
                cmd.DrawProcedural(Matrix4x4.identity, m_ColorPyramidPSMat, rtIndex, MeshTopology.Triangles, 3, 1, m_PropertyBlock);

                // Blur vertical.
                m_PropertyBlock.SetTexture(HDShaderIDs._Source, m_TempColorTargets[rtIndex]);
                m_PropertyBlock.SetVector(HDShaderIDs._SrcScaleBias, new Vector4(scaleX, scaleY, 0f, 0f));
                m_PropertyBlock.SetVector(HDShaderIDs._SrcUvLimits, new Vector4((dstMipWidth - 0.5f) / blurSourceTextureWidth, (dstMipHeight - 0.5f) / blurSourceTextureHeight, 0f, 1.0f / blurSourceTextureHeight));
                m_PropertyBlock.SetFloat(HDShaderIDs._SourceMip, 0);
                cmd.SetRenderTarget(destination, srcMipLevel + 1, CubemapFace.Unknown, -1);
                cmd.SetViewport(new Rect(0, 0, dstMipWidth, dstMipHeight));
                cmd.DrawProcedural(Matrix4x4.identity, m_ColorPyramidPSMat, rtIndex, MeshTopology.Triangles, 3, 1, m_PropertyBlock);

                srcMipLevel++;
                srcMipWidth  = srcMipWidth  >> 1;
                srcMipHeight = srcMipHeight >> 1;

                finalTargetMipWidth = finalTargetMipWidth >> 1;
                finalTargetMipHeight = finalTargetMipHeight >> 1;
            }

            return srcMipLevel + 1;
        }
    }
}
