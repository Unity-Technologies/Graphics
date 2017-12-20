#ifndef UNITY_SAMPLING_INCLUDED
#define UNITY_SAMPLING_INCLUDED

//-----------------------------------------------------------------------------
// Sample generator
//-----------------------------------------------------------------------------

#include "Fibonacci.hlsl"
#include "Hammersley.hlsl"

REAL Hash(uint s)
{
    s = s ^ 2747636419u;
    s = s * 2654435769u;
    s = s ^ (s >> 16);
    s = s * 2654435769u;
    s = s ^ (s >> 16);
    s = s * 2654435769u;
    return REAL(s) / 4294967295.0;
}

REAL2 InitRandom(REAL2 input)
{
    REAL2 r;
    r.x = Hash(uint(input.x * 4294967295.0));
    r.y = Hash(uint(input.y * 4294967295.0));

    return r;
}

//-----------------------------------------------------------------------------
// Coordinate system conversion
//-----------------------------------------------------------------------------

// Transforms the unit vector from the spherical to the Cartesian (right-handed, Z up) coordinate.
REAL3 SphericalToCartesian(REAL phi, REAL cosTheta)
{
    REAL sinPhi, cosPhi;
    sincos(phi, sinPhi, cosPhi);

    REAL sinTheta = sqrt(saturate(1.0 - cosTheta * cosTheta));

    return REAL3(sinTheta * cosPhi, sinTheta * sinPhi, cosTheta);
}

// Converts Cartesian coordinates given in the right-handed coordinate system
// with Z pointing upwards (OpenGL style) to the coordinates in the left-handed
// coordinate system with Y pointing up and Z facing forward (DirectX style).
REAL3 TransformGLtoDX(REAL3 v)
{
    return v.xzy;
}

// Performs conversion from equiareal map coordinates to Cartesian (DirectX cubemap) ones.
REAL3 ConvertEquiarealToCubemap(REAL u, REAL v)
{
    REAL phi      = TWO_PI - TWO_PI * u;
    REAL cosTheta = 1.0 - 2.0 * v;

    return TransformGLtoDX(SphericalToCartesian(phi, cosTheta));
}

// Convert a texel position into normalized position [-1..1]x[-1..1]
REAL2 CubemapTexelToNVC(uint2 unPositionTXS, uint cubemapSize)
{
    return 2.0 * REAL2(unPositionTXS) / REAL(max(cubemapSize - 1, 1)) - 1.0;
}

// Map cubemap face to world vector basis
static const REAL3 CUBEMAP_FACE_BASIS_MAPPING[6][3] =
{
    //XPOS face
    {
        REAL3(0.0, 0.0, -1.0),
        REAL3(0.0, -1.0, 0.0),
        REAL3(1.0, 0.0, 0.0)
    },
    //XNEG face
    {
        REAL3(0.0, 0.0, 1.0),
        REAL3(0.0, -1.0, 0.0),
        REAL3(-1.0, 0.0, 0.0)
    },
    //YPOS face
    {
        REAL3(1.0, 0.0, 0.0),
        REAL3(0.0, 0.0, 1.0),
        REAL3(0.0, 1.0, 0.0)
    },
    //YNEG face
    {
        REAL3(1.0, 0.0, 0.0),
        REAL3(0.0, 0.0, -1.0),
        REAL3(0.0, -1.0, 0.0)
    },
    //ZPOS face
    {
        REAL3(1.0, 0.0, 0.0),
        REAL3(0.0, -1.0, 0.0),
        REAL3(0.0, 0.0, 1.0)
    },
    //ZNEG face
    {
        REAL3(-1.0, 0.0, 0.0),
        REAL3(0.0, -1.0, 0.0),
        REAL3(0.0, 0.0, -1.0)
    }
};

