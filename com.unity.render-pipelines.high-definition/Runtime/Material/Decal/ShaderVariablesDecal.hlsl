#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.cs.hlsl"

StructuredBuffer<DecalData> _DecalDatas;

TEXTURE2D(_DecalAtlas2D);
SAMPLER(_trilinear_clamp_sampler_DecalAtlas2D);

#if defined(PLATFORM_SUPPORTS_BUFFER_ATOMICS_IN_PIXEL_SHADER)

#ifndef SHADERPASS
RWStructuredBuffer<uint> _DecalPropertyMaskBuffer : register(u0);
#elif !(SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR || SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH)
#ifdef DECALS_4RT
RWStructuredBuffer<uint> _DecalPropertyMaskBuffer : register(u4);
#else
RWStructuredBuffer<uint> _DecalPropertyMaskBuffer : register(u3);
#endif
#endif

StructuredBuffer<uint> _DecalPropertyMaskBufferSRV;
#endif

UNITY_INSTANCING_BUFFER_START(Decal)
UNITY_DEFINE_INSTANCED_PROP(float4x4, _NormalToWorld)
UNITY_INSTANCING_BUFFER_END(Decal)
