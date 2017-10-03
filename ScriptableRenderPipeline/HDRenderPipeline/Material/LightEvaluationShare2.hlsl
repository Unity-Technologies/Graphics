#ifdef HAS_LIGHTLOOP


#ifdef WANT_SSS_CODE
// Currently, we only model diffuse transmission. Specular transmission is not yet supported.
// We assume that the back side of the object is a uniformly illuminated infinite plane
// with the reversed normal (and the view vector) of the current sample.
float3 EvaluateTransmission(BSDFData bsdfData, float intensity, float shadow)
{
    // For low thickness, we can reuse the shadowing status for the back of the object.
    shadow = bsdfData.useThinObjectMode ? shadow : 1;

    float backLight = intensity * shadow;

    return backLight * bsdfData.transmittance; // Premultiplied with the diffuse color
}
#endif

//-----------------------------------------------------------------------------
// EvaluateBSDF_Directional (supports directional and box projector lights)
//-----------------------------------------------------------------------------

float4 EvaluateCookie_Directional(LightLoopContext lightLoopContext, DirectionalLightData lightData,
                                  float3 lighToSample)
{
    // Compute the NDC position (in [-1, 1]^2) by projecting 'positionWS' onto the near plane.
    // 'lightData.right' and 'lightData.up' are pre-scaled on CPU.
    float3x3 lightToWorld = float3x3(lightData.right, lightData.up, lightData.forward);
    float3   positionLS   = mul(lighToSample, transpose(lightToWorld));
    float2   positionNDC  = positionLS.xy;

    bool isInBounds;

    // Remap the texture coordinates from [-1, 1]^2 to [0, 1]^2.
    float2 coord = positionNDC * 0.5 + 0.5;

    if (lightData.tileCookie)
    {
        // Tile the texture if the 'repeat' wrap mode is enabled.
        coord = frac(coord);
        isInBounds = true;
    }
    else
    {
        isInBounds = Max3(abs(positionNDC.x), abs(positionNDC.y), 1 - positionLS.z) <= 1;
    }

    // We let the sampler handle tiling or clamping to border.
    // Note: tiling (the repeat mode) is not currently supported.
    float4 cookie = SampleCookie2D(lightLoopContext, coord, lightData.cookieIndex);

    cookie.a = isInBounds ? cookie.a : 0;

    return cookie;
}

