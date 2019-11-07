// This files include various function uses to evaluate lights
// use #define LIGHT_EVALUATION_NO_HEIGHT_FOG to disable Height fog attenuation evaluation
// use #define LIGHT_EVALUATION_NO_COOKIE to disable cookie evaluation
// use #define LIGHT_EVALUATION_NO_CONTACT_SHADOWS to disable contact shadow evaluation
// use #define LIGHT_EVALUATION_NO_SHADOWS to disable evaluation of shadow including contact shadow (but not micro shadow)
// use #define OVERRIDE_EVALUATE_ENV_INTERSECTION to provide a new version of EvaluateLight_EnvIntersection

// Samples the area light's associated cookie
//  cookieIndex, the index of the cookie texture in the Texture2DArray
//  L, the 4 local-space corners of the area light polygon transformed by the LTC M^-1 matrix
//  F, the *normalized* vector irradiance
float3 SampleAreaLightCookie(int cookieIndex, float4x3 L, float3 F)
{
    // L[0..3] : LL UL UR LR

    float3  origin = L[0];
    float3  right = L[3] - origin;
    float3  up = L[1] - origin;

    float3  normal = cross(right, up);
    float   sqArea = dot(normal, normal);
    normal *= rsqrt(sqArea);

    // Compute intersection of irradiance vector with the area light plane
    float   hitDistance = dot(origin, normal) / dot(F, normal);
    float3  hitPosition = hitDistance * normal;
    hitPosition -= origin;  // Relative to bottom-left corner

                            // Here, right and up vectors are not necessarily orthonormal
                            // We create the orthogonal vector "ortho" by projecting "up" onto the vector orthogonal to "right"
                            //  ortho = up - (up.right') * right'
                            // Where right' = right / sqrt( dot( right, right ) ), the normalized right vector
    float   recSqLengthRight = 1.0 / dot(right, right);
    float   upRightMixing = dot(up, right);
    float3  ortho = up - upRightMixing * right * recSqLengthRight;

    // The V coordinate along the "up" vector is simply the projection against the ortho vector
    float   v = dot(hitPosition, ortho) / dot(ortho, ortho);

    // The U coordinate is not only the projection against the right vector
    //  but also the subtraction of the influence of the up vector upon the right vector
    //  (indeed, if the up & right vectors are not orthogonal then a certain amount of
    //  the up coordinate also influences the right coordinate)
    //
    //       |    up
    // ortho ^....*--------*
    //       |   /:       /
    //       |  / :      /
    //       | /  :     /
    //       |/   :    /
    //       +----+-->*----->
    //            : right
    //          mix of up into right that needs to be subtracted from simple projection on right vector
    //
    float   u = (dot(hitPosition, right) - upRightMixing * v) * recSqLengthRight;
    // Currently the texture happens to be reversed when comparing it to the area light emissive mesh itself. This needs
    // Further investigation to solve the problem. So for the moment we simply decided to inverse the x coordinate of hitUV
    // as a temporary solution
    // TODO: Invesigate more!
    float2  hitUV = float2(1.0 - u, v);

    // Assuming the original cosine lobe distribution Do is enclosed in a cone of 90 deg  aperture,
    //  following the idea of orthogonal projection upon the area light's plane we find the intersection
    //  of the cone to be a disk of area PI*d^2 where d is the hit distance we computed above.
    // We also know the area of the transformed polygon A = sqrt( sqArea ) and we pose the ratio of covered area as PI.d^2 / A.
    //
    // Knowing the area in square texels of the cookie texture A_sqTexels = texture width * texture height (default is 128x128 square texels)
    //  we can deduce the actual area covered by the cone in square texels as:
    //  A_covered = Pi.d^2 / A * A_sqTexels
    //
    // From this, we find the mip level as: mip = log2( sqrt( A_covered ) ) = log2( A_covered ) / 2
    // Also, assuming that A_sqTexels is of the form 2^n * 2^n we get the simplified expression: mip = log2( Pi.d^2 / A ) / 2 + n
    //
    const float COOKIE_MIPS_COUNT = _CookieSizePOT;
    float   mipLevel = 0.5 * log2(1e-8 + PI * hitDistance*hitDistance * rsqrt(sqArea)) + COOKIE_MIPS_COUNT;

    return SAMPLE_TEXTURE2D_ARRAY_LOD(_AreaCookieTextures, s_trilinear_clamp_sampler, hitUV, cookieIndex, mipLevel).xyz;
}

//-----------------------------------------------------------------------------
// Directional Light evaluation helper
//-----------------------------------------------------------------------------

