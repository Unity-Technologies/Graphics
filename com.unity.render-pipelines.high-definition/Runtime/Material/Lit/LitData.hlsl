//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/SampleUVMapping.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl"

//#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SphericalCapPivot/SPTDistribution.hlsl"
//#define SPECULAR_OCCLUSION_USE_SPTD

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

    float3 dPdx = ddx_fine(input.positionRWS);
    float3 dPdy = ddy_fine(input.positionRWS);

    float3 sigmaX = dPdx - dot(dPdx, vertexNormalWS) * vertexNormalWS;
    float3 sigmaY = dPdy - dot(dPdy, vertexNormalWS) * vertexNormalWS;
    //float flipSign = dot(sigmaY, cross(vertexNormalWS, sigmaX) ) ? -1.0 : 1.0;
    float flipSign = dot(dPdy, cross(vertexNormalWS, dPdx)) < 0.0 ? -1.0 : 1.0; // gives same as the commented out line above

    // TODO: Optimize! The compiler will not be able to remove the tangent space that are not use because it can't know due to our UVMapping constant we use for both base and details
    // To solve this we should track which UVSet is use for normal mapping... Maybe not as simple as it sounds
    SurfaceGradientGenBasisTB(vertexNormalWS, sigmaX, sigmaY, flipSign, input.texCoord1.xy, layerTexCoord.vertexTangentWS1, layerTexCoord.vertexBitangentWS1);
    #if defined(_REQUIRE_UV2) || defined(_REQUIRE_UV3)
    SurfaceGradientGenBasisTB(vertexNormalWS, sigmaX, sigmaY, flipSign, input.texCoord2.xy, layerTexCoord.vertexTangentWS2, layerTexCoord.vertexBitangentWS2);
    #endif
    #if defined(_REQUIRE_UV3)
    SurfaceGradientGenBasisTB(vertexNormalWS, sigmaX, sigmaY, flipSign, input.texCoord3.xy, layerTexCoord.vertexTangentWS3, layerTexCoord.vertexBitangentWS3);
    #endif
}
#endif

// Used by SamplePrefilteredNormalMap() below.
#define NORMAL_SPACE_TANGENT 0
#define NORMAL_SPACE_OBJECT  1
#define NORMAL_SPACE_WORLD   2

