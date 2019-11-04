#ifdef SHADER_VARIABLES_INCLUDE_CB
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderVariablesDecal.cs.hlsl"
#else

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.cs.hlsl"

StructuredBuffer<DecalData> _DecalDatas;

TEXTURE2D(_DecalAtlas2D);
SAMPLER(_trilinear_clamp_sampler_DecalAtlas2D);

#ifdef PLATFORM_SUPPORTS_BUFFER_ATOMICS_IN_PIXEL_SHADER
RWStructuredBuffer<uint> _DecalPropertyMaskBuffer;
StructuredBuffer<uint> _DecalPropertyMaskBufferSRV;
#endif

UNITY_INSTANCING_BUFFER_START(Decal)
UNITY_DEFINE_INSTANCED_PROP(float4x4, _NormalToWorld)
UNITY_INSTANCING_BUFFER_END(Decal)

#endif
