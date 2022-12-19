#ifndef RAY_TRACING_SHADOW_UTILITIES_H_
#define RAY_TRACING_SHADOW_UTILITIES_H_

// Constant buffer that holds all scalar that we need
CBUFFER_START(RayTracingShadowUtilitiesConstantBuffer)
    uint _RaytracingTargetLight;
    float _RaytracingLightRadius;
    float _RaytracingLightAngle;
    float _RaytracingLightSizeX;
    float _RaytracingLightSizeY;
    int _RaytracingShadowSlot;

    float4 _RaytracingChannelMask;
    float4 _RaytracingChannelMask0;
    float4 _RaytracingChannelMask1;
CBUFFER_END

// The various cases for shadow rays (spot and point)
#define POINT_VALID_DIR_PDF 1.0
#define POINT_WHITE_PDF -1.0
#define POINT_BLACK_PDF -2.0
#define POINT_BACK_FACE_PDF -3.0

float DistSqrToLight(LightData lightData, float3 positionWS)
{
    // If the point is inside the spherical range, it will be visible
    float3 dir = (lightData.positionRWS - positionWS);
    return dot(dir, dir);
}

bool PositionInSpotRange(LightData lightData, float spotAngle, float3 positionWS, float dist2)
{
    // Is the light inside the range of the light?
    bool pointInsideRange = dist2 < (lightData.range * lightData.range);

    // If the direction is going out of the cone, we invalidate this ray (to avoid casting useless rays)
    // We need to use the direction towards the cone origin for culling, not the sampled point on the light
    float3 relativePosition = positionWS - lightData.positionRWS;
    float3 cullingDirection = normalize(relativePosition);
    bool pointInsideCone = dot(cullingDirection, lightData.forward) > cos(0.5f * spotAngle);

    return pointInsideRange && pointInsideCone;
}

bool PositionInPyramidRange(LightData lightData, float spotAngleX, float spotAngleY, float3 positionWS, float dist2)
{
    // Is the light inside the range of the light?
    bool pointInsideRange = dist2 < (lightData.range * lightData.range);

    // If the direction is going out of the cone, we invalidate this ray (to avoid casting useless rays)
    float3 relativePosition = positionWS - lightData.positionRWS;

    float3 lightX = normalize(lightData.right);
    float3 lightY = normalize(lightData.up);
    float3 cullingDirectionX = normalize(relativePosition - dot(relativePosition, lightY) * lightY);
    float3 cullingDirectionY = normalize(relativePosition - dot(relativePosition, lightX) * lightX);

    bool pointInsidePyramid = dot(cullingDirectionX, lightData.forward) > cos(0.5f * spotAngleX) &&
                              dot(cullingDirectionY, lightData.forward) > cos(0.5f * spotAngleY);

    return pointInsideRange && pointInsidePyramid;
}

bool PositionInBoxRange(LightData lightData, float sizeX, float sizeY, float3 positionWS, float dist2)
{
    // Is the light inside the range of the light?
    bool pointInsideRange = dist2 < (lightData.range * lightData.range);

    // If the position is outside of the box light shaft, we invalidate this ray (to avoid casting useless rays)
    float3 relativePosition = positionWS - lightData.positionRWS;
    bool pointInsideBox = abs(dot(relativePosition, normalize(lightData.right))) < 0.5f * sizeX &&
                          abs(dot(relativePosition, normalize(lightData.up))) < 0.5f * sizeY;

    return pointInsideRange && pointInsideBox;
}

bool PositionInPointRange(LightData lightData, float dist2)
{
    return dist2 < (lightData.range * lightData.range);
}

#endif // RAY_TRACING_SHADOW_UTILITIES_H_
