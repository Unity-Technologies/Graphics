#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureXR.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialBlendModeEnum.cs.hlsl"

#if VFX_BLENDMODE_ALPHA
    #define _BlendMode BLENDINGMODE_ALPHA
#elif VFX_BLENDMODE_ADD
    #define _BlendMode BLENDINGMODE_ADDITIVE
#elif VFX_BLENDMODE_PREMULTIPLY
    #define _BlendMode BLENDINGMODE_PREMULTIPLY
#else
    //Opaque, doesn't really matter what we specify, but a definition is needed to avoid compilation errors.
    #define _BlendMode BLENDINGMODE_ALPHA
#endif

#if HDRP_LIT
#define VFX_NEEDS_POSWS_INTERPOLATOR 1 // Needed for LPPV
#elif IS_TRANSPARENT_PARTICLE // Fog for opaque is handled in a dedicated pass
#define VFX_NEEDS_POSWS_INTERPOLATOR 1
#endif

#if HDRP_MATERIAL_TYPE_SIMPLELIT
#define HDRP_MATERIAL_TYPE_STANDARD 1
#define HDRP_MATERIAL_TYPE_SIMPLE 1
#elif HDRP_MATERIAL_TYPE_SIMPLELIT_TRANSLUCENT
#define HDRP_MATERIAL_TYPE_TRANSLUCENT 1
#define HDRP_MATERIAL_TYPE_SIMPLE 1
#endif

#ifndef HDRP_ENABLE_SHADOWS
#define _RECEIVE_SHADOWS_OFF
#endif

#if IS_TRANSPARENT_PARTICLE
#define _SURFACE_TYPE_TRANSPARENT
#endif

#if VFX_SIX_WAY_COLOR_ABSORPTION
    #define _SIX_WAY_COLOR_ABSORPTION
#endif

#ifdef USE_TEXTURE2D_X_AS_ARRAY
#define CameraBuffer Texture2DArray
#define VFXSamplerCameraBuffer VFXSampler2DArray
#else
#define CameraBuffer Texture2D
#define VFXSamplerCameraBuffer VFXSampler2D
#endif

#define UNITY_ACCESS_HYBRID_INSTANCED_PROP(name, type) name

//HDRP is forcing the USING_STEREO_MATRICES define in case of compute shader (See TextureXR.hlsl)
#define USE_MULTI_COMPILE_XR_IN_OUTPUT_UPDATE 0

#if USE_GEOMETRY_SHADER
#define CULL_VERTEX(o) return;
#else
#define CULL_VERTEX(o) { o.VFX_VARYING_POSCS.x = VFX_NAN; return o; }
#endif

#if HAS_STRIPS
#define HAS_STRIPS_DATA 1
#endif

// Enable the support of global mip bias in the shader.
// Only has effect if the global mip bias is enabled in shader config and DRS is enabled.
#define SUPPORT_GLOBAL_MIP_BIAS
