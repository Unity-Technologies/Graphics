// Continuation of LightEvaluation.hlsl.

//-----------------------------------------------------------------------------
// Directional and punctual lights (infinitesimal solid angle)
//-----------------------------------------------------------------------------

bool ShouldEvaluateThickObjectTransmission(float3 V, float3 L, PreLightData preLightData,
                                           BSDFData bsdfData, int shadowIndex)
{
    // Currently, we don't consider (NdotV < 0) as transmission.
    // TODO: ignore normal map? What about double sided-surfaces with one-sided normals?
    float NdotL = dot(bsdfData.normalWS, L);

    // If a material does not support transmission, it will never have this flag.
    return HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_TRANSMISSION_MODE_THICK_OBJECT) &&
           (shadowIndex >= 0) && (NdotL < 0);
}

DirectLighting ShadeSurface_Infinitesimal(PreLightData preLightData, BSDFData bsdfData,
                                          float3 V, float3 L, float3 lightColor,
                                          float diffuseDimmer, float specularDimmer)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

#ifndef DEBUG_DISPLAY
    if (Max3(lightColor.r, lightColor.g, lightColor.b) > 0)
    {
        CBxDF cbxdf = EvaluateCBxDF(V, L, preLightData, bsdfData);

        lighting.diffuse  = (cbxdf.diffR + cbxdf.diffT * bsdfData.transmittance) * diffuseDimmer  * lightColor;
        lighting.specular = (cbxdf.specR + cbxdf.specT * bsdfData.transmittance) * specularDimmer * lightColor;
    }
#else
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        // Only lighting, no BSDF.
        lighting.diffuse  = lightColor * saturate(dot(bsdfData.normalWS, L));
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
    if ((light.lightDimmer > 0) && IsNonZeroCBxDF(V, L, preLightData, bsdfData))
    {
        float4 lightColor = EvaluateLight_Directional(lightLoopContext, posInput, light);

        if (ShouldEvaluateThickObjectTransmission(V, L, preLightData, bsdfData, light.shadowIndex))
        {
            // Transmission through thick objects does not support shadowing
            // from directional lights. It will use the 'baked' transmittance value.
        }
        else
        {
            // TODO: the shadow code should do it for us. That would be far more efficient.
            float3 sN  = GetNormalForShadowBias(bsdfData);
                   sN *= FastSign(dot(sN, L));

            // This code works for both surface reflection and thin object transmission.
            lightColor.a *= EvaluateShadow_Directional(lightLoopContext, posInput, light, builtinData, sN);
            lightColor.a *= ComputeMicroShadowing(bsdfData.ambientOcclusion,
                                                  abs(dot(bsdfData.normalWS, L)),
                                                  _MicroShadowOpacity);
        }

        lightColor.rgb *= lightColor.a; // Composite

        // Simulate a sphere/disk light with this hack.
        // Note that it is not correct with our precomputation of PartLambdaV
        // (means if we disable the optimization it will not have the
        // same result) but we don't care as it is a hack anyway.
        ClampRoughness(bsdfData, light.minRoughness);

        lighting = ShadeSurface_Infinitesimal(preLightData, bsdfData, V, L, lightColor.rgb,
                                              light.diffuseDimmer, light.specularDimmer);
    }

    return lighting;
}

//-----------------------------------------------------------------------------
// Punctual lights
//-----------------------------------------------------------------------------

// Must be called after checking the results of ShouldEvaluateThickObjectTransmission().
float3 EvaluateTransmittance_Punctual(LightLoopContext lightLoopContext,
                                      PositionInputs posInput, BSDFData bsdfData,
                                      LightData light, float3 L, float4 distances)
{
#ifdef MATERIAL_INCLUDE_TRANSMISSION
    /*********************
    * TODO: SHADOW BIAS. *
    *********************/
    // Using the shadow map, compute the distance from the light to the back face of the object.
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
#else
    return 0;
#endif
}

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
    if ((light.lightDimmer > 0) && IsNonZeroCBxDF(V, L, preLightData, bsdfData))
    {
        float4 lightColor = EvaluateLight_Punctual(lightLoopContext, posInput, light, L, distances);

        if (ShouldEvaluateThickObjectTransmission(V, L, preLightData, bsdfData, light.shadowIndex))
        {
            // Replace the 'baked' value using 'thickness from shadow'.
            bsdfData.transmittance = EvaluateTransmittance_Punctual(lightLoopContext, posInput,
                                                                    bsdfData, light, L, distances);
        }
        else
        {
            // TODO: the shadow code should do it for us. That would be far more efficient.
            float3 sN  = GetNormalForShadowBias(bsdfData);
                   sN *= FastSign(dot(sN, L));

            // This code works for both surface reflection and thin object transmission.
            lightColor.a *= EvaluateShadow_Punctual(lightLoopContext, posInput, light, builtinData, sN, L, distances);
        }

        lightColor.rgb *= lightColor.a; // Composite

        // Simulate a sphere/disk light with this hack.
        // Note that it is not correct with our precomputation of PartLambdaV
        // (means if we disable the optimization it will not have the
        // same result) but we don't care as it is a hack anyway.
        ClampRoughness(bsdfData, light.minRoughness);

        lighting = ShadeSurface_Infinitesimal(preLightData, bsdfData, V, L, lightColor.rgb,
                                              light.diffuseDimmer, light.specularDimmer);
    }

    return lighting;
}
