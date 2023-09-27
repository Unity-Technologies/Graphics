//-----------------------------------------------------------------------------
// Includes
//-----------------------------------------------------------------------------
// SurfaceData is define in Lit.cs which generate Lit.cs.hlsl
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.cs.hlsl"

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/PreIntegratedFGD/PreIntegratedFGD.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LTCAreaLight/LTCAreaLight.hlsl"

// Those define allow to include desired SSS/Transmission functions
#define MATERIAL_INCLUDE_SUBSURFACESCATTERING
#define MATERIAL_INCLUDE_TRANSMISSION
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SubsurfaceScattering/SubsurfaceScattering.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"

//-----------------------------------------------------------------------------
// Helper functions/variable specific to this material
//-----------------------------------------------------------------------------

float4 GetDiffuseOrDefaultColor(BSDFData bsdfData, float replace)
{
    // Use frensel0 as mettalic weight. all value below 0.2 (ior of diamond) are dielectric
    // all value above 0.45 are metal, in between we lerp.
    float weight = saturate((Max3(bsdfData.fresnel0.r, bsdfData.fresnel0.g, bsdfData.fresnel0.b) - 0.2) / (0.45 - 0.2));

    return float4(lerp(bsdfData.diffuseColor, bsdfData.fresnel0, weight * replace), weight);
}

float3 GetNormalForShadowBias(BSDFData bsdfData)
{
    // In forward we can used geometric normal for shadow bias which improve quality
#if (SHADERPASS == SHADERPASS_FORWARD)
    return bsdfData.geomNormalWS;
#else
    return bsdfData.normalWS;
#endif
}

float GetAmbientOcclusionForMicroShadowing(BSDFData bsdfData)
{
    return 1.0; // Don't do microshadowing for simpleLit
}

// This function is similar to ApplyDebugToSurfaceData but for BSDFData
void ApplyDebugToBSDFData(inout BSDFData bsdfData)
{
#ifdef DEBUG_DISPLAY
    // Override value if requested by user
    // this can be use also in case of debug lighting mode like specular only
    bool overrideSpecularColor = _DebugLightingSpecularColor.x != 0.0;

    if (overrideSpecularColor)
    {
        float3 overrideSpecularColor = _DebugLightingSpecularColor.yzw;
        bsdfData.fresnel0 = overrideSpecularColor;
    }
#endif
}

void GetSurfaceDataDebug(uint paramId, SurfaceData surfaceData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedSurfaceDataDebug(paramId, surfaceData, result, needLinearToSRGB);
}

void GetBSDFDataDebug(uint paramId, BSDFData bsdfData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedBSDFDataDebug(paramId, bsdfData, result, needLinearToSRGB);
}

//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(uint2 positionSS, SurfaceData surfaceData)
{
    BSDFData bsdfData;
    ZERO_INITIALIZE(BSDFData, bsdfData);

    // IMPORTANT: In case of foward or gbuffer pass all enable flags are statically know at compile time, so the compiler can do compile time optimization
    bsdfData.materialFeatures    = surfaceData.materialFeatures;

    // Standard material
    bsdfData.ambientOcclusion    = surfaceData.ambientOcclusion;
    bsdfData.specularOcclusion   = surfaceData.specularOcclusion;
    bsdfData.normalWS            = surfaceData.normalWS;
    bsdfData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);

    // There is no metallic with SSS and specular color mode
    float metallic = HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR | MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_LIT_TRANSMISSION) ? 0.0 : surfaceData.metallic;

    bsdfData.diffuseColor = ComputeDiffuseColor(surfaceData.baseColor, metallic);
    bsdfData.fresnel0     = HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR) ? surfaceData.specularColor : ComputeFresnel0(surfaceData.baseColor, surfaceData.metallic, DEFAULT_SPECULAR_VALUE);

    // Note: we have ZERO_INITIALIZE the struct so bsdfData.anisotropy == 0.0
    // Note: DIFFUSION_PROFILE_NEUTRAL_ID is 0

    // In forward everything is statically know and we could theorically cumulate all the material features. So the code reflect it.
    // However in practice we keep parity between deferred and forward, so we should constrain the various features.
    // The UI is in charge of setuping the constrain, not the code. So if users is forward only and want unleash power, it is easy to unleash by some UI change

    bsdfData.diffusionProfileIndex = FindDiffusionProfileIndex(surfaceData.diffusionProfileHash);

    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING))
    {
        // Assign profile id and overwrite fresnel0
        FillMaterialSSS(bsdfData.diffusionProfileIndex, surfaceData.subsurfaceMask, bsdfData);
    }

    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_TRANSMISSION))
    {
        // Assign profile id and overwrite fresnel0
        FillMaterialTransmission(bsdfData.diffusionProfileIndex, surfaceData.thickness, bsdfData);
    }

    // roughnessT and roughnessB are clamped, and are meant to be used with punctual and directional lights.
    // perceptualRoughness is not clamped, and is meant to be used for IBL.
    // perceptualRoughness can be modify by FillMaterialClearCoatData, so ConvertAnisotropyToClampRoughness must be call after
    ConvertAnisotropyToRoughness(bsdfData.perceptualRoughness, bsdfData.anisotropy, bsdfData.roughnessT, bsdfData.roughnessB);

