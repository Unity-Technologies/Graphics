#ifndef LIGHTWEIGHT_LIGHTING_INCLUDED
#define LIGHTWEIGHT_LIGHTING_INCLUDED

#include "LightweightCore.cginc"
#include "LightweightShadows.cginc"

#define PI 3.14159265359f
#define kDieletricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)

#ifndef UNITY_SPECCUBE_LOD_STEPS
#define UNITY_SPECCUBE_LOD_STEPS 6
#endif

#ifdef NO_ADDITIONAL_LIGHTS
#undef _ADDITIONAL_LIGHTS
#endif

// If lightmap is not defined than we evaluate GI (ambient + probes) from SH
// We might do it fully or partially in vertex to save shader ALU
#if !defined(LIGHTMAP_ON)
    #if SHADER_TARGET < 30
        // Evaluates SH fully in vertex
        #define EVALUATE_SH_VERTEX
    #else
        // Evaluates L2 SH in vertex and L0L1 in pixel
        #define EVALUATE_SH_MIXED
    #endif
#endif

///////////////////////////////////////////////////////////////////////////////
//                         BRDF Functions                                    //
///////////////////////////////////////////////////////////////////////////////
struct BRDFData
{
    half3 diffuse;
    half3 specular;
    half perceptualRoughness;
    half roughness;
    half grazingTerm;
};

half ReflectivitySpecular(half3 specular)
{
#if (SHADER_TARGET < 30)
    // SM2.0: instruction count limitation
    return specular.r; // Red channel - because most metals are either monocrhome or with redish/yellowish tint
#else
    return max(max(specular.r, specular.g), specular.b);
#endif
}

half OneMinusReflectivityMetallic(half metallic)
{
    // We'll need oneMinusReflectivity, so
    //   1-reflectivity = 1-lerp(dielectricSpec, 1, metallic) = lerp(1-dielectricSpec, 0, metallic)
    // store (1-dielectricSpec) in kDieletricSpec.a, then
    //   1-reflectivity = lerp(alpha, 0, metallic) = alpha + metallic*(0 - alpha) =
    //                  = alpha - metallic * alpha
    half oneMinusDielectricSpec = kDieletricSpec.a;
    return oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
}

inline void InitializeBRDFData(half3 albedo, half metallic, half3 specular, half smoothness, half alpha, out BRDFData outBRDFData)
{
#ifdef _SPECULAR_SETUP
    half reflectivity = ReflectivitySpecular(specular);
    half oneMinusReflectivity = 1.0 - reflectivity;

    outBRDFData.diffuse = albedo * (half3(1.0h, 1.0h, 1.0h) - specular);
    outBRDFData.specular = specular;
#else

    half oneMinusReflectivity = OneMinusReflectivityMetallic(metallic);
    half reflectivity = 1.0 - oneMinusReflectivity;

    outBRDFData.diffuse = albedo * oneMinusReflectivity;
    outBRDFData.specular = lerp(kDieletricSpec.rgb, albedo, metallic);
#endif

    outBRDFData.grazingTerm = saturate(smoothness + reflectivity);
    outBRDFData.perceptualRoughness = 1.0h - smoothness;
    outBRDFData.roughness = outBRDFData.perceptualRoughness * outBRDFData.perceptualRoughness;

#ifdef _ALPHAPREMULTIPLY_ON
    outBRDFData.diffuse *= alpha;
    alpha = alpha * oneMinusReflectivity + reflectivity;
#endif
}

half3 LightweightEnvironmentBRDF(BRDFData brdfData, half3 indirectDiffuse, half3 indirectSpecular, half roughness2, half fresnelTerm)
{
    half3 c = indirectDiffuse * brdfData.diffuse;
    float surfaceReduction = 1.0 / (roughness2 + 1.0);
    c += surfaceReduction * indirectSpecular * lerp(brdfData.specular, brdfData.grazingTerm, fresnelTerm);
    return c;
}

