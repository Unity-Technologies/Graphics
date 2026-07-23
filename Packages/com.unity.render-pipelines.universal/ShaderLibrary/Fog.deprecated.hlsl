#ifndef UNIVERSAL_FOG_DEPRECATED_INCLUDED
#define UNIVERSAL_FOG_DEPRECATED_INCLUDED

// Kept defined for back-compat: legacy shaders gate their per-fragment fog on _FOG_FRAGMENT.
#define _FOG_FRAGMENT 1

// Deprecated two-stage fog API, kept for external back-compat. New code uses BlendDistanceFog (DistanceFog.hlsl).

// Deprecated. Use DistanceFogOcclusionFactor(nearToFarZ).
real ComputeFogFactorZ0ToFar(float z)
{
    #if defined(FOG_LINEAR_KEYWORD_DECLARED)
    if (FOG_LINEAR)
    {
        // factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
        float fogFactor = saturate(z * unity_FogParams.z + unity_FogParams.w);
        return real(fogFactor);
    }
    #endif

    #if defined(FOG_EXP_KEYWORD_DECLARED)
    if (FOG_EXP)
    {
        // factor = exp(-(density*z)^2)
        // -density * z computed at vertex
        return real(unity_FogParams.x * z);
    }
    #endif

    #if defined(FOG_EXP2_KEYWORD_DECLARED)
    if (FOG_EXP2)
    {
        // factor = exp(-(density*z)^2)
        // -density * z computed at vertex
        return real(unity_FogParams.x * z);
    }
    #endif

    // This process is necessary to avoid errors in iOS graphics tests
    // when using the dynamic branching of fog keywords.
    return real(0.0);
}

// Deprecated. Use DistanceFogOcclusionFactor(DistanceFogNearToFarZ(positionCS)).
real ComputeFogFactor(float zPositionCS)
{
    float clipZ_0Far = UNITY_Z_0_FAR_FROM_CLIPSPACE(zPositionCS);
    return ComputeFogFactorZ0ToFar(clipZ_0Far);
}

// Deprecated. Use DistanceFogOcclusionFactor(nearToFarZ) (single-stage; no separate factor).
half ComputeFogIntensity(half fogFactor)
{
    #if defined(FOG_EXP_KEYWORD_DECLARED)
    if (FOG_EXP)
    {
        // factor = exp(-density*z)
        // fogFactor = density*z compute at vertex
        return saturate(exp2(-fogFactor));
    }
    #endif

    #if defined(FOG_EXP2_KEYWORD_DECLARED)
    if (FOG_EXP2)
    {
        // factor = exp(-(density*z)^2)
        // fogFactor = density*z compute at vertex
        return saturate(exp2(-fogFactor * fogFactor));
    }
    #endif

    #if defined(FOG_LINEAR_KEYWORD_DECLARED)
    if (FOG_LINEAR)
    {
        return fogFactor;
    }
    #endif

    return 0.0;
}

// Deprecated. Fog now applies to the final color via BlendDistanceFog; no fogCoord round-trip needed.
real InitializeInputDataFog(float4 positionWS, real vertFogFactor)
{
    real fogFactor = 0.0;
#if defined(_FOG_FRAGMENT)
    bool anyFogEnabled = false;

    #if defined(FOG_LINEAR_KEYWORD_DECLARED)
    if (FOG_LINEAR)
        anyFogEnabled = true;
    #endif

    #if defined(FOG_EXP_KEYWORD_DECLARED)
    if (FOG_EXP)
        anyFogEnabled = true;
    #endif

    #if defined(FOG_EXP2_KEYWORD_DECLARED)
    if (FOG_EXP2)
        anyFogEnabled = true;
    #endif

    if (anyFogEnabled)
    {
        // Compiler eliminates unused math --> matrix.column_z * vec
        float viewZ = -(mul(UNITY_MATRIX_V, positionWS).z);
        // View Z is 0 at camera pos, remap 0 to near plane.
        float nearToFarZ = max(viewZ - _ProjectionParams.y, 0);
        fogFactor = ComputeFogFactorZ0ToFar(nearToFarZ);
    }
#else // #if defined(_FOG_FRAGMENT)
    fogFactor = vertFogFactor;
#endif // #if defined(_FOG_FRAGMENT)
    return fogFactor;
}

// Deprecated. Use DistanceFogOcclusionFactor(nearToFarZ) (single-stage; no separate factor).
float ComputeFogIntensity(float fogFactor)
{
    #if defined(FOG_EXP_KEYWORD_DECLARED)
    if (FOG_EXP)
    {
        // factor = exp(-density*z)
        // fogFactor = density*z compute at vertex
        return saturate(exp2(-fogFactor));
    }
    #endif

    #if defined(FOG_EXP2_KEYWORD_DECLARED)
    if (FOG_EXP2)
    {
        // factor = exp(-(density*z)^2)
        // fogFactor = density*z compute at vertex
        return saturate(exp2(-fogFactor * fogFactor));
    }
    #endif

    #if defined(FOG_LINEAR_KEYWORD_DECLARED)
    if (FOG_LINEAR)
    {
        return fogFactor;
    }
    #endif

    return 0.0;
}

// Deprecated. Use BlendDistanceFogColor(color, positionCS, fogColor).
half3 MixFogColor(half3 fragColor, half3 fogColor, half fogFactor)
{
    bool anyFogEnabled = false;

    #if defined(FOG_LINEAR_KEYWORD_DECLARED)
    if (FOG_LINEAR)
        anyFogEnabled = true;
    #endif

    #if defined(FOG_EXP_KEYWORD_DECLARED)
    if (FOG_EXP)
        anyFogEnabled = true;
    #endif

    #if defined(FOG_EXP2_KEYWORD_DECLARED)
    if (FOG_EXP2)
        anyFogEnabled = true;
    #endif

    if (anyFogEnabled)
    {
        if (IsFogEnabled())
        {
            half fogIntensity = ComputeFogIntensity(fogFactor);
            // Workaround for UUM-61728: using a manual lerp to avoid rendering artifacts on some GPUs when Vulkan is used
            fragColor = fragColor * fogIntensity + fogColor * (half(1.0) - fogIntensity);
        }
    }
    return fragColor;
}

// Deprecated. Use BlendDistanceFogColor(color, positionCS, fogColor).
float3 MixFogColor(float3 fragColor, float3 fogColor, float fogFactor)
{
    bool anyFogEnabled = false;

    #if defined(FOG_LINEAR_KEYWORD_DECLARED)
    if (FOG_LINEAR)
        anyFogEnabled = true;
    #endif

    #if defined(FOG_EXP_KEYWORD_DECLARED)
    if (FOG_EXP)
        anyFogEnabled = true;
    #endif

    #if defined(FOG_EXP2_KEYWORD_DECLARED)
    if (FOG_EXP2)
        anyFogEnabled = true;
    #endif

    if (anyFogEnabled)
    {
        if (IsFogEnabled())
        {
            float fogIntensity = ComputeFogIntensity(fogFactor);
            fragColor = lerp(fogColor, fragColor, fogIntensity);
        }
    }
    return fragColor;
}

// Deprecated. Use BlendDistanceFog(color, positionCS).
half3 MixFog(half3 fragColor, half fogFactor)
{
    return MixFogColor(fragColor, half3(unity_FogColor.rgb), fogFactor);
}

// Deprecated. Use BlendDistanceFog(color, positionCS).
float3 MixFog(float3 fragColor, float fogFactor)
{
    return MixFogColor(fragColor, unity_FogColor.rgb, fogFactor);
}

#endif // UNIVERSAL_FOG_DEPRECATED_INCLUDED
