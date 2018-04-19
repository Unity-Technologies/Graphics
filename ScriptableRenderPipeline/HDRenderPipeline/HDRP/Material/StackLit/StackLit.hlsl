//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------
// SurfaceData is defined in StackLit.cs which generates StackLit.cs.hlsl
#include "StackLit.cs.hlsl"
//#include "../SubsurfaceScattering/SubsurfaceScattering.hlsl"
//#include "CoreRP/ShaderLibrary/VolumeRendering.hlsl"

//NEWLITTODO : wireup CBUFFERs for ambientocclusion, and other uniforms and samplers used: 
//
// We need this for AO, Depth/Color pyramids, LTC lights data, FGD pre-integrated data.
//
// Also add options at the top of this file, see Lit.hlsl.

//-----------------------------------------------------------------------------
// Texture and constant buffer declaration
//-----------------------------------------------------------------------------

// Declare the BSDF specific FGD property and its fetching function
#include "../PreIntegratedFGD/PreIntegratedFGD.hlsl"

//-----------------------------------------------------------------------------
// Definition
//-----------------------------------------------------------------------------

#define HAS_REFRACTION (defined(_REFRACTION_PLANE) || defined(_REFRACTION_SPHERE)) && (defined(_REFRACTION_SSRAY_PROXY) || defined(_REFRACTION_SSRAY_HIZ))
#define DEFAULT_SPECULAR_VALUE 0.04

//-----------------------------------------------------------------------------
// Configuration
//-----------------------------------------------------------------------------
#define LIT_DIFFUSE_LAMBERT_BRDF // TODO Disney Diffuse
#define LIT_USE_GGX_ENERGY_COMPENSATION


//-----------------------------------------------------------------------------
// Helper functions/variable specific to this material
//-----------------------------------------------------------------------------

// This method allows us to know at compile time what material features should be removed from the code by Tile (Indepenently of the value of material feature flag per pixel).
// This is only useful for classification during lighting, so it's not needed in EncodeIntoGBuffer and ConvertSurfaceDataToBSDFData (where we always know exactly what the material feature is)
bool HasFeatureFlag(uint featureFlags, uint flag)
{
    return ((featureFlags & flag) != 0);
}

float3 ComputeDiffuseColor(float3 baseColor, float metallic)
{
    return baseColor * (1.0 - metallic);
}

float3 ComputeFresnel0(float3 baseColor, float metallic, float dielectricF0)
{
    return lerp(dielectricF0.xxx, baseColor, metallic);
}




// This function is use to help with debugging and must be implemented by any lit material
// Implementer must take into account what are the current override component and
// adjust SurfaceData properties accordingdly
void ApplyDebugToSurfaceData(float3x3 worldToTangent, inout SurfaceData surfaceData)
{
#ifdef DEBUG_DISPLAY
    // NOTE: THe _Debug* uniforms come from /HDRP/Debug/DebugDisplay.hlsl

    // Override value if requested by user
    // this can be use also in case of debug lighting mode like diffuse only
    bool overrideAlbedo = _DebugLightingAlbedo.x != 0.0;
    bool overrideSmoothness = _DebugLightingSmoothness.x != 0.0;
    bool overrideNormal = _DebugLightingNormal.x != 0.0;

    if (overrideAlbedo)
    {
        float3 overrideAlbedoValue = _DebugLightingAlbedo.yzw;
        surfaceData.baseColor = overrideAlbedoValue;
    }

    if (overrideSmoothness)
    {
        //NEWLITTODO
        //float overrideSmoothnessValue = _DebugLightingSmoothness.y;
        //surfaceData.perceptualSmoothness = overrideSmoothnessValue;
    }

    if (overrideNormal)
    {
        surfaceData.normalWS = worldToTangent[2];
    }
#endif
}

