#ifndef LIGHTWEIGHT_LIGHTING_INCLUDED
#define LIGHTWEIGHT_LIGHTING_INCLUDED

#include "UnityCG.cginc"

#include "UnityStandardInput.cginc"

#include "LightweightShadows.cginc"

#define PI 3.14159265359f
#define kDieletricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)

#define MAX_VISIBLE_LIGHTS 16

CBUFFER_START(_PerObject)
half4 unity_LightIndicesOffsetAndCount;
half4 unity_4LightIndices0;
half4 unity_4LightIndices1;
half _Shininess;
CBUFFER_END

CBUFFER_START(_PerCamera)
float4 _MainLightPosition;
half4 _MainLightColor;
float4 _MainLightAttenuationParams;
half4 _MainLightSpotDir;

half4 _AdditionalLightCount;
float4 _AdditionalLightPosition[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightColor[MAX_VISIBLE_LIGHTS];
float4 _AdditionalLightAttenuationParams[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightSpotDir[MAX_VISIBLE_LIGHTS];
CBUFFER_END

CBUFFER_START(_PerFrame)
half4 _GlossyEnvironmentColor;
sampler2D _AttenuationTexture;
CBUFFER_END

#if defined(UNITY_COLORSPACE_GAMMA)
#define LIGHTWEIGHT_GAMMA_TO_LINEAR(gammaColor) gammaColor * gammaColor
#define LIGHTWEIGHT_LINEAR_TO_GAMMA(linColor) sqrt(color)
#else
#define LIGHTWEIGHT_GAMMA_TO_LINEAR(color) color
#define LIGHTWEIGHT_LINEAR_TO_GAMMA(color) color
#endif

// Main light initialized without indexing
#define INITIALIZE_MAIN_LIGHT(light) \
    light.pos = _MainLightPosition; \
    light.color = _MainLightColor; \
    light.atten = _MainLightAttenuationParams; \
    light.spotDir = _MainLightSpotDir;

// Indexing might have a performance hit for old mobile hardware
#define INITIALIZE_LIGHT(light, i) \
    half4 indices = (i < 4) ? unity_4LightIndices0 : unity_4LightIndices1; \
    int index = (i < 4) ? i : i - 4; \
    int lightIndex = indices[index]; \
    light.pos = _AdditionalLightPosition[lightIndex]; \
    light.color = _AdditionalLightColor[lightIndex]; \
    light.atten = _AdditionalLightAttenuationParams[lightIndex]; \
    light.spotDir = _AdditionalLightSpotDir[lightIndex]

struct LightInput
{
    float4 pos;
    half4 color;
    float4 atten;
    half4 spotDir;
};

struct SurfaceData
{
    half3 albedo;
    half3 specular;
    half  metallic;
    half  smoothness;
    half3 normal;
    half3 emission;
    half  occlusion;
    half  alpha;
};

struct SurfaceInput
{
    float4  lightmapUV;
    half3   normalWS;
    half3   tangentWS;
    half3   bitangentWS;
    float3  positionWS;
    half3   viewDirectionWS;
    half    fogFactor;
};

struct BRDFData
{
    half3 diffuse;
    half3 specular;
    half perceptualRoughness;
    half roughness;
    half grazingTerm;
};

inline void InitializeSurfaceData(out SurfaceData outSurfaceData)
{
    outSurfaceData.albedo = half3(1.0h, 1.0h, 1.0h);
    outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);
    outSurfaceData.metallic = 1.0h;
    outSurfaceData.smoothness = 0.5h;
    outSurfaceData.normal = half3(0.0h, 0.0h, 1.0h);
    outSurfaceData.occlusion = 1.0h;
    outSurfaceData.emission = half3(0.0h, 0.0h, 0.0h);
    outSurfaceData.alpha = 1.0h;
}

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

void ApplyFog(inout half3 color, half fogFactor)
{
#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
	color = lerp(unity_FogColor, color, fogFactor);
#endif
}

inline void InitializeBRDFData(half3 albedo, half metallic, half3 specular, half smoothness, half alpha, out BRDFData outBRDFData)
{
    // BRDF SETUP
#ifdef _METALLIC_SETUP
    // We'll need oneMinusReflectivity, so
    //   1-reflectivity = 1-lerp(dielectricSpec, 1, metallic) = lerp(1-dielectricSpec, 0, metallic)
    // store (1-dielectricSpec) in kDieletricSpec.a, then
    //   1-reflectivity = lerp(alpha, 0, metallic) = alpha + metallic*(0 - alpha) =
    //                  = alpha - metallic * alpha
    half oneMinusDielectricSpec = kDieletricSpec.a;
    half oneMinusReflectivity = oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
    half reflectivity = 1.0 - oneMinusReflectivity;

    outBRDFData.diffuse = albedo * oneMinusReflectivity;
    outBRDFData.specular = lerp(kDieletricSpec.rgb, albedo, metallic);
#else
    half reflectivity = SpecularReflectivity(specular);

    outBRDFData.diffuse = albedo * (half3(1.0h, 1.0h, 1.0h) - specular);
    outBRDFData.specular = specular;
#endif

    outBRDFData.grazingTerm = saturate(smoothness + reflectivity);
    outBRDFData.perceptualRoughness = 1.0h - smoothness;
    outBRDFData.roughness = outBRDFData.perceptualRoughness * outBRDFData.perceptualRoughness;

#ifdef _ALPHAPREMULTIPLY_ON
    outBRDFData.diffuse *= alpha;
    alpha = reflectivity + alpha * (1.0 - reflectivity);
#endif
}

// Based on Minimalist CookTorrance BRDF
// Implementation is slightly different from original derivation: http://www.thetenthplanet.de/archives/255
//
// * NDF [Modified] GGX
// * Modified Kelemen and Szirmay-​Kalos for Visibility term
// * Fresnel approximated with 1/LdotH
half3 LightweightBDRF(BRDFData brdfData, half roughness2, half3 normal, half3 lightDirection, half3 viewDir)
{
#ifndef _SPECULARHIGHLIGHTS_OFF
    half3 halfDir = Unity_SafeNormalize(lightDirection + viewDir);

    half NoH = saturate(dot(normal, halfDir));
    half LoH = saturate(dot(lightDirection, halfDir));

    // GGX Distribution multiplied by combined approximation of Visibility and Fresnel
    // See "Optimizing PBR for Mobile" from Siggraph 2015 moving mobile graphics course
    // https://community.arm.com/events/1155
    half d = NoH * NoH * (roughness2 - 1.h) + 1.00001h;

    half LoH2 = LoH * LoH;
    half specularTerm = roughness2 / ((d * d) * max(0.1h, LoH2) * (brdfData.roughness + 0.5h) * 4);

    // on mobiles (where half actually means something) denominator have risk of overflow
    // clamp below was added specifically to "fix" that, but dx compiler (we convert bytecode to metal/gles)
    // sees that specularTerm have only non-negative terms, so it skips max(0,..) in clamp (leaving only min(100,...))
#if defined (SHADER_API_MOBILE)
    specularTerm = specularTerm - 1e-4h;
#endif

#if defined (SHADER_API_MOBILE)
    specularTerm = clamp(specularTerm, 0.0, 100.0); // Prevent FP16 overflow on mobiles
#endif

    half3 color = specularTerm * brdfData.specular + brdfData.diffuse;
    return color;
#else
    return brdfData.diffuse;
#endif
}

half3 LightweightBRDFIndirect(BRDFData brdfData, UnityIndirect indirect, half roughness2, half fresnelTerm)
{
    half3 c = indirect.diffuse * brdfData.diffuse;
    float surfaceReduction = 1.0 / (roughness2 + 1.0);
    c += surfaceReduction * indirect.specular * lerp(brdfData.specular, brdfData.grazingTerm, fresnelTerm);
    return c;
}

UnityIndirect LightweightGI(float4 lightmapUV, half3 normalWorld, half3 reflectVec, half occlusion, half perceptualRoughness)
{
    UnityIndirect o = (UnityIndirect)0;

#ifdef LIGHTMAP_ON
    o.diffuse += (DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, lightmapUV.xy))) * occlusion;
