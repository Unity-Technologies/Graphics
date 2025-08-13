#ifndef _SAMPLING_SAMPLING_COMMON_HLSL_
#define _SAMPLING_SAMPLING_COMMON_HLSL_

#ifndef PI
#define PI 3.14159265358979323846f
#endif

#define FLOAT_ONE_MINUS_EPSILON 0.99999994

// Paper: Building an Orthonormal Basis, Revisited.
// Tom Duff, James Burgess, Per Christensen, Christophe Hery, Andrew Kensler, Max Liani, and Ryusuke Villemin (Pixar).
// https://graphics.pixar.com/library/OrthonormalB/paper.pdf
void OrthoBasisFromVector(float3 n, out float3 b1, out float3 b2)
{
    float sign = n.z >= 0.0f ? 1.0f : -1.0f;
    const float a = -1.0f / (sign + n.z);
    const float b = n.x * n.y * a;
    b1 = float3(1.0f + sign * n.x * n.x * a, sign * b, -sign * n.x);
    b2 = float3(b, sign + n.y * n.y * a, -n.y);
}

float3x3 OrthoBasisFromVector(float3 n)
{
    float3 t, b;
    OrthoBasisFromVector(n, t, b);
    return transpose(float3x3(t, b, n));
}

void SampleDiffuseBrdf(float2 u, float3 shadingNormal, out float3 wi)
{
    wi = float3(0, 0, 0);

    float a = sqrt(u.x);
    float b = 2.0 * PI * u.y;
    float3 localWi = float3(a * cos(b), a * sin(b), sqrt(1.0f - u.x));

    float3x3 TBN = OrthoBasisFromVector(shadingNormal);
    wi = mul(TBN, localWi);
}

bool SampleDiffuseBrdf(float2 u, float3 geometryNormal, float3 shadingNormal, float3 wo, out float3 wi, out float pdf)
{
    pdf = 0.0f;
    wi = 0.0f;
    bool valid = true;
    if (dot(geometryNormal, wo) <= 0.0f)
        valid = false;
    else
    {
        float a = sqrt(1.0f - u.x);
        float b = 2.0 * PI * u.y;
        float3 localWi = float3(a * cos(b), a * sin(b), sqrt(u.x));

        float3x3 TBN = OrthoBasisFromVector(shadingNormal);
        wi = mul(TBN, localWi);

        pdf = localWi.z / PI;
    }
    return valid;
}

float3 MapSquareToSphere(float2 unitSquareCoords)
{
    float theta = (2.0f * PI) * unitSquareCoords.x;
    float sinTheta, cosTheta;
    sincos(theta, sinTheta, cosTheta);

    float cosPhi = 1.0f - 2.0f * unitSquareCoords.y;
    float sinPhi = sqrt(max(0.0f, 1.0f - cosPhi * cosPhi));

    return float3(sinPhi * cosTheta, sinPhi * sinTheta, cosPhi);
}

// An optimized version of the area-preserving square-to-disk map presented in "A Low Distortion Map Between Disk and Square". Copied from from http://psgraphics.blogspot.com/2011/01/improved-code-for-concentric-map.html.
float2 MapSquareToDisk(float2 rnd)
{
    //Code flow makes sure that division by 0 and thus NaNs cannot happen.
    float phi;
    float r;
    float a = rnd.x * 2.0f - 1.0f;
    float b = rnd.y * 2.0f - 1.0f;

    if (a * a > b * b)
    {
        r = a;
        phi = (PI * 0.25f) * (b / a);
    }
    else
    {
        r = b;
        if (b == 0.0f)
            phi = PI * 0.5f;
        else
            phi = (PI * 0.5f) - (PI * 0.25f) * (a / b);
    }
    return float2(r * cos(phi), r * sin(phi));
}

float3 CosineSample(float2 u, float3 normal)
{
    float a = sqrt(u.x);
    float b = 2.0f * PI * u.y;
    float3 localDir = float3(a * cos(b), a * sin(b), sqrt(1.0f - u.x));
    float3x3 basis = OrthoBasisFromVector(normal);
    return mul(basis, localDir);
}

float3 UniformHemisphereSample(float2 u, float3 normal)
{
    float z = u[0];
    float r = sqrt(max(0.0f, 1.0f - z * z));
    float phi = 2.0f * PI * u[1];
    float3 localDir = float3(r * cos(phi), r * sin(phi), z);
    float3x3 basis = OrthoBasisFromVector(normal);
    return mul(basis, localDir);
}

float PowerHeuristic(float f, float b)
{
    float q = (f * f) + (b * b);
    return q > 0 ? (f * f) / q : 0;
}

float UintToFloat01(uint x)
{
    return min(x * 2.3283064365386963e-10, FLOAT_ONE_MINUS_EPSILON); // (1.f / (1ULL << 32));
}

int Log2Int(uint v)
{
    return firstbithigh(v);
}

int Log2IntUp(uint v)
{
    int n = Log2Int(v);
    return v > (1u << n) ? n + 1 : n;
}

#endif