// Returns normalConeTS = {normalTS, variance}.
// Only isotropic cone support. :-(
float4 SamplePrefilteredNormalMap(TEXTURE2D(normalMap), SAMPLER(samplerState),
                                  UVMapping uvMapping, float2 normalMapSize, float normalScale,
                                  int inputNormalSpace)
{
    // With normal mapping, we can distinguish 2 types of input normals:
    // those that are relative to the mesh fragment's normal (group 1),
    // and those that are not (already "perturbed", group 2).
    // Group 1 includes tangent-space normal maps.
    // Group 2 contains object- and world-space normal maps.
    // We should not access the tangent frame unless we use the tangent-space normal maps.

    float3 surfGrad            = 0;
    float  lodVarianceScale    = 0;
    float  averageNormalLength = 0;

    if (uvMapping.mappingType == UV_MAPPING_TRIPLANAR)
    {
        // Triplanar mapping always uses tangent-space normals.
        float3 volumeGrad    = 0;
        float3 averageNormal = 0;

        if (uvMapping.triplanarWeights.x > 0) // ZY -> -X (left-handed)
        {
            float w = uvMapping.triplanarWeights.x;

            float3 normal  = SAMPLE_TEXTURE2D(normalMap, samplerState, uvMapping.uvZY).xyz * 2 - 1;
            averageNormal += w * normal;

            float normalLengthSq = dot(normal, normal);
            normal *= rsqrt(normalLengthSq);

            float2 bumpGrad = ConvertTangentSpaceNormalToHeightMapGradient(normal);

            averageNormalLength += w * sqrt(normalLengthSq);
            lodVarianceScale    += w * saturate(ComputeTextureLOD(uvMapping.uvZY, normalMapSize));

            volumeGrad.z += w * bumpGrad.x;
            volumeGrad.y += w * bumpGrad.y;
        }

        if (uvMapping.triplanarWeights.y > 0) // XZ -> -Y (left-handed)
        {
            float w = uvMapping.triplanarWeights.y;

            float3 normal  = SAMPLE_TEXTURE2D(normalMap, samplerState, uvMapping.uvXZ).xyz * 2 - 1;
            averageNormal += w * normal;

            float normalLengthSq = dot(normal, normal);
            normal *= rsqrt(normalLengthSq);

            float2 bumpGrad = ConvertTangentSpaceNormalToHeightMapGradient(normal);

            averageNormalLength += w * sqrt(normalLengthSq);
            lodVarianceScale    += w * saturate(ComputeTextureLOD(uvMapping.uvXZ, normalMapSize));

            volumeGrad.x += w * bumpGrad.x;
            volumeGrad.z += w * bumpGrad.y;
        }

        if (uvMapping.triplanarWeights.z > 0) // XY -> Z (left-handed)
        {
            float w = uvMapping.triplanarWeights.z;

            float3 normal  = SAMPLE_TEXTURE2D(normalMap, samplerState, uvMapping.uvXY).xyz * 2 - 1;
            averageNormal += w * normal;

            float normalLengthSq = dot(normal, normal);
            normal *= rsqrt(normalLengthSq);

            float2 bumpGrad = ConvertTangentSpaceNormalToHeightMapGradient(normal);

            averageNormalLength += w * sqrt(normalLengthSq);
            lodVarianceScale    += w * saturate(ComputeTextureLOD(uvMapping.uvXY, normalMapSize));

            volumeGrad.x += w * bumpGrad.x;
            volumeGrad.y += w * bumpGrad.y;
        }

        surfGrad = SurfaceGradientFromVolumeGradient(uvMapping.normalWS, volumeGrad);

        // If average tangent-space normals are aligned, this results in average variance.
        // However, if they point in different direction, they form a cone around
        // the resulting normal, shortening the average normal and increasing variance.
        averageNormalLength = length(averageNormal);
    }
    else
    {
        float3 normal = SAMPLE_TEXTURE2D(normalMap, samplerState, uvMapping.uv).xyz * 2 - 1;

        float averageNormalLengthSq = dot(normal, normal);
              averageNormalLength   = sqrt(averageNormalLengthSq);

        normal *= rsqrt(averageNormalLengthSq);

        lodVarianceScale = saturate(ComputeTextureLOD(uvMapping.uv, normalMapSize));

        if (inputNormalSpace == NORMAL_SPACE_TANGENT)
        {
            float2 bumpGrad = ConvertTangentSpaceNormalToHeightMapGradient(normal);

            if (uvMapping.mappingType == UV_MAPPING_UVSET)
            {
                // This is the only case when we have access to the tangent frame.
                surfGrad = SurfaceGradientFromTBN(bumpGrad, uvMapping.tangentWS, uvMapping.bitangentWS);
            }
            else // UV_MAPPING_PLANAR
            {
                // The normal map lies in the XZ plane (and not XY), since Unity assumes that Y is up.
                // It is also aligned with the X and Z axes.
                surfGrad = SurfaceGradientFromVolumeGradient(uvMapping.normalWS, float3(bumpGrad.x, 0, bumpGrad.y));
            }
        }
        else // (NORMAL_SPACE_OBJECT || NORMAL_SPACE_WORLD)
        {
            if (inputNormalSpace == NORMAL_SPACE_OBJECT)
            {
                normal = TransformObjectToWorldDir(normal);
            }

            surfGrad = SurfaceGradientFromPerturbedNormal(uvMapping.normalWS, normal);
        }
    }

    // We cannot correctly support normal scaling because its effect on spherical variance is not clear.
    // Normal scale affects slopes of the height field. However, our normal map only contains
    // statistics of directions, not slopes. So there simply isn't enough information to correctly
    // adjust spherical variance. We limit the error by clamping the normal scale to 1.
    normalScale = min(normalScale, 1);

    // Scale the slopes.
    surfGrad *= normalScale;

    // Reduce variance for certain configurations. This is all a big hack.
    // Fade in normal map filtering results (0% for MIP 0, 100% for MIP 1+).
    // TODO: scale standard deviation instead? More linear?
    float varianceScale = lodVarianceScale * normalScale;

    // (1 - averageNormalLength) * varianceScale.
    float normalMapVariance = varianceScale - varianceScale * averageNormalLength;

    return float4(surfGrad, normalMapVariance);
}

