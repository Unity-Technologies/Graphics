#ifndef LIGHTWEIGHT_PASS_META_INCLUDED
#define LIGHTWEIGHT_PASS_META_INCLUDED

#include "LightweightSurfaceInput.cginc"
#include "LightweightLighting.cginc"
#include "UnityMetaPass.cginc"

struct MetaVertexInput
{
    float4 vertex   : POSITION;
    half3 normal    : NORMAL;
    float2 uv0      : TEXCOORD0;
    float2 uv1      : TEXCOORD1;
    float2 uv2      : TEXCOORD2;
#ifdef _TANGENT_TO_WORLD
    half4 tangent   : TANGENT;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct MetaVertexOuput
{
    float4 pos      : SV_POSITION;
    float2 uv       : TEXCOORD0;
};

MetaVertexOuput LightweightVertexMeta(MetaVertexInput v)
{
    MetaVertexOuput o;
    o.pos = UnityMetaVertexPosition(v.vertex, v.uv1.xy, v.uv2.xy, unity_LightmapST, unity_DynamicLightmapST);
    o.uv = TRANSFORM_TEX(v.uv0, _MainTex);
    return o;
}

fixed4 LightweightFragmentMeta(MetaVertexOuput i) : SV_Target
{
    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(i.uv, surfaceData);

    BRDFData brdfData;
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

    UnityMetaInput o;
    UNITY_INITIALIZE_OUTPUT(UnityMetaInput, o);

    #if defined(EDITOR_VISUALIZATION)
        o.Albedo = brdfData.diffuse;
    #else
        o.Albedo = brdfData.diffuse + brdfData.specular * brdfData.roughness * 0.5;
    #endif
    o.SpecularColor = surfaceData.specular;
    o.Emission = surfaceData.emission;

    return UnityMetaFragment(o);
}

fixed4 LightweightFragmentMetaSimple(MetaVertexOuput i) : SV_Target
{
    UnityMetaInput o;
    UNITY_INITIALIZE_OUTPUT(UnityMetaInput, o);

    float2 uv = i.uv;
    o.Albedo = _Color.rgb * tex2D(_MainTex, uv).rgb;
    o.SpecularColor = SpecularGloss(uv, 1.0);
    o.Emission = Emission(uv);

    return UnityMetaFragment(o);
}

#endif
