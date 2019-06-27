//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------
// SurfaceData is defined in Fabric.cs which generates Fabric.cs.hlsl
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/Eye.cs.hlsl"
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

// #define FABRIC_DISPLAY_REFERENCE_IBL

//-----------------------------------------------------------------------------
// Helper functions/variable specific to this material
//-----------------------------------------------------------------------------

float3 GetNormalForShadowBias(BSDFData bsdfData)
{
    return bsdfData.geomNormalWS;
}

float GetAmbientOcclusionForMicroShadowing(BSDFData bsdfData)
{
    return bsdfData.ambientOcclusion;
}

void ClampRoughness(inout BSDFData bsdfData, float minRoughness)
{
    bsdfData.roughness = max(minRoughness, bsdfData.roughness);
}

// This function is use to help with debugging and must be implemented by any lit material
// Implementer must take into account what are the current override component and
// adjust SurfaceData properties accordingdly
void ApplyDebugToSurfaceData(float3x3 tangentToWorld, inout SurfaceData surfaceData)
{
#ifdef DEBUG_DISPLAY
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
        float overrideSmoothnessValue = _DebugLightingSmoothness.y;
        surfaceData.perceptualSmoothness = overrideSmoothnessValue;
    }

    if (overrideNormal)
    {
        surfaceData.normalWS = tangentToWorld[2];
    }

    // PBR validator isn't supported
    if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_VALIDATE_DIFFUSE_COLOR)
    {
        surfaceData.baseColor = float3(1.0, 0.0, 1.0);
    }
    else if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_VALIDATE_SPECULAR_COLOR)
    {
        surfaceData.baseColor = float3(1.0, 0.0, 1.0);
    }

#endif
}

// This function is similar to ApplyDebugToSurfaceData but for BSDFData
// Note: This will be available and used in ShaderPassForward.hlsl since in Fabric.shader,
// just before including the core code of the pass (ShaderPassForward.hlsl) we include
// Material.hlsl (or Lighting.hlsl which includes it) which in turn includes us,
// Fabric.shader, via the #if defined(UNITY_MATERIAL_*) glue mechanism.
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

    bsdfData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(lerp(surfaceData.perceptualSmoothness, surfaceData.corneaPerceptualSmoothness, surfaceData.mask.x));
    
    bsdfData.fresnel0 = IorToFresnel0(lerp(surfaceData.scleraIOR, surfaceData.corneaIOR, surfaceData.mask.x)).xxx;
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
            result = TransformWorldToViewDir(surfaceData.normalWS) * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_IRIS_NORMAL_VIEW_SPACE:
            result = TransformWorldToViewDir(surfaceData.irisNormalWS) * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
            result = TransformWorldToViewDir(surfaceData.geomNormalWS) * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_SCLERA_IOR:
            result = saturate((surfaceData.scleraIOR - 1.0) / 1.5).xxx;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_CORNEA_IOR:
            result = saturate((surfaceData.corneaIOR - 1.0) / 1.5).xxx;
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
            result = TransformWorldToViewDir(bsdfData.normalWS) * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_DIFFUSE_NORMAL_VIEW_SPACE:
            result = TransformWorldToViewDir(bsdfData.diffuseNormalWS) * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
            result = TransformWorldToViewDir(bsdfData.geomNormalWS) * 0.5 + 0.5;
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

    // Area lights (17 VGPRs)
    // TODO: 'orthoBasisViewNormal' is just a rotation around the normal and should thus be just 1x VGPR.
    float3x3 orthoBasisViewDiffuseNormal;
    float3x3 orthoBasisViewNormal;   // Right-handed view-dependent orthogonal basis around the normal (6x VGPRs)
    float3x3 ltcTransformDiffuse;    // Inverse transformation for Lambertian or Disney Diffuse        (4x VGPRs)
    float3x3 ltcTransformSpecular;   // Inverse transformation for GGX                                 (4x VGPRs)
};

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
    // UVs for sampling the LUTs
    float theta = FastACosPos(clampedNdotV); // For Area light - UVs for sampling the LUTs
    float2 uv = Remap01ToHalfTexelCoord(float2(bsdfData.perceptualRoughness, theta * INV_HALF_PI), LTC_LUT_SIZE);

    // Note we load the matrix transpose (avoid to have to transpose it in shader)
    preLightData.ltcTransformDiffuse = k_identity3x3;

    // Get the inverse LTC matrix for GGX
    // Note we load the matrix transpose (avoid to have to transpose it in shader)
    preLightData.ltcTransformSpecular      = 0.0;
    preLightData.ltcTransformSpecular._m22 = 1.0;
    preLightData.ltcTransformSpecular._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, s_linear_clamp_sampler, uv, LTC_GGX_MATRIX_INDEX, 0);

    // Construct a right-handed view-dependent orthogonal basis around the normal
    preLightData.orthoBasisViewDiffuseNormal = GetOrthoBasisViewNormal(V, bsdfData.diffuseNormalWS, preLightData.NdotV);
    preLightData.orthoBasisViewNormal = GetOrthoBasisViewNormal(V, N, preLightData.NdotV);

    return preLightData;
}

