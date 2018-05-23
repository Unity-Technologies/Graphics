//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "CoreRP/ShaderLibrary/Sampling/SampleUVMapping.hlsl"
#include "../MaterialUtilities.hlsl"

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

    float3 vertexNormalWS;
    float3 vertexTangentWS[4];
    float3 vertexBitangentWS[4];
};

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
    float3 vertexNormalWS = input.worldToTangent[2];
    uvMapping.vertexNormalWS = vertexNormalWS;

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
}

float4 SampleTexture2DScaleBias(TEXTURE2D_ARGS(textureName, samplerName), float textureNameUV, float textureNameUVLocal, float4 textureNameST, TextureUVMapping uvMapping)
{
    return SAMPLE_TEXTURE2D(textureName, samplerName, (uvMapping.texcoords[textureNameUV][textureNameUVLocal] * textureNameST.xy + textureNameST.zw));
}

// If we use triplanar on any of the properties, then we enable the triplanar path
float4 SampleTexture2DTriplanarScaleBias(TEXTURE2D_ARGS(textureName, samplerName), float textureNameUV, float textureNameUVLocal, float4 textureNameST, TextureUVMapping uvMapping)
{
#ifdef _USE_TRIPLANAR
    if (textureNameUV == TEXCOORD_INDEX_TRIPLANAR)
    {
        float4 val = float4(0.0, 0.0, 0.0, 0.0);

        if (uvMapping.triplanarWeights[textureNameUVLocal].x > 0.0)
            val += uvMapping.triplanarWeights[textureNameUVLocal].x * SampleTexture2DScaleBias(TEXTURE2D_PARAM(textureName, samplerName), TEXCOORD_INDEX_PLANAR_YZ, textureNameUVLocal, textureNameST, uvMapping);
        if (uvMapping.triplanarWeights[textureNameUVLocal].y > 0.0)
            val += uvMapping.triplanarWeights[textureNameUVLocal].y * SampleTexture2DScaleBias(TEXTURE2D_PARAM(textureName, samplerName), TEXCOORD_INDEX_PLANAR_ZX, textureNameUVLocal, textureNameST, uvMapping);
        if (uvMapping.triplanarWeights[textureNameUVLocal].z > 0.0)
            val += uvMapping.triplanarWeights[textureNameUVLocal].z * SampleTexture2DScaleBias(TEXTURE2D_PARAM(textureName, samplerName), TEXCOORD_INDEX_PLANAR_XY, textureNameUVLocal, textureNameST, uvMapping);

        return val;
    }
    else
    {
#endif // _USE_TRIPLANAR
        return SampleTexture2DScaleBias(TEXTURE2D_PARAM(textureName, samplerName), textureNameUV, textureNameUVLocal, textureNameST, uvMapping);
#ifdef _USE_TRIPLANAR
    }
#endif
}

