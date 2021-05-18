#ifndef UNIVERSAL_META_PASS_INCLUDED
#define UNIVERSAL_META_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv0          : TEXCOORD0;
    float2 uv1          : TEXCOORD1;
    float2 uv2          : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

#define Varyings MetaVaryings

Varyings UniversalVertexMeta(Attributes input)
{
    Varyings output = (Varyings)0;
    output = (Varyings)FillMetaVaryings(input.positionOS, input.uv0, input.uv1, input.uv2);
    return output;
}

half4 UniversalFragmentMeta(Varyings fragIn, MetaInput metaInput)
{
#ifdef EDITOR_VISUALIZATION
    metaInput.VizUV = fragIn.VizUV;
    metaInput.LightCoord = fragIn.LightCoord;
#endif

    return UnityMetaFragment(metaInput);
}
#endif
