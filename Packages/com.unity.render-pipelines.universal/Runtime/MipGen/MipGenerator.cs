using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering
{
    internal struct PackedMipChainInfo
    {
        public Vector2Int textureSize;
        public int mipLevelCount; // mips contain min (closest) depth
        public int mipLevelCountCheckerboard;
        public Vector2Int[] mipLevelSizes;
        public Vector2Int[] mipLevelOffsets; // mips contain min (closest) depth
        public Vector2Int[] mipLevelOffsetsCheckerboard;

        private Vector2 cachedTextureScale;
        private Vector2Int cachedHardwareTextureSize;
        private int cachedCheckerboardMipCount;

        public void Allocate()
        {
            mipLevelOffsets = new Vector2Int[15];
            mipLevelOffsetsCheckerboard = new Vector2Int[15];
            mipLevelSizes = new Vector2Int[15];
        }

        enum PackDirection
        {
            Right,
            Down,
        }

        static Vector2Int NextMipBegin(Vector2Int prevMipBegin, Vector2Int prevMipSize, PackDirection dir)
        {
            Vector2Int mipBegin = prevMipBegin;
            if (dir == PackDirection.Right)
                mipBegin.x += prevMipSize.x;
            else
                mipBegin.y += prevMipSize.y;
            return mipBegin;
        }

        // We pack all MIP levels into the top MIP level to avoid the Pow2 MIP chain restriction.
        // We compute the required size iteratively.
        // This function is NOT fast, but it is illustrative, and can be optimized later.
        public void ComputePackedMipChainInfo(Vector2Int viewportSize, int checkerboardMipCount)
        {
            // only support up to 2 mips of checkerboard data being created
            checkerboardMipCount = Mathf.Clamp(checkerboardMipCount, 0, 2);

            bool isHardwareDrsOn = DynamicResolutionHandler.instance.HardwareDynamicResIsEnabled();
            Vector2Int hardwareTextureSize = isHardwareDrsOn ? DynamicResolutionHandler.instance.ApplyScalesOnSize(viewportSize) : viewportSize;
            Vector2 textureScale = isHardwareDrsOn ? new Vector2((float)viewportSize.x / (float)hardwareTextureSize.x, (float)viewportSize.y / (float)hardwareTextureSize.y) : new Vector2(1.0f, 1.0f);

            // No work needed.
            if (cachedHardwareTextureSize == hardwareTextureSize && cachedTextureScale == textureScale && cachedCheckerboardMipCount == checkerboardMipCount)
                return;

            cachedHardwareTextureSize = hardwareTextureSize;
            cachedTextureScale = textureScale;
            cachedCheckerboardMipCount = checkerboardMipCount;

            mipLevelSizes[0] = hardwareTextureSize;
            mipLevelOffsets[0] = Vector2Int.zero;
            mipLevelOffsetsCheckerboard[0] = mipLevelOffsets[0];

            int mipLevel = 0;
            Vector2Int mipSize = hardwareTextureSize;
            bool hasCheckerboard = (checkerboardMipCount != 0);
            do
            {
                mipLevel++;

                // Round up.
                mipSize.x = Mathf.Max(1, (mipSize.x + 1) >> 1);
                mipSize.y = Mathf.Max(1, (mipSize.y + 1) >> 1);

                mipLevelSizes[mipLevel] = mipSize;

                Vector2Int prevMipSize = mipLevelSizes[mipLevel - 1];
                Vector2Int prevMipBegin = mipLevelOffsets[mipLevel - 1];
                Vector2Int prevMipBeginCheckerboard = mipLevelOffsetsCheckerboard[mipLevel - 1];

                Vector2Int mipBegin = prevMipBegin;
                Vector2Int mipBeginCheckerboard = prevMipBeginCheckerboard;
                if (mipLevel == 1)
                {
                    // first mip always below full resolution
                    mipBegin = NextMipBegin(prevMipBegin, prevMipSize, PackDirection.Down);

                    // pack checkerboard next to it if present
                    if (hasCheckerboard)
                        mipBeginCheckerboard = NextMipBegin(mipBegin, mipSize, PackDirection.Right);
                    else
                        mipBeginCheckerboard = mipBegin;
                }
                else
                {
                    // alternate directions, mip 2 starts with down if checkerboard, right if not
                    bool isOdd = ((mipLevel & 1) != 0);
                    PackDirection dir = (isOdd ^ hasCheckerboard) ? PackDirection.Down : PackDirection.Right;

                    mipBegin = NextMipBegin(prevMipBegin, prevMipSize, dir);
                    mipBeginCheckerboard = NextMipBegin(prevMipBeginCheckerboard, prevMipSize, dir);
                }

                mipLevelOffsets[mipLevel] = mipBegin;
                mipLevelOffsetsCheckerboard[mipLevel] = mipBeginCheckerboard;

                hardwareTextureSize.x = Mathf.Max(hardwareTextureSize.x, mipBegin.x + mipSize.x);
                hardwareTextureSize.y = Mathf.Max(hardwareTextureSize.y, mipBegin.y + mipSize.y);
                hardwareTextureSize.x = Mathf.Max(hardwareTextureSize.x, mipBeginCheckerboard.x + mipSize.x);
                hardwareTextureSize.y = Mathf.Max(hardwareTextureSize.y, mipBeginCheckerboard.y + mipSize.y);
            }
            while ((mipSize.x > 1) || (mipSize.y > 1));

            textureSize = new Vector2Int(
                (int)Mathf.Ceil((float)hardwareTextureSize.x * textureScale.x), (int)Mathf.Ceil((float)hardwareTextureSize.y * textureScale.y));

            mipLevelCount = mipLevel + 1;
            mipLevelCountCheckerboard = hasCheckerboard ? (1 + checkerboardMipCount) : 0;
        }
    }

    internal class MipGenerator
    {
        ComputeShader m_ColorPyramidCS = null;
        ComputeShader m_DepthPyramidCS = null;
        Shader m_ColorPyramidPS;
        Material m_ColorPyramidPSMat;
        MaterialPropertyBlock m_PropertyBlockBlur;

        int m_DepthDownsampleKernel = -1;
        int m_ColorDownsampleKernel = -1;
        int m_ColorGaussianKernel = -1;

        bool m_PreferCompute;
        bool m_SupportCompute;

        LocalKeyword m_DisableTexture2DArrayColorKeyword;
        LocalKeyword m_TargetUnormColorKeyword;
        LocalKeyword m_TargetHalfColorKeyword;
        LocalKeyword m_TargetFloatColorKeyword;
        LocalKeyword m_DisableTexture2DArrayColorPSKeyword;
        LocalKeyword m_DisableTexture2DArrayDepthKeyword;
        LocalKeyword m_EnableCheckerboardKeyword;

        public static readonly int _DepthMipChain = Shader.PropertyToID("_DepthMipChain");
        public static readonly int _DepthPyramidConstants = Shader.PropertyToID("DepthPyramidConstants");

        public static readonly int _Size = Shader.PropertyToID("_Size");
        public static readonly int _Source = Shader.PropertyToID("_Source");
        public static readonly int _Destination = Shader.PropertyToID("_Destination");

        public static readonly int _SourceMip = Shader.PropertyToID("_SourceMip");
        public static readonly int _SrcScaleBias = Shader.PropertyToID("_SrcScaleBias");
        public static readonly int _SrcUvLimits = Shader.PropertyToID("_SrcUvLimits");

        public static readonly string k_EnableCheckerboard = "ENABLE_CHECKERBOARD";

        public const int k_MinimumResolutionGaussian = 8;

        public MipGenerator(bool preferCompute = true)
        {
            if (!GraphicsSettings.TryGetRenderPipelineSettings<MipGenRenderPipelineRuntimeResources>(out var runtimeShaders))
            {
                Debug.LogErrorFormat(
                    $"Couldn't find the required resources for the {nameof(MipGenerator)} .");
            }

            m_SupportCompute = SystemInfo.supportsComputeShaders
                && runtimeShaders.colorPyramidCS.HasKernel("ColorDownsample")
                && runtimeShaders.colorPyramidCS.HasKernel("ColorGaussian")
                && runtimeShaders.depthPyramidCS.HasKernel("DepthDownsample");
            if (m_SupportCompute)
            {
                m_ColorPyramidCS = runtimeShaders.colorPyramidCS;
                m_DepthPyramidCS = runtimeShaders.depthPyramidCS;

                m_ColorDownsampleKernel = m_ColorPyramidCS.FindKernel("ColorDownsample");
                m_ColorGaussianKernel = m_ColorPyramidCS.FindKernel("ColorGaussian");
                m_DisableTexture2DArrayColorKeyword = new LocalKeyword(m_ColorPyramidCS, ShaderKeywordStrings.DisableTexture2DXArray);
                m_TargetUnormColorKeyword = new LocalKeyword(m_ColorPyramidCS, "TARGET_UNORM");
                m_TargetHalfColorKeyword = new LocalKeyword(m_ColorPyramidCS, "TARGET_HALF");
                m_TargetFloatColorKeyword = new LocalKeyword(m_ColorPyramidCS, "TARGET_FLOAT");

                m_DepthDownsampleKernel = m_DepthPyramidCS.FindKernel("DepthDownsample");
                m_DisableTexture2DArrayDepthKeyword = new LocalKeyword(m_DepthPyramidCS, ShaderKeywordStrings.DisableTexture2DXArray);
                m_EnableCheckerboardKeyword = new LocalKeyword(m_DepthPyramidCS, k_EnableCheckerboard);
            }

            m_ColorPyramidPS = runtimeShaders.colorPyramidPS;
            m_DisableTexture2DArrayColorPSKeyword = new LocalKeyword(m_ColorPyramidPS, ShaderKeywordStrings.DisableTexture2DXArray);
            m_ColorPyramidPSMat = CoreUtils.CreateEngineMaterial(m_ColorPyramidPS);
            m_PropertyBlockBlur = new MaterialPropertyBlock();

            m_PreferCompute = preferCompute;
        }

        public void Release()
        {
            CoreUtils.Destroy(m_ColorPyramidPSMat);
        }

        class DepthPyramidPassData
        {
            public PackedMipChainInfo info;
            public ComputeShader cs;
            public int kernel;
            public TextureHandle depthTexture;
            public LocalKeyword disableTexture2DArrayKeyword;
            public LocalKeyword enableCheckerboardKeyword;
        }

        // Generates an in-place depth pyramid Compute only
        // TODO: Mip-mapping depth is problematic for precision at lower mips, generate a packed atlas instead
        public void RenderMinDepthPyramid(RenderGraph renderGraph, TextureHandle depthTexture, ref PackedMipChainInfo depthBufferMipChainInfo)
        {
            if (!m_SupportCompute)
            {
                Debug.LogError("MipGenerator: Can't render depth pyramid, platform doesn't support compute shaders.");
                return;
            }

            using (var builder = renderGraph.AddComputePass<DepthPyramidPassData>("Generate Depth Buffer MIP Chain", out var passData))
            {
                passData.info = depthBufferMipChainInfo;
                passData.cs = m_DepthPyramidCS;
                passData.kernel = m_DepthDownsampleKernel;
                passData.depthTexture = depthTexture;
                passData.disableTexture2DArrayKeyword = m_DisableTexture2DArrayDepthKeyword;
                passData.enableCheckerboardKeyword = m_EnableCheckerboardKeyword;

                // Enable global state modification so we can set keywords.
                builder.AllowGlobalStateModification(true);
                builder.UseTexture(passData.depthTexture, AccessFlags.ReadWrite);
                builder.SetRenderFunc<DepthPyramidPassData>(static (data, context) =>
                {
                    var cmd = context.cmd;
                    var sourceIsArray = (((RenderTexture)data.depthTexture).dimension == TextureDimension.Tex2DArray);

                    cmd.SetKeyword(data.cs, data.disableTexture2DArrayKeyword, !sourceIsArray);

                    // Note: Gather() doesn't take a LOD parameter and we cannot bind an SRV of a MIP level,
                    // and we don't support Min samplers either. So we are forced to perform 4x loads.
                    for (int dstIndex0 = 1; dstIndex0 < data.info.mipLevelCount;)
                    {
                        int minCount = Mathf.Min(data.info.mipLevelCount - dstIndex0, 4);
                        int cbCount = 0;
                        if (dstIndex0 < data.info.mipLevelCountCheckerboard)
                        {
                            cbCount = data.info.mipLevelCountCheckerboard - dstIndex0;
                            Debug.Assert(dstIndex0 == 1, "expected to make checkerboard mips on the first pass");
                            Debug.Assert(cbCount <= minCount, "expected fewer checkerboard mips than min mips");
                            Debug.Assert(cbCount <= 2, "expected 2 or fewer checkerboard mips for now");
                        }

                        Vector2Int srcOffset = data.info.mipLevelOffsets[dstIndex0 - 1];
                        Vector2Int srcSize = data.info.mipLevelSizes[dstIndex0 - 1];
                        int dstIndex1 = Mathf.Min(dstIndex0 + 1, data.info.mipLevelCount - 1);
                        int dstIndex2 = Mathf.Min(dstIndex0 + 2, data.info.mipLevelCount - 1);
                        int dstIndex3 = Mathf.Min(dstIndex0 + 3, data.info.mipLevelCount - 1);

                        DepthPyramidConstants cb = new DepthPyramidConstants
                        {
                            _MinDstCount = (uint)minCount,
                            _CbDstCount = (uint)cbCount,
                            _SrcOffset = srcOffset,
                            _SrcLimit = srcSize - Vector2Int.one,
                            _DstSize0 = data.info.mipLevelSizes[dstIndex0],
                            _DstSize1 = data.info.mipLevelSizes[dstIndex1],
                            _DstSize2 = data.info.mipLevelSizes[dstIndex2],
                            _DstSize3 = data.info.mipLevelSizes[dstIndex3],
                            _MinDstOffset0 = data.info.mipLevelOffsets[dstIndex0],
                            _MinDstOffset1 = data.info.mipLevelOffsets[dstIndex1],
                            _MinDstOffset2 = data.info.mipLevelOffsets[dstIndex2],
                            _MinDstOffset3 = data.info.mipLevelOffsets[dstIndex3],
                            _CbDstOffset0 = data.info.mipLevelOffsetsCheckerboard[dstIndex0],
                            _CbDstOffset1 = data.info.mipLevelOffsetsCheckerboard[dstIndex1],
                        };

                        ConstantBuffer.Push(cmd, in cb, data.cs, _DepthPyramidConstants);

                        cmd.SetComputeTextureParam(data.cs, data.kernel, _DepthMipChain, data.depthTexture);
                        cmd.SetKeyword(data.cs, data.enableCheckerboardKeyword, cbCount != 0);

                        Vector2Int dstSize = data.info.mipLevelSizes[dstIndex0];
                        cmd.DispatchCompute(data.cs, data.kernel, CoreUtils.DivRoundUp(dstSize.x, 8), CoreUtils.DivRoundUp(dstSize.y, 8), ((RenderTexture)data.depthTexture).volumeDepth);

                        dstIndex0 += minCount;
                    }

                });
            }
        }

        private class PassDataMipChainRaster
        {
            public MaterialPropertyBlock propertyBlock;
            public Material material;
            public TextureHandle source;
            public int dstMipWidth, dstMipHeight;
            public int srcMipLevel;
            public float scaleX, scaleY;
            public float blurSourceTextureWidth, blurSourceTextureHeight;
            public LocalKeyword disableTexture2DArrayKeyword;
            public bool sourceIsArray;
        }

        private class PassDataMipChainCompute
        {
            public int numTheadGroupX, numTheadGroupY, numTheadGroupZ;
            public int srcMipWidth, srcMipHeight;
            public int dstMipWidth, dstMipHeight;
            public int srcMipLevel;
            public TextureHandle tempDownsamplePyramid, destination;
            public ComputeShader cs;
            public LocalKeyword disableTexture2DArrayKeyword;
            public LocalKeyword targetUnormKeyword, targetHalfKeyword, targetFloatKeyword;
            public bool targetHalf, targetFloat;
            public int downsampleKernel, gaussianKernel;
            public bool sourceIsArray;
        }

        // Generates the gaussian pyramid of source into destination
        // We can't do it in place as the color pyramid has to be read while writing to the color
        // buffer in some cases (e.g. refraction, distortion)
        // Returns the number of mips
        public int RenderColorGaussianPyramid(RenderGraph renderGraph, Vector2Int size, TextureHandle source, TextureHandle destination)
        {
            TextureDesc descSrc = renderGraph.GetTextureDesc(source);
            TextureDesc descDst = renderGraph.GetTextureDesc(destination);

            // Select between Tex2D and Tex2DArray versions of the kernels
            bool sourceIsArray = (descSrc.dimension == TextureDimension.Tex2DArray);
            // Sanity check
            if (sourceIsArray)
            {
                Debug.Assert(descSrc.dimension == descDst.dimension, "MipGenerator source texture does not match dimension of destination!");
            }

            int srcMipLevel = 0;
            int srcMipWidth = size.x;
            int srcMipHeight = size.y;

            bool isHardwareDrsOn = DynamicResolutionHandler.instance.HardwareDynamicResIsEnabled();
            var hardwareTextureSize = new Vector2Int(descSrc.width, descSrc.height);
            if (isHardwareDrsOn)
                hardwareTextureSize = DynamicResolutionHandler.instance.ApplyScalesOnSize(hardwareTextureSize);

            renderGraph.AddCopyPass(source, destination, "Copy mip 0");
            var finalTargetSize = new Vector2Int(descDst.width, descDst.height);
            if (descDst.useDynamicScale && isHardwareDrsOn)
                finalTargetSize = DynamicResolutionHandler.instance.ApplyScalesOnSize(finalTargetSize);

            bool usePixelShader = !m_SupportCompute || !m_PreferCompute;

            // Note: smaller mips are excluded as we don't need them and the gaussian compute works
            // on 8x8 blocks
            while (srcMipWidth >= k_MinimumResolutionGaussian || srcMipHeight >= k_MinimumResolutionGaussian)
            {
                int dstMipWidth = Mathf.Max(1, srcMipWidth >> 1);
                int dstMipHeight = Mathf.Max(1, srcMipHeight >> 1);

                var desc = new TextureDesc(dstMipWidth, dstMipHeight);
                desc.name = "Temporary Downsampled Pyramid";
                desc.scale = Vector2.one * 0.5f;
                desc.dimension = descSrc.dimension;
                desc.colorFormat = descDst.colorFormat;
                desc.enableRandomWrite = !usePixelShader;
                desc.useMipMap = false;
                desc.slices = sourceIsArray ? TextureXR.slices : 1;
                var tempDownsamplePyramid = renderGraph.CreateTexture(desc);

                // Scale for downsample
                float downsampleScaleX = ((float)srcMipWidth / finalTargetSize.x);
                float downsampleScaleY = ((float)srcMipHeight / finalTargetSize.y);

                if (usePixelShader)
                {
                    // Downsample.
                    renderGraph.AddBlitPass(destination, tempDownsamplePyramid, new(downsampleScaleX, downsampleScaleY), Vector2.zero, sourceMip: srcMipLevel, passName: "Color Pyramid - Downsample");

                    // In this mip generation process, source viewport can be smaller than the source render target itself because of the RTHandle system
                    // We are not using the scale provided by the RTHandle system for two reasons:
                    // - Source might be a planar probe which will not be scaled by the system (since it's actually the final target of probe rendering at the exact size)
                    // - When computing mip size, depending on even/odd sizes, the scale computed for mip 0 might miss a texel at the border.
                    //   This can result in a shift in the mip map downscale that depends on the render target size rather than the actual viewport
                    //   (Two rendering at the same viewport size but with different RTHandle reference size would yield different results which can break automated testing)
                    // So in the end we compute a specific scale for downscale and blur passes at each mip level.

                    // Scales for Blur
                    // Same size as m_TempColorTargets which is the source for vertical blur
                    desc.name = "Temp Gaussian Pyramid Target";
                    desc.slices = sourceIsArray ? TextureXR.slices : 1;
                    desc.dimension = descSrc.dimension;
                    desc.filterMode = FilterMode.Bilinear;
                    desc.colorFormat = descDst.colorFormat;
                    desc.enableRandomWrite = false;
                    desc.useMipMap = false;
                    var tempColorTarget = renderGraph.CreateTexture(desc);

                    var hardwareBlurSourceTextureSize = new Vector2Int(dstMipWidth, dstMipHeight);
                    if (isHardwareDrsOn)
                        hardwareBlurSourceTextureSize = DynamicResolutionHandler.instance.ApplyScalesOnSize(hardwareBlurSourceTextureSize);

                    float blurSourceTextureWidth = (float)hardwareBlurSourceTextureSize.x;
                    float blurSourceTextureHeight = (float)hardwareBlurSourceTextureSize.y;

                    float scaleX = ((float)dstMipWidth / blurSourceTextureWidth);
                    float scaleY = ((float)dstMipHeight / blurSourceTextureHeight);

                    // Blur horizontal.
                    using (var builder = renderGraph.AddRasterRenderPass<PassDataMipChainRaster>("Color Pyramid - Horizontal Blur", out var passData))
                    {
                        passData.propertyBlock = m_PropertyBlockBlur;
                        passData.source = tempDownsamplePyramid;
                        passData.material = m_ColorPyramidPSMat;
                        passData.dstMipWidth = dstMipWidth;
                        passData.dstMipHeight = dstMipHeight;
                        passData.scaleX = scaleX;
                        passData.scaleY = scaleY;
                        passData.blurSourceTextureWidth = blurSourceTextureWidth;
                        passData.blurSourceTextureHeight = blurSourceTextureHeight;
                        passData.disableTexture2DArrayKeyword = m_DisableTexture2DArrayColorPSKeyword;
                        passData.sourceIsArray = sourceIsArray;

                        builder.UseTexture(passData.source, AccessFlags.Read);
                        builder.SetRenderAttachment(tempColorTarget, 0);
                        builder.SetRenderFunc<PassDataMipChainRaster>(static (data, context) => MipChainRasterBlurExecutePass(data, context, false));
                    }

                    // Blur vertical.
                    using (var builder = renderGraph.AddRasterRenderPass<PassDataMipChainRaster>("Color Pyramid - Vertical Blur", out var passData))
                    {
                        passData.propertyBlock = m_PropertyBlockBlur;
                        passData.source = tempColorTarget;
                        passData.material = m_ColorPyramidPSMat;
                        passData.dstMipWidth = dstMipWidth;
                        passData.dstMipHeight = dstMipHeight;
                        passData.scaleX = scaleX;
                        passData.scaleY = scaleY;
                        passData.blurSourceTextureWidth = blurSourceTextureWidth;
                        passData.blurSourceTextureHeight = blurSourceTextureHeight;
                        passData.disableTexture2DArrayKeyword = m_DisableTexture2DArrayColorPSKeyword;
                        passData.sourceIsArray = sourceIsArray;

                        builder.UseTexture(passData.source, AccessFlags.Read);
                        builder.SetRenderAttachment(destination, 0, AccessFlags.Write, srcMipLevel + 1, -1);
                        builder.SetRenderFunc<PassDataMipChainRaster>(static (data, context) => MipChainRasterBlurExecutePass(data, context, true));
                    }
                }
                else
                {
                    using (var builder = renderGraph.AddComputePass<PassDataMipChainCompute>("Color Pyramid - MIP Chain (Compute)", out var passData))
                    {
                        passData.numTheadGroupX = (dstMipWidth + 7) / 8;
                        passData.numTheadGroupY = (dstMipHeight + 7) / 8;
                        passData.numTheadGroupZ = TextureXR.slices;
                        passData.srcMipWidth = srcMipWidth;
                        passData.srcMipHeight = srcMipHeight;
                        passData.dstMipWidth = dstMipWidth;
                        passData.dstMipHeight = dstMipHeight;
                        passData.srcMipLevel = srcMipLevel;
                        passData.tempDownsamplePyramid = tempDownsamplePyramid;
                        passData.destination = destination;
                        passData.cs = m_ColorPyramidCS;
                        passData.disableTexture2DArrayKeyword = m_DisableTexture2DArrayColorKeyword;
                        passData.targetUnormKeyword = m_TargetUnormColorKeyword;
                        passData.targetHalfKeyword = m_TargetHalfColorKeyword;
                        passData.targetFloatKeyword = m_TargetFloatColorKeyword;
                        passData.targetHalf = GraphicsFormatUtility.IsHalfFormat(descDst.colorFormat);
                        passData.targetFloat = !passData.targetHalf && GraphicsFormatUtility.IsFloatFormat(descDst.colorFormat);
                        passData.downsampleKernel = m_ColorDownsampleKernel;
                        passData.gaussianKernel = m_ColorGaussianKernel;
                        passData.sourceIsArray = sourceIsArray;

                        // Enable global state modification so we can set keywords.
                        builder.AllowGlobalStateModification(true);
                        builder.UseTexture(passData.tempDownsamplePyramid, AccessFlags.ReadWrite);
                        builder.UseTexture(destination, AccessFlags.ReadWrite);

                        builder.SetRenderFunc(static (PassDataMipChainCompute data, ComputeGraphContext context) =>
                        {
                            context.cmd.SetKeyword(data.cs, data.disableTexture2DArrayKeyword, !data.sourceIsArray);
                            context.cmd.SetKeyword(data.cs, data.targetFloatKeyword, data.targetFloat);
                            context.cmd.SetKeyword(data.cs, data.targetHalfKeyword, data.targetHalf);
                            context.cmd.SetKeyword(data.cs, data.targetUnormKeyword, !data.targetFloat && !data.targetHalf);

                            // Downsample.
                            context.cmd.SetComputeVectorParam(data.cs, _Size, new Vector4(data.srcMipWidth, data.srcMipHeight, 0f, 0f));
                            context.cmd.SetComputeTextureParam(data.cs, data.downsampleKernel, _Source, data.destination, data.srcMipLevel);
                            context.cmd.SetComputeTextureParam(data.cs, data.downsampleKernel, _Destination, data.tempDownsamplePyramid);
                            context.cmd.DispatchCompute(data.cs, data.downsampleKernel, data.numTheadGroupX, data.numTheadGroupY, data.numTheadGroupZ);

                            // Single pass blur
                            context.cmd.SetComputeVectorParam(data.cs, _Size, new Vector4(data.dstMipWidth, data.dstMipHeight, 0f, 0f));
                            context.cmd.SetComputeTextureParam(data.cs, data.gaussianKernel, _Source, data.tempDownsamplePyramid);
                            context.cmd.SetComputeTextureParam(data.cs, data.gaussianKernel, _Destination, data.destination, data.srcMipLevel + 1);
                            context.cmd.DispatchCompute(data.cs, data.gaussianKernel, data.numTheadGroupX, data.numTheadGroupY, data.numTheadGroupZ);
                        });
                    }
                }

                srcMipLevel++;
                srcMipWidth = dstMipWidth;
                srcMipHeight = dstMipHeight;

                finalTargetSize.x >>= 1;
                finalTargetSize.y >>= 1;
            }

            return srcMipLevel + 1;
        }

        static void MipChainRasterBlurExecutePass(PassDataMipChainRaster data, RasterGraphContext context, bool isVertical)
        {
            data.propertyBlock.SetTexture(_Source, data.source);
            data.propertyBlock.SetVector(_SrcScaleBias, new Vector4(data.scaleX, data.scaleY, 0f, 0f));
            data.propertyBlock.SetVector(_SrcUvLimits, new Vector4((data.dstMipWidth - 0.5f) / data.blurSourceTextureWidth, (data.dstMipHeight - 0.5f) / data.blurSourceTextureHeight, !isVertical ? 1.0f / data.blurSourceTextureWidth : 0f, isVertical ? 1.0f / data.blurSourceTextureHeight : 0));
            data.propertyBlock.SetFloat(_SourceMip, 0);
            data.material.SetKeyword(data.disableTexture2DArrayKeyword, !data.sourceIsArray);
            context.cmd.SetViewport(new Rect(0, 0, data.dstMipWidth, data.dstMipHeight));
            context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3, 1, data.propertyBlock);
        }
    }
}