float3 EvaluateCookie_Directional(LightLoopContext lightLoopContext, DirectionalLightData light,
                                  float3 lightToSample)
{

    // Translate and rotate 'positionWS' into the light space.
    // 'light.right' and 'light.up' are pre-scaled on CPU.
    float3x3 lightToWorld = float3x3(light.right, light.up, light.forward);
    float3   positionLS   = mul(lightToSample, transpose(lightToWorld));

    // Perform orthographic projection.
    float2 positionCS  = positionLS.xy;

    // Remap the texture coordinates from [-1, 1]^2 to [0, 1]^2.
    float2 positionNDC = positionCS * 0.5 + 0.5;

    // We let the sampler handle clamping to border.
    return SampleCookie2D(lightLoopContext, positionNDC, light.cookieIndex, light.tileCookie);
}

// Returns unassociated (non-premultiplied) color with alpha (attenuation).
// The calling code must perform alpha-compositing.
float4 EvaluateLight_Directional(LightLoopContext lightLoopContext, PositionInputs posInput,
                                 DirectionalLightData light)
{
    float4 color = float4(light.color, 1.0);

    float3 L = -light.forward;

    float3 oDepth = 0;

#ifndef LIGHT_EVALUATION_NO_HEIGHT_FOG
    // Height fog attenuation.
    {
        // TODO: should probably unify height attenuation somehow...
        float cosZenithAngle = L.y;
        float fragmentHeight = posInput.positionWS.y;
        oDepth += OpticalDepthHeightFog(_HeightFogBaseExtinction, _HeightFogBaseHeight,
                                        _HeightFogExponents, cosZenithAngle, fragmentHeight);
    }
#endif

#if SHADEROPTIONS_PRECOMPUTED_ATMOSPHERIC_ATTENUATION
    // Precomputes atmospheric attenuation for the directional light on the CPU,
    // which makes it independent from the fragment's position, which is faster but wrong.
    // Basically, the code below runs on the CPU, using camera.positionWS, and modifies light.color.
#else
    // Use scalar or integer cores (more efficient).
    bool interactsWithSky = asint(light.distanceFromCamera) >= 0;

    if (interactsWithSky)
    {
        // TODO: should probably unify height attenuation somehow...
        // TODO: Not sure it's possible to precompute cam rel pos since variables
        // in the two constant buffers may be set at a different frequency?
        float3 X = GetAbsolutePositionWS(posInput.positionWS) * 0.001; // Convert m to km
        float3 C = _PlanetCenterPosition;

        float r        = distance(X, C);
        float cosHoriz = ComputeCosineOfHorizonAngle(r);
        float cosTheta = dot(X - C, L) * rcp(r); // Normalize

        if (cosTheta >= cosHoriz) // Above horizon
        {
            oDepth += ComputeAtmosphericOpticalDepth(r, cosTheta, true);
        }
        else
        {
            // return 0; // Kill the light. This generates a warning, so can't early out. :-(
            oDepth = FLT_INF;
        }
    }
#endif

    color.rgb *= TransmittanceFromOpticalDepth(oDepth);

#ifndef LIGHT_EVALUATION_NO_COOKIE
    if (light.cookieIndex >= 0)
    {
        float3 lightToSample = posInput.positionWS - light.positionRWS;
        float3 cookie = EvaluateCookie_Directional(lightLoopContext, light, lightToSample);

        color.rgb *= cookie;
    }
#endif

    return color;
}

