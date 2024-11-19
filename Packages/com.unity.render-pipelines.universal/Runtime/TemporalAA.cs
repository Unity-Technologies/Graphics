using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.Universal
{
    // All of TAA here, work on TAA == work on this file.

    /// <summary>
    /// Temporal Anti-aliasing quality setting.
    /// </summary>
    public enum TemporalAAQuality
    {
        // Quality options were tuned to give meaningful performance differences on mobile hardware.

        /// <summary>
        /// 5-tap RGB clamp. No motion dilation. Suitable for no/low motion scenes.
        /// </summary>
        VeryLow = 0,

        /// <summary>
        /// 5-tap RGB clamp. 5-tap motion dilation.
        /// </summary>
        Low,

        /// <summary>
        /// 9-tap YCoCg variance clamp. 9-tap motion dilation.
        /// </summary>
        Medium,

        /// <summary>
        /// 9-tap YCoCg variance clamp and bicubic history.
        /// </summary>
        High,

        // VeryHigh is a catch all option for enabling all the implemented features regardless of cost.
        // Currently, 9-tap YCoCg variance clip, bicubic history and center sample filtering.
        // In the future, VeryHigh mode could read additional buffers to improve the quality further.

        /// <summary>
        /// Best quality, everything enabled.
        /// </summary>
        VeryHigh
    }

    /// <summary>
    /// Temporal anti-aliasing.
    /// </summary>
    public static class TemporalAA
    {
        internal static class ShaderConstants
        {
            public static readonly int _TaaAccumulationTex = Shader.PropertyToID("_TaaAccumulationTex");
            public static readonly int _TaaMotionVectorTex = Shader.PropertyToID("_TaaMotionVectorTex");

            public static readonly int _TaaFilterWeights   = Shader.PropertyToID("_TaaFilterWeights");

            public static readonly int _TaaFrameInfluence     = Shader.PropertyToID("_TaaFrameInfluence");
            public static readonly int _TaaVarianceClampScale = Shader.PropertyToID("_TaaVarianceClampScale");

            public static readonly int _CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
        }

        internal static class ShaderKeywords
        {
            public static readonly string TAA_LOW_PRECISION_SOURCE = "TAA_LOW_PRECISION_SOURCE";
        }

        /// <summary>
        /// Temporal anti-aliasing settings.
        /// </summary>
        [Serializable]
        public struct Settings
        {
            [SerializeField]
            [FormerlySerializedAs("quality")]
            internal TemporalAAQuality m_Quality;
            [SerializeField]
            [FormerlySerializedAs("frameInfluence")]
            internal float m_FrameInfluence;
            [SerializeField]
            [FormerlySerializedAs("jitterScale")]
            internal float m_JitterScale;
            [SerializeField]
            [FormerlySerializedAs("mipBias")]
            internal float m_MipBias;
            [SerializeField]
            [FormerlySerializedAs("varianceClampScale")]
            internal float m_VarianceClampScale;
            [SerializeField]
            [FormerlySerializedAs("contrastAdaptiveSharpening")]
            internal float m_ContrastAdaptiveSharpening;

            // Internal API
            [NonSerialized] internal int resetHistoryFrames;      // Number of frames the history is reset. 0 no reset, 1 normal reset, 2 XR reset, -1 infinite (toggle on)
            [NonSerialized] internal int jitterFrameCountOffset;  // Jitter "seed" == Time.frameCount + jitterFrameCountOffset. Used for testing determinism.

            /// <summary>
            /// The quality level to use for the temporal anti-aliasing.
            /// </summary>
            public TemporalAAQuality quality
            {
                get => m_Quality;
                set => m_Quality = (TemporalAAQuality)Mathf.Clamp((int)value, (int)TemporalAAQuality.VeryLow, (int)TemporalAAQuality.VeryHigh);
            }
            /// <summary>
            /// Determines how much the history buffer is blended together with current frame result. Higher values means more history contribution, which leads to better anti aliasing, but also more prone to ghosting.
            /// Between 0.0 - 1.0.
            /// </summary>
            public float baseBlendFactor
            {
                // URP uses frame influence, amount of current frame to blend with history.
                // HDRP uses base blend factor, the amount of history to blend with current frame.
                // We flip the value here to match HDRP for consistent API/UI.
                get => 1.0f - m_FrameInfluence;
                set => m_FrameInfluence = Mathf.Clamp01(1.0f - value);
            }

            /// <summary>
            /// Determines the scale to the jitter applied when TAA is enabled. Lowering this value will lead to less visible flickering and jittering, but also will produce more aliased images.
            /// </summary>
            public float jitterScale
            {
                get => m_JitterScale;
                set => m_JitterScale = Mathf.Clamp01(value);
            }

            /// <summary>
            /// Determines how much texture mip map selection is biased when rendering. Lowering this can slightly reduce blur on textures at the cost of performance. Requires mip maps in textures.
            /// Between -1.0 - 0.0.
            /// </summary>
            public float mipBias
            {
                get => m_MipBias;
                set => m_MipBias = Mathf.Clamp(value, -1.0f, 0.0f);
            }

            /// <summary>
            /// Determines the strength of the history color rectification clamp. Lower values can reduce ghosting, but produce more flickering. Higher values reduce flickering, but are prone to blur and ghosting.
            /// Between 0.001 - 10.0.
            /// Good values around 1.0.
            /// </summary>
            public float varianceClampScale
            {
                get => m_VarianceClampScale;
                set => m_VarianceClampScale = Mathf.Clamp(value, 0.001f, 10.0f);
            }

            /// <summary>
            /// Enables high quality post sharpening to reduce TAA blur. The FSR upscaling overrides this setting if enabled.
            /// Between 0.0 - 1.0.
            /// Use 0.0 to disable.
            /// </summary>
            public float contrastAdaptiveSharpening
            {
                get => m_ContrastAdaptiveSharpening;
                set => m_ContrastAdaptiveSharpening = Mathf.Clamp01(value);
            }

            /// <summary>
            /// Creates a new instance of the settings with default values.
            /// </summary>
            /// <returns>Default settings.</returns>
            public static Settings Create()
            {
                Settings s;

                s.m_Quality                    = TemporalAAQuality.High;
                s.m_FrameInfluence             = 0.1f;
                s.m_JitterScale                = 1.0f;
                s.m_MipBias                    = 0.0f;
                s.m_VarianceClampScale         = 0.9f;
                s.m_ContrastAdaptiveSharpening = 0.0f; // Disabled

                s.resetHistoryFrames     = 0;
                s.jitterFrameCountOffset = 0;

                return s;
            }
        }


        /// <summary>
        /// A function delegate that returns a jitter offset for the provided frame
        /// This provides support for cases where a non-standard jitter pattern is desired
        /// </summary>
        /// <param name="frameIndex">index of the current frame</param>
        /// <param name="jitter">computed jitter offset</param>
        /// <param name="allowScaling">true if the jitter function's output supports scaling</param>
        internal delegate void JitterFunc(int frameIndex, out Vector2 jitter, out bool allowScaling);

        internal static int CalculateTaaFrameIndex(ref Settings settings)
        {
            // URP supports adding an offset value to the TAA frame index for testing determinism.
            int taaFrameCountOffset = settings.jitterFrameCountOffset;
            return Time.frameCount + taaFrameCountOffset;
        }

        internal static Matrix4x4 CalculateJitterMatrix(UniversalCameraData cameraData, JitterFunc jitterFunc)
        {
            Matrix4x4 jitterMat = Matrix4x4.identity;

            bool isJitter = cameraData.IsTemporalAAEnabled();
            if (isJitter)
            {
                int taaFrameIndex = CalculateTaaFrameIndex(ref cameraData.taaSettings);

                float actualWidth = cameraData.cameraTargetDescriptor.width;
                float actualHeight = cameraData.cameraTargetDescriptor.height;
                float jitterScale = cameraData.taaSettings.jitterScale;

                Vector2 jitter;
                bool allowScaling;
                jitterFunc(taaFrameIndex, out jitter, out allowScaling);

                if (allowScaling)
                    jitter *= jitterScale;

                float offsetX = jitter.x * (2.0f / actualWidth);
                float offsetY = jitter.y * (2.0f / actualHeight);

                jitterMat = Matrix4x4.Translate(new Vector3(offsetX, offsetY, 0.0f));
            }

            return jitterMat;
        }

        internal static void CalculateJitter(int frameIndex, out Vector2 jitter, out bool allowScaling)
        {
            // The variance between 0 and the actual halton sequence values reveals noticeable
            // instability in Unity's shadow maps, so we avoid index 0.
            float jitterX = HaltonSequence.Get((frameIndex & 1023) + 1, 2) - 0.5f;
            float jitterY = HaltonSequence.Get((frameIndex & 1023) + 1, 3) - 0.5f;

            jitter = new Vector2(jitterX, jitterY);
            allowScaling = true;
        }

        // Static allocation of JitterFunc delegate to avoid GC
        internal static JitterFunc s_JitterFunc = CalculateJitter;

        private static readonly Vector2[] taaFilterOffsets = new Vector2[]
        {
            new Vector2(0.0f, 0.0f),

            new Vector2(0.0f, 1.0f),
            new Vector2(1.0f, 0.0f),
            new Vector2(-1.0f, 0.0f),
            new Vector2(0.0f, -1.0f),

            new Vector2(-1.0f, 1.0f),
            new Vector2(1.0f, -1.0f),
            new Vector2(1.0f, 1.0f),
            new Vector2(-1.0f, -1.0f)
        };

        private static readonly float[] taaFilterWeights = new float[taaFilterOffsets.Length + 1];

        internal static float[] CalculateFilterWeights(ref Settings settings)
        {
            int taaFrameIndex = CalculateTaaFrameIndex(ref settings);

            // Based on HDRP
            // Precompute weights used for the Blackman-Harris filter.
            float totalWeight = 0;
            for (int i = 0; i < 9; ++i)
            {
                // The internal jitter function used by TAA always allows scaling
                CalculateJitter(taaFrameIndex, out var jitter, out var _);
                jitter *= settings.jitterScale;

                // The rendered frame (pixel grid) is already jittered.
                // We sample 3x3 neighbors with int offsets, but weight the samples
                // relative to the distance to the non-jittered pixel center.
                // From the POV of offset[0] at (0,0), the original pixel center is at (-jitter.x, -jitter.y).
                float x = taaFilterOffsets[i].x - jitter.x;
                float y = taaFilterOffsets[i].y - jitter.y;
                float d2 = (x * x + y * y);

                taaFilterWeights[i] = Mathf.Exp((-0.5f / (0.22f)) * d2);
                totalWeight += taaFilterWeights[i];
            }

            // Normalize weights.
            for (int i = 0; i < 9; ++i)
            {
                taaFilterWeights[i] /= totalWeight;
            }

            return taaFilterWeights;
        }

        internal static GraphicsFormat[] AccumulationFormatList = new GraphicsFormat[]
        {
            GraphicsFormat.R16G16B16A16_SFloat,
            GraphicsFormat.B10G11R11_UFloatPack32,
            GraphicsFormat.R8G8B8A8_UNorm,
            GraphicsFormat.B8G8R8A8_UNorm,
        };

        internal static RenderTextureDescriptor TemporalAADescFromCameraDesc(ref RenderTextureDescriptor cameraDesc)
        {
            RenderTextureDescriptor taaDesc = cameraDesc;

            // Explicitly set from cameraDesc.* for clarity.
            taaDesc.width = cameraDesc.width;
            taaDesc.height = cameraDesc.height;
            taaDesc.msaaSamples = 1;
            taaDesc.volumeDepth = cameraDesc.volumeDepth;
            taaDesc.mipCount = 0;
            taaDesc.graphicsFormat = cameraDesc.graphicsFormat;
            taaDesc.sRGB = false;
            taaDesc.depthStencilFormat = GraphicsFormat.None;
            taaDesc.dimension = cameraDesc.dimension;
            taaDesc.vrUsage = cameraDesc.vrUsage;
            taaDesc.memoryless = RenderTextureMemoryless.None;
            taaDesc.useMipMap = false;
            taaDesc.autoGenerateMips = false;
            taaDesc.enableRandomWrite = false;
            taaDesc.bindMS = false;
            taaDesc.useDynamicScale = false;

            if (!SystemInfo.IsFormatSupported(taaDesc.graphicsFormat, GraphicsFormatUsage.Render))
            {
                taaDesc.graphicsFormat = GraphicsFormat.None;
                for (int i = 0; i < AccumulationFormatList.Length; i++)
                    if (SystemInfo.IsFormatSupported(AccumulationFormatList[i], GraphicsFormatUsage.Render))
                    {
                        taaDesc.graphicsFormat = AccumulationFormatList[i];
                        break;
                    }
            }

            return taaDesc;
        }

        static uint s_warnCounter = 0;

        internal static string ValidateAndWarn(UniversalCameraData cameraData, bool isSTPRequested = false)
        {
            string reasonWarning = null;

            if(reasonWarning == null && !cameraData.postProcessEnabled)
                reasonWarning = "because camera has post-processing disabled.";

            if (cameraData.taaHistory == null)
            {
                reasonWarning = "due to invalid persistent data.";
            }

            if (reasonWarning == null && cameraData.cameraTargetDescriptor.msaaSamples != 1)
            {
                if (cameraData.xr != null && cameraData.xr.enabled)
                    reasonWarning = "because MSAA is on. MSAA must be disabled globally for all cameras in XR mode.";
                else
                    reasonWarning = "because MSAA is on. Turn MSAA off on the camera or current URP Asset.";
            }

            if(reasonWarning == null && cameraData.camera.TryGetComponent<UniversalAdditionalCameraData>(out var additionalCameraData))
            {
                if (additionalCameraData.renderType == CameraRenderType.Overlay ||
                    additionalCameraData.cameraStack.Count > 0)
                {
                    reasonWarning = "because camera is stacked.";
                }
            }

            if (reasonWarning == null && cameraData.camera.allowDynamicResolution)
                reasonWarning = "because camera has dynamic resolution enabled. You can use a constant render scale instead.";

            if(reasonWarning == null && !cameraData.renderer.SupportsMotionVectors())
                reasonWarning = "because the renderer does not implement motion vectors. Motion vectors are required.";

            const int warningThrottleFrames = 60 * 1; // 60 FPS * 1 sec
            if (s_warnCounter % warningThrottleFrames == 0)
                Debug.LogWarning("Disabling TAA " + (isSTPRequested ? "and STP " : "") + reasonWarning);
            s_warnCounter++;

            return reasonWarning;
        }

        internal static void ExecutePass(CommandBuffer cmd, Material taaMaterial, ref CameraData cameraData, RTHandle source, RTHandle destination, RenderTexture motionVectors)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.TemporalAA)))
            {
                int multipassId = 0;
#if ENABLE_VR && ENABLE_XR_MODULE
                multipassId = cameraData.xr.multipassId;
#endif
                bool isNewFrame = cameraData.taaHistory.GetAccumulationVersion(multipassId) != Time.frameCount;

                RTHandle taaHistoryAccumulationTex = cameraData.taaHistory.GetAccumulationTexture(multipassId);
                taaMaterial.SetTexture(ShaderConstants._TaaAccumulationTex, taaHistoryAccumulationTex);

                // On frame rerender or pause, stop all motion using a black motion texture.
                // This is done to avoid blurring the Taa resolve due to motion and Taa history mismatch.
                //
                // Taa history copy is in sync with motion vectors and Time.frameCount, but we updated the TAA history
                // for the next frame, as we did not know that we're going render this frame again.
                // We would need history double buffering to solve this properly, but at the cost of memory.
                //
                // Frame #1: MotionVectors.Update: #1 Prev: #-1, Taa.Execute: #1 Prev: #-1, Taa.CopyHistory: #1 Prev: #-1
                // Frame #2: MotionVectors.Update: #2 Prev: #1, Taa.Execute: #2 Prev #1, Taa.CopyHistory: #2
                // <pause or render frame #2 again>
                // Frame #2: MotionVectors.Update: #2, Taa.Execute: #2 prev #2   (Ooops! Incorrect history for frame #2!)
                taaMaterial.SetTexture(ShaderConstants._TaaMotionVectorTex, isNewFrame ? motionVectors : Texture2D.blackTexture);

                ref var taa = ref cameraData.taaSettings;
                float taaInfluence = taa.resetHistoryFrames == 0 ? taa.m_FrameInfluence : 1.0f;
                taaMaterial.SetFloat(ShaderConstants._TaaFrameInfluence, taaInfluence);
                taaMaterial.SetFloat(ShaderConstants._TaaVarianceClampScale, taa.varianceClampScale);

                if (taa.quality == TemporalAAQuality.VeryHigh)
                    taaMaterial.SetFloatArray(ShaderConstants._TaaFilterWeights, CalculateFilterWeights(ref taa));

                switch (taaHistoryAccumulationTex.rt.graphicsFormat)
                {
                    // Avoid precision issues with YCoCg and low bit color formats.
                    case GraphicsFormat.B10G11R11_UFloatPack32:
                    case GraphicsFormat.R8G8B8A8_UNorm:
                    case GraphicsFormat.B8G8R8A8_UNorm:
                        taaMaterial.EnableKeyword(ShaderKeywords.TAA_LOW_PRECISION_SOURCE);
                        break;
                    default:
                        taaMaterial.DisableKeyword(ShaderKeywords.TAA_LOW_PRECISION_SOURCE);
                        break;
                }

                CoreUtils.SetKeyword(taaMaterial, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, cameraData.isAlphaOutputEnabled);

                Blitter.BlitCameraTexture(cmd, source, destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, taaMaterial, (int)taa.quality);

                if (isNewFrame)
                {
                    int kHistoryCopyPass = taaMaterial.shader.passCount - 1;
                    Blitter.BlitCameraTexture(cmd, destination, taaHistoryAccumulationTex, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, taaMaterial, kHistoryCopyPass);
                    cameraData.taaHistory.SetAccumulationVersion(multipassId, Time.frameCount);
                }
            }
        }

        private class TaaPassData
        {
            internal TextureHandle dstTex;
            internal TextureHandle srcColorTex;
            internal TextureHandle srcDepthTex;
            internal TextureHandle srcMotionVectorTex;
            internal TextureHandle srcTaaAccumTex;

            internal Material material;
            internal int passIndex;

            internal float taaFrameInfluence;
            internal float taaVarianceClampScale;
            internal float[] taaFilterWeights;

            internal bool taaLowPrecisionSource;
            internal bool taaAlphaOutput;
        }

        internal static void Render(RenderGraph renderGraph, Material taaMaterial, UniversalCameraData cameraData, ref TextureHandle srcColor, ref TextureHandle srcDepth, ref TextureHandle srcMotionVectors, ref TextureHandle dstColor)
        {
            int multipassId = 0;
#if ENABLE_VR && ENABLE_XR_MODULE
            multipassId = cameraData.xr.multipassId;
#endif

            ref var taa = ref cameraData.taaSettings;

            bool isNewFrame = cameraData.taaHistory.GetAccumulationVersion(multipassId) != Time.frameCount;
            float taaInfluence = taa.resetHistoryFrames == 0 ? taa.m_FrameInfluence : 1.0f;

            RTHandle accumulationTexture = cameraData.taaHistory.GetAccumulationTexture(multipassId);
            TextureHandle srcAccumulation = renderGraph.ImportTexture(accumulationTexture);

            // On frame rerender or pause, stop all motion using a black motion texture.
            // This is done to avoid blurring the Taa resolve due to motion and Taa history mismatch.
            // The TAA history was updated for the next frame, as we did not know yet that we're going render this frame again.
            // We would need to keep the both the current and previous history (double buffering) in order to resolve
            // either this frame (again) or the next frame correctly, but it would cost more memory.
            TextureHandle activeMotionVectors = isNewFrame ? srcMotionVectors : renderGraph.defaultResources.blackTexture;

            using (var builder = renderGraph.AddRasterRenderPass<TaaPassData>("Temporal Anti-aliasing", out var passData, ProfilingSampler.Get(URPProfileId.RG_TAA)))
            {
                passData.dstTex = dstColor;
                builder.SetRenderAttachment(dstColor, 0, AccessFlags.Write);
                passData.srcColorTex = srcColor;
                builder.UseTexture(srcColor, AccessFlags.Read);
                passData.srcDepthTex = srcDepth;
                builder.UseTexture(srcDepth, AccessFlags.Read);
                passData.srcMotionVectorTex = activeMotionVectors;
                builder.UseTexture(activeMotionVectors, AccessFlags.Read);
                passData.srcTaaAccumTex = srcAccumulation;
                builder.UseTexture(srcAccumulation, AccessFlags.Read);

                passData.material = taaMaterial;
                passData.passIndex = (int)taa.quality;

                passData.taaFrameInfluence = taaInfluence;
                passData.taaVarianceClampScale = taa.varianceClampScale;

                if (taa.quality == TemporalAAQuality.VeryHigh)
                    passData.taaFilterWeights = CalculateFilterWeights(ref taa);
                else
                    passData.taaFilterWeights = null;

                switch (accumulationTexture.rt.graphicsFormat)
                {
                    // Avoid precision issues with YCoCg and low bit color formats.
                    case GraphicsFormat.B10G11R11_UFloatPack32:
                    case GraphicsFormat.R8G8B8A8_UNorm:
                    case GraphicsFormat.B8G8R8A8_UNorm:
                        passData.taaLowPrecisionSource = true;
                        break;
                    default:
                        passData.taaLowPrecisionSource = false;
                        break;
                }

                passData.taaAlphaOutput = cameraData.isAlphaOutputEnabled;

                builder.SetRenderFunc(static (TaaPassData data, RasterGraphContext context) =>
                {
                    data.material.SetFloat(ShaderConstants._TaaFrameInfluence, data.taaFrameInfluence);
                    data.material.SetFloat(ShaderConstants._TaaVarianceClampScale, data.taaVarianceClampScale);
                    data.material.SetTexture(ShaderConstants._TaaAccumulationTex, data.srcTaaAccumTex);
                    data.material.SetTexture(ShaderConstants._TaaMotionVectorTex, data.srcMotionVectorTex);
                    data.material.SetTexture(ShaderConstants._CameraDepthTexture, data.srcDepthTex);
                    CoreUtils.SetKeyword(data.material, ShaderKeywords.TAA_LOW_PRECISION_SOURCE, data.taaLowPrecisionSource);
                    CoreUtils.SetKeyword(data.material, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, data.taaAlphaOutput);

                    if(data.taaFilterWeights != null)
                        data.material.SetFloatArray(ShaderConstants._TaaFilterWeights, data.taaFilterWeights);

                    Blitter.BlitTexture(context.cmd, data.srcColorTex, Vector2.one, data.material, data.passIndex);
                });
            }

            if (isNewFrame)
            {
                int kHistoryCopyPass = taaMaterial.shader.passCount - 1;
                using (var builder = renderGraph.AddRasterRenderPass<TaaPassData>("Temporal Anti-aliasing Copy History", out var passData, ProfilingSampler.Get(URPProfileId.RG_TAACopyHistory)))
                {
                    passData.dstTex = srcAccumulation;
                    builder.SetRenderAttachment(srcAccumulation, 0, AccessFlags.Write);
                    passData.srcColorTex = dstColor;
                    builder.UseTexture(dstColor, AccessFlags.Read);   // Resolved color is the new history

                    passData.material = taaMaterial;
                    passData.passIndex = kHistoryCopyPass;

                    builder.SetRenderFunc((TaaPassData data, RasterGraphContext context) => { Blitter.BlitTexture(context.cmd, data.srcColorTex, Vector2.one, data.material, data.passIndex); });
                }

                cameraData.taaHistory.SetAccumulationVersion(multipassId, Time.frameCount);
            }
        }
    }
}
