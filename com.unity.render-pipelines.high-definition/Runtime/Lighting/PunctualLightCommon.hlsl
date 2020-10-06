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

// Returns the normalized light vector L and the distances = {d, d^2, 1/d, d_proj}.
void GetPunctualLightVectors(float3 positionWS, LightData light, out float3 L, out float4 distances)
{
    float3 lightToSample = positionWS - light.positionRWS;

    distances.w = dot(lightToSample, light.forward);

    if (light.lightType == GPULIGHTTYPE_PROJECTOR_BOX)
    {
        L = -light.forward;

        float dist = -dot(lightToSample, L);
        float distSq = dist * dist;
        distances.y = distSq;
        if (light.rangeAttenuationBias == 1.0) // Light uses range attenuation
        {
            float distRcp = rcp(dist);
            distances.x = dist;
            distances.z = distRcp;
            ModifyDistancesForFillLighting(distances, light.size.x);
        }
        else // Light is directionnal
        {
            // Note we maintain distances.y as otherwise the windowing function will give wrong results.
            // There won't be attenuation as the light attenuation scale and biases are set such that attenuation is prevented.
            distances.xz = 1; // No distance or angle attenuation
        }
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
