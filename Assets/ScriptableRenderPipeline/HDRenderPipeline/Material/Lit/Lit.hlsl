//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------

// SurfaceData is define in Lit.cs which generate Lit.cs.hlsl
#include "Lit.cs.hlsl"
#include "SubsurfaceScatteringProfile.cs.hlsl"

// In case we pack data uint16 buffer we need to change the output render target format to uint16
// TODO: Is there a way to automate these output type based on the format declare in lit.cs ?
#if SHADEROPTIONS_PACK_GBUFFER_IN_U16
#define GBufferType0 uint4
#define GBufferType1 uint4

// TODO: How to abstract that ? We would like to avoid this PS4 test here
#ifdef SHADER_API_PS4
// On PS4 we need to specify manually the format of the output render target, output type is not enough
#pragma PSSL_target_output_format(target 0 FMT_UINT16_ABGR)
#pragma PSSL_target_output_format(target 1 FMT_UINT16_ABGR)
#endif

#else
#define GBufferType0 float4
#define GBufferType1 float4
#define GBufferType2 float4
#define GBufferType3 float4
#endif

// Reference Lambert diffuse / GGX Specular for IBL and area lights
#ifdef HAS_LIGHTLOOP // Both reference define below need to be define only if LightLoop is present, else we get a compile error
//#define LIT_DISPLAY_REFERENCE_AREA
//#define LIT_DISPLAY_REFERENCE_IBL
#endif
// Use Lambert diffuse instead of Disney diffuse
// #define LIT_DIFFUSE_LAMBERT_BRDF
// Use optimization of Precomputing LambdaV
// TODO: Test if this is a win
// #define LIT_USE_BSDF_PRE_LAMBDAV
// TODO: Check if anisotropy with a dynamic if on anisotropy > 0 is performant. Because it may mean we always calculate both isotropy and anisotropy case.
// Maybe we should always calculate anisotropy in case of standard ? Don't think the compile can optimize correctly.

SamplerState ltc_linear_clamp_sampler;
// TODO: This one should be set into a constant Buffer at pass frequency (with _Screensize)
TEXTURE2D(_PreIntegratedFGD);
TEXTURE2D_ARRAY(_LtcData); // We pack the 3 Ltc data inside a texture array
#define LTC_GGX_MATRIX_INDEX 0 // RGBA
#define LTC_DISNEY_DIFFUSE_MATRIX_INDEX 1 // RGBA
#define LTC_MULTI_GGX_FRESNEL_DISNEY_DIFFUSE_INDEX 2 // RGB, A unused
#define LTC_LUT_SIZE   64
#define LTC_LUT_SCALE  ((LTC_LUT_SIZE - 1) * rcp(LTC_LUT_SIZE))
#define LTC_LUT_OFFSET (0.5 * rcp(LTC_LUT_SIZE))

#define MIN_N_DOT_V    0.0001               // The minimum value of 'NdotV'
#define SSS_WRAP_ANGLE (PI/12)              // Used for wrap lighting
#define SSS_WRAP_LIGHT cos(PI/2 - SSS_WRAP_ANGLE)

CBUFFER_START(UnitySSSParameters)
uint   _EnableSSSAndTransmission;           // Globally toggles subsurface and transmission scattering on/off
uint   _TexturingModeFlags;                 // 1 bit/profile; 0 = PreAndPostScatter, 1 = PostScatter
uint   _TransmissionFlags;                  // 2 bit/profile; 0 = inf. thick, 1 = thin, 2 = regular
// Use float4 to avoid any packing issue between compute and pixel shaders
float4  _ThicknessRemaps[SSS_N_PROFILES];   // R: start, G = end - start, BA unused
float4 _ShapeParams[SSS_N_PROFILES];        // RGB = S = 1 / D, A = filter radius
float4 _TransmissionTints[SSS_N_PROFILES];  // RGB = color, A = unused
CBUFFER_END

//-----------------------------------------------------------------------------
// Ligth and material classification for the deferred rendering path
// Configure what kind of combination is supported
//-----------------------------------------------------------------------------

// Lighting architecture and material are suppose to be decoupled files.
// However as we use material classification it is hard to be fully separated
// the dependecy is define in this include where there is shared define for material and lighting in case of deferred material.
// If a user do a lighting architecture without material classification, this can be remove
#include "../../Lighting/TilePass/TilePass.cs.hlsl"

// Combination need to be define in increasing "comlexity" order as define by FeatureFlagsToTileVariant
static const uint kFeatureVariantFlags[NUM_FEATURE_VARIANTS] =
{
    // Standard
    /*  0 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /*  1 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_AREA | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /*  2 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /*  3 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /*  4 */ LIGHT_FEATURE_MASK_FLAGS | MATERIALFEATUREFLAGS_LIT_STANDARD,

    // SSS
    /*  5 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | MATERIALFEATUREFLAGS_LIT_SSS,
    /*  6 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_AREA | MATERIALFEATUREFLAGS_LIT_SSS,
    /*  7 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_SSS,
    /*  8 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_SSS,
    /*  9 */ LIGHT_FEATURE_MASK_FLAGS | MATERIALFEATUREFLAGS_LIT_SSS,

    // Specular/Aniso
    /* 10 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | MATERIALFEATUREFLAGS_LIT_ANISO,
    /* 11 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_AREA | MATERIALFEATUREFLAGS_LIT_ANISO,
    /* 12 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_ANISO,
    /* 13 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_ANISO,
    /* 14 */ LIGHT_FEATURE_MASK_FLAGS | MATERIALFEATUREFLAGS_LIT_ANISO,

    // SSS is a superset of material standard. With foliage or crowd SSS and standard can overlap a lot, better to have a dedicated combination
    /* 15 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | MATERIALFEATUREFLAGS_LIT_SSS | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 16 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_AREA | MATERIALFEATUREFLAGS_LIT_SSS | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 17 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_SSS | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 18 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_SSS | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 19 */ LIGHT_FEATURE_MASK_FLAGS | MATERIALFEATUREFLAGS_LIT_SSS,

    // Future usage
    /* 20 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | MATERIALFEATUREFLAGS_LIT_UNUSED0,
    /* 21 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_AREA | MATERIALFEATUREFLAGS_LIT_UNUSED0,
    /* 22 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_UNUSED0,
    /* 23 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_UNUSED0,
    /* 24 */ LIGHT_FEATURE_MASK_FLAGS | MATERIALFEATUREFLAGS_LIT_UNUSED0,

    // Future usage
    /* 25 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | MATERIALFEATUREFLAGS_LIT_UNUSED1,
    /* 26 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_AREA | MATERIALFEATUREFLAGS_LIT_UNUSED1,
    /* 27 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_UNUSED1,
    /* 28 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_UNUSED1,
    /* 29 */ LIGHT_FEATURE_MASK_FLAGS | MATERIALFEATUREFLAGS_LIT_UNUSED1,

    /* 30 */ LIGHT_FEATURE_MASK_FLAGS | MATERIAL_FEATURE_MASK_FLAGS, // Catch all case with MATERIAL_FEATURE_MASK_FLAGS is needed in case we disable material classification
};

uint FeatureFlagsToTileVariant(uint featureFlags)
{
    for (int i = 0; i < NUM_FEATURE_VARIANTS; i++)
    {
        if ((featureFlags & kFeatureVariantFlags[i]) == featureFlags)
            return i;
    }
    return NUM_FEATURE_VARIANTS - 1;
}

// This function need to return a compile time value, else there is no optimization
uint TileVariantToFeatureFlags(uint variant)
{
    return kFeatureVariantFlags[variant];
}

//-----------------------------------------------------------------------------
// Helper functions/variable specific to this material
//-----------------------------------------------------------------------------

float PackMaterialId(int materialId)
{
    return float(materialId) / 3.0;
}

