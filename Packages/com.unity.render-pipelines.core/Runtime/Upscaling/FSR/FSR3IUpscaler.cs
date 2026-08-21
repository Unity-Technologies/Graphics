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
static class RegisterFSR3
{
    static RegisterFSR3() => UpscalerRegistry.Register<FSR3IUpscaler, FSR3Options>(FSR3IUpscaler.upscalerName);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitRuntime() => UpscalerRegistry.Register<FSR3IUpscaler, FSR3Options>(FSR3IUpscaler.upscalerName);
}

/// <summary>
/// Per-camera context for FSR3 upscaling. Wraps the native AMD FSRUpscalerContext
/// and tracks the settings it was created with for validation.
/// Native context creation is deferred until first use (requires CommandBuffer).
/// </summary>
public class FSR3UpscalerContext : PluginUpscalerContext<FSRUpscalerContext, FSR3Options>
{
    public FSR3Quality createdWithQuality { get; }

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
    public FfxApiCreateContextUpscaleFlags createdWithFlags { get; private set; }

    /// <summary>
    /// Creates a new FSR3 context wrapper. Native context creation is deferred.
    /// Stores context-creation settings (quality). Per-frame settings (sharpness)
    /// should be read from io.options in RecordRenderGraph.
    /// </summary>
    public FSR3UpscalerContext(FSR3Options options, Vector2Int displayResolution)
        : base(displayResolution)
    {
        createdWithQuality = options.fsr3QualityMode;
    }

    /// <summary>
    /// Returns the native FSR3 context, creating it on first use and recreating it when the max render size or
    /// initialization flags in <paramref name="settings"/> differ from those the current context was created with.
    /// </summary>
    /// <param name="cmd">Command buffer to record any creation/destruction commands.</param>
    /// <param name="settings">Initialization settings for the native context (carries the requested max render size and flags).</param>
    /// <returns>The native FSR3 context, valid for the requested max render size and flags.</returns>
    public FSRUpscalerContext EnsureNativeContext(CommandBuffer cmd, FSRUpscalerCommandInitializationData settings)
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
            m_NativeContext = GraphicsDevice.device.CreateFSRUpscale3(cmd, settings);
            createdWithMaxRenderSize = requestedMaxRenderSize;
            createdWithFlags = settings.ffxFsrFlags;
        }
        return m_NativeContext;
    }

    /// <inheritdoc/>
    protected override void DestroyNativeContext(CommandBuffer cmd, FSRUpscalerContext context)
        => GraphicsDevice.device.DestroyFeature(cmd, context);

    /// <inheritdoc/>
    protected override bool ValidateOptions(FSR3Options options)
    {
        // Quality mode changes require context recreation
        return options.fsr3QualityMode == createdWithQuality;
    }
}

/// <summary>
/// FidelityFX Super Resolution 3 upscaler implementation.
/// </summary>
public class FSR3IUpscaler : AbstractUpscaler
{
    public static readonly string upscalerName = "FidelityFX Super Resolution 3";

#region FSR3_UTILITIES
    private static bool CheckFeatureAvailable()
    {
        if (!FSRUtils.CheckPluginAvailable())
            return false;

        if (!GraphicsDevice.device.IsFeatureAvailable(GraphicsDeviceFeature.FSRUpscale3))
        {
            Debug.LogWarning("FSR3 is not available on this device.");
            return false;
        }

        return true;
    }
#endregion

#region RENDERGRAPH_INTERFACE_DATA
    private class FSR3PassData
    {
        public FSR3UpscalerContext upscalerContext;
        public FSRUpscalerCommandInitializationData initSettings;
        public FSR2CommandExecutionData execData; // FSR2/3/4 share the same struct.
        public TextureHandle colorInput;
        public TextureHandle depth;
        public TextureHandle motionVectors;
        public TextureHandle colorOutput;
    }
#endregion

#region IUPSCALER_INTERFACE
    public FSR3IUpscaler()
    {
        m_Ready = CheckFeatureAvailable();

        m_ResolutionSuggestion = new FSRUtils.ResolutionSuggestion("FSR3",
            () => GraphicsDevice.device.GetUpscaleRatioFromQualityMode(FSR3Quality.UltraPerformance),
            () => GraphicsDevice.device.GetUpscaleRatioFromQualityMode(FSR3Quality.Quality));
    }

