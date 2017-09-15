#ifndef LIGHTWEIGHT_INPUT_INCLUDED
#define LIGHTWEIGHT_INPUT_INCLUDED

#include "UnityCG.cginc"

#define MAX_VISIBLE_LIGHTS 16

// Main light initialized without indexing
#define INITIALIZE_MAIN_LIGHT(light) \
    light.pos = _LightPosition; \
    light.color = _LightColor; \
    light.atten = _LightAttenuationParams; \
    light.spotDir = _LightSpotDir;

// Indexing might have a performance hit for old mobile hardware
#define INITIALIZE_LIGHT(light, lightIndex) \
                            light.pos = globalLightPos[lightIndex]; \
                            light.color = globalLightColor[lightIndex]; \
                            light.atten = globalLightAtten[lightIndex]; \
                            light.spotDir = globalLightSpotDir[lightIndex]

#if !(defined(_SINGLE_DIRECTIONAL_LIGHT) || defined(_SINGLE_SPOT_LIGHT) || defined(_SINGLE_POINT_LIGHT))
#define _MULTIPLE_LIGHTS
#endif

#if defined(UNITY_COLORSPACE_GAMMA) && defined(LIGHTWEIGHT_LINEAR)
// Ideally we want an approximation of gamma curve 2.0 to save ALU on GPU but as off now it won't match the GammaToLinear conversion of props in engine
//#define LIGHTWEIGHT_GAMMA_TO_LINEAR(gammaColor) gammaColor * gammaColor
//#define LIGHTWEIGHT_LINEAR_TO_GAMMA(linColor) sqrt(color)
#define LIGHTWEIGHT_GAMMA_TO_LINEAR(sRGB) sRGB * (sRGB * (sRGB * 0.305306011h + 0.682171111h) + 0.012522878h)
#define LIGHTWEIGHT_LINEAR_TO_GAMMA(linRGB) max(1.055h * pow(max(linRGB, 0.h), 0.416666667h) - 0.055h, 0.h)
#define LIGHTWEIGHT_GAMMA_TO_LINEAR4(sRGBA) float4(sRGBA.rgb * (sRGBA.rgb * (sRGBA.rgb * 0.305306011h + 0.682171111h) + 0.012522878h), sRGBA.a)
#define LIGHTWEIGHT_LINEAR_TO_GAMMA4(linRGBA) float4(max(1.055h * pow(max(linRGBA.rgb, 0.h), 0.416666667h) - 0.055h, 0.h), linRGBA.a)
#else
#define LIGHTWEIGHT_GAMMA_TO_LINEAR(color) color
#define LIGHTWEIGHT_LINEAR_TO_GAMMA(color) color
#define LIGHTWEIGHT_GAMMA_TO_LINEAR4(color) color
#define LIGHTWEIGHT_LINEAR_TO_GAMMA4(color) color
#endif


struct LightInput
{
    float4 pos;
    half4 color;
    float4 atten;
    half4 spotDir;
};

sampler2D _AttenuationTexture;

// Per object light list data
#ifdef _MULTIPLE_LIGHTS
half4 unity_LightIndicesOffsetAndCount;
half4 unity_4LightIndices0;

// The variables are very similar to built-in unity_LightColor, unity_LightPosition,
// unity_LightAtten, unity_SpotDirection as used by the VertexLit shaders, except here
// we use world space positions instead of view space.
half4 globalLightCount;
half4 globalLightColor[MAX_VISIBLE_LIGHTS];
float4 globalLightPos[MAX_VISIBLE_LIGHTS];
half4 globalLightSpotDir[MAX_VISIBLE_LIGHTS];
float4 globalLightAtten[MAX_VISIBLE_LIGHTS];
#else
float4 _LightPosition;
half4 _LightColor;
float4 _LightAttenuationParams;
half4 _LightSpotDir;
#endif

sampler2D _MetallicSpecGlossMap;

half4 _DieletricSpec;
half _Shininess;
samplerCUBE _Cube;
half4 _ReflectColor;

struct VertInput
{
	float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float4 texcoord0 : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
#ifdef VERTINPUT_CUSTOM
	VERTINPUT_CUSTOM;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertOutput
{
	float4 posWS : TEXCOORD0;
	float4 viewDir : TEXCOORD1; // xyz: viewDir
	half4 fogCoord : TEXCOORD2; // x: fogCoord, yzw: vertexColor
	half3 normal : TEXCOORD3;
	half4 meshUV0 : TEXCOORD4; // uv01.xy: uv0, uv01.zw: uv1 // uv
#ifdef VERTOUTPUT_CUSTOM
	VERTOUTPUT_CUSTOM
#endif
	float4 hpos : SV_POSITION;
    UNITY_VERTEX_OUTPUT_STEREO
};

#endif
