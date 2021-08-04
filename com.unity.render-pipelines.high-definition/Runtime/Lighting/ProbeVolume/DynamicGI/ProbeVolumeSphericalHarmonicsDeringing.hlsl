#ifndef PROBE_VOLUME_SPHERICAL_HARMONICS_DERINGING
#define PROBE_VOLUME_SPHERICAL_HARMONICS_DERINGING

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/DynamicGI/ProbeVolumeSphericalHarmonicsLighting.hlsl"

#define DERING_LUMINANCE_ONLY (1)

// TODO: Use a better luminance function (at least better luminance coefficients).
float ComputeLuminance(float3 color)
{
    return dot(color, float3(0.25, 0.5, 0.25));
}

// Dering the 9x float3 SH coefficients, and write out the corresponding shCoefficientIndex for the current pixel.
// Needed for isolating individual color channels in deringing code.
struct SHOutgoingRadiosityScalar
{
    float data[SH_COEFFICIENT_COUNT];
};

SHOutgoingRadiosityScalar SHOutgoingRadiosityReadColorChannel(SHOutgoingRadiosity shOutgoingRadiosity, int colorChannelIndex)
{
    SHOutgoingRadiosityScalar shOutgoingRadiosityScalar;
    for (int i = 0; i < SH_COEFFICIENT_COUNT; ++i)
    {
        shOutgoingRadiosityScalar.data[i] = shOutgoingRadiosity.data[i][colorChannelIndex];
    }
    return shOutgoingRadiosityScalar;
}

SHOutgoingRadiosityScalar SHOutgoingRadiosityLuminanceCompute(SHOutgoingRadiosity shOutgoingRadiosity)
{
    SHOutgoingRadiosityScalar shOutgoingRadiosityScalar;
    for (int i = 0; i < SH_COEFFICIENT_COUNT; ++i)
    {
        shOutgoingRadiosityScalar.data[i] = ComputeLuminance(shOutgoingRadiosity.data[i]);
    }
    return shOutgoingRadiosityScalar;
}

void SHOutgoingRadiosityWriteColorChannel(inout SHOutgoingRadiosity shOutgoingRadiosity, SHOutgoingRadiosityScalar shOutgoingRadiosityScalar, int colorChannelIndex)
{
    for (int i = 0; i < SH_COEFFICIENT_COUNT; ++i)
    {
        shOutgoingRadiosity.data[i][colorChannelIndex] = shOutgoingRadiosityScalar.data[i];
    }
}

void SHOutgoingRadiosityScalarRotateBand1(float3x3 M, inout float x[3])
{
    float3 SH = float3(-x[2], -x[0], x[1]);

    x[0] = dot(SH, -float3(M[0][1], M[1][1], M[2][1]));
    x[1] = dot(SH, float3(M[0][2], M[1][2], M[2][2]));
    x[2] = dot(SH, -float3(M[0][0], M[1][0], M[2][0]));
}

