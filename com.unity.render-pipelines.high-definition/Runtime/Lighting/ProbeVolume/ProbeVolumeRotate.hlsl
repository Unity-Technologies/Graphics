#ifndef PROBE_VOLUME_ROTATE_H
#define PROBE_VOLUME_ROTATE_H

// Sources (derivation in shadertoy):
// https://www.shadertoy.com/view/NlsGWB
// http://filmicworlds.com/blog/simple-and-fast-spherical-harmonic-rotation/
// https://zvxryb.github.io/blog/2015/09/03/sh-lighting-part2/

void SphericalHarmonicsRotateBandL1(float3x3 M, inout float3 x[3])
{
    float3x3 SH = float3x3(x[2], x[0], x[1]);

    x[0] = mul(float3(M[0][1], M[1][1], M[2][1]), SH);
    x[1] = mul(float3(M[0][2], M[1][2], M[2][2]), SH);
    x[2] = mul(float3(M[0][0], M[1][0], M[2][0]), SH);
}

void SphericalHarmonicsRotateBandL2(float3x3 M, inout float3 x[5])
{
    // Decomposed + factored version of 5x5 matrix multiply of invA * sh from source.
    float3 sh0 = x[1] * 0.5 + (x[3] * -0.5 + x[4] * 2.0); // 2x MADD
    float3 sh1 = x[0] * 0.5 + 3.0 * x[2] - x[3] * 0.5 + x[4];
    float3 sh2 = x[0];
    float3 sh3 = x[3];
    float3 sh4 = x[1];

    const float kInv = sqrt(2.0);
    const float k3 = 0.25;
    const float k4 = -1.0 / 6.0;
    
    // Decomposed + factored version of 5x5 matrix multiply of 5 normals projected to 5 SH2 bands.
    // Column 0
    {
        float3 rn0 = float3(M[0][0], M[0][1], M[0][2]) * kInv; // (float3(1, 0, 0) * M) / k;
        x[0] = (rn0.x * rn0.y) * sh0;
        x[1] = (rn0.y * rn0.z) * sh0;
        x[2] = (rn0.z * rn0.z * k3 + k4) * sh0;
        x[3] = (rn0.x * rn0.z) * sh0;
        x[4] = (rn0.x * rn0.x - rn0.y * rn0.y) * sh0;
    }

    // Column 1
    {
        float3 rn1 = float3(M[2][0], M[2][1], M[2][2]) * kInv; // (float3(0, 0, 1) * M) / k;
        x[0] += (rn1.x * rn1.y) * sh1;
        x[1] += (rn1.y * rn1.z) * sh1;
        x[2] += (rn1.z * rn1.z * k3 + k4) * sh1;
        x[3] += (rn1.x * rn1.z) * sh1;
        x[4] += (rn1.x * rn1.x - rn1.y * rn1.y) * sh1;
    }

    // Column 2
    {
        float3 rn2 = float3(M[0][0] + M[1][0], M[0][1] + M[1][1], M[0][2] + M[1][2]); // (float3(k, k, 0) * M) / k;
        x[0] += (rn2.x * rn2.y) * sh2;
        x[1] += (rn2.y * rn2.z) * sh2;
        x[2] += (rn2.z * rn2.z * k3 + k4) * sh2;
        x[3] += (rn2.x * rn2.z) * sh2;
        x[4] += (rn2.x * rn2.x - rn2.y * rn2.y) * sh2;
    }

    // Column 3
    {
        float3 rn3 = float3(M[0][0] + M[2][0], M[0][1] + M[2][1], M[0][2] + M[2][2]); // (float3(k, 0, k) * M) / k;
        x[0] += (rn3.x * rn3.y) * sh3;
        x[1] += (rn3.y * rn3.z) * sh3;
        x[2] += (rn3.z * rn3.z * k3 + k4) * sh3;
        x[3] += (rn3.x * rn3.z) * sh3;
        x[4] += (rn3.x * rn3.x - rn3.y * rn3.y) * sh3;
    }

    // Column 4
    {
        float3 rn4 = float3(M[1][0] + M[2][0], M[1][1] + M[2][1], M[1][2] + M[2][2]); // (float3(0, k, k) * M) / k;
        x[0] += (rn4.x * rn4.y) * sh4;
        x[1] += (rn4.y * rn4.z) * sh4;
        x[2] += (rn4.z * rn4.z * k3 + k4) * sh4;
        x[3] += (rn4.x * rn4.z) * sh4;
        x[4] += (rn4.x * rn4.x - rn4.y * rn4.y) * sh4;
    }

    x[4] *= 0.25;
}

void SphericalHarmonicsRotateL1(float3x3 M, inout float3 x[4])
{
    float3 x1[3];
    x1[0] = x[1];
    x1[1] = x[2];
    x1[2] = x[3];
    SphericalHarmonicsRotateBandL1(M, x1);
    x[1] = x1[0];
    x[2] = x1[1];
    x[3] = x1[2];
}

