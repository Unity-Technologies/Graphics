#if ENABLE_UPSCALER_FRAMEWORK && ENABLE_METALFX_MODULE && (UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_TVOS || UNITY_VISIONOS)
using System;

namespace UnityEngine.Rendering.MetalFX
{
    [Serializable]
    public class MetalFXTemporalOptions : UpscalerOptions
    {
    }
}
#endif
