#ifndef UNIVERSAL_SSAO_INCLUDED
#define UNIVERSAL_SSAO_INCLUDED

#define SOURCE_DEPTH

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"

#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
TEXTURE2D_ARRAY_FLOAT(_CameraDepthTexture);
#else
TEXTURE2D_FLOAT(_CameraDepthTexture);
#endif

TEXTURE2D(_MainTex);
TEXTURE2D(_TempTarget);
TEXTURE2D(_TempTarget2);
TEXTURE2D(_ScreenSpaceAOTexture);
TEXTURE2D(_CameraGBufferTexture2);
TEXTURE2D(_CameraDepthNormalsTexture);

SAMPLER(sampler_MainTex);
SAMPLER(sampler_TempTarget);
SAMPLER(sampler_TempTarget2);
SAMPLER(sampler_ScreenSpaceAOTexture);
SAMPLER(sampler_CameraDepthTexture);
SAMPLER(sampler_CameraDepthNormalsTexture);
SAMPLER(sampler_CameraGBufferTexture2);


#define SAMPLE_DEPTH_AO(uv) SAMPLE_DEPTH_TEXTURE(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, uv).r;

//float4 _ScreenParams;
float4x4 ProjectionMatrix;
float4 _MainTex_TexelSize;
float4 _CameraDepthTexture_TexelSize;
float4 _ScreenSpaceAOTexture_TexelSize;

// SSAO Settings
half _SSAO_Intensity;
half _SSAO_Radius;
int _SSAO_Samples;
float _SSAO_DownScale;

// Constants
#define EPSILON         1.0e-4

// Other parameters
#define INTENSITY _SSAO_Intensity
#define RADIUS _SSAO_Radius
#define DOWNSAMPLE _SSAO_DownScale
#define SAMPLE_COUNT _SSAO_Samples



// --------
// Options for further customization
// --------

// By default, a 5-tap Gaussian with the linear sampling technique is used
// in the bilateral noise filter. It can be replaced with a 7-tap Gaussian
// with adaptive sampling by enabling the macro below. Although the
// differences are not noticeable in most cases, it may provide preferable
// results with some special usage (e.g. NPR without textureing).
//#define BLUR_HIGH_QUALITY

// By default, a fixed sampling pattern is used in the AO estimator. Although
// this gives preferable results in most cases, a completely random sampling
// pattern could give aesthetically better results. Disable the macro below
// to use such a random pattern instead of the fixed one.
#define FIX_SAMPLING_PATTERN

// The SampleNormal function normalizes samples from G-buffer because
// they're possibly unnormalized. We can eliminate this if it can be said
// that there is no wrong shader that outputs unnormalized normals.
// #define VALIDATE_NORMALS

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

// Gamma encoding (only needed in gamma lighting mode)
half EncodeAO(half x)
{
    #if UNITY_COLORSPACE_GAMMA
        return 1.0 - max(LinearToSRGB(1.0 - saturate(x)), 0.0);
    #else
        return x;
    #endif
}

// Accessors for packed AO/normal buffer
half4 PackAONormal(half ao, half3 n)
{
    return half4(ao, n * 0.5 + 0.5);
}

half GetPackedAO(half4 p)
{
    return p.r;
}

half3 GetPackedNormal(half4 p)
{
    return p.gba * 2.0 - 1.0;
}

// Trigonometric function utility
float2 CosSin(float theta)
{
    float sn, cs;
    sincos(theta, sn, cs);
    return float2(cs, sn);
}

// Pseudo random number generator with 2D coordinates
float UVRandom(float u, float v)
{
    float f = dot(float2(12.9898, 78.233), float2(u, v));
    return frac(43758.5453 * sin(f));
}

// Check if the camera is perspective.
// (returns 1.0 when orthographic)
float CheckPerspective(float x)
{
    return lerp(x, 1.0, unity_OrthoParams.w);
}

