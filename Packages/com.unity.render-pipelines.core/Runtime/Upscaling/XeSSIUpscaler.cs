using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
#if ENABLE_XESS && ENABLE_INTELXESS_MODULE
using UnityEngine.Intel;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

#if ENABLE_XESS && ENABLE_INTELXESS_MODULE

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
internal static class RegisterXeSS
{
    static RegisterXeSS() => UpscalerRegistry.Register<XeSSIUpscaler, XeSSOptions>(XeSSIUpscaler.registeredId, XeSSIUpscaler.registeredName);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitRuntime() => UpscalerRegistry.Register<XeSSIUpscaler, XeSSOptions>(XeSSIUpscaler.registeredId, XeSSIUpscaler.registeredName);
}

internal class XeSSIUpscalerContext : PluginUpscalerContext<XeSSNativeContext, XeSSOptions>
{
    public XeSSQuality createdWithQuality { get; }

    public Vector2Int createdWithMaxRenderSize { get; private set; }

    // Note that this flag contains e.g., validity of responsive mask and exposure mask parameters
    // so we don't need separate fields for them.
    public XeSSInitializationFlags createdWithFlags { get; private set; }

    public XeSSIUpscalerContext(XeSSOptions options, Vector2Int displayResolution)
        : base(displayResolution)
    {
        createdWithQuality = options.xessQualityMode;
    }

    public XeSSNativeContext EnsureNativeContext(CommandBuffer cmd, XeSSUpscalerCommandInitializationData settings)
    {
        var requestedMaxRenderSize = new Vector2Int((int)settings.maxRenderSizeWidth, (int)settings.maxRenderSizeHeight);
        bool recreate = createdWithMaxRenderSize != requestedMaxRenderSize || createdWithFlags != settings.xessFlags;
        if (m_NativeContext != null && recreate)
        {
            DestroyNativeContext(cmd, m_NativeContext);
            m_NativeContext = null;
        }
        if (m_NativeContext == null)
        {
            m_NativeContext = GraphicsDevice.device.CreateNativeContext(cmd, settings);
            createdWithMaxRenderSize = requestedMaxRenderSize;
            createdWithFlags = settings.xessFlags;
        }
        return m_NativeContext;
    }

    protected override void DestroyNativeContext(CommandBuffer cmd, XeSSNativeContext context)
    {
        GraphicsDevice.device.DestroyNativeContext(cmd, context);
    }

    protected override bool ValidateOptions(XeSSOptions options)
    {
        // Checks state changes that require context recreation.
        return options.xessQualityMode == createdWithQuality;
    }
}

#if UNITY_EDITOR
[UpscalerSupportedBuildTarget(BuildTarget.StandaloneWindows, graphicsDeviceTypes = new[] { GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Direct3D12, GraphicsDeviceType.Vulkan })]
#endif
internal class XeSSIUpscaler : AbstractUpscaler
{
    public static readonly string registeredId = "intel.xess";
    public static readonly string registeredName = "XeSS Super Resolution";

#region XESS_UTILITIES
    static bool CheckXeSSFeatureAvailable()
    {
        if (!XeSSUnityPlugin.Load())
        {
            Debug.LogWarning("Failed to load XeSSUnityPlugin.");
            return false;
        }

        var device = GraphicsDevice.CreateGraphicsDevice();
        if (device == null)
        {
            Debug.LogWarning("XeSSUnityPlugin failed to create its internal device.");
            return false;
        }

        return device.IsFeatureAvailable(GraphicsDeviceFeature.XeSSUpscale);
    }

    private bool IsSupportedOnDevice()
    {
        if (Debug.isDebugBuild && !m_XeSSReady && !m_WarnedUnsupportedDevice)
        {
            Debug.LogWarning("XeSS Super Resolution is not supported on current device.");
            m_WarnedUnsupportedDevice = true;
            return false;
        }
        if (Debug.isDebugBuild)
            m_WarnedUnsupportedDevice = false;
        return m_XeSSReady;
    }
#endregion // XESS_UTILITIES

#region RENDERGRAPH_INTERFACE_DATA
    class XeSSGraphData
    {
        public XeSSIUpscalerContext upscalerContext;
        public XeSSUpscalerCommandInitializationData initSettings;
        public XeSSUpscalerCommandExecutionData execData;
        public Vector2 motionVectorScale;
        public TextureHandle colorInput;
        public TextureHandle depth;
        public TextureHandle motionVectors;
        public TextureHandle exposure;
        public TextureHandle responsiveMask;
        public TextureHandle colorOutput;
    };
#endregion

#region IUPSCALER_INTERFACE
    public XeSSIUpscaler()
    {
        m_XeSSReady = CheckXeSSFeatureAvailable();
    }

    public override string upscalerId => registeredId;
    public override string name => registeredName;
    public override bool isSupportedOnDevice => IsSupportedOnDevice();
    public override bool isTemporal => true;
    public override bool supportsSharpening => false;
    public override bool hasQualityMode => true;
    public override ReactiveMaskSource reactiveMaskSource => ReactiveMaskSource.Color;