void SHOutgoingRadiosityScalarRotateBand2(float3x3 M, inout float x[5])
{
    // Decomposed + factored version of 5x5 matrix multiply of invA * sh from source.
    const float k0 = 0.9152912328637689;
    const float k1 = 0.9152912328637689 * 2.0;
    const float k2 = 1.5853309190424043;
    float sh0 = x[1] * -0.5 + (x[3] * 0.5 + x[4]); // 2x MADD
    float sh1 = (x[0] + (k2 / k0) * x[2] + x[3] + x[4]) * 0.5;
    float sh2 = x[0];
    float sh3 = -x[3];
    float sh4 = -x[1];

    const float k = 1.0 / sqrt(2.0);
    const float kInv = sqrt(2.0);
    const float k3 = k0 * 2.0 * K3SQRT5DIV4SQRTPI * k * k; // sqrt(3.0) / 2.0
    const float k4 = k0 * 2.0 * -KALMOSTONETHIRD;

    // Decomposed + factored version of 5x5 matrix multiply of 5 normals projected to 5 SH2 bands.
    // Column 0
    {
        float3 rn0 = float3(M[0][0], M[0][1], M[0][2]) * kInv; // (float3(1, 0, 0) * M) / k;
        x[0] = (rn0.x * rn0.y) * sh0;
        x[1] = (-rn0.y * rn0.z) * sh0;
        x[2] = (rn0.z * rn0.z * k3 + k4) * sh0;
        x[3] = (-rn0.x * rn0.z) * sh0;
        x[4] = (rn0.x * rn0.x - rn0.y * rn0.y) * sh0;
    }

    // Column 1
    {
        float3 rn1 = float3(M[2][0], M[2][1], M[2][2]) * kInv; // (float3(0, 0, 1) * M) / k;
        x[0] += (rn1.x * rn1.y) * sh1;
        x[1] += (-rn1.y * rn1.z) * sh1;
        x[2] += (rn1.z * rn1.z * k3 + k4) * sh1;
        x[3] += (-rn1.x * rn1.z) * sh1;
        x[4] += (rn1.x * rn1.x - rn1.y * rn1.y) * sh1;
    }

    // Column 2
    {
        float3 rn2 = float3(M[0][0] + M[1][0], M[0][1] + M[1][1], M[0][2] + M[1][2]); // (float3(k, k, 0) * M) / k;
        x[0] += (rn2.x * rn2.y) * sh2;
        x[1] += (-rn2.y * rn2.z) * sh2;
        x[2] += (rn2.z * rn2.z * k3 + k4) * sh2;
        x[3] += (-rn2.x * rn2.z) * sh2;
        x[4] += (rn2.x * rn2.x - rn2.y * rn2.y) * sh2;
    }

    // Column 3
    {
        float3 rn3 = float3(M[0][0] + M[2][0], M[0][1] + M[2][1], M[0][2] + M[2][2]); // (float3(k, 0, k) * M) / k;
        x[0] += (rn3.x * rn3.y) * sh3;
        x[1] += (-rn3.y * rn3.z) * sh3;
        x[2] += (rn3.z * rn3.z * k3 + k4) * sh3;
        x[3] += (-rn3.x * rn3.z) * sh3;
        x[4] += (rn3.x * rn3.x - rn3.y * rn3.y) * sh3;
    }

    // Column 4
    {
        float3 rn4 = float3(M[1][0] + M[2][0], M[1][1] + M[2][1], M[1][2] + M[2][2]); // (float3(0, k, k) * M) / k;
        x[0] += (rn4.x * rn4.y) * sh4;
        x[1] += (-rn4.y * rn4.z) * sh4;
        x[2] += (rn4.z * rn4.z * k3 + k4) * sh4;
        x[3] += (-rn4.x * rn4.z) * sh4;
        x[4] += (rn4.x * rn4.x - rn4.y * rn4.y) * sh4;
    }

    x[4] *= 0.5;
}

void SHOutgoingRadiosityScalarRotate(float3x3 M, inout SHOutgoingRadiosityScalar shOutgoingRadiosityScalar)
{
    float x1[3];
    x1[0] = shOutgoingRadiosityScalar.data[1];
    x1[1] = shOutgoingRadiosityScalar.data[2];
    x1[2] = shOutgoingRadiosityScalar.data[3];
    SHOutgoingRadiosityScalarRotateBand1(M, x1);
    float x2[5];
    x2[0] = shOutgoingRadiosityScalar.data[4];
    x2[1] = shOutgoingRadiosityScalar.data[5];
    x2[2] = shOutgoingRadiosityScalar.data[6];
    x2[3] = shOutgoingRadiosityScalar.data[7];
    x2[4] = shOutgoingRadiosityScalar.data[8];
    SHOutgoingRadiosityScalarRotateBand2(M, x2);
    shOutgoingRadiosityScalar.data[1] = x1[0];
    shOutgoingRadiosityScalar.data[2] = x1[1];
    shOutgoingRadiosityScalar.data[3] = x1[2];
    shOutgoingRadiosityScalar.data[4] = x2[0];
    shOutgoingRadiosityScalar.data[5] = x2[1];
    shOutgoingRadiosityScalar.data[6] = x2[2];
    shOutgoingRadiosityScalar.data[7] = x2[3];
    shOutgoingRadiosityScalar.data[8] = x2[4];
}

