#ifndef UNITY_MATERIAL_LIT_INCLUDED
#define UNITY_MATERIAL_LIT_INCLUDED

//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------

// SurfaceData is define in Lit.cs which generate Lit.cs.hlsl
#include "Lit.cs.hlsl"

// TODO: Check if anisotropy with a dynamic if on anisotropy > 0 is performant. Because it may mean we always calculate both isotrpy and anisotropy case.
// Maybe we should always calculate anisotropy in case of standard ? Don't think the compile can optimize correctly.

//-----------------------------------------------------------------------------
// Helper functions/variable specific to this materia
//-----------------------------------------------------------------------------

float PackMaterialId(int materialId)
{
    return float(materialId) / 3.0;
}

int UnpackMaterialId(float f)
{
    return int(round(f * 3.0));
}

// TODO: How can I declare a sampler for this one that is bilinear filtering
// TODO: This one should be set into a constant Buffer at pass frequency (with _Screensize)
UNITY_DECLARE_TEX2D(_PreIntegratedFGD);

// For image based lighting, a part of the BSDF is pre-integrated.
// This is done both for specular and diffuse (in case of DisneyDiffuse)
void GetPreIntegratedFGD(float NdotV, float perceptualRoughness, float3 fresnel0, out float3 specularFGD, out float diffuseFGD)
{
    // Pre-integrate GGX FGD
    //  _PreIntegratedFGD.x = Gv * (1 - Fc)  with Fc = (1 - H.L)^5
    //  _PreIntegratedFGD.y = Gv * Fc
    // Pre integrate DisneyDiffuse FGD:
    // _PreIntegratedFGD.z = DisneyDiffuse
    float3 preFGD = UNITY_SAMPLE_TEX2D_LOD(_PreIntegratedFGD, float2(NdotV, perceptualRoughness), 0).xyz;

    // f0 * Gv * (1 - Fc) + Gv * Fc
    specularFGD = fresnel0 * preFGD.x + preFGD.y;
#if DIFFUSE_LAMBERT_BRDF
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
    BSDFData bsdfData = (BSDFData)0;

    bsdfData.specularOcclusion = surfaceData.specularOcclusion;
    bsdfData.normalWS = surfaceData.normalWS;
    bsdfData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);
    bsdfData.roughness = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);
    bsdfData.materialId = surfaceData.materialId;
    bsdfData.diffuseColor = surfaceData.baseColor;

    if (bsdfData.materialId == MATERIALID_LIT_STANDARD)
    {
        bsdfData.diffuseColor = surfaceData.baseColor * (1.0 - surfaceData.metalic);
        bsdfData.fresnel0 = lerp(float3(surfaceData.specular, surfaceData.specular, surfaceData.specular), surfaceData.baseColor, surfaceData.metalic);

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
        bsdfData.diffuseColor = surfaceData.baseColor * (1.0 - surfaceData.metalic);
        bsdfData.fresnel0 = lerp(float3(surfaceData.specular, surfaceData.specular, surfaceData.specular), surfaceData.baseColor, surfaceData.metalic);
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
                        out float4 outGBuffer0,
                        out float4 outGBuffer1,
                        out float4 outGBuffer2)
{
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
        outGBuffer2 = float4(octTangentWS * 0.5 + 0.5, surfaceData.anisotropy, surfaceData.metalic);
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
        outGBuffer2 = float4(octCoatNormalWS * 0.5 + 0.5, PerceptualSmoothnessToRoughness(surfaceData.coatPerceptualSmoothness), surfaceData.metalic);
    }
    else if (surfaceData.materialId == MATERIALID_LIT_SPECULAR)
    {
        outGBuffer2 = float4(surfaceData.specularColor, 0.0);
    }
}

