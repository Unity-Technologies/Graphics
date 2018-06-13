// This files include various function uses to evaluate material

//-----------------------------------------------------------------------------
// Lighting structure for light accumulation
//-----------------------------------------------------------------------------

// These structure allow to accumulate lighting accross the Lit material
// AggregateLighting is init to zero and transfer to EvaluateBSDF, but the LightLoop can't access its content.
struct DirectLighting
{
    float3 diffuse;
    float3 specular;
};

struct IndirectLighting
{
    float3 specularReflected;
    float3 specularTransmitted;
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
    float3 indirectAmbientOcclusion;
    float3 directAmbientOcclusion;
    float3 indirectSpecularOcclusion;
};

void GetScreenSpaceAmbientOcclusion(float2 positionSS, float NdotV, float perceptualRoughness, float ambientOcclusionFromData, float specularOcclusionFromData, out AmbientOcclusionFactor aoFactor)
{
    // Note: When we ImageLoad outside of texture size, the value returned by Load is 0 (Note: On Metal maybe it clamp to value of texture which is also fine)
    // We use this property to have a neutral value for AO that doesn't consume a sampler and work also with compute shader (i.e use ImageLoad)
    // We store inverse AO so neutral is black. So either we sample inside or outside the texture it return 0 in case of neutral

    // Ambient occlusion use for indirect lighting (reflection probe, baked diffuse lighting)
#ifndef _SURFACE_TYPE_TRANSPARENT
    float indirectAmbientOcclusion = 1.0 - LOAD_TEXTURE2D(_AmbientOcclusionTexture, positionSS).x;
    // Ambient occlusion use for direct lighting (directional, punctual, area)
    float directAmbientOcclusion = lerp(1.0, indirectAmbientOcclusion, _AmbientOcclusionParam.w);
#else
    float indirectAmbientOcclusion = 1.0;
    float directAmbientOcclusion = 1.0;
#endif

    float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    float specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(NdotV), indirectAmbientOcclusion, roughness);

    aoFactor.indirectSpecularOcclusion = lerp(_AmbientOcclusionParam.rgb, float3(1.0, 1.0, 1.0), min(specularOcclusionFromData, specularOcclusion));
    aoFactor.indirectAmbientOcclusion = lerp(_AmbientOcclusionParam.rgb, float3(1.0, 1.0, 1.0), min(ambientOcclusionFromData, indirectAmbientOcclusion));
    aoFactor.directAmbientOcclusion = lerp(_AmbientOcclusionParam.rgb, float3(1.0, 1.0, 1.0), directAmbientOcclusion);
}

void GetScreenSpaceAmbientOcclusionMultibounce(float2 positionSS, float NdotV, float perceptualRoughness, float ambientOcclusionFromData, float specularOcclusionFromData, float3 diffuseColor, float3 fresnel0, out AmbientOcclusionFactor aoFactor)
{
    // Use GTAOMultiBounce approximation for ambient occlusion (allow to get a tint from the diffuseColor)
    // Note: When we ImageLoad outside of texture size, the value returned by Load is 0 (Note: On Metal maybe it clamp to value of texture which is also fine)
    // We use this property to have a neutral value for AO that doesn't consume a sampler and work also with compute shader (i.e use ImageLoad)
    // We store inverse AO so neutral is black. So either we sample inside or outside the texture it return 0 in case of neutral

    // Ambient occlusion use for indirect lighting (reflection probe, baked diffuse lighting)
#ifndef _SURFACE_TYPE_TRANSPARENT
    float indirectAmbientOcclusion = 1.0 - LOAD_TEXTURE2D(_AmbientOcclusionTexture, positionSS).x;
    // Ambient occlusion use for direct lighting (directional, punctual, area)
    float directAmbientOcclusion = lerp(1.0, indirectAmbientOcclusion, _AmbientOcclusionParam.w);
#else
    float indirectAmbientOcclusion = 1.0;
    float directAmbientOcclusion = 1.0;
#endif

    float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    float specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(NdotV), indirectAmbientOcclusion, roughness);

    aoFactor.indirectSpecularOcclusion = GTAOMultiBounce(min(specularOcclusionFromData, specularOcclusion), fresnel0);
    aoFactor.indirectAmbientOcclusion = GTAOMultiBounce(min(ambientOcclusionFromData, indirectAmbientOcclusion), diffuseColor);
    aoFactor.directAmbientOcclusion = GTAOMultiBounce(directAmbientOcclusion, diffuseColor);
}

