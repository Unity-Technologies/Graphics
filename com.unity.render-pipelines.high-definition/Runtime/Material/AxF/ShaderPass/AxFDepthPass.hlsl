#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

#ifdef _ALPHATEST_ON
    #define ATTRIBUTES_NEED_TEXCOORD0
    #define ATTRIBUTES_NEED_TEXCOORD1

    #define VARYINGS_NEED_POSITION_WS // Required to get view vector and to get planar/triplanar mapping working
    #define VARYINGS_NEED_TEXCOORD0
    #define VARYINGS_NEED_TEXCOORD1
    
#elif defined(LOD_FADE_CROSSFADE)
    #define VARYINGS_NEED_POSITION_WS // Required to get view vector use in cross fade effect 
#endif //..._ALPHATEST_ON

// This include will define the various Attributes/Varyings structure
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
