#ifndef UNIVERSAL_SHADER_OPTIONS_INCLUDED
#define UNIVERSAL_SHADER_OPTIONS_INCLUDED

#if !defined (_SHADER_QUALITY_LOW) && !defined (_SHADER_QUALITY_MEDIUM) && !defined (_SHADER_QUALITY_HIGH)
#if defined(SHADER_API_GLES)
#define _SHADER_QUALITY_LOW
#elif defined(SHADER_API_MOBILE) || defined(SHADER_API_SWITCH)
#define _SHADER_QUALITY_MEDIUM
#else
#define _SHADER_QUALITY_HIGH
#endif
#endif


#if defined (_SHADER_QUALITY_LOW)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderQualityLow.cs.hlsl"
#elif defined (_SHADER_QUALITY_MEDIUM)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderQualityMedium.cs.hlsl"
#elif defined (_SHADER_QUALITY_HIGH)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderQualityHigh.cs.hlsl"
#endif
#endif