BSDFData DecodeFromGBuffer( float4 inGBuffer0,
                            float4 inGBuffer1,
                            float4 inGBuffer2)
{
    BSDFData bsdfData = (BSDFData)0;

    float3 baseColor = inGBuffer0.rgb;
    bsdfData.specularOcclusion = inGBuffer0.a;

    bsdfData.normalWS = UnpackNormalOctEncode(float2(inGBuffer1.r * 2.0 - 1.0, inGBuffer1.g * 2.0 - 1.0));
    bsdfData.perceptualRoughness = inGBuffer1.b;
    bsdfData.roughness = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);
    bsdfData.materialId = UnpackMaterialId(inGBuffer1.a);

    if (bsdfData.materialId == MATERIALID_LIT_STANDARD)
    {
        float metalic = inGBuffer2.a;
        // TODO extract spec
        float specular = 0.04;
        float anisotropy = inGBuffer2.b;

        bsdfData.diffuseColor = baseColor * (1.0 - metalic);
        bsdfData.fresnel0 = lerp(float3(specular, specular, specular), baseColor, metalic);

        bsdfData.tangentWS = UnpackNormalOctEncode(float2(inGBuffer2.rg * 2.0 - 1.0));
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
        float metalic = inGBuffer2.a;
        // TODO extract spec
        float specular = 0.04;

        bsdfData.diffuseColor = baseColor * (1.0 - metalic);
        bsdfData.fresnel0 = lerp(float3(specular, specular, specular), baseColor, metalic);
        bsdfData.coatNormalWS = UnpackNormalOctEncode(float2(inGBuffer2.rg * 2.0 - 1.0));
        bsdfData.coatRoughness = inGBuffer2.b;
    }
    else if (bsdfData.materialId == MATERIALID_LIT_SPECULAR)
    {
        bsdfData.diffuseColor = baseColor;
        bsdfData.fresnel0 = inGBuffer2.rgb;
    }

    return bsdfData;
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
        case DEBUGVIEW_LIT_SURFACEDATA_METALIC:
            result = surfaceData.metalic.xxx;
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
    float3 iblR;        // Reflction vector, same as above.

    float3 specularFGD; // Store preconvole BRDF for both specular and diffuse
    float diffuseFGD;

    // TODO: if we want we can store ambient occlusion here from SSAO pass for example that can be use for IBL specular occlusion
    // float ambientOcclusion; // Feed from an ambient occlusion buffer
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
        iblNormalWS = GetAnisotropicModifiedNormal(bsdfData.normalWS, bsdfData.tangentWS, V, bsdfData.anisotropy);
        
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

    return preLightData;
}

//-----------------------------------------------------------------------------
// bake lighting function
//-----------------------------------------------------------------------------

// GetBakedDiffuseLigthing function compute the bake lighting + emissive color to be store in emissive buffer (Deferred case)
// In forward it must be add to the final contribution.
// This function require the 3 structure surfaceData, builtinData, bsdfData because it may require both the engine side data, and data that will not be store inside the gbuffer.
float3 GetBakedDiffuseLigthing(PreLightData prelightData, SurfaceData surfaceData, BuiltinData builtinData, BSDFData bsdfData)
{
    // Premultiply bake diffuse lighting information with DisneyDiffuse pre-integration
    return builtinData.bakeDiffuseLighting * prelightData.diffuseFGD * surfaceData.ambientOcclusion * bsdfData.diffuseColor + builtinData.emissiveColor * builtinData.emissiveIntensity;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Punctual
//-----------------------------------------------------------------------------

void EvaluateBSDF_Punctual(	float3 V, float3 positionWS, PreLightData prelightData, PunctualLightData lightData, BSDFData bsdfData,
                            out float4 diffuseLighting,
                            out float4 specularLighting)
{
    // All punctual light type in the same formula, attenuation is neutral depends on light type.
    // light.positionWS is the normalize light direction in case of directional light and invSqrAttenuationRadius is 0
    // mean dot(unL, unL) = 1 and mean GetDistanceAttenuation() will return 1
    // For point light and directional GetAngleAttenuation() return 1

    float3 unL = lightData.positionWS - positionWS * lightData.useDistanceAttenuation;
    float3 L = normalize(unL);

    float attenuation = GetDistanceAttenuation(unL, lightData.invSqrAttenuationRadius);
    attenuation *= GetAngleAttenuation(L, lightData.forward, lightData.angleScale, lightData.angleOffset);
    float illuminance = saturate(dot(bsdfData.normalWS, L)) * attenuation;

    diffuseLighting = float4(0.0, 0.0, 0.0, 1.0);
    specularLighting = float4(0.0, 0.0, 0.0, 1.0);

    if (illuminance > 0.0f)
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

            #ifdef USE_BSDF_PRE_LAMBDAV
            Vis = V_SmithJointGGXAnisoLambdaV(  prelightData.TdotV, prelightData.BdotV, prelightData.NdotV, TdotL, BdotL, NdotL,
                                                bsdfData.roughnessT, bsdfData.roughnessB, prelightData.anisoGGXlambdaV);
            #else
            Vis = V_SmithJointGGXAniso( prelightData.TdotV, prelightData.BdotV, prelightData.NdotV, TdotL, BdotL, NdotL,
                                        bsdfData.roughnessT, bsdfData.roughnessB);
            #endif

            float TdotH = saturate(dot(bsdfData.tangentWS, H));
            float BdotH = saturate(dot(bsdfData.bitangentWS, H));
            D = D_GGXAnisoDividePI(TdotH, BdotH, NdotH, bsdfData.roughnessT, bsdfData.roughnessB);
        }
        else
        {
            #ifdef USE_BSDF_PRE_LAMBDAV
            Vis = V_SmithJointGGX(NdotL, prelightData.NdotV, bsdfData.roughness, prelightData.ggxLambdaV);
            #else
            Vis = V_SmithJointGGX(NdotL, prelightData.NdotV, bsdfData.roughness);
            #endif
            D = D_GGXDividePI(NdotH, bsdfData.roughness);
        }
        specularLighting.rgb = F * Vis * D;
        #ifdef DIFFUSE_LAMBERT_BRDF
        float diffuseTerm = LambertDividePI();
        #else
        float diffuseTerm = DisneyDiffuseDividePI(prelightData.NdotV, NdotL, LdotH, bsdfData.perceptualRoughness);
        #endif
        diffuseLighting.rgb = bsdfData.diffuseColor * diffuseTerm;

        diffuseLighting.rgb *= lightData.color * illuminance;
        specularLighting.rgb *= lightData.color * illuminance;
    }
}