float EvaluateShadow_Directional(LightLoopContext lightLoopContext, PositionInputs posInput,
                                 DirectionalLightData light, BuiltinData builtinData, float3 N)
{
#ifndef LIGHT_EVALUATION_NO_SHADOWS
    float shadow     = 1.0;
    float shadowMask = 1.0;
    float NdotL      = dot(N, -light.forward); // Disable contact shadow and shadow mask when facing away from light (i.e transmission)

#ifdef SHADOWS_SHADOWMASK
    // shadowMaskSelector.x is -1 if there is no shadow mask
    // Note that we override shadow value (in case we don't have any dynamic shadow)
    shadow = shadowMask = (light.shadowMaskSelector.x >= 0.0 && NdotL > 0.0) ? dot(BUILTIN_DATA_SHADOW_MASK, light.shadowMaskSelector) : 1.0;
#endif

    if ((light.shadowIndex >= 0) && (light.shadowDimmer > 0))
    {
        shadow = lightLoopContext.shadowValue;

    #ifdef SHADOWS_SHADOWMASK
        // TODO: Optimize this code! Currently it is a bit like brute force to get the last transistion and fade to shadow mask, but there is
        // certainly more efficient to do
        // We reuse the transition from the cascade system to fade between shadow mask at max distance
        uint  payloadOffset;
        real  fade;
        int cascadeCount;
        int shadowSplitIndex = 0;

        shadowSplitIndex = EvalShadow_GetSplitIndex(lightLoopContext.shadowContext, light.shadowIndex, posInput.positionWS, fade, cascadeCount);

        // we have a fade caclulation for each cascade but we must lerp with shadow mask only for the last one
        // if shadowSplitIndex is -1 it mean we are outside cascade and should return 1.0 to use shadowmask: saturate(-shadowSplitIndex) return 0 for >= 0 and 1 for -1
        fade = ((shadowSplitIndex + 1) == cascadeCount) ? fade : saturate(-shadowSplitIndex);

        // In the transition code (both dithering and blend) we use shadow = lerp( shadow, 1.0, fade ) for last transition
        // mean if we expend the code we have (shadow * (1 - fade) + fade). Here to make transition with shadow mask
        // we will remove fade and add fade * shadowMask which mean we do a lerp with shadow mask
        shadow = shadow - fade + fade * shadowMask;

        // See comment in EvaluateBSDF_Punctual
        shadow = light.nonLightMappedOnly ? min(shadowMask, shadow) : shadow;
    #endif

        shadow = lerp(shadowMask, shadow, light.shadowDimmer);
    }

    // Transparents have no contact shadow information
#if !defined(_SURFACE_TYPE_TRANSPARENT) && !defined(LIGHT_EVALUATION_NO_CONTACT_SHADOWS)
    shadow = min(shadow, NdotL > 0.0 ? GetContactShadow(lightLoopContext, light.contactShadowMask, light.isRayTracedContactShadow) : 1.0);
#endif

#ifdef DEBUG_DISPLAY
    if (_DebugShadowMapMode == SHADOWMAPDEBUGMODE_SINGLE_SHADOW && light.shadowIndex == _DebugSingleShadowIndex)
        g_DebugShadowAttenuation = shadow;
#endif

    return shadow;
#else // LIGHT_EVALUATION_NO_SHADOWS
    return 1.0;
#endif
}

//-----------------------------------------------------------------------------
// Punctual Light evaluation helper
//-----------------------------------------------------------------------------

// distances = {d, d^2, 1/d, d_proj}
void ModifyDistancesForFillLighting(inout float4 distances, float lightSqRadius)
{
    // Apply the sphere light hack to soften the core of the punctual light.
    // It is not physically plausible (using max() is more correct, but looks worse).
    // See https://www.desmos.com/calculator/otqhxunqhl
    // We only modify 1/d for performance reasons.
    float sqDist = distances.y;
    distances.z = rsqrt(sqDist + lightSqRadius); // Recompute 1/d
}

// Returns the normalized light vector L and the distances = {d, d^2, 1/d, d_proj}.
void GetPunctualLightVectors(float3 positionWS, LightData light, out float3 L, out float4 distances)
{
    float3 lightToSample = positionWS - light.positionRWS;

    distances.w = dot(lightToSample, light.forward);

    if (light.lightType == GPULIGHTTYPE_PROJECTOR_BOX)
    {
        L = -light.forward;
        distances.xyz = 1; // No distance or angle attenuation
    }
    else
    {
        float3 unL     = -lightToSample;
        float  distSq  = dot(unL, unL);
        float  distRcp = rsqrt(distSq);
        float  dist    = distSq * distRcp;

        L = unL * distRcp;
        distances.xyz = float3(dist, distSq, distRcp);

        ModifyDistancesForFillLighting(distances, light.size.x);
    }
}

