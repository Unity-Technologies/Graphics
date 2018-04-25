//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "CoreRP/ShaderLibrary/Sampling/SampleUVMapping.hlsl"
#include "../MaterialUtilities.hlsl"

////-----------------------------------------------------------------------------
//// Texture Mapping (think of LayerTexCoord as simply TexCoordMappings,
//// ie no more layers here - cf Lit materials)
////-----------------------------------------------------------------------------
//
////
//// For easier copying of code for now use a LayerTexCoord wrapping struct.
//// We don't have details yet.
////
//// NEWLITTODO: Eventually, we could quickly share GetBuiltinData of LitBuiltinData.hlsl
//// in our GetSurfaceAndBuiltinData( ) here, since we will use the LayerTexCoord identifier,
//// and an identical ComputeLayerTexCoord( ) prototype
////
//struct LayerTexCoord
//{
//    UVMapping base;
//
//    // Store information that will be share by all UVMapping
//    float3 vertexNormalWS; // TODO: store also object normal map for object triplanar
//};
//
//// Want to use only one sampler for normalmap/bentnormalmap either we use OS or TS. And either we have normal map or bent normal or both.
////
//// Note (compared to Lit shader):
////
//// We don't have a layered material with which we are sharing code here like the LayeredLit shader, but we can also save a couple of
//// samplers later if we use bentnormals.
////
//// _IDX suffix is meaningless here, could use the name SAMPLER_NORMALMAP_ID instead of SAMPLER_NORMALMAP_IDX and replace all
//// indirect #ifdef _NORMALMAP_TANGENT_SPACE_IDX #ifdef and _NORMALMAP_IDX tests with the more direct
//// shader_feature keywords _NORMALMAP_TANGENT_SPACE and _NORMALMAP.
////
//// (Originally in the LayeredLit shader, shader_feature keywords like _NORMALMAP become _NORMALMAP0 but since files are shared,
//// LitDataIndividualLayer will use a generic _NORMALMAP_IDX defined before its inclusion by the client LitData or LayeredLitData.
//// That way, LitDataIndividualLayer supports multiple inclusions)

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

float3 GetNormalTS(FragInputs input, float2 texCoord, float3 detailNormalTS, float detailMask)
{
    // TODO: different spaces (eg #ifdef _NORMALMAP_TANGENT_SPACE #elif object space, SURFACE_GRADIENT, etc.)
    // and use detail map

    float3 normalTS;

    // Note we don't use the _NORMALMAP_IDX mechanism of the Lit shader, since we don't have "layers", we can
    // directly use the shader_feature keyword:
#ifdef _NORMALMAP
    normalTS = float3(0.0, 0.0, 1.0);  //normalTS = SAMPLE_UVMAPPING_NORMALMAP(_NormalMap, SAMPLER_NORMALMAP_ID, texCoord, _NormalScale);
#else
    normalTS = float3(0.0, 0.0, 1.0);
#endif

    return normalTS;
}

//-----------------------------------------------------------------------------
// Texture Mapping
//-----------------------------------------------------------------------------

#define TEXCOORD_INDEX_UV0          (0)
#define TEXCOORD_INDEX_UV1          (1)
#define TEXCOORD_INDEX_UV2          (2)
#define TEXCOORD_INDEX_UV3          (3)
#define TEXCOORD_INDEX_PLANAR_XY    (4)
#define TEXCOORD_INDEX_PLANAR_YZ    (5)
#define TEXCOORD_INDEX_PLANAR_ZX    (6)
#define TEXCOORD_INDEX_TRIPLANAR    (7)
#define TEXCOORD_INDEX_COUNT        (TEXCOORD_INDEX_TRIPLANAR) // Triplanar is not consider as having mapping

// If we use triplanar on any of the properties, then we enable the triplanar path
#ifdef _USE_TRIPLANAR
float4 SampleTexture2DTriplanar(TEXTURE2D_ARGS(textureName, samplerName), float textureNameUV, float4 textureNameST, float2 texcoords[TEXCOORD_INDEX_COUNT], float3 triplanarWeights)
{
    if (textureNameUV == TEXCOORD_INDEX_TRIPLANAR)
    {
        float4 val = float4(0.0, 0.0, 0.0, 0.0);

        if (triplanarWeights.x > 0.0)
            val += triplanarWeights.x * SAMPLE_TEXTURE2D(textureName, samplerName, (texcoords[TEXCOORD_INDEX_PLANAR_YZ] * textureNameST.xy + textureNameST.zw));
        if (triplanarWeights.y > 0.0)
            val += triplanarWeights.y * SAMPLE_TEXTURE2D(textureName, samplerName, (texcoords[TEXCOORD_INDEX_PLANAR_ZX] * textureNameST.xy + textureNameST.zw));
        if (triplanarWeights.z > 0.0)
            val += triplanarWeights.z * SAMPLE_TEXTURE2D(textureName, samplerName, (texcoords[TEXCOORD_INDEX_PLANAR_XY] * textureNameST.xy + textureNameST.zw));

        return val;
    }
    else
    {
        return SAMPLE_TEXTURE2D(textureName, samplerName, (texcoords[textureNameUV] * textureNameST.xy + textureNameST.zw));
    }
}

#define SAMPLE_TEXTURE2D_SCALE_BIAS(name) SampleTexture2DTriplanar(name, sampler##name, name##UV, name##_ST, texcoords, triplanarWeights)

