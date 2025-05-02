#ifndef UNITY_TEXTUREXR_INCLUDED
#define UNITY_TEXTUREXR_INCLUDED

// single-pass instancing is the default VR method for SRPs
// multi-pass is working but not recommended due to lower performance
// single-pass multi-view is not yet supported
// single-pass doule-wide is deprecated

// Must be in sync with C# with property useTexArray in TextureXR.cs
#if (defined(SHADER_API_D3D11) && !defined(SHADER_API_XBOXONE) && !defined(SHADER_API_GAMECORE)) || defined(SHADER_API_PSSL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_METAL)
    #define UNITY_TEXTURE2D_X_ARRAY_SUPPORTED
#endif

// Validate supported platforms
#if defined(STEREO_INSTANCING_ON) && !defined(UNITY_TEXTURE2D_X_ARRAY_SUPPORTED)
    #error Single-pass instancing is not supported on this platform (see UNITY_TEXTURE2D_X_ARRAY_SUPPORTED).
#endif

#if defined(UNITY_SINGLE_PASS_STEREO)
    #error Single-pass (double-wide) is not compatible with TextureXR.hlsl.
#endif

// Control if TEXTURE2D_X macros will expand to texture arrays
#if defined(UNITY_TEXTURE2D_X_ARRAY_SUPPORTED) && !defined(DISABLE_TEXTURE2D_X_ARRAY)
    #define USE_TEXTURE2D_X_AS_ARRAY
#endif

// Early defines for single-pass instancing
#if defined(STEREO_INSTANCING_ON) && defined(UNITY_TEXTURE2D_X_ARRAY_SUPPORTED)
    #define UNITY_STEREO_INSTANCING_ENABLED
#endif

// Workaround for lack of multi compile in compute/ray shaders
#if defined(UNITY_TEXTURE2D_X_ARRAY_SUPPORTED) && (defined(SHADER_STAGE_COMPUTE) || defined(SHADER_STAGE_RAY_TRACING))
    #define UNITY_STEREO_INSTANCING_ENABLED
#endif

// Define to override default rendering matrices
#if defined(UNITY_STEREO_INSTANCING_ENABLED)
    #define USING_STEREO_MATRICES
#endif

