// Continuation of LightEvaluation.hlsl.
// use #define MATERIAL_INCLUDE_TRANSMISSION to include thick transmittance evaluation
// use #define MATERIAL_INCLUDE_PRECOMPUTED_TRANSMISSION to apply pre-computed transmittance (or thin transmittance only)
// use #define OVERRIDE_SHOULD_EVALUATE_THICK_OBJECT_TRANSMISSION to provide a new version of ShouldEvaluateThickObjectTransmission
//-----------------------------------------------------------------------------
// Directional and punctual lights (infinitesimal solid angle)
//-----------------------------------------------------------------------------

#ifndef OVERRIDE_SHOULD_EVALUATE_THICK_OBJECT_TRANSMISSION
bool ShouldEvaluateThickObjectTransmission(float3 V, float3 L, PreLightData preLightData,
                                           BSDFData bsdfData, int shadowIndex)
{
#ifdef MATERIAL_INCLUDE_TRANSMISSION
    // Currently, we don't consider (NdotV < 0) as transmission.
    // TODO: ignore normal map? What about double sided-surfaces with one-sided normals?
    float NdotL = dot(bsdfData.normalWS, L);

    // If a material does not support transmission, it will never have this flag, and
    // the optimization pass of the compiler will remove all of the associated code.
    // However, this will take a lot more CPU time than doing the same thing using
    // the preprocessor.
    return HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_TRANSMISSION_MODE_THICK_OBJECT) &&
           (shadowIndex >= 0.0) && (NdotL < 0.0);
#else
    return false;
#endif
}
#endif

DirectLighting ShadeSurface_Infinitesimal(PreLightData preLightData, BSDFData bsdfData,
                                          float3 V, float3 L, float3 lightColor,
                                          float diffuseDimmer, float specularDimmer)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    if (Max3(lightColor.r, lightColor.g, lightColor.b) > 0)
    {
        CBSDF cbsdf = EvaluateBSDF(V, L, preLightData, bsdfData);

#if defined(MATERIAL_INCLUDE_TRANSMISSION) || defined(MATERIAL_INCLUDE_PRECOMPUTED_TRANSMISSION)
        float3 transmittance = bsdfData.transmittance;
#else
        float3 transmittance = float3(0.0, 0.0, 0.0);
#endif
        // If transmittance or the CBSDF's transmission components are known to be 0,
        // the optimization pass of the compiler will remove all of the associated code.
        // However, this will take a lot more CPU time than doing the same thing using
        // the preprocessor.
        lighting.diffuse  = (cbsdf.diffR + cbsdf.diffT * transmittance) * lightColor * diffuseDimmer;
        lighting.specular = (cbsdf.specR + cbsdf.specT * transmittance) * lightColor * specularDimmer;
    }

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        // Only lighting, no BSDF.
        lighting.diffuse = lightColor * saturate(dot(bsdfData.normalWS, L));
    }
#endif

    return lighting;
}

//-----------------------------------------------------------------------------
// Directional lights
//-----------------------------------------------------------------------------

DirectLighting ShadeSurface_Directional(LightLoopContext lightLoopContext,
                                        PositionInputs posInput, BuiltinData builtinData,
                                        PreLightData preLightData, DirectionalLightData light,
                                        BSDFData bsdfData, float3 V)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 L = -light.forward;

    // Is it worth evaluating the light?
    if ((light.lightDimmer > 0) && IsNonZeroBSDF(V, L, preLightData, bsdfData))
    {
        float4 lightColor = EvaluateLight_Directional(lightLoopContext, posInput, light);
        lightColor.rgb *= lightColor.a; // Composite

#ifdef MATERIAL_INCLUDE_TRANSMISSION
        if (ShouldEvaluateThickObjectTransmission(V, L, preLightData, bsdfData, light.shadowIndex))
        {
            // Transmission through thick objects does not support shadowing
            // from directional lights. It will use the 'baked' transmittance value.
            lightColor *= _DirectionalTransmissionMultiplier;
        }
        else
#endif
        {
            DirectionalShadowType shadow = EvaluateShadow_Directional(lightLoopContext, posInput, light, builtinData, GetNormalForShadowBias(bsdfData));
            float NdotL  = dot(bsdfData.normalWS, L); // No microshadowing when facing away from light (use for thin transmission as well)
            shadow *= NdotL >= 0.0 ? ComputeMicroShadowing(GetAmbientOcclusionForMicroShadowing(bsdfData), NdotL, _MicroShadowOpacity) : 1.0;
            lightColor.rgb *= ComputeShadowColor(shadow, light.shadowTint, light.penumbraTint);
        }

        // Simulate a sphere/disk light with this hack.
        // Note that it is not correct with our precomputation of PartLambdaV
        // (means if we disable the optimization it will not have the
        // same result) but we don't care as it is a hack anyway.
        ClampRoughness(preLightData, bsdfData, light.minRoughness);

        lighting = ShadeSurface_Infinitesimal(preLightData, bsdfData, V, L, lightColor.rgb,
                                              light.diffuseDimmer, light.specularDimmer);
    }

    return lighting;
}