#if HAS_REFRACTION
    // Note: Reuse thickness of transmission's property set
    FillMaterialTransparencyData(surfaceData.baseColor, surfaceData.metallic, surfaceData.ior, surfaceData.transmittanceColor, surfaceData.atDistance,
        surfaceData.thickness, surfaceData.transmittanceMask, bsdfData);
#endif

    ApplyDebugToBSDFData(bsdfData);

    return bsdfData;
}

// Precomputed lighting data to send to the various lighting functions
struct PreLightData
{
    float NdotV;                     // Could be negative due to normal mapping, use ClampNdotV()

    // GGX
    float partLambdaV;
    float energyCompensation;

    // IBL
    float3 iblR;                     // Reflected specular direction, used for IBL in EvaluateBSDF_Env()
    float  iblPerceptualRoughness;

    float3 specularFGD;              // Store preintegrated BSDF for both specular and diffuse
    float  diffuseFGD;

    // Area lights (17 VGPRs)
    // TODO: 'orthoBasisViewNormal' is just a rotation around the normal and should thus be just 1x VGPR.
    float3x3 orthoBasisViewNormal;   // Right-handed view-dependent orthogonal basis around the normal (6x VGPRs)
    float3x3 ltcTransformDiffuse;    // Inverse transformation for Lambertian or Disney Diffuse        (4x VGPRs)
    float3x3 ltcTransformSpecular;   // Inverse transformation for GGX                                 (4x VGPRs)

    // Clear coat
    float    coatPartLambdaV;
    float3   coatIblR;
    float    coatIblF;               // Fresnel term for view vector
    float3x3 ltcTransformCoat;       // Inverse transformation for GGX                                 (4x VGPRs)

#if HAS_REFRACTION
    // Refraction
    float3 transparentRefractV;      // refracted view vector after exiting the shape
    float3 transparentPositionWS;    // start of the refracted ray after exiting the shape
    float3 transparentTransmittance; // transmittance due to absorption
    float transparentSSMipLevel;     // mip level of the screen space gaussian pyramid for rough refraction
#endif
};

//
// ClampRoughness helper specific to this material
//
void ClampRoughness(inout PreLightData preLightData, inout BSDFData bsdfData, float minRoughness)
{
    // No anistropy in simple lit
    bsdfData.roughnessT    = max(minRoughness, bsdfData.roughnessT);
}

//-----------------------------------------------------------------------------
// BSDF share between directional light, punctual light and area light (reference)
//-----------------------------------------------------------------------------

