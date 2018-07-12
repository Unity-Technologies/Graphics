// This file contains various helper declarations for declaring and sampling members of the ShadowContext struct.

// shadow lookup routines when dynamic array access is possible
#if SHADOW_SUPPORTS_DYNAMIC_INDEXING != 0

// Shader model >= 5.1
#   define SHADOW_DEFINE_SAMPLING_FUNC_T2DA_COMP( _Tex2DArraySlots  , _SamplerCompSlots )   real4 SampleCompShadow_T2DA( ShadowContext ctxt, uint texIdx, uint sampIdx, real3 tcs, real slice                   )   { return SAMPLE_TEXTURE2D_ARRAY_SHADOW( ctxt.tex2DArray[texIdx], ctxt.compSamplers[sampIdx], tcs, slice );      }
#   define SHADOW_DEFINE_SAMPLING_FUNC_T2DA_SAMP( _Tex2DArraySlots  , _SamplerSlots )       real4 SampleShadow_T2DA(      ShadowContext ctxt, uint texIdx, uint sampIdx, real2 tcs, real slice, real lod = 0.0  )   { return SAMPLE_TEXTURE2D_ARRAY_LOD( ctxt.tex2DArray[texIdx], ctxt.samplers[sampIdx], tcs, slice, lod );        }
#   define SHADOW_DEFINE_SAMPLING_FUNC_T2DA_LOAD( _Tex2DArraySlots  )                       real4 LoadShadow_T2DA(        ShadowContext ctxt, uint texIdx,               uint2  tcs, uint  slice, uint lod = 0      )   { return LOAD_TEXTURE2D_ARRAY_LOD( ctxt.tex2DArray[texIdx], tcs, slice, lod ).x;                                }
#   define SHADOW_DEFINE_SAMPLING_FUNC_TCA_COMP(  _TexCubeArraySlots, _SamplerCompSlots )   real4 SampleCompShadow_TCA(  ShadowContext ctxt, uint texIdx, uint sampIdx, real4 tcs, real cubeIdx                 )   { return SAMPLE_TEXTURECUBE_ARRAY_SHADOW( ctxt.texCubeArray[texIdx], ctxt.compSamplers[sampIdx], tcs, cubeIdx );}
#   define SHADOW_DEFINE_SAMPLING_FUNC_TCA_SAMP(  _TexCubeArraySlots, _SamplerSlots )       real4 SampleShadow_TCA(   ShadowContext ctxt, uint texIdx, uint sampIdx, real3 tcs, real cubeIdx, real lod = 0.0 )  { return SAMPLE_TEXTURECUBE_ARRAY_LOD( ctxt.texCubeArray[texIdx], ctxt.samplers[sampIdx], tcs, cubeIdx, lod );  }

#else // helper macros if dynamic indexing does not work

// Sampler and texture combinations are static. No shadowmap will ever be sampled with two different samplers.
// Once shadowmaps and samplers are fixed consider writing custom dispatchers directly accessing textures and samplers.
#   define SHADOW_DEFINE_SAMPLING_FUNC_T2DA_COMP( _Tex2DArraySlots, _SamplerCompSlots )                                         \
        real4 SampleCompShadow_T2DA( ShadowContext ctxt, uint texIdx, uint sampIdx, real3 tcs, real slice )                     \
        {                                                                                                                       \
            real4 res = 1.0.xxxx;                                                                                               \
            UNITY_UNROLL for( uint i = 0; i < _Tex2DArraySlots; i++ )                                                           \
            {                                                                                                                   \
                UNITY_UNROLL for( uint j = 0; j < _SamplerCompSlots; j++ )                                                      \
                {                                                                                                               \
                    UNITY_BRANCH if( i == texIdx && j == sampIdx )                                                              \
                    {                                                                                                           \
                        res = SAMPLE_TEXTURE2D_ARRAY_SHADOW( ctxt.tex2DArray[i], ctxt.compSamplers[j], tcs, slice );            \
                        break;                                                                                                  \
                    }                                                                                                           \
                }                                                                                                               \
            }                                                                                                                   \
            return res;                                                                                                         \
        }

#   define SHADOW_DEFINE_SAMPLING_FUNC_T2DA_SAMP( _Tex2DArraySlots, _SamplerSlots )                                             \
        real4 SampleShadow_T2DA( ShadowContext ctxt, uint texIdx, uint sampIdx, real2 tcs, real slice, real lod = 0.0 )         \
        {                                                                                                                       \
            real4 res = 1.0.xxxx;                                                                                               \
            UNITY_UNROLL for( uint i = 0; i < _Tex2DArraySlots; i++ )                                                           \
            {                                                                                                                   \
                UNITY_UNROLL for( uint j = 0; j < _SamplerSlots; j++ )                                                          \
                {                                                                                                               \
                    UNITY_BRANCH if( i == texIdx && j == sampIdx )                                                              \
                    {                                                                                                           \
                        res = SAMPLE_TEXTURE2D_ARRAY_LOD( ctxt.tex2DArray[i], ctxt.samplers[j], tcs, slice, lod );              \
                        break;                                                                                                  \
                    }                                                                                                           \
                }                                                                                                               \
            }                                                                                                                   \
            return res;                                                                                                         \
        }

