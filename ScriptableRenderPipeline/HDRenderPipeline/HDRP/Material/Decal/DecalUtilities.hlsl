#include "Decal.hlsl"

DECLARE_DBUFFER_TEXTURE(_DBufferTexture); 

void AddDecalContribution(uint2 unPositionSS, inout SurfaceData surfaceData)
{
    // Currently disabled
#if 0
	FETCH_DBUFFER(DBuffer, _DBufferTexture, unPositionSS);
	DecalSurfaceData decalSurfaceData;
	DECODE_FROM_DBUFFER(DBuffer, decalSurfaceData);

	surfaceData.baseColor.xyz = lerp(surfaceData.baseColor.xyz, decalSurfaceData.baseColor.xyz, decalSurfaceData.baseColor.w);
	surfaceData.normalWS.xyz = normalize(lerp(surfaceData.normalWS.xyz, decalSurfaceData.normalWS.xyz, decalSurfaceData.normalWS.w));
#endif
}
