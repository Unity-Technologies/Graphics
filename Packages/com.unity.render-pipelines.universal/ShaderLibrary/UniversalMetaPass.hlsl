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

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
#ifdef EDITOR_VISUALIZATION
    float2 VizUV        : TEXCOORD1;
    float4 LightCoord   : TEXCOORD2;
#endif
};

Varyings UniversalVertexMeta(Attributes input)
{
    Varyings output = (Varyings)0;
    output.positionCS = UnityMetaVertexPosition(input.positionOS.xyz, input.uv1, input.uv2);
    output.uv = TRANSFORM_TEX(input.uv0, _BaseMap);
#ifdef EDITOR_VISUALIZATION
    UnityEditorVizData(input.positionOS.xyz, input.uv0, input.uv1, input.uv2, output.VizUV, output.LightCoord);
#endif
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
