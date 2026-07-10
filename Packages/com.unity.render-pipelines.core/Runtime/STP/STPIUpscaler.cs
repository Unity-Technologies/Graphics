#if ENABLE_UPSCALER_FRAMEWORK
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

#if UNITY_EDITOR
using UnityEditor;
#endif


#if UNITY_EDITOR
[InitializeOnLoad]
#endif
static class RegisterSTP
{
    static RegisterSTP() => UpscalerRegistry.Register<STPIUpscaler, STPOptions>(STPIUpscaler.upscalerName);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitRuntime() => UpscalerRegistry.Register<STPIUpscaler, STPOptions>(STPIUpscaler.upscalerName);
}

/// <summary>
/// Per-camera context for STP upscaling. Wraps the STP.HistoryContext
/// and tracks the display resolution it was created for.
/// </summary>
public class STPUpscalerContext : IUpscalerContext
{
    private STP.HistoryContext m_HistoryContext;

    /// <inheritdoc/>
    public Vector2Int createdForDisplayResolution { get; private set; }

    /// <inheritdoc/>
    public int lastUsedFrame { get; set; }

    public STP.HistoryContext historyContext => m_HistoryContext;

    public STPUpscalerContext(Vector2Int displayResolution)
    {
        m_HistoryContext = new STP.HistoryContext();
        createdForDisplayResolution = displayResolution;
    }

    /// <inheritdoc/>
    public bool IsValidForOptions(UpscalerOptions options)
    {
        // STP options only contain injection point which doesn't affect context validity.
        // Resolution validation is handled by the framework via createdForDisplayResolution.
        return true;
    }

    /// <inheritdoc/>
    public void Cleanup(CommandBuffer cmd)
    {
        if (m_HistoryContext != null)
        {
            m_HistoryContext.Dispose();
            m_HistoryContext = null;
        }
    }
}

public class STPIUpscaler : AbstractUpscaler
{
    public static readonly string upscalerName = "Spatial-Temporal Post-Processing";

    private const string k_UpscaledColorTargetName = "_UpscaledCameraColor";

    public override string name => upscalerName;

    public override bool isTemporal => true;
    public override bool supportsSharpening => true;

