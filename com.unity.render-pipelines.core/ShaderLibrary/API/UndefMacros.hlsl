// Before declaring macros we need to undef some of them to avoid warnings.
// Said warnings are happening as these macros are included in HLSLSupport.cginc
// and API files re-define them.

#undef CBUFFER_START
#undef UNITY_BRANCH
#undef UNITY_FLATTEN
#undef UNITY_UNROLL
#undef UNITY_LOOP
#undef SAMPLE_DEPTH_TEXTURE
#undef SAMPLE_DEPTH_TEXTURE_LOD
#undef SAMPLE_DEPTH_TEXTURE_LOD

// This is defined in Common.hlsl and not in the API files.
#undef GLOBAL_CBUFFER_START
