using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Avoid GCAlloc by capturing functor...
        TextureDesc m_DepthMomentsPyramidVSM32Desc;
        TextureDesc m_DepthMomentsPyramidVSM16Desc;
        TextureDesc m_DepthMomentsPyramidMoments16Desc;
        TextureDesc m_DepthMomentsPyramidEVSM32Desc;
        TextureDesc m_DepthMomentsPyramidEVSM16Desc;
        TextureDesc m_HierarchicalVarianceScreenSpaceShadowsDesc;

        
        ComputeShader m_DepthMomentsPyramidCS;

        int m_DepthMomentsLinearizeVSM32Kernel;
        int m_DepthMomentsLinearizeVSM16Kernel;
        int m_DepthMomentsLinearizeMoments16Kernel;
        int m_DepthMomentsLinearizeEVSM32Kernel;
        int m_DepthMomentsLinearizeEVSM16Kernel;

        int m_DepthMomentsDownsampleVSM32Kernel;
        int m_DepthMomentsDownsampleVSM16Kernel;
        int m_DepthMomentsDownsampleMoments16Kernel;
        int m_DepthMomentsDownsampleEVSM32Kernel;
        int m_DepthMomentsDownsampleEVSM16Kernel;

        ComputeShader m_HierarchicalVarianceScreenSpaceShadowsCS { get { return defaultResources.shaders.hierarchicalVarianceScreenSpaceShadowsCS; } }
        int m_HierarchicalVarianceScreenSpaceShadowsRaymarchVSM32Kernel = -1;
        int m_HierarchicalVarianceScreenSpaceShadowsRaymarchVSM16Kernel = -1;
        int m_HierarchicalVarianceScreenSpaceShadowsRaymarchMoments16Kernel = -1;
        int m_HierarchicalVarianceScreenSpaceShadowsRaymarchEVSM32Kernel = -1;
        int m_HierarchicalVarianceScreenSpaceShadowsRaymarchEVSM16Kernel = -1;

        int m_HierarchicalVarianceScreenSpaceShadowsRaymarchVSM32TransmissionAccumulatorKernel = -1;
        int m_HierarchicalVarianceScreenSpaceShadowsRaymarchVSM16TransmissionAccumulatorKernel = -1;
        int m_HierarchicalVarianceScreenSpaceShadowsRaymarchMoments16TransmissionAccumulatorKernel = -1;
        int m_HierarchicalVarianceScreenSpaceShadowsRaymarchEVSM32TransmissionAccumulatorKernel = -1;
        int m_HierarchicalVarianceScreenSpaceShadowsRaymarchEVSM16TransmissionAccumulatorKernel = -1;

        int m_HierarchicalVarianceScreenSpaceShadowsBilateralBlurVSM32Kernel = -1;
        int m_HierarchicalVarianceScreenSpaceShadowsBilateralBlurVSM16Kernel = -1;
        int m_HierarchicalVarianceScreenSpaceShadowsBilateralBlurMoments16Kernel = -1;
        int m_HierarchicalVarianceScreenSpaceShadowsBilateralBlurEVSM32Kernel = -1;
        int m_HierarchicalVarianceScreenSpaceShadowsBilateralBlurEVSM16Kernel = -1;

        int m_HierarchicalVarianceScreenSpaceShadowsBilateralUpsampleVSM32Kernel = -1;
        int m_HierarchicalVarianceScreenSpaceShadowsBilateralUpsampleVSM16Kernel = -1;
        int m_HierarchicalVarianceScreenSpaceShadowsBilateralUpsampleMoments16Kernel = -1;
        int m_HierarchicalVarianceScreenSpaceShadowsBilateralUpsampleEVSM32Kernel = -1;
        int m_HierarchicalVarianceScreenSpaceShadowsBilateralUpsampleEVSM16Kernel = -1;

        public static int s_HVSSSRaymarchLODSampleCount = 12;
        public static int s_HVSSSRaymarchLODMin = 1;
        public static int s_HVSSSRaymarchLODMax = 5;
        public static int s_HVSSSRaymarchLODBias = -2;
        public static float s_HVSSSThicknessMin = 0.06f;
        public static float s_HVSSSThicknessMax = 0.1f;
        public static int s_HVSSSDitherMode = 0;
        public static int s_HVSSSUpsampleBlurRadiusPixels = 1;
        public static float s_HVSSSBlackpoint = 0.5f;
        public static float s_HVSSSContrast = 0.0f;
        public static Vector2 s_HVSSSEVSMExponents = new Vector2(0.125f, 1.0f);

        public enum HVSSSMode : int
        {
            VSM32 = 0,
            VSM16,
            Moments16,
            EVSM32,
            EVSM16
        }
        public static HVSSSMode s_HVSSSMode = HVSSSMode.VSM16;
        public static bool s_HVSSSTransmissionAccumulatorEnabled = true;

        void InitializeHierarchicalVarianceScreenSpaceShadows(HDRenderPipelineAsset hdAsset)
        {
            m_DepthMomentsPyramidVSM32Desc = new TextureDesc(ComputeDepthBufferMipChainSize, true, true)
            { colorFormat = GraphicsFormat.R32G32_SFloat, enableRandomWrite = true, name = "CameraDepthMomentsMipChain" };

            m_DepthMomentsPyramidVSM16Desc = new TextureDesc(ComputeDepthBufferMipChainSize, true, true)
            { colorFormat = GraphicsFormat.R16G16_UNorm, enableRandomWrite = true, name = "CameraDepthMomentsMipChain" };

            m_DepthMomentsPyramidMoments16Desc = new TextureDesc(ComputeDepthBufferMipChainSize, true, true)
            { colorFormat = GraphicsFormat.R16G16B16A16_UNorm, enableRandomWrite = true, name = "CameraDepthMomentsMipChain" };

            m_DepthMomentsPyramidEVSM32Desc = new TextureDesc(ComputeDepthBufferMipChainSize, true, true)
            { colorFormat = GraphicsFormat.R32G32B32A32_SFloat, enableRandomWrite = true, name = "CameraDepthMomentsMipChain" };

            m_DepthMomentsPyramidEVSM16Desc = new TextureDesc(ComputeDepthBufferMipChainSize, true, true)
            { colorFormat = GraphicsFormat.R16G16B16A16_UNorm, enableRandomWrite = true, name = "CameraDepthMomentsMipChain" }; 


            m_HierarchicalVarianceScreenSpaceShadowsDesc = new TextureDesc(Vector2.one, true, true)
            { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite = true, name = "CameraHierarchicalVarianceScreenSpaceShadows" };

            m_DepthMomentsPyramidCS = defaultResources.shaders.depthMomentsPyramidCS;

            m_DepthMomentsLinearizeVSM32Kernel = m_DepthMomentsPyramidCS.FindKernel("DepthMomentsLinearizeVSM32");
            m_DepthMomentsLinearizeVSM16Kernel = m_DepthMomentsPyramidCS.FindKernel("DepthMomentsLinearizeVSM16");
            m_DepthMomentsLinearizeMoments16Kernel = m_DepthMomentsPyramidCS.FindKernel("DepthMomentsLinearizeMoments16");
            m_DepthMomentsLinearizeEVSM32Kernel = m_DepthMomentsPyramidCS.FindKernel("DepthMomentsLinearizeEVSM32");
            m_DepthMomentsLinearizeEVSM16Kernel = m_DepthMomentsPyramidCS.FindKernel("DepthMomentsLinearizeEVSM16");

            m_DepthMomentsDownsampleVSM32Kernel = m_DepthMomentsPyramidCS.FindKernel("DepthMomentsDownsampleVSM32");
            m_DepthMomentsDownsampleVSM16Kernel = m_DepthMomentsPyramidCS.FindKernel("DepthMomentsDownsampleVSM16");
            m_DepthMomentsDownsampleMoments16Kernel = m_DepthMomentsPyramidCS.FindKernel("DepthMomentsDownsampleMoments16");
            m_DepthMomentsDownsampleEVSM32Kernel = m_DepthMomentsPyramidCS.FindKernel("DepthMomentsDownsampleEVSM32");
            m_DepthMomentsDownsampleEVSM16Kernel = m_DepthMomentsPyramidCS.FindKernel("DepthMomentsDownsampleEVSM16");

            m_HierarchicalVarianceScreenSpaceShadowsRaymarchVSM32Kernel = m_HierarchicalVarianceScreenSpaceShadowsCS.FindKernel("RaymarchScreenSpaceShadowsVSM32");
            m_HierarchicalVarianceScreenSpaceShadowsRaymarchVSM16Kernel = m_HierarchicalVarianceScreenSpaceShadowsCS.FindKernel("RaymarchScreenSpaceShadowsVSM16");
            m_HierarchicalVarianceScreenSpaceShadowsRaymarchMoments16Kernel = m_HierarchicalVarianceScreenSpaceShadowsCS.FindKernel("RaymarchScreenSpaceShadowsMoments16");
            m_HierarchicalVarianceScreenSpaceShadowsRaymarchEVSM32Kernel = m_HierarchicalVarianceScreenSpaceShadowsCS.FindKernel("RaymarchScreenSpaceShadowsEVSM32");
            m_HierarchicalVarianceScreenSpaceShadowsRaymarchEVSM16Kernel = m_HierarchicalVarianceScreenSpaceShadowsCS.FindKernel("RaymarchScreenSpaceShadowsEVSM16");

            m_HierarchicalVarianceScreenSpaceShadowsRaymarchVSM32TransmissionAccumulatorKernel = m_HierarchicalVarianceScreenSpaceShadowsCS.FindKernel("RaymarchScreenSpaceShadowsVSM32TransmissionAccumulator");
            m_HierarchicalVarianceScreenSpaceShadowsRaymarchVSM16TransmissionAccumulatorKernel = m_HierarchicalVarianceScreenSpaceShadowsCS.FindKernel("RaymarchScreenSpaceShadowsVSM16TransmissionAccumulator");
            m_HierarchicalVarianceScreenSpaceShadowsRaymarchMoments16TransmissionAccumulatorKernel = m_HierarchicalVarianceScreenSpaceShadowsCS.FindKernel("RaymarchScreenSpaceShadowsMoments16TransmissionAccumulator");
            m_HierarchicalVarianceScreenSpaceShadowsRaymarchEVSM32TransmissionAccumulatorKernel = m_HierarchicalVarianceScreenSpaceShadowsCS.FindKernel("RaymarchScreenSpaceShadowsEVSM32TransmissionAccumulator");
            m_HierarchicalVarianceScreenSpaceShadowsRaymarchEVSM16TransmissionAccumulatorKernel = m_HierarchicalVarianceScreenSpaceShadowsCS.FindKernel("RaymarchScreenSpaceShadowsEVSM16TransmissionAccumulator");

            m_HierarchicalVarianceScreenSpaceShadowsBilateralBlurVSM32Kernel = m_HierarchicalVarianceScreenSpaceShadowsCS.FindKernel("SeperableBilateralBlurVSM32");
            m_HierarchicalVarianceScreenSpaceShadowsBilateralBlurVSM16Kernel = m_HierarchicalVarianceScreenSpaceShadowsCS.FindKernel("SeperableBilateralBlurVSM16");
            m_HierarchicalVarianceScreenSpaceShadowsBilateralBlurMoments16Kernel = m_HierarchicalVarianceScreenSpaceShadowsCS.FindKernel("SeperableBilateralBlurMoments16");
            m_HierarchicalVarianceScreenSpaceShadowsBilateralBlurEVSM32Kernel = m_HierarchicalVarianceScreenSpaceShadowsCS.FindKernel("SeperableBilateralBlurEVSM32");
            m_HierarchicalVarianceScreenSpaceShadowsBilateralBlurEVSM16Kernel = m_HierarchicalVarianceScreenSpaceShadowsCS.FindKernel("SeperableBilateralBlurEVSM16");

            m_HierarchicalVarianceScreenSpaceShadowsBilateralUpsampleVSM32Kernel = m_HierarchicalVarianceScreenSpaceShadowsCS.FindKernel("BilateralUpsampleVSM32");
            m_HierarchicalVarianceScreenSpaceShadowsBilateralUpsampleVSM16Kernel = m_HierarchicalVarianceScreenSpaceShadowsCS.FindKernel("BilateralUpsampleVSM16");
            m_HierarchicalVarianceScreenSpaceShadowsBilateralUpsampleMoments16Kernel = m_HierarchicalVarianceScreenSpaceShadowsCS.FindKernel("BilateralUpsampleMoments16");
            m_HierarchicalVarianceScreenSpaceShadowsBilateralUpsampleEVSM32Kernel = m_HierarchicalVarianceScreenSpaceShadowsCS.FindKernel("BilateralUpsampleEVSM32");
            m_HierarchicalVarianceScreenSpaceShadowsBilateralUpsampleEVSM16Kernel = m_HierarchicalVarianceScreenSpaceShadowsCS.FindKernel("BilateralUpsampleEVSM16");
        }

        private TextureDesc GetDepthMomentsPyramidDesc()
        {
            switch (s_HVSSSMode)
            {
                case HVSSSMode.Moments16: return m_DepthMomentsPyramidMoments16Desc;
                case HVSSSMode.VSM16: return m_DepthMomentsPyramidVSM16Desc;
                case HVSSSMode.VSM32: return m_DepthMomentsPyramidVSM32Desc;
                case HVSSSMode.EVSM32: return m_DepthMomentsPyramidEVSM32Desc;
                case HVSSSMode.EVSM16: return m_DepthMomentsPyramidEVSM16Desc;
                default: Debug.Assert(false); return default(TextureDesc);
            }
        }

        private int GetDepthMomentsLinearizeKernel()
        {
            switch (s_HVSSSMode)
            {
                case HVSSSMode.Moments16: return m_DepthMomentsLinearizeMoments16Kernel;
                case HVSSSMode.VSM32: return m_DepthMomentsLinearizeVSM32Kernel;
                case HVSSSMode.VSM16: return m_DepthMomentsLinearizeVSM16Kernel;
                case HVSSSMode.EVSM32: return m_DepthMomentsLinearizeEVSM32Kernel;
                case HVSSSMode.EVSM16: return m_DepthMomentsLinearizeEVSM16Kernel;
                default: Debug.Assert(false); return -1;
            }
        }

        private int GetDepthMomentsDownsampleKernel()
        {
            switch (s_HVSSSMode)
            {
                case HVSSSMode.Moments16: return m_DepthMomentsDownsampleMoments16Kernel;
                case HVSSSMode.VSM32: return m_DepthMomentsDownsampleVSM32Kernel;
                case HVSSSMode.VSM16: return m_DepthMomentsDownsampleVSM16Kernel;
                case HVSSSMode.EVSM32: return m_DepthMomentsDownsampleEVSM32Kernel;
                case HVSSSMode.EVSM16: return m_DepthMomentsDownsampleEVSM16Kernel;
                default: Debug.Assert(false); return -1;
            }
        }

        private int GetHierarchicalVarianceScreenSpaceShadowsRaymarchKernel()
        {
            if (s_HVSSSTransmissionAccumulatorEnabled)
            {
                switch (s_HVSSSMode)
                {
                    case HVSSSMode.Moments16: return m_HierarchicalVarianceScreenSpaceShadowsRaymarchMoments16TransmissionAccumulatorKernel;
                    case HVSSSMode.VSM32: return m_HierarchicalVarianceScreenSpaceShadowsRaymarchVSM32TransmissionAccumulatorKernel;
                    case HVSSSMode.VSM16: return m_HierarchicalVarianceScreenSpaceShadowsRaymarchVSM16TransmissionAccumulatorKernel;
                    case HVSSSMode.EVSM32: return m_HierarchicalVarianceScreenSpaceShadowsRaymarchEVSM32TransmissionAccumulatorKernel;
                    case HVSSSMode.EVSM16: return m_HierarchicalVarianceScreenSpaceShadowsRaymarchEVSM16TransmissionAccumulatorKernel;
                    default: Debug.Assert(false); return -1;
                }
            }
            else
            {
                switch (s_HVSSSMode)
                {
                    case HVSSSMode.Moments16: return m_HierarchicalVarianceScreenSpaceShadowsRaymarchMoments16Kernel;
                    case HVSSSMode.VSM32: return m_HierarchicalVarianceScreenSpaceShadowsRaymarchVSM32Kernel;
                    case HVSSSMode.VSM16: return m_HierarchicalVarianceScreenSpaceShadowsRaymarchVSM16Kernel;
                    case HVSSSMode.EVSM32: return m_HierarchicalVarianceScreenSpaceShadowsRaymarchEVSM32Kernel;
                    case HVSSSMode.EVSM16: return m_HierarchicalVarianceScreenSpaceShadowsRaymarchEVSM16Kernel;
                    default: Debug.Assert(false); return -1;
                }
            }
            
        }

        private int GetHierarchicalVarianceScreenSpaceShadowsBilateralBlurKernel()
        {
            switch (s_HVSSSMode)
            {
                case HVSSSMode.Moments16: return m_HierarchicalVarianceScreenSpaceShadowsBilateralBlurMoments16Kernel;
                case HVSSSMode.VSM32: return m_HierarchicalVarianceScreenSpaceShadowsBilateralBlurVSM32Kernel;
                case HVSSSMode.VSM16: return m_HierarchicalVarianceScreenSpaceShadowsBilateralBlurVSM16Kernel;
                case HVSSSMode.EVSM32: return m_HierarchicalVarianceScreenSpaceShadowsBilateralBlurEVSM32Kernel;
                case HVSSSMode.EVSM16: return m_HierarchicalVarianceScreenSpaceShadowsBilateralBlurEVSM16Kernel;
                default: Debug.Assert(false); return -1;
            }
        }

        private int GetHierarchicalVarianceScreenSpaceShadowsBilateralUpsampleKernel()
        {
            switch (s_HVSSSMode)
            {
                case HVSSSMode.Moments16: return m_HierarchicalVarianceScreenSpaceShadowsBilateralUpsampleMoments16Kernel;
                case HVSSSMode.VSM32: return m_HierarchicalVarianceScreenSpaceShadowsBilateralUpsampleVSM32Kernel;
                case HVSSSMode.VSM16: return m_HierarchicalVarianceScreenSpaceShadowsBilateralUpsampleVSM16Kernel;
                case HVSSSMode.EVSM32: return m_HierarchicalVarianceScreenSpaceShadowsBilateralUpsampleEVSM32Kernel;
                case HVSSSMode.EVSM16: return m_HierarchicalVarianceScreenSpaceShadowsBilateralUpsampleEVSM16Kernel;
                default: Debug.Assert(false); return -1;
            }
        }

        class CopyDepthBufferToMomentsPyramidData
        {
            public TextureHandle inputDepth;
            public TextureHandle outputDepthMomentsPyramid;
            public MipGenerator mipGenerator;
            public HDUtils.PackedMipChainInfo mipInfo;
            public int width;
            public int height;
            public Vector4 zBufferParams;
            public float depthMin;
            public float depthMax;
            public Vector2 evsmExponents;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
            public ComputeShader depthMomentsPyramidCS;
            public int depthMomentsLinearizeKernel;
        }

        void CopyDepthBufferToMomentsPyramid(RenderGraph renderGraph, HDCamera hdCamera, ref PrepassOutput output)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects) || !hdCamera.frameSettings.IsEnabled(FrameSettingsField.HierarchicalVarianceScreenSpaceShadows))
            {
                output.depthMomentsPyramidTexture = renderGraph.defaultResources.blackTextureXR;
                return;
            }

            using (var builder = renderGraph.AddRenderPass<CopyDepthBufferToMomentsPyramidData>("Copy depth buffer to moments pyramid", out var passData, ProfilingSampler.Get(HDProfileId.CopyDepthBufferToMomentsPyramid)))
            {
                builder.EnableAsyncCompute(hdCamera.frameSettings.HiearchicalVarianceScreenSpaceShadowsRunAsync());

                passData.inputDepth = builder.ReadTexture(output.resolvedDepthBuffer);
                passData.outputDepthMomentsPyramid = builder.WriteTexture(renderGraph.CreateTexture(GetDepthMomentsPyramidDesc()));
                passData.mipGenerator = m_MipGenerator;
                passData.mipInfo = GetDepthBufferMipChainInfo();
                passData.width = hdCamera.actualWidth;
                passData.height = hdCamera.actualHeight;
                passData.zBufferParams = hdCamera.zBufferParams;
                passData.depthMin = m_HierarchicalVarianceScreenSpaceShadowsData.depthMin;
                passData.depthMax = m_HierarchicalVarianceScreenSpaceShadowsData.depthMax;
                passData.evsmExponents = s_HVSSSEVSMExponents;

                BlueNoise blueNoise = GetBlueNoiseManager();
                passData.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();

                passData.depthMomentsPyramidCS = m_DepthMomentsPyramidCS;
                passData.depthMomentsLinearizeKernel = GetDepthMomentsLinearizeKernel();

                output.depthMomentsPyramidTexture = passData.outputDepthMomentsPyramid;

                builder.SetRenderFunc(
                (CopyDepthBufferToMomentsPyramidData data, RenderGraphContext context) =>
                {
                    RenderDepthMomentsLinearize(context.cmd, data.inputDepth, data.outputDepthMomentsPyramid, data.mipInfo, data.zBufferParams, data.depthMin, data.depthMax, data.evsmExponents, data.ditheredTextureSet, data.depthMomentsPyramidCS, data.depthMomentsLinearizeKernel);
                });
            }
        }

        private static void RenderDepthMomentsLinearize(CommandBuffer cmd, RenderTexture depthTexture, RenderTexture texture, HDUtils.PackedMipChainInfo info, Vector4 zBufferParams, float depthMin, float depthMax, Vector2 evsmExponents, BlueNoise.DitheredTextureSet ditheredTextureSet, ComputeShader depthMomentsPyramidCS, int depthMomentsLinearizeKernel)
        {
            HDUtils.CheckRTCreated(depthTexture);
            HDUtils.CheckRTCreated(texture);

            var cs = depthMomentsPyramidCS;
            int kernel = depthMomentsLinearizeKernel;

            Vector2Int dstSize = info.mipLevelSizes[0];
            Vector2Int dstOffset = info.mipLevelOffsets[0];
            Vector2Int dstLimit = dstOffset + dstSize - Vector2Int.one;
            Vector2Int srcSize = info.mipLevelSizes[0];
            Vector2Int srcOffset = new Vector2Int(0, 0);
            Vector2Int srcLimit = srcOffset + srcSize - Vector2Int.one;

            cmd.SetComputeVectorParam(cs, HDShaderIDs._SrcOffsetAndLimit, new Vector4(srcOffset.x, srcOffset.y, srcLimit.x, srcLimit.y));
            cmd.SetComputeVectorParam(cs, HDShaderIDs._DstOffsetAndLimit, new Vector4(dstOffset.x, dstOffset.y, dstLimit.x, dstLimit.y));
            cmd.SetComputeVectorParam(cs, HDShaderIDs._HVSSSDepthMinMax, new Vector4(depthMin, depthMax, 1.0f / depthMin, 1.0f / depthMax));
            cmd.SetComputeVectorParam(cs, HDShaderIDs._HVSSSDepthScaleBias, new Vector4(1.0f / (depthMax - depthMin), -depthMin / (depthMax - depthMin), depthMax - depthMin, depthMin));
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ZBufferParams, zBufferParams);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._HVSSSEVSMExponents, new Vector4(evsmExponents.x, evsmExponents.y, 1.0f / evsmExponents.x, 1.0f / evsmExponents.y));
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._DepthTexture, depthTexture);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._DepthMomentsMipChain, texture);

            BlueNoise.BindDitheredTextureSetCompute(cmd, ditheredTextureSet, cs, kernel);

            cmd.DispatchCompute(cs, kernel, HDUtils.DivRoundUp(dstSize.x, 8), HDUtils.DivRoundUp(dstSize.y, 8), texture.volumeDepth);
        }

        class GenerateDepthMomentsPyramidPassData
        {
            public TextureHandle depthMomentsPyramidTexture;
            public HDUtils.PackedMipChainInfo mipInfo;
            public MipGenerator mipGenerator;
            public bool mip0AlreadyComputed;
            public Vector2 evsmExponents;

            public ComputeShader depthMomentsPyramidCS;
            public int depthMomentsDownsampleKernel;

            public BlueNoise.DitheredTextureSet ditheredTextureSet;
        }

        void GenerateDepthMomentsPyramid(RenderGraph renderGraph, HDCamera hdCamera, bool mip0AlreadyComputed, ref PrepassOutput output)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects) || !hdCamera.frameSettings.IsEnabled(FrameSettingsField.HierarchicalVarianceScreenSpaceShadows))
            {
                output.depthMomentsPyramidTexture = renderGraph.defaultResources.blackTextureXR;
                return;
            }

            CopyDepthBufferToMomentsPyramid(renderGraph, hdCamera, ref output);

            using (var builder = renderGraph.AddRenderPass<GenerateDepthMomentsPyramidPassData>("Generate Depth Buffer Moments MIP Chain", out var passData, ProfilingSampler.Get(HDProfileId.DepthMomentsPyramid)))
            {
                builder.EnableAsyncCompute(hdCamera.frameSettings.HiearchicalVarianceScreenSpaceShadowsRunAsync());

                passData.depthMomentsPyramidTexture = builder.WriteTexture(output.depthMomentsPyramidTexture);
                passData.mipInfo = GetDepthBufferMipChainInfo();
                passData.mipGenerator = m_MipGenerator;
                passData.mip0AlreadyComputed = mip0AlreadyComputed;
                passData.evsmExponents = s_HVSSSEVSMExponents;
                passData.depthMomentsPyramidCS = m_DepthMomentsPyramidCS;
                passData.depthMomentsDownsampleKernel = GetDepthMomentsDownsampleKernel();

                BlueNoise blueNoise = GetBlueNoiseManager();
                passData.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();

                builder.SetRenderFunc(
                (GenerateDepthMomentsPyramidPassData data, RenderGraphContext context) =>
                {
                    RenderDepthMomentsPyramid(context, data.depthMomentsPyramidTexture, data.mipInfo, data.mip0AlreadyComputed, data.evsmExponents, data.ditheredTextureSet, data.depthMomentsPyramidCS, data.depthMomentsDownsampleKernel);
                });

                output.depthMomentsPyramidTexture = passData.depthMomentsPyramidTexture;
            }
        }

        private static void RenderDepthMomentsPyramid(RenderGraphContext context, RenderTexture texture, HDUtils.PackedMipChainInfo info, bool mip1AlreadyComputed, Vector2 evsmExponents, BlueNoise.DitheredTextureSet ditheredTextureSet, ComputeShader depthMomentsPyramidCS, int depthMomentsDownsampleKernel)
        {
            HDUtils.CheckRTCreated(texture);

            var cs = depthMomentsPyramidCS;
            int kernel = depthMomentsDownsampleKernel;

            BlueNoise.BindDitheredTextureSetCompute(context.cmd, ditheredTextureSet, cs, kernel);

            // TODO: Do it 1x MIP at a time for now. In the future, do 4x MIPs per pass, or even use a single pass.
            // Note: Gather() doesn't take a LOD parameter and we cannot bind an SRV of a MIP level,
            // and we don't support Min samplers either. So we are forced to perform 4x loads.
            for (int i = 1; i < info.mipLevelCount; i++)
            {
                if (mip1AlreadyComputed && i == 1) continue;

                Vector2Int dstSize = info.mipLevelSizes[i];
                Vector2Int dstOffset = info.mipLevelOffsets[i];
                Vector2Int dstLimit = dstOffset + dstSize - Vector2Int.one;
                Vector2Int srcSize = info.mipLevelSizes[i - 1];
                Vector2Int srcOffset = info.mipLevelOffsets[i - 1];
                Vector2Int srcLimit = srcOffset + srcSize - Vector2Int.one;

                context.cmd.SetComputeVectorParam(cs, HDShaderIDs._SrcOffsetAndLimit, new Vector4(srcOffset.x, srcOffset.y, srcLimit.x, srcLimit.y));
                context.cmd.SetComputeVectorParam(cs, HDShaderIDs._DstOffsetAndLimit, new Vector4(dstOffset.x, dstOffset.y, dstLimit.x, dstLimit.y));
                context.cmd.SetComputeVectorParam(cs, HDShaderIDs._HVSSSEVSMExponents, new Vector4(evsmExponents.x, evsmExponents.y, 1.0f / evsmExponents.x, 1.0f / evsmExponents.y));
                context.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._DepthMomentsMipChain, texture);

                context.cmd.DispatchCompute(cs, kernel, HDUtils.DivRoundUp(dstSize.x, 8), HDUtils.DivRoundUp(dstSize.y, 8), texture.volumeDepth);
            }
        }

        class ComputeHierarchicalVarianceScreenSpaceShadowsData
        {
            public TextureHandle inputDepthMomentsPyramid;
            public TextureHandle outputHVSSSBufferA;
            public TextureHandle outputHVSSSBufferB;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
            public HDUtils.PackedMipChainInfo mipInfo;
            public int width;
            public int height;
            public int screenSpaceShadowCount;
            public Vector4[] lightPositionsWS = new Vector4[4];
            public float[] lightRanges = new float[4];
            public float depthMin;
            public float depthMax;
            public ComputeShader computeShader;
            public int raymarchScreenSpaceShadowsKernel;
            public int bilateralBlurKernel;
            public int bilateralUpsampleKernel;
            public int lodMax;
            public int lodRaymarchMin;
            public int lodRaymarchBias;
            public int raymarchLODSampleCount;
            public float thicknessMin;
            public float thicknessMax;
            public int ditherMode;
            public int upsampleBlurRadiusPixels;
            public float contrast;
            public float blackpoint;
            public Vector2 evsmExponents;
        }

        void ComputeHierarchicalVarianceScreenSpaceShadows(RenderGraph renderGraph, HDCamera hdCamera, ref PrepassOutput output)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects) || !hdCamera.frameSettings.IsEnabled(FrameSettingsField.HierarchicalVarianceScreenSpaceShadows) || (m_HierarchicalVarianceScreenSpaceShadowsData.count == 0))
            {
                output.hierarchicalVarianceScreenSpaceShadowsTexture = renderGraph.defaultResources.whiteTextureXR;
                return;
            }

            using (var builder = renderGraph.AddRenderPass<ComputeHierarchicalVarianceScreenSpaceShadowsData>("Compute Hierarchical Variance Screen Space Shadows", out var passData, ProfilingSampler.Get(HDProfileId.ComputeHierarchicalVarianceScreenSpaceShadows)))
            {
                builder.EnableAsyncCompute(hdCamera.frameSettings.HiearchicalVarianceScreenSpaceShadowsRunAsync());

                passData.inputDepthMomentsPyramid = builder.ReadTexture(output.depthMomentsPyramidTexture);
                passData.outputHVSSSBufferA = builder.WriteTexture(renderGraph.CreateTexture(m_HierarchicalVarianceScreenSpaceShadowsDesc));
                passData.outputHVSSSBufferB = builder.WriteTexture(renderGraph.CreateTexture(m_HierarchicalVarianceScreenSpaceShadowsDesc));

                BlueNoise blueNoise = GetBlueNoiseManager();
                passData.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();
                passData.mipInfo = GetDepthBufferMipChainInfo();
                passData.width = hdCamera.actualWidth;
                passData.height = hdCamera.actualHeight;
                passData.screenSpaceShadowCount = m_HierarchicalVarianceScreenSpaceShadowsData.count;
                for (int i = 0; i < 4; ++i) { passData.lightPositionsWS[i] = m_HierarchicalVarianceScreenSpaceShadowsData.positionsWS[i]; }
                for (int i = 0; i < 4; ++i) { passData.lightRanges[i] = m_HierarchicalVarianceScreenSpaceShadowsData.ranges[i]; }
                passData.depthMin = m_HierarchicalVarianceScreenSpaceShadowsData.depthMin;
                passData.depthMax = m_HierarchicalVarianceScreenSpaceShadowsData.depthMax;
                passData.computeShader = m_HierarchicalVarianceScreenSpaceShadowsCS;
                passData.raymarchScreenSpaceShadowsKernel = GetHierarchicalVarianceScreenSpaceShadowsRaymarchKernel();
                passData.bilateralBlurKernel = GetHierarchicalVarianceScreenSpaceShadowsBilateralBlurKernel();
                passData.bilateralUpsampleKernel = GetHierarchicalVarianceScreenSpaceShadowsBilateralUpsampleKernel();

                passData.lodMax = Mathf.Min(s_HVSSSRaymarchLODMax, passData.mipInfo.mipLevelCount - 1);
                passData.lodRaymarchMin = Mathf.Min(s_HVSSSRaymarchLODMin, passData.mipInfo.mipLevelCount - 1);
                passData.lodRaymarchBias = s_HVSSSRaymarchLODBias;
                passData.raymarchLODSampleCount = s_HVSSSRaymarchLODSampleCount;

                passData.thicknessMin = s_HVSSSThicknessMin;
                passData.thicknessMax = s_HVSSSThicknessMax;

                passData.ditherMode = s_HVSSSDitherMode;

                passData.upsampleBlurRadiusPixels = s_HVSSSUpsampleBlurRadiusPixels;

                passData.contrast = s_HVSSSContrast;
                passData.blackpoint = s_HVSSSBlackpoint;
                passData.evsmExponents = s_HVSSSEVSMExponents;

                uint bufferSwapCount = (uint)passData.lodMax;
                bufferSwapCount += (passData.lodRaymarchMin == 0) ? 1u : 0u;
                bool bufferSwapCountIsOdd = (bufferSwapCount & 1u) > 0;
                output.hierarchicalVarianceScreenSpaceShadowsTexture = bufferSwapCountIsOdd ? passData.outputHVSSSBufferB : passData.outputHVSSSBufferA;

                builder.SetRenderFunc(
                (ComputeHierarchicalVarianceScreenSpaceShadowsData data, RenderGraphContext context) =>
                {
                    ComputeHierarchicalVarianceScreenSpaceShadows(data, context);
                });
            }
        }

        private static void ComputeHierarchicalVarianceScreenSpaceShadows(ComputeHierarchicalVarianceScreenSpaceShadowsData data, RenderGraphContext context)
        {
            int inputDepthWidth = ((RenderTexture)data.inputDepthMomentsPyramid).width;
            int inputDepthHeight = ((RenderTexture)data.inputDepthMomentsPyramid).height;
            context.cmd.SetComputeVectorParam(data.computeShader, HDShaderIDs._DepthMomentsMipChainSize, new Vector4(inputDepthWidth, inputDepthHeight, 1.0f / inputDepthWidth, 1.0f / inputDepthHeight));
            context.cmd.SetComputeVectorParam(data.computeShader, HDShaderIDs._HVSSSDepthMinMax, new Vector4(data.depthMin, data.depthMax, 1.0f / data.depthMin, 1.0f / data.depthMax));
            context.cmd.SetComputeVectorParam(data.computeShader, HDShaderIDs._HVSSSDepthScaleBias, new Vector4(1.0f / (data.depthMax - data.depthMin), -data.depthMin / (data.depthMax - data.depthMin), data.depthMax - data.depthMin, data.depthMin));
            context.cmd.SetComputeVectorParam(data.computeShader, HDShaderIDs._HVSSSThickness, new Vector4(data.thicknessMin, data.thicknessMax, 1.0f / data.thicknessMin, 1.0f / data.thicknessMax));
            context.cmd.SetComputeVectorParam(data.computeShader, HDShaderIDs._HVSSSUpsampleBlurRadiusPixelsAndInverseSquared, new Vector4(data.upsampleBlurRadiusPixels, 1.0f / (data.upsampleBlurRadiusPixels * data.upsampleBlurRadiusPixels), 0.0f, 0.0f));
            context.cmd.SetComputeVectorParam(data.computeShader, HDShaderIDs._HVSSSContrastAndBlackpointAndDitherMode, new Vector4(data.contrast, data.blackpoint, data.ditherMode, 0.0f));
            context.cmd.SetComputeVectorParam(data.computeShader, HDShaderIDs._HVSSSEVSMExponents, new Vector4(data.evsmExponents.x, data.evsmExponents.y, 1.0f / data.evsmExponents.x, 1.0f / data.evsmExponents.y));
            BlueNoise.BindDitheredTextureSetCompute(context.cmd, data.ditheredTextureSet, data.computeShader, data.raymarchScreenSpaceShadowsKernel);
            BlueNoise.BindDitheredTextureSetCompute(context.cmd, data.ditheredTextureSet, data.computeShader, data.bilateralBlurKernel);
            BlueNoise.BindDitheredTextureSetCompute(context.cmd, data.ditheredTextureSet, data.computeShader, data.bilateralUpsampleKernel);


            TextureHandle hvsssBufferSource = data.outputHVSSSBufferA;
            TextureHandle hvsssBufferDestination = data.outputHVSSSBufferB;
            for (int lod = data.lodMax; lod >= (data.lodRaymarchMin == 0 ? 0 : 1); --lod)
            {
                int lodRaymarchBiased = Mathf.Clamp(lod + data.lodRaymarchBias, 0, data.lodMax);
                if (lod >= data.lodRaymarchMin)
                {
                    context.cmd.SetComputeTextureParam(data.computeShader, data.raymarchScreenSpaceShadowsKernel, HDShaderIDs._DepthMomentsMipChain, data.inputDepthMomentsPyramid);
                    context.cmd.SetComputeTextureParam(data.computeShader, data.raymarchScreenSpaceShadowsKernel, HDShaderIDs._HVSSSDestinationTexture, hvsssBufferSource);
                    context.cmd.SetComputeTextureParam(data.computeShader, data.raymarchScreenSpaceShadowsKernel, HDShaderIDs._HVSSSSourceTexture, TextureXR.GetWhiteTexture());

                    Vector2Int raymarchSrcSize = data.mipInfo.mipLevelSizes[lodRaymarchBiased];
                    Vector2Int raymarchSrcOffset = data.mipInfo.mipLevelOffsets[lodRaymarchBiased];
                    Vector2Int raymarchSrcLimit = raymarchSrcOffset + raymarchSrcSize - Vector2Int.one;

                    Vector2Int raymarchDstSize = data.mipInfo.mipLevelSizes[Mathf.Max(0, lod)];
                    Vector2Int raymarchDstOffset = data.mipInfo.mipLevelOffsets[Mathf.Max(0, lod)];
                    Vector2Int raymarchDstLimit = raymarchDstOffset + raymarchDstSize - Vector2Int.one;

                    context.cmd.SetComputeVectorParam(data.computeShader, HDShaderIDs._SrcOffsetAndLimit, new Vector4(raymarchSrcOffset.x, raymarchSrcOffset.y, raymarchSrcLimit.x, raymarchSrcLimit.y));
                    context.cmd.SetComputeVectorParam(data.computeShader, HDShaderIDs._DstOffsetAndLimit, new Vector4(raymarchDstOffset.x, raymarchDstOffset.y, raymarchDstLimit.x, raymarchDstLimit.y));

                    {
                        // Max raymarch distance is the diagonal of our fullscreen buffer.
                        float surfaceToLightDistancePixelsMax = Mathf.Sqrt(data.width * data.width + data.height * data.height) * 1.0f;
#if false
                        // Partition max distance into log2 cascades
                        // log2(x + 1) / log2(surfaceToLightDistancePixelsMax + 1) -> normalized [0, 1] log range across all cascades
                        // log2(cascadeRaymarchDistanceStart + 1) / log2(surfaceToLightDistancePixelsMax + 1) = cascadeRaymarchDistanceStartLog2Normalized
                        // log2(cascadeRaymarchDistanceStart + 1) = cascadeRaymarchDistanceStartLog2Normalized * log2(surfaceToLightDistancePixelsMax + 1)
                        // cascadeRaymarchDistanceStart + 1 = exp2(cascadeRaymarchDistanceStartLog2Normalized * log2(surfaceToLightDistancePixelsMax + 1))
                        // cascadeRaymarchDistanceStart = exp2(cascadeRaymarchDistanceStartLog2Normalized * log2(surfaceToLightDistancePixelsMax + 1)) - 1

                        // Closed form version.
                        float cascadeRaymarchDistanceStartLog2Normalized = (float)(lod - data.lodRaymarchMin) / (float)((data.lodMax - data.lodRaymarchMin) + 1);
                        float cascadeRaymarchDistanceEndLog2Normalized = (float)(lod - data.lodRaymarchMin + 1) / (float)((data.lodMax - data.lodRaymarchMin) + 1);
                        float cascadeRaymarchDistanceStart = Mathf.Pow(2.0f, cascadeRaymarchDistanceStartLog2Normalized * Mathf.Log(surfaceToLightDistancePixelsMax + 1.0f, 2.0f)) - 1.0f;
                        float cascadeRaymarchDistanceEnd = Mathf.Pow(2.0f, cascadeRaymarchDistanceEndLog2Normalized * Mathf.Log(surfaceToLightDistancePixelsMax + 1.0f, 2.0f)) - 1.0f;
#else
                        // Iterative calculation version. Supports cascade overlap.
                        float cascadeOverlapRatio = 0.25f;
                        int sampleCountPerCascade = data.raymarchLODSampleCount;
                        float cascadeRaymarchDistanceStart = 0.0f;
                        for (int i = data.lodRaymarchMin; i < lod; ++i)
                        {
                            cascadeRaymarchDistanceStart += sampleCountPerCascade * (1.0f - cascadeOverlapRatio) * Mathf.Pow(2.0f, i);
                        }
                        float cascadeRaymarchDistanceEnd = cascadeRaymarchDistanceStart + sampleCountPerCascade * Mathf.Pow(2.0f, lod);
#endif
                        cascadeRaymarchDistanceStart /= 1 << lodRaymarchBiased;
                        cascadeRaymarchDistanceEnd /= 1 << lodRaymarchBiased;

                        context.cmd.SetComputeVectorParam(data.computeShader, HDShaderIDs._HVSSSCascadeRaymarchDistancePixelsStartEnd, new Vector4(cascadeRaymarchDistanceStart, cascadeRaymarchDistanceEnd, 0.0f, 0.0f));
                    }

                    for (int channel = 0; channel < data.screenSpaceShadowCount; ++channel)
                    {
                        context.cmd.SetComputeVectorParam(data.computeShader, HDShaderIDs._LODCurrentAndNextAndMax, new Vector4(lod, lod, data.lodMax, channel));
                        context.cmd.SetComputeVectorParam(data.computeShader, HDShaderIDs._HVSSSRaymarchLODBiasMinMax, new Vector4(data.lodRaymarchBias, data.lodRaymarchMin, data.lodMax, 0.0f));
                        context.cmd.SetComputeVectorParam(data.computeShader, HDShaderIDs._HVSSSLightPositionWS, new Vector4(data.lightPositionsWS[channel].x, data.lightPositionsWS[channel].y, data.lightPositionsWS[channel].z, data.lightRanges[channel]));
                        context.cmd.DispatchCompute(data.computeShader, data.raymarchScreenSpaceShadowsKernel, HDUtils.DivRoundUp(raymarchDstSize.x, 8), HDUtils.DivRoundUp(raymarchDstSize.y, 8), ((RenderTexture)hvsssBufferSource).volumeDepth);
                    }
                }

                Vector2Int dstSize = data.mipInfo.mipLevelSizes[Mathf.Max(0, lod - 1)];
                Vector2Int dstOffset = data.mipInfo.mipLevelOffsets[Mathf.Max(0, lod - 1)];
                Vector2Int dstLimit = dstOffset + dstSize - Vector2Int.one;
                Vector2Int srcSize = data.mipInfo.mipLevelSizes[lod];
                Vector2Int srcOffset = data.mipInfo.mipLevelOffsets[lod];
                Vector2Int srcLimit = srcOffset + srcSize - Vector2Int.one;

                context.cmd.SetComputeVectorParam(data.computeShader, HDShaderIDs._SrcOffsetAndLimit, new Vector4(srcOffset.x, srcOffset.y, srcLimit.x, srcLimit.y));
                context.cmd.SetComputeVectorParam(data.computeShader, HDShaderIDs._DstOffsetAndLimit, new Vector4(dstOffset.x, dstOffset.y, dstLimit.x, dstLimit.y));

                {
                    context.cmd.SetComputeTextureParam(data.computeShader, data.bilateralBlurKernel, HDShaderIDs._DepthMomentsMipChain, data.inputDepthMomentsPyramid);

                    for (int blurAxis = 0; blurAxis < 2; ++blurAxis)
                    {
                        context.cmd.SetComputeVectorParam(data.computeShader, HDShaderIDs._LODCurrentAndNextAndMax, new Vector4(lod, lod, data.lodMax, blurAxis));

                        context.cmd.SetComputeTextureParam(data.computeShader, data.bilateralBlurKernel, HDShaderIDs._HVSSSSourceTexture, (blurAxis == 0) ? hvsssBufferSource : hvsssBufferDestination);
                        context.cmd.SetComputeTextureParam(data.computeShader, data.bilateralBlurKernel, HDShaderIDs._HVSSSDestinationTexture, (blurAxis == 0) ? hvsssBufferDestination : hvsssBufferSource);

                        context.cmd.DispatchCompute(data.computeShader, data.bilateralBlurKernel, HDUtils.DivRoundUp(srcSize.x, 8), HDUtils.DivRoundUp(srcSize.y, 8), ((RenderTexture)hvsssBufferSource).volumeDepth);
                    }
                }

                if (lod > 0)
                {
                    context.cmd.SetComputeTextureParam(data.computeShader, data.bilateralUpsampleKernel, HDShaderIDs._DepthMomentsMipChain, data.inputDepthMomentsPyramid);
                    context.cmd.SetComputeTextureParam(data.computeShader, data.bilateralUpsampleKernel, HDShaderIDs._HVSSSSourceTexture, hvsssBufferSource);
                    context.cmd.SetComputeTextureParam(data.computeShader, data.bilateralUpsampleKernel, HDShaderIDs._HVSSSDestinationTexture, hvsssBufferDestination);

                    context.cmd.SetComputeVectorParam(data.computeShader, HDShaderIDs._LODCurrentAndNextAndMax, new Vector4(lod, lod - 1, data.lodMax, 0));
                    context.cmd.DispatchCompute(data.computeShader, data.bilateralUpsampleKernel, HDUtils.DivRoundUp(dstSize.x, 8), HDUtils.DivRoundUp(dstSize.y, 8), ((RenderTexture)hvsssBufferSource).volumeDepth);
                }

                (hvsssBufferSource, hvsssBufferDestination) = (hvsssBufferDestination, hvsssBufferSource);
            }
        }
    }
}
