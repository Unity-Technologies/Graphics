#include "ShaderLibrary/SampleUVMapping.hlsl"
#include "../MaterialUtilities.hlsl"

void GetBuiltinData(FragInputs input, SurfaceData surfaceData, float alpha, float depthOffset, out BuiltinData builtinData)
{
    // Builtin Data
    builtinData.opacity = alpha;

    // TODO: Sample lightmap/lightprobe/volume proxy
    // This should also handle projective lightmap
    // Note that data input above can be use to sample into lightmap (like normal)
    builtinData.bakeDiffuseLighting = SampleBakedGI(input.positionWS, surfaceData.normalWS, input.texCoord1, input.texCoord2);

    // Emissive Intensity is only use here, but is part of BuiltinData to enforce UI parameters as we want the users to fill one color and one intensity
	builtinData.emissiveIntensity = _EmissiveIntensity; // We still store intensity here so we can reuse it with debug code

    // If we chose an emissive color, we have a dedicated texture for it and don't use MaskMap
#ifdef _EMISSIVE_COLOR
#ifdef _EMISSIVE_COLOR_MAP
    builtinData.emissiveColor = SAMPLE_TEXTURE2D(_EmissiveColorMap, sampler_EmissiveColorMap, input.texCoord0).rgb * _EmissiveColor * builtinData.emissiveIntensity;
#else
    builtinData.emissiveColor = _EmissiveColor * builtinData.emissiveIntensity;
#endif
// If we have a MaskMap, use emissive slot as a mask on baseColor
#elif defined(_MASKMAP) && !defined(LAYERED_LIT_SHADER) // With layered lit we have no emissive mask option
    builtinData.emissiveColor = surfaceData.baseColor * (SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.texCoord0).b * builtinData.emissiveIntensity).xxx;
#else
    builtinData.emissiveColor = float3(0.0, 0.0, 0.0);
#endif

    builtinData.velocity = float2(0.0, 0.0);

#ifdef _DISTORTION_ON
    float3 distortion = SAMPLE_TEXTURE2D(_DistortionVectorMap, sampler_DistortionVectorMap, input.texCoord0).rgb;
    builtinData.distortion = distortion.rg;
    builtinData.distortionBlur = distortion.b;
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
    UVMapping base;
    UVMapping details;

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
    //float flipSign = dot(sigmaY, cross(nrmVertexNormal, sigmaX) ) ? -1.0 : 1.0;
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

#define SAMPLER_NORMALMAP_IDX sampler_NormalMap
#define SAMPLER_DETAILMASK_IDX sampler_DetailMask
#define SAMPLER_DETAILMAP_IDX sampler_DetailMap
#define SAMPLER_MASKMAP_IDX sampler_MaskMap
#define SAMPLER_SPECULAROCCLUSIONMAP_IDX sampler_SpecularOcclusionMap
#define SAMPLER_HEIGHTMAP_IDX sampler_HeightMap

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
#ifdef _MASKMAP
#define _MASKMAP_IDX
#endif
#ifdef _SPECULAROCCLUSIONMAP
#define _SPECULAROCCLUSIONMAP_IDX
#endif

