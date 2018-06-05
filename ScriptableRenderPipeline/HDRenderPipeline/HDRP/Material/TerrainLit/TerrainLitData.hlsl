//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------

#include "CoreRP/ShaderLibrary/Sampling/SampleUVMapping.hlsl"
#include "../MaterialUtilities.hlsl"
#include "../Lit/LitBuiltinData.hlsl"
#include "../Decal/DecalUtilities.hlsl"

#include "TerrainLitSplatCommon.hlsl"
#include "TerrainLitDataMeshModification.hlsl"

void GetSurfaceAndBuiltinData(inout FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
#ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
    {
        float3 normalOS = SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_Control0, (input.texCoord0 + 0.5f) * _TerrainHeightmapRecipSize.xy).rgb * 2 - 1;
        float3 normalWS = ((float3x3)GetObjectToWorldMatrix(), normalOS);
        float3 tangentWS = cross(GetObjectToWorldMatrix()._13_23_33, normalWS);
        float renormFactor = 1.0 / length(normalWS);

        // bitangent on the fly option in xnormal to reduce vertex shader outputs.
        // this is the mikktspace transformation (must use unnormalized attributes)
        float3x3 worldToTangent = CreateWorldToTangent(normalWS, tangentWS.xyz, 1);

        // surface gradient based formulation requires a unit length initial normal. We can maintain compliance with mikkts
        // by uniformly scaling all 3 vectors since normalization of the perturbed normal will cancel it.
        input.worldToTangent[0] = worldToTangent[0] * renormFactor;
        input.worldToTangent[1] = worldToTangent[1] * renormFactor;
        input.worldToTangent[2] = worldToTangent[2] * renormFactor;		// normalizes the interpolated vertex normal

        input.texCoord0 *= _TerrainHeightmapRecipSize.zw;
    }
#endif

    // terrain lightmap uvs are always taken from uv0
    input.texCoord1 = input.texCoord2 = input.texCoord0;

    ApplyDoubleSidedFlipOrMirror(input); // Apply double sided flip on the vertex normal

    // TODO: triplanar and SURFACE_GRADIENT?
    // TODO: POM

    float2 uv = input.texCoord0;
    float2 uvSplats[_MAX_LAYER];
    uvSplats[0] = uv * _Splat0_ST.xy + _Splat0_ST.zw;
#if _LAYER_COUNT > 1
    uvSplats[1] = uv * _Splat1_ST.xy + _Splat1_ST.zw;
#endif
#if _LAYER_COUNT > 2
    uvSplats[2] = uv * _Splat2_ST.xy + _Splat2_ST.zw;
#endif
#if _LAYER_COUNT > 3
    uvSplats[3] = uv * _Splat3_ST.xy + _Splat3_ST.zw;
#endif
#if _LAYER_COUNT > 4
    uvSplats[4] = uv * _Splat4_ST.xy + _Splat4_ST.zw;
#endif
#if _LAYER_COUNT > 5
    uvSplats[5] = uv * _Splat5_ST.xy + _Splat5_ST.zw;
#endif
#if _LAYER_COUNT > 6
    uvSplats[6] = uv * _Splat6_ST.xy + _Splat6_ST.zw;
#endif
#if _LAYER_COUNT > 7
    uvSplats[7] = uv * _Splat7_ST.xy + _Splat7_ST.zw;
#endif

    // sample weights from Control maps
    float4 blendMasks0 = SAMPLE_TEXTURE2D(_Control0, sampler_Control0, uv);
#if _LAYER_COUNT > 4
    float4 blendMasks1 = SAMPLE_TEXTURE2D(_Control1, sampler_Control0, uv);
#else
    float4 blendMasks1 = float4(0, 0, 0, 0);
#endif

    // adjust weights for density mode
#ifdef _DENSITY_MODE
    // 20.0 is the number of steps in inputAlphaMask (Density mask. We decided 20 empirically)
    float4 opacityAsDensity0 = saturate((float4(1.0, 1.0, 1.0, 1.0) - (float4(1.0, 1.0, 1.0, 1.0) - blendMasks0)) * 20.0);
    float4 useOpacityAsDensityParam0 = float4(_OpacityAsDensity0, _OpacityAsDensity1, _OpacityAsDensity2, _OpacityAsDensity3);
    blendMasks0 = lerp(blendMasks0, opacityAsDensity0, useOpacityAsDensityParam0);
    #if _LAYER_COUNT > 4
        float4 opacityAsDensity1 = saturate((float4(1.0, 1.0, 1.0, 1.0) - (float4(1.0, 1.0, 1.0, 1.0) - blendMasks0)) * 20.0);
        float4 useOpacityAsDensityParam1 = float4(_OpacityAsDensity4, _OpacityAsDensity5, _OpacityAsDensity6, _OpacityAsDensity7);
        blendMasks1 = lerp(blendMasks1, opacityAsDensity1, useOpacityAsDensityParam1);
    #endif