float4 EvaluateCookie_Punctual(LightLoopContext lightLoopContext, LightData light,
                               float3 lightToSample)
{
#ifndef LIGHT_EVALUATION_NO_COOKIE
    int lightType = light.lightType;

    // Translate and rotate 'positionWS' into the light space.
    // 'light.right' and 'light.up' are pre-scaled on CPU.
    float3x3 lightToWorld = float3x3(light.right, light.up, light.forward);
    float3   positionLS   = mul(lightToSample, transpose(lightToWorld));

    float4 cookie;

    UNITY_BRANCH if (lightType == GPULIGHTTYPE_POINT)
    {
        cookie.rgb = SampleCookieCube(lightLoopContext, positionLS, light.cookieIndex);
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
        cookie.rgb = SampleCookie2D(lightLoopContext, positionNDC, light.cookieIndex, false);
        cookie.a   = isInBounds ? 1.0 : 0.0;
    }

#else

    // When we disable cookie, we must still perform border attenuation for pyramid and box
    // as by default we always bind a cookie white texture for them to mimic it.
    float4 cookie = float4(1.0, 1.0, 1.0, 1.0);

    int lightType = light.lightType;

    if (lightType == GPULIGHTTYPE_PROJECTOR_PYRAMID || lightType == GPULIGHTTYPE_PROJECTOR_BOX)
    {
        // Translate and rotate 'positionWS' into the light space.
        // 'light.right' and 'light.up' are pre-scaled on CPU.
        float3x3 lightToWorld = float3x3(light.right, light.up, light.forward);
        float3 positionLS     = mul(lightToSample, transpose(lightToWorld));

        // Perform orthographic or perspective projection.
        float  perspectiveZ = (lightType != GPULIGHTTYPE_PROJECTOR_BOX) ? positionLS.z : 1.0;
        float2 positionCS   = positionLS.xy / perspectiveZ;
        bool   isInBounds   = Max3(abs(positionCS.x), abs(positionCS.y), 1.0 - positionLS.z) <= 1.0;

        // Manually clamp to border (black).
        cookie.a = isInBounds ? 1.0 : 0.0;
    }
#endif

    return cookie;
}

// Returns unassociated (non-premultiplied) color with alpha (attenuation).
// The calling code must perform alpha-compositing.
// distances = {d, d^2, 1/d, d_proj}, where d_proj = dot(lightToSample, light.forward).
float4 EvaluateLight_Punctual(LightLoopContext lightLoopContext, PositionInputs posInput,
    LightData light, float3 L, float4 distances)
{
    float4 color = float4(light.color, 1.0);

    color.a *= PunctualLightAttenuation(distances, light.rangeAttenuationScale, light.rangeAttenuationBias,
                                        light.angleScale, light.angleOffset);

#ifndef LIGHT_EVALUATION_NO_HEIGHT_FOG
    // Height fog attenuation.
    // TODO: add an if()?
    {
        float cosZenithAngle = L.y;
        float distToLight = (light.lightType == GPULIGHTTYPE_PROJECTOR_BOX) ? distances.w : distances.x;
        float fragmentHeight = posInput.positionWS.y;
        color.a *= TransmittanceHeightFog(_HeightFogBaseExtinction, _HeightFogBaseHeight,
                                          _HeightFogExponents, cosZenithAngle,
                                          fragmentHeight, distToLight);
    }
#endif

    // Projector lights (box, pyramid) always have cookies, so we can perform clipping inside the if().
    // Thus why we don't disable the code here based on LIGHT_EVALUATION_NO_COOKIE but we do it
    // inside the EvaluateCookie_Punctual call
    if (light.cookieIndex >= 0)
    {
        float3 lightToSample = posInput.positionWS - light.positionRWS;
        float4 cookie = EvaluateCookie_Punctual(lightLoopContext, light, lightToSample);

        color *= cookie;
    }

    return color;
}

