#ifndef LIGHTWEIGHT_PIPELINE_CORE_INCLUDED
#define LIGHTWEIGHT_PIPELINE_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Input.hlsl"

#if !defined(SHADER_HINT_NICE_QUALITY)
#ifdef SHADER_API_MOBILE
#define SHADER_HINT_NICE_QUALITY 0
#else
#define SHADER_HINT_NICE_QUALITY 1
#endif
#endif

#ifndef BUMP_SCALE_NOT_SUPPORTED
#define BUMP_SCALE_NOT_SUPPORTED !SHADER_HINT_NICE_QUALITY
#endif

struct VertexPositionInputs
{
    float3 positionWS; // World space position
    float3 positionVS; // View space position
    float4 positionCS; // Homogeneous clip space position
};

struct VertexTBN
{
    real3 tangentWS;
    real3 binormalWS;
    real3 normalWS;
};

VertexPositionInputs GetVertexPositionInputs(float3 positionOS)
{
    VertexPositionInputs input;
    input.positionWS = TransformObjectToWorld(positionOS);
    input.positionVS = TransformWorldToView(input.positionWS);
    input.positionCS = TransformWorldToHClip(input.positionWS);
    return input;
}

VertexTBN GetVertexTBN(float3 normalOS)
{
    VertexTBN tbn;
    tbn.tangentWS = real3(1.0, 0.0, 0.0);
    tbn.binormalWS = real3(0.0, 1.0, 0.0);
    tbn.normalWS = TransformObjectToWorldNormal(normalOS);
    return tbn;
}

VertexTBN GetVertexTBN(float3 normalOS, float4 tangentOS)
{
    VertexTBN tbn;

    // mikkts space compliant. only normalize when extracting normal at frag.
    real sign = tangentOS.w * GetOddNegativeScale();
    tbn.normalWS = TransformObjectToWorldNormal(normalOS);
    tbn.tangentWS = TransformObjectToWorldDir(tangentOS.xyz);
    tbn.binormalWS = cross(tbn.normalWS, tbn.tangentWS) * sign;
    return tbn;
}

#if UNITY_REVERSED_Z
    #if SHADER_API_OPENGL || SHADER_API_GLES || SHADER_API_GLES3
        //GL with reversed z => z clip range is [near, -far] -> should remap in theory but dont do it in practice to save some perf (range is close enough)
        #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(-(coord), 0)
    #else
        //D3d with reversed Z => z clip range is [near, 0] -> remapping to [0, far]
        //max is required to protect ourselves from near plane not being correct/meaningfull in case of oblique matrices.
        #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(((1.0-(coord)/_ProjectionParams.y)*_ProjectionParams.z),0)
    #endif
#elif UNITY_UV_STARTS_AT_TOP
    //D3d without reversed z => z clip range is [0, far] -> nothing to do
    #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) (coord)
#else
    //Opengl => z clip range is [-near, far] -> should remap in theory but dont do it in practice to save some perf (range is close enough)
    #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) (coord)
#endif

// TODO: Really need a better define for iOS Metal than the framebuffer fetch one, that's also enabled on android and webgl (???)
#if defined(SHADER_API_VULKAN) || (defined(SHADER_API_METAL) && defined(UNITY_FRAMEBUFFER_FETCH_AVAILABLE))
    // Renderpass inputs: Vulkan/Metal subpass input
#define UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT(idx) cbuffer hlslcc_SubpassInput_f_##idx { float4 hlslcc_fbinput_##idx; }
#define UNITY_DECLARE_FRAMEBUFFER_INPUT_HALF(idx) cbuffer hlslcc_SubpassInput_h_##idx { half4 hlslcc_fbinput_##idx; }

#define UNITY_READ_FRAMEBUFFER_INPUT(idx, v2fname) hlslcc_fbinput_##idx
#else
    // Renderpass inputs: General fallback path
#define UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT(idx) TEXTURE2D_FLOAT(_UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize
#define UNITY_DECLARE_FRAMEBUFFER_INPUT_HALF(idx) TEXTURE2D_HALF(_UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize

#define UNITY_READ_FRAMEBUFFER_INPUT(idx, v2fvertexname) _UnityFBInput##idx.Load(uint3(v2fvertexname.xy, 0))
#endif

float3 GetCameraPositionWS()
{
    return _WorldSpaceCameraPos;
}

float4 GetScaledScreenParams()
{
    return _ScaledScreenParams;
}

void AlphaDiscard(real alpha, real cutoff, real offset = 0.0h)
{
#ifdef _ALPHATEST_ON
    clip(alpha - cutoff + offset);
#endif
}

