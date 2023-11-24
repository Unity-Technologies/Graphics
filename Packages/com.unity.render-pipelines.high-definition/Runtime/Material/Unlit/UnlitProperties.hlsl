#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"

TEXTURE2D(_DistortionVectorMap);
SAMPLER(sampler_DistortionVectorMap);

TEXTURE2D(_UnlitColorMap);
SAMPLER(sampler_UnlitColorMap);

TEXTURE2D(_EmissiveColorMap);
SAMPLER(sampler_EmissiveColorMap);

CBUFFER_START(UnityPerMaterial)

float4  _UnlitColor;
float4 _UnlitColorMap_ST;
float4 _UnlitColorMap_TexelSize;

float3 _EmissiveColor;
float4 _EmissiveColorMap_ST;
float _EmissiveExposureWeight;

float _AlphaCutoff;
float _DistortionScale;
float _DistortionVectorScale;
float _DistortionVectorBias;
float _DistortionBlurScale;
float _DistortionBlurRemapMin;
float _DistortionBlurRemapMax;
float4 _DistortionVectorMap_ST;
float _AlphaRemapMin;
float _AlphaRemapMax;
float _BlendMode;

// Caution: C# code in BaseLitUI.cs call LightmapEmissionFlagsProperty() which assume that there is an existing "_EmissionColor"
// value that exist to identify if the GI emission need to be enabled.
// In our case we don't use such a mechanism but need to keep the code quiet. We declare the value and always enable it.
// TODO: Fix the code in legacy unity so we can customize the behavior for GI
float3 _EmissionColor;

// For raytracing indirect illumination effects, we need to be able to define if the emissive part of the material should contribute or not (mainly for area light sources in order to avoid double contribution)
// By default, the emissive is contributing
float _IncludeIndirectLighting;

// Mipmap Streaming Debug
UNITY_TEXTURE_STREAMING_DEBUG_VARS;

CBUFFER_END

// Following two variables are feeded by the C++ Editor for Scene selection
// Following three variables are feeded by the C++ Editor for Scene selection
int _ObjectId;
int _PassValue;

#ifdef UNITY_DOTS_INSTANCING_ENABLED

UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _UnlitColor);
    UNITY_DOTS_INSTANCED_PROP(float3, _EmissiveColor);
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaCutoff);
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

static float4 unity_DOTS_Sampled_UnlitColor;
static float3 unity_DOTS_Sampled_EmissiveColor;
static float  unity_DOTS_Sampled_AlphaCutoff;

void SetupDOTSUnlitPropertyCaches()
{
    unity_DOTS_Sampled_UnlitColor    = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _UnlitColor);
    unity_DOTS_Sampled_EmissiveColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float3, _EmissiveColor);
    unity_DOTS_Sampled_AlphaCutoff   = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaCutoff);
}

#undef UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES
#define UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES() SetupDOTSUnlitPropertyCaches()

#define _UnlitColor     unity_DOTS_Sampled_UnlitColor
#define _EmissiveColor  unity_DOTS_Sampled_EmissiveColor
#define _AlphaCutoff    unity_DOTS_Sampled_AlphaCutoff

#endif
