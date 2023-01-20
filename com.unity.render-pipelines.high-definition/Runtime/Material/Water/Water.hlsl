//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------
// SurfaceData is defined in Water.cs which generates Water.cs.hlsl
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Water/Water.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/WaterSystemDef.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/WaterUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Reflection/VolumeProjection.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ScreenSpaceLighting/ScreenSpaceLighting.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ScreenSpaceLighting/ScreenSpaceTracing.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SubsurfaceScattering/SubsurfaceScattering.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LTCAreaLight/LTCAreaLight.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/PreIntegratedFGD/PreIntegratedFGD.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/WaterTileClassification.hlsl"

#define USE_DIFFUSE_LAMBERT_BRDF

//-----------------------------------------------------------------------------
// Texture and constant buffer declaration
//-----------------------------------------------------------------------------
TEXTURE2D_X(_WaterGBufferTexture0);
TEXTURE2D_X(_WaterGBufferTexture1);
TEXTURE2D_X(_WaterGBufferTexture2);
TEXTURE2D_X(_WaterGBufferTexture3);
StructuredBuffer<WaterSurfaceProfile> _WaterSurfaceProfiles;

//-----------------------------------------------------------------------------
// Helper functions/variable specific to this material
//-----------------------------------------------------------------------------
#define WATER_FRESNEL_ZERO 0.02037318784 // IorToFresnel0(1.333f)
#define WATER_TIP_ANISOTROPY 1.0
#define WATER_BODY_ANISOTROPY 0.5
#define WATER_NORMAL_REDIRECTION_FACTOR 0.2

float3 GetNormalForShadowBias(BSDFData bsdfData)
{
    return bsdfData.lowFrequencyNormalWS;
}

float4 GetDiffuseOrDefaultColor(BSDFData bsdfData, float replace)
{
    return float4(bsdfData.diffuseColor, 0.0);
}

float GetAmbientOcclusionForMicroShadowing(BSDFData bsdfData)
{
    return 1.0f;
}

NormalData ConvertSurfaceDataToNormalData(SurfaceData surfaceData)
{
    NormalData normalData;
    normalData.normalWS = surfaceData.normalWS;
    normalData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);
    return normalData;
}

void GetSurfaceDataDebug(uint paramId, SurfaceData surfaceData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedSurfaceDataDebug(paramId, surfaceData, result, needLinearToSRGB);

    // Overide debug value output to be more readable
    switch (paramId)
    {
        case DEBUGVIEW_WATER_SURFACEDATA_NORMAL_VIEW_SPACE:
        {
            float3 vsNormal = TransformWorldToViewDir(surfaceData.normalWS);
            result = IsNormalized(vsNormal) ?  vsNormal * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        }
    }
}

void GetBSDFDataDebug(uint paramId, BSDFData bsdfData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedBSDFDataDebug(paramId, bsdfData, result, needLinearToSRGB);

    // Overide debug value output to be more readable
    switch (paramId)
    {
    case DEBUGVIEW_WATER_BSDFDATA_NORMAL_VIEW_SPACE:
        // Convert to view space
        {
            float3 vsNormal = TransformWorldToViewDir(bsdfData.normalWS);
            result = IsNormalized(vsNormal) ?  vsNormal * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        }
    }
}

void GetPBRValidatorDebug(SurfaceData surfaceData, inout float3 result)
{
    result = surfaceData.baseColor;
}

//-----------------------------------------------------------------------------
// Conversion of the surface data to bsdf data
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(uint2 positionSS, SurfaceData surfaceData)
{
    BSDFData bsdfData;
    ZERO_INITIALIZE(BSDFData, bsdfData);

    bsdfData.diffuseColor = surfaceData.baseColor;
    // The fresnel0 is the constant water's fresnel when it is actually water,
    // but we add the foam color to it. It is probably not the best solution, but it works okay.
    bsdfData.fresnel0 = WATER_FRESNEL_ZERO;

    bsdfData.normalWS = surfaceData.normalWS;
    bsdfData.lowFrequencyNormalWS = surfaceData.lowFrequencyNormalWS;

    bsdfData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);
    bsdfData.roughness = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);

    bsdfData.foam = surfaceData.foam;
    bsdfData.caustics = surfaceData.caustics;
    bsdfData.tipThickness = surfaceData.tipThickness;

    return bsdfData;
}

//-----------------------------------------------------------------------------
// conversion function for deferred

// Layout of the water gbuffer
// GBuffer0 R16G16 (32 bits)
// - GBuffer0: Bits 31 -> 0: Diffuse color (11 11 10)

// GBuffer1 R8G8B8A8 (32 bits)
// - GBuffer1: Bits 31 -> 8: Compressed Complete Normal
// - GBuffer1: Bits 7 -> 0: Discrete Perceptual Roughness (0 -> 255)

// GBuffer2 R8G8B8A8 (32 bits)
// - GBuffer1: Bits 31 -> 8: Compressed Low Frequency Normal
// - GBuffer1: Bits 7 -> 0: Discrete Foam Value (0 -> 255)

