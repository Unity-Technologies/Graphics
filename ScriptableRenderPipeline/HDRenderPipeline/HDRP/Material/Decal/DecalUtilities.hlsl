#include "Decal.hlsl"

DECLARE_DBUFFER_TEXTURE(_DBufferTexture); 

void AddDecalContribution(uint2 unPositionSS, inout SurfaceData surfaceData)
{
	if(_EnableDBuffer)
	{
		FETCH_DBUFFER(DBuffer, _DBufferTexture, unPositionSS);
		DecalSurfaceData decalSurfaceData;
		DECODE_FROM_DBUFFER(DBuffer, decalSurfaceData);
		uint mask = UnpackByte(_DecalHTile[unPositionSS / 8]); 
		// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html

		if(mask & 1)
		{
			surfaceData.baseColor.xyz = surfaceData.baseColor.xyz * decalSurfaceData.baseColor.w + decalSurfaceData.baseColor.xyz;
		}
		if(mask & 2)
		{
			surfaceData.normalWS.xyz = normalize(surfaceData.normalWS.xyz * decalSurfaceData.normalWS.w + decalSurfaceData.normalWS.xyz);
		}
		if(mask & 4)
		{
			surfaceData.metallic = surfaceData.metallic * decalSurfaceData.mask.w + decalSurfaceData.mask.x;
			surfaceData.ambientOcclusion = surfaceData.ambientOcclusion * decalSurfaceData.mask.w + decalSurfaceData.mask.y;
			surfaceData.perceptualSmoothness = surfaceData.perceptualSmoothness * decalSurfaceData.mask.w + decalSurfaceData.mask.z;
		}
	}
}
