#ifndef UNITY_SPHERICAL_HARMONICS_INCLUDED
#define UNITY_SPHERICAL_HARMONICS_INCLUDED

#ifdef UNITY_COLORSPACE_GAMMA
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#endif

// SH Basis coefs
#define kSHBasis0  0.28209479177387814347f // {0, 0} : 1/2 * sqrt(1/Pi)
#define kSHBasis1  0.48860251190291992159f // {1, 0} : 1/2 * sqrt(3/Pi)
#define kSHBasis2  1.09254843059207907054f // {2,-2} : 1/2 * sqrt(15/Pi)
#define kSHBasis3  0.31539156525252000603f // {2, 0} : 1/4 * sqrt(5/Pi)
#define kSHBasis4  0.54627421529603953527f // {2, 2} : 1/4 * sqrt(15/Pi)

static const float kSHBasisCoef[] = { kSHBasis0, -kSHBasis1, kSHBasis1, -kSHBasis1, kSHBasis2, -kSHBasis2, kSHBasis3, -kSHBasis2, kSHBasis4 };

// Clamped cosine convolution coefs (pre-divided by PI)
// See https://seblagarde.wordpress.com/2012/01/08/pi-or-not-to-pi-in-game-lighting-equation/
#define kClampedCosine0 (1.0f)
#define kClampedCosine1 (2.0f / 3.0f)
#define kClampedCosine2 (1.0f / 4.0f)

static const float kClampedCosineCoefs[] = { kClampedCosine0, kClampedCosine1, kClampedCosine1, kClampedCosine1, kClampedCosine2, kClampedCosine2, kClampedCosine2, kClampedCosine2, kClampedCosine2 };

// Ref: "Efficient Evaluation of Irradiance Environment Maps" from ShaderX 2
real3 SHEvalLinearL0L1(real3 N, real4 shAr, real4 shAg, real4 shAb)
{
    real4 vA = real4(N, 1.0);

    real3 x1;
    // Linear (L1) + constant (L0) polynomial terms
    x1.r = dot(shAr, vA);
    x1.g = dot(shAg, vA);
    x1.b = dot(shAb, vA);

    return x1;
}

real3 SHEvalLinearL1(real3 N, real3 shAr, real3 shAg, real3 shAb)
{
    real3 x1;
    x1.r = dot(shAr, N);
    x1.g = dot(shAg, N);
    x1.b = dot(shAb, N);

    return x1;
}

real3 SHEvalLinearL2(real3 N, real4 shBr, real4 shBg, real4 shBb, real4 shC)
{
    real3 x2;
    // 4 of the quadratic (L2) polynomials
    real4 vB = N.xyzz * N.yzzx;
    x2.r = dot(shBr, vB);
    x2.g = dot(shBg, vB);
    x2.b = dot(shBb, vB);

    // Final (5th) quadratic (L2) polynomial
    real vC = N.x * N.x - N.y * N.y;
    real3 x3 = shC.rgb * vC;

    return x2 + x3;
}

#if !HALF_IS_FLOAT
half3 SampleSH9(half4 SHCoefficients[7], half3 N)
{
    half4 shAr = SHCoefficients[0];
    half4 shAg = SHCoefficients[1];
    half4 shAb = SHCoefficients[2];
    half4 shBr = SHCoefficients[3];
    half4 shBg = SHCoefficients[4];
    half4 shBb = SHCoefficients[5];
    half4 shCr = SHCoefficients[6];

    // Linear + constant polynomial terms
    half3 res = SHEvalLinearL0L1(N, shAr, shAg, shAb);

    // Quadratic polynomials
    res += SHEvalLinearL2(N, shBr, shBg, shBb, shCr);

#ifdef UNITY_COLORSPACE_GAMMA
    res = LinearToSRGB(res);
#endif

    return res;
}

