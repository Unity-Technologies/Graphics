#ifndef UNITY_HDRENDERPIPELINE_AMBIENTOCCLUSION_COMMON
#define UNITY_HDRENDERPIPELINE_AMBIENTOCCLUSION_COMMON

#include "../../../ShaderLibrary/Common.hlsl"
#include "../../../ShaderLibrary/Packing.hlsl"

// Uniforms given from the camera.
float4x4 unity_WorldToCamera;
float4x4 unity_CameraProjection;
float4 _ZBufferParams;
float4 _ScreenParams;

// GBuffer RT1 (10:10:8/2:2)
// Normal (RG), PerceptualRoughness (B), MaterialID (A).
TEXTURE2D(_CameraGBufferTexture1);
SAMPLER2D(sampler_CameraGBufferTexture1);

TEXTURE2D(_CameraDepthTexture);
SAMPLER2D(sampler_CameraDepthTexture);

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
float SampleDepth(float2 uv)
{
    float z = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv).x;
    return LinearEyeDepth(z, _ZBufferParams) + CheckBounds(uv, z);
}

half3 SampleNormal(float2 uv)
{
    float4 packed = SAMPLE_TEXTURE2D(_CameraGBufferTexture1, sampler_CameraGBufferTexture1, uv);

    float roughness;
    uint index;
    UnpackFloatInt10bit(packed.z, 4.0, roughness, index);

    float3 norm;
#ifdef USE_NORMAL_TETRAHEDRON_ENCODING
    norm = UnpackNormalTetraEncode(packed.xy * 2.0 - 1.0, index);
#else
    packed.xy *= float2((index & 1) ? 1.0 : -1.0, (index & 2) ? 1.0 : -1.0);
    norm = UnpackNormalOctEncode(packed.xy);
#endif

    return mul((float3x3)unity_WorldToCamera, norm);
}

// Normal vector comparer (for geometry-aware weighting)
half CompareNormal(half3 d1, half3 d2)
{
    return smoothstep(kGeometryCoeff, 1.0, dot(d1, d2));
}

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
    float3 vertex : POSITION;
};

struct Varyings
{
    float4 vertex : SV_POSITION;
    float2 texcoord : TEXCOORD0;
};

Varyings Vert(Attributes input)
{
    Varyings output;
    output.vertex = float4(input.vertex.xy, 0.0, 1.0);
    output.texcoord = (input.vertex.xy + 1.0) * 0.5;
#if UNITY_UV_STARTS_AT_TOP
    output.texcoord.y = 1.0 - output.texcoord.y;
#endif
    return output;
}

#endif // UNITY_HDRENDERPIPELINE_AMBIENTOCCLUSION_COMMON
