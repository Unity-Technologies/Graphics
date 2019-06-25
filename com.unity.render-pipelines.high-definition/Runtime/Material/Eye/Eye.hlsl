//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------
// SurfaceData is defined in Fabric.cs which generates Fabric.cs.hlsl
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/Eye.cs.hlsl"
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

    if (overrideAlbedo)
    {
        float3 overrideAlbedoValue = _DebugLightingAlbedo.yzw;
        surfaceData.baseColor = overrideAlbedoValue;
    }

    if (overrideNormal)
    {
        surfaceData.normalWS = tangentToWorld[2];
    }

    if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_VALIDATE_DIFFUSE_COLOR)
    {
        surfaceData.baseColor = pbrDiffuseColorValidate(surfaceData.baseColor, surfaceData.specularColor, false, false).xyz;
    }
    else if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_VALIDATE_SPECULAR_COLOR)
    {
        surfaceData.baseColor = pbrSpecularColorValidate(surfaceData.baseColor, surfaceData.specularColor, false, false).xyz;
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
    normalData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.irisSmoothness);
    return normalData;
}

//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(uint2 positionSS, SurfaceData surfaceData)
{
    BSDFData bsdfData;
    ZERO_INITIALIZE(BSDFData, bsdfData);

    bsdfData.diffuseColor = surfaceData.baseColor;
    bsdfData.specularOcclusion = surfaceData.specularOcclusion;
    bsdfData.normalWS = surfaceData.normalWS;
    bsdfData.geomNormalWS = surfaceData.geomNormalWS;
    bsdfData.irisRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.irisSmoothness);
    bsdfData.scleraRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.scleraSmoothness);

    bsdfData.ambientOcclusion = surfaceData.ambientOcclusion;
    bsdfData.eyeMask = surfaceData.mask;

    // After the fill material SSS data has operated, in the case of the fabric we force the value of the fresnel0 term
    bsdfData.fresnel0 = 0.04;

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
    case DEBUGVIEW_EYE_SURFACEDATA_NORMAL_VIEW_SPACE:
        // Convert to view space
        result = TransformWorldToViewDir(surfaceData.normalWS) * 0.5 + 0.5;
        break;
    case DEBUGVIEW_EYE_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
        result = TransformWorldToViewDir(surfaceData.geomNormalWS) * 0.5 + 0.5;
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
    case DEBUGVIEW_EYE_BSDFDATA_NORMAL_VIEW_SPACE:
        // Convert to view space
        result = TransformWorldToViewDir(bsdfData.normalWS) * 0.5 + 0.5;
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

    float3 specularFGD;              // Store preintegrated BSDF for both specular and diffuse
    float  diffuseFGD;

    // IBL
    float3 iblR;                     // Reflected specular direction, used for IBL in EvaluateBSDF_Env()
    float  iblPerceptualRoughness;
};

// This function is call to precompute heavy calculation before lightloop
PreLightData GetPreLightData(float3 V, PositionInputs posInput, inout BSDFData bsdfData)
{
    PreLightData preLightData;
    float3 N = bsdfData.normalWS;
    preLightData.NdotV = dot(N, V);
    float clampedNdotV = ClampNdotV(preLightData.NdotV);
    float realRoughness = lerp(bsdfData.scleraRoughness, bsdfData.irisRoughness, bsdfData.eyeMask);
    preLightData.partLambdaV = GetSmithJointGGXPartLambdaV(clampedNdotV, realRoughness);

    float3 iblN;
    {
        preLightData.partLambdaV = GetSmithJointGGXPartLambdaV(clampedNdotV, realRoughness);
        iblN = N;
    }
    preLightData.iblR = reflect(-V, iblN);
    float specularReflectivity;
    GetPreIntegratedFGDGGXAndDisneyDiffuse(clampedNdotV, preLightData.iblPerceptualRoughness, bsdfData.fresnel0, preLightData.specularFGD, preLightData.diffuseFGD, specularReflectivity);

    return preLightData;
}

//-----------------------------------------------------------------------------
// bake lighting function
//-----------------------------------------------------------------------------

// This define allow to say that we implement a ModifyBakedDiffuseLighting function to be call in PostInitBuiltinData
#define MODIFY_BAKED_DIFFUSE_LIGHTING

void ModifyBakedDiffuseLighting(float3 V, PositionInputs posInput, SurfaceData surfaceData, inout BuiltinData builtinData)
{
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
    float NdotL = dot(bsdfData.normalWS, L);

    return (NdotL > 0.0);
}

