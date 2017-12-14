#include "Decal.hlsl"

DECLARE_DBUFFER_TEXTURE(_DBufferTexture); 

void AddDecalContribution(uint2 unPositionSS, inout SurfaceData surfaceData)
{
    // Currently disabled
#if 1
	FETCH_DBUFFER(DBuffer, _DBufferTexture, unPositionSS);
	DecalSurfaceData decalSurfaceData;
	DECODE_FROM_DBUFFER(DBuffer, decalSurfaceData);

	surfaceData.baseColor.xyz = lerp(surfaceData.baseColor.xyz, decalSurfaceData.baseColor.xyz, decalSurfaceData.baseColor.w);
	surfaceData.normalWS.xyz = normalize(lerp(surfaceData.normalWS.xyz, decalSurfaceData.normalWS.xyz, decalSurfaceData.normalWS.w));
	surfaceData.metallic = lerp(surfaceData.metallic, decalSurfaceData.MSH.x, decalSurfaceData.MSH.w);
	surfaceData.perceptualSmoothness = lerp(surfaceData.perceptualSmoothness, decalSurfaceData.MSH.y, decalSurfaceData.MSH.w);
#endif
}
