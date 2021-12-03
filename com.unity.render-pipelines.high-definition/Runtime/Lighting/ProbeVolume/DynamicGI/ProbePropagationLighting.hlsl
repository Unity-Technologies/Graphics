#ifndef PROBE_PROPAGATION_LIGHTING
#define PROBE_PROPAGATION_LIGHTING

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightEvaluation.hlsl"

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/DynamicGI/ProbeVolumeDynamicGI.hlsl"

struct SurfaceHitData
{
    float3 albedo;
    float3 position;
    float3 normal;
};


// -------------------------------------------------------------
// Lighting Utils
// -------------------------------------------------------------

LightLoopContext GetEmptyLightLoopContext()
{
    LightLoopContext context;
    context.shadowContext = InitShadowContext();
    context.shadowValue = 1.0;
    context.sampleReflection = false;
    context.contactShadow = 0;
    context.contactShadowFade = 0;

    return context;
}

float3 EvaluateLight_RectArea_LambertDiffuseOnly(LightLoopContext lightLoopContext, float3 N, PositionInputs posInput, LightData lightData, BuiltinData builtinData)
{
    float3 outDiffuseLighting = 0;

    float3 positionWS = posInput.positionWS;

#if SHADEROPTIONS_BARN_DOOR
    // Apply the barn door modification to the light data
    RectangularLightApplyBarnDoor(lightData, positionWS);
#endif

    float3 unL = lightData.positionRWS - positionWS;

    if (dot(lightData.forward, unL) < FLT_EPS)
    {
        // Rotate the light direction into the light space.
        float3x3 lightToWorld = float3x3(lightData.right, lightData.up, -lightData.forward);
        unL = mul(unL, transpose(lightToWorld));

        // TODO: This could be precomputed.
        float halfWidth = lightData.size.x * 0.5;
        float halfHeight = lightData.size.y * 0.5;

        // Define the dimensions of the attenuation volume.
        // TODO: This could be precomputed.
        float  range = lightData.range;
        float3 invHalfDim = rcp(float3(range + halfWidth,
            range + halfHeight,
            range));

        // Compute the light attenuation.
        // The attenuation volume is an axis-aligned box s.t.
        // hX = (r + w / 2), hY = (r + h / 2), hZ = r.
        float intensity = BoxDistanceAttenuation(unL, invHalfDim,
            lightData.rangeAttenuationScale,
            lightData.rangeAttenuationBias);

        // Terminate if the shaded point is too far away.
        if (intensity != 0.0)
        {
            lightData.diffuseDimmer *= intensity;
            lightData.specularDimmer *= intensity;

            // Translate the light s.t. the shaded point is at the origin of the coordinate system.
            lightData.positionRWS -= positionWS;

            float4x3 lightVerts;

            // TODO: some of this could be precomputed.
            lightVerts[0] = lightData.positionRWS + lightData.right * -halfWidth + lightData.up * -halfHeight; // LL
            lightVerts[1] = lightData.positionRWS + lightData.right * -halfWidth + lightData.up *  halfHeight; // UL
            lightVerts[2] = lightData.positionRWS + lightData.right *  halfWidth + lightData.up *  halfHeight; // UR
            lightVerts[3] = lightData.positionRWS + lightData.right *  halfWidth + lightData.up * -halfHeight; // LR

            // Rotate the endpoints into the local coordinate system.

            // The orthonormal basis used here should be GetOrthoBasisViewNormal but we don't have access of V.
            lightVerts = mul(lightVerts, transpose(GetLocalFrame(N)));

            float3 ltcValue;

            // Evaluate the diffuse part
            // Polygon irradiance in the transformed configuration.

            // Because we evaluate only lambertian diffuse here, the LTC transform is just identity, hence we skip the multiply.
            float4x3 LD = lightVerts;

            float3 formFactorD;
#ifdef APPROXIMATE_POLY_LIGHT_AS_SPHERE_LIGHT
            formFactorD = PolygonFormFactor(LD);
            ltcValue = PolygonIrradianceFromVectorFormFactor(formFactorD);
#else
            ltcValue = PolygonIrradiance(LD, formFactorD);
#endif
            ltcValue *= lightData.diffuseDimmer;

            // Only apply cookie if there is one
            if (lightData.cookieMode != COOKIEMODE_NONE)
            {
#ifndef APPROXIMATE_POLY_LIGHT_AS_SPHERE_LIGHT
                formFactorD = PolygonFormFactor(LD);
#endif
                ltcValue *= SampleAreaLightCookie(lightData.cookieScaleOffset, LD, formFactorD);
            }

            outDiffuseLighting = ltcValue;

            // Raytracing shadow algorithm require to evaluate lighting without shadow, so it defined SKIP_RASTERIZED_AREA_SHADOWS
            // This is only present in Lit Material as it is the only one using the improved shadow algorithm.
#ifndef SKIP_RASTERIZED_AREA_SHADOWS
            SHADOW_TYPE shadow = EvaluateShadow_RectArea(lightLoopContext, posInput, lightData, builtinData, N, normalize(lightData.positionRWS), length(lightData.positionRWS));
            lightData.color.rgb *= ComputeShadowColor(shadow, lightData.shadowTint, lightData.penumbraTint);
#endif

            // Save ALU by applying 'lightData.color' only once.
            outDiffuseLighting *= lightData.color;

#ifdef DEBUG_DISPLAY
            if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
            {
                // Only lighting, not BSDF
                // Apply area light on lambert then multiply by PI to cancel Lambert
                outDiffuseLighting = PolygonIrradiance(mul(lightVerts, k_identity3x3));
                outDiffuseLighting *= PI * lightData.diffuseDimmer;
            }
#endif
        }
    }

    return outDiffuseLighting;
}


