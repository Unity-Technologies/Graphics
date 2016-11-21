//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------

// SurfaceData is define in Lit.cs which generate Lit.cs.hlsl
#include "Lit.cs.hlsl"

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
// #define LIT_DISPLAY_REFERENCE_AREA
// #define LIT_DISPLAY_REFERENCE_IBL
#endif
// Use Lambert diffuse instead of Disney diffuse
// #define LIT_DIFFUSE_LAMBERT_BRDF
// Use optimization of Precomputing LambdaV
// TODO: Test if this is a win
// #define LIT_USE_BSDF_PRE_LAMBDAV
// TODO: Check if anisotropy with a dynamic if on anisotropy > 0 is performant. Because it may mean we always calculate both isotropy and anisotropy case.
// Maybe we should always calculate anisotropy in case of standard ? Don't think the compile can optimize correctly.

// TODO: I haven't configure this sampler in the code, we should be able to do it (but Unity don't allow it for now...)
// By default Unity provide MIG_MAG_LINEAR_POINT sampler, so it fit with our need.
#ifdef LIT_DISPLAY_REFERENCE_IBL
// When reference mode is enabled, then we need to chose another sampler not related to cubemap code...
SAMPLER2D(sampler_LtcGGXMatrix);
#define SRL_BilinearSampler sampler_LtcGGXMatrix // Used for all textures
#else
SAMPLER2D(sampler_PreIntegratedFGD);
#define SRL_BilinearSampler sampler_PreIntegratedFGD // Used for all textures
#endif

// TODO: This one should be set into a constant Buffer at pass frequency (with _Screensize)
TEXTURE2D(_PreIntegratedFGD);
TEXTURE2D(_LtcGGXMatrix);                    // RGBA
TEXTURE2D(_LtcDisneyDiffuseMatrix);          // RGBA
TEXTURE2D(_LtcMultiGGXFresnelDisneyDiffuse); // RGB, A unused



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
    float3 preFGD = SAMPLE_TEXTURE2D_LOD(_PreIntegratedFGD, SRL_BilinearSampler, float2(NdotV, perceptualRoughness), 0).xyz;

    // f0 * Gv * (1 - Fc) + Gv * Fc
    specularFGD = fresnel0 * preFGD.x + preFGD.y;

#ifdef LIT_DIFFUSE_LAMBERT_BRDF
    diffuseFGD = 1.0;
#else
    diffuseFGD = preFGD.z;
#endif
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

    if (bsdfData.materialId == MATERIALID_LIT_STANDARD)
    {
        bsdfData.diffuseColor = surfaceData.baseColor * (1.0 - surfaceData.metallic);
        bsdfData.fresnel0 = lerp(float3(surfaceData.specular, surfaceData.specular, surfaceData.specular), surfaceData.baseColor, surfaceData.metallic);

        bsdfData.tangentWS = surfaceData.tangentWS;
        bsdfData.bitangentWS = cross(surfaceData.normalWS, surfaceData.tangentWS);        
        ConvertAnisotropyToRoughness(bsdfData.roughness, surfaceData.anisotropy, bsdfData.roughnessT, bsdfData.roughnessB);
        bsdfData.anisotropy = surfaceData.anisotropy;

        bsdfData.materialId = surfaceData.anisotropy > 0 ? MATERIALID_LIT_ANISO : bsdfData.materialId;
    }
    else if (bsdfData.materialId == MATERIALID_LIT_SSS)
    {
        bsdfData.diffuseColor = surfaceData.baseColor;
        bsdfData.fresnel0 = 0.028; // TODO take from subSurfaceProfile
        bsdfData.subSurfaceRadius = surfaceData.subSurfaceRadius;
        bsdfData.thickness = surfaceData.thickness;
        bsdfData.subSurfaceProfile = surfaceData.subSurfaceProfile;
    }
    else if (bsdfData.materialId == MATERIALID_LIT_CLEAR_COAT)
    {
        bsdfData.diffuseColor = surfaceData.baseColor * (1.0 - surfaceData.metallic);
        bsdfData.fresnel0 = lerp(float3(surfaceData.specular, surfaceData.specular, surfaceData.specular), surfaceData.baseColor, surfaceData.metallic);
        bsdfData.coatNormalWS = surfaceData.coatNormalWS;
        bsdfData.coatRoughness = PerceptualSmoothnessToRoughness(surfaceData.coatPerceptualSmoothness);
    }
    else if (bsdfData.materialId == MATERIALID_LIT_SPECULAR)
    {
        bsdfData.diffuseColor = surfaceData.baseColor;
        bsdfData.fresnel0 = surfaceData.specularColor;
    }

    return bsdfData;
}

//-----------------------------------------------------------------------------
// conversion function for deferred
//-----------------------------------------------------------------------------

