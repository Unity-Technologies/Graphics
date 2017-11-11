#ifdef HAS_LIGHTLOOP

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
        isInBounds = Max3(abs(positionNDC.x), abs(positionNDC.y), 1.0 - positionLS.z) <= 1.0;
    }

    // We let the sampler handle tiling or clamping to border.
    // Note: tiling (the repeat mode) is not currently supported.
    float4 cookie = SampleCookie2D(lightLoopContext, coord, lightData.cookieIndex);

    cookie.a = isInBounds ? cookie.a : 0.0;

    return cookie;
}

DirectLighting EvaluateBSDF_Directional(    LightLoopContext lightLoopContext,
                                            float3 V, PositionInputs posInput, PreLightData preLightData,
                                            DirectionalLightData lightData, BSDFData bsdfData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 positionWS = posInput.positionWS;

    float3 L = -lightData.forward; // Lights are pointing backward in Unity
    float NdotL = dot(bsdfData.normalWS, L);
    float illuminance = saturate(NdotL);

    float shadow     = 1.0;

    [branch] if (lightData.shadowIndex >= 0)
    {
#ifdef _SURFACE_TYPE_TRANSPARENT
        shadow = GetDirectionalShadowAttenuation(lightLoopContext.shadowContext, positionWS, bsdfData.normalWS, lightData.shadowIndex, L, posInput.unPositionSS);
#else
        shadow = LOAD_TEXTURE2D(_DeferredShadowTexture, posInput.unPositionSS).x;
#endif
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
        BSDF(V, L, positionWS, preLightData, bsdfData, lighting.diffuse, lighting.specular);

        lighting.diffuse *= illuminance * lightData.diffuseScale;
        lighting.specular *= illuminance * lightData.specularScale;
    }

#ifdef WANT_SSS_CODE
    [branch] if (bsdfData.enableTransmission)
    {
        // We apply wrapped lighting instead of the regular Lambertian diffuse
        // to compensate for approximations within EvaluateTransmission().
        float illuminance = Lambert() * ComputeWrappedDiffuseLighting(-NdotL, SSS_WRAP_LIGHT);

        // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
        lighting.diffuse += EvaluateTransmission(bsdfData, illuminance * lightData.diffuseScale, shadow);
    }
#endif

    // Save ALU by applying 'lightData.color' only once.
    lighting.diffuse *= lightData.color;
    lighting.specular *= lightData.color;

    return lighting;
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

DirectLighting EvaluateBSDF_Punctual(   LightLoopContext lightLoopContext,
                                        float3 V, PositionInputs posInput,
                                        PreLightData preLightData, LightData lightData, BSDFData bsdfData, int GPULightType)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 positionWS = posInput.positionWS;
    int    lightType  = GPULightType;

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

    float shadow = 1.0;

    [branch] if (lightData.shadowIndex >= 0)
    {
        // TODO: make projector lights cast shadows.
        float3 offset = float3(0.0, 0.0, 0.0); // GetShadowPosOffset(nDotL, normal);
        float4 L_dist = { L, dist };
        shadow = GetPunctualShadowAttenuation(lightLoopContext.shadowContext, positionWS + offset, bsdfData.normalWS, lightData.shadowIndex, L_dist, posInput.unPositionSS);
        shadow = lerp(1.0, shadow, lightData.shadowDimmer);
        illuminance *= shadow;
    }

#ifdef VOLUMETRIC_SHADOWING_ENABLED
    float volumetricShadow = Transmittance(OpticalDepthHomogeneous(preLightData.globalFogExtinction, dist));

    // Premultiply.
    lightData.diffuseScale  *= volumetricShadow;
    lightData.specularScale *= volumetricShadow;
#endif

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
        bsdfData.roughness = max(bsdfData.roughness, lightData.minRoughness); // Simulate that a punctual light have a radius with this hack
        BSDF(V, L, positionWS, preLightData, bsdfData, lighting.diffuse, lighting.specular);

        lighting.diffuse *= illuminance * lightData.diffuseScale;
        lighting.specular *= illuminance * lightData.specularScale;
    }

