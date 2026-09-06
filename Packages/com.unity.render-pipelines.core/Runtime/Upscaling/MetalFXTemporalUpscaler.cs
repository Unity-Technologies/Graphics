#if ENABLE_UPSCALER_FRAMEWORK && ENABLE_METALFX_MODULE && (UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_TVOS || UNITY_VISIONOS)
using System;
using Unity.Profiling.LowLevel;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.MetalFX
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    static class RegisterMetalFXTemporal
    {
        static RegisterMetalFXTemporal()
            => UpscalerRegistry.Register<MetalFXTemporalUpscaler, MetalFXTemporalOptions>(MetalFXTemporalUpscaler.registeredId, MetalFXTemporalUpscaler.registeredName);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InitRuntime()
            => UpscalerRegistry.Register<MetalFXTemporalUpscaler, MetalFXTemporalOptions>(MetalFXTemporalUpscaler.registeredId, MetalFXTemporalUpscaler.registeredName);
    }

    /// <summary>
    /// Native per-camera state for MetalFX Temporal: the native context handle and a stable dispatch-data buffer
    /// (see MetalFXSpatialNative).
    /// </summary>
    public sealed class MetalFXTemporalNative
    {
        public IntPtr scaler;        // context handle from MetalFX_CreateTemporalContext
    }

    public class MetalFXTemporalContext : PluginUpscalerContext<MetalFXTemporalNative, MetalFXTemporalOptions>
    {
        public MetalFXTemporalContext(Vector2Int displayResolution)
            : base(displayResolution)
        {
        }

        GraphicsFormat m_ColorFormat;
        GraphicsFormat m_DepthFormat;
        GraphicsFormat m_MotionFormat;
        GraphicsFormat m_OutputFormat;
        Vector2Int m_MaxRenderSize;
        Vector2Int m_OutputSize;
        bool m_DynamicResolution;

        public MetalFXTemporalNative GetOrCreateNativeContext(CommandBuffer cmd, IntPtr color, IntPtr depth, IntPtr motion, IntPtr output,
            bool dynamicResolution, GraphicsFormat colorFormat, GraphicsFormat depthFormat,
            GraphicsFormat motionFormat, GraphicsFormat outputFormat, Vector2Int maxRenderSize, Vector2Int outputSize)
        {
            if (m_NativeContext != null && (colorFormat != m_ColorFormat || depthFormat != m_DepthFormat ||
                                            motionFormat != m_MotionFormat || outputFormat != m_OutputFormat ||
                                            maxRenderSize != m_MaxRenderSize || outputSize != m_OutputSize ||
                                            dynamicResolution != m_DynamicResolution))
            {
                DestroyNativeContext(cmd, m_NativeContext);
                m_NativeContext = null;
            }

            if (m_NativeContext == null)
            {
                IntPtr scaler = MetalFXDevice.CreateTemporalContext(color, depth, motion, output, dynamicResolution,
                                                                    maxRenderSize.x, maxRenderSize.y, outputSize.x, outputSize.y);
                if (scaler != IntPtr.Zero)
                {
                    m_NativeContext = new MetalFXTemporalNative
                    {
                        scaler = scaler,
                    };
                    m_ColorFormat = colorFormat;
                    m_DepthFormat = depthFormat;
                    m_MotionFormat = motionFormat;
                    m_OutputFormat = outputFormat;
                    m_MaxRenderSize = maxRenderSize;
                    m_OutputSize = outputSize;
                    m_DynamicResolution = dynamicResolution;
                }
            }
            return m_NativeContext;
        }

        protected override void DestroyNativeContext(CommandBuffer cmd, MetalFXTemporalNative context)
        {
            if (context.scaler == IntPtr.Zero)
                return;

            // Route the release through the render thread
            Debug.Assert(cmd != null, "DestroyNativeContext requires a command buffer to release on the render thread.");
            MetalFXDevice.DestroyContext(cmd, context.scaler);
        }

        protected override bool ValidateOptions(MetalFXTemporalOptions options) => true;
    }

#if UNITY_EDITOR
    [UpscalerSupportedBuildTarget(BuildTarget.StandaloneOSX, BuildTarget.iOS, BuildTarget.tvOS, graphicsDeviceTypes = new[] { GraphicsDeviceType.Metal })]