PreLightData GetPreLightData(float3 V, PositionInputs posInput, inout BSDFData bsdfData)
{
    PreLightData preLightData;
    // Don't init to zero to allow to track warning about uninitialized data

    float3 N = bsdfData.normalWS;
    preLightData.NdotV = dot(N, V);
    preLightData.iblPerceptualRoughness = bsdfData.perceptualRoughness;

    float clampedNdotV = ClampNdotV(preLightData.NdotV);

    preLightData.coatPartLambdaV = 0;
    preLightData.coatIblR = 0;
    preLightData.coatIblF = 0;

    // Handle IBL + area light + multiscattering.
    // Note: use the not modified by anisotropy iblPerceptualRoughness here.
    float specularReflectivity;
    GetPreIntegratedFGDGGXAndDisneyDiffuse(clampedNdotV, preLightData.iblPerceptualRoughness, bsdfData.fresnel0, preLightData.specularFGD, preLightData.diffuseFGD, specularReflectivity);
    preLightData.diffuseFGD = 1.0;

    preLightData.energyCompensation = 0.0;

    preLightData.partLambdaV = GetSmithJointGGXPartLambdaV(clampedNdotV, bsdfData.roughnessT);

    preLightData.iblR = reflect(-V, N);

    // Area light
    preLightData.ltcTransformDiffuse  = k_identity3x3;
    preLightData.ltcTransformSpecular = SampleLtcMatrix(bsdfData.perceptualRoughness, clampedNdotV, LTCLIGHTINGMODEL_GGX);

    // Construct a right-handed view-dependent orthogonal basis around the normal
    preLightData.orthoBasisViewNormal = GetOrthoBasisViewNormal(V, N, preLightData.NdotV);

    preLightData.ltcTransformCoat = 0.0;

#if HAS_REFRACTION
    RefractionModelResult refraction = REFRACTION_MODEL(V, posInput, bsdfData);
    preLightData.transparentRefractV = refraction.rayWS;
    preLightData.transparentPositionWS = refraction.positionWS;
    preLightData.transparentTransmittance = exp(-bsdfData.absorptionCoefficient * refraction.dist);
    // Empirical remap to try to match a bit the refraction probe blurring for the fallback
    // Use IblPerceptualRoughness so we can handle approx of clear coat.
    preLightData.transparentSSMipLevel = PositivePow(preLightData.iblPerceptualRoughness, 1.3) * uint(max(_ColorPyramidLodCount - 1, 0));
#endif

    return preLightData;
}

NormalData ConvertSurfaceDataToNormalData(SurfaceData surfaceData)
{
    NormalData normalData;
    normalData.normalWS = surfaceData.normalWS;
    normalData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);
    return normalData;
}

#ifdef HAS_LIGHTLOOP

bool IsNonZeroBSDF(float3 V, float3 L, PreLightData preLightData, BSDFData bsdfData)
{
    float NdotL = dot(bsdfData.normalWS, L);

#if HDRP_MATERIAL_TYPE_SIMPLELIT_TRANSLUCENT
    return HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_TRANSMISSION) || (NdotL > 0.0);
#else
    return  NdotL > 0.0;
#endif
}

