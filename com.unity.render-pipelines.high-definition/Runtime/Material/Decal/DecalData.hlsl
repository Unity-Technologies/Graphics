//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/SampleUVMapping.hlsl"

void GetSurfaceData(FragInputs input, float3 V, PositionInputs posInput, float angleFadeFactor, out DecalSurfaceData surfaceData)
{
#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR)
    // With inspector version of decal we can use instancing to get normal to world access
    float4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
    float fadeFactor = clamp(normalToWorld[0][3], 0.0f, 1.0f) * angleFadeFactor;
    float2 scale = float2(normalToWorld[3][0], normalToWorld[3][1]);
    float2 offset = float2(normalToWorld[3][2], normalToWorld[3][3]);
	float2 texCoords = input.texCoord0.xy * scale + offset;
#elif (SHADERPASS == SHADERPASS_DBUFFER_MESH) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH)

    #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
    LODDitheringTransition(ComputeFadeMaskSeed(V, posInput.positionSS), unity_LODFade.x);
    #endif

	float fadeFactor = _DecalBlend.x;
	float2 texCoords = input.texCoord0.xy;
#endif

    float albedoMapBlend = fadeFactor;
    float maskMapBlend = _DecalMaskMapBlueScale * fadeFactor;

    ZERO_INITIALIZE(DecalSurfaceData, surfaceData);

#ifdef _MATERIAL_AFFECTS_EMISSION
    surfaceData.emissive = _EmissiveColor.rgb * fadeFactor;
    #ifdef _EMISSIVEMAP
    surfaceData.emissive *= SAMPLE_TEXTURE2D(_EmissiveColorMap, sampler_EmissiveColorMap, texCoords).rgb;
    #endif

    // Inverse pre-expose using _EmissiveExposureWeight weight
    float3 emissiveRcpExposure = surfaceData.emissive * GetInverseCurrentExposureMultiplier();
    surfaceData.emissive = lerp(emissiveRcpExposure, surfaceData.emissive, _EmissiveExposureWeight);
#endif // _MATERIAL_AFFECTS_EMISSION

    // Following code match the code in DecalUtilities.hlsl used for cluster. It have the same kind of condition and similar code structure
    surfaceData.baseColor = _BaseColor;
#ifdef _COLORMAP
    surfaceData.baseColor *= SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, texCoords);
 #endif
	surfaceData.baseColor.w *= fadeFactor;
    albedoMapBlend = surfaceData.baseColor.w;
    // outside _COLORMAP because we still have base color for albedoMapBlend
#ifndef _MATERIAL_AFFECTS_ALBEDO
	surfaceData.baseColor.w = 0.0;	// dont blend any albedo - Note: as we already do RT color masking this is not needed, albedo will not be affected anyway
#endif

    // In case of Smoothness / AO / Metal, all the three are always computed but color mask can change
    // Note: We always use a texture here as the decal atlas for transparent decal cluster only handle texture case
    // If no texture is assign it is the white texture
#ifdef _MATERIAL_AFFECTS_MASKMAP
    #ifdef _MASKMAP
    surfaceData.mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, texCoords);
	maskMapBlend *= surfaceData.mask.z;	// store before overwriting with smoothness
    #ifdef DECALS_4RT
    surfaceData.mask.x = lerp(_MetallicRemapMin, _MetallicRemapMax, surfaceData.mask.x);
    surfaceData.mask.y = lerp(_AORemapMin, _AORemapMax, surfaceData.mask.y);
    #endif
    surfaceData.mask.z = lerp(_SmoothnessRemapMin, _SmoothnessRemapMax, surfaceData.mask.w);
    #else
    #ifdef DECALS_4RT
    surfaceData.mask.x = _Metallic;
    surfaceData.mask.y = _AO;
    #endif
    surfaceData.mask.z = _Smoothness;
    #endif

	surfaceData.mask.w = _MaskBlendSrc ? maskMapBlend : albedoMapBlend;
#endif

	// needs to be after mask, because blend source could be in the mask map blue
    // Note: We always use a texture here as the decal atlas for transparent decal cluster only handle texture case
    // If no texture is assign it is the bump texture (0.0, 0.0, 1.0)
#ifdef _MATERIAL_AFFECTS_NORMAL

    #ifdef _NORMALMAP
	float3 normalTS = UnpackNormalmapRGorAG(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, texCoords));
    #else
    float3 normalTS = float3(0.0, 0.0, 1.0);
    #endif
    float3 normalWS = float3(0.0, 0.0, 0.0);

    #if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR)
	normalWS = mul((float3x3)normalToWorld, normalTS);
    #elif (SHADERPASS == SHADERPASS_DBUFFER_MESH)	
    // We need to normalize as we use mikkt tangent space and this is expected (tangent space is not normalize)
    normalWS = normalize(TransformTangentToWorld(normalTS, input.tangentToWorld));
    #endif

	surfaceData.normalWS.xyz = normalWS;
	surfaceData.normalWS.w = _NormalBlendSrc ? maskMapBlend : albedoMapBlend;

#endif

	surfaceData.MAOSBlend.xy = float2(surfaceData.mask.w, surfaceData.mask.w);
}
