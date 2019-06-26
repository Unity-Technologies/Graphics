#ifndef LIGHTWEIGHT_FOG_INCLUDED
#define LIGHTWEIGHT_FOG_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

///////////////////////////////////////////////////////////////////////////////
//                      Constant Buffers                                     //
///////////////////////////////////////////////////////////////////////////////

CBUFFER_START(_PerCamera)
float3 _FogColor;
float4 _FogParams;
float4 _FogMap_HDR;
CBUFFER_END

#ifdef FOGMAP
TEXTURECUBE(_FogMap);       SAMPLER(sampler_FogMap);
#endif

real ComputeFogFactor(VertexPositionInputs vertInputs)
{
    float clipZ_01 = UNITY_Z_0_FAR_FROM_CLIPSPACE(vertInputs.positionCS.z);

#if defined(FOG_LINEAR)
    // factor = (end-z)/(end-start) = vertInputs.positionCS.z * (-1/(end-start)) + (end/(end-start))
    float fogFactor = saturate(clipZ_01 * _FogParams.z + _FogParams.w);
    return real(fogFactor);
#elif defined(FOG_EXP2)
    // factor = exp(-(density*z)^2)
    // -density * vertInputs.positionCS.z computed at vertex
    return real(_FogParams.x * clipZ_01);
#elif defined(FOG_EXP)
//_FogParams.x = height; _FogParams.y = falloff; _FogParams.z = distanceOffset; _FogParams.w = distanceFalloff;
    float height = min(_WorldSpaceCameraPos.y,vertInputs.positionWS.y);
    float heightFactor = saturate((_FogParams.x+_FogParams.y-height)/_FogParams.y);
    float distanceFactor = saturate((clipZ_01-_FogParams.z)/_FogParams.w);
    return real(1-(heightFactor*distanceFactor));
#else
    return 0.0h;
#endif
}

half3 MixFogColor(real3 fragColor, real3 fogColor, real fogFactor)
{
#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
#if defined(FOG_EXP)
    // factor = exp(-density*z)
    // fogFactor = density*z compute at vertex
    //fogFactor = pow(fogFactor,2);
#elif defined(FOG_EXP2)
    // factor = exp(-(density*z)^2)
    // fogFactor = density*z compute at vertex
    fogFactor = saturate(exp2(-fogFactor*fogFactor));
#endif
    fragColor = lerp(fogColor, fragColor, fogFactor);
#endif
    return fragColor;
}

half3 MipFog(real3 viewDirection, float z)
{
#ifdef FOGMAP
    half depth = (1-pow(Linear01Depth(z, _ZBufferParams), 0.5h)) * 7.0;
    viewDirection.z = -viewDirection.z; // flip to match skybox
    half3 color = SAMPLE_TEXTURECUBE_LOD(_FogMap, sampler_FogMap, viewDirection, depth);
#if !defined(UNITY_USE_NATIVE_HDR)
    color = DecodeHDREnvironment(half4(color, 1), _FogMap_HDR);
#endif
#else
    half3 color = half3(1, 0, 0);
#endif
    return color;
}

half3 MixFog(real3 fragColor, real fogFactor, real3 viewDirection, float z)
{
#ifdef FOGMAP
    half3 fogColor = MipFog(viewDirection, z);
#else
    half3 fogColor = _FogColor;
#endif
    return MixFogColor(fragColor, fogColor, fogFactor);
}

#endif //LIGHTWEIGHT_FOG_INCLUDED
