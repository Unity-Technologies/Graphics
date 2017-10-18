#ifndef LIGHTWEIGHT_PIPELINE_CORE_INCLUDED
#define LIGHTWEIGHT_PIPELINE_CORE_INCLUDED

#include "LightweightInput.cginc"
#include "LightweightLighting.cginc"
#include "LightweightShadows.cginc"

#if defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
#define LIGHTWEIGHT_SPECULAR_HIGHLIGHTS
#endif

#define kDieletricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)

half SpecularReflectivity(half3 specular)
{
#if (SHADER_TARGET < 30)
    // SM2.0: instruction count limitation
    // SM2.0: simplified SpecularStrength
    return specular.r; // Red channel - because most metals are either monocrhome or with redish/yellowish tint
#else
    return max(max(specular.r, specular.g), specular.b);
#endif
}

inline void InitializeSurfaceData(LightweightVertexOutput IN, out SurfaceData outSurfaceData)
{
    float2 uv = IN.uv01.xy;
    half4 albedoAlpha = tex2D(_MainTex, uv);

    half4 specGloss = MetallicSpecGloss(uv, albedoAlpha);
    outSurfaceData.albedo = LIGHTWEIGHT_GAMMA_TO_LINEAR(albedoAlpha.rgb) * _Color.rgb;

#if _METALLIC_SETUP
    outSurfaceData.specular = half4(1.0h, 1.0h, 1.0h, 1.0h);
    outSurfaceData.metallic = specGloss.r;
#else
    outSurfaceData.specular = specGloss.rgb;
    outSurfaceData.metallic = 1.0h;
#endif

    outSurfaceData.smoothness = specGloss.a;
    outSurfaceData.normal = Normal(uv);
    outSurfaceData.occlusion = OcclusionLW(uv);
    outSurfaceData.emission = EmissionLW(uv);
    outSurfaceData.ambient = IN.fogCoord.yzw;
    outSurfaceData.alpha = Alpha(albedoAlpha.a);
}

void InitializeSurfaceInput(LightweightVertexOutput IN, out SurfaceInput outSurfaceInput)
{
#if LIGHTMAP_ON
    outSurfaceInput.lightmapUV = float4(IN.uv01.zw, 0.0, 0.0);
#else
    outSurfaceInput.lightmapUV = float4(0.0, 0.0, 0.0, 0.0);
#endif

#if _NORMALMAP
    outSurfaceInput.tangent = IN.tangent;
    outSurfaceInput.binormal = IN.binormal;
#else
    outSurfaceInput.tangent = half3(1.0h, 0.0h, 0.0h);
    outSurfaceInput.binormal = half3(0.0h, 1.0h, 0.0h);
#endif

    outSurfaceInput.normal = IN.normal;
    outSurfaceInput.worldPos = IN.posWS;
    outSurfaceInput.viewDir = IN.viewDir;
    outSurfaceInput.fogFactor = IN.fogCoord.x;
}

inline void InitializeBRDFData(SurfaceData surfaceData, out BRDFData outBRDFData)
{
    // BRDF SETUP
#ifdef _METALLIC_SETUP
    // We'll need oneMinusReflectivity, so
    //   1-reflectivity = 1-lerp(dielectricSpec, 1, metallic) = lerp(1-dielectricSpec, 0, metallic)
    // store (1-dielectricSpec) in kDieletricSpec.a, then
    //   1-reflectivity = lerp(alpha, 0, metallic) = alpha + metallic*(0 - alpha) =
    //                  = alpha - metallic * alpha
    half oneMinusDielectricSpec = kDieletricSpec.a;
    half oneMinusReflectivity = oneMinusDielectricSpec - surfaceData.metallic * oneMinusDielectricSpec;
    half reflectivity = 1.0 - oneMinusReflectivity;

    outBRDFData.diffuse = surfaceData.albedo * oneMinusReflectivity;
    outBRDFData.specular = lerp(kDieletricSpec.rgb, surfaceData.albedo, surfaceData.metallic);
#else
    half reflectivity = SpecularReflectivity(surfaceData.specular);

    outBRDFData.diffuse = surfaceData.albedo * (half3(1.0h, 1.0h, 1.0h) - surfaceData.specular);
    outBRDFData.specular = surfaceData.specular;
#endif

    outBRDFData.grazingTerm = saturate(surfaceData.smoothness + reflectivity);
    outBRDFData.perceptualRoughness = 1.0h - surfaceData.smoothness;
    outBRDFData.roughness = outBRDFData.perceptualRoughness * outBRDFData.perceptualRoughness;

#ifdef _ALPHAPREMULTIPLY_ON
    half alpha = surfaceData.alpha;
    outBRDFData.diffuse *= alpha;
    surfaceData.alpha = reflectivity + alpha * (1.0 - reflectivity);
#endif
}

half3 TangentToWorldNormal(half3 normalTangent, half3 tangent, half3 binormal, half3 normal)
{
    half3x3 tangentToWorld = half3x3(tangent, binormal, normal);
    return normalize(mul(normalTangent, tangentToWorld));
}

float ComputeFogFactor(float z)
{
    float clipZ_01 = UNITY_Z_0_FAR_FROM_CLIPSPACE(z);

#if defined(FOG_LINEAR)
    // factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
    float fogFactor = saturate(clipZ_01 * unity_FogParams.z + unity_FogParams.w);
    return fogFactor;
#elif defined(FOG_EXP)
    // factor = exp(-density*z)
    float unityFogFactor = unity_FogParams.y * clipZ_01;
    return saturate(exp2(-unityFogFactor));
#elif defined(FOG_EXP2)
    // factor = exp(-(density*z)^2)
    float unityFogFactor = unity_FogParams.x * clipZ_01;
    return saturate(exp2(-unityFogFactor*unityFogFactor));
#else
    return 0.0;
#endif
}

void ApplyFog(inout half3 color, float fogFactor)
{
#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
    color = lerp(unity_FogColor, color, fogFactor);
#endif
}

half4 OutputColor(half3 color, half alpha)
{
#if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
    return half4(LIGHTWEIGHT_LINEAR_TO_GAMMA(color), alpha);
#else
    return half4(LIGHTWEIGHT_LINEAR_TO_GAMMA(color), 1);
#endif
}


#endif
