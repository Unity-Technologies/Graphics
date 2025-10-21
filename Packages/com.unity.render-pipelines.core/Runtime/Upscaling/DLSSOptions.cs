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
using UnityEditorInternal;
#endif

#if ENABLE_UPSCALER_FRAMEWORK && ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE

[Serializable]
public class DLSSOptions : UpscalerOptions
{
    [Tooltip("Selects a performance quality setting for NVIDIA Deep Learning Super Sampling (DLSS).")]
    public DLSSQuality DLSSQualityMode = DLSSQuality.MaximumQuality;

    [Tooltip("Forces a fixed resolution scale derived from the selected quality mode, ignoring dynamic resolution.")]
    public bool FixedResolutionMode = false;

    [Tooltip("DLSS will use the specified render preset for the Quality mode.")]
    public DLSSPreset DLSSRenderPresetQuality;
    [Tooltip("DLSS will use the specified render preset for the Balanced mode.")]
    public DLSSPreset DLSSRenderPresetBalanced;
    [Tooltip("DLSS will use the specified render preset for the Performance mode.")]
    public DLSSPreset DLSSRenderPresetPerformance;
    [Tooltip("DLSS will use the specified render preset for the Ultra Performance mode.")]
    public DLSSPreset DLSSRenderPresetUltraPerformance;
    [Tooltip("DLSS will use the specified render preset for the DLAA mode.")]
    public DLSSPreset DLSSRenderPresetDLAA;
}

#endif // ENABLE_UPSCALER_FRAMEWORK && ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
