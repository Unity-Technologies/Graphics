#ifndef TERRAIN_LIT_META_PASS_INCLUDED
#define TERRAIN_LIT_META_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl"

Varyings TerrainVertexMeta(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    TerrainInstancing(input.positionOS, input.normalOS, input.uv0);
    output = UniversalVertexMeta(input);
    return output;
}

half4 TerrainFragmentMeta(Varyings input) : SV_Target
{

    return UniversalFragmentMetaLit(input);
}

#endif