//-----------------------------------------------------------------------------
// Reference code for image based lighting
// ----------------------------------------------------------------------------

// Ref: Moving Frostbite to PBR (Appendix A)
float3 IntegrateLambertIBLRef(  EnvLightData lightData, BSDFData bsdfData,
                                UNITY_ARGS_ENV(_EnvTextures),
                                uint sampleCount = 2048)
{
    float3 N        = bsdfData.normalWS;
    float3 acc      = float3(0.0, 0.0, 0.0);
    // Add some jittering on Hammersley2d
    float2 randNum  = InitRandom(N.xy * 0.5 + 0.5);

    float3 tangentX, tangentY;
    GetLocalFrame(N, tangentX, tangentY);

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
            float4 val = UNITY_SAMPLE_ENV_LOD(_EnvTextures, L, lightData, 0);

            // diffuse Albedo is apply here as describe in ImportanceSampleLambert function
            acc += bsdfData.diffuseColor * Lambert() * weightOverPdf * val.rgb;
        }
    }

    return acc / sampleCount;
}

float3 IntegrateDisneyDiffuseIBLRef(float3 V, EnvLightData lightData, BSDFData bsdfData,
                                    UNITY_ARGS_ENV(_EnvTextures),
                                    uint sampleCount = 2048)
{
    float3 N = bsdfData.normalWS;
    float NdotV = dot(N, V);
    float3 acc  = float3(0.0, 0.0, 0.0);
    // Add some jittering on Hammersley2d
    float2 randNum  = InitRandom(N.xy * 0.5 + 0.5);

    float3 tangentX, tangentY;
    GetLocalFrame(N, tangentX, tangentY);

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
            float4 val = UNITY_SAMPLE_ENV_LOD(_EnvTextures, L, lightData, 0);
            acc += bsdfData.diffuseColor * disneyDiffuse * weightOverPdf * val.rgb;
        }
    }

    return acc / sampleCount;
}

// Ref: Moving Frostbite to PBR (Appendix A)
float3 IntegrateSpecularGGXIBLRef(  float3 V, EnvLightData lightData, BSDFData bsdfData,
                                    UNITY_ARGS_ENV(_EnvTextures),
                                    uint sampleCount = 2048)
{
    float3 N        = bsdfData.normalWS;
    float NdotV     = saturate(dot(N, V));
    float3 acc      = float3(0.0, 0.0, 0.0);

    // Add some jittering on Hammersley2d
    float2 randNum  = InitRandom(V.xy * 0.5 + 0.5);

    float3 tangentX, tangentY;
    GetLocalFrame(N, tangentX, tangentY);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float2 u    = Hammersley2d(i, sampleCount);
        u           = frac(u + randNum + 0.5);

        float VdotH;
        float NdotL;
        float3 L;
        float weightOverPdf;

        // GGX BRDF
        ImportanceSampleGGX(u, V, N, tangentX, tangentY, bsdfData.roughness, NdotV,
                            L, VdotH, NdotL, weightOverPdf);

        if (NdotL > 0.0)
        {
            // Fresnel component is apply here as describe in ImportanceSampleGGX function
            float3 FweightOverPdf = F_Schlick(bsdfData.fresnel0, VdotH) * weightOverPdf;

            float4 val = UNITY_SAMPLE_ENV_LOD(_EnvTextures, L, lightData, 0);

            acc += FweightOverPdf * val.rgb;
        }
    }

    return acc / sampleCount;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Env
// ----------------------------------------------------------------------------

