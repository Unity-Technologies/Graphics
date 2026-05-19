using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
#if ENABLE_UPSCALER_FRAMEWORK && ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
using UnityEngine.NVIDIA;
#endif
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if ENABLE_UPSCALER_FRAMEWORK && ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
static class RegisterDLSS
{
    static RegisterDLSS() => UpscalerRegistry.Register<DLSSIUpscaler, DLSSOptions>(DLSSIUpscaler.upscalerName);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitRuntime() => UpscalerRegistry.Register<DLSSIUpscaler, DLSSOptions>(DLSSIUpscaler.upscalerName);
}

/// <summary>
/// Per-camera context for DLSS upscaling. Wraps the native NVIDIA DLSSContext
/// and tracks the settings it was created with for validation.
/// Native context creation is deferred until first use (requires CommandBuffer).
/// </summary>
public class DLSSUpscalerContext : PluginUpscalerContext<DLSSContext, DLSSOptions>
{
    private readonly DLSSQuality m_CreatedWithQuality;
    private readonly bool m_CreatedWithFixedResolutionMode;
    private readonly DLSSPreset m_CreatedWithPresetQuality;
    private readonly DLSSPreset m_CreatedWithPresetBalanced;
    private readonly DLSSPreset m_CreatedWithPresetPerformance;
    private readonly DLSSPreset m_CreatedWithPresetUltraPerformance;
    private readonly DLSSPreset m_CreatedWithPresetDLAA;

    /// <summary>
    /// Creates a new DLSS context wrapper. Native context creation is deferred.
    /// </summary>
    public DLSSUpscalerContext(DLSSOptions options, Vector2Int displayResolution)
        : base(displayResolution)
    {
        m_CreatedWithQuality = options.dlssQualityMode;
        m_CreatedWithFixedResolutionMode = options.fixedResolutionMode;
        m_CreatedWithPresetQuality = options.dlssRenderPresetQuality;
        m_CreatedWithPresetBalanced = options.dlssRenderPresetBalanced;
        m_CreatedWithPresetPerformance = options.dlssRenderPresetPerformance;
        m_CreatedWithPresetUltraPerformance = options.dlssRenderPresetUltraPerformance;
        m_CreatedWithPresetDLAA = options.dlssRenderPresetDLAA;
    }

    /// <summary>
    /// Gets the native DLSS context, creating it if necessary.
    /// </summary>
    /// <param name="cmd">Command buffer to record creation commands.</param>
    /// <param name="settings">Initialization settings for the native context.</param>
    /// <returns>The native DLSS context.</returns>
    public DLSSContext GetOrCreateNativeContext(CommandBuffer cmd, DLSSCommandInitializationData settings)
    {
        m_NativeContext ??= GraphicsDevice.device.CreateFeature(cmd, settings);
        return m_NativeContext;
    }

    /// <inheritdoc/>
    protected override void DestroyNativeContext(CommandBuffer cmd, DLSSContext context)
        => GraphicsDevice.device.DestroyFeature(cmd, context);

    /// <inheritdoc/>
    protected override bool ValidateOptions(DLSSOptions options)
    {
        // Quality mode, fixed resolution mode, and preset changes require context recreation
        return options.dlssQualityMode == m_CreatedWithQuality &&
               options.fixedResolutionMode == m_CreatedWithFixedResolutionMode &&
               options.dlssRenderPresetQuality == m_CreatedWithPresetQuality &&
               options.dlssRenderPresetBalanced == m_CreatedWithPresetBalanced &&
               options.dlssRenderPresetPerformance == m_CreatedWithPresetPerformance &&
               options.dlssRenderPresetUltraPerformance == m_CreatedWithPresetUltraPerformance &&
               options.dlssRenderPresetDLAA == m_CreatedWithPresetDLAA;
    }
}

public class DLSSIUpscaler : AbstractUpscaler
{
    public static readonly string upscalerName = "Deep Learning Super Sampling 4";

#region DLSS_UTILITIES
    static bool CheckDLSSFeatureAvailable()
    {
        // check plugin availability
        if (!UnityEngine.NVIDIA.NVUnityPlugin.IsLoaded())
        {
            Debug.LogWarning("NVUnityPlugin not loaded.");
            return false;
        }

        // check GPU vendor
        if (!SystemInfo.graphicsDeviceVendor.ToLowerInvariant().Contains("nvidia"))
        {
            Debug.LogWarning("DLSS not available on non-NVIDIA graphics cards.");
            return false;
        }

        // check device
        UnityEngine.NVIDIA.GraphicsDevice device = UnityEngine.NVIDIA.GraphicsDevice.CreateGraphicsDevice();
        if (device == null)
        {
            Debug.LogWarning("NVUnityPlugin failed to create device.");
            return false;
        }

        // check DLSS feature
        if(!device.IsFeatureAvailable(UnityEngine.NVIDIA.GraphicsDeviceFeature.DLSS))
        {
            Debug.LogWarning("DLSS not available on the current NVIDIA graphics card.");
            return false;
        }

        return true;
    }
#endregion // DLSS_UTILITIES
    

#region RENDERGRAPH_INTERFACE_DATA
    class DLSSGraphData
    {
        public DLSSUpscalerContext upscalerContext;
        public bool needsInitSettings; // True when native context doesn't exist yet
        public DLSSCommandInitializationData initSettings;
        public DLSSCommandExecutionData execData;
        public TextureHandle colorInput;
        public TextureHandle depth;
        public TextureHandle motionVectors;
        public TextureHandle colorOutput;
    };
#endregion

#region IUPSCALER_INTERFACE
    public DLSSIUpscaler(DLSSOptions o)
    {
        // check availability
        if(!CheckDLSSFeatureAvailable())
        {
            m_DLSSReady = false;
            return;
        }

        // setup options
        m_Options = o;
        if (m_Options == null)
        {
            Debug.LogWarning("null options given to DLSSIUpscaler()");
            m_Options = (DLSSOptions)ScriptableObject.CreateInstance(typeof(DLSSOptions));
            m_Options.upscalerName = name;
        }
        if (string.IsNullOrEmpty(m_Options.upscalerName))
        {
            Debug.LogWarning("options given with empty ID");
            m_Options.upscalerName = name;
        }

        m_DLSSReady = true;
    }

