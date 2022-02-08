using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    [DisallowMultipleRendererFeature]
    [Tooltip("Exposure Processing allows for various forms of pre-exposure to be applied.")]
    internal class ExposureProcessing : ScriptableRendererFeature
    {
        [SerializeField, HideInInspector] ComputeShader m_ExposureComputeShader;

        public static readonly int _ExposureTexture = Shader.PropertyToID("_ExposureTexture");
        public static readonly int _PrevExposureTexture = Shader.PropertyToID("_PrevExposureTexture");

        CalculateFixedExposurePass m_CalculateFixedExposurePass;
        //CalculateDyanamicExposurePass m_CalculateDynamicExposurePass;
        SetExposurePass m_SetExposurePass;
        AdvanceExposureFramePass m_AdvanceExposureFramePass;

        public override void Create()
        {
            m_CalculateFixedExposurePass ??= new CalculateFixedExposurePass(RenderPassEvent.BeforeRendering, m_ExposureComputeShader);
            m_SetExposurePass ??= new SetExposurePass(RenderPassEvent.BeforeRendering);
            m_AdvanceExposureFramePass ??= new AdvanceExposureFramePass(RenderPassEvent.BeforeRendering);
        }

        internal static void SetDefaultExposure()
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.SetGlobalTexture(_ExposureTexture, Texture2D.whiteTexture);

            cmd.SetGlobalTexture(_PrevExposureTexture, Texture2D.whiteTexture);
            Graphics.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // if we have no exposure... do nothing.
            if (renderingData.cameraData.exposure == null
                && !renderingData.cameraData.shouldOverrideExposure)
                return;

            if (renderingData.cameraData.exposure != null
                && !renderingData.cameraData.exposure.active
                && !renderingData.cameraData.shouldOverrideExposure)
                return;

            RenderTextureDescriptor rtd = new()
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

            if (renderingData.cameraData.frameCache.m_ExposureTexture == null)
            {
                renderingData.cameraData.frameCache.m_ExposureTexture = new RenderTexture(rtd);
                renderingData.cameraData.frameCache.m_PrevExposureTexture = new RenderTexture(rtd);
                renderingData.cameraData.frameCache.m_ExposureTexture.Create();
                renderingData.cameraData.frameCache.m_PrevExposureTexture.Create();
            }

            // Do fixed exposure...
            if(renderingData.cameraData.shouldOverrideExposure
                || (renderingData.cameraData.exposure != null
                    && (renderingData.cameraData.exposure.mode.value == ExposureMode.Fixed
                        || renderingData.cameraData.exposure.mode.value == ExposureMode.UsePhysicalCamera)))
            {
                // we can calculate fixed exposure before rendering for this camera... nice :)
                m_AdvanceExposureFramePass.renderPassEvent = RenderPassEvent.BeforeRendering;
                m_CalculateFixedExposurePass.renderPassEvent = RenderPassEvent.BeforeRendering;
                m_SetExposurePass.renderPassEvent = RenderPassEvent.BeforeRendering;
                renderer.EnqueuePass(m_AdvanceExposureFramePass);
                renderer.EnqueuePass(m_CalculateFixedExposurePass);
                renderer.EnqueuePass(m_SetExposurePass);
            }
            // Do dynamic exposure
            else if (renderingData.cameraData.exposure != null)
            {
                // dynamic exposure needs to be done at the end of a frame, but set at the start.
                m_SetExposurePass.renderPassEvent = RenderPassEvent.BeforeRendering;
                m_AdvanceExposureFramePass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
                m_CalculateFixedExposurePass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
                renderer.EnqueuePass(m_SetExposurePass);
                renderer.EnqueuePass(m_AdvanceExposureFramePass);
                renderer.EnqueuePass(m_CalculateFixedExposurePass);
            }
        }

        public class SetExposurePass : ScriptableRenderPass
        {
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
        }

        public class AdvanceExposureFramePass : ScriptableRenderPass
        {
            public AdvanceExposureFramePass(RenderPassEvent evt)
            {
                profilingSampler = new ProfilingSampler(nameof(AdvanceExposureFramePass));
                renderPassEvent = evt;
                overrideCameraTarget = true;
                useNativeRenderPass = false;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                renderingData.cameraData.frameCache.NewExposureFrame();
            }
        }

        public class CalculateFixedExposurePass : ScriptableRenderPass
        {
            // public static readonly int _PreviousExposureTexture = Shader.PropertyToID("_PreviousExposureTexture");

            private ComputeShader m_ExposureComputeShader;

            private static void GenerateSimpleExposureTexture(ExposureMode mode, float compensation, float exposure,
                Camera camera, CommandBuffer cmd, ComputeShader exposureShader, RenderTexture rth)
            {
                if (exposureShader == null)
                    return;

                int kernel = 0;
                Vector4 exposureParams;
                Vector4 exposureParams2 = new Vector4(0.0f, 0.0f, ColorUtils.lensImperfectionExposureScale,
                    ColorUtils.s_LightMeterCalibrationConstant);

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

            public CalculateFixedExposurePass(RenderPassEvent evt, ComputeShader exposureComputeShader)
            {
                base.profilingSampler = new ProfilingSampler(nameof(CalculateFixedExposurePass));
                renderPassEvent = evt;
                overrideCameraTarget = true;

                m_ExposureComputeShader = exposureComputeShader;

                base.useNativeRenderPass = false;
            }

            /// <inheritdoc/>
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get();

                // if we have overriddend exposure
                if (renderingData.cameraData.shouldOverrideExposure)
                {
                    GenerateSimpleExposureTexture(
                        ExposureMode.Fixed,
                        0,
                        renderingData.cameraData.overrideExposureValue,
                        renderingData.cameraData.camera,
                        cmd,
                        m_ExposureComputeShader,
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
                                renderingData.cameraData.camera,
                                cmd,
                                m_ExposureComputeShader,
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

            // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
            static class ShaderConstants
            {
                public static readonly int _ExposureParams = Shader.PropertyToID("_ExposureParams");
                public static readonly int _ExposureParams2 = Shader.PropertyToID("_ExposureParams2");
                public static readonly int _OutputTexture = Shader.PropertyToID("_OutputTexture");
            }
        }
    }
}