#endif
    public class MetalFXTemporalUpscaler : AbstractUpscaler
    {
        public static readonly string registeredId = "apple.metalfx-temporal";
        public static readonly string registeredName = "MetalFX Temporal";

        static readonly ProfilingSampler k_MetalFXTemporal = ProfilingSampler.Create("MetalFXTemporal", MarkerFlags.Default);

        public override string upscalerId => registeredId;
        public override string name => registeredName;
        public override bool isTemporal => true;
        public override bool supportsSharpening => false;
        public override bool isSupportedOnDevice => Supported;

        uint m_WarnCounter;
        const int k_WarningThrottleFrames = 60 * 1; // 60 FPS * 1 sec

        // Cache only success: querying before the Metal device is ready returns false, and caching that would
        // disable MetalFX for the whole session. Re-query until it reports supported, then latch.
        bool m_Supported;
        bool Supported => m_Supported || (m_Supported = MetalFXDevice.IsTemporalSupported());
        // Scale range is device-fixed and only read after Supported is true, so caching once is safe.
        float? m_MaxScale;
        float MaxScale => m_MaxScale ??= MetalFXDevice.GetTemporalMaxScale();
        float? m_MinScale;
        float MinScale => m_MinScale ??= MetalFXDevice.GetTemporalMinScale();

        void WarnThrottled(string message)
        {
            if (m_WarnCounter % k_WarningThrottleFrames == 0)
                Debug.LogWarning(message);
            unchecked { m_WarnCounter++; }
        }

        class PassData
        {
            public MetalFXTemporalContext context;
            public TextureHandle color;
            public TextureHandle depth;
            public TextureHandle motion;
            public TextureHandle output;
            public Vector2 jitter;
            public Vector2 motionVectorScale;
            public bool reset;
            public bool depthReversed;
            public Vector2Int inputContentSize;
            public Vector2Int maxRenderSize;
            public Vector2Int outputSize;
            public bool dynamicResolution;
        }

        public MetalFXTemporalUpscaler() { }

        public override void CalculateJitter(int frameIndex, float upscaleRatio, out Vector2 jitter, out bool allowScaling)
        {
            // When temporal isn't supported the camera falls back to the pipeline's default filtering, which can't
            // resolve jitter. Apply no jitter so the fallback doesn't shimmer. When supported, use the framework default.
            if (!Supported)
            {
                jitter = Vector2.zero;
                allowScaling = false;
                return;
            }

            base.CalculateJitter(frameIndex, upscaleRatio, out jitter, out allowScaling);
        }

        public override UpscalerResolutionInfo GetResolutionInfo(Vector2Int displayResolution, UpscalerOptions options)
        {
            if (!Supported)
                return base.GetResolutionInfo(displayResolution, options);

            // maxScale == 0 means the range isn't queryable on this OS: the scaler runs at its fixed
            // creation ratio, so there is nothing to clamp.
            float maxScale = MaxScale;
            if (maxScale <= 0f)
                return base.GetResolutionInfo(displayResolution, options);

            float minScale = MinScale;
            if (minScale <= 0f)
                minScale = 1.0f; // defensive: avoid divide-by-zero.

            // ratio = display / renderRes must stay within [minScale, maxScale]
            var minRes = new Vector2Int(
                Mathf.CeilToInt(displayResolution.x / maxScale),
                Mathf.CeilToInt(displayResolution.y / maxScale));
            var maxRes = new Vector2Int(
                Mathf.Min(displayResolution.x, Mathf.FloorToInt(displayResolution.x / minScale)),
                Mathf.Min(displayResolution.y, Mathf.FloorToInt(displayResolution.y / minScale)));

            return UpscalerResolutionInfo.Range(minRes, maxRes);
        }

        public override IUpscalerContext CreateContext(UpscalerOptions options, Vector2Int displayResolution)
        {
            return new MetalFXTemporalContext(displayResolution);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // When MetalFX temporal can't run, return without producing an output.
            if (!Supported)
            {
                WarnThrottled("MetalFX Temporal is not supported on this device/OS; the upscaler is skipped and the camera falls back to default filtering.");
                return;
            }

            UpscalingIO io = frameData.Get<UpscalingIO>();

            // MetalFX temporal only supports a device-specific upscale ratio range.
            // When maxScale is zero, the scaler just runs at its fixed creation ratio.
            float maxScale = MaxScale;
            if (maxScale > 0f)
            {
                float minScale = MinScale;
                float scaleX = (float)io.postUpscaleResolution.x / io.preUpscaleResolution.x;
                float scaleY = (float)io.postUpscaleResolution.y / io.preUpscaleResolution.y;
                if (scaleX > maxScale || scaleY > maxScale || scaleX < minScale || scaleY < minScale)
                {
                    WarnThrottled($"MetalFXTemporalUpscaler: requested upscale ratio ({scaleX:F2}x{scaleY:F2}) " +
                                  $"is outside the supported range [{minScale:F2}, {maxScale:F2}]; the upscaler is skipped and the camera falls back to default filtering.");
                    return;
                }
            }

            var context = io.context as MetalFXTemporalContext;
            if (context == null)
            {
                WarnThrottled("MetalFXTemporalUpscaler: No valid context provided via io.context; the upscaler is skipped and the camera falls back to default filtering.");
                return;
            }

            TextureHandle outputColor;
            {
                TextureDesc inputDesc = io.cameraColor.GetDescriptor(renderGraph);
                TextureDesc outputDesc = inputDesc;
                outputDesc.width = io.postUpscaleResolution.x;
                outputDesc.height = io.postUpscaleResolution.y;
                outputDesc.msaaSamples = MSAASamples.None;
                outputDesc.useMipMap = false;
                outputDesc.autoGenerateMips = false;
                outputDesc.useDynamicScale = false;
                outputDesc.discardBuffer = false;
                outputDesc.enableRandomWrite = true;
                outputDesc.name = "_MetalFXTemporalOutput";
                outputDesc.clearBuffer = false;
                outputDesc.filterMode = FilterMode.Bilinear;
                outputColor = renderGraph.CreateTexture(outputDesc);
            }

            // Convert Unity motion vectors to the pixel-space offsets MetalFX expects.
            float motionVectorSign = io.motionVectorDirection == UpscalingIO.MotionVectorDirection.PreviousFrameToCurrentFrame ? -1.0f : 1.0f;
            float motionVectorScaleX = motionVectorSign * (io.motionVectorDomain == UpscalingIO.MotionVectorDomain.NDC ? io.motionVectorTextureSize.x : 1.0f);
            float motionVectorScaleY = motionVectorSign * (io.motionVectorDomain == UpscalingIO.MotionVectorDomain.NDC ? io.motionVectorTextureSize.y : 1.0f);

            using (var builder = renderGraph.AddUnsafePass<PassData>("MetalFX Temporal", out PassData passData, k_MetalFXTemporal))
            {
                passData.context = context;
                passData.color = io.cameraColor;
                passData.depth = io.cameraDepth;
                passData.motion = io.motionVectorColor;
                passData.output = outputColor;
                passData.jitter = io.subpixelJitter;
                passData.motionVectorScale = new Vector2(motionVectorScaleX, motionVectorScaleY);
                passData.reset = io.resetHistory;
                passData.depthReversed = io.invertedDepth;
                passData.inputContentSize = io.preUpscaleResolution;
                // maxPreUpscaleResolution is sticky and can exceed a shrunken display.
                // MetalFX only supports upscaling configurations (creation input <= output), so clamp the baked input
                // size to the output; the native texture copies content through a scratch when sizes mismatch.
                passData.maxRenderSize = Vector2Int.Min(io.maxPreUpscaleResolution, io.postUpscaleResolution);
                passData.outputSize = io.postUpscaleResolution;
                passData.dynamicResolution = io.dynamicResolution.HasValue;

                builder.UseTexture(io.cameraColor);
                builder.UseTexture(io.cameraDepth);
                builder.UseTexture(io.motionVectorColor);
                builder.UseTexture(outputColor, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
                {
                    Texture color = data.color;
                    Texture depth = data.depth;
                    Texture motion = data.motion;
                    Texture output = data.output;
                    if (color == null || depth == null || motion == null || output == null)
                        return;

                    IntPtr colorPtr = color.GetNativeTexturePtr();
                    IntPtr depthPtr = depth.GetNativeTexturePtr();
                    IntPtr motionPtr = motion.GetNativeTexturePtr();
                    IntPtr outputPtr = output.GetNativeTexturePtr();

                    CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);

                    MetalFXTemporalNative native = data.context.GetOrCreateNativeContext(
                        cmd, colorPtr, depthPtr, motionPtr, outputPtr, data.dynamicResolution,
                        color.graphicsFormat, depth.graphicsFormat, motion.graphicsFormat, output.graphicsFormat,
                        data.maxRenderSize, data.outputSize);
                    if (native == null)
                        return;

                    var dispatch = new MetalFXTemporalDispatchData
                    {
                        context = native.scaler,
                        color = colorPtr,
                        depth = depthPtr,
                        motion = motionPtr,
                        destination = outputPtr,
                        jitterX = data.jitter.x,
                        jitterY = data.jitter.y,
                        motionVectorScaleX = data.motionVectorScale.x,
                        motionVectorScaleY = data.motionVectorScale.y,
                        reset = data.reset ? 1 : 0,
                        depthReversed = data.depthReversed ? 1 : 0,
                        inputContentWidth = data.inputContentSize.x,
                        inputContentHeight = data.inputContentSize.y,
                    };

                    MetalFXDevice.Dispatch(cmd, MetalFXDevice.k_EventDispatchTemporal, in dispatch);
                });
            }

            io.cameraColor = outputColor;
        }
    }
}
#endif
