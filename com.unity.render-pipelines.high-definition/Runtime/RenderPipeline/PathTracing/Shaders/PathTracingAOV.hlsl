#ifndef UNITY_PATH_TRACING_AOV_INCLUDED
#define UNITY_PATH_TRACING_AOV_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingIntersection.hlsl"

struct AOVData
{
    float3 albedo;
    float3 normal;
};

// Helper functions to read and write AOV values in the payload, hijacking other existing member variables
void SetAlbedo(inout PathIntersection payload, float3 albedo)
{
    payload.cone.width = albedo.x;
    payload.cone.spreadAngle = albedo.y;
    payload.remainingDepth = asint(albedo.z);
}

float3 GetAlbedo(in PathIntersection payload)
{
    return float3(payload.cone.width, payload.cone.spreadAngle, asfloat(payload.remainingDepth));
}

void SetNormal(inout PathIntersection payload, float3 normal)
{
    payload.pixelCoord.x = asint(normal.x);
    payload.pixelCoord.y = asint(normal.y);
    payload.maxRoughness = normal.z;
}

float3 GetNormal(inout PathIntersection payload)
{
    return float3(asfloat(payload.pixelCoord.x), asfloat(payload.pixelCoord.y), payload.maxRoughness);
}

void ClearAOVData(inout PathIntersection payload)
{
    SetAlbedo(payload, 0.0);
    SetNormal(payload, 0.0);
    payload.motionVector = 0;
}

void WriteAOVData(inout PathIntersection pathIntersection, AOVData aovData, float3 positionOS)
{
    SetAlbedo(pathIntersection, aovData.albedo);
    SetNormal(pathIntersection, aovData.normal);

    float2 jitteredPixelCoord = pathIntersection.motionVector;

    // Compute motion vector
    float3 prevPosWS = mul(GetPrevObjectToWorldMatrix(), float4(positionOS, 1.0)).xyz;
    float4 prevClipPos = mul(UNITY_MATRIX_PREV_VP, prevPosWS);
    prevClipPos.xy /= prevClipPos.w;
    prevClipPos.y = -prevClipPos.y;

    float2 prevFramePos = (prevClipPos.xy * 0.5 + 0.5) * _ScreenSize.xy;
    pathIntersection.motionVector =  jitteredPixelCoord - prevFramePos;
}

#endif //UNITY_PATH_TRACING_AOV_INCLUDED
