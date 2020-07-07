#ifndef UNIVERSAL_DEPTH_ONLY_PASS_INCLUDED
#define UNIVERSAL_DEPTH_ONLY_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Attributes
{
    float4 positionOS     : POSITION;
    float4 tangentOS      : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float3 normal       : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float3 positionWS   : TEXCOORD0;
    float2 uv           : TEXCOORD1;

#if defined(_NORMALMAP)
    float4 normalWS                 : TEXCOORD2;    // xyz: normal, w: viewDir.x
    float4 tangentWS                : TEXCOORD3;    // xyz: tangent, w: viewDir.y
    float4 bitangentWS              : TEXCOORD4;    // xyz: bitangent, w: viewDir.z
#else
    float3 normalWS                 : TEXCOORD2;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

inline float4 EncodeDepthNormal(float depth, float3 normal)
{
    float4 enc;
    enc.xy = PackNormalOctRectEncode(normal);
    enc.zw = PackFloatToR8G8(depth);
    return enc;
}

Varyings DepthNormalsVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal, input.tangentOS);

    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.positionWS = vertexInput.positionWS;
    output.uv         = TRANSFORM_TEX(input.texcoord, _BaseMap);

    half3 viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;
    #if defined(_NORMALMAP)
        output.normalWS = half4(normalInput.normalWS, viewDirWS.x);
        output.tangentWS = half4(normalInput.tangentWS, viewDirWS.y);
        output.bitangentWS = half4(normalInput.bitangentWS, viewDirWS.z);
    #else
        output.normalWS = NormalizeNormalPerVertex(normalInput.normalWS);
    #endif

    return output;
}

float4 DepthNormalsFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);

    float3 normal = input.normalWS;
    return float4(PackNormalOctRectEncode(TransformWorldToViewDir(normal, true)), 0.0, 0.0);
}
#endif
