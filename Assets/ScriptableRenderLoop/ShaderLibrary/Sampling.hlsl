#ifndef UNITY_SAMPLING_INCLUDED
#define UNITY_SAMPLING_INCLUDED

//-----------------------------------------------------------------------------
// Sample generator
//-----------------------------------------------------------------------------

#include "Fibonacci.hlsl"
#include "Hammersley.hlsl"

float Hash(uint s)
{
    s = s ^ 2747636419u;
    s = s * 2654435769u;
    s = s ^ (s >> 16);
    s = s * 2654435769u;
    s = s ^ (s >> 16);
    s = s * 2654435769u;
    return float(s) / 4294967295.0;
}

float2 InitRandom(float2 input)
{
    float2 r;
    r.x = Hash(uint(input.x * 4294967295.0));
    r.y = Hash(uint(input.y * 4294967295.0));

    return r;
}

//-----------------------------------------------------------------------------
// Sampling function
// Reference : http://www.cs.virginia.edu/~jdl/bib/globillum/mis/shirley96.pdf + PBRT
// Caution: Our light point backward (-Z), these sampling function follow this convention
//-----------------------------------------------------------------------------

float3 UniformSampleSphere(float u1, float u2)
{
    float phi = TWO_PI * u2;
    float cosTheta = 1.0 - 2.0 * u1;
    float sinTheta = sqrt(max(0.0, 1.0 - cosTheta * cosTheta));

    return float3(sinTheta * cos(phi), sinTheta * sin(phi), cosTheta); // Light point backward (-Z)
}

float3 UniformSampleHemisphere(float u1, float u2)
{
    float phi = TWO_PI * u2;
    float cosTheta = u1;
    float sinTheta = sqrt(max(0.0, 1.0 - cosTheta * cosTheta));

    return float3(sinTheta * cos(phi), sinTheta * sin(phi), cosTheta); // Light point backward (-Z)
}

float3 UniformSampleDisk(float u1, float u2)
{
    float r = sqrt(u1);
    float phi = TWO_PI * u2;

    return float3(r * cos(phi), r * sin(phi), 0); // Generate in XY plane as light point backward (-Z)
}

void SampleSphere(  float2 u,
                    float4x4 localToWorld,
                    float radius,
                    out float lightPdf,
                    out float3 P,
                    out float3 Ns)
{
    float u1 = u.x;
    float u2 = u.y;

    Ns = UniformSampleSphere(u1, u2);

    // Transform from unit sphere to world space
    P = radius * Ns + localToWorld[3].xyz;

    // pdf is inverse of area
    lightPdf = 1.0 / (FOUR_PI * radius * radius);
}

void SampleHemisphere(  float2 u,
                        float4x4 localToWorld,
                        float radius,
                        out float lightPdf,
                        out float3 P,
                        out float3 Ns)
{
    float u1 = u.x;
    float u2 = u.y;

    // Random point at hemisphere surface
    Ns = -UniformSampleHemisphere(u1, u2); // We want the y down hemisphere
    P = radius * Ns;

    // Transform to world space
    P = mul(float4(P, 1.0), localToWorld).xyz;
    Ns = mul(Ns, (float3x3)(localToWorld));

    // pdf is inverse of area
    lightPdf = 1.0 / (TWO_PI * radius * radius);
}

// Note: The cylinder has no end caps (i.e. no disk on the side)
void SampleCylinder(float2 u,
                    float4x4 localToWorld,
                    float radius,
                    float width,
                    out float lightPdf,
                    out float3 P,
                    out float3 Ns)
{
    float u1 = u.x;
    float u2 = u.y;

    // Random point at cylinder surface
    float t = (u1 - 0.5) * width;
    float theta = 2.0 * PI * u2;
    float cosTheta = cos(theta);
    float sinTheta = sin(theta);

    // Cylinder are align on the right axis
    P = float3(t, radius * cosTheta, radius * sinTheta);
    Ns = normalize(float3(0.0, cosTheta, sinTheta));

    // Transform to world space
    P = mul(float4(P, 1.0), localToWorld).xyz;
    Ns = mul(Ns, (float3x3)(localToWorld));

    // pdf is inverse of area
    lightPdf = 1.0 / (TWO_PI * radius * width);
}

void SampleRectangle(   float2 u,
                        float4x4 localToWorld,
                        float width,
                        float height,
                        out float	lightPdf,
                        out float3	P,
                        out float3	Ns)
{
    // Random point at rectangle surface
    P = float3((u.x - 0.5) * width, (u.y - 0.5) * height, 0);
    Ns = float3(0, 0, -1); // Light point backward (-Z)

    // Transform to world space
    P = mul(float4(P, 1.0), localToWorld).xyz;
    Ns = mul(Ns, (float3x3)(localToWorld));

    // pdf is inverse of area
    lightPdf = 1.0 / (width * height);
}

void SampleDisk(float2 u,
                float4x4 localToWorld,
                float radius,
                out float lightPdf,
                out float3 P,
                out float3 Ns)
{
    // Random point at disk surface
    P = UniformSampleDisk(u.x, u.y) * radius;
    Ns = float3(0.0, 0.0, -1.0); // Light point backward (-Z)

    // Transform to world space
    P = mul(float4(P, 1.0), localToWorld).xyz;
    Ns = mul(Ns, (float3x3)(localToWorld));

    // pdf is inverse of area
    lightPdf = 1.0 / (PI * radius * radius);
}

#endif // UNITY_SAMPLING_INCLUDED
