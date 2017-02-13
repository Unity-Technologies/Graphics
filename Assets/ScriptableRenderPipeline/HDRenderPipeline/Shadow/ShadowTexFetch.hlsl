

// shadow lookup routines when dynamic array access is possible
#if SHADOW_SUPPORTS_DYNAMIC_INDEXING != 0

#	define SHADOW_DEFINE_SAMPLING_FUNCS( _Tex2DArraySlots, _TexCubeArraySlots ) \
		float4 SampleShadow_T2DA( ShadowContext ctxt, uint texIdx, SamplerComparisonState ss, float3 tcs, float slice					)	{ return SAMPLE_TEXTURE2D_ARRAY_SHADOW(ctxt.tex2DArray[texIdx], ss, tcs, slice);		}	\
		float4 SampleShadow_T2DA( ShadowContext ctxt, uint texIdx, SamplerState			  ss, float2 tcs, float slice, float lod = 0.0	)	{ return SAMPLE_TEXTURE2D_ARRAY_LOD(ctxt.tex2DArray[texIdx], ss, tcs, slice, lod);		}	\
		float4 SampleShadow_TCA(  ShadowContext ctxt, uint texIdx, SamplerComparisonState ss, float4 tcs, float cubeIdx					)	{ return SAMPLE_TEXTURECUBE_ARRAY_SHADOW(ctxt.texCubeArray[texIdx], ss, tcs, cubeIdx);	}	\
		float4 SampleShadow_TCA(  ShadowContext ctxt, uint texIdx, SamplerState			  ss, float3 tcs, float cubeIdx, float lod = 0.0)	{ return SAMPLE_TEXTURECUBE_ARRAY_LOD(ctxt.texCubeArray[texIdx], ss, tcs, cubeIdx, lod);}


#else // helper macros if dynamic indexing does not work

//	recursion helpers
#	define SHADOW_T2DA_SAMPLE_COMP( _idx )	case _idx : return SAMPLE_TEXTURE2D_ARRAY_SHADOW( ctxt.tex2DArray[_idx], ss, tcs, slice );
#	define SHADOW_T2DA_SAMPLE( _idx )		case _idx : return SAMPLE_TEXTURE2D_ARRAY_LOD( ctxt.tex2DArray[_idx], ss, tcs.xy, tcs.z, lod );
#	define SHADOW_TCA_SAMPLE_COMP( _idx )	case _idx : return SAMPLE_TEXTURECUBE_ARRAY_SHADOW( ctxt.texCubeArray[texIdx], ss, tcs, cubeIdx );
#	define SHADOW_TCA_SAMPLE( _idx )		case _idx : return SAMPLE_TEXTURECUBE_ARRAY_LOD( ctxt.texCubeArray[texIdx], ss, tcs, cubeIdx, lod );

//  poor man's recursion
#	define SHADOW_REC_FUNC( _a, _b, _idx ) _a ## _b( _idx )

#	define SHADOW_REC0( _a, _b) // expands to nothing

#	define SHADOW_REC1( _a, _b)		default:						\
									SHADOW_REC_FUNC( _a, _b, 0 )

#	define SHADOW_REC2( _a, _b)		SHADOW_REC1( _a, _b )			\
						 	 		SHADOW_REC_FUNC( _a, _b, 1 )

#	define SHADOW_REC3( _a, _b)		SHADOW_REC2( _a, _b )			\
						 	 		SHADOW_REC_FUNC( _a, _b, 2 )

#	define SHADOW_REC4( _a, _b)		SHADOW_REC3( _a, _b )			\
						 	 		SHADOW_REC_FUNC( _a, _b, 3 )

#	define SHADOW_REC5( _a, _b)		SHADOW_REC4( _a, _b )			\
						 	 		SHADOW_REC_FUNC( _a, _b, 4 )

#	define SHADOW_REC6( _a, _b)		SHADOW_REC5( _a, _b )			\
									SHADOW_REC_FUNC( _a, _b, 5 )

#	define SHADOW_REC7( _a, _b)		SHADOW_REC6( _a, _b )			\
									SHADOW_REC_FUNC( _a, _b, 6 )

#	define SHADOW_REC8( _a, _b)		SHADOW_REC7( _a, _b )			\
									SHADOW_REC_FUNC( _a, _b, 7 )

#	define SHADOW_REC9( _a, _b)		SHADOW_REC8( _a, _b )			\
									SHADOW_REC_FUNC( _a, _b, 8 )

#	define SHADOW_REC10( _a, _b)	SHADOW_REC9( _a, _b )			\
									SHADOW_REC_FUNC( _a, _b, 9 )
//	standard macro helpers
#	define SHADOW_EXPAND( _x )					_x
#	define SHADOW_CAT( _x, _y )					_x ## _y
#	define SHADOW_REC_ENTRY( _x, _y, _z, _w )	SHADOW_CAT( _x, _y )( _z, _w )
//	and the actual definition of the sampling functions
#	define SHADOW_DEFINE_SAMPLING_FUNCS( _Tex2DArraySlots, _TexCubeArraySlots, _SamplerCompSlots, _SamplerSlots  )														\
		float4 SampleShadow_T2DA( ShadowContext ctxt, uint texIdx, SamplerComparisonState ss, float3 tcs, float slice )				\
		{																															\
			[branch]																												\
			switch( texIdx )																										\
			{																														\
			SHADOW_REC_ENTRY( SHADOW_REC, SHADOW_EXPAND( _Tex2DArraySlots ), SHADOW_T2DA_, SAMPLE_COMP )							\
			}																														\
		}																															\
		float4 SampleShadow_T2DA( ShadowContext ctxt, uint texIdx, SamplerState ss, float3 tcs, float lod = 0.0 )					\
		{																															\
			[branch]																												\
			switch( texIdx )																										\
			{																														\
			SHADOW_REC_ENTRY( SHADOW_REC, SHADOW_EXPAND( _Tex2DArraySlots ), SHADOW_T2DA_, SAMPLE )									\
			}																														\
		}																															\
		float4 SampleShadow_TCA( ShadowContext ctxt, uint texIdx, SamplerComparisonState ss, float4 tcs, float cubeIdx )			\
		{																															\
			[branch]																												\
			switch( texIdx )																										\
			{																														\
			SHADOW_REC_ENTRY( SHADOW_REC, SHADOW_EXPAND( _TexCubeArraySlots ), SHADOW_TCA_, SAMPLE_COMP )							\
			}																														\
		}																															\
		float4 SampleShadow_TCA(ShadowContext ctxt, uint texIdx, SamplerState ss, float3 tcs, float cubeIdx, float lod = 0.0 )		\
		{																															\
			[branch]																												\
			switch( texIdx )																										\
			{																														\
			SHADOW_REC_ENTRY( SHADOW_REC, SHADOW_EXPAND( _TexCubeArraySlots ), SHADOW_TCA_, SAMPLE )								\
			}																														\
		}

#endif
