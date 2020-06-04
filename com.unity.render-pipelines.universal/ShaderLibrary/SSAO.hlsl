#ifndef UNIVERSAL_SSAO_INCLUDED
#define UNIVERSAL_SSAO_INCLUDED

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

// Textures & Samplers
TEXTURE2D_X(_BaseMap);
TEXTURE2D_X(_ScreenSpaceOcclusionTexture);

SAMPLER(sampler_BaseMap);
SAMPLER(sampler_ScreenSpaceOcclusionTexture);

// Params
float4 _BlurOffset;
float4 _SSAOParams;
float4 _BaseMap_TexelSize;
float4 _CameraDepthTexture_TexelSize;

// SSAO Settings
#define DOWNSAMPLE _SSAOParams.x
#define INTENSITY _SSAOParams.y
#define RADIUS _SSAOParams.z

// GLES2: In many cases, dynamic looping is not supported.
#if defined(SHADER_API_GLES) && !defined(SHADER_API_GLES3)
    #define SAMPLE_COUNT 3
#else
    #define SAMPLE_COUNT _SSAOParams.w
#endif

// Function defines
#define SCREEN_PARAMS        GetScaledScreenParams()
#define SAMPLE_BASEMAP(uv)   SAMPLE_TEXTURE2D_X(_BaseMap, sampler_BaseMap, UnityStereoTransformScreenSpaceTex(uv));
#define SAMPLE_BASEMAP_R(uv) SAMPLE_TEXTURE2D_X(_BaseMap, sampler_BaseMap, UnityStereoTransformScreenSpaceTex(uv)).r;


// Constants
// kContrast determines the contrast of occlusion. This allows users to control over/under
// occlusion. At the moment, this is not exposed to the editor because it's rarely useful.
static const float kContrast = 0.6;

// The constants below are used in the AO estimator. Beta is mainly used for suppressing
// self-shadowing noise, and Epsilon is used to prevent calculation underflow. See the
// paper (Morgan 2011 http://goo.gl/2iz3P) for further details of these constants.
static const float kBeta = 0.002;
#define EPSILON         1.0e-4


float2 GetScreenSpacePosition(float2 uv)
{
    return uv * SCREEN_PARAMS.xy * DOWNSAMPLE;
}

// Sample point picker
float3 PickSamplePoint(float2 uv, float randAddon, int index)
{
    float2 positionSS = GetScreenSpacePosition(uv);
    float noise = InterleavedGradientNoise(positionSS, index);
    return SampleSphereUniform(frac(noise), (noise * PI));
}

float SampleAndGetLinearDepth(float2 uv)
{
    float rawDepth = SampleSceneDepth(uv.xy).r;
    #if defined(_ORTHOGRAPHIC)
        float linearDepth = ((_ProjectionParams.z - _ProjectionParams.y) * (1.0 - rawDepth) + _ProjectionParams.y);
    #else
        float linearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
    #endif
    return linearDepth;
}

float3 ReconstructViewPos(float2 uv, float depth, float2 p11_22, float2 p13_31)
{
    #if defined(_ORTHOGRAPHIC)
        float3 viewPos = float3(((uv.xy * 2.0 - 1.0 - p13_31) / p11_22), depth);
    #else
        float3 viewPos = float3(depth * ((uv.xy * 2.0 - 1.0 - p13_31) / p11_22), depth);
    #endif
    return viewPos;
}

// Try reconstructing normal accurately from depth buffer.
// Low:    DDX/DDY on the current pixel
// Medium: 3 taps on each direction | x | * | y |
// High:   5 taps on each direction: | z | x | * | y | w |
// https://atyuwen.github.io/posts/normal-reconstruction/
// https://wickedengine.net/2019/09/22/improved-normal-reconstruction-from-depth/
float3 ReconstructNormal(float2 uv, float depth, float3 vpos, float2 p11_22, float2 p13_31)
{
    #if defined(_RECONSTRUCT_NORMAL_LOW)
        return normalize(cross(ddy(vpos), ddx(vpos)));
    #else
        float2 delta = _CameraDepthTexture_TexelSize.xy * 2.0;

        // Sample the neighbour fragments
        float2 lUV = float2(-delta.x, 0.0);
        float2 rUV = float2( delta.x, 0.0);
        float2 uUV = float2(0.0,  delta.y);
        float2 dUV = float2(0.0, -delta.y);

        float3 l1 = float3(uv + lUV, 0.0); l1.z = SampleAndGetLinearDepth(l1.xy); // Left1
        float3 r1 = float3(uv + rUV, 0.0); r1.z = SampleAndGetLinearDepth(r1.xy); // Right1
        float3 u1 = float3(uv + uUV, 0.0); u1.z = SampleAndGetLinearDepth(u1.xy); // Up1
        float3 d1 = float3(uv + dUV, 0.0); d1.z = SampleAndGetLinearDepth(d1.xy); // Down1

        // Determine the closest horizontal and vertical pixels...
        // horizontal: left = 0.0 right = 1.0
        // vertical  : down = 0.0    up = 1.0
        #if defined(_RECONSTRUCT_NORMAL_MEDIUM)
             uint closest_horizontal = l1.z > r1.z ? 0 : 1;
             uint closest_vertical   = d1.z > u1.z ? 0 : 1;
        #else
            float3 l2 = float3(uv + lUV * 2.0, 0.0); l2.z = SampleAndGetLinearDepth(l2.xy); // Left2
            float3 r2 = float3(uv + rUV * 2.0, 0.0); r2.z = SampleAndGetLinearDepth(r2.xy); // Right2
            float3 u2 = float3(uv + uUV * 2.0, 0.0); u2.z = SampleAndGetLinearDepth(u2.xy); // Up2
            float3 d2 = float3(uv + dUV * 2.0, 0.0); d2.z = SampleAndGetLinearDepth(d2.xy); // Down2

            const uint closest_horizontal = abs( (2.0 * l1.z - l2.z) - depth) < abs( (2.0 * r1.z - r2.z) - depth) ? 0 : 1;
            const uint closest_vertical   = abs( (2.0 * d1.z - d2.z) - depth) < abs( (2.0 * u1.z - u2.z) - depth) ? 0 : 1;
        #endif


        // Calculate the triangle, in a counter-clockwize order, to
        // use based on the closest horizontal and vertical depths.
        // h == 0.0 && v == 0.0: p1 = left,  p2 = down
        // h == 1.0 && v == 0.0: p1 = down,  p2 = right
        // h == 1.0 && v == 1.0: p1 = right, p2 = up
        // h == 0.0 && v == 1.0: p1 = up,    p2 = left
        // Calculate the view space positions for the three points...
        float3 P1;
        float3 P2;
        if (closest_vertical == 0)
        {
            P1 = closest_horizontal == 0 ? l1 : d1;
            P2 = closest_horizontal == 0 ? d1 : r1;
        }
        else
        {
            P1 = closest_horizontal == 0 ? u1 : r1;
            P2 = closest_horizontal == 0 ? l1 : u1;
        }

        P1 = ReconstructViewPos(P1.xy, P1.z, p11_22, p13_31);
        P2 = ReconstructViewPos(P2.xy, P2.z, p11_22, p13_31);

        // Use the cross product to calculate the normal...
        return normalize(cross(P2 - vpos, P1 - vpos));
    #endif
}