void ComputeLayerTexCoord(float2 texCoord0, float2 texCoord1, float2 texCoord2, float2 texCoord3,
    float3 positionWS, int mappingType, float worldScale, inout LayerTexCoord layerTexCoord, float additionalTiling = 1.0)
{
    // Handle uv0, uv1, uv2, uv3 based on _UVMappingMask weight (exclusif 0..1)
    float2 uvBase = ADD_IDX(_UVMappingMask).x * texCoord0 +
        ADD_IDX(_UVMappingMask).y * texCoord1 +
        ADD_IDX(_UVMappingMask).z * texCoord2 +
        ADD_IDX(_UVMappingMask).w * texCoord3;

    // Only used with layered, allow to have additional tiling
    uvBase *= additionalTiling.xx;


    float2 uvDetails = ADD_IDX(_UVDetailsMappingMask).x * texCoord0 +
        ADD_IDX(_UVDetailsMappingMask).y * texCoord1 +
        ADD_IDX(_UVDetailsMappingMask).z * texCoord2 +
        ADD_IDX(_UVDetailsMappingMask).w * texCoord3;

    uvDetails *= additionalTiling.xx;

    // If base is planar/triplanar then detail map is forced to be planar/triplanar
    ADD_IDX(layerTexCoord.details).mappingType = ADD_IDX(layerTexCoord.base).mappingType = mappingType;
    ADD_IDX(layerTexCoord.details).normalWS = ADD_IDX(layerTexCoord.base).normalWS = layerTexCoord.vertexNormalWS;
    // Copy data for the uvmapping
    ADD_IDX(layerTexCoord.details).triplanarWeights = ADD_IDX(layerTexCoord.base).triplanarWeights = layerTexCoord.triplanarWeights;

    // TODO: Currently we only handle world planar/triplanar but we may want local planar/triplanar.
    // In this case both position and normal need to be convert to object space.

    // planar/triplanar
    float2 uvXZ;
    float2 uvXY;
    float2 uvZY;
    GetTriplanarCoordinate(positionWS * worldScale, uvXZ, uvXY, uvZY);

    // Planar is just XZ of triplanar
    if (mappingType == UV_MAPPING_PLANAR)
    {
        uvBase = uvDetails = uvXZ;
    }

    // Apply tiling options
    ADD_IDX(layerTexCoord.base).uv = TRANSFORM_TEX(uvBase, ADD_IDX(_DiffuseColorMap));
    ADD_IDX(layerTexCoord.details).uv = TRANSFORM_TEX(uvDetails, ADD_IDX(_DetailMap));

    ADD_IDX(layerTexCoord.base).uvXZ = TRANSFORM_TEX(uvXZ, ADD_IDX(_DiffuseColorMap));
    ADD_IDX(layerTexCoord.base).uvXY = TRANSFORM_TEX(uvXY, ADD_IDX(_DiffuseColorMap));
    ADD_IDX(layerTexCoord.base).uvZY = TRANSFORM_TEX(uvZY, ADD_IDX(_DiffuseColorMap));

    ADD_IDX(layerTexCoord.details).uvXZ = TRANSFORM_TEX(uvXZ, ADD_IDX(_DetailMap));
    ADD_IDX(layerTexCoord.details).uvXY = TRANSFORM_TEX(uvXY, ADD_IDX(_DetailMap));
    ADD_IDX(layerTexCoord.details).uvZY = TRANSFORM_TEX(uvZY, ADD_IDX(_DetailMap));

#ifdef SURFACE_GRADIENT
    // This part is only relevant for normal mapping with UV_MAPPING_UVSET
    // Note: This code work only in pixel shader (as we rely on ddx), it should not be use in other context
    ADD_IDX(layerTexCoord.base).tangentWS = ADD_IDX(_UVMappingMask).x * layerTexCoord.vertexTangentWS0 +
        ADD_IDX(_UVMappingMask).y * layerTexCoord.vertexTangentWS1 +
        ADD_IDX(_UVMappingMask).z * layerTexCoord.vertexTangentWS2 +
        ADD_IDX(_UVMappingMask).w * layerTexCoord.vertexTangentWS3;

    ADD_IDX(layerTexCoord.base).bitangentWS = ADD_IDX(_UVMappingMask).x * layerTexCoord.vertexBitangentWS0 +
        ADD_IDX(_UVMappingMask).y * layerTexCoord.vertexBitangentWS1 +
        ADD_IDX(_UVMappingMask).z * layerTexCoord.vertexBitangentWS2 +
        ADD_IDX(_UVMappingMask).w * layerTexCoord.vertexBitangentWS3;

    ADD_IDX(layerTexCoord.details).tangentWS = ADD_IDX(_UVDetailsMappingMask).x * layerTexCoord.vertexTangentWS0 +
        ADD_IDX(_UVDetailsMappingMask).y * layerTexCoord.vertexTangentWS1 +
        ADD_IDX(_UVDetailsMappingMask).z * layerTexCoord.vertexTangentWS2 +
        ADD_IDX(_UVDetailsMappingMask).w * layerTexCoord.vertexTangentWS3;

    ADD_IDX(layerTexCoord.details).bitangentWS = ADD_IDX(_UVDetailsMappingMask).x * layerTexCoord.vertexBitangentWS0 +
        ADD_IDX(_UVDetailsMappingMask).y * layerTexCoord.vertexBitangentWS1 +
        ADD_IDX(_UVDetailsMappingMask).z * layerTexCoord.vertexBitangentWS2 +
        ADD_IDX(_UVDetailsMappingMask).w * layerTexCoord.vertexBitangentWS3;
#endif
}

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
    _UVMappingMask = float4(1.0, 0.0, 0.0, 0.0);
    ComputeLayerTexCoord(texCoord0, texCoord1, texCoord2, texCoord3,
        positionWS, mappingType, _TexWorldScale, layerTexCoord);
}