// Reconstruct view-space position from UV and depth.
// p11_22 = (unity_CameraProjection._11, unity_CameraProjection._22)
// p13_31 = (unity_CameraProjection._13, unity_CameraProjection._23)
float3 ReconstructViewPos(float2 uv, float depth, float2 p11_22, float2 p13_31)
{
    return float3((uv * 2.0 - 1.0 - p13_31) / p11_22 * CheckPerspective(depth), depth);
}

// Interleaved gradient function from Jimenez 2014
// http://www.iryoku.com/next-generation-post-processing-in-call-of-duty-advanced-warfare
float GradientNoise(float2 uv)
{
    uv = floor(uv * _ScreenParams.xy);
    float f = dot(float2(0.06711056, 0.00583715), uv);
    return frac(52.9829189 * frac(f));
}

// Sample point picker
float3 PickSamplePoint(float2 uv, float index)
{
    // Uniformaly distributed points on a unit sphere
    // http://mathworld.wolfram.com/SpherePointPicking.html
#if defined(FIX_SAMPLING_PATTERN)
    float gn = GradientNoise(uv * DOWNSAMPLE);
    // FIXEME: This was added to avoid a NVIDIA driver issue.
    //                                   vvvvvvvvvvvv
    float u = frac(UVRandom(0.0, index + uv.x * 1e-10) + gn) * 2.0 - 1.0;
    float theta = (UVRandom(1.0, index + uv.x * 1e-10) + gn) * TWO_PI;
#else
    float u = UVRandom(uv.x + _Time.x, uv.y + index) * 2.0 - 1.0;
    float theta = UVRandom(-uv.x - _Time.x, uv.y + index) * TWO_PI;
#endif
    float3 v = float3(CosSin(theta) * sqrt(1.0 - u * u), u);
    // Make them distributed between [0, _Radius]
    float l = sqrt((index + 1.0) / SAMPLE_COUNT) * RADIUS;
    return v * l;
}

// Normal vector comparer (for geometry-aware weighting)
half CompareNormal(half3 d1, half3 d2)
{
    return smoothstep(kGeometryCoeff, 1.0, dot(d1, d2));
}

// Boundary check for depth sampler
// (returns a very large value if it lies out of bounds)
float CheckBounds(float2 uv, float linear01Depth)
{
    float ob = any(uv < 0.0) + any(uv > 1.0);
    
    #if defined(UNITY_REVERSED_Z)
        ob += lerp(0.0, 1.0, step(linear01Depth, 0.00001));
    #else
        ob += lerp(0.0, 1.0, step(0.99999, linear01Depth));
    #endif
    
    return ob * 1e8;
}


float3 reconstructPosition(in float2 uv, in float z, in float4x4 InvVP)
{
    float x = uv.x * 2.0f - 1.0f;
    float y = (1.0 - uv.y) * 2.0f - 1.0f;

    float4 position_s = float4(x, y, z, 1.0f);
    float4 position_v = mul(InvVP, position_s);
    return position_v.xyz / position_v.w;
}

// Try reconstructing normal accurately from depth buffer.
// input DepthBuffer: stores linearized depth in range (0, 1).
// 5 taps on each direction: | z | x | * | y | w |, '*' denotes the center sample.
// https://atyuwen.github.io/posts/normal-reconstruction/
float3 ReconstructNormal(float2 spos)
{
    float2 stc = spos / _ScreenParams.xy;
    float depth = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, stc).x;

