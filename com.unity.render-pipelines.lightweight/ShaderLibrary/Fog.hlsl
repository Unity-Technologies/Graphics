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

TEXTURECUBE(_FogMap);       SAMPLER(sampler_FogMap);

real ComputeFogFactor(VertexPositionInputs vertInputs)
{
    float clipZ_01 = UNITY_Z_0_FAR_FROM_CLIPSPACE(vertInputs.positionCS.z);

#if defined(FOG_LINEAR)
    // factor = (end-z)/(end-start) = vertInputs.positionCS.z * (-1/(end-start)) + (end/(end-start))
    float fogFactor = saturate(clipZ_01 * _FogParams.z + _FogParams.w);
    return real(fogFactor);
#elif defined(FOG_EXP) || defined(FOG_EXP2)
    // factor = exp(-(density*z)^2)
    // -density * vertInputs.positionCS.z computed at vertex
    return real(_FogParams.x * clipZ_01);
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
    fogFactor = saturate(exp2(-fogFactor));
#elif defined(FOG_EXP2)
    // factor = exp(-(density*z)^2)
    // fogFactor = density*z compute at vertex
    fogFactor = saturate(exp2(-fogFactor*fogFactor));
#endif
    fragColor = lerp(fogColor, fragColor, fogFactor);
#endif

    return fragColor;
}

half3 MixFog(real3 fragColor, real fogFactor)
{
    return MixFogColor(fragColor, _FogColor, fogFactor);
}

half3 MixFogDir(real3 fragColor, real fogFactor, real3 dir, float clipz)
{
// float z_eye = gl_ProjectionMatrix[3].z/(-z_ndc - gl_ProjectionMatrix[2].z);
// UNITY_MATRIX_P[3].z/(-UNITY_Z_0_FAR_FROM_CLIPSPACE(clipz) - UNITY_MATRIX_P[2].z);
    half depth = (1-pow(Linear01Depth(clipz, _ZBufferParams), 0.5h)) * 7.0;
    dir.z = -dir.z;
    float3 fogCube = SAMPLE_TEXTURECUBE_LOD(_FogMap, sampler_FogMap, dir, depth);
#if !defined(UNITY_USE_NATIVE_HDR)
    fogCube = DecodeHDREnvironment(half4(fogCube, 1), _FogMap_HDR);
#endif
    return MixFogColor(fragColor, fogCube.rgb, fogFactor);
}

#endif //LIGHTWEIGHT_FOG_INCLUDED