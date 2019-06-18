#ifndef UNITY_TEXTUREXR_INCLUDED
#define UNITY_TEXTUREXR_INCLUDED

// single-pass instancing is the default VR method for HDRP
// multi-pass is working but not recommended due to lower performance
// multi-view is not yet supported
// single-pass doule-wide is deprecated

// XRTODO: refactor this with UnityInstancing.hlsl and sync with LWRP
// XRTODO: update supported platforms based on Unity version (for required C++ fixes)

// XRTODO: consolidate with TextureXR.cs
#define XR_MAX_VIEWS 2

// Must be in sync with C# with property useTexArray in TextureXR.cs
#if (defined(SHADER_API_D3D11) && !defined(SHADER_API_XBOXONE)) || defined(SHADER_API_PSSL) || defined(SHADER_API_VULKAN)
    #define UNITY_TEXTURE2D_X_ARRAY_SUPPORTED
#endif

// Validate supported platforms
#if defined(STEREO_INSTANCING_ON) && !defined(UNITY_TEXTURE2D_X_ARRAY_SUPPORTED)
    #error Single-pass stereo instancing is not supported on this platform (see UNITY_TEXTURE2D_X_ARRAY_SUPPORTED).
#endif

#if defined(UNITY_SINGLE_PASS_STEREO)
    #error Single-pass (double-wide) is not compatible with HDRP.
#endif

// Control if TEXTURE2D_X macros will expand to texture arrays
#if defined(UNITY_TEXTURE2D_X_ARRAY_SUPPORTED) && !defined(DISABLE_TEXTURE2D_X_ARRAY)
    #define USE_TEXTURE2D_X_AS_ARRAY
#endif

// Early defines for single-pass stereo instancing
#if defined(STEREO_INSTANCING_ON) && defined(UNITY_TEXTURE2D_X_ARRAY_SUPPORTED)
    #define UNITY_STEREO_INSTANCING_ENABLED
#endif

// Workaround for lack of multi compile in compute shaders
#if defined(SHADER_STAGE_COMPUTE) && defined(UNITY_TEXTURE2D_X_ARRAY_SUPPORTED)
    #define UNITY_STEREO_INSTANCING_ENABLED
#endif

// Define to override default rendering matrices (used mostly in ShaderVariables.hlsl)
#if defined(UNITY_STEREO_INSTANCING_ENABLED)
    #define USING_STEREO_MATRICES
#endif

// Helper macros to handle XR instancing with Texture2DArray
// With single-pass stereo instancing, unity_StereoEyeIndex is used to select the eye in the current context.
// Otherwise, the index is statically set to 0
#if defined(USE_TEXTURE2D_X_AS_ARRAY)

    // Only single-pass stereo instancing used array indexing
    #if defined(UNITY_STEREO_INSTANCING_ENABLED)
        #define SLICE_ARRAY_INDEX   unity_StereoEyeIndex
    #else
        #define SLICE_ARRAY_INDEX  0
    #endif

    #define TEXTURE2D_X                                                      TEXTURE2D_ARRAY
    #define TEXTURE2D_X_PARAM                                                TEXTURE2D_ARRAY_PARAM
    #define TEXTURE2D_X_ARGS                                                 TEXTURE2D_ARRAY_ARGS
    #define TEXTURE2D_X_HALF                                                 TEXTURE2D_ARRAY_HALF
    #define TEXTURE2D_X_FLOAT                                                TEXTURE2D_ARRAY_FLOAT
    #define TEXTURE2D_X_UINT(textureName)                                    Texture2DArray<uint> textureName
    #define TEXTURE2D_X_MSAA(type, textureName)                              Texture2DMSArray<type> textureName

    #define RW_TEXTURE2D_X(type, textureName)                                RW_TEXTURE2D_ARRAY(type, textureName)
    #define COORD_TEXTURE2D_X(pixelCoord)                                    uint3(pixelCoord, SLICE_ARRAY_INDEX)
    #define LOAD_TEXTURE2D_X(textureName, unCoord2)                          LOAD_TEXTURE2D_ARRAY(textureName, unCoord2, SLICE_ARRAY_INDEX)
    #define LOAD_TEXTURE2D_X_MSAA(textureName, unCoord2, sampleIndex)        LOAD_TEXTURE2D_ARRAY_MSAA(textureName, unCoord2, SLICE_ARRAY_INDEX, sampleIndex)
    #define LOAD_TEXTURE2D_X_LOD(textureName, unCoord2, lod)                 LOAD_TEXTURE2D_ARRAY_LOD(textureName, unCoord2, SLICE_ARRAY_INDEX, lod)
    #define SAMPLE_TEXTURE2D_X(textureName, samplerName, coord2)             SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, coord2, SLICE_ARRAY_INDEX)
    #define SAMPLE_TEXTURE2D_X_LOD(textureName, samplerName, coord2, lod)    SAMPLE_TEXTURE2D_ARRAY_LOD(textureName, samplerName, coord2, SLICE_ARRAY_INDEX, lod)
    #define GATHER_TEXTURE2D_X(textureName, samplerName, coord2)             GATHER_TEXTURE2D_ARRAY(textureName, samplerName, coord2, SLICE_ARRAY_INDEX)
    #define GATHER_RED_TEXTURE2D_X(textureName, samplerName, coord2)         GATHER_RED_TEXTURE2D(textureName, samplerName, float3(coord2, SLICE_ARRAY_INDEX))
    #define GATHER_GREEN_TEXTURE2D_X(textureName, samplerName, coord2)       GATHER_GREEN_TEXTURE2D(textureName, samplerName, float3(coord2, SLICE_ARRAY_INDEX))
