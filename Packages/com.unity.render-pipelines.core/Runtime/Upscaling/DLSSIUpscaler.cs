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
    public DLSSQuality createdWithQuality { get; }
    public DLSSPreset createdWithPresetQuality { get; }
    public DLSSPreset createdWithPresetBalanced { get; }
    public DLSSPreset createdWithPresetPerformance { get; }
    public DLSSPreset createdWithPresetUltraPerformance { get; }
    public DLSSPreset createdWithPresetDLAA { get; }

    /// <summary>
    /// Creates a new DLSS context wrapper. Native context creation is deferred.
    /// Stores context-creation settings (quality, presets) for init and validation.
    /// Per-frame settings should be read from io.options in RecordRenderGraph.
    /// </summary>
    public DLSSUpscalerContext(DLSSOptions options, Vector2Int displayResolution)
        : base(displayResolution)
    {
        createdWithQuality = options.dlssQualityMode;
        createdWithPresetQuality = options.dlssRenderPresetQuality;
        createdWithPresetBalanced = options.dlssRenderPresetBalanced;
        createdWithPresetPerformance = options.dlssRenderPresetPerformance;
        createdWithPresetUltraPerformance = options.dlssRenderPresetUltraPerformance;
        createdWithPresetDLAA = options.dlssRenderPresetDLAA;
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
        // Quality mode and preset changes require context recreation
        return options.dlssQualityMode == createdWithQuality &&
               options.dlssRenderPresetQuality == createdWithPresetQuality &&
               options.dlssRenderPresetBalanced == createdWithPresetBalanced &&
               options.dlssRenderPresetPerformance == createdWithPresetPerformance &&
               options.dlssRenderPresetUltraPerformance == createdWithPresetUltraPerformance &&
               options.dlssRenderPresetDLAA == createdWithPresetDLAA;
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
    public DLSSIUpscaler()
    {
        m_DLSSReady = CheckDLSSFeatureAvailable();
    }

    public override string name => upscalerName;
    public override bool isTemporal => true;
    public override bool supportsSharpening => false;
    public override bool hasQualityMode => true;

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

    public override UpscalerResolutionInfo GetResolutionInfo(Vector2Int displayResolution, UpscalerOptions options)
    {
        var dlssOptions = options as DLSSOptions;
        if (!m_DLSSReady || dlssOptions == null)
            return UpscalerResolutionInfo.Fixed(displayResolution);

        // Query the DLSS SDK for the optimal render resolution and the supported [min,max] range for this quality mode.
        if (GraphicsDevice.device.GetOptimalSettings(
                (uint)displayResolution.x,
                (uint)displayResolution.y,
                dlssOptions.dlssQualityMode,
                out OptimalDLSSSettingsData optimalSettings))
        {
            var optimal = new Vector2Int((int)optimalSettings.outRenderWidth, (int)optimalSettings.outRenderHeight);
            var min = new Vector2Int((int)optimalSettings.minWidth, (int)optimalSettings.minHeight);
            var max = new Vector2Int((int)optimalSettings.maxWidth, (int)optimalSettings.maxHeight);

            // The render resolution must always stay within [min,max] or DLSS Evaluate fails, so even CustomScaling
            // reports the range as a clamp (the pipeline drives within it). When the quality mode has no range
            // (min == max — e.g. UltraPerformance, or an older DLSS library) only the optimal is valid, so pin to it.
            switch (dlssOptions.resolutionMode)
            {
                case UpscalerResolutionMode.CustomScaling:
                    return (min != max)
                        ? UpscalerResolutionInfo.Range(min, max)
                        : UpscalerResolutionInfo.Fixed(optimal);

                default: // QualityMode
                    return UpscalerResolutionInfo.Fixed(optimal);
            }
        }

        // Fallback if GetOptimalSettings fails
        return UpscalerResolutionInfo.Fixed(displayResolution);
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
        // Round half up, don't truncate: upscaleRatio is reconstructed as display/round(renderSize), so a clean preset
        // (e.g. 1.5x) arrives slightly low (~1.497) and 8*1.497^2 = 17.93 would truncate to 17 instead of 18.
        return Mathf.FloorToInt(k_BasePhaseCount * upscaleRatio * upscaleRatio + 0.5f);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if(!m_DLSSReady)
            return;

        Debug.Assert(GraphicsDevice.device != null);

        UpscalingIO io = frameData.Get<UpscalingIO>();

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
                // The DLSS SDK requires the feature to be created at the quality mode's optimal render size (not the max,
                // display, or current size) regardless of resolution mode. The per-frame subrect (execData below) is the
                // actual render size; it may vary but must stay within the quality mode's [min,max] or Evaluate fails.
                // The pipeline allocates the input color buffer at the max so the subrect always fits.
                Vector2Int optimalRenderSize = io.preUpscaleResolution; // fallback if the SDK query fails
                if (GraphicsDevice.device.GetOptimalSettings(
                        (uint)io.postUpscaleResolution.x, (uint)io.postUpscaleResolution.y,
                        upscalerContext.createdWithQuality, out OptimalDLSSSettingsData optimalSettings))
                {
                    optimalRenderSize = new Vector2Int((int)optimalSettings.outRenderWidth, (int)optimalSettings.outRenderHeight);
                }
                passData.initSettings.inputRTWidth = (uint)optimalRenderSize.x;
                passData.initSettings.inputRTHeight = (uint)optimalRenderSize.y;
                passData.initSettings.outputRTWidth = (uint)io.postUpscaleResolution.x;
                passData.initSettings.outputRTHeight = (uint)io.postUpscaleResolution.y;
                passData.initSettings.quality = upscalerContext.createdWithQuality;
                passData.initSettings.presetQualityMode = upscalerContext.createdWithPresetQuality;
                passData.initSettings.presetBalancedMode = upscalerContext.createdWithPresetBalanced;
                passData.initSettings.presetPerformanceMode = upscalerContext.createdWithPresetPerformance;
                passData.initSettings.presetUltraPerformanceMode = upscalerContext.createdWithPresetUltraPerformance;
                passData.initSettings.presetDlaaMode = upscalerContext.createdWithPresetDLAA;
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
    #endregion
}

#endif // ENABLE_UPSCALER_FRAMEWORK && ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
