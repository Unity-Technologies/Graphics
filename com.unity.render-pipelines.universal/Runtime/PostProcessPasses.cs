using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public class SetExposurePass : ScriptableRenderPass
    {
        public static readonly int _ExposureTexture = Shader.PropertyToID("_ExposureTexture");
        public static readonly int _PrevExposureTexture = Shader.PropertyToID("_PrevExposureTexture");

        public SetExposurePass(RenderPassEvent evt)
        {
            base.profilingSampler = new ProfilingSampler(nameof(SetExposurePass));
            renderPassEvent = evt;
            overrideCameraTarget = true;
            base.useNativeRenderPass = false;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.SetGlobalTexture(_ExposureTexture,
                renderingData.cameraData.frameCache.m_ExposureTexture
                    ? renderingData.cameraData.frameCache.m_ExposureTexture
                    : Texture2D.whiteTexture);

            cmd.SetGlobalTexture(_PrevExposureTexture,
                renderingData.cameraData.frameCache.m_PrevExposureTexture
                    ? renderingData.cameraData.frameCache.m_PrevExposureTexture
                    : Texture2D.whiteTexture);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Cleanup()
        {
        }
    }

    public class CalculateExposurePass : ScriptableRenderPass
    {
       // RenderTargetHandle m_ExposureTextureHandle;
        ComputeShader m_ExposureComputeShader;

        // public static readonly int _PreviousExposureTexture = Shader.PropertyToID("_PreviousExposureTexture");


        private static void GenerateSimpleExposureTexture(ExposureMode mode, float compensation, float exposure, SRPPhysicalCamera camera, CommandBuffer cmd, ComputeShader exposureShader, RenderTexture rth)
        {
            if (exposureShader == null)
                return;

            int kernel = 0;
            Vector4 exposureParams;
            Vector4 exposureParams2 = new Vector4(0.0f, 0.0f, ColorUtils.lensImperfectionExposureScale, ColorUtils.s_LightMeterCalibrationConstant);

            if (mode == ExposureMode.Fixed)
            {
                kernel = exposureShader.FindKernel("KFixedExposure");
                exposureParams = new Vector4(compensation /*+ m_DebugExposureCompensation*/, exposure, 0f, 0f);
            }
            else // ExposureMode.UsePhysicalCamera
            {
                kernel = exposureShader.FindKernel("KManualCameraExposure");
                exposureParams = new Vector4(compensation, camera.aperture, camera.shutterSpeed, camera.iso);
            }

            cmd.SetComputeVectorParam(exposureShader, ShaderConstants._ExposureParams, exposureParams);
            cmd.SetComputeVectorParam(exposureShader, ShaderConstants._ExposureParams2, exposureParams2);

            cmd.SetComputeTextureParam(exposureShader, kernel, ShaderConstants._OutputTexture, rth);
            cmd.DispatchCompute(exposureShader, kernel, 1, 1, 1);
        }

        readonly RenderTextureDescriptor rtd = new()
        {
            width = 1,
            height = 1,
            msaaSamples = 1,
            volumeDepth = 1,
            mipCount = 0,
            graphicsFormat = GraphicsFormat.R32G32_SFloat,
            sRGB = false,
            depthBufferBits = 0,
            dimension = TextureDimension.Tex2D,
            vrUsage = VRTextureUsage.None,
            memoryless = RenderTextureMemoryless.None,
            useMipMap = false,
            autoGenerateMips = false,
            enableRandomWrite = true,
            bindMS = false,
            useDynamicScale = false
        };

        public CalculateExposurePass(RenderPassEvent evt, PostProcessData data)
        {
            base.profilingSampler = new ProfilingSampler(nameof(CalculateExposurePass));
            renderPassEvent = evt;
            overrideCameraTarget = true;

            m_ExposureComputeShader = data.shaders.exposureComputeShader;

            base.useNativeRenderPass = false;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // if we have no exposure... do nothing.
            if (renderingData.cameraData.exposure == null
                && !renderingData.cameraData.shouldOverrideExposure)
                return;

            //renderingData.cameraData.frameCache.NewExposureFrame();

            if (renderingData.cameraData.frameCache.m_ExposureTexture == null)
            {
                renderingData.cameraData.frameCache.m_ExposureTexture = new RenderTexture(rtd);
                renderingData.cameraData.frameCache.m_PrevExposureTexture = new RenderTexture(rtd);
                renderingData.cameraData.frameCache.m_ExposureComputeShader = Object.Instantiate(m_ExposureComputeShader);
            }

            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.SetGlobalTexture(SetExposurePass._ExposureTexture,Texture2D.whiteTexture);
            cmd.SetGlobalTexture(SetExposurePass._PrevExposureTexture,Texture2D.whiteTexture);

            // if we have overriddend exposure
            if(renderingData.cameraData.shouldOverrideExposure == true)
            {
                GenerateSimpleExposureTexture(
                    ExposureMode.Fixed,
                    0,
                    renderingData.cameraData.overrideExposureValue,
                    renderingData.cameraData.physicalParameters,
                    cmd,
                    renderingData.cameraData.frameCache.m_ExposureComputeShader,
                    renderingData.cameraData.frameCache.m_ExposureTexture);
            }
            else
            {
                //if we have a 'simple' exposure
                switch (renderingData.cameraData.exposure.mode.value)
                {
                    case ExposureMode.Fixed:
                    case ExposureMode.UsePhysicalCamera:
                        GenerateSimpleExposureTexture(
                            renderingData.cameraData.exposure.mode.value,
                            renderingData.cameraData.exposure.compensation.value,
                            renderingData.cameraData.exposure.fixedExposure.value,
                            renderingData.cameraData.physicalParameters,
                            cmd,
                            renderingData.cameraData.frameCache.m_ExposureComputeShader,
                            renderingData.cameraData.frameCache.m_ExposureTexture);
                        break;
                    case ExposureMode.Automatic:
                    case ExposureMode.AutomaticHistogram:
                    case ExposureMode.CurveMapping:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Unknown Exposure Mode");
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <inheritdoc/>
        public override void OnFinishCameraStackRendering(CommandBuffer cmd)
        { }

        public void Cleanup()
        { }

        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        static class ShaderConstants
        {
            public static  readonly int _ExposureParams                 = Shader.PropertyToID("_ExposureParams");
            public static readonly int _ExposureParams2                = Shader.PropertyToID("_ExposureParams2");
            public static readonly int _OutputTexture                  = Shader.PropertyToID("_OutputTexture");
        }
    }

    /// <summary>
    /// Type acts as wrapper for post process passes. Can we be recreated and destroyed at any point during runtime with post process data.
    /// </summary>
    internal struct PostProcessPasses : IDisposable
    {
        ColorGradingLutPass m_ColorGradingLutPass;
        PostProcessPass m_PostProcessPass;
        PostProcessPass m_FinalPostProcessPass;
        CalculateExposurePass m_CalculateExposurePass;
        SetExposurePass m_SetExposurePass;

        RenderTargetHandle m_AfterPostProcessColor;
        RenderTargetHandle m_ColorGradingLut;

        PostProcessData m_RendererPostProcessData;
        PostProcessData m_CurrentPostProcessData;
        Material m_BlitMaterial;

        public ColorGradingLutPass colorGradingLutPass { get => m_ColorGradingLutPass; }
        public PostProcessPass postProcessPass { get => m_PostProcessPass; }
        public PostProcessPass finalPostProcessPass { get => m_FinalPostProcessPass; }
        public CalculateExposurePass calculateExposurePass { get => m_CalculateExposurePass; }

        public SetExposurePass setExposurePass { get => m_SetExposurePass; }
        public RenderTargetHandle afterPostProcessColor { get => m_AfterPostProcessColor; }
        public RenderTargetHandle colorGradingLut { get => m_ColorGradingLut; }

        public bool isCreated { get => m_CurrentPostProcessData != null; }

        public PostProcessPasses(PostProcessData rendererPostProcessData, Material blitMaterial)
        {
            m_ColorGradingLutPass = null;
            m_PostProcessPass = null;
            m_FinalPostProcessPass = null;
            m_CalculateExposurePass = null;
            m_SetExposurePass = null;
            m_AfterPostProcessColor = new RenderTargetHandle();
            m_ColorGradingLut = new RenderTargetHandle();
            m_CurrentPostProcessData = null;

            m_AfterPostProcessColor.Init("_AfterPostProcessTexture");
            m_ColorGradingLut.Init("_InternalGradingLut");

            m_RendererPostProcessData = rendererPostProcessData;
            m_BlitMaterial = blitMaterial;

            Recreate(rendererPostProcessData);
        }

        /// <summary>
        /// Recreates post process passes with supplied data. If already contains valid post process passes, they will be replaced by new ones.
        /// </summary>
        /// <param name="data">Resources used for creating passes. In case of the null, no passes will be created.</param>
        public void Recreate(PostProcessData data)
        {
            if (m_RendererPostProcessData)
                data = m_RendererPostProcessData;

            if (data == m_CurrentPostProcessData)
                return;

            if (m_CurrentPostProcessData != null)
            {
                m_ColorGradingLutPass?.Cleanup();
                m_PostProcessPass?.Cleanup();
                m_FinalPostProcessPass?.Cleanup();
                m_CalculateExposurePass?.Cleanup();
                m_SetExposurePass?.Cleanup();

                // We need to null post process passes to avoid using them
                m_ColorGradingLutPass = null;
                m_PostProcessPass = null;
                m_FinalPostProcessPass = null;
                m_CurrentPostProcessData = null;
                m_CalculateExposurePass = null;
                m_SetExposurePass = null;
            }

            if (data != null)
            {
                m_ColorGradingLutPass = new ColorGradingLutPass(RenderPassEvent.BeforeRenderingPrePasses, data);
                m_PostProcessPass = new PostProcessPass(RenderPassEvent.BeforeRenderingPostProcessing, data, m_BlitMaterial);
                m_FinalPostProcessPass = new PostProcessPass(RenderPassEvent.AfterRenderingPostProcessing, data, m_BlitMaterial);
                m_CalculateExposurePass = new CalculateExposurePass(RenderPassEvent.AfterRenderingTransparents, data);
                m_SetExposurePass = new SetExposurePass(RenderPassEvent.BeforeRendering);
                m_CurrentPostProcessData = data;
            }
        }

        public void Dispose()
        {
            // always dispose unmanaged resources
            m_ColorGradingLutPass?.Cleanup();
            m_PostProcessPass?.Cleanup();
            m_FinalPostProcessPass?.Cleanup();
            m_CalculateExposurePass?.Cleanup();
            m_SetExposurePass?.Cleanup();
        }
    }
}
