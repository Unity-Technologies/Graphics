#ifndef WATER_DECAL_UTILITIES_H
#define WATER_DECAL_UTILITIES_H

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/ShaderVariablesWater.cs.hlsl"

float2 GetGradient(float2 intPos)
{
    float rand = frac(sin(dot(intPos, float2(12.9898, 78.233))) * 43758.5453);;
    float angle = 6.283185 * rand;
    return float2(cos(angle), sin(angle));
}

float DeformerNoise2D(float2 pos)
{
    float2 i = floor(pos);
    float2 f = pos - i;
    float2 blend = f * f * (3.0 - 2.0 * f);
    float g0 = dot(GetGradient(i + float2(0, 0)), f - float2(0, 0));
    float g1 = dot(GetGradient(i + float2(1, 0)), f - float2(1, 0));
    float g2 = dot(GetGradient(i + float2(0, 1)), f - float2(0, 1));
    float g3 = dot(GetGradient(i + float2(1, 1)), f - float2(1, 1));
    float noiseVal = lerp(lerp(g0, g1, blend.x), lerp(g2, g3, blend.x), blend.y);
    return saturate(noiseVal / 0.7 * 0.5 + 0.5); // normalize to about [0:1]
}

// Distance to a parabola by IQ
// https://iquilezles.org/articles/distfunctions2d/
float sdParabola(float2 pos, float k )
{
    pos.x = abs(pos.x);
    float ik = 1.0/k;
    float p = ik*(pos.y - 0.5*ik)/3.0;
    float q = 0.25*ik*ik*pos.x;
    float h = q*q - p*p*p;
    float r = sqrt(abs(h));
    float x = (h>0.0) ? pow(q+r,1.0/3.0) - pow(abs(q-r), 1.0/3.0)*sign(r-q) : 2.0*cos(atan2(r, q)/3.0)*sqrt(p);
    return length(pos-float2(x,k*x*x)) * sign(pos.x-x);
}

struct WaveData
{
    float position;
    int bellIndex;
};

WaveData EvaluateWaveData(float position)
{
    WaveData waveData;

    // We need to know in which bell we are.
    waveData.bellIndex = position > 0.0 ? floor(position) : ceil(-position);

    // We're only interested in the fractional part of the position
    waveData.position = 1.0 - frac(position);

    // Return the wave data
    return waveData;
}

float BreakingRegionFactor(float2 breakingRange, float2 positionOS)
{
    return saturate((positionOS.x - breakingRange.x) / (breakingRange.y - breakingRange.x));
}

float2 EvaluateBlendRegion(float2 blendRegion, float2 breakingRange, float2 positionOS)
{
    return lerp(blendRegion, float2(0.1, 0.9), BreakingRegionFactor(breakingRange, positionOS));
}

float ShoreWaveFirstShape(WaveData waveData)
{
    return 0.5 * sin((waveData.position - 1.25) * 2.0 * PI) + 0.5;
}

float ShoreWaveSecondShape(WaveData waveData)
{
    return waveData.position < 0.2 ? waveData.position * waveData.position  * 25.0 : 0.5 * cos(4 * (waveData.position - 0.2)) + 0.5;
}

float ShoreWaveBlendShapes(float firstLobe, float secondLobe, float2 breakingRange, float normalizedWidth)
{
    // Variable that goes from 0 to 1 and reaches 1.0 when the
    float shapePicking = saturate(normalizedWidth / breakingRange.x);
    float amplitude = lerp(firstLobe * shapePicking, secondLobe, shapePicking);
    if (normalizedWidth >= breakingRange.x)
    {
        if (normalizedWidth <= breakingRange.y)
        {
            float range = BreakingRegionFactor(breakingRange, normalizedWidth);
            amplitude *= lerp(1.0, 0.3, range);
        }
        else
        {
            float range = (normalizedWidth - breakingRange.y) / (1.0 - breakingRange.y);
            amplitude *= lerp(0.3, 0.0, range);
        }
    }
    return amplitude;
}


float EvaluateSineWaveActivation(uint bellIndex, uint skippedWaves)
{
    // Based on the requested frequency, return the activation function
    return bellIndex % skippedWaves == 0 ? 1.0 : 0.0;
}

void EvaluateBoxAmplitude_float(float2 uv, float2 blendRegion, bool cubicBlend, out float amplitude)
{
    // Apply the edge attenuation
    float2 distanceToEdges = 1 - abs(uv * 2 - 1);
    if (blendRegion.x != 0.0f) distanceToEdges.x /= blendRegion.x;
    if (blendRegion.y != 0.0f) distanceToEdges.y /= blendRegion.y;

    float2 lerpFactor = saturate(distanceToEdges);
    if (cubicBlend) lerpFactor *= lerpFactor;

    amplitude = min(lerpFactor.x, lerpFactor.y);
}

