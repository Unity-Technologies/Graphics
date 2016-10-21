#ifndef UNITY_LIGHTING_INCLUDED
#define UNITY_LIGHTING_INCLUDED

// We need to define the macro used for env map evaluation based on the different architecture.
// Like for material we have one define by architecture.
// TODO: who setup the define for a given architecture ?

#include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/Material/Material.hlsl"

#include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/Lighting/LightingForward/LightingForward.hlsl"

#endif // UNITY_LIGHTING_INCLUDED
