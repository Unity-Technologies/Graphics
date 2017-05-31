#ifndef UNITY_HDRENDERPIPELINE_AMBIENTOCCLUSION_COMMON
#define UNITY_HDRENDERPIPELINE_AMBIENTOCCLUSION_COMMON

#include "../../../../ShaderLibrary/Common.hlsl"

#include "../../../ShaderConfig.cs.hlsl"
#include "../../../ShaderVariables.hlsl"
#define UNITY_MATERIAL_LIT // Needs to be defined before including Material.hlsl
#include "../../../Material/Material.hlsl"

TEXTURE2D(_CameraDepthTexture);
SAMPLER2D(sampler_CameraDepthTexture);

DECLARE_GBUFFER_TEXTURE(_GBufferTexture);

// The constant below determines the contrast of occlusion. This allows
// users to control over/under occlusion. At the moment, this is not exposed
// to the editor because it's rarely useful.
static const float kContrast = 0.6;

// The constant below controls the geometry-awareness of the bilateral
// filter. The higher value, the more sensitive it is.
static const float kGeometryCoeff = 0.8;

// The constants below are used in the AO estimator. Beta is mainly used
// for suppressing self-shadowing noise, and Epsilon is used to prevent
// calculation underflow. See the paper (Morgan 2011 http://goo.gl/2iz3P)
// for further details of these constants.
static const float kBeta = 0.002;

// A small value used for avoiding self-occlusion.
static const float kEpsilon = 1e-6;

float2 SinCos(float theta)
{
    float sn, cs;
    sincos(theta, sn, cs);
    return float2(sn, cs);
}

// Pseudo random number generator with 2D coordinates
float UVRandom(float u, float v)
{
    float f = dot(float2(12.9898, 78.233), float2(u, v));
    return frac(43758.5453 * sin(f));
}

// Interleaved gradient function from Jimenez 2014 http://goo.gl/eomGso
float GradientNoise(float2 uv)
{
    uv = floor(uv * _ScreenParams.xy);
    float f = dot(float2(0.06711056, 0.00583715), uv);
    return frac(52.9829189 * frac(f));
}

// Boundary check for depth sampler
// (returns a very large value if it lies out of bounds)
float CheckBounds(float2 uv, float d)
{
    float ob = any(uv < 0) + any(uv > 1);
#if defined(UNITY_REVERSED_Z)
    ob += (d <= 0.00001);
#else
    ob += (d >= 0.99999);
#endif
    return ob * 1e8;
}

// AO/normal packed format conversion
half4 PackAONormal(half ao, half3 n)
{
    return half4(ao, n * 0.5 + 0.5);
}

half GetPackedAO(half4 p)
{
    return p.x;
}

half3 GetPackedNormal(half4 p)
{
    return p.yzw * 2.0 - 1.0;
}

// Depth/normal sampling
float SampleDepth(uint2 unPositionSS)
{
    float z = LOAD_TEXTURE2D(_CameraDepthTexture, unPositionSS).x;
    return LinearEyeDepth(z, _ZBufferParams) + CheckBounds(float2(0.5, 0.5), z); // TODO: We should use the stencil to not affect the sky and save CheckBounds cost - also uv can't be out of bounds on xy... so put a constant here
}

half3 SampleNormal(BSDFData bsdfData)
{
    return mul((float3x3)unity_WorldToCamera, bsdfData.normalWS);
}

// Normal vector comparer (for geometry-aware weighting)
half CompareNormal(half3 d1, half3 d2)
{
    return smoothstep(kGeometryCoeff, 1.0, dot(d1, d2));
}

// TODO: Test. We may need to use full matrix here to reconver VS position as it may not work in case of oblique projection (planar reflection)

// Reconstruct view-space position from UV and depth.
// p11_22 = (unity_CameraProjection._11, unity_CameraProjection._22)
// p13_31 = (unity_CameraProjection._13, unity_CameraProjection._23)
float3 ReconstructViewPos(float2 uv, float depth, float2 p11_22, float2 p13_31)
{
    return float3((uv * 2.0 - 1.0 - p13_31) / p11_22 * depth, depth);
}

// Default vertex shader
struct Attributes
{
    uint vertexID : SV_VertexID;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
};

Varyings Vert(Attributes input)
{
    Varyings output;
    output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
    return output;
}

#endif // UNITY_HDRENDERPIPELINE_AMBIENTOCCLUSION_COMMON
