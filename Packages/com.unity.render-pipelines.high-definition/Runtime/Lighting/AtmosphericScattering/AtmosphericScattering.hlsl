#ifndef UNITY_ATMOSPHERIC_SCATTERING_INCLUDED
#define UNITY_ATMOSPHERIC_SCATTERING_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl"

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/VolumetricLighting/VBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/PhysicallyBasedSky/PhysicallyBasedSkyEvaluation.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/UnderWaterUtilities.hlsl"

#ifdef DEBUG_DISPLAY
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
#endif

StructuredBuffer<CelestialBodyData> _CelestialBodyDatas;

TEXTURE3D(_VBufferLighting);

float3 GetFogColor(float3 V, float fragDist)
{
    float3 color = _FogColor.rgb;

    if (_FogColorMode == FOGCOLORMODE_SKY_COLOR)
    {
        // Based on Uncharted 4 "Mip Sky Fog" trick: http://advances.realtimerendering.com/other/2016/naughty_dog/NaughtyDog_TechArt_Final.pdf
        float mipLevel = (1.0 - _MipFogMaxMip * saturate((fragDist - _MipFogNear) / (_MipFogFar - _MipFogNear))) * (ENVCONSTANTS_CONVOLUTION_MIP_COUNT - 1);
        // For the atmospheric scattering, we use the GGX convoluted version of the cubemap. That matches the of the idnex 0
        color *= SampleSkyTexture(-V, mipLevel, 0).rgb; // '_FogColor' is the tint
    }

    return color;
}