// This function is similar to ApplyDebugToSurfaceData but for BSDFData
//
// NOTE: 
//
// This will be available and used in ShaderPassForward.hlsl since in StackLit.shader, 
// just before including the core code of the pass (ShaderPassForward.hlsl) we include 
// Material.hlsl (or Lighting.hlsl which includes it) which in turn includes us, 
// StackLit.shader, via the #if defined(UNITY_MATERIAL_*) glue mechanism.
//
void ApplyDebugToBSDFData(inout BSDFData bsdfData)
{
#ifdef DEBUG_DISPLAY
    // Override value if requested by user
    // this can be use also in case of debug lighting mode like specular only
    
    //NEWLITTODO
    //bool overrideSpecularColor = _DebugLightingSpecularColor.x != 0.0;

    //if (overrideSpecularColor)
    //{
    //   float3 overrideSpecularColor = _DebugLightingSpecularColor.yzw;
    //    bsdfData.fresnel0 = overrideSpecularColor;
    //}
#endif
}

//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(SurfaceData surfaceData)
{
    BSDFData bsdfData;
    ZERO_INITIALIZE(BSDFData, bsdfData);

    // IMPORTANT: In our forward only case, all enable flags are statically know at compile time, so the compiler can do compile time optimization
    bsdfData.materialFeatures = surfaceData.materialFeatures;

    // Two lobe base material
    bsdfData.normalWS = surfaceData.normalWS;
    bsdfData.perceptualRoughnessA = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothnessA);
    bsdfData.perceptualRoughnessB = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothnessB);
    bsdfData.lobeMix = surfaceData.lobeMix;

    // There is no metallic with SSS and specular color mode
    //todo: float metallic = HasFeatureFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR | MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_LIT_TRANSMISSION) ? 0.0 : surfaceData.metallic;
    float metallic = surfaceData.metallic;

    bsdfData.diffuseColor = ComputeDiffuseColor(surfaceData.baseColor, metallic);
    bsdfData.fresnel0 = ComputeFresnel0(surfaceData.baseColor, surfaceData.metallic, DEFAULT_SPECULAR_VALUE);

    // roughnessT and roughnessB are clamped, and are meant to be used with punctual and directional lights.
    // perceptualRoughness is not clamped, and is meant to be used for IBL.
    // TODO: add ui inputs, +tangent map, tangentws to use anisotropy; for now bsdfData.anisotropy = 0,
    // so only bsdfData.roughnessT is used.
    ConvertAnisotropyToClampRoughness(bsdfData.perceptualRoughnessA, bsdfData.anisotropy, bsdfData.roughnessAT, bsdfData.roughnessAB);
    ConvertAnisotropyToClampRoughness(bsdfData.perceptualRoughnessB, bsdfData.anisotropy, bsdfData.roughnessBT, bsdfData.roughnessBB);

    ApplyDebugToBSDFData(bsdfData);
    return bsdfData;
}

//-----------------------------------------------------------------------------
// Debug method (use to display values)
//-----------------------------------------------------------------------------

void GetSurfaceDataDebug(uint paramId, SurfaceData surfaceData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedSurfaceDataDebug(paramId, surfaceData, result, needLinearToSRGB);

    // Overide debug value output to be more readable
    switch (paramId)
    {
        case DEBUGVIEW_STACKLIT_SURFACEDATA_NORMAL_VIEW_SPACE:
            // Convert to view space
            result = TransformWorldToViewDir(surfaceData.normalWS) * 0.5 + 0.5;
            break;
    }
}

