//-----------------------------------------------------------------------------
// Includes
//-----------------------------------------------------------------------------

#define USE_DIFFUSE_LAMBERT_BRDF

#include "Lit.hlsl"

//-----------------------------------------------------------------------------
// BSDF share between directional light, punctual light and area light (reference)
//-----------------------------------------------------------------------------

// This function apply BSDF. Assumes that NdotL is positive.
void SimpleBSDF(  float3 V, float3 L, float NdotL, float3 positionWS, PreLightData preLightData, BSDFData bsdfData,
            out float3 diffuseLighting,
            out float3 specularLighting)
{
    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateSimpleBSDF().
    diffuseLighting = Lambert();
    specularLighting = 0;

#if HDRP_ENABLE_SPECULAR
    float LdotV, NdotH, LdotH, NdotV, invLenLV;
    GetBSDFAngle(V, L, NdotL, preLightData.NdotV, LdotV, NdotH, LdotH, NdotV, invLenLV);

    float3 F = F_Schlick(bsdfData.fresnel0, LdotH);

    float DV;
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_ANISOTROPY))
    {
        float3 H = (L + V) * invLenLV;

        // For anisotropy we must not saturate these values
        float TdotH = dot(bsdfData.tangentWS, H);
        float TdotL = dot(bsdfData.tangentWS, L);
        float BdotH = dot(bsdfData.bitangentWS, H);
        float BdotL = dot(bsdfData.bitangentWS, L);

        // TODO: Do comparison between this correct version and the one from isotropic and see if there is any visual difference
        DV = DV_SmithJointGGXAniso(TdotH, BdotH, NdotH, NdotV, TdotL, BdotL, NdotL,
                                   bsdfData.roughnessT, bsdfData.roughnessB, preLightData.partLambdaV);
    }
    else
    {
        DV = DV_SmithJointGGX(NdotH, NdotL, NdotV, bsdfData.roughnessT, preLightData.partLambdaV);
    }
    specularLighting = F * DV;
#endif // HDRP_ENABLE_SPECULAR
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Directional
//-----------------------------------------------------------------------------

float3 SimpleEvaluateCookie_Directional(LightLoopContext lightLoopContext, DirectionalLightData lightData,
                                  float3 lightToSample)
{

    // Translate and rotate 'positionWS' into the light space.
    // 'lightData.right' and 'lightData.up' are pre-scaled on CPU.
    float3x3 lightToWorld  = float3x3(lightData.right, lightData.up, lightData.forward);
    float3   positionLS    = mul(lightToSample, transpose(lightToWorld));

    // Perform orthographic projection.
    float2 positionCS    = positionLS.xy;

    // Remap the texture coordinates from [-1, 1]^2 to [0, 1]^2.
    float2 positionNDC = positionCS * 0.5 + 0.5;

    // We let the sampler handle clamping to border.
    return SampleCookie2D(lightLoopContext, positionNDC, lightData.cookieIndex, lightData.tileCookie);
}

// None of the outputs are premultiplied.
// Note: When doing transmission we always have only one shadow sample to do: Either front or back. We use NdotL to know on which side we are
void SimpleEvaluateLight_Directional(LightLoopContext lightLoopContext, PositionInputs posInput,
                               DirectionalLightData lightData, BuiltinData builtinData,
                               float3 N, float3 L,
                               out float3 color, out float attenuation)
{
    float3 positionWS = posInput.positionWS;
    float  shadow     = 1.0;
    float  shadowMask = 1.0;

    color       = lightData.color;
    attenuation = 1.0; // Note: no volumetric attenuation along shadow rays for directional lights

#if HDRP_ENABLE_COOKIE
    UNITY_BRANCH if (lightData.cookieIndex >= 0)
    {
        float3 lightToSample = positionWS - lightData.positionRWS;
        float3 cookie = SimpleEvaluateCookie_Directional(lightLoopContext, lightData, lightToSample);

        color *= cookie;
    }
#endif

#if HDRP_ENABLE_SHADOWS
    // We test NdotL >= 0.0 to not sample the shadow map if it is not required.
    UNITY_BRANCH if (lightData.shadowIndex >= 0 && (dot(N, L) >= 0.0))
    {
#ifdef USE_DEFERRED_DIRECTIONAL_SHADOWS
        shadow = LOAD_TEXTURE2D(_DeferredShadowTexture, posInput.positionSS).x;
#else
        shadow = GetDirectionalShadowAttenuation(lightLoopContext.shadowContext, positionWS, N, lightData.shadowIndex, L, posInput.positionSS);
#endif
    }
#endif // HDRP_ENABLE_SHADOWS

    attenuation *= shadow;
}