#   define SHADOW_DEFINE_SAMPLING_FUNC_T2DA_LOAD( _Tex2DArraySlots )                                                            \
        real LoadShadow_T2DA( ShadowContext ctxt, uint texIdx, uint2 tcs, uint slice, uint lod = 0 )                            \
        {                                                                                                                       \
            real res = 1.0;                                                                                                     \
            UNITY_UNROLL for( uint i = 0; i < _Tex2DArraySlots; i++ )                                                           \
            {                                                                                                                   \
                UNITY_BRANCH if( i == texIdx )                                                                                  \
                {                                                                                                               \
                    res = LOAD_TEXTURE2D_ARRAY_LOD( ctxt.tex2DArray[i], tcs, slice, lod ).x;                                    \
                    break;                                                                                                      \
                }                                                                                                               \
            }                                                                                                                   \
            return res;                                                                                                         \
        }


#   define SHADOW_DEFINE_SAMPLING_FUNC_TCA_COMP( _TexCubeArraySlots, _SamplerCompSlots )                                        \
        real4 SampleCompShadow_TCA( ShadowContext ctxt, uint texIdx, uint sampIdx, real4 tcs, real cubeIdx )                    \
        {                                                                                                                       \
            real4 res = 1.0.xxxx;                                                                                               \
            UNITY_UNROLL for( uint i = 0; i < _TexCubeArraySlots; i++ )                                                         \
            {                                                                                                                   \
                UNITY_UNROLL for( uint j = 0; j < _SamplerCompSlots; j++ )                                                      \
                {                                                                                                               \
                    UNITY_BRANCH if( i == texIdx && j == sampIdx )                                                              \
                    {                                                                                                           \
                        res = SAMPLE_TEXTURECUBE_ARRAY_SHADOW( ctxt.texCubeArray[i], ctxt.compSamplers[j], tcs, cubeIdx );      \
                        break;                                                                                                  \
                    }                                                                                                           \
                }                                                                                                               \
            }                                                                                                                   \
            return res;                                                                                                         \
        }

#   define SHADOW_DEFINE_SAMPLING_FUNC_TCA_SAMP( _TexCubeArraySlots, _SamplerSlots )                                            \
        real4 SampleShadow_TCA( ShadowContext ctxt, uint texIdx, uint sampIdx, real3 tcs, real cubeIdx, real lod = 0.0 )        \
        {                                                                                                                       \
            real4 res = 1.0.xxxx;                                                                                               \
            UNITY_UNROLL for( uint i = 0; i < _TexCubeArraySlots; i++ )                                                         \
            {                                                                                                                   \
                UNITY_UNROLL for( uint j = 0; j < _SamplerSlots; j++ )                                                          \
                {                                                                                                               \
                    UNITY_BRANCH if( i == texIdx && j == sampIdx )                                                              \
                    {                                                                                                           \
                        res = SAMPLE_TEXTURECUBE_ARRAY_LOD( ctxt.texCubeArray[i], ctxt.samplers[j], tcs, cubeIdx, lod );        \
                        break;                                                                                                  \
                    }                                                                                                           \
                }                                                                                                               \
            }                                                                                                                   \
            return res;                                                                                                         \
        }
#endif // SHADOW_SUPPORTS_DYNAMIC_INDEXING != 0

// helper macro to suppress code generation if _cnt is 0
#define SHADOW_DECLARE_SAMPLING_FUNC_T2DA_COMP( _Tex2DArraySlots  , _SamplerCompSlots ) real4 SampleCompShadow_T2DA( ShadowContext ctxt, uint texIdx, uint sampIdx, real3 tcs, real slice                   );
#define SHADOW_DECLARE_SAMPLING_FUNC_T2DA_SAMP( _Tex2DArraySlots  , _SamplerSlots )     real4 SampleShadow_T2DA(      ShadowContext ctxt, uint texIdx, uint sampIdx, real2 tcs, real slice, real lod = 0.0  );
#define SHADOW_DECLARE_SAMPLING_FUNC_T2DA_LOAD( _Tex2DArraySlots  )                     real4 LoadShadow_T2DA(        ShadowContext ctxt, uint texIdx,               uint2  tcs, uint  slice, uint lod = 0      );
#define SHADOW_DECLARE_SAMPLING_FUNC_TCA_COMP(  _TexCubeArraySlots, _SamplerCompSlots ) real4 SampleCompShadow_TCA(  ShadowContext ctxt, uint texIdx, uint sampIdx, real4 tcs, real cubeIdx                 );
#define SHADOW_DECLARE_SAMPLING_FUNC_TCA_SAMP(  _TexCubeArraySlots, _SamplerSlots )     real4 SampleShadow_TCA(   ShadowContext ctxt, uint texIdx, uint sampIdx, real3 tcs, real cubeIdx, real lod = 0.0 );