/*#if defined(SOURCE_DEPTH_NORMALS)
    float depth = UnpackFloatFromR8G8(_CameraDepthNormalsTexture.Sample(sampler_CameraDepthNormalsTexture, stc));
    float3 pos = ComputeWorldSpacePosition(stc, depth, UNITY_MATRIX_I_VP);
    return normalize(cross(ddx(pos), ddy(pos)));
#else
    float3 pos = ComputeWorldSpacePosition(stc, depth, UNITY_MATRIX_I_VP);
    return normalize(cross(ddy(pos), ddx(pos)));
#endif*/

    float2 hXCoord = stc - float2(1.0 / _ScreenParams.x, 0.0                  );
    float2 hYCoord = stc + float2(1.0 / _ScreenParams.x, 0.0                  );
    float2 hZCoord = stc - float2(2.0 / _ScreenParams.x, 0.0                  );
    float2 hWCoord = stc + float2(2.0 / _ScreenParams.x, 0.0                  );
    float2 vXCoord = stc - float2(0.0,                   1.0 / _ScreenParams.y);
    float2 vYCoord = stc + float2(0.0,                   1.0 / _ScreenParams.y);
    float2 vZCoord = stc - float2(0.0,                   2.0 / _ScreenParams.y);
    float2 vWCoord = stc + float2(0.0,                   2.0 / _ScreenParams.y);

    float4 H;
    H.x = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, hXCoord).x;
    H.y = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, hYCoord).x;
    H.z = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, hZCoord).x;
    H.w = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, hWCoord).x;

    float4 V;
    V.x = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, vXCoord).x;
    V.y = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, vYCoord).x;
    V.z = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, vZCoord).x;
    V.w = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, vWCoord).x;

    float3 a, b;

    //float2 he = abs(H.xy * H.zw * rcp(2 * H.zw - H.xy) - depth);
    float2 he = abs(2.0 * H.x - H.z) - depth;
    if (he.x > he.y)
    {
        //hDeriv = Calculate horizontal derivative of world position from taps | z | x |
        a = ComputeWorldSpacePosition(hZCoord, H.z, UNITY_MATRIX_I_VP);
        b = ComputeWorldSpacePosition(hXCoord, H.x, UNITY_MATRIX_I_VP);
    }
    else
    {
        //hDeriv = Calculate horizontal derivative of world position from taps | y | w |
        a = ComputeWorldSpacePosition(hYCoord, H.y, UNITY_MATRIX_I_VP);
        b = ComputeWorldSpacePosition(hWCoord, H.w, UNITY_MATRIX_I_VP);
    }
    float3 hDeriv = ddx(lerp(a, b, 0.5));


    //float2 ve = abs(V.xy * V.zw * rcp(2 * V.zw - V.xy) - depth);
    float2 ve = abs(2.0 * V.x - V.z) - depth;
    if (ve.x > ve.y)
    {
        //vDeriv = Calculate vertical derivative of world position from taps | z | x |
        a = ComputeWorldSpacePosition(vZCoord, V.z, UNITY_MATRIX_I_VP);
        b = ComputeWorldSpacePosition(vXCoord, V.x, UNITY_MATRIX_I_VP);
    }
    else
    {
        //vDeriv = Calculate vertical derivative of world position from taps | y | w |
        a = ComputeWorldSpacePosition(vYCoord, V.y, UNITY_MATRIX_I_VP);
        b = ComputeWorldSpacePosition(vWCoord, V.w, UNITY_MATRIX_I_VP);
    }
    float3 vDeriv = ddy(lerp(a, b, 0.5));


    return normalize(cross(vDeriv, hDeriv));
}

// Calculates the normals from a texture sample
float3 CalculateNormalFromTextureSample(float4 textureValue, half2 positionCS)
{
    // Deferred...
    #if defined(SOURCE_GBUFFER)
        float3 normal = textureValue.xyz;
        normal = normal * 2 - any(normal); // gets (0,0,0) when norm == 0
        normal = mul((float3x3)unity_WorldToCamera, normal);

        #if defined(VALIDATE_NORMALS)
            normal = normalize(normal);
        #endif

        return normal;

    // Forward with DepthNormals texture...
    #elif defined(SOURCE_DEPTH_NORMALS)
        return UnpackNormalOctRectEncode(textureValue.xy) * float3(1.0, 1.0, -1.0);

    // Forward with Depth texture
    #else
        return ReconstructNormal(positionCS.xy);
    #endif
}

float CalculateDepthFromTextureDepthValue(float depth, float2 uv)
{
    float linear01Depth = Linear01Depth(depth, _ZBufferParams);
    float linearEyeDepth = LinearEyeDepth(depth, _ZBufferParams);
    return linearEyeDepth + CheckBounds(uv, linear01Depth);
}

