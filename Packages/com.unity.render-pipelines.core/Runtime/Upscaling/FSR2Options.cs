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
    [EnumOption(typeof(FSR2Quality), "FSR2 Quality Mode")]
    public int FSR2QualityMode = (int)FSR2Quality.Quality;

    [BoolOption("Fixed Resolution")]
    public bool FixedResolutionMode = false;

    [BoolOption("Enable Sharpening")]
    public bool EnableSharpening = false;

    [FloatOption(0.0f, 1.0f, "Sharpness")]
    public float Sharpness = 0.92f;
};


#endif // ENABLE_UPSCALER_FRAMEWORK && ENABLE_AMD && ENABLE_AMD_MODULE
