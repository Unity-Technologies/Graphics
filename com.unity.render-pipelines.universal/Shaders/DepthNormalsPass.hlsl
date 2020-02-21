#ifndef UNIVERSAL_DEPTH_ONLY_PASS_INCLUDED
#define UNIVERSAL_DEPTH_ONLY_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Attributes
{
    float4 positionOS     : POSITION;
    float2 texcoord     : TEXCOORD0;
    float3 normal       : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float3 positionWS   : TEXCOORD0;
    float2 uv           : TEXCOORD1;
    float3 normal       : TEXCOORD2;
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
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal);

    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.positionWS = vertexInput.positionWS;
    output.uv         = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.normal     = NormalizeNormalPerVertex(normalInput.normalWS);

    return output;
}

float4 DepthNormalsFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);

    float depth = ComputeNormalizedDeviceCoordinatesWithZ(input.positionWS, UNITY_MATRIX_VP).z;

    // Retrieve the normal from the bump map or mesh normal
    #if defined(_NORMALMAP)
        float3 normal = SampleNormal(input.uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    #else
        float3 normal = NormalizeNormalPerPixel(input.normal);
    #endif
        //normal = NormalizeNormalPerPixel(input.normal);
    return EncodeDepthNormal(depth, normal);
}
#endif
