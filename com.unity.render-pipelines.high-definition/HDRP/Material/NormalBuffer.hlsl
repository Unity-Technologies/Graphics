#include "CoreRP/ShaderLibrary/Packing.hlsl"
#include "CoreRP/ShaderLibrary/CommonMaterial.hlsl"

// ----------------------------------------------------------------------------
// Encoding/decoding normal buffer functions
// ----------------------------------------------------------------------------

struct NormalData
{
    float3 normalWS;
    float  perceptualRoughness;
};

#define NormalBufferType0 float4  // Must match GBufferType1 in deferred

// SSSBuffer texture declaration
TEXTURE2D(_NormalBufferTexture0);

void EncodeIntoNormalBuffer(NormalData normalData, uint2 positionSS, out NormalBufferType0 outNormalBuffer0)
{
    // The sign of the Z component of the normal MUST round-trip through the G-Buffer, otherwise
    // the reconstruction of the tangent frame for anisotropic GGX creates a seam along the Z axis.
    // The constant was eye-balled to not cause artifacts.
    // TODO: find a proper solution. E.g. we could re-shuffle the faces of the octahedron
    // s.t. the sign of the Z component round-trips.
    const float seamThreshold = 1.0 / 1024.0;
    normalData.normalWS.z = CopySign(max(seamThreshold, abs(normalData.normalWS.z)), normalData.normalWS.z);

    // RT1 - 8:8:8:8
    // Our tangent encoding is based on our normal.
#if defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN)
    // With octahedral quad packing we get an artifact for reconstructed tangent at the center of this quad. We use rect packing instead to avoid it.
    float2 octNormalWS = PackNormalOctRectEncode(normalData.normalWS);
#else
     float2 octNormalWS = PackNormalOctQuadEncode(normalData.normalWS);
#endif
    float3 packNormalWS = PackFloat2To888(saturate(octNormalWS * 0.5 + 0.5));
    // We store perceptualRoughness instead of roughness because it is perceptually linear.
    outNormalBuffer0 = float4(packNormalWS, normalData.perceptualRoughness);
}

void DecodeFromNormalBuffer(float4 normalBuffer, uint2 positionSS, out NormalData normalData)
{
    float3 packNormalWS = normalBuffer.rgb;
    float2 octNormalWS = Unpack888ToFloat2(packNormalWS);
#if defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN)
    normalData.normalWS = UnpackNormalOctRectEncode(octNormalWS * 2.0 - 1.0);
#else
    normalData.normalWS = UnpackNormalOctQuadEncode(octNormalWS * 2.0 - 1.0);
#endif
    normalData.perceptualRoughness = normalBuffer.a;
}

void DecodeFromNormalBuffer(uint2 positionSS, out NormalData normalData)
{
    float4 normalBuffer = LOAD_TEXTURE2D(_NormalBufferTexture0, positionSS);
    DecodeFromNormalBuffer(normalBuffer, positionSS, normalData);
}

// OUTPUT_NORMAL_NORMALBUFFER start from SV_Target0 as it is used during depth prepass where there is no color buffer
#define OUTPUT_NORMALBUFFER(NAME) out NormalBufferType0 MERGE_NAME(NAME, 0) : SV_Target0
#define ENCODE_INTO_NORMALBUFFER(SURFACE_DATA, UNPOSITIONSS, NAME) EncodeIntoNormalBuffer(ConvertSurfaceDataToNormalData(SURFACE_DATA), UNPOSITIONSS, MERGE_NAME(NAME, 0))

#define DECODE_FROM_NORMALBUFFER(UNPOSITIONSS, NORMAL_DATA) DecodeFromNormalBuffer(UNPOSITIONSS, NORMAL_DATA)
