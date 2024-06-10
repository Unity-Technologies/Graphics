//-----------------------------------------------------------------------------
// Includes
//-----------------------------------------------------------------------------
// SurfaceData is define in SixWaySmokeLit.cs which generate SixWaySmokeLit.cs.hlsl
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SixWayLit/SixWaySmokeLit.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LTCAreaLight/LTCAreaLight.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/PreIntegratedFGD/PreIntegratedFGD.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinGIUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl" //Prevents compilation errors during build, but not used
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SixWayLighting.hlsl"

//-----------------------------------------------------------------------------
// Helper functions/variable specific to this material
//-----------------------------------------------------------------------------
float4 GetDiffuseOrDefaultColor(BSDFData bsdfData, float replace)
{
    return float4(bsdfData.diffuseColor.rgb,0);
}

float3 GetNormalForShadowBias(BSDFData bsdfData)
{
    return bsdfData.normalWS;
}

float GetAmbientOcclusionForMicroShadowing(BSDFData bsdfData)
{
    return 1.0; // Don't do microshadowing for simpleLit
}

// This function is similar to ApplyDebugToSurfaceData but for BSDFData
void ApplyDebugToBSDFData(inout BSDFData bsdfData)
{
    //Do nothing
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
    bool overrideAO = _DebugLightingAmbientOcclusion.x != 0.0;

    if (overrideAO)
    {
        float overrideAOValue = _DebugLightingAmbientOcclusion.y;
        surfaceData.ambientOcclusion = overrideAOValue;
    }

    #endif
}
void GetPBRValidatorDebug(SurfaceData surfaceData, inout float3 result)
{
    result = surfaceData.baseColor.rgb;
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

    bsdfData.normalWS               = surfaceData.normalWS;
    bsdfData.tangentWS              = surfaceData.tangentWS;

    bsdfData.diffuseColor           = surfaceData.baseColor;
    bsdfData.absorptionRange        = surfaceData.absorptionRange;
    bsdfData.leftBottomFront        = surfaceData.leftBottomFront;
    bsdfData.rightTopBack           = surfaceData.rightTopBack;
    bsdfData.ambientOcclusion       = surfaceData.ambientOcclusion;

    bsdfData.bakeDiffuseLighting0   = surfaceData.bakeDiffuseLighting0;
    bsdfData.bakeDiffuseLighting1   = surfaceData.bakeDiffuseLighting1;
    bsdfData.bakeDiffuseLighting2   = surfaceData.bakeDiffuseLighting2;

    ApplyDebugToBSDFData(bsdfData);

    return bsdfData;
}

// Precomputed lighting data to send to the various lighting functions
struct PreLightData
{
 // Empty
};

//
// ClampRoughness helper specific to this material
//
void ClampRoughness(inout PreLightData preLightData, inout BSDFData bsdfData, float minRoughness)
{
    //Do nothing
}

//-----------------------------------------------------------------------------
// BSDF share between directional light, punctual light and area light (reference)
//-----------------------------------------------------------------------------

PreLightData GetPreLightData(float3 V, PositionInputs posInput, inout BSDFData bsdfData)
{
    PreLightData preLightData;
    // Do nothing
    return preLightData;
}

NormalData ConvertSurfaceDataToNormalData(SurfaceData surfaceData)
{
    //Prevents compilation issue when using the multi_compile WRITE_NORMAL_BUFFER
    NormalData normalData;
    ZERO_INITIALIZE(NormalData, normalData);
    return normalData;
}

//-----------------------------------------------------------------------------
// light transport functions
//-----------------------------------------------------------------------------

LightTransportData GetLightTransportData(SurfaceData surfaceData, BuiltinData builtinData, BSDFData bsdfData)
{
    LightTransportData lightTransportData;

    // DiffuseColor for lightmapping
    lightTransportData.diffuseColor = bsdfData.diffuseColor.rgb;
    lightTransportData.emissiveColor = builtinData.emissiveColor;

    return lightTransportData;
}

/// GI functions

void SixWayBakedDiffuseLighting(BSDFData bsdfData, inout BuiltinData builtinData)
{
    builtinData.bakeDiffuseLighting = 0;

    bool alphaPremultipled = (_BlendMode == BLENDINGMODE_PREMULTIPLY);

    const real3 L0 = real3(bsdfData.bakeDiffuseLighting0.w, bsdfData.bakeDiffuseLighting1.w, bsdfData.bakeDiffuseLighting2.w);
    const real3 diffuseGIData[3] = { bsdfData.bakeDiffuseLighting0.xyz, bsdfData.bakeDiffuseLighting1.xyz, bsdfData.tangentWS.w * bsdfData.bakeDiffuseLighting2.xyz};


    builtinData.bakeDiffuseLighting = GetSixWayDiffuseContributions(bsdfData.rightTopBack, bsdfData.leftBottomFront,
                                        bsdfData.diffuseColor, L0, diffuseGIData,
                                        bsdfData.absorptionRange, alphaPremultipled);

    //builtinData.bakeDiffuseLighting *= scale; occlusion ?
}