//-----------------------------------------------------------------------------
// Punctual lights
//-----------------------------------------------------------------------------

#ifdef MATERIAL_INCLUDE_TRANSMISSION
// Must be called after checking the results of ShouldEvaluateThickObjectTransmission().
float3 EvaluateTransmittance_Punctual(LightLoopContext lightLoopContext,
                                      PositionInputs posInput, BSDFData bsdfData,
                                      LightData light, float3 L, float4 distances)
{
    // Using the shadow map, compute the distance from the light to the back face of the object.
    // TODO: SHADOW BIAS.
    float distBackFaceToLight = GetPunctualShadowClosestDistance(lightLoopContext.shadowContext, s_linear_clamp_sampler,
                                                                 posInput.positionWS, light.shadowIndex, L, light.positionRWS,
                                                                 light.lightType == GPULIGHTTYPE_POINT);

    // Our subsurface scattering models use the semi-infinite planar slab assumption.
    // Therefore, we need to find the thickness along the normal.
    // Note: based on the artist's input, dependence on the NdotL has been disabled.
    float distFrontFaceToLight   = distances.x;
    float thicknessInUnits       = (distFrontFaceToLight - distBackFaceToLight) /* * -NdotL */;
    float thicknessInMeters      = thicknessInUnits * _WorldScales[bsdfData.diffusionProfileIndex].x;
    float thicknessInMillimeters = thicknessInMeters * MILLIMETERS_PER_METER;

    // We need to make sure it's not less than the baked thickness to minimize light leaking.
    float thicknessDelta = max(0, thicknessInMillimeters - bsdfData.thickness);

    float3 S = _ShapeParams[bsdfData.diffusionProfileIndex].rgb;

#if 0
    float3 expOneThird = exp(((-1.0 / 3.0) * thicknessDelta) * S);
#else
    // Help the compiler. S is premultiplied by ((-1.0 / 3.0) * LOG2_E) on the CPU.
    float3 p = thicknessDelta * S;
    float3 expOneThird = exp2(p);
#endif

    // Approximate the decrease of transmittance by e^(-1/3 * dt * S).
    return bsdfData.transmittance * expOneThird;
}
#endif

DirectLighting ShadeSurface_Punctual(LightLoopContext lightLoopContext,
                                     PositionInputs posInput, BuiltinData builtinData,
                                     PreLightData preLightData, LightData light,
                                     BSDFData bsdfData, float3 V)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 L;
    float4 distances; // {d, d^2, 1/d, d_proj}
    GetPunctualLightVectors(posInput.positionWS, light, L, distances);

    // Is it worth evaluating the light?
    if ((light.lightDimmer > 0) && IsNonZeroBSDF(V, L, preLightData, bsdfData))
    {
        float4 lightColor = EvaluateLight_Punctual(lightLoopContext, posInput, light, L, distances);
        lightColor.rgb *= lightColor.a; // Composite

#ifdef MATERIAL_INCLUDE_TRANSMISSION
        if (ShouldEvaluateThickObjectTransmission(V, L, preLightData, bsdfData, light.shadowIndex))
        {
            // Replace the 'baked' value using 'thickness from shadow'.
            bsdfData.transmittance = EvaluateTransmittance_Punctual(lightLoopContext, posInput,
                                                                    bsdfData, light, L, distances);
        }
        else
#endif
        {
            // This code works for both surface reflection and thin object transmission.
            float shadow = EvaluateShadow_Punctual(lightLoopContext, posInput, light, builtinData, GetNormalForShadowBias(bsdfData), L, distances);
            lightColor.rgb *= ComputeShadowColor(shadow, light.shadowTint, light.penumbraTint);

#ifdef DEBUG_DISPLAY
            // The step with the attenuation is required to avoid seeing the screen tiles at the end of lights because the attenuation always falls to 0 before the tile ends.
            // Note: g_DebugShadowAttenuation have been setup in EvaluateShadow_Punctual
            if (_DebugShadowMapMode == SHADOWMAPDEBUGMODE_SINGLE_SHADOW && light.shadowIndex == _DebugSingleShadowIndex)
                g_DebugShadowAttenuation *= step(FLT_EPS, lightColor.a);
#endif
        }

        // Simulate a sphere/disk light with this hack.
        // Note that it is not correct with our precomputation of PartLambdaV
        // (means if we disable the optimization it will not have the
        // same result) but we don't care as it is a hack anyway.
        ClampRoughness(preLightData, bsdfData, light.minRoughness);

        lighting = ShadeSurface_Infinitesimal(preLightData, bsdfData, V, L, lightColor.rgb,
                                              light.diffuseDimmer, light.specularDimmer);
    }

    return lighting;
}