void EvaluateBowWaveAmplitude_float(float2 uv, float elevation, out float amplitude)
{
    float2 normPos = uv * 2 - 1;
    float transitionSize = 0.1;

    normPos.y = normPos.y * 0.5 + 0.5;
    float2 parabolaPos = normPos;
    parabolaPos.y -= transitionSize;

    float dist = sdParabola(parabolaPos, 1);

    if (dist > 0.0)
    {
        float transition = smoothstep(0, 1, saturate(dist / transitionSize));
        amplitude = lerp(elevation, 0.0, transition);
    }
    else
    {
        float transition = smoothstep(0, 1, saturate(-dist / (transitionSize)));
        amplitude = lerp(elevation, 1, transition);
    }

    // Apply attenuation so that the tail has less deformation with a cubic profile
    float lengthAttenuation = (1.0 - saturate(normPos.y));
    amplitude *= lengthAttenuation * lengthAttenuation;
}

float EvaluateWaveBlendAttenuation(float2 blendRegion, float2 breakingRange, float2 positionOS)
{
    float2 region = EvaluateBlendRegion(blendRegion, breakingRange, positionOS);
    // Apply the edge attenuation
    float leftFactor = saturate((positionOS.y) / region.x);
    float rightFactor = saturate((1.0 - positionOS.y) / (1.0 - region.y));
    float factor = leftFactor * rightFactor;
    return factor * factor;
}

float EvaluateDeepFoamAmount(float2 deepFoamRange, float positionOS)
{
    float rangeSize = max(deepFoamRange.y - deepFoamRange.x, 0.001);
    float midPoint = deepFoamRange.x + rangeSize * 0.5;
    float appartitionFactor = saturate((positionOS - deepFoamRange.x) / (midPoint - deepFoamRange.x));
    float fadeFactor = saturate((deepFoamRange.y - positionOS) / (deepFoamRange.y - midPoint));
    return appartitionFactor * fadeFactor;
}

float EvaluateSurfaceFoamAmount(float2 blendRegion, float2 breakingRange, float2 positionOS)
{
    // Evaluate the region where the foam happens
    float2 region = EvaluateBlendRegion(blendRegion, breakingRange, positionOS);

    // Wave breaking point
    float positionInRange = saturate((positionOS.y - region.x) / (region.y - region.x));
    float v = positionInRange > 0.5 ? 1.0 - positionInRange : positionInRange;
    float breakingFactor = saturate(v / 0.2);
    float appartitionFactor = saturate((positionOS.x - breakingRange.x) / 0.05);
    float fadeFactor = saturate((breakingRange.y - positionOS.x) / 0.05);
    return appartitionFactor * fadeFactor * breakingFactor;

}

void EvaluateShoreWaveDecal_float(float2 uv, float waveLength, uint skippedWaves, float2 waveBlend,
    float waveOffset, float2 breakingRange, float2 deepFoamRange, out float amplitude, out float2 foam)
{
    // Compute the overall position (takes into account wave speed and wave length)
    float t = ((uv.x * 2 - 1) - waveOffset) / waveLength;

    // Evaluate the wave data
    WaveData waveData = EvaluateWaveData(t);
    float waveActivation = EvaluateSineWaveActivation(waveData.bellIndex, skippedWaves);
    float attenuation = EvaluateWaveBlendAttenuation(waveBlend, breakingRange, uv);
    float noise2D = DeformerNoise2D(uv * 0.25);

    // Evaluate the round lobe
    float firstShape = ShoreWaveFirstShape(waveData);

    // Evaluate the breaking lobe
    float secondShape = ShoreWaveSecondShape(waveData);

    // Start from the target amplitude
    amplitude = (0.8 + 0.2 * noise2D) * 0.5;

    // Blend the lobes
    amplitude *= ShoreWaveBlendShapes(firstShape, secondShape, breakingRange, uv.x);

    // Apply the edge attenuation and skip wave
    amplitude *= waveActivation * attenuation;


    // Define where the foam appears on the wave
    float surfacefoamWaveLocation = saturate((waveData.position - 0.1) / 0.1) * (1.0 - saturate((waveData.position - 0.5) / 0.02));
    float deepFoamWaveLocation = saturate((waveData.position - 0.1) / 0.1) * (1.0 - saturate((waveData.position - 0.3) / 0.1));

    // Define what amount of foam appears
    float deepfoamAmount = EvaluateDeepFoamAmount(deepFoamRange, uv.x);
    float surfaceFoamAmount = EvaluateSurfaceFoamAmount(waveBlend, breakingRange, uv);

    // Evaluate the perlin noise
    float perlinNoise = 0.2 + noise2D;

    // Combine to generate the foam
    foam.x = surfaceFoamAmount * surfacefoamWaveLocation * 4;
    foam.y = deepfoamAmount * deepFoamWaveLocation * 2;
    foam *= perlinNoise * waveActivation * attenuation;
}

#endif // WATER_DECAL_UTILITIES_H