DirectLighting SimpleEvaluateBSDF_Directional(LightLoopContext lightLoopContext,
                                        float3 V, PositionInputs posInput, PreLightData preLightData,
                                        DirectionalLightData lightData, BSDFData bsdfData,
                                        BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 L = -lightData.forward;
    float3 N = bsdfData.normalWS;
    float NdotL = dot(N, L);

    float3 transmittance = float3(0.0, 0.0, 0.0);
#if HDRP_ENABLE_TRANSMISSION
    if (HasFlag(bsdfData.materialFeatures, MATERIAL_FEATURE_FLAGS_TRANSMISSION_MODE_THIN_THICKNESS))
    {
        // Caution: This function modify N and contactShadowIndex
        transmittance = PreEvaluateDirectionalLightTransmission(NdotL, lightData, bsdfData, N, lightData.contactShadowIndex); // contactShadowIndex is only modify for the code of this function
    }
#endif

    float3 color;
    float attenuation;
    SimpleEvaluateLight_Directional(lightLoopContext, posInput, lightData, builtinData, N, L, color, attenuation);

    float intensity = max(0, attenuation * NdotL); // Warning: attenuation can be greater than 1 due to the inverse square attenuation (when position is close to light)

    // Note: We use NdotL here to early out, but in case of clear coat this is not correct. But we are ok with this
    UNITY_BRANCH if (intensity > 0.0)
    {
        SimpleBSDF(V, L, NdotL, posInput.positionWS, preLightData, bsdfData, lighting.diffuse, lighting.specular);

        lighting.diffuse  *= intensity * lightData.diffuseScale;
        lighting.specular *= intensity * lightData.specularScale;
    }

#if HDRP_ENABLE_TRANSMISSION
    // The mixed thickness mode is not supported by directional lights due to poor quality and high performance impact.
    if (HasFlag(bsdfData.materialFeatures, MATERIAL_FEATURE_FLAGS_TRANSMISSION_MODE_THIN_THICKNESS))
    {
        float  NdotV = ClampNdotV(preLightData.NdotV);
        float  LdotV = dot(L, V);
        // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
        lighting.diffuse += EvaluateTransmission(bsdfData, transmittance, NdotL, NdotV, LdotV, attenuation * lightData.diffuseScale);
    }
#endif

    // Save ALU by applying light and cookie colors only once.
    lighting.diffuse  *= color;
    lighting.specular *= color;

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        // Only lighting, not BSDF
        lighting.diffuse = color * intensity * lightData.diffuseScale;
    }
#endif

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Punctual (supports spot, point and projector lights)
//-----------------------------------------------------------------------------