// Helper macros to handle XR single-pass with Texture2DArray
// With single-pass instancing, unity_StereoEyeIndex is used to select the eye in the current context.
// Otherwise, the index is statically set to 0
#if defined(USE_TEXTURE2D_X_AS_ARRAY)

    // Only single-pass stereo instancing used array indexing
    #if defined(UNITY_STEREO_INSTANCING_ENABLED)
        #define SLICE_ARRAY_INDEX   unity_StereoEyeIndex
    #else
        #define SLICE_ARRAY_INDEX  0
    #endif

    #define COORD_TEXTURE2D_X(pixelCoord)                                    uint3(pixelCoord, SLICE_ARRAY_INDEX)
    #define INDEX_TEXTURE2D_ARRAY_X(slot)                                    ((slot) * _XRViewCount + SLICE_ARRAY_INDEX)

    #define TEXTURE2D_X                                                      TEXTURE2D_ARRAY
    #define TEXTURE2D_X_PARAM                                                TEXTURE2D_ARRAY_PARAM
    #define TEXTURE2D_X_ARGS                                                 TEXTURE2D_ARRAY_ARGS
    #define TEXTURE2D_X_HALF                                                 TEXTURE2D_ARRAY_HALF
    #define TEXTURE2D_X_FLOAT                                                TEXTURE2D_ARRAY_FLOAT
    //Using explicit sample count of 1 to force DXC to actually reflect the texture as MS. The actual count appears to be irrelevant and any 2D MS texture array should bind to it
    #define TEXTURE2D_X_MSAA(type, textureName)                              Texture2DMSArray<type, 1> textureName

    #define RW_TEXTURE2D_X(type, textureName)                                RW_TEXTURE2D_ARRAY(type, textureName)
    #define TYPED_TEXTURE2D_X(type, textureName)                             TYPED_TEXTURE2D_ARRAY(type, textureName)
    #define LOAD_TEXTURE2D_X(textureName, unCoord2)                          LOAD_TEXTURE2D_ARRAY(textureName, unCoord2, SLICE_ARRAY_INDEX)
    #define LOAD_TEXTURE2D_X_MSAA(textureName, unCoord2, sampleIndex)        LOAD_TEXTURE2D_ARRAY_MSAA(textureName, unCoord2, SLICE_ARRAY_INDEX, sampleIndex)
    #define LOAD_TEXTURE2D_X_LOD(textureName, unCoord2, lod)                 LOAD_TEXTURE2D_ARRAY_LOD(textureName, unCoord2, SLICE_ARRAY_INDEX, lod)
    #define SAMPLE_TEXTURE2D_X(textureName, samplerName, coord2)             SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, coord2, SLICE_ARRAY_INDEX)
    #define SAMPLE_TEXTURE2D_X_LOD(textureName, samplerName, coord2, lod)    SAMPLE_TEXTURE2D_ARRAY_LOD(textureName, samplerName, coord2, SLICE_ARRAY_INDEX, lod)
    #define GATHER_TEXTURE2D_X(textureName, samplerName, coord2)             GATHER_TEXTURE2D_ARRAY(textureName, samplerName, coord2, SLICE_ARRAY_INDEX)
    #define GATHER_RED_TEXTURE2D_X(textureName, samplerName, coord2)         GATHER_RED_TEXTURE2D(textureName, samplerName, float3(coord2, SLICE_ARRAY_INDEX))
    #define GATHER_GREEN_TEXTURE2D_X(textureName, samplerName, coord2)       GATHER_GREEN_TEXTURE2D(textureName, samplerName, float3(coord2, SLICE_ARRAY_INDEX))
    #define GATHER_BLUE_TEXTURE2D_X(textureName, samplerName, coord2)        GATHER_BLUE_TEXTURE2D(textureName, samplerName, float3(coord2, SLICE_ARRAY_INDEX))
    #define GATHER_ALPHA_TEXTURE2D_X(textureName, samplerName, coord2)       GATHER_ALPHA_TEXTURE2D(textureName, samplerName, float3(coord2, SLICE_ARRAY_INDEX))
#else
    #define SLICE_ARRAY_INDEX                                                0

    #define COORD_TEXTURE2D_X(pixelCoord)                                    pixelCoord
    #define INDEX_TEXTURE2D_ARRAY_X(slot)                                    (slot)

    #define TEXTURE2D_X                                                      TEXTURE2D
    #define TEXTURE2D_X_PARAM                                                TEXTURE2D_PARAM
    #define TEXTURE2D_X_ARGS                                                 TEXTURE2D_ARGS
    #define TEXTURE2D_X_HALF                                                 TEXTURE2D_HALF
    #define TEXTURE2D_X_FLOAT                                                TEXTURE2D_FLOAT
    //Using explicit sample count of 1 to force DXC to actually reflect the texture as MS. The actual count appears to be irrelevant and any 2D MS texture should bind to it
    #define TEXTURE2D_X_MSAA(type, textureName)                              Texture2DMS<type, 1> textureName

    #define RW_TEXTURE2D_X                                                   RW_TEXTURE2D
    #define TYPED_TEXTURE2D_X                                                TYPED_TEXTURE2D
    #define LOAD_TEXTURE2D_X                                                 LOAD_TEXTURE2D
    #define LOAD_TEXTURE2D_X_MSAA                                            LOAD_TEXTURE2D_MSAA
    #define LOAD_TEXTURE2D_X_LOD                                             LOAD_TEXTURE2D_LOD
    #define SAMPLE_TEXTURE2D_X                                               SAMPLE_TEXTURE2D
    #define SAMPLE_TEXTURE2D_X_LOD                                           SAMPLE_TEXTURE2D_LOD
    #define GATHER_TEXTURE2D_X                                               GATHER_TEXTURE2D
    #define GATHER_RED_TEXTURE2D_X                                           GATHER_RED_TEXTURE2D
    #define GATHER_GREEN_TEXTURE2D_X                                         GATHER_GREEN_TEXTURE2D
    #define GATHER_BLUE_TEXTURE2D_X                                          GATHER_BLUE_TEXTURE2D
    #define GATHER_ALPHA_TEXTURE2D_X                                         GATHER_ALPHA_TEXTURE2D