// Calculates the depth from a texture sample
float CalculateDepthFromTextureSample(float4 textureValue, float2 uv)
{
    // Deferred
    #if defined(SOURCE_GBUFFER)
        float depth = UnpackFloatFromR8G8(textureValue.zw);

    // Forward with DepthNormals texture...
    #elif defined(SOURCE_DEPTH_NORMALS)
        float depth = UnpackFloatFromR8G8(textureValue.zw);

    // Forward with Depth texture
    #else
        float depth = textureValue.x;
    #endif

    return CalculateDepthFromTextureDepthValue(depth, uv);
}

float4 SampleTexture(float2 uv)
{
    // Deferred
    #if defined(SOURCE_GBUFFER)
        return SAMPLE_TEXTURE2D(_CameraGBufferTexture2, sampler_CameraGBufferTexture2, uv);

    // Forward with DepthNormals texture...
    #elif defined(SOURCE_DEPTH_NORMALS)
        return SAMPLE_TEXTURE2D_LOD(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, uv, 0);

    // Forward with Depth texture
    #else
        return SAMPLE_TEXTURE2D_LOD(_CameraDepthTexture, sampler_CameraDepthTexture, uv, 0);
    #endif
}

// Samples a texture for depth
float SampleDepth(float2 uv)
{
    float4 cdn = SampleTexture(uv);
    return CalculateDepthFromTextureSample(cdn, uv);
}

// Samples a texture for normals
float3 SampleNormal(float2 uv, float2 positionCS)
{
    // Deferred
    #if defined(SOURCE_GBUFFER)
        return CalculateNormalFromTextureSample(SampleTexture(uv), float2(0.0,0.0));

    // Forward with DepthNormals texture...
    #elif defined(SOURCE_DEPTH_NORMALS)
        return CalculateNormalFromTextureSample(SampleTexture(uv), float2(0.0, 0.0));

    // Forward with Depth texture
    #else
        return ReconstructNormal(positionCS);
    #endif
}

//
// Distance-based AO estimator based on Morgan 2011
// "Alchemy screen-space ambient obscurance algorithm"
// http://graphics.cs.williams.edu/papers/AlchemyHPG11/
//
float4 SSAO(Varyings input) : SV_Target
{
    float2 uv = input.uv.xy;
    float2 positionCS = input.positionCS.xy;

    // Parameters used in coordinate conversion
    float3x3 proj = (float3x3)unity_CameraProjection;
    float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
    float2 p13_31 = float2(unity_CameraProjection._13, unity_CameraProjection._23);

    // View space normal and depth
    float4 textureVal = SampleTexture(uv);
    float3 norm_o = CalculateNormalFromTextureSample(textureVal, positionCS);
    float depth_o = CalculateDepthFromTextureSample(textureVal, uv);

    /*return float4(norm_o, 0.0);
#if defined(SOURCE_DEPTH_NORMALS)
    return UnpackFloatFromR8G8(SampleTexture(uv).zw);
#else
    return SAMPLE_TEXTURE2D_LOD(_CameraDepthTexture, sampler_CameraDepthTexture, uv, 0).x;
#endif
    //return float4(depth_o, 0,0, 0.0);
*/

    // Reconstruct the view-space position.
    float3 vpos_o = ReconstructViewPos(uv, depth_o, p11_22, p13_31);
    
    float ao = 0.0;
    for (int s = 0; s < int(SAMPLE_COUNT); s++)
    {
        #if defined(SHADER_API_D3D11)
            // This 'floor(1.0001 * s)' operation is needed to avoid a DX11 NVidia shader issue.
            s = floor(1.0001 * s);
        #endif

        // Sample point
        float3 v_s1 = PickSamplePoint(uv, s);

        v_s1 = faceforward(v_s1, -norm_o, v_s1);
        float3 vpos_s1 = vpos_o + v_s1;

        // Reproject the sample point
        float3 spos_s1 = mul(proj, vpos_s1);
        float2 uv_s1_01 = (spos_s1.xy / CheckPerspective(vpos_s1.z) + 1.0) * 0.5;

        // Depth at the sample point
        float depth_s1 = SampleDepth(uv_s1_01);

        // Relative position of the sample point
        float3 vpos_s2 = ReconstructViewPos(uv_s1_01, depth_s1, p11_22, p13_31);
        float3 v_s2 = vpos_s2 - vpos_o;

        // Estimate the obscurance value
        float a1 = max(dot(v_s2, norm_o) - kBeta * depth_o, 0.0);
        float a2 = dot(v_s2, v_s2) + EPSILON;
        ao += a1 / a2;
    }

    // Intensity normalization
    ao *= RADIUS; 

    // Apply contrast
    ao = PositivePow(ao * INTENSITY / SAMPLE_COUNT, kContrast);

    return PackAONormal(ao, norm_o);
}