    public override IUpscalerContext CreateContext(UpscalerOptions options, Vector2Int displayResolution)
    {
        if (!m_XeSSReady || options is not XeSSOptions xessOptions)
            return null;

        return new XeSSIUpscalerContext(xessOptions, displayResolution);
    }

    public override void CalculateJitter(int frameIndex, float upscaleRatio, out Vector2 jitter, out bool allowScaling)
    {
        if (m_XeSSReady)
        {
            const float k_BasePhaseCount = 8.0f;
            int numPhases = Mathf.FloorToInt(k_BasePhaseCount * upscaleRatio * upscaleRatio + 0.5f);

            int haltonIndex = (frameIndex % numPhases) + 1;
            float x = HaltonSequence.Get(haltonIndex, 2) - 0.5f;
            float y = HaltonSequence.Get(haltonIndex, 3) - 0.5f;

            jitter = new Vector2(x, y);
        }
        else
        {
            jitter = Vector2.zero;
        }
        allowScaling = false;
    }

    public override UpscalerResolutionInfo GetResolutionInfo(Vector2Int displayResolution, UpscalerOptions options)
    {
        var xessOptions = options as XeSSOptions;
        if (!m_XeSSReady || xessOptions == null)
            return UpscalerResolutionInfo.Fixed(displayResolution);

        UpscalerResolutionInfo info;
        if (xessOptions.resolutionMode == UpscalerResolutionMode.CustomScaling)
        {
            info = base.GetResolutionInfo(displayResolution, options);
        }
        else
        {
            info = GraphicsDevice.device.GetRenderResolutionFromQualityMode(xessOptions.xessQualityMode,
                       (uint)displayResolution.x, (uint)displayResolution.y,
                       out uint renderWidth, out uint renderHeight)
                ? UpscalerResolutionInfo.Fixed(new Vector2Int((int)renderWidth, (int)renderHeight))
                : UpscalerResolutionInfo.Fixed(displayResolution); // SDK/device not ready
        }

        return info;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (!m_XeSSReady)
            return;

        UpscalingIO io = frameData.Get<UpscalingIO>();

        var xessOptions = io.options as XeSSOptions;

        var upscalerContext = io.context as XeSSIUpscalerContext;
        if (upscalerContext == null)
        {
            Debug.LogWarning("XeSSIUpscaler: No valid context provided via io.context. Skipping upscaling.");
            return;
        }

        TextureHandle outputColor;
        {
            TextureDesc inputDesc = io.cameraColor.GetDescriptor(renderGraph);
            TextureDesc outputDesc = inputDesc;
            outputDesc.width = io.postUpscaleResolution.x;
            outputDesc.height = io.postUpscaleResolution.y;
            outputDesc.format = GraphicsFormatUtility.GetLinearFormat(inputDesc.format);
            outputDesc.msaaSamples = MSAASamples.None;
            outputDesc.useMipMap = false;
            outputDesc.autoGenerateMips = false;
            outputDesc.useDynamicScale = false;
            outputDesc.anisoLevel = 0;
            outputDesc.discardBuffer = false;
            outputDesc.enableRandomWrite = true; // compute shader resource
            outputDesc.name = "_XeSSOutputTarget";
            outputDesc.clearBuffer = false;
            outputDesc.filterMode = FilterMode.Bilinear;

            outputColor = renderGraph.CreateTexture(outputDesc);
        }

        using (var builder = renderGraph.AddUnsafePass("XeSS", out XeSSGraphData passData, new ProfilingSampler("XeSS")))
        {
            bool useNdcVelocity = io.motionVectorDomain == UpscalingIO.MotionVectorDomain.NDC;
            // Note: Don't use exposure texture. URP does not provide it and how HDRP uses it does not play well with how XeSS samples it.
            const bool useExposureTexture = false;// io.exposureTexture.IsValid();
            bool hasResponsiveMaskTexture = io.reactiveMask.IsValid();
            float motionVectorSign = io.motionVectorDirection == UpscalingIO.MotionVectorDirection.PreviousFrameToCurrentFrame ? -1.0f : 1.0f;
            float motionVectorScaleX = useNdcVelocity ? io.motionVectorTextureSize.x : 1.0f;
            float motionVectorScaleY = useNdcVelocity ? io.motionVectorTextureSize.y : 1.0f;

            passData.upscalerContext = upscalerContext;

            // Fill init data always, but EnsureNativeContext rarely recreates the native context
            // therefore it won't cause reallocation of native context every frame.
            {
                bool displayResolutionMotionVectors = io.motionVectorTextureSize.x == io.postUpscaleResolution.x &&
                                                      io.motionVectorTextureSize.y == io.postUpscaleResolution.y;
                passData.initSettings = new XeSSUpscalerCommandInitializationData();
                passData.initSettings.SetFlag(XeSSInitializationFlags.HighResolutionMotionVector, displayResolutionMotionVectors);
                passData.initSettings.SetFlag(XeSSInitializationFlags.InvertedDepth, io.invertedDepth);
                passData.initSettings.SetFlag(XeSSInitializationFlags.ExposureScaleTexture, useExposureTexture);
                passData.initSettings.SetFlag(XeSSInitializationFlags.ResponsivePixelMask, hasResponsiveMaskTexture);
                passData.initSettings.SetFlag(XeSSInitializationFlags.UseNdcVelcoity, false); // motionVectorScale is used instead.
                passData.initSettings.SetFlag(XeSSInitializationFlags.ExternalDescriptorHeap, false);
                passData.initSettings.SetFlag(XeSSInitializationFlags.LdrInputColor, !io.hdrInput);
                passData.initSettings.SetFlag(XeSSInitializationFlags.JitteredMotionVector, io.jitteredMotionVectors);
                passData.initSettings.SetFlag(XeSSInitializationFlags.EnableAutoExposure, !useExposureTexture);
                passData.initSettings.maxRenderSizeWidth = (uint)io.maxPreUpscaleResolution.x;
                passData.initSettings.maxRenderSizeHeight = (uint)io.maxPreUpscaleResolution.y;
                passData.initSettings.displaySizeWidth = (uint)io.postUpscaleResolution.x;
                passData.initSettings.displaySizeHeight = (uint)io.postUpscaleResolution.y;
                passData.initSettings.qualitySetting = xessOptions.xessQualityMode;
            }

            // Per-frame execution data read from io.options, so live changes (e.g. jitter offset) apply without recreating the context.
            passData.execData.jitterOffsetX = io.subpixelJitter.x;
            passData.execData.jitterOffsetY = io.subpixelJitter.y;
            passData.execData.exposureScale = (io.preExposureValue == 0.0f) ? 1.0f : io.preExposureValue;
            passData.execData.resetHistory = io.resetHistory ? 1u : 0u;
            passData.execData.inputWidth = (uint)io.preUpscaleResolution.x;
            passData.execData.inputHeight = (uint)io.preUpscaleResolution.y;
            passData.execData.inputColorBaseX = 0;
            passData.execData.inputColorBaseY = 0;
            passData.execData.inputMotionVectorBaseX = 0;
            passData.execData.inputMotionVectorBaseY = 0;
            passData.execData.inputDepthBaseX = 0;
            passData.execData.inputDepthBaseY = 0;
            passData.execData.inputResponsiveMaskBaseX = 0;
            passData.execData.inputResponsiveMaskBaseY = 0;
            passData.execData.outputColorBaseX = 0;
            passData.execData.outputColorBaseY = 0;

            // In XeSS SDK, motion vector scale is not a part of execution parameter structs, so follow its design.
            passData.motionVectorScale = new Vector2(motionVectorSign * motionVectorScaleX, motionVectorSign * motionVectorScaleY);

            // Texture handles
            passData.colorInput = io.cameraColor;
            passData.depth = io.cameraDepth;
            passData.motionVectors = io.motionVectorColor;
            passData.exposure = useExposureTexture ? io.exposureTexture : TextureHandle.nullHandle;
            passData.responsiveMask = io.reactiveMask;
            passData.colorOutput = outputColor;

            builder.UseTexture(passData.colorInput);
            builder.UseTexture(passData.depth);
            builder.UseTexture(passData.motionVectors);
            if (passData.exposure.IsValid())
                builder.UseTexture(passData.exposure);
            if (passData.responsiveMask.IsValid())
                builder.UseTexture(passData.responsiveMask);
            builder.UseTexture(outputColor, AccessFlags.Write);

            builder.SetRenderFunc((XeSSGraphData data, UnsafeGraphContext ctx) =>
            {
                CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);

                Debug.Assert(data.initSettings.displaySizeWidth > 0, "XeSS init settings must be populated");

                XeSSNativeContext nativeContext = data.upscalerContext.EnsureNativeContext(cmd, data.initSettings);
                Debug.Assert(nativeContext != null);

                nativeContext.executeData = data.execData;
                XeSSTextureTable textureTable = new()
                {
                    colorInput = data.colorInput,
                    depth = data.depth,
                    motionVectors = data.motionVectors,
                    exposureTexture = data.exposure,
                    responsiveMask = data.responsiveMask,
                    colorOutput = data.colorOutput,
                };

                GraphicsDevice.device.SetMotionVectorScale(nativeContext, data.motionVectorScale);
                GraphicsDevice.device.ExecuteUpscale(cmd, nativeContext, textureTable);
            });
        }

        io.cameraColor = outputColor;
    }
#endregion

#region DATA
    private bool m_XeSSReady = false;
    private bool m_WarnedUnsupportedDevice = false;
#endregion
}

#endif // ENABLE_XESS && ENABLE_INTELXESS_MODULE
