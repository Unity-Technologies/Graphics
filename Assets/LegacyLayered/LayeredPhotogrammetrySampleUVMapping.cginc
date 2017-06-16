// This structure abstract uv mapping inside one struct.
// It represent a mapping of any uv (with its associated tangent space for derivative if SurfaceGradient mode) - UVSet0 to 4, planar, triplanar

#define UV_MAPPING_UVSET 0
#define UV_MAPPING_PLANAR 1

// Planar/Triplanar convention for Unity in world space
void GetTriplanarCoordinate(float3 position, out float2 uvXZ, out float2 uvXY, out float2 uvZY)
{
    // Caution: This must follow the same rule as what is use for SurfaceGradient triplanar
    // TODO: Currently the normal mapping looks wrong without SURFACE_GRADIENT option because we don't handle corretly the tangent space
    uvXZ = float2(position.z, position.x);
    uvXY = float2(position.x, position.y);
    uvZY = float2(position.z, position.y);
}

struct UVMapping
{
    int mappingType;
    float2 uv;  // Current uv or planar uv

    float3 normalWS; // vertex normal
};

#define TEXTURE2D_ARGS(textureName, samplerName) Texture2D textureName, SamplerState samplerName
#define TEXTURE2D_PARAM(textureName, samplerName) textureName, samplerName
#define UNITY_SAMPLE_TEX2D_SAMPLER(tex,samplertex,coord) tex.Sample (samplertex,coord)
#define UNITY_SAMPLE_TEX2D_LOD_SAMPLER(tex,samplertex,coord, lod) tex.SampleLevel (samplertex,coord, lod)
#define UNITY_SAMPLE_TEX2D_BIAS_SAMPLER(tex,samplertex,coord, bias) tex.SampleBias (samplertex,coord, bias)

float3 UnpackNormalRGB(float4 packedNormal, float scale = 1.0)
{
    float3 normal;
    normal.xyz = packedNormal.rgb * 2.0 - 1.0;
    normal.xy *= scale;
    return normalize(normal);
}

float3 UnpackNormalAG(float4 packedNormal, float scale = 1.0)
{
    float3 normal;
    normal.xy = packedNormal.wy * 2.0 - 1.0;
    normal.xy *= scale;
    normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
    return normal;
}

float3 UnpackNormalmapRGorAG(float4 packedNormal, float scale = 1.0)
{
    // This do the trick
    packedNormal.w *= packedNormal.x;
    return UnpackNormalAG(packedNormal, scale);
}

// Regular sampling functions
#define ADD_FUNC_SUFFIX(Name) Name
#define SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping, unused) UNITY_SAMPLE_TEX2D_SAMPLER(textureName, samplerName, uvMapping)
#include "LayeredPhotogrammetrySampleUVMappingInternal.cginc"
#undef ADD_FUNC_SUFFIX
#undef SAMPLE_TEXTURE_FUNC

// Lod sampling functions
#define ADD_FUNC_SUFFIX(Name) Name##Lod
#define SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping, lod) UNITY_SAMPLE_TEX2D_LOD_SAMPLER(textureName, samplerName, uvMapping, lod)
#include "LayeredPhotogrammetrySampleUVMappingInternal.cginc"
#undef ADD_FUNC_SUFFIX
#undef SAMPLE_TEXTURE_FUNC

// Bias sampling functions
#define ADD_FUNC_SUFFIX(Name) Name##Bias
#define SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping, bias) UNITY_SAMPLE_TEX2D_BIAS_SAMPLER(textureName, samplerName, uvMapping, bias)
#include "LayeredPhotogrammetrySampleUVMappingInternal.cginc"
#undef ADD_FUNC_SUFFIX
#undef SAMPLE_TEXTURE_FUNC

// Macro to improve readibility of surface data
#define SAMPLE_UVMAPPING_TEXTURE2D(textureName, samplerName, uvMapping)             SampleUVMapping(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, 0.0) // Last 0.0 is unused
#define SAMPLE_UVMAPPING_TEXTURE2D_LOD(textureName, samplerName, uvMapping, lod)    SampleUVMappingLod(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, lod)
#define SAMPLE_UVMAPPING_TEXTURE2D_BIAS(textureName, samplerName, uvMapping, bias)  SampleUVMappingBias(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, bias)

#define SAMPLE_UVMAPPING_NORMALMAP(textureName, samplerName, uvMapping, scale)              SampleUVMappingNormal(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, scale, 0.0)
#define SAMPLE_UVMAPPING_NORMALMAP_LOD(textureName, samplerName, uvMapping, scale, lod)     SampleUVMappingNormalLod(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, scale, lod)
#define SAMPLE_UVMAPPING_NORMALMAP_BIAS(textureName, samplerName, uvMapping, scale, bias)   SampleUVMappingNormalBias(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, scale, bias)

#define SAMPLE_UVMAPPING_NORMALMAP_AG(textureName, samplerName, uvMapping, scale)              SampleUVMappingNormalAG(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, scale, 0.0)
#define SAMPLE_UVMAPPING_NORMALMAP_AG_LOD(textureName, samplerName, uvMapping, scale, lod)     SampleUVMappingNormalAGLod(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, scale, lod)
#define SAMPLE_UVMAPPING_NORMALMAP_AG_BIAS(textureName, samplerName, uvMapping, scale, bias)   SampleUVMappingNormalAGBias(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, scale, bias)

#define SAMPLE_UVMAPPING_NORMALMAP_RGB(textureName, samplerName, uvMapping, scale)              SampleUVMappingNormalRGB(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, scale, 0.0)
#define SAMPLE_UVMAPPING_NORMALMAP_RGB_LOD(textureName, samplerName, uvMapping, scale, lod)     SampleUVMappingNormalRGBLod(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, scale, lod)
#define SAMPLE_UVMAPPING_NORMALMAP_RGB_BIAS(textureName, samplerName, uvMapping, scale, bias)   SampleUVMappingNormalRGBBias(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, scale, bias)
