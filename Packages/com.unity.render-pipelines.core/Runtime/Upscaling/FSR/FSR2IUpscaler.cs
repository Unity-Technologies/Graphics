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
            m_NativeContext = GraphicsDevice.device.CreateFSRUpscale2(cmd, settings);
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

        m_ResolutionSuggestion = new FSRUtils.ResolutionSuggestion("FSR2",
            () => GraphicsDevice.device.GetUpscaleRatioFromQualityMode(FSR2Quality.UltraPerformance),
            () => GraphicsDevice.device.GetUpscaleRatioFromQualityMode(FSR2Quality.Quality));
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
        allowScaling = false;
        jitter = m_FSR2Ready ? FSRUtils.CalculateJitter(frameIndex, upscaleRatio) : Vector2.zero;
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
        info.recommendedMinScale = m_ResolutionSuggestion.GetRecommendedMinScale();
        return info;
    }

    public override float CalculateMipBias(Vector2Int preUpscaleResolution, Vector2Int postUpscaleResolution)
    {
        if (!m_FSR2Ready)
            return base.CalculateMipBias(preUpscaleResolution, postUpscaleResolution);

        return FSRUtils.CalculateMipBias(preUpscaleResolution, postUpscaleResolution);
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
            m_ResolutionSuggestion.NotifyIfBelowRecommendedRenderScale(io);

        TextureHandle outputColor = FSRUtils.CreateOutputTexture(renderGraph, io, "_FSR2OutputTarget");

        using (var builder = renderGraph.AddUnsafePass<FSR2GraphData>("FidelityFX Super Resolution 2", out FSR2GraphData passData, new ProfilingSampler("FSR2")))
        {
            var currentOptions = io.options as FSR2Options;

            bool enableSharpening = (currentOptions != null) ? currentOptions.enableSharpening : false;
            float sharpness = (currentOptions != null) ? currentOptions.sharpness : 0.0f;

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

            FSRUtils.PopulateExecutionData(ref passData.execData, io, io.subpixelJitter, enableSharpening, sharpness);

            builder.UseTexture(io.cameraColor);
            builder.UseTexture(io.cameraDepth);
            builder.UseTexture(io.motionVectorColor);
            builder.UseTexture(outputColor, AccessFlags.Write);

            passData.colorInput = io.cameraColor;
            passData.depth = io.cameraDepth;
            passData.motionVectors = io.motionVectorColor;
            passData.colorOutput = outputColor;

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

                GraphicsDevice.device.ExecuteFSRUpscale2(cmd, nativeContext, textureTable);
            });
        }

        io.cameraColor = outputColor;
    }
#endregion


#region DATA
    private bool m_FSR2Ready = false;
    private FSRUtils.ResolutionSuggestion m_ResolutionSuggestion = null;
#endregion
}

#endif // ENABLE_UPSCALER_FRAMEWORK && ENABLE_AMD && ENABLE_AMD_MODULE