// _preIntegratedFGD and _CubemapLD are unique for each BRDF
void EvaluateBSDF_Env(  float3 V, float3 positionWS, PreLightData prelightData, EnvLightData lightData, BSDFData bsdfData,
                        UNITY_ARGS_ENV(_EnvTextures),
                        out float4 diffuseLighting,
                        out float4 specularLighting)
{
// Reference Lambert diffuse / GGX Specular
//#define LIT_DISPLAY_REFERENCE

#ifdef LIT_DISPLAY_REFERENCE

    specularLighting.rgb = IntegrateSpecularGGXIBLRef(V, lightData, bsdfData, UNITY_PASS_ENV(_EnvTextures));
    specularLighting.a = 1.0;

/*
    #ifdef DIFFUSE_LAMBERT_BRDF
    diffuseLighting.rgb = IntegrateLambertIBLRef(lightData, bsdfData, UNITY_PASS_ENV(_EnvTextures));
    #else
    diffuseLighting.rgb = IntegrateDisneyDiffuseIBLRef(V, lightData, bsdfData, UNITY_PASS_ENV(_EnvTextures));
    #endif
    diffuseLighting.a = 1.0;
*/
    diffuseLighting = float4(0.0, 0.0, 0.0, 0.0);

#else
    // TODO: factor this code in common, so other material authoring don't require to rewrite everything, 
    // also think about how such a loop can handle 2 cubemap at the same time as old unity. Macro can allow to do that
    // but we need to have UNITY_SAMPLE_ENV_LOD replace by a true function instead that is define by the lighting arcitecture.
    // Also not sure how to deal with 2 intersection....
    // Box and sphere are related to light property (but we have also distance based roughness etc...)

    // TODO: test the strech from Tomasz
    // float shrinkedRoughness = AnisotropicStrechAtGrazingAngle(bsdfData.roughness, bsdfData.perceptualRoughness, NdotV);
    
    // Note: As explain in GetPreLightData we use normalWS and not iblNormalWS here (in case of anisotropy)
    //float3 rayWS = GetSpecularDominantDir(bsdfData.normalWS, prelightData.iblR, bsdfData.roughness);

    float3 rayWS = prelightData.iblR;

    float3 R = rayWS;
    float weight = 1.0;

    /*
    if (lightData.shapeType == ENVSHAPETYPE_BOX)
    {
        // worldToLocal assume no scaling
        float3 positionLS = mul(lightData.worldToLocal, float4(positionWS, 1.0)).xyz;
        float3 rayLS = mul((float3x3)lightData.worldToLocal, rayWS);
        float3 boxOuterDistance = lightData.innerDistance + float3(lightData.blendDistance, lightData.blendDistance, lightData.blendDistance);
        float dist = BoxRayIntersectSimple(positionLS, rayLS, -boxOuterDistance, boxOuterDistance);
 
        // No need to normalize for fetching cubemap
        R = (positionWS + dist * rayWS) - lightData.capturePointWS; // TODO: check that

        // TODO: add distance based roughness

        // Calculate falloff value, so reflections on the edges of the Volume would gradually blend to previous reflection.
        // Also this ensures that pixels not located in the reflection Volume AABB won't
        // accidentally pick up reflections from this Volume.
        float distFade = DistancePointBox(positionLS, -lightData.innerDistance, lightData.innerDistance);
        weight = saturate(1.0 - distFade / max(lightData.blendDistance, 0.0001)); // avoid divide by zero

        // Smooth weighting
        weight = smoothstep01(weight);
    }
    else if (lightData.shapeType == ENVSHAPETYPE_SPHERE)
    {
        float sphereRadius = lightData.innerDistance.x;
        float2 intersections;
        SphereRayIntersect(intersections, positionWS - lightData.positionWS, R, sphereRadius);
        // TODO: check if we can have simplified formula like for box
        // No need to normalize for fetching cubemap
        R = (positionWS + intersections.y * rayWS) - lightData.capturePointWS;

        float distFade = length(positionWS - lightData.positionWS);
        weight = saturate(((sphereRadius + lightData.blendDistance) - distFade) / max(lightData.blendDistance, 0.0001)); // avoid divide by zero
        
        // Smooth weighting
        weight = smoothstep01(weight);
    }
    */

    float mip = perceptualRoughnessToMipmapLevel(bsdfData.perceptualRoughness);
    float4 preLD = UNITY_SAMPLE_ENV_LOD(_EnvTextures, R, lightData, mip);
    specularLighting.rgb = preLD.rgb * prelightData.specularFGD;

    // Apply specular occlusion on it
    specularLighting.rgb *= bsdfData.specularOcclusion;
    specularLighting.a = weight;

    diffuseLighting = float4(0.0, 0.0, 0.0, 0.0);

#endif    
}

#endif // UNITY_MATERIAL_LIT_INCLUDED