void EvaluateBSDF_Directional(LightLoopContext lightLoopContext,
                              float3 V, PositionInputs posInput, PreLightData preLightData,
                              DirectionalLightData lightData, BSDFData bsdfData,
                              out float3 diffuseLighting,
                              out float3 specularLighting)
{
    float3 positionWS = posInput.positionWS;

    float3 L = -lightData.forward; // Lights are pointing backward in Unity
    float NdotL = dot(bsdfData.normalWS, L);
    float illuminance = saturate(NdotL);

    diffuseLighting  = float3(0, 0, 0); // TODO: check whether using 'out' instead of 'inout' increases the VGPR pressure
    specularLighting = float3(0, 0, 0); // TODO: check whether using 'out' instead of 'inout' increases the VGPR pressure
    float shadow     = 1;

    [branch] if (lightData.shadowIndex >= 0)
    {
        shadow = GetDirectionalShadowAttenuation(lightLoopContext.shadowContext, positionWS, bsdfData.normalWS, lightData.shadowIndex, L, posInput.unPositionSS);
        illuminance *= shadow;
    }

    [branch] if (lightData.cookieIndex >= 0)
    {
        float3 lightToSurface = positionWS - lightData.positionWS;
        float4 cookie = EvaluateCookie_Directional(lightLoopContext, lightData, lightToSurface);

        // Premultiply.
        lightData.color         *= cookie.rgb;
        lightData.diffuseScale  *= cookie.a;
        lightData.specularScale *= cookie.a;
    }

    [branch] if (illuminance > 0.0)
    {
        BSDF(V, L, positionWS, preLightData, bsdfData, diffuseLighting, specularLighting);

        diffuseLighting  *= illuminance * lightData.diffuseScale;
        specularLighting *= illuminance * lightData.specularScale;
    }

#ifdef WANT_SSS_CODE
    [branch] if (bsdfData.enableTransmission)
    {
        // We apply wrapped lighting instead of the regular Lambertian diffuse
        // to compensate for approximations within EvaluateTransmission().
        float illuminance = Lambert() * ComputeWrappedDiffuseLighting(-NdotL, SSS_WRAP_LIGHT);

        // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
        diffuseLighting += EvaluateTransmission(bsdfData, illuminance * lightData.diffuseScale, shadow);
    }
#endif

    // Save ALU by applying 'lightData.color' only once.
    diffuseLighting  *= lightData.color;
    specularLighting *= lightData.color;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Punctual (supports spot, point and projector lights)
//-----------------------------------------------------------------------------

float4 EvaluateCookie_Punctual(LightLoopContext lightLoopContext, LightData lightData,
                               float3 lighToSample)
{
    int lightType = lightData.lightType;

    // Translate and rotate 'positionWS' into the light space.
    // 'lightData.right' and 'lightData.up' are pre-scaled on CPU.
    float3x3 lightToWorld = float3x3(lightData.right, lightData.up, lightData.forward);
    float3   positionLS   = mul(lighToSample, transpose(lightToWorld));

    float4 cookie;

    [branch] if (lightType == GPULIGHTTYPE_POINT)
    {
        cookie = SampleCookieCube(lightLoopContext, positionLS, lightData.cookieIndex);
    }
    else
    {
        // Compute the NDC position (in [-1, 1]^2) by projecting 'positionWS' onto the plane at 1m distance.
        // Box projector lights require no perspective division.
        float  perspectiveZ = (lightType != GPULIGHTTYPE_PROJECTOR_BOX) ? positionLS.z : 1;
        float2 positionNDC  = positionLS.xy / perspectiveZ;
        bool   isInBounds   = Max3(abs(positionNDC.x), abs(positionNDC.y), 1 - positionLS.z) <= 1;

        // Remap the texture coordinates from [-1, 1]^2 to [0, 1]^2.
        float2 coord = positionNDC * 0.5 + 0.5;

        // We let the sampler handle clamping to border.
        cookie = SampleCookie2D(lightLoopContext, coord, lightData.cookieIndex);
        cookie.a = isInBounds ? cookie.a : 0;
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

void EvaluateBSDF_Punctual( LightLoopContext lightLoopContext,
                            float3 V, PositionInputs posInput, PreLightData preLightData, LightData lightData, BSDFData bsdfData,
                            out float3 diffuseLighting,
                            out float3 specularLighting)
{
    float3 positionWS = posInput.positionWS;
    int    lightType  = lightData.lightType;

    // All punctual light type in the same formula, attenuation is neutral depends on light type.
    // light.positionWS is the normalize light direction in case of directional light and invSqrAttenuationRadius is 0
    // mean dot(unL, unL) = 1 and mean GetDistanceAttenuation() will return 1
    // For point light and directional GetAngleAttenuation() return 1

    float3 lightToSurface = positionWS - lightData.positionWS;
    float3 unL    = -lightToSurface;
    float  distSq = dot(unL, unL);
    float  dist   = sqrt(distSq);
    float3 L      = (lightType != GPULIGHTTYPE_PROJECTOR_BOX) ? unL * rsqrt(distSq) : -lightData.forward;
    float  NdotL  = dot(bsdfData.normalWS, L);
    float  illuminance = saturate(NdotL);

    float attenuation = GetPunctualShapeAttenuation(lightData, L, distSq);

    // Premultiply.
    lightData.diffuseScale  *= attenuation;
    lightData.specularScale *= attenuation;

    diffuseLighting  = float3(0, 0, 0); // TODO: check whether using 'out' instead of 'inout' increases the VGPR pressure
    specularLighting = float3(0, 0, 0); // TODO: check whether using 'out' instead of 'inout' increases the VGPR pressure
    float shadow     = 1;

    [branch] if (lightData.shadowIndex >= 0)
    {
        // TODO: make projector lights cast shadows.
        float3 offset = float3(0.0, 0.0, 0.0); // GetShadowPosOffset(nDotL, normal);
        float4 L_dist = { L, dist };
        shadow = GetPunctualShadowAttenuation(lightLoopContext.shadowContext, positionWS + offset, bsdfData.normalWS, lightData.shadowIndex, L_dist, posInput.unPositionSS);
        shadow = lerp(1.0, shadow, lightData.shadowDimmer);
        illuminance *= shadow;
    }

    // Projector lights always have a cookies, so we can perform clipping inside the if().
    [branch] if (lightData.cookieIndex >= 0)
    {
        float4 cookie = EvaluateCookie_Punctual(lightLoopContext, lightData, lightToSurface);

        // Premultiply.
        lightData.color         *= cookie.rgb;
        lightData.diffuseScale  *= cookie.a;
        lightData.specularScale *= cookie.a;
    }

    [branch] if (illuminance > 0.0)
    {
        bsdfData.roughness = max(bsdfData.roughness, lightData.minRoughness); // Simulate that a punctual ligth have a radius with this hack
        BSDF(V, L, positionWS, preLightData, bsdfData, diffuseLighting, specularLighting);

        diffuseLighting  *= illuminance * lightData.diffuseScale;
        specularLighting *= illuminance * lightData.specularScale;
    }

#ifdef WANT_SSS_CODE
    [branch] if (bsdfData.enableTransmission)
    {
        // We apply wrapped lighting instead of the regular Lambertian diffuse
        // to compensate for approximations within EvaluateTransmission().
        float illuminance = Lambert() * ComputeWrappedDiffuseLighting(-NdotL, SSS_WRAP_LIGHT);

        // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
        diffuseLighting += EvaluateTransmission(bsdfData, illuminance * lightData.diffuseScale, shadow);
    }
#endif

    // Save ALU by applying 'lightData.color' only once.
    diffuseLighting  *= lightData.color;
    specularLighting *= lightData.color;
}

#include "Lit/LitReference.hlsl"

//-----------------------------------------------------------------------------
// EvaluateBSDF_Line - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

void EvaluateBSDF_Line(LightLoopContext lightLoopContext,
                       float3 V, PositionInputs posInput,
                       PreLightData preLightData, LightData lightData, BSDFData bsdfData,
                       out float3 diffuseLighting, out float3 specularLighting)
{
    float3 positionWS = posInput.positionWS;

#ifdef LIT_DISPLAY_REFERENCE_AREA
    IntegrateBSDF_LineRef(V, positionWS, preLightData, lightData, bsdfData,
                          diffuseLighting, specularLighting);
#else
    diffuseLighting  = float3(0.0, 0.0, 0.0);
    specularLighting = float3(0.0, 0.0, 0.0);

    float  len = lightData.size.x;
    float3 T   = lightData.right;

    float3 unL = lightData.positionWS - positionWS;

    // Pick the major axis of the ellipsoid.
    float3 axis = lightData.right;

    // We define the ellipsoid s.t. r1 = (r + len / 2), r2 = r3 = r.
    // TODO: This could be precomputed.
    float radius         = rsqrt(lightData.invSqrAttenuationRadius);
    float invAspectRatio = radius / (radius + (0.5 * len));

    // Compute the light attenuation.
    float intensity = GetEllipsoidalDistanceAttenuation(unL,  lightData.invSqrAttenuationRadius,
                                                        axis, invAspectRatio);

    // Terminate if the shaded point is too far away.
    if (intensity == 0.0) return;

    lightData.diffuseScale  *= intensity;
    lightData.specularScale *= intensity;

    // Translate the light s.t. the shaded point is at the origin of the coordinate system.
    lightData.positionWS -= positionWS;

    // TODO: some of this could be precomputed.
    float3 P1 = lightData.positionWS - T * (0.5 * len);
    float3 P2 = lightData.positionWS + T * (0.5 * len);

    // Rotate the endpoints into the local coordinate system.
    P1 = mul(P1, transpose(preLightData.orthoBasisViewNormal));
    P2 = mul(P2, transpose(preLightData.orthoBasisViewNormal));

    // Compute the binormal in the local coordinate system.
    float3 B = normalize(cross(P1, P2));

    float ltcValue;

    // Evaluate the diffuse part
    {
        ltcValue  = LTCEvaluate(P1, P2, B, preLightData.ltcTransformDiffuse);
        ltcValue *= lightData.diffuseScale;
        diffuseLighting = bsdfData.diffuseColor * (preLightData.ltcMagnitudeDiffuse * ltcValue);
    }

#ifdef WANT_SSS_CODE
    [branch] if (bsdfData.enableTransmission)
    {
        // Flip the view vector and the normal. The bitangent stays the same.
        float3x3 flipMatrix = float3x3(-1,  0,  0,
                                        0,  1,  0,
                                        0,  0, -1);

        // Use the Lambertian approximation for performance reasons.
        // The matrix multiplication should not generate any extra ALU on GCN.
        ltcValue  = LTCEvaluate(P1, P2, B, mul(flipMatrix, k_identity3x3));
        ltcValue *= lightData.diffuseScale;

        // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
        diffuseLighting += EvaluateTransmission(bsdfData, ltcValue, 1);
    }
#endif

    // Evaluate the specular part
    {
        ltcValue  = LTCEvaluate(P1, P2, B, preLightData.ltcTransformSpecular);
        ltcValue *= lightData.specularScale;
        specularLighting += preLightData.ltcMagnitudeFresnel * ltcValue;
    }

    // Save ALU by applying 'lightData.color' only once.
    diffuseLighting  *= lightData.color;
    specularLighting *= lightData.color;
#endif // LIT_DISPLAY_REFERENCE_AREA
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Area - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

// #define ELLIPSOIDAL_ATTENUATION

void EvaluateBSDF_Rect( LightLoopContext lightLoopContext,
                        float3 V, PositionInputs posInput,
                        PreLightData preLightData, LightData lightData, BSDFData bsdfData,
                        out float3 diffuseLighting, out float3 specularLighting)
{
    float3 positionWS = posInput.positionWS;

#ifdef LIT_DISPLAY_REFERENCE_AREA
    IntegrateBSDF_AreaRef(V, positionWS, preLightData, lightData, bsdfData,
                          diffuseLighting, specularLighting);
#else
    diffuseLighting  = float3(0.0, 0.0, 0.0);
    specularLighting = float3(0.0, 0.0, 0.0);

    float3 unL = lightData.positionWS - positionWS;

    [branch]
    if (dot(lightData.forward, unL) >= 0.0001)
    {
        // The light is back-facing.
        return;
    }

    // Rotate the light direction into the light space.
    float3x3 lightToWorld = float3x3(lightData.right, lightData.up, -lightData.forward);
    unL = mul(unL, transpose(lightToWorld));

    // TODO: This could be precomputed.
    float halfWidth  = lightData.size.x * 0.5;
    float halfHeight = lightData.size.y * 0.5;

    // Define the dimensions of the attenuation volume.
    // TODO: This could be precomputed.
    float  radius     = rsqrt(lightData.invSqrAttenuationRadius);
    float3 invHalfDim = rcp(float3(radius + halfWidth,
                                   radius + halfHeight,
                                   radius));

    // Compute the light attenuation.
#ifdef ELLIPSOIDAL_ATTENUATION
    // The attenuation volume is an axis-aligned ellipsoid s.t.
    // r1 = (r + w / 2), r2 = (r + h / 2), r3 = r.
    float intensity = GetEllipsoidalDistanceAttenuation(unL, invHalfDim);
#else
    // The attenuation volume is an axis-aligned box s.t.
    // hX = (r + w / 2), hY = (r + h / 2), hZ = r.
    float intensity = GetBoxDistanceAttenuation(unL, invHalfDim);
#endif

    // Terminate if the shaded point is too far away.
    if (intensity == 0.0) return;

    lightData.diffuseScale  *= intensity;
    lightData.specularScale *= intensity;

    // Translate the light s.t. the shaded point is at the origin of the coordinate system.
    lightData.positionWS -= positionWS;

    float4x3 lightVerts;

    // TODO: some of this could be precomputed.
    lightVerts[0] = lightData.positionWS + lightData.right *  halfWidth + lightData.up *  halfHeight;
    lightVerts[1] = lightData.positionWS + lightData.right *  halfWidth + lightData.up * -halfHeight;
    lightVerts[2] = lightData.positionWS + lightData.right * -halfWidth + lightData.up * -halfHeight;
    lightVerts[3] = lightData.positionWS + lightData.right * -halfWidth + lightData.up *  halfHeight;

    // Rotate the endpoints into the local coordinate system.
    lightVerts = mul(lightVerts, transpose(preLightData.orthoBasisViewNormal));

    float ltcValue;

    // Evaluate the diffuse part
    {
        // Polygon irradiance in the transformed configuration.
        ltcValue  = PolygonIrradiance(mul(lightVerts, preLightData.ltcTransformDiffuse));
        ltcValue *= lightData.diffuseScale;
        diffuseLighting = bsdfData.diffuseColor * (preLightData.ltcMagnitudeDiffuse * ltcValue);
    }

#ifdef WANT_SSS_CODE
    [branch] if (bsdfData.enableTransmission)
    {
        // Flip the view vector and the normal. The bitangent stays the same.
        float3x3 flipMatrix = float3x3(-1,  0,  0,
                                        0,  1,  0,
                                        0,  0, -1);

        // Use the Lambertian approximation for performance reasons.
        // The matrix multiplication should not generate any extra ALU on GCN.
        float3x3 ltcTransform = mul(flipMatrix, k_identity3x3);

        // Polygon irradiance in the transformed configuration.
        ltcValue  = PolygonIrradiance(mul(lightVerts, ltcTransform));
        ltcValue *= lightData.diffuseScale;

        // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
        diffuseLighting += EvaluateTransmission(bsdfData, ltcValue, 1);
    }
#endif

    // Evaluate the specular part
    {
        // Polygon irradiance in the transformed configuration.
        ltcValue  = PolygonIrradiance(mul(lightVerts, preLightData.ltcTransformSpecular));
        ltcValue *= lightData.specularScale;
        specularLighting += preLightData.ltcMagnitudeFresnel * ltcValue;
    }

    // Save ALU by applying 'lightData.color' only once.
    diffuseLighting  *= lightData.color;
    specularLighting *= lightData.color;
#endif // LIT_DISPLAY_REFERENCE_AREA
}

void EvaluateBSDF_Area(LightLoopContext lightLoopContext,
    float3 V, PositionInputs posInput,
    PreLightData preLightData, LightData lightData, BSDFData bsdfData, int GPULightType,
    out float3 diffuseLighting, out float3 specularLighting)
{
    if (GPULightType == GPULIGHTTYPE_LINE)
    {
        EvaluateBSDF_Line(lightLoopContext, V, posInput, preLightData, lightData, bsdfData, diffuseLighting, specularLighting);
    }
    else
    {
        EvaluateBSDF_Rect(lightLoopContext, V, posInput, preLightData, lightData, bsdfData, diffuseLighting, specularLighting);
    }
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Env
// ----------------------------------------------------------------------------

// _preIntegratedFGD and _CubemapLD are unique for each BRDF
void EvaluateBSDF_Env(  LightLoopContext lightLoopContext,
                        float3 V, PositionInputs posInput, PreLightData preLightData, EnvLightData lightData, BSDFData bsdfData,
                        out float3 diffuseLighting, out float3 specularLighting, out float2 weight)
{
    float3 positionWS = posInput.positionWS;

#ifdef LIT_DISPLAY_REFERENCE_IBL

    specularLighting = IntegrateSpecularGGXIBLRef(lightLoopContext, V, preLightData, lightData, bsdfData);

/*
    #ifdef LIT_DIFFUSE_LAMBERT_BRDF
    diffuseLighting = IntegrateLambertIBLRef(lightData, V, bsdfData);
    #else
    diffuseLighting = IntegrateDisneyDiffuseIBLRef(lightLoopContext, V, preLightData, lightData, bsdfData);
    #endif
*/
    diffuseLighting = float3(0.0, 0.0, 0.0);

    weight = float2(0.0, 1.0);

#else
    // TODO: factor this code in common, so other material authoring don't require to rewrite everything,
    // also think about how such a loop can handle 2 cubemap at the same time as old unity. Macro can allow to do that
    // but we need to have UNITY_SAMPLE_ENV_LOD replace by a true function instead that is define by the lighting arcitecture.
    // Also not sure how to deal with 2 intersection....
    // Box and sphere are related to light property (but we have also distance based roughness etc...)

    // TODO: test the strech from Tomasz
    // float shrinkedRoughness = AnisotropicStrechAtGrazingAngle(bsdfData.roughness, bsdfData.perceptualRoughness, NdotV);

    // In this code we redefine a bit the behavior of the reflcetion proble. We separate the projection volume (the proxy of the scene) form the influence volume (what pixel on the screen is affected)

    // 1. First determine the projection volume

    // In Unity the cubemaps are capture with the localToWorld transform of the component.
    // This mean that location and oritention matter. So after intersection of proxy volume we need to convert back to world.

    // CAUTION: localToWorld is the transform use to convert the cubemap capture point to world space (mean it include the offset)
    // the center of the bounding box is thus in locals space: positionLS - offsetLS
    // We use this formulation as it is the one of legacy unity that was using only AABB box.

    float3 R = preLightData.iblDirWS;

    float3x3 worldToLocal = transpose(float3x3(lightData.right, lightData.up, lightData.forward)); // worldToLocal assume no scaling
    float3 positionLS = positionWS - lightData.positionWS;
    positionLS = mul(positionLS, worldToLocal).xyz - lightData.offsetLS; // We want to calculate the intersection from the center of the bounding box.

    if (lightData.envShapeType == ENVSHAPETYPE_SPHERE)
    {
        float3 dirLS = mul(R, worldToLocal);
        float sphereOuterDistance = lightData.innerDistance.x + lightData.blendDistance;
        float dist = SphereRayIntersectSimple(positionLS, dirLS, sphereOuterDistance);

        R = (positionWS + dist * R) - lightData.positionWS;
    }
    else if (lightData.envShapeType == ENVSHAPETYPE_BOX)
    {
        float3 dirLS = mul(R, worldToLocal);
        float3 boxOuterDistance = lightData.innerDistance + float3(lightData.blendDistance, lightData.blendDistance, lightData.blendDistance);
        float dist = BoxRayIntersectSimple(positionLS, dirLS, -boxOuterDistance, boxOuterDistance);

        // No need to normalize for fetching cubemap
        // We can reuse dist calculate in LS directly in WS as there is no scaling. Also the offset is already include in lightData.positionWS
        R = (positionWS + dist * R) - lightData.positionWS;

        // TODO: add distance based roughness
    }

    // 2. Apply the influence volume (Box volume is used for culling whatever the influence shape)
    // TODO: In the future we could have an influence volume inside the projection volume (so with a different transform, in this case we will need another transform)
    weight.y = 1.0;

    if (lightData.envShapeType == ENVSHAPETYPE_SPHERE)
    {
        float distFade = max(length(positionLS) - lightData.innerDistance.x, 0.0);
        weight.y = saturate(1.0 - distFade / max(lightData.blendDistance, 0.0001)); // avoid divide by zero
    }
    else if (lightData.envShapeType == ENVSHAPETYPE_BOX ||
             lightData.envShapeType == ENVSHAPETYPE_NONE)
    {
        // Calculate falloff value, so reflections on the edges of the volume would gradually blend to previous reflection.
        float distFade = DistancePointBox(positionLS, -lightData.innerDistance, lightData.innerDistance);
        weight.y = saturate(1.0 - distFade / max(lightData.blendDistance, 0.0001)); // avoid divide by zero
    }

    // Smooth weighting
    weight.x = 0.0;
    weight.y = Smoothstep01(weight.y);

    float3 F = 1.0;
    specularLighting = float3(0.0, 0.0, 0.0);

    float4 preLD = SampleEnv(lightLoopContext, lightData.envIndex, R, preLightData.iblMipLevel);
    specularLighting += F * preLD.rgb * preLightData.specularFGD;

    diffuseLighting = float3(0.0, 0.0, 0.0);

#endif
}

//-----------------------------------------------------------------------------
// PostEvaluateBSDF
// ----------------------------------------------------------------------------

void PostEvaluateBSDF(  LightLoopContext lightLoopContext, PreLightData preLightData, BSDFData bsdfData, LightLoopAccumulatedLighting accLighting, float3 bakeDiffuseLighting,
                        out float3 diffuseLighting, out float3 specularLighting)
{
    // Add indirect diffuse + emissive (if any) - Ambient occlusion is multiply by emissive which is wrong but not a big deal
    bakeDiffuseLighting *= GTAOMultiBounce(lightLoopContext.indirectAmbientOcclusion, bsdfData.diffuseColor);

    float specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(preLightData.NdotV, lightLoopContext.indirectAmbientOcclusion, bsdfData.roughness);
    // Try to mimic multibounce with specular color. Not the point of the original formula but ok result.
    // Take the min of screenspace specular occlusion and visibility cone specular occlusion
    accLighting.envSpecularLighting *= GTAOMultiBounce(min(bsdfData.specularOcclusion, specularOcclusion), bsdfData.fresnel0);

    // TODO: we could call a function like PostBSDF that will apply albedo and divide by PI once for the loop

    // envDiffuseLighting is not used in our case
    diffuseLighting = (accLighting.dirDiffuseLighting + accLighting.punctualDiffuseLighting + accLighting.areaDiffuseLighting) * GTAOMultiBounce(lightLoopContext.directAmbientOcclusion, bsdfData.diffuseColor) + bakeDiffuseLighting;
    specularLighting = accLighting.dirSpecularLighting + accLighting.punctualSpecularLighting + accLighting.areaSpecularLighting + accLighting.envSpecularLighting;
}


#endif // #ifdef HAS_LIGHTLOOP
