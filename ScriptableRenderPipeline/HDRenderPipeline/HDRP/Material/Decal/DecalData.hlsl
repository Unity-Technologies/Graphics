//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "CoreRP/ShaderLibrary/Packing.hlsl"
#include "CoreRP/ShaderLibrary/Sampling/SampleUVMapping.hlsl"

void GetSurfaceData(float2 texCoordDS, float4x4 decalToWorld, out DecalSurfaceData surfaceData)
{
	surfaceData.baseColor = float4(0,0,0,0);
	surfaceData.normalWS = float4(0,0,0,0);
	surfaceData.mask = float4(0,0,0,0);
	surfaceData.HTileMask = 0;
	float totalBlend = _DecalBlend * clamp(decalToWorld[0][3], 0.0f, 1.0f);
#if _COLORMAP
	surfaceData.baseColor = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, texCoordDS.xy);
	surfaceData.baseColor.w *= totalBlend;
	surfaceData.HTileMask |= DBUFFERHTILEBIT_DIFFUSE;
#endif
	UVMapping texCoord;
	ZERO_INITIALIZE(UVMapping, texCoord);
	texCoord.uv = texCoordDS.xy;
#if _NORMALMAP
	surfaceData.normalWS.xyz = mul((float3x3)decalToWorld, SAMPLE_UVMAPPING_NORMALMAP(_NormalMap, sampler_NormalMap, texCoord, 1)) * 0.5f + 0.5f;
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
