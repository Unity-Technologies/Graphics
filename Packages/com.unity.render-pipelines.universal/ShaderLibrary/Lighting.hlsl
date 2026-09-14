#ifndef UNIVERSAL_LIGHTING_INCLUDED
#define UNIVERSAL_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DistanceFog.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusion.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.deprecated.hlsl"

#if !defined(_LIGHT_LAYERS_KEYWORD_DECLARED)
    #if !defined(_LIGHT_LAYERS)
        static const bool _LIGHT_LAYERS = 0;
    #elif DEFINED_NONZERO(_LIGHT_LAYERS)
        #undef _LIGHT_LAYERS
        #define _LIGHT_LAYERS 1
    #endif
#endif

bool LightLayersAvailable()
{
    return _LIGHT_LAYERS;
}

bool IsMatchingLightLayer(Light light, uint renderingLayers)
{
    bool isMatching = true;
    if (_LIGHT_LAYERS)
        isMatching = IsMatchingLightLayer(light.layerMask, renderingLayers);
    return isMatching;
}

///////////////////////////////////////////////////////////////////////////////
//                      Lighting Functions                                   //
///////////////////////////////////////////////////////////////////////////////
half3 LightingLambert(half3 lightColor, half3 lightDir, half3 normal)
{
    half NdotL = saturate(dot(normal, lightDir));
    return lightColor * NdotL;
}

half3 LightingSpecular(half3 lightColor, half3 lightDir, half3 normal, half3 viewDir, half4 specular, half smoothness)
{
    float3 halfVec = SafeNormalize(float3(lightDir) + float3(viewDir));
    half NdotH = half(saturate(dot(normal, halfVec)));
    half modifier = pow(float(NdotH), float(smoothness)); // Half produces banding, need full precision
    // NOTE: In order to fix internal compiler error on mobile platforms, this needs to be float3
    float3 specularReflection = specular.rgb * modifier;
    return lightColor * specularReflection;
}

half3 LightingPhysicallyBased(BRDFData brdfData, BRDFData brdfDataClearCoat,
    half3 lightColor, half3 lightDirectionWS, float lightAttenuation,
    half3 normalWS, half3 viewDirectionWS,
    half clearCoatMask, bool useSpecularHighlights, bool useClearCoat)
{
#if (UNITY_PLATFORM_META_QUEST)
    half NdotL = dot(normalWS, lightDirectionWS);
#else
    half NdotL = saturate(dot(normalWS, lightDirectionWS));
#endif

#if (UNITY_PLATFORM_META_QUEST)
    [branch]
    if (NdotL > 0.0)
    {
        half3 radiance = lightColor * (lightAttenuation * saturate(NdotL));
#else
        half3 radiance = lightColor * (lightAttenuation * NdotL);
#endif
        half3 brdf = brdfData.diffuse;
        [branch]
        if (useSpecularHighlights)
        {
            brdf += brdfData.specular * DirectBRDFSpecular(brdfData, normalWS, lightDirectionWS, viewDirectionWS);

            if (useClearCoat)
            {
                // Clear coat evaluates the specular a second time and has some common terms with the base specular.
                // We rely on the compiler to merge these and compute them only once.
                half brdfCoat = kDielectricSpec.r * DirectBRDFSpecular(brdfDataClearCoat, normalWS, lightDirectionWS, viewDirectionWS);

                // Mix clear coat and base layer using khronos glTF recommended formula
                // https://github.com/KhronosGroup/glTF/blob/master/extensions/2.0/Khronos/KHR_materials_clearcoat/README.md
                // Use NoV for direct too instead of LoH as an optimization (NoV is light invariant).
                half NoV = saturate(dot(normalWS, viewDirectionWS));
                // Use slightly simpler fresnelTerm (Pow4 vs Pow5) as a small optimization.
                // It is matching fresnel used in the GI/Env, so should produce a consistent clear coat blend (env vs. direct)
                half coatFresnel = kDielectricSpec.x + kDielectricSpec.a * Pow4(1.0 - NoV);

                brdf = brdf * (1.0 - clearCoatMask * coatFresnel) + brdfCoat * clearCoatMask;
            }
        }
        return brdf * radiance;
#if (UNITY_PLATFORM_META_QUEST)
    }
#endif
    return 0.0;
}