// GBuffer3 R8G8B8A8
// - GBuffer2: Bits 31 -> 16: Tip Thickness (Compressed)
// - GBuffer2: Bits 15 -> 4: Caustics
// - GBuffer2: Bits 3 -> 0:  Surface Index
uint2 SplitUInt32ToUInt16(uint input)
{
    return uint2(input & 0xFFFF, (input >> 16) & 0xFFFF);
}

uint MergeUInt16ToUInt32(uint2 input)
{
    return (input.x & 0xFFFF) | ((input.y & 0xFFFF) << 16);
}

float3 CompressNormal(float3 normal)
{
    float2 octNormal = PackNormalOctQuadEncode(normal);
    return PackFloat2To888(saturate(octNormal * 0.5 + 0.5));
}

float3 DecompressNormal(float3 cmpNormal)
{
    float2 octNormal = Unpack888ToFloat2(cmpNormal);
    return UnpackNormalOctQuadEncode(octNormal * 2.0 - 1.0);
}

float4 CompressGBuffer0(float3 diffuseColor)
{
    return float4(diffuseColor, 0.0);
}

void DecompressGBuffer0(float4 gbuffer0, inout BSDFData bsdfData)
{
    bsdfData.diffuseColor = gbuffer0.xyz;
}

float4 CompressGBuffer1(float3 normalWS, float roughness)
{
    return float4(CompressNormal(normalWS), roughness);
}

void DecompressGBuffer1(float4 gbuffer1, inout BSDFData bsdfData)
{
    // Decompress the normal vector and roughness
    bsdfData.normalWS = DecompressNormal(gbuffer1.xyz);
    bsdfData.perceptualRoughness = gbuffer1.w;
}

float4 CompressGBuffer2(float3 lowFrequencyNormalWS, float foam)
{
    return float4(CompressNormal(lowFrequencyNormalWS), saturate(foam * 2.0));
}

void DecompressGBuffer2(float4 gbuffer2, inout BSDFData bsdfData)
{
    // Decompress the low frequency normal vector and the foam
    bsdfData.lowFrequencyNormalWS = DecompressNormal(gbuffer2.xyz);
    bsdfData.foam = gbuffer2.w * 0.5;
}

float4 CompressGBuffer3(float tipThickness, float caustics, uint surfaceIndex, bool frontFace)
{
    // Compress the caustics to the [0, 1] interval before compression into the gbuffer
    caustics = caustics / (1.0 + caustics);

    // The 0xfff value used on the caustics bits is used to keep track that we are on a backface.
    // We compress the caustics and surface index into 16 bits and export those to the z and w channels
    uint cmpCausticsFrontFace = frontFace ? min((uint)(caustics * 4096.0), 4094) : 0xfff;
    uint cmpSurfaceIndex = surfaceIndex & 0xf;
    uint lower16Bits = ((cmpCausticsFrontFace & 0xfff) << 4) | surfaceIndex & 0xf;

    // We compress the tip thickness into the upper 16 bits
    uint upper16Bits = f32tof16(tipThickness);

    // Export the compressed data
    return float4((upper16Bits >> 8 & 0xFF) / 255.0f, (upper16Bits & 0xFF) / 255.0f,
                (lower16Bits >> 8 & 0xFF) / 255.0f, (lower16Bits & 0xFF) / 255.0f);
}

void DecompressGBuffer3(float4 gbuffer3, inout BSDFData bsdfData)
{
    // Repack the 4 values into two uints
    uint upper16Bits = ((uint)(gbuffer3.x * 255.0f)) << 8 | ((uint)(gbuffer3.y * 255.0f));
    uint lower16Bits = ((uint)(gbuffer3.z * 255.0f)) << 8 | ((uint)(gbuffer3.w * 255.0f));

    bsdfData.tipThickness = f16tof32(upper16Bits);
    bsdfData.frontFace = ((lower16Bits >> 4) & 0xfff) != 0xfff;
    bsdfData.caustics = bsdfData.frontFace ? ((lower16Bits >> 4) / 4096.0) : 0;

    // Decompress the caustics from the [0, 1] interval after decompression into the gbuffer
    bsdfData.caustics = bsdfData.caustics / (1.0 - bsdfData.caustics);
    bsdfData.surfaceIndex = lower16Bits & 0xf;
}

void EncodeIntoGBuffer(BSDFData bsdfData, BuiltinData builtinData
                        , uint2 positionSS
                        , out float4 outGBuffer0
                        , out float4 outGBuffer1
                        , out float4 outGBuffer2
                        , out float4 outGBuffer3)
{
    // Output to the Gbuffer0
    outGBuffer0 = CompressGBuffer0(bsdfData.diffuseColor);

    // Output to the Gbuffer1
    outGBuffer1 = CompressGBuffer1(bsdfData.normalWS, bsdfData.perceptualRoughness);

    // Output to the Gbuffer2
    outGBuffer2 = CompressGBuffer2(bsdfData.lowFrequencyNormalWS, bsdfData.foam);

    // Output to the Gbuffer3
    outGBuffer3 = CompressGBuffer3(bsdfData.tipThickness, bsdfData.caustics, _SurfaceIndex, bsdfData.frontFace);
}

