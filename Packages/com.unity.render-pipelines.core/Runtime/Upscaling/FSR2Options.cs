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
    [Tooltip("Selects a performance quality setting for AMD FidelityFX 2.0 Super Resolution (FSR2).")]
    public FSR2Quality FSR2QualityMode = FSR2Quality.Quality;

    [Tooltip("Forces a fixed resolution scale derived from the selected quality mode, ignoring dynamic resolution.")]
    public bool FixedResolutionMode = false;

    [Tooltip("Enable an additional sharpening pass on FidelityFX 2.0 Super Resolution (FSR2).")]
    public bool EnableSharpening = false;

    [Tooltip("The sharpness value between 0 and 1, where 0 is no additional sharpness and 1 is maximum additional sharpness.")]
    [Range(0.0f, 1.0f)]
    public float Sharpness = 0.92f;
};


#endif // ENABLE_UPSCALER_FRAMEWORK && ENABLE_AMD && ENABLE_AMD_MODULE