float4 SampleTexture2DTriplanarNormalScaleBias(TEXTURE2D_ARGS(textureName, samplerName), float textureNameUV, float textureNameUVLocal, float4 textureNameST, float textureNameObjSpace, TextureUVMapping uvMapping, float scale)
{
    if (textureNameObjSpace)
    {
        // TODO: obj triplanar (need to do * 2 - 1 before blending)

        // We forbid scale in case of object space as it make no sense
        // Decompress normal ourselve
        float3 normalOS = SampleTexture2DTriplanarScaleBias(TEXTURE2D_PARAM(textureName, samplerName), textureNameUV, textureNameUVLocal, textureNameST, uvMapping).xyz * 2.0 - 1.0;
        // no need to renormalize normalOS for SurfaceGradientFromPerturbedNormal
        return float4(SurfaceGradientFromPerturbedNormal(uvMapping.vertexNormalWS, TransformObjectToWorldDir(normalOS)), 1.0);
    }
    else
    {
#ifdef _USE_TRIPLANAR
        if (textureNameUV == TEXCOORD_INDEX_TRIPLANAR)
        {
            float2 derivXplane;
            float2 derivYPlane;
            float2 derivZPlane;
            derivXplane = derivYPlane = derivZPlane = float2(0.0, 0.0);

            if (uvMapping.triplanarWeights[textureNameUVLocal].x > 0.0)
                derivXplane = uvMapping.triplanarWeights[textureNameUVLocal].x * UnpackDerivativeNormalRGorAG(SampleTexture2DScaleBias(TEXTURE2D_PARAM(textureName, samplerName), TEXCOORD_INDEX_PLANAR_YZ, textureNameUVLocal, textureNameST, uvMapping), scale);
            if (uvMapping.triplanarWeights[textureNameUVLocal].y > 0.0)
                derivYPlane = uvMapping.triplanarWeights[textureNameUVLocal].y * UnpackDerivativeNormalRGorAG(SampleTexture2DScaleBias(TEXTURE2D_PARAM(textureName, samplerName), TEXCOORD_INDEX_PLANAR_ZX, textureNameUVLocal, textureNameST, uvMapping), scale);
            if (uvMapping.triplanarWeights[textureNameUVLocal].z > 0.0)
                derivZPlane = uvMapping.triplanarWeights[textureNameUVLocal].z * UnpackDerivativeNormalRGorAG(SampleTexture2DScaleBias(TEXTURE2D_PARAM(textureName, samplerName), TEXCOORD_INDEX_PLANAR_XY, textureNameUVLocal, textureNameST, uvMapping), scale);

            // Assume derivXplane, derivYPlane and derivZPlane sampled using (z,y), (z,x) and (x,y) respectively.
            float3 volumeGrad = float3(derivZPlane.x + derivYPlane.y, derivZPlane.y + derivXplane.y, derivXplane.x + derivYPlane.x);
            return float4(SurfaceGradientFromVolumeGradient(uvMapping.vertexNormalWS, volumeGrad), 1.0);
        }
#endif

        float2 deriv = UnpackDerivativeNormalRGorAG(SampleTexture2DScaleBias(TEXTURE2D_PARAM(textureName, samplerName), textureNameUV, textureNameUVLocal, textureNameST, uvMapping), scale);

        if (textureNameUV <= TEXCOORD_INDEX_UV3)
        {
            return float4(SurfaceGradientFromTBN(deriv, uvMapping.vertexTangentWS[textureNameUV], uvMapping.vertexBitangentWS[textureNameUV]), 1.0);
        }
        else
        {
            float3  volumeGrad;
            if (textureNameUV == TEXCOORD_INDEX_PLANAR_YZ)
                volumeGrad = float3(0.0, deriv.y, deriv.x);
            else if (textureNameUV == TEXCOORD_INDEX_PLANAR_ZX)
                volumeGrad = float3(deriv.y, 0.0, deriv.x);
            else if (textureNameUV == TEXCOORD_INDEX_PLANAR_XY)
                volumeGrad = float3(deriv.x, deriv.y, 0.0);

            return float4(SurfaceGradientFromVolumeGradient(uvMapping.vertexNormalWS, volumeGrad), 1.0f);
        }
    }
}

#define SAMPLE_TEXTURE2D_SCALE_BIAS(name) SampleTexture2DTriplanarScaleBias(name, sampler##name, name##UV, name##UVLocal, name##_ST, uvMapping)
#define SAMPLE_TEXTURE2D_NORMAL_SCALE_BIAS(name, scale) SampleTexture2DTriplanarNormalScaleBias(name, sampler##name, name##UV, name##UVLocal, name##_ST, name##ObjSpace, uvMapping, scale)

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
    // Surface Data
    // -------------------------------------------------------------

    float alpha = SAMPLE_TEXTURE2D_SCALE_BIAS(_BaseColorMap).a * _BaseColor.a;
#ifdef _ALPHATEST_ON
    //NEWLITTODO: Once we include those passes in the main StackLit.shader, add handling of CUTOFF_TRANSPARENT_DEPTH_PREPASS and _POSTPASS
    // and the related properties (in the .shader) and uniforms (in the StackLitProperties file) _AlphaCutoffPrepass, _AlphaCutoffPostpass
    DoAlphaTest(alpha, _AlphaCutoff);
