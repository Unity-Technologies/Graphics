#ifndef RAY_TRACING_HELPERS_H_
#define RAY_TRACING_HELPERS_H_

#if defined(SHADER_STAGE_RAY_TRACING)
float EvaluateRayTracingBias(float3 positionRWS)
{
    float distanceToCamera = length(positionRWS);
    float blend = saturate((distanceToCamera - _ProjectionParams.y) / (_ProjectionParams.z - _ProjectionParams.y));
    return lerp(_RayTracingRayBias, _RayTracingDistantRayBias, blend);
}
#endif

#endif // RAY_TRACING_HELPERS_H_
