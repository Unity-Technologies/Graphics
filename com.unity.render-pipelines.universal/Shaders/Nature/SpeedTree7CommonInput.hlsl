#ifndef UNIVERSAL_SPEEDTREE7COMMON_INPUT_INCLUDED
#define UNIVERSAL_SPEEDTREE7COMMON_INPUT_INCLUDED

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

#ifdef EFFECT_HUE_VARIATION
    half4 _HueVariation;
#endif

half4 _Color;

// For Directional lights, this variable contains shadow-casting light's direction.
// For Spot lights and Point lights, this variable contains shadow-casting light's position (direction is different at each vertex of the shadow casting geometry).
// This variable is set by UnityEngine.Rendering.Universal.ShadowUtils.SetupShadowCasterConstantBuffer in com.unity.render-pipelines.universal/Runtime/ShadowUtils.cs 
float3 _ShadowCastingLightParameters;


#endif
