#ifndef UNITY_PATH_TRACING_AOV_INCLUDED
#define UNITY_PATH_TRACING_AOV_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingPayload.hlsl"

struct AOVData
{
    float3 albedo;
    float3 normal;
};

bool NeedAOVData(PathPayload payload)
{
    return payload.segmentID == 0 && IsOutputFlagOn(payload, OUTPUT_FLAG_AOV);
}

void WriteAOVData(AOVData aovData, float3 positionWS, inout PathPayload payload)
{
    // Check if we have anything to write to the payload
    if (!NeedAOVData(payload))
        return;

    // Compute motion vector (from pixel coordinates and world position passed as inputs)
    float3 positionOS = TransformWorldToObject(positionWS);
    float4 prevPosWS = mul(GetPrevObjectToWorldMatrix(), float4(positionOS, 1.0));
    float4 prevClipPos = mul(UNITY_MATRIX_PREV_VP, prevPosWS);
    prevClipPos.xy /= prevClipPos.w;
    prevClipPos.y = -prevClipPos.y;
    float2 viewportSize = _ScreenSize.xy;
    float2 prevPixelCoord = (prevClipPos.xy * 0.5 + 0.5) * viewportSize;

    // Write final AOV values to the payload
    payload.aovMotionVector -= prevPixelCoord;
    payload.aovAlbedo = aovData.albedo;
    payload.aovNormal = aovData.normal;
}

#endif //UNITY_PATH_TRACING_AOV_INCLUDED