float4 SimpleEvaluateCookie_Punctual(LightLoopContext lightLoopContext, LightData lightData,
                               float3 lightToSample)
{
    int lightType = lightData.lightType;

    // Translate and rotate 'positionWS' into the light space.
    // 'lightData.right' and 'lightData.up' are pre-scaled on CPU.
    float3x3 lightToWorld = float3x3(lightData.right, lightData.up, lightData.forward);
    float3   positionLS   = mul(lightToSample, transpose(lightToWorld));

    float4 cookie;

    UNITY_BRANCH if (lightType == GPULIGHTTYPE_POINT)
    {
        cookie.rgb = SampleCookieCube(lightLoopContext, positionLS, lightData.cookieIndex);
        cookie.a   = 1;
    }
    else
    {
        // Perform orthographic or perspective projection.
        float  perspectiveZ = (lightType != GPULIGHTTYPE_PROJECTOR_BOX) ? positionLS.z : 1.0;
        float2 positionCS   = positionLS.xy / perspectiveZ;
        bool   isInBounds   = Max3(abs(positionCS.x), abs(positionCS.y), 1.0 - positionLS.z) <= 1.0;

        // Remap the texture coordinates from [-1, 1]^2 to [0, 1]^2.
        float2 positionNDC = positionCS * 0.5 + 0.5;

        // Manually clamp to border (black).
        cookie.rgb = SampleCookie2D(lightLoopContext, positionNDC, lightData.cookieIndex, false);
        cookie.a   = isInBounds ? 1 : 0;
    }

    return cookie;
}

void SimpleEvaluateLight_Punctual(LightLoopContext lightLoopContext, PositionInputs posInput,
                            LightData lightData, BuiltinData builtinData,
                            float3 N, float3 L, float3 lightToSample, float4 distances,
                            out float3 color, out float attenuation)
{
    float3 positionWS    = posInput.positionWS;
    float  shadow        = 1.0;

    color       = lightData.color;
    attenuation = SmoothPunctualLightAttenuation(distances, lightData.rangeAttenuationScale, lightData.rangeAttenuationBias,
                                                 lightData.angleScale, lightData.angleOffset);

#if HDRP_ENABLE_TRANSMISSION
    // TODO: sample the extinction from the density V-buffer.
    float distVol = (lightData.lightType == GPULIGHTTYPE_PROJECTOR_BOX) ? distances.w : distances.x;
    attenuation *= TransmittanceHomogeneousMedium(_GlobalExtinction, distVol);
#endif

    // Projector lights always have cookies, so we can perform clipping inside the if().
#if HDRP_ENABLE_COOKIE
    UNITY_BRANCH if (lightData.cookieIndex >= 0)
    {
        float4 cookie = SimpleEvaluateCookie_Punctual(lightLoopContext, lightData, lightToSample);

        color       *= cookie.rgb;
        attenuation *= cookie.a;
    }
#endif

#if HDRP_ENABLE_SHADOWS
    // We test NdotL >= 0.0 to not sample the shadow map if it is not required.
    UNITY_BRANCH if (lightData.shadowIndex >= 0 && (dot(N, L) >= 0.0))
    {
        // TODO: make projector lights cast shadows.
        // Note:the case of NdotL < 0 can appear with isThinModeTransmission, in this case we need to flip the shadow bias
        shadow = GetPunctualShadowAttenuation(lightLoopContext.shadowContext, positionWS, N, lightData.shadowIndex, L, distances.x, posInput.positionSS);
        shadow = lerp(1.0, shadow, lightData.shadowDimmer);

        // Transparent have no contact shadow information
#ifndef _SURFACE_TYPE_TRANSPARENT
        shadow = min(shadow, GetContactShadow(lightLoopContext, lightData.contactShadowIndex));
#endif
    }
#endif // HDRP_ENABLE_SHADOWS

    attenuation *= shadow;
}

