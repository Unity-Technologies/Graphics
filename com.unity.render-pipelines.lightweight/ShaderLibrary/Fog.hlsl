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
float _Rotation;
CBUFFER_END

#ifdef FOGMAP
TEXTURECUBE(_FogMap);       SAMPLER(sampler_FogMap);
#endif

float3 RotateAroundYInDegrees (float3 vertex, float degrees)
{
    float alpha = degrees * 3.1 / 180.0;
    float sina, cosa;
    sincos(alpha, sina, cosa);
    float2x2 m = float2x2(cosa, -sina, sina, cosa);
    return float3(mul(m, vertex.xz), vertex.y).xzy;
}

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

/*
float ComputeVolumetricFog( in float3 cameraToWorldPos )
{
// NOTE: cVolFogHeightDensityAtViewer = exp( -cHeightFalloff *
cViewPos.z );
float fogInt = length( cameraToWorldPos ) * cVolFogHeightDensityAtViewer;
const float cSlopeThreshold = 0.01;
if( abs( cameraToWorldPos.z ) > cSlopeThreshold )
{
     float t = cHeightFalloff * cameraToWorldPos.z;
     fogInt *= ( 1.0 - exp( -t ) ) / t;
}
   return exp( -cGlobalDensity * fogInt );
}
*/
/*
half3 cameraToWorldPos = vertInputs.positionWS - _WorldSpaceCameraPos;
half fogInt = length( cameraToWorldPos ) * exp(-_FogParams.z * clipZ_01z);
const half slopeThreshold = 0.01h;
if(abs(_WorldSpaceCameraPos.y) > slopeThreshold)
{
    half t = _FogParams.x * cameraToWorldPos.y;
    fogInt *= ( 1.0 - exp( -t ) ) / t;
}
return exp( -1.5 * fogInt);
*/
    half height = (1-pow(saturate((vertInputs.positionWS.y - _FogParams.z) * 0.01), 0.25));
    half distance = _FogParams.x * length(vertInputs.positionWS - _WorldSpaceCameraPos);
    return 1-saturate(height * distance);
    
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
    //fogFactor = saturate(exp2(-fogFactor));
    //fragColor = lerp(fragColor, fogColor, fogFactor);
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
    half depth = (1-pow(Linear01Depth(z, _ZBufferParams), 0.5h)) * _FogParams.y;
    viewDirection.z = -viewDirection.z; // flip to match skybox
    viewDirection = RotateAroundYInDegrees(viewDirection, _Rotation);
    half3 color = SAMPLE_TEXTURECUBE_LOD(_FogMap, sampler_FogMap, viewDirection, depth);
#if !defined(UNITY_USE_NATIVE_HDR)
    color = DecodeHDREnvironment(half4(color, 1), _FogMap_HDR) * 2.0;
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
