//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------
// SurfaceData is defined in Eye.cs which generates Eye.cs.hlsl
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/EyeUtils.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/Eye.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/EyeCausticLUT.hlsl"
// Those define allow to include desired SSS/Transmission functions
#define MATERIAL_INCLUDE_SUBSURFACESCATTERING
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SubsurfaceScattering/SubsurfaceScattering.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"

//-----------------------------------------------------------------------------
// Texture and constant buffer declaration
//-----------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LTCAreaLight/LTCAreaLight.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/PreIntegratedFGD/PreIntegratedFGD.hlsl"

//-----------------------------------------------------------------------------
// Helper functions/variable specific to this material
//-----------------------------------------------------------------------------

float4 GetDiffuseOrDefaultColor(BSDFData bsdfData, float replace)
{
    return float4(bsdfData.diffuseColor, 0.0);
}

float3 GetNormalForShadowBias(BSDFData bsdfData)
{
    return bsdfData.geomNormalWS;
}

float GetAmbientOcclusionForMicroShadowing(BSDFData bsdfData)
{
    return bsdfData.ambientOcclusion;
}

// This function is use to help with debugging and must be implemented by any lit material
// Implementer must take into account what are the current override component and
// adjust SurfaceData properties accordingdly
void ApplyDebugToSurfaceData(float3x3 tangentToWorld, inout SurfaceData surfaceData)
{
#ifdef DEBUG_DISPLAY
    // NOTE: THe _Debug* uniforms come from /HDRP/Debug/DebugDisplay.hlsl

    // Override value if requested by user
    // this can be use also in case of debug lighting mode like diffuse only
    bool overrideAlbedo = _DebugLightingAlbedo.x != 0.0;
    bool overrideSmoothness = _DebugLightingSmoothness.x != 0.0;
    bool overrideNormal = _DebugLightingNormal.x != 0.0;
    bool overrideAO = _DebugLightingAmbientOcclusion.x != 0.0;

    if (overrideAlbedo)
    {
        float3 overrideAlbedoValue = _DebugLightingAlbedo.yzw;
        surfaceData.baseColor = overrideAlbedoValue;
    }

    if (overrideSmoothness)
    {
        float overrideSmoothnessValue = _DebugLightingSmoothness.y;
        surfaceData.perceptualSmoothness = overrideSmoothnessValue;
    }

    if (overrideNormal)
    {
        surfaceData.normalWS = tangentToWorld[2];
    }

    if (overrideAO)
    {
        float overrideAOValue = _DebugLightingAmbientOcclusion.y;
        surfaceData.ambientOcclusion = overrideAOValue;
    }

    if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_VALIDATE_DIFFUSE_COLOR)
    {
        surfaceData.baseColor = pbrDiffuseColorValidate(surfaceData.baseColor, float3(0.04, 0.04, 0.04), false, false).xyz;
    }
    else if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_VALIDATE_SPECULAR_COLOR)
    {
        surfaceData.baseColor = pbrSpecularColorValidate(surfaceData.baseColor, float3(0.04, 0.04, 0.04), false, false).xyz;
    }
#endif
}

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

NormalData ConvertSurfaceDataToNormalData(SurfaceData surfaceData)
{
    NormalData normalData;
    normalData.normalWS = surfaceData.normalWS;
    normalData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);
    return normalData;
}

SSSData ConvertSurfaceDataToSSSData(SurfaceData surfaceData)
{
    SSSData sssData;

    sssData.diffuseColor = surfaceData.baseColor;
    sssData.subsurfaceMask = surfaceData.subsurfaceMask;
    sssData.diffusionProfileIndex = FindDiffusionProfileIndex(surfaceData.diffusionProfileHash);

    return sssData;
}

