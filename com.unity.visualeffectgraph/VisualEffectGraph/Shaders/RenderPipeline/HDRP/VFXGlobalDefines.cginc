#if IS_TRANSPARENT_PARTICLE && !HDRP_LIT // Fog for opaque is handled in a dedicated pass
#define USE_FOG 1
#define VFX_NEEDS_POSWS_INTERPOLATOR 1
#endif