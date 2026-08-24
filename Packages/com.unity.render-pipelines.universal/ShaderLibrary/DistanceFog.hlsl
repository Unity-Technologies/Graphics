#ifndef UNIVERSAL_DISTANCE_FOG_INCLUDED
#define UNIVERSAL_DISTANCE_FOG_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebuggingCommon.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ExposureFunctions.hlsl"

// Normalize the fog keywords so they can be read as plain runtime values everywhere,
// whether they are multi-compile, dynamic-branch, absent or manually defined.
#if !defined(FOG_LINEAR_KEYWORD_DECLARED)
    #if !defined(FOG_LINEAR)
        static const bool FOG_LINEAR = 0;
    #elif DEFINED_NONZERO(FOG_LINEAR)
        #undef FOG_LINEAR
        #define FOG_LINEAR 1
    #endif
#endif

#if !defined(FOG_EXP_KEYWORD_DECLARED)
    #if !defined(FOG_EXP)
        static const bool FOG_EXP = 0;
    #elif DEFINED_NONZERO(FOG_EXP)
        #undef FOG_EXP
        #define FOG_EXP 1
    #endif
#endif

#if !defined(FOG_EXP2_KEYWORD_DECLARED)
    #if !defined(FOG_EXP2)
        static const bool FOG_EXP2 = 0;
    #elif DEFINED_NONZERO(FOG_EXP2)
        #undef FOG_EXP2
        #define FOG_EXP2 1
    #endif
#endif

// Calculate a normalized fog blend factor in [0,1] range.
// Returns 1 when 'nearToFarZ' = 0, decreasing towards 0 as 'nearToFarZ' 
// approaches the distance between the near and far-plane (far-near).
float DistanceFogOcclusionFactor(float nearToFarZ)
{
    float fogT = 1.0;

    if (FOG_EXP)
    {
        // exp(-density * z); unity_FogParams.x holds the density term scaled for exp2
        float f = unity_FogParams.x * nearToFarZ;
        fogT = saturate(exp2(-f));
    }
    else if (FOG_EXP2)
    {
        // exp(-(density * z)^2)
        float f = unity_FogParams.x * nearToFarZ;
        fogT = saturate(exp2(-f * f));
    }
    else if (FOG_LINEAR)
    {
        // (end - z) / (end - start)
        fogT = saturate(nearToFarZ * unity_FogParams.z + unity_FogParams.w);
    }

    return fogT;
}

// Calculate 'nearToFarZ' (near-plane-relative view distance) from fragment coordinates (SV_Position).
float DistanceFogNearToFarZ(float4 positionCS)
{
    float nearToFarZ = 0;

    // TODO(perf): both branches are affine in (positionCS.z * positionCS.w, positionCS.w), so this could
    // collapse to a branchless fma(w, fma(Cx, z, Cy), Cz) with a per-camera coefficient triple
    // (perspective -> (0, 1, -near); ortho -> (-(far-near), far-near, 0)); the triple can also fold in the
    // fog density/linear scale. Needs new CPU params to be uploaded.
    if (IsPerspectiveProjection())
    {
        // Perspective: positionCS.w is the linear eye depth, subtract the near plane.
        nearToFarZ = positionCS.w - _ProjectionParams.y;
    }
    else
    {
        // Ortho: positionCS.w is always 1, so recover eye distance from device depth (positionCS.z)
        // mapped through the near and far planes.
    #if UNITY_REVERSED_Z
        nearToFarZ = (_ProjectionParams.z - _ProjectionParams.y) * (1.0 - positionCS.z);
    #else
        nearToFarZ = (_ProjectionParams.z - _ProjectionParams.y) * positionCS.z;
    #endif
    }

    return nearToFarZ;
}

// Blends 'color' with an explicit 'fogColor'.
// Blending parameters are derived from fragment coordinates (SV_Position).
half3 BlendDistanceFogColor(half3 color, float4 positionCS, half3 fogColor)
{
    if ((FOG_LINEAR || FOG_EXP || FOG_EXP2) && IsFogEnabled())
    {
        half t = DistanceFogOcclusionFactor(DistanceFogNearToFarZ(positionCS));
        // UUM-61728: manual lerp avoids Vulkan artifacts on some GPUs
        color = color * t + fogColor * (half(1.0) - t);
    }

    return color;
}

float3 BlendDistanceFogColor(float3 color, float4 positionCS, float3 fogColor)
{
    if ((FOG_LINEAR || FOG_EXP || FOG_EXP2) && IsFogEnabled())
    {
        float t = DistanceFogOcclusionFactor(DistanceFogNearToFarZ(positionCS));
        color = lerp(fogColor, color, t);
    }

    return color;
}

// Blends 'color' with an explicit 'fogColor'.
// Blending parameters are derived from explicit eye depth ('linearEyeDepth' is view-space z).
half3 BlendDistanceFogColorFromEyeDepth(half3 color, float linearEyeDepth, half3 fogColor)
{
    if ((FOG_LINEAR || FOG_EXP || FOG_EXP2) && IsFogEnabled())
    {
        half t = DistanceFogOcclusionFactor(linearEyeDepth - _ProjectionParams.y);
        // UUM-61728: manual lerp avoids Vulkan artifacts on some GPUs
        color = color * t + fogColor * (half(1.0) - t);
    }

    return color;
}

float3 BlendDistanceFogColorFromEyeDepth(float3 color, float linearEyeDepth, float3 fogColor)
{
    if ((FOG_LINEAR || FOG_EXP || FOG_EXP2) && IsFogEnabled())
    {
        float t = DistanceFogOcclusionFactor(linearEyeDepth - _ProjectionParams.y);
        color = lerp(fogColor, color, t);
    }

    return color;
}

// Blends 'color' with URP's active fog color.
// Blending parameters are derived from fragment coordinates (SV_Position).
half3 BlendDistanceFog(half3 color, float4 positionCS)
{
    return BlendDistanceFogColor(color, positionCS, half3(unity_FogColor.rgb));
}

float3 BlendDistanceFog(float3 color, float4 positionCS)
{
    return BlendDistanceFogColor(color, positionCS, unity_FogColor.rgb);
}

// Blends 'color' with URP's active fog color.
// Blending parameters are derived from explicit eye depth ('linearEyeDepth' is view-space z).
half3 BlendDistanceFogFromEyeDepth(half3 color, float linearEyeDepth)
{
    return BlendDistanceFogColorFromEyeDepth(color, linearEyeDepth, half3(unity_FogColor.rgb));
}

float3 BlendDistanceFogFromEyeDepth(float3 color, float linearEyeDepth)
{
    return BlendDistanceFogColorFromEyeDepth(color, linearEyeDepth, unity_FogColor.rgb);
}

URP_LIGHT_ACCUM3 BlendDistanceFogExposed(half3 color, float4 positionCS, float preExposureMultiplier)
{
    return BlendDistanceFogColor(color, positionCS, unity_FogColor.rgb * preExposureMultiplier);
}

#endif // UNIVERSAL_DISTANCE_FOG_INCLUDED
