// Our main include uses HLSLSupport.cginc which is conflicting wiht the default SRP includes like Core.hlsl and Common.hlsl
// However there is no common include set between URP/HDRP and BiRP and we don't want to have to use two sets so we stuck
// to HLSLSupport.cginc. This creates some conflicts which, for now, can be fixed by undefining. This seems to work for now
// although it might have to be revisited at some point.
#undef GLOBAL_CBUFFER_START
#undef GLOBAL_CBUFFER_END
#undef CBUFFER_START
#undef CBUFFER_END
#undef SAMPLE_DEPTH_TEXTURE
#undef SAMPLE_DEPTH_TEXTURE_LOD

#ifdef SHADER_API_VULKAN
#undef UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION
#undef UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_0
#undef UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_90
#undef UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_180
#undef UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_270
#undef UNITY_DISPLAY_ORIENTATION_PRETRANSFORM
#endif

#include "Internal/UnityUIE.cginc"
