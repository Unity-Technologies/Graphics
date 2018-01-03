#include "Decal.hlsl"

DECLARE_DBUFFER_TEXTURE(_DBufferTexture); 

void AddDecalContribution(uint2 unPositionSS, inout SurfaceData surfaceData)
{
	if(_EnableDBuffer)
	{
		FETCH_DBUFFER(DBuffer, _DBufferTexture, unPositionSS);
		DecalSurfaceData decalSurfaceData;
		DECODE_FROM_DBUFFER(DBuffer, decalSurfaceData);

		surfaceData.baseColor.xyz = lerp(surfaceData.baseColor.xyz, decalSurfaceData.baseColor.xyz, decalSurfaceData.baseColor.w);
		surfaceData.normalWS.xyz = normalize(lerp(surfaceData.normalWS.xyz, decalSurfaceData.normalWS.xyz, decalSurfaceData.normalWS.w));
		surfaceData.metallic = lerp(surfaceData.metallic, decalSurfaceData.mask.x, decalSurfaceData.mask.z);
		surfaceData.ambientOcclusion = lerp(surfaceData.ambientOcclusion, decalSurfaceData.mask.y, decalSurfaceData.mask.z);
		surfaceData.perceptualSmoothness = lerp(surfaceData.perceptualSmoothness, decalSurfaceData.mask.w, decalSurfaceData.mask.z);
	}
}
