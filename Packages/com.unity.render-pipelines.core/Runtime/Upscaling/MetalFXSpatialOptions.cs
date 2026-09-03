#if ENABLE_UPSCALER_FRAMEWORK && ENABLE_METALFX_MODULE && (UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_TVOS || UNITY_VISIONOS)
using System;

namespace UnityEngine.Rendering.MetalFX
{
    [Serializable]
    public class MetalFXSpatialOptions : UpscalerOptions
    {
        void Awake() => injectionPoint = DynamicResolutionHandler.UpsamplerScheduleType.AfterPost;
        void Reset() => injectionPoint = DynamicResolutionHandler.UpsamplerScheduleType.AfterPost;
    }
}
#endif