#else
    o.diffuse = ShadeSH9(half4(normalWorld, 1.0)) * occlusion;
#endif

#ifndef _GLOSSYREFLECTIONS_OFF
    Unity_GlossyEnvironmentData g;
    g.roughness = perceptualRoughness;
    g.reflUVW = reflectVec;
    o.specular = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, g) * occlusion;
#else
    o.specular = _GlossyEnvironmentColor * occlusion;
#endif

    return o;
}

half SpotAttenuation(half3 spotDirection, half3 lightDirection, float4 attenuationParams)
{
    // Spot Attenuation with a linear falloff can be defined as
    // (SdotL - cosOuterAngle) / (cosInnerAngle - cosOuterAngle)
    // This can be rewritten as
    // invAngleRange = 1.0 / (cosInnerAngle - cosOuterAngle)
    // SdotL * invAngleRange + (-cosOuterAngle * invAngleRange)
    // If we precompute the terms in a MAD instruction
    half SdotL = dot(spotDirection, lightDirection);

    // attenuationParams.x = invAngleRange
    // attenuationParams.y = (-cosOuterAngle  invAngleRange)
    return saturate(SdotL * attenuationParams.x + attenuationParams.y);
}

// In per-vertex falloff there's no smooth falloff to light range. A hard cut will be noticed
inline half ComputeVertexLightAttenuation(LightInput lightInput, half3 normal, float3 worldPos, out half3 lightDirection)
{
    float4 attenuationParams = lightInput.atten;
    float3 posToLightVec = lightInput.pos - worldPos * lightInput.pos.w;
    float distanceSqr = max(dot(posToLightVec, posToLightVec), 0.001);

    // normalized light dir
    lightDirection = half3(posToLightVec * rsqrt(distanceSqr));

    // attenuationParams.z = kQuadFallOff = (25.0) / (lightRange * lightRange)
    // attenuationParams.w = lightRange * lightRange
    half lightAtten = half(1.0 / (1.0 + distanceSqr * attenuationParams.z));
    lightAtten *= SpotAttenuation(lightInput.spotDir.xyz, lightDirection, attenuationParams);
    return lightAtten;
}