//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(uint2 positionSS, SurfaceData surfaceData)
{
    BSDFData bsdfData;
    ZERO_INITIALIZE(BSDFData, bsdfData);

    bsdfData.materialFeatures = surfaceData.materialFeatures;

    bsdfData.diffuseColor = surfaceData.baseColor;
    bsdfData.specularOcclusion = surfaceData.specularOcclusion;
    bsdfData.normalWS = surfaceData.normalWS;
    bsdfData.diffuseNormalWS = surfaceData.irisNormalWS;
    bsdfData.geomNormalWS = surfaceData.geomNormalWS;

    bsdfData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);

    bsdfData.fresnel0 = IorToFresnel0(surfaceData.IOR).xxx;
    bsdfData.IOR = surfaceData.IOR;
    bsdfData.ambientOcclusion = surfaceData.ambientOcclusion;

    bsdfData.mask = surfaceData.mask;

    // Note: we have ZERO_INITIALIZE the struct so bsdfData.anisotropy == 0.0
    // Note: DIFFUSION_PROFILE_NEUTRAL_ID is 0

    // In forward everything is statically know and we could theorically cumulate all the material features. So the code reflect it.
    // However in practice we keep parity between deferred and forward, so we should constrain the various features.
    // The UI is in charge of setuping the constrain, not the code. So if users is forward only and want unleash power, it is easy to unleash by some UI change

    bsdfData.diffusionProfileIndex = FindDiffusionProfileIndex(surfaceData.diffusionProfileHash);

    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_EYE_SUBSURFACE_SCATTERING))
    {
        // Assign profile id and overwrite fresnel0
        FillMaterialSSS(bsdfData.diffusionProfileIndex, surfaceData.subsurfaceMask, bsdfData);
    }

    bsdfData.roughness = ClampRoughnessForAnalyticalLights(PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness));

    bsdfData.irisPlaneOffset = surfaceData.irisPlaneOffset;
    bsdfData.irisRadius = surfaceData.irisRadius;
    bsdfData.causticIntensity = surfaceData.causticIntensity;
    bsdfData.causticBlend = surfaceData.causticBlend;

    ApplyDebugToBSDFData(bsdfData);



    return bsdfData;
}

//-----------------------------------------------------------------------------
// Debug method (use to display values)
//-----------------------------------------------------------------------------

// This function call the generated debug function and allow to override the debug output if needed
void GetSurfaceDataDebug(uint paramId, SurfaceData surfaceData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedSurfaceDataDebug(paramId, surfaceData, result, needLinearToSRGB);

    // Overide debug value output to be more readable
    switch (paramId)
    {
    case DEBUGVIEW_EYE_SURFACEDATA_MATERIAL_FEATURES:
        result = (surfaceData.materialFeatures.xxx) / 255.0; // Aloow to read with color picker debug mode
        break;
    case DEBUGVIEW_EYE_SURFACEDATA_NORMAL_VIEW_SPACE:
        // Convert to view space
        {
            float3 vsNormal = TransformWorldToViewDir(surfaceData.normalWS);
            result = IsNormalized(vsNormal) ?  vsNormal * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        }
    case DEBUGVIEW_EYE_SURFACEDATA_IRIS_NORMAL_VIEW_SPACE:
        {
            float3 vsIrisNormal = TransformWorldToViewDir(surfaceData.irisNormalWS);
            result = IsNormalized(vsIrisNormal) ?  vsIrisNormal * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        }
    case DEBUGVIEW_EYE_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
        {
            float3 vsGeomNormal = TransformWorldToViewDir(surfaceData.geomNormalWS);
            result = IsNormalized(vsGeomNormal) ?  vsGeomNormal * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        }
    case DEBUGVIEW_EYE_SURFACEDATA_IOR:
        result = saturate((surfaceData.IOR - 1.0) / 1.5).xxx;
        break;
    }
}

// This function call the generated debug function and allow to override the debug output if needed
void GetBSDFDataDebug(uint paramId, BSDFData bsdfData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedBSDFDataDebug(paramId, bsdfData, result, needLinearToSRGB);

    // Overide debug value output to be more readable
    switch (paramId)
    {
    case DEBUGVIEW_EYE_BSDFDATA_MATERIAL_FEATURES:
        result = (bsdfData.materialFeatures.xxx) / 255.0; // Aloow to read with color picker debug mode
        break;
    case DEBUGVIEW_EYE_BSDFDATA_NORMAL_VIEW_SPACE:
        // Convert to view space
        {
            float3 vsNormal = TransformWorldToViewDir(bsdfData.normalWS);
            result = IsNormalized(vsNormal) ?  vsNormal * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        }
    case DEBUGVIEW_EYE_BSDFDATA_DIFFUSE_NORMAL_VIEW_SPACE:
        {
            float3 vsDiffuseNormal = TransformWorldToViewDir(bsdfData.diffuseNormalWS);
            result = IsNormalized(vsDiffuseNormal) ?  vsDiffuseNormal * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        }
    case DEBUGVIEW_EYE_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
        {
            float3 vsGeomNormal = TransformWorldToViewDir(bsdfData.geomNormalWS);
            result = IsNormalized(vsGeomNormal) ?  vsGeomNormal * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        }
    case DEBUGVIEW_EYE_BSDFDATA_IOR:
        result = saturate((bsdfData.IOR - 1.0) / 1.5).xxx;
        break;
    }
}