// Based on Minimalist CookTorrance BRDF
// Implementation is slightly different from original derivation: http://www.thetenthplanet.de/archives/255
//
// * NDF [Modified] GGX
// * Modified Kelemen and Szirmay-​Kalos for Visibility term
// * Fresnel approximated with 1/LdotH
half3 LightweightDirectBDRF(BRDFData brdfData, half roughness2, half3 normal, half3 lightDirection, half3 viewDir)
{
#ifndef _SPECULARHIGHLIGHTS_OFF
    half3 halfDir = SafeNormalize(lightDirection + viewDir);

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

///////////////////////////////////////////////////////////////////////////////
//                        Attenuation Functions                               /
///////////////////////////////////////////////////////////////////////////////
half CookieAttenuation(float3 worldPos)
{
#ifdef _MAIN_LIGHT_COOKIE
#ifdef _MAIN_DIRECTIONAL_LIGHT
    float2 cookieUV = mul(_WorldToLight, float4(worldPos, 1.0)).xy;
    return tex2D(_MainLightCookie, cookieUV).a;
#elif defined(_MAIN_SPOT_LIGHT)
    float4 projPos = mul(_WorldToLight, float4(worldPos, 1.0));
    float2 cookieUV = projPos.xy / projPos.w + 0.5;
    return tex2D(_MainLightCookie, cookieUV).a;
#endif // POINT LIGHT cookie not supported
#endif

    return 1;
}

// Matches Unity Vanila attenuation
// Attenuation smoothly decreases to light range.
half DistanceAttenuation(half3 distanceSqr, half4 distanceAttenuation)
{
    // We use a shared distance attenuation for additional directional and puctual lights
    // for directional lights attenuation will be 1
    half quadFalloff = distanceAttenuation.x;
    half denom = distanceSqr * quadFalloff + 1.0;
    half lightAtten = 1.0 / denom;

    // We need to smoothly fade attenuation to light range. We start fading linearly at 80% of light range
    // Therefore:
    // fadeDistance = (0.8 * 0.8 * lightRangeSq)
    // smoothFactor = (lightRangeSqr - distanceSqr) / (lightRangeSqr - fadeDistance)
    // We can rewrite that to fit a MAD by doing
    // distanceSqr * (1.0 / (fadeDistanceSqr - lightRangeSqr)) + (-lightRangeSqr / (fadeDistanceSqr - lightRangeSqr)
    // distanceSqr *        distanceAttenuation.y            +             distanceAttenuation.z
    half smoothFactor = saturate(distanceSqr * distanceAttenuation.y + distanceAttenuation.z);
    return lightAtten * smoothFactor;
}

half SpotAttenuation(half3 spotDirection, half3 lightDirection, half4 spotAttenuation)
{
    // Spot Attenuation with a linear falloff can be defined as
    // (SdotL - cosOuterAngle) / (cosInnerAngle - cosOuterAngle)
    // This can be rewritten as
    // invAngleRange = 1.0 / (cosInnerAngle - cosOuterAngle)
    // SdotL * invAngleRange + (-cosOuterAngle * invAngleRange)
    // SdotL * spotAttenuation.x + spotAttenuation.y

    // If we precompute the terms in a MAD instruction
    half SdotL = dot(spotDirection, lightDirection);
    return saturate(SdotL * spotAttenuation.x + spotAttenuation.y);
}

inline half GetLightDirectionAndRealtimeAttenuation(LightInput lightInput, half3 normal, float3 worldPos, out half3 lightDirection)
{
    float3 posToLightVec = lightInput.pos.xyz - worldPos * lightInput.pos.w;
    float distanceSqr = max(dot(posToLightVec, posToLightVec), 0.001);

    // normalized light dir
    lightDirection = half3(posToLightVec * rsqrt(distanceSqr));
    half lightAtten = DistanceAttenuation(distanceSqr, lightInput.distanceAttenuation);
    lightAtten *= SpotAttenuation(lightInput.spotDirection.xyz, lightDirection, lightInput.spotAttenuation);
    return lightAtten;
}

inline half GetMainLightDirectionAndRealtimeAttenuation(LightInput lightInput, half3 normalWS, float3 positionWS, out half3 lightDirection)
{
#ifdef _MAIN_DIRECTIONAL_LIGHT
    // Light pos holds normalized light dir
    lightDirection = lightInput.pos;
    half attenuation = 1.0;
#else
    half attenuation = GetLightDirectionAndRealtimeAttenuation(lightInput, normalWS, positionWS, lightDirection);
#endif

    // Cookies and shadows are only computed for main light
    attenuation *= CookieAttenuation(positionWS);
    attenuation *= LIGHTWEIGHT_SHADOW_ATTENUATION(positionWS, normalWS, lightDirection);

    return attenuation;
}

///////////////////////////////////////////////////////////////////////////////
//                      Lighting Functions                                   //
///////////////////////////////////////////////////////////////////////////////
half3 LightingLambert(half3 lightColor, half3 lightDir, half3 normal)
{
    half NdotL = saturate(dot(normal, lightDir));
    return lightColor * NdotL;
}

half3 LightingSpecular(half3 lightColor, half3 lightDir, half3 normal, half3 viewDir, half4 specularGloss, half shininess)
{
    half3 halfVec = SafeNormalize(lightDir + viewDir);
    half NdotH = saturate(dot(normal, halfVec));
    half3 specularReflection = specularGloss.rgb * pow(NdotH, shininess) * specularGloss.a;
    return lightColor * specularReflection;
}

half3 VertexLighting(float3 positionWS, half3 normalWS)
{
    half3 vertexLightColor = half3(0.0, 0.0, 0.0);

#if defined(_VERTEX_LIGHTS)
    int vertexLightStart = _AdditionalLightCount.x;
    int vertexLightEnd = min(_AdditionalLightCount.y, unity_LightIndicesOffsetAndCount.y);
    for (int lightIter = vertexLightStart; lightIter < vertexLightEnd; ++lightIter)
    {
        LightInput light;
        INITIALIZE_LIGHT(light, lightIter);

        half3 lightDirection;
        half atten = GetLightDirectionAndRealtimeAttenuation(light, normalWS, positionWS, lightDirection);
        half3 lightColor = light.color * atten;
        vertexLightColor += LightingLambert(lightColor, lightDirection, normalWS);
    }
#endif

    return vertexLightColor;
}

///////////////////////////////////////////////////////////////////////////////
//                      Global Illumination                                  //
///////////////////////////////////////////////////////////////////////////////
half3 DiffuseGI(half3 indirectDiffuse, half3 lambert, half mainLightRealtimeAttenuation, half occlusion)
{
    // If shadows and mixed subtractive mode is enabled we need to remove direct
    // light contribution from lightmap from occluded pixels so we can have dynamic objects
    // casting shadows onto static correctly.
#if defined(_MIXED_LIGHTING_SUBTRACTIVE) && defined(LIGHTMAP_ON) && defined(_SHADOWS)
    indirectDiffuse = SubtractDirectMainLightFromLightmap(indirectDiffuse, mainLightRealtimeAttenuation, lambert);
#endif

    return indirectDiffuse * occlusion;
}

half3 GlossyEnvironmentReflection(half3 viewDirectionWS, half3 normalWS, half perceptualRoughness, half occlusion)
{
    half3 reflectVector = reflect(-viewDirectionWS, normalWS);

#if !defined(_GLOSSYREFLECTIONS_OFF)
    half roughness = perceptualRoughness * (1.7 - 0.7 * perceptualRoughness);
    half mip = roughness * UNITY_SPECCUBE_LOD_STEPS;
    half4 rgbm = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflectVector, mip);
    return DecodeHDR(rgbm, unity_SpecCube0_HDR) * occlusion;
#endif

    return _GlossyEnvironmentColor * occlusion;
}