#endif

    // Standard
    surfaceData.baseColor = SAMPLE_TEXTURE2D_SCALE_BIAS(_BaseColorMap).rgb * _BaseColor.rgb;

    float4 gradient = SAMPLE_TEXTURE2D_NORMAL_SCALE_BIAS(_NormalMap, _NormalScale);
    //TODO: bentNormalTS

    surfaceData.perceptualSmoothnessA = dot(SAMPLE_TEXTURE2D_SCALE_BIAS(_SmoothnessAMap), _SmoothnessAMapChannelMask);
    surfaceData.perceptualSmoothnessA = lerp(_SmoothnessARange.x, _SmoothnessARange.y, surfaceData.perceptualSmoothnessA);
    surfaceData.perceptualSmoothnessA = lerp(_SmoothnessA, surfaceData.perceptualSmoothnessA, _SmoothnessAUseMap);

    surfaceData.metallic = dot(SAMPLE_TEXTURE2D_SCALE_BIAS(_MetallicMap), _MetallicMapChannelMask);
    surfaceData.metallic = lerp(_MetallicRange.x, _MetallicRange.y, surfaceData.metallic);
    surfaceData.metallic = lerp(_Metallic, surfaceData.metallic, _MetallicUseMap);

    surfaceData.dielectricIor = _DielectricIor;

    surfaceData.ambientOcclusion = dot(SAMPLE_TEXTURE2D_SCALE_BIAS(_AmbientOcclusionMap), _AmbientOcclusionMapChannelMask);
    surfaceData.ambientOcclusion = lerp(_AmbientOcclusionRange.x, _AmbientOcclusionRange.y, surfaceData.ambientOcclusion);
    surfaceData.ambientOcclusion = lerp(_AmbientOcclusion, surfaceData.ambientOcclusion, _AmbientOcclusionUseMap);

    // These static material feature allow compile time optimization
    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_STACK_LIT_STANDARD;

    // Feature dependent data
#ifdef _MATERIAL_FEATURE_DUAL_SPECULAR_LOBE
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_STACK_LIT_DUAL_SPECULAR_LOBE;
    surfaceData.lobeMix = _LobeMix;
    surfaceData.perceptualSmoothnessB = dot(SAMPLE_TEXTURE2D_SCALE_BIAS(_SmoothnessBMap), _SmoothnessBMapChannelMask);
    surfaceData.perceptualSmoothnessB = lerp(_SmoothnessBRange.x, _SmoothnessBRange.y, surfaceData.perceptualSmoothnessB);
    surfaceData.perceptualSmoothnessB = lerp(_SmoothnessB, surfaceData.perceptualSmoothnessB, _SmoothnessBUseMap);
#else
    surfaceData.lobeMix = 0.0;
    surfaceData.perceptualSmoothnessB = 1.0;
#endif

#ifdef _MATERIAL_FEATURE_ANISOTROPY
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY;
    // TODO: manage anistropy map
    //surfaceData.anisotropy = dot(SAMPLE_TEXTURE2D_SCALE_BIAS(_AnistropyMap), _AnistropyMapChannelMask);
    //surfaceData.anisotropy = lerp(_AnistropyRange.x, _AnistropyRange.y, surfaceData.anisotropy);
    surfaceData.anisotropy = _Anisotropy; // In all cases we must multiply anisotropy with the map
#else
    surfaceData.anisotropy = 0.0;
#endif
    surfaceData.tangentWS = normalize(input.worldToTangent[0].xyz); // The tangent is not normalize in worldToTangent for mikkt. TODO: Check if it expected that we normalize with Morten. Tag: SURFACE_GRADIENT

    float4 coatGradient = float4(0.0, 0.0, 0.0, 1.0f);
#ifdef _MATERIAL_FEATURE_COAT
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_STACK_LIT_COAT;
    surfaceData.coatPerceptualSmoothness = dot(SAMPLE_TEXTURE2D_SCALE_BIAS(_CoatSmoothnessMap), _CoatSmoothnessMapChannelMask);
    surfaceData.coatPerceptualSmoothness = lerp(_CoatSmoothnessRange.x, _CoatSmoothnessRange.y, surfaceData.coatPerceptualSmoothness);
    surfaceData.coatPerceptualSmoothness = lerp(_CoatSmoothness, surfaceData.coatPerceptualSmoothness, _CoatSmoothnessUseMap);
    surfaceData.coatIor = _CoatIor;
    surfaceData.coatThickness = _CoatThickness;
    surfaceData.coatExtinction = _CoatExtinction; // in thickness^-1 units

#ifdef _MATERIAL_FEATURE_COAT_NORMALMAP
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_STACK_LIT_COAT_NORMAL_MAP;
    coatGradient = SAMPLE_TEXTURE2D_NORMAL_SCALE_BIAS(_CoatNormalMap, _CoatNormalScale);
#endif

#else
    surfaceData.coatPerceptualSmoothness = 0.0;
    surfaceData.coatIor = 1.0001;
    surfaceData.coatThickness = 0.0;
    surfaceData.coatExtinction = float3(1.0, 1.0, 1.0);
#endif // _MATERIAL_FEATURE_COAT

