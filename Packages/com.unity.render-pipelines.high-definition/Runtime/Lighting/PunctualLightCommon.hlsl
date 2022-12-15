#ifndef PUNCTUAL_LIGHT_COMMON_HLSL
#define PUNCTUAL_LIGHT_COMMON_HLSL

//-----------------------------------------------------------------------------
// Punctual Light evaluation helper
//-----------------------------------------------------------------------------

// distances = {d, d^2, 1/d, d_proj}
void ModifyDistancesForFillLighting(inout float4 distances, float lightSqRadius)
{
    // Apply the sphere light hack to soften the core of the punctual light.
    // It is not physically plausible (using max() is more correct, but looks worse).
    // See https://www.desmos.com/calculator/otqhxunqhl
    // We only modify 1/d for performance reasons.
    float sqDist = distances.y;
    distances.z = rsqrt(sqDist + lightSqRadius); // Recompute 1/d
}

// Returns the normalized light vector L
float3 GetPunctualLightVector(float3 positionWS, LightData light)
{
    float3 L;
    if (light.lightType == GPULIGHTTYPE_PROJECTOR_BOX)
    {
        L = -light.forward;
    }
    else
    {
        float3 lightToSample = positionWS - light.positionRWS;
        float3 unL     = -lightToSample;
        float  distSq  = dot(unL, unL);
        float  distRcp = rsqrt(distSq);
        L = unL * distRcp;
    }
    return L;
}

// Returns the normalized light vector L and the distances = {d, d^2, 1/d, d_proj}.
void GetPunctualLightVectors(float3 positionWS, LightData light, out float3 L, out float4 distances)
{
    float3 lightToSample = positionWS - light.positionRWS;

    distances.w = dot(lightToSample, light.forward);

    if (light.lightType == GPULIGHTTYPE_PROJECTOR_BOX)
    {
        float dist = distances.w;
        float distSq = dist * dist;

        L = -light.forward;
        distances.xyz = float3(dist, distSq, 1.0);
    }
    else
    {
        float3 unL     = -lightToSample;
        float  distSq  = dot(unL, unL);
        float  distRcp = rsqrt(distSq);
        float  dist    = distSq * distRcp;

        L = unL * distRcp;
        distances.xyz = float3(dist, distSq, distRcp);

        ModifyDistancesForFillLighting(distances, light.size.x);
    }
}

#endif