uint EvaluateLightLayers(uint surfaceIndex)
{
    #if defined(RENDERING_LAYERS)
    return _WaterSurfaceProfiles[surfaceIndex].renderingLayers;
    #else
    return RENDERING_LAYERS_MASK;
    #endif
}

void DecodeFromGBuffer(uint2 positionSS, out BSDFData bsdfData, out BuiltinData builtinData)
{
    ZERO_INITIALIZE(BSDFData, bsdfData);
    ZERO_INITIALIZE(BuiltinData, builtinData);

    // Read the gbuffer values
    float4 inGBuffer0 = LOAD_TEXTURE2D_X(_WaterGBufferTexture0, positionSS);
    float4 inGBuffer1 = LOAD_TEXTURE2D_X(_WaterGBufferTexture1, positionSS);
    float4 inGBuffer2 = LOAD_TEXTURE2D_X(_WaterGBufferTexture2, positionSS);
    float4 inGBuffer3 = LOAD_TEXTURE2D_X(_WaterGBufferTexture3, positionSS);

    // Decompress the gbuffer 0
    DecompressGBuffer0(inGBuffer0, bsdfData);

    // Decompress the gbuffer 1
    DecompressGBuffer1(inGBuffer1, bsdfData);

    // Decompress the gbuffer 2
    DecompressGBuffer2(inGBuffer2, bsdfData);

    // Decompress the gbuffer 3
    DecompressGBuffer3(inGBuffer3, bsdfData);

    // Recompute the water fresnel0
    bsdfData.fresnel0 = WATER_FRESNEL_ZERO;

    // Decompress the additional data of the gbuffer3
    bsdfData.roughness = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);

    // Fill the built in data (not used for now), the useful values are re-evaluated during the post evaluate BSDF
    builtinData.renderingLayers = EvaluateLightLayers(bsdfData.surfaceIndex);
    builtinData.shadowMask0 = 1.0;
    builtinData.shadowMask1 = 1.0;
    builtinData.shadowMask2 = 1.0;
    builtinData.shadowMask3 = 1.0;
    builtinData.emissiveColor = 0.0;

    // Evaluated at the end of the pipeline, useless here.
    builtinData.bakeDiffuseLighting = 0.0;
}

void DecodeWaterFromNormalBuffer(uint2 positionSS, out NormalData normalData)
{
    float4 inGBuffer1 = LOAD_TEXTURE2D_X(_WaterGBufferTexture1, positionSS);
    // Decompress the normal vector
    normalData.normalWS = DecompressNormal(inGBuffer1.xyz);
    // Compress the roughness
    normalData.perceptualRoughness = inGBuffer1.w;
}

void DecodeWaterSurfaceIndexFromGBuffer(uint2 positionSS, out uint surfaceIndex)
{
    float4 inGBuffer3 = LOAD_TEXTURE2D_X(_WaterGBufferTexture3, positionSS);
    uint lower16Bits = ((uint)(inGBuffer3.z * 255.0f)) << 8 | ((uint)(inGBuffer3.w * 255.0f));
    surfaceIndex = lower16Bits & 0xf;
}

void DecodeWaterFrontFaceFromGBuffer(uint2 positionSS, out bool frontFace)
{
    float4 inGBuffer3 = LOAD_TEXTURE2D_X(_WaterGBufferTexture3, positionSS);
    uint lower16Bits = ((uint)(inGBuffer3.z * 255.0f)) << 8 | ((uint)(inGBuffer3.w * 255.0f));
    frontFace = ((lower16Bits >> 4) & 0xfff) != 0xfff;
}

void DecompressWaterSSRData(uint2 positionSS, out uint surfaceIndex, out bool frontFace)
{
    BSDFData bsdfData;
    ZERO_INITIALIZE(BSDFData, bsdfData);
    float4 inGBuffer3 = LOAD_TEXTURE2D_X(_WaterGBufferTexture3, positionSS);
    DecompressGBuffer3(inGBuffer3, bsdfData);
    surfaceIndex = bsdfData.surfaceIndex;
    frontFace = bsdfData.frontFace;
}

//-----------------------------------------------------------------------------
// PreLightData
//
// Make sure we respect naming conventions to reuse ShaderPassForward as is,
// ie struct (even if opaque to the ShaderPassForward) name is PreLightData,
// GetPreLightData prototype.
//-----------------------------------------------------------------------------

// Precomputed lighting data to send to the various lighting functions
struct PreLightData
{
    // Scattering
    float tipScatteringHeight;
    float bodyScatteringHeight;
    float maxRefractionDistance;
    float3 scatteringColor;
    float envRoughness;
    float3 upDirection;

    // Refraction
    float3 transparencyColor;
    float outScatteringCoefficient;
    float colorPyramidScale;
    int colorPyramidMipOffset;

    float NdotV;                     // Could be negative due to normal mapping, use ClampNdotV()
    float partLambdaV;

    // IBL
    float3 iblR;                     // Reflected specular direction, used for IBL in EvaluateBSDF_Env()
    float  iblPerceptualRoughness;

    float3 specularFGD;              // Store preintegrated BSDF for both specular and diffuse
    float  diffuseFGD;

