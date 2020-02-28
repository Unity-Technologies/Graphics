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


struct CoordDepth
{
    float4 uv;
    float d;
};


// Try reconstructing normal accurately from depth buffer.
// https://wickedengine.net/2019/09/22/improved-normal-reconstruction-from-depth/
float3 ReconstructNormalSimple(float4 uv)
{
    float2 texSize = _ScreenSpaceAOTexture_TexelSize.xy;

    CoordDepth c = (CoordDepth)0; // Center
    CoordDepth l = (CoordDepth)0; // Left
    CoordDepth r = (CoordDepth)0; // Right
    CoordDepth u = (CoordDepth)0; // Up
    CoordDepth d = (CoordDepth)0; // Down


    float flipped = _ProjectionParams.x;
    float flipped2 = 1;// _ProjectionParams.x;

    // Calculate the center depth and then left, right, up and down pixels
    c.uv = uv;                                                       
    l.uv = c.uv - float4(texSize.x*flipped2,        0.0, texSize.x*flipped2,       0.0);
    r.uv = c.uv + float4(texSize.x*flipped2,        0.0, texSize.x*flipped2,       0.0);
    u.uv = c.uv + float4(      0.0,  texSize.y*flipped,       0.0, texSize.y*flipped);
    d.uv = c.uv - float4(      0.0,  texSize.y*flipped,       0.0, texSize.y*flipped);

    c.d = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(c.uv.xy));
    l.d = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(l.uv.xy));
    r.d = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(r.uv.xy));
    u.d = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(u.uv.xy));
    d.d = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(d.uv.xy));

    // Determine the closest horizontal and vertical pixels...
    // horizontal: left = 0.0 right = 1.0
    // vertical  : down = 0.0    up = 1.0
    const uint closest_horizontal = step(abs(r.d - c.d), abs(l.d - c.d));
    const uint closest_vertical   = step(abs(u.d - c.d), abs(d.d - c.d));

    // Calculate the triangle to use based on the closest horizontal and vertical depths
    // Using lerps instead of if statements.
    // h == 0.0 && v == 0.0: p1 = left,  p2 = down
    // h == 0.0 && v == 1.0: p1 = up,    p2 = left
    // h == 1.0 && v == 0.0: p1 = down,  p2 = right
    // h == 1.0 && v == 1.0: p1 = right, p2 = up
    float3 p1 = lerp(lerp(float3(l.uv.zw, l.d), float3(d.uv.zw, d.d), closest_horizontal), lerp(float3(u.uv.zw, u.d), float3(r.uv.zw, r.d), closest_horizontal), closest_vertical);
    float3 p2 = lerp(lerp(float3(d.uv.zw, d.d), float3(r.uv.zw, r.d), closest_horizontal), lerp(float3(l.uv.zw, l.d), float3(u.uv.zw, u.d), closest_horizontal), closest_vertical);

    // Depth ....
    #if UNITY_REVERSED_Z
        c.d = 1.0 - c.d;
        p1.z = 1.0 - p1.z;
        p2.z = 1.0 - p2.z;
    #endif
     
    c.d  = 2.0 * c.d  - 1.0;
    p1.z = 2.0 * p1.z - 1.0;
    p2.z = 2.0 * p2.z - 1.0;

    // Calculate the view space positions...
    float3 P  = ComputeViewSpacePosition(c.uv.zw, c.d, unity_CameraInvProjection); 
    float3 P1 = ComputeViewSpacePosition(p1.xy,  p1.z, unity_CameraInvProjection);
    float3 P2 = ComputeViewSpacePosition(p2.xy,  p2.z, unity_CameraInvProjection);

    // Use the cross product to calculate the normal...
    // OpenGL
    if (flipped > 0)
    {
        return normalize(cross(P2 - P, P1 - P));
    }

    // Direct X
    return normalize(cross(P1 - P, P2 - P)) * float3(-1, 1,- 1);
}
//#define DEBUG_NORMAL