#ifdef _MATERIAL_FEATURE_IRIDESCENCE
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_STACK_LIT_IRIDESCENCE;
    surfaceData.iridescenceIor = _IridescenceIor;
    surfaceData.iridescenceThickness = dot(SAMPLE_TEXTURE2D_SCALE_BIAS(_IridescenceThicknessMap), _IridescenceThicknessMapChannelMask);
    surfaceData.iridescenceThickness = lerp(_IridescenceThicknessRange.x, _IridescenceThicknessRange.y, surfaceData.iridescenceThickness);
    surfaceData.iridescenceThickness = lerp(_IridescenceThickness, surfaceData.iridescenceThickness, _IridescenceThicknessUseMap);
#else
    surfaceData.iridescenceIor = 1.0;
    surfaceData.iridescenceThickness = 0.0;
#endif

#if defined(_MATERIAL_FEATURE_SUBSURFACE_SCATTERING) || defined(_MATERIAL_FEATURE_TRANSMISSION)
    surfaceData.diffusionProfile = _DiffusionProfile;
#else
    surfaceData.diffusionProfile = 0;
#endif

#ifdef _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_STACK_LIT_SUBSURFACE_SCATTERING;
    surfaceData.subsurfaceMask = dot(SAMPLE_TEXTURE2D_SCALE_BIAS(_SubsurfaceMaskMap), _SubsurfaceMaskMapChannelMask);
    surfaceData.subsurfaceMask = lerp(_SubsurfaceMaskRange.x, _SubsurfaceMaskRange.y, surfaceData.subsurfaceMask);
    surfaceData.subsurfaceMask = lerp(_SubsurfaceMask, surfaceData.subsurfaceMask, _SubsurfaceMaskUseMap);
#else
    surfaceData.subsurfaceMask = 0.0;
#endif

#ifdef _MATERIAL_FEATURE_TRANSMISSION
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_STACK_LIT_TRANSMISSION;
    surfaceData.thickness = dot(SAMPLE_TEXTURE2D_SCALE_BIAS(_ThicknessMap), _ThicknessMapChannelMask);
    surfaceData.thickness = lerp(_ThicknessRange.x, _ThicknessRange.y, surfaceData.thickness);
    surfaceData.thickness = lerp(_Thickness, surfaceData.thickness, _ThicknessUseMap);
#else
    surfaceData.thickness = 1.0;
#endif

    // -------------------------------------------------------------
    // Surface Data Part 2 (outsite GetSurfaceData( ) in Lit shader):
    // -------------------------------------------------------------

    surfaceData.geomNormalWS = input.worldToTangent[2];
    // Convert back to world space normal
    surfaceData.normalWS = SurfaceGradientResolveNormal(input.worldToTangent[2], gradient.xyz);
    surfaceData.coatNormalWS = SurfaceGradientResolveNormal(input.worldToTangent[2], coatGradient.xyz);

    surfaceData.averageNormalLengthA = gradient.w;
    surfaceData.averageNormalLengthB = coatGradient.w;

    // TODO: decal etc.

#if defined(DEBUG_DISPLAY)
    if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
    {
        if (_BaseColorMapUV != TEXCOORD_INDEX_TRIPLANAR)
        {
            surfaceData.baseColor = GetTextureDataDebug(_DebugMipMapMode, uvMapping.texcoords[_BaseColorMapUV][_BaseColorMapUVLocal], _BaseColorMap, _BaseColorMap_TexelSize, _BaseColorMap_MipInfo, surfaceData.baseColor);
        }
        else
        {
            surfaceData.baseColor = float3(0.0, 0.0, 0.0);
        }
        surfaceData.metallic = 0.0;
    }
#endif

    // -------------------------------------------------------------
    // Builtin Data:
    // -------------------------------------------------------------

    // NEWLITTODO: for all BuiltinData, might need to just refactor and use a comon function like that
    // contained in LitBuiltinData.hlsl

    builtinData.opacity = alpha;

    builtinData.bakeDiffuseLighting = SampleBakedGI(input.positionWS, surfaceData.normalWS, input.texCoord1, input.texCoord2);

    // It is safe to call this function here as surfaceData have been filled
    // We want to know if we must enable transmission on GI for SSS material, if the material have no SSS, this code will be remove by the compiler.
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(surfaceData);
    if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_TRANSMISSION))
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
    builtinData.emissiveColor *= SAMPLE_TEXTURE2D_SCALE_BIAS(_EmissiveColorMap).rgb;

    // TODO:
    builtinData.velocity = float2(0.0, 0.0);

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
