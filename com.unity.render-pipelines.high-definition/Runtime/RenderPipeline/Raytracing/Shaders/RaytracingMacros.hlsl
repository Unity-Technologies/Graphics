#ifdef SAMPLE_TEXTURE2D
#undef SAMPLE_TEXTURE2D
#define SAMPLE_TEXTURE2D(textureName, samplerName, coord2)                              textureName.SampleLevel(samplerName, coord2, _RayTracingLodBias)
#endif

#ifdef PLATFORM_SAMPLE_TEXTURE2D
#undef PLATFORM_SAMPLE_TEXTURE2D
#define PLATFORM_SAMPLE_TEXTURE2D(textureName, samplerName, coord2)                     textureName.SampleLevel(samplerName, coord2, _RayTracingLodBias)
#endif

#ifdef SAMPLE_TEXTURE3D
#undef SAMPLE_TEXTURE3D
#define SAMPLE_TEXTURE3D(textureName, samplerName, coord3)                              textureName.SampleLevel(samplerName, coord3, 0)
#endif

#ifdef PLATFORM_SAMPLE_TEXTURE3D
#undef PLATFORM_SAMPLE_TEXTURE3D
#define PLATFORM_SAMPLE_TEXTURE3D(textureName, samplerName, coord3)                     textureName.SampleLevel(samplerName, coord3, 0)
#endif

#ifdef SAMPLE_TEXTURECUBE_ARRAY
#undef SAMPLE_TEXTURECUBE_ARRAY
#define SAMPLE_TEXTURECUBE_ARRAY(textureName, samplerName, coord3, index)               textureName.SampleLevel(samplerName, float4(coord3, index), 0)
#endif

#ifdef PLATFORM_SAMPLE_TEXTURECUBE_ARRAY
#undef PLATFORM_SAMPLE_TEXTURECUBE_ARRAY
#define PLATFORM_SAMPLE_TEXTURECUBE_ARRAY(textureName, samplerName, coord3, index)      textureName.SampleLevel(samplerName, float4(coord3, index), 0)
#endif

#ifdef SAMPLE_TEXTURE2D_ARRAY
#undef SAMPLE_TEXTURE2D_ARRAY
#define SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, coord2, index)                 textureName.SampleLevel(samplerName, float3(coord2, index), 0)
#endif

#ifdef PLATFORM_SAMPLE_TEXTURE2D_ARRAY
#undef PLATFORM_SAMPLE_TEXTURE2D_ARRAY
#define PLATFORM_SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, coord2, index)        textureName.SampleLevel(samplerName, float3(coord2, index), 0)
#endif

#ifdef SAMPLE_TEXTURECUBE
#undef SAMPLE_TEXTURECUBE
#define SAMPLE_TEXTURECUBE(textureName, samplerName, coord3)                            textureName.SampleLevel(samplerName, coord3, 0)
#endif

#ifdef PLATFORM_SAMPLE_TEXTURECUBE
#undef PLATFORM_SAMPLE_TEXTURECUBE
#define PLATFORM_SAMPLE_TEXTURECUBE(textureName, samplerName, coord3)                   textureName.SampleLevel(samplerName, coord3, 0)
#endif