// All units in meters!
// Assumes that there is NO sky occlusion along the ray AT ALL.
// We evaluate atmospheric scattering for the sky and other celestial bodies
// during the sky pass. The opaque atmospheric scattering pass applies atmospheric
// scattering to all other opaque geometry.
void EvaluatePbrAtmosphere(float3 positionPS, float3 V, float distAlongRay, bool renderSunDisk,
                           out float3 skyColor, out float3 skyOpacity)
{
    skyColor = skyOpacity = 0;

    const float  R = _PlanetaryRadius;
    const float2 n = float2(_AirDensityFalloff, _AerosolDensityFalloff);
    const float2 H = float2(_AirScaleHeight,    _AerosolScaleHeight);
    const float3 O = positionPS;

    const float  tFrag = abs(distAlongRay); // Clear the "hit ground" flag

    float3 N; float r; // These params correspond to the entry point
    float  tEntry = IntersectAtmosphere(O, V, N, r).x;
    float  tExit  = IntersectAtmosphere(O, V, N, r).y;

    float NdotV  = dot(N, V);
    float cosChi = -NdotV;
    float cosHor = ComputeCosineOfHorizonAngle(r);

    bool rayIntersectsAtmosphere = (tEntry >= 0);
    bool lookAboveHorizon        = (cosChi >= cosHor);

    // Our precomputed tables only contain information above ground.
    // Being on or below ground still counts as outside.
    // If it's outside the atmosphere, we only need one texture look-up.
    bool hitGround = distAlongRay < 0;
    bool rayEndsInsideAtmosphere = (tFrag < tExit) && !hitGround;

    if (rayIntersectsAtmosphere)
    {
        float2 Z = R * n;
        float r0 = r, cosChi0 = cosChi;

        float r1 = 0, cosChi1 = 0;
        float3 N1 = 0;

        if (tFrag < tExit)
        {
            float3 P1 = O + tFrag * -V;

            r1      = length(P1);
            N1      = P1 * rcp(r1);
            cosChi1 = dot(P1, -V) * rcp(r1);

            // Potential swap.
            cosChi0 = (cosChi1 >= 0) ? cosChi0 : -cosChi0;
        }

        float2 ch0, ch1 = 0;

        {
            float2 z0 = r0 * n;

            ch0.x = RescaledChapmanFunction(z0.x, Z.x, cosChi0);
            ch0.y = RescaledChapmanFunction(z0.y, Z.y, cosChi0);
        }

        if (tFrag < tExit)
        {
            float2 z1 = r1 * n;

            ch1.x = ChapmanUpperApprox(z1.x, abs(cosChi1)) * exp(Z.x - z1.x);
            ch1.y = ChapmanUpperApprox(z1.y, abs(cosChi1)) * exp(Z.y - z1.y);
        }

        // We may have swapped X and Y.
        float2 ch = abs(ch0 - ch1);

        float3 optDepth = ch.x * H.x * _AirSeaLevelExtinction.xyz
                        + ch.y * H.y * _AerosolSeaLevelExtinction;

        skyOpacity = 1 - TransmittanceFromOpticalDepth(optDepth); // from 'tEntry' to 'tFrag'

        for (uint i = 0; i < _CelestialLightCount; i++)
        {
            CelestialBodyData light = _CelestialBodyDatas[i];
            float3 L = -light.forward.xyz;

            // The sun disk hack causes some issues when applied to nearby geometry, so don't do that.
            if (renderSunDisk && asint(light.angularRadius) != 0 && light.distanceFromCamera <= tFrag)
            {
                float c = dot(L, -V);

                if (-0.99999 < c && c < 0.99999)
                {
                    float alpha = light.angularRadius;
                    float beta  = acos(c);
                    float gamma = min(alpha, beta);

                    // Make sure that if (beta = Pi), no rotation is performed.
                    gamma *= (PI - beta) * rcp(PI - gamma);

                    // Perform a shortest arc rotation.
                    float3   A = normalize(cross(L, -V));
                    float3x3 R = RotationFromAxisAngle(A, sin(gamma), cos(gamma));

                    // Rotate the light direction.
                    L = mul(R, L);
                }
            }

            // TODO: solve in spherical coords?
            float height = r - R;
            float  NdotL = dot(N, L);
            float3 projL = L - N * NdotL;
            float3 projV = V - N * NdotV;
            float  phiL  = acos(clamp(dot(projL, projV) * rsqrt(max(dot(projL, projL) * dot(projV, projV), FLT_EPS)), -1, 1));

            TexCoord4D tc = ConvertPositionAndOrientationToTexCoords(height, NdotV, NdotL, phiL);

            float3 radiance = 0; // from 'tEntry' to 'tExit'

            // Single scattering does not contain the phase function.
            float LdotV = dot(L, V);

            // Air.
            radiance += lerp(SAMPLE_TEXTURE3D_LOD(_AirSingleScatteringTexture,     s_linear_clamp_sampler, float3(tc.u, tc.v, tc.w0), 0).rgb,
                             SAMPLE_TEXTURE3D_LOD(_AirSingleScatteringTexture,     s_linear_clamp_sampler, float3(tc.u, tc.v, tc.w1), 0).rgb,
                             tc.a) * AirPhase(LdotV);

            // Aerosols.
            // TODO: since aerosols are in a separate texture,
            // they could use a different max height value for improved precision.
            radiance += lerp(SAMPLE_TEXTURE3D_LOD(_AerosolSingleScatteringTexture, s_linear_clamp_sampler, float3(tc.u, tc.v, tc.w0), 0).rgb,
                             SAMPLE_TEXTURE3D_LOD(_AerosolSingleScatteringTexture, s_linear_clamp_sampler, float3(tc.u, tc.v, tc.w1), 0).rgb,
                             tc.a) * AerosolPhase(LdotV);

            // MS.
            radiance += lerp(SAMPLE_TEXTURE3D_LOD(_MultipleScatteringTexture,      s_linear_clamp_sampler, float3(tc.u, tc.v, tc.w0), 0).rgb,
                             SAMPLE_TEXTURE3D_LOD(_MultipleScatteringTexture,      s_linear_clamp_sampler, float3(tc.u, tc.v, tc.w1), 0).rgb,
                             tc.a) * MS_EXPOSURE_INV;

            if (rayEndsInsideAtmosphere)
            {
                float3 radiance1 = 0; // from 'tFrag' to 'tExit'

                // TODO: solve in spherical coords?
                float height1 = r1 - R;
                float  NdotV1 = -cosChi1;
                float  NdotL1 = dot(N1, L);
                float3 projL1 = L - N1 * NdotL1;
                float3 projV1 = V - N1 * NdotV1;
                float  phiL1  = acos(clamp(dot(projL1, projV1) * rsqrt(max(dot(projL1, projL1) * dot(projV1, projV1), FLT_EPS)), -1, 1));

                tc = ConvertPositionAndOrientationToTexCoords(height1, NdotV1, NdotL1, phiL1);

                // Single scattering does not contain the phase function.

                // Air.
                radiance1 += lerp(SAMPLE_TEXTURE3D_LOD(_AirSingleScatteringTexture,     s_linear_clamp_sampler, float3(tc.u, tc.v, tc.w0), 0).rgb,
                                  SAMPLE_TEXTURE3D_LOD(_AirSingleScatteringTexture,     s_linear_clamp_sampler, float3(tc.u, tc.v, tc.w1), 0).rgb,
                                  tc.a) * AirPhase(LdotV);

                // Aerosols.
                // TODO: since aerosols are in a separate texture,
                // they could use a different max height value for improved precision.
                radiance1 += lerp(SAMPLE_TEXTURE3D_LOD(_AerosolSingleScatteringTexture, s_linear_clamp_sampler, float3(tc.u, tc.v, tc.w0), 0).rgb,
                                  SAMPLE_TEXTURE3D_LOD(_AerosolSingleScatteringTexture, s_linear_clamp_sampler, float3(tc.u, tc.v, tc.w1), 0).rgb,
                                  tc.a) * AerosolPhase(LdotV);

                // MS.
                radiance1 += lerp(SAMPLE_TEXTURE3D_LOD(_MultipleScatteringTexture,      s_linear_clamp_sampler, float3(tc.u, tc.v, tc.w0), 0).rgb,
                                  SAMPLE_TEXTURE3D_LOD(_MultipleScatteringTexture,      s_linear_clamp_sampler, float3(tc.u, tc.v, tc.w1), 0).rgb,
                                  tc.a) * MS_EXPOSURE_INV;

                // L(tEntry, tFrag) = L(tEntry, tExit) - T(tEntry, tFrag) * L(tFrag, tExit)
                radiance = max(0, radiance - (1 - skyOpacity) * radiance1);
            }

            radiance *= light.color.rgb; // Globally scale the intensity

            skyColor += radiance;
        }

        #ifndef DISABLE_ATMOS_EVALUATE_ARTIST_OVERRIDE
        AtmosphereArtisticOverride(cosHor, cosChi, skyColor, skyOpacity);
        #endif
    }
}

