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
    private readonly FSR2Quality m_CreatedWithQuality;
    private readonly bool m_CreatedWithFixedResolutionMode;

    /// <summary>
    /// Creates a new FSR2 context wrapper. Native context creation is deferred.
    /// </summary>
    public FSR2UpscalerContext(FSR2Options options, Vector2Int displayResolution)
        : base(displayResolution)
    {
        m_CreatedWithQuality = options.fsr2QualityMode;
        m_CreatedWithFixedResolutionMode = options.fixedResolutionMode;
    }

    /// <summary>
    /// Gets the native FSR2 context, creating it if necessary.
    /// </summary>
    /// <param name="cmd">Command buffer to record creation commands.</param>
    /// <param name="settings">Initialization settings for the native context.</param>
    /// <returns>The native FSR2 context.</returns>
    public FSR2Context GetOrCreateNativeContext(CommandBuffer cmd, FSR2CommandInitializationData settings)
    {
        m_NativeContext ??= GraphicsDevice.device.CreateFeature(cmd, settings);
        return m_NativeContext;
    }

    /// <inheritdoc/>
    protected override void DestroyNativeContext(CommandBuffer cmd, FSR2Context context)
        => GraphicsDevice.device.DestroyFeature(cmd, context);

    /// <inheritdoc/>
    protected override bool ValidateOptions(FSR2Options options)
    {
        // Quality mode and fixed resolution mode changes require context recreation
        // Sharpness changes do NOT require recreation (just parameters)
        return options.fsr2QualityMode == m_CreatedWithQuality &&
               options.fixedResolutionMode == m_CreatedWithFixedResolutionMode;
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
        public bool needsInitSettings; // True when native context doesn't exist yet
        public FSR2CommandInitializationData initSettings;
        public FSR2CommandExecutionData execData;
        public TextureHandle colorInput;
        public TextureHandle depth;
        public TextureHandle motionVectors;
        public TextureHandle colorOutput;
    };
#endregion

#region IUPSCALER_INTERFACE
    public FSR2IUpscaler(FSR2Options o)
    {
        if(!CheckFSR2FeatureAvailable())
        {
            m_FSR2Ready = false;
            return;
        }

        m_Options = o;
        m_FSR2Ready = true;
    }

    public override string name => upscalerName;
    public override bool isTemporal => true;
    public override bool supportsSharpening => true;
    public override UpscalerOptions options => m_Options;

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
        return (int)(basePhaseCount * upscaleRatio * upscaleRatio);
    }

    public override void NegotiatePreUpscaleResolution(ref Vector2Int preUpscaleResolution, Vector2Int postUpscaleResolution)
    {
        if (m_Options.fixedResolutionMode)
        {
            Debug.Assert(GraphicsDevice.device != null);

            FSR2Quality qualityMode = (FSR2Quality)m_Options.fsr2QualityMode;
            GraphicsDevice.device.GetRenderResolutionFromQualityMode(qualityMode,
                (uint)postUpscaleResolution.x,
                (uint)postUpscaleResolution.y,
                out uint renderResoolutionX,
                out uint renderResoolutionY
            );
            preUpscaleResolution.x = (int)renderResoolutionX;
            preUpscaleResolution.y = (int)renderResoolutionY;
        }
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

            // Setup pass data
            passData.upscalerContext = upscalerContext;
            passData.needsInitSettings = !upscalerContext.hasNativeContext;

            // Only build initialization settings when native context needs to be created
            if (passData.needsInitSettings)
            {
                bool displayResolutionMotionVectors = io.motionVectorTextureSize.x == io.postUpscaleResolution.x &&
                                                       io.motionVectorTextureSize.y == io.postUpscaleResolution.y;
                passData.initSettings = new FSR2CommandInitializationData();
                passData.initSettings.SetFlag(FfxFsr2InitializationFlags.EnableHighDynamicRange, io.hdrInput);
                passData.initSettings.SetFlag(FfxFsr2InitializationFlags.EnableDisplayResolutionMotionVectors, displayResolutionMotionVectors);
                passData.initSettings.SetFlag(FfxFsr2InitializationFlags.DepthInverted, io.invertedDepth);
                passData.initSettings.SetFlag(FfxFsr2InitializationFlags.EnableMotionVectorsJitterCancellation, io.jitteredMotionVectors);
                passData.initSettings.maxRenderSizeWidth = (uint)io.preUpscaleResolution.x;
                passData.initSettings.maxRenderSizeHeight = (uint)io.preUpscaleResolution.y;
                passData.initSettings.displaySizeWidth = (uint)io.postUpscaleResolution.x;
                passData.initSettings.displaySizeHeight = (uint)io.postUpscaleResolution.y;
            }

            // Per-frame execution data
            passData.execData.enableSharpening = m_Options.enableSharpening ? 1 : 0;
            passData.execData.sharpness = m_Options.sharpness;
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

                // Verify init settings were populated if native context needs to be created
                Debug.Assert(!data.needsInitSettings || data.initSettings.displaySizeWidth > 0,
                    "FSR2 init settings must be populated when native context doesn't exist");

                // Get the native context, creating it if necessary (uses pre-built init settings)
                FSR2Context nativeContext = data.upscalerContext.GetOrCreateNativeContext(cmd, data.initSettings);
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
    private FSR2Options m_Options = null;
#endregion
}

#endif // ENABLE_UPSCALER_FRAMEWORK && ENABLE_AMD && ENABLE_AMD_MODULE
