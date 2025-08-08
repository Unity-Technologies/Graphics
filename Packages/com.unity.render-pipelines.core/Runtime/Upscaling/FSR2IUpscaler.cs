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
    static RegisterFSR2() => UpscalerRegistry.Register<FSR2IUpscaler, FSR2Options>(FSR2IUpscaler.UPSCALER_NAME);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitRuntime() => UpscalerRegistry.Register<FSR2IUpscaler, FSR2Options>(FSR2IUpscaler.UPSCALER_NAME);
}

public class FSR2IUpscaler : AbstractUpscaler
{
    public static readonly string UPSCALER_NAME = "FSR2 (IUpscaler)";

#region FSR2_UTILITIES
    static bool CheckFSR2FeatureAvailable()
    {
        // check plugin availability
        if (!UnityEngine.AMD.AMDUnityPlugin.IsLoaded())
        {
            Debug.LogWarning("AMDUnityPlugin not loaded.");
            return false;
        }
    
        // check plugin version
        // if (s_ExpectedDeviceVersion != UnityEngine.AMD.GraphicsDevice.version)
        // {
        //     Debug.LogWarning("Cannot instantiate AMD device because the version HDRP expects does not match the backend version.");
        //     return false;
        // }

        // check device
        UnityEngine.AMD.GraphicsDevice device = UnityEngine.AMD.GraphicsDevice.CreateGraphicsDevice();
        if (device == null)
        {
            Debug.LogWarning("AMDUnityPlugin failed to create device.");
            return false;
        }

        return true;
    }
    static void DestroyContext(ref FSR2Context ctx, CommandBuffer cmd)
    {
        GraphicsDevice.device.DestroyFeature(cmd, ctx);
        ctx = null;
    }
    static void CreateContext(ref FSR2Context ctx, CommandBuffer cmd, ref FSR2GraphData data)
    {
        bool displayResolutionMotionVectors = data.motionVectorSizeX == data.colorOutputSizeX && data.motionVectorSizeY == data.colorOutputSizeY;

        FSR2CommandInitializationData settings = new();
        settings.SetFlag(FfxFsr2InitializationFlags.EnableHighDynamicRange, data.inputIsHDR);
        settings.SetFlag(FfxFsr2InitializationFlags.EnableDisplayResolutionMotionVectors, displayResolutionMotionVectors);
        settings.SetFlag(FfxFsr2InitializationFlags.DepthInverted, data.invertedDepthBuffer);
        settings.SetFlag(FfxFsr2InitializationFlags.EnableMotionVectorsJitterCancellation, data.motionVectorsAreJittered);
        settings.maxRenderSizeWidth = data.colorInputSizeX;
        settings.maxRenderSizeHeight = data.colorInputSizeY;
        settings.displaySizeWidth = data.colorOutputSizeX;
        settings.displaySizeHeight = data.colorOutputSizeY;
        ctx = GraphicsDevice.device.CreateFeature(cmd, settings);
    }
#endregion // FSR2_UTILITIES
    

#region RENDERGRAPH_INTERFACE_DATA
    class FSR2GraphData
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

        m_OutputResolutionPrevious = new Vector2Int(0, 0);
        m_InputResolution = new Vector2Int(1, 1);
        m_OutputResolution = new Vector2Int(1, 1);
        m_Jitter = new Vector2(0, 0);

        m_FSR2Ready = true;
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

        jitter = new Vector2(x, y);
        allowScaling = false;

        m_Jitter = jitter;
    }

    static int CalculateJitterPhaseCount(float upscaleRatio)
    {
        const float basePhaseCount = 8.0f;
        return (int)(basePhaseCount * upscaleRatio * upscaleRatio);
    }

    private bool ShouldResetFSR2Context(UpscalingIO io)
    {
        bool nullContext = m_FSR2Context == null;
        bool outputResolutionChanged = m_OutputResolutionPrevious != io.postUpscaleResolution;

        return nullContext || outputResolutionChanged;
    }

    public override void NegotiatePreUpscaleResolution(ref Vector2Int preUpscaleResolution, Vector2Int postUpscaleResolution)
    {
        if (m_Options.FixedResolutionMode)
        {
            Debug.Assert(GraphicsDevice.device != null);

            FSR2Quality qualityMode = (FSR2Quality)m_Options.FSR2QualityMode;
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

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if(!m_FSR2Ready)
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

            // setup pass data (UpscalingIO --> FSR2GraphData)
            passData.shouldReinitializeContext = ShouldResetFSR2Context(io);

            passData.execData.enableSharpening = m_Options.EnableSharpening ? 1 : 0;
            passData.execData.sharpness = m_Options.Sharpness;
            passData.execData.MVScaleX = motionVectorSign * motionVectorScaleX;
            passData.execData.MVScaleY = motionVectorSign * motionVectorScaleY;
            passData.execData.renderSizeWidth = (uint)io.preUpscaleResolution.x;
            passData.execData.renderSizeHeight = (uint)io.preUpscaleResolution.y;
            passData.execData.jitterOffsetX = m_Jitter.x;
            passData.execData.jitterOffsetY = m_Jitter.y;
            passData.execData.cameraNear = io.nearClipPlane;
            passData.execData.cameraFar = io.farClipPlane;
            passData.execData.cameraFovAngleVertical = 2.0f * (float)Math.PI * (1 / 360.0f) * io.fieldOfViewDegrees; // radians
            passData.execData.preExposure = 1.0f; // Mathf.Clamp(io.preExposureValue, 0.20f, 2.0f); // clamp to a reasonable value to prevent ghosting
            passData.execData.frameTimeDelta = io.deltaTime * 1000.0f; // in milliseconds
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
            builder.SetRenderFunc((FSR2GraphData data, UnsafeGraphContext ctx) =>
            {
                CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                if (data.shouldReinitializeContext)
                {
                    if (m_FSR2Context != null)
                    {
                        DestroyContext(ref m_FSR2Context, cmd);
                    }
                    CreateContext(ref m_FSR2Context, cmd, ref data);
                }
                
                Debug.Assert(m_FSR2Context != null);

                m_FSR2Context.executeData = data.execData;
                FSR2TextureTable textureTable = new()
                {
                    colorInput = data.colorInput,
                    depth = data.depth,
                    motionVectors = data.motionVectors,
                    colorOutput = data.colorOutput,
                };

                GraphicsDevice.device.ExecuteFSR2(cmd, m_FSR2Context, textureTable);
            });
        }

        io.cameraColor = outputColor;

        m_OutputResolutionPrevious = io.postUpscaleResolution;
    }
#endregion


#region DATA
    // static data
    private bool m_FSR2Ready = false;

    // per-view FSR2 data (per camera / per use)
    private FSR2Context m_FSR2Context = null;
    private FSR2Options m_Options = null;

    private Vector2Int m_OutputResolutionPrevious;
    private Vector2Int m_InputResolution;
    private Vector2Int m_OutputResolution;
    private Vector2 m_Jitter;
    #endregion
}

#endif // ENABLE_UPSCALER_FRAMEWORK && ENABLE_AMD && ENABLE_AMD_MODULE