//-----------------------------------------------------------------------------
// bake lighting function
//-----------------------------------------------------------------------------

// This define allow to say that we implement a ModifyBakedDiffuseLighting function to be call in PostInitBuiltinData
#define MODIFY_BAKED_DIFFUSE_LIGHTING

void ModifyBakedDiffuseLighting(float3 V, PositionInputs posInput, SurfaceData surfaceData, inout BuiltinData builtinData)
{
    // To get the data we need to do the whole process - compiler should optimize everything
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);
    PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

    // For SSS we need to take into account the state of diffuseColor
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_EYE_SUBSURFACE_SCATTERING))
    {
        bsdfData.diffuseColor = GetModifiedDiffuseColorForSSS(bsdfData);
    }

    // Premultiply (back) bake diffuse lighting information with DisneyDiffuse pre-integration
    // Note: When baking reflection probes, we approximate the diffuse with the fresnel0
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
    clampedNdotL = saturate(dot(N, L));

    cbsdf.diffR = Lambert() * clampedNdotL * (1 - F);

    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
    return cbsdf;
}

//-----------------------------------------------------------------------------
// Surface shading (all light types) below
//-----------------------------------------------------------------------------

# include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightEvaluation.hlsl"
# include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialEvaluation.hlsl"
# include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/SurfaceShading.hlsl"

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
// EvaluateBSDF_Punctual (supports spot, point and projector lights)
//-----------------------------------------------------------------------------

float ComputePunctualCaustic(float3 V, float3 positionWS, float3 lightPosWS, BSDFData bsdfData)
{
    float3 pos = TransformWorldToObject(positionWS);

    float3 normal = normalize(TransformWorldToObjectDir(bsdfData.normalWS));
    float3 lightPos = TransformWorldToObject(lightPosWS);
    float3 lightDir = normalize(lightPos);

    // Completely heuristic!
    float causticSclera = pow(2.0 * saturate(-dot(normal.xy, lightDir.xy)), 20);
   // float alphaSclera = pos.z < 1.05 ? saturate(1.0 - smoothStep((pos.z - 0.95) * 10)) : 0.01;

    float causticIris = 2.0 * pow(saturate(dot(-normalize(pos.xy), lightDir.xy)), 2);
    //float alphaIris = pos.z > 1.05 ? 1.0 : 0.0;

    //return causticSclera * alphaSclera + causticIris * alphaIris;
    return causticSclera * min(bsdfData.mask.y, 1 - bsdfData.mask.y) + causticIris * bsdfData.mask.x;
}

DirectLighting EvaluateBSDF_Punctual(LightLoopContext lightLoopContext,
                                     float3 V, PositionInputs posInput,
                                     PreLightData preLightData, LightData lightData,
                                     BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting dl = ShadeSurface_Punctual(  lightLoopContext, posInput, builtinData,
                                                preLightData, lightData, bsdfData, V);

    dl.diffuse *= 1.0 + ComputePunctualCaustic(V, posInput.positionWS, lightData.positionRWS, bsdfData);

    return dl;
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
#ifdef ELLIPSOIDAL_ATTENUATION
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
            lightVerts[0] = lightData.positionRWS + lightData.right * halfWidth + lightData.up * halfHeight;
            lightVerts[1] = lightData.positionRWS + lightData.right * halfWidth + lightData.up * -halfHeight;
            lightVerts[2] = lightData.positionRWS + lightData.right * -halfWidth + lightData.up * -halfHeight;
            lightVerts[3] = lightData.positionRWS + lightData.right * -halfWidth + lightData.up * halfHeight;

            // Note: We don't have the same normal for diffuse and specular
            // Rotate the endpoints into the local coordinate system.
            float4x3 lightVertsDiff  = mul(lightVerts, transpose(preLightData.orthoBasisViewDiffuseNormal));

            float3 ltcValue;

            // Evaluate the diffuse part
            // Polygon irradiance in the transformed configuration.
            float4x3 LD = mul(lightVertsDiff, preLightData.ltcTransformDiffuse);
            ltcValue = PolygonIrradiance(LD);
            ltcValue *= lightData.diffuseDimmer;

            // Only apply cookie if there is one
            if (lightData.cookieIndex >= 0)
            {
                // Compute the cookie data for the diffuse term
                float3 formFactorD = PolygonFormFactor(LD);
                ltcValue *= SampleAreaLightCookie(lightData.cookieIndex, LD, formFactorD);
            }

            // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
            // See comment for specular magnitude, it apply to diffuse as well
            lighting.diffuse = preLightData.diffuseFGD * ltcValue;

            // Evaluate the specular part
            float4x3 lightVertsSpec = mul(lightVerts, transpose(preLightData.orthoBasisViewNormal));

            // Polygon irradiance in the transformed configuration.
            float4x3 LS = mul(lightVertsSpec, preLightData.ltcTransformSpecular);
            ltcValue = PolygonIrradiance(LS);
            ltcValue *= lightData.specularDimmer;

            // Only apply cookie if there is one
            if (lightData.cookieIndex >= 0)
            {
                // Compute the cookie data for the specular term
                float3 formFactorS = PolygonFormFactor(LS);
                ltcValue *= SampleAreaLightCookie(lightData.cookieIndex, LS, formFactorS);
            }

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

    float shadow = 1.0;
    float shadowMask = 1.0;
#ifdef SHADOWS_SHADOWMASK
    // shadowMaskSelector.x is -1 if there is no shadow mask
    // Note that we override shadow value (in case we don't have any dynamic shadow)
    shadow = shadowMask = (lightData.shadowMaskSelector.x >= 0.0) ? dot(BUILTIN_DATA_SHADOW_MASK, lightData.shadowMaskSelector) : 1.0;
#endif

#if defined(SCREEN_SPACE_SHADOWS) && !defined(_SURFACE_TYPE_TRANSPARENT)
    if (lightData.screenSpaceShadowIndex >= 0)
    {
        shadow = GetScreenSpaceShadow(posInput, lightData.screenSpaceShadowIndex);
    }
    else
#endif // ENABLE_RAYTRACING
    if (lightData.shadowIndex != -1)
    {
#if RASTERIZED_AREA_LIGHT_SHADOWS
            // lightData.positionRWS now contains the Light vector. 
            shadow = GetAreaLightAttenuation(lightLoopContext.shadowContext, posInput.positionSS, posInput.positionWS, bsdfData.normalWS, lightData.shadowIndex, normalize(lightData.positionRWS), length(lightData.positionRWS));
#ifdef SHADOWS_SHADOWMASK
            // See comment for punctual light shadow mask
            shadow = lightData.nonLightMappedOnly ? min(shadowMask, shadow) : shadow;
#endif
        shadow = lerp(shadowMask, shadow, lightData.shadowDimmer);

#endif
    }

#if RASTERIZED_AREA_LIGHT_SHADOWS || SUPPORTS_RAYTRACED_AREA_SHADOWS
    float3 shadowColor = ComputeShadowColor(shadow, lightData.shadowTint);
    lighting.diffuse *= shadowColor;
    lighting.specular *= shadowColor;
#endif

    return lighting;
}

