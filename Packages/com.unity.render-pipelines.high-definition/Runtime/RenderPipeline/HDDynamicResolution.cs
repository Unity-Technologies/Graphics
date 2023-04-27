using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Diagnostics;

/// <summary>
/// Component that controls dynamic resolution scaling in HDRP (High Definition Render Pipeline) via DRH (Dynamic Resolution Handler).
/// </summary>
/// <remarks>
/// This class controls the dynamic resolution based on the averaged GPU frametime over [EvaluationFrameCount] frames.
/// If it exceeds the target [ScaleUpDuration] times consecutively (which takes [ScaleUpDuration] * [EvaluationFrameCount] frames), we request an increased screen scale to DRH.
/// If it falls behind the target framerate [ScaleDownDuration] times consecutively, we request a decreased screen scale to DRH.
/// </remarks>
public class HDDynamicResolution : MonoBehaviour
{
    /// <summary>
    /// Target frame rate for dynamic resolution. If Application.targetFrameRate is already set, Application.targetFrameRate overrides this parameter.
    /// </summary>
    [Min(1.0f)]
    [Tooltip("Sets the desired target frame rate in FPS. If Application.targetFrameRate is already set, Application.targetFrameRate overrides this parameter.")]
    public float DefaultTargetFrameRate = 60.0f;

    /// <summary>
    /// We evaluate GPU performance every [EvaluationFrameCount] frames. The evaluation is comparison between the averaged GPU frame time over [EvaluationFrameCount] frames and 1 / [DefaultTargetFrameRate].
    /// </summary>
    [Min(1)]
    [Tooltip("Per how many frames we evaluate GPU performance against the target frame rate, using the averaged GPU frame time over frames.")]
    public int EvaluationFrameCount = 15;

    /// <summary>
    /// If averaged GPU framerate exceeds the target framerate [ScaleUpDuration] times consecutively, we request increased scale to DRH. 
    /// </summary>
    [Tooltip("Sets the number of consecutive times where the GPU performance is above the target to increase dynamic resolution by one step.")]
    public uint ScaleUpDuration = 8;

    /// <summary>
    /// If averaged GPU framerate falls behind the target framerate [ScaleDownDuration] times consecutively, we request decreased scale to DRH.
    /// </summary>
    [Tooltip("Sets the number of consecutive times where the GPU performance is below the target to decrease dynamic resolution by one step.")]
    public uint ScaleDownDuration = 4;

    /// <summary>
    /// The number of steps to upscale from minimum screen percentage to maximum screen percentage.
    /// </summary>
    [Min(1)]
    [Tooltip("Sets the number of steps to upscale from minimum screen percentage to maximum screen percentage set in the current HDRP Asset.")]
    public int ScaleUpStepCount = 5;

    /// <summary>
    /// The number of steps to downscale from maximum screen percentage to minimum screen percentage.
    /// </summary>
    [Min(1)]
    [Tooltip("Sets the number of steps to downscale from maximum screen percentage to minimum screen percentage set in the current HDRP Asset.")]
    public int ScaleDownStepCount = 2;

    /// <summary>
    /// Enables the debug view of dynamic resolution. Only on development build or editor.
    /// </summary>
    [Tooltip("Enables the debug view of dynamic resolution.")]
    public bool EnableDebugView = false;

    // The number of frames to skip after initialization
    const uint InitialFramesToSkip = 1;

    float m_AccumGPUFrameTime = 0.0f;
    int m_CurrentFrameSlot = 0;

    float m_GPUFrameTime = 0.0f;

    uint m_ScaleUpCounter = 0;
    uint m_ScaleDownCounter = 0;

    // interpolation factor between min and max screen percentage
    static float s_CurrentScaleFraction = 1.0f;

    bool m_Initialized = false;
    uint m_InitialFrameCounter = 0;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    GUIStyle m_DebugStyle = new GUIStyle();

    float m_MaxGPUFrameTime = 0.0f;
    float m_MinGPUFrameTime = 0.0f;
    float m_SampledMaxGPUFrameTime = 0.0f;
    float m_SampledMinGPUFrameTime = 0.0f;
    float m_SampledCPUFrameTime = 0.0f;
#endif

