//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "CoreRP/ShaderLibrary/Sampling/SampleUVMapping.hlsl"
#include "../MaterialUtilities.hlsl"

//-----------------------------------------------------------------------------
// Texture Mapping (think of LayerTexCoord as simply TexCoordMappings, 
// ie no more layers here - cf Lit materials)
//-----------------------------------------------------------------------------

//
// For easier copying of code for now use a LayerTexCoord wrapping struct.
// We don't have details yet.
//
// NEWLITTODO: Eventually, we could quickly share GetBuiltinData of LitBuiltinData.hlsl 
// in our GetSurfaceAndBuiltinData( ) here, since we will use the LayerTexCoord identifier,
// and an identical ComputeLayerTexCoord( ) prototype
//
struct LayerTexCoord
{
    UVMapping base;
    UVMapping details;

    // Store information that will be share by all UVMapping
    float3 vertexNormalWS; // TODO: store also object normal map for object triplanar
};

// Want to use only one sampler for normalmap/bentnormalmap either we use OS or TS. And either we have normal map or bent normal or both.
// 
// Note (compared to Lit shader): 
//
// We don't have a layered material with which we are sharing code here like the LayeredLit shader, but we can also save a couple of 
// samplers later if we use bentnormals.
//
// _IDX suffix is meaningless here, could use the name SAMPLER_NORMALMAP_ID instead of SAMPLER_NORMALMAP_IDX and replace all 
// indirect #ifdef _NORMALMAP_TANGENT_SPACE_IDX #ifdef and _NORMALMAP_IDX tests with the more direct 
// shader_feature keywords _NORMALMAP_TANGENT_SPACE and _NORMALMAP.
//
// (Originally in the LayeredLit shader, shader_feature keywords like _NORMALMAP become _NORMALMAP0 but since files are shared,
// LitDataIndividualLayer will use a generic _NORMALMAP_IDX defined before its inclusion by the client LitData or LayeredLitData.
// That way, LitDataIndividualLayer supports multiple inclusions)
//
// 
#ifdef _NORMALMAP_TANGENT_SPACE
    #if defined(_NORMALMAP)
    #define SAMPLER_NORMALMAP_ID sampler_NormalMap
    // TODO:
    //#elif defined(_BENTNORMALMAP)
    //#define SAMPLER_NORMALMAP_ID sampler_BentNormalMap
    #endif
#else
    // TODO:
    //#error STACKLIT_USES_ONLY_TANGENT_SPACE_FOR_NOW
    //#if defined(_NORMALMAP)
    //#define SAMPLER_NORMALMAP_ID sampler_NormalMapOS
    //#elif defined(_BENTNORMALMAP)
    //#define SAMPLER_NORMALMAP_ID sampler_BentNormalMapOS
    //#endif
#endif

void ComputeLayerTexCoord( // Uv related parameters
                                    float2 texCoord0, float2 texCoord1, float2 texCoord2, float2 texCoord3, float4 uvMappingMask, 
                                    // scale and bias for base 
                                    float2 texScale, float2 texBias, 
                                    // mapping type and output
                                    int mappingType, inout LayerTexCoord layerTexCoord)
{

    //TODO: Planar, Triplanar, detail map, surface_gradient.

    // Handle uv0, uv1, uv2, uv3 based on _UVMappingMask weight (exclusif 0..1)
    float2 uvBase = uvMappingMask.x * texCoord0 +
                    uvMappingMask.y * texCoord1 +
                    uvMappingMask.z * texCoord2 +
                    uvMappingMask.w * texCoord3;
    
    // Copy data in uvmapping fields: used by generic sampling code (see especially SampleUVMappingNormalInternal.hlsl)
    layerTexCoord.base.mappingType = mappingType;
    layerTexCoord.base.normalWS = layerTexCoord.vertexNormalWS;

    // Apply tiling options
    layerTexCoord.base.uv = uvBase * texScale + texBias;
}



float3 GetNormalTS(FragInputs input, LayerTexCoord layerTexCoord, float3 detailNormalTS, float detailMask)
{
    // TODO: different spaces (eg #ifdef _NORMALMAP_TANGENT_SPACE #elif object space, SURFACE_GRADIENT, etc.)
    // and use detail map

    float3 normalTS;

    // Note we don't use the _NORMALMAP_IDX mechanism of the Lit shader, since we don't have "layers", we can 
    // directly use the shader_feature keyword:
#ifdef _NORMALMAP
        normalTS = SAMPLE_UVMAPPING_NORMALMAP(_NormalMap, SAMPLER_NORMALMAP_ID, layerTexCoord.base, _NormalScale);
#else
    normalTS = float3(0.0, 0.0, 1.0);
#endif

    return normalTS;
}