void SampleDepthNormalView(float2 uv, float2 p11_22, float2 p13_31, out float depth, out float3 normal, out float3 vpos)
{
    depth  = SampleAndGetLinearDepth(uv);
    vpos = ReconstructViewPos(uv, depth, p11_22, p13_31);

    #if defined(SOURCE_DEPTH_NORMALS)
        normal = SampleSceneNormals(uv);
    #else
        normal = ReconstructNormal(uv, depth, vpos, p11_22, p13_31);
    #endif
}

// Distance-based AO estimator based on Morgan 2011
// "Alchemy screen-space ambient obscurance algorithm"
// http://graphics.cs.williams.edu/papers/AlchemyHPG11/
float4 SSAO(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = input.uv;

    // Parameters used in coordinate conversion
    float3x3 camProj = (float3x3)unity_CameraProjection;
    float2 p11_22 = float2(camProj._11, camProj._22);
    float2 p13_31 = float2(camProj._13, camProj._23);

    // Get the depth, normal and view position for this fragment
    float depth_o;
    float3 norm_o;
    float3 vpos_o;
    SampleDepthNormalView(uv, p11_22, p13_31, depth_o, norm_o, vpos_o);

    // This was added to avoid a NVIDIA driver issue.
    float randAddon = uv.x * 1e-10;

    float rcpSampleCount = rcp(SAMPLE_COUNT);
    float samplePointRadiusMultiplier = RADIUS * rcpSampleCount;
    float ao = 0.0;
    for (int s = 0; s < int(SAMPLE_COUNT); s++)
    {
        #if defined(SHADER_API_D3D11)
            // This 'floor(1.0001 * s)' operation is needed to avoid a DX11 NVidia shader issue.
            s = floor(1.0001 * s);
        #endif

        // Sample point
        float3 v_s1 = PickSamplePoint(uv.xy, randAddon + s, s);

        // Make it distributed between [0, _Radius]
        v_s1 *= (s + 1.0) * samplePointRadiusMultiplier;

        v_s1 = faceforward(v_s1, -norm_o, v_s1);
        float3 vpos_s1 = vpos_o + v_s1;

        // Reproject the sample point
        float3 spos_s1 = mul(camProj, vpos_s1);
        #if defined(_ORTHOGRAPHIC)
            float2 uv_s1_01 = clamp((spos_s1.xy + 1.0) * 0.5, 0.0, 1.0);
        #else
            float2 uv_s1_01 = clamp((spos_s1.xy * rcp(vpos_s1.z) + 1.0) * 0.5, 0.0, 1.0);
        #endif

        // Depth at the sample point
        float depth_s1 = SampleAndGetLinearDepth(uv_s1_01);

        // Relative position of the sample point
        float3 vpos_s2 = ReconstructViewPos(uv_s1_01, depth_s1, p11_22, p13_31);
        float3 v_s2 = vpos_s2 - vpos_o;

        // Estimate the obscurance value
        float a1 = max(dot(v_s2, norm_o) - kBeta * depth_o, 0.0);
        float a2 = dot(v_s2, v_s2) + EPSILON;
        ao += a1 * rcp(a2);
    }

    // Intensity normalization
    ao *= RADIUS;

    // Apply contrast
    ao = PositivePow(ao * INTENSITY * rcpSampleCount, kContrast);
    return 1.0 - ao;
}


half4 KawaseBlur(Varyings input) : SV_Target
{
    half sum  = 4.0 * SAMPLE_BASEMAP_R(input.uv                 );
    sum      +=       SAMPLE_BASEMAP_R(input.uv + _BlurOffset.xy);
    sum      +=       SAMPLE_BASEMAP_R(input.uv + _BlurOffset.xw);
    sum      +=       SAMPLE_BASEMAP_R(input.uv + _BlurOffset.zy);
    sum      +=       SAMPLE_BASEMAP_R(input.uv + _BlurOffset.zw);
    sum      *= 0.125; // Divide by 8

    return half4(sum, sum, sum, 1.0);
}

#endif //UNIVERSAL_SSAO_INCLUDED
