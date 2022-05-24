//-----------------------------------------------------------------------------
// Includes
//-----------------------------------------------------------------------------
// SurfaceData is define in SixWaySmokeLit.cs which generate SixWaySmokeLit.cs.hlsl
//TODO: Move file once we're settled (in Runtime)
#include "Packages/com.unity.render-pipelines.high-definition/Editor/VFXGraph/Outputs/SmokeLighting/SixWaySmokeLit.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LTCAreaLight/LTCAreaLight.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/PreIntegratedFGD/PreIntegratedFGD.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinGIUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl" //Prevents compilation errors during build, but not used


#define MODIFY_BAKED_DIFFUSE_LIGHTING

//-----------------------------------------------------------------------------
// Helper functions/variable specific to this material
//-----------------------------------------------------------------------------

float4 GetDiffuseOrDefaultColor(BSDFData bsdfData, float replace)
{
    return float4(bsdfData.diffuseColor,0);
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
    bsdfData.normalWS            = surfaceData.normalWS;
    bsdfData.tangentWS           = surfaceData.tangentWS;

    bsdfData.diffuseColor = surfaceData.baseColor;
    bsdfData.rigLBtF = surfaceData.rigLBtF;
    bsdfData.rigRTBk = surfaceData.rigRTBk;

    bsdfData.bakeDiffuseLighting0 = surfaceData.bakeDiffuseLighting0;
    bsdfData.bakeDiffuseLighting1 = surfaceData.bakeDiffuseLighting1;
    bsdfData.bakeDiffuseLighting2 = surfaceData.bakeDiffuseLighting2;
    bsdfData.backBakeDiffuseLighting0 = surfaceData.backBakeDiffuseLighting0;
    bsdfData.backBakeDiffuseLighting1 = surfaceData.backBakeDiffuseLighting1;
    bsdfData.backBakeDiffuseLighting2 = surfaceData.backBakeDiffuseLighting2;

    bsdfData.fresnel0 = ComputeFresnel0(surfaceData.baseColor, 0, DEFAULT_SPECULAR_VALUE);

    ApplyDebugToBSDFData(bsdfData);

    return bsdfData;
}

// Precomputed lighting data to send to the various lighting functions
struct PreLightData
{
    float NdotV;                     // Could be negative due to normal mapping, use ClampNdotV()

    float3 specularFGD;              // Store preintegrated BSDF for both specular and diffuse
    float  diffuseFGD;
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
    // Don't init to zero to allow to track warning about uninitialized data

    float3 N = bsdfData.normalWS;
    preLightData.NdotV = dot(N, V);

    float clampedNdotV = ClampNdotV(preLightData.NdotV);

    float specularReflectivity;
    GetPreIntegratedFGDGGXAndDisneyDiffuse(clampedNdotV, 1.0f, bsdfData.fresnel0, preLightData.specularFGD, preLightData.diffuseFGD, specularReflectivity);

    return preLightData;
}

NormalData ConvertSurfaceDataToNormalData(SurfaceData surfaceData)
{
    //Prevents compilation issue when using the multi_compile WRITE_NORMAL_BUFFER
    NormalData normalData;
    ZERO_INITIALIZE(NormalData, normalData);
    return normalData;
}

void ModifyBakedDiffuseLighting(float3 V, PositionInputs posInput, PreLightData preLightData, BSDFData bsdfData, inout BuiltinData builtinData)
{
    builtinData.bakeDiffuseLighting = 0;
    float3x3 bakeDiffuseLightingMat = float3x3(bsdfData.bakeDiffuseLighting0,
                                            bsdfData.bakeDiffuseLighting1,
                                            bsdfData.bakeDiffuseLighting2);
    float3x3 backBakeDiffuseLightingMat = float3x3(bsdfData.backBakeDiffuseLighting0,
                                        bsdfData.backBakeDiffuseLighting1,
                                        bsdfData.backBakeDiffuseLighting2);
    for (int i = 0; i<3; i++)
    {
        builtinData.bakeDiffuseLighting += bsdfData.rigRTBk[i] * bakeDiffuseLightingMat[i] + bsdfData.rigLBtF[i] * backBakeDiffuseLightingMat[i];
    }
    builtinData.bakeDiffuseLighting *= INV_PI;
    // Premultiply (back) bake diffuse lighting information with DisneyDiffuse pre-integration
    // Note: When baking reflection probes, we approximate the diffuse with the fresnel0
    builtinData.bakeDiffuseLighting *= preLightData.diffuseFGD * GetDiffuseOrDefaultColor(bsdfData, _ReplaceDiffuseForIndirect).rgb;
}


