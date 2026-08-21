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
public class FSR4Options : UpscalerOptions
{
    #region BACKING_FIELDS
    [SerializeField]
    [Tooltip("Selects a performance quality setting for AMD FidelityFX 4 Super Resolution (FSR4).")]
    private FSR4Quality m_FSR4QualityMode = FSR4Quality.Quality;

    [SerializeField]
    [Tooltip("Enable an additional sharpening pass on FidelityFX 4 Super Resolution (FSR4).")]
    private bool m_EnableSharpening = false;

    [SerializeField]
    [Tooltip("The sharpness value between 0 and 1, where 0 is no additional sharpness and 1 is maximum additional sharpness.")]
    [Range(0.0f, 1.0f)]
    private float m_Sharpness = 0.92f;

    [SerializeField]
    [Tooltip("Enable runtime debug checking (may impact performance). Useful for development.")]
    private bool m_EnableDebugChecking = false;
    #endregion

    #region PROPERTIES
    /// <summary>
    /// Gets or sets the performance quality setting for AMD FSR4.
    /// </summary>
    public FSR4Quality fsr4QualityMode
    {
        get { return m_FSR4QualityMode; }
        set { m_FSR4QualityMode = value; }
    }

    /// <summary>
    /// Enables or disables the additional sharpening pass within FSR4.
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

    /// <summary>
    /// Enable runtime API validation and debug checks (may impact performance).
    /// </summary>
    public bool enableDebugChecking
    {
        get { return m_EnableDebugChecking; }
        set { m_EnableDebugChecking = value; }
    }
    #endregion
}

#endif // ENABLE_UPSCALER_FRAMEWORK && ENABLE_AMD && ENABLE_AMD_MODULE
