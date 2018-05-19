#ifndef SHADOW_HLSL
#define SHADOW_HLSL

//
// Shadow master include header.
//
// There are four relevant files for shadows.
// First ShadowContext.hlsl provides a macro SHADOWCONTEXT_DECLARE that must be used in order to define the specific ShadowContext struct and accompanying loader.
// ShadowContext loading and resource setup from C# must be in sync.
//

/* Required defines: (define these to the desired numbers - must be in sync with loading and resource setup from C#)
#define SHADOWCONTEXT_MAX_TEX2DARRAY   0
#define SHADOWCONTEXT_MAX_TEXCUBEARRAY 0
#define SHADOWCONTEXT_MAX_SAMPLER      0
#define SHADOWCONTEXT_MAX_COMPSAMPLER  0
*/

/* Default values for optional defines:
#define SHADOW_SUPPORTS_DYNAMIC_INDEXING        0   // Dynamic indexing only works on >= sm 5.1
#define SHADOW_OPTIMIZE_REGISTER_USAGE          0   // Redefine this as 1 in your ShadowContext.hlsl to optimize for register usage over instruction count
#define SHADOW_USE_VIEW_BIAS_SCALING            0   // Enable view bias scaling to mitigate light leaking across edges. Uses the light vector if SHADOW_USE_ONLY_VIEW_BASED_BIASING is defined, otherwise uses the normal.
#define SHADOW_USE_ONLY_VIEW_BASED_BIASING      0   // Enable only light view vector based biasing. If undefined, biasing will be based on the normal and calling code must provide a valid normal.
#define SHADOW_USE_SAMPLE_BIASING               0   // Enable per sample biasing for wide multi-tap PCF filters. Incompatible with SHADOW_USE_ONLY_VIEW_BASED_BIASING.
#define SHADOW_USE_DEPTH_BIAS                   0   // Enable clip space z biasing
// #define SHADOW_DISPATCH_USE_CUSTOM_PUNCTUAL      // Enable custom implementations of GetPunctualShadowAttenuation. If not defined, a default implementation will be used.
// #define SHADOW_DISPATCH_USE_CUSTOM_DIRECTIONAL   // Enable custom implementations of GetDirectionalShadowAttenuation. If not defined, a default implementation will be used.
*/


#ifndef SHADOW_SUPPORTS_DYNAMIC_INDEXING
#   define SHADOW_SUPPORTS_DYNAMIC_INDEXING 0
#endif
#ifndef SHADOW_OPTIMIZE_REGISTER_USAGE
#   define SHADOW_OPTIMIZE_REGISTER_USAGE   0
#endif
#ifndef SHADOW_USE_VIEW_BIAS_SCALING
#   define SHADOW_USE_VIEW_BIAS_SCALING     0
#endif
#ifndef SHADOW_USE_SAMPLE_BIAS
#   define SHADOW_USE_SAMPLE_BIAS           0
#endif
#ifndef SHADOW_USE_SAMPLE_BIAS
#   define SHADOW_USE_SAMPLE_BIAS           0
#endif
#ifndef SHADOW_USE_DEPTH_BIAS
#   define SHADOW_USE_DEPTH_BIAS            0
#endif

#if SHADOW_USE_ONLY_VIEW_BASED_BIASING != 0
#   if SHADOW_USE_SAMPLE_BIASING != 0
#       pragma message "Shadows: SHADOW_USE_SAMPLE_BIASING was enabled together with SHADOW_USE_ONLY_VIEW_BASED_BIASING. Sample biasing requires the normal. Disabling SHADOW_USE_SAMPLE_BIASING again."
#       undef  SHADOW_USE_SAMPLE_BIASING
#       define SHADOW_USE_SAMPLE_BIASING 0
#   endif
#endif

#if SHADOW_OPTIMIZE_REGISTER_USAGE == 1
#   pragma warning( disable : 3557 ) // loop only executes for 1 iteration(s)
#endif

