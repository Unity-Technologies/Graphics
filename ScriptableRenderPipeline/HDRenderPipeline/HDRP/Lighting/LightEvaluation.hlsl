// This files include various function uses to evaluate lights
// To use deferred directional shadow with cascaded shadow map, 
// it is required to define USE_DEFERRED_DIRECTIONAL_SHADOWS before including this files

//-----------------------------------------------------------------------------
// Directional Light evaluation helper
//-----------------------------------------------------------------------------

float3 EvaluateCookie_Directional(LightLoopContext lightLoopContext, DirectionalLightData lightData,
                                  float3 positionWS)
{

    // Translate and rotate 'positionWS' into the light space.
    // 'lightData.right' and 'lightData.up' are pre-scaled on CPU.
    float3   lightToSample = positionWS - lightData.positionWS;
    float3x3 lightToWorld  = float3x3(lightData.right, lightData.up, lightData.forward);
    float3   positionLS    = mul(lightToSample, transpose(lightToWorld));

    // Perform orthographic projection.
    float2 positionCS    = positionLS.xy;

    // Remap the texture coordinates from [-1, 1]^2 to [0, 1]^2.
    float2 positionNDC = positionCS * 0.5 + 0.5;

    // Tile the texture if the 'repeat' wrap mode is enabled.
    positionNDC = lightData.tileCookie ? frac(positionNDC) : positionNDC;

    // We let the sampler handle clamping to border.
    return SampleCookie2D(lightLoopContext, positionNDC, lightData.cookieIndex);
}

// None of the outputs are premultiplied.
void EvaluateLight_Directional(LightLoopContext lightLoopContext, PositionInputs posInput,
                               DirectionalLightData lightData, BakeLightingData bakeLightingData,
                               float3 N, float3 L,
                               out float3 color, out float attenuation)
{
    float3 positionWS = posInput.positionWS;
    float  shadow     = 1.0;
    float  shadowMask = 1.0;

    color       = lightData.color;
    attenuation = 1.0;

#ifdef SHADOWS_SHADOWMASK
    // shadowMaskSelector.x is -1 if there is no shadow mask
    // Note that we override shadow value (in case we don't have any dynamic shadow)
    shadow = shadowMask = (lightData.shadowMaskSelector.x >= 0.0) ? dot(bakeLightingData.bakeShadowMask, lightData.shadowMaskSelector) : 1.0;
#endif

    [branch] if (lightData.shadowIndex >= 0)
    {
#ifdef USE_DEFERRED_DIRECTIONAL_SHADOWS
        shadow = LOAD_TEXTURE2D(_DeferredShadowTexture, posInput.positionSS).x;
#else
        shadow = GetDirectionalShadowAttenuation(lightLoopContext.shadowContext, positionWS, N, lightData.shadowIndex, L, posInput.positionSS);
#endif

#ifdef SHADOWS_SHADOWMASK
        float fade = saturate(posInput.linearDepth * lightData.fadeDistanceScaleAndBias.x + lightData.fadeDistanceScaleAndBias.y);

        // See comment in EvaluateBSDF_Punctual
        shadow = lightData.dynamicShadowCasterOnly ? min(shadowMask, shadow) : shadow;
        shadow = lerp(shadow, shadowMask, fade); // Caution to lerp parameter: fade is the reverse of shadowDimmer

        // Note: There is no shadowDimmer when there is no shadow mask
#endif
    }

    attenuation *= shadow;

    [branch] if (lightData.cookieIndex >= 0)
    {
        float3 cookie = EvaluateCookie_Directional(lightLoopContext, lightData, positionWS);

        color *= cookie;
    }
}

//-----------------------------------------------------------------------------
// Punctual Light evaluation helper
//-----------------------------------------------------------------------------