int UnpackMaterialId(float f)
{
    return int(round(f * 3.0));
}

// For image based lighting, a part of the BSDF is pre-integrated.
// This is done both for specular and diffuse (in case of DisneyDiffuse)
void GetPreIntegratedFGD(float NdotV, float perceptualRoughness, float3 fresnel0, out float3 specularFGD, out float diffuseFGD)
{
    // Pre-integrate GGX FGD
    //  _PreIntegratedFGD.x = Gv * (1 - Fc)  with Fc = (1 - H.L)^5
    //  _PreIntegratedFGD.y = Gv * Fc
    // Pre integrate DisneyDiffuse FGD:
    // _PreIntegratedFGD.z = DisneyDiffuse
    float3 preFGD = SAMPLE_TEXTURE2D_LOD(_PreIntegratedFGD, ltc_linear_clamp_sampler, float2(NdotV, perceptualRoughness), 0).xyz;

    // f0 * Gv * (1 - Fc) + Gv * Fc
    specularFGD = fresnel0 * preFGD.x + preFGD.y;

#ifdef LIT_DIFFUSE_LAMBERT_BRDF
    diffuseFGD = 1.0;
#else
    diffuseFGD = preFGD.z;
#endif
}

void ApplyDebugToBSDFData(inout BSDFData bsdfData)
{
#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_SPECULAR_LIGHTING)
    {
        bool overrideSmoothness = _DebugLightingSmoothness.x != 0.0;
        float overrideSmoothnessValue = _DebugLightingSmoothness.y;

        if (overrideSmoothness)
        {
            bsdfData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(overrideSmoothnessValue);
            bsdfData.roughness = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);
        }
    }

    if (_DebugLightingMode == DEBUGLIGHTINGMODE_DIFFUSE_LIGHTING)
    {
        bsdfData.diffuseColor = _DebugLightingAlbedo.xyz;
    }
#endif
}

static const float3 convertSpecularToValue = float3(0.02, 0.04, 0.20);

void FillMaterialIdStandardData(float3 baseColor, int specular, float metallic, inout BSDFData bsdfData)
{
    bsdfData.diffuseColor = baseColor * (1.0 - metallic);
    float val = convertSpecularToValue[specular];
    bsdfData.fresnel0 = lerp(val.xxx, baseColor, metallic);
}

void FillMaterialIdAnisoData(float roughness, float3 normalWS, float3 tangentWS, float anisotropy, inout BSDFData bsdfData)
{
    bsdfData.tangentWS = tangentWS;
    bsdfData.bitangentWS = cross(normalWS, tangentWS);
    ConvertAnisotropyToRoughness(roughness, anisotropy, bsdfData.roughnessT, bsdfData.roughnessB);
    bsdfData.anisotropy = anisotropy;
}

void FillMaterialIdSSSData(float3 baseColor, int subsurfaceProfile, float subsurfaceRadius, float thickness, inout BSDFData bsdfData)
{
    bsdfData.diffuseColor = baseColor;

    // TODO take from subsurfaceProfile
    bsdfData.fresnel0 = 0.04; // Should be 0.028 for the skin
    bsdfData.subsurfaceProfile = subsurfaceProfile;
    bsdfData.subsurfaceRadius  = subsurfaceRadius;
    bsdfData.thickness         = _ThicknessRemaps[subsurfaceProfile].x +
                                 _ThicknessRemaps[subsurfaceProfile].y * thickness;

    uint transmissionMode = BitFieldExtract(_TransmissionFlags, 2u, 2u * subsurfaceProfile);

    bsdfData.enableTransmission = transmissionMode != SSS_TRSM_MODE_NONE && (_EnableSSSAndTransmission > 0);
    bsdfData.useThinObjectMode  = transmissionMode == SSS_TRSM_MODE_THIN;

    if (bsdfData.enableTransmission)
    {
        bsdfData.transmittance = ComputeTransmittance(_ShapeParams[subsurfaceProfile].rgb,
                                                      _TransmissionTints[subsurfaceProfile].rgb,
                                                      bsdfData.thickness, bsdfData.subsurfaceRadius);
    }

    bool performPostScatterTexturing = IsBitSet(_TexturingModeFlags, subsurfaceProfile);

#if defined(SHADERPASS) && (SHADERPASS == SHADERPASS_LIGHT_TRANSPORT) // In case of GI pass don't modify the diffuseColor
    if (0)
#else
    if (_EnableSSSAndTransmission != 0) // If we globally disable SSS effect, don't modify diffuseColor
#endif
    {
        // We modify the albedo here as this code is used by all lighting (including light maps and GI).
        if (performPostScatterTexturing)
        {
        #ifndef SSS_PASS
            bsdfData.diffuseColor = float3(1.0, 1.0, 1.0);
        #endif
        }
        else
        {
            bsdfData.diffuseColor = sqrt(bsdfData.diffuseColor);
        }
    }
}

//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(SurfaceData surfaceData)
{
    BSDFData bsdfData;
    ZERO_INITIALIZE(BSDFData, bsdfData);

    bsdfData.specularOcclusion = surfaceData.specularOcclusion;
    bsdfData.normalWS = surfaceData.normalWS;
    bsdfData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);
    bsdfData.roughness = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);
    bsdfData.materialId = surfaceData.materialId;

    // IMPORTANT: In case of foward or gbuffer pass we must know what we are statically, so compiler can do compile time optimization
    if (bsdfData.materialId == MATERIALID_LIT_STANDARD)
    {
        if (surfaceData.specular == SPECULARVALUE_SPECULAR_COLOR)
        {
            bsdfData.diffuseColor = surfaceData.baseColor;
            bsdfData.fresnel0 = surfaceData.specularColor;
        }
        else
        {
            FillMaterialIdStandardData(surfaceData.baseColor, surfaceData.specular, surfaceData.metallic, bsdfData);
        }
    }
    else if (bsdfData.materialId == MATERIALID_LIT_ANISO)
    {
        FillMaterialIdStandardData(surfaceData.baseColor, surfaceData.specular, surfaceData.metallic, bsdfData);
        FillMaterialIdAnisoData(bsdfData.roughness, surfaceData.normalWS, surfaceData.tangentWS, surfaceData.anisotropy, bsdfData);
    }
    else if (bsdfData.materialId == MATERIALID_LIT_SSS)
    {
        FillMaterialIdSSSData(surfaceData.baseColor, surfaceData.subsurfaceProfile, surfaceData.subsurfaceRadius, surfaceData.thickness, bsdfData);
    }

    ApplyDebugToBSDFData(bsdfData);

    return bsdfData;
}

//-----------------------------------------------------------------------------
// conversion function for deferred
//-----------------------------------------------------------------------------

