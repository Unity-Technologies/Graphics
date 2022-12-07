#ifndef UNITY_COMMON_DEPRECATED_INCLUDED
#define UNITY_COMMON_DEPRECATED_INCLUDED

// Function that are in this file shouldn't be used. they are obsolete and could be removed in the future
// they are here to keep compatibility with previous version

// Please use void LODDitheringTransition(uint2 fadeMaskSeed, float ditherFactor)
void LODDitheringTransition(uint3 fadeMaskSeed, float ditherFactor)
{
    ditherFactor = ditherFactor < 0.0 ? 1 + ditherFactor : ditherFactor;

    float p = GenerateHashedRandomFloat(fadeMaskSeed);
    p = (ditherFactor >= 0.5) ? p : 1 - p;
    clip(ditherFactor - p);
}

#endif
