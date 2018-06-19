// This must be include after SubsurfaceScattering.hlsl

// This files include various helper function to easily setup SSS and transmission inside a material. It require that the material follow naming convention
// User can request either SSS function, or Transmission, or both, they need to define INCLUDE_SUBSURFACESCATTERING and/or INCLUDE_TRANSMISSION
// And define the lighting model to use for transmission. By default it is Lambert. For Disney: TRANSMISSION_DISNEY_DIFFUSE_BRDF
// Also user need to be sure that upper 16bit of bsdfData.materialFeatures are not used

// Additional bits set in 'bsdfData.materialFeatures' to save registers and simplify feature tracking.
#define MATERIAL_FEATURE_SSS_TRANSMISSION_START (1 << 16) // It should be safe to start these flags

#define MATERIAL_FEATURE_FLAGS_SSS_OUTPUT_SPLIT_LIGHTING         ((MATERIAL_FEATURE_SSS_TRANSMISSION_START) << 0)
#define MATERIAL_FEATURE_FLAGS_SSS_TEXTURING_MODE_OFFSET FastLog2((MATERIAL_FEATURE_SSS_TRANSMISSION_START) << 1) // 2 bits
#define MATERIAL_FEATURE_FLAGS_TRANSMISSION_MODE_MIXED_THICKNESS ((MATERIAL_FEATURE_SSS_TRANSMISSION_START) << 3)
// Flags used as a shortcut to know if we have thin mode transmission
#define MATERIAL_FEATURE_FLAGS_TRANSMISSION_MODE_THIN_THICKNESS  ((MATERIAL_FEATURE_SSS_TRANSMISSION_START) << 4)

#ifdef INCLUDE_SUBSURFACESCATTERING
// Assume that bsdfData.diffusionProfile is init
void FillMaterialSSS(uint diffusionProfile, float subsurfaceMask, inout BSDFData bsdfData)
{
    bsdfData.diffusionProfile = diffusionProfile;
    bsdfData.fresnel0 = _TransmissionTintsAndFresnel0[diffusionProfile].a;
    bsdfData.subsurfaceMask = subsurfaceMask;
    bsdfData.materialFeatures |= MATERIAL_FEATURE_FLAGS_SSS_OUTPUT_SPLIT_LIGHTING;
    bsdfData.materialFeatures |= GetSubsurfaceScatteringTexturingMode(bsdfData.diffusionProfile) << MATERIAL_FEATURE_FLAGS_SSS_TEXTURING_MODE_OFFSET;
}

bool ShouldOutputSplitLighting(BSDFData bsdfData)
{
    return HasFeatureFlag(bsdfData.materialFeatures, MATERIAL_FEATURE_FLAGS_SSS_OUTPUT_SPLIT_LIGHTING);
}

float3 SSSGetModifiedDiffuseColor(BSDFData bsdfData)
{
    // Subsurface scattering mdoe
    uint   texturingMode = (bsdfData.materialFeatures >> MATERIAL_FEATURE_FLAGS_SSS_TEXTURING_MODE_OFFSET) & 3;
    return ApplySubsurfaceScatteringTexturingMode(texturingMode, bsdfData.diffuseColor);
}

#endif

#ifdef INCLUDE_TRANSMISSION

