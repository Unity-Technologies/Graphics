#ifndef UNITY_SAMPLING_INCLUDED
#define UNITY_SAMPLING_INCLUDED

//-----------------------------------------------------------------------------
// Sample generator
//-----------------------------------------------------------------------------

#include "Hammersley2D.hlsl"

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

static const float2 k_Fibonacci2dSeq55[] = {
    float2(0.00000000, 0.00000000),
    float2(0.61818182, 0.01818182),
    float2(0.23636365, 0.03636364),
    float2(0.85454547, 0.05454545),
    float2(0.47272730, 0.07272727),
    float2(0.09090900, 0.09090909),
    float2(0.70909095, 0.10909091),
    float2(0.32727289, 0.12727273),
    float2(0.94545460, 0.14545454),
    float2(0.56363630, 0.16363636),
    float2(0.18181801, 0.18181819),
    float2(0.80000019, 0.20000000),
    float2(0.41818190, 0.21818182),
    float2(0.03636360, 0.23636363),
    float2(0.65454578, 0.25454545),
    float2(0.27272701, 0.27272728),
    float2(0.89090919, 0.29090908),
    float2(0.50909138, 0.30909091),
    float2(0.12727261, 0.32727271),
    float2(0.74545479, 0.34545454),
    float2(0.36363602, 0.36363637),
    float2(0.98181820, 0.38181818),
    float2(0.60000038, 0.40000001),
    float2(0.21818161, 0.41818181),
    float2(0.83636379, 0.43636364),
    float2(0.45454597, 0.45454547),
    float2(0.07272720, 0.47272727),
    float2(0.69090843, 0.49090910),
    float2(0.30909157, 0.50909090),
    float2(0.92727280, 0.52727270),
    float2(0.54545403, 0.54545456),
    float2(0.16363716, 0.56363636),
    float2(0.78181839, 0.58181816),
    float2(0.39999962, 0.60000002),
    float2(0.01818275, 0.61818182),
    float2(0.63636398, 0.63636363),
    float2(0.25454521, 0.65454543),
    float2(0.87272835, 0.67272729),
    float2(0.49090958, 0.69090909),
    float2(0.10909081, 0.70909089),
    float2(0.72727203, 0.72727275),
    float2(0.34545517, 0.74545455),
    float2(0.96363640, 0.76363635),
    float2(0.58181763, 0.78181821),
    float2(0.20000076, 0.80000001),
    float2(0.81818199, 0.81818181),
    float2(0.43636322, 0.83636361),
    float2(0.05454636, 0.85454547),
    float2(0.67272758, 0.87272727),
    float2(0.29090881, 0.89090908),
    float2(0.90909195, 0.90909094),
    float2(0.52727318, 0.92727274),
    float2(0.14545441, 0.94545454),
    float2(0.76363754, 0.96363634),
    float2(0.38181686, 0.98181820)
};

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
