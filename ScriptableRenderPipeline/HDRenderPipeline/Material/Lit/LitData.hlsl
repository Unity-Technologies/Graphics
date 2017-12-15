//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "ShaderLibrary/Sampling/SampleUVMapping.hlsl"
#include "../MaterialUtilities.hlsl"
#include "../Decal/DecalUtilities.hlsl"

// TODO: move this function to commonLighting.hlsl once validated it work correctly
float GetSpecularOcclusionFromBentAO(float3 V, float3 bentNormalWS, SurfaceData surfaceData)
{
    // Retrieve cone angle
    // Ambient occlusion is cosine weighted, thus use following equation. See slide 129
    float cosAv = sqrt(1.0 - surfaceData.ambientOcclusion);
    float roughness = max(PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness), 0.01); // Clamp to 0.01 to avoid edge cases
    float cosAs = exp2((-log(10.0)/log(2.0)) * Sq(roughness));
    float cosB = dot(bentNormalWS, reflect(-V, surfaceData.normalWS));

    return SphericalCapIntersectionSolidArea(cosAv, cosAs, cosB) / (TWO_PI * (1.0 - cosAs));
}

void GetBuiltinData(FragInputs input, SurfaceData surfaceData, float alpha, float3 bentNormalWS, float depthOffset, out BuiltinData builtinData)
{
    // Builtin Data
    builtinData.opacity = alpha;

    // TODO: Sample lightmap/lightprobe/volume proxy
    // This should also handle projective lightmap
    builtinData.bakeDiffuseLighting = SampleBakedGI(input.positionWS, bentNormalWS, input.texCoord1, input.texCoord2);

    // It is safe to call this function here as surfaceData have been filled
    // We want to know if we must enable transmission on GI for SSS material, if the material have no SSS, this code will be remove by the compiler.
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(surfaceData);
    if (bsdfData.enableTransmission)
    {
        // For now simply recall the function with inverted normal, the compiler should be able to optimize the lightmap case to not resample the directional lightmap
        // however it will not optimize the lightprobe case due to the proxy volume relying on dynamic if (we rely must get right of this dynamic if), not a problem for SH9, but a problem for proxy volume.
        // TODO: optimize more this code.
        // Add GI transmission contribution by resampling the GI for inverted vertex normal
        builtinData.bakeDiffuseLighting += SampleBakedGI(input.positionWS, -input.worldToTangent[2], input.texCoord1, input.texCoord2) * bsdfData.transmittance;
    }

#ifdef SHADOWS_SHADOWMASK
    float4 shadowMask = SampleShadowMask(input.positionWS, input.texCoord1);
    builtinData.shadowMask0 = shadowMask.x;
    builtinData.shadowMask1 = shadowMask.y;
    builtinData.shadowMask2 = shadowMask.z;
    builtinData.shadowMask3 = shadowMask.w;
#else
    builtinData.shadowMask0 = 0.0;
    builtinData.shadowMask1 = 0.0;
    builtinData.shadowMask2 = 0.0;
    builtinData.shadowMask3 = 0.0;
#endif

    // Emissive Intensity is only use here, but is part of BuiltinData to enforce UI parameters as we want the users to fill one color and one intensity
    builtinData.emissiveIntensity = _EmissiveIntensity; // We still store intensity here so we can reuse it with debug code

    builtinData.emissiveColor = _EmissiveColor * builtinData.emissiveIntensity * lerp(float3(1.0, 1.0, 1.0), surfaceData.baseColor.rgb, _AlbedoAffectEmissive);
#ifdef _EMISSIVE_COLOR_MAP
    builtinData.emissiveColor *= SAMPLE_TEXTURE2D(_EmissiveColorMap, sampler_EmissiveColorMap, TRANSFORM_TEX(input.texCoord0, _EmissiveColorMap)).rgb;
#endif

    builtinData.velocity = float2(0.0, 0.0);

#if (SHADERPASS == SHADERPASS_DISTORTION) || defined(DEBUG_DISPLAY)
    float3 distortion = SAMPLE_TEXTURE2D(_DistortionVectorMap, sampler_DistortionVectorMap, input.texCoord0).rgb;
    distortion.rg = distortion.rg * _DistortionVectorScale.xx + _DistortionVectorBias.xx;
    builtinData.distortion = distortion.rg * _DistortionScale;
    builtinData.distortionBlur = clamp(distortion.b * _DistortionBlurScale, 0.0, 1.0) * (_DistortionBlurRemapMax - _DistortionBlurRemapMin) + _DistortionBlurRemapMin;
#else
    builtinData.distortion = float2(0.0, 0.0);
    builtinData.distortionBlur = 0.0;
#endif

    builtinData.depthOffset = depthOffset;
}

// Struct that gather UVMapping info of all layers + common calculation
// This is use to abstract the mapping that can differ on layers
struct LayerTexCoord
{
#ifndef LAYERED_LIT_SHADER
    UVMapping base;
    UVMapping details;
#else
    // Regular texcoord
    UVMapping base0;
    UVMapping base1;
    UVMapping base2;
    UVMapping base3;

