//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "../../../Core/ShaderLibrary/SampleUVMapping.hlsl"
#include "../MaterialUtilities.hlsl"

void DoAlphaTest(float alpha, float alphaCutoff)
{
    // For Deferred:
    // If we have a prepass, we need to remove the clip from the GBuffer pass (otherwise HiZ does not work on PS4)
    // For Forward (Full forward or ForwardOnlyOpaque in deferred):
    // Opaque geometry always has a depth pre-pass so we never want to do the clip here. For transparent we perform the clip as usual.
    #if ((SHADER_PASS == SHADERPASS_GBUFFER) && !defined(_BYPASS_ALPHA_TEST)) || (SHADER_PASS == SHADERPASS_FORWARD && defined(SURFACE_TYPE_TRANSPARENT))
        clip(alpha - alphaCutoff);
    #endif
}

// TODO: move this function to commonLighting.hlsl once validated it work correctly
float GetSpecularOcclusionFromBentAO(float3 V, float3 bentNormalWS, SurfaceData surfaceData)
{
    // Retrieve cone angle
    // Ambient occlusion is cosine weighted, thus use following equation. See slide 129
    float cosAv = sqrt(1.0 - surfaceData.ambientOcclusion);
    float roughness = max(PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness), 0.01); // Clamp to 0.01 to avoid edge cases
    float cosAs = exp2(-3.32193 * Sqr(roughness));
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

    // Emissive Intensity is only use here, but is part of BuiltinData to enforce UI parameters as we want the users to fill one color and one intensity
    builtinData.emissiveIntensity = _EmissiveIntensity; // We still store intensity here so we can reuse it with debug code

    builtinData.emissiveColor = _EmissiveColor * builtinData.emissiveIntensity * lerp(float3(1.0, 1.0, 1.0), surfaceData.baseColor.rgb, _AlbedoAffectEmissive);
#ifdef _EMISSIVE_COLOR_MAP
    builtinData.emissiveColor *= SAMPLE_TEXTURE2D(_EmissiveColorMap, sampler_EmissiveColorMap, input.texCoord0).rgb;
#endif

    builtinData.velocity = float2(0.0, 0.0);

#if (SHADERPASS == SHADERPASS_DISTORTION)
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

// Share by Lit and LayeredLit. Return object scaling for displacement map depends if it is vertex (affect vertex displacement) or pixel displacement (affect tiling)
float3 GetDisplacementObjectScale(bool vertexDisplacement)
{
    float3 objectScale = float3(1.0, 1.0, 1.0);

    // TODO: This should be an uniform for the object, this code should be remove once we have it. - Workaround for now
    // To handle object scaling with pixel displacement we need to multiply the view vector by the inverse scale.
    // To Handle object scaling with vertex/tessellation displacement we must multiply displacement by object scale
    // Currently we extract either the scale (ObjectToWorld) or the inverse scale (worldToObject) directly by taking the transform matrix
    float4x4 worldTransform;
    if (vertexDisplacement)
    {
        worldTransform = GetObjectToWorldMatrix();
    }

    else
    {
        worldTransform = GetWorldToObjectMatrix();
    }

    objectScale.x = length(float3(worldTransform._m00, worldTransform._m01, worldTransform._m02));
    // In the specific case of pixel displacement mapping, to get a consistent behavior compare to tessellation we require to not take into account y scale if lock object scale is not enabled
#if !defined(_PIXEL_DISPLACEMENT) || (defined(_PIXEL_DISPLACEMENT_LOCK_OBJECT_SCALE))
    objectScale.y = length(float3(worldTransform._m10, worldTransform._m11, worldTransform._m12));
#endif
    objectScale.z = length(float3(worldTransform._m20, worldTransform._m21, worldTransform._m22));

    return objectScale;
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

#define SAMPLER_DETAILMAP_IDX sampler_DetailMap
#define SAMPLER_MASKMAP_IDX sampler_MaskMap
#define SAMPLER_HEIGHTMAP_IDX sampler_HeightMap

// include LitDataInternal to define GetSurfaceData
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
#ifdef _BENTNORMALMAP
#define _BENTNORMALMAP_IDX
#endif
#include "LitDataInternal.hlsl"

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

// Note: This function is call by both Per vertex and Per pixel displacement
float GetMaxDisplacement()
{
    float maxDisplacement = 0.0;
#if defined(_HEIGHTMAP)
    maxDisplacement = abs(_HeightAmplitude); // _HeightAmplitude can be negative if min and max are inverted, but the max displacement must be positive
#endif
    return maxDisplacement;
}

// Return the minimun uv size for all layers including triplanar
float2 GetMinUvSize(LayerTexCoord layerTexCoord)
{
    float2 minUvSize = float2(FLT_MAX, FLT_MAX);

#if defined(_HEIGHTMAP)
    if (layerTexCoord.base.mappingType == UV_MAPPING_TRIPLANAR)
    {
        minUvSize = min(layerTexCoord.base.uvZY * _HeightMap_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base.uvXZ * _HeightMap_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base.uvXY * _HeightMap_TexelSize.zw, minUvSize);
    }
    else
    {
        minUvSize = min(layerTexCoord.base.uv * _HeightMap_TexelSize.zw, minUvSize);
    }
#endif

    return minUvSize;
}

struct PerPixelHeightDisplacementParam
{
    float2 uv;
};

float ComputePerPixelHeightDisplacement(float2 texOffsetCurrent, float lod, PerPixelHeightDisplacementParam param)
{
    // Note: No multiply by amplitude here. This is include in the maxHeight provide to POM
    // Tiling is automatically handled correctly here.
    return SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, param.uv + texOffsetCurrent, lod).r;
}

#include "../../../Core/ShaderLibrary/PerPixelDisplacement.hlsl"

void ApplyDisplacementTileScale(inout float height)
{
    // Inverse tiling scale = 2 / (abs(_BaseColorMap_ST.x) + abs(_BaseColorMap_ST.y)
    // Inverse tiling scale *= (1 / _TexWorldScale) if planar or triplanar
#ifdef _DISPLACEMENT_LOCK_TILING_SCALE
    height *= _InvTilingScale;
#endif
}