    public override string name => upscalerName;
    public override bool isTemporal => true;
    public override bool supportsSharpening => false;
    public override UpscalerOptions options => m_Options;

    public override IUpscalerContext CreateContext(UpscalerOptions options, Vector2Int displayResolution)
    {
        if (!m_DLSSReady || options is not DLSSOptions dlssOptions)
            return null;

        return new DLSSUpscalerContext(dlssOptions, displayResolution);
    }

    public override void CalculateJitter(int frameIndex, float upscaleRatio, out Vector2 jitter, out bool allowScaling)
    {
        int numPhases = CalculateJitterPhaseCount(upscaleRatio);
        int haltonIndex = (frameIndex % numPhases) + 1;
        float x = HaltonSequence.Get(haltonIndex, 2) - 0.5f;
        float y = HaltonSequence.Get(haltonIndex, 3) - 0.5f;
        jitter = new Vector2(x, y);
        allowScaling = false;
    }

    public override void NegotiatePreUpscaleResolution(ref Vector2Int preUpscaleResolution, Vector2Int postUpscaleResolution)
    {
        if(m_Options.fixedResolutionMode)
        {
            Debug.Assert(GraphicsDevice.device != null);

            DLSSQuality qualityMode = (DLSSQuality)m_Options.dlssQualityMode;
            GraphicsDevice.device.GetOptimalSettings(
                (uint)postUpscaleResolution.x,
                (uint)postUpscaleResolution.y,
                qualityMode,
                out OptimalDLSSSettingsData dlssOptimalData
            );
            preUpscaleResolution.x = (int)dlssOptimalData.outRenderWidth;
            preUpscaleResolution.y = (int)dlssOptimalData.outRenderHeight;
        }
    }

    public override float CalculateMipBias(Vector2Int preUpscaleResolution, Vector2Int postUpscaleResolution)
    {
        // NVUnityPlugin should provide this value.
        float xBias = Mathf.Log((float)preUpscaleResolution.x / postUpscaleResolution.x, 2f);
        float yBias = Mathf.Log((float)preUpscaleResolution.y / postUpscaleResolution.y, 2f);
        return Mathf.Min(xBias, yBias) - 1.0f;
    }

