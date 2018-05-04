
#ifdef DEBUG_DISPLAY
#include "HDRP/Debug/DebugDisplay.hlsl"
#endif
#include "HDRP/Material/Material.hlsl"
//#include "HDRP/Material/BuiltIn/BuiltInData.cs.hlsl"

float3 VFXSampleLightProbes(float3 normalWS)
{
    real4 SHCoefficients[7];
    SHCoefficients[0] = unity_SHAr;
    SHCoefficients[1] = unity_SHAg;
    SHCoefficients[2] = unity_SHAb;
    SHCoefficients[3] = unity_SHBr;
    SHCoefficients[4] = unity_SHBg;
    SHCoefficients[5] = unity_SHBb;
    SHCoefficients[6] = unity_SHC;

    return SampleSH9(SHCoefficients, normalWS);
}

float3 VFXGetAmbient(VFX_VARYING_PS_INPUTS i,const SurfaceData surfaceData)
{
    float3 ambient = surfaceData.baseColor * VFXSampleLightProbes(surfaceData.normalWS);

    #if HDRP_USE_EMISSIVE
    float3 emissive = float3(1,1,1);
    #if HDRP_USE_EMISSIVE_MAP
    emissive *= VFXGetTextureColorWithProceduralUV(VFX_SAMPLER(emissiveMap),i,uvs).rgb * i.materialProperties.w;
    #endif
    #if HDRP_USE_EMISSIVE_COLOR || HDRP_USE_ADDITIONAL_EMISSIVE_COLOR
    emissive *= i.emissiveColor;
    #endif
    ambient += emissive;
    #endif

    return ambient;
}

SurfaceData VFXGetSurfaceData(VFX_VARYING_PS_INPUTS i, float3 normalWS,float2 uvs, uint diffusionProfile)
{
    SurfaceData surfaceData = (SurfaceData)0;

    float4 color = float4(1,1,1,1);
    #if HDRP_USE_BASE_COLOR
    color *= VFXGetFragmentColor(i);
    VFXClipFragmentColor(color.a,i);
    #elif HDRP_USE_ADDITIONAL_BASE_COLOR
    color *= i.color;
    #endif
    surfaceData.baseColor = color.rgb;

    #if HDRP_MATERIAL_TYPE_STANDARD
    surfaceData.materialFeatures = 1;
    surfaceData.metallic = i.materialProperties.y;
    #elif HDRP_MATERIAL_TYPE_SPECULAR
    surfaceData.materialFeatures = 2;
    surfaceData.specularColor = i.specularColor;
    #elif HDRP_MATERIAL_TYPE_TRANSLUCENT
    surfaceData.materialFeatures = 8;
    surfaceData.thickness = i.materialProperties.y;
    surfaceData.diffusionProfile = diffusionProfile;
    surfaceData.subsurfaceMask = 1.0f;
    #endif

    surfaceData.normalWS = normalWS;
    surfaceData.perceptualSmoothness = i.materialProperties.x;
    surfaceData.specularOcclusion = 1.0f;
    surfaceData.ambientOcclusion = 1.0f;

    #if HDRP_USE_BASE_COLOR_MAP
    surfaceData.baseColor *= VFXGetTextureColorWithProceduralUV(VFX_SAMPLER(baseColorMap),i,uvs);
    #endif

    #if HDRP_USE_MASK_MAP
    float4 mask = VFXGetTextureColorWithProceduralUV(VFX_SAMPLER(maskMap),i,uvs);
    surfaceData.metallic *= mask.r;
    surfaceData.ambientOcclusion *= mask.g;
    surfaceData.perceptualSmoothness *= mask.a;
    #endif

    return surfaceData;
}
