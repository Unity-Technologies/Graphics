// This files include various function uses to evaluate lights
// To use deferred directional shadow with cascaded shadow map,
// it is required to define USE_DEFERRED_DIRECTIONAL_SHADOWS before including this files

//-----------------------------------------------------------------------------
// Directional Light evaluation helper
//-----------------------------------------------------------------------------

float3 EvaluateCookie_Directional(LightLoopContext lightLoopContext, DirectionalLightData lightData,
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
void EvaluateLight_Directional(LightLoopContext lightLoopContext, PositionInputs posInput,
                               DirectionalLightData lightData, BakeLightingData bakeLightingData,
                               float3 N, float3 L,
                               out float3 color, out float attenuation)
{
    float3 positionWS = posInput.positionWS;
    float  shadow     = 1.0;
    float  shadowMask = 1.0;

    color       = lightData.color;
    attenuation = 1.0; // Note: no volumetric attenuation along shadow rays for directional lights

    UNITY_BRANCH if (lightData.cookieIndex >= 0)
    {
        float3 lightToSample = positionWS - lightData.positionWS;
        float3 cookie = EvaluateCookie_Directional(lightLoopContext, lightData, lightToSample);

        color *= cookie;
    }

#ifdef SHADOWS_SHADOWMASK
    // shadowMaskSelector.x is -1 if there is no shadow mask
    // Note that we override shadow value (in case we don't have any dynamic shadow)
    shadow = shadowMask = (lightData.shadowMaskSelector.x >= 0.0) ? dot(bakeLightingData.bakeShadowMask, lightData.shadowMaskSelector) : 1.0;
#endif

    UNITY_BRANCH if (lightData.shadowIndex >= 0)
    {
#ifdef USE_DEFERRED_DIRECTIONAL_SHADOWS
        shadow = LOAD_TEXTURE2D(_DeferredShadowTexture, posInput.positionSS).x;
#else
        shadow = GetDirectionalShadowAttenuation(lightLoopContext.shadowContext, positionWS, N, lightData.shadowIndex, L, posInput.positionSS);
#endif

#ifdef SHADOWS_SHADOWMASK

        // TODO: Optimize this code! Currently it is a bit like brute force to get the last transistion and fade to shadow mask, but there is
        // certainly more efficient to do
        // We reuse the transition from the cascade system to fade between shadow mask at max distance
        uint  payloadOffset;
        real  fade;
        int cascadeCount;
        int shadowSplitIndex = EvalShadow_GetSplitIndex(lightLoopContext.shadowContext, lightData.shadowIndex, positionWS, payloadOffset, fade, cascadeCount);
        // we have a fade caclulation for each cascade but we must lerp with shadow mask only for the last one
        // if shadowSplitIndex is -1 it mean we are outside cascade and should return 1.0 to use shadowmask: saturate(-shadowSplitIndex) return 0 for >= 0 and 1 for -1
        fade = ((shadowSplitIndex + 1) == cascadeCount) ? fade : saturate(-shadowSplitIndex);

        // In the transition code (both dithering and blend) we use shadow = lerp( shadow, 1.0, fade ) for last transition
        // mean if we expend the code we have (shadow * (1 - fade) + fade). Here to make transition with shadow mask
        // we will remove fade and add fade * shadowMask which mean we do a lerp with shadow mask
        shadow = shadow - fade + fade * shadowMask;

        // See comment in EvaluateBSDF_Punctual
        shadow = lightData.nonLightmappedOnly ? min(shadowMask, shadow) : shadow;

        // Note: There is no shadowDimmer when there is no shadow mask
#endif
    }

    attenuation *= shadow;
}

//-----------------------------------------------------------------------------
// Punctual Light evaluation helper
//-----------------------------------------------------------------------------

float4 EvaluateCookie_Punctual(LightLoopContext lightLoopContext, LightData lightData,
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

// None of the outputs are premultiplied.
// distances = {d, d^2, 1/d, d_proj}, where d_proj = dot(lightToSample, lightData.forward).
void EvaluateLight_Punctual(LightLoopContext lightLoopContext, PositionInputs posInput,
                            LightData lightData, BakeLightingData bakeLightingData,
                            float3 N, float3 L, float3 lightToSample, float4 distances,
                            out float3 color, out float attenuation)
{
    float3 positionWS = posInput.positionWS;
    float  shadow     = 1.0;
    float  shadowMask = 1.0;

    color       = lightData.color;
    attenuation = SmoothPunctualLightAttenuation(distances, lightData.invSqrAttenuationRadius,
                                                 lightData.angleScale, lightData.angleOffset);

#if (SHADEROPTIONS_VOLUMETRIC_LIGHTING_PRESET != 0)
    // TODO: sample the extinction from the density V-buffer.
    float distVol = (lightData.lightType == GPULIGHTTYPE_PROJECTOR_BOX) ? distances.w : distances.x;
    attenuation *= TransmittanceHomogeneousMedium(_GlobalExtinction, distVol);
#endif

    // Projector lights always have cookies, so we can perform clipping inside the if().
    UNITY_BRANCH if (lightData.cookieIndex >= 0)
    {
        float4 cookie = EvaluateCookie_Punctual(lightLoopContext, lightData, lightToSample);

        color       *= cookie.rgb;
        attenuation *= cookie.a;
    }

#ifdef SHADOWS_SHADOWMASK
    // shadowMaskSelector.x is -1 if there is no shadow mask
    // Note that we override shadow value (in case we don't have any dynamic shadow)
    shadow = shadowMask = (lightData.shadowMaskSelector.x >= 0.0) ? dot(bakeLightingData.bakeShadowMask, lightData.shadowMaskSelector) : 1.0;
#endif

    UNITY_BRANCH if (lightData.shadowIndex >= 0)
    {
        // TODO: make projector lights cast shadows.
        shadow = GetPunctualShadowAttenuation(lightLoopContext.shadowContext, positionWS, N, lightData.shadowIndex, L, distances.x, posInput.positionSS);
#ifdef SHADOWS_SHADOWMASK
        // Note: Legacy Unity have two shadow mask mode. ShadowMask (ShadowMask contain static objects shadow and ShadowMap contain only dynamic objects shadow, final result is the minimun of both value)
        // and ShadowMask_Distance (ShadowMask contain static objects shadow and ShadowMap contain everything and is blend with ShadowMask based on distance (Global distance setup in QualitySettigns)).
        // HDRenderPipeline change this behavior. Only ShadowMask mode is supported but we support both blend with distance AND minimun of both value. Distance is control by light.
        // The following code do this.
        // The min handle the case of having only dynamic objects in the ShadowMap
        // The second case for blend with distance is handled with ShadowDimmer. ShadowDimmer is define manually and by shadowDistance by light.
        // With distance, ShadowDimmer become one and only the ShadowMask appear, we get the blend with distance behavior.
        shadow = lightData.nonLightmappedOnly ? min(shadowMask, shadow) : shadow;
        shadow = lerp(shadowMask, shadow, lightData.shadowDimmer);
#else
        shadow = lerp(1.0, shadow, lightData.shadowDimmer);
#endif
    }

    attenuation *= shadow;

}

// Environment map share function
#include "Reflection/VolumeProjection.hlsl"

void EvaluateLight_EnvIntersection(float3 positionWS, float3 normalWS, EnvLightData lightData, int influenceShapeType, inout float3 R, inout float weight)
{
    // Guideline for reflection volume: In HDRenderPipeline we separate the projection volume (the proxy of the scene) from the influence volume (what pixel on the screen is affected)
    // However we add the constrain that the shape of the projection and influence volume is the same (i.e if we have a sphere shape projection volume, we have a shape influence).
    // It allow to have more coherence for the dynamic if in shader code.
    // Users can also chose to not have any projection, in this case we use the property minProjectionDistance to minimize code change. minProjectionDistance is set to huge number
    // that simulate effect of no shape projection

    float3x3 worldToIS = WorldToInfluenceSpace(lightData); // IS: Influence space
    float3 positionIS = WorldToInfluencePosition(lightData, worldToIS, positionWS);
    float3 dirIS = mul(R, worldToIS);

    float3x3 worldToPS = WorldToProxySpace(lightData); // PS: Proxy space
    float3 positionPS = WorldToProxyPosition(lightData, worldToPS, positionWS);
    float3 dirPS = mul(R, worldToPS);

    float projectionDistance = 0;

    // Process the projection
    // In Unity the cubemaps are capture with the localToWorld transform of the component.
    // This mean that location and orientation matter. So after intersection of proxy volume we need to convert back to world.
    if (influenceShapeType == ENVSHAPETYPE_SPHERE)
    {
        projectionDistance = IntersectSphereProxy(lightData, dirPS, positionPS);
        // We can reuse dist calculate in LS directly in WS as there is no scaling. Also the offset is already include in lightData.capturePositionWS
        float3 capturePositionWS = lightData.capturePositionWS;
        R = (positionWS + projectionDistance * R) - capturePositionWS;

        weight = InfluenceSphereWeight(lightData, normalWS, positionWS, positionIS, dirIS);
    }
    else if (influenceShapeType == ENVSHAPETYPE_BOX)
    {
        projectionDistance = IntersectBoxProxy(lightData, dirPS, positionPS);
        // No need to normalize for fetching cubemap
        // We can reuse dist calculate in LS directly in WS as there is no scaling. Also the offset is already include in lightData.capturePositionWS
        float3 capturePositionWS = lightData.capturePositionWS;
        R = (positionWS + projectionDistance * R) - capturePositionWS;

        weight = InfluenceBoxWeight(lightData, normalWS, positionWS, positionIS, dirIS);
    }

    // Smooth weighting
    weight = Smoothstep01(weight);
    weight *= lightData.weight;
}
