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
// Coordinate system conversion
//-----------------------------------------------------------------------------

// Transforms the unit vector from the spherical to the Cartesian (right-handed, Z up) coordinate.
float3 SphericalToCartesian(float phi, float cosTheta)
{
    float sinPhi, cosPhi;
    sincos(phi, sinPhi, cosPhi);

    float sinTheta = sqrt(saturate(1.0 - cosTheta * cosTheta));

    return float3(sinTheta * cosPhi, sinTheta * sinPhi, cosTheta);
}

// Converts Cartesian coordinates given in the right-handed coordinate system
// with Z pointing upwards (OpenGL style) to the coordinates in the left-handed
// coordinate system with Y pointing up and Z facing forward (DirectX style).
float3 TransformGLtoDX(float3 v)
{
    return v.xzy;
}

// Performs conversion from equiareal map coordinates to Cartesian (DirectX cubemap) ones.
float3 ConvertEquiarealToCubemap(float u, float v)
{
    float phi      = TWO_PI - TWO_PI * u;
    float cosTheta = 1.0 - 2.0 * v;

    return TransformGLtoDX(SphericalToCartesian(phi, cosTheta));
}

// Convert a texel position into normalized position [-1..1]x[-1..1]
float2 CubemapTexelToNVC(uint2 unPositionTXS, uint cubemapSize)
{
    return 2.0 * float2(unPositionTXS) / float(max(cubemapSize - 1, 1)) - 1.0;
}

// Map cubemap face to world vector basis
static const float3 CUBEMAP_FACE_BASIS_MAPPING[6][3] =
{
    //XPOS face
    {
        float3(0.0, 0.0, -1.0),
        float3(0.0, -1.0, 0.0),
        float3(1.0, 0.0, 0.0)
    },
    //XNEG face
    {
        float3(0.0, 0.0, 1.0),
        float3(0.0, -1.0, 0.0),
        float3(-1.0, 0.0, 0.0)
    },
    //YPOS face
    {
        float3(1.0, 0.0, 0.0),
        float3(0.0, 0.0, 1.0),
        float3(0.0, 1.0, 0.0)
    },
    //YNEG face
    {
        float3(1.0, 0.0, 0.0),
        float3(0.0, 0.0, -1.0),
        float3(0.0, -1.0, 0.0)
    },
    //ZPOS face
    {
        float3(1.0, 0.0, 0.0),
        float3(0.0, -1.0, 0.0),
        float3(0.0, 0.0, 1.0)
    },
    //ZNEG face
    {
        float3(-1.0, 0.0, 0.0),
        float3(0.0, -1.0, 0.0),
        float3(0.0, 0.0, -1.0)
    }
};

// Convert a normalized cubemap face position into a direction
float3 CubemapTexelToDirection(float2 positionNVC, uint faceId)
{
    float3 dir = CUBEMAP_FACE_BASIS_MAPPING[faceId][0] * positionNVC.x
               + CUBEMAP_FACE_BASIS_MAPPING[faceId][1] * positionNVC.y
               + CUBEMAP_FACE_BASIS_MAPPING[faceId][2];

    return normalize(dir);
}

//-----------------------------------------------------------------------------
// Sampling function
// Reference : http://www.cs.virginia.edu/~jdl/bib/globillum/mis/shirley96.pdf + PBRT
// Caution: Our light point backward (-Z), these sampling function follow this convention
//-----------------------------------------------------------------------------

// Performs uniform sampling of the unit disk.
// Ref: PBRT v3, p. 777.
float2 SampleDiskUniform(float u1, float u2)
{
    float r   = sqrt(u1);
    float phi = TWO_PI * u2;

    float sinPhi, cosPhi;
    sincos(phi, sinPhi, cosPhi);

    return r * float2(cosPhi, sinPhi);
}

// Performs cosine-weighted sampling of the hemisphere.
// Ref: PBRT v3, p. 780.
float3 SampleHemisphereCosine(float u1, float u2)
{
    float3 localL;

    // Since we don't really care about the area distortion,
    // we substitute uniform disk sampling for the concentric one.
    localL.xy = SampleDiskUniform(u1, u2);

    // Project the point from the disk onto the hemisphere.
    localL.z = sqrt(1.0 - u1);

    return localL;
}

float3 SampleHemisphereUniform(float u1, float u2)
{
    float phi      = TWO_PI * u2;
    float cosTheta = 1.0 - u1;

    return SphericalToCartesian(phi, cosTheta);
}

float3 SampleSphereUniform(float u1, float u2)
{
    float phi      = TWO_PI * u2;
    float cosTheta = 1.0 - 2.0 * u1;

    return SphericalToCartesian(phi, cosTheta);
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

    Ns = SampleSphereUniform(u1, u2);

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
    Ns = -SampleHemisphereUniform(u1, u2); // We want the y down hemisphere
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
                        out float   lightPdf,
                        out float3  P,
                        out float3  Ns)
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
    P  = float3(radius * SampleDiskUniform(u.x, u.y), 0);
    Ns = float3(0.0, 0.0, -1.0); // Light point backward (-Z)

    // Transform to world space
    P = mul(float4(P, 1.0), localToWorld).xyz;
    Ns = mul(Ns, (float3x3)(localToWorld));

    // pdf is inverse of area
    lightPdf = 1.0 / (PI * radius * radius);
}

#endif // UNITY_SAMPLING_INCLUDED
