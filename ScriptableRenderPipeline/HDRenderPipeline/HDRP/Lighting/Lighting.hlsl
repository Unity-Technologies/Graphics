#ifndef UNITY_LIGHTING_INCLUDED
#define UNITY_LIGHTING_INCLUDED

#include "CoreRP/ShaderLibrary/CommonLighting.hlsl"
#include "CoreRP/ShaderLibrary/CommonShadow.hlsl"
#include "CoreRP/ShaderLibrary/Sampling/Sampling.hlsl"
#include "CoreRP/ShaderLibrary/AreaLighting.hlsl"
#include "CoreRP/ShaderLibrary/ImageBasedLighting.hlsl"

// The light loop (or lighting architecture) is in charge to:
// - Define light list
// - Define the light loop
// - Setup the constant/data
// - Do the reflection hierarchy
// - Provide sampling function for shadowmap, ies, cookie and reflection (depends on the specific use with the light loops like index array or atlas or single and texture format (cubemap/latlong))

#define HAS_LIGHTLOOP // Allow to not define LightLoop related function in Material.hlsl

#include "../Lighting/LightDefinition.cs.hlsl"
#include "../Lighting/LightUtilities.hlsl"

#include "LightLoop/Shadow.hlsl"

#if defined(LIGHTLOOP_SINGLE_PASS) || defined(LIGHTLOOP_TILE_PASS)
#include "../Lighting/LightLoop/LightLoopDef.hlsl"
#endif

#include "../Material/Material.hlsl" // Depends on LightLoopDef and shadows

// Volumetrics have their own light loop.
#ifndef UNITY_MATERIAL_VOLUMETRIC
	// LightLoop use evaluation BSDF function for light type define in Material.hlsl
	#if defined(LIGHTLOOP_SINGLE_PASS) || defined(LIGHTLOOP_TILE_PASS)
	#include "../Lighting/LightLoop/LightLoop.hlsl"
	#endif
#endif

#endif // UNITY_LIGHTING_INCLUDED