// Encode SurfaceData (BSDF parameters) into GBuffer
// Must be in sync with RT declared in HDRenderPipeline.cs ::Rebuild
void EncodeIntoGBuffer( SurfaceData surfaceData,
                        float3 bakeDiffuseLighting,
                        #if SHADEROPTIONS_PACK_GBUFFER_IN_U16
                        out GBufferType0 outGBufferU0,
                        out GBufferType1 outGBufferU1
                        #else
                        out GBufferType0 outGBuffer0,
                        out GBufferType1 outGBuffer1,
                        out GBufferType2 outGBuffer2,
                        out GBufferType3 outGBuffer3
                        #endif
                        )
{
    #if SHADEROPTIONS_PACK_GBUFFER_IN_U16
    float4 outGBuffer0, outGBuffer1, outGBuffer2, outGBuffer3;
    #endif

    // RT0 - 8:8:8:8 sRGB
    outGBuffer0 = float4(surfaceData.baseColor, surfaceData.specularOcclusion);

    // RT1 - 10:10:10:2
    // We store perceptualRoughness instead of roughness because it save a sqrt ALU when decoding
    // (as we want both perceptualRoughness and roughness for the lighting due to Disney Diffuse model)
    // Encode normal on 20bit with oct compression + 2bit of sign
    float2 octNormalWS = PackNormalOctEncode(surfaceData.normalWS);
    // To have more precision encode the sign of xy in a separate uint
    uint octNormalSign = (octNormalWS.x > 0.0 ? 1 : 0) + (octNormalWS.y > 0.0 ? 2 : 0);
    // Store octNormalSign on two bits with perceptualRoughness
    outGBuffer1 = float4(abs(octNormalWS), PackFloatInt10bit(PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness), octNormalSign, 4.0), PackMaterialId(surfaceData.materialId));

    // RT2 - 8:8:8:8
    if (surfaceData.materialId == MATERIALID_LIT_STANDARD)
    {
        // Encode specular on two bit for the enum
        // Note: we encode two parametrization at the same time, specularColor and metal/specular
        if (surfaceData.specular == SPECULARVALUE_SPECULAR_COLOR)
        {
            outGBuffer2 = float4(surfaceData.specularColor, PackFloatInt8bit(0.0, surfaceData.specular, 4.0)); // As all is static, Pack function should produce the result compile time
        }
        else
        {
            // Note: it is important to setup anisotropy field to 0 else materialId will be anisotropic
            outGBuffer2 = float4(float3(0.0, 0.0, 0.0), PackFloatInt8bit(surfaceData.metallic, surfaceData.specular, 4.0));
        }
    }
    else if (surfaceData.materialId == MATERIALID_LIT_ANISO)
    {
        outGBuffer1.a = PackMaterialId(MATERIALID_LIT_STANDARD); // We save 1bit in gbuffer1 and use aniso value instead to detect we are aniso
        // Encode tangent on 16bit with oct compression
        float2 octTangentWS = PackNormalOctEncode(surfaceData.tangentWS);
        outGBuffer2 = float4(octTangentWS * 0.5 + 0.5, surfaceData.anisotropy, PackFloatInt8bit(surfaceData.metallic, surfaceData.specular, 4.0));
    }
    else if (surfaceData.materialId == MATERIALID_LIT_SSS)
    {
        outGBuffer2 = float4(surfaceData.subsurfaceRadius, surfaceData.thickness, 0, PackByte(surfaceData.subsurfaceProfile));
    }

    // Lighting
    outGBuffer3 = float4(bakeDiffuseLighting, 0.0);

    #if SHADEROPTIONS_PACK_GBUFFER_IN_U16
    // Now pack all buffer into 2 uint buffer

    // We don't have hardware sRGB to store base color in case we pack int u16, so rather than perform full sRGB encoding just use cheap gamma20
    // TODO: test alternative like FastLinearToSRGB to better match unpacked gbuffer
    outGBuffer0.xyz = LinearToGamma20(outGBuffer0.xyz);

    uint packedGBuffer1 = PackR10G10B10A2(outGBuffer1);

    outGBufferU0 = uint4(   PackFloatToUInt(outGBuffer0.x, 8, 0)  | PackFloatToUInt(outGBuffer0.y, 8, 8),
                            PackFloatToUInt(outGBuffer0.z, 8, 0)  | PackFloatToUInt(outGBuffer0.w, 8, 8),
                            (packedGBuffer1 & 0x0000FFFF),
                            (packedGBuffer1 & 0xFFFF0000) >> 16);

    uint packedGBuffer3 = PackR11G11B10f(outGBuffer3.xyz);

    outGBufferU1 = uint4(   PackFloatToUInt(outGBuffer2.x, 8, 0)  | PackFloatToUInt(outGBuffer2.y, 8, 8),
                            PackFloatToUInt(outGBuffer2.z, 8, 0)  | PackFloatToUInt(outGBuffer2.w, 8, 8),
                            (packedGBuffer3 & 0x0000FFFF),
                            (packedGBuffer3 & 0xFFFF0000) >> 16);
    #endif
}

float4 DecodeGBuffer0(GBufferType0 encodedGBuffer0)
{
    float4 decodedGBuffer0;
#if SHADEROPTIONS_PACK_GBUFFER_IN_U16
    decodedGBuffer0.x = UnpackUIntToFloat(encodedGBuffer0.x, 8, 0);
    decodedGBuffer0.y = UnpackUIntToFloat(encodedGBuffer0.x, 8, 8);
    decodedGBuffer0.z = UnpackUIntToFloat(encodedGBuffer0.y, 8, 0);
    decodedGBuffer0.w = UnpackUIntToFloat(encodedGBuffer0.y, 8, 8);

    decodedGBuffer0.xyz = Gamma20ToLinear(encodedGBuffer0.xyz);
#else
    decodedGBuffer0 = encodedGBuffer0;
#endif
    return decodedGBuffer0;
}

