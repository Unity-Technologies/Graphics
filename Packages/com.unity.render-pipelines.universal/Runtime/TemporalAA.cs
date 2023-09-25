using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal enum TemporalAAQuality
    {
        VeryLow = 0,
        Low,
        Medium,
        High,
        VeryHigh
    }

    // Temporal AA data that persists over a frame. (per camera)
    sealed internal class TaaPersistentData
    {
        private static GraphicsFormat[] formatList = new GraphicsFormat[]
        {
            GraphicsFormat.R16G16B16A16_SFloat,
            GraphicsFormat.B10G11R11_UFloatPack32,
            GraphicsFormat.R8G8B8A8_UNorm,
            GraphicsFormat.B8G8R8A8_UNorm,
        };

        RenderTextureDescriptor m_RtDesc;
        RTHandle m_AccumulationTexture;
        RTHandle m_AccumulationTexture2;
        int m_LastAccumUpdateFrameIndex;
        int m_LastAccumUpdateFrameIndex2;

        public RenderTextureDescriptor rtd => m_RtDesc;
        public RTHandle accumulationTexture(int index) => index != 0 ? m_AccumulationTexture2 : m_AccumulationTexture;
        public int GetLastAccumFrameIndex(int index) => index != 0 ? m_LastAccumUpdateFrameIndex2 : m_LastAccumUpdateFrameIndex;
        public void SetLastAccumFrameIndex(int index, int value)
        {
            if (index != 0)
                m_LastAccumUpdateFrameIndex2 = value;
            else
                m_LastAccumUpdateFrameIndex = value;
        }

        public TaaPersistentData()
        {
        }

        public void Init(int sizeX, int sizeY, int volumeDepth, GraphicsFormat format, VRTextureUsage vrUsage, TextureDimension texDim)
        {
            if ((m_RtDesc.width != sizeX || m_RtDesc.height != sizeY || m_RtDesc.volumeDepth != volumeDepth || m_AccumulationTexture == null) &&
                (sizeX > 0 && sizeY >0))
            {
                RenderTextureDescriptor desc = new RenderTextureDescriptor();

                const bool enableRandomWrite = false; // aka UAV, Load/Store
                FormatUsage usage = enableRandomWrite ? FormatUsage.LoadStore : FormatUsage.Render;

                desc.width = sizeX;
                desc.height = sizeY;
                desc.msaaSamples = 1;
                desc.volumeDepth = volumeDepth;
                desc.mipCount = 0;
                desc.graphicsFormat = CheckFormat(format, usage);
                desc.sRGB = false;
                desc.depthBufferBits = 0;
                desc.dimension = texDim;
                desc.vrUsage = vrUsage;
                desc.memoryless = RenderTextureMemoryless.None;
                desc.useMipMap = false;
                desc.autoGenerateMips = false;
                desc.enableRandomWrite = enableRandomWrite;
                desc.bindMS = false;
                desc.useDynamicScale = false;

                m_RtDesc = desc;

                DeallocateTargets();
            }

            GraphicsFormat CheckFormat(GraphicsFormat format, FormatUsage usage)
            {
                // Should do query per usage, but we rely on the fact that "LoadStore" implies "Render" in the code.
                bool success = SystemInfo.IsFormatSupported(format, usage);
                if (!success)
                    return FindFormat(usage); // Fallback
                return format;
            }

            GraphicsFormat FindFormat( FormatUsage usage )
            {
                for (int i = 0; i < formatList.Length; i++)
                    if (SystemInfo.IsFormatSupported(formatList[i], usage))
                    {
                        return formatList[i];
                    }

                return GraphicsFormat.B8G8R8A8_UNorm;
            }
        }

        public bool AllocateTargets(bool xrMultipassEnabled = false)
        {
            bool didAlloc = false;

            // The rule is that if the target needs to be reallocated, the m_AccumulationTexture has already been set to null.
            // So during allocation, the logic is as simple as allocate it if it's non-null.
            if (m_AccumulationTexture == null)
            {
                m_AccumulationTexture = RTHandles.Alloc(m_RtDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"_TaaAccumulationTex");
                didAlloc = true;
            }

            // Second eye for XR multipass (the persistent data is shared, for now)
            if (xrMultipassEnabled && m_AccumulationTexture2 == null)
            {
                m_AccumulationTexture2 = RTHandles.Alloc(m_RtDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"_TaaAccumulationTex2");
                didAlloc = true;
            }

            return didAlloc;
        }

        public void DeallocateTargets()
        {
            m_AccumulationTexture?.Release();
            m_AccumulationTexture2?.Release();
            m_AccumulationTexture = null;
            m_AccumulationTexture2 = null;
            m_LastAccumUpdateFrameIndex = -1;
            m_LastAccumUpdateFrameIndex2 = -1;
        }

    };

    // All of TAA here, work on TAA == work on this file.
    static class TemporalAA
    {
        static internal class ShaderConstants
        {
            public static readonly int _TaaAccumulationTex = Shader.PropertyToID("_TaaAccumulationTex");
            public static readonly int _TaaMotionVectorTex = Shader.PropertyToID("_TaaMotionVectorTex");

            public static readonly int _TaaFilterWeights   = Shader.PropertyToID("_TaaFilterWeights");

            public static readonly int _TaaFrameInfluence     = Shader.PropertyToID("_TaaFrameInfluence");
            public static readonly int _TaaVarianceClampScale = Shader.PropertyToID("_TaaVarianceClampScale");

            public static readonly int _CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
        }

        static internal class ShaderKeywords
        {
            public static readonly string TAA_LOW_PRECISION_SOURCE = "TAA_LOW_PRECISION_SOURCE";
        }

        [Serializable]
        internal struct Settings
        {
            public TemporalAAQuality quality;
            public float frameInfluence;
            public float jitterScale;
            public float mipBias;
            public float varianceClampScale;
            public float contrastAdaptiveSharpening;

            [NonSerialized] public int resetHistoryFrames;      // Number of frames the history is reset. 0 no reset, 1 normal reset, 2 XR reset, -1 infinite (toggle on)
            [NonSerialized] public int jitterFrameCountOffset;  // Jitter "seed" == Time.frameCount + jitterFrameCountOffset. Used for testing determinism.

            public static Settings Create()
            {
                Settings s;

                s.quality                    = TemporalAAQuality.High;
                s.frameInfluence             = 0.1f;
                s.jitterScale                = 1.0f;
                s.mipBias                    = 0.0f;
                s.varianceClampScale         = 0.9f;
                s.contrastAdaptiveSharpening = 0.0f; // Disabled

                s.resetHistoryFrames = 0;
                s.jitterFrameCountOffset = 0;

                return s;
            }
        }

        static internal Matrix4x4 CalculateJitterMatrix(ref CameraData cameraData)
        {
            Matrix4x4 jitterMat = Matrix4x4.identity;

            bool isJitter = cameraData.IsTemporalAAEnabled();
            if (isJitter)
            {
                int taaFrameCountOffset = cameraData.taaSettings.jitterFrameCountOffset;
                int taaFrameIndex = Time.frameCount + taaFrameCountOffset;

                float actualWidth = cameraData.cameraTargetDescriptor.width;
                float actualHeight = cameraData.cameraTargetDescriptor.height;
                float jitterScale = cameraData.taaSettings.jitterScale;

                var jitter = CalculateJitter(taaFrameIndex) * jitterScale;

                float offsetX = jitter.x * (2.0f / actualWidth);
                float offsetY = jitter.y * (2.0f / actualHeight);

                jitterMat = Matrix4x4.Translate(new Vector3(offsetX, offsetY, 0.0f));
            }

            return jitterMat;
        }

        static internal Vector2 CalculateJitter(int frameIndex)
        {
            // The variance between 0 and the actual halton sequence values reveals noticeable
            // instability in Unity's shadow maps, so we avoid index 0.
            float jitterX = HaltonSequence.Get((frameIndex & 1023) + 1, 2) - 0.5f;
            float jitterY = HaltonSequence.Get((frameIndex & 1023) + 1, 3) - 0.5f;

            return new Vector2(jitterX, jitterY);
        }

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

        static internal float[] CalculateFilterWeights(float jitterScale)
        {
            // Based on HDRP
            // Precompute weights used for the Blackman-Harris filter.
            float totalWeight = 0;
            for (int i = 0; i < 9; ++i)
            {
                Vector2 jitter = CalculateJitter(Time.frameCount) * jitterScale;
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

        static internal string ValidateAndWarn(ref CameraData cameraData)
        {
            string warning = null;

            if (cameraData.taaPersistentData == null)
            {
                warning = "Disabling TAA due to invalid persistent data.";
            }

            if (warning == null && cameraData.cameraTargetDescriptor.msaaSamples != 1)
            {
                if (cameraData.xr != null && cameraData.xr.enabled)
                    warning = "Disabling TAA because MSAA is on. MSAA must be disabled globally for all cameras in XR mode.";
                else
                    warning = "Disabling TAA because MSAA is on.";
            }

            if(warning == null && cameraData.camera.TryGetComponent<UniversalAdditionalCameraData>(out var additionalCameraData))
            {
                if (additionalCameraData.renderType == CameraRenderType.Overlay ||
                    additionalCameraData.cameraStack.Count > 0)
                {
                    warning = "Disabling TAA because camera is stacked.";
                }
            }

            if (warning == null && cameraData.camera.allowDynamicResolution)
                warning = "Disabling TAA because camera has dynamic resolution enabled. You can use a constant render scale instead.";

            if(warning == null && !cameraData.postProcessEnabled)
                warning = "Disabling TAA because camera has post-processing disabled.";

            const int warningThrottleFrames = 60 * 1; // 60 FPS * 1 sec
            if(Time.frameCount % warningThrottleFrames == 0)
                Debug.LogWarning(warning);
            
            return warning;
        }

        internal static void ExecutePass(CommandBuffer cmd, Material taaMaterial, ref CameraData cameraData, RTHandle source, RTHandle destination, RenderTexture motionVectors)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.TemporalAA)))
            {
                int multipassId = 0;
#if ENABLE_VR && ENABLE_XR_MODULE
                multipassId = cameraData.xr.multipassId;
#endif

                bool isNewFrame = cameraData.taaPersistentData.GetLastAccumFrameIndex(multipassId) != Time.frameCount;

                RTHandle taaHistoryAccumulationTex = cameraData.taaPersistentData.accumulationTexture(multipassId);
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
                float taaInfluence = taa.resetHistoryFrames == 0 ? taa.frameInfluence : 1.0f;
                taaMaterial.SetFloat(ShaderConstants._TaaFrameInfluence, taaInfluence);
                taaMaterial.SetFloat(ShaderConstants._TaaVarianceClampScale, taa.varianceClampScale);

                if (taa.quality == TemporalAAQuality.VeryHigh)
                    taaMaterial.SetFloatArray(ShaderConstants._TaaFilterWeights, CalculateFilterWeights(taa.jitterScale));

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

                Blitter.BlitCameraTexture(cmd, source, destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, taaMaterial, (int)taa.quality);

                if (isNewFrame)
                {
                    int kHistoryCopyPass = taaMaterial.shader.passCount - 1;
                    Blitter.BlitCameraTexture(cmd, destination, taaHistoryAccumulationTex, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, taaMaterial, kHistoryCopyPass);
                    cameraData.taaPersistentData.SetLastAccumFrameIndex(multipassId, Time.frameCount);
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
        }

        internal static void Render(RenderGraph renderGraph, Material taaMaterial, ref CameraData cameraData, ref TextureHandle srcColor, ref TextureHandle srcDepth, ref TextureHandle srcMotionVectors, ref TextureHandle dstColor)
        {
            int multipassId = 0;
#if ENABLE_VR && ENABLE_XR_MODULE
            multipassId = cameraData.xr.multipassId;
#endif

            ref var taa = ref cameraData.taaSettings;

            bool isNewFrame = cameraData.taaPersistentData.GetLastAccumFrameIndex(multipassId) != Time.frameCount;
            float taaInfluence = taa.resetHistoryFrames == 0 ? taa.frameInfluence : 1.0f;

            RTHandle accumulationTexture = cameraData.taaPersistentData.accumulationTexture(multipassId);
            TextureHandle srcAccumulation = renderGraph.ImportTexture(accumulationTexture);

            // On frame rerender or pause, stop all motion using a black motion texture.
            // This is done to avoid blurring the Taa resolve due to motion and Taa history mismatch.
            // The TAA history was updated for the next frame, as we did not know yet that we're going render this frame again.
            // We would need to keep the both the current and previous history (double buffering) in order to resolve
            // either this frame (again) or the next frame correctly, but it would cost more memory.
            TextureHandle activeMotionVectors = isNewFrame ? srcMotionVectors : renderGraph.defaultResources.blackTexture;

            using (var builder = renderGraph.AddRenderPass<TaaPassData>("Temporal Anti-aliasing", out var passData, ProfilingSampler.Get(URPProfileId.TemporalAA)))
            {
                passData.dstTex = builder.UseColorBuffer(dstColor, 0);
                passData.srcColorTex = builder.ReadTexture(srcColor);
                passData.srcDepthTex = builder.ReadTexture(srcDepth);
                passData.srcMotionVectorTex = builder.ReadTexture(activeMotionVectors);
                passData.srcTaaAccumTex = builder.ReadTexture(srcAccumulation);

                passData.material = taaMaterial;
                passData.passIndex = (int)taa.quality;

                passData.taaFrameInfluence = taaInfluence;
                passData.taaVarianceClampScale = taa.varianceClampScale;

                if (taa.quality == TemporalAAQuality.VeryHigh)
                    passData.taaFilterWeights = CalculateFilterWeights(taa.jitterScale);
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

                builder.SetRenderFunc((TaaPassData data, RenderGraphContext context) =>
                {
                    data.material.SetFloat(ShaderConstants._TaaFrameInfluence, data.taaFrameInfluence);
                    data.material.SetFloat(ShaderConstants._TaaVarianceClampScale, data.taaVarianceClampScale);
                    data.material.SetTexture(ShaderConstants._TaaAccumulationTex, data.srcTaaAccumTex);
                    data.material.SetTexture(ShaderConstants._TaaMotionVectorTex, data.srcMotionVectorTex);
                    data.material.SetTexture(ShaderConstants._CameraDepthTexture, data.srcDepthTex);
                    CoreUtils.SetKeyword(data.material, ShaderKeywords.TAA_LOW_PRECISION_SOURCE, data.taaLowPrecisionSource);

                    if(data.taaFilterWeights != null)
                        data.material.SetFloatArray(ShaderConstants._TaaFilterWeights, data.taaFilterWeights);

                    Blitter.BlitTexture(context.cmd, data.srcColorTex, Vector2.one, data.material, data.passIndex);
                });
            }

            if (isNewFrame)
            {
                int kHistoryCopyPass = taaMaterial.shader.passCount - 1;
                using (var builder = renderGraph.AddRenderPass<TaaPassData>("Temporal Anti-aliasing Copy History", out var passData, new ProfilingSampler("TemporalAAHistoryCopy")))
                {
                    passData.dstTex = builder.UseColorBuffer(srcAccumulation, 0);
                    passData.srcColorTex = builder.ReadTexture(dstColor);   // Resolved color is the new history

                    passData.material = taaMaterial;
                    passData.passIndex = kHistoryCopyPass;

                    builder.SetRenderFunc((TaaPassData data, RenderGraphContext context) => { Blitter.BlitTexture(context.cmd, data.srcColorTex, Vector2.one, data.material, data.passIndex); });
                }
            }
        }
    }
}