// Try reconstructing normal accurately from depth buffer.
// input DepthBuffer: stores linearized depth in range (0, 1).
// 5 taps on each direction: | z | x | * | y | w |, '*' denotes the center sample.
// https://atyuwen.github.io/posts/normal-reconstruction/
float3 ReconstructNormal(float4 spos)
{
    return ReconstructNormalSimple(spos);

/*
    float3 center = float3(spos / _ScreenParams.xy, 0.0);
    
    //return depth;
    //float3 P = ComputeWorldSpacePosition(stc, depth, UNITY_MATRIX_I_VP);
    //return normalize(cross(ddy(P), ddx(P)));

    float3 l2 = float3(center.xy - float2(2.0 / _ScreenParams.x, 0.0                  ), 0.0);
    float3 l1 = float3(center.xy - float2(1.0 / _ScreenParams.x, 0.0                  ), 0.0);
    float3 r1 = float3(center.xy + float2(1.0 / _ScreenParams.x, 0.0                  ), 0.0);
    float3 r2 = float3(center.xy + float2(2.0 / _ScreenParams.x, 0.0                  ), 0.0);
    float3 u2 = float3(center.xy + float2(0.0,                   2.0 / _ScreenParams.y), 0.0);
    float3 u1 = float3(center.xy + float2(0.0,                   1.0 / _ScreenParams.y), 0.0);
    float3 d1 = float3(center.xy - float2(0.0,                   1.0 / _ScreenParams.y), 0.0);
    float3 d2 = float3(center.xy - float2(0.0,                   2.0 / _ScreenParams.y), 0.0);

    l2.z = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, l2.xy);
    l1.z = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, l1.xy);
    r1.z = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, r1.xy);
    r2.z = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, r2.xy);
    center.z = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, center.xy);
    u2.z = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, u2.xy);
    u1.z = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, u1.xy);
    d1.z = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, d1.xy);
    d2.z = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, d2.xy);


    float3 H = l1.z < (r1.z - EPSILON) ? float3(l1.xy, l1.z) : float3(r1.xy, r1.z);
    float3 V = u1.z < (d1.z - EPSILON) ? float3(u1.xy, u1.z) : float3(d1.xy, d1.z);

    float3 HPos = ComputeWorldSpacePosition(H.xy, H.z, UNITY_MATRIX_I_VP);
    float3 VPos = ComputeWorldSpacePosition(V.xy, V.z, UNITY_MATRIX_I_VP);

    float3 dddd = ddx(HPos);
    float3 bbbb = ddy(HPos);
    return normalize(cross(bbbb, dddd));
    //return normalize(cross(HPos, VPos));

    

            //float2 he = abs(float2(h_x.z, h_y.z) * float2(h_z.z, h_w.z) * rcp(2.0 * float2(h_z.z, h_w.z) - float2(h_x.z, h_y.z)) - depth);
            //return float3(he, 0);
            //he = abs(2.0 * h_x.z - h_y.z) - depth;

            //float2 ve = abs(V.xy * V.zw * rcp(2 * V.zw - V.xy) - depth);
            //float2 ve = abs(float2(v_x.z, v_y.z) * float2(v_z.z, v_w.z) * rcp(2.0 * float2(v_z.z, v_w.z) - float2(v_x.z, v_y.z)) - depth);
            //float2 ve = abs(2.0 * v_x.z - v_y.z) - depth;
            //return float3(ve, 0);

            //float linearXZ = Linear01Depth(_CameraDepthTexture.Sample(sampler_CameraDepthTexture, h_x.xy).x, _ZBufferParams);
            //float linearYZ = Linear01Depth(_CameraDepthTexture.Sample(sampler_CameraDepthTexture, h_x.xy).x, _ZBufferParams);
            //const uint best_Z_horizontal = abs(h_x.z - depth) < abs(h_y.z - depth) ? 2 : 1;
            //const uint best_Z_vertical = abs(v_x.z - depth) < abs(v_y.z - depth) ? 2 : 1;
            //if (best_Z_horizontal == 1)
            //if (best_Z_vertical == 1)

            //float L1 = Linear01Depth(l1.z, _ZBufferParams);
            //float L2 = Linear01Depth(l2.z, _ZBufferParams);
            //float R1 = Linear01Depth(r1.z, _ZBufferParams);
            //float R2 = Linear01Depth(r2.z, _ZBufferParams);
            //float D  = Linear01Depth(center.z, _ZBufferParams);
            //float2 he = float2( abs( (2.0 * L1 - L2) - D), abs( (2.0 * R2 - R1) - D) );

    float3 hDeriv;
    float3 vDeriv;
    float3 a, b;

    // 5 taps on each direction: | z | x | * | y | w |, '*' denotes the center sample.
    //float2 he = float2(abs(2.0 * l2.z - l1.z), abs(2.0 * r2.z - r1.z)) - center.z;
    //float2 ve = float2(abs(2.0 * u2.z - u1.z), abs(2.0 * d2.z - d1.z)) - center.z;

    // If the depth is stored as is (not linearized) there is no need for this kind of interpolation and abs(2 * H.x - H.z) - depth) is sufficient,
    float2 he = float2( abs( (2.0 * l1.z - l2.z) - center.z), abs( (2.0 * r2.z - r1.z) - center.z) );
    float2 ve = float2( abs( (2.0 * u1.z - u2.z) - center.z), abs( (2.0 * d2.z - d1.z) - center.z) );

    //return float3(he, 0.0);
    if (he.x > he.y)
    {
        return float3(1,0,0);
        //hDeriv = Calculate horizontal derivative of world position from taps | z | x |
        if (l2.z > l1.z)
        {
return float3(1,0,0);
            hDeriv = ddx(ComputeWorldSpacePosition(l2.xy, l2.z, UNITY_MATRIX_I_VP));
        }
        else
        {
return float3(0,1,0);
            hDeriv = ddx(ComputeWorldSpacePosition(l1.xy, l1.z, UNITY_MATRIX_I_VP));
        }

        a = ComputeWorldSpacePosition(l2.xy, l2.z, UNITY_MATRIX_I_VP);
        b = ComputeWorldSpacePosition(l1.xy, l1.z, UNITY_MATRIX_I_VP);
    }
    else
    {
        if (r2.z > r1.z)
        {
            hDeriv = ddx(ComputeWorldSpacePosition(r2.xy, r2.z, UNITY_MATRIX_I_VP));
        }
        else
        {
            hDeriv = ddx(ComputeWorldSpacePosition(r1.xy, r1.z, UNITY_MATRIX_I_VP));
        }
        return float3(0,0,0);
        //hDeriv = Calculate horizontal derivative of world position from taps | y | w |
        a = ComputeWorldSpacePosition(r2.xy, r2.z, UNITY_MATRIX_I_VP);
        b = ComputeWorldSpacePosition(r1.xy, r1.z, UNITY_MATRIX_I_VP);
    }
    //hDeriv = ddx(lerp(a, b, 0.50));
    return hDeriv;

    
    if (ve.x > ve.y)
    {
        //return float3(1,0,0);
        //vDeriv = Calculate vertical derivative of world position from taps | z | x |
        a = ComputeWorldSpacePosition(u2.xy, u2.z, UNITY_MATRIX_I_VP);
        b = ComputeWorldSpacePosition(u1.xy, u1.z, UNITY_MATRIX_I_VP);
    }
    else
    {
        //return float3(0,1,0);
        //vDeriv = Calculate vertical derivative of world position from taps | y | w |
        a = ComputeWorldSpacePosition(d2.xy, d2.z, UNITY_MATRIX_I_VP);
        b = ComputeWorldSpacePosition(d1.xy, d1.z, UNITY_MATRIX_I_VP);
    }
    vDeriv = ddy(lerp(a, b, 0.50));

    return normalize(cross(vDeriv, hDeriv));
*/
}