#ifdef WANT_SSS_CODE
    [branch] if (bsdfData.enableTransmission)
    {
        // We apply wrapped lighting instead of the regular Lambertian diffuse
        // to compensate for approximations within EvaluateTransmission().
        float illuminance = Lambert() * ComputeWrappedDiffuseLighting(-NdotL, SSS_WRAP_LIGHT);

        // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
        lighting.diffuse += EvaluateTransmission(bsdfData, illuminance * lightData.diffuseScale, shadow);
    }
#endif

    // Save ALU by applying 'lightData.color' only once.
    lighting.diffuse *= lightData.color;
    lighting.specular *= lightData.color;

    return lighting;
}

#include "Lit\LitReference.hlsl"

//-----------------------------------------------------------------------------
// EvaluateBSDF_Line - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Line(   LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, LightData lightData, BSDFData bsdfData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 positionWS = posInput.positionWS;

#ifdef LIT_DISPLAY_REFERENCE_AREA
    IntegrateBSDF_LineRef(V, positionWS, preLightData, lightData, bsdfData,
                          lighting.diffuse, lighting.specular);
#else
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
    if (intensity == 0.0)
        return lighting;

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
        lighting.diffuse = bsdfData.diffuseColor * (preLightData.ltcMagnitudeDiffuse * ltcValue);
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
        lighting.diffuse += EvaluateTransmission(bsdfData, ltcValue, 1);
    }
#endif

    // Evaluate the specular part
    {
        ltcValue  = LTCEvaluate(P1, P2, B, preLightData.ltcTransformSpecular);
        ltcValue *= lightData.specularScale;
        lighting.specular += preLightData.ltcMagnitudeFresnel * ltcValue;
    }

    // Save ALU by applying 'lightData.color' only once.
    lighting.diffuse *= lightData.color;
    lighting.specular *= lightData.color;
#endif // LIT_DISPLAY_REFERENCE_AREA

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Area - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

// #define ELLIPSOIDAL_ATTENUATION

DirectLighting EvaluateBSDF_Rect(   LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, LightData lightData, BSDFData bsdfData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 positionWS = posInput.positionWS;

#ifdef LIT_DISPLAY_REFERENCE_AREA
    IntegrateBSDF_AreaRef(V, positionWS, preLightData, lightData, bsdfData,
                          lighting.diffuse, lighting.specular);
#else
    float3 unL = lightData.positionWS - positionWS;

    [branch]
    if (dot(lightData.forward, unL) >= 0.0001)
    {
        // The light is back-facing.
        return lighting;
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
    if (intensity == 0.0)
        return lighting;

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
        lighting.diffuse = bsdfData.diffuseColor * (preLightData.ltcMagnitudeDiffuse * ltcValue);
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
        lighting.diffuse += EvaluateTransmission(bsdfData, ltcValue, 1);
    }
#endif

    // Evaluate the specular part
    {
        // Polygon irradiance in the transformed configuration.
        ltcValue  = PolygonIrradiance(mul(lightVerts, preLightData.ltcTransformSpecular));
        ltcValue *= lightData.specularScale;
        lighting.specular += preLightData.ltcMagnitudeFresnel * ltcValue;
    }

    // Save ALU by applying 'lightData.color' only once.
    lighting.diffuse *= lightData.color;
    lighting.specular *= lightData.color;
#endif // LIT_DISPLAY_REFERENCE_AREA

    return lighting;
}

DirectLighting EvaluateBSDF_Area(   LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, LightData lightData, BSDFData bsdfData, int GPULightType)
{
    if (GPULightType == GPULIGHTTYPE_LINE)
    {
        return EvaluateBSDF_Line(lightLoopContext, V, posInput, preLightData, lightData, bsdfData);
    }
    else
    {
        return EvaluateBSDF_Rect(lightLoopContext, V, posInput, preLightData, lightData, bsdfData);
    }
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_SSLighting for screen space lighting
// ----------------------------------------------------------------------------

IndirectLighting EvaluateBSDF_SSReflection(LightLoopContext lightLoopContext,
                                            float3 V, PositionInputs posInput,
                                            PreLightData preLightData, BSDFData bsdfData,
                                            inout float hierarchyWeight)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    // TODO

    return lighting;
}

IndirectLighting EvaluateBSDF_SSRefraction(LightLoopContext lightLoopContext,
                                            float3 V, PositionInputs posInput,
                                            PreLightData preLightData, BSDFData bsdfData,
                                            inout float hierarchyWeight)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