// optimal linear direction, related to bent normal, etc.
float3 SHOutgoingRadiosityScalarGetOptimalLinear(const SHOutgoingRadiosityScalar shOutgoingRadiosityScalar)
{
    return float3(-shOutgoingRadiosityScalar.data[3], -shOutgoingRadiosityScalar.data[1], shOutgoingRadiosityScalar.data[2]);
}

// source: Building an Orthonormal Basis, Revisited
// http://jcgt.org/published/0006/01/01/
// Same as reference implementation, except transposed.
float3x3 ComputeTangentToWorldMatrix(float3 n)
{
    float3x3 res;
    res[2][0] = n.x;
    res[2][1] = n.y;
    res[2][2] = n.z;

    float s = (n.z >= 0.0f) ? 1.0f : -1.0f;
    float a = -1.0f / (s + n.z);
    float b = n.x * n.y * a;

    res[0][0] = 1.0f + s * n.x * n.x * a;
    res[0][1] = s * b;
    res[0][2] = -s * n.x;

    res[1][0] = b;
    res[1][1] = s + n.y * n.y * a;
    res[1][2] = -n.y;

    return res;
}

void FrameFromNormal(float3 normal, out float3 tangent, out float3 binormal)
{
#if 0
    // PPSloan version:
    if (abs(normal.x) > abs(normal.z))
    {
        binormal.x = -normal.y;
        binormal.y = normal.x;
        binormal.z = 0.0f;
    }
    else
    {
        binormal.x = 0.0f;
        binormal.y = -normal.z;
        binormal.z = normal.y;
    }

    binormal = normalize(binormal);
    tangent = cross(binormal, normal);
#else
    float3x3 tangentToWorldMatrix = ComputeTangentToWorldMatrix(normal);
    float3x3 worldToTangentMatrix = transpose(transpose(tangentToWorldMatrix)); // TODO: Test this transpose - from the conversion from glsl to hlsl
    binormal = worldToTangentMatrix[1];
    tangent = worldToTangentMatrix[0];
#endif
}

// a * z^2 + b * z + c + ce is the function, we add the window parameters l2, l1
// l2 * a * z^2 + l1 * b * z + l2 *c + c3, and want to make sure we make the minimum zero (or some positive epsilon)
float ComputeMinError(const float a, const float b, const float c, inout float zmin)
{
    // a * z^2 + b * z + c
    // differentiate with respect to z:
    // 2a z + b
    // second derivative is simply 2a, so if that's positive it is a minima, if it's negative it's a maxims (so test ends)

    if (a > 0.f)
    {
        float ztest = -b / (2.0 * a);

        // check if this min value is a valid minima
        if (ztest > -1.0f && ztest < 1.0f)
        {
            zmin = ztest;
            return a * zmin * zmin + b * zmin + c;
        }
    }

    // if you get here, it must be one of the two extrema, +/- z, just compute both and take the smaller...
    float ep = a + b + c;
    float em = a - b + c;

    if (ep < em)
    {
        zmin = 1.0f;
        return ep;
    }

    zmin = -1.0f;
    return em;
}