    public override string name => upscalerName;
    public override bool isTemporal => true;
    public override bool supportsSharpening => true;
    public override bool hasQualityMode => true;

    public override IUpscalerContext CreateContext(UpscalerOptions options, Vector2Int displayResolution)
    {
        if (!m_Ready || options is not FSR3Options fsr3Options)
            return null;

        return new FSR3UpscalerContext(fsr3Options, displayResolution);
    }

    public override void CalculateJitter(int frameIndex, float upscaleRatio, out Vector2 jitter, out bool allowScaling)
    {
        allowScaling = false;
        jitter = m_Ready ? FSRUtils.CalculateJitter(frameIndex, upscaleRatio) : Vector2.zero;
    }

    public override UpscalerResolutionInfo GetResolutionInfo(Vector2Int displayResolution, UpscalerOptions options)
    {
        var fsr3Options = options as FSR3Options;
        if (!m_Ready || fsr3Options == null)
            return UpscalerResolutionInfo.Fixed(displayResolution);

        UpscalerResolutionInfo info;
        if (fsr3Options.resolutionMode == UpscalerResolutionMode.CustomScaling)
        {
            // Custom scaling: the user drives the render resolution (Render Scale / DRS); FSR3 has no opinion on it and
            // no hard per-quality min/max range (unlike DLSS/XeSS) — it supports DRS globally instead.
            info = base.GetResolutionInfo(displayResolution, options);
        }
        else // Quality mode: the preset defines a single optimal render resolution (no range).
        {
            GraphicsDevice.device.GetRenderResolutionFromQualityModeFSR3(
                fsr3Options.fsr3QualityMode,
                (uint)displayResolution.x, (uint)displayResolution.y,
                out uint renderResolutionX, out uint renderResolutionY);
            info = UpscalerResolutionInfo.Fixed(new Vector2Int((int)renderResolutionX, (int)renderResolutionY));
        }

        // Resolution- and mode-independent, so report it on both resolution-mode branches.
        info.recommendedMinScale = m_ResolutionSuggestion.GetRecommendedMinScale();

        return info;
    }