void GetBSDFDataDebug(uint paramId, BSDFData bsdfData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedBSDFDataDebug(paramId, bsdfData, result, needLinearToSRGB);

    // Overide debug value output to be more readable
    switch (paramId)
    {
        case DEBUGVIEW_STACKLIT_BSDFDATA_NORMAL_VIEW_SPACE:
            // Convert to view space
            result = TransformWorldToViewDir(bsdfData.normalWS) * 0.5 + 0.5;
            break;
    }
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
    float NdotV;                     // Could be negative due to normal mapping, use ClampNdotV()

    // IBL: we calculate and prefetch the pre-integrated split sum data for
    // both lobes
    float3 iblR[2];                  // Dominant specular direction, used for IBL in EvaluateBSDF_Env()
    float  iblPerceptualRoughness[2];

    float3 specularFGD[2];           // Store preconvoled BRDF for both specular and diffuse
    float  diffuseFGD[2];

    // GGX
    float partLambdaV[2]; // One for each lobe
    float energyCompensation;
};

PreLightData GetPreLightData(float3 V, PositionInputs posInput, inout BSDFData bsdfData)
{
    PreLightData preLightData;
    ZERO_INITIALIZE(PreLightData, preLightData);

    float3 N = bsdfData.normalWS;
    preLightData.NdotV = dot(N, V);

    float NdotV = ClampNdotV(preLightData.NdotV);

    float3 iblN[2], iblR[2]; 
    // We will need two hacked N for the stretch anisotropic hack later.
    // (Could use a UNITY_UNROLL loop, but not much code to dupe)

    preLightData.partLambdaV[0] = GetSmithJointGGXPartLambdaV(NdotV, bsdfData.roughnessAT);
    preLightData.partLambdaV[1] = GetSmithJointGGXPartLambdaV(NdotV, bsdfData.roughnessBT);
    iblN[0] = iblN[1] = N;

    // IBL
    // Handle IBL pre calculated data + GGX multiscattering energy loss compensation term

    float specularReflectivity[2];

    GetPreIntegratedFGDGGXAndDisneyDiffuse(NdotV, preLightData.iblPerceptualRoughness[0], bsdfData.fresnel0, preLightData.specularFGD[0], preLightData.diffuseFGD[0], specularReflectivity[0]);
    GetPreIntegratedFGDGGXAndDisneyDiffuse(NdotV, preLightData.iblPerceptualRoughness[1], bsdfData.fresnel0, preLightData.specularFGD[1], preLightData.diffuseFGD[1], specularReflectivity[1]);

#ifdef LIT_DIFFUSE_LAMBERT_BRDF
    preLightData.diffuseFGD[0] = preLightData.diffuseFGD[1] = 1.0;
#endif

    iblR[0] = reflect(-V, iblN[0]);
    iblR[1] = reflect(-V, iblN[1]);
    // This is a ad-hoc tweak to better match reference of anisotropic GGX.
    // TODO: We need a better hack.
    float fact = saturate(1.2 - abs(bsdfData.anisotropy));
    preLightData.iblPerceptualRoughness[0] *= fact;
    preLightData.iblPerceptualRoughness[1] *= fact;
    // Corretion of reflected direction for better handling of rough material
    preLightData.iblR[0] = GetSpecularDominantDir(N, iblR[0], preLightData.iblPerceptualRoughness[0], NdotV);
    preLightData.iblR[1] = GetSpecularDominantDir(N, iblR[1], preLightData.iblPerceptualRoughness[1], NdotV);

#ifdef LIT_USE_GGX_ENERGY_COMPENSATION
    // Ref: Practical multiple scattering compensation for microfacet models.
    // We only apply the formulation for metals.
    // For dielectrics, the change of reflectance is negligible.
    // We deem the intensity difference of a couple of percent for high values of roughness
    // to not be worth the cost of another precomputed table.
    // Note: this formulation bakes the BSDF non-symmetric!

    // Note: that this also assumes all specular comes from GGX BSDFs.
    // (That's the FGD we use above to get integral[BSDF/F (N.w) dw] )
    // In our case, since this compensation term is already an average 
    // applied to a sum (akin to a "split sum" approximation) we will 
    // just lerp our two "specularReflectivities"
    preLightData.energyCompensation = 1.0 / lerp(specularReflectivity[0], specularReflectivity[1], bsdfData.lobeMix) - 1.0;
#else
    preLightData.energyCompensation = 0.0;
#endif // LIT_USE_GGX_ENERGY_COMPENSATION

    return preLightData;
}