// ZH only version of solver
void SearchSHWindow(const float a, const float b, const float c, const float ce, inout float4 window, inout float zmin)
{
    window[0] = 1.0f;

    const float3 g_shWindowTableBig[7] =
    {
        float3(1.000000000f, 1.000000000f, 1.000000000f ),
        float3(0.970142245f, 0.906294703f, 0.811708033f ),
        float3(0.940284550f, 0.812589467f, 0.623416066f ),
        float3(0.921331406f, 0.764526367f, 0.544733047f ),
        float3(0.902378201f, 0.716463327f, 0.466050029f ),
        float3(0.829950631f, 0.564606309f, 0.295258284f ),
        float3(0.757523060f, 0.412749231f, 0.124466546f )
    };

    for (int i = 0; i < 7; i++)
    {
        float curError = ComputeMinError(a * g_shWindowTableBig[i][1], b * g_shWindowTableBig[i][0], c * g_shWindowTableBig[i][1] + ce, zmin);

        if (curError > 0.0f)
        {
            // shouldn't have called probably, since delta function is being used in this case...
            if (i == 0)
            {
                // zmin is already set
                window[1] = g_shWindowTableBig[i][0];
                window[2] = g_shWindowTableBig[i][1];
                window[3] = g_shWindowTableBig[i][2];
                return;
            }

            // do binary search between these two values...
            float t = 0.5f;

            // previous iteration had a negative...
            int ilast = i - 1;

            float tmin = 0.0f;
            float tmax = 1.0f;

            int iter = 0;

            // binary search...

            const int maxIter = 5;

            while (iter < maxIter)
            {
                t = (tmin + tmax) * 0.5f;
                float v0 = g_shWindowTableBig[ilast][0] * (1.0f - t) + g_shWindowTableBig[i][0] * t;
                float v1 = g_shWindowTableBig[ilast][1] * (1.0f - t) + g_shWindowTableBig[i][1] * t;
                float midErr = ComputeMinError(a * v1, b * v0, c * v1 + ce, zmin);

                if (midErr < 0.0f)
                {
                    tmin = t;
                }
                else
                {
                    tmax = t;
                }
                iter++;
            }

            t = tmax;

            window[1] = g_shWindowTableBig[ilast][0] * (1.0f - t) + g_shWindowTableBig[i][0] * t;
            window[2] = g_shWindowTableBig[ilast][1] * (1.0f - t) + g_shWindowTableBig[i][1] * t;
            window[3] = g_shWindowTableBig[ilast][2] * (1.0f - t) + g_shWindowTableBig[i][2] * t;

            // this is just for the min z value that might be used later...
            ComputeMinError(a * window[2], b * window[1], c * window[2] + ce, zmin);

            //window[3] = g_shWindowTableBig[ilast][2] * t + g_shWindowTableBig[i][2] * ( 1.0f - t );
            return;
        }
    }

    // delta function is all we can be at this point - but this means we never succesfully windowed anything also...
    // zmin set from the last compute error function...
    window[1] = g_shWindowTableBig[6][0];
    window[2] = g_shWindowTableBig[6][1];
    window[3] = g_shWindowTableBig[6][2];
}

// this is related to the above, but instead of a pure quadratc, we have to multiply "extra" by
// z*(1-z^2)^(1/2), use newtons method to optimize... This function is more likely if you have a negative
// near the "bottom" of the function...

