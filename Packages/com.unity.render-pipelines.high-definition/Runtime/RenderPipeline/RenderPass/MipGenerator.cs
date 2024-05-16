using UnityEditor;
using UnityEngine.Experimental.Rendering;
using static Unity.Mathematics.math;

namespace UnityEngine.Rendering.HighDefinition
{
    class MipGenerator
    {
        RTHandle[] m_TempColorTargets;
        RTHandle[] m_TempDownsamplePyramid;

        ComputeShader m_DepthPyramidCS;
        ComputeShader m_ColorPyramidCS;
        Shader m_ColorPyramidPS;
        Material m_ColorPyramidPSMat;
        MaterialPropertyBlock m_PropertyBlock;

        int m_DepthDownsampleKernel;
        int m_ColorDownsampleKernel;
        int m_ColorGaussianKernel;

        public MipGenerator(HDRenderPipeline renderPipeline)
        {
            m_TempColorTargets = new RTHandle[xrMaxSliceCount];
            m_TempDownsamplePyramid = new RTHandle[xrMaxSliceCount];
            m_DepthPyramidCS = renderPipeline.runtimeShaders.depthPyramidCS;
            m_ColorPyramidCS = renderPipeline.runtimeShaders.colorPyramidCS;

            m_DepthDownsampleKernel = m_DepthPyramidCS.FindKernel("KDepthDownsample8DualUav");
            m_ColorDownsampleKernel = m_ColorPyramidCS.FindKernel("KColorDownsample");
            m_ColorGaussianKernel = m_ColorPyramidCS.FindKernel("KColorGaussian");

            m_ColorPyramidPS = renderPipeline.runtimeShaders.colorPyramidPS;
            m_ColorPyramidPSMat = CoreUtils.CreateEngineMaterial(m_ColorPyramidPS);
            m_PropertyBlock = new MaterialPropertyBlock();
        }

        public void Release()
        {
            for (int i = 0; i < xrMaxSliceCount; ++i)
            {
                RTHandles.Release(m_TempColorTargets[i]);
                m_TempColorTargets[i] = null;
                RTHandles.Release(m_TempDownsamplePyramid[i]);
                m_TempDownsamplePyramid[i] = null;
            }

            CoreUtils.Destroy(m_ColorPyramidPSMat);
        }

        int xrMaxSliceCount
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

            var cs = m_DepthPyramidCS;
            int kernel = m_DepthDownsampleKernel;

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._DepthMipChain, texture);