    // Area lights (17 VGPRs)
    // TODO: 'orthoBasisViewNormal' is just a rotation around the normal and should thus be just 1x VGPR.
    float3x3 orthoBasisViewDiffuseNormal;
    float3x3 orthoBasisViewNormal;   // Right-handed view-dependent orthogonal basis around the normal (6x VGPRs)
    float3x3 ltcTransformDiffuse;    // Inverse transformation for Lambertian or Disney Diffuse        (4x VGPRs)
    float3x3 ltcTransformSpecular;   // Inverse transformation for GGX                                 (4x VGPRs)

    // Refraction
    float3 transparentRefractV;      // refracted view vector after exiting the shape
    float3 transparentPositionWS;    // start of the refracted ray after exiting the shape
    float3 transparentTransmittance; // transmittance due to absorption
    float transparentSSMipLevel;     // mip level of the screen space gaussian pyramid for rough refraction
};

//
// ClampRoughness helper specific to this material
//
void ClampRoughness(inout PreLightData preLightData, inout BSDFData bsdfData, float minRoughness)
{
    bsdfData.roughness = max(minRoughness, bsdfData.roughness);
}

void AdjustWaterNormalForSSR(WaterSurfaceProfile profile, inout float3 normal)
{
    float verticalProj = dot(normal, profile.upDirection);
    if (verticalProj < 0.0)
        normal -= profile.upDirection * verticalProj * 2.0;
}

void EvaluateSmoothnessFade(float3 positionRWS, WaterSurfaceProfile profile, inout BSDFData bsdfData)
{
    // If the surface is less rough than what the transition
    if (bsdfData.perceptualRoughness < profile.roughnessEndValue)
    {
         // Distance from the camera to the pixel
        float distanceToCamera = length(positionRWS);

        // Value that allows us to do the blending
        float blendValue = saturate((distanceToCamera - profile.smoothnessFadeStart) / profile.smoothnessFadeDistance);
        bsdfData.perceptualRoughness = lerp(bsdfData.perceptualRoughness, profile.roughnessEndValue, blendValue);
        bsdfData.roughness = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);
    }
}

// This function is call to precompute heavy calculation before lightloop
PreLightData GetPreLightData(float3 V, PositionInputs posInput, inout BSDFData bsdfData)
{
    PreLightData preLightData;
    ZERO_INITIALIZE(PreLightData, preLightData);

    // Grab the water profile of this surface
    WaterSurfaceProfile profile = _WaterSurfaceProfiles[bsdfData.surfaceIndex];

    // Make sure to apply the smoothness fade
    EvaluateSmoothnessFade(posInput.positionWS, profile, bsdfData);

    // Profile data
    preLightData.tipScatteringHeight = profile.tipScatteringHeight;
    preLightData.bodyScatteringHeight = profile.bodyScatteringHeight;
    preLightData.maxRefractionDistance = profile.maxRefractionDistance;
    preLightData.transparencyColor = profile.transparencyColor;
    preLightData.outScatteringCoefficient = profile.outScatteringCoefficient;
    preLightData.upDirection = profile.upDirection;
    preLightData.colorPyramidScale = profile.colorPyramidScale;
    preLightData.colorPyramidMipOffset = profile.colorPyramidMipOffset;
    // Evaluate the scattering color and take into account the under water ambient probe contribution as this value is only used for under-water scenarios
    preLightData.scatteringColor = profile.scatteringColor * lerp(1.0, _WaterAmbientProbe.w * GetCurrentExposureMultiplier(), profile.underWaterAmbientProbeContribution);

    float3 N = bsdfData.normalWS;
    preLightData.NdotV = dot(N, V);
    preLightData.iblPerceptualRoughness = max(profile.envPerceptualRoughness, bsdfData.perceptualRoughness);
    float clampedNdotV = ClampNdotV(preLightData.NdotV);

    float NdotVLowFrequency = dot(bsdfData.lowFrequencyNormalWS, V);
    NdotVLowFrequency = ClampNdotV(NdotVLowFrequency);
    // Handle IBL + area light + multiscattering.
    // Note: use the not modified by anisotropy iblPerceptualRoughness here.
    float specularReflectivity;
    GetPreIntegratedFGDGGXAndDisneyDiffuse(clampedNdotV, preLightData.iblPerceptualRoughness, bsdfData.fresnel0, preLightData.specularFGD, preLightData.diffuseFGD, specularReflectivity);
#ifdef USE_DIFFUSE_LAMBERT_BRDF
    preLightData.diffuseFGD = 1.0;
#endif

    float3 iblN;
    preLightData.partLambdaV = GetSmithJointGGXPartLambdaV(clampedNdotV, bsdfData.roughness);
    iblN = N;

    preLightData.iblR = reflect(-V, iblN);

    // Area light
    // UVs for sampling the LUTs
    float theta = FastACosPos(clampedNdotV); // For Area light - UVs for sampling the LUTs
    float2 uv = Remap01ToHalfTexelCoord(float2(bsdfData.perceptualRoughness, theta * INV_HALF_PI), LTC_LUT_SIZE);

    // Note we load the matrix transpose (avoid to have to transpose it in shader)
#ifdef USE_DIFFUSE_LAMBERT_BRDF
    preLightData.ltcTransformDiffuse = k_identity3x3;
#else
    // Get the inverse LTC matrix for Disney Diffuse
    preLightData.ltcTransformDiffuse      = 0.0;
    preLightData.ltcTransformDiffuse._m22 = 1.0;
    preLightData.ltcTransformDiffuse._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, s_linear_clamp_sampler, uv, LTCLIGHTINGMODEL_DISNEY_DIFFUSE, 0);
