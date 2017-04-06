
// These functions are use to hide the handling of triplanar mapping
// Normal need a specific treatment as they use special encoding for both base and detail map
// Also we use multiple inclusion to handle the various variation for lod and bias

// param can be unused, lod or bias
float4 ADD_FUNC_SUFFIX(SampleUVMapping)(TEXTURE2D_ARGS(textureName, samplerName), UVMapping uvMapping, float param)
{
    if (uvMapping.mappingType == UV_MAPPING_TRIPLANAR)
    {
        float3 triplanarWeights = uvMapping.triplanarWeights;
        float4 val = float4(0.0, 0.0, 0.0, 0.0);

        if (triplanarWeights.x > 0.0)
            val += triplanarWeights.x * SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uvZY, param);
        if (triplanarWeights.y > 0.0)
            val += triplanarWeights.y * SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uvXZ, param);
        if (triplanarWeights.z > 0.0)
            val += triplanarWeights.z * SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uvXY, param);

        return val;
    }
    else // UV_MAPPING_UVSET / UV_MAPPING_PLANAR
    {
        return SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uv, param);
    }
}

// Nested multiple includes of the file to handle all variations of normal map (AG, RG or RGB)

// TODO: Handle BC5 format, currently this code is for DXT5nm - After the change, rename this function UnpackNormalmapRGorAG
// This version is use for the base normal map
#define ADD_NORMAL_FUNC_SUFFIX(Name) Name
#define UNPACK_NORMAL_FUNC UnpackNormalAG
#define UNPACK_DERIVATIVE_FUNC UnpackDerivativeNormalAG
#include "SampleUVMappingNormalInternal.hlsl"
#undef ADD_NORMAL_FUNC_SUFFIX
#undef UNPACK_NORMAL_FUNC
#undef UNPACK_DERIVATIVE_FUNC

// This version is for normalmap with AG encoding only. Mainly use with details map.
#define ADD_NORMAL_FUNC_SUFFIX(Name) Name##AG
#define UNPACK_NORMAL_FUNC UnpackNormalAG
#define UNPACK_DERIVATIVE_FUNC UnpackDerivativeNormalAG
#include "SampleUVMappingNormalInternal.hlsl"
#undef ADD_NORMAL_FUNC_SUFFIX
#undef UNPACK_NORMAL_FUNC
#undef UNPACK_DERIVATIVE_FUNC

// This version is for normalmap with RGB encoding only, i.e uncompress or BC7.
#define ADD_NORMAL_FUNC_SUFFIX(Name) Name##RGB
#define UNPACK_NORMAL_FUNC UnpackNormalRGB
#define UNPACK_DERIVATIVE_FUNC UnpackDerivativeNormalRGB
#include "SampleUVMappingNormalInternal.hlsl"
#undef ADD_NORMAL_FUNC_SUFFIX
#undef UNPACK_NORMAL_FUNC
#undef UNPACK_DERIVATIVE_FUNC