#else
    #define SLICE_ARRAY_INDEX                                                0

    #define TEXTURE2D_X                                                      TEXTURE2D
    #define TEXTURE2D_X_PARAM                                                TEXTURE2D_PARAM
    #define TEXTURE2D_X_ARGS                                                 TEXTURE2D_ARGS
    #define TEXTURE2D_X_HALF                                                 TEXTURE2D_HALF
    #define TEXTURE2D_X_FLOAT                                                TEXTURE2D_FLOAT
    #define TEXTURE2D_X_UINT(textureName)                                    Texture2D<uint> textureName
    #define TEXTURE2D_X_MSAA(type, textureName)                              Texture2DMS<type> textureName

    #define RW_TEXTURE2D_X                                                   RW_TEXTURE2D
    #define COORD_TEXTURE2D_X(pixelCoord)                                    pixelCoord
    #define LOAD_TEXTURE2D_X                                                 LOAD_TEXTURE2D
    #define LOAD_TEXTURE2D_X_MSAA                                            LOAD_TEXTURE2D_MSAA
    #define LOAD_TEXTURE2D_X_LOD                                             LOAD_TEXTURE2D_LOD
    #define SAMPLE_TEXTURE2D_X                                               SAMPLE_TEXTURE2D
    #define SAMPLE_TEXTURE2D_X_LOD                                           SAMPLE_TEXTURE2D_LOD
    #define GATHER_TEXTURE2D_X                                               GATHER_TEXTURE2D
    #define GATHER_RED_TEXTURE2D_X                                           GATHER_RED_TEXTURE2D
    #define GATHER_GREEN_TEXTURE2D_X                                         GATHER_GREEN_TEXTURE2D
#endif

// see Unity\Shaders\Includes\UnityShaderVariables.cginc for impl used by the C++ renderer
#if defined(USING_STEREO_MATRICES) && defined(UNITY_STEREO_INSTANCING_ENABLED)
    static uint unity_StereoEyeIndex;
#else
    #define unity_StereoEyeIndex 0
#endif

// Helper macro to assign eye index during compute pass (usually from SV_DispatchThreadID)
#if defined(SHADER_STAGE_COMPUTE)
    #if defined(UNITY_STEREO_INSTANCING_ENABLED)
        #define UNITY_STEREO_ASSIGN_COMPUTE_EYE_INDEX(eyeIndex) unity_StereoEyeIndex = eyeIndex;
    #else
        #define UNITY_STEREO_ASSIGN_COMPUTE_EYE_INDEX(eyeIndex)
    #endif
#endif

#endif // UNITY_TEXTUREXR_INCLUDED
