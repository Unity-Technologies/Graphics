
#if SHADERPASS == SHADERPASS_DEPTH_ONLY
    #ifdef WRITE_NORMAL_BUFFER
        #if defined(_NORMALMAP)
            #define OVERRIDE_SPLAT_SAMPLER_NAME sampler_Normal0
        #elif defined(_MASKMAP)
            #define OVERRIDE_SPLAT_SAMPLER_NAME sampler_Mask0
        #endif
    #endif
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