void DecodeFromGBuffer(
#if SHADEROPTIONS_PACK_GBUFFER_IN_U16
    GBufferType0 inGBufferU0,
    GBufferType1 inGBufferU1,
#else
    GBufferType0 inGBuffer0,
    GBufferType1 inGBuffer1,
    GBufferType2 inGBuffer2,
    GBufferType3 inGBuffer3,
#endif
    uint featureFlags,
    out BSDFData bsdfData,
    out float3 bakeDiffuseLighting)
{
    ZERO_INITIALIZE(BSDFData, bsdfData);

#if SHADEROPTIONS_PACK_GBUFFER_IN_U16
    float4 inGBuffer0, inGBuffer1, inGBuffer2, inGBuffer3;

    inGBuffer0 = DecodeGBuffer0(inGBufferU0);

    uint packedGBuffer1 = inGBufferU0.z | inGBufferU0.w << 16;
    inGBuffer1 = UnpackR10G10B10A2(packedGBuffer1);

    inGBuffer2.x = UnpackUIntToFloat(inGBufferU1.x, 8, 0);
    inGBuffer2.y = UnpackUIntToFloat(inGBufferU1.x, 8, 8);
    inGBuffer2.z = UnpackUIntToFloat(inGBufferU1.y, 8, 0);
    inGBuffer2.w = UnpackUIntToFloat(inGBufferU1.y, 8, 8);

    uint packedGBuffer3 = inGBufferU1.z | inGBufferU1.w << 16;
    inGBuffer3.xyz = UnpackR11G11B10f(packedGBuffer1);
    inGBuffer3.w = 0.0;
#endif

    float3 baseColor = inGBuffer0.rgb;
    bsdfData.specularOcclusion = inGBuffer0.a;

    int octNormalSign;
    UnpackFloatInt10bit(inGBuffer1.b, 4.0, bsdfData.perceptualRoughness, octNormalSign);
    inGBuffer1.r *= (octNormalSign & 1) ? 1.0 : -1.0;
    inGBuffer1.g *= (octNormalSign & 2) ? 1.0 : -1.0;
    bsdfData.normalWS = UnpackNormalOctEncode(float2(inGBuffer1.r, inGBuffer1.g));

    bsdfData.roughness = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);

    // The material features system for material classification must allow compile time optimization (i.e everything should be static)
    // Note that as we store materialId for Aniso based on content of RT2 we need to add few extra condition.
    // The code is also call from MaterialFeatureFlagsFromGBuffer, so must work fully dynamic if featureFlags is 0xFFFFFFFF
    int supportsStandard = (featureFlags & (MATERIALFEATUREFLAGS_LIT_STANDARD | MATERIALFEATUREFLAGS_LIT_ANISO)) != 0;
    int supportsSSS = (featureFlags & (MATERIALFEATUREFLAGS_LIT_SSS)) != 0;

    if (supportsStandard + supportsSSS > 1)
    {
        // only fetch materialid if it is not statically known from feature flags
        bsdfData.materialId = UnpackMaterialId(inGBuffer1.a);
    }
    else
    {
        // materialid is statically known. this allows the compiler to eliminate a lot of code.
        if (supportsStandard)
            bsdfData.materialId = MATERIALID_LIT_STANDARD;
        else // if (supportsSSS)
            bsdfData.materialId = MATERIALID_LIT_SSS;
    }

    if (bsdfData.materialId == MATERIALID_LIT_STANDARD)
    {
        float metallic;
        int specular;
        UnpackFloatInt8bit(inGBuffer2.a, 4.0, metallic, specular);
        float anisotropy = inGBuffer2.b;

        if (featureFlags & (MATERIAL_FEATURE_MASK_FLAGS) == MATERIALFEATUREFLAGS_LIT_STANDARD)
        {
            if (specular == SPECULARVALUE_SPECULAR_COLOR)
            {
                bsdfData.diffuseColor = baseColor;
                bsdfData.fresnel0 = inGBuffer2.rgb;
            }
            else
            {
                FillMaterialIdStandardData(baseColor, specular, metallic, bsdfData);
            }
        }
        else if (featureFlags & (MATERIAL_FEATURE_MASK_FLAGS) == MATERIALFEATUREFLAGS_LIT_ANISO)
        {
            bsdfData.materialId = MATERIALID_LIT_ANISO;
            FillMaterialIdStandardData(baseColor, specular, metallic, bsdfData);
            float3 tangentWS = UnpackNormalOctEncode(float2(inGBuffer2.rg * 2.0 - 1.0));
            FillMaterialIdAnisoData(bsdfData.roughness, bsdfData.normalWS, tangentWS, anisotropy, bsdfData);
        }
        else // either MATERIAL_FEATURE_MASK_FLAGS or MATERIALFEATUREFLAGS_LIT_STANDARD | MATERIALFEATUREFLAGS_LIT_ANISO
        {
            if (specular == SPECULARVALUE_SPECULAR_COLOR)
            {
                bsdfData.diffuseColor = baseColor;
                bsdfData.fresnel0 = inGBuffer2.rgb;
            }
            else if (anisotropy > 0)
            {
                bsdfData.materialId = MATERIALID_LIT_ANISO;
                FillMaterialIdStandardData(baseColor, specular, metallic, bsdfData);
                float3 tangentWS = UnpackNormalOctEncode(float2(inGBuffer2.rg * 2.0 - 1.0));
                FillMaterialIdAnisoData(bsdfData.roughness, bsdfData.normalWS, tangentWS, anisotropy, bsdfData);
            }
            else
            {
                FillMaterialIdStandardData(baseColor, specular, metallic, bsdfData);
            }
        }
    }
    else // bsdfData.materialId == MATERIALID_LIT_SSS
    {
        float subsurfaceRadius  = inGBuffer2.x;
        float thickness         = inGBuffer2.y;
        int   subsurfaceProfile = UnpackByte(inGBuffer2.w);

        FillMaterialIdSSSData(baseColor, subsurfaceProfile, subsurfaceRadius, thickness, bsdfData);
    }

    bakeDiffuseLighting = inGBuffer3.rgb;

    ApplyDebugToBSDFData(bsdfData);
}

uint MaterialFeatureFlagsFromGBuffer(
#if SHADEROPTIONS_PACK_GBUFFER_IN_U16
    GBufferType0 inGBufferU0,
    GBufferType1 inGBufferU1
#else
    GBufferType0 inGBuffer0,
    GBufferType1 inGBuffer1,
    GBufferType2 inGBuffer2,
    GBufferType3 inGBuffer3
#endif
)
{
    BSDFData bsdfData;
    float3 unused;

    DecodeFromGBuffer(
#if SHADEROPTIONS_PACK_GBUFFER_IN_U16
        inGBufferU0, inGBufferU1,
#else
        inGBuffer0, inGBuffer1, inGBuffer2, inGBuffer3,
#endif
        0xFFFFFFFF,
        bsdfData,
        unused
    );

    return (1 << bsdfData.materialId); // This match all the MATERIALFEATUREFLAGS_LIT_XXX flag
}


//-----------------------------------------------------------------------------
// Debug method (use to display values)
//-----------------------------------------------------------------------------

void GetSurfaceDataDebug(uint paramId, SurfaceData surfaceData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedSurfaceDataDebug(paramId, surfaceData, result, needLinearToSRGB);

    switch (paramId)
    {
        // TODO: Remap here!
        case DEBUGVIEW_LIT_SURFACEDATA_SPECULAR:
            result = surfaceData.specular.xxx;
            break;
    }
}

void GetBSDFDataDebug(uint paramId, BSDFData bsdfData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedBSDFDataDebug(paramId, bsdfData, result, needLinearToSRGB);
}

//-----------------------------------------------------------------------------
// PreLightData
//-----------------------------------------------------------------------------

// Precomputed lighting data to send to the various lighting functions
struct PreLightData
{
    // General
    float NdotV;                         // Geometric version (not clamped)

    // GGX iso
    float ggxLambdaV;

    // GGX Aniso
    float TdotV;
    float BdotV;
    float anisoGGXLambdaV;

    // IBL
    float3 iblDirWS;                     // Dominant specular direction, used for IBL in EvaluateBSDF_Env()
    float  iblMipLevel;

    float3 specularFGD;                  // Store preconvoled BRDF for both specular and diffuse
    float diffuseFGD;

    // area light
    float3x3 ltcXformGGX;                // TODO: make sure the compiler not wasting VGPRs on constants
    float3x3 ltcXformDisneyDiffuse;      // TODO: make sure the compiler not wasting VGPRs on constants
    float    ltcGGXFresnelMagnitudeDiff; // The difference of magnitudes of GGX and Fresnel
    float    ltcGGXFresnelMagnitude;
    float    ltcDisneyDiffuseMagnitude;
};

PreLightData GetPreLightData(float3 V, PositionInputs posInput, BSDFData bsdfData)
{
    PreLightData preLightData;

    float NdotV = dot(bsdfData.normalWS, V);

    float3 iblNormalWS = GetViewShiftedNormal(bsdfData.normalWS, V, NdotV, MIN_N_DOT_V);

    preLightData.NdotV = NdotV;      // Store the unaltered (geometric) version
    NdotV = max(NdotV, MIN_N_DOT_V); // Use the modified (clamped) version

    float3 iblR = reflect(-V, iblNormalWS);

    // GGX iso
    preLightData.ggxLambdaV = GetSmithJointGGXLambdaV(NdotV, bsdfData.roughness);

    // GGX aniso
    preLightData.TdotV = 0;
    preLightData.BdotV = 0;
    if (bsdfData.materialId == MATERIALID_LIT_ANISO)
    {
        preLightData.TdotV = dot(bsdfData.tangentWS, V);
        preLightData.BdotV = dot(bsdfData.bitangentWS, V);
        preLightData.anisoGGXLambdaV = GetSmithJointGGXAnisoLambdaV(preLightData.TdotV, preLightData.BdotV, NdotV, bsdfData.roughnessT, bsdfData.roughnessB);
        // Tangent = highlight stretch (anisotropy) direction. Bitangent = grain (brush) direction.
        float3 anisoIblNormalWS = GetAnisotropicModifiedNormal(bsdfData.bitangentWS, iblNormalWS, V, bsdfData.anisotropy);

        // NOTE: If we follow the theory we should use the modified normal for the different calculation implying a normal (like NdotV) and use iblNormalWS
        // into function like GetSpecularDominantDir(). However modified normal is just a hack. The goal is just to stretch a cubemap, no accuracy here.
        // With this in mind and for performance reasons we chose to only use modified normal to calculate R.
        iblR = reflect(-V, anisoIblNormalWS);
    }

    // IBL
    GetPreIntegratedFGD(NdotV, bsdfData.perceptualRoughness, bsdfData.fresnel0, preLightData.specularFGD, preLightData.diffuseFGD);

    preLightData.iblDirWS = GetSpecularDominantDir(iblNormalWS, iblR, bsdfData.roughness, NdotV);
    preLightData.iblMipLevel = PerceptualRoughnessToMipmapLevel(bsdfData.perceptualRoughness);

    // Area light
    // UVs for sampling the LUTs
    float theta = FastACos(NdotV);
    float2 uv = LTC_LUT_OFFSET + LTC_LUT_SCALE * float2(bsdfData.perceptualRoughness, theta * INV_HALF_PI);

    // Get the inverse LTC matrix for GGX
    // Note we load the matrix transpose (avoid to have to transpose it in shader)
    preLightData.ltcXformGGX      = 0.0;
    preLightData.ltcXformGGX._m22 = 1.0;
    preLightData.ltcXformGGX._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, ltc_linear_clamp_sampler, uv, LTC_GGX_MATRIX_INDEX, 0);

    // Get the inverse LTC matrix for Disney Diffuse
    // Note we load the matrix transpose (avoid to have to transpose it in shader)
    preLightData.ltcXformDisneyDiffuse      = 0.0;
    preLightData.ltcXformDisneyDiffuse._m22 = 1.0;
    preLightData.ltcXformDisneyDiffuse._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, ltc_linear_clamp_sampler, uv, LTC_DISNEY_DIFFUSE_MATRIX_INDEX, 0);

    float3 ltcMagnitude = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, ltc_linear_clamp_sampler, uv, LTC_MULTI_GGX_FRESNEL_DISNEY_DIFFUSE_INDEX, 0).rgb;
    preLightData.ltcGGXFresnelMagnitudeDiff = ltcMagnitude.r;
    preLightData.ltcGGXFresnelMagnitude     = ltcMagnitude.g;
    preLightData.ltcDisneyDiffuseMagnitude  = ltcMagnitude.b;

    return preLightData;
}

