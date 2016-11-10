//-----------------------------------------------------------------------------
// Configuration
//-----------------------------------------------------------------------------

// #define DIFFUSE_LAMBERT_BRDF
// #define USE_BSDF_PRE_LAMBDAV

// Note: C# define can't be reuse in another C# file and ideally we would like that these define are present both on C# and HLSL side... How to do that ?
// For now sync by hand within HDRenderLoop.cs file and this one
// TODO: Currently it is not yet possible to use this feature, we need to provide previousPositionCS to the vertex shader as part of Attribute for GBuffer pass
//#define VELOCITY_IN_GBUFFER