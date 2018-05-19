//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "CoreRP/ShaderLibrary/Packing.hlsl"
#include "CoreRP/ShaderLibrary/Sampling/SampleUVMapping.hlsl"

void GetSurfaceData(float2 texCoordDS, float4x4 normalToWorld, out DecalSurfaceData surfaceData)
{
	surfaceData.baseColor = float4(0,0,0,0);
	surfaceData.normalWS = float4(0,0,0,0);
	surfaceData.mask = float4(0,0,0,0);
	surfaceData.HTileMask = 0;
	float totalBlend = clamp(normalToWorld[0][3], 0.0f, 1.0f);
	float2 scale = float2(normalToWorld[3][0], normalToWorld[3][1]);
	float2 offset = float2(normalToWorld[3][2], normalToWorld[3][3]);
	texCoordDS = texCoordDS * scale + offset;
#if _COLORMAP
	surfaceData.baseColor = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, texCoordDS.xy);
	surfaceData.baseColor.w *= totalBlend;
	totalBlend = surfaceData.baseColor.w;	// base alpha affects all other channels;
	surfaceData.HTileMask |= DBUFFERHTILEBIT_DIFFUSE;
#endif
#if _NORMALMAP
	surfaceData.normalWS.xyz = mul((float3x3)normalToWorld, UnpackNormalmapRGorAG(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, texCoordDS))) * 0.5f + 0.5f;
	surfaceData.normalWS.w = totalBlend;
	surfaceData.HTileMask |= DBUFFERHTILEBIT_NORMAL;
#endif
#if _MASKMAP
	surfaceData.mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, texCoordDS.xy); 
	surfaceData.mask.z = surfaceData.mask.w;
	surfaceData.mask.w = totalBlend;
	surfaceData.HTileMask |= DBUFFERHTILEBIT_MASK;
#endif
}