void GetPBRValidatorDebug(SurfaceData surfaceData, inout float3 result)
{
    result = surfaceData.baseColor;
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
    float NdotV;        // Could be negative due to normal mapping, use ClampNdotV()
    float partLambdaV;

    // IBL
    float3 iblR;                     // Reflected specular direction, used for IBL in EvaluateBSDF_Env()
    float  iblPerceptualRoughness;

    float3 specularFGD;              // Store preintegrated BSDF for both specular and diffuse
    float  diffuseFGD;
    //caustic lookup
    float2 irisPlanePosition; //position on iris plane, ie -1 left/bottom, 1 right/top
    // Area lights (17 VGPRs)
    // TODO: 'orthoBasisViewNormal' is just a rotation around the normal and should thus be just 1x VGPR.
    float3x3 orthoBasisViewDiffuseNormal;
    float3x3 orthoBasisViewNormal;   // Right-handed view-dependent orthogonal basis around the normal (6x VGPRs)
    float3x3 ltcTransformDiffuse;    // Inverse transformation for Lambertian or Disney Diffuse        (4x VGPRs)
    float3x3 ltcTransformSpecular;   // Inverse transformation for GGX                                 (4x VGPRs)
};

//
// ClampRoughness helper specific to this material
//
void ClampRoughness(inout PreLightData preLightData, inout BSDFData bsdfData, float minRoughness)
{
    bsdfData.roughness = max(minRoughness, bsdfData.roughness);
}

// This function is call to precompute heavy calculation before lightloop
PreLightData GetPreLightData(float3 V, PositionInputs posInput, inout BSDFData bsdfData)
{
    PreLightData preLightData;
    ZERO_INITIALIZE(PreLightData, preLightData);

    float3 N = bsdfData.normalWS;
    preLightData.NdotV = dot(N, V);
    preLightData.iblPerceptualRoughness = bsdfData.perceptualRoughness;

    float clampedNdotV = ClampNdotV(preLightData.NdotV);

    // Handle IBL + area light + multiscattering.
    // Note: use the not modified by anisotropy iblPerceptualRoughness here.
    float specularReflectivity;
    GetPreIntegratedFGDGGXAndDisneyDiffuse(clampedNdotV, preLightData.iblPerceptualRoughness, bsdfData.fresnel0, preLightData.specularFGD, preLightData.diffuseFGD, specularReflectivity);
    preLightData.diffuseFGD = 1.0;

    float3 iblN;
    preLightData.partLambdaV = GetSmithJointGGXPartLambdaV(clampedNdotV, bsdfData.roughness);
    iblN = N;

    preLightData.iblR = reflect(-V, iblN);

    // Area light
    preLightData.ltcTransformDiffuse  = k_identity3x3;
    preLightData.ltcTransformSpecular = SampleLtcMatrix(bsdfData.perceptualRoughness, clampedNdotV, LTCLIGHTINGMODEL_GGX);

    // Construct a right-handed view-dependent orthogonal basis around the normal
    preLightData.orthoBasisViewDiffuseNormal = GetOrthoBasisViewNormal(V, bsdfData.diffuseNormalWS, dot(V, bsdfData.diffuseNormalWS));
    preLightData.orthoBasisViewNormal = GetOrthoBasisViewNormal(V, N, preLightData.NdotV);

    //for caustic lookup
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_EYE_CAUSTIC_FROM_LUT))
    {
        float3 posOS = TransformWorldToObject(posInput.positionWS);
        float3 refrOS = TransformWorldToObjectDir(refract(-V, N, 1.0 / bsdfData.IOR));

        float t = max(posOS.z - bsdfData.irisPlaneOffset, 0.f) / max(-refrOS.z, 1e-5f);

        float3 irisPositionOS = posOS + refrOS * t;
        preLightData.irisPlanePosition = irisPositionOS.xy / bsdfData.irisRadius;
    }

    return preLightData;
}

//-----------------------------------------------------------------------------
// bake lighting function
//-----------------------------------------------------------------------------

// This define allow to say that we implement a ModifyBakedDiffuseLighting function to be call in PostInitBuiltinData
#define MODIFY_BAKED_DIFFUSE_LIGHTING

