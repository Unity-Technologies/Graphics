#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
<<<<<<< HEAD
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingBuiltinData.hlsl"

bool GetSurfaceDataFromIntersection(FragInputs input, float3 V, PositionInputs posInput, IntersectionVertice intersectionVertice, RayCone rayCone, out SurfaceData surfaceData, out BuiltinData builtinData)
=======
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl"

float2 GetIntersectionTextureCoordinates(FragInputs input, float4 uvMask, float2 tiling, float2 offset, float worldScale)
{
#if defined(_MAPPING_PLANAR) || defined(_MAPPING_TRIPLANAR)
    // Triplanar mapping will default to planar
    float2 uv = GetAbsolutePositionWS(input.positionRWS).xz * worldScale;
#else
    // Traditional UV mapping
    float2 uv = uvMask.x * input.texCoord0.xy +
                uvMask.y * input.texCoord1.xy +
                uvMask.z * input.texCoord2.xy +
                uvMask.w * input.texCoord3.xy;
#endif

    // Apply tiling and offset
    uv = uv * tiling + offset;

    return uv;
}

float GetIntersectionTextureArea(IntersectionVertex input, float4 uvMask, float2 tiling, float worldScale)
{
#if defined(_MAPPING_PLANAR) || defined(_MAPPING_TRIPLANAR)
    // Triplanar mapping will default to planar
    float area = input.triangleArea * worldScale * worldScale;
#else
    // Traditional UV mapping
    float area = uvMask.x * input.texCoord0Area +
                 uvMask.y * input.texCoord1Area +
                 uvMask.z * input.texCoord2Area +
                 uvMask.w * input.texCoord3Area;
#endif

    // Apply tiling factor to the tex coord area
    area *= tiling.x * tiling.y;

    return area;
}

bool GetSurfaceDataFromIntersection(FragInputs input, float3 V, PositionInputs posInput, IntersectionVertex intersectionVertex, RayCone rayCone, out SurfaceData surfaceData, out BuiltinData builtinData)
>>>>>>> master
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

<<<<<<< HEAD
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
=======
    // Generate the primary UV coordinates and area
    const float2 uvBase = GetIntersectionTextureCoordinates(input, _UVMappingMask, _BaseColorMap_ST.xy, _BaseColorMap_ST.zw, _TexWorldScale);

    // Compute base LOD for all textures (all using the same UV parameterization)
    #ifdef USE_RAY_CONE_LOD
    // Convert the cone width to object space, since it is the space we computed primitive areas in
    const float3x3 worldToObject = (float3x3)WorldToObject3x4();
    const float3 scale3 = float3(length(worldToObject[0]), length(worldToObject[1]), length(worldToObject[2]));
    const float coneWidthOS = rayCone.width * (scale3.x + scale3.y + scale3.z) / 3;
    const float uvArea = GetIntersectionTextureArea(intersectionVertex, _UVMappingMask, _BaseColorMap_ST.xy, _TexWorldScale);
    const float baseLOD = computeBaseTextureLOD(V, input.worldToTangent[2], coneWidthOS, uvArea, intersectionVertex.triangleArea);
    #else
    const float baseLOD = 0;
    #endif

    float lod = 0.0;

    // The base color of the object mixed with the base color texture
    #ifdef USE_RAY_CONE_LOD
    lod = computeTargetTextureLOD(_BaseColorMap, baseLOD);
    #endif
    surfaceData.baseColor = SAMPLE_TEXTURE2D_LOD(_BaseColorMap, sampler_BaseColorMap, uvBase, lod).rgb * _BaseColor.rgb;

    // Transparency Data
    float alpha = SAMPLE_TEXTURE2D_LOD(_BaseColorMap, sampler_BaseColorMap, uvBase, lod).a * _BaseColor.a;
>>>>>>> master

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
    #ifdef USE_RAY_CONE_LOD
<<<<<<< HEAD
    float normalLOD = computeTextureLOD(_NormalMap, _UVMappingMask, V, input.worldToTangent[2], rayCone, intersectionVertice);
    #else
    float normalLOD = 0.0f;
    #endif
    float3 normalTS = UnpackNormalmapRGorAG(SAMPLE_TEXTURE2D_LOD(_NormalMap, sampler_NormalMap, uvBase, normalLOD), _NormalScale);