#if HAS_REFRACTION
    // Refraction process:
    //  1. Depending on the shape model, we calculate the refracted point in world space and the optical depth
    //  2. We calculate the screen space position of the refracted point
    //  3. If this point is available (ie: in color buffer and point is not in front of the object)
    //    a. Get the corresponding color depending on the roughness from the gaussian pyramid of the color buffer
    //    b. Multiply by the transmittance for absorption (depends on the optical depth)

    float3 refractedBackPointWS = float3(0.0, 0.0, 0.0);
    float opticalDepth = 0.0;

    uint2 depthSize = uint2(_PyramidDepthMipSize.xy);

    // For all refraction approximation, to calculate the refracted point in world space,
    //   we approximate the scene as a plane (back plane) with normal -V at the depth hit point.
    //   (We avoid to raymarch the depth texture to get the refracted point.)
#if defined(_REFRACTION_PLANE)
    // Plane shape model:
    //  We approximate locally the shape of the object as a plane with normal {bsdfData.normalWS} at {bsdfData.positionWS}
    //  with a thickness {bsdfData.thickness}

    // Refracted ray
    float3 R = refract(-V, bsdfData.normalWS, 1.0 / bsdfData.ior);

    // Get the depth of the approximated back plane
    float pyramidDepth = LOAD_TEXTURE2D_LOD(_PyramidDepthTexture, posInput.positionSS * (depthSize >> 2), 2).r;
    float depth = LinearEyeDepth(pyramidDepth, _ZBufferParams);

    // Distance from point to the back plane
    float distFromP = depth - posInput.depthVS;

    // Optical depth within the thin plane
    opticalDepth = bsdfData.thickness / dot(R, -bsdfData.normalWS);

    // The refracted ray exiting the thin plane is the same as the incident ray (parallel interfaces and same ior)
    float VoR = dot(-V, R);
    float VoN = dot(V, bsdfData.normalWS);
    refractedBackPointWS = posInput.positionWS + R * opticalDepth - V * (distFromP - VoR * opticalDepth);

#elif defined(_REFRACTION_SPHERE)
    // Sphere shape model:
    //  We approximate locally the shape of the object as sphere, that is tangent to the shape.
    //  The sphere has a diameter of {bsdfData.thickness}
    //  The center of the sphere is at {bsdfData.positionWS} - {bsdfData.normalWS} * {bsdfData.thickness}
    //
    //  So the light is refracted twice: in and out of the tangent sphere

    // Get the depth of the approximated back plane
    float pyramidDepth = LOAD_TEXTURE2D_LOD(_PyramidDepthTexture, posInput.positionSS * (depthSize >> 2), 2).r;
    float depth = LinearEyeDepth(pyramidDepth, _ZBufferParams);

    // Distance from point to the back plane
    float depthFromPosition = depth - posInput.depthVS;

    // First refraction (tangent sphere in)
    // Refracted ray
    float3 R1 = refract(-V, bsdfData.normalWS, 1.0 / bsdfData.ior);
    // Center of the tangent sphere
    float3 C = posInput.positionWS - bsdfData.normalWS * bsdfData.thickness * 0.5;

    // Second refraction (tangent sphere out)
    float NoR1 = dot(bsdfData.normalWS, R1);
    // Optical depth within the sphere
    opticalDepth = -NoR1 * bsdfData.thickness;
    // Out hit point in the tangent sphere
    float3 P1 = posInput.positionWS + R1 * opticalDepth;
    // Out normal
    float3 N1 = normalize(C - P1);
    // Out refracted ray
    float3 R2 = refract(R1, N1, bsdfData.ior);
    float N1oR2 = dot(N1, R2);
    float VoR1 = dot(V, R1);

    // Refracted source point
    refractedBackPointWS = P1 - R2 * (depthFromPosition - NoR1 * VoR1 * bsdfData.thickness) / N1oR2;

