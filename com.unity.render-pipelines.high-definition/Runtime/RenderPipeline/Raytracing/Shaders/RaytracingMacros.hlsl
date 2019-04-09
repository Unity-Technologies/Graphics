#ifdef SAMPLE_TEXTURE2D
#undef SAMPLE_TEXTURE2D
#define SAMPLE_TEXTURE2D(textureName, samplerName, coord2)                          	textureName.SampleLevel(samplerName, coord2, 0)
#undef SAMPLE_TEXTURE3D
#define SAMPLE_TEXTURE3D(textureName, samplerName, coord3)                      		textureName.SampleLevel(samplerName, coord3, 0)
#undef SAMPLE_TEXTURECUBE_ARRAY
#define SAMPLE_TEXTURECUBE_ARRAY(textureName, samplerName, coord3, index)                textureName.SampleLevel(samplerName, float4(coord3, index), 0)
#endif
