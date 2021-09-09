// This files include various function uses to evaluate material

//-----------------------------------------------------------------------------
// Lighting structure for light accumulation
//-----------------------------------------------------------------------------

// These structure allow to accumulate lighting accross the Lit material
// AggregateLighting is init to zero and transfer to EvaluateBSDF, but the LightLoop can't access its content.
struct DirectLighting
{
    real3 diffuse;
    real3 specular;
};

struct IndirectLighting
{
    real3 specularReflected;
    real3 specularTransmitted;
};

struct AggregateLighting
{
    DirectLighting   direct;
    IndirectLighting indirect;
};

void AccumulateDirectLighting(DirectLighting src, inout AggregateLighting dst)
{
    dst.direct.diffuse += src.diffuse;
    dst.direct.specular += src.specular;
}

void AccumulateIndirectLighting(IndirectLighting src, inout AggregateLighting dst)
{
    dst.indirect.specularReflected += src.specularReflected;
    dst.indirect.specularTransmitted += src.specularTransmitted;
}

//-----------------------------------------------------------------------------
// Ambient occlusion helper
//-----------------------------------------------------------------------------

// Ambient occlusion
struct AmbientOcclusionFactor
{
    real3 indirectAmbientOcclusion;
    real3 directAmbientOcclusion;
    real3 indirectSpecularOcclusion;
    real3 directSpecularOcclusion;
};

// Get screen space ambient occlusion only:
float GetScreenSpaceDiffuseOcclusion(float2 positionSS)
{
    #if (SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT) || (SHADERPASS == SHADERPASS_RAYTRACING_FORWARD)
        // When we are in raytracing mode, we do not want to take the screen space computed AO texture
        float indirectAmbientOcclusion = 1.0;
    #else
        // Note: When we ImageLoad outside of texture size, the value returned by Load is 0 (Note: On Metal maybe it clamp to value of texture which is also fine)
        // We use this property to have a neutral value for AO that doesn't consume a sampler and work also with compute shader (i.e use ImageLoad)
        // We store inverse AO so neutral is black. So either we sample inside or outside the texture it return 0 in case of neutral
        // Ambient occlusion use for indirect lighting (reflection probe, baked diffuse lighting)
        #ifndef _SURFACE_TYPE_TRANSPARENT
        float indirectAmbientOcclusion = 1.0 - LOAD_TEXTURE2D_X(_AmbientOcclusionTexture, positionSS).x;
        #else
        float indirectAmbientOcclusion = 1.0;
        #endif
    #endif

    return indirectAmbientOcclusion;
}

float3 GetScreenSpaceAmbientOcclusion(float2 positionSS)
{
    float indirectAmbientOcclusion = GetScreenSpaceDiffuseOcclusion(positionSS);
    return lerp(_AmbientOcclusionParam.rgb, float3(1.0, 1.0, 1.0), indirectAmbientOcclusion);
}

void GetScreenSpaceAmbientOcclusion(float2 positionSS, float NdotV, float perceptualRoughness, float ambientOcclusionFromData, float specularOcclusionFromData, out AmbientOcclusionFactor aoFactor)
{
    float indirectAmbientOcclusion = GetScreenSpaceDiffuseOcclusion(positionSS);
    float directAmbientOcclusion = lerp(1.0, indirectAmbientOcclusion, _AmbientOcclusionParam.w);

    float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    float indirectSpecularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(NdotV), indirectAmbientOcclusion, roughness);
    float directSpecularOcclusion = lerp(1.0, indirectSpecularOcclusion, _AmbientOcclusionParam.w);

    aoFactor.indirectSpecularOcclusion = lerp(_AmbientOcclusionParam.rgb, float3(1.0, 1.0, 1.0), min(specularOcclusionFromData, indirectSpecularOcclusion));
    aoFactor.indirectAmbientOcclusion = lerp(_AmbientOcclusionParam.rgb, float3(1.0, 1.0, 1.0), min(ambientOcclusionFromData, indirectAmbientOcclusion));
    aoFactor.directSpecularOcclusion = lerp(_AmbientOcclusionParam.rgb, float3(1.0, 1.0, 1.0), directSpecularOcclusion);
    aoFactor.directAmbientOcclusion = lerp(_AmbientOcclusionParam.rgb, float3(1.0, 1.0, 1.0), directAmbientOcclusion);
}

// Use GTAOMultiBounce approximation for ambient occlusion (allow to get a tint from the diffuseColor)
void GetScreenSpaceAmbientOcclusionMultibounce(float2 positionSS, float NdotV, float perceptualRoughness, float ambientOcclusionFromData, float specularOcclusionFromData, float3 diffuseColor, float3 fresnel0, out AmbientOcclusionFactor aoFactor)
{
    float indirectAmbientOcclusion = GetScreenSpaceDiffuseOcclusion(positionSS);
    float directAmbientOcclusion = lerp(1.0, indirectAmbientOcclusion, _AmbientOcclusionParam.w);

    float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
	// This specular occlusion formulation make sense only with SSAO. When we use Raytracing AO we support different range (local, medium, sky). When using medium or
	// sky occlusion, the result on specular occlusion can be a disaster (all is black). Thus we use _SpecularOcclusionBlend when using RTAO to disable this trick.
    float indirectSpecularOcclusion = lerp(1.0, GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(NdotV), indirectAmbientOcclusion, roughness), _SpecularOcclusionBlend);
    float directSpecularOcclusion = lerp(1.0, indirectSpecularOcclusion, _AmbientOcclusionParam.w);

    aoFactor.indirectSpecularOcclusion = GTAOMultiBounce(min(specularOcclusionFromData, indirectSpecularOcclusion), fresnel0);
    aoFactor.indirectAmbientOcclusion = GTAOMultiBounce(min(ambientOcclusionFromData, indirectAmbientOcclusion), diffuseColor);
    aoFactor.directSpecularOcclusion = GTAOMultiBounce(directSpecularOcclusion, fresnel0);
    aoFactor.directAmbientOcclusion = GTAOMultiBounce(directAmbientOcclusion, diffuseColor);
}

