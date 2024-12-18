// This file contains functionality to apply Fog as a deferred, full-screen post-processing pass.
// Requires depth texture as input.
#ifndef UNIVERSAL_FOG_DEFERRED
#define UNIVERSAL_FOG_DEFERRED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferInput.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings Vertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionOS = input.positionOS.xyz;
    output.positionCS = float4(positionOS.xy, UNITY_RAW_FAR_CLIP_VALUE, 1.0); // Force triangle to be on zfar

    return output;
}

half4 Frag(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    GBufferData gBufferData = UnpackGBuffers(input.positionCS.xy);
    float viewZ = LinearEyeDepth(gBufferData.depth, _ZBufferParams); // TODO: This wont work for orthographic camera!
    float nearToFarZ = max(viewZ - _ProjectionParams.y, 0);
    half fogFactor = ComputeFogFactorZ0ToFar(nearToFarZ);
    half fogIntensity = ComputeFogIntensity(fogFactor);

    return half4(unity_FogColor.rgb, fogIntensity);
}

#endif
