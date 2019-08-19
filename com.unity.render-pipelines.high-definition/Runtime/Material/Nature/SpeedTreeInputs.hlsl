#ifndef HDRP_SPEEDTREE_INPUT_INCLUDED
#define HDRP_SPEEDTREE_INPUT_INCLUDED

#define SPEEDTREE_Y_UP

#define _ALPHATEST_ON

//#ifdef EFFECT_BUMP    // Things like this should be done in the .cs
//#define _NORMALMAP
//#endif

// Enabling both TEXCOORD0 and TEXCOORD1 means you get 4 components instead of 2, and we pretty much always need that
//#define ATTRIBUTES_NEED_TEXCOORD0
//#define ATTRIBUTES_NEED_TEXCOORD1
//#if defined(_REQUIRE_UV2) || defined(_REQUIRE_UV3) || defined(DYNAMICLIGHTMAP_ON) || defined(DEBUG_DISPLAY) || (SHADERPASS == SHADERPASS_LIGHT_TRANSPORT)
//#define ATTRIBUTES_NEED_TEXCOORD2
//#define VARYINGS_NEED_TEXCOORD2
//#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

///////////////////////////////////////////////////////////////////////
//  struct SpeedTreeVertexInput

// texcoord setup
//
//      BRANCHES                        FRONDS                      LEAVES
// 0    diffuse uv, branch wind xy      "                           "
// 1    lod xyz, 0                      lod xyz, 0                  anchor xyz, lod scalar
// 2    detail/seam uv, seam amount, 0  frond wind xyz, 0           leaf wind xyz, leaf group

struct SpeedTreeVertexInput
{
    float4 vertex       : POSITION;
    float4 tangent      : TANGENT;
    float3 normal       : NORMAL;
    float4 texcoord     : TEXCOORD0;
    float4 texcoord1    : TEXCOORD1;
    float4 texcoord2    : TEXCOORD2;
    float2 texcoord3    : TEXCOORD3;
    float4 color        : COLOR;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

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

#include "SpeedTreeWind.hlsl"

#ifdef SPEEDTREE_VERSION_8
UNITY_INSTANCING_BUFFER_START(STWind)
UNITY_DEFINE_INSTANCED_PROP(float, _GlobalWindTime)
UNITY_INSTANCING_BUFFER_END(STWind)
#endif

#endif

#endif