// we only care about the sign of the error
float ComputeMinError(const float a, const float b, const float c, const float extra, inout float zmin)
{
    // f(z) = a z^2 + b z + c + extra * z * ( 1 - z^2 )^(1/2)
    // df/dz = 2az + b + extra * ( 1 - 2 * z^2 ) / sqrtf( 1- z^2 )
    // df2/dz2 = 2a - extra z ( 3 - 2 z^2 )/ ( 1- z^2) ^(3/2)

    float z0 = -0.707107f; // minimum of extra function...

    // these are the values at the extremes
    float minM = a - b + c;
    float minP = a + b + c;

    int iter = 0;

    bool haveNewton = false;

    float z = z0;
    const int MAX_ITER = 10;

    float quadMinZ = ComputeMinError(a, b, c, zmin);
    // the smallest -z*sqrt(1-z^2) can be is -0.5f, so that's a conservative bound...
    if ((quadMinZ - 0.5f * extra) > 0.0f)
        return 1.0f;

    while (iter < MAX_ITER)
    {
        float sqrtPart = sqrt(1.0f - z * z);

        //float fx = a * z * z + b * z + c + extra * z * sqrtPart;

        float fpx = 2.0f * a * z + b + extra * (1.0f - 2.0f * z * z) / sqrtPart;
        float fx2 = 2.0f * a - extra * z * (3.0f - 2.0f * z * z) / (sqrtPart * (1.0f - z * z));

        float deltaZ = fpx / fx2;

        float nz = z - deltaZ;

        if (isinf(nz) || (abs(nz) >= 3.0f))
        {
            iter = MAX_ITER;
            haveNewton = false;
        }
        else
        {
            // early out for any negative...
            z = nz;
            float fx = a * z * z + b * z + c + extra * z * sqrtPart;

            if (fx < 0.0f)
            {
                zmin = z;
                return fx;
            }

            haveNewton = true;
        }
        iter++;

        // no reason to noodle too much, the function can't change that fast anyways...
        if (abs(deltaZ) < 0.005f)
        {
            iter = MAX_ITER; // we have stalled out in the search
        }
    }

    if (haveNewton)
    {
        float sqrtPart = sqrt(1.0f - z * z);
        float newtonErr = a * z * z + b * z + c + extra * z * sqrtPart;

        if (newtonErr < minM && newtonErr < minP)
        {
            zmin = z;
            return newtonErr;
        }
    }

    if (minM < minP)
    {
        zmin = -1.0f;
        return minM;
    }

    zmin = 1.0f;
    return minP;
}

// lump the m==1 and m==2 bands together, but nothing else
void SearchSHWindow(const float aIn, const float b, const float cIn, const float ce, const float q1in, const float q2, inout float4 window, inout float zmin)
{
    window[0] = 1.0f;

    const float3 g_shWindowTableBig[7] =
    {
        float3(1.000000000f, 1.000000000f, 1.000000000f ),
        float3(0.970142245f, 0.906294703f, 0.811708033f ),
        float3(0.940284550f, 0.812589467f, 0.623416066f ),
        float3(0.921331406f, 0.764526367f, 0.544733047f ),
        float3(0.902378201f, 0.716463327f, 0.466050029f ),
        float3(0.829950631f, 0.564606309f, 0.295258284f ),
        float3(0.757523060f, 0.412749231f, 0.124466546f )
    };

    const float extraFactor = 0.5462742153f;
    const float extraFactorq1 = 1.092548431f;


    // you can push all of the rest of the energy into a single SH function that is not a ZH
    // one of them is xz, or x^2 - y^2, just take the most negative possibility, which is -extraFactor * (1 - z^2)
    // so you have a += extraFactor * qe and c -= extraFactor * qe
    // otherwise, everything is the same...
    float a = aIn + extraFactor * q2;
    float c = cIn - extraFactor * q2;

    float q1 = extraFactorq1 * q1in;

    for (int i = 0; i < 7; i++)
    {
        float curError = ComputeMinError(a * g_shWindowTableBig[i][1], b * g_shWindowTableBig[i][0], c * g_shWindowTableBig[i][1] + ce, q1 * g_shWindowTableBig[i][1], zmin);

        if (curError > 0.0f)
        {
            // shouldn't have called probably, since delta function is being used in this case...
            if (i == 0)
            {
                // zmin is already set
                window[1] = g_shWindowTableBig[i][0];
                window[2] = g_shWindowTableBig[i][1];
                window[3] = g_shWindowTableBig[i][2];
                return;
            }

            // do binary search between these two values...
            float t = 0.5f;

            // previous iteration had a negative...
            int ilast = i - 1;

            float tmin = 0.0f;
            float tmax = 1.0f;

            int iter = 0;

            // binary search...
            const int maxIter = 5;

            while (iter < maxIter)
            {
                t = (tmin + tmax) * 0.5f;
                float v0 = g_shWindowTableBig[ilast][0] * (1.0f - t) + g_shWindowTableBig[i][0] * t;
                float v1 = g_shWindowTableBig[ilast][1] * (1.0f - t) + g_shWindowTableBig[i][1] * t;
                float midErr = ComputeMinError(a * v1, b * v0, c * v1 + ce, q1 * v1, zmin);

                if (midErr < 0.0f)
                {
                    tmin = t;
                }
                else
                {
                    tmax = t;
                }
                iter++;
            }

            t = tmax;

            window[1] = g_shWindowTableBig[ilast][0] * (1.0f - t) + g_shWindowTableBig[i][0] * t;
            window[2] = g_shWindowTableBig[ilast][1] * (1.0f - t) + g_shWindowTableBig[i][1] * t;
            window[3] = g_shWindowTableBig[ilast][2] * (1.0f - t) + g_shWindowTableBig[i][2] * t;

            // this is just for the min z value that might be used later...
            //float errA = ComputeMinError( a * window[2], b * window[1], c * window[2] + ce, q1 * window[2], zmin );

            //window[3] = g_shWindowTableBig[ilast][2] * t + g_shWindowTableBig[i][2] * ( 1.0f - t );
            return;
        }
    }

    // delta function is all we can be at this point - but this means we never succesfully windowed anything also...
    // zmin set from the last compute error function...
    window[1] = g_shWindowTableBig[6][0];
    window[2] = g_shWindowTableBig[6][1];
    window[3] = g_shWindowTableBig[6][2];
}