//-----------------------------------------------------------------------------
// bake lighting function
//-----------------------------------------------------------------------------

//
// GetBakedDiffuseLighting will be called from ShaderPassForward.hlsl.
//
// GetBakedDiffuseLighting function compute the bake lighting + emissive color to be store in emissive buffer (Deferred case)
// In forward it must be add to the final contribution.
// This function require the 3 structure surfaceData, builtinData, bsdfData because it may require both the engine side data, and data that will not be store inside the gbuffer.
float3 GetBakedDiffuseLighting(SurfaceData surfaceData, BuiltinData builtinData, BSDFData bsdfData, PreLightData preLightData)
{
    //NEWLITTODO

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        // The lighting in SH or lightmap is assume to contain bounced light only (i.e no direct lighting), and is divide by PI (i.e Lambert is apply), so multiply by PI here to get back the illuminance
        return builtinData.bakeDiffuseLighting * PI;
    }
#endif
    //NEWLITTODO
    // Premultiply bake diffuse lighting information with DisneyDiffuse pre-integration
    //return builtinData.bakeDiffuseLighting * preLightData.diffuseFGD * surfaceData.ambientOcclusion * bsdfData.diffuseColor + builtinData.emissiveColor;
    return builtinData.bakeDiffuseLighting * bsdfData.diffuseColor; //...todo, just to return something for now, .bakeDiffuseLighting is bogus for now anyway.
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

    //NEWLITTODO
    //float roughness = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);
    //lightTransportData.diffuseColor = bsdfData.diffuseColor + bsdfData.fresnel0 * roughness * 0.5 * surfaceData.metallic;
    lightTransportData.diffuseColor = bsdfData.diffuseColor;
    lightTransportData.emissiveColor = builtinData.emissiveColor;

    return lightTransportData;
}

//-----------------------------------------------------------------------------
// LightLoop related function (Only include if required)
// HAS_LIGHTLOOP is define in Lighting.hlsl
//-----------------------------------------------------------------------------

#ifdef HAS_LIGHTLOOP

#ifndef _SURFACE_TYPE_TRANSPARENT
// For /Lighting/LightEvaluation.hlsl:
#define USE_DEFERRED_DIRECTIONAL_SHADOWS // Deferred shadows are always enabled for opaque objects
#endif

#include "../../Lighting/LightEvaluation.hlsl"
#include "../../Lighting/Reflection/VolumeProjection.hlsl"

//-----------------------------------------------------------------------------
// Lighting structure for light accumulation
//-----------------------------------------------------------------------------

// These structure allow to accumulate lighting accross the Lit material
// AggregateLighting is init to zero and transfer to EvaluateBSDF, but the LightLoop can't access its content.
//
// In fact, all structures here are opaque but used by LightLoop.hlsl.
// The Accumulate* functions are also used by LightLoop to accumulate the contributions of lights.
//
struct DirectLighting
{
    float3 diffuse;
    float3 specular;
};

struct IndirectLighting
{
    float3 specularReflected;
    float3 specularTransmitted;
};

struct AggregateLighting
{
    DirectLighting   direct;
    IndirectLighting indirect;
};

void AccumulateDirectLighting(DirectLighting src, inout AggregateLighting dst)
{
    dst.direct.diffuse += src.diffuse;
    dst.direct.specular += src.specular;
}

void AccumulateIndirectLighting(IndirectLighting src, inout AggregateLighting dst)
{
    dst.indirect.specularReflected += src.specularReflected;
    dst.indirect.specularTransmitted += src.specularTransmitted;
}

//-----------------------------------------------------------------------------
// BSDF share between directional light, punctual light and area light (reference)
//-----------------------------------------------------------------------------


