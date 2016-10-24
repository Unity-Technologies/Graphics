#ifndef UNITY_LIGHTING_INCLUDED
#define UNITY_LIGHTING_INCLUDED

// The lighting architecture is in charge to define the light loop
// It is also in charge to define the sampling function for shadowmap, ies, cookie and reflection 
// as only the lighting architecture is aware of the usage of texture atlas, array and format (latlong, 2D, cube)

#define LIGHTING // This define is used to know that we have include lighting when compiling material, else it will generate "default" function that are neutral to use Material.hlsl alone.

#ifdef SINGLE_PASS 
#include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/Lighting/SinglePass/SinglePass.hlsl"
//#elif ...
#endif

#include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/Material/Material.hlsl"

#ifdef SINGLE_PASS 
#include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/Lighting/SinglePass/SinglePassLoop.hlsl"
//#elif ...
#endif


#endif // UNITY_LIGHTING_INCLUDED
