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
    return float4(bsdfData.diffuseColor.rgb,0);
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

float3 GetTransmissionWithAbsorption(float transmission, float4 absorptionColor, float absorptionRange)
{
    absorptionColor.rgb = max(VFX_EPSILON, absorptionColor.rgb);
#if VFX_SIX_WAY_ABSORPTION
    #if VFX_BLENDMODE_PREMULTIPLY
    transmission /= (absorptionColor.a > 0) ? absorptionColor.a : 1.0f  ;
    #endif

    // Empirical value used to parametrize absorption from color
    const float absorptionStrength = 0.2f;
    float3 densityScales = 1.0f + log2(absorptionColor.rgb) / log2(absorptionStrength);
    // Recompute transmission based on density scaling
    float3 outTransmission = pow(saturate(transmission / absorptionRange), densityScales) * absorptionRange;

    #if VFX_BLENDMODE_PREMULTIPLY
    outTransmission *= (absorptionColor.a > 0) ? absorptionColor.a : 1.0f  ;
    #endif

    return min(absorptionRange, outTransmission); // clamp values out of range
#else
    return transmission.xxx * absorptionColor.rgb; // simple multiply
#endif
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
    bsdfData.absorptionRange = surfaceData.absorptionRange;
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

void ModifyBakedDiffuseLighting(float3 V, PositionInputs posInput, PreLightData preLightData, BSDFData bsdfData, inout BuiltinData builtinData)
{
    builtinData.bakeDiffuseLighting = 0;

    // Scale to be energy conserving: Total energy = 4*pi; divided by 6 directions
    float scale = 4.0f * PI / 6.0f;

    float3 frontBakeDiffuseLighting = bsdfData.tangentWS.w > 0.0f ? bsdfData.bakeDiffuseLighting2 : bsdfData.backBakeDiffuseLighting2;
    float3 backBakeDiffuseLighting = bsdfData.tangentWS.w > 0.0f ? bsdfData.backBakeDiffuseLighting2 : bsdfData.bakeDiffuseLighting2;

    float3x3 bakeDiffuseLightingMat;
    bakeDiffuseLightingMat[0] = bsdfData.bakeDiffuseLighting0;
    bakeDiffuseLightingMat[1] = bsdfData.bakeDiffuseLighting1;
    bakeDiffuseLightingMat[2] = frontBakeDiffuseLighting;
    builtinData.bakeDiffuseLighting += GetTransmissionWithAbsorption(bsdfData.rigRTBk.x, bsdfData.diffuseColor, bsdfData.absorptionRange) * bakeDiffuseLightingMat[0];
    builtinData.bakeDiffuseLighting += GetTransmissionWithAbsorption(bsdfData.rigRTBk.y, bsdfData.diffuseColor, bsdfData.absorptionRange) * bakeDiffuseLightingMat[1];
    builtinData.bakeDiffuseLighting += GetTransmissionWithAbsorption(bsdfData.rigRTBk.z, bsdfData.diffuseColor, bsdfData.absorptionRange) * bakeDiffuseLightingMat[2];

    bakeDiffuseLightingMat[0] = bsdfData.backBakeDiffuseLighting0;
    bakeDiffuseLightingMat[1] = bsdfData.backBakeDiffuseLighting1;
    bakeDiffuseLightingMat[2] = backBakeDiffuseLighting;
    builtinData.bakeDiffuseLighting += GetTransmissionWithAbsorption(bsdfData.rigLBtF.x, bsdfData.diffuseColor, bsdfData.absorptionRange) * bakeDiffuseLightingMat[0];
    builtinData.bakeDiffuseLighting += GetTransmissionWithAbsorption(bsdfData.rigLBtF.y, bsdfData.diffuseColor, bsdfData.absorptionRange) * bakeDiffuseLightingMat[1];
    builtinData.bakeDiffuseLighting += GetTransmissionWithAbsorption(bsdfData.rigLBtF.z, bsdfData.diffuseColor, bsdfData.absorptionRange) * bakeDiffuseLightingMat[2];

    builtinData.bakeDiffuseLighting *= scale;
}


#ifdef HAS_LIGHTLOOP

bool IsNonZeroBSDF(float3 V, float3 L, PreLightData preLightData, BSDFData bsdfData)
{
    return true; //Smoke must be also lit from behind
}

float3 TransformToLocalFrame(float3 L, BSDFData bsdfData)
{
    float3 zVec = -bsdfData.normalWS;
    float3 xVec = bsdfData.tangentWS.xyz;
    float3 yVec = cross(zVec, xVec) * bsdfData.tangentWS.w;
    float3x3 tbn = float3x3(xVec, yVec, zVec);
    return mul(tbn, L);
}

CBSDF EvaluateBSDF(float3 V, float3 L, PreLightData preLightData, BSDFData bsdfData)
{
    CBSDF cbsdf;
    ZERO_INITIALIZE(CBSDF, cbsdf);

    float3 dir = TransformToLocalFrame(L, bsdfData);
    float3 weights = dir >= 0 ? bsdfData.rigRTBk.xyz : bsdfData.rigLBtF.xyz;
    float3 sqrDir = dir*dir;

    cbsdf.diffR = GetTransmissionWithAbsorption(dot(sqrDir, weights), bsdfData.diffuseColor, bsdfData.absorptionRange);

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
    lightLoopOutput.diffuseLighting = lighting.direct.diffuse + builtinData.bakeDiffuseLighting + builtinData.emissiveColor;
    lightLoopOutput.specularLighting = lighting.direct.specular + lighting.indirect.specularReflected;

#ifdef DEBUG_DISPLAY
    AmbientOcclusionFactor aoFactor;
    ZERO_INITIALIZE(AmbientOcclusionFactor, aoFactor);
    PostEvaluateBSDFDebugDisplay(aoFactor, builtinData, lighting, bsdfData.diffuseColor, lightLoopOutput);
#endif
}

#endif


/////////////////////////////
// Six Way Smoke Remapping //
/////////////////////////////

float3 ApplyLightMapContrast(float3 originalValue, float2 remapControls)
{
    const bool3 overThreshold = originalValue > remapControls.x;
    float3 X = overThreshold ? float3(1,1,1) - originalValue : originalValue;
    float3 C = overThreshold ? float3(1,1,1) - remapControls.x : remapControls.x;
    float3 O = C * pow(clamp(X,0,1) / (C + VFX_EPSILON), remapControls.y);
    O = overThreshold ? float3(1,1,1) - O : O;

    return O;
}

float3 RemapFrom01(float3 x, float a, float b)
{
    return (b - a) * x + a.xxx;
}

float3 RemapTo01(float3 x, float a, float b)
{
    return clamp((x - a.xxx) / (b - a),0,1);
}

void RemapLightMaps( inout float3 rigRTBk, inout float3 rigLBtF, float2 remapControls)
{
    rigRTBk = ApplyLightMapContrast(rigRTBk, remapControls);
    rigLBtF = ApplyLightMapContrast(rigLBtF, remapControls);
}
void RemapLightMaps( inout float3 rigRTBk, inout float3 rigLBtF, float4 remapCurve)
{
    [unroll]
    for(int i = 0; i < 3; i++)
    {
        rigRTBk[i] = SampleCurve(remapCurve, rigRTBk[i]);
        rigLBtF[i] = SampleCurve(remapCurve, rigLBtF[i]);
    }
}

void RemapLightMapsRangesFrom( inout float3 rigRTBk, inout float3 rigLBtF, float alpha, float4 remapRanges)
{
    rigRTBk = RemapTo01(rigRTBk, remapRanges.x, remapRanges.y);
    rigLBtF = RemapTo01(rigLBtF, remapRanges.x, remapRanges.y);
}

void RemapLightMapsRangesTo( inout float3 rigRTBk, inout float3 rigLBtF, float alpha, float4 remapRanges)
{
    rigRTBk = RemapFrom01(rigRTBk, remapRanges.z, remapRanges.w);
    rigLBtF = RemapFrom01(rigLBtF, remapRanges.z, remapRanges.w);

    rigRTBk = max(0.0f, rigRTBk);
    rigLBtF = max(0.0f, rigLBtF);
}

void SixWaySwapUV(inout float3 rigRTBk, inout float3 rigLBtF)
{
    float right = rigRTBk.y;
    float top = rigLBtF.x;
    float left = rigLBtF.y;
    float bottom = rigRTBk.x;
    rigRTBk.x = right;
    rigLBtF.x = left;
    rigRTBk.y = top;
    rigLBtF.y = bottom;
}
