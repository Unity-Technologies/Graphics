#ifndef __FEATURE_FLAGS_H__
#define __FEATURE_FLAGS_H__

#include "TilePass.cs.hlsl"

uint FeatureFlagsToTileVariant(uint featureFlags)
{
	if(featureFlags & FEATURE_FLAG_AREA_LIGHT)
	{
		return 1;
	}
	else
	{
		return 0;
	}
}

uint TileVariantToFeatureFlags(uint variant)
{
	if(variant == 0)
	{
		return 0xFFFFFFFF & (~FEATURE_FLAG_AREA_LIGHT);
	}
	else if(variant == 1)
	{
		return 0xFFFFFFFF;
	}
	
}

#endif