#ifdef HAS_LIGHTLOOP

bool IsNonZeroBSDF(float3 V, float3 L, PreLightData preLightData, BSDFData bsdfData)
{
    return true; //Smoke must be also lit from behind
}

float3 TransformToLocalFrame(float3 L, BSDFData bsdfData)
{
    float3x3 tbn = GetLocalFrame(bsdfData.normalWS, bsdfData.tangentWS);
    return mul(tbn, L);
}

CBSDF EvaluateBSDF(float3 V, float3 L, PreLightData preLightData, BSDFData bsdfData)
{
    CBSDF cbsdf;
    ZERO_INITIALIZE(CBSDF, cbsdf);

    float3 localL = TransformToLocalFrame(L, bsdfData);
    float3 weights;
    weights.x = localL.x > 0 ? bsdfData.rigRTBk.x : bsdfData.rigLBtF.x;
    weights.y = localL.y < 0 ? bsdfData.rigRTBk.y : bsdfData.rigLBtF.y;
    weights.z = localL.z < 0 ? bsdfData.rigRTBk.z : bsdfData.rigLBtF.z;
    float3 dir = localL;
    float3 sqrDir = dir*dir;

    float diffTerm = Lambert();
    cbsdf.diffR = diffTerm * dot(sqrDir,weights);

    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
    return cbsdf;
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Reflection/VolumeProjection.hlsl"
#define OVERRIDE_EVALUATE_ENV_INTERSECTION

#if !HDRP_ENABLE_COOKIE
#define LIGHT_EVALUATION_NO_COOKIE
#endif
#define LIGHT_EVALUATION_NO_CONTACT_SHADOWS
// TODO: validate that the condition will work!
#if !HDRP_ENABLE_SHADOWS
#define LIGHT_EVALUATION_NO_SHADOWS
#endif

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
                #ifdef LIGHT_EVALUATION_SPLINE_SHADOW_BIAS
                posInput.positionWS += -lightData.forward * GetSplineOffsetForShadowBias(bsdfData);
                #endif

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
    float3 positionWS = posInput.positionWS;

    float  len = lightData.size.x;
    float3 T   = lightData.right;

    float3 unL = lightData.positionRWS - positionWS;

    float3 P1 = lightData.positionRWS - T * (0.5 * len);
    float3 P2 = lightData.positionRWS + T * (0.5 * len);

    const float solidAngle = FlatAngleSegment(positionWS, P1, P2);
    float3 L;

    const float3 dh = - lightData.forward;
    float3 ph = IntersectRayPlane(positionWS, dh, lightData.positionRWS, lightData.forward);

    // Compute the closest position on the rectangle.
    ph = ClosestPointSegment(P1,P2,positionWS);
    L = normalize(ph - positionWS);

    // Configure a theoretically placed point light at the most important position contributing the area light irradiance.
    float3 lightColor = lightData.color * solidAngle;

    // Shadows
#ifndef SKIP_RASTERIZED_AREA_SHADOWS
    {
    #ifdef LIGHT_EVALUATION_SPLINE_SHADOW_BIAS
        posInput.positionWS += -lightData.forward * GetSplineOffsetForShadowBias(bsdfData);
    #endif

        SHADOW_TYPE shadow = EvaluateShadow_RectArea(lightLoopContext, posInput, lightData, builtinData, bsdfData.normalWS, normalize(lightData.positionRWS), length(lightData.positionRWS));
        lightColor *= ComputeShadowColor(shadow, lightData.shadowTint, lightData.penumbraTint);
    }
#endif

    lighting = ShadeSurface_Infinitesimal(preLightData, bsdfData, V, L, lightColor.rgb,
                                          lightData.diffuseDimmer, lightData.specularDimmer);

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
    lightLoopOutput.diffuseLighting = bsdfData.diffuseColor * lighting.direct.diffuse + builtinData.bakeDiffuseLighting + builtinData.emissiveColor;
    lightLoopOutput.specularLighting = lighting.direct.specular + lighting.indirect.specularReflected;

#ifdef DEBUG_DISPLAY
    AmbientOcclusionFactor aoFactor;
    ZERO_INITIALIZE(AmbientOcclusionFactor, aoFactor);
    PostEvaluateBSDFDebugDisplay(aoFactor, builtinData, lighting, bsdfData.diffuseColor, lightLoopOutput);
#endif
}

#endif
