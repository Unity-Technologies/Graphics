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
    static class RegisterMetalFXSpatial
    {
        static RegisterMetalFXSpatial()
            => UpscalerRegistry.Register<MetalFXSpatialUpscaler, MetalFXSpatialOptions>(MetalFXSpatialUpscaler.registeredId, MetalFXSpatialUpscaler.registeredName);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InitRuntime()
            => UpscalerRegistry.Register<MetalFXSpatialUpscaler, MetalFXSpatialOptions>(MetalFXSpatialUpscaler.registeredId, MetalFXSpatialUpscaler.registeredName);
    }

    /// <summary>
    /// Native per-camera state for MetalFX Spatial: the native context handle and a stable dispatch-data buffer
    /// whose pointer must stay valid until the render thread consumes it.
    /// </summary>
    public sealed class MetalFXSpatialNative
    {
        public IntPtr scaler;        // context handle from MetalFX_CreateSpatialContext
    }

    public class MetalFXSpatialContext : PluginUpscalerContext<MetalFXSpatialNative, MetalFXSpatialOptions>
    {
        public MetalFXSpatialContext(Vector2Int displayResolution)
            : base(displayResolution)
        {
        }

        GraphicsFormat m_ColorFormat;
        GraphicsFormat m_OutputFormat;
        Vector2Int m_MaxRenderSize;
        Vector2Int m_OutputSize;
        bool m_Hdr;

        public MetalFXSpatialNative GetOrCreateNativeContext(CommandBuffer cmd, IntPtr source, IntPtr destination,
                                                             GraphicsFormat colorFormat, GraphicsFormat outputFormat,
                                                             Vector2Int maxRenderSize, Vector2Int outputSize, bool hdr)
        {
            if (m_NativeContext != null && (colorFormat != m_ColorFormat || outputFormat != m_OutputFormat ||
                                            maxRenderSize != m_MaxRenderSize || outputSize != m_OutputSize ||
                                            hdr != m_Hdr))
            {
                DestroyNativeContext(cmd, m_NativeContext);
                m_NativeContext = null;
            }

            if (m_NativeContext == null)
            {
                IntPtr scaler = MetalFXDevice.CreateSpatialContext(source, destination, maxRenderSize.x, maxRenderSize.y,
                                                                   outputSize.x, outputSize.y, hdr);
                if (scaler != IntPtr.Zero)
                {
                    m_NativeContext = new MetalFXSpatialNative
                    {
                        scaler = scaler,
                    };
                    m_ColorFormat = colorFormat;
                    m_OutputFormat = outputFormat;
                    m_MaxRenderSize = maxRenderSize;
                    m_OutputSize = outputSize;
                    m_Hdr = hdr;
                }
            }
            return m_NativeContext;
        }

        /// <inheritdoc/>
        protected override void DestroyNativeContext(CommandBuffer cmd, MetalFXSpatialNative context)
        {
            if (context.scaler == IntPtr.Zero)
                return;

            // Route the release through the render thread
            Debug.Assert(cmd != null, "DestroyNativeContext requires a command buffer to release on the render thread.");
            MetalFXDevice.DestroyContext(cmd, context.scaler);
        }

        /// <inheritdoc/>
        protected override bool ValidateOptions(MetalFXSpatialOptions options) => true;
    }

#if UNITY_EDITOR
    [UpscalerSupportedBuildTarget(BuildTarget.StandaloneOSX, BuildTarget.iOS, BuildTarget.tvOS, BuildTarget.VisionOS, graphicsDeviceTypes = new[] { GraphicsDeviceType.Metal })]