void EvaluateAtmosphericScattering(float3 V, float2 positionNDC, float tFrag, out float3 skyColor, out float3 skyOpacity)
{
#if SHADEROPTIONS_PRECOMPUTED_ATMOSPHERIC_ATTENUATION
    EvaluateCameraAtmosphericScattering(V, positionNDC, tFrag, skyColor, skyOpacity);
#else
    float3 O = GetCameraPositionWS() - _PlanetCenterPosition;
    EvaluatePbrAtmosphere(O, -V, tFrag, false, skyColor, skyOpacity);
    skyColor *= _IntensityMultiplier * GetCurrentExposureMultiplier();
#endif
}

float3 GetViewForwardDir1(float4x4 viewMatrix)
{
    return -viewMatrix[2].xyz;
}

// Returns false when fog is not applied
bool EvaluateAtmosphericScattering(PositionInputs posInput, float3 V, out float3 color, out float3 opacity)
{
    color = opacity = 0;

#ifdef DEBUG_DISPLAY
    // Don't sample atmospheric scattering when lighting debug more are enabled so fog is not visible
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_MATCAP_VIEW || (_DebugLightingMode >= DEBUGLIGHTINGMODE_DIFFUSE_LIGHTING && _DebugLightingMode <= DEBUGLIGHTINGMODE_EMISSIVE_LIGHTING))
        return false;

    if (_DebugShadowMapMode == SHADOWMAPDEBUGMODE_SINGLE_SHADOW || _DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER || _DebugLightingMode == DEBUGLIGHTINGMODE_LUMINANCE_METER)
        return false;

    if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
        return false;
#endif

    #ifdef OPAQUE_FOG_PASS
    bool isSky = posInput.deviceDepth == UNITY_RAW_FAR_CLIP_VALUE;
    #else
    bool isSky = false;
    #endif

    // Convert depth to distance along the ray. Doesn't work with tilt shift, etc.
    // When a pixel is at far plane, the world space coordinate reconstruction is not reliable.
    // So in order to have a valid position (for example for height fog) we just consider that the sky is a sphere centered on camera with a radius of 5km (arbitrarily chosen value!)
    float tFrag = isSky ? _MaxFogDistance : posInput.linearDepth * rcp(dot(-V, GetViewForwardDir()));

    // Analytic fog starts where volumetric fog ends
    float volFogEnd = 0.0f;

    bool underWater = false;