void SphericalHarmonicsRotateL2(float3x3 M, inout float3 x[9])
{
    float3 x1[3];
    x1[0] = x[1];
    x1[1] = x[2];
    x1[2] = x[3];
    SphericalHarmonicsRotateBandL1(M, x1);
    x[1] = x1[0];
    x[2] = x1[1];
    x[3] = x1[2];

    float3 x2[5];
    x2[0] = x[4];
    x2[1] = x[5];
    x2[2] = x[6];
    x2[3] = x[7];
    x2[4] = x[8];
    SphericalHarmonicsRotateBandL2(M, x2);
    x[4] = x2[0];
    x[5] = x2[1];
    x[6] = x2[2];
    x[7] = x2[3];
    x[8] = x2[4];
}

float3 QuaternionRotate(float4 q, float3 v)
{
    float3 t = 2.0 * cross(q.xyz, v);
    return v + q.w * t + cross(q.xyz, t);
}

float4 QuaternionAxisAngle(float3 axis, float angle)
{
    float sina, cosa;
    float angleHalf = angle * 0.5;
    float angleHalfSin = sin(angleHalf);
    float angleHalfCos = cos(angleHalf);
    return float4(axis * angleHalfSin, angleHalfCos);
}

void SphericalHarmonicsCoefficientsRGBToAtlasFloat4L1(float3 shCoefficientsRGB[4], out float4 shCoefficientsAtlasFloat4[3])
{
    // Constant (DC terms):
    shCoefficientsAtlasFloat4[0].x = shCoefficientsRGB[0].x; // shAr.w
    shCoefficientsAtlasFloat4[0].y = shCoefficientsRGB[0].y; // shAg.w
    shCoefficientsAtlasFloat4[0].z = shCoefficientsRGB[0].z; // shAb.w

    // Linear: (used by L1 and L2)
    // Swizzle the coefficients to be in { x, y, z } order.
    shCoefficientsAtlasFloat4[0].w = shCoefficientsRGB[3].x; // shAr.x
    shCoefficientsAtlasFloat4[1].x = shCoefficientsRGB[1].x; // shAr.y
    shCoefficientsAtlasFloat4[1].y = shCoefficientsRGB[2].x; // shAr.z

    shCoefficientsAtlasFloat4[1].z = shCoefficientsRGB[3].y; // shAg.x
    shCoefficientsAtlasFloat4[1].w = shCoefficientsRGB[1].y; // shAg.y
    shCoefficientsAtlasFloat4[2].x = shCoefficientsRGB[2].y; // shAg.z

    shCoefficientsAtlasFloat4[2].y = shCoefficientsRGB[3].z; // shAb.x
    shCoefficientsAtlasFloat4[2].z = shCoefficientsRGB[1].z; // shAb.y
    shCoefficientsAtlasFloat4[2].w = shCoefficientsRGB[2].z; // shAb.z
}

void SphericalHarmonicsCoefficientsAtlasFloat4ToRGBL1(float4 shCoefficientsAtlasFloat4[3], out float3 shCoefficientsRGB[4])
{
    // Constant (DC Terms) L0.rgb
    shCoefficientsRGB[0] = shCoefficientsAtlasFloat4[0].xyz; // shAr.w, shAg.w, shAb.w

    // Linear L1.rgb
    shCoefficientsRGB[3] = float3(shCoefficientsAtlasFloat4[0].w, shCoefficientsAtlasFloat4[1].z, shCoefficientsAtlasFloat4[2].y); // shAr.x, shAg.x, shAb.x
    shCoefficientsRGB[1] = float3(shCoefficientsAtlasFloat4[1].x, shCoefficientsAtlasFloat4[1].w, shCoefficientsAtlasFloat4[2].z); // shAr.y, shAg.y, shAb.y
    shCoefficientsRGB[2] = float3(shCoefficientsAtlasFloat4[1].y, shCoefficientsAtlasFloat4[2].x, shCoefficientsAtlasFloat4[2].w); // shAr.z, shAg.z, shAb.z
}