void ModifyBakedDiffuseLighting(float3 V, PositionInputs posInput, PreLightData preLightData, BSDFData bsdfData, inout BuiltinData builtinData)
{
    // For SSS we need to take into account the state of diffuseColor
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_EYE_SUBSURFACE_SCATTERING))
    {
        bsdfData.diffuseColor = GetModifiedDiffuseColorForSSS(bsdfData);
    }

    // Premultiply (back) bake diffuse lighting information with diffuse pre-integration
    builtinData.bakeDiffuseLighting *= preLightData.diffuseFGD * bsdfData.diffuseColor;
}

//-----------------------------------------------------------------------------
// light transport functions
//-----------------------------------------------------------------------------

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
    return true; // In order to get cqustic effect and concavity
}

// This function apply BSDF. Assumes that NdotL is positive.
CBSDF EvaluateBSDF(float3 V, float3 L, PreLightData preLightData, BSDFData bsdfData)
{
    // There are multiple transparent layers occuring near the surface of the human eye,
    // each with unique BSDF properties
    //
    // 0: Fluids (tears)
    // 1: Cornea (lens) surface
    // 2: Cornea (lens) fluids
    // 3: Iris surface / sclera surface
    //
    // For simplicity and performance, We simplify down to two distinct layers we would like to shade:
    // Layer 0 (Specular Only): Surface fluids and cornea (lens).
    // Layer 1 (Diffuse Only): Sclera and iris (post cornea refraction).
    //
    // This is a reasonable approximation, as the index of refraction of layers 0-2 are highly similar,
    // and roughness of layers 0-2 are highly similar (and very low), and layers 0-2 are all highly transparent

    CBSDF cbsdf;
    ZERO_INITIALIZE(CBSDF, cbsdf);

    float3 N = bsdfData.normalWS;

    float NdotV = preLightData.NdotV;
    float clampedNdotV = ClampNdotV(NdotV);

    float NdotL = dot(N, L);
    float clampedNdotL = saturate(NdotL);

    float LdotV, NdotH, LdotH, invLenLV;
    GetBSDFAngle(V, L, NdotL, NdotV, LdotV, NdotH, LdotH, invLenLV);

    float3 F = F_Schlick(bsdfData.fresnel0, LdotH);
    // We use abs(NdotL) to handle the none case of double sided
    float DV = DV_SmithJointGGX(NdotH, abs(NdotL), clampedNdotV, bsdfData.roughness, preLightData.partLambdaV);

    cbsdf.specR = F * DV * clampedNdotL;

    // Use diffuse normal map for diffuse

    N = bsdfData.diffuseNormalWS;
    // Add some power wrap lighting (currently empirical hard coded value) for more natural effect
    clampedNdotL = ComputeWrappedPowerDiffuseLighting(dot(N, L), PI / 12.0, 2.0);

    cbsdf.diffR = Lambert() * clampedNdotL * (1.0 - F);

    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
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

float ComputeCaustic(float3 V, float3 positionOS, float3 lightDirOS, BSDFData bsdfData)
{
    if (bsdfData.mask.x < 0.001)
    {
        return 0.0;
    }

    // Totally empirical! TODO: need to revisit
    float causticIris = 2.0 * pow(saturate(dot(-normalize(positionOS.xy), lightDirOS.xy)), 2);
    float causticSclera = min(2000.0 * pow(saturate(dot(-normalize(positionOS.xy), lightDirOS.xy)), 20), 100.0);

    return causticSclera * min(bsdfData.mask.x, (1.0 - bsdfData.mask.x)) + causticIris * bsdfData.mask.x;
}

// This is a test coming from Heretic demo. It is way more expensive
// and is sometimes better, sometime not than the "else" codee and don't support caustic.
void LightEyeTransform(PositionInputs posInput, BSDFData bsdfData, inout LightData lightData)
{
    float3 L = normalize(lightData.positionRWS - posInput.positionWS);
    float3 refractL = -refract(-L, bsdfData.geomNormalWS, 1.0 / bsdfData.IOR);

    float3 axis = normalize(cross(L, refractL));

    float angle = acos(dot(L, refractL));

    lightData.positionRWS = Rotate(posInput.positionWS, lightData.positionRWS, axis, angle);
    lightData.forward = Rotate(float3(0, 0, 0), lightData.forward, axis, angle);
    lightData.right = Rotate(float3(0, 0, 0), lightData.right, axis, angle);
    lightData.up = Rotate(float3(0, 0, 0), lightData.up, axis, angle);
}
void LightEyeTransform(PositionInputs posInput, BSDFData bsdfData, inout DirectionalLightData lightData)
{
    float3 L = -lightData.forward;
    float3 refractL = -refract(-L, bsdfData.geomNormalWS, 1.0 / bsdfData.IOR);

    float3 axis = normalize(cross(L, refractL));

    float angle = acos(dot(L, refractL));

    lightData.forward = Rotate(float3(0, 0, 0), lightData.forward, axis, angle);
    lightData.right = Rotate(float3(0, 0, 0), lightData.right, axis, angle);
    lightData.up = Rotate(float3(0, 0, 0), lightData.up, axis, angle);
}
//-----------------------------------------------------------------------------
// EvaluateBSDF_Directional
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Directional(LightLoopContext lightLoopContext,
                                        float3 V, PositionInputs posInput, PreLightData preLightData,
                                        DirectionalLightData lightData, BSDFData bsdfData,
                                        BuiltinData builtinData)
{
    DirectLighting dl = ShadeSurface_Directional(   lightLoopContext, posInput, builtinData,
                                                    preLightData, lightData, bsdfData, V);

    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_EYE_CINEMATIC))
    {
        float c = bsdfData.mask.x;
        if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_EYE_CAUSTIC_FROM_LUT))
        {
            float3 lightPosOS = TransformWorldToObjectDir(-lightData.forward) * 1000.f;
            c = ComputeCausticFromLUT(preLightData.irisPlanePosition, bsdfData.irisPlaneOffset, lightPosOS, bsdfData.causticIntensity);
        }
        // Evaluate a second time the light but for a different position and for diffuse only.
        LightEyeTransform(posInput, bsdfData, lightData);

        DirectLighting dlIris = ShadeSurface_Directional(   lightLoopContext, posInput, builtinData,
                                                            preLightData, lightData, bsdfData, V);

        float3 caustic = ApplyCausticToDiffuse(dlIris.diffuse, c, bsdfData.mask.x, bsdfData.causticBlend);
        dl.diffuse = (1.f - bsdfData.mask.x) * dl.diffuse + caustic;
    }
    else
    {
        float3 positionOS = TransformWorldToObject(posInput.positionWS);
        float3 lightDirOS = TransformWorldToObjectDir(-lightData.forward);
        // Atteunate the caustic value for directional as it is stronger than for other light
        dl.diffuse *= 1.0 + 0.5 * ComputeCaustic(V, positionOS, lightDirOS, bsdfData);
    }

    return dl;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Punctual (supports spot, point and projector lights)
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Punctual(LightLoopContext lightLoopContext,
                                     float3 V, PositionInputs posInput,
                                     PreLightData preLightData, LightData lightData,
                                     BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting dl = ShadeSurface_Punctual(  lightLoopContext, posInput, builtinData,
                                                preLightData, lightData, bsdfData, V);

    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_EYE_CINEMATIC))
    {
        float c = bsdfData.mask.x;
        if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_EYE_CAUSTIC_FROM_LUT))
        {
            c = ComputeCausticFromLUT(preLightData.irisPlanePosition, bsdfData.irisPlaneOffset, TransformWorldToObject(lightData.positionRWS), bsdfData.causticIntensity);
        }
        // Evaluate a second time the light but for a different position and for diffuse only.
        LightEyeTransform(posInput, bsdfData, lightData);

        DirectLighting dlIris = ShadeSurface_Punctual(  lightLoopContext, posInput, builtinData,
                                                        preLightData, lightData, bsdfData, V);

        float3 caustic = ApplyCausticToDiffuse(dlIris.diffuse, c, bsdfData.mask.x, bsdfData.causticBlend);
        dl.diffuse = (1.f - bsdfData.mask.x) * dl.diffuse + caustic;
    }
    else
    {
        float3 positionOS = TransformWorldToObject(posInput.positionWS);
        float3 lightPosOS = TransformWorldToObject(lightData.positionRWS);
        float3 lightDirOS = normalize(lightPosOS);
        dl.diffuse *= 1.0 + ComputeCaustic(V, positionOS, lightDirOS, bsdfData);
    }

    return dl;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Area - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Area2(LightLoopContext lightLoopContext,
    float3 V, PositionInputs posInput,
    PreLightData preLightData, LightData lightData,
    BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    const bool isRectLight = lightData.lightType == GPULIGHTTYPE_RECTANGLE; // static

#if SHADEROPTIONS_BARN_DOOR
    if (isRectLight)
    {
        RectangularLightApplyBarnDoor(lightData, posInput.positionWS);
    }
#endif

    // Translate the light s.t. the shaded point is at the origin of the coordinate system.
    float3 unL = lightData.positionRWS - posInput.positionWS;

    // These values could be precomputed on CPU to save VGPR or ALU.
    float halfLength = lightData.size.x * 0.5;
    float halfHeight = lightData.size.y * 0.5; // = 0 for a line light

    float intensity = PillowWindowing(unL, lightData.right, lightData.up, halfLength, halfHeight,
                                      lightData.rangeAttenuationScale, lightData.rangeAttenuationBias);

    // Make sure the light is front-facing (and has a non-zero effective area).
    intensity *= (isRectLight && dot(unL, lightData.forward) >= 0) ? 0 : 1;

    bool isVisible = true;

    // Raytracing shadow algorithm require to evaluate lighting without shadow, so it defined SKIP_RASTERIZED_AREA_SHADOWS
    // This is only present in Lit Material as it is the only one using the improved shadow algorithm.
#ifndef SKIP_RASTERIZED_AREA_SHADOWS
    if (isRectLight && intensity > 0)
    {
        SHADOW_TYPE shadow = EvaluateShadow_RectArea(lightLoopContext, posInput, lightData, builtinData, bsdfData.normalWS, normalize(lightData.positionRWS), length(lightData.positionRWS));
        lightData.color.rgb *= ComputeShadowColor(shadow, lightData.shadowTint, lightData.penumbraTint);

        isVisible = Max3(lightData.color.r, lightData.color.g, lightData.color.b) > 0;
    }
#endif

    // Terminate if the shaded point is occluded or is too far away.
    if (isVisible && intensity > 0)
    {
        float4 ltcValue;

        // ----- 1. Evaluate the diffuse part -----

        // Rotate the light vectors into the local coordinate system.
        // Note: we don't have the same normal for diffuse and specular.
        float3 centerDiff = mul(preLightData.orthoBasisViewDiffuseNormal, unL);
        float3 rightDiff  = mul(preLightData.orthoBasisViewDiffuseNormal, lightData.right);
        float3 upDiff     = mul(preLightData.orthoBasisViewDiffuseNormal, lightData.up);

        ltcValue = EvaluateLTC_Area(isRectLight, centerDiff, rightDiff, upDiff, halfLength, halfHeight,
                                #ifdef USE_DIFFUSE_LAMBERT_BRDF
                                    k_identity3x3, 1.0f,
                                #else
                                    // LTC light cookies appear broken unless diffuse roughness is set to 1.
                                    transpose(preLightData.ltcTransformDiffuse), /*bsdfData.perceptualRoughness*/ 1.0f,
                                #endif
                                    lightData.cookieMode, lightData.cookieScaleOffset);

        ltcValue.a *= intensity * lightData.diffuseDimmer;

        // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
        lighting.diffuse += ltcValue.rgb * ltcValue.a;

        // ----- 2. Evaluate the specular part -----

        // Rotate the light vectors into the local coordinate system.
        // Note: we don't have the same normal for diffuse and specular.
        float3 centerSpec = mul(preLightData.orthoBasisViewNormal, unL);
        float3 rightSpec  = mul(preLightData.orthoBasisViewNormal, lightData.right);
        float3 upSpec     = mul(preLightData.orthoBasisViewNormal, lightData.up);

        ltcValue = EvaluateLTC_Area(isRectLight, centerSpec, rightSpec, upSpec, halfLength, halfHeight,
                                    transpose(preLightData.ltcTransformSpecular), bsdfData.perceptualRoughness,
                                    lightData.cookieMode, lightData.cookieScaleOffset);

        ltcValue.a *= intensity * lightData.specularDimmer;

        // We need to multiply by the magnitude of the integral of the BRDF
        // ref: http://advances.realtimerendering.com/s2016/s2016_ltc_fresnel.pdf
        lighting.specular += ltcValue.rgb * ltcValue.a;

        // We need to multiply by the magnitude of the integral of the BRDF
        // ref: http://advances.realtimerendering.com/s2016/s2016_ltc_fresnel.pdf
        lighting.diffuse  *= lightData.color * preLightData.diffuseFGD;
        lighting.specular *= lightData.color * preLightData.specularFGD;

        // ----- 3. Debug display -----

    #ifdef DEBUG_DISPLAY
        if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
        {
            ltcValue = EvaluateLTC_Area(isRectLight, centerDiff, rightDiff, upDiff, halfLength, halfHeight,
                                        k_identity3x3, 1.0f,
                                        lightData.cookieMode, lightData.cookieScaleOffset);

            ltcValue.a *= intensity * lightData.diffuseDimmer;

            // Only lighting, not BSDF
            lighting.diffuse  = lightData.color * ltcValue.rgb * ltcValue.a;
            // Apply area light on Lambert then multiply by PI to cancel Lambert
            lighting.diffuse *= PI;
        }
    #endif
    }

    return lighting;
}

