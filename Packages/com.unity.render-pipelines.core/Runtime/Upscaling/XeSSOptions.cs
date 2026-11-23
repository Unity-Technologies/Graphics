using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
#if ENABLE_XESS && ENABLE_INTELXESS_MODULE
using UnityEngine.Intel;
#endif
using System;


#if UNITY_EDITOR
using UnityEditor;
#endif

#if ENABLE_XESS && ENABLE_INTELXESS_MODULE

[Serializable]
public class XeSSOptions : UpscalerOptions
{
    #region BACKING_FIELDS
    [SerializeField]
    [Tooltip("Selects a performance quality setting for Intel XeSS.")]
    private XeSSQuality m_XeSSQualityMode = XeSSQuality.Quality;
    #endregion

    #region PROPERTIES
    /// <summary>
    /// Gets or sets the performance quality setting for Intel XeSS.
    /// </summary>
    public XeSSQuality xessQualityMode
    {
        get { return m_XeSSQualityMode; }
        set { m_XeSSQualityMode = value; }
    }
    #endregion

    /// <summary>
    /// Shallow copies values from another XeSSOptions instance into this one.
    /// </summary>
    public void CopyFrom(XeSSOptions other)
    {
        if (other == null)
            return;

        resolutionMode = other.resolutionMode;
        m_XeSSQualityMode = other.m_XeSSQualityMode;
    }
}

#endif // ENABLE_XESS && ENABLE_INTELXESS_MODULE
