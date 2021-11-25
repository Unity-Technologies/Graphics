#ifndef UNIVERSAL_SPEEDTREE7COMMON_INPUT_INCLUDED
#define UNIVERSAL_SPEEDTREE7COMMON_INPUT_INCLUDED

#if defined(SPEEDTREE_ALPHATEST)
#define _ALPHATEST_ON
#endif

#ifdef EFFECT_BUMP
    #define _NORMALMAP
#endif

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

#ifdef ENABLE_WIND
    #define WIND_QUALITY_NONE       0
    #define WIND_QUALITY_FASTEST    1
    #define WIND_QUALITY_FAST       2
    #define WIND_QUALITY_BETTER     3
    #define WIND_QUALITY_BEST       4
    #define WIND_QUALITY_PALM       5

    uniform half _WindQuality;
    uniform half _WindEnabled;

    #include "SpeedTreeWind.cginc"
#endif

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
float4 _MainTex_TexelSize;
float4 _MainTex_MipInfo;

#ifdef EFFECT_HUE_VARIATION
    half4 _HueVariation;
#endif

half4 _Color;

// Shadow Casting Light geometric parameters. These variables are used when applying the shadow Normal Bias and are set by UnityEngine.Rendering.Universal.ShadowUtils.SetupShadowCasterConstantBuffer in com.unity.render-pipelines.universal/Runtime/ShadowUtils.cs
// For Directional lights, _LightDirection is used when applying shadow Normal Bias.
// For Spot lights and Point lights, _LightPosition is used to compute the actual light direction because it is different at each shadow caster geometry vertex.
float3 _LightDirection;
float3 _LightPosition;

#endif