// Assume that bsdfData.diffusionProfile is init
void FillMaterialTransmission(uint diffusionProfile, float thickness, inout BSDFData bsdfData)
{
    bsdfData.diffusionProfile = diffusionProfile;
    bsdfData.fresnel0 = _TransmissionTintsAndFresnel0[diffusionProfile].a;

    bsdfData.thickness = _ThicknessRemaps[diffusionProfile].x + _ThicknessRemaps[diffusionProfile].y * thickness;

    // The difference between the thin and the regular (a.k.a. auto-thickness) modes is the following:
    // * in the thin object mode, we assume that the geometry is thin enough for us to safely share
    // the shadowing information between the front and the back faces;
    // * the thin mode uses baked (textured) thickness for all transmission calculations;
    // * the thin mode uses wrapped diffuse lighting for the NdotL;
    // * the auto-thickness mode uses the baked (textured) thickness to compute transmission from
    // indirect lighting and non-shadow-casting lights; for shadowed lights, it calculates
    // the thickness using the distance to the closest occluder sampled from the shadow map.
    // If the distance is large, it may indicate that the closest occluder is not the back face of
    // the current object. That's not a problem, since large thickness will result in low intensity.
    bool useThinObjectMode = IsBitSet(asuint(_TransmissionFlags), diffusionProfile);

    bsdfData.materialFeatures |= useThinObjectMode ? MATERIAL_FEATURE_FLAGS_TRANSMISSION_MODE_THIN_THICKNESS : MATERIAL_FEATURE_FLAGS_TRANSMISSION_MODE_MIXED_THICKNESS;

    // Compute transmittance using baked thickness here. It may be overridden for direct lighting
    // in the auto-thickness mode (but is always used for indirect lighting).
#if SHADEROPTIONS_USE_DISNEY_SSS
    bsdfData.transmittance = ComputeTransmittanceDisney(_ShapeParams[diffusionProfile].rgb,
                                                        _TransmissionTintsAndFresnel0[diffusionProfile].rgb,
                                                        bsdfData.thickness);
#else
    bsdfData.transmittance = ComputeTransmittanceJimenez(_HalfRcpVariancesAndWeights[diffusionProfile][0].rgb,
                                                         _HalfRcpVariancesAndWeights[diffusionProfile][0].a,
                                                         _HalfRcpVariancesAndWeights[diffusionProfile][1].rgb,
                                                         _HalfRcpVariancesAndWeights[diffusionProfile][1].a,
                                                         _TransmissionTintsAndFresnel0[diffusionProfile].rgb,
                                                         bsdfData.thickness);
#endif
}

#ifdef HAS_LIGHTLOOP

#define SSS_WRAP_ANGLE (PI/12)              // 15 degrees
#define SSS_WRAP_LIGHT cos(PI/2 - SSS_WRAP_ANGLE)

// Currently, we only model diffuse transmission. Specular transmission is not yet supported.
// Transmitted lighting is computed as follows:
// - we assume that the object is a thick plane (slab);
// - we reverse the front-facing normal for the back of the object;
// - we assume that the incoming radiance is constant along the entire back surface;
// - we apply BSDF-specific diffuse transmission to transmit the light subsurface and back;
// - we integrate the diffuse reflectance profile w.r.t. the radius (while also accounting
//   for the thickness) to compute the transmittance;
// - we multiply the transmitted radiance by the transmittance.
float3 EvaluateTransmission(BSDFData bsdfData, float3 transmittance, float NdotL, float NdotV, float LdotV, float attenuation)
{
    // Apply wrapped lighting to better handle thin objects at grazing angles.
    float wrappedNdotL = ComputeWrappedDiffuseLighting(-NdotL, SSS_WRAP_LIGHT);

    // Apply BSDF-specific diffuse transmission to attenuation. See also: [SSS-NOTE-TRSM]
    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
#ifdef TRANSMISSION_DISNEY_DIFFUSE_BRDF
    attenuation *= DisneyDiffuse(NdotV, max(0, -NdotL), LdotV, bsdfData.perceptualRoughness);
#else
    attenuation *= Lambert();
#endif

    float intensity = attenuation * wrappedNdotL;
    return intensity * transmittance;
}

void PreEvaluateLightTransmission(float NdotL, BSDFData bsdfData, inout float3 normalWS, inout float contactShadowIndex)
{
    // When using thin transmission mode we don't fetch shadow map for back face, we reuse front face shadow
    // However we flip the normal for the bias (and the NdotL test) and disable contact shadow
    if (HasFeatureFlag(bsdfData.materialFeatures, MATERIAL_FEATURE_FLAGS_TRANSMISSION_MODE_THIN_THICKNESS) && NdotL < 0)
    {
        //  Disable shadow contact in case of transmission and backface shadow
        normalWS = -normalWS;
        contactShadowIndex = -1;
    }
}