float3x3 GetLocalTBN(float3 normal, float4 tangent)
{
    float3 zVec = -normal;
    float3 xVec = tangent.xyz;
    float3 yVec = cross(zVec, xVec) * tangent.w;
    return float3x3(xVec, yVec, zVec);
}

float3 TransformToLocalFrame(float3 L, BSDFData bsdfData)
{
    float3x3 tbn = GetLocalTBN(bsdfData.normalWS, bsdfData.tangentWS);
    return mul(tbn, L);
}


void GatherLightProbeData(float3 positionRWS, float3x3 tbn, out float4 diffuseGIData[3])
{
    if (unity_ProbeVolumeParams.x == 0.0)
    {
        float3 ambientL0 = EvaluateLightProbeL0();

        [unroll]
        for (int i = 0; i<3; i++)
        {
            float3 bakeDiffuseLighting = EvaluateLightProbeL1(tbn[i]);
            diffuseGIData[i].xyz = bakeDiffuseLighting;
            diffuseGIData[i].w = ambientL0[i];
        }
    }
    else
    {
        [unroll]
        for (int i = 0; i<3; i++)
        {
            float3 bakeDiffuseLighting = 0;
            float3 backBakeDiffuseLighting = 0;
            // Note: Probe volume here refer to LPPV not APV
            SampleProbeVolumeSH4(TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH), positionRWS, tbn[i], -tbn[i], GetProbeVolumeWorldToObject(),
                                 unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z, unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz, bakeDiffuseLighting, backBakeDiffuseLighting);

            float3 ambientL0 = 0.5f * (bakeDiffuseLighting + backBakeDiffuseLighting);
            bakeDiffuseLighting = 0.5f * (bakeDiffuseLighting - backBakeDiffuseLighting);

            diffuseGIData[i].xyz = bakeDiffuseLighting;
            diffuseGIData[i].w = ambientL0[i];
        }
    }
}
#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
void SampleAPVSixWay(APVSample apvSample, float3x3 tbn, out float4 diffuseGIData[3])
{
    [unroll]
    for (int i = 0; i<3; i++)
    {
        float3 bakeDiffuseLighting;
        EvaluateAPVL1(apvSample, tbn[i], bakeDiffuseLighting);
        diffuseGIData[i].xyz = bakeDiffuseLighting;
        diffuseGIData[i].w = apvSample.L0[i];
    }
}

void EvaluateAmbientProbeSixWay(float weight, float3x3 tbn, out float4 diffuseGIData[3])
{
    float3 ambientL0 = EvaluateAmbientProbeL0();
    [unroll]
    for (int i = 0; i<3; i++)
    {
        float3 bakeDiffuseLighting = EvaluateAmbientProbeL1(tbn[i]) * (1.0f - weight);
        diffuseGIData[i].xyz = bakeDiffuseLighting;
        diffuseGIData[i].w = ambientL0[i];
    }
}
void GatherAPVData(float3 positionRWS, float3x3 tbn, out float4 diffuseGIData[3])
{
    APVResources apvRes = FillAPVResources();
    float3 posWS = GetAbsolutePositionWS(positionRWS);
    float3 V = GetWorldSpaceNormalizeViewDir(positionRWS);

    APVSample apvSample = SampleAPV(posWS, -tbn[2], 0xFFFFFFFF, V);

    if (apvSample.status != APV_SAMPLE_STATUS_INVALID)
    {
        #if MANUAL_FILTERING == 0
        apvSample.Decode();
        #endif

        SampleAPVSixWay(apvSample, tbn, diffuseGIData); //Sample Only L1 even if L2 is available

        if (_APVWeight < 1.f)
        {
            EvaluateAmbientProbeSixWay(_APVWeight, tbn, diffuseGIData);
        }
    }
    else
    {
        // no valid brick, fallback to ambient probe
        EvaluateAmbientProbeSixWay(0.0f, tbn, diffuseGIData);
    }
}
#endif

void GatherDiffuseGIData(float3 normal, float4 tangent, float3 positionRWS, out float4 diffuseGIData0, out float4 diffuseGIData1, out float4 diffuseGIData2)
{
    float4 diffuseGIData[3];
    float3x3 tbn = GetLocalTBN(normal,tangent);
    #if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    GatherAPVData(positionRWS, tbn, diffuseGIData);
    #else
    GatherLightProbeData(positionRWS, tbn, diffuseGIData);
    #endif
    diffuseGIData0 = diffuseGIData[0];
    diffuseGIData1 = diffuseGIData[1];
    diffuseGIData2 = diffuseGIData[2];
}