#endif

    // Calculate screen space coordinates of refracted point in back plane
    float4 refractedBackPointCS = mul(_ViewProjMatrix, float4(refractedBackPointWS, 1.0));
    float2 refractedBackPointSS = ComputeScreenSpacePosition(refractedBackPointCS);
    float refractedBackPointDepth = LinearEyeDepth(LOAD_TEXTURE2D_LOD(_PyramidDepthTexture, refractedBackPointSS * depthSize, 0).r, _ZBufferParams);

    // Exit if texel is out of color buffer
    // Or if the texel is from an object in front of the object
    if (refractedBackPointDepth < posInput.depthVS
        || any(refractedBackPointSS < 0.0)
        || any(refractedBackPointSS > 1.0))
    {
        // Do nothing and don't update the hierarchy weight so we can fall back on refraction probe
        return lighting;
    }

    // Map the roughness to the correct mip map level of the color pyramid
    float mipLevel = PerceptualRoughnessToMipmapLevel(bsdfData.perceptualRoughness, uint(_GaussianPyramidColorMipSize.z));
    lighting.specularTransmitted = SAMPLE_TEXTURE2D_LOD(_GaussianPyramidColorTexture, s_trilinear_clamp_sampler, refractedBackPointSS, mipLevel).rgb;

    // Beer-Lamber law for absorption
    float3 transmittance = exp(-bsdfData.absorptionCoefficient * opticalDepth);
    lighting.specularTransmitted *= transmittance;

    float weight = 1.0;
    UpdateLightingHierarchyWeights(hierarchyWeight, weight); // Shouldn't be needed, but safer in case we decide to change hiearchy priority
    lighting.specularTransmitted *= weight;
#else
    // No refraction, no need to go further
    hierarchyWeight = 1.0;
#endif

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Env
// ----------------------------------------------------------------------------

// _preIntegratedFGD and _CubemapLD are unique for each BRDF
IndirectLighting EvaluateBSDF_Env(  LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, EnvLightData lightData, BSDFData bsdfData, int envShapeType, int GPUImageBasedLightingType,
                                    inout float hierarchyWeight)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);
#if !HAS_REFRACTION
    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFRACTION)
        return lighting;
#endif

    float3 envLighting = float3(0.0, 0.0, 0.0);
    float3 positionWS = posInput.positionWS;
    float weight = 1.0;

#ifdef LIT_DISPLAY_REFERENCE_IBL

    envLighting = IntegrateSpecularGGXIBLRef(lightLoopContext, V, preLightData, lightData, bsdfData);

    // TODO: Do refraction reference (is it even possible ?)


//    #ifdef LIT_DIFFUSE_LAMBERT_BRDF
//    envLighting += IntegrateLambertIBLRef(lightData, V, bsdfData);
//    #else
//    envLighting += IntegrateDisneyDiffuseIBLRef(lightLoopContext, V, preLightData, lightData, bsdfData);
//    #endif

    weight = 1.0;