// This maybe call directly by tessellation (domain) shader, thus all part regarding surface gradient must be done
// in function with FragInputs input as parameters
// layerTexCoord must have been initialize to 0 outside of this function
void GetLayerTexCoord(float2 texCoord0, float2 texCoord1, float2 texCoord2, float2 texCoord3,
                      float3 positionWS, float3 vertexNormalWS, inout LayerTexCoord layerTexCoord)
{
    layerTexCoord.vertexNormalWS = vertexNormalWS;
    // TODO: 
    //layerTexCoord.triplanarWeights = ComputeTriplanarWeights(vertexNormalWS);

    int mappingType = UV_MAPPING_UVSET;

    //TODO: _MAPPING_PLANAR, _MAPPING_TRIPLANAR

    // Be sure that the compiler is aware that we don't use UV1 to UV3 for main layer so it can optimize code
    ComputeLayerTexCoord(   texCoord0, texCoord1, texCoord2, texCoord3, _UVMappingMask, /* TODO _UVDetailsMappingMask, */
                            _BaseColorMap_ST.xy, _BaseColorMap_ST.zw, /* TODO _DetailMap_ST.xy, _DetailMap_ST.zw, 1.0, _LinkDetailsWithBase,
                            /* TODO positionWS, _TexWorldScale, */
                            mappingType, layerTexCoord);
}

// This is call only in this file
// layerTexCoord must have been initialize to 0 outside of this function
void GetLayerTexCoord(FragInputs input, inout LayerTexCoord layerTexCoord)
{
// TODO: SURFACE_GRADIENT
//#ifdef SURFACE_GRADIENT
    //GenerateLayerTexCoordBasisTB(input, layerTexCoord);
//#endif

    GetLayerTexCoord(   input.texCoord0, input.texCoord1, input.texCoord2, input.texCoord3,
                        input.positionWS, input.worldToTangent[2].xyz, layerTexCoord);
}

//-----------------------------------------------------------------------------
// ...Texture Mapping
//-----------------------------------------------------------------------------

//
// cf with 
//    LitData.hlsl:GetSurfaceAndBuiltinData()
//    LitDataIndividualLayer.hlsl:GetSurfaceData( )
//    LitBuiltinData.hlsl:GetBuiltinData() 
//
// Here we can combine them
//
void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    ApplyDoubleSidedFlipOrMirror(input); // Apply double sided flip on the vertex normal


    LayerTexCoord layerTexCoord;
    ZERO_INITIALIZE(LayerTexCoord, layerTexCoord);
    GetLayerTexCoord(input, layerTexCoord);

    // -------------------------------------------------------------
    // Surface Data:
    // -------------------------------------------------------------

    // We perform the conversion to world of the normalTS outside of the GetSurfaceData
    // so it allow us to correctly deal with detail normal map and optimize the code for the layered shaders
    float3 normalTS;
    // TODO: Those are only needed once we handle specular occlusion and optionnally bent normal maps.
    // Also, for the builtinData part, use bentnormal to sample diffuse GI
    //float3 bentNormalTS;
    //float3 bentNormalWS;

    //float alpha = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, baseColorMapUv).a * _BaseColor.a;
    float alpha = SAMPLE_UVMAPPING_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, layerTexCoord.base).a * _BaseColor.a;
#ifdef _ALPHATEST_ON
    //NEWLITTODO: Once we include those passes in the main StackLit.shader, add handling of CUTOFF_TRANSPARENT_DEPTH_PREPASS and _POSTPASS
    // and the related properties (in the .shader) and uniforms (in the StackLitProperties file) _AlphaCutoffPrepass, _AlphaCutoffPostpass
    DoAlphaTest(alpha, _AlphaCutoff);
#endif

    // TODO detail map:
    float3 detailNormalTS = float3(0.0, 0.0, 0.0);
    float detailMask = 0.0;


    //TODO remove the following and use fetching macros that use uvmapping :
    //float2 baseColorMapUv = TRANSFORM_TEX(input.texCoord0, _BaseColorMap);
    //surfaceData.baseColor = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, baseColorMapUv).rgb * _BaseColor.rgb;
    surfaceData.baseColor = SAMPLE_UVMAPPING_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, layerTexCoord.base).rgb * _BaseColor.rgb;


    //surfaceData.normalWS = float3(0.0, 0.0, 0.0);

    normalTS = GetNormalTS(input, layerTexCoord, detailNormalTS, detailMask);
    //TODO: bentNormalTS

#if defined(_SMOOTHNESSMASKMAPA)
    surfaceData.perceptualSmoothnessA = dot(SAMPLE_UVMAPPING_TEXTURE2D(_SmoothnessAMap, sampler_SmoothnessAMap, layerTexCoord.base), _MetallicMapChannel, _SmoothnessAMapChannel);
    surfaceData.perceptualSmoothnessA = lerp(_SmoothnessARemap.x, _SmoothnessARemap.y, surfaceData.perceptualSmoothnessA);
