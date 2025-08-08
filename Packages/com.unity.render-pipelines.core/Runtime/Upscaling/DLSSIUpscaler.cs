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
    static RegisterDLSS() => UpscalerRegistry.Register<DLSSIUpscaler, DLSSOptions>(DLSSIUpscaler.UPSCALER_NAME);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitRuntime() => UpscalerRegistry.Register<DLSSIUpscaler, DLSSOptions>(DLSSIUpscaler.UPSCALER_NAME);
}

public class DLSSIUpscaler : AbstractUpscaler
{
    public static readonly string UPSCALER_NAME = "DLSS (IUpscaler)";

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
    static void DestroyContext(ref DLSSContext ctx, CommandBuffer cmd)
    {
        GraphicsDevice.device.DestroyFeature(cmd, ctx);
        ctx = null;
    }
    static void CreateContext(ref DLSSContext ctx, CommandBuffer cmd, ref DLSSGraphData data, ref DLSSOptions options)
    {
        /*  Motion vectors are typically calculated at the 
            same resolution as the input color frame (i.e. at the render resolution). If the rendering engine 
            supports calculating motion vectors at the display/output resolution and dilating the motion 
            vectors, DLSS can accept those by setting the flag to “0”. This is preferred, though uncommon, 
            and can result in higher quality antialiasing of moving objects and less blurring of small objects 
            and thin details.
        */
        bool MVLowResolution = data.motionVectorSizeX <= data.colorInputSizeX || data.motionVectorSizeY <= data.colorInputSizeY;

        DLSSCommandInitializationData settings = new();
        settings.SetFlag(DLSSFeatureFlags.IsHDR, data.inputIsHDR);
        settings.SetFlag(DLSSFeatureFlags.MVLowRes, MVLowResolution);
        settings.SetFlag(DLSSFeatureFlags.DepthInverted, data.invertedDepthBuffer);
        settings.SetFlag(DLSSFeatureFlags.MVJittered, data.motionVectorsAreJittered);
        settings.inputRTWidth   = data.colorInputSizeX;
        settings.inputRTHeight  = data.colorInputSizeY;
        settings.outputRTWidth  = data.colorOutputSizeX;
        settings.outputRTHeight = data.colorOutputSizeY;
        settings.quality = (DLSSQuality)options.DLSSQualityMode;
        settings.presetQualityMode = (DLSSPreset)options.DLSSRenderPresetQuality;
        settings.presetBalancedMode = (DLSSPreset)options.DLSSRenderPresetBalanced;
        settings.presetPerformanceMode = (DLSSPreset)options.DLSSRenderPresetPerformance;
        settings.presetUltraPerformanceMode = (DLSSPreset)options.DLSSRenderPresetUltraPerformance;
        settings.presetDlaaMode = (DLSSPreset)options.DLSSRenderPresetDLAA;
        ctx = GraphicsDevice.device.CreateFeature(cmd, settings);
    }
#endregion // DLSS_UTILITIES
    

#region RENDERGRAPH_INTERFACE_DATA
    class DLSSGraphData
    {
        public bool shouldReinitializeContext;

        public uint colorInputSizeX;
        public uint colorInputSizeY;
        public uint colorOutputSizeX;
        public uint colorOutputSizeY;
        public uint motionVectorSizeX;
        public uint motionVectorSizeY;
        public bool invertedDepthBuffer;
        public bool inputIsHDR;
        public bool motionVectorsAreJittered;

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
        if(!CheckDLSSFeatureAvailable())
        {
            m_DLSSReady = false;
            return;
        }

        m_Options = o;
        if(m_Options == null)
        {
            Debug.LogWarning("null options given to DLSSIUpscaler()");
            m_Options = (DLSSOptions)ScriptableObject.CreateInstance(typeof(DLSSOptions));
            m_Options.UpscalerName = GetName();
        }
        if (string.IsNullOrEmpty(m_Options.UpscalerName))
        {
            Debug.LogWarning("options given with empty ID");
            m_Options.UpscalerName = GetName();
        }
        m_QualityModeHistory = (DLSSQuality)m_Options.DLSSQualityMode;