// This function apply BSDF. Assumes that NdotL is positive.
void BSDF(  float3 V, float3 L, float NdotL, float3 positionWS, PreLightData preLightData, BSDFData bsdfData,
            out float3 diffuseLighting,
            out float3 specularLighting)
{
    float3 N = bsdfData.normalWS;

    // Optimized math. Ref: PBR Diffuse Lighting for GGX + Smith Microsurfaces (slide 114).
    float LdotV    = dot(L, V);
    float invLenLV = rsqrt(max(2.0 * LdotV + 2.0, FLT_EPS));            // invLenLV = rcp(length(L + V)), clamp to avoid rsqrt(0) = NaN
    float NdotH    = saturate((NdotL + preLightData.NdotV) * invLenLV); // Do not clamp NdotV here
    float LdotH    = saturate(invLenLV * LdotV + invLenLV);
    float NdotV    = ClampNdotV(preLightData.NdotV);

    // TODO: Proper Fresnel
    float3 F = F_Schlick(bsdfData.fresnel0, LdotH);

    // TODO: with iridescence, will be per light sample.

    float DV[2];

    DV[0] = DV_SmithJointGGX(NdotH, NdotL, NdotV, bsdfData.roughnessAT, preLightData.partLambdaV[0]);
    DV[1] = DV_SmithJointGGX(NdotH, NdotL, NdotV, bsdfData.roughnessBT, preLightData.partLambdaV[1]);

    specularLighting = F * lerp(DV[0], DV[1], bsdfData.lobeMix);

    // TODO: config option + diffuse GGX
    float  diffuseTerm = Lambert();

    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
    diffuseLighting = diffuseTerm;

    // TODO: coat
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Directional
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Directional(LightLoopContext lightLoopContext,
                                        float3 V, PositionInputs posInput, PreLightData preLightData,
                                        DirectionalLightData lightData, BSDFData bsdfData,
                                        BakeLightingData bakeLightingData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 N     = bsdfData.normalWS;
    float3 L     = -lightData.forward; // Lights point backward in Unity
    //float  NdotV = ClampNdotV(preLightData.NdotV);
    float  NdotL = dot(N, L);
    //float  LdotV = dot(L, V);

    // color and attenuation are outputted  by EvaluateLight: 
    float3 color;
    float attenuation;
    EvaluateLight_Directional(lightLoopContext, posInput, lightData, bakeLightingData, N, L, color, attenuation);

    float intensity = max(0, attenuation * NdotL); // Warning: attenuation can be greater than 1 due to the inverse square attenuation (when position is close to light)

    // Note: We use NdotL here to early out, but in case of clear coat this is not correct. But we are ok with this
    UNITY_BRANCH if (intensity > 0.0)
    {
        BSDF(V, L, NdotL, posInput.positionWS, preLightData, bsdfData, lighting.diffuse, lighting.specular);

        lighting.diffuse  *= intensity * lightData.diffuseScale;
        lighting.specular *= intensity * lightData.specularScale;
    }

    // NEWLITTODO: Mixed thickness, transmission

    // Save ALU by applying light and cookie colors only once.
    lighting.diffuse  *= color;
    lighting.specular *= color;

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        // Only lighting, not BSDF
        lighting.diffuse = color * intensity * lightData.diffuseScale;
    }
#endif

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Punctual (supports spot, point and projector lights)
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Punctual(LightLoopContext lightLoopContext,
                                     float3 V, PositionInputs posInput,
                                     PreLightData preLightData, LightData lightData, BSDFData bsdfData, BakeLightingData bakeLightingData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 lightToSample = posInput.positionWS - lightData.positionWS;
    int    lightType     = lightData.lightType;

    float3 L;
    float4 distances; // {d, d^2, 1/d, d_proj}
    distances.w = dot(lightToSample, lightData.forward);

    if (lightType == GPULIGHTTYPE_PROJECTOR_BOX)
    {
        L = -lightData.forward;
        distances.xyz = 1; // No distance or angle attenuation
    }
    else
    {
        float3 unL     = -lightToSample;
        float  distSq  = dot(unL, unL);
        float  distRcp = rsqrt(distSq);
        float  dist    = distSq * distRcp;

        L = unL * distRcp;
        distances.xyz = float3(dist, distSq, distRcp);
    }

    float3 N     = bsdfData.normalWS;
    float  NdotV = ClampNdotV(preLightData.NdotV);
    float  NdotL = dot(N, L);
    float  LdotV = dot(L, V);

    // NEWLITTODO: mixedThickness, transmission

    float3 color;
    float attenuation;
    EvaluateLight_Punctual(lightLoopContext, posInput, lightData, bakeLightingData, N, L,
                           lightToSample, distances, color, attenuation);


    float intensity = max(0, attenuation * NdotL); // Warning: attenuation can be greater than 1 due to the inverse square attenuation (when position is close to light)

    // Note: We use NdotL here to early out, but in case of clear coat this is not correct. But we are ok with this
    UNITY_BRANCH if (intensity > 0.0)
    {
        // Simulate a sphere light with this hack
        // Note that it is not correct with our pre-computation of PartLambdaV (mean if we disable the optimization we will not have the
        // same result) but we don't care as it is a hack anyway

        //NEWLITTODO: Do we want this hack in stacklit ? Yes we have area lights, but cheap and not much maintenance to leave it here.
        // For now no roughness anyways.

        //bsdfData.coatRoughness = max(bsdfData.coatRoughness, lightData.minRoughness);
        //bsdfData.roughnessT = max(bsdfData.roughnessT, lightData.minRoughness);
        //bsdfData.roughnessB = max(bsdfData.roughnessB, lightData.minRoughness);

        BSDF(V, L, NdotL, posInput.positionWS, preLightData, bsdfData, lighting.diffuse, lighting.specular);

        lighting.diffuse  *= intensity * lightData.diffuseScale;
        lighting.specular *= intensity * lightData.specularScale;
    }

    //NEWLITTODO : transmission


    // Save ALU by applying light and cookie colors only once.
    lighting.diffuse  *= color;
    lighting.specular *= color;

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        // Only lighting, not BSDF
        lighting.diffuse = color * intensity * lightData.diffuseScale;
    }
#endif

    return lighting;
}

