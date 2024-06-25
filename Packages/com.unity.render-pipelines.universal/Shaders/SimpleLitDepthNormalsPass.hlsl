#ifndef UNIVERSAL_SIMPLE_LIT_DEPTH_NORMALS_PASS_INCLUDED
#define UNIVERSAL_SIMPLE_LIT_DEPTH_NORMALS_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#if defined(_ALPHATEST_ON) || defined(_NORMALMAP)
    #define REQUIRES_UV_INTERPOLATOR
#endif

struct Attributes
{
    float4 positionOS   : POSITION;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float3 normal       : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS      : SV_POSITION;

    #if defined(REQUIRES_UV_INTERPOLATOR)
        float2 uv          : TEXCOORD1;
    #endif

    #ifdef _NORMALMAP
        half4 normalWS    : TEXCOORD2;    // xyz: normal, w: viewDir.x
        half4 tangentWS   : TEXCOORD3;    // xyz: tangent, w: viewDir.y
        half4 bitangentWS : TEXCOORD4;    // xyz: bitangent, w: viewDir.z
    #else
        half3 normalWS    : TEXCOORD2;
        half3 viewDir     : TEXCOORD3;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};


Varyings DepthNormalsVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    #if defined(REQUIRES_UV_INTERPOLATOR)
        output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    #endif
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal, input.tangentOS);

    #if defined(_NORMALMAP)
        half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
        output.normalWS = half4(normalInput.normalWS, viewDirWS.x);
        output.tangentWS = half4(normalInput.tangentWS, viewDirWS.y);
        output.bitangentWS = half4(normalInput.bitangentWS, viewDirWS.z);
    #else
        output.normalWS = half3(NormalizeNormalPerVertex(normalInput.normalWS));
    #endif

    return output;
}

void DepthNormalsFragment(
    Varyings input
    , out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out float4 outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #if defined(_ALPHATEST_ON)
        Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
    #endif

    #if defined(LOD_FADE_CROSSFADE)
        LODFadeCrossFade(input.positionCS);
    #endif

    #if defined(_GBUFFER_NORMALS_OCT)
        float3 normalWS = normalize(input.normalWS);
        float2 octNormalWS = PackNormalOctQuadEncode(normalWS);           // values between [-1, +1], must use fp32 on some platforms
        float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);   // values between [ 0,  1]
        half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);      // values between [ 0,  1]
        outNormalWS = half4(packedNormalWS, 0.0);
    #else
        #if defined(_NORMALMAP)
            half3 normalTS = SampleNormal(input.uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));
            half3 normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz));
        #else
            half3 normalWS = input.normalWS;
        #endif

        normalWS = NormalizeNormalPerPixel(normalWS);
        outNormalWS = half4(normalWS, 0.0);
    #endif

    #ifdef _WRITE_RENDERING_LAYERS
        uint renderingLayers = GetMeshRenderingLayer();
        outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
    #endif
}

#endif
