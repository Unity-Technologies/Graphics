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

[Serializable]
public class FSR2Options : UpscalerOptions
{
    #region BACKING_FIELDS
    [SerializeField]
    [Tooltip("Selects a performance quality setting for AMD FidelityFX 2.0 Super Resolution (FSR2).")]
    private FSR2Quality m_FSR2QualityMode = FSR2Quality.Quality;

    [SerializeField]
    [Tooltip("Forces a fixed resolution scale derived from the selected quality mode, ignoring dynamic resolution.")]
    private bool m_FixedResolutionMode = false;

    [SerializeField]
    [Tooltip("Enable an additional sharpening pass on FidelityFX 2.0 Super Resolution (FSR2).")]
    private bool m_EnableSharpening = false;

    [SerializeField]
    [Tooltip("The sharpness value between 0 and 1, where 0 is no additional sharpness and 1 is maximum additional sharpness.")]
    [Range(0.0f, 1.0f)]
    private float m_Sharpness = 0.92f;
    #endregion

    #region PROPERTIES
    /// <summary>
    /// Gets or sets the performance quality setting for AMD FSR2.
    /// </summary>
    public FSR2Quality fsr2QualityMode
    {
        get { return m_FSR2QualityMode; }
        set { m_FSR2QualityMode = value; }
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
    /// Enables or disables the additional sharpening pass within FSR2.
    /// </summary>
    public bool enableSharpening
    {
        get { return m_EnableSharpening; }
        set { m_EnableSharpening = value; }
    }

    /// <summary>
    /// Controls the intensity of the sharpening filter (0.0 to 1.0).
    /// </summary>
    public float sharpness
    {
        get { return m_Sharpness; }
        set { m_Sharpness = value; }
    }
    #endregion
}

#endif // ENABLE_UPSCALER_FRAMEWORK && ENABLE_AMD && ENABLE_AMD_MODULE