float ApplyPerPixelDisplacement(FragInputs input, float3 V, inout LayerTexCoord layerTexCoord)
{
#if defined(_PIXEL_DISPLACEMENT) && defined(_HEIGHTMAP)
    // These variables are known at the compile time.
    bool isPlanar = layerTexCoord.base.mappingType == UV_MAPPING_PLANAR;
    bool isTriplanar = layerTexCoord.base.mappingType == UV_MAPPING_TRIPLANAR;

    // See comment in layered version for details
    float  maxHeight = GetMaxDisplacement();
    ApplyDisplacementTileScale(maxHeight);
    float2 minUvSize = GetMinUvSize(layerTexCoord);
    float  lod       = ComputeTextureLOD(minUvSize);

    // TODO: precompute uvSpaceScale
    float2 invPrimScale = (isPlanar || isTriplanar) ? float2(1.0, 1.0) : _InvPrimScale.xy;
    float  worldScale   = (isPlanar || isTriplanar) ? _TexWorldScale : 1.0;
    float2 uvSpaceScale = invPrimScale * _BaseColorMap_ST.xy * (worldScale * maxHeight);
    float2 scaleOffsetDetails = _DetailMap_ST.xy;

    PerPixelHeightDisplacementParam ppdParam;

    float height = 0; // final height processed
    float NdotV  = 0;

    // planar/triplanar
    float2 uvXZ;
    float2 uvXY;
    float2 uvZY;
    GetTriplanarCoordinate(V, uvXZ, uvXY, uvZY);

    // TODO: support object space planar/triplanar ?

    // We need to calculate the texture space direction. It depends on the mapping.
    if (isTriplanar)
    {
        float planeHeight;

        // Perform a POM in each direction and modify appropriate texture coordinate
        [branch] if (layerTexCoord.triplanarWeights.x >= 0.001)
        {
            ppdParam.uv      = layerTexCoord.base.uvZY;
            float3 viewDirTS = float3(uvZY, abs(V.x));
            float3 viewDirUV = normalize(float3(viewDirTS.xy * uvSpaceScale, viewDirTS.z)); // TODO: skip normalize
            float  unitAngle = saturate(FastACosPos(viewDirUV.z) * INV_HALF_PI);            // TODO: optimize
            int    numSteps  = (int)lerp(_PPDMinSamples, _PPDMaxSamples, unitAngle);
            float2 offset    = ParallaxOcclusionMapping(lod, _PPDLodThreshold, numSteps, viewDirUV, ppdParam, planeHeight);

            // Apply offset to all triplanar UVSet
            layerTexCoord.base.uvZY    += offset;
            layerTexCoord.details.uvZY += offset * scaleOffsetDetails;
            height += layerTexCoord.triplanarWeights.x * planeHeight;
            NdotV  += layerTexCoord.triplanarWeights.x * viewDirTS.z;
        }

        [branch] if (layerTexCoord.triplanarWeights.y >= 0.001)
        {
            ppdParam.uv      = layerTexCoord.base.uvXZ;
            float3 viewDirTS = float3(uvXZ, abs(V.y));
            float3 viewDirUV = normalize(float3(viewDirTS.xy * uvSpaceScale, viewDirTS.z)); // TODO: skip normalize
            float  unitAngle = saturate(FastACosPos(viewDirUV.z) * INV_HALF_PI);            // TODO: optimize
            int    numSteps  = (int)lerp(_PPDMinSamples, _PPDMaxSamples, unitAngle);
            float2 offset    = ParallaxOcclusionMapping(lod, _PPDLodThreshold, numSteps, viewDirUV, ppdParam, planeHeight);

            layerTexCoord.base.uvXZ    += offset;
            layerTexCoord.details.uvXZ += offset * scaleOffsetDetails;
            height += layerTexCoord.triplanarWeights.y * planeHeight;
            NdotV  += layerTexCoord.triplanarWeights.y * viewDirTS.z;
        }

        [branch] if (layerTexCoord.triplanarWeights.z >= 0.001)
        {
            ppdParam.uv      = layerTexCoord.base.uvXY;
            float3 viewDirTS = float3(uvXY, abs(V.z));
            float3 viewDirUV = normalize(float3(viewDirTS.xy * uvSpaceScale, viewDirTS.z)); // TODO: skip normalize
            float  unitAngle = saturate(FastACosPos(viewDirUV.z) * INV_HALF_PI);            // TODO: optimize
            int    numSteps  = (int)lerp(_PPDMinSamples, _PPDMaxSamples, unitAngle);
            float2 offset    = ParallaxOcclusionMapping(lod, _PPDLodThreshold, numSteps, viewDirUV, ppdParam, planeHeight);

            layerTexCoord.base.uvXY    += offset;
            layerTexCoord.details.uvXY += offset * scaleOffsetDetails;
            height += layerTexCoord.triplanarWeights.z * planeHeight;
            NdotV  += layerTexCoord.triplanarWeights.z * viewDirTS.z;
        }
    }
    else
    {
        ppdParam.uv = layerTexCoord.base.uv; // For planar it is uv too, not uvXZ

        // Note: The TBN is not normalize as it is based on mikkt. We should normalize it, but POM is always use on simple enough surfarce that mean it is not required (save 2 normalize). Tag: SURFACE_GRADIENT
        float3 viewDirTS = isPlanar ? float3(uvXZ, V.y) : TransformWorldToTangent(V, input.worldToTangent) * GetDisplacementObjectScale(false).xzy; // Switch from Y-up to Z-up (as we move to tangent space)
        NdotV = viewDirTS.z;

        // Transform the view vector into the UV space.
        float3 viewDirUV    = normalize(float3(viewDirTS.xy * uvSpaceScale, viewDirTS.z)); // TODO: skip normalize
        float  unitAngle    = saturate(FastACosPos(viewDirUV.z) * INV_HALF_PI);            // TODO: optimize
        int    numSteps     = (int)lerp(_PPDMinSamples, _PPDMaxSamples, unitAngle);
        float2 offset       = ParallaxOcclusionMapping(lod, _PPDLodThreshold, numSteps, viewDirUV, ppdParam, height);

        // Apply offset to all UVSet0 / planar
        layerTexCoord.base.uv += offset;
        // Note: Applying offset on detail uv is only correct if it use the same UVSet or is planar or triplanar. It is up to the user to do the correct thing.
        layerTexCoord.details.uv += offset * scaleOffsetDetails;
    }

    // Since POM "pushes" geometry inwards (rather than extrude it), { height = height - 1 }.
    // Since the result is used as a 'depthOffsetVS', it needs to be positive, so we flip the sign.
    float verticalDisplacement = maxHeight - height * maxHeight;
    return verticalDisplacement / max(NdotV, 0.001);
#else
    return 0.0;
#endif
}

// Calculate displacement for per vertex displacement mapping
float3 ComputePerVertexDisplacement(LayerTexCoord layerTexCoord, float4 vertexColor, float lod)
{
    float height = (SAMPLE_UVMAPPING_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, layerTexCoord.base, lod).r - _HeightCenter) * _HeightAmplitude;

    // Height is affected by tiling property and by object scale (depends on option).
    // Apply scaling from tiling properties (TexWorldScale and tiling from BaseColor)
    ApplyDisplacementTileScale(height);
    // Applying scaling of the object if requested
#ifdef _VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE
    float3 objectScale = GetDisplacementObjectScale(true);
    // Reminder: mappingType is know statically, so code below is optimize by the compiler
    // Planar and Triplanar are in world space thus it is independent of object scale
    return height.xxx * ((layerTexCoord.base.mappingType == UV_MAPPING_UVSET) ? objectScale : float3(1.0, 1.0, 1.0));
#else
    return height.xxx;
#endif
}

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
#ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
    LODDitheringTransition(posInput.unPositionSS, unity_LODFade.x);
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
    // Use bent normal to sample GI if available
    surfaceData.specularOcclusion = 1.0;
#ifdef _BENTNORMALMAP
    GetNormalWS(input, V, bentNormalTS, bentNormalWS);
    #ifdef _ENABLESPECULAROCCLUSION
    // If we have bent normal and ambient occlusion, process a specular occlusion
    surfaceData.specularOcclusion = GetSpecularOcclusionFromBentAO(V, bentNormalWS, surfaceData);
    #endif
#else
    bentNormalWS = surfaceData.normalWS;
#endif
    // This is use with anisotropic material
    surfaceData.tangentWS = Orthonormalize(surfaceData.tangentWS, surfaceData.normalWS);

    // Caution: surfaceData must be fully initialize before calling GetBuiltinData
    GetBuiltinData(input, surfaceData, alpha, bentNormalWS, depthOffset, builtinData);
}

#else // if LAYERED_LIT_SHADER

#define LAYERS_HEIGHTMAP_ENABLE (defined(_HEIGHTMAP0) || defined(_HEIGHTMAP1) || (_LAYER_COUNT > 2 && defined(_HEIGHTMAP2)) || (_LAYER_COUNT > 3 && defined(_HEIGHTMAP3)))

// Number of sampler are limited, we need to share sampler as much as possible with lit material
// for this we put the constraint that the sampler are the same in a layered material for all textures of the same type
// then we take the sampler matching the first textures use of this type
#if defined(_NORMALMAP0)
    #if defined(_NORMALMAP_TANGENT_SPACE0)
    #define SAMPLER_NORMALMAP_IDX sampler_NormalMap0
    #else
    #define SAMPLER_NORMALMAP_IDX sampler_NormalMapOS0
    #endif
#elif defined(_NORMALMAP1)
    #if defined(_NORMALMAP_TANGENT_SPACE1)
    #define SAMPLER_NORMALMAP_IDX sampler_NormalMap1
    #else
    #define SAMPLER_NORMALMAP_IDX sampler_NormalMapOS1
    #endif
#elif defined(_NORMALMAP2)
    #if defined(_NORMALMAP_TANGENT_SPACE2)
    #define SAMPLER_NORMALMAP_IDX sampler_NormalMap2
    #else
    #define SAMPLER_NORMALMAP_IDX sampler_NormalMapOS2
    #endif
#elif defined(_NORMALMAP3)
    #if defined(_NORMALMAP_TANGENT_SPACE3)
    #define SAMPLER_NORMALMAP_IDX sampler_NormalMap3
    #else
    #define SAMPLER_NORMALMAP_IDX sampler_NormalMapOS3
    #endif
#elif defined(_BENTNORMALMAP0)
    #if defined(_NORMALMAP_TANGENT_SPACE0)
    #define SAMPLER_NORMALMAP_IDX sampler_BentNormalMap0
    #else
    #define SAMPLER_NORMALMAP_IDX sampler_BentNormalMapOS0
    #endif
#elif defined(_BENTNORMALMAP1)
    #if defined(_NORMALMAP_TANGENT_SPACE1)
    #define SAMPLER_NORMALMAP_IDX sampler_BentNormalMap1
    #else
    #define SAMPLER_NORMALMAP_IDX sampler_BentNormalMapOS1
    #endif
#elif defined(_BENTNORMALMAP2)
    #if defined(_NORMALMAP_TANGENT_SPACE2)
    #define SAMPLER_NORMALMAP_IDX sampler_BentNormalMap2
    #else
    #define SAMPLER_NORMALMAP_IDX sampler_BentNormalMapOS2
    #endif
