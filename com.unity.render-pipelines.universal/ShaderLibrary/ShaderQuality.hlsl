#ifndef UNIVERSAL_SHADER_OPTIONS_INCLUDED
#define UNIVERSAL_SHADER_OPTIONS_INCLUDED

//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderOptions.hlsl"
//use in some shader to compile properly:
//#pragma multi_compile SHADER_OPTIONS_LOW SHADER_OPTIONS_MEDIUM SHADER_OPTIONS_HIGH

#if !defined (SHADER_QUALITY_LOW) && !defined (SHADER_QUALITY_MEDIUM) && !defined (SHADER_QUALITY_HIGH)
#if defined(SHADER_API_MOBILE) || defined(SHADER_API_SWITCH)
#if defined(SHADER_API_GLES)
#define SHADER_QUALITY_LOW
#else
#define SHADER_QUALITY_MEDIUM
#endif
#else
#define SHADER_QUALITY_HIGH
#endif
#endif

#if defined (SHADER_QUALITY_LOW)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderQualityLow.cs.hlsl"
#elif defined (SHADER_QUALITY_MEDIUM)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderQualityMedium.cs.hlsl"
#elif defined (SHADER_QUALITY_HIGH)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderQualityHigh.cs.hlsl"
#endif
#endif