// NEWLITTODO: For a refence rendering option for area light, like LIT_DISPLAY_REFERENCE_AREA option in eg EvaluateBSDF_<area light type> : 
//#include "LitReference.hlsl"

//-----------------------------------------------------------------------------
// EvaluateBSDF_Line - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Line(   LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, LightData lightData, BSDFData bsdfData, BakeLightingData bakeLightingData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    //NEWLITTODO

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Area - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

// #define ELLIPSOIDAL_ATTENUATION

DirectLighting EvaluateBSDF_Rect(   LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, LightData lightData, BSDFData bsdfData, BakeLightingData bakeLightingData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    //NEWLITTODO

    return lighting;
}

DirectLighting EvaluateBSDF_Area(LightLoopContext lightLoopContext,
    float3 V, PositionInputs posInput,
    PreLightData preLightData, LightData lightData,
    BSDFData bsdfData, BakeLightingData bakeLightingData)
{
    if (lightData.lightType == GPULIGHTTYPE_LINE)
    {
        return EvaluateBSDF_Line(lightLoopContext, V, posInput, preLightData, lightData, bsdfData, bakeLightingData);
    }
    else
    {
        return EvaluateBSDF_Rect(lightLoopContext, V, posInput, preLightData, lightData, bsdfData, bakeLightingData);
    }
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_SSLighting for screen space lighting
// ----------------------------------------------------------------------------

IndirectLighting EvaluateBSDF_SSLighting(LightLoopContext lightLoopContext,
                                            float3 V, PositionInputs posInput,
                                            PreLightData preLightData, BSDFData bsdfData,
                                            EnvLightData envLightData,
                                            int GPUImageBasedLightingType,
                                            inout float hierarchyWeight)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    //NEWLITTODO

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

    // TODO: refraction

#if !HAS_REFRACTION
    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFRACTION)
        return lighting;
