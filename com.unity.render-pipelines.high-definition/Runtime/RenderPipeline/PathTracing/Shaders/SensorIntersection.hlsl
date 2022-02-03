#ifndef UNITY_SENSOR_INTERSECTION_INCLUDED
#define UNITY_SENSOR_INTERSECTION_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingIntersection.hlsl"

// As we must keep the PathIntersection structure untouched (and at the very least of the same size),
// we alias some of its unused fields to store the sensors data.

float3 GetBeamOrigin(PathIntersection payload)
{
    return payload.value;
}

void SetBeamOrigin(inout PathIntersection payload, float3 beamOrigin)
{
    payload.value = beamOrigin;
}

float3 GetBeamDirection(PathIntersection payload)
{
    return float3(payload.alpha, payload.cone.width, payload.cone.spreadAngle);
}

void SetBeamDirection(inout PathIntersection payload, float3 beamDirection)
{
    payload.alpha = beamDirection.x;
    payload.cone.width = beamDirection.y;
    payload.cone.spreadAngle = beamDirection.z;
}

void ClearBeamData(inout PathIntersection payload)
{
    SetBeamOrigin(payload, 0.0);
    SetBeamDirection(payload, 0.0);
}

#endif // UNITY_SENSOR_INTERSECTION_INCLUDED
