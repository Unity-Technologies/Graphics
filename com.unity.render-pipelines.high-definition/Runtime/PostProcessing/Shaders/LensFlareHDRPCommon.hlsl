#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"

struct Attributes
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 texcoord : TEXCOORD0;
    float occlusion : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};

sampler2D _FlareTex;
TEXTURE2D_X(_FlareOcclusionBufferTex);

float4 _FlareColor;
float4 _FlareData0; // x: localCos0, y: localSin0, zw: PositionOffsetXY
float4 _FlareData1; // x: OcclusionRadius, y: OcclusionSampleCount, z: ScreenPosZ, w: Falloff
float4 _FlareData2; // xy: ScreenPos, zw: FlareSize
float4 _FlareData3; // xy: RayOffset, z: invSideCount, w: Edge Offset
float4 _FlareData4; // x: SDF Roundness, y: SDF Frequency

#define _LocalCos0          _FlareData0.x
#define _LocalSin0          _FlareData0.y
#define _PositionOffset     _FlareData0.zw

#define _ScreenPosZ         _FlareData1.z
#define _FlareFalloff       _FlareData1.w

#define _ScreenPos          _FlareData2.xy
#define _FlareSize          _FlareData2.zw

#define _FlareRayOffset     _FlareData3.xy
#define _FlareShapeInvSide  _FlareData3.z
#define _FlareEdgeOffset    _FlareData3.w

#define _FlareSDFRoundness  _FlareData4.x
#define _FlareSDFFrequency  _FlareData4.y

float2 Rotate(float2 v, float cos0, float sin0)
{
    return float2(v.x * cos0 - v.y * sin0,
                  v.x * sin0 + v.y * cos0);
}

Varyings vert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float screenRatio = _ScreenSize.y / _ScreenSize.x;

    float4 posPreScale = float4(2.0f, 2.0f, 1.0f, 1.0f) * GetQuadVertexPosition(input.vertexID) - float4(1.0f, 1.0f, 0.0f, 0.0);
    output.texcoord = GetQuadTexCoord(input.vertexID);

    posPreScale.xy *= _FlareSize;
    float2 local = Rotate(posPreScale.xy, _LocalCos0, _LocalSin0);

    local.x *= screenRatio;

    output.positionCS.xy = local + _ScreenPos + _FlareRayOffset + _PositionOffset;
    output.positionCS.zw = posPreScale.zw;

    output.occlusion = 1.0f;

    return output;
}

float4 ComputeGlow(float2 uv)
{
    float2 v = (uv - 0.5f) * 2.0f;

#if FLARE_INVERSE_SDF
    float sdf = saturate(-length(v) + _FlareEdgeOffset);
    // Cannot be simplify as 1 - sdf
    sdf = sdf * (1.0f - sdf) / (sdf + 1e-6f);
#else
    float sdf = saturate(-length(v) + 1.0f + _FlareEdgeOffset);
#endif

    return pow(sdf, _FlareFalloff);
}

// Ref: https://www.shadertoy.com/view/MtKcWW
float4 ComputeIris(float2 uv_)
{
    const float r = _FlareEdgeOffset - _FlareSDFRoundness;

    // these 2 lines can be precomputed
    float an = 6.2831853f * _FlareShapeInvSide;
    float he = r * tan(0.5f * an);

    float2 p = (uv_ - 0.5f) * 2.0f;

    p = -p.yx;
    float bn = an * floor((atan2(p.y, p.x) + 0.5f * an) / an);
    float cos0 = cos(bn);
    float sin0 = sin(bn);
    p = float2( cos0 * p.x + sin0 * p.y,
               -sin0 * p.x + cos0 * p.y);

    // side of polygon
    float sdf = length(p - float2(r, clamp(p.y, -he, he))) * sign(p.x - r) - _FlareSDFRoundness;

#if FLARE_INVERSE_SDF
    sdf = saturate(-sdf);
    sdf = sdf * (1.0f - sdf) / (sdf + 1e-6f);
#else
    sdf = sdf * (1.0f - sdf) / (sdf + 1e-6f);
    sdf = saturate(sdf);
#endif

    return saturate(pow(sdf, _FlareFalloff));
}

float noise(float t)
{
    return InitRandom(t.xx).x;
}

float noise(float2 t)
{
    return InitRandom(t);
}

