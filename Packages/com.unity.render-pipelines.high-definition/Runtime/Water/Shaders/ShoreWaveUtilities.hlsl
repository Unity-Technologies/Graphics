#ifndef SHORE_WAVE_UTILITIES_H
#define SHORE_WAVE_UTILITIES_H

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/ShaderVariablesWater.cs.hlsl"

struct WaveData
{
    float position;
    int bellIndex;
};

WaveData EvaluateWaveData(float position, float waveSpeed, float waveLength)
{
    WaveData waveData;

    // Compute the overall position (takes into account wave speed and wave length)
    waveData.position = (position - _SimulationTime * waveSpeed) / waveLength;

    // We need to know in which bell we are.
    waveData.bellIndex = waveData.position > 0.0 ? floor(waveData.position) : ceil(-waveData.position);

    // We're only interested in the fractional part of the position
    waveData.position = 1.0 - frac(waveData.position);

    // Return the wave data
    return waveData;
}

float BreakingRegionFactor(float2 breakingRange, float2 positionOS)
{
    return saturate((positionOS.x - breakingRange.x) / (breakingRange.y - breakingRange.x));
}

float2 EvaluateBlendRegion(WaterDeformerData deformer, float2 positionOS)
{
    return lerp(deformer.blendRegion, float2(0.1, 0.9), BreakingRegionFactor(deformer.breakingRange, positionOS));
}

float ShoreWaveAmplitudeVariation(float2 positionWS)
{
    return (0.8 + 0.2 * DeformerNoise2D(positionWS * 0.25));
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


float EvaluateSineWaveActivation(uint bellIndex, uint waveRepetition)
{
    // Based on the requested frequency, return the activation function
    return bellIndex % waveRepetition == 0 ? 1.0 : 0.0;
}

float EvaluateBoxBlendAttenuation(float2 regionSize, float2 deformerPosOS, float2 blendRegion, int cubicBlend)
{
    // Apply the edge attenuation
    float2 distanceToEdges = abs(regionSize) * 0.5 - abs(deformerPosOS);
    float2 lerpFactor = saturate(distanceToEdges / blendRegion);
    lerpFactor *= cubicBlend ? lerpFactor : 1;
    return min(lerpFactor.x, lerpFactor.y);
}

float EvaluateWaveBlendAttenuation(WaterDeformerData deformer, float2 positionOS)
{
    float2 blendRegion = EvaluateBlendRegion(deformer, positionOS);
    // Apply the edge attenuation
    float leftFactor = saturate((positionOS.y) / blendRegion.x);
    float rightFactor = saturate((1.0 - positionOS.y) / (1.0 - blendRegion.y));
    float factor = leftFactor * rightFactor;
    return factor * factor;
}

float EvaluateDeepFoamAmount(WaterDeformerData deformer, float positionOS)
{
    float rangeSize = max(deformer.deepFoamRange.y - deformer.deepFoamRange.x, 0.001);
    float midPoint = deformer.deepFoamRange.x + rangeSize * 0.5;
    float appartitionFactor = saturate((positionOS - deformer.deepFoamRange.x) / (midPoint - deformer.deepFoamRange.x));
    float fadeFactor = saturate((deformer.deepFoamRange.y - positionOS) / (deformer.deepFoamRange.y - midPoint));
    return appartitionFactor * fadeFactor;
}

float EvaluateSurfaceFoamAmount(WaterDeformerData deformer, float2 positionOS)
{
    // Evaluate the region where the foam happens
    float2 blendRegion = EvaluateBlendRegion(deformer, positionOS);

    // Wave breaking point
    float breakingRange = (blendRegion.y - blendRegion.x) * 0.5;
    float positionInRange = saturate((positionOS.y - blendRegion.x) / (blendRegion.y - blendRegion.x));
    float v = positionInRange > 0.5 ? 1.0 - positionInRange : positionInRange;
    float breakingFactor = saturate(v / 0.2);
    float appartitionFactor = saturate((positionOS.x - deformer.breakingRange.x) / 0.05);
    float fadeFactor = saturate((deformer.breakingRange.y - positionOS.x) / 0.05);
    return appartitionFactor * fadeFactor * breakingFactor;

}

#endif // SHORE_WAVE_UTILITIES_H