DirectLighting EvaluateBSDF_Area(LightLoopContext lightLoopContext,
    float3 V, PositionInputs posInput,
    PreLightData preLightData, LightData lightData,
    BSDFData bsdfData, BuiltinData builtinData)
{
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

    // Note: RGB is already premultiplied by A.
    // TODO: we should multiply all indirect lighting by the FGD value only ONCE.
    lighting.specularReflected = ssrLighting.rgb /* * ssrLighting.a */ * preLightData.specularFGD;
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
    EvaluateLight_EnvIntersection(positionWS, bsdfData.normalWS, lightData, influenceShapeType, R, weight);

    float3 F = preLightData.specularFGD;

    float iblMipLevel;
    // TODO: We need to match the PerceptualRoughnessToMipmapLevel formula for planar, so we don't do this test (which is specific to our current lightloop)
    // Specific case for Texture2Ds, their convolution is a gaussian one and not a GGX one - So we use another roughness mip mapping.
    if (IsEnvIndexTexture2D(lightData.envIndex))
    {
        // Empirical remapping
        iblMipLevel = PlanarPerceptualRoughnessToMipmapLevel(preLightData.iblPerceptualRoughness, _ColorPyramidScale.z);
    }
    else
    {
        iblMipLevel = PerceptualRoughnessToMipmapLevel(preLightData.iblPerceptualRoughness);
    }

    float4 preLD = SampleEnv(lightLoopContext, lightData.envIndex, R, iblMipLevel);
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
                        out float3 diffuseLighting, out float3 specularLighting)
{
    AmbientOcclusionFactor aoFactor;
    GetScreenSpaceAmbientOcclusionMultibounce(posInput.positionSS, preLightData.NdotV, bsdfData.perceptualRoughness, bsdfData.ambientOcclusion, bsdfData.specularOcclusion, bsdfData.diffuseColor, bsdfData.fresnel0, aoFactor);
    ApplyAmbientOcclusionFactor(aoFactor, builtinData, lighting);

    // Subsurface scattering mode
    float3 modifiedDiffuseColor = GetModifiedDiffuseColorForSSS(bsdfData);

    // Apply the albedo to the direct diffuse lighting (only once). The indirect (baked)
    // diffuse lighting has already multiply the albedo in ModifyBakedDiffuseLighting().
    diffuseLighting = modifiedDiffuseColor * lighting.direct.diffuse + builtinData.bakeDiffuseLighting + builtinData.emissiveColor;
    specularLighting = lighting.direct.specular + lighting.indirect.specularReflected;

#ifdef DEBUG_DISPLAY
    PostEvaluateBSDFDebugDisplay(aoFactor, builtinData, lighting, bsdfData.diffuseColor, diffuseLighting, specularLighting);
#endif
}

#endif // #ifdef HAS_LIGHTLOOP
