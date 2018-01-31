#include "Decal.hlsl"

DECLARE_DBUFFER_TEXTURE(_DBufferTexture); 

void AddDecalContribution(uint2 unPositionSS, inout SurfaceData surfaceData)
{
	if(_EnableDBuffer)
	{
		// the code in the macros, gets moved inside the conditionals by the compiler
		FETCH_DBUFFER(DBuffer, _DBufferTexture, unPositionSS);
		DecalSurfaceData decalSurfaceData;
		DECODE_FROM_DBUFFER(DBuffer, decalSurfaceData);
		uint mask = UnpackByte(_DecalHTile[unPositionSS / 8]); 

		// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
		if(mask & DBUFFERHTILEBIT_DIFFUSE)
		{
			surfaceData.baseColor.xyz = surfaceData.baseColor.xyz * decalSurfaceData.baseColor.w + decalSurfaceData.baseColor.xyz;
		}
		if(mask & DBUFFERHTILEBIT_NORMAL)
		{
			surfaceData.normalWS.xyz = normalize(surfaceData.normalWS.xyz * decalSurfaceData.normalWS.w + decalSurfaceData.normalWS.xyz);
		}
		if(mask & DBUFFERHTILEBIT_MASK)
		{
			surfaceData.metallic = surfaceData.metallic * decalSurfaceData.mask.w + decalSurfaceData.mask.x;
			surfaceData.ambientOcclusion = surfaceData.ambientOcclusion * decalSurfaceData.mask.w + decalSurfaceData.mask.y;
			surfaceData.perceptualSmoothness = surfaceData.perceptualSmoothness * decalSurfaceData.mask.w + decalSurfaceData.mask.z;
		}
	}
}
