#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.cs.hlsl"

StructuredBuffer<DecalData> _DecalDatas;

TEXTURE2D(_DecalAtlas2D);
SAMPLER(_trilinear_clamp_sampler_DecalAtlas2D);

UNITY_INSTANCING_BUFFER_START(Decal)
UNITY_DEFINE_INSTANCED_PROP(float4x4, _NormalToWorld)
UNITY_DEFINE_INSTANCED_PROP(uint, _DecalLayerMaskFromDecal)
UNITY_INSTANCING_BUFFER_END(Decal)