    void Update()
    {
        if (!FrameTimingManager.IsFeatureEnabled())
        {
            return;
        }

        if (!m_Initialized)
        {
            if (m_InitialFrameCounter >= InitialFramesToSkip)
            {
                DynamicResolutionHandler.SetDynamicResScaler(
                    delegate () { return s_CurrentScaleFraction; },
                    DynamicResScalePolicyType.ReturnsMinMaxLerpFactor);
                m_Initialized = true;
            }
            else
            {
                ++m_InitialFrameCounter;
            }
        }

        if (m_Initialized && UpdateFrameStats())
        {
            // Normally, this scope is called every [EvaluationFrameCount] frames

            m_GPUFrameTime = m_AccumGPUFrameTime / EvaluationFrameCount;

            float targetFrameRate = Application.targetFrameRate > 0 ? (float)Application.targetFrameRate : DefaultTargetFrameRate;
            float desiredFrameTime = 1000.0f / targetFrameRate;
            float headroom = desiredFrameTime - m_GPUFrameTime;

            if (headroom < 0.0f) // inclined to scale down
            {
                m_ScaleUpCounter = 0;
                ++m_ScaleDownCounter;

                if (m_ScaleDownCounter >= ScaleDownDuration)
                {
                    m_ScaleDownCounter = 0;

                    s_CurrentScaleFraction = Mathf.Clamp01(s_CurrentScaleFraction - 1.0f / ScaleDownStepCount);
                }
            }
            else // inclined to scale up
            {
                m_ScaleDownCounter = 0;
                ++m_ScaleUpCounter;

                if (m_ScaleUpCounter >= ScaleUpDuration)
                {
                    m_ScaleUpCounter = 0;

                    s_CurrentScaleFraction = Mathf.Clamp01(s_CurrentScaleFraction + 1.0f / ScaleUpStepCount);
                }
            }
        }
    }

    static void ResetScale()
    {
        s_CurrentScaleFraction = 1.0f;
    }

    void ResetCounters()
    {
        m_ScaleUpCounter = 0;
        m_ScaleDownCounter = 0;
        m_CurrentFrameSlot = 0;
    }

    // Update GPU frame time history. Returns true if all the history buffer is filled.
    bool UpdateFrameStats()
    {
        // get one last frame's timings
        FrameTimingManager.CaptureFrameTimings();
        FrameTiming[] timing = new FrameTiming[1];
        uint numTimingsCopied = FrameTimingManager.GetLatestTimings(1, timing);

        if (numTimingsCopied == 0)
        {
            ResetCounters();
            return false; // The feature is off, or waiting for GPU readback.
        }

        if (timing[0].gpuFrameTime == 0.0) // Take this as an invalid frame. (frames disjoint event, GPU restart, or high GPU load?)
        {
            return false;
        }

        if (timing[0].cpuTimeFrameComplete < timing[0].cpuTimePresentCalled) // sanity check.
        {
            return false;
        }

        if (m_CurrentFrameSlot == 0)
            m_AccumGPUFrameTime = 0.0f;

        m_AccumGPUFrameTime += (float)timing[0].gpuFrameTime;

        UpdateGUIData(timing[0]);

        m_CurrentFrameSlot = (m_CurrentFrameSlot + 1) % EvaluationFrameCount;
        return m_CurrentFrameSlot == 0;
    }

    void OnEnable()
    {
    }

    void OnDisable()
    {
        ResetScale();
        ResetCounters();
    }

    void Start()
    {
    }

    void OnDestroy()
    {
        ResetScale();
    }

    void UpdateGUIData(FrameTiming timing)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (EnableDebugView)
        {
            if (m_CurrentFrameSlot == 0)
            {
                m_MaxGPUFrameTime = m_MinGPUFrameTime = (float)timing.gpuFrameTime;
            }
            else
            {
                m_MaxGPUFrameTime = Math.Max(m_MaxGPUFrameTime, (float)timing.gpuFrameTime);
                m_MinGPUFrameTime = Math.Min(m_MinGPUFrameTime, (float)timing.gpuFrameTime);
            }

            if (m_CurrentFrameSlot == EvaluationFrameCount - 1)
            {
                m_SampledCPUFrameTime = (float)timing.cpuFrameTime;
                m_SampledMaxGPUFrameTime = m_MaxGPUFrameTime;
                m_SampledMinGPUFrameTime = m_MinGPUFrameTime;
            }
        }
#endif
    }

    /// <summary>
    /// Print debug information on the screen.
    /// </summary>
    /// <remarks>
    /// Resolution on UI is the requested value in this component, not the actual resolution which is tuned in DynamicResolutionHandler.
    /// </remarks>
    void OnGUI()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (EnableDebugView)
        {
            m_DebugStyle = GUI.skin.box;
            m_DebugStyle.fontSize = 32;
            m_DebugStyle.alignment = TextAnchor.MiddleLeft;
            int drsWidth = 0;
            int drsHeight = 0;
            float scaleX = -1.0f;

            // debug only for main camera for simplicity.
            var camera = Camera.main;
            if (camera != null)
            {
                DynamicResolutionHandler.UpdateAndUseCamera(camera);
                var scaleFraction = DynamicResolutionHandler.instance.GetResolvedScale();
                drsWidth = (int)Mathf.Ceil(scaleFraction.x * Screen.width);
                drsHeight = (int)Mathf.Ceil(scaleFraction.y * Screen.height);
                scaleX = scaleFraction.x;
            }

            GUILayout.Label(
                string.Format(
                    "Resolution: {0} x {1}\nScale: {2:F3}\nCPU: {3:F3}\nGPU Ave: {4:F3} Max: {5:F3} Min: {6:F3}",
                    drsWidth,
                    drsHeight,
                    scaleX,
                    m_SampledCPUFrameTime,
                    m_GPUFrameTime,
                    m_SampledMaxGPUFrameTime,
                    m_SampledMinGPUFrameTime),
                m_DebugStyle);
        }
#endif
    }
}