// This is call only in this file
// layerTexCoord must have been initialize to 0 outside of this function
void GetLayerTexCoord(FragInputs input, inout LayerTexCoord layerTexCoord)
{
#ifdef SURFACE_GRADIENT
    GenerateLayerTexCoordBasisTB(input, layerTexCoord);
#endif

    GetLayerTexCoord(input.texCoord0, input.texCoord1, input.texCoord2, input.texCoord3,
        input.positionWS, input.worldToTangent[2].xyz, layerTexCoord);
}

float3 GetNormalTS(FragInputs input, LayerTexCoord layerTexCoord, float3 detailNormalTS, float detailMask, bool useBias, float bias)
{
    float3 normalTS;

#ifdef _NORMALMAP_IDX
#ifdef _NORMALMAP_TANGENT_SPACE_IDX
    if (useBias)
    {
        normalTS = SAMPLE_UVMAPPING_NORMALMAP_BIAS(ADD_IDX(_NormalMap), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base), ADD_IDX(_NormalScale), bias);
    }
    else
    {
        normalTS = SAMPLE_UVMAPPING_NORMALMAP(ADD_IDX(_NormalMap), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base), ADD_IDX(_NormalScale));
    }
#else // Object space
    // to be able to combine object space normal with detail map or to apply a "scale" we transform it to tangent space (object space normal composition is complex operation).
    // then later we will re-transform it to world space.
    // Note: There is no such a thing like triplanar with object space normal, so we call directly 2D function
    if (useBias)
    {
#ifdef SURFACE_GRADIENT
        // /We need to decompress the normal ourselve here as UnpackNormalRGB will return a surface gradient
        float3 normalOS = SAMPLE_TEXTURE2D_BIAS(ADD_IDX(_NormalMap), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base).uv, bias).xyz * 2.0 - 1.0;
        // normalize(normalOS) // TO CHECK: SurfaceGradientFromPerturbedNormal doesn't require normalOS to be normalize, to check
        normalTS = SurfaceGradientFromPerturbedNormal(input.worldToTangent[2], normalOS);
        normalTS *= ADD_IDX(_NormalScale);
#else
        float3 normalOS = UnpackNormalRGB(SAMPLE_TEXTURE2D_BIAS(ADD_IDX(_NormalMap), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base).uv, bias), 1.0);
        normalTS = TransformObjectToTangent(normalOS, input.worldToTangent);
        normalTS.xy *= ADD_IDX(_NormalScale);  // Scale in tangent space
        normalTS = (normalTS);
#endif
    }
    else
    {
#ifdef SURFACE_GRADIENT
        // /We need to decompress the normal ourselve here as UnpackNormalRGB will return a surface gradient
        float3 normalOS = SAMPLE_TEXTURE2D(ADD_IDX(_NormalMap), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base).uv).xyz * 2.0 - 1.0;
        // normalize(normalOS) // TO CHECK: SurfaceGradientFromPerturbedNormal doesn't require normalOS to be normalize, to check
        normalTS = SurfaceGradientFromPerturbedNormal(input.worldToTangent[2], normalOS);
        normalTS *= ADD_IDX(_NormalScale);
