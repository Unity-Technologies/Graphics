#ifndef UNIVERSAL_META_PASS_INCLUDED
#define UNIVERSAL_META_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/MetaPass.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

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

#define MetaInput UnityMetaInput

Varyings MetaVertexPosition(float4 positionOS, float2 uv0, float2 uv1, float2 uv2)
{
    Varyings ret = (Varyings)0;
    ret.positionCS = UnityMetaVertexPosition(positionOS.xyz, uv1, uv2, unity_LightmapST, unity_DynamicLightmapST);

#ifdef EDITOR_VISUALIZATION
    if (unity_VisualizationMode == EDITORVIZ_TEXTURE)
        ret.VizUV = UnityMetaVizUV(unity_EditorViz_UVIndex, uv0, uv1, uv2, unity_EditorViz_Texture_ST);
    else if (unity_VisualizationMode == EDITORVIZ_SHOWLIGHTMASK)
    {
        ret.VizUV = uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
        ret.LightCoord = mul(unity_EditorViz_WorldToLight, float4(TransformObjectToWorld(positionOS), 1));
    }
#endif
    return ret;
}

Varyings UniversalVertexMeta(Attributes input)
{
    Varyings output;
    output = MetaVertexPosition(input.positionOS, input.uv0, input.uv1, input.uv2);
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