    public override float CalculateMipBias(Vector2Int preUpscaleResolution, Vector2Int postUpscaleResolution)
    {
        if (!m_Ready)
            return base.CalculateMipBias(preUpscaleResolution, postUpscaleResolution);

        return FSRUtils.CalculateMipBias(preUpscaleResolution, postUpscaleResolution);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (!m_Ready)
            return;

        Debug.Assert(GraphicsDevice.device != null);

        UpscalingIO io = frameData.Get<UpscalingIO>();

        // Get the per-camera context from UpscalingIO (set by the pipeline)
        var upscalerContext = io.context as FSR3UpscalerContext;
        if (upscalerContext == null)
        {
            Debug.LogWarning("FSR3IUpscaler: No valid context provided via io.context. Skipping upscaling.");
            return;
        }

        if (Debug.isDebugBuild)
            m_ResolutionSuggestion.NotifyIfBelowRecommendedRenderScale(io);

        TextureHandle outputColor = FSRUtils.CreateOutputTexture(renderGraph, io, "_FSR3OutputTarget");

        using (var builder = renderGraph.AddUnsafePass<FSR3PassData>("FidelityFX Super Resolution 3", out var passData, new ProfilingSampler("FSR3")))
        {
            var currentOptions = io.options as FSR3Options;

            bool enableDebugChecking = (currentOptions != null) ? currentOptions.enableDebugChecking : false;
            bool enableSharpening = (currentOptions != null) ? currentOptions.enableSharpening : false;
            float sharpness = (currentOptions != null) ? currentOptions.sharpness : 0.0f;

            // Filled every frame (cheap), but EnsureNativeContext only (re)creates the native context when the max render
            // size or init flags actually change (see those fields) — both rare, so this is not a per-frame reallocation.
            passData.upscalerContext = upscalerContext;
            {
                bool displayResMotionVectors = io.motionVectorTextureSize.x == io.postUpscaleResolution.x &&
                                               io.motionVectorTextureSize.y == io.postUpscaleResolution.y;
                passData.initSettings = new FSRUpscalerCommandInitializationData();
                passData.initSettings.SetFlag(FfxApiCreateContextUpscaleFlags.EnableHighDynamicRange, io.hdrInput);
                passData.initSettings.SetFlag(FfxApiCreateContextUpscaleFlags.EnableDisplayResolutionMotionVectors, displayResMotionVectors);
                passData.initSettings.SetFlag(FfxApiCreateContextUpscaleFlags.EnableMotionVectorsJitterCancellation, io.jitteredMotionVectors);
                passData.initSettings.SetFlag(FfxApiCreateContextUpscaleFlags.DepthInverted, io.invertedDepth);
                passData.initSettings.SetFlag(FfxApiCreateContextUpscaleFlags.EnableDynamicResolution, io.dynamicResolution.HasValue);
                passData.initSettings.SetFlag(FfxApiCreateContextUpscaleFlags.EnableDebugChecking, enableDebugChecking);
                passData.initSettings.SetFlag(FfxApiCreateContextUpscaleFlags.EnableDebugVisualization, false);
                // Allocate at the max render size; the per-frame renderSize (execData below) is the actually-rendered
                // sub-region, so dynamic resolution never reallocates. With DRS off the max equals the render size.
                passData.initSettings.maxRenderSizeWidth = (uint)io.maxPreUpscaleResolution.x;
                passData.initSettings.maxRenderSizeHeight = (uint)io.maxPreUpscaleResolution.y;
                passData.initSettings.displaySizeWidth = (uint)io.postUpscaleResolution.x;
                passData.initSettings.displaySizeHeight = (uint)io.postUpscaleResolution.y;
            }

            FSRUtils.PopulateExecutionData(ref passData.execData, io, io.subpixelJitter, enableSharpening, sharpness);

            builder.UseTexture(io.cameraColor);
            builder.UseTexture(io.cameraDepth);
            builder.UseTexture(io.motionVectorColor);
            builder.UseTexture(outputColor, AccessFlags.Write);

            passData.colorInput = io.cameraColor;
            passData.depth = io.cameraDepth;
            passData.motionVectors = io.motionVectorColor;
            passData.colorOutput = outputColor;

            builder.SetRenderFunc((FSR3PassData data, UnsafeGraphContext ctx) =>
            {
                CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);

                Debug.Assert(data.initSettings.displaySizeWidth > 0, "FSR3 init settings must be populated");

                // Get the native context, (re)creating it only if the max render size or init flags changed (the context decides).
                FSRUpscalerContext nativeContext = data.upscalerContext.EnsureNativeContext(cmd, data.initSettings);
                Debug.Assert(nativeContext != null);

                nativeContext.executeData = data.execData;

                // FSR2/3/4 use the same texture table.
                FSR2TextureTable textureTable = new()
                {
                    colorInput = data.colorInput,
                    depth = data.depth,
                    motionVectors = data.motionVectors,
                    colorOutput = data.colorOutput,
                };

                GraphicsDevice.device.ExecuteFSRUpscale3(cmd, nativeContext, textureTable);
            });
        }

        io.cameraColor = outputColor;
    }
#endregion

#region DATA
    private bool m_Ready = false;
    private FSRUtils.ResolutionSuggestion m_ResolutionSuggestion = null;
#endregion
}

#endif // ENABLE_UPSCALER_FRAMEWORK && ENABLE_AMD && ENABLE_AMD_MODULE