#ifdef WATER_FOG_PASS
    underWater = IsUnderWater(posInput.positionSS.xy);
#elif defined(SUPPORT_WATER_ABSORPTION)
    // When viewing object trough the surface from above, we modify opacity
    // and early return cause fog will be applied on water directly
    if (EvaluateUnderwaterAbsorption(posInput, underWater, opacity))
        return false;
#endif

    if (_FogEnabled)
    {
        float4 volFog = float4(0.0, 0.0, 0.0, 0.0);

        if (_EnableVolumetricFog != 0)
        {
            bool doBiquadraticReconstruction = _VolumetricFilteringEnabled == 0; // Only if filtering is disabled.
            float4 value = SampleVBuffer(TEXTURE3D_ARGS(_VBufferLighting, s_linear_clamp_sampler),
                                         posInput.positionNDC,
                                         tFrag,
                                         _VBufferViewportSize,
                                         _VBufferLightingViewportScale.xyz,
                                         _VBufferLightingViewportLimit.xyz,
                                         _VBufferDistanceEncodingParams,
                                         _VBufferDistanceDecodingParams,
                                         true, doBiquadraticReconstruction, false);

            // TODO: add some slowly animated noise (dither?) to the reconstructed value.
            // TODO: re-enable tone mapping after implementing pre-exposure.
            volFog = DelinearizeRGBA(float4(/*FastTonemapInvert*/(value.rgb), value.a));
            volFogEnd = _VBufferLastSliceDist;
        }

        float distDelta = tFrag - volFogEnd;
        if (!underWater && distDelta > 0)
        {
            // Apply the distant (fallback) fog.
            float cosZenith = -dot(V, _PlanetUp);

            //float startHeight = dot(GetPrimaryCameraPosition() - V * volFogEnd, _PlanetUp);
            float startHeight = volFogEnd * cosZenith;
            #if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING == 0)
            startHeight += _CameraAltitude;
            #endif

            // For both homogeneous and exponential media,
            // Integrate[Transmittance[x] * Scattering[x], {x, 0, t}] = Albedo * Opacity[t].
            // Note that pulling the incoming radiance (which is affected by the fog) out of the
            // integral is wrong, as it means that shadow rays are not volumetrically shadowed.
            // This will result in fog looking overly bright.

            float3 volAlbedo = _HeightFogBaseScattering.xyz / _HeightFogBaseExtinction;
            float  odFallback = OpticalDepthHeightFog(_HeightFogBaseExtinction, _HeightFogBaseHeight,
                _HeightFogExponents, cosZenith, startHeight, distDelta);
            float  trFallback = TransmittanceFromOpticalDepth(odFallback);
            float  trCamera = 1 - volFog.a;

            volFog.rgb += trCamera * GetFogColor(V, tFrag) * GetCurrentExposureMultiplier() * volAlbedo * (1 - trFallback);
            volFog.a = 1 - (trCamera * trFallback);
        }

        color = volFog.rgb; // Already pre-exposed
        opacity = volFog.a;
    }

    #ifdef SUPPORT_WATER_ABSORPTION
    if (underWater)
    {
        WaterSurfaceProfile prof = _WaterSurfaceProfiles[_UnderWaterSurfaceIndex];
        float3 extinction = prof.extinction * prof.extinctionMultiplier;
        float3 opticalDepth = 0.0f;

        float absorptionDistance = tFrag - volFogEnd;
        if (absorptionDistance > 0)
        {
            float cosZenith  = -dot(V, prof.upDirection);
            opticalDepth = extinction * absorptionDistance;

            float3 tr = (1 - opacity) * GetCurrentExposureMultiplier();
            if (cosZenith != 1)
                tr *= rcp(1 - cosZenith) * OpacityFromOpticalDepth(opticalDepth - opticalDepth * cosZenith);

            color += tr * prof.underwaterColor;
        }

        // Depth from pixel to camera + depth from camera to surface
        float distanceToSurface = max(-dot(posInput.positionWS, prof.upDirection) - GetWaterCameraHeight(), 0);
        opticalDepth += distanceToSurface * extinction;

        float caustics = 1;
        #ifdef SUPPORT_WATER_CAUSTICS
        caustics = EvaluateSimulationCaustics(posInput.positionWS, distanceToSurface, posInput.positionNDC.xy);
        #endif

        #ifdef SUPPORT_WATER_CAUSTICS_SHADOW
        if (_DirectionalShadowIndex >= 0) // In case the user asked for shadow to explicitly be affected by shadows
        {
            DirectionalLightData light = _DirectionalLightDatas[_DirectionalShadowIndex];
            if ((light.lightDimmer > 0) && (light.shadowDimmer > 0))
            {
                float3 L = -light.forward;
                HDShadowContext ctx = InitShadowContext();
                float sunShadow = GetDirectionalShadowAttenuation(ctx, posInput.positionSS, posInput.positionWS, L, light.shadowIndex, L);
                caustics = 1 + (caustics - 1) * lerp(_UnderWaterCausticsShadowIntensity, 1.0, sunShadow);
            }
        }
        #endif

        opacity = 1 - (1 - opacity) * caustics * TransmittanceFromOpticalDepth(opticalDepth);

        /*
        #ifdef _ENABLE_FOG_ON_TRANSPARENT
        if (_PreRefractionPass != 0 && hasExcluder)
        {
            // If we are here, this means we are seing a transparent that is in front of an opaque with an excluder
            // Use case is a looking through a boat window from the exterior. We need to opacify the object to make underwater
            // visible between the glass and the camera
            // We use only the x channel as we can't do chromatic alpha blend
            outColor.a = lerp(outColor.a, 1, opacity.x);
        }
        #endif
        */

        // Don't apply atmospheric scattering from sky when underwater
        return true;
    }
    #endif

