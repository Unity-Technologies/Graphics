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
    [EnumOption(typeof(DLSSQuality), "DLSS Quality Mode")]
    public int DLSSQualityMode = (int)DLSSQuality.MaximumQuality;

    [BoolOption("Force Quality Mode")]
    public bool FixedResolutionMode = false;

    // TODO: fix available preset values, currently all values are displayed.
    // every preset have their own list of available presets, that may differ from each other.
    // need a way to represent the available subset of enum values for each preset to enforce the value requirements.
    [EnumOption(typeof(DLSSPreset), "DLSS Preset for Quality")]
    public int DLSSRenderPresetQuality;
    [EnumOption(typeof(DLSSPreset), "DLSS Preset for Balanced")]
    public int DLSSRenderPresetBalanced;
    [EnumOption(typeof(DLSSPreset), "DLSS Preset for Performance")]
    public int DLSSRenderPresetPerformance;
    [EnumOption(typeof(DLSSPreset), "DLSS Preset for Ultra Performance")]
    public int DLSSRenderPresetUltraPerformance;
    [EnumOption(typeof(DLSSPreset), "DLSS Preset for DLAA")]
    public int DLSSRenderPresetDLAA;
}

#endif // ENABLE_UPSCALER_FRAMEWORK && ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
