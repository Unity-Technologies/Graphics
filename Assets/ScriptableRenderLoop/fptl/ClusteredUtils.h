#ifndef __CLUSTEREDUTILS_H__
#define __CLUSTEREDUTILS_H__


int SnapToClusterIdx(float z_in, float fModulUserScale)
{
#ifdef LEFT_HAND_COORDINATES
	float z = z_in;
#else
	float z = -z_in;
#endif

	float userscale = g_fClustScale;
#ifdef ENABLE_DEPTH_TEXTURE_BACKPLANE
	userscale *= fModulUserScale;
#endif

	// using the inverse of the geometric series
	const float dist = max(0, z-g_fNearPlane);
	return (int) clamp( log2(dist*userscale*(g_fClustBase-1.0f) + 1) / log2(g_fClustBase), 0.0, (float) ((1<<g_iLog2NumClusters)-1) );
}

float ClusterIdxToZ(int k, float fModulUserScale)
{
	float res;

	float userscale = g_fClustScale;
#ifdef ENABLE_DEPTH_TEXTURE_BACKPLANE
	userscale *= fModulUserScale;
#endif

	float dist = (pow(g_fClustBase,(float) k)-1.0)/(userscale*(g_fClustBase-1.0f));
	res = dist+g_fNearPlane;

#ifdef LEFT_HAND_COORDINATES
	return res;
#else
	return -res;
#endif
}


#endif