#define TRANSMISSION_WRAP_ANGLE (PI/12)
#define TRANSMISSION_WRAP_LIGHT cos(PI/2 - TRANSMISSION_WRAP_ANGLE)

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

    float LdotV, NdotH, LdotH, invLenLV;
    GetBSDFAngle(V, L, NdotL, NdotV, LdotV, NdotH, LdotH, invLenLV);

    float3 F = F_Schlick(bsdfData.fresnel0, LdotH);
    // We use abs(NdotL) to handle the none case of double sided
    float realRoughness = lerp(bsdfData.scleraRoughness, bsdfData.irisRoughness, bsdfData.eyeMask);
    float DV = DV_SmithJointGGX(NdotH, abs(NdotL), clampedNdotV, realRoughness, preLightData.partLambdaV);

     float3 specTerm = F * DV;

    float diffTerm = DisneyDiffuse(clampedNdotV, abs(NdotL), LdotV, realRoughness);

    // The compiler should optimize these. Can revisit later if necessary.
    cbsdf.diffR = diffTerm * clampedNdotL;
    cbsdf.diffT = diffTerm * flippedNdotL;

    // Probably worth branching here for perf reasons.
    // This branch will be optimized away if there's no transmission.
    if (NdotL > 0)
    {
        cbsdf.specR = specTerm * clampedNdotL;
    }

    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
    return cbsdf;
}

//-----------------------------------------------------------------------------
// Surface shading (all light types) below
//-----------------------------------------------------------------------------

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
// EvaluateBSDF_Punctual (supports spot, point and projector lights)
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Punctual(LightLoopContext lightLoopContext,
                                     float3 V, PositionInputs posInput,
                                     PreLightData preLightData, LightData lightData,
                                     BSDFData bsdfData, BuiltinData builtinData)
{
    return ShadeSurface_Punctual(lightLoopContext, posInput, builtinData,
                                 preLightData, lightData, bsdfData, V);
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
#if !HAS_REFRACTION
    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFRACTION)
        return lighting;
#endif

    float3 envLighting;
    float3 positionWS = posInput.positionWS;
    float weight = 1.0;

#ifdef LIT_DISPLAY_REFERENCE_IBL

    envLighting = IntegrateSpecularGGXIBLRef(lightLoopContext, V, preLightData, lightData, bsdfData);

    // TODO: Do refraction reference (is it even possible ?)
    // TODO: handle clear coat


//    #ifdef USE_DIFFUSE_LAMBERT_BRDF
//    envLighting += IntegrateLambertIBLRef(lightData, V, bsdfData);
//    #else
//    envLighting += IntegrateDisneyDiffuseIBLRef(lightLoopContext, V, preLightData, lightData, bsdfData);
//    #endif

#else

    float3 R = preLightData.iblR;

#if HAS_REFRACTION
    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFRACTION)
    {
        positionWS = preLightData.transparentPositionWS;
        R = preLightData.transparentRefractV;
    }
    else
#endif
    {
        if (!IsEnvIndexTexture2D(lightData.envIndex)) // ENVCACHETYPE_CUBEMAP
        {
            R = GetSpecularDominantDir(bsdfData.normalWS, R, preLightData.iblPerceptualRoughness, ClampNdotV(preLightData.NdotV));
            // When we are rough, we tend to see outward shifting of the reflection when at the boundary of the projection volume
            // Also it appear like more sharp. To avoid these artifact and at the same time get better match to reference we lerp to original unmodified reflection.
            // Formula is empirical.
            float roughness = PerceptualRoughnessToRoughness(preLightData.iblPerceptualRoughness);
            R = lerp(R, preLightData.iblR, saturate(smoothstep(0, 1, roughness * roughness)));
        }
    }

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

    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION)
    {
        envLighting = F * preLD.rgb;
    }
#if HAS_REFRACTION
    else
    {
        // No clear coat support with refraction

        // specular transmisted lighting is the remaining of the reflection (let's use this approx)
        // With refraction, we don't care about the clear coat value, only about the Fresnel, thus why we use 'envLighting ='
        envLighting = (1.0 - F) * preLD.rgb * preLightData.transparentTransmittance;
    }
#endif

#endif // LIT_DISPLAY_REFERENCE_IBL

    UpdateLightingHierarchyWeights(hierarchyWeight, weight);
    envLighting *= weight * lightData.multiplier;

    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION)
        lighting.specularReflected = envLighting;
#if HAS_REFRACTION
    else
        lighting.specularTransmitted = envLighting * preLightData.transparentTransmittance;
#endif

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
    diffuseLighting = lighting.direct.diffuse * bsdfData.diffuseColor;
    specularLighting = lighting.direct.specular;
}

#endif // #ifdef HAS_LIGHTLOOP
