#ifndef LIGHTWEIGHT_LIGHTING_INCLUDED
#define LIGHTWEIGHT_LIGHTING_INCLUDED

#include "LightweightCore.cginc"
#include "LightweightShadows.cginc"

#define PI 3.14159265359f
#define kDieletricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)

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

#if (defined(_MAIN_DIRECTIONAL_LIGHT) || defined(_MAIN_SPOT_LIGHT) || defined(_MAIN_POINT_LIGHT))
#define _MAIN_LIGHT
#endif

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
    half3 ambient;
};

struct SurfaceInput
{
    float4 lightmapUV;
    half3 normalWS;
    half3 tangentWS;
    half3 bitangentWS;
    float3 positionWS;
    half3  viewDirectionWS;
    float  fogFactor;
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
    outSurfaceData.ambient = half3(0.0h, 0.0h, 0.0h);
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

UnityIndirect LightweightGI(float4 lightmapUV, half3 ambientColor, half3 normalWorld, half3 reflectVec, half occlusion, half perceptualRoughness)
{
    UnityIndirect o = (UnityIndirect)0;
#ifdef LIGHTMAP_ON
    ambientColor += (DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, lightmapUV.xy)));
#else
    ambientColor += SHEvalLinearL0L1(half4(normalWorld, 1.0));
    ambientColor = max(half3(0.0, 0.0, 0.0), ambientColor);
#endif

    o.diffuse = ambientColor * occlusion;

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

inline half ComputeLightAttenuationVertex(LightInput lightInput, half3 normal, float3 worldPos, out half3 lightDirection)
{
    float4 attenuationParams = lightInput.atten;
    float3 posToLightVec = lightInput.pos - worldPos;
    float distanceSqr = max(dot(posToLightVec, posToLightVec), 0.001);

    //// attenuationParams.z = kQuadFallOff = (25.0) / (lightRange * lightRange)
    //// attenuationParams.w = lightRange * lightRange
    //// TODO: we can precompute 1.0 / (attenuationParams.w * 0.64 - attenuationParams.w)
    //// falloff is computed from 80% light range squared
    float lightAtten = half(1.0 / (1.0 + distanceSqr * attenuationParams.z));

    // normalized light dir
    lightDirection = half3(posToLightVec * rsqrt(distanceSqr));

    half SdotL = saturate(dot(lightInput.spotDir.xyz, lightDirection));
    lightAtten *= saturate((SdotL - attenuationParams.x) / attenuationParams.y);

    return half(lightAtten);
}

inline half ComputeLightAttenuation(LightInput lightInput, half3 normal, float3 worldPos, out half3 lightDirection)
{
    float4 attenuationParams = lightInput.atten;

    float3 posToLightVec = lightInput.pos.xyz - worldPos * lightInput.pos.w;
    float distanceSqr = max(dot(posToLightVec, posToLightVec), 0.001);

#ifdef _ATTENUATION_TEXTURE
    float u = (distanceSqr * attenuationParams.z) / attenuationParams.w;
    float lightAtten = tex2D(_AttenuationTexture, float2(u, 0.0)).a;
#else
    //// attenuationParams.z = kQuadFallOff = (25.0) / (lightRange * lightRange)
    //// attenuationParams.w = lightRange * lightRange
    //// TODO: we can precompute 1.0 / (attenuationParams.w * 0.64 - attenuationParams.w)
    //// falloff is computed from 80% light range squared
    float lightAtten = half(1.0 / (1.0 + distanceSqr * attenuationParams.z));
    float falloff = saturate((distanceSqr - attenuationParams.w) / (attenuationParams.w * 0.64 - attenuationParams.w));
    lightAtten *= half(falloff);
#endif

    // normalized light dir
    lightDirection = half3(posToLightVec * rsqrt(distanceSqr));

    half SdotL = saturate(dot(lightInput.spotDir.xyz, lightDirection));
    lightAtten *= saturate((SdotL - attenuationParams.x) / attenuationParams.y);
    return half(lightAtten);
}

inline half ComputeMainLightAttenuation(LightInput lightInput, half3 normal, float3 worldPos, out half3 lightDirection)
{
#ifdef _MAIN_DIRECTIONAL_LIGHT
    // Light pos holds normalized light dir
    lightDirection = lightInput.pos;
    return 1.0;
#else
    return ComputeLightAttenuation(lightInput, normal, worldPos, lightDirection);
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

half4 LightweightFragmentPBR(half4 lightmapUV, float3 positionWS, half3 normalWS, half3 tangentWS, half3 bitangentWS,
    half3 viewDirectionWS, half fogFactor, half3 albedo, half metallic, half3 specular, half smoothness,
    half3 normalTS, half ambientOcclusion, half3 emission, half alpha, half3 ambient)
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
    UnityIndirect indirectLight = LightweightGI(lightmapUV, ambient, normalWS, reflectVec, ambientOcclusion, brdfData.perceptualRoughness);

    // PBS
    half fresnelTerm = Pow4(1.0 - saturate(dot(normalWS, viewDirectionWS)));
    half3 color = LightweightBRDFIndirect(brdfData, indirectLight, roughness2, fresnelTerm);
    half3 lightDirectionWS;

#ifdef _MAIN_LIGHT
    LightInput light;
    INITIALIZE_MAIN_LIGHT(light);
    half lightAtten = ComputeMainLightAttenuation(light, normalWS, positionWS, lightDirectionWS);
    lightAtten *= LIGHTWEIGHT_SHADOW_ATTENUATION(positionWS, normalize(vertexNormal), _ShadowLightDirection.xyz);

    half NdotL = saturate(dot(normalWS, lightDirectionWS));
    half3 radiance = light.color * (lightAtten * NdotL);
    color += LightweightBDRF(brdfData, roughness2, normalWS, lightDirectionWS, viewDirectionWS) * radiance;
#endif

#ifdef _ADDITIONAL_PIXEL_LIGHTS
    int pixelLightCount = min(_AdditionalLightCount.x, unity_LightIndicesOffsetAndCount.y);
    for (int lightIter = 0; lightIter < pixelLightCount; ++lightIter)
    {
        LightInput light;
        INITIALIZE_LIGHT(light, lightIter);
        half lightAtten = ComputeLightAttenuation(light, normalWS, positionWS, lightDirectionWS);

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
