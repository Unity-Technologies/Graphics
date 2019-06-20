#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
#define USE_FOG 1
#endif