#ifdef HAS_LIGHTLOOP

bool IsNonZeroBSDF(float3 V, float3 L, PreLightData preLightData, BSDFData bsdfData)
{
    return true; //Smoke must be also lit from behind
}

CBSDF EvaluateBSDF(float3 V, float3 L, PreLightData preLightData, BSDFData bsdfData)
{
    CBSDF cbsdf;
    ZERO_INITIALIZE(CBSDF, cbsdf);

    float3 dir = TransformToLocalFrame(L, bsdfData);
    float3 weights = dir >= 0 ? bsdfData.rightTopBack.xyz : bsdfData.leftBottomFront.xyz;
    float3 sqrDir = dir*dir;

    cbsdf.diffR = GetTransmissionWithAbsorption(dot(sqrDir, weights), bsdfData.diffuseColor, bsdfData.absorptionRange, (_BlendMode == BLENDINGMODE_PREMULTIPLY));

    return cbsdf;
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Reflection/VolumeProjection.hlsl"
#define OVERRIDE_EVALUATE_ENV_INTERSECTION

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
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/MostRepresentativePoint.hlsl"

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
    // We do not support screen space refraction
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);
    return lighting;
}

DirectLighting EvaluateBSDF_Rect(   LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, LightData lightData,
                                    BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);
    float3 positionWS = posInput.positionWS;

    #if SHADEROPTIONS_BARN_DOOR
    // Apply the barn door modification to the light data
    RectangularLightApplyBarnDoor(lightData, positionWS);
    #endif

    float3 unL = lightData.positionRWS - positionWS;
    if (dot(lightData.forward, unL) < FLT_EPS)
    {

        // Rotate the light direction into the light space.
        float3x3 lightToWorld = float3x3(lightData.right, lightData.up, -lightData.forward);
        unL = mul(unL, transpose(lightToWorld));

        const float halfWidth  = lightData.size.x * 0.5;
        const float halfHeight = lightData.size.y * 0.5;

        // Define the dimensions of the attenuation volume.
        // TODO: This could be precomputed.
        float  range      = lightData.range;
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

        if(intensity != 0.0f)
        {
            lightData.diffuseDimmer  *= intensity;
            lightData.specularDimmer *= intensity;

            float4x3 lightVerts;
            lightVerts[0] = lightData.positionRWS + lightData.right * -halfWidth + lightData.up * -halfHeight; // LL
            lightVerts[1] = lightData.positionRWS + lightData.right * -halfWidth + lightData.up *  halfHeight; // UL
            lightVerts[2] = lightData.positionRWS + lightData.right *  halfWidth + lightData.up *  halfHeight; // UR
            lightVerts[3] = lightData.positionRWS + lightData.right *  halfWidth + lightData.up * -halfHeight; // LR

            const float solidAngle = SolidAngleRectangle(positionWS, lightVerts);
            float3 L;

            const float3 dh = - lightData.forward;
            float3 ph = IntersectRayPlane(positionWS, dh, lightData.positionRWS, lightData.forward);

            // Compute the closest position on the rectangle.
            ph = ClosestPointRectangle(ph, lightData.positionRWS, -lightData.right, lightData.up, halfWidth, halfHeight);
            L = normalize(ph - positionWS);

            // Configure a theoretically placed point light at the most important position contributing the area light irradiance.
            float3 lightColor = lightData.color * solidAngle;

            // Only apply cookie if there is one
            if ( lightData.cookieMode != COOKIEMODE_NONE )
            {
                // Compute cookie's mip count.
                const float cookieWidth = lightData.cookieScaleOffset.x * _CookieAtlasSize.x; // Guaranteed power of two.
                const float cookieMips  = round(log2(cookieWidth));

                // Normalize the solid angle against the hemisphere surface area to determine a weight for choosing the mip.
                const float cookieMip = cookieMips - (cookieMips * solidAngle * INV_TWO_PI);

                LightData lightDataFlipped = lightData;
                {
                    // Flip the matrix since the cookie seems flipped incorrectly otherwise.
                    lightDataFlipped.right = -lightDataFlipped.right;
                }

                // Sample the cookie as if it were a typical punctual light.
                lightColor *= EvaluateCookie_Punctual(lightLoopContext, lightDataFlipped, -unL, cookieMip).rgb;
            }

            // Shadows
            #ifndef SKIP_RASTERIZED_AREA_SHADOWS
            {
                SHADOW_TYPE shadow = EvaluateShadow_RectArea(lightLoopContext, posInput, lightData, builtinData, bsdfData.normalWS, normalize(lightData.positionRWS), length(lightData.positionRWS));
                lightColor *= ComputeShadowColor(shadow, lightData.shadowTint, lightData.penumbraTint);
            }
            #endif

            lighting = ShadeSurface_Infinitesimal(preLightData, bsdfData, V, L, lightColor.rgb,
                                                  lightData.diffuseDimmer, lightData.specularDimmer);
        }
    }
    return lighting;

}