// Returns normalConeTS = {normalTS, variance}.
float4 ReorientNormalCone(float3 startNormalTS, float startNormalVariance,
                          float3 rotorNormalTS, float rotorNormalVariance, float rotationMask)
{
    float3 normalTS;
    float  variance;

#ifdef SURFACE_GRADIENT
    // We just add the slopes.
    normalTS = startNormalTS + rotorNormalTS * rotationMask;
#else
    // TODO: this should really be just a single spherical rotation.
    normalTS = lerp(startNormalTS, BlendNormalRNM(startNormalTS, rotorNormalTS), rotationMask);
#endif

    // TODO: scale standard deviation instead? More linear?
    rotorNormalVariance *= rotationMask;

    variance = CombineSphericalVariance(startNormalVariance, rotorNormalVariance);

    return float4(normalTS, variance);
}

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

#define SAMPLER_DETAILMAP_IDX       sampler_DetailMap
#define SAMPLER_DETAILNORMALMAP_IDX sampler_DetailNormalMap
#define SAMPLER_MASKMAP_IDX         sampler_MaskMap
#define SAMPLER_HEIGHTMAP_IDX       sampler_HeightMap

#define SAMPLER_SUBSURFACE_MASK_MAP_IDX sampler_SubsurfaceMaskMap
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
#ifdef _SUBSURFACE_MASK_MAP
#define _SUBSURFACE_MASK_MAP_IDX
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
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDataIndividualLayer.hlsl"

// This maybe call directly by tessellation (domain) shader, thus all part regarding surface gradient must be done
// in function with FragInputs input as parameters
// layerTexCoord must have been initialize to 0 outside of this function
void GetLayerTexCoord(float2 texCoord0, float2 texCoord1, float2 texCoord2, float2 texCoord3,
                      float3 positionRWS, float3 vertexNormalWS, inout LayerTexCoord layerTexCoord)
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
    ComputeLayerTexCoord(   texCoord0, texCoord1, texCoord2, texCoord3, _UVMappingMask, _UVDetailsMappingMask,
                            _BaseColorMap_ST.xy, _BaseColorMap_ST.zw, _DetailMap_ST.xy, _DetailMap_ST.zw, 1.0, _LinkDetailsWithBase,
                            positionRWS, _TexWorldScale,
                            mappingType, layerTexCoord);
}