half3 LightingPhysicallyBased(BRDFData brdfData, BRDFData brdfDataClearCoat, Light light, half3 normalWS, half3 viewDirectionWS, half clearCoatMask, bool useSpecularHighlights, bool useClearCoat)
{
    return LightingPhysicallyBased(brdfData, brdfDataClearCoat, light.color, light.direction, light.distanceAttenuation * light.shadowAttenuation, normalWS, viewDirectionWS, clearCoatMask, useSpecularHighlights, useClearCoat);
}

half3 LightingPhysicallyBased(BRDFData brdfData, Light light, half3 normalWS, half3 viewDirectionWS, bool useSpecularHighlights, bool useClearCoat)
{
    const BRDFData noClearCoat = CreateEmptyBRDFData();
    return LightingPhysicallyBased(brdfData, noClearCoat, light, normalWS, viewDirectionWS, 0.0, useSpecularHighlights, useClearCoat);
}

URP_LIGHT_ACCUM3 VertexLighting(float3 positionWS, half3 normalWS)
{
    URP_LIGHT_ACCUM3 vertexLightColor = 0.0;

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    uint lightsCount = GetAdditionalLightsCount();
    uint meshRenderingLayers = GetMeshRenderingLayer();

    LIGHT_LOOP_BEGIN(lightsCount)
        Light light = GetAdditionalLight(lightIndex, positionWS);

    if (IsMatchingLightLayer(light, meshRenderingLayers))
    {
#if defined(UNITY_PLATFORM_META_QUEST)
        if(light.distanceAttenuation > 0.0)
#endif
        {
            half3 lightColor = light.color * light.distanceAttenuation;
            vertexLightColor += LightingLambert(lightColor, light.direction, normalWS);
        }
    }

    LIGHT_LOOP_END
#endif

    return vertexLightColor; // HDR is preserved in the accumulator; ClampExposed narrows it at the shading output
}

struct LightingData
{
    // Accumulator precision toggles with _EXPOSURE (half when off, float when on). See ExposureFunctions.hlsl.
    URP_LIGHT_ACCUM3 giColor;
    URP_LIGHT_ACCUM3 mainLightColor;
    URP_LIGHT_ACCUM3 additionalLightsColor;
    URP_LIGHT_ACCUM3 vertexLightingColor;
    URP_LIGHT_ACCUM3 emissionColor;
};

URP_LIGHT_ACCUM3 CalculateLightingColor(LightingData lightingData, half3 albedo)
{
    URP_LIGHT_ACCUM3 lightingColor = 0;

    if (IsOnlyAOLightingFeatureEnabled())
    {
        return lightingData.giColor; // Contains white + AO
    }

    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_GLOBAL_ILLUMINATION))
    {
        lightingColor += lightingData.giColor;
    }

    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_MAIN_LIGHT))
    {
        lightingColor += lightingData.mainLightColor;
    }

    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_ADDITIONAL_LIGHTS))
    {
        lightingColor += lightingData.additionalLightsColor;
    }

    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_VERTEX_LIGHTING))
    {
        lightingColor += lightingData.vertexLightingColor;
    }

    lightingColor *= albedo;

    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_EMISSION))
    {
        lightingColor += lightingData.emissionColor;
    }

    return lightingColor;
}

URP_LIGHT_ACCUM4 CalculateFinalColor(LightingData lightingData, half alpha)
{
    URP_LIGHT_ACCUM3 finalColor = CalculateLightingColor(lightingData, 1);

    return URP_LIGHT_ACCUM4(finalColor, alpha);
}


// Deprecated: use CalculateFinalColor(lightingData, alpha) + BlendDistanceFog. fogCoord is view-space z.
URP_LIGHT_ACCUM4 CalculateFinalColor(LightingData lightingData, half3 albedo, half alpha, float fogCoord)
{
    URP_LIGHT_ACCUM3 lightingColor = CalculateLightingColor(lightingData, albedo);
    URP_LIGHT_ACCUM3 finalColor = BlendDistanceFogFromEyeDepth(lightingColor, -fogCoord);

    return URP_LIGHT_ACCUM4(finalColor, alpha);
}

