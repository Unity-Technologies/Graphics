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

        public RenderTextureDescriptor rtd => m_RtDesc;
        public RTHandle accumulationTexture(int index) => index != 0 ? m_AccumulationTexture2 : m_AccumulationTexture;

        public TaaPersistentData()
        {
        }

        public void Init(int sizeX, int sizeY, GraphicsFormat format, VRTextureUsage vrUsage, TextureDimension texDim)
        {
            if ((m_RtDesc.width != sizeX || m_RtDesc.height != sizeY || m_AccumulationTexture == null) &&
                (sizeX > 0 && sizeY >0))
            {
                RenderTextureDescriptor desc = new RenderTextureDescriptor();

                const bool enableRandomWrite = false; // aka UAV, Load/Store
                FormatUsage usage = enableRandomWrite ? FormatUsage.LoadStore : FormatUsage.Render;

                desc.width = sizeX;
                desc.height = sizeY;
                desc.msaaSamples = 1;
                desc.volumeDepth = 1;
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
        }

    };

    // All of TAA here, work on TAA == work on this file.
    static class TemporalAA
    {
        static internal class ShaderConstants
        {
            public static readonly int _TaaFrameInfluence = Shader.PropertyToID("_TaaFrameInfluence");
            public static readonly int _TaaAccumulationTex = Shader.PropertyToID("_TaaAccumulationTex");
            public static readonly int _TaaMotionVectorTex = Shader.PropertyToID("_TaaMotionVectorTex");
        }

        internal struct Settings
        {
            public TemporalAAQuality quality;
            public int resetHistoryFrames;  // Number of frames the history is reset. 0 no reset, 1 normal reset, 2 XR reset, -1 infinite (toggle on)

            public float frameInfluence;

            public static Settings Create()
            {
                Settings s;

                s.quality            = TemporalAAQuality.Medium;
                s.resetHistoryFrames = 0;

                s.frameInfluence     = 0.05f;

                return s;
            }
        }

        static internal Matrix4x4 CalculateJitterMatrix(ref CameraData cameraData)
        {
            Matrix4x4 jitterMat = Matrix4x4.identity;

            bool isJitter = cameraData.IsTemporalAAEnabled();
            if (isJitter)
            {
                int taaFrameIndex = Time.frameCount;

                float actualWidth = cameraData.pixelWidth;
                float actualHeight = cameraData.pixelHeight;

                var jitter = CalculateJitter(taaFrameIndex);

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

        static internal void ValidateAndWarn(ref CameraData cameraData)
        {
            if (cameraData.taaPersistentData == null)
            {
                Debug.LogWarning("Disabling TAA due to invalid persistent data.");
            }

            if (cameraData.cameraTargetDescriptor.msaaSamples != 1)
            {
                if(cameraData.xr != null && cameraData.xr.enabled)
                    Debug.LogWarning("Disabling TAA because MSAA is on. MSAA must be disabled globally for all cameras in XR mode.");
                else
                    Debug.LogWarning("Disabling TAA because MSAA is on.");
            }

            if(cameraData.camera.TryGetComponent<UniversalAdditionalCameraData>(out var additionalCameraData))
            {
                if (additionalCameraData.renderType == CameraRenderType.Overlay ||
                    additionalCameraData.cameraStack.Count > 0)
                {
                    Debug.LogWarning("Disabling TAA because camera is stacked.");
                }
            }

            if(cameraData.camera.allowDynamicResolution)
                Debug.LogWarning("Disabling TAA because camera has dynamic resolution enabled. You can use a constant render scale instead.");

        }

        internal static void ExecutePass(CommandBuffer cmd, Material taaMaterial, ref CameraData cameraData, RTHandle source, RTHandle destination, RenderTexture motionVectors)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.TemporalAA)))
            {
                ref var taa = ref cameraData.taaSettings;

                int multipassId = 0;
#if ENABLE_VR && ENABLE_XR_MODULE
                multipassId = cameraData.xr.multipassId;
#endif

                float taaInfluence = taa.resetHistoryFrames == 0 ? taa.frameInfluence : 1.0f;

                taaMaterial.SetFloat(ShaderConstants._TaaFrameInfluence, taaInfluence);
                taaMaterial.SetTexture(ShaderConstants._TaaAccumulationTex, cameraData.taaPersistentData.accumulationTexture(multipassId));
                taaMaterial.SetTexture(ShaderConstants._TaaMotionVectorTex, motionVectors);

                int kHistoryCopyPass = (int)TemporalAAQuality.High + 1;

                Blitter.BlitCameraTexture(cmd, source, destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, taaMaterial, (int)taa.quality);
                var history = cameraData.taaPersistentData.accumulationTexture(multipassId);
                Blitter.BlitCameraTexture(cmd, destination, history, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, taaMaterial, kHistoryCopyPass);
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

            internal float taaInfluence;
        }

        internal static void Render(RenderGraph renderGraph, Material taaMaterial, ref CameraData cameraData, ref TextureHandle srcColor, ref TextureHandle srcDepth, ref TextureHandle srcMotionVectors, ref TextureHandle dst)
        {
            ref var taa = ref cameraData.taaSettings;
            float taaInfluence = taa.resetHistoryFrames == 0 ? taa.frameInfluence : 1.0f;

            int multipassId = 0;
#if ENABLE_VR && ENABLE_XR_MODULE
            multipassId = cameraData.xr.multipassId;
#endif
            TextureHandle srcAccumulation = renderGraph.ImportTexture(cameraData.taaPersistentData.accumulationTexture(multipassId));

            using (var builder = renderGraph.AddRenderPass<TaaPassData>("Temporal Anti-aliasing", out var passData, ProfilingSampler.Get(URPProfileId.RG_TAA)))
            {
                passData.dstTex = builder.UseColorBuffer(dst, 0);
                passData.srcColorTex = builder.ReadTexture(srcColor);
                passData.srcDepthTex = builder.ReadTexture(srcDepth);
                passData.srcMotionVectorTex = builder.ReadTexture(srcMotionVectors);
                passData.srcTaaAccumTex = builder.ReadTexture(srcAccumulation);

                passData.material = taaMaterial;
                passData.passIndex = (int)taa.quality;

                passData.taaInfluence = taaInfluence;

                builder.SetRenderFunc((TaaPassData data, RenderGraphContext context) =>
                {
                    data.material.SetFloat(ShaderConstants._TaaFrameInfluence, data.taaInfluence);
                    data.material.SetTexture(ShaderConstants._TaaAccumulationTex, data.srcTaaAccumTex);
                    data.material.SetTexture(ShaderConstants._TaaMotionVectorTex, data.srcMotionVectorTex);
                    data.material.SetTexture("_CameraDepthTexture", data.srcDepthTex); // TODO: Use a constant for the name.

                    Blitter.BlitTexture(context.cmd, data.srcColorTex, Vector2.one, data.material, data.passIndex);
                });
            }

            int kHistoryCopyPass = (int)TemporalAAQuality.High + 1;
            using (var builder = renderGraph.AddRenderPass<TaaPassData>("Temporal Anti-aliasing Copy History", out var passData, ProfilingSampler.Get(URPProfileId.RG_TAACopyHistory)))
            {
                passData.dstTex = builder.UseColorBuffer(srcAccumulation, 0);
                passData.srcColorTex = builder.ReadTexture(dst);

                passData.material = taaMaterial;
                passData.passIndex = kHistoryCopyPass;

                builder.SetRenderFunc((TaaPassData data, RenderGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.srcColorTex, Vector2.one, data.material, data.passIndex);
                });
            }
        }
    }
}
