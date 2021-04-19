#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialBlendModeEnum.cs.hlsl"

#if VFX_BLENDMODE_ALPHA
    #define _BlendMode BLENDMODE_ALPHA 
#elif VFX_BLENDMODE_ADD
    #define _BlendMode BLENDMODE_ADDITIVE  
#elif VFX_BLENDMODE_PREMULTIPLY
    #define _BlendMode BLENDMODE_PREMULTIPLY   
#else
    //Opaque, doesn't really matter what we specify, but a definition is needed to avoid compilation errors.
    #define _BlendMode BLENDMODE_ALPHA 
#endif

#ifdef _BLENDMODE_PRESERVE_SPECULAR_LIGHTING
    #define _EnableBlendModePreserveSpecularLighting 1
#else
    #define _EnableBlendModePreserveSpecularLighting 0
#endif

#if IS_TRANSPARENT_PARTICLE && !HDRP_LIT // Fog for opaque is handled in a dedicated pass
#define USE_FOG 1
#define VFX_NEEDS_POSWS_INTERPOLATOR 1
#endif

#if HDRP_MATERIAL_TYPE_SIMPLELIT
#define HDRP_MATERIAL_TYPE_STANDARD 1
#define HDRP_MATERIAL_TYPE_SIMPLE 1
#elif HDRP_MATERIAL_TYPE_SIMPLELIT_TRANSLUCENT
#define HDRP_MATERIAL_TYPE_TRANSLUCENT 1
#define HDRP_MATERIAL_TYPE_SIMPLE 1
#endif

#if IS_TRANSPARENT_PARTICLE
#define _SURFACE_TYPE_TRANSPARENT
#endif
