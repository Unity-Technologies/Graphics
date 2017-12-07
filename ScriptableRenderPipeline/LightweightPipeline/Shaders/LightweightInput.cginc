#ifndef LIGHTWEIGHT_INPUT_INCLUDED
#define LIGHTWEIGHT_INPUT_INCLUDED

#define MAX_VISIBLE_LIGHTS 16

struct LightInput
{
    float4 pos;
    half4 color;
    half4 distanceAttenuation;
    half4 spotDirection;
    half4 spotAttenuation;
};

// Main light initialized without indexing
#define INITIALIZE_MAIN_LIGHT(light) \
    light.pos = _MainLightPosition; \
    light.color = _MainLightColor; \
    light.distanceAttenuation = _MainLightDistanceAttenuation; \
    light.spotDirection = _MainLightSpotDir; \
    light.spotAttenuation = _MainLightSpotAttenuation

// Indexing might have a performance hit for old mobile hardware
#define INITIALIZE_LIGHT(light, i) \
    half4 indices = (i < 4) ? unity_4LightIndices0 : unity_4LightIndices1; \
    int index = (i < 4) ? i : i - 4; \
    int lightIndex = indices[index]; \
    light.pos = _AdditionalLightPosition[lightIndex]; \
    light.color = _AdditionalLightColor[lightIndex]; \
    light.distanceAttenuation = _AdditionalLightDistanceAttenuation[lightIndex]; \
    light.spotDirection = _AdditionalLightSpotDir[lightIndex]; \
    light.spotAttenuation = _AdditionalLightSpotAttenuation[lightIndex]

///////////////////////////////////////////////////////////////////////////////
//                      Constant Buffers                                     //
///////////////////////////////////////////////////////////////////////////////

CBUFFER_START(_PerFrame)
half4 _GlossyEnvironmentColor;
half4 _SubtractiveShadowColor;
CBUFFER_END

CBUFFER_START(_PerCamera)
float4 _MainLightPosition;
half4 _MainLightColor;
half4 _MainLightDistanceAttenuation;
half4 _MainLightSpotDir;
half4 _MainLightSpotAttenuation;
float4x4 _WorldToLight;

half4 _AdditionalLightCount;
float4 _AdditionalLightPosition[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightColor[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightDistanceAttenuation[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightSpotDir[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightSpotAttenuation[MAX_VISIBLE_LIGHTS];
CBUFFER_END

sampler2D _MainLightCookie;

// These are set internally by the engine upon request by RendererConfiguration.
// Check GetRendererSettings in LightweightPipeline.cs
CBUFFER_START(_PerObject)
half4 unity_LightIndicesOffsetAndCount;
half4 unity_4LightIndices0;
half4 unity_4LightIndices1;
CBUFFER_END

#endif