#else
    #if defined(_NORMALMAP_TANGENT_SPACE3)
    #define SAMPLER_NORMALMAP_IDX sampler_BentNormalMap3
    #else
    #define SAMPLER_NORMALMAP_IDX sampler_BentNormalMapOS3
    #endif
#endif

#if defined(_DETAIL_MAP0)
#define SAMPLER_DETAILMAP_IDX sampler_DetailMap0
#elif defined(_DETAIL_MAP1)
#define SAMPLER_DETAILMAP_IDX sampler_DetailMap1
#elif defined(_DETAIL_MAP2)
#define SAMPLER_DETAILMAP_IDX sampler_DetailMap2
#else
#define SAMPLER_DETAILMAP_IDX sampler_DetailMap3
#endif

#if defined(_MASKMAP0)
#define SAMPLER_MASKMAP_IDX sampler_MaskMap0
#elif defined(_MASKMAP1)
#define SAMPLER_MASKMAP_IDX sampler_MaskMap1
#elif defined(_MASKMAP2)
#define SAMPLER_MASKMAP_IDX sampler_MaskMap2
#else
#define SAMPLER_MASKMAP_IDX sampler_MaskMap3
#endif

#if defined(_HEIGHTMAP0)
#define SAMPLER_HEIGHTMAP_IDX sampler_HeightMap0
#elif defined(_HEIGHTMAP1)
#define SAMPLER_HEIGHTMAP_IDX sampler_HeightMap1
#elif defined(_HEIGHTMAP2)
#define SAMPLER_HEIGHTMAP_IDX sampler_HeightMap2
#elif defined(_HEIGHTMAP3)
#define SAMPLER_HEIGHTMAP_IDX sampler_HeightMap3
#endif

// Define a helper macro

#define ADD_ZERO_IDX(Name) Name##0

// include LitDataInternal multiple time to define the variation of GetSurfaceData for each layer
#define LAYER_INDEX 0
#define ADD_IDX(Name) Name##0
#ifdef _NORMALMAP0
#define _NORMALMAP_IDX
#endif
#ifdef _NORMALMAP_TANGENT_SPACE0
#define _NORMALMAP_TANGENT_SPACE_IDX
#endif
#ifdef _DETAIL_MAP0
#define _DETAIL_MAP_IDX
#endif
#ifdef _MASKMAP0
#define _MASKMAP_IDX
#endif
#ifdef _BENTNORMALMAP0
#define _BENTNORMALMAP_IDX
#endif
#include "LitDataInternal.hlsl"
#undef LAYER_INDEX
#undef ADD_IDX
#undef _NORMALMAP_IDX
#undef _NORMALMAP_TANGENT_SPACE_IDX
#undef _DETAIL_MAP_IDX
#undef _MASKMAP_IDX
#undef _BENTNORMALMAP_IDX

#define LAYER_INDEX 1
#define ADD_IDX(Name) Name##1
#ifdef _NORMALMAP1
#define _NORMALMAP_IDX
#endif
#ifdef _NORMALMAP_TANGENT_SPACE1
#define _NORMALMAP_TANGENT_SPACE_IDX
#endif
#ifdef _DETAIL_MAP1
#define _DETAIL_MAP_IDX
#endif
#ifdef _MASKMAP1
#define _MASKMAP_IDX
#endif
#ifdef _BENTNORMALMAP1
#define _BENTNORMALMAP_IDX
#endif
#include "LitDataInternal.hlsl"
#undef LAYER_INDEX
#undef ADD_IDX
#undef _NORMALMAP_IDX
#undef _NORMALMAP_TANGENT_SPACE_IDX
#undef _DETAIL_MAP_IDX
#undef _MASKMAP_IDX
#undef _BENTNORMALMAP_IDX

#define LAYER_INDEX 2
#define ADD_IDX(Name) Name##2
#ifdef _NORMALMAP2
#define _NORMALMAP_IDX
#endif
#ifdef _NORMALMAP_TANGENT_SPACE2
#define _NORMALMAP_TANGENT_SPACE_IDX
#endif
#ifdef _DETAIL_MAP2
#define _DETAIL_MAP_IDX
#endif
#ifdef _MASKMAP2
#define _MASKMAP_IDX
#endif
#ifdef _BENTNORMALMAP2
#define _BENTNORMALMAP_IDX
#endif
#include "LitDataInternal.hlsl"
#undef LAYER_INDEX
#undef ADD_IDX
#undef _NORMALMAP_IDX
#undef _NORMALMAP_TANGENT_SPACE_IDX
#undef _DETAIL_MAP_IDX
#undef _MASKMAP_IDX
#undef _BENTNORMALMAP_IDX

#define LAYER_INDEX 3
#define ADD_IDX(Name) Name##3
#ifdef _NORMALMAP3
#define _NORMALMAP_IDX
#endif
#ifdef _NORMALMAP_TANGENT_SPACE3
#define _NORMALMAP_TANGENT_SPACE_IDX
#endif
#ifdef _DETAIL_MAP3
#define _DETAIL_MAP_IDX
#endif
#ifdef _MASKMAP3
#define _MASKMAP_IDX
#endif
#ifdef _BENTNORMALMAP3
#define _BENTNORMALMAP_IDX
#endif
#include "LitDataInternal.hlsl"
#undef LAYER_INDEX
#undef ADD_IDX
#undef _NORMALMAP_IDX
#undef _NORMALMAP_TANGENT_SPACE_IDX
#undef _DETAIL_MAP_IDX
#undef _MASKMAP_IDX
#undef _BENTNORMALMAP_IDX

float3 BlendLayeredVector3(float3 x0, float3 x1, float3 x2, float3 x3, float weight[4])
{
    float3 result = float3(0.0, 0.0, 0.0);

    result = x0 * weight[0] + x1 * weight[1];
#if _LAYER_COUNT >= 3
    result += (x2 * weight[2]);
#endif
#if _LAYER_COUNT >= 4
    result += x3 * weight[3];
#endif

    return result;
}

float BlendLayeredScalar(float x0, float x1, float x2, float x3, float weight[4])
{
    float result = 0.0;

    result = x0 * weight[0] + x1 * weight[1];
#if _LAYER_COUNT >= 3
    result += x2 * weight[2];
#endif
#if _LAYER_COUNT >= 4
    result += x3 * weight[3];
#endif

    return result;
}

#define SURFACEDATA_BLEND_VECTOR3(surfaceData, name, mask) BlendLayeredVector3(MERGE_NAME(surfaceData, 0) MERGE_NAME(., name), MERGE_NAME(surfaceData, 1) MERGE_NAME(., name), MERGE_NAME(surfaceData, 2) MERGE_NAME(., name), MERGE_NAME(surfaceData, 3) MERGE_NAME(., name), mask);
#define SURFACEDATA_BLEND_SCALAR(surfaceData, name, mask) BlendLayeredScalar(MERGE_NAME(surfaceData, 0) MERGE_NAME(., name), MERGE_NAME(surfaceData, 1) MERGE_NAME(., name), MERGE_NAME(surfaceData, 2) MERGE_NAME(., name), MERGE_NAME(surfaceData, 3) MERGE_NAME(., name), mask);
#define PROP_BLEND_SCALAR(name, mask) BlendLayeredScalar(name##0, name##1, name##2, name##3, mask);