DirectLighting EvaluateBSDF_Area(LightLoopContext lightLoopContext,
    float3 V, PositionInputs posInput,
    PreLightData preLightData, LightData lightData,
    BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting dl = EvaluateBSDF_Area2(lightLoopContext, V, posInput, preLightData, lightData, bsdfData, builtinData);

    //diffuse with refracted light direction (if cinematic eye)
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_EYE_CINEMATIC))
    {
        float c = bsdfData.mask.x;
        if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_EYE_CAUSTIC_FROM_LUT))
        {
            c = ComputeCausticFromLUT(preLightData.irisPlanePosition, bsdfData.irisPlaneOffset, TransformWorldToObject(lightData.positionRWS), bsdfData.causticIntensity);
        }

        LightEyeTransform(posInput, bsdfData, lightData);

        DirectLighting dl2 = EvaluateBSDF_Area2(lightLoopContext, V, posInput, preLightData, lightData, bsdfData, builtinData);

        float3 caustic = ApplyCausticToDiffuse(dl2.diffuse, c, bsdfData.mask.x, bsdfData.causticBlend);
        dl.diffuse = (1.f - bsdfData.mask.x) * dl.diffuse + caustic;
    }

    return dl;
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

    // TODO: we should multiply all indirect lighting by the FGD value only ONCE.
    lighting.specularReflected = ssrLighting.rgb * preLightData.specularFGD;
    reflectionHierarchyWeight = ssrLighting.a;

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

    // No refraction

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

    // Note: using influenceShapeType and projectionShapeType instead of (lightData|proxyData).shapeType allow to make compiler optimization in case the type is know (like for sky)
    float intersectionDistance = EvaluateLight_EnvIntersection(positionWS, bsdfData.normalWS, lightData, influenceShapeType, R, weight);

    float3 F = preLightData.specularFGD;

    float4 preLD = SampleEnvWithDistanceBaseRoughness(lightLoopContext, posInput, lightData, R, preLightData.iblPerceptualRoughness, intersectionDistance);
    weight *= preLD.a; // Used by planar reflection to discard pixel

    envLighting = F * preLD.rgb;

    UpdateLightingHierarchyWeights(hierarchyWeight, weight);
    envLighting *= weight * lightData.multiplier;

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
    AmbientOcclusionFactor aoFactor;
    GetScreenSpaceAmbientOcclusionMultibounce(posInput.positionSS, preLightData.NdotV, bsdfData.perceptualRoughness, bsdfData.ambientOcclusion, bsdfData.specularOcclusion, bsdfData.diffuseColor, bsdfData.fresnel0, aoFactor);
    ApplyAmbientOcclusionFactor(aoFactor, builtinData, lighting);

    // Subsurface scattering mode
    float3 modifiedDiffuseColor = GetModifiedDiffuseColorForSSS(bsdfData);

    // Apply the albedo to the direct diffuse lighting (only once). The indirect (baked)
    // diffuse lighting has already multiply the albedo in ModifyBakedDiffuseLighting().
    lightLoopOutput.diffuseLighting = modifiedDiffuseColor * lighting.direct.diffuse + builtinData.bakeDiffuseLighting + builtinData.emissiveColor;
    lightLoopOutput.specularLighting = lighting.direct.specular + lighting.indirect.specularReflected;

#ifdef DEBUG_DISPLAY
    PostEvaluateBSDFDebugDisplay(aoFactor, builtinData, lighting, bsdfData.diffuseColor, lightLoopOutput);
#endif
}

#endif // #ifdef HAS_LIGHTLOOP