///////////////////////////////////////////////////////////////////////////////
//                      Fragment Functions                                   //
//       Used by ShaderGraph and others builtin renderers                    //
///////////////////////////////////////////////////////////////////////////////
half4 LightweightFragmentPBR(float3 positionWS, half3 normalWS, half3 viewDirectionWS,
    half3 bakedGI, half3 vertexLighting, half3 albedo, half metallic, half3 specular,
    half smoothness, half occlusion, half3 emission, half alpha)
{
    half4 bakedOcclusion = half4(0, 0, 0, 0);
    BRDFData brdfData;
    InitializeBRDFData(albedo, metallic, specular, smoothness, alpha, brdfData);

    half3 lightDirectionWS;

    LightInput mainLight;
    INITIALIZE_MAIN_LIGHT(mainLight);

    // No distance fade.
    half realtimeMainLightAtten = GetMainLightDirectionAndRealtimeAttenuation(mainLight, normalWS, positionWS, lightDirectionWS);
    half NdotL = saturate(dot(normalWS, lightDirectionWS));
    half3 radiance = mainLight.color * NdotL;

    half3 indirectDiffuse = DiffuseGI(bakedGI, radiance, realtimeMainLightAtten, occlusion);
    half3 indirectSpecular = GlossyEnvironmentReflection(viewDirectionWS, normalWS, brdfData.perceptualRoughness, occlusion);

    half roughness2 = brdfData.roughness * brdfData.roughness;
    half fresnelTerm = _Pow4(1.0 - saturate(dot(normalWS, viewDirectionWS)));
    half3 color = LightweightEnvironmentBRDF(brdfData, indirectDiffuse, indirectSpecular, roughness2, fresnelTerm);

    half mainLightAtten = MixRealtimeAndBakedOcclusion(realtimeMainLightAtten, bakedOcclusion, mainLight.distanceAttenuation);
    radiance *= mainLightAtten;

    color += LightweightDirectBDRF(brdfData, roughness2, normalWS, lightDirectionWS, viewDirectionWS) * radiance;
    color += vertexLighting * brdfData.diffuse;

#ifdef _ADDITIONAL_LIGHTS
    int pixelLightCount = min(_AdditionalLightCount.x, unity_LightIndicesOffsetAndCount.y);
    for (int lightIter = 0; lightIter < pixelLightCount; ++lightIter)
    {
        LightInput light;
        INITIALIZE_LIGHT(light, lightIter);

        half lightAttenuation = GetLightDirectionAndRealtimeAttenuation(light, normalWS, positionWS, lightDirectionWS);
        lightAttenuation = MixRealtimeAndBakedOcclusion(lightAttenuation, bakedOcclusion, light.distanceAttenuation);

        half NdotL = saturate(dot(normalWS, lightDirectionWS));
        half3 radiance = light.color * (lightAttenuation * NdotL);
        color += LightweightDirectBDRF(brdfData, roughness2, normalWS, lightDirectionWS, viewDirectionWS) * radiance;
    }
#endif

    color += emission;
    return half4(color, alpha);
}

