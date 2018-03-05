#ifndef LIGHTWEIGHT_PIPELINE_CORE_INCLUDED
#define LIGHTWEIGHT_PIPELINE_CORE_INCLUDED

#include "CoreRP/ShaderLibrary/Common.hlsl"
#include "CoreRP/ShaderLibrary/Packing.hlsl"
#include "Input.hlsl"

///////////////////////////////////////////////////////////////////////////////
#ifdef _NORMALMAP
    #define OUTPUT_NORMAL(IN, OUT) OutputTangentToWorld(IN.tangent, IN.normal, OUT.tangent, OUT.binormal, OUT.normal)
#else
    #define OUTPUT_NORMAL(IN, OUT) OUT.normal = TransformObjectToWorldNormal(IN.normal)
#endif

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

void AlphaDiscard(half alpha, half cutoff)
{
#ifdef _ALPHATEST_ON
    clip(alpha - cutoff);
#endif
}

half3 UnpackNormal(half4 packedNormal)
{
    // Compiler will optimize the scale away
#if defined(UNITY_NO_DXT5nm)
    return UnpackNormalRGB(packedNormal, 1.0);
#else
    return UnpackNormalmapRGorAG(packedNormal, 1.0);
#endif
}

half3 UnpackNormalScale(half4 packedNormal, half bumpScale)
{
#if defined(UNITY_NO_DXT5nm)
    return UnpackNormalRGB(packedNormal, bumpScale);
#else
    return UnpackNormalmapRGorAG(packedNormal, bumpScale);
#endif
}

void OutputTangentToWorld(half4 vertexTangent, half3 vertexNormal, out half3 tangentWS, out half3 binormalWS, out half3 normalWS)
{
    half sign = vertexTangent.w * GetOddNegativeScale();
    normalWS = TransformObjectToWorldNormal(vertexNormal);
    tangentWS = normalize(mul((half3x3)UNITY_MATRIX_M, vertexTangent.xyz));
    binormalWS = cross(normalWS, tangentWS) * sign;
}

half3 TangentToWorldNormal(half3 normalTangent, half3 tangent, half3 binormal, half3 normal)
{
    half3x3 tangentToWorld = half3x3(tangent, binormal, normal);
    return normalize(mul(normalTangent, tangentToWorld));
}

// TODO: A similar function should be already available in SRP lib on master. Use that instead
float4 ComputeScreenPos(float4 positionCS)
{
    float4 o = positionCS * 0.5f;
    o.xy = float2(o.x, o.y * _ProjectionParams.x) + o.w;
    o.zw = positionCS.zw;
    return o;
}

half ComputeFogFactor(float z)
{
    float clipZ_01 = UNITY_Z_0_FAR_FROM_CLIPSPACE(z);

#if defined(FOG_LINEAR)
    // factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
    float fogFactor = saturate(clipZ_01 * unity_FogParams.z + unity_FogParams.w);
    return half(fogFactor);
#elif defined(FOG_EXP2)
    // factor = exp(-(density*z)^2)
    // -density * z computed at vertex
    return half(unity_FogParams.x * clipZ_01);
#else
    return 0.0h;
#endif
}

void ApplyFogColor(inout half3 color, half3 fogColor, half fogFactor)
{
#if defined (FOG_LINEAR) || defined(FOG_EXP2)
#if defined(FOG_EXP2)
    // factor = exp(-(density*z)^2)
    // fogFactor = density*z compute at vertex
    fogFactor = saturate(exp2(-fogFactor*fogFactor));
#endif
    color = lerp(fogColor, color, fogFactor);
#endif
}

void ApplyFog(inout half3 color, half fogFactor)
{
    ApplyFogColor(color, unity_FogColor.rgb, fogFactor);
}

// Stereo-related bits
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    #define SCREENSPACE_TEXTURE TEXTURE2D_ARRAY
#else
    #define SCREENSPACE_TEXTURE TEXTURE2D
#endif // defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)

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