// Encode SurfaceData (BSDF parameters) into GBuffer
// Must be in sync with RT declared in HDRenderLoop.cs ::Rebuild
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
    // Encode normal on 20bit with oct compression
    float2 octNormalWS = PackNormalOctEncode(surfaceData.normalWS);
    // We store perceptualRoughness instead of roughness because it save a sqrt ALU when decoding
    // (as we want both perceptualRoughness and roughness for the lighting due to Disney Diffuse model)
    // TODO: Store 2 bit of flag into perceptualSmoothness (one for SSR, other is free (deferred planar reflection ID ? / MatID extension ?)
    outGBuffer1 = float4(octNormalWS * 0.5 + 0.5, PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness), PackMaterialId(surfaceData.materialId));

    // RT2 - 8:8:8:8
    if (surfaceData.materialId == MATERIALID_LIT_STANDARD)
    {
        // Encode tangent on 16bit with oct compression
        float2 octTangentWS = PackNormalOctEncode(surfaceData.tangentWS);
        // TODO: store metal and specular together, specular should be an enum (fixed value)
        outGBuffer2 = float4(octTangentWS * 0.5 + 0.5, surfaceData.anisotropy, surfaceData.metallic);
    }
    else if (surfaceData.materialId == MATERIALID_LIT_SSS)
    {
        outGBuffer2 = float4(surfaceData.subSurfaceRadius, surfaceData.thickness, 0.0, surfaceData.subSurfaceProfile / 8.0f); // Number of profile not define yet
    }
    else if (surfaceData.materialId == MATERIALID_LIT_CLEAR_COAT)
    {
        // Encode coat normal on 16bit with oct compression
        float2 octCoatNormalWS = PackNormalOctEncode(surfaceData.coatNormalWS);
        // TODO: store metal and specular together, specular should be an enum (fixed value)
        outGBuffer2 = float4(octCoatNormalWS * 0.5 + 0.5, PerceptualSmoothnessToRoughness(surfaceData.coatPerceptualSmoothness), surfaceData.metallic);
    }
    else if (surfaceData.materialId == MATERIALID_LIT_SPECULAR)
    {
        outGBuffer2 = float4(surfaceData.specularColor, 0.0);
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
                        out BSDFData bsdfData,
                        out float3 bakeDiffuseLighting)
{
    ZERO_INITIALIZE(BSDFData, bsdfData);

    #if SHADEROPTIONS_PACK_GBUFFER_IN_U16
    float4 inGBuffer0, inGBuffer1, inGBuffer2, inGBuffer3;
    
    inGBuffer0.x = UnpackUIntToFloat(inGBufferU0.x, 8, 0);
    inGBuffer0.y = UnpackUIntToFloat(inGBufferU0.x, 8, 8);
    inGBuffer0.z = UnpackUIntToFloat(inGBufferU0.y, 8, 0);
    inGBuffer0.w = UnpackUIntToFloat(inGBufferU0.y, 8, 8);

    inGBuffer0.xyz = Gamma20ToLinear(inGBuffer0.xyz);

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

    bsdfData.normalWS = UnpackNormalOctEncode(float2(inGBuffer1.r * 2.0 - 1.0, inGBuffer1.g * 2.0 - 1.0));
    bsdfData.perceptualRoughness = inGBuffer1.b;
    bsdfData.roughness = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);
    bsdfData.materialId = UnpackMaterialId(inGBuffer1.a);

    if (bsdfData.materialId == MATERIALID_LIT_STANDARD)
    {
        float metallic = inGBuffer2.a;
        // TODO extract spec
        float specular = 0.04;
        float anisotropy = inGBuffer2.b;

        bsdfData.diffuseColor = baseColor * (1.0 - metallic);
        bsdfData.fresnel0 = lerp(float3(specular, specular, specular), baseColor, metallic);

        bsdfData.tangentWS = UnpackNormalOctEncode(float2(inGBuffer2.rg * 2.0 - 1.0));
        // TODO: Do we need to orthonormalize here, IIRC Eric say that we should
        bsdfData.bitangentWS = cross(bsdfData.normalWS, bsdfData.tangentWS);
        ConvertAnisotropyToRoughness(bsdfData.roughness, anisotropy, bsdfData.roughnessT, bsdfData.roughnessB);
        bsdfData.anisotropy = anisotropy;

        bsdfData.materialId = anisotropy > 0 ? MATERIALID_LIT_ANISO : bsdfData.materialId;
    }
    else if (bsdfData.materialId == MATERIALID_LIT_SSS)
    {
        bsdfData.diffuseColor = baseColor;
        bsdfData.fresnel0 = 0.028; // TODO take from subSurfaceProfile
        bsdfData.subSurfaceRadius = inGBuffer2.r;
        bsdfData.thickness = inGBuffer2.g;
        bsdfData.subSurfaceProfile = inGBuffer2.a * 8.0f;
    }
    else if (bsdfData.materialId == MATERIALID_LIT_CLEAR_COAT)
    {
        float metallic = inGBuffer2.a;
        // TODO extract spec
        float specular = 0.04;

        bsdfData.diffuseColor = baseColor * (1.0 - metallic);
        bsdfData.fresnel0 = lerp(float3(specular, specular, specular), baseColor, metallic);
        bsdfData.coatNormalWS = UnpackNormalOctEncode(float2(inGBuffer2.rg * 2.0 - 1.0));
        bsdfData.coatRoughness = inGBuffer2.b;
    }
    else if (bsdfData.materialId == MATERIALID_LIT_SPECULAR)
    {
        bsdfData.diffuseColor = baseColor;
        bsdfData.fresnel0 = inGBuffer2.rgb;
    }

    bakeDiffuseLighting = inGBuffer3.rgb;
}

//-----------------------------------------------------------------------------
// Debug method (use to display values)
//-----------------------------------------------------------------------------

void GetSurfaceDataDebug(uint paramId, SurfaceData surfaceData, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_LIT_SURFACEDATA_BASE_COLOR:
            result = surfaceData.baseColor; needLinearToSRGB = true;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_SPECULAR_OCCLUSION:
            result = surfaceData.specularOcclusion.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_NORMAL_WS:
            result = surfaceData.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_PERCEPTUAL_SMOOTHNESS:
            result = surfaceData.perceptualSmoothness.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_MATERIAL_ID:
            result = GetIndexColor(surfaceData.materialId);
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_AMBIENT_OCCLUSION:
            result = surfaceData.ambientOcclusion.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_TANGENT_WS:
            result = surfaceData.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_ANISOTROPY:
            result = surfaceData.anisotropy.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_METALLIC:
            result = surfaceData.metallic.xxx;
            break;
        // TODO: Remap here!
        case DEBUGVIEW_LIT_SURFACEDATA_SPECULAR:
            result = surfaceData.specular.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_SUB_SURFACE_RADIUS:
            result = surfaceData.subSurfaceRadius.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_THICKNESS:
            result = surfaceData.thickness.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_SUB_SURFACE_PROFILE:
            result = GetIndexColor(surfaceData.subSurfaceProfile);
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_COAT_NORMAL_WS:
            result = surfaceData.coatNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_COAT_PERCEPTUAL_SMOOTHNESS:
            result = surfaceData.coatPerceptualSmoothness.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_SPECULAR_COLOR:
            result = surfaceData.specularColor; needLinearToSRGB = true;
            break;
    }
}