float3 lensflare(float2 uv, float2 pos)
{
    float2 main = uv - pos;
    float2 uvd = uv * (length(uv));

    float ang = atan2(main.x, main.y) * 10.5f;
    float dist = length(main); dist = pow(dist, .1);
    float n = noise(float2(ang * 16.0, dist * 32.0));

    float f0 = 1.0 / (length(uv - pos) * 16.0 + 1.0);

    f0 = f0 + f0 * (sin(noise(sin(ang * 2. + pos.x) * 4.0 - cos(ang * 3. + pos.y)) * 16.) * .1 + dist * .1 + .8);

    float f1 = max(0.01 - pow(length(uv + 1.2 * pos), 1.9), .0) * 7.0;

    float f2 = max(1.0 / (1.0 + 32.0 * pow(length(uvd + 0.8 * pos), 2.0)), .0) * 00.25;
    float f22 = max(1.0 / (1.0 + 32.0 * pow(length(uvd + 0.85 * pos), 2.0)), .0) * 00.23;
    float f23 = max(1.0 / (1.0 + 32.0 * pow(length(uvd + 0.9 * pos), 2.0)), .0) * 00.21;

    float2 uvx = lerp(uv, uvd, -0.5);

    float f4 = max(0.01 - pow(length(uvx + 0.4 * pos), 2.4), .0) * 6.0;
    float f42 = max(0.01 - pow(length(uvx + 0.45 * pos), 2.4), .0) * 5.0;
    float f43 = max(0.01 - pow(length(uvx + 0.5 * pos), 2.4), .0) * 3.0;

    uvx = lerp(uv, uvd, -.4);

    float f5 = max(0.01 - pow(length(uvx + 0.2 * pos), 5.5), .0) * 2.0;
    float f52 = max(0.01 - pow(length(uvx + 0.4 * pos), 5.5), .0) * 2.0;
    float f53 = max(0.01 - pow(length(uvx + 0.6 * pos), 5.5), .0) * 2.0;

    uvx = lerp(uv, uvd, -0.5);

    float f6 = max(0.01 - pow(length(uvx - 0.3 * pos), 1.6), .0) * 6.0;
    float f62 = max(0.01 - pow(length(uvx - 0.325 * pos), 1.6), .0) * 3.0;
    float f63 = max(0.01 - pow(length(uvx - 0.35 * pos), 1.6), .0) * 5.0;

    float3 c = .0;

    c.r += f2 + f4 + f5 + f6;
    c.g += f22 + f42 + f52 + f62;
    c.b += f23 + f43 + f53 + f63;

    c = c * 1.3 - length(uvd) * .05;
    c += f0;

    return c;
}

float sdStar(float2 p, in float r, in int n, in float m)
{
    // these 4 lines can be precomputed for a given shape
    float an = 3.141593 / float(n);
    float en = 3.141593 / m;
    float2  acs = float2(cos(an), sin(an));
    //float2  ecs = float2(cos(en), sin(en)); // ecs=vec2(0,1) and simplify, for regular polygon,
    float2  ecs = float2(0.0f, 1.0f); // ecs=vec2(0,1) and simplify, for regular polygon,

    // reduce to first sector
    float bn = fmod(atan2(p.x, p.y), 2.0 * an) - an;
    p = length(p) * float2(cos(bn), abs(sin(bn)));

    // line sdf
    p -= r * acs;
    p += ecs * clamp(-dot(p, ecs), 0.0, r * acs.y / ecs.y);

    //return length(p) * sign(p.x);
    return length(p) * sign(p.x);
}

float4 Interpolation_C2_InterpAndDeriv(float2 x) { return x.xyxy * x.xyxy * (x.xyxy * (x.xyxy * (x.xyxy * float2(6.0f, 0.0f).xxyy + float2(-15.0f, 30.0f).xxyy) + float2(10.0f, -60.0f).xxyy) + float2(0.0f, 30.0f).xxyy); }
float2 Interpolation_C2_InterpAndDeriv(float x) { return x * x * (x * (x * (x * float2(6.0f, 0.0f) + float2(-15.0f, 30.0f)) + float2(10.0f, -60.0f)) + float2(0.0f, 30.0f)); }

void NoiseHash2D(float2 gridcell, out float4 hash_0, out float4 hash_1)
{
    float2 kOffset = float2(26.0f, 161.0f);
    float kDomain = 71.0f;
    float2 kLargeFloats = 1.0f / float2(951.135664f, 642.949883f);

    float4 P = float4(gridcell.xy, gridcell.xy + 1.0f);
    P = P - floor(P * (1.0f / kDomain)) * kDomain;
    P += kOffset.xyxy;
    P *= P;
    P = P.xzxz * P.yyww;
    hash_0 = frac(P * kLargeFloats.x);
    hash_1 = frac(P * kLargeFloats.y);
}

float2 GeneratePerlinNoise1D(float coordinate)
{
    // establish our grid cell and unit position
    float2 i = floor(float2(coordinate, 0.0f));
    float4 f_fmin1 = float2(coordinate, 0.0f).xyxy - float4(i, i + 1.0f);

    // calculate the hash
    float4 hash_x, hash_y;
    NoiseHash2D(i, hash_x, hash_y);

    // calculate the gradient results
    float4 grad_x = hash_x - 0.49999f;
    float4 grad_y = hash_y - 0.49999f;
    float4 norm = rsqrt(grad_x * grad_x + grad_y * grad_y);
    grad_x *= norm;
    grad_y *= norm;
    float4 dotval = (grad_x * f_fmin1.xzxz + grad_y * f_fmin1.yyww);

    // convert our data to a more parallel format
    float2 dotval0_grad0 = float2(dotval.x, grad_x.x);
    float2 dotval1_grad1 = float2(dotval.y, grad_x.y);
    float2 dotval2_grad2 = float2(dotval.z, grad_x.z);
    float2 dotval3_grad3 = float2(dotval.w, grad_x.w);

    // evaluate common constants
    float2 k0_gk0 = dotval1_grad1 - dotval0_grad0;
    float2 k1_gk1 = dotval2_grad2 - dotval0_grad0;
    float2 k2_gk2 = dotval3_grad3 - dotval2_grad2 - k0_gk0;

    // C2 Interpolation
    float4 blend = Interpolation_C2_InterpAndDeriv(f_fmin1.xy);

    // calculate final noise + deriv
    float2 results = dotval0_grad0
        + blend.x * k0_gk0
        + blend.y * (k1_gk1 + blend.x * k2_gk2);

    results.y += blend.z * (k0_gk0.x + blend.y * k2_gk2.x);

    return results * 2.0f;  // scale to -1.0 -> 1.0 range  *= 1.0/sqrt(0.25)
}

