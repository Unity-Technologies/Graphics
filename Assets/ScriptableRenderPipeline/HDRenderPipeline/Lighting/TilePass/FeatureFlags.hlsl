#ifndef __FEATURE_FLAGS_H__
#define __FEATURE_FLAGS_H__

#include "TilePass.cs.hlsl"

static const uint FeatureVariantFlags[NUM_FEATURE_VARIANTS] =
{
/* 0 */ 0,
/* 1 */ FEATURE_FLAG_SKY_LIGHT | FEATURE_FLAG_DIRECTIONAL_LIGHT,
/* 2 */ FEATURE_FLAG_SKY_LIGHT | FEATURE_FLAG_DIRECTIONAL_LIGHT | FEATURE_FLAG_ENV_LIGHT,
/* 3 */ FEATURE_FLAG_SKY_LIGHT | FEATURE_FLAG_DIRECTIONAL_LIGHT | FEATURE_FLAG_PUNCTUAL_LIGHT,
/* 3 */ FEATURE_FLAG_SKY_LIGHT | FEATURE_FLAG_DIRECTIONAL_LIGHT | FEATURE_FLAG_PUNCTUAL_LIGHT | FEATURE_FLAG_ENV_LIGHT,
/* 5 */ FEATURE_FLAG_SKY_LIGHT | FEATURE_FLAG_DIRECTIONAL_LIGHT | FEATURE_FLAG_AREA_LIGHT,
/* 6 */ FEATURE_FLAG_SKY_LIGHT | FEATURE_FLAG_DIRECTIONAL_LIGHT | FEATURE_FLAG_AREA_LIGHT | FEATURE_FLAG_PUNCTUAL_LIGHT | FEATURE_FLAG_ENV_LIGHT,
/* 7 */ 0xFFFFFFFF,
};

/*
public static uint FEATURE_FLAG_PUNCTUAL_LIGHT = 1<<0;
public static uint FEATURE_FLAG_AREA_LIGHT = 1<<1;
public static uint FEATURE_FLAG_DIRECTIONAL_LIGHT = 1<<2;
public static uint FEATURE_FLAG_ENV_LIGHT = 1<<3;
public static uint FEATURE_FLAG_SKY_LIGHT = 1<<4;
*/

uint FeatureFlagsToTileVariant(uint featureFlags)
{
    for(int i = 0; i < NUM_FEATURE_VARIANTS; i++)
    {
        if((featureFlags & FeatureVariantFlags[i]) == featureFlags)
            return i;
    }
    return NUM_FEATURE_VARIANTS - 1;
}

uint TileVariantToFeatureFlags(uint variant)
{
    return FeatureVariantFlags[variant];
}

#endif
