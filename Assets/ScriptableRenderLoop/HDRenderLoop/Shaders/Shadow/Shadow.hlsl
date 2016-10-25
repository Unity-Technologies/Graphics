// TODO: 
// - How to support a Gather sampling with such abstraction ?
// - What's belong to shadow and what's belong to renderloop ? (shadowmap size depends on the usage of atlas or not)
// - Is PunctualShadowData fixed or customizable ? Who is the owner ? Should it be pass to GetPunctualShadowAttenuation ? Sure it should...
// - Could be return by GetShadowTextureCoordinate() and pass to GetPunctualShadowAttenuation(). But in this case, who control the atlas application ? 
// TODO: 
// Caution: formula doesn't work as we are texture atlas...
// if (max3(abs(NDC.x), abs(NDC.y), 1.0f - texCoordXYZ.z) <= 1.0f) return 1.0;

#ifdef SHADOWFILTERING_FIXED_SIZE_PCF
#include "FixedSizePCF/FixedSizePCF.hlsl"
#endif

// GetPunctualShadowAttenuation is the "default" algorithm use for punctual light, material can call explicitely a particular algorithm if required (in this case users must ensure that the algorithm is present in the project).
float GetPunctualShadowAttenuation(LightLoopContext lightLoopContext, int index, PunctualShadowData shadowData, float3 shadowCoord, float3 shadowPosDX, float3 shadowPosDY, float2 unPositionSS)
{
#ifdef SHADOWFILTERING_FIXED_SIZE_PCF
    return GetShadowAttenuationFixedSizePCF(lightLoopContext, index, shadowData, shadowCoord, shadowPosDX, shadowPosDY, unPositionSS);
#else
    return 1.0;
#endif
}
