using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_UPSCALER_FRAMEWORK && ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
using UnityEngine.NVIDIA;
#endif
using System;

#if ENABLE_UPSCALER_FRAMEWORK && ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE

[Serializable]
public class DLSSOptions : UpscalerOptions
{
    #region BACKING_FIELDS
    [SerializeField]
    [Tooltip("Selects a performance quality setting for NVIDIA Deep Learning Super Sampling (DLSS).")]
    private DLSSQuality m_DLSSQualityMode = DLSSQuality.MaximumQuality;

    [SerializeField]
    [Tooltip("Forces a fixed resolution scale derived from the selected quality mode, ignoring dynamic resolution.")]
    private bool m_FixedResolutionMode = false;

    [Header("Render Presets")]
    [SerializeField]
    [Tooltip("DLSS will use the specified render preset for the Quality mode.")]
    private DLSSPreset m_DLSSRenderPresetQuality;

    [SerializeField]
    [Tooltip("DLSS will use the specified render preset for the Balanced mode.")]
    private DLSSPreset m_DLSSRenderPresetBalanced;

    [SerializeField]
    [Tooltip("DLSS will use the specified render preset for the Performance mode.")]
    private DLSSPreset m_DLSSRenderPresetPerformance;

    [SerializeField]
    [Tooltip("DLSS will use the specified render preset for the Ultra Performance mode.")]
    private DLSSPreset m_DLSSRenderPresetUltraPerformance;

    [SerializeField]
    [Tooltip("DLSS will use the specified render preset for the DLAA mode.")]
    private DLSSPreset m_DLSSRenderPresetDLAA;
    #endregion

    #region PROPERTIES
    /// <summary>
    /// Gets or sets the performance quality setting for NVIDIA DLSS.
    /// </summary>
    public DLSSQuality dlssQualityMode
    {
        get { return m_DLSSQualityMode; }
        set { m_DLSSQualityMode = value; }
    }

    /// <summary>
    /// If true, forces a fixed resolution scale derived from the quality mode, ignoring dynamic resolution settings.
    /// </summary>
    public bool fixedResolutionMode
    {
        get { return m_FixedResolutionMode; }
        set { m_FixedResolutionMode = value; }
    }

    /// <summary>
    /// The specific render preset to use when DLSS is in Quality mode.
    /// </summary>
    public DLSSPreset dlssRenderPresetQuality
    {
        get { return m_DLSSRenderPresetQuality; }
        set { m_DLSSRenderPresetQuality = value; }
    }

    /// <summary>
    /// The specific render preset to use when DLSS is in Balanced mode.
    /// </summary>
    public DLSSPreset dlssRenderPresetBalanced
    {
        get { return m_DLSSRenderPresetBalanced; }
        set { m_DLSSRenderPresetBalanced = value; }
    }

    /// <summary>
    /// The specific render preset to use when DLSS is in Performance mode.
    /// </summary>
    public DLSSPreset dlssRenderPresetPerformance
    {
        get { return m_DLSSRenderPresetPerformance; }
        set { m_DLSSRenderPresetPerformance = value; }
    }

    /// <summary>
    /// The specific render preset to use when DLSS is in Ultra Performance mode.
    /// </summary>
    public DLSSPreset dlssRenderPresetUltraPerformance
    {
        get { return m_DLSSRenderPresetUltraPerformance; }
        set { m_DLSSRenderPresetUltraPerformance = value; }
    }

    /// <summary>
    /// The specific render preset to use when DLSS is in DLAA mode.
    /// </summary>
    public DLSSPreset dlssRenderPresetDLAA
    {
        get { return m_DLSSRenderPresetDLAA; }
        set { m_DLSSRenderPresetDLAA = value; }
    }
    #endregion

    /// <summary>
    /// Checks if the settings of this object match another instance.
    /// </summary>
    public bool IsSame(DLSSOptions other)
    {
        if (other == null)
            return false;

        return m_DLSSQualityMode == other.m_DLSSQualityMode &&
               m_FixedResolutionMode == other.m_FixedResolutionMode &&
               m_DLSSRenderPresetQuality == other.m_DLSSRenderPresetQuality &&
               m_DLSSRenderPresetBalanced == other.m_DLSSRenderPresetBalanced &&
               m_DLSSRenderPresetPerformance == other.m_DLSSRenderPresetPerformance &&
               m_DLSSRenderPresetUltraPerformance == other.m_DLSSRenderPresetUltraPerformance &&
               m_DLSSRenderPresetDLAA == other.m_DLSSRenderPresetDLAA;
    }

    /// <summary>
    /// Shallow copies values from another DLSSOptions instance into this one.
    /// </summary>
    public void CopyFrom(DLSSOptions other)
    {
        if (other == null)
            return;

        m_DLSSQualityMode = other.m_DLSSQualityMode;
        m_FixedResolutionMode = other.m_FixedResolutionMode;
        m_DLSSRenderPresetQuality = other.m_DLSSRenderPresetQuality;
        m_DLSSRenderPresetBalanced = other.m_DLSSRenderPresetBalanced;
        m_DLSSRenderPresetPerformance = other.m_DLSSRenderPresetPerformance;
        m_DLSSRenderPresetUltraPerformance = other.m_DLSSRenderPresetUltraPerformance;
        m_DLSSRenderPresetDLAA = other.m_DLSSRenderPresetDLAA;
    }
}

#endif // ENABLE_UPSCALER_FRAMEWORK && ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