half4 LightweightFragmentLambert(float3 positionWS, half3 normalWS, half3 viewDirectionWS,
    half fogFactor, half3 diffuseGI, half3 diffuse, half3 emission, half alpha)
{
    half4 bakedOcclusion = half4(0, 0, 0, 0);
    half3 lightDirection;

    LightInput mainLight;
    INITIALIZE_MAIN_LIGHT(mainLight);
    half realtimeMainLightAtten = GetMainLightDirectionAndRealtimeAttenuation(mainLight, normalWS, positionWS, lightDirection);
    half3 NdotL = saturate(dot(normalWS, lightDirection));
    half3 lambert = mainLight.color * NdotL;

    half3 indirectDiffuse = DiffuseGI(diffuseGI, lambert, realtimeMainLightAtten, 1.0);
    half mainLightAtten = MixRealtimeAndBakedOcclusion(realtimeMainLightAtten, bakedOcclusion, mainLight.distanceAttenuation);

    half3 diffuseColor = lambert * mainLightAtten + indirectDiffuse;

#ifdef _ADDITIONAL_LIGHTS
    int pixelLightCount = min(_AdditionalLightCount.x, unity_LightIndicesOffsetAndCount.y);
    for (int lightIter = 0; lightIter < pixelLightCount; ++lightIter)
    {
        LightInput light;
        INITIALIZE_LIGHT(light, lightIter);

        half lightAttenuation = GetLightDirectionAndRealtimeAttenuation(light, normalWS, positionWS, lightDirection);
        lightAttenuation = MixRealtimeAndBakedOcclusion(lightAttenuation, bakedOcclusion, light.distanceAttenuation);

        half3 attenuatedLightColor = light.color * lightAttenuation;
        diffuseColor += LightingLambert(attenuatedLightColor, lightDirection, normalWS);
    }
#endif

    half3 finalColor = diffuseColor * diffuse + emission;

    // Computes Fog Factor per vextex
    ApplyFog(finalColor, fogFactor);
    half4 color = half4(finalColor, alpha);
    return OUTPUT_COLOR(color);
}

