#ifndef TERRAIN_LIT_META_PASS_INCLUDED
#define TERRAIN_LIT_META_PASS_INCLUDED

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
    //UNITY_VERTEX_OUTPUT_STEREO
};

Varyings TerrainVertexMeta(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    //UNITY_TRANSFER_INSTANCE_ID(input, output);
    TerrainInstancing(input.positionOS, input.normalOS, input.uv0);
    output.positionCS = MetaVertexPosition(input.positionOS, input.uv1, input.uv2,
        unity_LightmapST, unity_DynamicLightmapST);
    output.uv = TRANSFORM_TEX(input.uv0, _MainTex);
    return output;
}

half4 TerrainFragmentMeta(Varyings input) : SV_Target
{
    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);

    BRDFData brdfData;
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

    MetaInput metaInput;
    metaInput.Albedo = brdfData.diffuse + brdfData.specular * brdfData.roughness * 0.5;
    metaInput.SpecularColor = surfaceData.specular;
    metaInput.Emission = surfaceData.emission;

    return MetaFragment(metaInput);
}

#endif