DirectLighting EvaluateBSDF_Line(   LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, LightData lightData,
                                    BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);
    float3 positionWS = posInput.positionWS;

    float  len = lightData.size.x;
    float3 T   = lightData.right;

    float3 unL = lightData.positionRWS - positionWS;

    // Pick the major axis of the ellipsoid.
    float3 axis = lightData.right;

    // We define the ellipsoid s.t. r1 = (r + len / 2), r2 = r3 = r.
    // TODO: This could be precomputed.
    float range          = lightData.range;
    float invAspectRatio = saturate(range / (range + (0.5 * len)));

    // Compute the light attenuation.
    float intensity = EllipsoidalDistanceAttenuation(unL, axis, invAspectRatio,
                                                     lightData.rangeAttenuationScale,
                                                     lightData.rangeAttenuationBias);

    // Terminate if the shaded point is too far away.
    if (intensity != 0.0)
    {
        lightData.diffuseDimmer  *= intensity;
        lightData.specularDimmer *= intensity;

        float3 P1 = lightData.positionRWS - T * (0.5 * len);
        float3 P2 = lightData.positionRWS + T * (0.5 * len);

        const float solidAngle = FlatAngleSegment(positionWS, P1, P2);

        const float3 dh = - lightData.forward;
        float3 ph = IntersectRayPlane(positionWS, dh, lightData.positionRWS, lightData.forward);

        // Compute the closest position on the rectangle.
        ph = ClosestPointSegment(P1,P2,positionWS);
        float3 L = normalize(ph - positionWS);

        // Configure a theoretically placed point light at the most important position contributing the area light irradiance.
        float3 lightColor = lightData.color * solidAngle;

        // Shadows
        #ifndef SKIP_RASTERIZED_AREA_SHADOWS
        {
            SHADOW_TYPE shadow = EvaluateShadow_RectArea(lightLoopContext, posInput, lightData, builtinData, bsdfData.normalWS, normalize(lightData.positionRWS), length(lightData.positionRWS));
            lightColor *= ComputeShadowColor(shadow, lightData.shadowTint, lightData.penumbraTint);
        }
        #endif

        lighting = ShadeSurface_Infinitesimal(preLightData, bsdfData, V, L, lightColor.rgb,
                                              lightData.diffuseDimmer, lightData.specularDimmer);
    }

    return lighting;
}


DirectLighting EvaluateBSDF_Area(LightLoopContext lightLoopContext,
    float3 V, PositionInputs posInput,
    PreLightData preLightData, LightData lightData,
    BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    if (lightData.lightType == GPULIGHTTYPE_TUBE)
    {
        return EvaluateBSDF_Line(lightLoopContext, V, posInput, preLightData, lightData, bsdfData, builtinData);
    }
    else
    {
        return EvaluateBSDF_Rect(lightLoopContext, V, posInput, preLightData, lightData, bsdfData, builtinData);
    }

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
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);
    return lighting;
}

void PostEvaluateBSDF(  LightLoopContext lightLoopContext,
                        float3 V, PositionInputs posInput,
                        PreLightData preLightData, BSDFData bsdfData, BuiltinData builtinData, AggregateLighting lighting,
                        out LightLoopOutput lightLoopOutput)
{
    SixWayBakedDiffuseLighting(bsdfData, builtinData);

    AmbientOcclusionFactor aoFactor;
    // Use GTAOMultiBounce approximation for ambient occlusion (allow to get a tint from the baseColor)
    //Specular related values are set to 0 because they are not used for smoke lighting
    #if 0
    GetScreenSpaceAmbientOcclusion(posInput.positionSS, 0.0f, 0.0f, bsdfData.ambientOcclusion, 0.0f, aoFactor);
    #else
    GetScreenSpaceAmbientOcclusionMultibounce(posInput.positionSS, 0.0f, 0.0f, bsdfData.ambientOcclusion, 0.0f, bsdfData.diffuseColor.rgb, 0.0f, aoFactor);
    #endif
    ApplyAmbientOcclusionFactor(aoFactor, builtinData, lighting);

    lightLoopOutput.diffuseLighting = lighting.direct.diffuse + builtinData.bakeDiffuseLighting + builtinData.emissiveColor;
    lightLoopOutput.specularLighting = lighting.direct.specular + lighting.indirect.specularReflected;

#ifdef DEBUG_DISPLAY
    PostEvaluateBSDFDebugDisplay(aoFactor, builtinData, lighting, bsdfData.diffuseColor.rgb, lightLoopOutput);
#endif
}

#endif