#include "CoreRP/Shadow/ShadowBase.cs.hlsl"	// ShadowData definition, auto generated (don't modify)
#include "ShadowTexFetch.hlsl"				// Resource sampling definitions (don't modify)

struct ShadowContext
{
	StructuredBuffer<ShadowData>	shadowDatas;
	StructuredBuffer<int4>			payloads;
	SHADOWCONTEXT_DECLARE_TEXTURES( SHADOWCONTEXT_MAX_TEX2DARRAY, SHADOWCONTEXT_MAX_TEXCUBEARRAY, SHADOWCONTEXT_MAX_COMPSAMPLER, SHADOWCONTEXT_MAX_SAMPLER )
};

SHADOW_DEFINE_SAMPLING_FUNCS( SHADOWCONTEXT_MAX_TEX2DARRAY, SHADOWCONTEXT_MAX_TEXCUBEARRAY, SHADOWCONTEXT_MAX_COMPSAMPLER, SHADOWCONTEXT_MAX_SAMPLER )

// helper function to extract shadowmap data from the ShadowData struct
void UnpackShadowmapId( uint shadowmapId, out uint texIdx, out uint sampIdx )
{
	texIdx  = (shadowmapId >> 24) & 0xff;
	sampIdx = (shadowmapId >> 16) & 0xff;
}
void UnpackShadowType( uint packedShadowType, out uint shadowType, out uint shadowAlgorithm )
{
	shadowType		= packedShadowType >> 10;
	shadowAlgorithm = packedShadowType & 0x1ff;
}

void UnpackShadowType( uint packedShadowType, out uint shadowType )
{
	shadowType = packedShadowType >> 10;
}

// shadow sampling prototypes
real GetPunctualShadowAttenuation( ShadowContext shadowContext, real3 positionWS, real3 normalWS, int shadowDataIndex, real3 L, real L_dist );
real GetPunctualShadowAttenuation( ShadowContext shadowContext, real3 positionWS, real3 normalWS, int shadowDataIndex, real3 L, real L_dist, real2 positionSS );

// shadow sampling prototypes with screenspace info
real GetDirectionalShadowAttenuation( ShadowContext shadowContext, real3 positionWS, real3 normalWS, int shadowDataIndex, real3 L );
real GetDirectionalShadowAttenuation( ShadowContext shadowContext, real3 positionWS, real3 normalWS, int shadowDataIndex, real3 L, real2 positionSS );

#include "ShadowSampling.hlsl"			// sampling patterns (don't modify)
#include "ShadowAlgorithms.hlsl"		// engine default algorithms (don't modify)

#ifndef SHADOW_DISPATCH_USE_CUSTOM_PUNCTUAL
real GetPunctualShadowAttenuation( ShadowContext shadowContext, real3 positionWS, real3 normalWS, int shadowDataIndex, real3 L, real L_dist )
{
	return EvalShadow_PunctualDepth( shadowContext, positionWS, normalWS, shadowDataIndex, L, L_dist );
}

real GetPunctualShadowAttenuation( ShadowContext shadowContext, real3 positionWS, real3 normalWS, int shadowDataIndex, real3 L, real L_dist, real2 positionSS )
{
	return GetPunctualShadowAttenuation( shadowContext, positionWS, normalWS, shadowDataIndex, L, L_dist );
}
#endif

#ifndef SHADOW_DISPATCH_USE_CUSTOM_DIRECTIONAL
real GetDirectionalShadowAttenuation( ShadowContext shadowContext, real3 positionWS, real3 normalWS, int shadowDataIndex, real3 L )
{
	return EvalShadow_CascadedDepth_Blend( shadowContext, positionWS, normalWS, shadowDataIndex, L );
}

real GetDirectionalShadowAttenuation( ShadowContext shadowContext, real3 positionWS, real3 normalWS, int shadowDataIndex, real3 L, real2 positionSS )
{
	return GetDirectionalShadowAttenuation( shadowContext, positionWS, normalWS, shadowDataIndex, L );
}
#endif

#endif // SHADOW_HLSL