#endif //defined(USE_TEXTURE2D_X_AS_ARRAY)

// see Unity\Shaders\Includes\UnityShaderVariables.cginc for impl used by the C++ renderer
#if defined(USING_STEREO_MATRICES) && defined(UNITY_STEREO_INSTANCING_ENABLED)
    static uint unity_StereoEyeIndex;
#else
    #define unity_StereoEyeIndex 0
#endif

// Helper macro to assign view index during compute/ray pass (usually from SV_DispatchThreadID or DispatchRaysIndex())
#if defined(SHADER_STAGE_COMPUTE) || defined(SHADER_STAGE_RAY_TRACING)
    #if defined(UNITY_STEREO_INSTANCING_ENABLED)
        #define UNITY_XR_ASSIGN_VIEW_INDEX(viewIndex) unity_StereoEyeIndex = viewIndex;
    #else
        #define UNITY_XR_ASSIGN_VIEW_INDEX(viewIndex)
    #endif

    // Backward compatibility
    #define UNITY_STEREO_ASSIGN_COMPUTE_EYE_INDEX   UNITY_XR_ASSIGN_VIEW_INDEX
#endif

#if defined(SHADER_API_METAL) && defined(UNITY_NEEDS_RENDERPASS_FBFETCH_FALLBACK)
    // Special metal fallback (allows branching per input to texture load or proper fbf)

#if defined(USE_TEXTURE2D_X_AS_ARRAY)