LightingData CreateLightingData(InputData inputData, SurfaceData surfaceData)
{
    LightingData lightingData;

    lightingData.giColor = inputData.bakedGI;
    lightingData.emissionColor = surfaceData.emission;
    lightingData.vertexLightingColor = 0;
    lightingData.mainLightColor = 0;
    lightingData.additionalLightsColor = 0;

    return lightingData;
}

// useSpecularHighlights: the material provides a specular color (_SPECGLOSSMAP or _SPECULAR_COLOR).
half3 CalculateBlinnPhong(Light light, InputData inputData, SurfaceData surfaceData, bool useSpecularHighlights, bool useAlphaPremultiply)
{
    half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
    half3 lightDiffuseColor = LightingLambert(attenuatedLightColor, light.direction, inputData.normalWS);

    half3 lightSpecularColor = half3(0,0,0);
    if (useSpecularHighlights)
    {
        half smoothness = exp2(10 * surfaceData.smoothness + 1);
        lightSpecularColor += LightingSpecular(attenuatedLightColor, light.direction, inputData.normalWS, inputData.viewDirectionWS, half4(surfaceData.specular, 1), smoothness);
    }

    if (useAlphaPremultiply)
        lightDiffuseColor *= surfaceData.alpha;

    return lightDiffuseColor * surfaceData.albedo + lightSpecularColor;
}

///////////////////////////////////////////////////////////////////////////////
//                      Fragment Functions                                   //
//       Used by ShaderGraph and others builtin renderers                    //
///////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////
/// PBR lighting...
////////////////////////////////////////////////////////////////////////////////
URP_LIGHT_ACCUM4 UniversalFragmentPBR(InputData inputData, SurfaceData surfaceData, bool isSpecularSetup, bool useSpecularHighlights, bool useAlphaPremultiply, bool useClearCoat, bool receiveShadows, bool isSurfaceTypeTransparent, bool useEnvironmentReflections)
{
    BRDFData brdfData = InitializeBRDFData(surfaceData, isSpecularSetup, useAlphaPremultiply);

    #if defined(DEBUG_DISPLAY)
    float4 debugColor;

    if (CanDebugOverrideOutputColor(inputData, surfaceData, brdfData, debugColor))
    {
        return CompensateDebugColorForPreExposure(debugColor);
    }
    #endif

    BRDFData brdfDataClearCoat = CreateEmptyBRDFData();
    if (useClearCoat)
    {
        // base brdfData is modified here, rely on the compiler to eliminate dead computation by InitializeBRDFData()
        brdfDataClearCoat = InitializeBRDFDataClearCoat(surfaceData.clearCoatMask, surfaceData.clearCoatSmoothness, brdfData);
    }
    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData, isSurfaceTypeTransparent);
    uint meshRenderingLayers = GetMeshRenderingLayer();

#if (UNITY_PLATFORM_META_QUEST)
    if (dot(GetMainLight().direction, inputData.normalWS) <= 0.0)
    {
        inputData.shadowCoord.z = -1; // Force outside of shadowmap
    }
#endif

    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor, receiveShadows, isSurfaceTypeTransparent);

    // NOTE: We don't apply AO to the GI here because it's done in the lighting calculation below...
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

    LightingData lightingData = CreateLightingData(inputData, surfaceData);

    lightingData.giColor = GlobalIllumination(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
                                              inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
                                              inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV, useClearCoat, useEnvironmentReflections);

    if (IsMatchingLightLayer(mainLight, meshRenderingLayers))
    {
        lightingData.mainLightColor = LightingPhysicallyBased(brdfData, brdfDataClearCoat,
                                                              mainLight,
                                                              inputData.normalWS, inputData.viewDirectionWS,
                                                              surfaceData.clearCoatMask, useSpecularHighlights, useClearCoat);
    }

    #if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_CLUSTER_LIGHT_LOOP
    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor, receiveShadows, isSurfaceTypeTransparent);

        if (IsMatchingLightLayer(light, meshRenderingLayers))
        {
            lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
                                                                          inputData.normalWS, inputData.viewDirectionWS,
                                                                          surfaceData.clearCoatMask, useSpecularHighlights, useClearCoat);
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor, receiveShadows, isSurfaceTypeTransparent);

        if (IsMatchingLightLayer(light, meshRenderingLayers))
        {
#if defined(UNITY_PLATFORM_META_QUEST)
            if(light.distanceAttenuation > 0.0)
#endif
            lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
                                                                          inputData.normalWS, inputData.viewDirectionWS,
                                                                          surfaceData.clearCoatMask, useSpecularHighlights, useClearCoat);
        }
    LIGHT_LOOP_END
    #endif

    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
    lightingData.vertexLightingColor += inputData.vertexLighting * brdfData.diffuse;
    #endif