#endif

    float3 envLighting;
    float3 positionWS = posInput.positionWS;
    float weight;
    float tempWeight[2];

#ifdef LIT_DISPLAY_REFERENCE_IBL

    envLighting = IntegrateSpecularGGXIBLRef(lightLoopContext, V, preLightData, lightData, bsdfData);

    // TODO: Do refraction reference (is it even possible ?)
    // TODO: handle clear coat

    // TODO: Handle two lobes in reference


//    #ifdef LIT_DIFFUSE_LAMBERT_BRDF
//    envLighting += IntegrateLambertIBLRef(lightData, V, bsdfData);
//    #else
//    envLighting += IntegrateDisneyDiffuseIBLRef(lightLoopContext, V, preLightData, lightData, bsdfData);
//    #endif

#else

    float3 R[2];
    R[0] = preLightData.iblR[0];
    R[1] = preLightData.iblR[1];

    // TODO: Refraction

    // We will sample 2 times (one for each lobe) the environment.
    // Steps are:

    // -Calculate influence weights from intersection with the proxies.
    // Since the weights are influence blending weights, we can correctly 
    // use our lobe weight and mix them.
    // -Fudge the sampling direction  to dampen boundary artefacts.
    // -Do early discard for planar reflections.
    // -Fetch 2 samples of preintegrated environment lighting 
    // (see preLD, first part of the split-sum approx.)
    // -Use the 2 BSDF preintegration terms we pre-fetched in preLightData 
    // (second part of the split-sum approx.,
    //  and common to all Env. Lights. using the same BSDF and
    //  we only have GGX thus only one FGD map for now)
    // -Multiply the two split sum terms together for each lobe 
    // and linearly combine the results.

    // Note: using influenceShapeType and projectionShapeType instead of (lightData|proxyData).shapeType allow to make compiler optimization in case the type is know (like for sky)
    tempWeight[0] = tempWeight[1] = 1.0;
    EvaluateLight_EnvIntersection(positionWS, bsdfData.normalWS, lightData, influenceShapeType, R[0], tempWeight[0]);
    EvaluateLight_EnvIntersection(positionWS, bsdfData.normalWS, lightData, influenceShapeType, R[1], tempWeight[1]);
    weight = lerp(tempWeight[0], tempWeight[1], bsdfData.lobeMix);

    // TODO: reflections + coating 

    // When we are rough, we tend to see outward shifting of the reflection when at the boundary of the projection volume
    // Also it appear like more sharp. To avoid these artifact and at the same time get better match to reference we lerp to original unmodified reflection.
    // Formula is empirical.
    float roughness = PerceptualRoughnessToRoughness(preLightData.iblPerceptualRoughness[0]);
    R[0] = lerp(R[0], preLightData.iblR[0], saturate(smoothstep(0, 1, roughness * roughness)));
    roughness = PerceptualRoughnessToRoughness(preLightData.iblPerceptualRoughness[1]);
    R[1] = lerp(R[1], preLightData.iblR[1], saturate(smoothstep(0, 1, roughness * roughness)));

    float3 sampleDirectionDiscardWS = lightData.sampleDirectionDiscardWS;
    // Use by planar reflection to early reject opposite plan reflection, neutral for reflection probe
    if ( (dot(sampleDirectionDiscardWS, R[0]) < 0) && (dot(sampleDirectionDiscardWS, R[1]) < 0))
        return lighting;

    float3 F[2];
    F[0] = preLightData.specularFGD[0];
    F[1] = preLightData.specularFGD[1];

    float4 preLD[2]; 

    float iblMipLevel = PerceptualRoughnessToMipmapLevel(preLightData.iblPerceptualRoughness[0]);
    preLD[0] = SampleEnv(lightLoopContext, lightData.envIndex, R[0], iblMipLevel);

    iblMipLevel = PerceptualRoughnessToMipmapLevel(preLightData.iblPerceptualRoughness[1]);
    preLD[1] = SampleEnv(lightLoopContext, lightData.envIndex, R[1], iblMipLevel);

    // Used by planar reflection to discard pixel:
    weight *= lerp(preLD[0].a, preLD[1].a, bsdfData.lobeMix);

    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION)
    {
        envLighting = lerp(F[0] * preLD[0].rgb, F[1] * preLD[1].rgb, bsdfData.lobeMix);
        // TODO: Clear Coat component if needed
    }
    else
    {
        // TODO: Refractions

        // No clear coat support with refraction

        // specular transmisted lighting is the remaining of the reflection (let's use this approx)
        // With refraction, we don't care about the clear coat value, only about the Fresnel, thus why we use 'envLighting ='
        //envLighting = (1.0 - F) * preLD.rgb * preLightData.transparentTransmittance;
    }

