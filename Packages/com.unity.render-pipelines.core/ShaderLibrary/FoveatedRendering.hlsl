#ifndef UNITY_FOVEATED_RENDERING_INCLUDED
#define UNITY_FOVEATED_RENDERING_INCLUDED

#if defined(_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
# if defined(SHADER_API_PS5)
#   include "Packages/com.unity.render-pipelines.ps5/ShaderLibrary/API/FoveatedRendering_PSSL.hlsl"
# endif
# if defined(SHADER_API_METAL)
#   include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/FoveatedRendering_Metal.hlsl"
# endif

// Adapt old remap functions to their new name
float2 RemapFoveatedRenderingResolve(float2 uv) { return RemapFoveatedRenderingLinearToNonUniform(uv); }
float2 RemapFoveatedRenderingPrevFrameResolve(float2 uv) {return RemapFoveatedRenderingPrevFrameLinearToNonUniform(uv); }
float2 RemapFoveatedRenderingDistort(float2 uv) { return RemapFoveatedRenderingNonUniformToLinear(uv); }
float2 RemapFoveatedRenderingPrevFrameDistort(float2 uv) { return RemapFoveatedRenderingPrevFrameNonUniformToLinear(uv); }
int2 RemapFoveatedRenderingDistortCS(int2 positionCS, bool yflip) { return RemapFoveatedRenderingNonUniformToLinearCS(positionCS, yflip); }

#endif

#endif // UNITY_FOVEATED_RENDERING_INCLUDED
