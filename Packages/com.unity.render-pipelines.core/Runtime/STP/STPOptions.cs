#if ENABLE_UPSCALER_FRAMEWORK
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class STPOptions : UpscalerOptions
{
    // UpscalerOptions contains the injection point option with default value BeforePostProcess.
    //
    // An empty options class like this must be defined & registered if we want to
    // provide the option to change the injection point of the IUpscaler.
    //
    // If no options are defined & registered for an IUpscaler, there won't be a dropdown
    // in the Render Pipeline settings to change the injection point. 
}

#endif