// This function apply BSDF. Assumes that NdotL is positive.
CBSDF EvaluateBSDF(float3 V, float3 L, PreLightData preLightData, BSDFData bsdfData)
{
    CBSDF cbsdf;
    ZERO_INITIALIZE(CBSDF, cbsdf);

    float3 N = bsdfData.normalWS;

    float NdotV = preLightData.NdotV;
    float NdotL = dot(N, L);
    float clampedNdotV = ClampNdotV(NdotV);
    float clampedNdotL = saturate(NdotL);
    float flippedNdotL = ComputeWrappedDiffuseLighting(-NdotL, TRANSMISSION_WRAP_LIGHT);

#if HDRP_ENABLE_SPECULAR
    float LdotV, NdotH, LdotH, invLenLV;
    GetBSDFAngle(V, L, NdotL, NdotV, LdotV, NdotH, LdotH, invLenLV);

    float3 F = F_Schlick(bsdfData.fresnel0, LdotH);

    // We use abs(NdotL) to handle the none case of double sided
    float DV = DV_SmithJointGGX(NdotH, abs(NdotL), clampedNdotV, bsdfData.roughnessT, preLightData.partLambdaV);
    float3 specTerm = F * DV;

    // Probably worth branching here for perf reasons.
    // This branch will be optimized away if there's no transmission.
    if (NdotL > 0.0)
    {
        cbsdf.specR = specTerm * clampedNdotL;
    }
#endif // HDRP_ENABLE_SPECULAR

    float diffTerm = Lambert();
    cbsdf.diffR = diffTerm * clampedNdotL;
    cbsdf.diffT = diffTerm * flippedNdotL;

    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
    return cbsdf;
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Reflection/VolumeProjection.hlsl"

float EvaluateLight_EnvIntersection(float3 positionWS, float3 normalWS, EnvLightData light, int influenceShapeType, inout float3 R, inout float weight)
{
    // Guideline for reflection volume: In HDRenderPipeline we separate the projection volume (the proxy of the scene) from the influence volume (what pixel on the screen is affected)
    // However we add the constrain that the shape of the projection and influence volume is the same (i.e if we have a sphere shape projection volume, we have a shape influence).
    // It allow to have more coherence for the dynamic if in shader code.
    // Users can also chose to not have any projection, in this case we use the property minProjectionDistance to minimize code change. minProjectionDistance is set to huge number
    // that simulate effect of no shape projection

    float3x3 worldToIS = WorldToInfluenceSpace(light); // IS: Influence space
    float3 positionIS = WorldToInfluencePosition(light, worldToIS, positionWS);
    float3 dirIS = normalize(mul(R, worldToIS));

    float3x3 worldToPS = WorldToProxySpace(light); // PS: Proxy space
    float3 positionPS = WorldToProxyPosition(light, worldToPS, positionWS);
    float3 dirPS = mul(R, worldToPS);

    float projectionDistance = 0;

    // Process the projection
    // In Unity the cubemaps are capture with the localToWorld transform of the component.
    // This mean that location and orientation matter. So after intersection of proxy volume we need to convert back to world.
    if (influenceShapeType == ENVSHAPETYPE_SPHERE)
    {
        projectionDistance = IntersectSphereProxy(light, dirPS, positionPS);
        // We can reuse dist calculate in LS directly in WS as there is no scaling. Also the offset is already include in light.capturePositionRWS
        R = (positionWS + projectionDistance * R) - light.capturePositionRWS;

        weight = InfluenceSphereWeight(light, normalWS, positionWS, positionIS, dirIS);
    }
    else if (influenceShapeType == ENVSHAPETYPE_BOX)
    {
        projectionDistance = IntersectBoxProxy(light, dirPS, positionPS);
        // No need to normalize for fetching cubemap
        // We can reuse dist calculate in LS directly in WS as there is no scaling. Also the offset is already include in light.capturePositionRWS
        R = (positionWS + projectionDistance * R) - light.capturePositionRWS;

        weight = InfluenceBoxWeight(light, normalWS, positionWS, positionIS, dirIS);
    }

    // Smooth weighting
    weight = Smoothstep01(weight);
    weight *= light.weight;

    return projectionDistance;
}

#define OVERRIDE_EVALUATE_ENV_INTERSECTION
#define LIGHT_EVALUATION_NO_HEIGHT_FOG
// TODO: validate that the condition will work!
#if !HDRP_ENABLE_COOKIE
#define LIGHT_EVALUATION_NO_COOKIE
#endif
#define LIGHT_EVALUATION_NO_CONTACT_SHADOWS
// TODO: validate that the condition will work!
#if defined(_RECEIVE_SHADOWS_OFF)
#define LIGHT_EVALUATION_NO_SHADOWS
#endif
#define LIGHT_EVALUATION_NO_CLOUDS_SHADOWS

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightEvaluation.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialEvaluation.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/SurfaceShading.hlsl"

//-----------------------------------------------------------------------------
// EvaluateBSDF_Directional
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Directional(LightLoopContext lightLoopContext,
                                        float3 V, PositionInputs posInput, PreLightData preLightData,
                                        DirectionalLightData lightData, BSDFData bsdfData,
                                        BuiltinData builtinData)
{
    return ShadeSurface_Directional(lightLoopContext, posInput, builtinData,
                                    preLightData, lightData, bsdfData, V);
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Punctual
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Punctual(LightLoopContext lightLoopContext,
                                     float3 V, PositionInputs posInput,
                                     PreLightData preLightData, LightData lightData,
                                     BSDFData bsdfData, BuiltinData builtinData)
{
    return ShadeSurface_Punctual(lightLoopContext, posInput, builtinData,
                                 preLightData, lightData, bsdfData, V);
}

IndirectLighting EvaluateBSDF_ScreenSpaceReflection(PositionInputs posInput,
                                                    PreLightData   preLightData,
                                                    BSDFData       bsdfData,
                                                    inout float    reflectionHierarchyWeight)
{
    // We do not support screen space reflections
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);
    return lighting;
}


IndirectLighting EvaluateBSDF_ScreenspaceRefraction(LightLoopContext lightLoopContext,
                                                    float3 V, PositionInputs posInput,
                                                    PreLightData preLightData, BSDFData bsdfData,
                                                    EnvLightData envLightData,
                                                    inout float hierarchyWeight)
{
    // We do not support screen space refraction (TODO: Should we?)
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);
    return lighting;
}