#endif

    // adjust weights for height based blending
#ifdef _TERRAIN_HEIGHT_MAP
    float4 heights0 = float4(0, 0, 0, 0);
    float4 heights1 = float4(0, 0, 0, 0);
    heights0.r = (SAMPLE_TEXTURE2D(_Height0, sampler_Splat0, uvSplats[0]).r * blendMasks0.r - _HeightCenter0) * _HeightAmplitude0;
    #if _LAYER_COUNT > 1
        heights0.g = (SAMPLE_TEXTURE2D(_Height1, sampler_Splat0, uvSplats[1]).r * blendMasks0.g - _HeightCenter1) * _HeightAmplitude1;
    #endif
    #if _LAYER_COUNT > 2
        heights0.b = (SAMPLE_TEXTURE2D(_Height2, sampler_Splat0, uvSplats[2]).r * blendMasks0.b - _HeightCenter2) * _HeightAmplitude2;
    #endif
    #if _LAYER_COUNT > 3
        heights0.a = (SAMPLE_TEXTURE2D(_Height3, sampler_Splat0, uvSplats[3]).r * blendMasks0.a - _HeightCenter3) * _HeightAmplitude3;
    #endif
    #if _LAYER_COUNT > 4
        heights1.r = (SAMPLE_TEXTURE2D(_Height4, sampler_Splat0, uvSplats[4]).r * blendMasks1.r - _HeightCenter4) * _HeightAmplitude4;
    #endif
    #if _LAYER_COUNT > 5
        heights1.g = (SAMPLE_TEXTURE2D(_Height5, sampler_Splat0, uvSplats[5]).r * blendMasks1.g - _HeightCenter5) * _HeightAmplitude5;
    #endif
    #if _LAYER_COUNT > 6
        heights1.b = (SAMPLE_TEXTURE2D(_Height6, sampler_Splat0, uvSplats[6]).r * blendMasks1.b - _HeightCenter6) * _HeightAmplitude6;
    #endif
    #if _LAYER_COUNT > 7
        heights1.a = (SAMPLE_TEXTURE2D(_Height7, sampler_Splat0, uvSplats[7]).r * blendMasks1.a - _HeightCenter7) * _HeightAmplitude7;
    #endif

    // Modify blendMask to take into account the height of the layer. Higher height should be more visible.
    ApplyHeightBlend(heights0, heights1, blendMasks0, blendMasks1);
#endif

    float weights[_MAX_LAYER];
    ZERO_INITIALIZE_ARRAY(float, weights, _MAX_LAYER);

#if defined(_DENSITY_MODE)
    // calculate weight of each layers
    // Algorithm is like this:
    // Top layer have priority on others layers
    // If a top layer doesn't use the full weight, the remaining can be use by the following layer.
    float weightsSum = 0.0f;
    #if _LAYER_COUNT > 7
        weights[7] = min(blendMasks1.a, (1.0f - weightsSum));
        weightsSum = saturate(weightsSum + weights[7]);
    #endif
    #if _LAYER_COUNT > 6
        weights[6] = min(blendMasks1.b, (1.0f - weightsSum));
        weightsSum = saturate(weightsSum + weights[6]);
    #endif
    #if _LAYER_COUNT > 5
        weights[5] = min(blendMasks1.g, (1.0f - weightsSum));
        weightsSum = saturate(weightsSum + weights[5]);
    #endif
    #if _LAYER_COUNT > 4
        weights[4] = min(blendMasks1.r, (1.0f - weightsSum));
        weightsSum = saturate(weightsSum + weights[4]);
    #endif
    #if _LAYER_COUNT > 3
        weights[3] = min(blendMasks0.a, (1.0f - weightsSum));
        weightsSum = saturate(weightsSum + weights[3]);
    #endif
    #if _LAYER_COUNT > 2
        weights[2] = min(blendMasks0.b, (1.0f - weightsSum));
        weightsSum = saturate(weightsSum + weights[2]);
    #endif
    #if _LAYER_COUNT > 1
        weights[1] = min(blendMasks0.g, (1.0f - weightsSum));
        weightsSum = saturate(weightsSum + weights[1]);
    #endif
    weights[0] = min(blendMasks0.r, (1.0f - weightsSum));
