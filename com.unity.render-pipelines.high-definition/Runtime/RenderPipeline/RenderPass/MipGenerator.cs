using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    using RTHandle = RTHandleSystem.RTHandle;

    public class MipGenerator
    {
        const int kKernelTex2D = 0;
        const int kKernelTex2DArray = 1;
        RTHandle[] m_TempColorTargets;

        ComputeShader m_DepthPyramidCS;
        ComputeShader m_ColorPyramidCS;
        Shader m_ColorPyramidPS;
        Material m_ColorPyramidPSMat;
        MaterialPropertyBlock m_PropertyBlock;

        int m_DepthDownsampleKernel;
        int[] m_ColorDownsampleKernel;
        int[] m_ColorDownsampleKernelCopyMip0;
        int[] m_ColorGaussianKernel;

        int[] m_SrcOffset;
        int[] m_DstOffset;

        public MipGenerator(HDRenderPipelineAsset asset)
        {
            m_TempColorTargets = new RTHandle[kernelCount];
            m_DepthPyramidCS = asset.renderPipelineResources.shaders.depthPyramidCS;
            m_ColorPyramidCS = asset.renderPipelineResources.shaders.colorPyramidCS;

            m_DepthDownsampleKernel = m_DepthPyramidCS.FindKernel("KDepthDownsample8DualUav");
            m_ColorDownsampleKernel = InitColorKernel("KColorDownsample");
            m_ColorDownsampleKernelCopyMip0 = InitColorKernel("KColorDownsampleCopyMip0");
            m_ColorGaussianKernel = InitColorKernel("KColorGaussian");

            m_SrcOffset = new int[4];
            m_DstOffset = new int[4];
            m_ColorPyramidPS = asset.renderPipelineResources.shaders.colorPyramidPS;
            m_ColorPyramidPSMat = CoreUtils.CreateEngineMaterial(m_ColorPyramidPS);
            m_PropertyBlock = new MaterialPropertyBlock();
        }

        public void Release()
        {
            for (int i = 0; i < kernelCount; ++i)
            {
                RTHandles.Release(m_TempColorTargets[i]);
                m_TempColorTargets[i] = null;
            }
        }

        private int kernelCount
        {
            get
            {
                if (TextureXR.useTexArray)
                    return 2;

                return 1;
            }
        }

        int[] InitColorKernel(string name)
        {
            int[] colorKernels = new int[kernelCount];
            colorKernels[kKernelTex2D] = m_ColorPyramidCS.FindKernel(name);

            if (TextureXR.useTexArray)
                colorKernels[kKernelTex2DArray] = m_ColorPyramidCS.FindKernel(name + "_Tex2DArray");

            return colorKernels;
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
            int kernelIndex = (source.dimension == TextureDimension.Tex2DArray) ? kKernelTex2DArray : kKernelTex2D;

            // Sanity check
            if (kernelIndex == kKernelTex2DArray)
            {
                Debug.Assert(source.dimension == destination.dimension, "MipGenerator source texture does not match dimension of destination!");
                Debug.Assert(m_ColorGaussianKernel.Length == kernelCount);
            }

            // Only create the temporary target on-demand in case the game doesn't actually need it
            if (m_TempColorTargets[kernelIndex] == null)
            {
                m_TempColorTargets[kernelIndex] = RTHandles.Alloc(
                    Vector2.one * 0.5f,
                    filterMode: FilterMode.Bilinear,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                    enableRandomWrite: true,
                    useMipMap: false,
                    enableMSAA: false,
                    xrInstancing: kernelIndex == kKernelTex2DArray,
                    useDynamicScale: true,
                    name: "Temp Gaussian Pyramid Target"
                );
            }

            #if UNITY_SWITCH
            bool preferFragment = true;
            #else
            bool preferFragment = false;
            #endif

            int srcMipLevel  = 0;
            int srcMipWidth  = size.x;
            int srcMipHeight = size.y;
            int slices = destination.volumeDepth;

            if (preferFragment)
            {
                Debug.Assert(!TextureXR.useTexArray, "Fragment version of mip generator is not compatible with texture array!");

                int tempTargetWidth = srcMipWidth >> 1;
                int tempTargetHeight = srcMipHeight >> 1;

                // Copies src mip0 to dst mip0
                m_PropertyBlock.SetTexture(HDShaderIDs._BlitTexture, source);
                m_PropertyBlock.SetVector(HDShaderIDs._BlitScaleBias, new Vector4(1f, 1f, 0f,0f));
                m_PropertyBlock.SetFloat(HDShaderIDs._BlitMipLevel, 0f);
                cmd.SetRenderTarget(destination, 0);
                cmd.DrawProcedural(Matrix4x4.identity, HDUtils.GetBlitMaterial(source.dimension), 0, MeshTopology.Triangles, 3, 1, m_PropertyBlock);

                // Note: smaller mips are excluded as we don't need them and the gaussian compute works
                // on 8x8 blocks
                // TODO: Could be further optimized by merging the smaller mips to reduce the amount of dispatches
                // Specifically, levels 2x2 and 1x1 (or their variations, depending on the aspect ratio) should not be used.
                while (srcMipWidth >= 8 || srcMipHeight >= 8)
                {
                    int dstMipWidth  = Mathf.Max(1, srcMipWidth  >> 1);
                    int dstMipHeight = Mathf.Max(1, srcMipHeight >> 1);

                    // Downsample.
                    // Note: this code is not valid on D3D11 because destination is used both as an input and target
                    m_PropertyBlock.SetTexture(HDShaderIDs._BlitTexture, destination);
                    m_PropertyBlock.SetVector(HDShaderIDs._BlitScaleBias, new Vector4(1f, 1f, 0f,0f));
                    m_PropertyBlock.SetFloat(HDShaderIDs._BlitMipLevel, srcMipLevel);
                    cmd.SetRenderTarget(destination, srcMipLevel + 1);
                    cmd.DrawProcedural(Matrix4x4.identity, HDUtils.GetBlitMaterial(source.dimension), 1, MeshTopology.Triangles, 3, 1, m_PropertyBlock);

                    // Blur horizontal.
                    m_PropertyBlock.SetTexture(HDShaderIDs._Source, destination);
                    m_PropertyBlock.SetVector(HDShaderIDs._SrcScaleBias, new Vector4(1f, 1f, 0f, 0f));
                    m_PropertyBlock.SetVector(HDShaderIDs._SrcUvLimits, new Vector4(1f, 1f, 1f / dstMipWidth, 0f));
                    m_PropertyBlock.SetFloat(HDShaderIDs._SourceMip, srcMipLevel + 1);
                    cmd.SetRenderTarget(m_TempColorTargets[kernelIndex], 0);
                    cmd.SetViewport(new Rect(0, 0, dstMipWidth, dstMipHeight));
                    cmd.DrawProcedural(Matrix4x4.identity, m_ColorPyramidPSMat, 0, MeshTopology.Triangles, 3, 1, m_PropertyBlock);

                    // Blur vertical.
                    m_PropertyBlock.SetTexture(HDShaderIDs._Source, m_TempColorTargets[kernelIndex]);
                    m_PropertyBlock.SetVector(HDShaderIDs._SrcScaleBias, new Vector4((float)dstMipWidth / tempTargetWidth, (float)dstMipHeight / tempTargetHeight, 0f, 0f));
                    m_PropertyBlock.SetVector(HDShaderIDs._SrcUvLimits, new Vector4((dstMipWidth - 0.5f) /  tempTargetWidth, (dstMipHeight - 0.5f) /  tempTargetHeight, 0f, 1f / tempTargetHeight));
                    m_PropertyBlock.SetFloat(HDShaderIDs._SourceMip, 0);
                    cmd.SetRenderTarget(destination, srcMipLevel + 1);
                    cmd.DrawProcedural(Matrix4x4.identity, m_ColorPyramidPSMat, 0, MeshTopology.Triangles, 3, 1, m_PropertyBlock);

                    srcMipLevel++;
                    srcMipWidth  = srcMipWidth  >> 1;
                    srcMipHeight = srcMipHeight >> 1;
                }
            }
            else
            {
                var cs = m_ColorPyramidCS;
                int downsampleKernel = m_ColorDownsampleKernel[kernelIndex];
                int downsampleKernelMip0 = m_ColorDownsampleKernelCopyMip0[kernelIndex];
                int gaussianKernel = m_ColorGaussianKernel[kernelIndex];

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
                        cmd.SetComputeTextureParam(cs, downsampleKernelMip0, HDShaderIDs._Destination, m_TempColorTargets[kernelIndex]);
                        cmd.DispatchCompute(cs, downsampleKernelMip0, (dstMipWidth + 7) / 8, (dstMipHeight + 7) / 8, slices);
                    }
                    else
                    {
                        cmd.SetComputeTextureParam(cs, downsampleKernel, HDShaderIDs._Source, destination, srcMipLevel);
                        cmd.SetComputeTextureParam(cs, downsampleKernel, HDShaderIDs._Destination, m_TempColorTargets[kernelIndex]);
                        cmd.DispatchCompute(cs, downsampleKernel, (dstMipWidth + 7) / 8, (dstMipHeight + 7) / 8, slices);
                    }

                    cmd.SetComputeVectorParam(cs, HDShaderIDs._Size, new Vector4(dstMipWidth, dstMipHeight, 0f, 0f));
                    cmd.SetComputeTextureParam(cs, gaussianKernel, HDShaderIDs._Source, m_TempColorTargets[kernelIndex]);
                    cmd.SetComputeTextureParam(cs, gaussianKernel, HDShaderIDs._Destination, destination, srcMipLevel + 1);
                    cmd.DispatchCompute(cs, gaussianKernel, (dstMipWidth + 7) / 8, (dstMipHeight + 7) / 8, slices);

                    srcMipLevel++;
                    srcMipWidth  = srcMipWidth  >> 1;
                    srcMipHeight = srcMipHeight >> 1;
                }
            }

            return srcMipLevel + 1;
        }
    }
}
