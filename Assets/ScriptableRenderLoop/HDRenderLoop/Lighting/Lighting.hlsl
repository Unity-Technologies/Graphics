#ifndef UNITY_LIGHTING_INCLUDED
#define UNITY_LIGHTING_INCLUDED

#include "CommonLighting.hlsl"
#include "CommonShadow.hlsl"
#include "Sampling.hlsl"
#include "AreaLighting.hlsl"
#include "ImageBasedLighting.hlsl"

// The light loop (or lighting architecture) is in charge to:
// - Define light list
// - Define the light loop
// - Setup the constant/data
// - Do the reflection hierarchy
// - Provide sampling function for shadowmap, ies, cookie and reflection (depends on the specific use with the light loops like index array or atlas or single and texture format (cubemap/latlong))

#define HAS_LIGHTLOOP // Allow to not define LightLoop related function in Material.hlsl

#include "Assets/ScriptableRenderLoop/HDRenderLoop/Lighting/LightDefinition.cs.hlsl"

#ifdef LIGHTLOOP_SINGLE_PASS 
#include "Assets/ScriptableRenderLoop/HDRenderLoop/Lighting/SinglePass/SinglePass.hlsl"
#endif

// Shadow use samling function define in header above and must be include before Material.hlsl
#include "Assets/ScriptableRenderLoop/HDRenderLoop/Shadow/Shadow.hlsl"
#include "Assets/ScriptableRenderLoop/HDRenderLoop/Material/Material.hlsl"

// LightLoop use evaluation BSDF function for light type define in Material.hlsl
#ifdef LIGHTLOOP_SINGLE_PASS 
#include "Assets/ScriptableRenderLoop/HDRenderLoop/Lighting/SinglePass/SinglePassLoop.hlsl"
#endif


#endif // UNITY_LIGHTING_INCLUDED
