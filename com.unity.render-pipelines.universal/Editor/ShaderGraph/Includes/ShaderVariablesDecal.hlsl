//#include "Packages/com.unity.render-pipelines.universal/Runtime/Decal/Decal.cs.hlsl"

UNITY_INSTANCING_BUFFER_START(Decal)
UNITY_DEFINE_INSTANCED_PROP(float4x4, _NormalToWorld)
UNITY_DEFINE_INSTANCED_PROP(uint, _DecalLayerMaskFromDecal)
UNITY_INSTANCING_BUFFER_END(Decal)