#else
        float3 normalOS = UnpackNormalRGB(SAMPLE_TEXTURE2D(ADD_IDX(_NormalMap), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base).uv), 1.0);
        normalTS = TransformObjectToTangent(normalOS, input.worldToTangent);
        normalTS.xy *= ADD_IDX(_NormalScale); // Scale in tangent space
        normalTS = (normalTS);
#endif
    }
#endif

#ifdef _DETAIL_MAP_IDX
#ifdef SURFACE_GRADIENT
    normalTS += detailNormalTS;
#else
    normalTS = lerp(normalTS, BlendNormalRNM(normalTS, detailNormalTS), detailMask);
#endif
#endif
#else
#ifdef SURFACE_GRADIENT
    normalTS = float3(0.0, 0.0, 0.0); // No gradient
#else
    normalTS = float3(0.0, 0.0, 1.0);
#endif
#endif

    return normalTS;
}

float GetSurfaceData(FragInputs input, LayerTexCoord layerTexCoord, out SurfaceData surfaceData, out float3 normalTS)
{
    float alpha = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_DiffuseColorMap), ADD_ZERO_IDX(sampler_DiffuseColorMap), ADD_IDX(layerTexCoord.base)).a * ADD_IDX(_DiffuseColor).a;

    // Perform alha test very early to save performance (a killed pixel will not sample textures)
#if defined(_ALPHATEST_ON) && !defined(LAYERED_LIT_SHADER)
    #ifdef HAIR_SHADOW
    clip(alpha - _AlphaCutoffShadow); // Let artists make hair shadow thiner
    #elif defined(HAIR_TRANSPARENT_DEPTH_WRITE)
    alpha = alpha > _AlphaCutoffOpacityThreshold ? 1.0 : alpha;
    clip(alpha - _AlphaCutoffPrepass); // Let artists make prepass cutout thinner
    #else
    clip(alpha - _AlphaCutoff);
    #endif
#endif

    float3 detailNormalTS = float3(0.0, 0.0, 0.0);
    float detailMask = 0.0;
#ifdef _DETAIL_MAP_IDX
    detailMask = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_DetailMask), SAMPLER_DETAILMASK_IDX, ADD_IDX(layerTexCoord.base)).g;
    float2 detailAlbedoAndSmoothness = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_DetailMap), SAMPLER_DETAILMAP_IDX, ADD_IDX(layerTexCoord.details)).rb;
    float detailAlbedo = detailAlbedoAndSmoothness.r;
    float detailSmoothness = detailAlbedoAndSmoothness.g;
    // Resample the detail map but this time for the normal map. This call should be optimize by the compiler
    // We split both call due to trilinear mapping
    detailNormalTS = SAMPLE_UVMAPPING_NORMALMAP_AG(ADD_IDX(_DetailMap), SAMPLER_DETAILMAP_IDX, ADD_IDX(layerTexCoord.details), ADD_ZERO_IDX(_DetailNormalScale));
#endif

    surfaceData.diffuseColor = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_DiffuseColorMap), ADD_ZERO_IDX(sampler_DiffuseColorMap), ADD_IDX(layerTexCoord.base)).rgb * ADD_IDX(_DiffuseColor).rgb;
#ifdef _DETAIL_MAP_IDX
    surfaceData.diffuseColor *= LerpWhiteTo(2.0 * saturate(detailAlbedo * ADD_IDX(_DetailAlbedoScale)), detailMask);
#endif

#ifdef _SPECULAROCCLUSIONMAP_IDX
    // TODO: Do something. For now just take alpha channel
    surfaceData.specularOcclusion = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_SpecularOcclusionMap), SAMPLER_SPECULAROCCLUSIONMAP_IDX, ADD_IDX(layerTexCoord.base)).a;
#else
    // The specular occlusion will be perform outside the internal loop
    surfaceData.specularOcclusion = 1.0;
#endif
    surfaceData.normalWS = float3(0.0, 0.0, 0.0); // Need to init this to keep quiet the compiler, but this is overriden later (0, 0, 0) so if we forget to override the compiler may comply.

                                                  // TODO: think about using BC5
    normalTS = ADD_IDX(GetNormalTS)(input, layerTexCoord, detailNormalTS, detailMask, false, 0.0);