float4 EvaluateCookie_Punctual(LightLoopContext lightLoopContext, LightData lightData,
                               float3 positionWS)
{
    int lightType = lightData.lightType;

    // Translate and rotate 'positionWS' into the light space.
    // 'lightData.right' and 'lightData.up' are pre-scaled on CPU.
    float3   lightToSample = positionWS - lightData.positionWS;
    float3x3 lightToWorld  = float3x3(lightData.right, lightData.up, lightData.forward);
    float3   positionLS    = mul(lightToSample, transpose(lightToWorld));

    float4 cookie;

    [branch] if (lightType == GPULIGHTTYPE_POINT)
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
        cookie.rgb = SampleCookie2D(lightLoopContext, positionNDC, lightData.cookieIndex);
        cookie.a   = isInBounds ? 1 : 0;
    }

    return cookie;
}

float GetPunctualShapeAttenuation(LightData lightData, float3 L, float distSq)
{
    // Note: lightData.invSqrAttenuationRadius is 0 when applyRangeAttenuation is false
    float attenuation = GetDistanceAttenuation(distSq, lightData.invSqrAttenuationRadius);
    // Reminder: lights are oriented backward (-Z)
    return attenuation * GetAngleAttenuation(L, -lightData.forward, lightData.angleScale, lightData.angleOffset);
}

// None of the outputs are premultiplied.
void EvaluateLight_Punctual(LightLoopContext lightLoopContext, PositionInputs posInput,
                            LightData lightData, BakeLightingData bakeLightingData,
                            float3 N, float3 L, float dist, float distSq,
                            out float3 color, out float attenuation)
{
    float3 positionWS = posInput.positionWS;
    float  shadow     = 1.0;
    float  shadowMask = 1.0;

    color       = lightData.color;
    attenuation = GetPunctualShapeAttenuation(lightData, L, distSq);

#ifdef SHADOWS_SHADOWMASK
    // shadowMaskSelector.x is -1 if there is no shadow mask
    // Note that we override shadow value (in case we don't have any dynamic shadow)
    shadow = shadowMask = (lightData.shadowMaskSelector.x >= 0.0) ? dot(bakeLightingData.bakeShadowMask, lightData.shadowMaskSelector) : 1.0;
#endif

    [branch] if (lightData.shadowIndex >= 0)
    {
        // TODO: make projector lights cast shadows.
        float3 offset = float3(0.0, 0.0, 0.0); // GetShadowPosOffset(nDotL, normal);
        float4 L_dist = float4(L, dist);
        shadow = GetPunctualShadowAttenuation(lightLoopContext.shadowContext, positionWS + offset, N, lightData.shadowIndex, L_dist, posInput.positionSS);
#ifdef SHADOWS_SHADOWMASK
        // Note: Legacy Unity have two shadow mask mode. ShadowMask (ShadowMask contain static objects shadow and ShadowMap contain only dynamic objects shadow, final result is the minimun of both value)
        // and ShadowMask_Distance (ShadowMask contain static objects shadow and ShadowMap contain everything and is blend with ShadowMask based on distance (Global distance setup in QualitySettigns)).
        // HDRenderPipeline change this behavior. Only ShadowMask mode is supported but we support both blend with distance AND minimun of both value. Distance is control by light.
        // The following code do this.
        // The min handle the case of having only dynamic objects in the ShadowMap
        // The second case for blend with distance is handled with ShadowDimmer. ShadowDimmer is define manually and by shadowDistance by light.
        // With distance, ShadowDimmer become one and only the ShadowMask appear, we get the blend with distance behavior.
        shadow = lightData.dynamicShadowCasterOnly ? min(shadowMask, shadow) : shadow;
        shadow = lerp(shadowMask, shadow, lightData.shadowDimmer);
#else
        shadow = lerp(1.0, shadow, lightData.shadowDimmer);
#endif
    }

    attenuation *= shadow;

    // Projector lights always have cookies, so we can perform clipping inside the if().
    [branch] if (lightData.cookieIndex >= 0)
    {
        float4 cookie = EvaluateCookie_Punctual(lightLoopContext, lightData, positionWS);

        color       *= cookie.rgb;
        attenuation *= cookie.a;
    }
}