float3 GeneratePerlinNoise2D(float2 coordinate)
{
    // establish our grid cell and unit position
    float2 i = floor(coordinate);
    float4 f_fmin1 = coordinate.xyxy - float4(i, i + 1.0f);

    // calculate the hash
    float4 hash_x, hash_y;
    NoiseHash2D(i, hash_x, hash_y);

    // calculate the gradient results
    float4 grad_x = hash_x - 0.49999f;
    float4 grad_y = hash_y - 0.49999f;
    float4 norm = rsqrt(grad_x * grad_x + grad_y * grad_y);
    grad_x *= norm;
    grad_y *= norm;
    float4 dotval = (grad_x * f_fmin1.xzxz + grad_y * f_fmin1.yyww);

    // convert our data to a more parallel format
    float3 dotval0_grad0 = float3(dotval.x, grad_x.x, grad_y.x);
    float3 dotval1_grad1 = float3(dotval.y, grad_x.y, grad_y.y);
    float3 dotval2_grad2 = float3(dotval.z, grad_x.z, grad_y.z);
    float3 dotval3_grad3 = float3(dotval.w, grad_x.w, grad_y.w);

    // evaluate common constants
    float3 k0_gk0 = dotval1_grad1 - dotval0_grad0;
    float3 k1_gk1 = dotval2_grad2 - dotval0_grad0;
    float3 k2_gk2 = dotval3_grad3 - dotval2_grad2 - k0_gk0;

    // C2 Interpolation
    float4 blend = Interpolation_C2_InterpAndDeriv(f_fmin1.xy);

    // calculate final noise + deriv
    float3 results = dotval0_grad0
        + blend.x * k0_gk0
        + blend.y * (k1_gk1 + blend.x * k2_gk2);

    results.yz += blend.zw * (float2(k0_gk0.x, k1_gk1.x) + blend.yx * k2_gk2.xx);

    return results * 1.4142135623730950488016887242097f;  // scale to -1.0 -> 1.0 range  *= 1.0/sqrt(0.5)
}

float4 ComputeShimmer(float2 uv)
{
    float2 v = (uv - 0.5f) * 2.0f;

    //float sdf = saturate(lensflare(v, 0.5f*_ScreenPos + 0.5f));
    //float sdf = saturate(lensflare(v, 0.0f));
    //float sdf = saturate(-lensflare(v, 0.0f));

    //float sdf = saturate(sdStar(v, _FlareEdgeOffset, 10.0f, 5.0f));

    //sdf = sdf * (1.0f - sdf) / (sdf + 1e-6f);

    //float sdf = lensflare(v, 0).x;

    float ang  = atan2(v.y, v.x);
    float cos0 = cos(ang);
    float sin0 = sin(ang);
    //float t = saturate((ang + 3.141593) / (2.0f * 3.141593));
    float t = saturate((ang + 3.141593) / (2.0f * 3.141593));

    //float noise = GeneratePerlinNoise1D(_FlareSDFFrequency * t * InitRandom(_CosTime * 0.5f + 0.5f)) * 0.5f + 0*0.5f;
    float noise = GeneratePerlinNoise2D(_FlareSDFFrequency * t).x * 0.5f + 0.5f;
    float coef = saturate(noise + _FlareEdgeOffset);
    //float coef = _FlareEdgeOffset * (sin(21.125f * (2.0f * 3.141593 * t))*0.5f);

    //float sdf = length(v - float2(cos0, sin0) * coef) - _FlareEdgeOffset * coef;
    float sdf = length(v * coef) - _FlareEdgeOffset;

#if FLARE_INVERSE_SDF
    sdf = saturate(-sdf);
    sdf = sdf * (1.0f - sdf) / (sdf + 1e-6f);
#else
    sdf = saturate(-sdf);
#endif

    return pow(sdf, _FlareFalloff);
}

float4 GetFlareShape(float2 uv)
{
#if FLARE_GLOW
    return ComputeGlow(uv);
#elif FLARE_IRIS
    return ComputeIris(uv);
#elif FLARE_SHIMMER
    return ComputeShimmer(uv);
#else
    return tex2D(_FlareTex, uv);
#endif
}

