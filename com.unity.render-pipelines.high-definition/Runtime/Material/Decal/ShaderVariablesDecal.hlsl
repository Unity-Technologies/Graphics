#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.cs.hlsl"

StructuredBuffer<DecalData> _DecalDatas;

TEXTURE2D(_DecalAtlas2D);
SAMPLER(_trilinear_clamp_sampler_DecalAtlas2D);

#if defined(PLATFORM_SUPPORTS_BUFFER_ATOMICS_IN_PIXEL_SHADER)

// To be compatible on all platform (with Unity), we need to have the UAV following the
// last RT bind. Which maybe vary if we use 3 or 4 Render target
#if (defined(DECALS_3RT) || defined(DECALS_4RT))
    #ifdef SHADER_API_PSSL
    RWStructuredBuffer<uint> _DecalPropertyMaskBuffer;
    #else
    // DX11 spec say that we must setup an index for UAV that is following the number of Render Target bind (starting from 0)
    // So here for 4RT we have index 0 + 4 mean u4
    #ifdef DECALS_4RT
    RWStructuredBuffer<uint> _DecalPropertyMaskBuffer : register(u4);
    #else
    RWStructuredBuffer<uint> _DecalPropertyMaskBuffer : register(u3);
    #endif
#endif

#endif

StructuredBuffer<uint> _DecalPropertyMaskBufferSRV;
#endif

UNITY_INSTANCING_BUFFER_START(Decal)
UNITY_DEFINE_INSTANCED_PROP(float4x4, _NormalToWorld)
UNITY_INSTANCING_BUFFER_END(Decal)