#endif

    // Get the inverse LTC matrix for GGX
    // Note we load the matrix transpose (avoid to have to transpose it in shader)
    preLightData.ltcTransformSpecular = 0.0;
    preLightData.ltcTransformSpecular._m22 = 1.0;
    preLightData.ltcTransformSpecular._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, s_linear_clamp_sampler, uv, LTCLIGHTINGMODEL_GGX, 0);

    preLightData.orthoBasisViewNormal = GetOrthoBasisViewNormal(V, N, preLightData.NdotV);
    return preLightData;
}

//-----------------------------------------------------------------------------
// light transport functions
//-----------------------------------------------------------------------------
float GetPhaseTerm(float3 lightDir, float3 V, BSDFData bsdfData, PreLightData preLightData)
{
    float3 biasedOceanLightDirection = lightDir;
    biasedOceanLightDirection.y -= ((1.f - WATER_INV_IOR) * 2.f);
    biasedOceanLightDirection = normalize(biasedOceanLightDirection);
    float3 singleScatteringRay = refract(-V, bsdfData.lowFrequencyNormalWS, WATER_INV_IOR);
    float cos0RL = dot(singleScatteringRay, biasedOceanLightDirection);
    float anisotropy = lerp(WATER_TIP_ANISOTROPY, WATER_BODY_ANISOTROPY, bsdfData.tipThickness);
    return CornetteShanksPhasePartVarying(anisotropy, cos0RL) * lerp(preLightData.tipScatteringHeight, preLightData.bodyScatteringHeight, bsdfData.tipThickness);
}

LightTransportData GetLightTransportData(SurfaceData surfaceData, BuiltinData builtinData, BSDFData bsdfData)
{
    LightTransportData lightTransportData;

    // DiffuseColor for lightmapping
    lightTransportData.diffuseColor = bsdfData.diffuseColor;
    lightTransportData.emissiveColor = builtinData.emissiveColor;

    return lightTransportData;
}

//-----------------------------------------------------------------------------
// LightLoop related function (Only include if required)
// HAS_LIGHTLOOP is define in Lighting.hlsl
//-----------------------------------------------------------------------------

#ifdef HAS_LIGHTLOOP

//-----------------------------------------------------------------------------
// BSDF share between directional light, punctual light and area light (reference)
//-----------------------------------------------------------------------------

bool IsNonZeroBSDF(float3 V, float3 L, PreLightData preLightData, BSDFData bsdfData)
{
    return true; // In order to get caustic effect and concavity
}

// This function apply BSDF. Assumes that NdotL is positive.
CBSDF EvaluateBSDF(float3 V, float3 L, PreLightData preLightData, BSDFData bsdfData)
{
    CBSDF cbsdf;
    ZERO_INITIALIZE(CBSDF, cbsdf);

    float3 N = bsdfData.normalWS;
    float NdotL_LF = dot(bsdfData.lowFrequencyNormalWS, L);
    float NdotLWrappedDiffuseLowFrequency = ComputeWrappedDiffuseLighting(NdotL_LF, 1.0f);
    float clampedNdotL_LF = saturate(NdotL_LF);
    float NdotV = preLightData.NdotV;
    float clampedNdotV = ClampNdotV(NdotV);

    float NdotL = dot(N, L);
    float clampedNdotL = saturate(NdotL);

    float LdotV, NdotH, LdotH, invLenLV;
    GetBSDFAngle(V, L, NdotL, NdotV, LdotV, NdotH, LdotH, invLenLV);

    float3 F = F_Schlick(bsdfData.fresnel0, LdotH);
    // We use abs(NdotL) to handle the none case of double sided
    float DV = DV_SmithJointGGX(NdotH, abs(NdotL), clampedNdotV, bsdfData.roughness, preLightData.partLambdaV);

#ifdef USE_DIFFUSE_LAMBERT_BRDF
    float diffTerm = Lambert();
#else
    float diffTerm = DisneyDiffuse(clampedNdotV, abs(NdotL), LdotV, bsdfData.perceptualRoughness);
#endif

    float specularSelfOcclusion = saturate(clampedNdotL_LF * 5.f);
    cbsdf.specR = F * DV * clampedNdotL * specularSelfOcclusion;

    cbsdf.diffR = diffTerm * lerp(NdotLWrappedDiffuseLowFrequency, 1.0, bsdfData.foam);

    // We don't multiply by 'bsdfData.diffuseColor' here. It is done in the EvaluateBSDF_XXX functions
    return cbsdf;
}

//-----------------------------------------------------------------------------
// Surface shading (all light types) below
//-----------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightEvaluation.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialEvaluation.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/SurfaceShading.hlsl"

// for Rotate function only
# include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl"

