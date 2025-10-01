#ifndef SURFACE_CACHE_COMMON
#define SURFACE_CACHE_COMMON

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

static const uint patchCapacity = 65536; // Must match C# side.
static const uint cascadeMax = 8; // Must match C# side.
#if UNITY_REVERSED_Z
static const float invalidNdcDepth = 0.0f;
#else
static const float invalidNdcDepth = 1.0f;
#endif

static const uint lowResScreenScaling = 4; // must match cpu side.

float LoadNdcDepth(Texture2D<float> depthBuffer, uint2 pos)
{
    const float depthBufferDepth = depthBuffer[pos];
    #if UNITY_REVERSED_Z
    return depthBufferDepth;
    #else
    if(depthBufferDepth == 1.0f)
        return invalidNdcDepth;
    else
        return depthBufferDepth * 2.0f - 1.0f;
    #endif
}

float2 PixelPosToUvPos(uint2 pixelPos, float2 reciprocalScreenSize)
{
    return (float2(pixelPos) + 0.5f) * reciprocalScreenSize;
}

namespace SphericalHarmonics
{
    static const float y0 = sqrt(1.0f / PI) * 0.5f;
    static const float y1Constant = sqrt(3.0f / PI) * 0.5f;
    static const float y20Constant = sqrt(15.0f / PI) * 0.5f;
    static const float y21Constant = y20Constant;
    static const float y22Constant = sqrt(5.0f / PI) * 0.25f;
    static const float y23Constant = y20Constant;
    static const float y24Constant = sqrt(15.0f / PI) * 0.25f;

    struct RGBL1
    {
        float3 l0;
        float3 l1s[3];
    };

    float3 Eval(in RGBL1 f, float3 direction)
    {
        return f.l0 * y0 + y1Constant * (f.l1s[0] * direction.x + f.l1s[1] * direction.z + f.l1s[2] * direction.y);
    }

    RGBL1 MulPure(RGBL1 f, float multiplier)
    {
        RGBL1 result;
        result.l0 = f.l0 * multiplier;
        result.l1s[0] = f.l1s[0] * multiplier;
        result.l1s[1] = f.l1s[1] * multiplier;
        result.l1s[2] = f.l1s[2] * multiplier;
        return result;
    }

    RGBL1 AddPure(RGBL1 a, RGBL1 b)
    {
        RGBL1 result;
        result.l0 = a.l0 + b.l0;
        result.l1s[0] = a.l1s[0] + b.l1s[0];
        result.l1s[1] = a.l1s[1] + b.l1s[1];
        result.l1s[2] = a.l1s[2] + b.l1s[2];
        return result;
    }

    // Multiply-add: a = a + b * c.
    void AddMut(inout RGBL1 a, RGBL1 b)
    {
        a.l0 += b.l0;
        a.l1s[0] += b.l1s[0];
        a.l1s[1] += b.l1s[1];
        a.l1s[2] += b.l1s[2];
    }

    // Multiply-add: a = a + b * c.
    void MulAddMut(inout RGBL1 a, float b, RGBL1 c)
    {
        a.l0 += b * c.l0;
        a.l1s[0] += b * c.l1s[0];
        a.l1s[1] += b * c.l1s[1];
        a.l1s[2] += b * c.l1s[2];
    }

    void MulMut(inout RGBL1 f, float multiplier)
    {
        f.l0 *= multiplier;
        f.l1s[0] *= multiplier;
        f.l1s[1] *= multiplier;
        f.l1s[2] *= multiplier;
    }

    RGBL1 Lerp(in RGBL1 a, in RGBL1 b, float3 s)
    {
        RGBL1 output;
        output.l0 = lerp(a.l0, b.l0, s);
        output.l1s[0] = lerp(a.l1s[0], b.l1s[0], s);
        output.l1s[1] = lerp(a.l1s[1], b.l1s[1], s);
        output.l1s[2] = lerp(a.l1s[2], b.l1s[2], s);
        return output;
    }

    // Convolve with cos(theta). https://cseweb.ucsd.edu/~ravir/papers/envmap/envmap.pdf
    void CosineConvolve(inout RGBL1 f)
    {
        const float aHat0 = PI;
        const float aHat1 = (2.0f * PI) / 3.0f;
        f.l0 *= aHat0;
        f.l1s[0] *= aHat1;
        f.l1s[1] *= aHat1;
        f.l1s[2] *= aHat1;
    }

    struct ScalarL2
    {
        float l0;
        float l1s[3];
        float l2s[5];
    };

    float Eval(in ScalarL2 f, float3 direction)
    {
        return f.l0 * y0 +
            f.l1s[0] * y1Constant * direction.x +
            f.l1s[1] * y1Constant * direction.z +
            f.l1s[2] * y1Constant * direction.y +
            f.l2s[0] * y20Constant * direction.x * direction.y +
            f.l2s[1] * y21Constant * direction.y * direction.z +
            f.l2s[2] * y22Constant * (3.0f * direction.z * direction.z - 1.0f) +
            f.l2s[3] * y23Constant * direction.x * direction.z +
            f.l2s[4] * y24Constant * (direction.x * direction.x - direction.y * direction.y);
    }

    ScalarL2 Lerp(ScalarL2 a, ScalarL2 b, float s)
    {
        SphericalHarmonics::ScalarL2 output;
        output.l0 = lerp(a.l0, b.l0, s);
        output.l1s[0] = lerp(a.l1s[0], b.l1s[0], s);
        output.l1s[1] = lerp(a.l1s[1], b.l1s[1], s);
        output.l1s[2] = lerp(a.l1s[2], b.l1s[2], s);
        output.l2s[0] = lerp(a.l2s[0], b.l2s[0], s);
        output.l2s[1] = lerp(a.l2s[1], b.l2s[1], s);
        output.l2s[2] = lerp(a.l2s[2], b.l2s[2], s);
        output.l2s[3] = lerp(a.l2s[3], b.l2s[3], s);
        output.l2s[4] = lerp(a.l2s[4], b.l2s[4], s);
        return output;
    }
}

#endif
