#ifndef HDRP_SPEEDTREE7_INPUT_INCLUDED
#define HDRP_SPEEDTREE7_INPUT_INCLUDED

#define SPEEDTREE_Y_UP

#ifdef EFFECT_BUMP
    #define _NORMALMAP
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"


///////////////////////////////////////////////////////////////////////
//  SpeedTree winds

#ifdef ENABLE_WIND

#define WIND_QUALITY_NONE       0
#define WIND_QUALITY_FASTEST    1
#define WIND_QUALITY_FAST       2
#define WIND_QUALITY_BETTER     3
#define WIND_QUALITY_BEST       4
#define WIND_QUALITY_PALM       5

uniform float _WindQuality;
uniform float _WindEnabled;

#include "SpeedTreeWind.cginc"

#endif

// Define Input structure

struct Input
{
    float4 color;
    float3 interpolator1;
#ifdef GEOM_TYPE_BRANCH_DETAIL
    float3 interpolator2;
#endif
};

// Define uniforms

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

#ifdef GEOM_TYPE_BRANCH_DETAIL
    #define GEOM_TYPE_BRANCH
#endif

#ifdef GEOM_TYPE_BRANCH_DETAIL
    sampler2D _DetailTex;
#endif

#if defined(GEOM_TYPE_FROND) || defined(GEOM_TYPE_LEAF) || defined(GEOM_TYPE_FACING_LEAF)
    #define SPEEDTREE_ALPHATEST
    float _Cutoff;
#endif

#ifdef SCENESELECTIONPASS
    int _ObjectId;
    int _PassValue;
#endif

#ifdef EFFECT_HUE_VARIATION
    #define HueVariationAmount interpolator1.z
    float4 _HueVariation;
#endif

#if defined(EFFECT_BUMP) && !defined(LIGHTMAP_ON)
    sampler2D _BumpMap;
#endif

float4 _Color;

#endif