void ApplyAmbientOcclusionFactor(AmbientOcclusionFactor aoFactor, inout BuiltinData builtinData, inout AggregateLighting lighting)
{
    // Note: In case of deferred Lit, builtinData.bakeDiffuseLighting contains indirect diffuse * surfaceData.ambientOcclusion + emissive,
    // so SSAO is multiplied by emissive which is wrong.
    // Also, we have double occlusion for diffuse lighting since it already had precomputed AO (aka "FromData") applied
    // (the * surfaceData.ambientOcclusion above)
    // This is a tradeoff to avoid storing the precomputed (from data) AO in the GBuffer.
    // (This is also why GetScreenSpaceAmbientOcclusion*() is effectively called with AOFromData = 1.0 in Lit:PostEvaluateBSDF() in the
    // deferred case since DecodeFromGBuffer will init bsdfData.ambientOcclusion to 1.0 and we will only have SSAO in the aoFactor here)
    builtinData.bakeDiffuseLighting *= aoFactor.indirectAmbientOcclusion;
    lighting.indirect.specularReflected *= aoFactor.indirectSpecularOcclusion;
    lighting.direct.diffuse *= aoFactor.directAmbientOcclusion;
    lighting.direct.specular *= aoFactor.directSpecularOcclusion;
}

#if defined(DEBUG_DISPLAY) && defined(HAS_LIGHTLOOP) && !defined(_ENABLE_SHADOW_MATTE)
// mipmapColor is color use to store texture streaming information in XXXData.hlsl (look for DEBUGMIPMAPMODE_NONE)
void PostEvaluateBSDFDebugDisplay(  AmbientOcclusionFactor aoFactor, BuiltinData builtinData, AggregateLighting lighting, float3 mipmapColor,
                                    inout LightLoopOutput lightLoopOutput)
{
    if (_DebugShadowMapMode != SHADOWMAPDEBUGMODE_NONE)
    {
        switch (_DebugShadowMapMode)
        {
        case SHADOWMAPDEBUGMODE_SINGLE_SHADOW:
            lightLoopOutput.diffuseLighting = g_DebugShadowAttenuation.xxx;
            lightLoopOutput.specularLighting = float3(0, 0, 0);
            break ;
        }
    }
    if (_DebugLightingMode != DEBUGLIGHTINGMODE_NONE)
    {
        // Caution: _DebugLightingMode is used in other part of the code, don't do anything outside of
        // current cases
        switch (_DebugLightingMode)
        {
        case DEBUGLIGHTINGMODE_LUX_METER:
            // Note: We don't include emissive here (and in deferred it is correct as lux calculation of bakeDiffuseLighting don't consider emissive)
            lightLoopOutput.diffuseLighting = lighting.direct.diffuse + builtinData.bakeDiffuseLighting;

            //Compress lighting values for color picker if enabled
            if (_ColorPickerMode != COLORPICKERDEBUGMODE_NONE)
                lightLoopOutput.diffuseLighting = lightLoopOutput.diffuseLighting / LUXMETER_COMPRESSION_RATIO;

            lightLoopOutput.specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
            break;

        case DEBUGLIGHTINGMODE_INDIRECT_DIFFUSE_OCCLUSION:
            lightLoopOutput.diffuseLighting = aoFactor.indirectAmbientOcclusion;
            lightLoopOutput.specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
            break;

        case DEBUGLIGHTINGMODE_INDIRECT_SPECULAR_OCCLUSION:
            lightLoopOutput.diffuseLighting = aoFactor.indirectSpecularOcclusion;
            lightLoopOutput.specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
            break;

        case DEBUGLIGHTINGMODE_VISUALIZE_SHADOW_MASKS:
            #ifdef SHADOWS_SHADOWMASK
            lightLoopOutput.diffuseLighting = float3(
                builtinData.shadowMask0 / 2 + builtinData.shadowMask1 / 2,
                builtinData.shadowMask1 / 2 + builtinData.shadowMask2 / 2,
                builtinData.shadowMask2 / 2 + builtinData.shadowMask3 / 2
            );
            lightLoopOutput.specularLighting = float3(0, 0, 0);
            #endif
            break ;

        case DEBUGLIGHTINGMODE_PROBE_VOLUME:
            lightLoopOutput.diffuseLighting = builtinData.bakeDiffuseLighting;
            lightLoopOutput.specularLighting = float3(0, 0, 0);
            break;
        }
    }
    else if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
    {
        lightLoopOutput.diffuseLighting = mipmapColor;
        lightLoopOutput.specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
    }
    else if (_DebugProbeVolumeMode != PROBEVOLUMEDEBUGMODE_NONE)
    {
        switch (_DebugProbeVolumeMode)
        {
        case PROBEVOLUMEDEBUGMODE_VISUALIZE_DEBUG_COLORS:
        case PROBEVOLUMEDEBUGMODE_VISUALIZE_VALIDITY:
            lightLoopOutput.diffuseLighting = builtinData.bakeDiffuseLighting;
            lightLoopOutput.specularLighting = float3(0.0, 0.0, 0.0);
            break;
        }
    }
}
#endif
