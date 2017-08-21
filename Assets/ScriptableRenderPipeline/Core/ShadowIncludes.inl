// This file is inlined by ShaderLibrary/Shadow/Shadow.hlsl twice.
// Each time either SHADOW_CONTEXT_INCLUDE or SHADOW_DISPATCH_INCLUDE is defined.
// In the case of SHADOW_CONTEXT_INCLUDE a valid path must be given to a file that contains
// the code to initialize a shadow context.
// SHADOW_DISPATCH_INCLUDE is optional.

#ifdef SHADOW_CONTEXT_INCLUDE
#	ifdef SHADOW_TILEPASS
#		include "HDRenderPipeline/Lighting/TilePass/ShadowContext.hlsl"
#	elif defined( SHADOW_FPTL )
#		include "fptl/ShadowContext.hlsl"
#	else
#		error "No valid path to the shadow context has been given."
#	endif
#endif

#ifdef SHADOW_DISPATCH_INCLUDE
#	ifdef SHADOW_TILEPASS
#		include "HDRenderPipeline/Lighting/TilePass/ShadowDispatch.hlsl"
#	elif defined( SHADOW_FPTL )
#		include "fptl/ShadowDispatch.hlsl"
#	else
		// It's ok not to have a dispatcher include as it only acts as an override
#	endif
#endif