real3 UnpackNormal(real4 packedNormal)
{
#if defined(UNITY_NO_DXT5nm)
    return UnpackNormalRGBNoScale(packedNormal);
#else
        // Compiler will optimize the scale away
    return UnpackNormalmapRGorAG(packedNormal, 1.0);
#endif
}

real3 UnpackNormalScale(real4 packedNormal, real bumpScale)
{
#if defined(UNITY_NO_DXT5nm)
    return UnpackNormalRGB(packedNormal, bumpScale);
#else
    return UnpackNormalmapRGorAG(packedNormal, bumpScale);
#endif
}

real3 FragmentNormalWS(real3 normal)
{
#if !SHADER_HINT_NICE_QUALITY
    // World normal is already normalized in vertex. Small acceptable error to save ALU.
    return normal;
#else
    return normalize(normal);
#endif
}

real3 FragmentViewDirWS(real3 viewDir)
{
#if !SHADER_HINT_NICE_QUALITY
    // View direction is already normalized in vertex. Small acceptable error to save ALU.
    return viewDir;
#else
    return SafeNormalize(viewDir);
#endif
}

real3 VertexViewDirWS(real3 viewDir)
{
#if !SHADER_HINT_NICE_QUALITY
    // Normalize in vertex and avoid renormalizing it in frag to save ALU.
    return SafeNormalize(viewDir);
#else
    return viewDir;
#endif
}

real3 TangentToWorldNormal(real3 normalTangent, real3 tangent, real3 binormal, real3 normal)
{
    real3x3 tangentToWorld = real3x3(tangent, binormal, normal);
    return FragmentNormalWS(mul(normalTangent, tangentToWorld));
}

// TODO: A similar function should be already available in SRP lib on master. Use that instead
float4 ComputeScreenPos(float4 positionCS)
{
    float4 o = positionCS * 0.5f;
    o.xy = float2(o.x, o.y * _ProjectionParams.x) + o.w;
    o.zw = positionCS.zw;
    return o;
}

real ComputeFogFactor(float z)
{
    float clipZ_01 = UNITY_Z_0_FAR_FROM_CLIPSPACE(z);

#if defined(FOG_LINEAR)
    // factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
    float fogFactor = saturate(clipZ_01 * unity_FogParams.z + unity_FogParams.w);
    return real(fogFactor);
#elif defined(FOG_EXP) || defined(FOG_EXP2)
    // factor = exp(-(density*z)^2)
    // -density * z computed at vertex
    return real(unity_FogParams.x * clipZ_01);
#else
    return 0.0h;
#endif
}

void ApplyFogColor(inout real3 color, real3 fogColor, real fogFactor)
{
#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
#if defined(FOG_EXP)
    // factor = exp(-density*z)
    // fogFactor = density*z compute at vertex
    fogFactor = saturate(exp2(-fogFactor));
#elif defined(FOG_EXP2)
    // factor = exp(-(density*z)^2)
    // fogFactor = density*z compute at vertex
    fogFactor = saturate(exp2(-fogFactor*fogFactor));
#endif
    color = lerp(fogColor, color, fogFactor);
#endif
}

void ApplyFog(inout real3 color, real fogFactor)
{
    ApplyFogColor(color, unity_FogColor.rgb, fogFactor);
}

// Stereo-related bits
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    #define SCREENSPACE_TEXTURE         TEXTURE2D_ARRAY
    #define SCREENSPACE_TEXTURE_FLOAT   TEXTURE2D_ARRAY_FLOAT
    #define SCREENSPACE_TEXTURE_HALF    TEXTURE2D_ARRAY_HALF
#else
    #define SCREENSPACE_TEXTURE         TEXTURE2D
    #define SCREENSPACE_TEXTURE_FLOAT   TEXTURE2D_FLOAT
    #define SCREENSPACE_TEXTURE_HALF    TEXTURE2D_HALF
#endif

#if defined(UNITY_SINGLE_PASS_STEREO)
float2 TransformStereoScreenSpaceTex(float2 uv, float w)
{
    // TODO: RVS support can be added here, if LWRP decides to support it
    float4 scaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];
    return uv.xy * scaleOffset.xy + scaleOffset.zw * w;
}

float2 UnityStereoTransformScreenSpaceTex(float2 uv)
{
    return TransformStereoScreenSpaceTex(saturate(uv), 1.0);
}
#else

#define UnityStereoTransformScreenSpaceTex(uv) uv

#endif // defined(UNITY_SINGLE_PASS_STEREO)

#endif