        m_OutputResolutionPrevious = new Vector2Int(0, 0);
        m_InputResolution = new Vector2Int(1, 1);
        m_OutputResolution = new Vector2Int(1, 1);
        m_Jitter = new Vector2(0, 0);

        m_DLSSReady = true;
    }

    public override string GetName() 
    {
        return UPSCALER_NAME;
    }
    public override bool IsTemporalUpscaler() { return true; }
    public override void CalculateJitter(int frameIndex, out Vector2 jitter, out bool allowScaling)
    {
        float upscaleRatio = (float)(m_OutputResolution.x) / m_InputResolution.x;
        int numPhases = CalculateJitterPhaseCount(upscaleRatio);

        int haltonIndex = (frameIndex % numPhases) + 1;
        float x = HaltonSequence.Get(haltonIndex, 2) - 0.5f;
        float y = HaltonSequence.Get(haltonIndex, 3) - 0.5f;

        //Debug.LogFormat("haltonIndex: {0}\nframeIndex: {1}\njitter=({2}, {3})\nphases: {4}\nupscaleRatio: {5}", haltonIndex, frameIndex, x, y, numPhases, upscaleRatio);

        jitter = new Vector2(x, y);
        allowScaling = false;

        m_Jitter = jitter;
    }

    public override void NegotiatePreUpscaleResolution(ref Vector2Int preUpscaleResolution, Vector2Int postUpscaleResolution)
    {
        if(m_Options.FixedResolutionMode)
        {
            Debug.Assert(GraphicsDevice.device != null);

            DLSSQuality qualityMode = (DLSSQuality)m_Options.DLSSQualityMode;
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

    static int CalculateJitterPhaseCount(float upscaleRatio)
    {
        const float basePhaseCount = 8.0f;
        return (int)(basePhaseCount * upscaleRatio * upscaleRatio);
    }

    private bool ShouldResetDLSSContext(UpscalingIO io)
    {
        bool qualityChanged = m_QualityModeHistory != (DLSSQuality)m_Options.DLSSQualityMode;

        bool presetChanged = m_PresetQualityHistory != (DLSSPreset)m_Options.DLSSRenderPresetQuality
            || m_PresetBalancedHistory != (DLSSPreset)m_Options.DLSSRenderPresetBalanced
            || m_PresetPerfHistory != (DLSSPreset)m_Options.DLSSRenderPresetPerformance
            || m_PresetUltraPerfHistory != (DLSSPreset)m_Options.DLSSRenderPresetUltraPerformance
            || m_PresetDLAAHistory != (DLSSPreset)m_Options.DLSSRenderPresetDLAA;

        bool outputResolutionChanged = m_OutputResolutionPrevious != io.postUpscaleResolution;

        bool nullContext = m_DLSSContext == null;

        return nullContext || qualityChanged || presetChanged || outputResolutionChanged;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if(!m_DLSSReady)
            return;

        Debug.Assert(GraphicsDevice.device != null);

        UpscalingIO io = frameData.Get<UpscalingIO>();

        m_InputResolution = io.preUpscaleResolution;
        m_OutputResolution = io.postUpscaleResolution;

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

            // setup pass data (UpscalingIO --> DLSSGraphData)
            passData.shouldReinitializeContext = ShouldResetDLSSContext(io);

            passData.execData.mvScaleX = motionVectorSign * motionVectorScaleX;
            passData.execData.mvScaleY = motionVectorSign * motionVectorScaleY;
            passData.execData.subrectOffsetX = 0;
            passData.execData.subrectOffsetY = 0;
            passData.execData.subrectWidth  = (uint)io.preUpscaleResolution.x;
            passData.execData.subrectHeight = (uint)io.preUpscaleResolution.y;
            passData.execData.jitterOffsetX = m_Jitter.x;
            passData.execData.jitterOffsetY = m_Jitter.y;
            passData.execData.preExposure = Mathf.Clamp(io.preExposureValue, 0.20f, 2.0f); // clamp to a reasonable value to prevent ghosting
            passData.execData.invertYAxis = io.flippedY ? 1u : 0u;
            passData.execData.invertXAxis = io.flippedX ? 1u : 0u;
            passData.execData.reset = io.resetHistory ? 1 : 0;

            builder.UseTexture(io.cameraColor);
            builder.UseTexture(io.cameraDepth);
            builder.UseTexture(io.motionVectorColor);
            builder.UseTexture(outputColor, AccessFlags.Write);
            passData.colorInput = io.cameraColor;
            passData.depth = io.cameraDepth;
            passData.motionVectors = io.motionVectorColor;
            passData.colorOutput = outputColor;

            passData.colorInputSizeX = (uint)io.preUpscaleResolution.x;
            passData.colorInputSizeY = (uint)io.preUpscaleResolution.y;
            passData.colorOutputSizeX = (uint)io.postUpscaleResolution.x;
            passData.colorOutputSizeY = (uint)io.postUpscaleResolution.y;
            passData.motionVectorSizeX = (uint)io.motionVectorTextureSize.x;
            passData.motionVectorSizeY = (uint)io.motionVectorTextureSize.y;
            passData.invertedDepthBuffer = io.invertedDepth;
            passData.inputIsHDR = io.hdrInput;
            passData.motionVectorsAreJittered = io.jitteredMotionVectors;

            // set render function
            builder.SetRenderFunc((DLSSGraphData data, UnsafeGraphContext ctx) =>
            {
                CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                if (data.shouldReinitializeContext)
                {
                    if (m_DLSSContext != null)
                    {
                        DestroyContext(ref m_DLSSContext, cmd);
                    }
                    CreateContext(ref m_DLSSContext, cmd, ref data, ref m_Options);
                }
                
                Debug.Assert(m_DLSSContext != null);

                m_DLSSContext.executeData = data.execData;
                DLSSTextureTable textureTable = new()
                {
                    colorInput = data.colorInput,
                    depth = data.depth,
                    motionVectors = data.motionVectors,
                    colorOutput = data.colorOutput,
                };

                GraphicsDevice.device.ExecuteDLSS(cmd, m_DLSSContext, textureTable);
            });
        }

        io.cameraColor = outputColor;

        // record history
        m_OutputResolutionPrevious = io.postUpscaleResolution;
        m_QualityModeHistory = (DLSSQuality)m_Options.DLSSQualityMode;
        m_PresetQualityHistory = (DLSSPreset)m_Options.DLSSRenderPresetQuality;
        m_PresetBalancedHistory = (DLSSPreset)m_Options.DLSSRenderPresetBalanced;
        m_PresetPerfHistory = (DLSSPreset)m_Options.DLSSRenderPresetPerformance;
        m_PresetUltraPerfHistory = (DLSSPreset)m_Options.DLSSRenderPresetUltraPerformance;
        m_PresetDLAAHistory = (DLSSPreset)m_Options.DLSSRenderPresetDLAA;
    }
#endregion


#region DATA
    // static data
    private bool m_DLSSReady = false;

    DLSSOptions m_Options = null;
    DLSSQuality m_QualityModeHistory = DLSSQuality.DLAA;
    DLSSPreset m_PresetQualityHistory = DLSSPreset.Preset_Default;
    DLSSPreset m_PresetBalancedHistory = DLSSPreset.Preset_Default;
    DLSSPreset m_PresetPerfHistory = DLSSPreset.Preset_Default;
    DLSSPreset m_PresetUltraPerfHistory = DLSSPreset.Preset_Default;
    DLSSPreset m_PresetDLAAHistory = DLSSPreset.Preset_Default;

    // per-view DLSS data (per camera / per use)
    private DLSSContext m_DLSSContext = null;

    private Vector2Int m_OutputResolutionPrevious;
    private Vector2Int m_InputResolution;
    private Vector2Int m_OutputResolution;
    private Vector2 m_Jitter;
    #endregion
}

#endif // ENABLE_UPSCALER_FRAMEWORK && ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
