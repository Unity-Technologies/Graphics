#ifndef SHADOW_HLSL
#define SHADOW_HLSL
//
// Shadow master include header.
//
// There are four relevant files for shadows.
// First ShadowContext.hlsl must declare the specific ShadowContext struct and the loader that goes along with it.
// ShadowContext loading and resource setup from C# must be in sync.
//
// Second there are two headers for shadow algorithms, whose signatures must match any of the Get...Attenuation function prototypes.
// The first header contains engine defaults, whereas the second header is empty by default. All project specific custom shadow algorithms should go in there or leave empty.
//
// Last there's a dispatcher include. By default the Get...Attenuation functions are rerouted to their default implementations. This can be overridden for each
// shadow type in the dispatcher source. For each overridden shadow type a specific define must be defined to prevent falling back to the default functions.
//


//#define SHADOWS_USE_SHADOWCTXT

#ifdef  SHADOWS_USE_SHADOWCTXT
#define SHADOW_SUPPORTS_DYNAMIC_INDEXING 0 // only on >= sm 5.1

// TODO: Remove this once we've moved over to the new system. Also delete the undef at the bottom again.
#define ShadowData ShadowDataExp

#include "ShadowBase.cs.hlsl"   // ShadowData definition, auto generated (don't modify)
#include "ShadowTexFetch.hlsl"  // Resource sampling definitions (don't modify)

#define SHADOWCONTEXT_DECLARE( _Tex2DArraySlots, _TexCubeArraySlots, _SamplerCompSlots, _SamplerSlots ) \
                                                                                                        \
    struct ShadowContext                                                                                \
    {                                                                                                   \
        StructuredBuffer<ShadowData>    shadowDatas;                                                    \
        StructuredBuffer<int4>          payloads;                                                       \
        Texture2DArray                  tex2DArray[_Tex2DArraySlots];                                   \
        TextureCubeArray                texCubeArray[_TexCubeArraySlots];                               \
        SamplerComparisonState          compSamplers[_SamplerCompSlots];                                \
        SamplerState                    samplers[_SamplerSlots];                                        \
    };                                                                                                  \
                                                                                                        \
    SHADOW_DEFINE_SAMPLING_FUNCS( _Tex2DArraySlots, _TexCubeArraySlots, _SamplerCompSlots, _SamplerSlots )

// Shadow context definition and initialization, i.e. resource binding (project header, must be kept in sync with C# runtime)
#include "ShadowContext.hlsl"

// helper function to extract shadowmap data from the ShadowData struct
void unpackShadowmapId( uint shadowmapId, out uint texIdx, out uint sampIdx, out float slice )
{
    texIdx  = (shadowmapId >> 24) & 0xff;
    sampIdx = (shadowmapId >> 16) & 0xff;
    slice   = (float)(shadowmapId & 0xffff);
}
void unpackShadowmapId( uint shadowmapId, out uint texIdx, out uint sampIdx )
{
    texIdx  = (shadowmapId >> 24) & 0xff;
    sampIdx = (shadowmapId >> 16) & 0xff;
}
void unpackShadowmapId( uint shadowmapId, out float slice )
{
    slice = (float)(shadowmapId & 0xffff);
}



// shadow sampling prototypes
float GetPunctualShadowAttenuation( ShadowContext shadowContext, float3 positionWS, int shadowDataIndex, float3 L );
float GetPunctualShadowAttenuation( ShadowContext shadowContext, float3 positionWS, int shadowDataIndex, float3 L, float2 unPositionSS );
// shadow sampling prototypes with screenspace info
float GetDirectionalShadowAttenuation( ShadowContext shadowContext, float3 positionWS, int shadowDataIndex, float3 L );
float GetDirectionalShadowAttenuation( ShadowContext shadowContext, float3 positionWS, int shadowDataIndex, float3 L, float2 unPositionSS );


// wedge in the actual shadow sampling algorithms
#include "ShadowSampling.hlsl"          // sampling patterns
#include "ShadowAlgorithms.hlsl"        // engine default algorithms (don't modify)
#include "ShadowAlgorithmsCustom.hlsl"  // project specific custom algorithms (project can modify this)


// default dispatchers for the individual shadow types (with and without screenspace support)
// point/spot light shadows
float GetPunctualShadowAttenuationDefault( ShadowContext shadowContext, float3 positionWS, int shadowDataIndex, float3 L )
{
    return EvalShadow_PunctualDepth(shadowContext, positionWS, shadowDataIndex, L);
}
float GetPunctualShadowAttenuationDefault( ShadowContext shadowContext, float3 positionWS, int shadowDataIndex, float3 L, float2 unPositionSS )
{
    return GetPunctualShadowAttenuationDefault( shadowContext, positionWS, shadowDataIndex, L );
}
// directional light shadows
float GetDirectionalShadowAttenuationDefault( ShadowContext shadowContext, float3 positionWS, int shadowDataIndex, float3 L )
{
    return EvalShadow_CascadedDepth( shadowContext, positionWS, shadowDataIndex, L );
}
float GetDirectionalShadowAttenuationDefault( ShadowContext shadowContext, float3 positionWS, int shadowDataIndex, float3 L, float2 unPositionSS )
{
    return GetDirectionalShadowAttenuationDefault( shadowContext, positionWS, shadowDataIndex, L );
}

// include project specific shadow dispatcher. If this file is not empty, it MUST define which default shadows it's overriding
#include "ShadowDispatch.hlsl"

// if shadow dispatch is empty we'll fall back to default shadow sampling implementations
#ifndef SHADOW_DISPATCH_USE_CUSTOM_PUNCTUAL
float GetPunctualShadowAttenuation( ShadowContext shadowContext, float3 positionWS, int shadowDataIndex, float3 L )
{
    return GetPunctualShadowAttenuationDefault( shadowContext, positionWS, shadowDataIndex, L );
}
float GetPunctualShadowAttenuation( ShadowContext shadowContext, float3 positionWS, int shadowDataIndex, float3 L, float2 unPositionSS )
{
    return GetPunctualShadowAttenuationDefault( shadowContext, positionWS, shadowDataIndex, L, unPositionSS );
}
#endif
#ifndef SHADOW_DISPATCH_USE_CUSTOM_DIRECTIONAL
float GetDirectionalShadowAttenuation( ShadowContext shadowContext, float3 positionWS, int shadowDataIndex, float3 L )
{
    return GetDirectionalShadowAttenuationDefault( shadowContext, positionWS, shadowDataIndex, L );
}
float GetDirectionalShadowAttenuation( ShadowContext shadowContext, float3 positionWS, int shadowDataIndex, float3 L, float2 unPositionSS )
{
    return GetDirectionalShadowAttenuationDefault( shadowContext, positionWS, shadowDataIndex, L, unPositionSS );
}
#endif

#undef ShadowData // TODO: Remove this once we've moved over to the new system. Also delete the define at the top again.

#endif // SHADOWS_USE_SHADOWCTXT

#endif // SHADOW_HLSL
