
// These functions are use to hide the handling of triplanar mapping
// Normal need a specific treatment as they use special encoding for both base and detail map
// Also we use multiple inclusion to handle the various variation for lod and bias

// param can be unused, lod or bias
float4 ADD_FUNC_SUFFIX(SampleUVMapping)(TEXTURE2D_ARGS(textureName, samplerName), UVMapping uvMapping, float param)
{
    {
        return SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uv, param);
    }
}

// Nested multiple includes of the file to handle all variations of normal map (AG, RG or RGB)

// This version is use for the base normal map (BC5 or DXT5nm)
#define ADD_NORMAL_FUNC_SUFFIX(Name) Name
#if defined(UNITY_NO_DXT5nm)
#define UNPACK_NORMAL_FUNC UnpackNormalRGB
#else
#define UNPACK_NORMAL_FUNC UnpackNormalmapRGorAG
#endif
float3 ADD_FUNC_SUFFIX(ADD_NORMAL_FUNC_SUFFIX(SampleUVMappingNormal))(TEXTURE2D_ARGS(textureName, samplerName), UVMapping uvMapping, float scale, float param)
{
    return UNPACK_NORMAL_FUNC(SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uv, param), scale);
}
#undef ADD_NORMAL_FUNC_SUFFIX
#undef UNPACK_NORMAL_FUNC

// This version is for normalmap with AG encoding only. Use with details map encoded with others properties (like smoothness).
#define ADD_NORMAL_FUNC_SUFFIX(Name) Name##AG
#define UNPACK_NORMAL_FUNC UnpackNormalAG
float3 ADD_FUNC_SUFFIX(ADD_NORMAL_FUNC_SUFFIX(SampleUVMappingNormal))(TEXTURE2D_ARGS(textureName, samplerName), UVMapping uvMapping, float scale, float param)
{
    return UNPACK_NORMAL_FUNC(SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uv, param), scale);
}
#undef ADD_NORMAL_FUNC_SUFFIX
#undef UNPACK_NORMAL_FUNC

// This version is for normalmap with RGB encoding only, i.e uncompress or BC7.
#define ADD_NORMAL_FUNC_SUFFIX(Name) Name##RGB
#define UNPACK_NORMAL_FUNC UnpackNormalRGB
float3 ADD_FUNC_SUFFIX(ADD_NORMAL_FUNC_SUFFIX(SampleUVMappingNormal))(TEXTURE2D_ARGS(textureName, samplerName), UVMapping uvMapping, float scale, float param)
{
    return UNPACK_NORMAL_FUNC(SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uv, param), scale);
}
#undef ADD_NORMAL_FUNC_SUFFIX
#undef UNPACK_NORMAL_FUNC