#define SHADOW_CAT( _left, _right ) _left ## _right

#define SHADOW_CHECK_0( _macro )
#define SHADOW_CHECK_1( _macro ) _macro
#define SHADOW_CHECK_2( _macro ) _macro
#define SHADOW_CHECK_3( _macro ) _macro
#define SHADOW_CHECK_4( _macro ) _macro
#define SHADOW_CHECK_5( _macro ) _macro
#define SHADOW_CHECK_6( _macro ) _macro
#define SHADOW_CHECK_7( _macro ) _macro
#define SHADOW_CHECK_8( _macro ) _macro
#define SHADOW_CHECK_9( _macro ) _macro
#define SHADOW_CHECK( _cnt, _macro ) SHADOW_CAT( SHADOW_CHECK_ , _cnt ) ( _macro )


#define SHADOW_CHECK_ALT_0( _macro, _alt ) _alt
#define SHADOW_CHECK_ALT_1( _macro, _alt ) _macro
#define SHADOW_CHECK_ALT_2( _macro, _alt ) _macro
#define SHADOW_CHECK_ALT_3( _macro, _alt ) _macro
#define SHADOW_CHECK_ALT_4( _macro, _alt ) _macro
#define SHADOW_CHECK_ALT_5( _macro, _alt ) _macro
#define SHADOW_CHECK_ALT_6( _macro, _alt ) _macro
#define SHADOW_CHECK_ALT_7( _macro, _alt ) _macro
#define SHADOW_CHECK_ALT_8( _macro, _alt ) _macro
#define SHADOW_CHECK_ALT_9( _macro, _alt ) _macro
#define SHADOW_CHECK_ALT( _cnt, _macro, _alt ) SHADOW_CAT( SHADOW_CHECK_ALT_ , _cnt ) ( _macro, _alt )

// helper macro to declare texture members for the shadow context.
#define SHADOWCONTEXT_DECLARE_TEXTURES( _Tex2DArraySlots, _TexCubeArraySlots, _SamplerCompSlots, _SamplerSlots )    \
    SHADOW_CHECK( _Tex2DArraySlots  , Texture2DArray            tex2DArray[_Tex2DArraySlots];       )               \
    SHADOW_CHECK( _TexCubeArraySlots, TextureCubeArray          texCubeArray[_TexCubeArraySlots];   )               \
    SHADOW_CHECK( _SamplerCompSlots , SamplerComparisonState    compSamplers[_Tex2DArraySlots];     )               \
    SHADOW_CHECK( _SamplerSlots     , SamplerState              samplers[_Tex2DArraySlots];         )
// helper macro to declare texture sampling functions for the shadow context.
#define SHADOW_DEFINE_SAMPLING_FUNCS( _Tex2DArraySlots, _TexCubeArraySlots, _SamplerCompSlots, _SamplerSlots )  \
    SHADOW_CHECK( _Tex2DArraySlots  , SHADOW_CHECK_ALT( _SamplerCompSlots, SHADOW_DEFINE_SAMPLING_FUNC_T2DA_COMP( _Tex2DArraySlots, _SamplerCompSlots ), SHADOW_DECLARE_SAMPLING_FUNC_T2DA_COMP( _Tex2DArraySlots, _SamplerCompSlots ) ) )  \
    SHADOW_CHECK( _Tex2DArraySlots  , SHADOW_CHECK_ALT( _SamplerSlots    , SHADOW_DEFINE_SAMPLING_FUNC_T2DA_SAMP( _Tex2DArraySlots, _SamplerSlots     ), SHADOW_DECLARE_SAMPLING_FUNC_T2DA_SAMP( _Tex2DArraySlots, _SamplerSlots     ) ) )  \
                                      SHADOW_CHECK_ALT( _Tex2DArraySlots , SHADOW_DEFINE_SAMPLING_FUNC_T2DA_LOAD( _Tex2DArraySlots                    ), SHADOW_DECLARE_SAMPLING_FUNC_T2DA_LOAD( _Tex2DArraySlots                    ) )    \
    SHADOW_CHECK( _TexCubeArraySlots, SHADOW_CHECK_ALT( _SamplerCompSlots, SHADOW_DEFINE_SAMPLING_FUNC_TCA_COMP(_TexCubeArraySlots, _SamplerCompSlots ), SHADOW_DECLARE_SAMPLING_FUNC_TCA_COMP(_TexCubeArraySlots, _SamplerCompSlots ) ) )  \
    SHADOW_CHECK( _TexCubeArraySlots, SHADOW_CHECK_ALT( _SamplerSlots    , SHADOW_DEFINE_SAMPLING_FUNC_TCA_SAMP(_TexCubeArraySlots, _SamplerSlots     ), SHADOW_DECLARE_SAMPLING_FUNC_TCA_SAMP(_TexCubeArraySlots, _SamplerSlots     ) ) )
