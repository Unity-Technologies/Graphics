#ifndef WORLEY_UTILITIES_H
#define WORLEY_UTILITIES_H

// Implementation inspired from https://www.shadertoy.com/view/3dVXDc
// Hash by David_Hoskins
#define UI0 1597334673U
#define UI1 3812015801U
#define UI2 uint2(UI0, UI1)
#define UI3 uint3(UI0, UI1, 2798796415U)
#define UIF (1.0 / float(0xffffffffU))

float3 hash33(float3 p)
{
    uint3 q = uint3(int3(p)) * UI3;
    q = (q.x ^ q.y ^ q.z) * UI3;
    return -1. + 2. * float3(q) * UIF;
}

// Density remapping function
float remap(float x, float a, float b, float c, float d)
{
    return (((x - a) / (b - a)) * (d - c)) + c;
}

// Gradient noise by iq (modified to be tileable)
float GradientNoise(float3 x, float freq)
{
    // grid
    float3 p = floor(x);
    float3 w = frac(x);

    // quintic interpolant
    float3 u = w * w * w * (w * (w * 6. - 15.) + 10.);

    // gradients
    float3 ga = hash33(fmod(p + float3(0., 0., 0.), freq));
    float3 gb = hash33(fmod(p + float3(1., 0., 0.), freq));
    float3 gc = hash33(fmod(p + float3(0., 1., 0.), freq));
    float3 gd = hash33(fmod(p + float3(1., 1., 0.), freq));
    float3 ge = hash33(fmod(p + float3(0., 0., 1.), freq));
    float3 gf = hash33(fmod(p + float3(1., 0., 1.), freq));
    float3 gg = hash33(fmod(p + float3(0., 1., 1.), freq));
    float3 gh = hash33(fmod(p + float3(1., 1., 1.), freq));

    // projections
    float va = dot(ga, w - float3(0., 0., 0.));
    float vb = dot(gb, w - float3(1., 0., 0.));
    float vc = dot(gc, w - float3(0., 1., 0.));
    float vd = dot(gd, w - float3(1., 1., 0.));
    float ve = dot(ge, w - float3(0., 0., 1.));
    float vf = dot(gf, w - float3(1., 0., 1.));
    float vg = dot(gg, w - float3(0., 1., 1.));
    float vh = dot(gh, w - float3(1., 1., 1.));

    // interpolation
    return va +
        u.x * (vb - va) +
        u.y * (vc - va) +
        u.z * (ve - va) +
        u.x * u.y * (va - vb - vc + vd) +
        u.y * u.z * (va - vc - ve + vg) +
        u.z * u.x * (va - vb - ve + vf) +
        u.x * u.y * u.z * (-va + vb + vc - vd + ve - vf - vg + vh);
}

// There is a difference between the original implementation's mod and hlsl's fmod, so we mimic the glsl version for the algorithm
#define Modulo(x,y) (x-y*floor(x/y))

// Tileable 3D worley noise
float WorleyNoise(float3 uv, float freq)
{
    float3 id = floor(uv);
    float3 p = frac(uv);

    float minDist = 10000.;
    for (float x = -1.; x <= 1.; ++x)
    {
        for (float y = -1.; y <= 1.; ++y)
        {
            for (float z = -1.; z <= 1.; ++z)
            {
                float3 offset = float3(x, y, z);
                float3 idOffset = id + offset;
                float3 h = hash33(Modulo(idOffset.xyz, freq)) * .5 + .5;
                h += offset;
                float3 d = p - h;
                minDist = min(minDist, dot(d, d));
            }
        }
    }
    return minDist;
}

float EvaluatePerlinFractalBrownianMotion(float3 position, float initialFrequence, int numOctaves)
{
    const float G = exp2(-0.85);

    // Accumulation values
    float amplitude = 1.0;
    float frequence = initialFrequence;
    float result = 0.0;

    for (int i = 0; i < numOctaves; ++i)
    {
        result += amplitude * GradientNoise(position * frequence, frequence);
        frequence *= 2.0;
        amplitude *= G;
    }
    return result;
}

#endif // WORLEY_UTILITIES_H