#define RENDERPASS_DECLARE_FALLBACK_X(T, idx)                                                   \
            Texture2DArray<T> _UnityFBInput##idx; float4 _UnityFBInput##idx##_TexelSize;                    \
            inline T ReadFBInput_##idx(bool var, uint2 coord) {                                             \
            [branch]if(var) { return hlslcc_fbinput_##idx; }                                                \
            else { return _UnityFBInput##idx.Load(uint4(coord, SLICE_ARRAY_INDEX, 0)); }                    \
            }

#define FRAMEBUFFER_INPUT_X_HALF(idx)                               cbuffer hlslcc_SubpassInput_f_##idx { half4 hlslcc_fbinput_##idx; bool hlslcc_fbfetch_##idx; };    \
                                                                                RENDERPASS_DECLARE_FALLBACK_X(half4, idx)

#define FRAMEBUFFER_INPUT_X_FLOAT(idx)                              cbuffer hlslcc_SubpassInput_f_##idx { float4 hlslcc_fbinput_##idx; bool hlslcc_fbfetch_##idx; };   \
                                                                                RENDERPASS_DECLARE_FALLBACK_X(float4, idx)

#define FRAMEBUFFER_INPUT_X_INT(idx)                                cbuffer hlslcc_SubpassInput_f_##idx { int4 hlslcc_fbinput_##idx; bool hlslcc_fbfetch_##idx; };    \
                                                                                RENDERPASS_DECLARE_FALLBACK_X(int4, idx)

#define FRAMEBUFFER_INPUT_X_UINT(idx)                               cbuffer hlslcc_SubpassInput_f_##idx { uint4 hlslcc_fbinput_##idx; bool hlslcc_fbfetch_##idx; };   \
                                                                                RENDERPASS_DECLARE_FALLBACK_X(uint4, idx)

#define LOAD_FRAMEBUFFER_INPUT_X(idx, v2fname)                      ReadFBInput_##idx(hlslcc_fbfetch_##idx, uint2(v2fname.xy))


#define RENDERPASS_DECLARE_FALLBACK_MS_X(T, idx)                                                          \
            Texture2DMSArray<T> _UnityFBInput##idx; float4 _UnityFBInput##idx##_TexelSize;                      \
            inline T ReadFBInput_##idx(bool var, uint2 coord, uint sampleIdx) {                                 \
                [branch]if(var) { return hlslcc_fbinput_##idx[sampleIdx]; }                                     \
                else { return _UnityFBInput##idx.Load(uint3(coord, SLICE_ARRAY_INDEX), sampleIdx); }            \
            }

#define FRAMEBUFFER_INPUT_X_FLOAT_MS(idx)                                                                 \
            cbuffer hlslcc_SubpassInput_F_##idx { float4 hlslcc_fbinput_##idx[8]; bool hlslcc_fbfetch_##idx; }; \
            RENDERPASS_DECLARE_FALLBACK_MS_X(float4, idx)

#define FRAMEBUFFER_INPUT_X_HALF_MS(idx)                                                                  \
            cbuffer hlslcc_SubpassInput_H_##idx { half4 hlslcc_fbinput_##idx[8]; bool hlslcc_fbfetch_##idx; };  \
            RENDERPASS_DECLARE_FALLBACK_MS_X(half4, idx)

#define FRAMEBUFFER_INPUT_X_INT_MS(idx)                                                                   \
            cbuffer hlslcc_SubpassInput_I_##idx { int4 hlslcc_fbinput_##idx[8]; bool hlslcc_fbfetch_##idx; };   \
            RENDERPASS_DECLARE_FALLBACK_MS_X(int4, idx)

#define FRAMEBUFFER_INPUT_X_UINT_MS(idx)                                                                  \
            cbuffer hlslcc_SubpassInput_U_##idx { uint4 hlslcc_fbinput_##idx[8]; bool hlslcc_fbfetch_##idx; };  \
            UNITY_RENDERPASS_DECLARE_FALLBACK_MS_X(uint4, idx)

#define LOAD_FRAMEBUFFER_INPUT_X_MS(idx, sampleIdx, v2fname) ReadFBInput_##idx(hlslcc_fbfetch_##idx, uint2(v2fname.xy), sampleIdx)

#else
    // X is just 2D texture so just use the existing macros that have all the metal magic for regular 2D textures
    #define FRAMEBUFFER_INPUT_X_HALF(idx)                               FRAMEBUFFER_INPUT_HALF(idx)
    #define FRAMEBUFFER_INPUT_X_FLOAT(idx)                              FRAMEBUFFER_INPUT_FLOAT(idx)
    #define FRAMEBUFFER_INPUT_X_INT(idx)                                FRAMEBUFFER_INPUT_INT(idx)
    #define FRAMEBUFFER_INPUT_X_UINT(idx)                               FRAMEBUFFER_INPUT_UINT(idx)
    #define LOAD_FRAMEBUFFER_INPUT_X(idx, v2fname)                      LOAD_FRAMEBUFFER_INPUT(idx, v2fname)

    #define FRAMEBUFFER_INPUT_X_HALF_MS(idx) FRAMEBUFFER_INPUT_HALF_MS(idx)
    #define FRAMEBUFFER_INPUT_X_FLOAT_MS(idx) FRAMEBUFFER_INPUT_FLOAT_MS(idx)
    #define FRAMEBUFFER_INPUT_X_INT_MS(idx) FRAMEBUFFER_INPUT_INT_MS(idx)
    #define FRAMEBUFFER_INPUT_X_UINT_MS(idx) FRAMEBUFFER_INPUT_UINT_MS(idx)
    #define LOAD_FRAMEBUFFER_INPUT_X_MS(idx, sampleIdx, v2fvertexname) LOAD_FRAMEBUFFER_INPUT_MS(idx, sampleIdx, v2fvertexname)

#endif //defined(USE_TEXTURE2D_X_AS_ARRAY)

#elif !defined(PLATFORM_SUPPORTS_NATIVE_RENDERPASS)

    // Use regular texture loads as a fallback these can be either 2d or array depending on the TEXTURE2D_X (USE_TEXTURE2D_X_AS_ARRAY) macros
#define FRAMEBUFFER_INPUT_X_HALF(idx)                               TEXTURE2D_X_HALF(_UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize
#define FRAMEBUFFER_INPUT_X_FLOAT(idx)                              TEXTURE2D_X_FLOAT(_UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize
#define FRAMEBUFFER_INPUT_X_INT(idx)                                TYPED_TEXTURE2D_X(int4, _UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize
#define FRAMEBUFFER_INPUT_X_UINT(idx)                               TYPED_TEXTURE2D_X(uint4, _UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize
#define LOAD_FRAMEBUFFER_INPUT_X(idx, v2fvertexname)                LOAD_TEXTURE2D_X(_UnityFBInput##idx,v2fvertexname.xy)

#define FRAMEBUFFER_INPUT_X_FLOAT_MS(idx) TEXTURE2D_X_MSAA(float4, _UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize
#define FRAMEBUFFER_INPUT_X_HALF_MS(idx) TEXTURE2D_X_MSAA(float4, _UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize
#define FRAMEBUFFER_INPUT_X_INT_MS(idx) TEXTURE2D_X_MSAA(int4, _UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize
#define FRAMEBUFFER_INPUT_X_UINT_MS(idx) TEXTURE2D_X_MSAA(uint4, _UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize
#define LOAD_FRAMEBUFFER_INPUT_X_MS(idx, sampleIdx, v2fvertexname) LOAD_TEXTURE2D_X_MSAA(_UnityFBInput##idx, v2fvertexname.xy, sampleIdx)

#else

    // Proper fbf, it will automatically ensure the correct eye is fb-fetched so we do not care if USE_TEXTURE2D_X_AS_ARRAY is enabled or not
#define FRAMEBUFFER_INPUT_X_HALF(idx)                               FRAMEBUFFER_INPUT_HALF(idx)
#define FRAMEBUFFER_INPUT_X_FLOAT(idx)                              FRAMEBUFFER_INPUT_FLOAT(idx)
#define FRAMEBUFFER_INPUT_X_INT(idx)                                FRAMEBUFFER_INPUT_INT(idx)
#define FRAMEBUFFER_INPUT_X_UINT(idx)                               FRAMEBUFFER_INPUT_UINT(idx)
#define LOAD_FRAMEBUFFER_INPUT_X(idx, v2fname)                      LOAD_FRAMEBUFFER_INPUT(idx, v2fname)

#define FRAMEBUFFER_INPUT_X_HALF_MS(idx) FRAMEBUFFER_INPUT_HALF_MS(idx)
#define FRAMEBUFFER_INPUT_X_FLOAT_MS(idx) FRAMEBUFFER_INPUT_FLOAT_MS(idx)
#define FRAMEBUFFER_INPUT_X_INT_MS(idx) FRAMEBUFFER_INPUT_INT_MS(idx)
#define FRAMEBUFFER_INPUT_X_UINT_MS(idx) FRAMEBUFFER_INPUT_UINT_MS(idx)
#define LOAD_FRAMEBUFFER_INPUT_X_MS(idx, sampleIdx, v2fvertexname) LOAD_FRAMEBUFFER_INPUT_MS(idx, sampleIdx, v2fname)

#endif //!defined(PLATFORM_SUPPORTS_NATIVE_RENDERPASS)

#endif // UNITY_TEXTUREXR_INCLUDED