            // Note: Gather() doesn't take a LOD parameter and we cannot bind an SRV of a MIP level,
            // and we don't support Min samplers either. So we are forced to perform 4x loads.
            for (int dstIndex0 = 1; dstIndex0 < info.mipLevelCount;)
            {
                int minCount = Mathf.Min(info.mipLevelCount - dstIndex0, 4);
                int cbCount = 0;
                if (dstIndex0 < info.mipLevelCountCheckerboard)
                { 
                    cbCount = info.mipLevelCountCheckerboard - dstIndex0;
                    Debug.Assert(dstIndex0 == 1, "expected to make checkerboard mips on the first pass");
                    Debug.Assert(cbCount <= minCount, "expected fewer checkerboard mips than min mips");
                    Debug.Assert(cbCount <= 2, "expected 2 or fewer checkerboard mips for now");
                }

                Vector2Int srcOffset = info.mipLevelOffsets[dstIndex0 - 1];
                Vector2Int srcSize = info.mipLevelSizes[dstIndex0 - 1];
                int dstIndex1 = Mathf.Min(dstIndex0 + 1, info.mipLevelCount - 1);
                int dstIndex2 = Mathf.Min(dstIndex0 + 2, info.mipLevelCount - 1);
                int dstIndex3 = Mathf.Min(dstIndex0 + 3, info.mipLevelCount - 1);

                DepthPyramidConstants cb = new DepthPyramidConstants
                {
                    _MinDstCount = (uint)minCount,
                    _CbDstCount = (uint)cbCount,
                    _SrcOffset = srcOffset,
                    _SrcLimit = srcSize - Vector2Int.one,
                    _DstSize0 = info.mipLevelSizes[dstIndex0],
                    _DstSize1 = info.mipLevelSizes[dstIndex1],
                    _DstSize2 = info.mipLevelSizes[dstIndex2],
                    _DstSize3 = info.mipLevelSizes[dstIndex3],
                    _MinDstOffset0 = info.mipLevelOffsets[dstIndex0],
                    _MinDstOffset1 = info.mipLevelOffsets[dstIndex1],
                    _MinDstOffset2 = info.mipLevelOffsets[dstIndex2],
                    _MinDstOffset3 = info.mipLevelOffsets[dstIndex3],
                    _CbDstOffset0 = info.mipLevelOffsetsCheckerboard[dstIndex0],
                    _CbDstOffset1 = info.mipLevelOffsetsCheckerboard[dstIndex1],
                };
                ConstantBuffer.Push(cmd, cb, cs, HDShaderIDs._DepthPyramidConstants);

                CoreUtils.SetKeyword(cmd, cs, "ENABLE_CHECKERBOARD", cbCount != 0);

                Vector2Int dstSize = info.mipLevelSizes[dstIndex0];
                cmd.DispatchCompute(cs, kernel, HDUtils.DivRoundUp(dstSize.x, 8), HDUtils.DivRoundUp(dstSize.y, 8), texture.volumeDepth);

                dstIndex0 += minCount;
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

            int srcMipLevel = 0;
            int srcMipWidth = size.x;
            int srcMipHeight = size.y;
            int slices = destination.volumeDepth;

            // For the fragment version, we need another buffer for the 2 pass blur.
            if (HDRenderPipeline.k_PreferFragment)
            {
                // Check if format has changed since last time we generated mips
                if (m_TempColorTargets[rtIndex] != null && m_TempColorTargets[rtIndex].rt.graphicsFormat != destination.graphicsFormat)
                {
                    RTHandles.Release(m_TempColorTargets[rtIndex]);
                    m_TempColorTargets[rtIndex] = null;
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
                        useDynamicScale: true,
                        name: "Temp Gaussian Pyramid Target"
                    );
                }
            }

            // Check if format has changed since last time we generated mips
            if (m_TempDownsamplePyramid[rtIndex] != null && m_TempDownsamplePyramid[rtIndex].rt.graphicsFormat != destination.graphicsFormat)
            {
                RTHandles.Release(m_TempDownsamplePyramid[rtIndex]);
                m_TempDownsamplePyramid[rtIndex] = null;
            }

            if (m_TempDownsamplePyramid[rtIndex] == null)
            {
                m_TempDownsamplePyramid[rtIndex] = RTHandles.Alloc(
                    Vector2.one * 0.5f,
                    sourceIsArray ? TextureXR.slices : 1,
                    dimension: source.dimension,
                    filterMode: FilterMode.Bilinear,
                    colorFormat: destination.graphicsFormat,
                    enableRandomWrite: true,
                    useMipMap: false,
                    useDynamicScale: true,
                    name: "Temporary Downsampled Pyramid"
                );

                cmd.SetRenderTarget(m_TempDownsamplePyramid[rtIndex]);
                cmd.ClearRenderTarget(false, true, Color.black);
            }

            bool isHardwareDrsOn = DynamicResolutionHandler.instance.HardwareDynamicResIsEnabled();
            var hardwareTextureSize = new Vector2Int(source.width, source.height);
            if (isHardwareDrsOn)
                hardwareTextureSize = DynamicResolutionHandler.instance.ApplyScalesOnSize(hardwareTextureSize);

            float sourceScaleX = (float)size.x / (float)hardwareTextureSize.x;
            float sourceScaleY = (float)size.y / (float)hardwareTextureSize.y;