    UVMapping details0;
    UVMapping details1;
    UVMapping details2;
    UVMapping details3;

    // Dedicated for blend mask
    UVMapping blendMask;
#endif

    // Store information that will be share by all UVMapping
    float3 vertexNormalWS; // TODO: store also object normal map for object triplanar
    float3 triplanarWeights;

#ifdef SURFACE_GRADIENT
    // tangent basis for each UVSet - up to 4 for now
    float3 vertexTangentWS0, vertexBitangentWS0;
    float3 vertexTangentWS1, vertexBitangentWS1;
    float3 vertexTangentWS2, vertexBitangentWS2;
    float3 vertexTangentWS3, vertexBitangentWS3;
#endif
};

#ifdef SURFACE_GRADIENT
void GenerateLayerTexCoordBasisTB(FragInputs input, inout LayerTexCoord layerTexCoord)
{
    float3 vertexNormalWS = input.worldToTangent[2];

    layerTexCoord.vertexTangentWS0 = input.worldToTangent[0];
    layerTexCoord.vertexBitangentWS0 = input.worldToTangent[1];

    // TODO: We should use relative camera position here - This will be automatic when we will move to camera relative space.
    float3 dPdx = ddx_fine(input.positionWS);
    float3 dPdy = ddy_fine(input.positionWS);

    float3 sigmaX = dPdx - dot(dPdx, vertexNormalWS) * vertexNormalWS;
    float3 sigmaY = dPdy - dot(dPdy, vertexNormalWS) * vertexNormalWS;
    //float flipSign = dot(sigmaY, cross(vertexNormalWS, sigmaX) ) ? -1.0 : 1.0;
    float flipSign = dot(dPdy, cross(vertexNormalWS, dPdx)) < 0.0 ? -1.0 : 1.0; // gives same as the commented out line above

    // TODO: Optimize! The compiler will not be able to remove the tangent space that are not use because it can't know due to our UVMapping constant we use for both base and details
    // To solve this we should track which UVSet is use for normal mapping... Maybe not as simple as it sounds
    SurfaceGradientGenBasisTB(vertexNormalWS, sigmaX, sigmaY, flipSign, input.texCoord1, layerTexCoord.vertexTangentWS1, layerTexCoord.vertexBitangentWS1);
    #if defined(_REQUIRE_UV2) || defined(_REQUIRE_UV3)
    SurfaceGradientGenBasisTB(vertexNormalWS, sigmaX, sigmaY, flipSign, input.texCoord2, layerTexCoord.vertexTangentWS2, layerTexCoord.vertexBitangentWS2);
    #endif
    #if defined(_REQUIRE_UV3)
    SurfaceGradientGenBasisTB(vertexNormalWS, sigmaX, sigmaY, flipSign, input.texCoord3, layerTexCoord.vertexTangentWS3, layerTexCoord.vertexBitangentWS3);
    #endif
}
#endif

#ifndef LAYERED_LIT_SHADER

// Want to use only one sampler for normalmap/bentnormalmap either we use OS or TS. And either we have normal map or bent normal or both.
#ifdef _NORMALMAP_TANGENT_SPACE
    #if defined(_NORMALMAP)
    #define SAMPLER_NORMALMAP_IDX sampler_NormalMap
    #elif defined(_BENTNORMALMAP)
    #define SAMPLER_NORMALMAP_IDX sampler_BentNormalMap
    #endif
#else
    #if defined(_NORMALMAP)
    #define SAMPLER_NORMALMAP_IDX sampler_NormalMapOS
    #elif defined(_BENTNORMALMAP)
    #define SAMPLER_NORMALMAP_IDX sampler_BentNormalMapOS
    #endif
#endif

#define SAMPLER_DETAILMAP_IDX sampler_DetailMap
#define SAMPLER_MASKMAP_IDX sampler_MaskMap
#define SAMPLER_HEIGHTMAP_IDX sampler_HeightMap

#define SAMPLER_SUBSURFACE_RADIUSMAP_IDX sampler_SubsurfaceRadiusMap
#define SAMPLER_THICKNESSMAP_IDX sampler_ThicknessMap

// include LitDataIndividualLayer to define GetSurfaceData
#define LAYER_INDEX 0
#define ADD_IDX(Name) Name
#define ADD_ZERO_IDX(Name) Name
#ifdef _NORMALMAP
#define _NORMALMAP_IDX
#endif
#ifdef _NORMALMAP_TANGENT_SPACE
#define _NORMALMAP_TANGENT_SPACE_IDX
#endif
#ifdef _DETAIL_MAP
#define _DETAIL_MAP_IDX
#endif
#ifdef _SUBSURFACE_RADIUS_MAP
#define _SUBSURFACE_RADIUS_MAP_IDX
#endif
#ifdef _THICKNESSMAP
#define _THICKNESSMAP_IDX
#endif
#ifdef _MASKMAP
#define _MASKMAP_IDX
#endif
#ifdef _BENTNORMALMAP
#define _BENTNORMALMAP_IDX
#endif
#include "LitDataIndividualLayer.hlsl"