half3 SampleSH4_L1(half4 SHCoefficients[3], half3 N)
{
    half4 shAr = SHCoefficients[0];
    half4 shAg = SHCoefficients[1];
    half4 shAb = SHCoefficients[2];

    // Linear + constant polynomial terms
    half3 res = SHEvalLinearL1(N, shAr.xyz, shAg.xyz, shAb.xyz);

    #ifdef UNITY_COLORSPACE_GAMMA
    res = LinearToSRGB(res);
    #endif

    return res;
}
#endif

float3 SampleSH9(float4 SHCoefficients[7], float3 N)
{
    float4 shAr = SHCoefficients[0];
    float4 shAg = SHCoefficients[1];
    float4 shAb = SHCoefficients[2];
    float4 shBr = SHCoefficients[3];
    float4 shBg = SHCoefficients[4];
    float4 shBb = SHCoefficients[5];
    float4 shCr = SHCoefficients[6];

    // Linear + constant polynomial terms
    float3 res = SHEvalLinearL0L1(N, shAr, shAg, shAb);

    // Quadratic polynomials
    res += SHEvalLinearL2(N, shBr, shBg, shBb, shCr);

#ifdef UNITY_COLORSPACE_GAMMA
    res = LinearToSRGB(res);
#endif

    return res;
}

float3 SampleSH9(StructuredBuffer<float4> data, float3 N)
{
    real4 SHCoefficients[7];
    SHCoefficients[0] = data[0];
    SHCoefficients[1] = data[1];
    SHCoefficients[2] = data[2];
    SHCoefficients[3] = data[3];
    SHCoefficients[4] = data[4];
    SHCoefficients[5] = data[5];
    SHCoefficients[6] = data[6];

    return SampleSH9(SHCoefficients, N);
}

float3 SampleSH4_L1(float4 SHCoefficients[3], float3 N)
{
    float4 shAr = SHCoefficients[0];
    float4 shAg = SHCoefficients[1];
    float4 shAb = SHCoefficients[2];

    // Linear + constant polynomial terms
    float3 res = SHEvalLinearL1(N, (float3)shAr, (float3)shAg, (float3)shAb);

    #ifdef UNITY_COLORSPACE_GAMMA
    res = LinearToSRGB(res);
    #endif

    return res;
}

void GetCornetteShanksPhaseFunction(out float3 zh, float anisotropy)
{
    float g = anisotropy;

    zh.x = 0.282095f;
    zh.y = 0.293162f * g * (4.0f + (g * g)) / (2.0f + (g * g));
    zh.z = (0.126157f + 1.44179f * (g * g) + 0.324403f * (g * g) * (g * g)) / (2.0f + (g * g));
}

void ConvolveZonal(inout float sh[27], float3 zh)
{
    for (int l = 0; l <= 2; l++)
    {
        float n = sqrt((4.0f * PI) / (2 * l + 1));
        float k = zh[l];
        float p = n * k;

        for (int m = -l; m <= l; m++)
        {
            int i = l * (l + 1) + m;

            for (int c = 0; c < 3; c++)
            {
                sh[c * 9 + i] = sh[c * 9 + i] * p;
            }
        }
    }
}

// Packs coefficients so that we can use Peter-Pike Sloan's shader code.
// The function does not perform premultiplication with coefficients of SH basis functions, caller need to do it
// See SetSHEMapConstants() in "Stupid Spherical Harmonics Tricks".
// Constant + linear
void PackSH(RWStructuredBuffer<float4> buffer, float sh[27])
{
    int c = 0;
    for (c = 0; c < 3; c++)

    {
        buffer[c] = float4(sh[c * 9 + 3], sh[c * 9 + 1], sh[c * 9 + 2], sh[c * 9 + 0] - sh[c * 9 + 6]);
    }

    // Quadratic (4/5)
    for (c = 0; c < 3; c++)
    {
        buffer[3 + c] = float4(sh[c * 9 + 4], sh[c * 9 + 5], sh[c * 9 + 6] * 3.0f, sh[c * 9 + 7]);
    }

    // Quadratic (5)
    buffer[6] = float4(sh[0 * 9 + 8], sh[1 * 9 + 8], sh[2 * 9 + 8], 1.0f);
}

#endif // UNITY_SPHERICAL_HARMONICS_INCLUDED