void SHZHDeRingFull(float zh[3], inout float4 window, const float q1err, const float q2err)
{
    const float dcVal = 0.2820947917738781f;
    const float lVal = 0.4886025119029199f;
    // sqrt(5 / Pi ) / 4, leading constant for 3z^2 -1...
    const float qVal = 0.315391565f;

    // a b c is the base part of the quadratic, from the ZH function
    float a = qVal * zh[2] * 3.0f;
    float b = lVal * zh[1];
    float c = -qVal * zh[2];

    float zmin;

    // could do this in 1 step, but doing it in stages seems to be better, since this first part is
    // analytic anyways

    // do nothing if the function is positive...
    if (ComputeMinError(a, b, c + dcVal * zh[0], zmin) > 0.0f)
    {
        window = 1;
    }
    else
    {
        SearchSHWindow(a, b, c, dcVal * zh[0], window, zmin);
    }

    if ((q1err > 0.0f) || (q2err > 0.0f))
    {
        //const float newResidual = sqrtf( residualErr2 ) * window[2];
        float newq1 = q1err * window[2];
        float newq2 = q2err * window[2];

        float4 extraWindow;

        float na = a * window[2];
        float nb = b * window[1];
        float nc = c * window[2];

        // just make the windows multiplicative...
        SearchSHWindow(na, nb, nc, dcVal * zh[0], newq1, newq2, extraWindow, zmin);

        window[1] *= extraWindow[1];
        window[2] *= extraWindow[2];
        window[3] *= extraWindow[3];
    }
}

void SHOutgoingRadiosityScalarComputeWindow(const SHOutgoingRadiosityScalar shOutgoingRadiosityScalar, inout float4 window)
{
    float3 optLin = SHOutgoingRadiosityScalarGetOptimalLinear(shOutgoingRadiosityScalar);

    float vecMag = sqrt(dot(optLin, optLin));

    // xform the sh into a frame where we can reason about things better...
    SHOutgoingRadiosityScalar shOutgoingRadiosityScalarRotated = shOutgoingRadiosityScalar;

    window = 1;

    if (vecMag > 0.0f)
    {
        optLin *= (1.0f / vecMag);
        float3 tangent;
        float3 binormal;
        FrameFromNormal(optLin, tangent, binormal);

        float3x3 rotationMatrix = transpose(float3x3(tangent, binormal, optLin)); // TODO: Test this transpose - from conversion from glsl to hlsl

        SHOutgoingRadiosityScalarRotate(rotationMatrix, shOutgoingRadiosityScalarRotated);
    }

    float zh[3];

    // pull out the Zh parts...
    zh[0] = shOutgoingRadiosityScalarRotated.data[0];
    zh[1] = shOutgoingRadiosityScalarRotated.data[2];
    zh[2] = shOutgoingRadiosityScalarRotated.data[6];

    // we want the "length" in each sub-space to bound things...

    float q1 = sqrt(shOutgoingRadiosityScalarRotated.data[5] * shOutgoingRadiosityScalarRotated.data[5] + shOutgoingRadiosityScalarRotated.data[7] * shOutgoingRadiosityScalarRotated.data[7]);
    float q2 = sqrt(shOutgoingRadiosityScalarRotated.data[4] * shOutgoingRadiosityScalarRotated.data[4] + shOutgoingRadiosityScalarRotated.data[8] * shOutgoingRadiosityScalarRotated.data[8]);

    SHZHDeRingFull(zh, window, q1, q2);
}