// Geometry-aware separable bilateral filter
float4 FragBlur(Varyings input) : SV_Target
{
    float2 uv = input.uv.xy;
    float2 positionCS = input.positionCS.xy;

    #if defined(BLUR_HORIZONTAL)
        // Horizontal pass: Always use 2 texels interval to match to the dither pattern.
        float2 delta = float2(_MainTex_TexelSize.x * 2.0, 0.0);
    #else
        // Vertical pass: Apply _Downsample to match to the dither pattern in the original occlusion buffer.
        float2 delta = float2(0.0, _MainTex_TexelSize.y / DOWNSAMPLE * 2.0);
    #endif

    #if defined(BLUR_HIGH_QUALITY)
        // High quality 7-tap Gaussian with adaptive sampling
        half4 p0  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv/*i.texcoordStereo*/);
        half4 p1a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(uv - delta));
        half4 p1b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(uv + delta));
        half4 p2a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(uv - delta * 2.0));
        half4 p2b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(uv + delta * 2.0));
        half4 p3a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(uv - delta * 3.2307692308));
        half4 p3b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(uv + delta * 3.2307692308));

        #if defined(BLUR_SAMPLE_CENTER_NORMAL)
            half3 n0 = SampleNormal(uv/*i.texcoordStereo*/, positionCS);
        #else
            half3 n0 = GetPackedNormal(p0);
        #endif

        half w0 = 0.37004405286;
        half w1a = CompareNormal(n0, GetPackedNormal(p1a)) * 0.31718061674;
        half w1b = CompareNormal(n0, GetPackedNormal(p1b)) * 0.31718061674;
        half w2a = CompareNormal(n0, GetPackedNormal(p2a)) * 0.19823788546;
        half w2b = CompareNormal(n0, GetPackedNormal(p2b)) * 0.19823788546;
        half w3a = CompareNormal(n0, GetPackedNormal(p3a)) * 0.11453744493;
        half w3b = CompareNormal(n0, GetPackedNormal(p3b)) * 0.11453744493;

        half s;
        s = GetPackedAO(p0)  * w0;
        s += GetPackedAO(p1a) * w1a;
        s += GetPackedAO(p1b) * w1b;
        s += GetPackedAO(p2a) * w2a;
        s += GetPackedAO(p2b) * w2b;
        s += GetPackedAO(p3a) * w3a;
        s += GetPackedAO(p3b) * w3b;

        s /= w0 + w1a + w1b + w2a + w2b + w3a + w3b;
    #else
        // Fater 5-tap Gaussian with linear sampling
        half4 p0  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv/*i.texcoordStereo*/);
        half4 p1a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(uv - delta * 1.3846153846));
        half4 p1b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(uv + delta * 1.3846153846));
        half4 p2a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(uv - delta * 3.2307692308));
        half4 p2b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(uv + delta * 3.2307692308));

        #if defined(BLUR_SAMPLE_CENTER_NORMAL)
            half3 n0 = SampleNormal(uv/*i.texcoordStereo*/, positionCS);
        #else
            half3 n0 = GetPackedNormal(p0);
        #endif

        half w0 = 0.2270270270;
        half w1a = CompareNormal(n0, GetPackedNormal(p1a)) * 0.3162162162;
        half w1b = CompareNormal(n0, GetPackedNormal(p1b)) * 0.3162162162;
        half w2a = CompareNormal(n0, GetPackedNormal(p2a)) * 0.0702702703;
        half w2b = CompareNormal(n0, GetPackedNormal(p2b)) * 0.0702702703;

        half s;
        s = GetPackedAO(p0)  * w0;
        s += GetPackedAO(p1a) * w1a;
        s += GetPackedAO(p1b) * w1b;
        s += GetPackedAO(p2a) * w2a;
        s += GetPackedAO(p2b) * w2b;

        s /= w0 + w1a + w1b + w2a + w2b;