// Convert a normalized cubemap face position into a direction
REAL3 CubemapTexelToDirection(REAL2 positionNVC, uint faceId)
{
    REAL3 dir = CUBEMAP_FACE_BASIS_MAPPING[faceId][0] * positionNVC.x
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
REAL2 SampleDiskUniform(REAL u1, REAL u2)
{
    REAL r   = sqrt(u1);
    REAL phi = TWO_PI * u2;

    REAL sinPhi, cosPhi;
    sincos(phi, sinPhi, cosPhi);

    return r * REAL2(cosPhi, sinPhi);
}

// Performs cosine-weighted sampling of the hemisphere.
// Ref: PBRT v3, p. 780.
REAL3 SampleHemisphereCosine(REAL u1, REAL u2)
{
    REAL3 localL;

    // Since we don't really care about the area distortion,
    // we substitute uniform disk sampling for the concentric one.
    localL.xy = SampleDiskUniform(u1, u2);

    // Project the point from the disk onto the hemisphere.
    localL.z = sqrt(1.0 - u1);

    return localL;
}

REAL3 SampleHemisphereUniform(REAL u1, REAL u2)
{
    REAL phi      = TWO_PI * u2;
    REAL cosTheta = 1.0 - u1;

    return SphericalToCartesian(phi, cosTheta);
}

REAL3 SampleSphereUniform(REAL u1, REAL u2)
{
    REAL phi      = TWO_PI * u2;
    REAL cosTheta = 1.0 - 2.0 * u1;

    return SphericalToCartesian(phi, cosTheta);
}

void SampleSphere(  REAL2 u,
                    REAL4x4 localToWorld,
                    REAL radius,
                    out REAL lightPdf,
                    out REAL3 P,
                    out REAL3 Ns)
{
    REAL u1 = u.x;
    REAL u2 = u.y;

    Ns = SampleSphereUniform(u1, u2);

    // Transform from unit sphere to world space
    P = radius * Ns + localToWorld[3].xyz;

    // pdf is inverse of area
    lightPdf = 1.0 / (FOUR_PI * radius * radius);
}

void SampleHemisphere(  REAL2 u,
                        REAL4x4 localToWorld,
                        REAL radius,
                        out REAL lightPdf,
                        out REAL3 P,
                        out REAL3 Ns)
{
    REAL u1 = u.x;
    REAL u2 = u.y;

    // Random point at hemisphere surface
    Ns = -SampleHemisphereUniform(u1, u2); // We want the y down hemisphere
    P = radius * Ns;

    // Transform to world space
    P = mul(REAL4(P, 1.0), localToWorld).xyz;
    Ns = mul(Ns, (REAL3x3)(localToWorld));

    // pdf is inverse of area
    lightPdf = 1.0 / (TWO_PI * radius * radius);
}

// Note: The cylinder has no end caps (i.e. no disk on the side)
void SampleCylinder(REAL2 u,
                    REAL4x4 localToWorld,
                    REAL radius,
                    REAL width,
                    out REAL lightPdf,
                    out REAL3 P,
                    out REAL3 Ns)
{
    REAL u1 = u.x;
    REAL u2 = u.y;

    // Random point at cylinder surface
    REAL t = (u1 - 0.5) * width;
    REAL theta = 2.0 * PI * u2;
    REAL cosTheta = cos(theta);
    REAL sinTheta = sin(theta);

    // Cylinder are align on the right axis
    P = REAL3(t, radius * cosTheta, radius * sinTheta);
    Ns = normalize(REAL3(0.0, cosTheta, sinTheta));

    // Transform to world space
    P = mul(REAL4(P, 1.0), localToWorld).xyz;
    Ns = mul(Ns, (REAL3x3)(localToWorld));

    // pdf is inverse of area
    lightPdf = 1.0 / (TWO_PI * radius * width);
}

void SampleRectangle(   REAL2 u,
                        REAL4x4 localToWorld,
                        REAL width,
                        REAL height,
                        out REAL   lightPdf,
                        out REAL3  P,
                        out REAL3  Ns)
{
    // Random point at rectangle surface
    P = REAL3((u.x - 0.5) * width, (u.y - 0.5) * height, 0);
    Ns = REAL3(0, 0, -1); // Light point backward (-Z)

    // Transform to world space
    P = mul(REAL4(P, 1.0), localToWorld).xyz;
    Ns = mul(Ns, (REAL3x3)(localToWorld));

    // pdf is inverse of area
    lightPdf = 1.0 / (width * height);
}

void SampleDisk(REAL2 u,
                REAL4x4 localToWorld,
                REAL radius,
                out REAL lightPdf,
                out REAL3 P,
                out REAL3 Ns)
{
    // Random point at disk surface
    P  = REAL3(radius * SampleDiskUniform(u.x, u.y), 0);
    Ns = REAL3(0.0, 0.0, -1.0); // Light point backward (-Z)

    // Transform to world space
    P = mul(REAL4(P, 1.0), localToWorld).xyz;
    Ns = mul(Ns, (REAL3x3)(localToWorld));

    // pdf is inverse of area
    lightPdf = 1.0 / (PI * radius * radius);
}

#endif // UNITY_SAMPLING_INCLUDED