// In per-pixel falloff attenuation smoothly decreases to light range.
inline half ComputePixelLightAttenuation(LightInput lightInput, half3 normal, float3 worldPos, out half3 lightDirection)
{
    float4 attenuationParams = lightInput.atten;
    float3 posToLightVec = lightInput.pos.xyz - worldPos * lightInput.pos.w;
    float distanceSqr = max(dot(posToLightVec, posToLightVec), 0.001);

    // normalized light dir
    lightDirection = half3(posToLightVec * rsqrt(distanceSqr));

    float u = (distanceSqr * attenuationParams.z) / attenuationParams.w;
    half lightAtten = tex2D(_AttenuationTexture, float2(u, 0.0)).a;
    lightAtten *= SpotAttenuation(lightInput.spotDir.xyz, lightDirection, attenuationParams);
    return lightAtten;
}

inline half ComputeMainLightAttenuation(LightInput lightInput, half3 normal, float3 worldPos, out half3 lightDirection)
{
#ifdef _MAIN_DIRECTIONAL_LIGHT
    // Light pos holds normalized light dir
    lightDirection = lightInput.pos;
    return 1.0;
#else
    return ComputePixelLightAttenuation(lightInput, normal, worldPos, lightDirection);
#endif
}

inline half3 LightingLambert(half3 diffuseColor, half3 lightDir, half3 normal, half atten)
{
    half NdotL = saturate(dot(normal, lightDir));
    return diffuseColor * (NdotL * atten);
}