void GetBSDFDataDebug(uint paramId, BSDFData bsdfData, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_LIT_BSDFDATA_DIFFUSE_COLOR:
            result = bsdfData.diffuseColor; needLinearToSRGB = true;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_FRESNEL0:
            result = bsdfData.fresnel0;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_SPECULAR_OCCLUSION:
            result = bsdfData.specularOcclusion.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_NORMAL_WS:
            result = bsdfData.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_PERCEPTUAL_ROUGHNESS:
            result = bsdfData.perceptualRoughness.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS:
            result = bsdfData.roughness.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_MATERIAL_ID:
            result = GetIndexColor(bsdfData.materialId);
            break;
        case DEBUGVIEW_LIT_BSDFDATA_TANGENT_WS:
            result = bsdfData.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_BITANGENT_WS:
            result = bsdfData.bitangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS_T:
            result = bsdfData.roughnessT.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS_B:
            result = bsdfData.roughnessB.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_ANISOTROPY:
            result = bsdfData.anisotropy.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_SUB_SURFACE_RADIUS:
            result = bsdfData.subSurfaceRadius.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_THICKNESS:
            result = bsdfData.thickness.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_SUB_SURFACE_PROFILE:
            result = GetIndexColor(bsdfData.subSurfaceProfile);
            break;
        case DEBUGVIEW_LIT_BSDFDATA_COAT_NORMAL_WS:
            result = bsdfData.coatNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_COAT_ROUGHNESS:
            result = bsdfData.coatRoughness.xxx;
            break;
    }
}

//-----------------------------------------------------------------------------
// PreLightData
//-----------------------------------------------------------------------------

// Precomputed lighting data to send to the various lighting functions
struct PreLightData
{
    float NdotV;
    float ggxLambdaV;

    // Aniso
    float TdotV;
    float BdotV;
    
    float anisoGGXLambdaV;

    // image based lighting
    // These variables aim to be use with EvaluateBSDF_Env 
    float3 iblNormalWS; // Normal to be use with image based lighting
    float3 iblR;        // Reflection vector, same as above.

    float3 specularFGD; // Store preconvole BRDF for both specular and diffuse
    float diffuseFGD;

    // TODO: if we want we can store ambient occlusion here from SSAO pass for example that can be use for IBL specular occlusion
    // float ambientOcclusion; // Feed from an ambient occlusion buffer

    // area light
    float3x3 ltcXformGGX;                // TODO: make sure the compiler not wasting VGPRs on constants
    float3x3 ltcXformDisneyDiffuse;      // TODO: make sure the compiler not wasting VGPRs on constants
    float    ltcGGXFresnelMagnitudeDiff; // The difference of magnitudes of GGX and Fresnel
    float    ltcGGXFresnelMagnitude;
    float    ltcDisneyDiffuseMagnitude;

    // Shadow (sampling rotation disc)
    float2 unPositionSS;
};

PreLightData GetPreLightData(float3 V, float3 positionWS, Coordinate coord, BSDFData bsdfData)
{
    PreLightData preLightData;

    // TODO: check Eric idea about doing that when writting into the GBuffer (with our forward decal)
#if 0
    preLightData.NdotV = GetShiftedNdotV(bsdfData.normalWS, V); // Note: May not work with speedtree...
#else
    preLightData.NdotV = GetNdotV(bsdfData.normalWS, V);
#endif

    preLightData.ggxLambdaV = GetSmithJointGGXLambdaV(preLightData.NdotV, bsdfData.roughness);

    float iblNdotV = preLightData.NdotV;
    float3 iblNormalWS = bsdfData.normalWS;

    // Check if we precompute anisotropy too
    if (bsdfData.materialId == MATERIALID_LIT_ANISO)
    {
        preLightData.TdotV = dot(bsdfData.tangentWS, V);
        preLightData.BdotV = dot(bsdfData.bitangentWS, V);
        preLightData.anisoGGXLambdaV = GetSmithJointGGXAnisoLambdaV(preLightData.TdotV, preLightData.BdotV, preLightData.NdotV, bsdfData.roughnessT, bsdfData.roughnessB);
        // Tangent = highlight stretch (anisotropy) direction. Bitangent = grain (brush) direction.
        iblNormalWS = GetAnisotropicModifiedNormal(bsdfData.bitangentWS, bsdfData.normalWS, V, bsdfData.anisotropy);
        
        // NOTE: If we follow the theory we should use the modified normal for the different calculation implying a normal (like NDotV) and use iblNormalWS
        // into function like GetSpecularDominantDir(). However modified normal is just a hack. The goal is just to stretch a cubemap, no accuracy here.
        // With this in mind and for performance reasons we chose to only use modified normal to calculate R.
        // iblNdotV = GetNdotV(iblNormalWS, V);
    }

    // We need to take into account the modified normal for faking anisotropic here.
    preLightData.iblR = reflect(-V, iblNormalWS);
    GetPreIntegratedFGD(iblNdotV, bsdfData.perceptualRoughness, bsdfData.fresnel0, preLightData.specularFGD, preLightData.diffuseFGD);

    // #if SHADERPASS == SHADERPASS_GBUFFER
    // preLightData.ambientOcclusion = _AmbientOcclusion.Load(uint3(coord.unPositionSS, 0)).x;
    // #endif

    // Area light specific
    // UVs for sampling the LUTs
    float theta = FastACos(dot(bsdfData.normalWS, V));
    // Scale and bias for the current precomputed table - the constant use here are the one that have been use when the table in LtcData.DisneyDiffuse.cs and LtcData.GGX.cs was use
    float2 uv = 0.0078125 + 0.984375 * float2(bsdfData.perceptualRoughness, theta * INV_HALF_PI);

    // Get the inverse LTC matrix for GGX
    // Note we load the matrix transpose (avoid to have to transpose it in shader)
    preLightData.ltcXformGGX      = 0.0;
    preLightData.ltcXformGGX._m22 = 1.0;
    preLightData.ltcXformGGX._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_LOD(_LtcGGXMatrix, SRL_BilinearSampler, uv, 0);

    // Get the inverse LTC matrix for Disney Diffuse
    // Note we load the matrix transpose (avoid to have to transpose it in shader)
    preLightData.ltcXformDisneyDiffuse      = 0.0;
    preLightData.ltcXformDisneyDiffuse._m22 = 1.0;
    preLightData.ltcXformDisneyDiffuse._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_LOD(_LtcDisneyDiffuseMatrix, SRL_BilinearSampler, uv, 0);

    float3 ltcMagnitude = SAMPLE_TEXTURE2D_LOD(_LtcMultiGGXFresnelDisneyDiffuse, SRL_BilinearSampler, uv, 0).r;
    preLightData.ltcGGXFresnelMagnitudeDiff = ltcMagnitude.r;
    preLightData.ltcGGXFresnelMagnitude     = ltcMagnitude.g;
    preLightData.ltcDisneyDiffuseMagnitude  = ltcMagnitude.b;

    // Shadow
    preLightData.unPositionSS = coord.unPositionSS;

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
    return builtinData.bakeDiffuseLighting * preLightData.diffuseFGD * surfaceData.ambientOcclusion * bsdfData.diffuseColor + builtinData.emissiveColor * builtinData.emissiveIntensity;
}

