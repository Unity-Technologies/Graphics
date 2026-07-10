using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
#if ENABLE_UPSCALER_FRAMEWORK && ENABLE_AMD && ENABLE_AMD_MODULE
using UnityEngine.AMD;
#endif
using System;


#if UNITY_EDITOR
using UnityEditor;
#endif

#if ENABLE_UPSCALER_FRAMEWORK && ENABLE_AMD && ENABLE_AMD_MODULE

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
static class RegisterFSR2
{
    static RegisterFSR2() => UpscalerRegistry.Register<FSR2IUpscaler, FSR2Options>(FSR2IUpscaler.upscalerName);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitRuntime() => UpscalerRegistry.Register<FSR2IUpscaler, FSR2Options>(FSR2IUpscaler.upscalerName);
}

/// <summary>
/// Per-camera context for FSR2 upscaling. Wraps the native AMD FSR2Context
/// and tracks the settings it was created with for validation.
/// Native context creation is deferred until first use (requires CommandBuffer).
/// </summary>
public class FSR2UpscalerContext : PluginUpscalerContext<FSR2Context, FSR2Options>
{
    public FSR2Quality createdWithQuality { get; }

    /// <summary>
    /// The max render size (= io.maxPreUpscaleResolution) the native context was created for. The native context sizes
    /// its internal buffers to this and rejects a per-frame renderSize above it (FFX_ERROR_OUT_OF_RANGE), so it must be
    /// recreated when this changes. It is stable under hardware DRS (the full allocation; only the per-frame subrect
    /// varies) but changes when the render allocation does — switching resolution mode or changing Render Scale.
    /// </summary>
    public Vector2Int createdWithMaxRenderSize { get; private set; }

    /// <summary>
    /// The initialization flags the native context was baked with. They follow per-frame inputs (HDR format, DRS state)
    /// that can change without changing the display resolution or options, so a flag change must recreate the native
    /// context. (The init display size needs no tracking — a resolution change already recreates the whole context via
    /// <see cref="IUpscalerContext.createdForDisplayResolution"/>.)
    /// </summary>
    public FfxFsr2InitializationFlags createdWithFlags { get; private set; }

    /// <summary>
    /// Creates a new FSR2 context wrapper. Native context creation is deferred.
    /// Stores context-creation settings (quality). Per-frame settings (sharpness)
    /// should be read from io.options in RecordRenderGraph.
    /// </summary>
    public FSR2UpscalerContext(FSR2Options options, Vector2Int displayResolution)
        : base(displayResolution)
    {
        createdWithQuality = options.fsr2QualityMode;
    }

    /// <summary>
    /// Returns the native FSR2 context, creating it on first use and recreating it when the max render size or
    /// initialization flags in <paramref name="settings"/> differ from those the current context was created with.
    /// </summary>
    /// <param name="cmd">Command buffer to record any creation/destruction commands.</param>
    /// <param name="settings">Initialization settings for the native context (carries the requested max render size and flags).</param>
    /// <returns>The native FSR2 context, valid for the requested max render size and flags.</returns>
    public FSR2Context EnsureNativeContext(CommandBuffer cmd, FSR2CommandInitializationData settings)
    {
        var requestedMaxRenderSize = new Vector2Int((int)settings.maxRenderSizeWidth, (int)settings.maxRenderSizeHeight);
        bool recreate = createdWithMaxRenderSize != requestedMaxRenderSize || createdWithFlags != settings.ffxFsrFlags;
        if (m_NativeContext != null && recreate)
        {
            DestroyNativeContext(cmd, m_NativeContext);
            m_NativeContext = null;
        }
        if (m_NativeContext == null)
        {
            m_NativeContext = GraphicsDevice.device.CreateFeature(cmd, settings);
            createdWithMaxRenderSize = requestedMaxRenderSize;
            createdWithFlags = settings.ffxFsrFlags;
        }
        return m_NativeContext;
    }

    /// <inheritdoc/>
    protected override void DestroyNativeContext(CommandBuffer cmd, FSR2Context context)
        => GraphicsDevice.device.DestroyFeature(cmd, context);

    /// <inheritdoc/>
    protected override bool ValidateOptions(FSR2Options options)
    {
        // Quality mode changes require context recreation
        // Sharpness changes do NOT require recreation (just parameters)
        return options.fsr2QualityMode == createdWithQuality;
    }
}

