
#ifdef DEBUG_DISPLAY
#include "HDRP/Debug/DebugDisplay.hlsl"
#endif
#ifndef SHADERPASS
#error THIS is an errror
#endif

#if (SHADERPASS != SHADERPASS_FORWARD)
#include "HDRP/Material/Material.hlsl"
#else
#include "HDRP/Lighting/Lighting.hlsl"
#endif

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

BuiltinData VFXGetBuiltinData(VFX_VARYING_PS_INPUTS i,const SurfaceData surfaceData, const BSDFData bsdfData, const PreLightData preLightData, const VFXUVData uvData, float opacity = 1.0f)
{
    BuiltinData builtinData = (BuiltinData)0;
    builtinData.opacity = opacity;
    builtinData.bakeDiffuseLighting = VFXSampleLightProbes(surfaceData.normalWS);
	builtinData.renderingLayers = _EnableLightLayers ? asuint(unity_RenderingLayer.x) : DEFAULT_LIGHT_LAYERS;

    #if HDRP_USE_EMISSIVE
    builtinData.emissiveColor = float3(1,1,1);
    #if HDRP_USE_EMISSIVE_MAP
    builtinData.emissiveColor *= SampleTexture(VFX_SAMPLER(emissiveMap),uvData).rgb * i.materialProperties.w;
    #endif
    #if HDRP_USE_EMISSIVE_COLOR || HDRP_USE_ADDITIONAL_EMISSIVE_COLOR
    builtinData.emissiveColor *= i.emissiveColor;
    #endif
    #endif

    builtinData.bakeDiffuseLighting = GetBakedDiffuseLighting(surfaceData, builtinData, bsdfData, preLightData);

    return builtinData;
}

SurfaceData VFXGetSurfaceData(VFX_VARYING_PS_INPUTS i, float3 normalWS,const VFXUVData uvData, uint diffusionProfile, out float opacity)
{
    SurfaceData surfaceData = (SurfaceData)0;

    float4 color = float4(1,1,1,1);
    #if HDRP_USE_BASE_COLOR
    color *= VFXGetParticleColor(i);
    #elif HDRP_USE_ADDITIONAL_BASE_COLOR
	#if defined(VFX_VARYING_COLOR)
    color.xyz *= i.VFX_VARYING_COLOR;
	#endif
	#if defined(VFX_VARYING_ALPHA)
    color.a *= i.VFX_VARYING_ALPHA;
	#endif
    #endif
    #if HDRP_USE_BASE_COLOR_MAP
    color.a *= SampleTexture(VFX_SAMPLER(baseColorMap),uvData).a;
    #endif
	color.a *= VFXGetSoftParticleFade(i);
    VFXClipFragmentColor(color.a,i);
    surfaceData.baseColor = color.rgb;
	opacity = color.a;

    #if HDRP_MATERIAL_TYPE_STANDARD
    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;
	#ifdef VFX_VARYING_METALLIC
    surfaceData.metallic = i.VFX_VARYING_METALLIC;
	#endif
    #elif HDRP_MATERIAL_TYPE_SPECULAR
    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR;
	#ifdef VFX_VARYING_SPECULAR
    surfaceData.specularColor = i.VFX_VARYING_SPECULAR;
	#endif
    #elif HDRP_MATERIAL_TYPE_TRANSLUCENT
    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_TRANSMISSION;
	#ifdef VFX_VARYING_THICKNESS
    surfaceData.thickness = i.VFX_VARYING_THICKNESS;
	#endif
    surfaceData.diffusionProfile = diffusionProfile;
    surfaceData.subsurfaceMask = 1.0f;
    #endif

    surfaceData.normalWS = normalWS;
	#ifdef VFX_VARYING_SMOOTHNESS
    surfaceData.perceptualSmoothness = i.VFX_VARYING_SMOOTHNESS;
	#endif
    surfaceData.specularOcclusion = 1.0f;
    surfaceData.ambientOcclusion = 1.0f;

    #if HDRP_USE_MASK_MAP
    float4 mask = SampleTexture(VFX_SAMPLER(maskMap),uvData);
    surfaceData.metallic *= mask.r;
    surfaceData.ambientOcclusion *= mask.g;
    surfaceData.perceptualSmoothness *= mask.a;
    #endif

    return surfaceData;
}
