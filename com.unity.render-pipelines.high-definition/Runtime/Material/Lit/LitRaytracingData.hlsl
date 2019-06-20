#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingBuiltinData.hlsl"

bool GetSurfaceDataFromIntersection(FragInputs input, float3 V, PositionInputs posInput, IntersectionVertice intersectionVertice, RayCone rayCone, out SurfaceData surfaceData, out BuiltinData builtinData)
{
#ifdef _DOUBLESIDED_ON
    float3 doubleSidedConstants = _DoubleSidedConstants.xyz;
#else
    float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
#endif

    ApplyDoubleSidedFlipOrMirror(input, doubleSidedConstants); // Apply double sided flip on the vertex normal

    // Initial value of the material features
    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;

#ifdef _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING;
#endif
#ifdef _MATERIAL_FEATURE_TRANSMISSION
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_LIT_TRANSMISSION;
#endif
#ifdef _MATERIAL_FEATURE_ANISOTROPY
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_LIT_ANISOTROPY;
#endif
#ifdef _MATERIAL_FEATURE_CLEAR_COAT
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_LIT_CLEAR_COAT;
#endif
#ifdef _MATERIAL_FEATURE_IRIDESCENCE
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_LIT_IRIDESCENCE;
#endif
#ifdef _MATERIAL_FEATURE_SPECULAR_COLOR
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR;
#endif
    
    // Generate the primary uv coordinates
    float2 uvBase = _UVMappingMask.x * input.texCoord0.xy +
                    _UVMappingMask.y * input.texCoord1.xy +
                    _UVMappingMask.z * input.texCoord2.xy +
                    _UVMappingMask.w * input.texCoord3.xy;

    // Apply tiling and offset
    uvBase = uvBase * _BaseColorMap_ST.xy + _BaseColorMap_ST.zw;

    // The base color of the object mixed with the base color texture
    #ifdef USE_RAY_CONE_LOD
    float lod = computeTextureLOD(_BaseColorMap, _UVMappingMask, V, input.worldToTangent[2], rayCone, intersectionVertice);
    surfaceData.baseColor = SAMPLE_TEXTURE2D_LOD(_BaseColorMap, sampler_BaseColorMap, uvBase, lod).rgb * _BaseColor.rgb;
    #else
    surfaceData.baseColor = SAMPLE_TEXTURE2D_LOD(_BaseColorMap, sampler_BaseColorMap, uvBase, 0).rgb * _BaseColor.rgb;
    #endif

    // Transparency Data
    float alpha = SAMPLE_TEXTURE2D_LOD(_BaseColorMap, sampler_BaseColorMap, uvBase, 0).a * _BaseColor.a;

#ifdef _ALPHATEST_ON
    if(alpha < _AlphaCutoff)
        return false;
#endif

    // Specular Color
    surfaceData.specularColor = _SpecularColor.rgb;
#ifdef _MATERIAL_FEATURE_SPECULAR_COLOR
    // Require to have setup baseColor
    // Reproduce the energy conservation done in legacy Unity. Not ideal but better for compatibility and users can unchek it
    surfaceData.baseColor *= _EnergyConservingSpecularColor > 0.0 ? (1.0 - Max3(surfaceData.specularColor.r, surfaceData.specularColor.g, surfaceData.specularColor.b)) : 1.0;
#endif

    // Default specular occlusion
    surfaceData.specularOcclusion = 1.0;

    #ifdef _NORMALMAP
    float3 normalTS = SAMPLE_TEXTURE2D_LOD(_NormalMap, sampler_NormalMap, uvBase, _NormalScale);
    GetNormalWS(input, normalTS, surfaceData.normalWS, doubleSidedConstants);
    #else
    surfaceData.normalWS = input.worldToTangent[2];
    #endif

    // Default smoothness
    #ifdef _MASKMAP
    surfaceData.perceptualSmoothness = SAMPLE_TEXTURE2D_LOD(_MaskMap, sampler_MaskMap, uvBase, 0).a;
    surfaceData.perceptualSmoothness = lerp(_SmoothnessRemapMin, _SmoothnessRemapMax, surfaceData.perceptualSmoothness);
    #else
    surfaceData.perceptualSmoothness = _Smoothness;
    #endif

    // Default Ambient occlusion
    #ifdef _MASKMAP
    surfaceData.ambientOcclusion = SAMPLE_TEXTURE2D_LOD(_MaskMap, sampler_MaskMap, uvBase, 0).g;
    surfaceData.ambientOcclusion = lerp(_AORemapMin, _AORemapMax, surfaceData.ambientOcclusion);
    #else
    surfaceData.ambientOcclusion = 1.0f;
    #endif

    // Default Metallic
    #ifdef _MASKMAP
    surfaceData.metallic = SAMPLE_TEXTURE2D_LOD(_MaskMap, sampler_MaskMap, uvBase, 0).r * _Metallic;
    #else
    surfaceData.metallic = _Metallic;
    #endif

#ifdef _MATERIAL_FEATURE_CLEAR_COAT
    surfaceData.coatMask = _CoatMask;
    // To shader feature for keyword to limit the variant
    surfaceData.coatMask *= SAMPLE_TEXTURE2D_LOD(_CoatMaskMap, sampler_CoatMaskMap, uvBase, 0.0f).r;
#else
    surfaceData.coatMask = 0.0;
#endif

#ifdef _MATERIAL_FEATURE_IRIDESCENCE
    #ifdef _IRIDESCENCE_THICKNESSMAP
    surfaceData.iridescenceThickness = SAMPLE_TEXTURE2D_LOD(_IridescenceThicknessMap, sampler_IridescenceThicknessMap, uvBase, 0.0f).r;
    surfaceData.iridescenceThickness = saturate(_IridescenceThicknessRemap.x + _IridescenceThicknessRemap.y * surfaceData.iridescenceThickness);
    #else
    surfaceData.iridescenceThickness = _IridescenceThickness;
    #endif
    surfaceData.iridescenceMask = _IridescenceMask;
    surfaceData.iridescenceMask *= SAMPLE_TEXTURE2D_LOD(_IridescenceMaskMap, sampler_IridescenceMaskMap, uvBase, 0.0f).r;
#else
    surfaceData.iridescenceThickness = 0.0;
    surfaceData.iridescenceMask = 0.0;
#endif

#ifdef _ANISOTROPYMAP
    surfaceData.anisotropy = SAMPLE_TEXTURE2D_LOD(_AnisotropyMap, sampler_AnisotropyMap, uvBase, 0.0f).r;
#else
    surfaceData.anisotropy = 1.0;
#endif
    surfaceData.anisotropy *= _Anisotropy;

    // Default specular color
    surfaceData.diffusionProfile = _DiffusionProfile;

    // Default subsurface mask
    surfaceData.subsurfaceMask = 0.0;

#ifdef _THICKNESSMAP_IDX
    surfaceData.thickness = SAMPLE_TEXTURE2D_LOD(_ThicknessMap, SAMPLER_THICKNESSMAP_IDX, uvBase, 0.0f).r;
    surfaceData.thickness = _ThicknessRemap.x + _ThicknessRemap.y * surfaceData.thickness;
#else
    surfaceData.thickness = _Thickness;
#endif

    // Default tangentWS
    surfaceData.tangentWS = normalize(input.worldToTangent[0].xyz);

    // Transparency
#if HAS_REFRACTION
    surfaceData.ior = _Ior;
    surfaceData.transmittanceColor = _TransmittanceColor;
#ifdef _TRANSMITTANCECOLORMAP
    surfaceData.transmittanceColor *= SAMPLE_TEXTURE2D_LOD(_TransmittanceColorMap, sampler_TransmittanceColorMap, uvBase, 0.0f).rgb;
#endif

    surfaceData.atDistance = _ATDistance;
    // Thickness already defined with SSS (from both thickness and thicknessMap)
    surfaceData.thickness *= _ThicknessMultiplier;
    // Rough refraction don't use opacity. Instead we use opacity as a transmittance mask.
    surfaceData.transmittanceMask = (1.0 - alpha);
    alpha = 1.0;
#else
    surfaceData.ior = 1.0;
    surfaceData.transmittanceColor = float3(1.0, 1.0, 1.0);
    surfaceData.atDistance = 1.0;
    surfaceData.transmittanceMask = 0.0;
#endif

    InitBuiltinData(alpha, surfaceData.normalWS, -input.worldToTangent[2], input.positionRWS, input.texCoord1, input.texCoord2, builtinData);
    builtinData.emissiveColor = _EmissiveColor * lerp(float3(1.0, 1.0, 1.0), surfaceData.baseColor.rgb, _AlbedoAffectEmissive);
    PostInitBuiltinData(V, posInput, surfaceData, builtinData);

    return true;
}