void SphericalHarmonicsCoefficientsRGBToAtlasFloat4L2(float3 shCoefficientsRGB[9], out float4 shCoefficientsAtlasFloat4[7])
{
    // Constant (DC terms):
    shCoefficientsAtlasFloat4[0].x = shCoefficientsRGB[0].x; // shAr.w
    shCoefficientsAtlasFloat4[0].y = shCoefficientsRGB[0].y; // shAg.w
    shCoefficientsAtlasFloat4[0].z = shCoefficientsRGB[0].z; // shAb.w

    // Linear: (used by L1 and L2)
    // Swizzle the coefficients to be in { x, y, z } order.
    shCoefficientsAtlasFloat4[0].w = shCoefficientsRGB[3].x; // shAr.x
    shCoefficientsAtlasFloat4[1].x = shCoefficientsRGB[1].x; // shAr.y
    shCoefficientsAtlasFloat4[1].y = shCoefficientsRGB[2].x; // shAr.z

    shCoefficientsAtlasFloat4[1].z = shCoefficientsRGB[3].y; // shAg.x
    shCoefficientsAtlasFloat4[1].w = shCoefficientsRGB[1].y; // shAg.y
    shCoefficientsAtlasFloat4[2].x = shCoefficientsRGB[2].y; // shAg.z

    shCoefficientsAtlasFloat4[2].y = shCoefficientsRGB[3].z; // shAb.x
    shCoefficientsAtlasFloat4[2].z = shCoefficientsRGB[1].z; // shAb.y
    shCoefficientsAtlasFloat4[2].w = shCoefficientsRGB[2].z; // shAb.z

    // Quadratic: (used by L2)
    shCoefficientsAtlasFloat4[3].x = shCoefficientsRGB[4].x; // shBr.x
    shCoefficientsAtlasFloat4[3].y = shCoefficientsRGB[5].x; // shBr.y
    shCoefficientsAtlasFloat4[3].z = shCoefficientsRGB[6].x; // shBr.z
    shCoefficientsAtlasFloat4[3].w = shCoefficientsRGB[7].x; // shBr.w

    shCoefficientsAtlasFloat4[4].x = shCoefficientsRGB[4].y; // shBg.x
    shCoefficientsAtlasFloat4[4].y = shCoefficientsRGB[5].y; // shBg.y
    shCoefficientsAtlasFloat4[4].z = shCoefficientsRGB[6].y; // shBg.z
    shCoefficientsAtlasFloat4[4].w = shCoefficientsRGB[7].y; // shBg.w

    shCoefficientsAtlasFloat4[5].x = shCoefficientsRGB[4].z; // shBb.x
    shCoefficientsAtlasFloat4[5].y = shCoefficientsRGB[5].z; // shBb.y
    shCoefficientsAtlasFloat4[5].z = shCoefficientsRGB[6].z; // shBb.z
    shCoefficientsAtlasFloat4[5].w = shCoefficientsRGB[7].z; // shBb.w

    shCoefficientsAtlasFloat4[6].x = shCoefficientsRGB[8].x; // shCr.x
    shCoefficientsAtlasFloat4[6].y = shCoefficientsRGB[8].y; // shCr.y
    shCoefficientsAtlasFloat4[6].z = shCoefficientsRGB[8].z; // shCr.z
    shCoefficientsAtlasFloat4[6].w = 0.0;
}

void SphericalHarmonicsCoefficientsAtlasFloat4ToRGBL2(float4 shCoefficientsAtlasFloat4[7], out float3 shCoefficientsRGB[9])
{
    // Constant (DC Terms) L0.rgb
    shCoefficientsRGB[0] = shCoefficientsAtlasFloat4[0].xyz; // shAr.w, shAg.w, shAb.w

    // Linear L1.rgb
    shCoefficientsRGB[3] = float3(shCoefficientsAtlasFloat4[0].w, shCoefficientsAtlasFloat4[1].z, shCoefficientsAtlasFloat4[2].y); // shAr.x, shAg.x, shAb.x
    shCoefficientsRGB[1] = float3(shCoefficientsAtlasFloat4[1].x, shCoefficientsAtlasFloat4[1].w, shCoefficientsAtlasFloat4[2].z); // shAr.y, shAg.y, shAb.y
    shCoefficientsRGB[2] = float3(shCoefficientsAtlasFloat4[1].y, shCoefficientsAtlasFloat4[2].x, shCoefficientsAtlasFloat4[2].w); // shAr.z, shAg.z, shAb.z

    // Quadratic L2.rgb
    shCoefficientsRGB[4] = float3(shCoefficientsAtlasFloat4[3].x, shCoefficientsAtlasFloat4[4].x, shCoefficientsAtlasFloat4[5].x); // shBr.x, shBg.x, shBb.x
    shCoefficientsRGB[5] = float3(shCoefficientsAtlasFloat4[3].y, shCoefficientsAtlasFloat4[4].y, shCoefficientsAtlasFloat4[5].y); // shBr.y, shBg.y, shBb.y
    shCoefficientsRGB[6] = float3(shCoefficientsAtlasFloat4[3].z, shCoefficientsAtlasFloat4[4].z, shCoefficientsAtlasFloat4[5].z); // shBr.z, shBg.z, shBb.z
    shCoefficientsRGB[7] = float3(shCoefficientsAtlasFloat4[3].w, shCoefficientsAtlasFloat4[4].w, shCoefficientsAtlasFloat4[5].w); // shBr.w, shBg.w, shBb.w
    shCoefficientsRGB[8] = shCoefficientsAtlasFloat4[6].xyz; // shCr.xyz
}

#endif