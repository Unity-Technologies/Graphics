#ifndef UNITY_SAMPLING_INCLUDED
#define UNITY_SAMPLING_INCLUDED

//-----------------------------------------------------------------------------
// Sample generator
//-----------------------------------------------------------------------------

// Ref: http://holger.dammertz.org/stuff/notes_HammersleyOnHemisphere.html
uint ReverseBits32(uint bits)
{
#if 0 // Shader model 5
    return reversebits(bits);
#else
    bits = (bits << 16) | (bits >> 16);
    bits = ((bits & 0x00ff00ff) << 8) | ((bits & 0xff00ff00) >> 8);
    bits = ((bits & 0x0f0f0f0f) << 4) | ((bits & 0xf0f0f0f0) >> 4);
    bits = ((bits & 0x33333333) << 2) | ((bits & 0xcccccccc) >> 2);
    bits = ((bits & 0x55555555) << 1) | ((bits & 0xaaaaaaaa) >> 1);
    return bits;
#endif
}

float RadicalInverse_VdC(uint bits)
{
    return float(ReverseBits32(bits)) * 2.3283064365386963e-10; // 0x100000000
}

float2 Hammersley2d(uint i, uint maxSampleCount)
{
    return float2(float(i) / float(maxSampleCount), RadicalInverse_VdC(i));
}

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
//-----------------------------------------------------------------------------

// Reference : Monte Carlo techniques for direct lighting calculations http://www.cs.virginia.edu/~jdl/bib/globillum/mis/shirley96.pdf + PBRT
float3 UniformSampleSphere(float u1, float u2)
{
    float phi = TWO_PI * u2;
    float cosTheta = 1.0 - 2.0 * u1;
    float sinTheta = sqrt(max(0.0, 1.0 - cosTheta * cosTheta));

    return float3(sinTheta * cos(phi), sinTheta * sin(phi), cosTheta); // our light point backward (-Z)
}

float3 UniformSampleHemisphere(float u1, float u2)
{
    float phi = TWO_PI * u2;
    float cosTheta = u1;
    float sinTheta = sqrt(max(0.0, 1.0 - cosTheta * cosTheta));

    return float3(sinTheta * cos(phi), sinTheta * sin(phi), cosTheta); // our light point backward (-Z)
}

float3 UniformSampleDisk(float u1, float u2)
{
    float r = sqrt(u1);
    float phi = TWO_PI * u2;

    return float3(r * cos(phi), r * sin(phi), 0); // generate in the XY plane as light point backward (-Z)
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

    // Random point at light surface
    Ns = uniformSampleSphere(u1, u2);
    // Transform point on unit sphere to world space 
    P = radius * Ns + localToWorld[3].xyz;
    // pdf is just the inverse of the area
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

    // Random point at light surface
    Ns = -uniformSampleHemisphere(u1, u2); // We want the y down hemisphere
    P = radius * Ns;
    // Transform to world space
    P = mul(float4(P, 1.0), localToWorld).xyz;
    Ns = mul(Ns, convertFloat3x3(localToWorld));
    // pdf is just the inverse of the area
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

    // Random point at light surface
    float t = (u1 - 0.5) * width;
    float theta = 2.0 * PI * u2;
    float cosTheta = cos(theta);
    float sinTheta = sin(theta);
    P = float3(t, radius * cosTheta, radius * sinTheta);  // Cylinder are align on the left axis in Frosbite	
    Ns = normalize(float3(0.0, cosTheta, sinTheta));
    // Transform to world space
    P = mul(float4(P, 1.0), localToWorld).xyz;
    Ns = mul(Ns, convertFloat3x3(localToWorld));
    // pdf is just the inverse of the area
    lightPdf = 1.0 / (TWO_PI * radius * width);
}

void SampleCapsule( float2 u,
                    float4x4 localToWorld,
                    float radius,
                    float width,
                    int passIt,
                    out float lightPdf,
                    out float3 P,
                    out float3 Ns)
{
    // Capsules are sampled in two times:
    //  - Pass 0: Cylinder
    //  - Pass 1: Hemisphere caps
    if (passIt == 0)
    {
        sampleCylinder(u, localToWorld, radius, width, lightPdf, P, Ns);
    }
    else
    {
        float u1 = u.x;
        float u2 = u.y;

        // Random point at light surface
        Ns = uniformSampleSphere(u1, u2);
        P = radius * Ns;

        // Split the sphere into two hemisphere and shift each hemisphere on one side of the cylinder
        P.x += (Ns.x > 0.0 ? 1.0 : -1.0) * 0.5 * width;

        // Transform to world space
        P = mul(float4(P, 1.0), localToWorld).xyz;
        Ns = mul(Ns, convertFloat3x3(localToWorld));
        // pdf is just the inverse of the area
        lightPdf = 1.0 / (FOUR_PI * radius * radius);
    }
}

void SampleRectangle(   float2 u,
                        float4x4 localToWorld,
                        float width,
                        float height,
                        out float	lightPdf,
                        out float3	P,
                        out float3	Ns)
{
    // Random point at light surface
    P = float3((u.x - 0.5) * width, (u.y - 0.5) * height, 0);
    Ns = float3(0, 0, -1); // By default our rectangle light point backward

    // Transform to world space
    P = mul(float4(P, 1.0), localToWorld).xyz;
    Ns = mul(Ns, convertFloat3x3(localToWorld));
    // pdf is just the inverse of the area
    lightPdf = 1.0 / (width * height);
}

void SampleDisk(float2 u,
                float4x4 localToWorld,
                float radius,
                out float lightPdf,
                out float3 P,
                out float3 Ns)
{
    // Random point at light surface
    P = uniformSampleDisk(u.x, u.y) * radius;
    Ns = float3(0.0, 0.0, -1.0);

    // Transform to world space
    P = mul(float4(P, 1.0), localToWorld).xyz;
    Ns = mul(Ns, convertFloat3x3(localToWorld));

    // pdf is just the inverse of the area
    lightPdf = 1.0 / (PI * radius * radius);
}

#endif // UNITY_SAMPLING_INCLUDED