#else

    // TODO: factor this code in common, so other material authoring don't require to rewrite everything,
    // TODO: test the strech from Tomasz
    // float shrinkedRoughness = AnisotropicStrechAtGrazingAngle(bsdfData.roughness, bsdfData.perceptualRoughness, NdotV);

    // Guideline for reflection volume: In HDRenderPipeline we separate the projection volume (the proxy of the scene) from the influence volume (what pixel on the screen is affected)
    // However we add the constrain that the shape of the projection and influence volume is the same (i.e if we have a sphere shape projection volume, we have a shape influence).
    // It allow to have more coherence for the dynamic if in shader code.
    // Users can also chose to not have any projection, in this case we use the property minProjectionDistance to minimize code change. minProjectionDistance is set to huge number
    // that simulate effect of no shape projection

    float3 R = preLightData.iblDirWS;

    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFRACTION)
    {
        // This is the same code than what is use in screen space refraction
        // TODO: put this code into a function
#if defined(_REFRACTION_PLANE)
        R = refract(-V, bsdfData.normalWS, 1.0 / bsdfData.ior);
#elif defined(_REFRACTION_SPHERE)
        float3 R1 = refract(-V, bsdfData.normalWS, 1.0 / bsdfData.ior);
        // Center of the tangent sphere
        float3 C = posInput.positionWS - bsdfData.normalWS * bsdfData.thickness * 0.5;
        // Second refraction (tangent sphere out)
        float NoR1 = dot(bsdfData.normalWS, R1);
        // Optical depth within the sphere
        float opticalDepth = -NoR1 * bsdfData.thickness;
        // Out hit point in the tangent sphere
        float3 P1 = posInput.positionWS + R1 * opticalDepth;
        // Out normal
        float3 N1 = normalize(C - P1);
        // Out refracted ray
        R = refract(R1, N1, bsdfData.ior);
#endif
    }

    // In Unity the cubemaps are capture with the localToWorld transform of the component.
    // This mean that location and orientation matter. So after intersection of proxy volume we need to convert back to world.

    // CAUTION: localToWorld is the transform use to convert the cubemap capture point to world space (mean it include the offset)
    // the center of the bounding box is thus in locals space: positionLS - offsetLS
    // We use this formulation as it is the one of legacy unity that was using only AABB box.
    float3x3 worldToLocal = transpose(float3x3(lightData.right, lightData.up, lightData.forward)); // worldToLocal assume no scaling
    float3 positionLS = positionWS - lightData.positionWS;
    positionLS = mul(positionLS, worldToLocal).xyz - lightData.offsetLS; // We want to calculate the intersection from the center of the bounding box.

    // Note: using envShapeType instead of lightData.envShapeType allow to make compiler optimization in case the type is know (like for sky)
    if (envShapeType == ENVSHAPETYPE_SPHERE)
    {
        // 1. First process the projection
        float3 dirLS = mul(R, worldToLocal);
        float sphereOuterDistance = lightData.innerDistance.x + lightData.blendDistance;
        float dist = SphereRayIntersectSimple(positionLS, dirLS, sphereOuterDistance);
        dist = max(dist, lightData.minProjectionDistance); // Setup projection to infinite if requested (mean no projection shape)
        // We can reuse dist calculate in LS directly in WS as there is no scaling. Also the offset is already include in lightData.positionWS
        R = (positionWS + dist * R) - lightData.positionWS;

        // 2. Process the influence
        float distFade = max(length(positionLS) - lightData.innerDistance.x, 0.0);
        weight = saturate(1.0 - distFade / max(lightData.blendDistance, 0.0001)); // avoid divide by zero
    }
    else if (envShapeType == ENVSHAPETYPE_BOX)
    {
        float3 dirLS = mul(R, worldToLocal);
        float3 boxOuterDistance = lightData.innerDistance + float3(lightData.blendDistance, lightData.blendDistance, lightData.blendDistance);
        float dist = BoxRayIntersectSimple(positionLS, dirLS, -boxOuterDistance, boxOuterDistance);
        dist = max(dist, lightData.minProjectionDistance); // Setup projection to infinite if requested (mean no projection shape)
        // No need to normalize for fetching cubemap
        // We can reuse dist calculate in LS directly in WS as there is no scaling. Also the offset is already include in lightData.positionWS
        R = (positionWS + dist * R) - lightData.positionWS;

        // TODO: add distance based roughness

        // Influence volume
        // Calculate falloff value, so reflections on the edges of the volume would gradually blend to previous reflection.
        float distFade = DistancePointBox(positionLS, -lightData.innerDistance, lightData.innerDistance);
        weight = saturate(1.0 - distFade / max(lightData.blendDistance, 0.0001)); // avoid divide by zero
    }

    // Smooth weighting
    weight = Smoothstep01(weight);

    float3 F = 1.0;

    float4 preLD = SampleEnv(lightLoopContext, lightData.envIndex, R, preLightData.iblMipLevel);
    envLighting += F * preLD.rgb * preLightData.specularFGD;

#endif

    UpdateLightingHierarchyWeights(hierarchyWeight, weight);

    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION)
        lighting.specularReflected = envLighting * weight;
    else
        lighting.specularTransmitted = envLighting * weight;

    return lighting;
}

//-----------------------------------------------------------------------------
// PostEvaluateBSDF
// ----------------------------------------------------------------------------

