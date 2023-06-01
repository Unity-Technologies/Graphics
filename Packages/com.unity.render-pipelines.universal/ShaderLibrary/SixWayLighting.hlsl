#ifndef SIXWAY_LIGHTING_INCLUDED
#define SIXWAY_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusion.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SixWayLighting.hlsl"

struct SixWaySurfaceData
{
    half3 rightTopBack;
    half3 leftBottomFront;
    half3 emission;
    half4 diffuseGIData0;
    half4 diffuseGIData1;
    half4 diffuseGIData2;
    half3 baseColor;
    half  alpha;
    half occlusion;
    half absorptionRange;
};

LightingData CreateLightingData(SixWaySurfaceData surfaceData)
{
    LightingData lightingData;
    ZERO_INITIALIZE( LightingData, lightingData);
    lightingData.emissionColor = surfaceData.emission;
    lightingData.mainLightColor = 0;
    lightingData.additionalLightsColor = 0;

    return lightingData;
}

#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
void SampleAPVSixWay(APVSample apvSample, half3x3 tbn, out half4 diffuseGIData[3])
{
    [unroll]
    for (int i = 0; i<3; i++)
    {
        EvaluateAPVL1(apvSample, tbn[i], diffuseGIData[i].xyz);
        diffuseGIData[i].w = apvSample.L0[i];
    }
}
#endif

void GatherDiffuseGIData(float3 positionWS, float3 normalWS, float3 tangentWS, inout half4 diffuseGIData0, inout half4 diffuseGIData1, inout half4 diffuseGIData2)
{
    #if defined(LIGHTMAP_ON)
    //Do nothing
    #else
        half4 diffuseGIData[] = {diffuseGIData0, diffuseGIData1, diffuseGIData2};
        float3x3 tbn = float3x3(tangentWS, cross(-normalWS, tangentWS), -normalWS);

        #if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
            APVSample apvSample = SampleAPV(positionWS, normalWS, 0);
            if (apvSample.status != APV_SAMPLE_STATUS_INVALID)
            {
                apvSample.Decode();
                SampleAPVSixWay(apvSample, tbn, diffuseGIData);
            }
        #else
            half3 L0 = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);

            for (int i = 0; i<3; i++)
            {
                diffuseGIData[i].xyz = SHEvalLinearL1(tbn[i], unity_SHAr.xyz, unity_SHAg.xyz, unity_SHAb.xyz);
                diffuseGIData[i].w = L0[i];
            }
        #endif
        diffuseGIData0 = diffuseGIData[0];
        diffuseGIData1 = diffuseGIData[1];
        diffuseGIData2 = diffuseGIData[2];
    #endif
}

half3 ComputeGIColor(SixWaySurfaceData surfaceData)
{
    half4 diffuseGIData0 = surfaceData.diffuseGIData0;
    half4 diffuseGIData1 = surfaceData.diffuseGIData1;
    half4 diffuseGIData2 = surfaceData.diffuseGIData2;

    const half3 L0 = half3(diffuseGIData0.w, diffuseGIData1.w, diffuseGIData2.w);
    const real3 diffuseGIData[3] = {diffuseGIData0.xyz, diffuseGIData1.xyz, diffuseGIData2.xyz};

    #if defined(_BLENDMODE_PREMULTIPLY)
    bool alphaPremultipled = true;
    #else
    bool alphaPremultipled = false;
    #endif

    half3 giColor = GetSixWayDiffuseContributions(surfaceData.rightTopBack, surfaceData.leftBottomFront,
                                            half4(surfaceData.baseColor, surfaceData.alpha), L0, diffuseGIData,
                                            surfaceData.absorptionRange, alphaPremultipled);

    giColor *= surfaceData.occlusion;
    return giColor;
}

half3 SixWayLightBlend(SixWaySurfaceData surfaceData, Light light,  half3x3 tangentToWorld)
{
    half3x3 localFrame = tangentToWorld;
    localFrame[1] *= -1;
    localFrame[2] *= -1;
    half3 dir = mul(localFrame, light.direction);
    half3 weights = dir >= 0 ? surfaceData.rightTopBack.xyz : surfaceData.leftBottomFront.xyz;
    half3 sqrDir = dir*dir;
    half transmission = dot(sqrDir, weights);

    #if defined(_BLENDMODE_PREMULTIPLY)
    bool alphaPremultipled = true;
    #else
    bool alphaPremultipled = false;
    #endif

    half3 cbsdf_R = GetTransmissionWithAbsorption(transmission, half4(surfaceData.baseColor, surfaceData.alpha), surfaceData.absorptionRange, alphaPremultipled);
    half3 radiance = light.color * light.distanceAttenuation * light.shadowAttenuation;
    return PI * cbsdf_R * radiance; // *PI because URP doesn't multiply by the Lambert term in its Lit implementation
}

half4 UniversalFragmentSixWay(InputData inputData, SixWaySurfaceData surfaceData)
{
    if(surfaceData.alpha == 0)
        return half4(0,0,0,0);
    half4 shadowMask = CalculateShadowMask(inputData);
    uint meshRenderingLayers = GetMeshRenderingLayer();
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData.normalizedScreenSpaceUV, 1.0f);
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);

    LightingData lightingData = CreateLightingData(surfaceData);

    lightingData.giColor = ComputeGIColor(surfaceData);

    #ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
    #endif
    {
        lightingData.mainLightColor = SixWayLightBlend(surfaceData, mainLight, inputData.tangentToWorld);
    }

    #if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_FORWARD_PLUS
    for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

        #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
        {
            lightingData.additionalLightsColor += SixWayLightBlend(surfaceData, light, inputData.tangentToWorld);
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

    #ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        #endif
    {
        lightingData.additionalLightsColor += SixWayLightBlend(surfaceData, light, inputData.tangentToWorld);
    }
    LIGHT_LOOP_END
    #endif

    return CalculateFinalColor(lightingData, surfaceData.alpha);
}

#endif
