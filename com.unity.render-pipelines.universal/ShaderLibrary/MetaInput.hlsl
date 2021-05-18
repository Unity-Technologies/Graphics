#ifndef UNIVERSAL_META_INPUT_INCLUDED
#define UNIVERSAL_META_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/MetaPass.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

#define MetaInput UnityMetaInput
#define MetaFragment UnityMetaFragment
float4 MetaVertexPosition(float4 positionOS, float2 uv1, float2 uv2, float4 uv1ST, float4 uv2ST)
{
    return UnityMetaVertexPosition(positionOS.xyz, uv1, uv2, uv1ST, uv2ST);
}

struct MetaVaryings
{
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
#ifdef EDITOR_VISUALIZATION
    float2 VizUV        : TEXCOORD1;
    float4 LightCoord   : TEXCOORD2;
#endif
};

MetaVaryings FillMetaVaryings(float4 positionOS, float2 uv0, float2 uv1, float2 uv2, float4 uv1ST, float4 uv2ST)
{
    MetaVaryings ret = (MetaVaryings)0;
    ret.positionCS = UnityMetaVertexPosition(positionOS.xyz, uv1, uv2, uv1ST, uv2ST);

#ifdef EDITOR_VISUALIZATION
    if (unity_VisualizationMode == EDITORVIZ_TEXTURE)
        ret.VizUV = UnityMetaVizUV(unity_EditorViz_UVIndex, uv0, uv1, uv2, unity_EditorViz_Texture_ST);
    else if (unity_VisualizationMode == EDITORVIZ_SHOWLIGHTMASK)
    {
        ret.VizUV = uv1 * uv1ST.xy + uv1ST.zw;
        ret.LightCoord = mul(unity_EditorViz_WorldToLight, float4(TransformObjectToWorld(positionOS), 1));
    }
#endif
    return ret;
}

MetaVaryings FillMetaVaryings(float4 positionOS, float2 uv0, float2 uv1, float2 uv2)
{
    return FillMetaVaryings(positionOS, uv0, uv1, uv2, unity_LightmapST, unity_DynamicLightmapST);
}
#endif