// This function return transmittance to provide to EvaluateTransmission
float3 SimplePreEvaluatePunctualLightTransmission(LightLoopContext lightLoopContext, PositionInputs posInput, float distFrontFaceToLight,
                                            float NdotL, float3 L, BSDFData bsdfData,
                                            inout float3 normalWS, inout LightData lightData)
{
    float3 transmittance = bsdfData.transmittance;

    // if NdotL is positive, we do one fetch on front face done by EvaluateLight_XXX. Just regular lighting
    // If NdotL is negative, we have two cases:
    // - Thin mode: Reuse the front face fetch as shadow for back face - flip the normal for the bias (and the NdotL test) and disable contact shadow
    // - Mixed mode: Do a fetch on back face to retrieve the thickness. The thickness will provide a shadow attenuation (with distance travelled there is less transmission).
    // (Note: SimpleEvaluateLight_Punctual discard the fetch if NdotL < 0)
    if (NdotL < 0 && lightData.shadowIndex >= 0)
    {
        if (HasFlag(bsdfData.materialFeatures, MATERIAL_FEATURE_FLAGS_TRANSMISSION_MODE_THIN_THICKNESS))
        {
            normalWS = -normalWS; // Flip normal for shadow bias
            lightData.contactShadowIndex = -1;  //  Disable shadow contact
        }
        else // MATERIAL_FEATURE_FLAGS_TRANSMISSION_MODE_MIXED_THICKNESS
        {
            // Recompute transmittance using the thickness value computed from the shadow map.

            // Compute the distance from the light to the back face of the object along the light direction.
            float distBackFaceToLight = GetPunctualShadowClosestDistance(   lightLoopContext.shadowContext, s_linear_clamp_sampler,
                                                                            posInput.positionWS, lightData.shadowIndex, L, lightData.positionRWS);

            // Our subsurface scattering models use the semi-infinite planar slab assumption.
            // Therefore, we need to find the thickness along the normal.
            // Warning: based on the artist's input, dependence on the NdotL has been disabled.
            float thicknessInUnits = (distFrontFaceToLight - distBackFaceToLight) /* * -NdotL */;
            float thicknessInMeters = thicknessInUnits * _WorldScales[bsdfData.diffusionProfile].x;
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

            // Note: we do not modify the distance to the light, or the light angle for the back face.
            // This is a performance-saving optimization which makes sense as long as the thickness is small.
        }
        
        transmittance = lerp( bsdfData.transmittance, transmittance, lightData.shadowDimmer);
    }

    return transmittance;
}

DirectLighting SimpleEvaluateBSDF_Punctual(LightLoopContext lightLoopContext,
                                     float3 V, PositionInputs posInput,
                                     PreLightData preLightData, LightData lightData, BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 L;
    float3 lightToSample;
    float4 distances; // {d, d^2, 1/d, d_proj}
    GetPunctualLightVectors(posInput.positionWS, lightData, L, lightToSample, distances);

    float3 N     = bsdfData.normalWS;
    float  NdotL = dot(N, L);

    float3 transmittance = float3(0.0, 0.0, 0.0);
#if HDRP_ENABLE_TRANSMISSION
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_TRANSMISSION))
    {
        // Caution: This function modify N and lightData.contactShadowIndex
        transmittance = SimplePreEvaluatePunctualLightTransmission(lightLoopContext, posInput, distances.x, NdotL, L, bsdfData, N, lightData);
    }
#endif

    float3 color;
    float attenuation;
    SimpleEvaluateLight_Punctual(lightLoopContext, posInput, lightData, builtinData, N, L,
                           lightToSample, distances, color, attenuation);

    float intensity = max(0, attenuation * NdotL); // Warning: attenuation can be greater than 1 due to the inverse square attenuation (when position is close to light)

    // Note: We use NdotL here to early out, but in case of clear coat this is not correct. But we are ok with this
    UNITY_BRANCH if (intensity > 0.0)
    {
        // Simulate a sphere light with this hack
        // Note that it is not correct with our pre-computation of PartLambdaV (mean if we disable the optimization we will not have the
        // same result) but we don't care as it is a hack anyway
        bsdfData.coatRoughness = max(bsdfData.coatRoughness, lightData.minRoughness);
        bsdfData.roughnessT = max(bsdfData.roughnessT, lightData.minRoughness);
        bsdfData.roughnessB = max(bsdfData.roughnessB, lightData.minRoughness);

        SimpleBSDF(V, L, NdotL, posInput.positionWS, preLightData, bsdfData, lighting.diffuse, lighting.specular);

        lighting.diffuse  *= intensity * lightData.diffuseScale;
        lighting.specular *= intensity * lightData.specularScale;
    }