public class FSR2IUpscaler : AbstractUpscaler
{
    public static readonly string upscalerName = "FidelityFX Super Resolution 2";

#region FSR2_UTILITIES
    static bool CheckFSR2FeatureAvailable()
    {
        // check plugin availability
        if (!UnityEngine.AMD.AMDUnityPlugin.IsLoaded())
        {
            Debug.LogWarning("AMDUnityPlugin not loaded.");
            return false;
        }

        // check device
        UnityEngine.AMD.GraphicsDevice device = UnityEngine.AMD.GraphicsDevice.CreateGraphicsDevice();
        if (device == null)
        {
            Debug.LogWarning("AMDUnityPlugin failed to create device.");
            return false;
        }

        return true;
    }
#endregion // FSR2_UTILITIES
    

#region RENDERGRAPH_INTERFACE_DATA
    class FSR2GraphData
    {
        public FSR2UpscalerContext upscalerContext;
        public FSR2CommandInitializationData initSettings;
        public FSR2CommandExecutionData execData;
        public TextureHandle colorInput;
        public TextureHandle depth;
        public TextureHandle motionVectors;
        public TextureHandle colorOutput;
    };
#endregion

#region IUPSCALER_INTERFACE
    public FSR2IUpscaler()
    {
        m_FSR2Ready = CheckFSR2FeatureAvailable();
    }

    public override string name => upscalerName;
    public override bool isTemporal => true;
    public override bool supportsSharpening => true;
    public override bool hasQualityMode => true;