#endif
    public class MetalFXSpatialUpscaler : AbstractUpscaler
    {
        public static readonly string registeredId = "apple.metalfx-spatial";
        public static readonly string registeredName = "MetalFX Spatial";

        static readonly ProfilingSampler k_MetalFXSpatial = ProfilingSampler.Create("MetalFXSpatial", MarkerFlags.Default);

        public override string upscalerId => registeredId;
        public override string name => registeredName;
        public override bool isTemporal => false;
        public override bool supportsSharpening => false;
        public override bool isSupportedOnDevice => Supported;

        // MetalFX Spatial is a single-frame upscaler with no temporal reprojection, so it reports zero jitter.
        public override void CalculateJitter(int frameIndex, float upscaleRatio, out Vector2 jitter, out bool allowScaling)
        {
            jitter = Vector2.zero;
            allowScaling = false;
        }

        uint m_WarnCounter;
        const int k_WarningThrottleFrames = 60 * 1; // 60 FPS * 1 sec

        // Cache only success: querying before the Metal device is ready returns false, and caching that would
        // disable MetalFX for the whole session. Re-query until it reports supported, then latch.
        bool m_Supported;
        bool Supported => m_Supported || (m_Supported = MetalFXDevice.IsSpatialSupported());

        void WarnThrottled(string message)
        {
            if (m_WarnCounter % k_WarningThrottleFrames == 0)
                Debug.LogWarning(message);
            unchecked { m_WarnCounter++; }
        }

        class PassData
        {
            public MetalFXSpatialContext context;
            public TextureHandle input;
            public TextureHandle output;
            public Vector2Int inputContentSize;
            public Vector2Int maxRenderSize;
            public Vector2Int outputSize;
            public bool hdrInput;
        }

        public MetalFXSpatialUpscaler() { }

        public override IUpscalerContext CreateContext(UpscalerOptions options, Vector2Int displayResolution)
        {
            return new MetalFXSpatialContext(displayResolution);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // When MetalFX spatial is unavailable, return without producing an output.
            if (!Supported)
            {
                WarnThrottled("MetalFX Spatial is not supported on this device/OS; the upscaler is skipped and the camera falls back to default filtering.");
                return;
            }

            UpscalingIO io = frameData.Get<UpscalingIO>();

            var context = io.context as MetalFXSpatialContext;
            if (context == null)
                return;

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
                outputDesc.name = "_MetalFXSpatialOutput";
                outputDesc.clearBuffer = false;
                outputDesc.filterMode = FilterMode.Bilinear;
                outputColor = renderGraph.CreateTexture(outputDesc);
            }

            using (var builder = renderGraph.AddUnsafePass<PassData>("MetalFX Spatial", out PassData passData, k_MetalFXSpatial))
            {
                passData.context = context;
                passData.input = io.cameraColor;
                passData.output = outputColor;
                passData.inputContentSize = io.preUpscaleResolution;
                // maxPreUpscaleResolution is sticky and can exceed a shrunken display.
                // MetalFX only supports upscaling configurations (creation input <= output), so clamp the baked input
                // size to the output; the native texture copies content through a scratch when sizes mismatch.
                passData.maxRenderSize = Vector2Int.Min(io.maxPreUpscaleResolution, io.postUpscaleResolution);
                passData.outputSize = io.postUpscaleResolution;
                passData.hdrInput = io.hdrInput;

                builder.UseTexture(io.cameraColor);
                builder.UseTexture(outputColor, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
                {
                    Texture src = data.input;
                    Texture dst = data.output;
                    if (src == null || dst == null)
                        return;

                    IntPtr srcPtr = src.GetNativeTexturePtr();
                    IntPtr dstPtr = dst.GetNativeTexturePtr();

                    CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);

                    MetalFXSpatialNative native = data.context.GetOrCreateNativeContext(
                        cmd, srcPtr, dstPtr, src.graphicsFormat, dst.graphicsFormat, data.maxRenderSize,
                        data.outputSize, data.hdrInput);
                    if (native == null)
                        return;

                    var dispatch = new MetalFXSpatialDispatchData
                    {
                        context = native.scaler,
                        source = srcPtr,
                        destination = dstPtr,
                        inputContentWidth = data.inputContentSize.x,
                        inputContentHeight = data.inputContentSize.y,
                    };

                    MetalFXDevice.Dispatch(cmd, MetalFXDevice.k_EventDispatchSpatial, in dispatch);
                });
            }

            io.cameraColor = outputColor;
        }
    }
}
#endif
