using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesReBlur
    {
        public float4 _ReBlurPreBlurRotator;

        public float4 _ReBlurBlurRotator;

        public float4 _ReBlurPostBlurRotator;

        public float4 _HistorySizeAndScale;

        public Vector2 _ReBlurPadding;
        public float _ReBlurDenoiserRadius;
        public float _ReBlurAntiFlickeringStrength;

        public float _ReBlurHistoryValidity;
        public float _PaddingRBD0;
        public float _PaddingRBD1;
        public float _PaddingRBD2;
    }

    class ReBlurDenoiser
    {
        // Random numbers used to do the local rotations for the blur passes
        public static readonly float[] k_PreBlurRands = new float[] { 0.840188f, 0.394383f, 0.783099f, 0.79844f, 0.911647f, 0.197551f, 0.335223f, 0.76823f, 0.277775f, 0.55397f, 0.477397f, 0.628871f, 0.364784f, 0.513401f, 0.95223f, 0.916195f, 0.635712f, 0.717297f, 0.141603f, 0.606969f, 0.0163006f, 0.242887f, 0.137232f, 0.804177f, 0.156679f, 0.400944f, 0.12979f, 0.108809f, 0.998924f, 0.218257f, 0.512932f, 0.839112f};
        public static readonly float[] k_BlurRands = new float[] { 0.61264f, 0.296032f, 0.637552f, 0.524287f, 0.493583f, 0.972775f, 0.292517f, 0.771358f, 0.526745f, 0.769914f, 0.400229f, 0.891529f, 0.283315f, 0.352458f, 0.807725f, 0.919026f, 0.0697553f, 0.949327f, 0.525995f, 0.0860558f, 0.192214f, 0.663227f, 0.890233f, 0.348893f, 0.0641713f, 0.020023f, 0.457702f, 0.0630958f, 0.23828f, 0.970634f, 0.902208f, 0.85092f};
        public static readonly float[] k_PostBlurRands = new float[] { 0.266666f, 0.53976f, 0.375207f, 0.760249f, 0.512535f, 0.667724f, 0.531606f, 0.0392803f, 0.437638f, 0.931835f, 0.93081f, 0.720952f, 0.284293f, 0.738534f, 0.639979f, 0.354049f, 0.687861f, 0.165974f, 0.440105f, 0.880075f, 0.829201f, 0.330337f, 0.228968f, 0.893372f, 0.35036f, 0.68667f, 0.956468f, 0.58864f, 0.657304f, 0.858676f, 0.43956f, 0.92397f};

        // Compute shaders and kernels
        ComputeShader m_PreBlurCS;
        int m_PreBlurKernel;

        ComputeShader m_TemporalAccumulationCS;
        int m_TemporalAccumulationKernel;

        ComputeShader m_MipGenerationCS;
        int m_MipGenerationKernel;
        int m_CopyMipKernel;

        ComputeShader m_HistoryFixCS;
        int m_HistoryFixKernel;

        ComputeShader m_BlurCS;
        int m_BlurKernel;

        ComputeShader m_PostBlurCS;
        int m_PostBlurKernel;

        ComputeShader m_CopyHistoryCS;
        int m_CopyHistoryAccumulationKernel;
        int m_CopyHistoryKernel;

        ComputeShader m_TemporalStabilizationCS;
        int m_TemporalStabilizationKernel;

        // Shader IDs
        public static readonly int _ShaderVariablesReBlur = Shader.PropertyToID("ShaderVariablesReBlur");
        public static readonly int _TargetMipLevel = Shader.PropertyToID("_TargetMipLevel");
        public static readonly int _ReBlurMipChain = Shader.PropertyToID("_ReBlurMipChain");

        public static readonly int _LightingDistanceTexture = Shader.PropertyToID("_LightingDistanceTexture");
        public static readonly int _LightingDistanceTextureRW = Shader.PropertyToID("_LightingDistanceTextureRW");
        public static readonly int _AccumulationTexture = Shader.PropertyToID("_AccumulationTexture");
        public static readonly int _AccumulationTextureRW = Shader.PropertyToID("_AccumulationTextureRW");

        public static readonly int _LightingDistanceHistoryBuffer = Shader.PropertyToID("_LightingDistanceHistoryBuffer");
        public static readonly int _AccumulationHistoryBuffer = Shader.PropertyToID("_AccumulationHistoryBuffer");
        public static readonly int _StabilizationHistoryBuffer = Shader.PropertyToID("_StabilizationHistoryBuffer");

        public ReBlurDenoiser()
        {
        }

        public void Init(HDRPRayTracingResources rpRTResources)
        {
            // PreBlur
            m_PreBlurCS = rpRTResources.reblurPreBlurCS;
            m_PreBlurKernel = m_PreBlurCS.FindKernel("PreBlur");

            // Temporal Accumulation
            m_TemporalAccumulationCS = rpRTResources.reblurTemporalAccumulationCS;
            m_TemporalAccumulationKernel = m_TemporalAccumulationCS.FindKernel("TemporalAccumulation");

            // MIP Generation
            m_MipGenerationCS = rpRTResources.reblurMipGenerationCS;
            m_MipGenerationKernel = m_MipGenerationCS.FindKernel("MipGeneration");
            m_CopyMipKernel = m_MipGenerationCS.FindKernel("CopyMip");

            // History Fix
            m_HistoryFixCS = rpRTResources.reblurHistoryFixCS;
            m_HistoryFixKernel = m_HistoryFixCS.FindKernel("HistoryFix");

            // Blur
            m_BlurCS = rpRTResources.reblurBlurCS;
            m_BlurKernel = m_BlurCS.FindKernel("Blur");

            // Post Blur
            m_PostBlurCS = rpRTResources.reblurPostBlurCS;
            m_PostBlurKernel = m_PostBlurCS.FindKernel("PostBlur");

            // Copy History
            m_CopyHistoryCS = rpRTResources.reblurCopyHistoryCS;
            m_CopyHistoryAccumulationKernel = m_CopyHistoryCS.FindKernel("CopyHistoryAccumulation");
            m_CopyHistoryKernel = m_CopyHistoryCS.FindKernel("CopyHistory");

            // Temporal Stabilization
            m_TemporalStabilizationCS = rpRTResources.reblurTemporalStabilizationCS;
            m_TemporalStabilizationKernel = m_TemporalStabilizationCS.FindKernel("TemporalStabilization");
        }

        public void Release()
        {
        }

        class ReblurIndirectSpecularPassData
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Denoiser parameters
            public bool fullResolution;
            public float historyValidity;
            public Vector4 historySizeAndScale;

            // Constant buffer
            public ShaderVariablesReBlur reblurCB;

            // Compute shaders and kernels
            public ComputeShader preBlurCS;
            public int preBlurKernel;
            public ComputeShader temporalAccumulationCS;
            public int temporalAccumulationKernel;
            public ComputeShader mipGenerationCS;
            public int mipGenerationKernel;
            public int copyMipKernel;
            public ComputeShader historyFixCS;
            public int historyFixKernel;
            public ComputeShader blurCS;
            public int blurKernel;
            public ComputeShader postBlurCS;
            public int postBlurKernel;
            public ComputeShader copyHistoryCS;
            public int copyHistoryAccumulationKernel;
            public int copyHistoryKernel;
            public ComputeShader temporalStabilizationCS;
            public int temporalStabilizationKernel;

            // Input resources
            public TextureHandle depthBuffer;
            public TextureHandle depthPyramidBuffer;
            public TextureHandle stencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle motionVectorBuffer;
            public TextureHandle clearCoatTexture;
            public TextureHandle historyValidation;
            public TextureHandle distanceTexture;

            // Temp resources
            public TextureHandle accTexture;
            public TextureHandle mipTexture;
            public TextureHandle tmpTexture;

            // History
            public TextureHandle mainHistory;
            public TextureHandle accumulationHistory;
            public TextureHandle stabilizationHistory;
            public TextureHandle historyDepth;

            // In/out buffer
            public TextureHandle lightingTexture;
        }

        float4 EvaluateRotator(float rand)
        {
            float ca = Mathf.Cos(rand);
            float sa = Mathf.Sin(rand);
            return new float4(ca, sa, -sa, ca);
        }

        static void GenerateMipLevels(CommandBuffer cmd, ReblurIndirectSpecularPassData data)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ReBlurMipGeneration)))
            {
                // Evaluate the dispatch parameters
                int tileSize = 8;
                int numTilesX = (data.texWidth + (tileSize - 1)) / tileSize;
                int numTilesY = (data.texHeight + (tileSize - 1)) / tileSize;

                // Mip0 is copied as is.
                cmd.SetComputeIntParam(data.mipGenerationCS, _TargetMipLevel, 0);
                cmd.SetComputeTextureParam(data.mipGenerationCS, data.copyMipKernel, _LightingDistanceTexture, data.lightingTexture);
                cmd.SetComputeTextureParam(data.mipGenerationCS, data.copyMipKernel, _LightingDistanceTextureRW, data.mipTexture, 0);
                cmd.DispatchCompute(data.mipGenerationCS, data.copyMipKernel, numTilesX, numTilesY, data.viewCount);

                // Mip1 is generated
                int numTilesX_1 = (data.texWidth / 2 + (tileSize - 1)) / tileSize;
                int numTilesY_1 = (data.texHeight / 2 + (tileSize - 1)) / tileSize;
                cmd.SetComputeTextureParam(data.mipGenerationCS, data.mipGenerationKernel, _LightingDistanceTexture, data.lightingTexture);
                cmd.SetComputeTextureParam(data.mipGenerationCS, data.mipGenerationKernel, HDShaderIDs._DepthTexture, data.depthPyramidBuffer);
                cmd.SetComputeTextureParam(data.mipGenerationCS, data.mipGenerationKernel, _LightingDistanceTextureRW, data.mipTexture, 1);
                cmd.DispatchCompute(data.mipGenerationCS, data.mipGenerationKernel, numTilesX_1, numTilesY_1, data.viewCount);

                // Mip2 is generated
                int numTilesX_2 = (data.texWidth / 4 + (tileSize - 1)) / tileSize;
                int numTilesY_2 = (data.texHeight / 4 + (tileSize - 1)) / tileSize;
                cmd.SetComputeIntParam(data.mipGenerationCS, _TargetMipLevel, 1);
                cmd.SetComputeTextureParam(data.mipGenerationCS, data.mipGenerationKernel, _LightingDistanceTexture, data.mipTexture);
                cmd.SetComputeTextureParam(data.mipGenerationCS, data.mipGenerationKernel, HDShaderIDs._DepthTexture, data.depthPyramidBuffer);
                cmd.SetComputeTextureParam(data.mipGenerationCS, data.mipGenerationKernel, _LightingDistanceTextureRW, data.tmpTexture, 2);
                cmd.DispatchCompute(data.mipGenerationCS, data.mipGenerationKernel, numTilesX_2, numTilesY_2, data.viewCount);

                // Copy the mip
                cmd.SetComputeIntParam(data.mipGenerationCS, _TargetMipLevel, 2);
                cmd.SetComputeTextureParam(data.mipGenerationCS, data.copyMipKernel, _LightingDistanceTexture, data.tmpTexture);
                cmd.SetComputeTextureParam(data.mipGenerationCS, data.copyMipKernel, _LightingDistanceTextureRW, data.mipTexture, 2);
                cmd.DispatchCompute(data.mipGenerationCS, data.copyMipKernel, numTilesX_2, numTilesY_2, data.viewCount);

                // Mip3 is generated
                int numTilesX_3 = (data.texWidth / 8 + (tileSize - 1)) / tileSize;
                int numTilesY_3 = (data.texHeight / 8 + (tileSize - 1)) / tileSize;
                cmd.SetComputeIntParam(data.mipGenerationCS, _TargetMipLevel, 2);
                cmd.SetComputeTextureParam(data.mipGenerationCS, data.mipGenerationKernel, _LightingDistanceTexture, data.tmpTexture);
                cmd.SetComputeTextureParam(data.mipGenerationCS, data.mipGenerationKernel, HDShaderIDs._DepthTexture, data.depthPyramidBuffer);
                cmd.SetComputeTextureParam(data.mipGenerationCS, data.mipGenerationKernel, _LightingDistanceTextureRW, data.mipTexture, 3);
                cmd.DispatchCompute(data.mipGenerationCS, data.mipGenerationKernel, numTilesX_3, numTilesY_3, data.viewCount);
            }
        }

        public TextureHandle DenoiseIndirectSpecular(RenderGraph renderGraph, HDCamera hdCamera, bool fullResolution, float historyValidity, float denoiserRadius, float antiFlickeringStrength,
            in HDRenderPipeline.PrepassOutput prepassOutput, TextureHandle clearCoatTexture,
            TextureHandle historyValidation,
            TextureHandle lightingTexture, TextureHandle distanceTexture,
            RTHandle mainHistory, RTHandle accumulationHistory, RTHandle stabilizationHistory)
        {
            using (var builder = renderGraph.AddRenderPass<ReblurIndirectSpecularPassData>("ReBlur Indirect Specular", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingReflectionFilter)))
            {
                builder.EnableAsyncCompute(false);

                // Camera parameters
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Denoiser parameters
                passData.fullResolution = fullResolution;
                passData.historyValidity = historyValidity;

                // Generate the rotators
                uint frameIndex = hdCamera.GetCameraFrameCount() % 32;
                passData.reblurCB._ReBlurPreBlurRotator = EvaluateRotator(k_PreBlurRands[frameIndex]);
                passData.reblurCB._ReBlurBlurRotator = EvaluateRotator(k_BlurRands[frameIndex]);
                passData.reblurCB._ReBlurPostBlurRotator = EvaluateRotator(k_PostBlurRands[frameIndex]);
                passData.reblurCB._HistorySizeAndScale = HDRenderPipeline.EvaluateRayTracingHistorySizeAndScale(hdCamera, mainHistory);
                passData.reblurCB._ReBlurDenoiserRadius = Mathf.Lerp(0.5f, 1.0f, denoiserRadius);
                passData.reblurCB._ReBlurHistoryValidity = historyValidity;
                float minAntiflicker = 0.0f;
                float maxAntiflicker = 3.5f;
                passData.reblurCB._ReBlurAntiFlickeringStrength = Mathf.Lerp(minAntiflicker, maxAntiflicker, antiFlickeringStrength);

                // CS & Kernels
                passData.preBlurCS = m_PreBlurCS;
                passData.preBlurKernel = m_PreBlurKernel;

                passData.temporalAccumulationCS = m_TemporalAccumulationCS;
                passData.temporalAccumulationKernel = m_TemporalAccumulationKernel;

                passData.mipGenerationCS = m_MipGenerationCS;
                passData.mipGenerationKernel = m_MipGenerationKernel;
                passData.copyMipKernel = m_CopyMipKernel;

                passData.historyFixCS = m_HistoryFixCS;
                passData.historyFixKernel = m_HistoryFixKernel;

                passData.blurCS = m_BlurCS;
                passData.blurKernel = m_BlurKernel;

                passData.postBlurCS = m_PostBlurCS;
                passData.postBlurKernel = m_PostBlurKernel;

                passData.copyHistoryCS = m_CopyHistoryCS;
                passData.copyHistoryAccumulationKernel = m_CopyHistoryAccumulationKernel;
                passData.copyHistoryKernel = m_CopyHistoryKernel;

                passData.temporalStabilizationCS = m_TemporalStabilizationCS;
                passData.temporalStabilizationKernel = m_TemporalStabilizationKernel;

                // Input resources
                passData.lightingTexture = builder.ReadTexture(lightingTexture);
                passData.distanceTexture = builder.ReadTexture(distanceTexture);
                passData.depthBuffer = builder.ReadTexture(prepassOutput.depthBuffer);
                passData.depthPyramidBuffer = builder.ReadTexture(prepassOutput.depthPyramidTexture);
                passData.stencilBuffer = builder.ReadTexture(prepassOutput.stencilBuffer);
                passData.normalBuffer = builder.ReadTexture(prepassOutput.normalBuffer);
                passData.motionVectorBuffer = builder.ReadTexture(prepassOutput.resolvedMotionVectorsBuffer);
                passData.clearCoatTexture = builder.ReadTexture(clearCoatTexture);
                passData.historyValidation = builder.ReadTexture(historyValidation);
                var historyDepth = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
                passData.historyDepth = historyDepth != null ? builder.ReadTexture(renderGraph.ImportTexture(historyDepth)) : renderGraph.defaultResources.blackTextureXR;

                // Temporary textures
                passData.accTexture = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R8_UInt, enableRandomWrite = true, useMipMap = true, autoGenerateMips = false, name = "ReBlur Acc Texture" });
                passData.mipTexture = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, useMipMap = true, autoGenerateMips = false, name = "ReBlur Color Pyramid" });
                passData.tmpTexture = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, useMipMap = true, autoGenerateMips = false, name = "ReBlur Color Pyramid Bis" });

                // Output resources
                passData.mainHistory = builder.ReadWriteTexture(renderGraph.ImportTexture(mainHistory));
                passData.accumulationHistory = builder.ReadWriteTexture(renderGraph.ImportTexture(accumulationHistory));
                passData.stabilizationHistory = builder.ReadWriteTexture(renderGraph.ImportTexture(stabilizationHistory));

                builder.SetRenderFunc((ReblurIndirectSpecularPassData data, RenderGraphContext ctx) =>
                {
                    // Evaluate the dispatch parameters
                    int tileSize = 8;

                    // Tile count to dispatch
                    int numTilesX = (data.texWidth + (tileSize - 1)) / tileSize;
                    int numTilesY = (data.texHeight + (tileSize - 1)) / tileSize;

                    using (new ProfilingScope(ctx.cmd, ProfilingSampler.Get(HDProfileId.ReBlurPreBlur)))
                    {
                        // Set the half res keyword
                        CoreUtils.SetKeyword(ctx.cmd, "HALF_RESOLUTION", !data.fullResolution);

                        // Input data
                        ConstantBuffer.Push(ctx.cmd, data.reblurCB, data.preBlurCS, _ShaderVariablesReBlur);
                        ctx.cmd.SetComputeTextureParam(data.preBlurCS, data.preBlurKernel, HDShaderIDs._LightingInputTexture, data.lightingTexture);
                        ctx.cmd.SetComputeTextureParam(data.preBlurCS, data.preBlurKernel, HDShaderIDs._DistanceInputTexture, data.distanceTexture);
                        ctx.cmd.SetComputeTextureParam(data.preBlurCS, data.preBlurKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                        ctx.cmd.SetComputeTextureParam(data.preBlurCS, data.preBlurKernel, HDShaderIDs._StencilTexture, data.stencilBuffer, 0, RenderTextureSubElement.Stencil);
                        ctx.cmd.SetComputeTextureParam(data.preBlurCS, data.preBlurKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.preBlurCS, data.preBlurKernel, HDShaderIDs._ClearCoatMaskTexture, data.clearCoatTexture);

                        // Output texture
                        ctx.cmd.SetComputeTextureParam(data.preBlurCS, data.preBlurKernel, _LightingDistanceTextureRW, data.tmpTexture);

                        // Dispatch
                        ctx.cmd.DispatchCompute(data.preBlurCS, data.preBlurKernel, numTilesX, numTilesY, data.viewCount);

                        // Reset the half res keyword
                        CoreUtils.SetKeyword(ctx.cmd, "HALF_RESOLUTION", false);
                    }

                    using (new ProfilingScope(ctx.cmd, ProfilingSampler.Get(HDProfileId.ReBlurTemporalAccumulation)))
                    {
                        // Input CB
                        ConstantBuffer.Push(ctx.cmd, data.reblurCB, data.temporalAccumulationCS, _ShaderVariablesReBlur);

                        // Simplified GBuffer + History
                        ctx.cmd.SetComputeTextureParam(data.temporalAccumulationCS, data.temporalAccumulationKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalAccumulationCS, data.temporalAccumulationKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalAccumulationCS, data.temporalAccumulationKernel, HDShaderIDs._ClearCoatMaskTexture, data.clearCoatTexture);
                        ctx.cmd.SetComputeTextureParam(data.temporalAccumulationCS, data.temporalAccumulationKernel, HDShaderIDs._CameraMotionVectorsTexture, data.motionVectorBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalAccumulationCS, data.temporalAccumulationKernel, HDShaderIDs._HistoryDepthTexture, data.historyDepth);

                        // Input Data
                        ctx.cmd.SetComputeTextureParam(data.temporalAccumulationCS, data.temporalAccumulationKernel, _LightingDistanceTexture, data.tmpTexture);
                        ctx.cmd.SetComputeTextureParam(data.temporalAccumulationCS, data.temporalAccumulationKernel, HDShaderIDs._ValidationBuffer, data.historyValidation);

                        // History buffer
                        ctx.cmd.SetComputeTextureParam(data.temporalAccumulationCS, data.temporalAccumulationKernel, _LightingDistanceHistoryBuffer, data.mainHistory);
                        ctx.cmd.SetComputeTextureParam(data.temporalAccumulationCS, data.temporalAccumulationKernel, _AccumulationHistoryBuffer, data.accumulationHistory);

                        // Output texture
                        ctx.cmd.SetComputeTextureParam(data.temporalAccumulationCS, data.temporalAccumulationKernel, _LightingDistanceTextureRW, data.lightingTexture);
                        ctx.cmd.SetComputeTextureParam(data.temporalAccumulationCS, data.temporalAccumulationKernel, _AccumulationTextureRW, data.accTexture);

                        // Dispatch
                        ctx.cmd.DispatchCompute(data.temporalAccumulationCS, data.temporalAccumulationKernel, numTilesX, numTilesY, data.viewCount);
                    }

                    // Generate the mip levels required for the history fix.
                    GenerateMipLevels(ctx.cmd, data);

                    using (new ProfilingScope(ctx.cmd, ProfilingSampler.Get(HDProfileId.ReBlurMipHistoryFix)))
                    {
                        // Mini GBuffer
                        ctx.cmd.SetComputeTextureParam(data.historyFixCS, data.historyFixKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                        ctx.cmd.SetComputeTextureParam(data.historyFixCS, data.historyFixKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.historyFixCS, data.historyFixKernel, _ReBlurMipChain, data.mipTexture);

                        // Input Data
                        ctx.cmd.SetComputeTextureParam(data.historyFixCS, data.historyFixKernel, _LightingDistanceTexture, data.lightingTexture);
                        ctx.cmd.SetComputeTextureParam(data.historyFixCS, data.historyFixKernel, _AccumulationTexture, data.accTexture);

                        // Output texture
                        ctx.cmd.SetComputeTextureParam(data.historyFixCS, data.historyFixKernel, _LightingDistanceTextureRW, data.tmpTexture);

                        // Dispatch
                        ctx.cmd.DispatchCompute(data.historyFixCS, data.historyFixKernel, numTilesX, numTilesY, data.viewCount);
                    }

                    using (new ProfilingScope(ctx.cmd, ProfilingSampler.Get(HDProfileId.ReBlurBlur)))
                    {
                        ConstantBuffer.Push(ctx.cmd, data.reblurCB, data.blurCS, _ShaderVariablesReBlur);

                        // Mini GBuffer
                        ctx.cmd.SetComputeTextureParam(data.blurCS, data.blurKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                        ctx.cmd.SetComputeTextureParam(data.blurCS, data.blurKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.blurCS, data.blurKernel, HDShaderIDs._StencilTexture, data.stencilBuffer, 0, RenderTextureSubElement.Stencil);
                        ctx.cmd.SetComputeTextureParam(data.blurCS, data.blurKernel, HDShaderIDs._ClearCoatMaskTexture, data.clearCoatTexture);

                        // Input Data
                        ctx.cmd.SetComputeTextureParam(data.blurCS, data.blurKernel, _LightingDistanceTexture, data.tmpTexture);
                        ctx.cmd.SetComputeTextureParam(data.blurCS, data.blurKernel, _AccumulationTexture, data.accTexture);

                        // Output Data
                        ctx.cmd.SetComputeTextureParam(data.blurCS, data.blurKernel, _LightingDistanceTextureRW, data.lightingTexture);

                        // Dispatch
                        ctx.cmd.DispatchCompute(data.blurCS, data.blurKernel, numTilesX, numTilesY, data.viewCount);
                    }

                    using (new ProfilingScope(ctx.cmd, ProfilingSampler.Get(HDProfileId.ReBlurCopyHistory)))
                    {
                        // Current Data
                        ctx.cmd.SetComputeTextureParam(data.copyHistoryCS, data.copyHistoryAccumulationKernel, _LightingDistanceTexture, data.lightingTexture);
                        ctx.cmd.SetComputeTextureParam(data.copyHistoryCS, data.copyHistoryAccumulationKernel, _AccumulationTexture, data.accTexture);

                        // History buffers
                        ctx.cmd.SetComputeTextureParam(data.copyHistoryCS, data.copyHistoryAccumulationKernel, _LightingDistanceTextureRW, data.mainHistory);
                        ctx.cmd.SetComputeTextureParam(data.copyHistoryCS, data.copyHistoryAccumulationKernel, _AccumulationTextureRW, data.accumulationHistory);

                        // Dispatch
                        ctx.cmd.DispatchCompute(data.copyHistoryCS, data.copyHistoryAccumulationKernel, numTilesX, numTilesY, data.viewCount);
                    }

                    using (new ProfilingScope(ctx.cmd, ProfilingSampler.Get(HDProfileId.ReBlurTemporalStabilization)))
                    {
                        ConstantBuffer.Push(ctx.cmd, data.reblurCB, data.temporalStabilizationCS, _ShaderVariablesReBlur);

                        // Mini GBuffer
                        ctx.cmd.SetComputeTextureParam(data.temporalStabilizationCS, data.temporalStabilizationKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalStabilizationCS, data.temporalStabilizationKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalStabilizationCS, data.temporalStabilizationKernel, HDShaderIDs._CameraMotionVectorsTexture, data.motionVectorBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalStabilizationCS, data.temporalStabilizationKernel, HDShaderIDs._ValidationBuffer, data.historyValidation);
                        ctx.cmd.SetComputeTextureParam(data.temporalStabilizationCS, data.temporalStabilizationKernel, HDShaderIDs._StencilTexture, data.stencilBuffer, 0, RenderTextureSubElement.Stencil);

                        // Input
                        ctx.cmd.SetComputeTextureParam(data.temporalStabilizationCS, data.temporalStabilizationKernel, HDShaderIDs._DenoiseInputTexture, data.lightingTexture);
                        ctx.cmd.SetComputeTextureParam(data.temporalStabilizationCS, data.temporalStabilizationKernel, _StabilizationHistoryBuffer, data.stabilizationHistory);

                        // Output
                        ctx.cmd.SetComputeTextureParam(data.temporalStabilizationCS, data.temporalStabilizationKernel, HDShaderIDs._DenoiseOutputTextureRW, data.tmpTexture);

                        // Dispatch
                        ctx.cmd.DispatchCompute(data.temporalStabilizationCS, data.temporalStabilizationKernel, numTilesX, numTilesY, data.viewCount);
                    }

                    using (new ProfilingScope(ctx.cmd, ProfilingSampler.Get(HDProfileId.ReBlurCopyHistoryStab)))
                    {
                        ctx.cmd.SetComputeTextureParam(data.copyHistoryCS, data.copyHistoryKernel, _LightingDistanceTexture, data.tmpTexture);
                        ctx.cmd.SetComputeTextureParam(data.copyHistoryCS, data.copyHistoryKernel, _LightingDistanceTextureRW, data.stabilizationHistory);
                        ctx.cmd.DispatchCompute(data.copyHistoryCS, data.copyHistoryKernel, numTilesX, numTilesY, data.viewCount);
                    }

                    using (new ProfilingScope(ctx.cmd, ProfilingSampler.Get(HDProfileId.ReBlurPostBlur)))
                    {
                        ConstantBuffer.Push(ctx.cmd, data.reblurCB, data.postBlurCS, _ShaderVariablesReBlur);

                        // Mini GBuffer
                        ctx.cmd.SetComputeTextureParam(data.postBlurCS, data.postBlurKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                        ctx.cmd.SetComputeTextureParam(data.postBlurCS, data.postBlurKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.postBlurCS, data.postBlurKernel, HDShaderIDs._StencilTexture, data.stencilBuffer, 0, RenderTextureSubElement.Stencil);
                        ctx.cmd.SetComputeTextureParam(data.postBlurCS, data.postBlurKernel, HDShaderIDs._ClearCoatMaskTexture, data.clearCoatTexture);

                        // Input Data
                        ctx.cmd.SetComputeTextureParam(data.postBlurCS, data.postBlurKernel, _LightingDistanceTexture, data.tmpTexture);
                        ctx.cmd.SetComputeTextureParam(data.postBlurCS, data.postBlurKernel, _AccumulationTexture, data.accTexture);

                        // Output buffer
                        ctx.cmd.SetComputeTextureParam(data.postBlurCS, data.postBlurKernel, _LightingDistanceTextureRW, data.lightingTexture);

                        // Dispatch
                        ctx.cmd.DispatchCompute(data.postBlurCS, data.postBlurKernel, numTilesX, numTilesY, data.viewCount);
                    }
                });

                return lightingTexture;
            }
        }
    }
}