// This is call only in this file
// layerTexCoord must have been initialize to 0 outside of this function
void GetLayerTexCoord(FragInputs input, inout LayerTexCoord layerTexCoord)
{
#ifdef SURFACE_GRADIENT
    GenerateLayerTexCoordBasisTB(input, layerTexCoord);
#endif

    GetLayerTexCoord(   input.texCoord0.xy, input.texCoord1.xy, input.texCoord2.xy, input.texCoord3.xy,
                        input.positionRWS, input.worldToTangent[2].xyz, layerTexCoord);
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDataDisplacement.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitBuiltinData.hlsl"

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
#ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
    uint3 fadeMaskSeed = asuint((int3)(V * _ScreenSize.xyx)); // Quantize V to _ScreenSize values
    LODDitheringTransition(fadeMaskSeed, unity_LODFade.x);
#endif

    ApplyDoubleSidedFlipOrMirror(input); // Apply double sided flip on the vertex normal

    LayerTexCoord layerTexCoord;
    ZERO_INITIALIZE(LayerTexCoord, layerTexCoord);
    GetLayerTexCoord(input, layerTexCoord);

    float depthOffset = ApplyPerPixelDisplacement(input, V, layerTexCoord);

#ifdef _DEPTHOFFSET_ON
    ApplyDepthOffsetPositionInput(V, depthOffset, GetViewForwardDir(), GetWorldToHClipMatrix(), posInput);
#endif

    // We perform the conversion to world of the normalTS outside of the GetSurfaceData
    // so it allow us to correctly deal with detail normal map and optimize the code for the layered shaders
    float3 normalTS;
    float3 bentNormalTS;
    float3 bentNormalWS;
    float alpha = GetSurfaceData(input, layerTexCoord, surfaceData, normalTS, bentNormalTS);
    GetNormalWS(input, normalTS, surfaceData.normalWS);

    // Use bent normal to sample GI if available
#ifdef _BENTNORMALMAP
    GetNormalWS(input, bentNormalTS, bentNormalWS);
#else
    bentNormalWS = surfaceData.normalWS;
#endif

    surfaceData.geomNormalWS = input.worldToTangent[2];

    // By default we use the ambient occlusion with Tri-ace trick (apply outside) for specular occlusion.
    // If user provide bent normal then we process a better term
#if defined(_BENTNORMALMAP) && defined(_ENABLESPECULAROCCLUSION)
    // If we have bent normal and ambient occlusion, process a specular occlusion
    #ifdef SPECULAR_OCCLUSION_USE_SPTD
    surfaceData.specularOcclusion = GetSpecularOcclusionFromBentAOPivot(V, bentNormalWS, surfaceData.normalWS, surfaceData.ambientOcclusion, PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness));
    #else
    surfaceData.specularOcclusion = GetSpecularOcclusionFromBentAO(V, bentNormalWS, surfaceData.normalWS, surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
    #endif
#elif defined(_MASKMAP)
    surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(dot(surfaceData.normalWS, V)), surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
#else
    surfaceData.specularOcclusion = 1.0;
#endif

    // This is use with anisotropic material
    surfaceData.tangentWS = Orthonormalize(surfaceData.tangentWS, surfaceData.normalWS);

#if HAVE_DECALS
    if (_EnableDecals)
    {
        DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, alpha);
        ApplyDecalToSurfaceData(decalSurfaceData, surfaceData);
    }
#endif

#ifdef _ENABLE_GEOMETRIC_SPECULAR_AA
    // Specular AA
    surfaceData.perceptualSmoothness = GeometricNormalFiltering(surfaceData.perceptualSmoothness, input.worldToTangent[2], _SpecularAAScreenSpaceVariance, _SpecularAAThreshold);
#endif

#if defined(DEBUG_DISPLAY)
    if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
    {
        surfaceData.baseColor = GetTextureDataDebug(_DebugMipMapMode, layerTexCoord.base.uv, _BaseColorMap, _BaseColorMap_TexelSize, _BaseColorMap_MipInfo, surfaceData.baseColor);
        surfaceData.metallic = 0;
    }

    // We need to call ApplyDebugToSurfaceData after filling the surfarcedata and before filling builtinData
    // as it can modify attribute use for static lighting
    ApplyDebugToSurfaceData(input.worldToTangent, surfaceData);
#endif

    // Caution: surfaceData must be fully initialize before calling GetBuiltinData
    GetBuiltinData(input, V, posInput, surfaceData, alpha, bentNormalWS, depthOffset, builtinData);
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDataMeshModification.hlsl"

#endif // #ifndef LAYERED_LIT_SHADER