#else
    weights[0] = blendMasks0.r;
    weights[1] = blendMasks0.g;
    weights[2] = blendMasks0.b;
    weights[3] = blendMasks0.a;
    weights[4] = blendMasks1.r;
    weights[5] = blendMasks1.g;
    weights[6] = blendMasks1.b;
    weights[7] = blendMasks1.a;
#endif

    // TODO: conditional samplings
    surfaceData.baseColor = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, uvSplats[0]).rgb * weights[0];
    surfaceData.perceptualSmoothness = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, uvSplats[0]).a * _Smoothness0 * weights[0];
    surfaceData.metallic = _Metallic0 * weights[0];
#if _LAYER_COUNT > 1
    surfaceData.baseColor += SAMPLE_TEXTURE2D(_Splat1, sampler_Splat0, uvSplats[1]).rgb * weights[1];
    surfaceData.perceptualSmoothness += SAMPLE_TEXTURE2D(_Splat1, sampler_Splat0, uvSplats[1]).a * _Smoothness1 * weights[1];
    surfaceData.metallic += _Metallic1 * weights[1];
#endif
#if _LAYER_COUNT > 2
    surfaceData.baseColor += SAMPLE_TEXTURE2D(_Splat2, sampler_Splat0, uvSplats[2]).rgb * weights[2];
    surfaceData.perceptualSmoothness += SAMPLE_TEXTURE2D(_Splat2, sampler_Splat0, uvSplats[2]).a * _Smoothness2 * weights[2];
    surfaceData.metallic += _Metallic2 * weights[2];
#endif
#if _LAYER_COUNT > 3
    surfaceData.baseColor += SAMPLE_TEXTURE2D(_Splat3, sampler_Splat0, uvSplats[3]).rgb * weights[3];
    surfaceData.perceptualSmoothness += SAMPLE_TEXTURE2D(_Splat3, sampler_Splat0, uvSplats[3]).a * _Smoothness3 * weights[3];
    surfaceData.metallic += _Metallic3 * weights[3];
#endif
#if _LAYER_COUNT > 4
    surfaceData.baseColor += SAMPLE_TEXTURE2D(_Splat4, sampler_Splat0, uvSplats[4]).rgb * weights[4];
    surfaceData.perceptualSmoothness += SAMPLE_TEXTURE2D(_Splat4, sampler_Splat0, uvSplats[4]).a * _Smoothness4 * weights[4];
    surfaceData.metallic += _Metallic4 * weights[4];
#endif
#if _LAYER_COUNT > 5
    surfaceData.baseColor += SAMPLE_TEXTURE2D(_Splat5, sampler_Splat0, uvSplats[5]).rgb * weights[5];
    surfaceData.perceptualSmoothness += SAMPLE_TEXTURE2D(_Splat5, sampler_Splat0, uvSplats[5]).a * _Smoothness5 * weights[5];
    surfaceData.metallic += _Metallic5 * weights[5];
#endif
#if _LAYER_COUNT > 6
    surfaceData.baseColor += SAMPLE_TEXTURE2D(_Splat6, sampler_Splat0, uvSplats[6]).rgb * weights[6];
    surfaceData.perceptualSmoothness += SAMPLE_TEXTURE2D(_Splat6, sampler_Splat0, uvSplats[6]).a * _Smoothness6 * weights[6];
    surfaceData.metallic += _Metallic6 * weights[6];
#endif
#if _LAYER_COUNT > 7
    surfaceData.baseColor += SAMPLE_TEXTURE2D(_Splat7, sampler_Splat0, uvSplats[7]).rgb * weights[7];
    surfaceData.perceptualSmoothness += SAMPLE_TEXTURE2D(_Splat7, sampler_Splat0, uvSplats[7]).a * _Smoothness7 * weights[7];
    surfaceData.metallic += _Metallic7 * weights[7];
#endif