#else
    surfaceData.perceptualSmoothnessA = _SmoothnessA;
#endif

#if defined(_SMOOTHNESSMASKMAPB)
    surfaceData.perceptualSmoothnessB = dot(SAMPLE_UVMAPPING_TEXTURE2D(_SmoothnessAMaB, sampler_SmoothnessBMap, layerTexCoord.base), _SmoothnessBMapChannel);
    surfaceData.perceptualSmoothnessB = lerp(_SmoothnessBRemap.x, _SmoothnessBRemap.y, surfaceData.perceptualSmoothnessB);
#else
    surfaceData.perceptualSmoothnessB = _SmoothnessB;
#endif

    // TODOSTACKLIT: lobe weighting
    surfaceData.lobeMix = _LobeMix;

    // TODO: Ambient occlusion, detail mask.
#ifdef _METALLICMAP
    surfaceData.metallic = dot(SAMPLE_UVMAPPING_TEXTURE2D(_MetallicMap, sampler_MetallicMap, layerTexCoord.base), _MetallicMapChannel);
#else
    surfaceData.metallic = 1.0;
#endif
    surfaceData.metallic *= _Metallic;

    // These static material feature allow compile time optimization
    // TODO: As we add features, or-set the flags eg MATERIALFEATUREFLAGS_LIT_* with #ifdef 
    // on corresponding _MATERIAL_FEATURE_* shader_feature kerwords (set by UI) so the compiler 
    // knows the value of surfaceData.materialFeatures.
    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;

    // -------------------------------------------------------------
    // Surface Data Part 2 (outsite GetSurfaceData( ) in Lit shader):
    // -------------------------------------------------------------

    GetNormalWS(input, V, normalTS, surfaceData.normalWS); // MaterialUtilities.hlsl


    // TODO: decal etc.

#if defined(DEBUG_DISPLAY)
    if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
    {
        surfaceData.baseColor = GetTextureDataDebug(_DebugMipMapMode, layerTexCoord.base.uv, _BaseColorMap, _BaseColorMap_TexelSize, _BaseColorMap_MipInfo, surfaceData.baseColor);
        surfaceData.metallic = 0;
    }
#endif

    // -------------------------------------------------------------
    // Builtin Data:
    // -------------------------------------------------------------

    // NEWLITTODO: for all BuiltinData, might need to just refactor and use a comon function like that 
    // contained in LitBuiltinData.hlsl

    builtinData.opacity = alpha;

    builtinData.bakeDiffuseLighting = float3(0.0, 0.0, 0.0);

    // Emissive Intensity is only use here, but is part of BuiltinData to enforce UI parameters as we want the users to fill one color and one intensity
    builtinData.emissiveIntensity = _EmissiveIntensity; // We still store intensity here so we can reuse it with debug code
    builtinData.emissiveColor = _EmissiveColor * builtinData.emissiveIntensity * lerp(float3(1.0, 1.0, 1.0), surfaceData.baseColor.rgb, _AlbedoAffectEmissive);

#ifdef _EMISSIVE_COLOR_MAP
    builtinData.emissiveColor *= SAMPLE_TEXTURE2D(_EmissiveColorMap, sampler_EmissiveColorMap, TRANSFORM_TEX(input.texCoord0, _EmissiveColorMap)).rgb;
#endif

    builtinData.velocity = float2(0.0, 0.0);

    
    //NEWLITTODO: shader feature SHADOWS_SHADOWMASK not there yet. 
    builtinData.shadowMask0 = 0.0;
    builtinData.shadowMask1 = 0.0;
    builtinData.shadowMask2 = 0.0;
    builtinData.shadowMask3 = 0.0;

#if (SHADERPASS == SHADERPASS_DISTORTION) || defined(DEBUG_DISPLAY)
    float3 distortion = SAMPLE_TEXTURE2D(_DistortionVectorMap, sampler_DistortionVectorMap, input.texCoord0).rgb;
    distortion.rg = distortion.rg * _DistortionVectorScale.xx + _DistortionVectorBias.xx;
    builtinData.distortion = distortion.rg * _DistortionScale;
    builtinData.distortionBlur = clamp(distortion.b * _DistortionBlurScale, 0.0, 1.0) * (_DistortionBlurRemapMax - _DistortionBlurRemapMin) + _DistortionBlurRemapMin;
#else
    builtinData.distortion = float2(0.0, 0.0);
    builtinData.distortionBlur = 0.0;
#endif

    builtinData.depthOffset = 0.0;

}