    public override IUpscalerContext CreateContext(UpscalerOptions options, Vector2Int displayResolution)
    {
        if (!m_FSR2Ready || options is not FSR2Options fsr2Options)
            return null;

        return new FSR2UpscalerContext(fsr2Options, displayResolution);
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

    static int CalculateJitterPhaseCount(float upscaleRatio)
    {
        const float basePhaseCount = 8.0f;
        // Round half up, don't truncate: upscaleRatio is reconstructed as display/round(renderSize), so a clean preset
        // (e.g. 1.5x) arrives slightly low (~1.497) and 8*1.497^2 = 17.93 would truncate to 17 instead of 18.
        return Mathf.FloorToInt(basePhaseCount * upscaleRatio * upscaleRatio + 0.5f);
    }

    public override UpscalerResolutionInfo GetResolutionInfo(Vector2Int displayResolution, UpscalerOptions options)
    {
        var fsr2Options = options as FSR2Options;
        if (!m_FSR2Ready || fsr2Options == null)
            return UpscalerResolutionInfo.Fixed(displayResolution);

        UpscalerResolutionInfo info;
        if (fsr2Options.resolutionMode == UpscalerResolutionMode.CustomScaling)
        {
            // Custom scaling: the user drives the render resolution (Render Scale / DRS); FSR2 has no opinion on it and
            // no hard per-quality min/max range (unlike DLSS/XeSS) — it supports DRS globally instead.
            info = base.GetResolutionInfo(displayResolution, options);
        }
        else // Quality mode: the preset defines a single optimal render resolution (no range).
        {
            // Fall through (don't early-return on failure) so recommendedMinScale is still reported below.
            info = GraphicsDevice.device.GetRenderResolutionFromQualityMode(fsr2Options.fsr2QualityMode,
                       (uint)displayResolution.x, (uint)displayResolution.y,
                       out uint renderWidth, out uint renderHeight)
                ? UpscalerResolutionInfo.Fixed(new Vector2Int((int)renderWidth, (int)renderHeight))
                : UpscalerResolutionInfo.Fixed(displayResolution); // SDK/device not ready
        }

        // Resolution- and mode-independent, so report it on both resolution-mode branches.
        info.recommendedMinScale = GetRecommendedMinScale();
        return info;
    }

    // FSR2's recommended minimum render scale: below it FSR2 still runs but quality degrades — advisory only, never clamped.
    // Two bounds, because the recommended max upscale is tighter under dynamic resolution (~1.5x → ~0.667) than for static
    // scaling (~3x → ~0.33). Each is cached on first success to avoid a per-frame P/Invoke; not cached on failure so a
    // device-not-ready result can be retried.
    float m_CachedRecommendedMinScale = -1f;        // static scaling; < 0 = not yet queried
    float m_CachedRecommendedMinScaleDynamic = -1f; // dynamic resolution; < 0 = not yet queried

    // Static-scaling floor: FSR2's largest upscale ratio (UltraPerformance).
    float GetRecommendedMinScale() => RecommendedMinScaleFor(FSR2Quality.UltraPerformance, ref m_CachedRecommendedMinScale);

    // Dynamic-resolution floor: FSR2's recommended ~1.5x max upscale (the Quality preset's ratio). See GPUOpen GDC 2022 presentation.
    float GetRecommendedMinScaleForDynamicResolution() => RecommendedMinScaleFor(FSR2Quality.Quality, ref m_CachedRecommendedMinScaleDynamic);

    float RecommendedMinScaleFor(FSR2Quality qualityMode, ref float cache)
    {
        if (cache >= 0f)
            return cache;
        if (!m_FSR2Ready || GraphicsDevice.device == null)
            return 0f; // not cached; retry once ready
        float upscaleRatio = GraphicsDevice.device.GetUpscaleRatioFromQualityMode(qualityMode);
        if (upscaleRatio <= 0f)
            return 0f; // not cached; allow retry
        return cache = 1f / upscaleRatio;
    }

    // Last render scale (rounded to 2 decimals) we logged a notice for; NaN means none. Tracked so a sustained or
    // jittering below-recommended scale logs once per distinct value instead of every frame.
    float m_LastNotifiedBelowScale = float.NaN;

    // FSR2 keeps working below its recommended render scale but image quality degrades, so log an informational notice
    // (never clamp the user's chosen scale). INFO severity matches the editor's HelpBox for this soft floor; hard
    // [min,max] clamps warn instead. preUpscaleResolution reflects both Render Scale and hardware DRS
    // (ScalableBufferManager), so this covers both.
    void NotifyIfBelowRecommendedRenderScale(UpscalingIO io)
    {
        bool dynamicResolution = io.dynamicResolution.HasValue;
        float recommendedMinScale = dynamicResolution ? GetRecommendedMinScaleForDynamicResolution() : GetRecommendedMinScale();
        if (recommendedMinScale <= 0f)
            return; // device gave no ratio

        if (io.postUpscaleResolution.x <= 0)
            return; // guard against a degenerate display size (early init / resize) before dividing

        float scale = (float)io.preUpscaleResolution.x / io.postUpscaleResolution.x;
        if (scale >= recommendedMinScale)
        {
            m_LastNotifiedBelowScale = float.NaN; // at/above the recommendation: clear it so dropping below notifies again
            return;
        }

        float roundedScale = Mathf.Floor(scale * 100f + 0.5f) / 100f;
        if (Mathf.Approximately(m_LastNotifiedBelowScale, roundedScale))
            return;
        m_LastNotifiedBelowScale = roundedScale;

        string mode = dynamicResolution ? "with dynamic resolution" : "at a fixed Render Scale";
        Debug.Log(
            $"FSR2 is rendering at Render Scale {scale:0.00} {mode}, below its recommended minimum of " +
            $"{recommendedMinScale:0.00} (beyond FSR2's recommended maximum upscale ratio for this mode). FSR2 still " +
            "runs, but image quality degrades at this scale. Raise Render Scale / dynamic resolution to stay at or " +
            "above the recommended minimum.");
    }

    public override float CalculateMipBias(Vector2Int preUpscaleResolution, Vector2Int postUpscaleResolution)
    {
        // AMDUnityPlugin should provide this value.
        float xBias = Mathf.Log((float)preUpscaleResolution.x / postUpscaleResolution.x, 2f);
        float yBias = Mathf.Log((float)preUpscaleResolution.y / postUpscaleResolution.y, 2f);
        return Mathf.Min(xBias, yBias) - 1.0f;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if(!m_FSR2Ready)
            return;

        Debug.Assert(GraphicsDevice.device != null);

        UpscalingIO io = frameData.Get<UpscalingIO>();

        // Get the per-camera context from UpscalingIO (set by the pipeline)
        var upscalerContext = io.context as FSR2UpscalerContext;
        if (upscalerContext == null)
        {
            Debug.LogWarning("FSR2IUpscaler: No valid context provided via io.context. Skipping upscaling.");
            return;
        }

        if (Debug.isDebugBuild)
            NotifyIfBelowRecommendedRenderScale(io);

        // describe output texture
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
            outputDesc.name = "_FSR2OutputTarget";
            outputDesc.clearBuffer = false;
            outputDesc.filterMode = FilterMode.Bilinear;
            outputColor = renderGraph.CreateTexture(outputDesc);
        }

        using (var builder = renderGraph.AddUnsafePass<FSR2GraphData>("FidelityFX Super Resolution 2", out FSR2GraphData passData, new ProfilingSampler("FSR2")))
        {
            float motionVectorSign = io.motionVectorDirection == UpscalingIO.MotionVectorDirection.PreviousFrameToCurrentFrame ? -1.0f : 1.0f;
            float motionVectorScaleX = io.motionVectorDomain == UpscalingIO.MotionVectorDomain.NDC ? io.motionVectorTextureSize.x : 1.0f;
            float motionVectorScaleY = io.motionVectorDomain == UpscalingIO.MotionVectorDomain.NDC ? io.motionVectorTextureSize.y : 1.0f;

            // Filled every frame (cheap), but EnsureNativeContext only (re)creates the native context when the max render
            // size or init flags actually change (see those fields) — both rare, so this is not a per-frame reallocation.
            passData.upscalerContext = upscalerContext;
            {
                bool displayResolutionMotionVectors = io.motionVectorTextureSize.x == io.postUpscaleResolution.x &&
                                                       io.motionVectorTextureSize.y == io.postUpscaleResolution.y;
                passData.initSettings = new FSR2CommandInitializationData();
                passData.initSettings.SetFlag(FfxFsr2InitializationFlags.EnableHighDynamicRange, io.hdrInput);
                passData.initSettings.SetFlag(FfxFsr2InitializationFlags.EnableDisplayResolutionMotionVectors, displayResolutionMotionVectors);
                passData.initSettings.SetFlag(FfxFsr2InitializationFlags.DepthInverted, io.invertedDepth);
                passData.initSettings.SetFlag(FfxFsr2InitializationFlags.EnableMotionVectorsJitterCancellation, io.jitteredMotionVectors);
                passData.initSettings.SetFlag(FfxFsr2InitializationFlags.EnableDynamicResolution, io.dynamicResolution.HasValue);
                // Allocate at the max render size; the per-frame renderSize (execData below) is the actually-rendered
                // sub-region, so dynamic resolution never reallocates. With DRS off the max equals the render size.
                passData.initSettings.maxRenderSizeWidth = (uint)io.maxPreUpscaleResolution.x;
                passData.initSettings.maxRenderSizeHeight = (uint)io.maxPreUpscaleResolution.y;
                passData.initSettings.displaySizeWidth = (uint)io.postUpscaleResolution.x;
                passData.initSettings.displaySizeHeight = (uint)io.postUpscaleResolution.y;
            }

            // Per-frame execution data read from io.options, so live changes (e.g. sharpness) apply without recreating
            // the context. Null-guarded only defensively; FSR2 always has framework-resolved options.
            var currentOptions = io.options as FSR2Options;
            passData.execData.enableSharpening = (currentOptions != null && currentOptions.enableSharpening) ? 1 : 0;
            passData.execData.sharpness = currentOptions != null ? currentOptions.sharpness : 0f;
            passData.execData.MVScaleX = motionVectorSign * motionVectorScaleX;
            passData.execData.MVScaleY = motionVectorSign * motionVectorScaleY;
            passData.execData.renderSizeWidth = (uint)io.preUpscaleResolution.x;
            passData.execData.renderSizeHeight = (uint)io.preUpscaleResolution.y;
            passData.execData.jitterOffsetX = io.subpixelJitter.x;
            passData.execData.jitterOffsetY = io.subpixelJitter.y;
            passData.execData.cameraNear = io.nearClipPlane;
            passData.execData.cameraFar = io.farClipPlane;
            passData.execData.cameraFovAngleVertical = 2.0f * (float)Math.PI * (1 / 360.0f) * io.fieldOfViewDegrees; // radians
            passData.execData.preExposure = 1.0f; // Mathf.Clamp(io.preExposureValue, 0.20f, 2.0f); // clamp to a reasonable value to prevent ghosting
            passData.execData.frameTimeDelta = io.deltaTime * 1000.0f; // in milliseconds
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
            builder.SetRenderFunc((FSR2GraphData data, UnsafeGraphContext ctx) =>
            {
                CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);

                Debug.Assert(data.initSettings.displaySizeWidth > 0, "FSR2 init settings must be populated");

                // Get the native context, (re)creating it only if the max render size or init flags changed (the context decides).
                FSR2Context nativeContext = data.upscalerContext.EnsureNativeContext(cmd, data.initSettings);
                Debug.Assert(nativeContext != null);

                nativeContext.executeData = data.execData;
                FSR2TextureTable textureTable = new()
                {
                    colorInput = data.colorInput,
                    depth = data.depth,
                    motionVectors = data.motionVectors,
                    colorOutput = data.colorOutput,
                };

                GraphicsDevice.device.ExecuteFSR2(cmd, nativeContext, textureTable);
            });
        }

        io.cameraColor = outputColor;
    }
#endregion


#region DATA
    private bool m_FSR2Ready = false;
#endregion
}

#endif // ENABLE_UPSCALER_FRAMEWORK && ENABLE_AMD && ENABLE_AMD_MODULE
