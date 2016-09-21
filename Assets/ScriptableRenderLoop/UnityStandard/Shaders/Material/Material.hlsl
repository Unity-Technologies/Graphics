#ifndef UNITY_MATERIAL_INCLUDED
#define UNITY_MATERIAL_INCLUDED

#include "../../ShaderLibrary/Packing.hlsl"
#include "../../ShaderLibrary/BSDF.hlsl"
#include "../../ShaderLibrary/CommonLighting.hlsl"

#include "../LightDefinition.cs"
#include "CommonMaterial.hlsl"

// Here we include all the different lighting model supported by the renderloop based on define done in .shader
#ifdef UNITY_MATERIAL_DISNEYGXX
#include "DisneyGGX.hlsl"
#endif

#endif // UNITY_MATERIAL_INCLUDED