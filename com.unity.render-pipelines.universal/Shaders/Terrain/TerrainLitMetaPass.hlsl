#ifndef TERRAIN_LIT_META_PASS_INCLUDED
#define TERRAIN_LIT_META_PASS_INCLUDED

#define BASEMAPNAME _MainTex
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl"

MetaVaryings TerrainVertexMeta(Attributes input)
{
    MetaVaryings output;
    TerrainInstancing(input.positionOS, input.normalOS, input.uv0);
    output = UniversalMetaVertexPosition(input.positionOS, input.uv0, input.uv1, input.uv2);
    return output;
}

half4 TerrainFragmentMeta(MetaVaryings input) : SV_Target
{
    return UniversalFragmentMetaLit(input);
}

#endif
