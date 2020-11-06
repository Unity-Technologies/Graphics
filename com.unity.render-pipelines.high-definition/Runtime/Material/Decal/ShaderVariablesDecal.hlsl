#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.cs.hlsl"

#if SHADEROPTIONS_PREPASSLESS_DECALS == 1
    // Evaluating decals in the material pass requires a custom decals only light list.
    // Cannot reuse LightLoop g_vLightListGlobal and g_vLayeredOffsetsBuffer buffers, as they are in active use in forward rendered shader passes.
    StructuredBuffer<uint> g_vDecalLightListGlobal;
    StructuredBuffer<uint> g_vDecalLayeredOffsetsBuffer;
#endif

StructuredBuffer<DecalData> _DecalDatas;

TEXTURE2D(_DecalAtlas2D);
SAMPLER(_trilinear_clamp_sampler_DecalAtlas2D);

UNITY_INSTANCING_BUFFER_START(Decal)
UNITY_DEFINE_INSTANCED_PROP(float4x4, _NormalToWorld)
UNITY_DEFINE_INSTANCED_PROP(uint, _DecalLayerMaskFromDecal)
UNITY_INSTANCING_BUFFER_END(Decal)
