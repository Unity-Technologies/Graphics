//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "CoreRP/ShaderLibrary/Packing.hlsl"
#include "CoreRP/ShaderLibrary/Sampling/SampleUVMapping.hlsl"

void GetSurfaceData(float2 texCoordDS, float3x3 decalToWorld, out DecalSurfaceData surfaceData)
{
	surfaceData.baseColor = float4(0,0,0,0);
	surfaceData.normalWS = float4(0,0,0,0);
	surfaceData.mask = float4(0,0,0,0);
	float totalBlend = _DecalBlend;
#if _COLORMAP
	surfaceData.baseColor = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, texCoordDS.xy);
	surfaceData.baseColor.w *= totalBlend;
#endif
	UVMapping texCoord;
	ZERO_INITIALIZE(UVMapping, texCoord);
	texCoord.uv = texCoordDS.xy;
#if _NORMALMAP
	surfaceData.normalWS.xyz = mul(decalToWorld, SAMPLE_UVMAPPING_NORMALMAP(_NormalMap, sampler_NormalMap, texCoord, 1)) * 0.5f + 0.5f;
	surfaceData.normalWS.w = totalBlend;
#endif
#if _MASKMAP
	surfaceData.mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, texCoordDS.xy); 
	surfaceData.mask.z = surfaceData.mask.w;
	surfaceData.mask.w = totalBlend;
#endif

}
