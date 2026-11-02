#ifndef UNIVERSAL_LIT_INPUT_DEPRECATED_INCLUDED
#define UNIVERSAL_LIT_INPUT_DEPRECATED_INCLUDED

// Deprecated helpers, kept for external back-compat.

// Trunk's LitInput.hlsl transitively provided the SurfaceInput helpers (Alpha,
// SampleAlbedoAlpha, SampleNormal, SampleEmission); custom shaders built on copies of
// LitInput rely on them.
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInputFunctions.deprecated.hlsl"

// Deprecated. Sample the metallic/specular gloss map through SampleMetallicSpecGloss instead.
#define SAMPLE_METALLICSPECULAR(uv) (IsSpecularSetup() ? SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, uv) : SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv))

#endif // UNIVERSAL_LIT_INPUT_DEPRECATED_INCLUDED
