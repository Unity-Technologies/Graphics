#ifndef WATER_DEFORMATION_UTILITIES_H
#define WATER_DEFORMATION_UTILITIES_H

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/WaterSystemDef.cs.hlsl"

// The set of deformers that should be applied this frame
StructuredBuffer<WaterDeformerData> _WaterDeformerData;
Texture2D<float> _WaterDeformerTextureAtlas;

// This array allows us to convert vertex ID to local position
static const float2 deformerCorners[6] = {float2(-1, -1), float2(1, -1), float2(1, 1), float2(-1, -1), float2(1, 1), float2(-1, 1)};

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

// Shore wave functions
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/ShoreWaveUtilities.hlsl"

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

#endif // WATER_DEFORMATION_UTILITIES_H