    public override IUpscalerContext CreateContext(UpscalerOptions options, Vector2Int displayResolution)
    {
        return new STPUpscalerContext(displayResolution);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UpscalingIO io = frameData.Get<UpscalingIO>();

        // Get the per-camera context from UpscalingIO (set by the pipeline)
        var upscalerContext = io.context as STPUpscalerContext;
        if (upscalerContext == null)
        {
            Debug.LogWarning("STPIUpscaler: No valid context provided via io.context. Skipping upscaling.");
            return;
        }

        // Create an output texture
        TextureHandle outputColor;
        {
            TextureDesc inputDesc = io.cameraColor.GetDescriptor(renderGraph);
            TextureDesc outputDesc = inputDesc;
            outputDesc.width = io.postUpscaleResolution.x;
            outputDesc.height = io.postUpscaleResolution.y;
            // Avoid enabling sRGB because STP works with compute shaders which can't output sRGB automatically.
            outputDesc.format = GraphicsFormatUtility.GetLinearFormat(inputDesc.format);
            outputDesc.msaaSamples = MSAASamples.None;
            outputDesc.useMipMap = false;
            outputDesc.autoGenerateMips = false;
            outputDesc.useDynamicScale = false;
            outputDesc.anisoLevel = 0;
            outputDesc.discardBuffer = false;
            // STP uses compute shaders so all render textures must enable random writes
            outputDesc.enableRandomWrite = true;

            outputDesc.name = k_UpscaledColorTargetName;
            outputDesc.clearBuffer = false;
            outputDesc.filterMode = FilterMode.Bilinear;
            outputColor = renderGraph.CreateTexture(outputDesc);
        }
        
        // Populate the STP config
        STP.Config config = new();
        {
            // TODO (Apoorva): Set this using UpscalingInputs when we add HDRP support
            config.enableTexArray = io.enableTexArray;
            config.enableMotionScaling = io.enableMotionScaling;
            config.noiseTexture = io.blueNoiseTextureSet[io.frameIndex & (io.blueNoiseTextureSet.Length - 1)];
            config.inputColor = io.cameraColor;
            config.inputDepth = io.cameraDepth;
            config.inputMotion = io.motionVectorColor;
            config.currentImageSize = io.preUpscaleResolution;
            config.priorImageSize = io.previousPreUpscaleResolution;
            config.outputImageSize = io.postUpscaleResolution;
            config.deltaTime = io.deltaTime;
            config.lastDeltaTime = io.previousDeltaTime;
            config.frameIndex = io.frameIndex;
            config.nearPlane = io.nearClipPlane;
            config.farPlane = io.farClipPlane;

            // TODO (Apoorva): Support stencil masking. URP doesn't support this, HDRP does. We should add support for
            // both.
            config.inputStencil = TextureHandle.nullHandle;
            config.stencilMask = 0;

            // TODO (Apoorva): Add support for debug views.
            config.debugView = TextureHandle.nullHandle;
            config.debugViewIndex = 0;

            config.destination = outputColor;

            // Use the per-camera history context
            bool hasValidHistory;
            {
                STP.HistoryUpdateInfo info;
                info.preUpscaleSize = io.preUpscaleResolution;
                info.postUpscaleSize = io.postUpscaleResolution;
                info.useHwDrs = io.dynamicResolution == DynamicResolutionType.Hardware;
                info.useTexArray = io.enableTexArray;
                hasValidHistory = upscalerContext.historyContext.Update(ref info);
            }
            config.historyContext = upscalerContext.historyContext;
            config.enableHwDrs = io.dynamicResolution == DynamicResolutionType.Hardware;
            config.hasValidHistory = !io.resetHistory && hasValidHistory;


            config.numActiveViews = io.numActiveViews;
            config.perViewConfigs = STP.perViewConfigs;
            Debug.Assert(io.numActiveViews <= STP.perViewConfigs.Length);
            for (int viewIndex = 0; viewIndex < io.numActiveViews; ++viewIndex)
            {
                int targetIndex = viewIndex;

                STP.PerViewConfig perViewConfig;

                // STP requires non-jittered versions of the current, previous, and "previous previous" projection
                // matrix.
                perViewConfig.currentProj = io.projectionMatrices[targetIndex];
                perViewConfig.lastProj = io.previousProjectionMatrices[targetIndex];
                perViewConfig.lastLastProj = io.previousPreviousProjectionMatrices[targetIndex];

                perViewConfig.currentView = io.viewMatrices[targetIndex];
                perViewConfig.lastView = io.previousViewMatrices[targetIndex];
                perViewConfig.lastLastView = io.previousPreviousViewMatrices[targetIndex];

                // NOTE: STP assumes the view matrices also contain the world space camera position so we inject the
                // camera position directly here.
                Vector3 currentPosition = io.worldSpaceCameraPositions[viewIndex];
                Vector3 lastPosition = io.previousWorldSpaceCameraPositions[viewIndex];
                Vector3 lastLastPosition = io.previousPreviousWorldSpaceCameraPositions[viewIndex];

                perViewConfig.currentView.SetColumn(3, new Vector4(-currentPosition.x, -currentPosition.y, -currentPosition.z, 1.0f));
                perViewConfig.lastView.SetColumn(3, new Vector4(-lastPosition.x, -lastPosition.y, -lastPosition.z, 1.0f));
                perViewConfig.lastLastView.SetColumn(3, new Vector4(-lastLastPosition.x, -lastLastPosition.y, -lastLastPosition.z, 1.0f));

                STP.perViewConfigs[viewIndex] = perViewConfig;
            }
        }

        STP.Execute(renderGraph, ref config);
        io.cameraColor = outputColor;
    }
}
#endif