float3 PostEvaluateLightTransmission(   ShadowContext shadowContext, PositionInputs posInput, float distFrontFaceToLight,
                                        float NdotL, float NdotV, float LdotV, float3 L, float attenuation, LightData lightData, BSDFData bsdfData)
{
    float3 transmittance = bsdfData.transmittance;

    // Note that if NdotL is positive, we have one fetch on front face done by EvaluateLight_Punctual, otherwise we have only one fetch
    // done by transmission code here (EvaluateLight_Punctual discard the fetch if NdotL < 0)
    bool mixedThicknessMode =   HasFeatureFlag(bsdfData.materialFeatures, MATERIAL_FEATURE_FLAGS_TRANSMISSION_MODE_MIXED_THICKNESS)
                                && NdotL < 0 && lightData.shadowIndex >= 0;

    if (mixedThicknessMode)
    {
        // Recompute transmittance using the thickness value computed from the shadow map.

        // Compute the distance from the light to the back face of the object along the light direction.
        float distBackFaceToLight = GetPunctualShadowClosestDistance(shadowContext, s_linear_clamp_sampler,
                                                                     posInput.positionWS, lightData.shadowIndex, L, lightData.positionWS);

        // Our subsurface scattering models use the semi-infinite planar slab assumption.
        // Therefore, we need to find the thickness along the normal.
        float thicknessInUnits       = (distFrontFaceToLight - distBackFaceToLight) * -NdotL;
        float thicknessInMeters      = thicknessInUnits * _WorldScales[bsdfData.diffusionProfile].x;
        float thicknessInMillimeters = thicknessInMeters * MILLIMETERS_PER_METER;

    #if SHADEROPTIONS_USE_DISNEY_SSS
        // We need to make sure it's not less than the baked thickness to minimize light leaking.
        float thicknessDelta = max(0, thicknessInMillimeters - bsdfData.thickness);

        float3 S = _ShapeParams[bsdfData.diffusionProfile].rgb;

        // Approximate the decrease of transmittance by e^(-1/3 * dt * S).
    #if 0
        float3 expOneThird = exp(((-1.0 / 3.0) * thicknessDelta) * S);
    #else
        // Help the compiler.
        float  k = (-1.0 / 3.0) * LOG2_E;
        float3 p = (k * thicknessDelta) * S;
        float3 expOneThird = exp2(p);
    #endif

        transmittance *= expOneThird;

    #else // SHADEROPTIONS_USE_DISNEY_SSS

        // We need to make sure it's not less than the baked thickness to minimize light leaking.
        thicknessInMillimeters = max(thicknessInMillimeters, bsdfData.thickness);

        transmittance = ComputeTransmittanceJimenez(_HalfRcpVariancesAndWeights[bsdfData.diffusionProfile][0].rgb,
                                                    _HalfRcpVariancesAndWeights[bsdfData.diffusionProfile][0].a,
                                                    _HalfRcpVariancesAndWeights[bsdfData.diffusionProfile][1].rgb,
                                                    _HalfRcpVariancesAndWeights[bsdfData.diffusionProfile][1].a,
                                                    _TransmissionTintsAndFresnel0[bsdfData.diffusionProfile].rgb,
                                                    thicknessInMillimeters);
    #endif // SHADEROPTIONS_USE_DISNEY_SSS
    }

    // Note: we do not modify the distance to the light, or the light angle for the back face.
    // This is a performance-saving optimization which makes sense as long as the thickness is small.
    return EvaluateTransmission(bsdfData, transmittance, NdotL, NdotV, LdotV, attenuation);
}

#endif // HAS_LIGHTLOOP

#endif

