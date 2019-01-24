//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/SampleUVMapping.hlsl"

void GetSurfaceData(FragInputs input, float3 V, PositionInputs posInput, out DecalSurfaceData surfaceData)
{
#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR)
    // With inspector version of decal we can use instancing to get normal to world access
    float4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
    float albedoMapBlend = clamp(normalToWorld[0][3], 0.0f, 1.0f);
    float2 scale = float2(normalToWorld[3][0], normalToWorld[3][1]);
    float2 offset = float2(normalToWorld[3][2], normalToWorld[3][3]);
	float2 texCoords = input.texCoord0.xy * scale + offset;
#elif (SHADERPASS == SHADERPASS_DBUFFER_MESH)
	float albedoMapBlend = _DecalBlend;
	float2 texCoords = input.texCoord0.xy;
#endif

    ZERO_INITIALIZE(DecalSurfaceData, surfaceData);
    surfaceData.baseColor = _BaseColor;

#ifdef _COLORMAP
    surfaceData.baseColor *= SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, texCoords);
#endif
	surfaceData.baseColor.w *= albedoMapBlend;
	albedoMapBlend = surfaceData.baseColor.w;   
// outside _COLORMAP because we still have base color
#ifdef _ALBEDOCONTRIBUTION
	surfaceData.HTileMask |= DBUFFERHTILEBIT_DIFFUSE;
#else
	surfaceData.baseColor.w = 0;	// dont blend any albedo
#endif

    // Default to _DecalBlend, if we use _NormalBlendSrc as maskmap and there is no maskmap, it mean we have 1
	float maskMapBlend = _DecalBlend;

#ifdef _MASKMAP
    surfaceData.mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, texCoords);
    surfaceData.mask.z *= _DecalMaskMapBlueScale;
	maskMapBlend *= surfaceData.mask.z;	// store before overwriting with smoothness
    surfaceData.mask.x = _MetallicScale * surfaceData.mask.x;
    surfaceData.mask.y = lerp(_AORemapMin, _AORemapMax, surfaceData.mask.y);
    surfaceData.mask.z = lerp(_SmoothnessRemapMin, _SmoothnessRemapMax, surfaceData.mask.w);
	surfaceData.HTileMask |= DBUFFERHTILEBIT_MASK;
	surfaceData.mask.w = _MaskBlendSrc ? maskMapBlend : albedoMapBlend;
#endif

	// needs to be after mask, because blend source could be in the mask map blue
#ifdef _NORMALMAP
	float3 normalTS = UnpackNormalmapRGorAG(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, texCoords));
#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR)
	float3 normalWS = mul((float3x3)normalToWorld, normalTS);
#elif (SHADERPASS == SHADERPASS_DBUFFER_MESH)	
    // We need to normalize as we use mikkt tangent space and this is expected (tangent space is not normalize)
    float3 normalWS = normalize(TransformTangentToWorld(normalTS, input.worldToTangent));
#endif
	surfaceData.normalWS.xyz = normalWS;
	surfaceData.HTileMask |= DBUFFERHTILEBIT_NORMAL;
	surfaceData.normalWS.w = _NormalBlendSrc ? maskMapBlend : albedoMapBlend;
#endif
	surfaceData.MAOSBlend.xy = float2(surfaceData.mask.w, surfaceData.mask.w);
}