float4 SHMinWindow(float4 windowA, float4 windowB)
{
    return min(windowA, windowB);
}

void SHOutgoingRadiosityDeringComputeWindow(SHOutgoingRadiosity shOutgoingRadiosity, out float3 windowOutIn)
{
    float4 windows[3];

    float4 windowOut = 1;

    for (int c = 0; c < 3; c++)
    {
        if (shOutgoingRadiosity.data[0][c] > 0.001f)
        {
            SHOutgoingRadiosityScalar shOutgoingRadiosityScalar = SHOutgoingRadiosityReadColorChannel(shOutgoingRadiosity, c);

            SHOutgoingRadiosityScalarComputeWindow(shOutgoingRadiosityScalar, windows[c]);
            windowOut = SHMinWindow(windowOut, windows[c]);
        }
    }

    windowOutIn = windowOut.xyz;
}

void SHOutgoingRadiosityDeringComputeWindowLuminance(SHOutgoingRadiosity shOutgoingRadiosity, out float3 windowOutIn)
{
    float4 windowOut = 1;

    if (ComputeLuminance(shOutgoingRadiosity.data[0]) > 0.001f)
    {
        SHOutgoingRadiosityScalar shOutgoingRadiosityScalar = SHOutgoingRadiosityLuminanceCompute(shOutgoingRadiosity);

        SHOutgoingRadiosityScalarComputeWindow(shOutgoingRadiosityScalar, windowOut);
    }

    windowOutIn = windowOut.xyz;
}

void SHOutgoingRadiosityConvolveWindow(inout SHOutgoingRadiosity shOutgoingRadiosity, float3 window)
{
    shOutgoingRadiosity.data[0] *= window[0];

    shOutgoingRadiosity.data[1] *= window[1];
    shOutgoingRadiosity.data[2] *= window[1];
    shOutgoingRadiosity.data[3] *= window[1];

    shOutgoingRadiosity.data[4] *= window[2];
    shOutgoingRadiosity.data[5] *= window[2];
    shOutgoingRadiosity.data[6] *= window[2];
    shOutgoingRadiosity.data[7] *= window[2];
    shOutgoingRadiosity.data[8] *= window[2];
}

void SHOutgoingRadiosityDering(inout SHOutgoingRadiosity shOutgoingRadiosity)
{
    float3 window;
    SHOutgoingRadiosityDeringComputeWindow(shOutgoingRadiosity, window);
    SHOutgoingRadiosityConvolveWindow(shOutgoingRadiosity, window);
}

void SHOutgoingRadiosityDeringLuminance(inout SHOutgoingRadiosity shOutgoingRadiosity)
{
    float3 window;
    SHOutgoingRadiosityDeringComputeWindowLuminance(shOutgoingRadiosity, window);
    SHOutgoingRadiosityConvolveWindow(shOutgoingRadiosity, window);
}

void WindowDirectSH(inout float3 sh[9])
{
    const float extraWindow[3] = { 1.0f, 0.922066f, 0.731864f };

    // Apply windowing: Essentially SHConv3 times the window constants
    for (int index = 0; index < 9; ++index)
    {
        float window = 0.0f;

        if (index == 0)
            window = extraWindow[0];
        else if (index < 4)
            window = extraWindow[1];
        else
            window = extraWindow[2];

        sh[index] *= window;
    }
}

#endif
