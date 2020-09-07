#ifndef UNIVERSAL_DEPRECATED_INCLUDED
#define UNIVERSAL_DEPRECATED_INCLUDED

// Stereo-related bits
#define SCREENSPACE_TEXTURE         TEXTURE2D_X
#define SCREENSPACE_TEXTURE_FLOAT   TEXTURE2D_X_FLOAT
#define SCREENSPACE_TEXTURE_HALF    TEXTURE2D_X_HALF

// Typo-fixes, re-route to new name for backwards compatiblity (if there are external dependencies).
#define kDieletricSpec kDielectricSpec
#define DirectBDRF     DirectBRDF

// Deprecated: not using consistent naming convention
#if defined(USING_STEREO_MATRICES)
#define unity_StereoMatrixIP unity_StereoMatrixInvP
#define unity_StereoMatrixIVP unity_StereoMatrixInvVP
#endif

// Previously used when rendering with DrawObjectsPass.
// Global object render pass data containing various settings.
// x,y,z are currently unused
// w is used for knowing whether the object is opaque(1) or alpha blended(0)
half4 _DrawObjectPassData;

#endif // UNIVERSAL_DEPRECATED_INCLUDED