//-----------------------------------------------------------------------------
// light transport functions
//-----------------------------------------------------------------------------

LighTransportData GetLightTransportData(SurfaceData surfaceData, BuiltinData builtinData, BSDFData bsdfData)
{
    LighTransportData lightTransportData;

    // diffuseColor for lightmapping should basically be diffuse color.
    // But rough metals (black diffuse) still scatter quite a lot of light around, so
    // we want to take some of that into account too.

    lightTransportData.diffuseColor = bsdfData.diffuseColor + bsdfData.fresnel0 * bsdfData.roughness * 0.5 * surfaceData.metallic;
    lightTransportData.emissiveColor = builtinData.emissiveColor * builtinData.emissiveIntensity;

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
    float3 H = normalize(V + L);
    float LdotH = saturate(dot(L, H));
    float NdotH = saturate(dot(bsdfData.normalWS, H));
    float NdotL = saturate(dot(bsdfData.normalWS, L));
    float3 F = F_Schlick(bsdfData.fresnel0, LdotH);

    float Vis;
    float D;
    // TODO: this way of handling aniso may not be efficient, or maybe with material classification, need to check perf here
    // Maybe always using aniso maybe a win ?
    if (bsdfData.materialId == MATERIALID_LIT_ANISO)
    {
        float TdotL = saturate(dot(bsdfData.tangentWS, L));
        float BdotL = saturate(dot(bsdfData.bitangentWS, L));

        #ifdef LIT_USE_BSDF_PRE_LAMBDAV
        Vis = V_SmithJointGGXAnisoLambdaV(  preLightData.TdotV, preLightData.BdotV, preLightData.NdotV, TdotL, BdotL, NdotL,
                                            bsdfData.roughnessT, bsdfData.roughnessB, preLightData.anisoGGXLambdaV);
        #else
        // TODO: Do comparison between this correct version and the one from isotropic and see if there is any visual difference
        Vis = V_SmithJointGGXAniso( preLightData.TdotV, preLightData.BdotV, preLightData.NdotV, TdotL, BdotL, NdotL,
                                    bsdfData.roughnessT, bsdfData.roughnessB);
        #endif

        // For anisotropy we must not saturate these values
        float TdotH = dot(bsdfData.tangentWS, H);
        float BdotH = dot(bsdfData.bitangentWS, H);
        D = D_GGXAniso(TdotH, BdotH, NdotH, bsdfData.roughnessT, bsdfData.roughnessB);
    }
    else
    {
        #ifdef LIT_USE_BSDF_PRE_LAMBDAV
        Vis = V_SmithJointGGX(NdotL, preLightData.NdotV, bsdfData.roughness, preLightData.ggxLambdaV);
        #else
        Vis = V_SmithJointGGX(NdotL, preLightData.NdotV, bsdfData.roughness);
        #endif
        D = D_GGX(NdotH, bsdfData.roughness);
    }
    specularLighting = F * (Vis * D);
    #ifdef LIT_DIFFUSE_LAMBERT_BRDF
    float diffuseTerm = Lambert();
    #else
    float diffuseTerm = DisneyDiffuse(preLightData.NdotV, NdotL, LdotH, bsdfData.perceptualRoughness);
    #endif
    diffuseLighting = bsdfData.diffuseColor * diffuseTerm;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Directional
//-----------------------------------------------------------------------------

void EvaluateBSDF_Directional(  LightLoopContext lightLoopContext,
                                float3 V, float3 positionWS, PreLightData preLightData, DirectionalLightData lightData, BSDFData bsdfData,
                                out float3 diffuseLighting,
                                out float3 specularLighting)
{
    float3 L = lightData.direction;
    float illuminance = saturate(dot(bsdfData.normalWS, L));

    diffuseLighting = float3(0.0, 0.0, 0.0);
    specularLighting = float3(0.0, 0.0, 0.0);

    [branch] if (lightData.shadowIndex >= 0 && illuminance > 0.0f)
    {
        float shadowAttenuation = GetDirectionalShadowAttenuation(lightLoopContext, positionWS, lightData.shadowIndex, L, preLightData.unPositionSS);

        illuminance *= shadowAttenuation;
    }

    [branch] if (illuminance > 0.0f)
    {
        BSDF(V, L, positionWS, preLightData, bsdfData, diffuseLighting, specularLighting);
        diffuseLighting *= lightData.color * illuminance * lightData.diffuseScale;
        specularLighting *= lightData.color * illuminance * lightData.specularScale;
    }
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Punctual
//-----------------------------------------------------------------------------

void EvaluateBSDF_Punctual( LightLoopContext lightLoopContext,
                            float3 V, float3 positionWS, PreLightData preLightData, LightData lightData, BSDFData bsdfData,
                            out float3 diffuseLighting,
                            out float3 specularLighting)
{
    // All punctual light type in the same formula, attenuation is neutral depends on light type.
    // light.positionWS is the normalize light direction in case of directional light and invSqrAttenuationRadius is 0
    // mean dot(unL, unL) = 1 and mean GetDistanceAttenuation() will return 1
    // For point light and directional GetAngleAttenuation() return 1

    float3 unL = lightData.positionWS - positionWS;
    float3 L = normalize(unL);

    float attenuation = GetDistanceAttenuation(unL, lightData.invSqrAttenuationRadius);
    // Reminder: lights are ortiented backward (-Z)
    attenuation *= GetAngleAttenuation(L, -lightData.forward, lightData.angleScale, lightData.angleOffset);
    float illuminance = saturate(dot(bsdfData.normalWS, L)) * attenuation;

    diffuseLighting = float3(0.0, 0.0, 0.0);
    specularLighting = float3(0.0, 0.0, 0.0);

    // TODO: measure impact of having all these dynamic branch here and the gain (or not) of testing illuminace > 0

    /*
    [branch] if (lightData.cookieIndex && illuminance > 0.0f)
    {
        float3x3 lightToWorld = float3x3(lightData.right, lightData.up, lightData.forward);
        illuminance *= SampleCookie(lightData.cookieIndex, lightToWorld, L);
    }
    */

    [branch] if (lightData.IESIndex >= 0 && illuminance > 0.0f)
    {
        float3x3 lightToWorld = float3x3(lightData.right, lightData.up, lightData.forward);
        float2 sphericalCoord = GetIESTextureCoordinate(lightToWorld, L);
        illuminance *= SampleIES(lightLoopContext, lightData.IESIndex, sphericalCoord, 0).r;
    }

    [branch] if (lightData.shadowIndex >= 0 && illuminance > 0.0f)
    {
        float3 offset = float3(0.0, 0.0, 0.0); // GetShadowPosOffset(nDotL, normal);
        float shadowAttenuation = GetPunctualShadowAttenuation(lightLoopContext, positionWS + offset, lightData.shadowIndex, L, preLightData.unPositionSS);
        shadowAttenuation = lerp(1.0, shadowAttenuation, lightData.shadowDimmer);

        illuminance *= shadowAttenuation;
    }

    [branch] if (illuminance > 0.0f)
    {
        BSDF(V, L, positionWS, preLightData, bsdfData, diffuseLighting, specularLighting);
        diffuseLighting *= lightData.color * illuminance * lightData.diffuseScale;
        specularLighting *= lightData.color * illuminance * lightData.specularScale;
    }
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Area - Reference
//-----------------------------------------------------------------------------

void IntegrateGGXAreaRef(   float3 V, float3 positionWS, PreLightData preLightData, LightData lightData, BSDFData bsdfData,
                            out float3 diffuseLighting,
                            out float3 specularLighting,
                            uint sampleCount = 512)
{
    // Add some jittering on Hammersley2d
    float2 randNum = InitRandom(V.xy * 0.5 + 0.5);

    diffuseLighting = float3(0.0, 0.0, 0.0);
    specularLighting = float3(0.0, 0.0, 0.0);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float3 P = float3(0.0, 0.0, 0.0);	// Sample light point. Random point on the light shape in local space.
        float3 Ns = float3(0.0, 0.0, 0.0);	// Unit surface normal at P
        float lightPdf = 0.0;	            // Pdf of the light sample

        float2 u = Hammersley2d(i, sampleCount);
        u = frac(u + randNum + 0.5);

        float4x4 localToWorld = float4x4(float4(lightData.right, 0.0), float4(lightData.up, 0.0), float4(lightData.forward, 0.0), float4(lightData.positionWS, 1.0));

        switch (lightData.lightType)
        {
            case GPULIGHTTYPE_SPHERE:
                SampleSphere(u, localToWorld, lightData.size.x, lightPdf, P, Ns);
                break;
            case GPULIGHTTYPE_HEMISPHERE:
                SampleHemisphere(u, localToWorld, lightData.size.x, lightPdf, P, Ns);
                break;
            case GPULIGHTTYPE_CYLINDER:
                SampleCylinder(u, localToWorld, lightData.size.x, lightData.size.y, lightPdf, P, Ns);
                break;
            case GPULIGHTTYPE_RECTANGLE:
                SampleRectangle(u, localToWorld, lightData.size.x, lightData.size.y, lightPdf, P, Ns);
                break;
            case GPULIGHTTYPE_DISK:
                SampleDisk(u, localToWorld, lightData.size.x, lightPdf, P, Ns);
                break;
            // case GPULIGHTTYPE_LINE: handled by a separate function.
        }

        // Get distance
        float3 unL = P - positionWS;
        float sqrDist = dot(unL, unL);
        float3 L = normalize(unL);

        // Cosine of the angle between the light direction and the normal of the light's surface.
        float cosLNs = dot(-L, Ns);
        cosLNs = lightData.twoSided ? abs(cosLNs) : saturate(cosLNs);

        // We calculate area reference light with the area integral rather than the solid angle one.
        float illuminance = cosLNs * saturate(dot(bsdfData.normalWS, L)) / (sqrDist * lightPdf);

        float3 localDiffuseLighting = float3(0.0, 0.0, 0.0);
        float3 localSpecularLighting = float3(0.0, 0.0, 0.0);

        if (illuminance > 0.0)
        {
            BSDF(V, L, positionWS, preLightData, bsdfData, localDiffuseLighting, localSpecularLighting);
            localDiffuseLighting *= lightData.color * illuminance * lightData.diffuseScale;
            localSpecularLighting *= lightData.color * illuminance * lightData.specularScale;
        }

        diffuseLighting += localDiffuseLighting;
        specularLighting += localSpecularLighting;
    }

    diffuseLighting /= float(sampleCount);
    specularLighting /= float(sampleCount);
}

//-----------------------------------------------------------------------------
// EvaluateBSDFLine - Reference
//-----------------------------------------------------------------------------

void IntegrateBSDFLineRef(float3 V, float3 positionWS, PreLightData preLightData,
                          LightData lightData, BSDFData bsdfData,
                          out float3 diffuseLighting, out float3 specularLighting,
                          int sampleCount = 128)
{
    diffuseLighting  = float3(0.0, 0.0, 0.0);
    specularLighting = float3(0.0, 0.0, 0.0);

    const float  len = lightData.size.x;
    const float3 dir = lightData.right;
    const float3 p1  = lightData.positionWS - lightData.right * (0.5 * len);
    const float  dt  = len * rcp(sampleCount);
    const float  off = 0.5 * dt;

    // Uniformly sample the line segment with the Pdf = 1 / len.
    const float invPdf = len;

    for (int i = 0; i < sampleCount; ++i)
    {
        // Place the sample in the middle of the interval.
        float  t     = off + i * dt;
        float3 sPos  = p1 + t * dir;
        float3 unL   = sPos - positionWS;
        float  dist2 = dot(unL, unL);
        float3 L     = normalize(unL);
        float  sinLD = length(cross(L, dir));
        float  NdotL = saturate(dot(bsdfData.normalWS, L));

        float3 lightDiff, lightSpec;

        BSDF(V, L, positionWS, preLightData, bsdfData, lightDiff, lightSpec);

        diffuseLighting  += lightDiff * (sinLD / dist2 * NdotL);
        specularLighting += lightSpec * (sinLD / dist2 * NdotL);
    }

    // The factor of 2 is due to the fact: Integral{0, 2 PI}{max(0, cos(x))dx} = 2.
    float normFactor = 2.0 * invPdf * rcp(sampleCount);

    diffuseLighting  *= normFactor * lightData.diffuseScale  * lightData.color;
    specularLighting *= normFactor * lightData.specularScale * lightData.color;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Line | Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

void EvaluateBSDF_Line( LightLoopContext lightLoopContext,
                        float3 V, float3 positionWS, PreLightData preLightData,
                        LightData lightData, BSDFData bsdfData,
                        out float3 diffuseLighting, out float3 specularLighting)
{
    diffuseLighting  = float3(0.0, 0.0, 0.0);
    specularLighting = float3(0.0, 0.0, 0.0);

    float  len = lightData.size.x;
    float3 dir = lightData.right;

    // TODO: precompute half-length. Same as for LTC area lights.
    float3 p1 = lightData.positionWS - lightData.right * (0.5 * len);
    float3 p2 = lightData.positionWS + lightData.right * (0.5 * len);

    // Translate both points s.t. the shaded point is at the origin of the coordinate system.
    p1 -= positionWS;
    p2 -= positionWS;

    // Construct an orthonormal basis (local coordinate system) around N.
    // TODO: it could be stored in PreLightData. All LTC lights compute it more than once!
    float3x3 basis;
    basis[0] = normalize(V - bsdfData.normalWS * preLightData.NdotV);
    basis[1] = normalize(cross(bsdfData.normalWS, basis[0]));
    basis[2] = bsdfData.normalWS;

    // Transform (rotate) both endpoints into the local coordinate system (left-handed).
    p1 = mul(p1, transpose(basis));
    p2 = mul(p2, transpose(basis));

    // Terminate the algorithm if both points are below the horizon.
    if (p1.z <= 0.0 && p2.z <= 0.0) return;

    if (p2.z <= 0.0)
    {
        // Convention: 'p2' is above the horizon.
        swap(p1, p2);
        dir = -dir;
    }

    // Clip the part of the light below the horizon.
    if (p1.z <= 0.0)
    {
        // p = p1 + t * dir; p.z == 0.
        float t = -p1.z / dir.z;
        p1 = float3(p1.xy + t * dir.xy, 0.0);

        // Set the length of the visible part of the light.
        len -= t;
    }

    // Compute the direction to the point on the line orthogonal to 'dir'.
    // Its length is the shortest distance to the line.
    float3 p0   = p1 - dot(p1, dir) * dir;
    float  dist = length(p0);

    // Compute the parameterization: distances from 'l1' and 'l2' to 'l0'.
    float l1 = dot(p1 - p0, dir);
    float l2 = l1 + len;

    // Integrate the clamped cosine over the line segment.
    float irradiance = LineIrradiance(l1, l2, dist, p0.z, dir.z);

    // Only Lambertian for now. TODO: Disney Diffuse and GGX.
    diffuseLighting = (lightData.diffuseScale * irradiance * INV_PI) * bsdfData.diffuseColor * lightData.color;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Area | Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

void EvaluateBSDF_Area( LightLoopContext lightLoopContext,
                        float3 V, float3 positionWS, PreLightData preLightData, LightData lightData, BSDFData bsdfData,
                        out float3 diffuseLighting,
                        out float3 specularLighting)
{
#ifdef LIT_DISPLAY_REFERENCE_AREA
    if (lightData.lightType == GPULIGHTTYPE_LINE)
    {
        IntegrateBSDFLineRef(V, positionWS, preLightData, lightData, bsdfData, diffuseLighting, specularLighting);
    }
    else
    {
        IntegrateGGXAreaRef(V, positionWS, preLightData, lightData, bsdfData, diffuseLighting, specularLighting);
    }
#else
    if (lightData.lightType == GPULIGHTTYPE_LINE)
    {
        diffuseLighting = float3(0.0, 0.0, 0.0);
        specularLighting = float3(0.0, 0.0, 0.0);
        // EvaluateBSDF_Line(lightLoopContext, V, positionWS, preLightData, lightData, bsdfData, diffuseLighting, specularLighting);
        return;
    }
    
    // TODO: This could be precomputed
    float halfWidth  = lightData.size.x * 0.5;
    float halfHeight = lightData.size.y * 0.5;

    // TODO: store 4 points and save 12 cycles (24x MADs - 12x MOVs).
    float3 p0 = lightData.positionWS + lightData.right * -halfWidth + lightData.up *  halfHeight;
    float3 p1 = lightData.positionWS + lightData.right * -halfWidth + lightData.up * -halfHeight;
    float3 p2 = lightData.positionWS + lightData.right *  halfWidth + lightData.up * -halfHeight;
    float3 p3 = lightData.positionWS + lightData.right *  halfWidth + lightData.up *  halfHeight;

    float4x3 matL = float4x3(p0, p1, p2, p3);
    float4x3 L    = matL - float4x3(positionWS, positionWS, positionWS, positionWS);

    diffuseLighting  = float3(0.0, 0.0, 0.0);
    specularLighting = float3(0.0, 0.0, 0.0);

    // Pick the correct axis along which to expand the fade-out sphere into an ellipsoid.
    float3 axisLS;
    float  minDim, maxDim;

    // The compiler should generate conditional MOVs.
    if (halfWidth >= halfHeight)
    {
        axisLS = lightData.right;
        minDim = halfHeight;
        maxDim = halfWidth;
    }
    else
    {
        axisLS = lightData.up;
        minDim = halfWidth;
        maxDim = halfHeight;
    }

    float3 dirLS          = positionWS - lightData.positionWS;
    float  lightSpaceProj = dot(dirLS, axisLS);
    float  invAspectRatio = minDim / maxDim;

    // We want 'dirLS' to shrink along 'axisLS' by the aspect ratio. Therefore,
    // we compute the difference between the original length and the shrunk one.
    // This is equivalent to the expansion of the fade-out sphere into an ellipsoid.
    float scaleLS = lightSpaceProj - lightSpaceProj * invAspectRatio;
    dirLS -= scaleLS * axisLS;

    // Compute the light attenuation.
    float sqDist    = dot(dirLS, dirLS);
    float intensity = SmoothDistanceAttenuation(sqDist, lightData.invSqrAttenuationRadius);

    // Return the black color if the shaded point is too far away.
    if (intensity == 0.0) return;

    lightData.diffuseScale  *= intensity;
    lightData.specularScale *= intensity;

    float ltcValue;

    // Evaluate the diffuse part.
    {
    #ifdef DIFFUSE_LAMBERT_BRDF
        static const float3x3 identity3x3 = {1.0, 0.0, 0.0,
                                             0.0, 1.0, 0.0,
                                             0.0, 0.0, 1.0};
                                             
        ltcValue = LTCEvaluate(L, V, bsdfData.normalWS, preLightData.NdotV, lightData.twoSided,
                               identity3x3);
    #else
        ltcValue = LTCEvaluate(L, V, bsdfData.normalWS, preLightData.NdotV, lightData.twoSided,
                               preLightData.ltcXformDisneyDiffuse);
    #endif

        if (ltcValue == 0.0)
        {
            // The polygon is either back-facing, or has been completely clipped.
            return;
        }

    #ifndef DIFFUSE_LAMBERT_BRDF
        ltcValue *= preLightData.ltcDisneyDiffuseMagnitude;
    #endif

        ltcValue *= lightData.diffuseScale;
        diffuseLighting = bsdfData.diffuseColor * lightData.color * ltcValue;
    }

    // Evaluate the specular part.
    {
        float3 fresnelTerm = bsdfData.fresnel0 * preLightData.ltcGGXFresnelMagnitudeDiff
                           + (float3)preLightData.ltcGGXFresnelMagnitude;

        ltcValue  = LTCEvaluate(L, V, bsdfData.normalWS, preLightData.NdotV, lightData.twoSided,
                                preLightData.ltcXformGGX);
        ltcValue *= lightData.specularScale;
        specularLighting = fresnelTerm * lightData.color * ltcValue;
    }
#endif
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Env - Reference
// ----------------------------------------------------------------------------

// Ref: Moving Frostbite to PBR (Appendix A)
float3 IntegrateLambertIBLRef(  LightLoopContext lightLoopContext,
                                EnvLightData lightData, BSDFData bsdfData,
                                uint sampleCount = 2048)
{
    float3 N        = bsdfData.normalWS;
    float3 tangentX = bsdfData.tangentWS;
    float3 tangentY = bsdfData.bitangentWS;
    float3 acc      = float3(0.0, 0.0, 0.0);

    // Add some jittering on Hammersley2d
    float2 randNum  = InitRandom(N.xy * 0.5 + 0.5);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float2 u    = Hammersley2d(i, sampleCount);
        u           = frac(u + randNum + 0.5);

        float3 L;
        float NdotL;
        float weightOverPdf;
        ImportanceSampleLambert(u, N, tangentX, tangentY, L, NdotL, weightOverPdf);

        if (NdotL > 0.0)
        {
            float4 val = SampleEnv(lightLoopContext, lightData.envIndex, L, 0);

            // diffuse Albedo is apply here as describe in ImportanceSampleLambert function
            acc += bsdfData.diffuseColor * LambertNoPI() * weightOverPdf * val.rgb;
        }
    }

    return acc / sampleCount;
}

float3 IntegrateDisneyDiffuseIBLRef(LightLoopContext lightLoopContext,
                                    float3 V, EnvLightData lightData, BSDFData bsdfData,
                                    uint sampleCount = 2048)
{
    float3 N        = bsdfData.normalWS;
    float3 tangentX = bsdfData.tangentWS;
    float3 tangentY = bsdfData.bitangentWS;
    float  NdotV    = saturate(dot(N, V));
    float3 acc      = float3(0.0, 0.0, 0.0);

    // Add some jittering on Hammersley2d
    float2 randNum  = InitRandom(N.xy * 0.5 + 0.5);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float2 u    = Hammersley2d(i, sampleCount);
        u           = frac(u + randNum + 0.5);

        float3 L;
        float NdotL;
        float weightOverPdf;
        // for Disney we still use a Cosine importance sampling, true Disney importance sampling imply a look up table
        ImportanceSampleLambert(u, N, tangentX, tangentY, L, NdotL, weightOverPdf);

        if (NdotL > 0.0)
        {            
            float3 H = normalize(L + V);
            float LdotH = dot(L, H);
            // Note: we call DisneyDiffuse that require to multiply by Albedo / PI. Divide by PI is already taken into account
            // in weightOverPdf of ImportanceSampleLambert call.
            float disneyDiffuse = DisneyDiffuse(NdotV, NdotL, LdotH, bsdfData.perceptualRoughness);

            // diffuse Albedo is apply here as describe in ImportanceSampleLambert function
            float4 val = SampleEnv(lightLoopContext, lightData.envIndex, L, 0);
            acc += bsdfData.diffuseColor * disneyDiffuse * weightOverPdf * val.rgb;
        }
    }

    return acc / sampleCount;
}

// Ref: Moving Frostbite to PBR (Appendix A)
float3 IntegrateSpecularGGXIBLRef(  LightLoopContext lightLoopContext,
                                    float3 V, EnvLightData lightData, BSDFData bsdfData,
                                    uint sampleCount = 2048)
{
    float3 N        = bsdfData.normalWS;
    float3 tangentX = bsdfData.tangentWS;
    float3 tangentY = bsdfData.bitangentWS;
    float  NdotV    = saturate(dot(N, V));
    float3 acc      = float3(0.0, 0.0, 0.0);

    // Add some jittering on Hammersley2d
    float2 randNum  = InitRandom(V.xy * 0.5 + 0.5);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float2 u    = Hammersley2d(i, sampleCount);
        u           = frac(u + randNum + 0.5);

        float VdotH;
        float NdotL;
        float3 L;
        float weightOverPdf;

        // GGX BRDF
        if (bsdfData.materialId = MATERIALID_LIT_ANISO)
        {
            ImportanceSampleAnisoGGX(u, V, N, tangentX, tangentY, bsdfData.roughnessT, bsdfData.roughnessB, NdotV, L, VdotH, NdotL, weightOverPdf);
        }
        else
        {
            ImportanceSampleGGX(u, V, N, tangentX, tangentY, bsdfData.roughness, NdotV, L, VdotH, NdotL, weightOverPdf);
        }


        if (NdotL > 0.0)
        {
            // Fresnel component is apply here as describe in ImportanceSampleGGX function
            float3 FweightOverPdf = F_Schlick(bsdfData.fresnel0, VdotH) * weightOverPdf;

            float4 val = SampleEnv(lightLoopContext, lightData.envIndex, L, 0);

            acc += FweightOverPdf * val.rgb;
        }
    }

    return acc / sampleCount;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Env
// ----------------------------------------------------------------------------

// _preIntegratedFGD and _CubemapLD are unique for each BRDF
void EvaluateBSDF_Env(  LightLoopContext lightLoopContext,
                        float3 V, float3 positionWS, PreLightData preLightData, EnvLightData lightData, BSDFData bsdfData,
                        out float3 diffuseLighting, out float3 specularLighting, out float2 weight)
{
#ifdef LIT_DISPLAY_REFERENCE_IBL

    specularLighting = IntegrateSpecularGGXIBLRef(lightLoopContext, V, lightData, bsdfData);

/*
    #ifdef LIT_DIFFUSE_LAMBERT_BRDF
    diffuseLighting = IntegrateLambertIBLRef(lightData, bsdfData);
    #else
    diffuseLighting = IntegrateDisneyDiffuseIBLRef(lightLoopContext, V, lightData, bsdfData);
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
    
    // Note: As explain in GetPreLightData we use normalWS and not iblNormalWS here (in case of anisotropy)
    float3 rayWS = GetSpecularDominantDir(bsdfData.normalWS, preLightData.iblR, bsdfData.roughness);

    float3 R = rayWS;
    weight = float2(1.0, 1.0);

    // In this code we redefine a bit the behavior of the reflcetion proble. We separate the projection volume (the proxy of the scene) form the influence volume (what pixel on the screen is affected)

    // 1. First determine the projection volume
    
    // In Unity the cubemaps are capture with the localToWorld transform of the component. 
    // This mean that location and oritention matter. So after intersection of proxy volume we need to convert back to world.
    
    // CAUTION: localToWorld is the transform use to convert the cubemap capture point to world space (mean it include the offset)
    // the center of the bounding box is thus in locals space: positionLS - offsetLS
    // We use this formulation as it is the one of legacy unity that was using only AABB box.

    float3x3 worldToLocal = transpose(float3x3(lightData.right, lightData.up, lightData.forward)); // worldToLocal assume no scaling
    float3 positionLS = positionWS - lightData.positionWS;
    positionLS = mul(positionLS, worldToLocal).xyz - lightData.offsetLS; // We want to calculate the intersection from the center of the bounding box.

    if (lightData.envShapeType == ENVSHAPETYPE_BOX)
    {
        float3 rayLS = mul(rayWS, worldToLocal);
        float3 boxOuterDistance = lightData.innerDistance + float3(lightData.blendDistance, lightData.blendDistance, lightData.blendDistance);
        float dist = BoxRayIntersectSimple(positionLS, rayLS, -boxOuterDistance, boxOuterDistance);

        // No need to normalize for fetching cubemap
        // We can reuse dist calculate in LS directly in WS as there is no scaling. Also the offset is already include in lightData.positionWS
        R = (positionWS + dist * rayWS) - lightData.positionWS;
        
        // TODO: add distance based roughness
    } 
    else if (lightData.envShapeType == ENVSHAPETYPE_SPHERE)
    {
        float3 rayLS = mul(rayWS, worldToLocal);
        float sphereOuterDistance = lightData.innerDistance.x + lightData.blendDistance;
        float dist = SphereRayIntersectSimple(positionLS, rayLS, sphereOuterDistance);

        R = (positionWS + dist * rayWS) - lightData.positionWS;
    }

    // 2. Apply the influence volume (Box volume is used for culling whatever the influence shape)
    // TODO: In the future we could have an influence volume inside the projection volume (so with a different transform, in this case we will need another transform)
    if (lightData.envShapeType == ENVSHAPETYPE_SPHERE)
    {
        float distFade = max(length(positionLS) - lightData.innerDistance.x, 0.0);
        weight.y = saturate(1.0 - distFade / max(lightData.blendDistance, 0.0001)); // avoid divide by zero
    }
    else // ENVSHAPETYPE_BOX or ENVSHAPETYPE_NONE 
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

    // TODO: compare current Morten version: offline cubemap with a particular remap + the bias in perceptualRoughnessToMipmapLevel
    // to classic remap like unreal/Frobiste. The function GetSpecularDominantDir can result in a better matching in this case
    // We let GetSpecularDominantDir currently as it still an improvement but not as good as it could be
    float mip = perceptualRoughnessToMipmapLevel(bsdfData.perceptualRoughness);
    float4 preLD = SampleEnv(lightLoopContext, lightData.envIndex, R, mip);
    specularLighting = preLD.rgb * preLightData.specularFGD;

    // Apply specular occlusion on it
    specularLighting *= bsdfData.specularOcclusion;
    diffuseLighting = float3(0.0, 0.0, 0.0);

#endif    
}

#endif // #ifdef HAS_LIGHTLOOP