#if defined(_TERRAIN_NORMAL_MAP)
    UVMapping normalUV;
    normalUV.mappingType = UV_MAPPING_UVSET;
    #ifdef SURFACE_GRADIENT
        normalUV.tangentWS = input.worldToTangent[0];
        normalUV.bitangentWS = input.worldToTangent[1];
    #endif
    normalUV.uv = uvSplats[0];
    float3 normalTS = SAMPLE_UVMAPPING_NORMALMAP(_Normal0, sampler_Splat0, normalUV, 1) * weights[0];
    #if _LAYER_COUNT > 1
        normalUV.uv = uvSplats[1];
        normalTS += SAMPLE_UVMAPPING_NORMALMAP(_Normal1, sampler_Splat0, normalUV, 1) * weights[1];
    #endif
    #if _LAYER_COUNT > 2
        normalUV.uv = uvSplats[2];
        normalTS += SAMPLE_UVMAPPING_NORMALMAP(_Normal2, sampler_Splat0, normalUV, 1) * weights[2];
    #endif
    #if _LAYER_COUNT > 3
        normalUV.uv = uvSplats[3];
        normalTS += SAMPLE_UVMAPPING_NORMALMAP(_Normal3, sampler_Splat0, normalUV, 1) * weights[3];
    #endif
    #if _LAYER_COUNT > 4
        normalUV.uv = uvSplats[4];
        normalTS += SAMPLE_UVMAPPING_NORMALMAP(_Normal4, sampler_Splat0, normalUV, 1) * weights[4];
    #endif
    #if _LAYER_COUNT > 5
        normalUV.uv = uvSplats[5];
        normalTS += SAMPLE_UVMAPPING_NORMALMAP(_Normal5, sampler_Splat0, normalUV, 1) * weights[5];
    #endif
    #if _LAYER_COUNT > 6
        normalUV.uv = uvSplats[6];
        normalTS += SAMPLE_UVMAPPING_NORMALMAP(_Normal6, sampler_Splat0, normalUV, 1) * weights[6];
    #endif
    #if _LAYER_COUNT > 7
        normalUV.uv = uvSplats[7];
        normalTS += SAMPLE_UVMAPPING_NORMALMAP(_Normal7, sampler_Splat0, normalUV, 1) * weights[7];
    #endif
#elif defined(SURFACE_GRADIENT)
    float3 normalTS = float3(0.0, 0.0, 0.0); // No gradient
#else
    float3 normalTS = float3(0.0, 0.0, 1.0);
#endif

    surfaceData.ambientOcclusion = 1;
    surfaceData.tangentWS = normalize(input.worldToTangent[0].xyz); // The tangent is not normalize in worldToTangent for mikkt. Tag: SURFACE_GRADIENT
    surfaceData.subsurfaceMask = 0;
    surfaceData.thickness = 1;
    surfaceData.diffusionProfile = 0;

    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;

    // Init other parameters
    surfaceData.anisotropy = 0.0;
    surfaceData.specularColor = float3(0.0, 0.0, 0.0);
    surfaceData.coatMask = 0.0;
    surfaceData.iridescenceThickness = 0.0;
    surfaceData.iridescenceMask = 0.0;

    // Transparency parameters
    // Use thickness from SSS
    surfaceData.ior = 1.0;
    surfaceData.transmittanceColor = float3(1.0, 1.0, 1.0);
    surfaceData.atDistance = 1000000.0;
    surfaceData.transmittanceMask = 0.0;

    GetNormalWS(input, V, normalTS, surfaceData.normalWS);
    float3 bentNormalWS = surfaceData.normalWS;

    // By default we use the ambient occlusion with Tri-ace trick (apply outside) for specular occlusion.
//#if defined(_MASKMAP0) || defined(_MASKMAP1) || defined(_MASKMAP2) || defined(_MASKMAP3)
//    surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(dot(surfaceData.normalWS, V), surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
//#else
    surfaceData.specularOcclusion = 1.0;
//#endif

#ifndef _DISABLE_DBUFFER
    float alpha = 1;
    AddDecalContribution(posInput, surfaceData, alpha);
#endif

#if defined(DEBUG_DISPLAY)
    if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
    {
        surfaceData.baseColor = GetTextureDataDebug(_DebugMipMapMode, layerTexCoord.base0.uv, _BaseColorMap0, _BaseColorMap0_TexelSize, _BaseColorMap0_MipInfo, surfaceData.baseColor);
        surfaceData.metallic = 0;
    }
#endif

    GetBuiltinData(input, surfaceData, 1, bentNormalWS, 0, builtinData);
}