//-----------------------------------------------------------------------------
// bake lighting function
//-----------------------------------------------------------------------------

// GetBakedDiffuseLigthing function compute the bake lighting + emissive color to be store in emissive buffer (Deferred case)
// In forward it must be add to the final contribution.
// This function require the 3 structure surfaceData, builtinData, bsdfData because it may require both the engine side data, and data that will not be store inside the gbuffer.
float3 GetBakedDiffuseLigthing(SurfaceData surfaceData, BuiltinData builtinData, BSDFData bsdfData, PreLightData preLightData)
{
    // Premultiply bake diffuse lighting information with DisneyDiffuse pre-integration
    return builtinData.bakeDiffuseLighting * preLightData.diffuseFGD * surfaceData.ambientOcclusion * bsdfData.diffuseColor + builtinData.emissiveColor;
}

//-----------------------------------------------------------------------------
// light transport functions
//-----------------------------------------------------------------------------

LightTransportData GetLightTransportData(SurfaceData surfaceData, BuiltinData builtinData, BSDFData bsdfData)
{
    LightTransportData lightTransportData;

    // diffuseColor for lightmapping should basically be diffuse color.
    // But rough metals (black diffuse) still scatter quite a lot of light around, so
    // we want to take some of that into account too.

    lightTransportData.diffuseColor = bsdfData.diffuseColor + bsdfData.fresnel0 * bsdfData.roughness * 0.5 * surfaceData.metallic;
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

void BSDF(  float3 V, float3 L, float3 positionWS, PreLightData preLightData, BSDFData bsdfData,
            out float3 diffuseLighting,
            out float3 specularLighting)
{
    // Optimized math. Ref: PBR Diffuse Lighting for GGX + Smith Microsurfaces (slide 114).
    float NdotL    = saturate(dot(bsdfData.normalWS, L)); // Must have the same value without the clamp
    float NdotV    = preLightData.NdotV;                  // Get the unaltered (geometric) version
    float LdotV    = dot(L, V);
    float invLenLV = rsqrt(abs(2 * LdotV + 2));           // invLenLV = rcp(length(L + V))
    float NdotH    = saturate((NdotL + NdotV) * invLenLV);
    float LdotH    = saturate(invLenLV * LdotV + invLenLV);

    NdotV          = max(NdotV, MIN_N_DOT_V);             // Use the modified (clamped) version

    float3 F = F_Schlick(bsdfData.fresnel0, LdotH);

    float Vis;
    float D;
    // TODO: this way of handling aniso may not be efficient, or maybe with material classification, need to check perf here
    // Maybe always using aniso maybe a win ?
    if (bsdfData.materialId == MATERIALID_LIT_ANISO)
    {
        float3 H = (L + V) * invLenLV;
        // For anisotropy we must not saturate these values
        float TdotH = dot(bsdfData.tangentWS, H);
        float TdotL = dot(bsdfData.tangentWS, L);
        float BdotH = dot(bsdfData.bitangentWS, H);
        float BdotL = dot(bsdfData.bitangentWS, L);

        bsdfData.roughnessT = ClampRoughnessForAnalyticalLights(bsdfData.roughnessT);
        bsdfData.roughnessB = ClampRoughnessForAnalyticalLights(bsdfData.roughnessB);

        #ifdef LIT_USE_BSDF_PRE_LAMBDAV
        Vis = V_SmithJointGGXAnisoLambdaV(preLightData.TdotV, preLightData.BdotV, NdotV, TdotL, BdotL, NdotL,
                                          bsdfData.roughnessT, bsdfData.roughnessB, preLightData.anisoGGXLambdaV);
        #else
        // TODO: Do comparison between this correct version and the one from isotropic and see if there is any visual difference
        Vis = V_SmithJointGGXAniso(preLightData.TdotV, preLightData.BdotV, NdotV, TdotL, BdotL, NdotL,
                                   bsdfData.roughnessT, bsdfData.roughnessB);
        #endif

        D = D_GGXAniso(TdotH, BdotH, NdotH, bsdfData.roughnessT, bsdfData.roughnessB);
    }
    else
    {
        bsdfData.roughness = ClampRoughnessForAnalyticalLights(bsdfData.roughness);

        #ifdef LIT_USE_BSDF_PRE_LAMBDAV
        Vis = V_SmithJointGGX(NdotL, NdotV, bsdfData.roughness, preLightData.ggxLambdaV);
        #else
        Vis = V_SmithJointGGX(NdotL, NdotV, bsdfData.roughness);
        #endif
        D = D_GGX(NdotH, bsdfData.roughness);
    }
    specularLighting = F * (Vis * D);

#ifdef LIT_DIFFUSE_LAMBERT_BRDF
    float  diffuseTerm = Lambert();
#elif LIT_DIFFUSE_GGX_BRDF
    float3 diffuseTerm = DiffuseGGX(bsdfData.diffuseColor, NdotV, NdotL, NdotH, LdotV, bsdfData.perceptualRoughness);
#else
    float  diffuseTerm = DisneyDiffuse(NdotV, NdotL, LdotH, bsdfData.perceptualRoughness);
#endif

    diffuseLighting = bsdfData.diffuseColor * diffuseTerm;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Directional (supports directional and box projector lights)
//-----------------------------------------------------------------------------

void EvaluateBSDF_Directional(LightLoopContext lightLoopContext,
                              float3 V, PositionInputs posInput, PreLightData preLightData,
                              DirectionalLightData lightData, BSDFData bsdfData,
                              out float3 diffuseLighting,
                              out float3 specularLighting)
{
    float3 positionWS = posInput.positionWS;

    float3 L = -lightData.forward; // Lights are pointing backward in Unity
    float NdotL = dot(bsdfData.normalWS, L);
    float illuminance = saturate(NdotL);

    diffuseLighting  = float3(0, 0, 0); // TODO: check whether using 'out' instead of 'inout' increases the VGPR pressure
    specularLighting = float3(0, 0, 0); // TODO: check whether using 'out' instead of 'inout' increases the VGPR pressure
    float3 cookie    = float3(1, 1, 1);
    float  shadow    = 1;

    [branch] if (lightData.shadowIndex >= 0)
    {
        shadow = GetDirectionalShadowAttenuation(lightLoopContext.shadowContext, positionWS, bsdfData.normalWS, lightData.shadowIndex, L, posInput.unPositionSS);
        illuminance *= shadow;
    }

    [branch] if (lightData.cookieIndex >= 0)
    {
    	// Compute the NDC position (in [-1, 1]^2) by projecting 'positionWS' onto the near plane.
    	// 'lightData.right' and 'lightData.up' are pre-scaled on CPU.
    	float3   lightToSurface = positionWS - lightData.positionWS;
    	float3x3 lightToWorld   = float3x3(lightData.right, lightData.up, lightData.forward);
    	float3   positionLS     = mul(lightToSurface, transpose(lightToWorld));
    	float2   positionNDC    = positionLS.xy;

        float clipFactor = 1.0f;

        // Remap the texture coordinates from [-1, 1]^2 to [0, 1]^2.
        float2 coord = positionNDC * 0.5 + 0.5;

        if (lightData.tileCookie)
        {
            // Tile the texture if the 'repeat' wrap mode is enabled.
            coord = frac(coord);
        }
        else
        {
			bool isInBounds = Max3(abs(positionNDC.x), abs(positionNDC.y), 1 - positionLS.z) <= 1;
        	clipFactor = isInBounds ? 1 : 0;
        }

        // We let the sampler handle tiling or clamping to border.
        // Note: tiling (the repeat mode) is not currently supported.
        float4 c = SampleCookie2D(lightLoopContext, coord, lightData.cookieIndex);

        // Use premultiplied alpha to save 1x VGPR.
        cookie = c.rgb * c.a * clipFactor;
    }

    [branch] if (illuminance > 0.0)
    {
        BSDF(V, L, positionWS, preLightData, bsdfData, diffuseLighting, specularLighting);

        diffuseLighting  *= (cookie * lightData.color) * (illuminance * lightData.diffuseScale);
        specularLighting *= (cookie * lightData.color) * (illuminance * lightData.specularScale);
    }

    [branch] if (bsdfData.enableTransmission)
    {
        // Currently, we only model diffuse transmission. Specular transmission is not yet supported.
        // We assume that the back side of the object is a uniformly illuminated infinite plane
        // (we reuse the illumination) with the reversed normal of the current sample.
        // We apply wrapped lighting instead of the regular Lambertian diffuse
        // to compensate for these approximations.
        illuminance = ComputeWrappedDiffuseLighting(NdotL, SSS_WRAP_LIGHT);

        // For low thickness, we can reuse the shadowing status for the back of the object.
        shadow       = bsdfData.useThinObjectMode ? shadow : 1;
        illuminance *= shadow;

        float3 backLight = (cookie * lightData.color) * (Lambert() * illuminance * lightData.diffuseScale);
        // TODO: multiplication by 'diffuseColor' and 'transmittance' is the same for each light.
        float3 transmittedLight = backLight * (bsdfData.diffuseColor * bsdfData.transmittance);

        // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
        diffuseLighting += transmittedLight;
    }
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Punctual (supports spot, point and projector lights)
//-----------------------------------------------------------------------------

void EvaluateBSDF_Punctual( LightLoopContext lightLoopContext,
                            float3 V, PositionInputs posInput, PreLightData preLightData, LightData lightData, BSDFData bsdfData,
                            out float3 diffuseLighting,
                            out float3 specularLighting)
{
    float3 positionWS = posInput.positionWS;
    int    lightType  = lightData.lightType;

    // All punctual light type in the same formula, attenuation is neutral depends on light type.
    // light.positionWS is the normalize light direction in case of directional light and invSqrAttenuationRadius is 0
    // mean dot(unL, unL) = 1 and mean GetDistanceAttenuation() will return 1
    // For point light and directional GetAngleAttenuation() return 1

    float3 lightToSurface = positionWS - lightData.positionWS;
    float3 unL = -lightToSurface;
    float3 L   = (lightType != GPULIGHTTYPE_PROJECTOR_BOX) ? normalize(unL) : -lightData.forward;

    float attenuation = (lightType != GPULIGHTTYPE_PROJECTOR_BOX) ? GetDistanceAttenuation(unL, lightData.invSqrAttenuationRadius) : 1;
    // Reminder: lights are oriented backward (-Z)
    attenuation *= GetAngleAttenuation(L, -lightData.forward, lightData.angleScale, lightData.angleOffset);
    float NdotL = dot(bsdfData.normalWS, L);
    float illuminance = saturate(NdotL * attenuation);

    diffuseLighting  = float3(0, 0, 0); // TODO: check whether using 'out' instead of 'inout' increases the VGPR pressure
    specularLighting = float3(0, 0, 0); // TODO: check whether using 'out' instead of 'inout' increases the VGPR pressure
    float3 cookie    = float3(1, 1, 1);
    float  shadow    = 1;

    [branch] if (lightData.shadowIndex >= 0)
    {
        // TODO: make projector lights cast shadows.
        float3 offset = float3(0.0, 0.0, 0.0); // GetShadowPosOffset(nDotL, normal);
        float4 L_dist = { normalize( L.xyz ), length( unL ) };
        shadow = GetPunctualShadowAttenuation(lightLoopContext.shadowContext, positionWS + offset, bsdfData.normalWS, lightData.shadowIndex, L_dist, posInput.unPositionSS);
        shadow = lerp(1.0, shadow, lightData.shadowDimmer);

        illuminance *= shadow;
    }

    // Projector lights always have a cookie.
    [branch] if (lightData.cookieIndex >= 0)
    {
        // Translate and rotate 'positionWS' into the light space.
        // 'lightData.right' and 'lightData.up' are pre-scaled on CPU.
        float3x3 lightToWorld = float3x3(lightData.right, lightData.up, lightData.forward);
        float3   positionLS   = mul(lightToSurface, transpose(lightToWorld));

        [branch] if (lightType == GPULIGHTTYPE_POINT)
        {
            float4 c = SampleCookieCube(lightLoopContext, positionLS, lightData.cookieIndex);

            // Use premultiplied alpha to save 1x VGPR.
            cookie = c.rgb * c.a;
        }
        else
        {
            // Compute the NDC position (in [-1, 1]^2) by projecting 'positionWS' onto the plane at 1m distance.
            // Box projector lights require no perspective division.
            float  perspectiveZ = (lightType != GPULIGHTTYPE_PROJECTOR_BOX) ? positionLS.z : 1;
            float2 positionNDC  = positionLS.xy / perspectiveZ;
            bool   isInBounds   = Max3(abs(positionNDC.x), abs(positionNDC.y), 1 - positionLS.z) <= 1;
            float  clipFactor   = isInBounds ? 1 : 0;

            // Remap the texture coordinates from [-1, 1]^2 to [0, 1]^2.
            float2 coord = positionNDC * 0.5 + 0.5;

            // We let the sampler handle clamping to border.
            float4 c = SampleCookie2D(lightLoopContext, coord, lightData.cookieIndex);

            // Use premultiplied alpha to save 1x VGPR.
            cookie = c.rgb * (c.a * clipFactor);
        }
    }

    [branch] if (illuminance > 0.0)
    {
        BSDF(V, L, positionWS, preLightData, bsdfData, diffuseLighting, specularLighting);

        diffuseLighting  *= (cookie.rgb * lightData.color) * (illuminance * lightData.diffuseScale);
        specularLighting *= (cookie.rgb * lightData.color) * (illuminance * lightData.specularScale);
    }

    [branch] if (bsdfData.enableTransmission)
    {
        // Currently, we only model diffuse transmission. Specular transmission is not yet supported.
        // We assume that the back side of the object is a uniformly illuminated infinite plane
        // (we reuse the illumination) with the reversed normal of the current sample.
        // We apply wrapped lighting instead of the regular Lambertian diffuse
        // to compensate for these approximations.
        illuminance = ComputeWrappedDiffuseLighting(NdotL, SSS_WRAP_LIGHT) * attenuation;

        // For low thickness, we can reuse the shadowing status for the back of the object.
        shadow       = bsdfData.useThinObjectMode ? shadow : 1;
        illuminance *= shadow;

        float3 backLight = (cookie.rgb * lightData.color) * (Lambert() * illuminance * lightData.diffuseScale);
        // TODO: multiplication by 'diffuseColor' and 'transmittance' is the same for each light.
        float3 transmittedLight = backLight * (bsdfData.diffuseColor * bsdfData.transmittance);

        // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
        diffuseLighting += transmittedLight;
    }
}

#include "LitReference.hlsl"

//-----------------------------------------------------------------------------
// EvaluateBSDF_Line - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

void EvaluateBSDF_Line(LightLoopContext lightLoopContext,
                       float3 V, PositionInputs posInput,
                       PreLightData preLightData, LightData lightData, BSDFData bsdfData,
                       out float3 diffuseLighting, out float3 specularLighting)
{
    float3 positionWS = posInput.positionWS;

#ifdef LIT_DISPLAY_REFERENCE_AREA
    IntegrateBSDF_LineRef(V, positionWS, preLightData, lightData, bsdfData,
                          diffuseLighting, specularLighting);
#else
    diffuseLighting  = float3(0.0, 0.0, 0.0);
    specularLighting = float3(0.0, 0.0, 0.0);

    float  len = lightData.size.x;
    float3 T   = lightData.right;

    float3 unL = lightData.positionWS - positionWS;

    // Pick the major axis of the ellipsoid.
    float3 axis = lightData.right;

    // We define the ellipsoid s.t. r1 = (r + len / 2), r2 = r3 = r.
    // TODO: This could be precomputed.
    float radius         = rsqrt(lightData.invSqrAttenuationRadius);
    float invAspectRatio = radius / (radius + (0.5 * len));

    // Compute the light attenuation.
    float intensity = GetEllipsoidalDistanceAttenuation(unL,  lightData.invSqrAttenuationRadius,
                                                        axis, invAspectRatio);

    // Terminate if the shaded point is too far away.
    if (intensity == 0.0) return;

    lightData.diffuseScale  *= intensity;
    lightData.specularScale *= intensity;

    // TODO: This could be precomputed.
    float3 P1 = lightData.positionWS - T * (0.5 * len);
    float3 P2 = lightData.positionWS + T * (0.5 * len);

    // Translate the endpoints s.t. the shaded point is at the origin of the coordinate system.
    P1 -= positionWS;
    P2 -= positionWS;

    // Construct a view-dependent orthonormal basis around N.
    // TODO: it could be stored in PreLightData, since all LTC lights compute it more than once.
    float3x3 basis;
    basis[0] = normalize(V - bsdfData.normalWS * preLightData.NdotV);
    basis[1] = normalize(cross(bsdfData.normalWS, basis[0]));
    basis[2] = bsdfData.normalWS;

    // Rotate the endpoints into the local coordinate system (left-handed).
    P1 = mul(P1, transpose(basis));
    P2 = mul(P2, transpose(basis));

    // Compute the binormal.
    float3 B = normalize(cross(P1, P2));

    float ltcValue;

    // Evaluate the diffuse part.
    {
    #ifdef LIT_DIFFUSE_LAMBERT_BRDF
        ltcValue = LTCEvaluate(P1, P2, B, k_identity3x3);
    #else
        ltcValue = LTCEvaluate(P1, P2, B, preLightData.ltcXformDisneyDiffuse);
    #endif

    #ifndef LIT_DIFFUSE_LAMBERT_BRDF
        ltcValue *= preLightData.ltcDisneyDiffuseMagnitude;
    #endif

        ltcValue *= lightData.diffuseScale;
        diffuseLighting = bsdfData.diffuseColor * lightData.color * ltcValue;
    }

    // Evaluate the specular part.
    {
        // TODO: the fit seems rather poor. The scaling factor of 0.5 allows us
        // to match the reference for rough metals, but further darkens dielectrics.
        float3 fresnelTerm = bsdfData.fresnel0 * preLightData.ltcGGXFresnelMagnitudeDiff
                           + (float3)preLightData.ltcGGXFresnelMagnitude;

        ltcValue  = LTCEvaluate(P1, P2, B, preLightData.ltcXformGGX);
        ltcValue *= lightData.specularScale;
        specularLighting = fresnelTerm * lightData.color * ltcValue;
    }
#endif // LIT_DISPLAY_REFERENCE_AREA
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Area - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

// #define ELLIPSOIDAL_ATTENUATION

void EvaluateBSDF_Rect( LightLoopContext lightLoopContext,
                        float3 V, PositionInputs posInput,
                        PreLightData preLightData, LightData lightData, BSDFData bsdfData,
                        out float3 diffuseLighting, out float3 specularLighting)
{
    float3 positionWS = posInput.positionWS;

#ifdef LIT_DISPLAY_REFERENCE_AREA
    IntegrateBSDF_AreaRef(V, positionWS, preLightData, lightData, bsdfData,
                          diffuseLighting, specularLighting);
#else
    diffuseLighting  = float3(0.0, 0.0, 0.0);
    specularLighting = float3(0.0, 0.0, 0.0);

    float3 unL = lightData.positionWS - positionWS;

    [branch]
    if (dot(lightData.forward, unL) >= 0.0001)
    {
        // The light is back-facing.
        return;
    }

    // Rotate the light direction into the light space.
    float3x3 lightToWorld = float3x3(lightData.right, lightData.up, -lightData.forward);
    unL = mul(unL, transpose(lightToWorld));

    // TODO: This could be precomputed.
    float halfWidth  = lightData.size.x * 0.5;
    float halfHeight = lightData.size.y * 0.5;

    // Define the dimensions of the attenuation volume.
    // TODO: This could be precomputed.
    float  radius     = rsqrt(lightData.invSqrAttenuationRadius);
    float3 invHalfDim = rcp(float3(radius + halfWidth,
                                   radius + halfHeight,
                                   radius));

    // Compute the light attenuation.
#ifdef ELLIPSOIDAL_ATTENUATION
    // The attenuation volume is an axis-aligned ellipsoid s.t.
    // r1 = (r + w / 2), r2 = (r + h / 2), r3 = r.
    float intensity = GetEllipsoidalDistanceAttenuation(unL, invHalfDim);
#else
    // The attenuation volume is an axis-aligned box s.t.
    // hX = (r + w / 2), hY = (r + h / 2), hZ = r.
    float intensity = GetBoxDistanceAttenuation(unL, invHalfDim);
#endif

    // Terminate if the shaded point is too far away.
    if (intensity == 0.0) return;

    lightData.diffuseScale  *= intensity;
    lightData.specularScale *= intensity;

    // TODO: store 4 points and save 12 cycles (24x MADs - 12x MOVs).
    float3 p0 = lightData.positionWS + lightData.right *  halfWidth + lightData.up *  halfHeight;
    float3 p1 = lightData.positionWS + lightData.right *  halfWidth + lightData.up * -halfHeight;
    float3 p2 = lightData.positionWS + lightData.right * -halfWidth + lightData.up * -halfHeight;
    float3 p3 = lightData.positionWS + lightData.right * -halfWidth + lightData.up *  halfHeight;

    float4x3 matL = float4x3(p0, p1, p2, p3) - float4x3(positionWS, positionWS, positionWS, positionWS);

    float ltcValue;

    // Evaluate the diffuse part.
    {
    #ifdef LIT_DIFFUSE_LAMBERT_BRDF
        ltcValue = LTCEvaluate(matL, V, bsdfData.normalWS, preLightData.NdotV, k_identity3x3);
    #else
        ltcValue = LTCEvaluate(matL, V, bsdfData.normalWS, preLightData.NdotV, preLightData.ltcXformDisneyDiffuse);
    #endif

    #ifndef LIT_DIFFUSE_LAMBERT_BRDF
        ltcValue *= preLightData.ltcDisneyDiffuseMagnitude;
    #endif

        ltcValue *= lightData.diffuseScale;
        diffuseLighting = bsdfData.diffuseColor * lightData.color * ltcValue;
    }

    // Evaluate the specular part.
    {
        // TODO: the fit seems rather poor. The scaling factor of 0.5 allows us
        // to match the reference for rough metals, but further darkens dielectrics.
        float3 fresnelTerm = bsdfData.fresnel0 * preLightData.ltcGGXFresnelMagnitudeDiff
                           + (float3)preLightData.ltcGGXFresnelMagnitude;

        ltcValue  = LTCEvaluate(matL, V, bsdfData.normalWS, preLightData.NdotV, preLightData.ltcXformGGX);
        ltcValue *= lightData.specularScale;
        specularLighting = fresnelTerm * lightData.color * ltcValue;
    }
#endif // LIT_DISPLAY_REFERENCE_AREA
}

void EvaluateBSDF_Area(LightLoopContext lightLoopContext,
    float3 V, PositionInputs posInput,
    PreLightData preLightData, LightData lightData, BSDFData bsdfData, int GPULightType,
    out float3 diffuseLighting, out float3 specularLighting)
{
    if (GPULightType == GPULIGHTTYPE_LINE)
    {
        EvaluateBSDF_Line(lightLoopContext, V, posInput, preLightData, lightData, bsdfData, diffuseLighting, specularLighting);
    }
    else
    {
        EvaluateBSDF_Rect(lightLoopContext, V, posInput, preLightData, lightData, bsdfData, diffuseLighting, specularLighting);
    }
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Env
// ----------------------------------------------------------------------------

// _preIntegratedFGD and _CubemapLD are unique for each BRDF
void EvaluateBSDF_Env(  LightLoopContext lightLoopContext,
                        float3 V, PositionInputs posInput, PreLightData preLightData, EnvLightData lightData, BSDFData bsdfData,
                        out float3 diffuseLighting, out float3 specularLighting, out float2 weight)
{
    float3 positionWS = posInput.positionWS;

#ifdef LIT_DISPLAY_REFERENCE_IBL

    specularLighting = IntegrateSpecularGGXIBLRef(lightLoopContext, V, preLightData, lightData, bsdfData);

/*
    #ifdef LIT_DIFFUSE_LAMBERT_BRDF
    diffuseLighting = IntegrateLambertIBLRef(lightData, V, bsdfData);
    #else
    diffuseLighting = IntegrateDisneyDiffuseIBLRef(lightLoopContext, V, preLightData, lightData, bsdfData);
    #endif
*/
    diffuseLighting = float3(0.0, 0.0, 0.0);

    weight = float2(0.0, 1.0);

#else
    // TODO: factor this code in common, so other material authoring don't require to rewrite everything,
    // also think about how such a loop can handle 2 cubemap at the same time as old unity. Macro can allow to do that
    // but we need to have UNITY_SAMPLE_ENV_LOD replace by a true function instead that is define by the lighting arcitecture.
    // Also not sure how to deal with 2 intersection....
    // Box and sphere are related to light property (but we have also distance based roughness etc...)

    // TODO: test the strech from Tomasz
    // float shrinkedRoughness = AnisotropicStrechAtGrazingAngle(bsdfData.roughness, bsdfData.perceptualRoughness, NdotV);

    // In this code we redefine a bit the behavior of the reflcetion proble. We separate the projection volume (the proxy of the scene) form the influence volume (what pixel on the screen is affected)

    // 1. First determine the projection volume

    // In Unity the cubemaps are capture with the localToWorld transform of the component.
    // This mean that location and oritention matter. So after intersection of proxy volume we need to convert back to world.

    // CAUTION: localToWorld is the transform use to convert the cubemap capture point to world space (mean it include the offset)
    // the center of the bounding box is thus in locals space: positionLS - offsetLS
    // We use this formulation as it is the one of legacy unity that was using only AABB box.

    float3 R = preLightData.iblDirWS;
    float3x3 worldToLocal = transpose(float3x3(lightData.right, lightData.up, lightData.forward)); // worldToLocal assume no scaling
    float3 positionLS = positionWS - lightData.positionWS;
    positionLS = mul(positionLS, worldToLocal).xyz - lightData.offsetLS; // We want to calculate the intersection from the center of the bounding box.

    if (lightData.envShapeType == ENVSHAPETYPE_BOX)
    {
        float3 dirLS = mul(R, worldToLocal);
        float3 boxOuterDistance = lightData.innerDistance + float3(lightData.blendDistance, lightData.blendDistance, lightData.blendDistance);
        float dist = BoxRayIntersectSimple(positionLS, dirLS, -boxOuterDistance, boxOuterDistance);

        // No need to normalize for fetching cubemap
        // We can reuse dist calculate in LS directly in WS as there is no scaling. Also the offset is already include in lightData.positionWS
        R = (positionWS + dist * R) - lightData.positionWS;

        // TODO: add distance based roughness
    }
    else if (lightData.envShapeType == ENVSHAPETYPE_SPHERE)
    {
        float3 dirLS = mul(R, worldToLocal);
        float sphereOuterDistance = lightData.innerDistance.x + lightData.blendDistance;
        float dist = SphereRayIntersectSimple(positionLS, dirLS, sphereOuterDistance);

        R = (positionWS + dist * R) - lightData.positionWS;
    }

    // 2. Apply the influence volume (Box volume is used for culling whatever the influence shape)
    // TODO: In the future we could have an influence volume inside the projection volume (so with a different transform, in this case we will need another transform)
    weight.y = 1.0;

    if (lightData.envShapeType == ENVSHAPETYPE_SPHERE)
    {
        float distFade = max(length(positionLS) - lightData.innerDistance.x, 0.0);
        weight.y = saturate(1.0 - distFade / max(lightData.blendDistance, 0.0001)); // avoid divide by zero
    }
    else if (lightData.envShapeType == ENVSHAPETYPE_BOX ||
             lightData.envShapeType == ENVSHAPETYPE_NONE)
    {
        // Calculate falloff value, so reflections on the edges of the volume would gradually blend to previous reflection.
        float distFade = DistancePointBox(positionLS, -lightData.innerDistance, lightData.innerDistance);
        weight.y = saturate(1.0 - distFade / max(lightData.blendDistance, 0.0001)); // avoid divide by zero
    }

    // Smooth weighting
    weight.x = 0.0;
    weight.y = smoothstep01(weight.y);

    // TODO: we must always perform a weight calculation as due to tiled rendering we need to smooth out cubemap at boundaries.
    // So goal is to split into two category and have an option to say if we parallax correct or not.

    float4 preLD = SampleEnv(lightLoopContext, lightData.envIndex, R, preLightData.iblMipLevel);
    specularLighting = preLD.rgb * preLightData.specularFGD;

    // Apply specular occlusion on it
    specularLighting *= bsdfData.specularOcclusion * GetSpecularOcclusion(preLightData.NdotV, lightLoopContext.ambientOcclusion, bsdfData.roughness);
    diffuseLighting = float3(0.0, 0.0, 0.0);

#endif
}

#endif // #ifdef HAS_LIGHTLOOP
