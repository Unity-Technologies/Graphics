#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
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

    // Generate the primary UV coordinates and area
    const float2 uvBase = GetIntersectionTextureCoordinates(input, _UVMappingMask, _BaseColorMap_ST.xy, _BaseColorMap_ST.zw, _TexWorldScale);

    // Compute base LOD for all textures (all using the same UV parameterization)
    #ifdef USE_RAY_CONE_LOD
    // Convert the cone width to object space, since it is the space we computed primitive areas in
    const float3x3 worldToObject = (float3x3)WorldToObject3x4();
    const float3 scale3 = float3(length(worldToObject[0]), length(worldToObject[1]), length(worldToObject[2]));
    const float coneWidthOS = rayCone.width * (scale3.x + scale3.y + scale3.z) / 3;
    const float uvArea = GetIntersectionTextureArea(intersectionVertex, _UVMappingMask, _BaseColorMap_ST.xy, _TexWorldScale);
    const float baseLOD = computeBaseTextureLOD(V, input.tangentToWorld[2], coneWidthOS, uvArea, intersectionVertex.triangleArea);
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
    lod = computeTargetTextureLOD(_NormalMap, baseLOD);
    #endif
    float3 normalTS = UnpackNormalmapRGorAG(SAMPLE_TEXTURE2D_LOD(_NormalMap, sampler_NormalMap, uvBase, lod), _NormalScale);
    GetNormalWS(input, normalTS, surfaceData.normalWS, doubleSidedConstants);
    #else
    surfaceData.normalWS = input.tangentToWorld[2];
    #endif

    // Default smoothness
    #ifdef _MASKMAP
    #ifdef USE_RAY_CONE_LOD
    lod = computeTargetTextureLOD(_MaskMap, baseLOD);
    #endif
    surfaceData.perceptualSmoothness = SAMPLE_TEXTURE2D_LOD(_MaskMap, sampler_MaskMap, uvBase, lod).a;
    surfaceData.perceptualSmoothness = lerp(_SmoothnessRemapMin, _SmoothnessRemapMax, surfaceData.perceptualSmoothness);
    #else
    surfaceData.perceptualSmoothness = _Smoothness;
    #endif

    // Default Ambient occlusion
    #ifdef _MASKMAP
    surfaceData.ambientOcclusion = SAMPLE_TEXTURE2D_LOD(_MaskMap, sampler_MaskMap, uvBase, lod).g;
    surfaceData.ambientOcclusion = lerp(_AORemapMin, _AORemapMax, surfaceData.ambientOcclusion);
    #else
    surfaceData.ambientOcclusion = 1.0f;
    #endif

    // Default Metallic
    #ifdef _MASKMAP
    surfaceData.metallic = SAMPLE_TEXTURE2D_LOD(_MaskMap, sampler_MaskMap, uvBase, lod).r * _Metallic;
    #else
    surfaceData.metallic = _Metallic;
    #endif

#ifdef _MATERIAL_FEATURE_CLEAR_COAT
    surfaceData.coatMask = _CoatMask;
    // To shader feature for keyword to limit the variant
    #ifdef USE_RAY_CONE_LOD
    lod = computeTargetTextureLOD(_CoatMaskMap, baseLOD);
    #endif
    surfaceData.coatMask *= SAMPLE_TEXTURE2D_LOD(_CoatMaskMap, sampler_CoatMaskMap, uvBase, lod).r;
#else
    surfaceData.coatMask = 0.0;
#endif

#ifdef _MATERIAL_FEATURE_IRIDESCENCE
    #ifdef _IRIDESCENCE_THICKNESSMAP
    #ifdef USE_RAY_CONE_LOD
    lod = computeTargetTextureLOD(_IridescenceThicknessMap, baseLOD);
    #endif
    surfaceData.iridescenceThickness = SAMPLE_TEXTURE2D_LOD(_IridescenceThicknessMap, sampler_IridescenceThicknessMap, uvBase, lod).r;
    surfaceData.iridescenceThickness = saturate(_IridescenceThicknessRemap.x + _IridescenceThicknessRemap.y * surfaceData.iridescenceThickness);
    #else
    surfaceData.iridescenceThickness = _IridescenceThickness;
    #endif
    #ifdef USE_RAY_CONE_LOD
    lod = computeTargetTextureLOD(_IridescenceMaskMap, baseLOD);
    #endif
    surfaceData.iridescenceMask = _IridescenceMask;
    surfaceData.iridescenceMask *= SAMPLE_TEXTURE2D_LOD(_IridescenceMaskMap, sampler_IridescenceMaskMap, uvBase, lod).r;
#else
    surfaceData.iridescenceThickness = 0.0;
    surfaceData.iridescenceMask = 0.0;
#endif

#ifdef _ANISOTROPYMAP
    #ifdef USE_RAY_CONE_LOD
    lod = computeTargetTextureLOD(_AnisotropyMap, baseLOD);
    #endif
    surfaceData.anisotropy = SAMPLE_TEXTURE2D_LOD(_AnisotropyMap, sampler_AnisotropyMap, uvBase, lod).r;
#else
    surfaceData.anisotropy = 1.0;
#endif
    surfaceData.anisotropy *= _Anisotropy;

    // Default specular color
    surfaceData.diffusionProfileHash = asuint(_DiffusionProfileHash);

    // Default subsurface mask
    surfaceData.subsurfaceMask = 0.0;

#ifdef _THICKNESSMAP_IDX
    #ifdef USE_RAY_CONE_LOD
    lod = computeTargetTextureLOD(_ThicknessMap, baseLOD);
    #endif
    surfaceData.thickness = SAMPLE_TEXTURE2D_LOD(_ThicknessMap, SAMPLER_THICKNESSMAP_IDX, uvBase, lod).r;
    surfaceData.thickness = _ThicknessRemap.x + _ThicknessRemap.y * surfaceData.thickness;
#else
    surfaceData.thickness = _Thickness;
#endif

    // Default tangentWS
    surfaceData.tangentWS = normalize(input.tangentToWorld[0].xyz);

    // Transparency
#if HAS_REFRACTION
    surfaceData.ior = _Ior;
    surfaceData.transmittanceColor = _TransmittanceColor;
#ifdef _TRANSMITTANCECOLORMAP
    #ifdef USE_RAY_CONE_LOD
    lod = computeTargetTextureLOD(_TransmittanceColorMap, baseLOD);
    #endif
    surfaceData.transmittanceColor *= SAMPLE_TEXTURE2D_LOD(_TransmittanceColorMap, sampler_TransmittanceColorMap, uvBase, lod).rgb;
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

    InitBuiltinData(posInput, alpha, surfaceData.normalWS, -input.tangentToWorld[2], input.texCoord1, input.texCoord2, builtinData);
    builtinData.emissiveColor = _EmissiveColor * lerp(float3(1.0, 1.0, 1.0), surfaceData.baseColor.rgb, _AlbedoAffectEmissive);
#if _EMISSIVE_COLOR_MAP
    #ifdef USE_RAY_CONE_LOD
    lod = computeTargetTextureLOD(_EmissiveColorMap, baseLOD);
    #endif
    builtinData.emissiveColor *= SAMPLE_TEXTURE2D_LOD(_EmissiveColorMap, sampler_EmissiveColorMap, uvBase , 0.0).rgb;
#endif
    PostInitBuiltinData(V, posInput, surfaceData, builtinData);

    return true;
}