// distances = {d, d^2, 1/d, d_proj}, where d_proj = dot(lightToSample, light.forward).
float EvaluateShadow_Punctual(LightLoopContext lightLoopContext, PositionInputs posInput,
                              LightData light, BuiltinData builtinData, float3 N, float3 L, float4 distances)
{
#ifndef LIGHT_EVALUATION_NO_SHADOWS
    float shadow     = 1.0;
    float shadowMask = 1.0;
    float NdotL      = dot(N, L); // Disable contact shadow and shadow mask when facing away from light (i.e transmission)


#ifdef SHADOWS_SHADOWMASK
    // shadowMaskSelector.x is -1 if there is no shadow mask
    // Note that we override shadow value (in case we don't have any dynamic shadow)
    shadow = shadowMask = (light.shadowMaskSelector.x >= 0.0 && NdotL > 0.0) ? dot(BUILTIN_DATA_SHADOW_MASK, light.shadowMaskSelector) : 1.0;
#endif

#if defined(SCREEN_SPACE_SHADOWS) && !defined(_SURFACE_TYPE_TRANSPARENT) && (SHADERPASS != SHADERPASS_VOLUMETRIC_LIGHTING)
    if(light.screenSpaceShadowIndex >= 0)
    {
        shadow = GetScreenSpaceShadow(posInput, light.screenSpaceShadowIndex);
    }
    else
#endif
    if ((light.shadowIndex >= 0) && (light.shadowDimmer > 0))
    {
        shadow = GetPunctualShadowAttenuation(lightLoopContext.shadowContext, posInput.positionSS, posInput.positionWS, N, light.shadowIndex, L, distances.x, light.lightType == GPULIGHTTYPE_POINT, light.lightType != GPULIGHTTYPE_PROJECTOR_BOX);

#ifdef SHADOWS_SHADOWMASK
        // Note: Legacy Unity have two shadow mask mode. ShadowMask (ShadowMask contain static objects shadow and ShadowMap contain only dynamic objects shadow, final result is the minimun of both value)
        // and ShadowMask_Distance (ShadowMask contain static objects shadow and ShadowMap contain everything and is blend with ShadowMask based on distance (Global distance setup in QualitySettigns)).
        // HDRenderPipeline change this behavior. Only ShadowMask mode is supported but we support both blend with distance AND minimun of both value. Distance is control by light.
        // The following code do this.
        // The min handle the case of having only dynamic objects in the ShadowMap
        // The second case for blend with distance is handled with ShadowDimmer. ShadowDimmer is define manually and by shadowDistance by light.
        // With distance, ShadowDimmer become one and only the ShadowMask appear, we get the blend with distance behavior.
        shadow = light.nonLightMappedOnly ? min(shadowMask, shadow) : shadow;
    #endif

        shadow = lerp(shadowMask, shadow, light.shadowDimmer);
    }

    // Transparents have no contact shadow information
#if !defined(_SURFACE_TYPE_TRANSPARENT) && !defined(LIGHT_EVALUATION_NO_CONTACT_SHADOWS)
    shadow = min(shadow, NdotL > 0.0 ? GetContactShadow(lightLoopContext, light.contactShadowMask, light.isRayTracedContactShadow) : 1.0);
#endif

#ifdef DEBUG_DISPLAY
    if (_DebugShadowMapMode == SHADOWMAPDEBUGMODE_SINGLE_SHADOW && light.shadowIndex == _DebugSingleShadowIndex)
        g_DebugShadowAttenuation = shadow;
#endif
    return shadow;
#else // LIGHT_EVALUATION_NO_SHADOWS
    return 1.0;
#endif
}

//-----------------------------------------------------------------------------
// Reflection probe evaluation helper
//-----------------------------------------------------------------------------

#ifndef OVERRIDE_EVALUATE_ENV_INTERSECTION
// Environment map share function
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Reflection/VolumeProjection.hlsl"

void EvaluateLight_EnvIntersection(float3 positionWS, float3 normalWS, EnvLightData light, int influenceShapeType, inout float3 R, inout float weight)
{
    // Guideline for reflection volume: In HDRenderPipeline we separate the projection volume (the proxy of the scene) from the influence volume (what pixel on the screen is affected)
    // However we add the constrain that the shape of the projection and influence volume is the same (i.e if we have a sphere shape projection volume, we have a shape influence).
    // It allow to have more coherence for the dynamic if in shader code.
    // Users can also chose to not have any projection, in this case we use the property minProjectionDistance to minimize code change. minProjectionDistance is set to huge number
    // that simulate effect of no shape projection

    float3x3 worldToIS = WorldToInfluenceSpace(light); // IS: Influence space
    float3 positionIS = WorldToInfluencePosition(light, worldToIS, positionWS);
    float3 dirIS = normalize(mul(R, worldToIS));

    float3x3 worldToPS = WorldToProxySpace(light); // PS: Proxy space
    float3 positionPS = WorldToProxyPosition(light, worldToPS, positionWS);
    float3 dirPS = mul(R, worldToPS);

    float projectionDistance = 0;

    // Process the projection
    // In Unity the cubemaps are capture with the localToWorld transform of the component.
    // This mean that location and orientation matter. So after intersection of proxy volume we need to convert back to world.
    if (influenceShapeType == ENVSHAPETYPE_SPHERE)
    {
        projectionDistance = IntersectSphereProxy(light, dirPS, positionPS);
        // We can reuse dist calculate in LS directly in WS as there is no scaling. Also the offset is already include in light.capturePositionRWS
        R = (positionWS + projectionDistance * R) - light.capturePositionRWS;

        weight = InfluenceSphereWeight(light, normalWS, positionWS, positionIS, dirIS);
    }
    else if (influenceShapeType == ENVSHAPETYPE_BOX)
    {
        projectionDistance = IntersectBoxProxy(light, dirPS, positionPS);
        // No need to normalize for fetching cubemap
        // We can reuse dist calculate in LS directly in WS as there is no scaling. Also the offset is already include in light.capturePositionRWS
        R = (positionWS + projectionDistance * R) - light.capturePositionRWS;

        weight = InfluenceBoxWeight(light, normalWS, positionWS, positionIS, dirIS);
    }

    // Smooth weighting
    weight = Smoothstep01(weight);
    weight *= light.weight;
}
#endif
