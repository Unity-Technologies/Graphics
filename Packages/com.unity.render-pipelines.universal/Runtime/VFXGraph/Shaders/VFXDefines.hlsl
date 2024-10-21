#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
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

// this is only necessary for the old VFXTarget pathway
// it defines the macro used to access hybrid instanced properties
// (new HDRP/URP Target pathway overrides the type so this is never used)
#define UNITY_ACCESS_HYBRID_INSTANCED_PROP(name, type) name


//Unlit can use the DepthNormal pass which creates a discrepancy while computing depth
#define FORCE_NORMAL_OUTPUT_UNLIT_VERTEX_SHADER 1

#if HAS_STRIPS
#define HAS_STRIPS_DATA 1
#endif
