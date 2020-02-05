#ifndef SHADERPASS
    #error Undefined_SHADERPASS
#endif

#if ((SHADERPASS != SHADERPASS_DEPTH_ONLY) && (SHADERPASS != SHADERPASS_SHADOWS))
    #error Wrong_SHADERPASS
#endif

// Add local helper definitions.
#include "LitParamDefsAdd.hlsl"

/* Varyings_PS are pixel shader inputs (vertex or domain shader outputs). */

#ifdef DEPTH_PASS_HAS_PS
        #define VARYINGS_NEED_POSITION_WS // TODO: remove, compute from depth

    #ifdef _PIXEL_DISPLACEMENT
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
    #endif

    #if (defined(_PIXEL_DISPLACEMENT) || defined(_ALPHATEST_ON)) // PPD may affect the position
            #define VARYINGS_NEED_TEXCOORD0 // Always!

        #ifdef TEX_UV1
            #define VARYINGS_NEED_TEXCOORD1
        #endif

        #ifdef TEX_UV2
            #define VARYINGS_NEED_TEXCOORD2
        #endif

        #ifdef TEX_UV3
            #define VARYINGS_NEED_TEXCOORD3
        #endif

        #ifdef TEX_COL
            #define VARYINGS_NEED_COLOR
        #endif
    #endif // (defined(_PIXEL_DISPLACEMENT) || defined(_ALPHATEST_ON))

    #ifdef _DOUBLESIDED_ON
        #define VARYINGS_NEED_CULLFACE
    #endif
#endif // DEPTH_PASS_HAS_PS

// Varyings_DS are domain shader inputs.
// Attributes are vertex shader inputs.
#include "LitVaryingsDsAndAttributes.hlsl"

// Remove local helper definitions.
#include "LitParamDefsRem.hlsl"

// This include will define the various Attributes/Varyings structure
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
