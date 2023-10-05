#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
#define USE_FOG 1
#endif

#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
#define CameraBuffer Texture2DArray
#define VFXSamplerCameraBuffer VFXSampler2DArray
#else
#define CameraBuffer Texture2D
#define VFXSamplerCameraBuffer VFXSampler2D
#endif

#if IS_TRANSPARENT_PARTICLE
#define _SURFACE_TYPE_TRANSPARENT
#endif

#if VFX_SIX_WAY_COLOR_ABSORPTION
    #define _SIX_WAY_COLOR_ABSORPTION
#endif

//URP currently does not allow to know the blend mode in the shader in general, but we have this information in VFX generated shaders.
#if VFX_BLENDMODE_PREMULTIPLY
#define _BLENDMODE_PREMULTIPLY
#endif
// this is only necessary for the old VFXTarget pathway
// it defines the macro used to access hybrid instanced properties
// (new HDRP/URP Target pathway overrides the type so this is never used)
#define UNITY_ACCESS_HYBRID_INSTANCED_PROP(name, type) name

#define USE_MULTI_COMPILE_XR_IN_OUTPUT_UPDATE 1

//Unlit can use the DepthNormal pass which creates a discrepancy while computing depth
#define FORCE_NORMAL_OUTPUT_UNLIT_VERTEX_SHADER 1

#if USE_GEOMETRY_SHADER
#define CULL_VERTEX(o) return;
#else
#define CULL_VERTEX(o) { o.VFX_VARYING_POSCS.x = VFX_NAN; return o; }
#endif
