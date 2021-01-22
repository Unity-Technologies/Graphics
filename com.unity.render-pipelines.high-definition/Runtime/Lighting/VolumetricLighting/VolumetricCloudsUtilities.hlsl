#ifndef  VOLUMETRIC_CLOUD_UTILITIES_H
#define VOLUMETRIC_CLOUD_UTILITIES_H

// Maximal length of the cloud ray
#define MAX_CLOUD_RAY_LENGTH 200000

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

float HenyeyGreenstein(float cosAngle, float g)
{
    // There is a mistake in the GPU Gem7 Paper, the result should be divided by 1/(4.PI)
    float g2 = g * g;
    return (1.0 / (4.0 * PI)) * (1.0 - g2) / PositivePow(1.0 + g2 - 2.0 * g * cosAngle, 1.5);
}

float PowderEffect(float cloudDensity, float cosAngle, float intensity)
{
    float powderEffect = 1.0 - exp(-cloudDensity * 4.0);
    powderEffect = saturate(powderEffect * 2.0);
    return lerp(1.0, lerp(1.0, powderEffect, smoothstep(0.5, -0.5, cosAngle)), intensity);
}

int RaySphereIntersection(float3 start, float3 dir, float radius, out float2 result)
{
    float a = dot(dir, dir);
    float b = 2.0 * dot(dir, start);
    float c = dot(start, start) - (radius * radius);
    float d = (b*b) - 4.0*a*c;
    result = 0.0;
    if (d < 0.0)
        return 0;

    // Compute the values required for the solution eval
    float sqrtD = sqrt(d);
    float rcp2a = 1.0 / (2.0 * a);
    result = float2((-b - sqrtD) * rcp2a, (-b + sqrtD) * rcp2a);

    // Remove the solutions we do not want
    int numSolutions = 2;
    if (result.x < 0.0)
    {
        numSolutions--;
        result.x = result.y;
    }
    if (result.y < 0.0)
        numSolutions--;

    // Return the number of solutions
    return numSolutions;
}

bool RaySphereIntersection(float3 start, float3 dir, float radius)
{
    float a = dot(dir, dir);
    float b = 2.0 * dot(dir, start);
    float c = dot(start, start) - (radius * radius);
    float d = (b*b) - 4.0*a*c;

    if (d < 0.0)
        return false;

    // Compute the values required for the solution eval
    float sqrtD = sqrt(d);
    float rcp2a = 1.0 / (2.0 * a);
    float2 result = float2((-b - sqrtD) * rcp2a, (-b + sqrtD) * rcp2a);
    return result.x > 0.0 || result.y > 0.0;
    //return ((-b - sqrtD) > 0.0 || (-b + sqrtD) > 0.0);
}

bool IntersectPlane(float3 ray_origin, float3 ray_dir, float3 pos, float3 normal, out float t)
{
    float denom = dot(normal, ray_dir);
    if (abs(denom) > 1e-6)
    {
        float3 d = pos - ray_origin;
        t = dot(d, normal) / denom;
        return (t >= 0);
    }
    return false;
}

float ConvertCloudDepth(float3 position)
{
    float4 hClip = TransformWorldToHClip(position);
    return hClip.z / hClip.w;
}

// Given that the sky is virtually a skybox, we cannot use the motion vector buffer
float2 EvaluateCloudMotionVectors(float2 fullResCoord, float deviceDepth, float positionFlag)
{
    PositionInputs posInput = GetPositionInput(fullResCoord, _ScreenSize.zw, deviceDepth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
    float4 worldPos = float4(posInput.positionWS, positionFlag);
    float4 prevPos = worldPos;

    float4 prevClipPos = mul(UNITY_MATRIX_PREV_VP, prevPos);
    float4 curClipPos = mul(UNITY_MATRIX_UNJITTERED_VP, worldPos);

    float2 previousPositionCS = prevClipPos.xy / prevClipPos.w;
    float2 positionCS = curClipPos.xy / curClipPos.w;

    // Convert from Clip space (-1..1) to NDC 0..1 space
    float2 velocity = (positionCS - previousPositionCS) * 0.5;
#if UNITY_UV_STARTS_AT_TOP
    velocity.y = -velocity.y;
#endif
    return velocity;
}

#define CENTER 4
#define CENTER_WEIGHT 0.622269
#define PLUS_0 1
#define PLUS_1 3
#define PLUS_2 5
#define PLUS_3 4
#define PLUS_WEIGHT 0.083286
#define CROSS_0 0
#define CROSS_1 2
#define CROSS_2 6
#define CROSS_3 8
#define CROSS_WEIGHT 0.011147

#endif // VOLUMETRIC_CLOUD_UTILITIES_H