void GetLayerTexCoord(float2 texCoord0, float2 texCoord1, float2 texCoord2, float2 texCoord3,
                      float3 positionWS, float3 vertexNormalWS, inout LayerTexCoord layerTexCoord)
{
    layerTexCoord.vertexNormalWS = vertexNormalWS;
    layerTexCoord.triplanarWeights = ComputeTriplanarWeights(vertexNormalWS);

    int mappingType = UV_MAPPING_UVSET;
#if defined(_LAYER_MAPPING_PLANAR_BLENDMASK)
    mappingType = UV_MAPPING_PLANAR;
#elif defined(_LAYER_MAPPING_TRIPLANAR_BLENDMASK)
    mappingType = UV_MAPPING_TRIPLANAR;
#endif

    // Note: Blend mask have its dedicated mapping and tiling.
    // To share code, we simply call the regular code from the main layer for it then save the result, then do regular call for all layers.
    ComputeLayerTexCoord0(  texCoord0, texCoord1, texCoord2, texCoord3, _UVMappingMaskBlendMask, _UVMappingMaskBlendMask,
                            _LayerMaskMap_ST.xy, _LayerMaskMap_ST.zw, float2(0.0, 0.0), float2(0.0, 0.0), 1.0, false,
                            positionWS, _TexWorldScaleBlendMask,
                            mappingType, layerTexCoord);

    layerTexCoord.blendMask = layerTexCoord.base0;

    // On all layers (but not on blend mask) we can scale the tiling with object scale (only uniform supported)
    // Note: the object scale doesn't affect planar/triplanar mapping as they already handle the object scale.
    float tileObjectScale = 1.0;
#ifdef _LAYER_TILING_COUPLED_WITH_UNIFORM_OBJECT_SCALE
    // Extract scaling from world transform
    float4x4 worldTransform = GetObjectToWorldMatrix();
    // assuming uniform scaling, take only the first column
    tileObjectScale = length(float3(worldTransform._m00, worldTransform._m01, worldTransform._m02));
#endif

    mappingType = UV_MAPPING_UVSET;
#if defined(_LAYER_MAPPING_PLANAR0)
    mappingType = UV_MAPPING_PLANAR;
#elif defined(_LAYER_MAPPING_TRIPLANAR0)
    mappingType = UV_MAPPING_TRIPLANAR;
#endif

    ComputeLayerTexCoord0(  texCoord0, texCoord1, texCoord2, texCoord3, _UVMappingMask0, _UVDetailsMappingMask0,
                            _BaseColorMap0_ST.xy, _BaseColorMap0_ST.zw, _DetailMap0_ST.xy, _DetailMap0_ST.zw, 1.0
                            #if !defined(_MAIN_LAYER_INFLUENCE_MODE)
                            * tileObjectScale  // We only affect layer0 in case we are not in influence mode (i.e we should not change the base object)
                            #endif
                            , _LinkDetailsWithBase0
                            , positionWS, _TexWorldScale0,
                            mappingType, layerTexCoord);

    mappingType = UV_MAPPING_UVSET;
#if defined(_LAYER_MAPPING_PLANAR1)
    mappingType = UV_MAPPING_PLANAR;
#elif defined(_LAYER_MAPPING_TRIPLANAR1)
    mappingType = UV_MAPPING_TRIPLANAR;
#endif
    ComputeLayerTexCoord1(  texCoord0, texCoord1, texCoord2, texCoord3, _UVMappingMask1, _UVDetailsMappingMask1,
                            _BaseColorMap1_ST.xy, _BaseColorMap1_ST.zw, _DetailMap1_ST.xy, _DetailMap1_ST.zw, tileObjectScale, _LinkDetailsWithBase1,
                            positionWS, _TexWorldScale1,
                            mappingType, layerTexCoord);

    mappingType = UV_MAPPING_UVSET;
#if defined(_LAYER_MAPPING_PLANAR2)
    mappingType = UV_MAPPING_PLANAR;
#elif defined(_LAYER_MAPPING_TRIPLANAR2)
    mappingType = UV_MAPPING_TRIPLANAR;
#endif
    ComputeLayerTexCoord2(  texCoord0, texCoord1, texCoord2, texCoord3, _UVMappingMask2, _UVDetailsMappingMask2,
                            _BaseColorMap2_ST.xy, _BaseColorMap2_ST.zw, _DetailMap2_ST.xy, _DetailMap2_ST.zw, tileObjectScale, _LinkDetailsWithBase2,
                            positionWS, _TexWorldScale2,
                            mappingType, layerTexCoord);

    mappingType = UV_MAPPING_UVSET;
#if defined(_LAYER_MAPPING_PLANAR3)
    mappingType = UV_MAPPING_PLANAR;
#elif defined(_LAYER_MAPPING_TRIPLANAR3)
    mappingType = UV_MAPPING_TRIPLANAR;
#endif
    ComputeLayerTexCoord3(  texCoord0, texCoord1, texCoord2, texCoord3, _UVMappingMask3, _UVDetailsMappingMask3,
                            _BaseColorMap3_ST.xy, _BaseColorMap3_ST.zw, _DetailMap3_ST.xy, _DetailMap3_ST.zw, tileObjectScale, _LinkDetailsWithBase3,
                            positionWS, _TexWorldScale3,
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

void ApplyDisplacementTileScale(inout float height0, inout float height1, inout float height2, inout float height3)
{
    // When we change the tiling, we have want to conserve the ratio with the displacement (and this is consistent with per pixel displacement)
#ifdef _DISPLACEMENT_LOCK_TILING_SCALE
    float tileObjectScale = 1.0;
    #ifdef _LAYER_TILING_COUPLED_WITH_UNIFORM_OBJECT_SCALE
    // Extract scaling from world transform
    float4x4 worldTransform = GetObjectToWorldMatrix();
    // assuming uniform scaling, take only the first column
    tileObjectScale = length(float3(worldTransform._m00, worldTransform._m01, worldTransform._m02));
    #endif

    // TODO: precompute all these scaling factors!
    height0 *= _InvTilingScale0;
    #if !defined(_MAIN_LAYER_INFLUENCE_MODE)
    height0 /= tileObjectScale;  // We only affect layer0 in case we are not in influence mode (i.e we should not change the base object)
    #endif
    height1 = (height1 / tileObjectScale) * _InvTilingScale1;
    height2 = (height2 / tileObjectScale) * _InvTilingScale2;
    height3 = (height3 / tileObjectScale) * _InvTilingScale3;
#endif
}

// This function is just syntaxic sugar to nullify height not used based on heightmap avaibility and layer
void SetEnabledHeightByLayer(inout float height0, inout float height1, inout float height2, inout float height3)
{
#ifndef _HEIGHTMAP0
    height0 = 0.0;
#endif
#ifndef _HEIGHTMAP1
    height1 = 0.0;
#endif
#ifndef _HEIGHTMAP2
    height2 = 0.0;
#endif
#ifndef _HEIGHTMAP3
    height3 = 0.0;
#endif

#if _LAYER_COUNT < 4
    height3 = 0.0;
#endif
#if _LAYER_COUNT < 3
    height2 = 0.0;
#endif
}

void ComputeMaskWeights(float4 inputMasks, out float outWeights[_MAX_LAYER])
{
    float masks[_MAX_LAYER];
    masks[0] = inputMasks.a;

    masks[1] = inputMasks.r;
#if _LAYER_COUNT > 2
    masks[2] = inputMasks.g;
#else
    masks[2] = 0.0;
#endif
#if _LAYER_COUNT > 3
    masks[3] = inputMasks.b;
#else
    masks[3] = 0.0;
#endif

    // calculate weight of each layers
    // Algorithm is like this:
    // Top layer have priority on others layers
    // If a top layer doesn't use the full weight, the remaining can be use by the following layer.
    float weightsSum = 0.0;

    [unroll]
    for (int i = _LAYER_COUNT - 1; i >= 0; --i)
    {
        outWeights[i] = min(masks[i], (1.0 - weightsSum));
        weightsSum = saturate(weightsSum + masks[i]);
    }
}

// Caution: Blend mask are Layer 1 R - Layer 2 G - Layer 3 B - Main Layer A
float4 GetBlendMask(LayerTexCoord layerTexCoord, float4 vertexColor, bool useLodSampling = false, float lod = 0)
{
    // Caution:
    // Blend mask are Main Layer A - Layer 1 R - Layer 2 G - Layer 3 B
    // Value for main layer is not use for blending itself but for alternate weighting like density.
    // Settings this specific Main layer blend mask in alpha allow to be transparent in case we don't use it and 1 is provide by default.
    float4 blendMasks = useLodSampling ? SAMPLE_UVMAPPING_TEXTURE2D_LOD(_LayerMaskMap, sampler_LayerMaskMap, layerTexCoord.blendMask, lod) : SAMPLE_UVMAPPING_TEXTURE2D(_LayerMaskMap, sampler_LayerMaskMap, layerTexCoord.blendMask);

#if defined(_LAYER_MASK_VERTEX_COLOR_MUL)
    blendMasks *= vertexColor;
#elif defined(_LAYER_MASK_VERTEX_COLOR_ADD)
    blendMasks = saturate(blendMasks + vertexColor * 2.0 - 1.0);
#endif

    return blendMasks;
}

float GetInfluenceMask(LayerTexCoord layerTexCoord, bool useLodSampling = false, float lod = 0)
{
    // Sample influence mask with same mapping as Main layer
    return useLodSampling ? SAMPLE_UVMAPPING_TEXTURE2D_LOD(_LayerInfluenceMaskMap, sampler_LayerInfluenceMaskMap, layerTexCoord.base0, lod).r : SAMPLE_UVMAPPING_TEXTURE2D(_LayerInfluenceMaskMap, sampler_LayerInfluenceMaskMap, layerTexCoord.base0).r;
}

float GetMaxHeight(float4 heights)
{
    float maxHeight = max(heights.r, heights.g);
#ifdef _LAYEREDLIT_4_LAYERS
    maxHeight = max(Max3(heights.r, heights.g, heights.b), heights.a);
#endif
#ifdef _LAYEREDLIT_3_LAYERS
    maxHeight = Max3(heights.r, heights.g, heights.b);
#endif

    return maxHeight;
}

// Returns layering blend mask after application of height based blend.
float4 ApplyHeightBlend(float4 heights, float4 blendMask)
{
    // Add offsets for all the layers.
    heights = heights + float4(_HeightOffset0, _HeightOffset1, _HeightOffset2, _HeightOffset3);

    // We need to mask out inactive layers so that their height does not impact the result.
    float4 maskedHeights = heights * blendMask.argb;

    float maxHeight = GetMaxHeight(maskedHeights);
    // Make sure that transition is not zero otherwise the next computation will be wrong.
    // The epsilon here also has to be bigger than the epsilon in the next computation.
    float transition = max(_HeightTransition, 1e-5);

    // The goal here is to have all but the highest layer at negative heights, then we add the transition so that if the next highest layer is near transition it will have a positive value.
    // Then we clamp this to zero and normalize everything so that highest layer has a value of 1.
    maskedHeights = maskedHeights - maxHeight.xxxx;
    // We need to add an epsilon here for active layers (hence the blendMask again) so that at least a layer shows up if everything's too low.
    maskedHeights = (max(0, maskedHeights + transition) + 1e-6) * blendMask.argb;

    // Normalize
    maxHeight = GetMaxHeight(maskedHeights);
    maskedHeights = maskedHeights / maxHeight.xxxx;

    return maskedHeights.yzwx;
}

// Return the maximun amplitude use by all enabled heightmap
// use for tessellation culling and per pixel displacement
// TODO: For vertex displacement this should take into account the modification in ApplyTessellationTileScale but it should be conservative here (as long as tiling is not negative)
float GetMaxDisplacement()
{
    float maxDisplacement = 0.0;

    // _HeightAmplitudeX can be negative if min and max are inverted, but the max displacement must be positive, take abs()
#if defined(_HEIGHTMAP0)
    maxDisplacement = abs(_HeightAmplitude0);
#endif

#if defined(_HEIGHTMAP1)
    maxDisplacement = max(  abs(_HeightAmplitude1)
                            #if defined(_MAIN_LAYER_INFLUENCE_MODE)
                            + abs(_HeightAmplitude0) * _InheritBaseHeight1
                            #endif
                            , maxDisplacement);
#endif

#if _LAYER_COUNT >= 3
#if defined(_HEIGHTMAP2)
    maxDisplacement = max(  abs(_HeightAmplitude2)
                            #if defined(_MAIN_LAYER_INFLUENCE_MODE)
                            + abs(_HeightAmplitude0) * _InheritBaseHeight2
                            #endif
                            , maxDisplacement);
#endif
#endif

#if _LAYER_COUNT >= 4
#if defined(_HEIGHTMAP3)
    maxDisplacement = max(  abs(_HeightAmplitude3)
                            #if defined(_MAIN_LAYER_INFLUENCE_MODE)
                            + abs(_HeightAmplitude0) * _InheritBaseHeight3
                            #endif
                            , maxDisplacement);
#endif
#endif

    return maxDisplacement;
}

// Return the minimun uv size for all layers including triplanar
float2 GetMinUvSize(LayerTexCoord layerTexCoord)
{
    float2 minUvSize = float2(FLT_MAX, FLT_MAX);

#if defined(_HEIGHTMAP0)
    if (layerTexCoord.base0.mappingType == UV_MAPPING_TRIPLANAR)
    {
        minUvSize = min(layerTexCoord.base0.uvZY * _HeightMap0_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base0.uvXZ * _HeightMap0_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base0.uvXY * _HeightMap0_TexelSize.zw, minUvSize);
    }
    else
    {
        minUvSize = min(layerTexCoord.base0.uv * _HeightMap0_TexelSize.zw, minUvSize);
    }
#endif

#if defined(_HEIGHTMAP1)
    if (layerTexCoord.base1.mappingType == UV_MAPPING_TRIPLANAR)
    {
        minUvSize = min(layerTexCoord.base1.uvZY * _HeightMap1_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base1.uvXZ * _HeightMap1_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base1.uvXY * _HeightMap1_TexelSize.zw, minUvSize);
    }
    else
    {
        minUvSize = min(layerTexCoord.base1.uv * _HeightMap1_TexelSize.zw, minUvSize);
    }
#endif

#if _LAYER_COUNT >= 3
#if defined(_HEIGHTMAP2)
    if (layerTexCoord.base2.mappingType == UV_MAPPING_TRIPLANAR)
    {
        minUvSize = min(layerTexCoord.base2.uvZY * _HeightMap2_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base2.uvXZ * _HeightMap2_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base2.uvXY * _HeightMap2_TexelSize.zw, minUvSize);
    }
    else
    {
        minUvSize = min(layerTexCoord.base2.uv * _HeightMap2_TexelSize.zw, minUvSize);
    }
#endif
#endif

#if _LAYER_COUNT >= 4
#if defined(_HEIGHTMAP3)
    if (layerTexCoord.base3.mappingType == UV_MAPPING_TRIPLANAR)
    {
        minUvSize = min(layerTexCoord.base3.uvZY * _HeightMap3_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base3.uvXZ * _HeightMap3_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base3.uvXY * _HeightMap3_TexelSize.zw, minUvSize);
    }
    else
    {
        minUvSize = min(layerTexCoord.base3.uv * _HeightMap3_TexelSize.zw, minUvSize);
    }
#endif
#endif

    return minUvSize;
}

#if defined(_PIXEL_DISPLACEMENT) && LAYERS_HEIGHTMAP_ENABLE
struct PerPixelHeightDisplacementParam
{
    float4 blendMasks;
    float2 uv[_MAX_LAYER];
    float2 uvSpaceScale[_MAX_LAYER];
#if defined(_MAIN_LAYER_INFLUENCE_MODE) && defined(_HEIGHTMAP0)
    float heightInfluence[_MAX_LAYER];
#endif
};

// Calculate displacement for per vertex displacement mapping
float ComputePerPixelHeightDisplacement(float2 texOffsetCurrent, float lod, PerPixelHeightDisplacementParam param)
{
    // See function ComputePerVertexDisplacement() for comment about the weights/influenceMask/BlendMask

    // Note: Amplitude is handled in uvSpaceScale, no need to multiply by it here.
    float height0 = SAMPLE_TEXTURE2D_LOD(_HeightMap0, SAMPLER_HEIGHTMAP_IDX, param.uv[0] + texOffsetCurrent * param.uvSpaceScale[0], lod).r;
    float height1 = SAMPLE_TEXTURE2D_LOD(_HeightMap1, SAMPLER_HEIGHTMAP_IDX, param.uv[1] + texOffsetCurrent * param.uvSpaceScale[1], lod).r;
    float height2 = SAMPLE_TEXTURE2D_LOD(_HeightMap2, SAMPLER_HEIGHTMAP_IDX, param.uv[2] + texOffsetCurrent * param.uvSpaceScale[2], lod).r;
    float height3 = SAMPLE_TEXTURE2D_LOD(_HeightMap3, SAMPLER_HEIGHTMAP_IDX, param.uv[3] + texOffsetCurrent * param.uvSpaceScale[3], lod).r;

    SetEnabledHeightByLayer(height0, height1, height2, height3);

    float4 blendMasks = param.blendMasks;
#if defined(_HEIGHT_BASED_BLEND)
    // Modify blendMask to take into account the height of the layer. Higher height should be more visible.
    blendMasks = ApplyHeightBlend(float4(height0, height1, height2, height3), param.blendMasks);
#endif

    float weights[_MAX_LAYER];
    ComputeMaskWeights(blendMasks, weights);

#if defined(_MAIN_LAYER_INFLUENCE_MODE) && defined(_HEIGHTMAP0)
    float influenceMask = blendMasks.a;
    #ifdef _INFLUENCEMASK_MAP
    influenceMask *= SAMPLE_TEXTURE2D_LOD(_LayerInfluenceMaskMap, sampler_BaseColorMap0, param.uv[0], lod).r;
    #endif
    height1 += height0 * _InheritBaseHeight1 * influenceMask;
    height2 += height0 * _InheritBaseHeight2 * influenceMask;
    height3 += height0 * _InheritBaseHeight3 * influenceMask;
#endif

    return BlendLayeredScalar(height0, height1, height2, height3, weights);
}

#include "../../../Core/ShaderLibrary/PerPixelDisplacement.hlsl"

#endif // defined(_PIXEL_DISPLACEMENT) && LAYERS_HEIGHTMAP_ENABLE

// PPD is affecting only one mapping at the same time, mean we need to execute it for each mapping (UV0, UV1, 3 times for triplanar etc..)
// We chose to not support all this case that are extremely hard to manage (for example mixing different mapping, mean it also require different tangent space that is not supported in Unity)
// For these reasons we put the following rules
// Rules:
// - Mapping is the same for all layers that use an Heightmap (i.e all are UV, planar or triplanar)
// - Mapping UV is UV0 only because we need to convert view vector in texture space and this is only available for UV0
// - Heightmap can be enabled per layer
// - Blend Mask use same mapping as main layer (UVO, Planar, Triplanar)
// From these rules it mean that PPD is enable only if the user 1) ask for it, 2) if there is one heightmap enabled on active layer, 3) if mapping is the same for all layer respecting 2), 4) if mapping is UV0, planar or triplanar mapping
// Most contraint are handled by the inspector (i.e the UI) like the mapping constraint and is assumed in the shader.
float ApplyPerPixelDisplacement(FragInputs input, float3 V, inout LayerTexCoord layerTexCoord, float4 blendMasks)
{
#if defined(_PIXEL_DISPLACEMENT) && LAYERS_HEIGHTMAP_ENABLE
    bool isPlanar = false;
    bool isTriplanar = false;

    // To know if we are planar or triplanar just need to check if any of the active heightmap layer is true as they are enforce to be the same mapping
#if defined(_HEIGHTMAP0)
    isPlanar = layerTexCoord.base0.mappingType == UV_MAPPING_PLANAR;
    isTriplanar = layerTexCoord.base0.mappingType == UV_MAPPING_TRIPLANAR;
#endif

#if defined(_HEIGHTMAP1)
    isPlanar = layerTexCoord.base1.mappingType == UV_MAPPING_PLANAR;
    isTriplanar = layerTexCoord.base1.mappingType == UV_MAPPING_TRIPLANAR;
#endif

#if _LAYER_COUNT >= 3
#if defined(_HEIGHTMAP2)
    isPlanar = layerTexCoord.base2.mappingType == UV_MAPPING_PLANAR;
    isTriplanar = layerTexCoord.base2.mappingType == UV_MAPPING_TRIPLANAR;
#endif
#endif

#if _LAYER_COUNT >= 4
#if defined(_HEIGHTMAP3)
    isPlanar = layerTexCoord.base3.mappingType == UV_MAPPING_PLANAR;
    isTriplanar = layerTexCoord.base3.mappingType == UV_MAPPING_TRIPLANAR;
#endif
#endif

    // Compute lod as we will sample inside a loop(so can't use regular sampling)
    // Note: It appear that CALCULATE_TEXTURE2D_LOD only return interger lod. We want to use float lod to have smoother transition and fading, so do our own calculation.
    // Approximation of lod to used. Be conservative here, we will take the highest mip of all layers.
    // Remember, we assume that we used the same mapping for all layer, so only size matter.
    float2 minUvSize = GetMinUvSize(layerTexCoord);
    float lod = ComputeTextureLOD(minUvSize);

    // TODO: Here we calculate the scale transform from world to UV space , which is what we have done in GetLayerTexCoord but without the texBias.
    // Mean we must also apply the same "additionalTiling", currently not apply Also precompute all this!
    float  maxHeight0 = abs(_HeightAmplitude0);
    float  maxHeight1 = abs(_HeightAmplitude1);
    float  maxHeight2 = abs(_HeightAmplitude2);
    float  maxHeight3 = abs(_HeightAmplitude3);

    ApplyDisplacementTileScale(maxHeight0, maxHeight1, maxHeight2, maxHeight3);
#if defined(_MAIN_LAYER_INFLUENCE_MODE) && defined(_HEIGHTMAP0)
    maxHeight1 += abs(_HeightAmplitude0) * _InheritBaseHeight1;
    maxHeight2 += abs(_HeightAmplitude0) * _InheritBaseHeight2;
    maxHeight3 += abs(_HeightAmplitude0) * _InheritBaseHeight3;
#endif

    float weights[_MAX_LAYER];
    ComputeMaskWeights(blendMasks, weights);
    float maxHeight = BlendLayeredScalar(maxHeight0, maxHeight1, maxHeight2, maxHeight3, weights);

    float2 worldScale0 = (isPlanar || isTriplanar) ? _TexWorldScale0.xx : _InvPrimScale.xy;
    float2 worldScale1 = (isPlanar || isTriplanar) ? _TexWorldScale1.xx : _InvPrimScale.xy;
    float2 worldScale2 = (isPlanar || isTriplanar) ? _TexWorldScale2.xx : _InvPrimScale.xy;
    float2 worldScale3 = (isPlanar || isTriplanar) ? _TexWorldScale3.xx : _InvPrimScale.xy;

    PerPixelHeightDisplacementParam ppdParam;
    ppdParam.blendMasks = blendMasks;
    ppdParam.uvSpaceScale[0] = _BaseColorMap0_ST.xy * worldScale0;// *maxHeight0;
    ppdParam.uvSpaceScale[1] = _BaseColorMap1_ST.xy * worldScale1;// *maxHeight1;
    ppdParam.uvSpaceScale[2] = _BaseColorMap2_ST.xy * worldScale2;// *maxHeight2;
    ppdParam.uvSpaceScale[3] = _BaseColorMap3_ST.xy * worldScale3;// *maxHeight3;

    float uvSpaceScale = BlendLayeredScalar(ppdParam.uvSpaceScale[0], ppdParam.uvSpaceScale[1], ppdParam.uvSpaceScale[2], ppdParam.uvSpaceScale[3], weights);

    float2 scaleOffsetDetails0 =_DetailMap0_ST.xy;
    float2 scaleOffsetDetails1 =_DetailMap1_ST.xy;
    float2 scaleOffsetDetails2 =_DetailMap2_ST.xy;
    float2 scaleOffsetDetails3 =_DetailMap3_ST.xy;

    float height; // final height processed
    float NdotV;

    // planar/triplanar
    float2 uvXZ;
    float2 uvXY;
    float2 uvZY;
    GetTriplanarCoordinate(V, uvXZ, uvXY, uvZY);

    // We need to calculate the texture space direction. It depends on the mapping.
    if (isTriplanar)
    {
        // This is not supported currently
        height = 1.0;
        NdotV  = 1.0;
    }
    else
    {
        // For planar it is uv too, not uvXZ
        ppdParam.uv[0] = layerTexCoord.base0.uv;
        ppdParam.uv[1] = layerTexCoord.base1.uv;
        ppdParam.uv[2] = layerTexCoord.base2.uv;
        ppdParam.uv[3] = layerTexCoord.base3.uv;

        // Note: The TBN is not normalize as it is based on mikkt. We should normalize it, but POM is always use on simple enough surface that mean it is not required (save 2 normalize). Tag: SURFACE_GRADIENT
        // Note: worldToTangent is only define for UVSet0, so we expect that layer that use POM have UVSet0
        float3 viewDirTS = isPlanar ? float3(uvXZ, V.y) : TransformWorldToTangent(V, input.worldToTangent) * GetDisplacementObjectScale(false).xzy; // Switch from Y-up to Z-up (as we move to tangent space)
        NdotV = viewDirTS.z;

        // Transform the view vector into the UV space.
        float3 viewDirUV = normalize(float3(viewDirTS.xy * maxHeight, viewDirTS.z));
        float  unitAngle = saturate(FastACosPos(viewDirUV.z) * INV_HALF_PI);            // TODO: optimize
        int    numSteps = (int)lerp(_PPDMinSamples, _PPDMaxSamples, unitAngle);
        float2 offset = ParallaxOcclusionMapping(lod, _PPDLodThreshold, numSteps, viewDirUV, ppdParam, height);
        offset *= uvSpaceScale;

        layerTexCoord.base0.uv += offset;
        layerTexCoord.base1.uv += offset;
        layerTexCoord.base2.uv += offset;
        layerTexCoord.base3.uv += offset;

        layerTexCoord.details0.uv += offset * scaleOffsetDetails0;
        layerTexCoord.details1.uv += offset * scaleOffsetDetails1;
        layerTexCoord.details2.uv += offset * scaleOffsetDetails2;
        layerTexCoord.details3.uv += offset * scaleOffsetDetails3;
    }

    // Since POM "pushes" geometry inwards (rather than extrude it), { height = height - 1 }.
    // Since the result is used as a 'depthOffsetVS', it needs to be positive, so we flip the sign. { height = -height + 1 }.

    float verticalDisplacement = maxHeight - height * maxHeight;
    return verticalDisplacement / max(NdotV, 0.001);
#else
    return 0.0;
#endif
}

// Calculate displacement for per vertex displacement mapping
float3 ComputePerVertexDisplacement(LayerTexCoord layerTexCoord, float4 vertexColor, float lod)
{
#if LAYERS_HEIGHTMAP_ENABLE
    float height0 = (SAMPLE_UVMAPPING_TEXTURE2D_LOD(_HeightMap0, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base0, lod).r - _HeightCenter0) * _HeightAmplitude0;
    float height1 = (SAMPLE_UVMAPPING_TEXTURE2D_LOD(_HeightMap1, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base1, lod).r - _HeightCenter1) * _HeightAmplitude1;
    float height2 = (SAMPLE_UVMAPPING_TEXTURE2D_LOD(_HeightMap2, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base2, lod).r - _HeightCenter2) * _HeightAmplitude2;
    float height3 = (SAMPLE_UVMAPPING_TEXTURE2D_LOD(_HeightMap3, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base3, lod).r - _HeightCenter3) * _HeightAmplitude3;
    // Height is affected by tiling property and by object scale (depends on option).
    // Apply scaling from tiling properties (TexWorldScale and tiling from BaseColor)
    ApplyDisplacementTileScale(height0, height1, height2, height3);
    // Nullify height that are not used, so compiler can remove unused case
    SetEnabledHeightByLayer(height0, height1, height2, height3);

    float4 blendMasks = GetBlendMask(layerTexCoord, vertexColor, true, lod);

    #if defined(_HEIGHT_BASED_BLEND)
    // Modify blendMask to take into account the height of the layer. Higher height should be more visible.
    blendMasks = ApplyHeightBlend(float4(height0, height1, height2, height3), blendMasks);
    #endif

    float weights[_MAX_LAYER];
    ComputeMaskWeights(blendMasks, weights);

    // _MAIN_LAYER_INFLUENCE_MODE is a pure visual mode that doesn't contribute to the weights of a layer
    // The motivation is like this: if a layer is visible, then we will apply influence on top of it (so it is only visual).
    // This is what is done for normal and baseColor and we do the same for height.
    // Note that if we apply influence before ApplyHeightBlend, then have a different behavior.
#if defined(_MAIN_LAYER_INFLUENCE_MODE) && defined(_HEIGHTMAP0)
    // Add main layer influence if any (simply add main layer add on other layer)
    // We multiply by the input mask for the first layer (blendMask.a) because if the mask here is black it means that the layer
    // is not actually underneath any visible layer so we don't want to inherit its height.
    float influenceMask = blendMasks.a;
    #ifdef _INFLUENCEMASK_MAP
    influenceMask *= GetInfluenceMask(layerTexCoord, true, lod);
    #endif
    height1 += height0 * _InheritBaseHeight1 * influenceMask;
    height2 += height0 * _InheritBaseHeight2 * influenceMask;
    height3 += height0 * _InheritBaseHeight3 * influenceMask;
#endif

    float heightResult = BlendLayeredScalar(height0, height1, height2, height3, weights).xxx;

   // Applying scaling of the object if requested
    #ifdef _VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE
    float3 objectScale = GetDisplacementObjectScale(true);
    // Reminder: mappingType is know statically, so code below is optimize by the compiler
    // Planar and Triplanar are in world space thus it is independent of object scale
    return heightResult.xxx * BlendLayeredVector3( ((layerTexCoord.base0.mappingType == UV_MAPPING_UVSET) ? objectScale : float3(1.0, 1.0, 1.0)),
                                                   ((layerTexCoord.base1.mappingType == UV_MAPPING_UVSET) ? objectScale : float3(1.0, 1.0, 1.0)),
                                                   ((layerTexCoord.base2.mappingType == UV_MAPPING_UVSET) ? objectScale : float3(1.0, 1.0, 1.0)),
                                                   ((layerTexCoord.base3.mappingType == UV_MAPPING_UVSET) ? objectScale : float3(1.0, 1.0, 1.0)), weights);
    #else
    return heightResult.xxx;
    #endif
#else
    return float3(0.0, 0.0, 0.0);
#endif
}

// Calculate weights to apply to each layer
// Caution: This function must not be use for per vertex/pixel displacement, there is a dedicated function for them.
// This function handle triplanar
void ComputeLayerWeights(FragInputs input, LayerTexCoord layerTexCoord, float4 inputAlphaMask, float4 blendMasks, out float outWeights[_MAX_LAYER])
{
    for (int i = 0; i < _MAX_LAYER; ++i)
    {
        outWeights[i] = 0.0f;
    }

#if defined(_DENSITY_MODE)
    // Note: blendMasks.argb because a is main layer
    float4 opacityAsDensity = saturate((inputAlphaMask - (float4(1.0, 1.0, 1.0, 1.0) - blendMasks.argb)) * 20.0); // 20.0 is the number of steps in inputAlphaMask (Density mask. We decided 20 empirically)
    float4 useOpacityAsDensityParam = float4(_OpacityAsDensity0, _OpacityAsDensity1, _OpacityAsDensity2, _OpacityAsDensity3);
    blendMasks.argb = lerp(blendMasks.argb, opacityAsDensity, useOpacityAsDensityParam);
#endif

#if LAYERS_HEIGHTMAP_ENABLE
    float height0 = (SAMPLE_UVMAPPING_TEXTURE2D(_HeightMap0, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base0).r - _HeightCenter0) * _HeightAmplitude0;
    float height1 = (SAMPLE_UVMAPPING_TEXTURE2D(_HeightMap1, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base1).r - _HeightCenter1) * _HeightAmplitude1;
    float height2 = (SAMPLE_UVMAPPING_TEXTURE2D(_HeightMap2, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base2).r - _HeightCenter2) * _HeightAmplitude2;
    float height3 = (SAMPLE_UVMAPPING_TEXTURE2D(_HeightMap3, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base3).r - _HeightCenter3) * _HeightAmplitude3;
    // Height is affected by tiling property and by object scale (depends on option).
    // Apply scaling from tiling properties (TexWorldScale and tiling from BaseColor)
    ApplyDisplacementTileScale(height0, height1, height2, height3);
    // Nullify height that are not used, so compiler can remove unused case
    SetEnabledHeightByLayer(height0, height1, height2, height3);

    // Reminder: _MAIN_LAYER_INFLUENCE_MODE is a purely visual mode, it is not take into account for the blendMasks
    // As it is purely visual, it is not apply in ComputeLayerWeights

    #if defined(_HEIGHT_BASED_BLEND)
    // Modify blendMask to take into account the height of the layer. Higher height should be more visible.
    blendMasks = ApplyHeightBlend(float4(height0, height1, height2, height3), blendMasks);
    #endif
#endif

    ComputeMaskWeights(blendMasks, outWeights);
}

float3 ComputeMainNormalInfluence(float influenceMask, FragInputs input, float3 normalTS0, float3 normalTS1, float3 normalTS2, float3 normalTS3, LayerTexCoord layerTexCoord, float inputMainLayerMask, float weights[_MAX_LAYER])
{
    // Get our regular normal from regular layering
    float3 normalTS = BlendLayeredVector3(normalTS0, normalTS1, normalTS2, normalTS3, weights);

    // THen get Main Layer Normal influence factor. Main layer is 0 because it can't be influence. In this case the final lerp return normalTS.
    float influenceFactor = BlendLayeredScalar(0.0, _InheritBaseNormal1, _InheritBaseNormal2, _InheritBaseNormal3, weights) * influenceMask;
    // We will add smoothly the contribution of the normal map by lerping between vertex normal ( (0,0,1) in tangent space) and the actual normal from the main layer depending on the influence factor.
    // Note: that we don't take details map into account here.
    #ifdef SURFACE_GRADIENT
    float3 neutralNormalTS = float3(0.0, 0.0, 0.0);
    #else
    float3 neutralNormalTS = float3(0.0, 0.0, 1.0);
    #endif
    float3 mainNormalTS = lerp(neutralNormalTS, normalTS0, influenceFactor);

    // Add on our regular normal a bit of Main Layer normal base on influence factor. Note that this affect only the "visible" normal.
    #ifdef SURFACE_GRADIENT
    return normalTS + influenceFactor * mainNormalTS * inputMainLayerMask;
    #else
    return lerp(normalTS, BlendNormalRNM(normalTS, mainNormalTS), influenceFactor * inputMainLayerMask); // Multiply by inputMainLayerMask in order to avoid influence where main layer should never be present
    #endif
}

float3 ComputeMainBaseColorInfluence(float influenceMask, float3 baseColor0, float3 baseColor1, float3 baseColor2, float3 baseColor3, LayerTexCoord layerTexCoord, float inputMainLayerMask, float weights[_MAX_LAYER])
{
    float3 baseColor = BlendLayeredVector3(baseColor0, baseColor1, baseColor2, baseColor3, weights);

    float influenceFactor = BlendLayeredScalar(0.0, _InheritBaseColor1, _InheritBaseColor2, _InheritBaseColor3, weights) * influenceMask * inputMainLayerMask; // Multiply by inputMainLayerMask in order to avoid influence where main layer should never be present

    // We want to calculate the mean color of the texture. For this we will sample a low mipmap
    float textureBias = 15.0; // Use maximum bias
    float3 baseMeanColor0 = SAMPLE_UVMAPPING_TEXTURE2D_BIAS(_BaseColorMap0, sampler_BaseColorMap0, layerTexCoord.base0, textureBias).rgb *_BaseColor0.rgb;
    float3 baseMeanColor1 = SAMPLE_UVMAPPING_TEXTURE2D_BIAS(_BaseColorMap1, sampler_BaseColorMap0, layerTexCoord.base1, textureBias).rgb *_BaseColor1.rgb;
    float3 baseMeanColor2 = SAMPLE_UVMAPPING_TEXTURE2D_BIAS(_BaseColorMap2, sampler_BaseColorMap0, layerTexCoord.base2, textureBias).rgb *_BaseColor2.rgb;
    float3 baseMeanColor3 = SAMPLE_UVMAPPING_TEXTURE2D_BIAS(_BaseColorMap3, sampler_BaseColorMap0, layerTexCoord.base3, textureBias).rgb *_BaseColor3.rgb;

    float3 meanColor = BlendLayeredVector3(baseMeanColor0, baseMeanColor1, baseMeanColor2, baseMeanColor3, weights);

    // If we inherit from base layer, we will add a bit of it
    // We add variance of current visible level and the base color 0 or mean (to retrieve initial color) depends on influence
    // (baseColor - meanColor) + lerp(meanColor, baseColor0, inheritBaseColor) simplify to
    // saturate(influenceFactor * (baseColor0 - meanColor) + baseColor);
    // There is a special case when baseColor < meanColor to avoid getting negative values.
    float3 factor = baseColor > meanColor ? (baseColor0 - meanColor) : (baseColor0 * baseColor / max(meanColor, 0.001) - baseColor); // max(to avoid divide by 0)
    return influenceFactor * factor + baseColor;
}

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
#ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
    LODDitheringTransition(posInput.unPositionSS, unity_LODFade.x);
#endif

    ApplyDoubleSidedFlipOrMirror(input); // Apply double sided flip on the vertex normal

    LayerTexCoord layerTexCoord;
    ZERO_INITIALIZE(LayerTexCoord, layerTexCoord);
    GetLayerTexCoord(input, layerTexCoord);

    float4 blendMasks = GetBlendMask(layerTexCoord, input.color);
    float depthOffset = ApplyPerPixelDisplacement(input, V, layerTexCoord, blendMasks);

#ifdef _DEPTHOFFSET_ON
    ApplyDepthOffsetPositionInput(V, depthOffset, GetWorldToHClipMatrix(), posInput);
#endif

    SurfaceData surfaceData0, surfaceData1, surfaceData2, surfaceData3;
    float3 normalTS0, normalTS1, normalTS2, normalTS3;
    float3 bentNormalTS0, bentNormalTS1, bentNormalTS2, bentNormalTS3;
    float alpha0 = GetSurfaceData0(input, layerTexCoord, surfaceData0, normalTS0, bentNormalTS0);
    float alpha1 = GetSurfaceData1(input, layerTexCoord, surfaceData1, normalTS1, bentNormalTS1);
    float alpha2 = GetSurfaceData2(input, layerTexCoord, surfaceData2, normalTS2, bentNormalTS2);
    float alpha3 = GetSurfaceData3(input, layerTexCoord, surfaceData3, normalTS3, bentNormalTS3);

    // Note: If per pixel displacement is enabled it mean we will fetch again the various heightmaps at the intersection location. Not sure the compiler can optimize.
    float weights[_MAX_LAYER];
    ComputeLayerWeights(input, layerTexCoord, float4(alpha0, alpha1, alpha2, alpha3), blendMasks, weights);

    // For layered shader, alpha of base color is used as either an opacity mask, a composition mask for inheritance parameters or a density mask.
    float alpha = PROP_BLEND_SCALAR(alpha, weights);

#ifdef _ALPHATEST_ON
    DoAlphaTest(alpha, _AlphaCutoff);
#endif

    float3 normalTS;
    float3 bentNormalTS;
    float3 bentNormalWS;
#if defined(_MAIN_LAYER_INFLUENCE_MODE)

    #ifdef _INFLUENCEMASK_MAP
    float influenceMask = GetInfluenceMask(layerTexCoord);
    #else
    float influenceMask = 0.0;
    #endif

    if (influenceMask > 0.0f)
    {
        surfaceData.baseColor = ComputeMainBaseColorInfluence(influenceMask, surfaceData0.baseColor, surfaceData1.baseColor, surfaceData2.baseColor, surfaceData3.baseColor, layerTexCoord, blendMasks.a, weights);
        normalTS = ComputeMainNormalInfluence(influenceMask, input, normalTS0, normalTS1, normalTS2, normalTS3, layerTexCoord, blendMasks.a, weights);
        bentNormalTS = ComputeMainNormalInfluence(influenceMask, input, bentNormalTS0, bentNormalTS1, bentNormalTS2, bentNormalTS3, layerTexCoord, blendMasks.a, weights);
    }
    else
#endif
    {
        surfaceData.baseColor = SURFACEDATA_BLEND_VECTOR3(surfaceData, baseColor, weights);
        normalTS = BlendLayeredVector3(normalTS0, normalTS1, normalTS2, normalTS3, weights);
        bentNormalTS = BlendLayeredVector3(bentNormalTS0, bentNormalTS1, bentNormalTS2, bentNormalTS3, weights);
    }

    surfaceData.perceptualSmoothness = SURFACEDATA_BLEND_SCALAR(surfaceData, perceptualSmoothness, weights);
    surfaceData.ambientOcclusion = SURFACEDATA_BLEND_SCALAR(surfaceData, ambientOcclusion, weights);
    surfaceData.metallic = SURFACEDATA_BLEND_SCALAR(surfaceData, metallic, weights);
    surfaceData.tangentWS = normalize(input.worldToTangent[0].xyz); // The tangent is not normalize in worldToTangent for mikkt. Tag: SURFACE_GRADIENT
    // Init other parameters
    surfaceData.materialId = 1; // MaterialId.LitStandard
    surfaceData.anisotropy = 0;
    surfaceData.subsurfaceRadius = 1.0;
    surfaceData.thickness = 0.0;
    surfaceData.subsurfaceProfile = 0;
    surfaceData.specularColor = float3(0.0, 0.0, 0.0);
    surfaceData.coatNormalWS = float3(0.0, 0.0, 0.0);
    surfaceData.coatCoverage = 0.0f;
    surfaceData.coatIOR = 0.5;

    // Transparency parameters
    // Use thickness from SSS
    surfaceData.ior = 1.0;
    surfaceData.transmittanceColor = float3(1.0, 1.0, 1.0);
    surfaceData.atDistance = 1000000.0;

    GetNormalWS(input, V, normalTS, surfaceData.normalWS);
    // Use bent normal to sample GI if available
    // If any layer use a bent normal map, then bentNormalTS contain the interpolated result of bentnormal and normalmap (in case no bent normal are available)
    // Note: the code in LitDataInternal ensure that we fallback on normal map for layer that have no bentnormal
    surfaceData.specularOcclusion = 1.0;
#if defined(_BENTNORMALMAP0) || defined(_BENTNORMALMAP1) || defined(_BENTNORMALMAP2) || defined(_BENTNORMALMAP3)
    GetNormalWS(input, V, bentNormalTS, bentNormalWS);
    #ifdef _ENABLESPECULAROCCLUSION
    // If we have bent normal and ambient occlusion, process a specular occlusion
    surfaceData.specularOcclusion = GetSpecularOcclusionFromBentAO(V, bentNormalWS, surfaceData);
    #endif
#else // if no bent normal are available at all just keep the calculation fully
    bentNormalWS = surfaceData.normalWS;
#endif

    GetBuiltinData(input, surfaceData, alpha, bentNormalWS, depthOffset, builtinData);
}

#endif // #ifndef LAYERED_LIT_SHADER

#include "LitTessellation.hlsl" // Must be after GetLayerTexCoord() declaration