#if !defined(_EXPOSURE) && REAL_IS_HALF && !defined(UNITY_PLATFORM_META_QUEST) // This is platform specific change targeting performance only
    // Clamp any half.inf+ to HALF_MAX
    return min(CalculateFinalColor(lightingData, surfaceData.alpha), HALF_MAX);
#else
    return CalculateFinalColor(lightingData, surfaceData.alpha);
#endif
}

////////////////////////////////////////////////////////////////////////////////
/// Phong lighting...
////////////////////////////////////////////////////////////////////////////////
URP_LIGHT_ACCUM4 UniversalFragmentBlinnPhong(InputData inputData, SurfaceData surfaceData, bool useSpecularHighlights, bool useAlphaPremultiply, bool receiveShadows, bool isSurfaceTypeTransparent)
{
    #if defined(DEBUG_DISPLAY)
    float4 debugColor;

    if (CanDebugOverrideOutputColor(inputData, surfaceData, debugColor))
    {
        return CompensateDebugColorForPreExposure(debugColor);
    }
    #endif

    uint meshRenderingLayers = GetMeshRenderingLayer();
    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData, isSurfaceTypeTransparent);
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor, receiveShadows, isSurfaceTypeTransparent);

    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, aoFactor);

    inputData.bakedGI *= surfaceData.albedo;

    LightingData lightingData = CreateLightingData(inputData, surfaceData);

    if (IsMatchingLightLayer(mainLight, meshRenderingLayers))
    {
        lightingData.mainLightColor += CalculateBlinnPhong(mainLight, inputData, surfaceData, useSpecularHighlights, useAlphaPremultiply);
    }

    #if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_CLUSTER_LIGHT_LOOP
    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor, receiveShadows, isSurfaceTypeTransparent);

        if (IsMatchingLightLayer(light, meshRenderingLayers))
        {
            lightingData.additionalLightsColor += CalculateBlinnPhong(light, inputData, surfaceData, useSpecularHighlights, useAlphaPremultiply);
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor, receiveShadows, isSurfaceTypeTransparent);

        if (IsMatchingLightLayer(light, meshRenderingLayers))
        {
#if defined(UNITY_PLATFORM_META_QUEST)
            if(light.distanceAttenuation > 0.0)
#endif
            lightingData.additionalLightsColor += CalculateBlinnPhong(light, inputData, surfaceData, useSpecularHighlights, useAlphaPremultiply);
        }
    LIGHT_LOOP_END
    #endif

    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
    lightingData.vertexLightingColor += inputData.vertexLighting * surfaceData.albedo;
    #endif

    return CalculateFinalColor(lightingData, surfaceData.alpha);
}

////////////////////////////////////////////////////////////////////////////////
/// Unlit
////////////////////////////////////////////////////////////////////////////////
URP_LIGHT_ACCUM4 UniversalFragmentBakedLit(InputData inputData, SurfaceData surfaceData, bool isSurfaceTypeTransparent)
{
    #if defined(DEBUG_DISPLAY)
    float4 debugColor;

    if (CanDebugOverrideOutputColor(inputData, surfaceData, debugColor))
    {
        return CompensateDebugColorForPreExposure(debugColor);
    }
    #endif

    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData, isSurfaceTypeTransparent);
    LightingData lightingData = CreateLightingData(inputData, surfaceData);

    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_AMBIENT_OCCLUSION))
    {
        lightingData.giColor *= aoFactor.indirectAmbientOcclusion;
    }

    // Fog is applied by the caller via BlendDistanceFog(color, positionCS).
    return half4(CalculateLightingColor(lightingData, surfaceData.albedo), surfaceData.alpha);
}

#endif
