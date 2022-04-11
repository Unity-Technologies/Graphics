// Upgrade NOTE: replaced 'defined at' with 'defined (at)'
#ifndef SHADERPASS
#error SHADERPASS must be defined (at) this point
#endif

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"

float3 VFXGetPositionRWS(VFX_VARYING_PS_INPUTS i)
{
    float3 posWS = (float3)0;
    #ifdef VFX_VARYING_POSWS
    posWS = i.VFX_VARYING_POSWS;
    #endif
    return VFXGetPositionRWS(posWS);
}

InputData VFXGetInputData(const VFX_VARYING_PS_INPUTS i, const PositionInputs posInputs, const SurfaceData surfaceData, float3 normalWS)
{
    InputData inputData = (InputData)0;

    inputData.positionWS = posInputs.positionWS.xyz;
    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(inputData.positionWS);


//When there is only one cascaded, this shadowCoord can be computed at vertex stage
//#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
//    inputData.shadowCoord = inputData.shadowCoord;
#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

    //This ComputeFogFactor can be moved to vertex and use interpolator instead
    float fogFactor = ComputeFogFactor(i.VFX_VARYING_POSCS.z);
    inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), fogFactor);

    //SampleSH could partially be done on vertex using SampleSHVertex & SampleSHPixel
    //For now, use directly the simpler per pixel fallback
    inputData.bakedGI = SampleSH(normalWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.VFX_VARYING_POSCS);

    //No static light map in VFX
    //inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
    return inputData;
}


#ifndef VFX_SHADERGRAPH

SurfaceData VFXGetSurfaceData(const VFX_VARYING_PS_INPUTS i, float3 normalWS, const VFXUVData uvData, out float opacity)
{
    SurfaceData surfaceData = (SurfaceData)0;

    float4 baseColorMapSample = (float4)1.0f;
    float4 color = float4(1,1,1,1);
    #if URP_USE_BASE_COLOR
    color *= VFXGetParticleColor(i);
    #elif URP_USE_ADDITIONAL_BASE_COLOR
    #if defined(VFX_VARYING_COLOR)
    color.xyz *= i.VFX_VARYING_COLOR;
    #endif
    #if defined(VFX_VARYING_ALPHA)
    color.a *= i.VFX_VARYING_ALPHA;
    #endif
    #endif
    #if URP_USE_BASE_COLOR_MAP
    baseColorMapSample = SampleTexture(VFX_SAMPLER(baseColorMap),uvData);
    #if URP_USE_BASE_COLOR_MAP_COLOR
    color.xyz *= baseColorMapSample.xyz;
    #endif
    #if URP_USE_BASE_COLOR_MAP_ALPHA
    color.a *= baseColorMapSample.a;
    #endif
    #endif
    color.a *= VFXGetSoftParticleFade(i);
    VFXClipFragmentColor(color.a,i);
    surfaceData.albedo = saturate(color.rgb);

    #if IS_OPAQUE_PARTICLE
    opacity = 1.0f;
    #else
    opacity = saturate(color.a);
    #endif
    surfaceData.alpha = opacity;

    float4 metallicMapSample = (float4)1.0f;
    float4 specularMapSample = (float4)1.0f;
    #if URP_MATERIAL_TYPE_METALLIC
    surfaceData.metallic = 1.0f;
    #ifdef VFX_VARYING_METALLIC
    surfaceData.metallic *= i.VFX_VARYING_METALLIC;
    #endif
    #if URP_USE_METALLIC_MAP
    metallicMapSample = SampleTexture(VFX_SAMPLER(metallicMap), uvData);
    surfaceData.metallic *= metallicMapSample.r;
    #endif
    #elif URP_MATERIAL_TYPE_SPECULAR
    surfaceData.specular = (float3)1.0f;
    #ifdef VFX_VARYING_SPECULAR
    surfaceData.specular *= saturate(i.VFX_VARYING_SPECULAR);
    #endif
    #if URP_USE_SPECULAR_MAP
    specularMapSample = SampleTexture(VFX_SAMPLER(specularMap), uvData);
    surfaceData.specular *= specularMapSample.rgb;
    #endif
    #endif

    surfaceData.normalTS = float3(1.0f, 0.0f, 0.0f); //NormalWS is directly modified in VFX

    surfaceData.smoothness = 1.0f;
    #ifdef VFX_VARYING_SMOOTHNESS
    surfaceData.smoothness *= i.VFX_VARYING_SMOOTHNESS;
    #endif
    #if URP_USE_SMOOTHNESS_IN_ALBEDO
    surfaceData.smoothness *= baseColorMapSample.a;
    #elif URP_USE_SMOOTHNESS_IN_METALLIC
    surfaceData.smoothness *= metallicMapSample.a;
    #elif URP_USE_SMOOTHNESS_IN_SPECULAR
    surfaceData.smoothness *= specularMapSample.a;
    #endif

    surfaceData.occlusion = 1.0f;
    #if URP_USE_OCCLUSION_MAP
    float4 mask = SampleTexture(VFX_SAMPLER(occlusionMap),uvData);
    surfaceData.occlusion *= mask.g;
    #endif

    #if URP_USE_EMISSIVE
    surfaceData.emission = float3(1, 1, 1);
    #if URP_USE_EMISSIVE_MAP
    float emissiveScale = 1.0f;
    #ifdef VFX_VARYING_EMISSIVESCALE
    emissiveScale = i.VFX_VARYING_EMISSIVESCALE;
    #endif
    surfaceData.emission *= SampleTexture(VFX_SAMPLER(emissiveMap), uvData).rgb * emissiveScale;
    #endif
    #if defined(VFX_VARYING_EMISSIVE) && (URP_USE_EMISSIVE_COLOR || URP_USE_ADDITIONAL_EMISSIVE_COLOR)
    surfaceData.emission *= i.VFX_VARYING_EMISSIVE;
    #endif
    #ifdef VFX_VARYING_EXPOSUREWEIGHT
    surfaceData.emission *= lerp(GetInverseCurrentExposureMultiplier(), 1.0f, i.VFX_VARYING_EXPOSUREWEIGHT);
    #endif
    #endif

    return surfaceData;
}


#endif
