#ifndef LIGHTWEIGHT_PIPELINE_CORE_INCLUDED
#define LIGHTWEIGHT_PIPELINE_CORE_INCLUDED

#include "LightweightPipelineInput.cginc"

#if defined(_HARD_SHADOWS) || defined(_SOFT_SHADOWS) || defined(_HARD_SHADOWS_CASCADES) || defined(_SOFT_SHADOWS_CASCADES)
#define _SHADOWS
#endif

#if defined(_HARD_SHADOWS_CASCADES) || defined(_SOFT_SHADOWS_CASCADES)
#define _SHADOW_CASCADES
#endif

#if defined(UNITY_COLORSPACE_GAMMA) && defined(LIGHTWEIGHT_LINEAR)
// Ideally we want an approximation of gamma curve 2.0 to save ALU on GPU but as off now it won't match the GammaToLinear conversion of props in engine
//#define LIGHTWEIGHT_GAMMA_TO_LINEAR(gammaColor) gammaColor * gammaColor
//#define LIGHTWEIGHT_LINEAR_TO_GAMMA(linColor) sqrt(color)
#define LIGHTWEIGHT_GAMMA_TO_LINEAR(sRGB) sRGB * (sRGB * (sRGB * 0.305306011h + 0.682171111h) + 0.012522878h)
#define LIGHTWEIGHT_LINEAR_TO_GAMMA(linRGB) max(1.055h * pow(max(linRGB, 0.h), 0.416666667h) - 0.055h, 0.h)
#else
#define LIGHTWEIGHT_GAMMA_TO_LINEAR(color) color
#define LIGHTWEIGHT_LINEAR_TO_GAMMA(color) color
#endif

#ifdef _SHADOWS
#include "LightweightPipelineShadows.cginc"
#endif

//  Does not support: _PARALLAXMAP, DIRLIGHTMAP_COMBINED
#define GLOSSMAP (defined(_SPECGLOSSMAP) || defined(_METALLICGLOSSMAP))

#ifndef SPECULAR_HIGHLIGHTS
#define SPECULAR_HIGHLIGHTS (!defined(_SPECULAR_HIGHLIGHTS_OFF))
#endif

half4 OutputColor(half3 color, half alpha)
{
#ifdef _ALPHABLEND_ON
    return LIGHTWEIGHT_LINEAR_TO_GAMMA(half4(color, alpha));
#else
    return half4(LIGHTWEIGHT_LINEAR_TO_GAMMA(color), 1);
#endif
}

half MetallicSetup_Reflectivity()
{
    return 1.0h - OneMinusReflectivityFromMetallic(_Metallic);
}

#ifdef _NORMALMAP

half3 TransformToTangentSpace(half3 tangent, half3 binormal, half3 normal, half3 v)
{
    // Mali400 shader compiler prefers explicit dot product over using a half3x3 matrix
    return half3(dot(tangent, v), dot(binormal, v), dot(normal, v));
}

void TangentSpaceLightingInput(half3 normalWorld, half4 vTangent, half3 lightDirWorld, half3 eyeVecWorld, out half3 tangentSpaceLightDir, out half3 tangentSpaceEyeVec)
{
    half3 tangentWorld = UnityObjectToWorldDir(vTangent.xyz);
    half sign = half(vTangent.w) * half(unity_WorldTransformParams.w);
    half3 binormalWorld = cross(normalWorld, tangentWorld) * sign;
    tangentSpaceLightDir = TransformToTangentSpace(tangentWorld, binormalWorld, normalWorld, lightDirWorld);
#if SPECULAR_HIGHLIGHTS
    tangentSpaceEyeVec = normalize(TransformToTangentSpace(tangentWorld, binormalWorld, normalWorld, eyeVecWorld));
#else
    tangentSpaceEyeVec = 0;
#endif
}

#endif // _NORMALMAP

half3 LightDirForSpecular(LightweightVertexOutput i, UnityLight mainLight)
{
#if SPECULAR_HIGHLIGHTS && defined(_NORMALMAP)
    return i.tangentSpaceLightDir;
#else
    return mainLight.dir;
#endif
}


#endif