DirectLighting EvaluateBSDF_Area(LightLoopContext lightLoopContext,
    float3 V, PositionInputs posInput,
    PreLightData preLightData, LightData lightData,
    BSDFData bsdfData, BuiltinData builtinData)
{
    // TODO: support area lights
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);
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
    float weight = 1.0;

    float3 R = preLightData.iblR;

    EvaluateLight_EnvIntersection(posInput.positionWS, bsdfData.normalWS, lightData, influenceShapeType, R, weight);

    // No distance based roughness for simple lit
    float4 preLD = SampleEnv(lightLoopContext, lightData.envIndex, R, PerceptualRoughnessToMipmapLevel(preLightData.iblPerceptualRoughness) * lightData.roughReflections, lightData.rangeCompressionFactorCompensation, posInput.positionNDC);
    weight *= preLD.a; // Used by planar reflection to discard pixel

    envLighting = F_Schlick(bsdfData.fresnel0, dot(bsdfData.normalWS, V)) * preLD.rgb;

    UpdateLightingHierarchyWeights(hierarchyWeight, weight);
    envLighting *= weight * lightData.multiplier;

    lighting.specularReflected = envLighting;

    return lighting;
}

void PostEvaluateBSDF(  LightLoopContext lightLoopContext,
                        float3 V, PositionInputs posInput,
                        PreLightData preLightData, BSDFData bsdfData, BuiltinData builtinData, AggregateLighting lighting,
                        out LightLoopOutput lightLoopOutput)
{
    AmbientOcclusionFactor aoFactor;
    // Use GTAOMultiBounce approximation for ambient occlusion (allow to get a tint from the baseColor)
    GetScreenSpaceAmbientOcclusion(posInput.positionSS, preLightData.NdotV, bsdfData.perceptualRoughness, bsdfData.ambientOcclusion, bsdfData.specularOcclusion, aoFactor);
    ApplyAmbientOcclusionFactor(aoFactor, builtinData, lighting);

    // Subsurface scattering mode
    float3 modifiedDiffuseColor = GetModifiedDiffuseColorForSSS(bsdfData);

    // Note: Unlike Lit material, the SimpleLit material don't have ModifyBakedDiffuseLighting() function
    // So we need to multiply by the diffuse albedo here.
    lightLoopOutput.diffuseLighting = modifiedDiffuseColor * lighting.direct.diffuse + builtinData.emissiveColor;

    #ifdef HDRP_ENABLE_ENV_LIGHT // TODO: check what this is suppose to do?
    // Note: When baking reflection probes, we approximate the diffuse with the fresnel0
    bsdfData.diffuseColor = modifiedDiffuseColor; // Note: This affect the debug mode of mipmap streaming for simple Lit in PostEvaluateBSDFDebugDisplay. But we are ok with that.
    lightLoopOutput.diffuseLighting += builtinData.bakeDiffuseLighting * GetDiffuseOrDefaultColor(bsdfData, _ReplaceDiffuseForIndirect).rgb;
    #endif

    lightLoopOutput.specularLighting = lighting.direct.specular + lighting.indirect.specularReflected;

#ifdef DEBUG_DISPLAY
    PostEvaluateBSDFDebugDisplay(aoFactor, builtinData, lighting, bsdfData.diffuseColor, lightLoopOutput);
#endif
}

#endif