inline half3 LightingBlinnPhong(half3 diffuseColor, half4 specularGloss, half3 lightDir, half3 normal, half3 viewDir, half atten)
{
    half NdotL = saturate(dot(normal, lightDir));
    half3 diffuse = diffuseColor * NdotL;

    half3 halfVec = normalize(lightDir + viewDir);
    half NdotH = saturate(dot(normal, halfVec));
    half3 specular = specularGloss.rgb * pow(NdotH, _Shininess * 128.0) * specularGloss.a;
    return (diffuse + specular) * atten;
}

half3 TangentToWorldNormal(half3 normalTangent, half3 tangent, half3 binormal, half3 normal)
{
	half3x3 tangentToWorld = half3x3(tangent, binormal, normal);
	return normalize(mul(normalTangent, tangentToWorld));
}

half4 OutputColor(half3 color, half alpha)
{
#if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
	return half4(LIGHTWEIGHT_LINEAR_TO_GAMMA(color), alpha);
#else
	return half4(LIGHTWEIGHT_LINEAR_TO_GAMMA(color), 1);
#endif
}

half4 LightweightFragmentPBR(half4 lightmapUV, float3 positionWS, half3 normalWS, half3 tangentWS, half3 bitangentWS,
    half3 viewDirectionWS, half fogFactor, half3 albedo, half metallic, half3 specular, half smoothness,
    half3 normalTS, half ambientOcclusion, half3 emission, half alpha)
{
    BRDFData brdfData;
    InitializeBRDFData(albedo, metallic, specular, smoothness, alpha, brdfData);

    half3 vertexNormal = normalWS;
#if _NORMALMAP
    normalWS = TangentToWorldNormal(normalTS, tangentWS, bitangentWS, normalWS);
#else
    normalWS = normalize(normalWS);
#endif

    half3 reflectVec = reflect(-viewDirectionWS, normalWS);
    half roughness2 = brdfData.roughness * brdfData.roughness;
    UnityIndirect indirectLight = LightweightGI(lightmapUV, normalWS, reflectVec, ambientOcclusion, brdfData.perceptualRoughness);

    // PBS
    half fresnelTerm = Pow4(1.0 - saturate(dot(normalWS, viewDirectionWS)));
    half3 color = LightweightBRDFIndirect(brdfData, indirectLight, roughness2, fresnelTerm);
    half3 lightDirectionWS;

    LightInput light;
    INITIALIZE_MAIN_LIGHT(light);
    half lightAtten = ComputeMainLightAttenuation(light, normalWS, positionWS, lightDirectionWS);
    lightAtten *= LIGHTWEIGHT_SHADOW_ATTENUATION(positionWS, normalize(vertexNormal), _ShadowLightDirection.xyz);

    half NdotL = saturate(dot(normalWS, lightDirectionWS));
    half3 radiance = light.color * (lightAtten * NdotL);
    color += LightweightBDRF(brdfData, roughness2, normalWS, lightDirectionWS, viewDirectionWS) * radiance;

#ifdef _ADDITIONAL_LIGHTS
    int pixelLightCount = min(_AdditionalLightCount.x, unity_LightIndicesOffsetAndCount.y);
    for (int lightIter = 0; lightIter < pixelLightCount; ++lightIter)
    {
        LightInput light;
        INITIALIZE_LIGHT(light, lightIter);
        half lightAtten = ComputePixelLightAttenuation(light, normalWS, positionWS, lightDirectionWS);

        half NdotL = saturate(dot(normalWS, lightDirectionWS));
        half3 radiance = light.color * (lightAtten * NdotL);
        color += LightweightBDRF(brdfData, roughness2, normalWS, lightDirectionWS, viewDirectionWS) * radiance;
    }
#endif

    color += emission;

    // Computes fog factor per-vertex
    ApplyFog(color, fogFactor);
    return OutputColor(color, alpha);
}
#endif