float3 GetShadowPunctual(LightData light, float3 positionRWS, float4 distances, float3 L, LightLoopContext context)
{
    PositionInputs posInput;
    posInput.positionWS = positionRWS;

    float4 positionCS = TransformWorldToHClip(positionRWS);
    positionCS.xyz /= positionCS.w;
    posInput.positionNDC = positionCS.xy * 0.5 + 0.5;
    posInput.positionSS = posInput.positionNDC.xy * _ScreenSize.xy;

    BuiltinData unused;

    SHADOW_TYPE shadow = EvaluateShadow_Punctual(context, posInput, light, unused, 0, L, distances);
    return ComputeShadowColor(shadow, light.shadowTint, light.penumbraTint);

}

float3 GetShadowDirectional(DirectionalLightData light, float3 positionRWS, LightLoopContext context)
{
    PositionInputs posInput;
    posInput.positionWS = positionRWS;

    light.contactShadowMask = 0;
    float3 L = -light.forward;
    context.shadowValue = GetDirectionalShadowAttenuation(context.shadowContext,
        0, posInput.positionWS, 0,
        light.shadowIndex, L);

    BuiltinData unused;

    SHADOW_TYPE shadow = EvaluateShadow_Directional(context, posInput, light, unused, 0);
    return ComputeShadowColor(shadow, light.shadowTint, light.penumbraTint);

}

float3 EvaluatePunctualLight(LightData light, float3 positionWS, out float3 L)
{
    float4 distances; // {d, d^2, 1/d, d_proj}
    float3 positionRWS = GetCameraRelativePositionWS(positionWS);

    GetPunctualLightVectors(positionRWS, light, L, distances);
    L = normalize(L);

    LightLoopContext context = GetEmptyLightLoopContext();

    PositionInputs posInput;
    posInput.positionWS = positionRWS;

    float4 lightColor = EvaluateLight_Punctual(context, posInput, light, L, distances);

    // hack to soften the intensity near probes to reduce motion aliasing, when lights are very close to probe locations
    //lightColor.a = lerp(lightColor.a, 0, saturate(distances.z));

    lightColor.rgb *= lightColor.a;

    float3 shadows = GetShadowPunctual(light, positionRWS, distances, L, context);

    return shadows * lightColor.rgb;
}

float3 EvaluateAreaLight(LightData light, float3 positionWS, float3 N)
{
    LightLoopContext context = GetEmptyLightLoopContext();

    PositionInputs posInput;
    float3 positionRWS = GetCameraRelativePositionWS(positionWS);
    posInput.positionWS = positionRWS;

    BuiltinData unused;
    return EvaluateLight_RectArea_LambertDiffuseOnly(context, N, posInput, light, unused);
}

float3 EvaluateDirectionalLight(DirectionalLightData light, float3 positionWS, out float3 L)
{
    float3 positionRWS = GetCameraRelativePositionWS(positionWS);

    LightLoopContext context = GetEmptyLightLoopContext();

    PositionInputs posInput;
    posInput.positionWS = positionRWS;

    float4 lightColor = EvaluateLight_Directional(context, posInput, light);
    lightColor.rgb *= lightColor.a;

    float3 shadows = GetShadowDirectional(light, positionRWS, context);

    L = -light.forward;

    // TODO: WHY? But it matches more...
    float scale =  INV_PI;
    return shadows * lightColor.rgb * scale;
}

float3 GetLightingForAxisPunctual(LightData light, float indirectScale, SurfaceHitData hit)
{
    float3 outputLighting = 0;

    float3 L;
    outputLighting = EvaluatePunctualLight(light, hit.position, L);
    outputLighting *= hit.albedo;
    float NdotL = saturate(dot(hit.normal, L));

    float scalarMultiplier = NdotL * INV_PI * indirectScale;
    outputLighting *= scalarMultiplier;

    return outputLighting;
}

float3 GetLightingForAxisDirectional(DirectionalLightData light, float indirectScale, SurfaceHitData hit)
{
    float3 outputLighting = 0;

    float3 L;
    outputLighting = EvaluateDirectionalLight(light, hit.position, L);
    outputLighting *= hit.albedo;
    float NdotL = saturate(dot(hit.normal, L));

    float scalarMultiplier = NdotL * INV_PI * indirectScale;
    outputLighting *= scalarMultiplier;

    return outputLighting;
}

float3 GetLightingForAxisArea(LightData light, float indirectScale, SurfaceHitData hit)
{
    float3 outputLighting = 0;

    outputLighting = EvaluateAreaLight(light, hit.position, hit.normal);
    outputLighting *= hit.albedo;

    float scalarMultiplier = indirectScale;
    outputLighting *= scalarMultiplier;

    return outputLighting;
}


#endif // endof PROBE_PROPAGATION_LIGHTING