#if HDRP_ENABLE_TRANSMISSION
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_TRANSMISSION))
    {
        float  NdotV = ClampNdotV(preLightData.NdotV);
        float  LdotV = dot(L, V);
        // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
        lighting.diffuse += EvaluateTransmission(bsdfData, transmittance, NdotL, NdotV, LdotV, attenuation * lightData.diffuseScale);
    }
#endif

    // Save ALU by applying light and cookie colors only once.
    lighting.diffuse  *= color;
    lighting.specular *= color;

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        // Only lighting, not BSDF
        lighting.diffuse = color * intensity * lightData.diffuseScale;
    }
#endif

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Env
// ----------------------------------------------------------------------------

void SimpleEvaluateLight_EnvIntersection(float3 positionWS, float3 normalWS, EnvLightData lightData, int influenceShapeType, inout float3 R, inout float weight)
{
    // Guideline for reflection volume: In HDRenderPipeline we separate the projection volume (the proxy of the scene) from the influence volume (what pixel on the screen is affected)
    // However we add the constrain that the shape of the projection and influence volume is the same (i.e if we have a sphere shape projection volume, we have a shape influence).
    // It allow to have more coherence for the dynamic if in shader code.
    // Users can also chose to not have any projection, in this case we use the property minProjectionDistance to minimize code change. minProjectionDistance is set to huge number
    // that simulate effect of no shape projection

    float3x3 worldToIS = WorldToInfluenceSpace(lightData); // IS: Influence space
    float3 positionIS = WorldToInfluencePosition(lightData, worldToIS, positionWS);
    float3 dirIS = mul(R, worldToIS);

    // Process the projection
    // In Unity the cubemaps are capture with the localToWorld transform of the component.
    // This mean that location and orientation matter. So after intersection of proxy volume we need to convert back to world.
    if (influenceShapeType == ENVSHAPETYPE_SPHERE)
    {
        weight = InfluenceSphereWeight(lightData, normalWS, positionWS, positionIS, dirIS);
    }
    else if (influenceShapeType == ENVSHAPETYPE_BOX)
    {
        weight = InfluenceBoxWeight(lightData, normalWS, positionWS, positionIS, dirIS);
    }

    // Smooth weighting
    weight = Smoothstep01(weight);
    weight *= lightData.weight;
}

// _preIntegratedFGD and _CubemapLD are unique for each BRDF
IndirectLighting SimpleEvaluateBSDF_Env(  LightLoopContext lightLoopContext,
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

    SimpleEvaluateLight_EnvIntersection(posInput.positionWS, bsdfData.normalWS, lightData, influenceShapeType, R, weight);

    float iblMipLevel;
    // TODO: We need to match the PerceptualRoughnessToMipmapLevel formula for planar, so we don't do this test (which is specific to our current lightloop)
    // Specific case for Texture2Ds, their convolution is a gaussian one and not a GGX one - So we use another roughness mip mapping.
#if !defined(SHADER_API_METAL)
    if (IsEnvIndexTexture2D(lightData.envIndex))
    {
        // Empirical remapping
        iblMipLevel = PlanarPerceptualRoughnessToMipmapLevel(preLightData.iblPerceptualRoughness, _ColorPyramidScale.z);
    }
    else
#endif
    {
        iblMipLevel = PerceptualRoughnessToMipmapLevel(preLightData.iblPerceptualRoughness);
    }

    float4 preLD = SampleEnv(lightLoopContext, lightData.envIndex, R, iblMipLevel);
    weight *= preLD.a; // Used by planar reflection to discard pixel

    envLighting = F_Schlick(bsdfData.fresnel0, dot(bsdfData.normalWS, V)) * preLD.rgb;

    UpdateLightingHierarchyWeights(hierarchyWeight, weight);
    envLighting *= weight * lightData.multiplier;

    lighting.specularReflected = envLighting;

    return lighting;
}