// This maybe call directly by tessellation (domain) shader, thus all part regarding surface gradient must be done
// in function with FragInputs input as parameters
// layerTexCoord must have been initialize to 0 outside of this function
void GetLayerTexCoord(float2 texCoord0, float2 texCoord1, float2 texCoord2, float2 texCoord3,
                      float3 positionWS, float3 vertexNormalWS, inout LayerTexCoord layerTexCoord)
{
    layerTexCoord.vertexNormalWS = vertexNormalWS;
    layerTexCoord.triplanarWeights = ComputeTriplanarWeights(vertexNormalWS);

    int mappingType = UV_MAPPING_UVSET;
#if defined(_MAPPING_PLANAR)
    mappingType = UV_MAPPING_PLANAR;
#elif defined(_MAPPING_TRIPLANAR)
    mappingType = UV_MAPPING_TRIPLANAR;
#endif

    // Be sure that the compiler is aware that we don't use UV1 to UV3 for main layer so it can optimize code
    ComputeLayerTexCoord(   texCoord0, texCoord1, texCoord2, texCoord3, float4(1.0, 0.0, 0.0, 0.0), _UVDetailsMappingMask,
                            _BaseColorMap_ST.xy, _BaseColorMap_ST.zw, _DetailMap_ST.xy, _DetailMap_ST.zw, 1.0, _LinkDetailsWithBase,
                            positionWS, _TexWorldScale,
                            mappingType, layerTexCoord);
}

// This is call only in this file
// layerTexCoord must have been initialize to 0 outside of this function
void GetLayerTexCoord(FragInputs input, inout LayerTexCoord layerTexCoord)
{
#ifdef SURFACE_GRADIENT
    GenerateLayerTexCoordBasisTB(input, layerTexCoord);
#endif

    GetLayerTexCoord(   input.texCoord0, input.texCoord1, input.texCoord2, input.texCoord3,
                        input.positionWS, input.worldToTangent[2].xyz, layerTexCoord);
}

#include "LitDataDisplacement.hlsl"

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
#ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
    LODDitheringTransition(posInput.positionSS, unity_LODFade.x);
#endif

    ApplyDoubleSidedFlipOrMirror(input); // Apply double sided flip on the vertex normal

    LayerTexCoord layerTexCoord;
    ZERO_INITIALIZE(LayerTexCoord, layerTexCoord);
    GetLayerTexCoord(input, layerTexCoord);

    float depthOffset = ApplyPerPixelDisplacement(input, V, layerTexCoord);

#ifdef _DEPTHOFFSET_ON
    ApplyDepthOffsetPositionInput(V, depthOffset, GetWorldToHClipMatrix(), posInput);
#endif

    // We perform the conversion to world of the normalTS outside of the GetSurfaceData
    // so it allow us to correctly deal with detail normal map and optimize the code for the layered shaders
    float3 normalTS;
    float3 bentNormalTS;
    float3 bentNormalWS;
    float alpha = GetSurfaceData(input, layerTexCoord, surfaceData, normalTS, bentNormalTS);
    GetNormalWS(input, V, normalTS, surfaceData.normalWS);

    // Ensure that the normal is front-facing.
    float NdotV;
    surfaceData.normalWS = GetViewReflectedNormal(surfaceData.normalWS, V, NdotV);

    // Use bent normal to sample GI if available
#ifdef _BENTNORMALMAP
    GetNormalWS(input, V, bentNormalTS, bentNormalWS);
#else
    bentNormalWS = surfaceData.normalWS;
#endif

    // By default we use the ambient occlusion with Tri-ace trick (apply outside) for specular occlusion.
    // If user provide bent normal then we process a better term
#if defined(_BENTNORMALMAP) && defined(_ENABLESPECULAROCCLUSION)
    // If we have bent normal and ambient occlusion, process a specular occlusion
    surfaceData.specularOcclusion = GetSpecularOcclusionFromBentAO(V, bentNormalWS, surfaceData);
#elif defined(_MASKMAP)
    surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(NdotV, surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
#else
    surfaceData.specularOcclusion = 1.0;
#endif

    // This is use with anisotropic material
    surfaceData.tangentWS = Orthonormalize(surfaceData.tangentWS, surfaceData.normalWS);

    AddDecalContribution(posInput.positionSS, surfaceData);

    // Caution: surfaceData must be fully initialize before calling GetBuiltinData
    GetBuiltinData(input, surfaceData, alpha, bentNormalWS, depthOffset, builtinData);
}

#include "LitDataMeshModification.hlsl"

#endif // #ifndef LAYERED_LIT_SHADER