void PostEvaluateBSDF(  LightLoopContext lightLoopContext,
                        float3 V, PositionInputs posInput,
                        PreLightData preLightData, BSDFData bsdfData, float3 bakeDiffuseLighting, AggregateLighting lighting,
                        out float3 diffuseLighting, out float3 specularLighting)
{
    // Use GTAOMultiBounce approximation for ambient occlusion (allow to get a tint from the baseColor)
#define GTAO_MULTIBOUNCE_APPROX 1

    // Note: When we ImageLoad outside of texture size, the value returned by Load is 0 (Note: On Metal maybe it clamp to value of texture which is also fine)
    // We use this property to have a neutral value for AO that doesn't consume a sampler and work also with compute shader (i.e use ImageLoad)
    // We store inverse AO so neutral is black. So either we sample inside or outside the texture it return 0 in case of neutral

    // Ambient occlusion use for indirect lighting (reflection probe, baked diffuse lighting)
    float indirectAmbientOcclusion = 1.0 - LOAD_TEXTURE2D(_AmbientOcclusionTexture, posInput.unPositionSS).x;
    // Ambient occlusion use for direct lighting (directional, punctual, area)
    float directAmbientOcclusion = lerp(1.0, indirectAmbientOcclusion, _AmbientOcclusionParam.w);

    // Add indirect diffuse + emissive (if any) - Ambient occlusion is multiply by emissive which is wrong but not a big deal
#if GTAO_MULTIBOUNCE_APPROX
    bakeDiffuseLighting *= GTAOMultiBounce(indirectAmbientOcclusion, bsdfData.diffuseColor);
#else
    bakeDiffuseLighting *= lerp(_AmbientOcclusionParam.rgb, float3(1.0, 1.0, 1.0), indirectAmbientOcclusion);
#endif

    float specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(preLightData.NdotV, indirectAmbientOcclusion, bsdfData.roughness);
    // Try to mimic multibounce with specular color. Not the point of the original formula but ok result.
    // Take the min of screenspace specular occlusion and visibility cone specular occlusion
#if GTAO_MULTIBOUNCE_APPROX
    lighting.indirect.specularReflected *= GTAOMultiBounce(min(bsdfData.specularOcclusion, specularOcclusion), bsdfData.fresnel0);
#else
    lighting.indirect.specularReflected *= lerp(_AmbientOcclusionParam.rgb, float3(1.0, 1.0, 1.0), min(bsdfData.specularOcclusion, specularOcclusion));
#endif

    // TODO: we could call a function like PostBSDF that will apply albedo and divide by PI once for the loop

    lighting.direct.diffuse *=
#if GTAO_MULTIBOUNCE_APPROX
                                GTAOMultiBounce(directAmbientOcclusion, bsdfData.diffuseColor);
#else
                                lerp(_AmbientOcclusionParam.rgb, float3(1.0, 1.0, 1.0), directAmbientOcclusion);
#endif

    diffuseLighting = lighting.direct.diffuse + bakeDiffuseLighting;
    // If refraction is enable we use the transmittanceMask to lerp between current diffuse lighting and refraction value
    // Physically speaking, it should be transmittanceMask should be 1, but for artistic reasons, we let the value vary
#if HAS_REFRACTION
    diffuseLighting = lerp(diffuseLighting, lighting.indirect.specularTransmitted, bsdfData.transmittanceMask);
#endif

    specularLighting = lighting.direct.specular + lighting.indirect.specularReflected;
    // Rescale the GGX to account for the multiple scattering.
    specularLighting *= 1.0 + bsdfData.fresnel0 * preLightData.energyCompensation;

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_INDIRECT_DIFFUSE_OCCLUSION_FROM_SSAO)
    {
        diffuseLighting = indirectAmbientOcclusion;
        specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
    }
    else if (_DebugLightingMode == DEBUGLIGHTINGMODE_INDIRECT_SPECULAR_OCCLUSION_FROM_SSAO)
    {
        diffuseLighting = GetSpecularOcclusionFromAmbientOcclusion(preLightData.NdotV, indirectAmbientOcclusion, bsdfData.roughness);
        specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
    }
    #if GTAO_MULTIBOUNCE_APPROX
    else if (_DebugLightingMode == DEBUGLIGHTINGMODE_INDIRECT_DIFFUSE_GTAO_FROM_SSAO)
    {
        diffuseLighting = GTAOMultiBounce(indirectAmbientOcclusion, bsdfData.diffuseColor);
        specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
    }
    else if (_DebugLightingMode == DEBUGLIGHTINGMODE_INDIRECT_SPECULAR_GTAO_FROM_SSAO)
    {
        diffuseLighting = GTAOMultiBounce(specularOcclusion, bsdfData.fresnel0);
        specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
    }
    #endif
#endif
}

#endif // #ifdef HAS_LIGHTLOOP
