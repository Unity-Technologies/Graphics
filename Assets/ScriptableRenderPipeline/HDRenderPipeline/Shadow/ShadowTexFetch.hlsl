

// shadow lookup routines when dynamic array access is possible
#if SHADOW_SUPPORTS_DYNAMIC_INDEXING != 0

// Shader model >= 5.1
#	define SHADOW_DEFINE_SAMPLING_FUNCS( _Tex2DArraySlots, _TexCubeArraySlots ) \
		float4 SampleCompShadow_T2DA( ShadowContext ctxt, uint texIdx, uint sampIdx, float3 tcs, float slice					)	{ return SAMPLE_TEXTURE2D_ARRAY_SHADOW( ctxt.tex2DArray[texIdx], ctxt.compSamplers[sampIdx], tcs, slice );		}	\
		float4 SampleShadow_T2DA(	  ShadowContext ctxt, uint texIdx, uint sampIdx, float2 tcs, float slice, float lod = 0.0	)	{ return SAMPLE_TEXTURE2D_ARRAY_LOD( ctxt.tex2DArray[texIdx], ctxt.samplers[sampIdx], tcs, slice, lod );		}	\
		float4 SampleCompShadow_TCA(  ShadowContext ctxt, uint texIdx, uint sampIdx, float4 tcs, float cubeIdx					)	{ return SAMPLE_TEXTURECUBE_ARRAY_SHADOW( ctxt.texCubeArray[texIdx], ctxt.compSamplers[sampIdx], tcs, cubeIdx );}	\
		float4 SampleShadow_TCA(	  ShadowContext ctxt, uint texIdx, uint sampIdx, float3 tcs, float cubeIdx, float lod = 0.0 )	{ return SAMPLE_TEXTURECUBE_ARRAY_LOD( ctxt.texCubeArray[texIdx], ctxt.samplers[sampIdx], tcs, cubeIdx, lod );	}


#else // helper macros if dynamic indexing does not work


// Sampler and texture combinations are static. No shadowmap will ever be sampled with two different samplers.
// Once shadowmaps and samplers are fixed consider writing custom dispatchers directly accessing textures and samplers.
#	define SHADOW_DEFINE_SAMPLING_FUNCS( _Tex2DArraySlots, _TexCubeArraySlots, _SamplerCompSlots, _SamplerSlots  )				  \
																																  \
		float4 SampleCompShadow_T2DA( ShadowContext ctxt, uint texIdx, uint sampIdx, float3 tcs, float slice )					  \
		{																														  \
			[unroll] for( uint i = 0; i < _Tex2DArraySlots; i++ )																  \
			{																													  \
				[unroll] for( uint j = 0; j < _SamplerCompSlots; j++ )															  \
				{																												  \
					[branch] if( i == texIdx && j == sampIdx )																	  \
					{																											  \
						return SAMPLE_TEXTURE2D_ARRAY_SHADOW( ctxt.tex2DArray[i], ctxt.compSamplers[j], tcs, slice );			  \
					}																											  \
				}																												  \
			}																													  \
			return 1.0;																											  \
		}																														  \
																																  \
		float4 SampleShadow_T2DA( ShadowContext ctxt, uint texIdx, uint sampIdx, float2 tcs, float slice, float lod = 0.0 )		  \
		{																														  \
			[unroll] for( uint i = 0; i < _Tex2DArraySlots; i++ )																  \
			{																													  \
				[unroll] for( uint j = 0; j < _SamplerSlots; j++ )																  \
				{																												  \
					[branch] if( i == texIdx && j == sampIdx )																	  \
					{																											  \
						return SAMPLE_TEXTURE2D_ARRAY_LOD( ctxt.tex2DArray[i], ctxt.samplers[j], tcs, slice, lod );				  \
					}																											  \
				}																												  \
			}																													  \
			return 1.0;																											  \
		}																														  \
																																  \
		float4 SampleCompShadow_TCA( ShadowContext ctxt, uint texIdx, uint sampIdx, float4 tcs, float cubeIdx )					  \
		{																														  \
			[unroll] for( uint i = 0; i < _TexCubeArraySlots; i++ )																  \
			{																													  \
				[unroll] for( uint j = 0; j < _SamplerCompSlots; j++ )															  \
				{																												  \
					[branch] if( i == texIdx && j == sampIdx )																	  \
					{																											  \
						return SAMPLE_TEXTURECUBE_ARRAY_SHADOW( ctxt.texCubeArray[i], ctxt.compSamplers[j], tcs, cubeIdx );		  \
					}																											  \
				}																												  \
			}																													  \
			return 1.0;																											  \
		}																														  \
																																  \
		float4 SampleShadow_TCA( ShadowContext ctxt, uint texIdx, uint sampIdx, float3 tcs, float cubeIdx, float lod = 0.0 )	  \
		{																														  \
			[unroll] for( uint i = 0; i < _TexCubeArraySlots; i++ )																  \
			{																													  \
				[unroll] for( uint j = 0; j < _SamplerSlots; j++ )																  \
				{																												  \
					[branch] if( i == texIdx && j == sampIdx )																	  \
					{																											  \
						return SAMPLE_TEXTURECUBE_ARRAY_LOD( ctxt.texCubeArray[i], ctxt.samplers[j], tcs, cubeIdx, lod );		  \
					}																											  \
				}																												  \
			}																													  \
			return 1.0;																											  \
		}
		
#endif