#else
#define SAMPLE_TEXTURE2D_SCALE_BIAS(name) SAMPLE_TEXTURE2D(name, sampler##name, (texcoords[name##UV] * name##_ST.xy + name##_ST.zw))
#endif // _USE_TRIPLANAR


void InitializeMappingData(FragInputs input, out float2 texcoords[TEXCOORD_INDEX_COUNT], out float3 triplanarWeights)
{
    float3 position = GetAbsolutePositionWS(input.positionWS);
    // If we use local planar mapping, convert to local space
    position = _UseLocalPlanarMapping ? TransformWorldToObject(position) : position;
    // planar/triplanar
    float2 uvXZ;
    float2 uvXY;
    float2 uvZY;
    GetTriplanarCoordinate(position, uvXZ, uvXY, uvZY);

#ifdef _USE_TRIPLANAR
    float3 vertexNormal = input.worldToTangent[2].xyz; // normal in WS
    vertexNormal = _UseLocalPlanarMapping ? TransformWorldToObjectDir(vertexNormal) : vertexNormal;
    triplanarWeights = ComputeTriplanarWeights(vertexNormal);
#else
    triplanarWeights = float3(0.0, 0.0, 0.0);
#endif

    // Build the texcoords array.
    texcoords[TEXCOORD_INDEX_UV0] = input.texCoord0;
    texcoords[TEXCOORD_INDEX_UV1] = input.texCoord1;
#ifdef _USE_UV2
    texcoords[TEXCOORD_INDEX_UV2] = input.texCoord2;
#else
    texcoords[TEXCOORD_INDEX_UV2] = float2(0.0, 0.0);
#endif
#ifdef _USE_UV3
    texcoords[TEXCOORD_INDEX_UV3] = input.texCoord3;
#else
    texcoords[TEXCOORD_INDEX_UV3] = float2(0.0, 0.0);
#endif
    texcoords[TEXCOORD_INDEX_PLANAR_XY] = uvXY;
    texcoords[TEXCOORD_INDEX_PLANAR_YZ] = uvZY;
    texcoords[TEXCOORD_INDEX_PLANAR_ZX] = uvXZ;
}

//-----------------------------------------------------------------------------
// GetSurfaceAndBuiltinData
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
    ApplyDoubleSidedFlipOrMirror(input); // Apply double sided flip on the vertex normal.

    float2 texcoords[TEXCOORD_INDEX_COUNT];
    float3 triplanarWeights;
    InitializeMappingData(input, texcoords, triplanarWeights);

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

    float alpha = SAMPLE_TEXTURE2D_SCALE_BIAS(_BaseColorMap).a * _BaseColor.a;
#ifdef _ALPHATEST_ON
    //NEWLITTODO: Once we include those passes in the main StackLit.shader, add handling of CUTOFF_TRANSPARENT_DEPTH_PREPASS and _POSTPASS
    // and the related properties (in the .shader) and uniforms (in the StackLitProperties file) _AlphaCutoffPrepass, _AlphaCutoffPostpass
    DoAlphaTest(alpha, _AlphaCutoff);
#endif

    // TODO detail map:
    float3 detailNormalTS = float3(0.0, 0.0, 0.0);
    float detailMask = 0.0;

    surfaceData.baseColor = SAMPLE_TEXTURE2D_SCALE_BIAS(_BaseColorMap).rgb * _BaseColor.rgb;

    //surfaceData.normalWS = float3(0.0, 0.0, 0.0);

    normalTS = GetNormalTS(input, texcoords[_NormalMapUV], detailNormalTS, detailMask);
    //TODO: bentNormalTS

    surfaceData.perceptualSmoothnessA = dot(SAMPLE_TEXTURE2D_SCALE_BIAS(_SmoothnessAMap), _SmoothnessAMapChannelMask);
    surfaceData.perceptualSmoothnessA = lerp(_SmoothnessARange.x, _SmoothnessARange.y, surfaceData.perceptualSmoothnessA);
    surfaceData.perceptualSmoothnessA = lerp(_SmoothnessA, surfaceData.perceptualSmoothnessA, _SmoothnessAUseMap);

    surfaceData.perceptualSmoothnessB = dot(SAMPLE_TEXTURE2D_SCALE_BIAS(_SmoothnessBMap), _SmoothnessBMapChannelMask);
    surfaceData.perceptualSmoothnessB = lerp(_SmoothnessBRange.x, _SmoothnessBRange.y, surfaceData.perceptualSmoothnessB);
    surfaceData.perceptualSmoothnessB = lerp(_SmoothnessB, surfaceData.perceptualSmoothnessB, _SmoothnessBUseMap);

    // TODOSTACKLIT: lobe weighting
    surfaceData.lobeMix = _LobeMix;

    // TODO: Ambient occlusion, detail mask.
    surfaceData.metallic = dot(SAMPLE_TEXTURE2D_SCALE_BIAS(_MetallicMap), _MetallicMapChannelMask);
    surfaceData.metallic = lerp(_MetallicRange.x, _MetallicRange.y, surfaceData.metallic);
    surfaceData.metallic = lerp(_Metallic, surfaceData.metallic, _MetallicUseMap);

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
        surfaceData.baseColor = GetTextureDataDebug(_DebugMipMapMode, texcoords[_BaseColorMapUV], _BaseColorMap, _BaseColorMap_TexelSize, _BaseColorMap_MipInfo, surfaceData.baseColor);
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
    builtinData.emissiveColor *= SAMPLE_TEXTURE2D_SCALE_BIAS(_EmissiveColorMap).rgb;

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
