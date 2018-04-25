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

struct TextureUVMapping
{
    float2 texcoords[TEXCOORD_INDEX_COUNT][2];
#ifdef _USE_TRIPLANAR
    float3 triplanarWeights[2];
#endif

#ifdef SURFACE_GRADIENT
   // float3 vertexTangentWS[4];
   // float3 vertexBitangentWS[4];
#endif
};

float4 SampleTexture2DPlanar(TEXTURE2D_ARGS(textureName, samplerName), float textureNameUV, float textureNameUVLocal, float4 textureNameST, TextureUVMapping uvMapping)
{
    return SAMPLE_TEXTURE2D(textureName, samplerName, (uvMapping.texcoords[textureNameUV][textureNameUVLocal] * textureNameST.xy + textureNameST.zw));
}

// If we use triplanar on any of the properties, then we enable the triplanar path
#ifdef _USE_TRIPLANAR
float4 SampleTexture2DTriplanar(TEXTURE2D_ARGS(textureName, samplerName), float textureNameUV, float textureNameUVLocal, float4 textureNameST, TextureUVMapping uvMapping)
{
    if (textureNameUV == TEXCOORD_INDEX_TRIPLANAR)
    {
        float4 val = float4(0.0, 0.0, 0.0, 0.0);

        if (uvMapping.triplanarWeights[textureNameUVLocal].x > 0.0)
            val += uvMapping.triplanarWeights[textureNameUVLocal].x * SAMPLE_TEXTURE2D(textureName, samplerName, (uvMapping.texcoords[TEXCOORD_INDEX_PLANAR_YZ][textureNameUVLocal] * textureNameST.xy + textureNameST.zw));
        if (uvMapping.triplanarWeights[textureNameUVLocal].y > 0.0)
            val += uvMapping.triplanarWeights[textureNameUVLocal].y * SAMPLE_TEXTURE2D(textureName, samplerName, (uvMapping.texcoords[TEXCOORD_INDEX_PLANAR_ZX][textureNameUVLocal] * textureNameST.xy + textureNameST.zw));
        if (uvMapping.triplanarWeights[textureNameUVLocal].z > 0.0)
            val += uvMapping.triplanarWeights[textureNameUVLocal].z * SAMPLE_TEXTURE2D(textureName, samplerName, (uvMapping.texcoords[TEXCOORD_INDEX_PLANAR_XY][textureNameUVLocal] * textureNameST.xy + textureNameST.zw));

        return val;
    }
    else
    {
        return SampleTexture2DPlanar(TEXTURE2D_PARAM(textureName, samplerName), textureNameUV, textureNameUVLocal, textureNameST, uvMapping);
    }
}

#define SAMPLE_TEXTURE2D_SCALE_BIAS(name) SampleTexture2DTriplanar(name, sampler##name, name##UV, name##UVLocal, name##_ST, uvMapping)
#else
#define SAMPLE_TEXTURE2D_SCALE_BIAS(name) SampleTexture2DPlanar(name, sampler##name, name##UV, name##UVLocal, name##_ST, uvMapping)
#endif // _USE_TRIPLANAR


void InitializeMappingData(FragInputs input, out TextureUVMapping uvMapping)
{
    float3 position = GetAbsolutePositionWS(input.positionWS);
    float2 uvXZ;
    float2 uvXY;
    float2 uvZY;

    // Build the texcoords array.
    uvMapping.texcoords[TEXCOORD_INDEX_UV0][0] = uvMapping.texcoords[TEXCOORD_INDEX_UV0][1] = input.texCoord0.xy;
    uvMapping.texcoords[TEXCOORD_INDEX_UV1][0] = uvMapping.texcoords[TEXCOORD_INDEX_UV1][1] = input.texCoord1.xy;
    uvMapping.texcoords[TEXCOORD_INDEX_UV2][0] = uvMapping.texcoords[TEXCOORD_INDEX_UV2][1] = input.texCoord2.xy;
    uvMapping.texcoords[TEXCOORD_INDEX_UV3][0] = uvMapping.texcoords[TEXCOORD_INDEX_UV3][1] = input.texCoord3.xy;

    // planar/triplanar
    GetTriplanarCoordinate(position, uvXZ, uvXY, uvZY);
    uvMapping.texcoords[TEXCOORD_INDEX_PLANAR_XY][0] = uvXY;
    uvMapping.texcoords[TEXCOORD_INDEX_PLANAR_YZ][0] = uvZY;
    uvMapping.texcoords[TEXCOORD_INDEX_PLANAR_ZX][0] = uvXZ;

    // If we use local planar mapping, convert to local space
    position = TransformWorldToObject(position);
    GetTriplanarCoordinate(position, uvXZ, uvXY, uvZY);
    uvMapping.texcoords[TEXCOORD_INDEX_PLANAR_XY][1] = uvXY;
    uvMapping.texcoords[TEXCOORD_INDEX_PLANAR_YZ][1] = uvZY;
    uvMapping.texcoords[TEXCOORD_INDEX_PLANAR_ZX][1] = uvXZ;

#ifdef _USE_TRIPLANAR
    float3 vertexNormal = input.worldToTangent[2].xyz;
    uvMapping.triplanarWeights[0] = ComputeTriplanarWeights(vertexNormal);
    // If we use local planar mapping, convert to local space
    vertexNormal = TransformWorldToObjectDir(vertexNormal);
    uvMapping.triplanarWeights[1] = ComputeTriplanarWeights(vertexNormal);
#endif

    // Normal mapping with surface gradient
#ifdef SURFACE_GRADIENT
    float3 vertexNormalWS = input.worldToTangent[2];

    uvMapping.vertexTangentWS[0] = input.worldToTangent[0];
    uvMapping.vertexBitangentWS[0] = input.worldToTangent[1];

    float3 dPdx = ddx_fine(input.positionWS);
    float3 dPdy = ddy_fine(input.positionWS);

    float3 sigmaX = dPdx - dot(dPdx, vertexNormalWS) * vertexNormalWS;
    float3 sigmaY = dPdy - dot(dPdy, vertexNormalWS) * vertexNormalWS;
    //float flipSign = dot(sigmaY, cross(vertexNormalWS, sigmaX) ) ? -1.0 : 1.0;
    float flipSign = dot(dPdy, cross(vertexNormalWS, dPdx)) < 0.0 ? -1.0 : 1.0; // gives same as the commented out line above

    SurfaceGradientGenBasisTB(vertexNormalWS, sigmaX, sigmaY, flipSign, input.texCoord1, uvMapping.vertexTangentWS[1], uvMapping.vertexBitangentWS[1]);
    SurfaceGradientGenBasisTB(vertexNormalWS, sigmaX, sigmaY, flipSign, input.texCoord2, uvMapping.vertexTangentWS[2], uvMapping.vertexBitangentWS[2]);
    SurfaceGradientGenBasisTB(vertexNormalWS, sigmaX, sigmaY, flipSign, input.texCoord3, uvMapping.vertexTangentWS[3], uvMapping.vertexBitangentWS[3]);
#endif // SURFACE_GRADIENT
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

    TextureUVMapping uvMapping;
    InitializeMappingData(input, uvMapping);

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

    normalTS = float3(0, 0, 1); // GetNormalTS(input, texcoords[_NormalMapUV], detailNormalTS, detailMask);
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
