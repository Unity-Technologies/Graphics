#ifndef WATER_TILE_CLASSIFICATION_H
#define WATER_TILE_CLASSIFICATION_H

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.cs.hlsl"

#define NUM_WATER_VARIANTS 5

// Combination need to be define in increasing "comlexity" order as define by FeatureFlagsToTileVariant
static const uint kWaterFeatureVariantFlags[NUM_WATER_VARIANTS + 1] =
{
    // Default lighting
    /*  0 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_SSREFLECTION | LIGHTFEATUREFLAGS_SSREFRACTION,
    // Default lighting + Punctual
    /*  1 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_SSREFLECTION | LIGHTFEATUREFLAGS_SSREFRACTION | LIGHTFEATUREFLAGS_PUNCTUAL,
    // Default lighting + Env
    /*  2 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_SSREFLECTION | LIGHTFEATUREFLAGS_SSREFRACTION | LIGHTFEATUREFLAGS_ENV,
    // Default lighting + Punctual + Env
    /*  3 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_SSREFLECTION | LIGHTFEATUREFLAGS_SSREFRACTION | LIGHTFEATUREFLAGS_PUNCTUAL | LIGHTFEATUREFLAGS_ENV,
    // Default lighting + Punctual + Env + Area
    /*  4 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_SSREFLECTION | LIGHTFEATUREFLAGS_SSREFRACTION | LIGHTFEATUREFLAGS_PUNCTUAL | LIGHTFEATUREFLAGS_ENV | LIGHTFEATUREFLAGS_AREA,
    // Used for indirect dispatches for all water pixels
    /* 5 */ 0
};

uint FeatureFlagsToTileVariant_Water(uint featureFlags)
{
    for (int i = 0; i < NUM_WATER_VARIANTS; i++)
    {
        if ((featureFlags & kWaterFeatureVariantFlags[i]) == featureFlags)
            return i;
    }
    return NUM_WATER_VARIANTS - 1;
}

#endif //