void ApplyAmbientOcclusionFactor(AmbientOcclusionFactor aoFactor, inout BakeLightingData bakeLightingData, inout AggregateLighting lighting)
{
    // Note: in case of Lit, bakeLightingData.bakeDiffuseLighting contain indirect diffuse + emissive,
    // so Ambient occlusion is multiply by emissive which is wrong but not a big deal
    bakeLightingData.bakeDiffuseLighting *= aoFactor.indirectAmbientOcclusion;
    lighting.indirect.specularReflected *= aoFactor.indirectSpecularOcclusion;
    lighting.direct.diffuse *= aoFactor.directAmbientOcclusion;
}

#ifdef DEBUG_DISPLAY
// mipmapColor is color use to store texture streaming information in XXXData.hlsl (look for DEBUGMIPMAPMODE_NONE)
void PostEvaluateBSDFDebugDisplay(  AmbientOcclusionFactor aoFactor, BakeLightingData bakeLightingData, AggregateLighting lighting, float3 mipmapColor,
                                    inout float3 diffuseLighting, inout float3 specularLighting)
{
    if (_DebugLightingMode != 0)
    {
        // Caution: _DebugLightingMode is used in other part of the code, don't do anything outside of
        // current cases
        switch (_DebugLightingMode)
        {
        case DEBUGLIGHTINGMODE_LUX_METER:
            diffuseLighting = lighting.direct.diffuse + bakeLightingData.bakeDiffuseLighting;

            //Compress lighting values for color picker if enabled
            if (_ColorPickerMode != COLORPICKERDEBUGMODE_NONE)
                diffuseLighting = diffuseLighting / LUXMETER_COMPRESSION_RATIO;
            
            specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
            break;

        case DEBUGLIGHTINGMODE_INDIRECT_DIFFUSE_OCCLUSION:
            diffuseLighting = aoFactor.indirectAmbientOcclusion;
            specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
            break;

        case DEBUGLIGHTINGMODE_INDIRECT_SPECULAR_OCCLUSION:
            diffuseLighting = aoFactor.indirectSpecularOcclusion;
            specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
            break;

        case DEBUGLIGHTINGMODE_SCREEN_SPACE_TRACING_REFRACTION:
            if (_DebugLightingSubMode != DEBUGSCREENSPACETRACING_COLOR)
                diffuseLighting = lighting.indirect.specularTransmitted;
            break;

        case DEBUGLIGHTINGMODE_SCREEN_SPACE_TRACING_REFLECTION:
            if (_DebugLightingSubMode != DEBUGSCREENSPACETRACING_COLOR)
                diffuseLighting = lighting.indirect.specularReflected;
            break;

        case DEBUGLIGHTINGMODE_VISUALIZE_SHADOW_MASKS:
            #ifdef SHADOWS_SHADOWMASK
            diffuseLighting = float3(
                bakeLightingData.bakeShadowMask.r / 2 + bakeLightingData.bakeShadowMask.g / 2,
                bakeLightingData.bakeShadowMask.g / 2 + bakeLightingData.bakeShadowMask.b / 2,
                bakeLightingData.bakeShadowMask.b / 2 + bakeLightingData.bakeShadowMask.a / 2
            );
            specularLighting = float3(0, 0, 0);
            #endif
            break ;
        }
    }
    else if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
    {
        diffuseLighting = mipmapColor;
        specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
    }
}
#endif