#endif // LIT_DISPLAY_REFERENCE_IBL

    UpdateLightingHierarchyWeights(hierarchyWeight, weight);
    envLighting *= weight * lightData.multiplier;

    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION)
        lighting.specularReflected = envLighting;
    //TODO refraction:
    //else
    //    lighting.specularTransmitted = envLighting * preLightData.transparentTransmittance;

    return lighting;
}

//-----------------------------------------------------------------------------
// PostEvaluateBSDF
// ----------------------------------------------------------------------------

void PostEvaluateBSDF(  LightLoopContext lightLoopContext,
                        float3 V, PositionInputs posInput,
                        PreLightData preLightData, BSDFData bsdfData, BakeLightingData bakeLightingData, AggregateLighting lighting,
                        out float3 diffuseLighting, out float3 specularLighting)
{
    // TODO: AO, SO, SSS, Refraction

    // diffuse lighting has already had the albedo applied in GetBakedDiffuseLighting().
    diffuseLighting = bsdfData.diffuseColor * lighting.direct.diffuse + bakeLightingData.bakeDiffuseLighting;

    specularLighting = lighting.direct.specular + lighting.indirect.specularReflected;
    // Apply the fudge factor (boost) to compensate for multiple scattering not accounted for in BSDF.
    // This assumes all spec comes from a GGX BSDF.
    // Note: The multiply with fresnel0 is to apply it only for metallic 
    specularLighting *= 1.0 + bsdfData.fresnel0 * preLightData.energyCompensation;

#ifdef DEBUG_DISPLAY

    if (_DebugLightingMode != 0)
    {
        specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting

        switch (_DebugLightingMode)
        {
        case DEBUGLIGHTINGMODE_LUX_METER:
            diffuseLighting = lighting.direct.diffuse + bakeLightingData.bakeDiffuseLighting;
            break;

        case DEBUGLIGHTINGMODE_INDIRECT_DIFFUSE_OCCLUSION:
            //diffuseLighting = aoFactor.indirectAmbientOcclusion;
            break;

        case DEBUGLIGHTINGMODE_INDIRECT_SPECULAR_OCCLUSION:
            //diffuseLighting = aoFactor.indirectSpecularOcclusion;
            break;

        case DEBUGLIGHTINGMODE_SCREEN_SPACE_TRACING_REFRACTION:
            //if (_DebugLightingSubMode != DEBUGSCREENSPACETRACING_COLOR)
            //    diffuseLighting = lighting.indirect.specularTransmitted;
            break;
        }
    }
    else if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
    {
        diffuseLighting = bsdfData.diffuseColor;
        specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
    }

#endif
}

#endif // #ifdef HAS_LIGHTLOOP