=======
    lod = computeTargetTextureLOD(_NormalMap, baseLOD);
    #endif
    float3 normalTS = UnpackNormalmapRGorAG(SAMPLE_TEXTURE2D_LOD(_NormalMap, sampler_NormalMap, uvBase, lod), _NormalScale);
>>>>>>> master
    GetNormalWS(input, normalTS, surfaceData.normalWS, doubleSidedConstants);
    #else
    surfaceData.normalWS = input.worldToTangent[2];
    #endif

    // Default smoothness
    #ifdef _MASKMAP
    #ifdef USE_RAY_CONE_LOD
<<<<<<< HEAD
    float maskLOD = computeTextureLOD(_MaskMap, _UVMappingMask, V, input.worldToTangent[2], rayCone, intersectionVertice);
    #else
    float maskLOD = 0.0f;
    #endif
    surfaceData.perceptualSmoothness = SAMPLE_TEXTURE2D_LOD(_MaskMap, sampler_MaskMap, uvBase, maskLOD).a;
=======
    lod = computeTargetTextureLOD(_MaskMap, baseLOD);
    #endif
    surfaceData.perceptualSmoothness = SAMPLE_TEXTURE2D_LOD(_MaskMap, sampler_MaskMap, uvBase, lod).a;
>>>>>>> master
    surfaceData.perceptualSmoothness = lerp(_SmoothnessRemapMin, _SmoothnessRemapMax, surfaceData.perceptualSmoothness);
    #else
    surfaceData.perceptualSmoothness = _Smoothness;
    #endif

    // Default Ambient occlusion
    #ifdef _MASKMAP
<<<<<<< HEAD
    surfaceData.ambientOcclusion = SAMPLE_TEXTURE2D_LOD(_MaskMap, sampler_MaskMap, uvBase, maskLOD).g;
=======
    surfaceData.ambientOcclusion = SAMPLE_TEXTURE2D_LOD(_MaskMap, sampler_MaskMap, uvBase, lod).g;
>>>>>>> master
    surfaceData.ambientOcclusion = lerp(_AORemapMin, _AORemapMax, surfaceData.ambientOcclusion);
    #else
    surfaceData.ambientOcclusion = 1.0f;
    #endif

    // Default Metallic
    #ifdef _MASKMAP
<<<<<<< HEAD
    surfaceData.metallic = SAMPLE_TEXTURE2D_LOD(_MaskMap, sampler_MaskMap, uvBase, maskLOD).r * _Metallic;
=======
    surfaceData.metallic = SAMPLE_TEXTURE2D_LOD(_MaskMap, sampler_MaskMap, uvBase, lod).r * _Metallic;
>>>>>>> master
    #else
    surfaceData.metallic = _Metallic;
    #endif

#ifdef _MATERIAL_FEATURE_CLEAR_COAT
    surfaceData.coatMask = _CoatMask;
    // To shader feature for keyword to limit the variant
<<<<<<< HEAD
    surfaceData.coatMask *= SAMPLE_TEXTURE2D_LOD(_CoatMaskMap, sampler_CoatMaskMap, uvBase, 0.0f).r;
=======
    #ifdef USE_RAY_CONE_LOD
    lod = computeTargetTextureLOD(_CoatMaskMap, baseLOD);
    #endif
    surfaceData.coatMask *= SAMPLE_TEXTURE2D_LOD(_CoatMaskMap, sampler_CoatMaskMap, uvBase, lod).r;
>>>>>>> master
#else
    surfaceData.coatMask = 0.0;
#endif

#ifdef _MATERIAL_FEATURE_IRIDESCENCE
    #ifdef _IRIDESCENCE_THICKNESSMAP
<<<<<<< HEAD
    surfaceData.iridescenceThickness = SAMPLE_TEXTURE2D_LOD(_IridescenceThicknessMap, sampler_IridescenceThicknessMap, uvBase, 0.0f).r;
=======
    #ifdef USE_RAY_CONE_LOD
    lod = computeTargetTextureLOD(_IridescenceThicknessMap, baseLOD);
    #endif
    surfaceData.iridescenceThickness = SAMPLE_TEXTURE2D_LOD(_IridescenceThicknessMap, sampler_IridescenceThicknessMap, uvBase, lod).r;