//-----------------------------------------------------------------------------
// EvaluateBSDF_Directional
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Directional(LightLoopContext lightLoopContext,
                                        float3 V, PositionInputs posInput, PreLightData preLightData,
                                        DirectionalLightData lightData, BSDFData bsdfData,
                                        BuiltinData builtinData)
{
    // Compute the direct lighting
    DirectLighting directLighting = ShadeSurface_Directional(lightLoopContext, posInput, builtinData, preLightData, lightData, bsdfData, V);

    // Add the foam and sub-surface scattering terms
    directLighting.diffuse = ((1.0 + GetPhaseTerm(-lightData.forward, V, bsdfData, preLightData)) * bsdfData.diffuseColor + bsdfData.foam) * directLighting.diffuse;

    // return the result
    return directLighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Punctual (supports spot, point and projector lights)
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Punctual(LightLoopContext lightLoopContext,
                                     float3 V, PositionInputs posInput,
                                     PreLightData preLightData, LightData lightData,
                                     BSDFData bsdfData, BuiltinData builtinData)
{
    // Compute the direct lighting
    DirectLighting directLighting = ShadeSurface_Punctual(lightLoopContext, posInput, builtinData, preLightData, lightData, bsdfData, V);

    // Add the foam and sub-surface scattering terms
    float3 L = GetPunctualLightVector(posInput.positionWS, lightData);
    directLighting.diffuse = ((1.0 + GetPhaseTerm(L, V, bsdfData, preLightData)) * bsdfData.diffuseColor + bsdfData.foam) * directLighting.diffuse;

    // return the result
    return directLighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Line
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Line(   LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, LightData lightData, BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    // TODO

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Rect
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Rect(   LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, LightData lightData, BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 positionWS = posInput.positionWS;

#if SHADEROPTIONS_BARN_DOOR
    // Apply the barn door modification to the light data
    RectangularLightApplyBarnDoor(lightData, positionWS);
#endif

    float3 unL = lightData.positionRWS - positionWS;
    if (dot(lightData.forward, unL) < 0.0001)
    {

         // Rotate the light direction into the light space.
        float3x3 lightToWorld = float3x3(lightData.right, lightData.up, -lightData.forward);
        unL = mul(unL, transpose(lightToWorld));

         // TODO: This could be precomputed.
        float halfWidth = lightData.size.x * 0.5;
        float halfHeight = lightData.size.y * 0.5;

         // Define the dimensions of the attenuation volume.
        // TODO: This could be precomputed.
        float range = lightData.range;
        float3 invHalfDim = rcp(float3(range + halfWidth,
                                    range + halfHeight,
                                    range));

         // Compute the light attenuation.
# ifdef ELLIPSOIDAL_ATTENUATION
        // The attenuation volume is an axis-aligned ellipsoid s.t.
        // r1 = (r + w / 2), r2 = (r + h / 2), r3 = r.
        float intensity = EllipsoidalDistanceAttenuation(unL, invHalfDim,
                                                        lightData.rangeAttenuationScale,
                                                        lightData.rangeAttenuationBias);
#else
        // The attenuation volume is an axis-aligned box s.t.
        // hX = (r + w / 2), hY = (r + h / 2), hZ = r.
        float intensity = BoxDistanceAttenuation(unL, invHalfDim,
                                                    lightData.rangeAttenuationScale,
                                                    lightData.rangeAttenuationBias);
#endif

        // Terminate if the shaded point is too far away.
        if (intensity != 0.0)
        {
            lightData.diffuseDimmer *= intensity;
            lightData.specularDimmer *= intensity;

             // Translate the light s.t. the shaded point is at the origin of the coordinate system.
            lightData.positionRWS -= positionWS;

             float4x3 lightVerts;

             // TODO: some of this could be precomputed.
            lightVerts[0] = lightData.positionRWS + lightData.right * -halfWidth + lightData.up * -halfHeight; // LL
            lightVerts[1] = lightData.positionRWS + lightData.right * -halfWidth + lightData.up *  halfHeight; // UL
            lightVerts[2] = lightData.positionRWS + lightData.right *  halfWidth + lightData.up *  halfHeight; // UR
            lightVerts[3] = lightData.positionRWS + lightData.right *  halfWidth + lightData.up * -halfHeight; // LR

             // Note: We don't have the same normal for diffuse and specular
            // Rotate the endpoints into the local coordinate system.
            float4x3 lightVertsDiff  = mul(lightVerts, transpose(preLightData.orthoBasisViewDiffuseNormal));

             float3 ltcValue;

             // Evaluate the diffuse part
            // Polygon irradiance in the transformed configuration.
            float4x3 LD = mul(lightVertsDiff, preLightData.ltcTransformDiffuse);
            ltcValue = PolygonIrradiance(LD);
            ltcValue *= lightData.diffuseDimmer;

            // TODO: re-enable this when HDRP version supports it
            // Only apply cookie if there is one
            //if (lightData.cookieMode != COOKIEMODE_NONE)
            //{
            //    // Compute the cookie data for the diffuse term
            //    float3 formFactorD = PolygonFormFactor(LD);
            //    ltcValue *= SampleAreaLightCookie(lightData.cookieScaleOffset, LD, formFactorD);
            //}

            // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
            // See comment for specular magnitude, it apply to diffuse as well
            lighting.diffuse = preLightData.diffuseFGD * ltcValue;

            // Evaluate the specular part
            float4x3 lightVertsSpec = mul(lightVerts, transpose(preLightData.orthoBasisViewNormal));

            // Polygon irradiance in the transformed configuration.
            float4x3 LS = mul(lightVertsSpec, preLightData.ltcTransformSpecular);
            ltcValue = PolygonIrradiance(LS);
            ltcValue *= lightData.specularDimmer;

            // TODO: re-enable this when HDRP version supports it
            // Only apply cookie if there is one
            //if (lightData.cookieMode != COOKIEMODE_NONE)
            //{
            //    // Compute the cookie data for the specular term
            //    float3 formFactorS = PolygonFormFactor(LS);
            //    ltcValue *= SampleAreaLightCookie(lightData.cookieScaleOffset, LS, formFactorS);
            //}

            // We need to multiply by the magnitude of the integral of the BRDF
            // ref: http://advances.realtimerendering.com/s2016/s2016_ltc_fresnel.pdf
            // This value is what we store in specularFGD, so reuse it
            lighting.specular += preLightData.specularFGD * ltcValue;

             // Save ALU by applying 'lightData.color' only once.
            lighting.diffuse *= lightData.color;
            lighting.specular *= lightData.color;

 #ifdef DEBUG_DISPLAY
            if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
            {
                // Only lighting, not BSDF
                // Apply area light on lambert then multiply by PI to cancel Lambert
                lighting.diffuse = PolygonIrradiance(mul(lightVerts, k_identity3x3));
                lighting.diffuse *= PI * lightData.diffuseDimmer;
            }
#endif
        }

    }

    // Add the foam and surface diffuse
    lighting.diffuse = lighting.diffuse * (bsdfData.diffuseColor + bsdfData.foam);

     return lighting;
}

DirectLighting EvaluateBSDF_Area(LightLoopContext lightLoopContext,
    float3 V, PositionInputs posInput,
    PreLightData preLightData, LightData lightData,
    BSDFData bsdfData, BuiltinData builtinData)
{
    /*
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_WATER_CINEMATIC))
    {
        LightWaterTransform(posInput, bsdfData, lightData.positionRWS, lightData.forward, lightData.right, lightData.up);
    }
    */
    if (lightData.lightType == GPULIGHTTYPE_TUBE)
    {
        return EvaluateBSDF_Line(lightLoopContext, V, posInput, preLightData, lightData, bsdfData, builtinData);
    }
    else
    {
        return EvaluateBSDF_Rect(lightLoopContext, V, posInput, preLightData, lightData, bsdfData, builtinData);
    }
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_SSLighting for screen space lighting
// ----------------------------------------------------------------------------

IndirectLighting EvaluateBSDF_ScreenSpaceReflection(PositionInputs posInput,
                                                    PreLightData   preLightData,
                                                    BSDFData       bsdfData,
                                                    inout float    reflectionHierarchyWeight)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    // TODO: this texture is sparse (mostly black). Can we avoid reading every texel? How about using Hi-S?
    float4 ssrLighting = LOAD_TEXTURE2D_X(_SsrLightingTexture, posInput.positionSS);
    InversePreExposeSsrLighting(ssrLighting);

    // Apply the weight on the ssr contribution (if required)
    ApplyScreenSpaceReflectionWeight(ssrLighting);

    // Set the default weight value
    reflectionHierarchyWeight = ssrLighting.a;
    lighting.specularReflected = ssrLighting.rgb * preLightData.specularFGD;

    // If we are underwater, we don't want to use the fallback hierarchy
    if (!bsdfData.frontFace)
    {
        float3 fallbackReflectionSignal = (1.0 - ssrLighting.a) * preLightData.scatteringColor * GetInverseCurrentExposureMultiplier();
        lighting.specularReflected += fallbackReflectionSignal * preLightData.specularFGD;
        reflectionHierarchyWeight = 1.0f;
    }

    // Note that we should have an attenuation here from the reflective surface to the intersection point, but for now we don't.
    return lighting;
}

IndirectLighting EvaluateBSDF_ScreenspaceRefraction(LightLoopContext lightLoopContext,
                                                    float3 V, PositionInputs posInput,
                                                    PreLightData preLightData, BSDFData bsdfData,
                                                    EnvLightData envLightData,
                                                    inout float hierarchyWeight)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    // Re-evaluate the refraction
    float3 refractedWaterPosRWS;
    float2 distortedWaterNDC;
    float refractedWaterDistance;
    float3 absorptionTint;
    ComputeWaterRefractionParams(posInput.positionWS, bsdfData.normalWS, bsdfData.lowFrequencyNormalWS,
        posInput.positionSS * _ScreenSize.zw, V, bsdfData.frontFace,
        preLightData.maxRefractionDistance, preLightData.transparencyColor, preLightData.outScatteringCoefficient,
        refractedWaterPosRWS, distortedWaterNDC, refractedWaterDistance, absorptionTint);

    // Apply a mip offset for the underwater data (if needed)
    float2 pixelCoordinates = distortedWaterNDC * _ScreenSize.xy;
    int lod = 0;
    if (!bsdfData.frontFace)
    {
        pixelCoordinates *= preLightData.colorPyramidScale;
        lod += preLightData.colorPyramidMipOffset;
    }

    // Read the camera color for the refracted ray
    float3 cameraColor = LoadCameraColor(pixelCoordinates, lod);

    if (bsdfData.frontFace)
        lighting.specularTransmitted = absorptionTint * cameraColor * absorptionTint * bsdfData.caustics * (1 - saturate(bsdfData.foam));
    else
        lighting.specularTransmitted = absorptionTint == 0.0 ? preLightData.scatteringColor : cameraColor;

    // Apply the additional attenuation, the fresnel and the exposure
    lighting.specularTransmitted *= (1.f - preLightData.specularFGD) * GetInverseCurrentExposureMultiplier();

    // Flag the hierarchy as complete, we should never be reading from the reflection probes or the sky for the refraction
    hierarchyWeight = 1;

    // we are done
    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Env
// ----------------------------------------------------------------------------

// _preIntegratedFGD and _CubemapLD are unique for each BRDF

IndirectLighting EvaluateBSDF_Env(  LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, EnvLightData lightData, BSDFData bsdfData,
                                    int influenceShapeType, int GPUImageBasedLightingType,
                                    inout float hierarchyWeight)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    float3 envLighting;
    float3 positionWS = posInput.positionWS;
    float weight = 1.0;
    float3 R = preLightData.iblR;
    float3 attenuation = 1.0f;

    if (!IsEnvIndexTexture2D(lightData.envIndex)) // ENVCACHETYPE_CUBEMAP
    {
        R = GetSpecularDominantDir(bsdfData.normalWS, R, preLightData.iblPerceptualRoughness, ClampNdotV(preLightData.NdotV));
        // When we are rough, we tend to see outward shifting of the reflection when at the boundary of the projection volume
        // Also it appear like more sharp. To avoid these artifact and at the same time get better match to reference we lerp to original unmodified reflection.
        // Formula is empirical.
        float roughness = PerceptualRoughnessToRoughness(preLightData.iblPerceptualRoughness);
        R = lerp(R, preLightData.iblR, saturate(smoothstep(0, 1, roughness * roughness)));

        // This intends to simulate indirect specular "multi bounce"
        if (bsdfData.frontFace)
        {
            float RdotUp = dot(R, preLightData.upDirection);
            if (RdotUp < 0.0)
            {
                float weight = saturate(-RdotUp * 2.0f);
                attenuation = lerp(float3(1.0, 1.0, 1.0), bsdfData.diffuseColor, weight);
            }
            R += preLightData.upDirection * WATER_NORMAL_REDIRECTION_FACTOR;
            R = normalize(R);
        }
    }

    // Note: using influenceShapeType and projectionShapeType instead of (lightData|proxyData).shapeType allow to make compiler optimization in case the type is know (like for sky)
    EvaluateLight_EnvIntersection(positionWS, bsdfData.normalWS, lightData, influenceShapeType, R, weight);

    float3 F = preLightData.specularFGD;

    float4 preLD = SampleEnv(lightLoopContext, lightData.envIndex, R, PerceptualRoughnessToMipmapLevel(preLightData.iblPerceptualRoughness), lightData.rangeCompressionFactorCompensation, posInput.positionNDC);
    weight *= preLD.a; // Used by planar reflection to discard pixel

    envLighting = F * preLD.rgb * attenuation;

    UpdateLightingHierarchyWeights(hierarchyWeight, weight);
    envLighting *= weight * lightData.multiplier;

    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION)
        lighting.specularReflected = envLighting;

    return lighting;
}

//-----------------------------------------------------------------------------
// PostEvaluateBSDF
// ----------------------------------------------------------------------------

void PostEvaluateBSDF(  LightLoopContext lightLoopContext,
                        float3 V, PositionInputs posInput,
                        PreLightData preLightData, BSDFData bsdfData, BuiltinData builtinData, AggregateLighting lighting,
                        out LightLoopOutput lightLoopOutput)
{
    // Compute the indirect diffuse term here, we do it here to save some VGPRs and increase the occupancy which is the main bottleneck here.
    float3 indirectDiffuse = _WaterAmbientProbe.xyz * (bsdfData.diffuseColor + bsdfData.foam) * preLightData.diffuseFGD * GetIndirectDiffuseMultiplier(builtinData.renderingLayers);

    // Evaluate the total diffuse and specular terms
    lightLoopOutput.diffuseLighting = lighting.direct.diffuse + indirectDiffuse;
    lightLoopOutput.specularLighting = lighting.direct.specular + lighting.indirect.specularTransmitted + lighting.indirect.specularReflected;

#ifdef DEBUG_DISPLAY
    AmbientOcclusionFactor aoFactor;
    ZERO_INITIALIZE(AmbientOcclusionFactor, aoFactor);
    PostEvaluateBSDFDebugDisplay(aoFactor, builtinData, lighting, bsdfData.diffuseColor, lightLoopOutput);
#endif
}

#endif // #ifdef HAS_LIGHTLOOP
