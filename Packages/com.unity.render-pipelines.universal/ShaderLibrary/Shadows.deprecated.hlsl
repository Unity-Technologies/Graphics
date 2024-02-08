#ifndef UNIVERSAL_SHADOWS_DEPRECATED_INCLUDED
#define UNIVERSAL_SHADOWS_DEPRECATED_INCLUDED

// Deprecated: Reduce the number of unique samplers by using inline samplers instead.
// Some graphics APIs support only a low number of unique active samplers.
#define sampler_ScreenSpaceShadowmapTexture sampler_PointClamp
#define sampler_MainLightShadowmapTexture sampler_LinearClampCompare
#define sampler_AdditionalLightsShadowmapTexture sampler_LinearClampCompare

#endif