>>>>>>> master
    surfaceData.iridescenceThickness = saturate(_IridescenceThicknessRemap.x + _IridescenceThicknessRemap.y * surfaceData.iridescenceThickness);
    #else
    surfaceData.iridescenceThickness = _IridescenceThickness;
    #endif
<<<<<<< HEAD
    surfaceData.iridescenceMask = _IridescenceMask;
    surfaceData.iridescenceMask *= SAMPLE_TEXTURE2D_LOD(_IridescenceMaskMap, sampler_IridescenceMaskMap, uvBase, 0.0f).r;
=======
    #ifdef USE_RAY_CONE_LOD
    lod = computeTargetTextureLOD(_IridescenceMaskMap, baseLOD);
    #endif
    surfaceData.iridescenceMask = _IridescenceMask;
    surfaceData.iridescenceMask *= SAMPLE_TEXTURE2D_LOD(_IridescenceMaskMap, sampler_IridescenceMaskMap, uvBase, lod).r;
>>>>>>> master
#else
    surfaceData.iridescenceThickness = 0.0;
    surfaceData.iridescenceMask = 0.0;
#endif

#ifdef _ANISOTROPYMAP
<<<<<<< HEAD
    surfaceData.anisotropy = SAMPLE_TEXTURE2D_LOD(_AnisotropyMap, sampler_AnisotropyMap, uvBase, 0.0f).r;
=======
    #ifdef USE_RAY_CONE_LOD
    lod = computeTargetTextureLOD(_AnisotropyMap, baseLOD);
    #endif
    surfaceData.anisotropy = SAMPLE_TEXTURE2D_LOD(_AnisotropyMap, sampler_AnisotropyMap, uvBase, lod).r;
>>>>>>> master
#else
    surfaceData.anisotropy = 1.0;
#endif
    surfaceData.anisotropy *= _Anisotropy;

    // Default specular color
    surfaceData.diffusionProfileHash = asuint(_DiffusionProfileHash);

    // Default subsurface mask
    surfaceData.subsurfaceMask = 0.0;

#ifdef _THICKNESSMAP_IDX
<<<<<<< HEAD
    surfaceData.thickness = SAMPLE_TEXTURE2D_LOD(_ThicknessMap, SAMPLER_THICKNESSMAP_IDX, uvBase, 0.0f).r;
=======
    #ifdef USE_RAY_CONE_LOD
    lod = computeTargetTextureLOD(_ThicknessMap, baseLOD);
    #endif
    surfaceData.thickness = SAMPLE_TEXTURE2D_LOD(_ThicknessMap, SAMPLER_THICKNESSMAP_IDX, uvBase, lod).r;
>>>>>>> master
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
<<<<<<< HEAD
    surfaceData.transmittanceColor *= SAMPLE_TEXTURE2D_LOD(_TransmittanceColorMap, sampler_TransmittanceColorMap, uvBase, 0.0f).rgb;
=======
    #ifdef USE_RAY_CONE_LOD
    lod = computeTargetTextureLOD(_TransmittanceColorMap, baseLOD);
    #endif
    surfaceData.transmittanceColor *= SAMPLE_TEXTURE2D_LOD(_TransmittanceColorMap, sampler_TransmittanceColorMap, uvBase, lod).rgb;
>>>>>>> master
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

    InitBuiltinData(posInput, alpha, surfaceData.normalWS, -input.worldToTangent[2], input.texCoord1, input.texCoord2, builtinData);
    builtinData.emissiveColor = _EmissiveColor * lerp(float3(1.0, 1.0, 1.0), surfaceData.baseColor.rgb, _AlbedoAffectEmissive);
<<<<<<< HEAD
    builtinData.emissiveColor *= SAMPLE_TEXTURE2D_LOD(_EmissiveColorMap, sampler_EmissiveColorMap, uvBase , 0.0).rgb;

=======
#if _EMISSIVE_COLOR_MAP
    #ifdef USE_RAY_CONE_LOD
    lod = computeTargetTextureLOD(_EmissiveColorMap, baseLOD);
    #endif
    builtinData.emissiveColor *= SAMPLE_TEXTURE2D_LOD(_EmissiveColorMap, sampler_EmissiveColorMap, uvBase , 0.0).rgb;
#endif
>>>>>>> master
    PostInitBuiltinData(V, posInput, surfaceData, builtinData);

    return true;
}
