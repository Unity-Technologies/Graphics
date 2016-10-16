#ifndef UNITY_LIGHTING_INCLUDED
#define UNITY_LIGHTING_INCLUDED

// We need to define the macro used for env map evaluation based on the different architecture.
// Like for material we have one define by architecture.
// TODO: who setup the define for a given architecture ?

// For now our loop looks use texture arrays (but we should support different define based on architecture).
// NOTE: How do we support a pass with tiled forward and single forward in the same renderer  (i.e with tex array and single tex)
#define UNITY_DECLARE_ENV(tex) UNITY_DECLARE_TEXCUBEARRAY(tex)
#define UNITY_ARGS_ENV(tex) UNITY_ARGS_TEXCUBEARRAY(tex)
#define UNITY_PASS_ENV(tex) UNITY_PASS_TEXCUBEARRAY(tex)
#define UNITY_SAMPLE_ENV_LOD(tex, coord, lightData, lod) UNITY_SAMPLE_TEXCUBEARRAY_LOD(tex, float4(coord, lightData.sliceIndex), lod)

#include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/Material/Material.hlsl"

#include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/Lighting/LightingForward/LightingForward.hlsl"

#endif // UNITY_LIGHTING_INCLUDED
