#ifdef SAMPLE_TEXTURE2D
#undef SAMPLE_TEXTURE2D
#define SAMPLE_TEXTURE2D(textureName, samplerName, coord2)                          	textureName.SampleLevel(samplerName, coord2, 0)
#endif

#ifdef SAMPLE_TEXTURE3D
#undef SAMPLE_TEXTURE3D
#define SAMPLE_TEXTURE3D(textureName, samplerName, coord3)                      		textureName.SampleLevel(samplerName, coord3, 0)
#endif

#ifdef SAMPLE_TEXTURECUBE_ARRAY
#undef SAMPLE_TEXTURECUBE_ARRAY
#define SAMPLE_TEXTURECUBE_ARRAY(textureName, samplerName, coord3, index)                textureName.SampleLevel(samplerName, float4(coord3, index), 0)
#endif

#ifdef SAMPLE_TEXTURE2D_ARRAY
#undef SAMPLE_TEXTURE2D_ARRAY
#define SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, coord2, index)                  textureName.SampleLevel(samplerName, float3(coord2, index), 0)
#endif

#ifdef SAMPLE_TEXTURECUBE
#undef SAMPLE_TEXTURECUBE
#define SAMPLE_TEXTURECUBE(textureName, samplerName, coord3)                             textureName.SampleLevel(samplerName, coord3, 0)
#endif

// FXC Supports the naïve "recursive" concatenation, while DXC and C do not https://github.com/pfultz2/Cloak/wiki/C-Preprocessor-tricks,-tips,-and-idioms
// However, FXC does not support the proper pattern (the one bellow), so we only override it in the case of ray tracing subshaders for the moment. 
//Note that this should be used for all shaders when DX12 used DXC for vert/frag shaders (which it does not for the moment)
#undef MERGE_NAME
#define MERGE_NAME_CONCAT(Name, ...) Name ## __VA_ARGS__
#define MERGE_NAME(X, Y) MERGE_NAME_CONCAT(X, Y)