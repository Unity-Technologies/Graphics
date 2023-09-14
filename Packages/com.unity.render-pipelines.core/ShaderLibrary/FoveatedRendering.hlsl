#ifndef UNITY_FOVEATED_RENDERING_INCLUDED
#define UNITY_FOVEATED_RENDERING_INCLUDED

#if (!defined(UNITY_COMPILER_DXC) && (defined(UNITY_PLATFORM_OSX) || defined(UNITY_PLATFORM_IOS))) || defined(SHADER_API_PS5)

    #if defined(SHADER_API_PS5) || defined(SHADER_API_METAL)

        #define SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER 1

        #if defined(SHADER_API_PS5)
            #include "Packages/com.unity.render-pipelines.ps5/ShaderLibrary/API/FoveatedRendering_PSSL.hlsl"
        #endif

        #if defined(SHADER_API_METAL)
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/FoveatedRendering_Metal.hlsl"
        #endif

    #endif

#endif

#endif // UNITY_FOVEATED_RENDERING_INCLUDED