    static int CalculateJitterPhaseCount(float upscaleRatio)
    {
        const float k_BasePhaseCount = 8.0f;
        return (int)(k_BasePhaseCount * upscaleRatio * upscaleRatio);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if(!m_DLSSReady)
            return;

        Debug.Assert(GraphicsDevice.device != null);

        UpscalingIO io = frameData.Get<UpscalingIO>();

        // Get the per-camera context from UpscalingIO (set by the pipeline)
        var upscalerContext = io.context as DLSSUpscalerContext;
        if (upscalerContext == null)
        {
            Debug.LogWarning("DLSSIUpscaler: No valid context provided via io.context. Skipping upscaling.");
            return;
        }

        // describe output texture
        TextureHandle outputColor;
        {
            TextureDesc inputDesc = io.cameraColor.GetDescriptor(renderGraph);
            TextureDesc outputDesc = inputDesc;
            outputDesc.width = io.postUpscaleResolution.x;
            outputDesc.height = io.postUpscaleResolution.y;

            outputDesc.format = inputDesc.format;
            outputDesc.msaaSamples = MSAASamples.None;
            outputDesc.useMipMap = false;
            outputDesc.autoGenerateMips = false;
            outputDesc.useDynamicScale = false;
            outputDesc.anisoLevel = 0;
            outputDesc.discardBuffer = false;
            outputDesc.enableRandomWrite = true; // compute shader resource
            outputDesc.name = "_DLSSOutputTarget";
            outputDesc.clearBuffer = false;
            outputDesc.filterMode = FilterMode.Bilinear;
            outputColor = renderGraph.CreateTexture(outputDesc);
        }

        using (var builder = renderGraph.AddUnsafePass<DLSSGraphData>("Deep Learning Super Sampling", out DLSSGraphData passData, new ProfilingSampler("DLSS")))
        {
            float motionVectorSign = io.motionVectorDirection == UpscalingIO.MotionVectorDirection.PreviousFrameToCurrentFrame ? -1.0f : 1.0f;
            float motionVectorScaleX = io.motionVectorDomain == UpscalingIO.MotionVectorDomain.NDC ? io.motionVectorTextureSize.x : 1.0f;
            float motionVectorScaleY = io.motionVectorDomain == UpscalingIO.MotionVectorDomain.NDC ? io.motionVectorTextureSize.y : 1.0f;

            // Setup pass data
            passData.upscalerContext = upscalerContext;
            passData.needsInitSettings = !upscalerContext.hasNativeContext;

            // Only build initialization settings when native context needs to be created
            if (passData.needsInitSettings)
            {
                bool mvLowResolution = io.motionVectorTextureSize.x <= io.preUpscaleResolution.x ||
                                       io.motionVectorTextureSize.y <= io.preUpscaleResolution.y;
                passData.initSettings = new DLSSCommandInitializationData();
                passData.initSettings.SetFlag(DLSSFeatureFlags.IsHDR, io.hdrInput);
                passData.initSettings.SetFlag(DLSSFeatureFlags.MVLowRes, mvLowResolution);
                passData.initSettings.SetFlag(DLSSFeatureFlags.DepthInverted, io.invertedDepth);
                passData.initSettings.SetFlag(DLSSFeatureFlags.MVJittered, io.jitteredMotionVectors);
                passData.initSettings.inputRTWidth = (uint)io.preUpscaleResolution.x;
                passData.initSettings.inputRTHeight = (uint)io.preUpscaleResolution.y;
                passData.initSettings.outputRTWidth = (uint)io.postUpscaleResolution.x;
                passData.initSettings.outputRTHeight = (uint)io.postUpscaleResolution.y;
                passData.initSettings.quality = (DLSSQuality)m_Options.dlssQualityMode;
                passData.initSettings.presetQualityMode = m_Options.dlssRenderPresetQuality;
                passData.initSettings.presetBalancedMode = m_Options.dlssRenderPresetBalanced;
                passData.initSettings.presetPerformanceMode = m_Options.dlssRenderPresetPerformance;
                passData.initSettings.presetUltraPerformanceMode = m_Options.dlssRenderPresetUltraPerformance;
                passData.initSettings.presetDlaaMode = m_Options.dlssRenderPresetDLAA;
            }

            // Per-frame execution data
            passData.execData.mvScaleX = motionVectorSign * motionVectorScaleX;
            passData.execData.mvScaleY = motionVectorSign * motionVectorScaleY;
            passData.execData.subrectOffsetX = 0;
            passData.execData.subrectOffsetY = 0;
            passData.execData.subrectWidth = (uint)io.preUpscaleResolution.x;
            passData.execData.subrectHeight = (uint)io.preUpscaleResolution.y;
            passData.execData.jitterOffsetX = io.subpixelJitter.x;
            passData.execData.jitterOffsetY = io.subpixelJitter.y;
            passData.execData.preExposure = Mathf.Clamp(io.preExposureValue, 0.20f, 2.0f); // clamp to a reasonable value to prevent ghosting
            passData.execData.invertYAxis = io.flippedY ? 1u : 0u;
            passData.execData.invertXAxis = io.flippedX ? 1u : 0u;
            passData.execData.reset = io.resetHistory ? 1 : 0;

            // Texture handles
            builder.UseTexture(io.cameraColor);
            builder.UseTexture(io.cameraDepth);
            builder.UseTexture(io.motionVectorColor);
            builder.UseTexture(outputColor, AccessFlags.Write);
            passData.colorInput = io.cameraColor;
            passData.depth = io.cameraDepth;
            passData.motionVectors = io.motionVectorColor;
            passData.colorOutput = outputColor;

            // set render function
            builder.SetRenderFunc((DLSSGraphData data, UnsafeGraphContext ctx) =>
            {
                CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);

                // Verify init settings were populated if native context needs to be created
                Debug.Assert(!data.needsInitSettings || data.initSettings.outputRTWidth > 0,
                    "DLSS init settings must be populated when native context doesn't exist");

                // Get the native context, creating it if necessary (uses pre-built init settings)
                DLSSContext nativeContext = data.upscalerContext.GetOrCreateNativeContext(cmd, data.initSettings);
                Debug.Assert(nativeContext != null);

                nativeContext.executeData = data.execData;
                DLSSTextureTable textureTable = new()
                {
                    colorInput = data.colorInput,
                    depth = data.depth,
                    motionVectors = data.motionVectors,
                    colorOutput = data.colorOutput,
                };

                GraphicsDevice.device.ExecuteDLSS(cmd, nativeContext, textureTable);
            });
        }

        io.cameraColor = outputColor;
    }
#endregion


    #region DATA
    private bool m_DLSSReady = false;
    private DLSSOptions m_Options = null;
    #endregion
}

#endif // ENABLE_UPSCALER_FRAMEWORK && ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