half4 LightweightFragmentBlinnPhong(float3 positionWS, half3 normalWS, half3 viewDirectionWS,
    half fogFactor, half3 diffuseGI, half3 diffuse, half4 specularGloss, half shininess, half3 emission, half alpha)
{
    half4 bakedOcclusion = half4(0, 0, 0, 0);
    half3 lightDirection;

    LightInput mainLight;
    INITIALIZE_MAIN_LIGHT(mainLight);
    half realtimeMainLightAtten = GetMainLightDirectionAndRealtimeAttenuation(mainLight, normalWS, positionWS, lightDirection);
    half3 NdotL = saturate(dot(normalWS, lightDirection));
    half3 lambert = mainLight.color * NdotL;

    half3 indirectDiffuse = DiffuseGI(diffuseGI, lambert, realtimeMainLightAtten, 1.0);
    half mainLightAtten = MixRealtimeAndBakedOcclusion(realtimeMainLightAtten, bakedOcclusion, mainLight.distanceAttenuation);

    half3 diffuseColor = lambert * mainLightAtten + indirectDiffuse;
    half3 specularColor = LightingSpecular(mainLight.color * mainLightAtten, lightDirection, normalWS, viewDirectionWS, specularGloss, shininess);

#ifdef _ADDITIONAL_LIGHTS
    int pixelLightCount = min(_AdditionalLightCount.x, unity_LightIndicesOffsetAndCount.y);
    for (int lightIter = 0; lightIter < pixelLightCount; ++lightIter)
    {
        LightInput light;
        INITIALIZE_LIGHT(light, lightIter);
        half lightAttenuation = GetLightDirectionAndRealtimeAttenuation(light, normalWS, positionWS, lightDirection);
        lightAttenuation = MixRealtimeAndBakedOcclusion(lightAttenuation, bakedOcclusion, light.distanceAttenuation);

        half3 attenuatedLightColor = light.color * lightAttenuation;
        diffuseColor += LightingLambert(attenuatedLightColor, lightDirection, normalWS);
        specularColor += LightingSpecular(attenuatedLightColor, lightDirection, normalWS, viewDirectionWS, specularGloss, shininess);
    }
#endif

    half3 finalColor = diffuseColor * diffuse + emission;
    finalColor += specularColor;

    // Computes Fog Factor per vextex
    ApplyFog(finalColor, fogFactor);
    half4 color = half4(finalColor, alpha);
    return OUTPUT_COLOR(color);
}
#endif