// Calculates the normals from a texture sample
float3 CalculateNormalFromTextureSample(float4 textureValue, float4 positionCS)
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
        return ReconstructNormal(positionCS);
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
float3 SampleNormal(float2 uv, float4 positionCS)
{
    // Deferred
    #if defined(SOURCE_GBUFFER)
        return CalculateNormalFromTextureSample(SampleTexture(uv), float4(0.0,0.0, 0.0, 0.0));

    // Forward with DepthNormals texture...
    #elif defined(SOURCE_DEPTH_NORMALS)
        return CalculateNormalFromTextureSample(SampleTexture(uv), float4(0.0, 0.0, 0.0, 0.0));

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
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = input.uv.xy;
    float4 positionCS = input.positionCS;

    // Parameters used in coordinate conversion
    float3x3 proj = (float3x3)unity_CameraProjection;
    float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
    float2 p13_31 = float2(unity_CameraProjection._13, unity_CameraProjection._23);

    // View space normal and depth
    float4 textureVal = SampleTexture(uv);
    float3 norm_o = CalculateNormalFromTextureSample(textureVal, input.uv);
    float depth_o = CalculateDepthFromTextureSample(textureVal, uv);

#if defined(DEBUG_NORMAL)
    return float4(norm_o.xy, (norm_o.z), 0.0);
#endif

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
    float4 positionCS = input.positionCS;

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