#if defined(_MASKMAP_IDX)
    surfaceData.perceptualSmoothness = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_MaskMap), SAMPLER_MASKMAP_IDX, ADD_IDX(layerTexCoord.base)).a;
#else
    surfaceData.perceptualSmoothness = 1.0;
#endif
    surfaceData.perceptualSmoothness *= ADD_IDX(_Smoothness);
#ifdef _DETAIL_MAP_IDX
    surfaceData.perceptualSmoothness *= LerpWhiteTo(2.0 * saturate(detailSmoothness * ADD_IDX(_DetailSmoothnessScale)), detailMask);
#endif

    // MaskMap is RGBA: Metallic, Ambient Occlusion (Optional), emissive Mask (Optional), Smoothness
#ifdef _MASKMAP_IDX
    surfaceData.ambientOcclusion = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_MaskMap), SAMPLER_MASKMAP_IDX, ADD_IDX(layerTexCoord.base)).g;
#else
    surfaceData.ambientOcclusion = 1.0;
#endif

    // TODO: think about using BC5
#ifdef _TANGENTMAP
#ifdef _NORMALMAP_TANGENT_SPACE_IDX // Normal and tangent use same space
    float3 tangentTS = SAMPLE_UVMAPPING_NORMALMAP(_TangentMap, sampler_TangentMap, layerTexCoord.base, 1.0);
    surfaceData.tangentWS = TransformTangentToWorld(tangentTS, input.worldToTangent);
#else // Object space
    // Note: There is no such a thing like triplanar with object space normal, so we call directly 2D function
    float3 tangentOS = UnpackNormalRGB(SAMPLE_TEXTURE2D(_TangentMap, sampler_TangentMap, layerTexCoord.base.uv), 1.0);
    surfaceData.tangentWS = TransformObjectToWorldDir(tangentOS);
#endif
#else
#ifdef SURFACE_GRADIENT
    surfaceData.tangentWS = normalize(input.worldToTangent[0].xyz); // The tangent is not normalize in worldToTangent when using surface gradient
#else
    surfaceData.tangentWS = input.worldToTangent[0].xyz;
#endif
#endif

#ifdef _ANISOTROPYMAP
    surfaceData.anisotropy = SAMPLE_UVMAPPING_TEXTURE2D(_AnisotropyMap, sampler_AnisotropyMap, layerTexCoord.base).b;
#else
    surfaceData.anisotropy = 1.0;
#endif
    surfaceData.anisotropy *= ADD_IDX(_Anisotropy);

    return alpha;
}

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
#ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
    LODDitheringTransition(posInput.unPositionSS, unity_LODFade.y); // Note that we pass the quantized value of LOD fade
#endif

    //ApplyDoubleSidedFlipOrMirror(input); // flipping is not working, so comment this off for hair( Temp )

    LayerTexCoord layerTexCoord;
    ZERO_INITIALIZE(LayerTexCoord, layerTexCoord);
    GetLayerTexCoord(input, layerTexCoord);

    float depthOffset = 0.0;

#ifdef _DEPTHOFFSET_ON
    ApplyDepthOffsetPositionInput(GetCameraForwardDir(), depthOffset, GetWorldToHClipMatrix(), posInput);
#endif

    // We perform the conversion to world of the normalTS outside of the GetSurfaceData
    // so it allow us to correctly deal with detail normal map and optimize the code for the layered shaders
    float3 normalTS;
    float alpha = GetSurfaceData(input, layerTexCoord, surfaceData, normalTS);
    GetNormalAndTangentWS(input, V, normalTS, surfaceData.normalWS, surfaceData.tangentWS);
    // Done one time for all layered - cumulate with spec occ alpha for now
    surfaceData.specularOcclusion *= GetHorizonOcclusion(V, surfaceData.normalWS, input.worldToTangent[2].xyz, _HorizonFade);

    // Caution: surfaceData must be fully initialize before calling GetBuiltinData
    GetBuiltinData(input, surfaceData, alpha, depthOffset, builtinData);
}