#endif

    return PackAONormal(s, n0);
}


// Geometry-aware bilateral filter (single pass/small kernel)
half BlurSmall(TEXTURE2D_PARAM(tex, samp), float2 uv, float2 delta)
{
    half4 p0 = SAMPLE_TEXTURE2D(tex, samp, UnityStereoTransformScreenSpaceTex(uv));
    half4 p1 = SAMPLE_TEXTURE2D(tex, samp, UnityStereoTransformScreenSpaceTex(uv + float2(-delta.x, -delta.y)));
    half4 p2 = SAMPLE_TEXTURE2D(tex, samp, UnityStereoTransformScreenSpaceTex(uv + float2(delta.x, -delta.y)));
    half4 p3 = SAMPLE_TEXTURE2D(tex, samp, UnityStereoTransformScreenSpaceTex(uv + float2(-delta.x, delta.y)));
    half4 p4 = SAMPLE_TEXTURE2D(tex, samp, UnityStereoTransformScreenSpaceTex(uv + float2(delta.x, delta.y)));

    half3 n0 = GetPackedNormal(p0);

    half w0 = 1.0;
    half w1 = CompareNormal(n0, GetPackedNormal(p1));
    half w2 = CompareNormal(n0, GetPackedNormal(p2));
    half w3 = CompareNormal(n0, GetPackedNormal(p3));
    half w4 = CompareNormal(n0, GetPackedNormal(p4));

    half s;
    s = GetPackedAO(p0) * w0;
    s += GetPackedAO(p1) * w1;
    s += GetPackedAO(p2) * w2;
    s += GetPackedAO(p3) * w3;
    s += GetPackedAO(p4) * w4;

    return s / (w0 + w1 + w2 + w3 + w4);
}

// Final composition shader
float4 FragComposition(Varyings input) : SV_Target
{
    float2 uv = input.uv.xy;

    float2 delta = _MainTex_TexelSize.xy / DOWNSAMPLE;
    half ao = BlurSmall(TEXTURE2D_ARGS(_MainTex, sampler_MainTex), uv, delta);

    return 1.0 - EncodeAO(ao);
}


#if !SHADER_API_GLES // Excluding the MRT pass under GLES2
    struct CompositionOutput
    {
        half4 gbuffer0 : SV_Target0;
        half4 gbuffer3 : SV_Target1;
    };

    CompositionOutput FragCompositionGBuffer(Varyings i)
    {
        // Workaround: _ScreenSpaceAOTexture_Texelsize hasn't been set properly
        // for some reasons. Use _ScreenParams instead.
        float2 delta = (_ScreenParams.zw - 1.0) / DOWNSAMPLE;
        half ao = BlurSmall(TEXTURE2D_ARGS(_ScreenSpaceAOTexture, sampler_ScreenSpaceAOTexture), i.uv, delta);

        CompositionOutput o;
        o.gbuffer0 = half4(0.0, 0.0, 0.0, ao);
        o.gbuffer3 = half4((half3)EncodeAO(ao), 0.0);
        return o;
    }
#else
    float4 FragCompositionGBuffer(Varyings i) : SV_Target
    {
        return (0.0).xxxx;
    }
#endif

float4 FragDebugOverlay(Varyings i) : SV_Target
{
    float2 delta = _ScreenSpaceAOTexture_TexelSize.xy / DOWNSAMPLE;
    half ao = BlurSmall(TEXTURE2D_ARGS(_ScreenSpaceAOTexture, sampler_ScreenSpaceAOTexture), i.uv, delta);
    ao = EncodeAO(ao);
    return float4(1.0 - ao.xxx, 1.0);
}

#endif //UNIVERSAL_SSAO_INCLUDED