            // Copies src mip0 to dst mip0
            // Note that we still use a fragment shader to do the first copy because fragment are faster at copying
            // data types like R11G11B10 (default) and pretty similar in term of speed with R16G16B16A16.
            m_PropertyBlock.SetTexture(HDShaderIDs._BlitTexture, source);
            m_PropertyBlock.SetVector(HDShaderIDs._BlitScaleBias, new Vector4(sourceScaleX, sourceScaleY, 0f, 0f));
            m_PropertyBlock.SetFloat(HDShaderIDs._BlitMipLevel, 0f);
            cmd.SetRenderTarget(destination, 0, CubemapFace.Unknown, -1);
            cmd.SetViewport(new Rect(0, 0, srcMipWidth, srcMipHeight));
            cmd.DrawProcedural(Matrix4x4.identity, HDUtils.GetBlitMaterial(source.dimension), 0, MeshTopology.Triangles, 3, 1, m_PropertyBlock);

            var finalTargetSize = new Vector2Int(destination.width, destination.height);
            if (destination.useDynamicScale && isHardwareDrsOn)
                finalTargetSize = DynamicResolutionHandler.instance.ApplyScalesOnSize(finalTargetSize);

            // Note: smaller mips are excluded as we don't need them and the gaussian compute works
            // on 8x8 blocks
            while (srcMipWidth >= 8 || srcMipHeight >= 8)
            {
                int dstMipWidth = Mathf.Max(1, srcMipWidth >> 1);
                int dstMipHeight = Mathf.Max(1, srcMipHeight >> 1);

                // Scale for downsample
                float scaleX = ((float)srcMipWidth / finalTargetSize.x);
                float scaleY = ((float)srcMipHeight / finalTargetSize.y);

                cmd.SetComputeVectorParam(m_ColorPyramidCS, HDShaderIDs._Size,
                    new Vector4(srcMipWidth, srcMipHeight, 0f, 0f));

                if (HDRenderPipeline.k_PreferFragment)
                {
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
                    // Same size as m_TempColorTargets which is the source for vertical blur
                    var hardwareBlurSourceTextureSize = new Vector2Int(m_TempDownsamplePyramid[rtIndex].rt.width, m_TempDownsamplePyramid[rtIndex].rt.height);
                    if (isHardwareDrsOn)
                        hardwareBlurSourceTextureSize = DynamicResolutionHandler.instance.ApplyScalesOnSize(hardwareBlurSourceTextureSize);

                    float blurSourceTextureWidth = (float)hardwareBlurSourceTextureSize.x;
                    float blurSourceTextureHeight = (float)hardwareBlurSourceTextureSize.y;

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
                }
                else
                {
                    // Downsample.
                    cmd.SetComputeTextureParam(m_ColorPyramidCS, m_ColorDownsampleKernel, HDShaderIDs._Source,
                        destination, srcMipLevel);
                    cmd.SetComputeTextureParam(m_ColorPyramidCS, m_ColorDownsampleKernel, HDShaderIDs._Destination,
                        m_TempDownsamplePyramid[rtIndex]);
                    cmd.DispatchCompute(m_ColorPyramidCS, m_ColorDownsampleKernel, (dstMipWidth + 7) / 8,
                        (dstMipHeight + 7) / 8, TextureXR.slices);

                    // Single pass blur
                    cmd.SetComputeVectorParam(m_ColorPyramidCS, HDShaderIDs._Size,
                        new Vector4(dstMipWidth, dstMipHeight, 0f, 0f));
                    cmd.SetComputeTextureParam(m_ColorPyramidCS, m_ColorGaussianKernel, HDShaderIDs._Source,
                        m_TempDownsamplePyramid[rtIndex]);
                    cmd.SetComputeTextureParam(m_ColorPyramidCS, m_ColorGaussianKernel, HDShaderIDs._Destination,
                        destination, srcMipLevel + 1);
                    cmd.DispatchCompute(m_ColorPyramidCS, m_ColorGaussianKernel, (dstMipWidth + 7) / 8,
                        (dstMipHeight + 7) / 8, TextureXR.slices);
                }

                srcMipLevel++;
                srcMipWidth >>= 1;
                srcMipHeight >>= 1;

                finalTargetSize.x >>= 1;
                finalTargetSize.y >>= 1;
            }

            return srcMipLevel + 1;
        }
    }
}
