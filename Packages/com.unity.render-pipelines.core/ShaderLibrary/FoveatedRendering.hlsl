#ifndef UNITY_FOVEATED_RENDERING_INCLUDED
#define UNITY_FOVEATED_RENDERING_INCLUDED

#if defined(_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
# if defined(SHADER_API_PS5)
#   include "Packages/com.unity.render-pipelines.ps5/ShaderLibrary/API/FoveatedRendering_PSSL.hlsl"
# endif
# if defined(SHADER_API_METAL)
#   include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/FoveatedRendering_Metal.hlsl"
# endif
#endif

#endif // UNITY_FOVEATED_RENDERING_INCLUDED