#ifndef ATMOSPHERE_NO_AERIAL_PERSPECTIVE
    // Sky pass already applies atmospheric scattering to the far plane.
    // This pass only handles geometry.
    if (_PBRFogEnabled && !isSky)
    {
        float3 skyColor = 0, skyOpacity = 0;

        EvaluateAtmosphericScattering(-V, posInput.positionNDC, tFrag, skyColor, skyOpacity);

        // Rendering of fog and atmospheric scattering cannot really be decoupled.
        #if 0
        // The best workaround is to deep composite them.
        float3 fogOD = OpticalDepthFromOpacity(fogOpacity);

        float3 fogRatio;
        fogRatio.r = (fogOpacity.r >= FLT_EPS) ? (fogOD.r * rcp(fogOpacity.r)) : 1;
        fogRatio.g = (fogOpacity.g >= FLT_EPS) ? (fogOD.g * rcp(fogOpacity.g)) : 1;
        fogRatio.b = (fogOpacity.b >= FLT_EPS) ? (fogOD.b * rcp(fogOpacity.b)) : 1;
        float3 skyRatio;
        skyRatio.r = (skyOpacity.r >= FLT_EPS) ? (skyOD.r * rcp(skyOpacity.r)) : 1;
        skyRatio.g = (skyOpacity.g >= FLT_EPS) ? (skyOD.g * rcp(skyOpacity.g)) : 1;
        skyRatio.b = (skyOpacity.b >= FLT_EPS) ? (skyOD.b * rcp(skyOpacity.b)) : 1;

        float3 logFogColor = fogRatio * fogColor;
        float3 logSkyColor = skyRatio * skyColor;

        float3 logCompositeColor = logFogColor + logSkyColor;
        float3 compositeOD = fogOD + skyOD;

        opacity = OpacityFromOpticalDepth(compositeOD);

        float3 rcpCompositeRatio;
        rcpCompositeRatio.r = (opacity.r >= FLT_EPS) ? (opacity.r * rcp(compositeOD.r)) : 1;
        rcpCompositeRatio.g = (opacity.g >= FLT_EPS) ? (opacity.g * rcp(compositeOD.g)) : 1;
        rcpCompositeRatio.b = (opacity.b >= FLT_EPS) ? (opacity.b * rcp(compositeOD.b)) : 1;

        color = rcpCompositeRatio * logCompositeColor;
        #else
        // Deep compositing assumes that the fog spans the same range as the atmosphere.
        // Our fog is short range, so deep compositing gives surprising results.
        // Using the "shallow" over operator is more appropriate in our context.
        // We could do something more clever with deep compositing, but this would
        // probably be a waste in terms of perf.
        CompositeOver(color, opacity, skyColor, skyOpacity, color, opacity);
        #endif
    }
#endif

    return true;
}

#endif
