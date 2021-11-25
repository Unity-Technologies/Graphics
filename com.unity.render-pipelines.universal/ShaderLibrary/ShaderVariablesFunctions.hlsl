#ifndef UNITY_SHADER_VARIABLES_FUNCTIONS_INCLUDED
#define UNITY_SHADER_VARIABLES_FUNCTIONS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.deprecated.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebuggingCommon.hlsl"

VertexPositionInputs GetVertexPositionInputs(float3 positionOS)
{
    VertexPositionInputs input;
    input.positionWS = TransformObjectToWorld(positionOS);
    input.positionVS = TransformWorldToView(input.positionWS);
    input.positionCS = TransformWorldToHClip(input.positionWS);

    float4 ndc = input.positionCS * 0.5f;
    input.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
    input.positionNDC.zw = input.positionCS.zw;

    return input;
}

VertexNormalInputs GetVertexNormalInputs(float3 normalOS)
{
    VertexNormalInputs tbn;
    tbn.tangentWS = real3(1.0, 0.0, 0.0);
    tbn.bitangentWS = real3(0.0, 1.0, 0.0);
    tbn.normalWS = TransformObjectToWorldNormal(normalOS);
    return tbn;
}

VertexNormalInputs GetVertexNormalInputs(float3 normalOS, float4 tangentOS)
{
    VertexNormalInputs tbn;

    // mikkts space compliant. only normalize when extracting normal at frag.
    real sign = real(tangentOS.w) * GetOddNegativeScale();
    tbn.normalWS = TransformObjectToWorldNormal(normalOS);
    tbn.tangentWS = real3(TransformObjectToWorldDir(tangentOS.xyz));
    tbn.bitangentWS = real3(cross(tbn.normalWS, float3(tbn.tangentWS))) * sign;
    return tbn;
}

float4 GetScaledScreenParams()
{
    return _ScaledScreenParams;
}

// Returns 'true' if the current view performs a perspective projection.
bool IsPerspectiveProjection()
{
    return (unity_OrthoParams.w == 0);
}

float3 GetCameraPositionWS()
{
    // Currently we do not support Camera Relative Rendering so
    // we simply return the _WorldSpaceCameraPos until then
    return _WorldSpaceCameraPos;

    // We will replace the code above with this one once
    // we start supporting Camera Relative Rendering
    //#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    //    return float3(0, 0, 0);
    //#else
    //    return _WorldSpaceCameraPos;
    //#endif
}

// Could be e.g. the position of a primary camera or a shadow-casting light.
float3 GetCurrentViewPosition()
{
    // Currently we do not support Camera Relative Rendering so
    // we simply return the _WorldSpaceCameraPos until then
    return GetCameraPositionWS();

    // We will replace the code above with this one once
    // we start supporting Camera Relative Rendering
    //#if defined(SHADERPASS) && (SHADERPASS != SHADERPASS_SHADOWS)
    //    return GetCameraPositionWS();
    //#else
    //    // This is a generic solution.
    //    // However, for the primary camera, using '_WorldSpaceCameraPos' is better for cache locality,
    //    // and in case we enable camera-relative rendering, we can statically set the position is 0.
    //    return UNITY_MATRIX_I_V._14_24_34;
    //#endif
}

// Returns the forward (central) direction of the current view in the world space.
float3 GetViewForwardDir()
{
    float4x4 viewMat = GetWorldToViewMatrix();
    return -viewMat[2].xyz;
}

// Computes the world space view direction (pointing towards the viewer).
float3 GetWorldSpaceViewDir(float3 positionWS)
{
    if (IsPerspectiveProjection())
    {
        // Perspective
        return GetCurrentViewPosition() - positionWS;
    }
    else
    {
        // Orthographic
        return -GetViewForwardDir();
    }
}

// Computes the object space view direction (pointing towards the viewer).
half3 GetObjectSpaceNormalizeViewDir(float3 positionOS)
{
    if (IsPerspectiveProjection())
    {
        // Perspective
        float3 V = TransformWorldToObject(GetCurrentViewPosition()) - positionOS;
        return half3(normalize(V));
    }
    else
    {
        // Orthographic
        return half3(TransformWorldToObjectNormal(-GetViewForwardDir()));
    }
}

half3 GetWorldSpaceNormalizeViewDir(float3 positionWS)
{
    if (IsPerspectiveProjection())
    {
        // Perspective
        float3 V = GetCurrentViewPosition() - positionWS;
        return half3(normalize(V));
    }
    else
    {
        // Orthographic
        return half3(-GetViewForwardDir());
    }
}

// UNITY_MATRIX_V defines a right-handed view space with the Z axis pointing towards the viewer.
// This function reverses the direction of the Z axis (so that it points forward),
// making the view space coordinate system left-handed.
void GetLeftHandedViewSpaceMatrices(out float4x4 viewMatrix, out float4x4 projMatrix)
{
    viewMatrix = UNITY_MATRIX_V;
    viewMatrix._31_32_33_34 = -viewMatrix._31_32_33_34;

    projMatrix = UNITY_MATRIX_P;
    projMatrix._13_23_33_43 = -projMatrix._13_23_33_43;
}

void AlphaDiscard(real alpha, real cutoff, real offset = real(0.0))
{
    #ifdef _ALPHATEST_ON
    if (IsAlphaDiscardEnabled())
        clip(alpha - cutoff + offset);
    #endif
}

half OutputAlpha(half outputAlpha, half surfaceType = half(0.0))
{
    return surfaceType == 1 ? outputAlpha : half(1.0);
}

half3 AlphaModulate(half3 albedo, half alpha)
{
    // Fake alpha for multiply blend by lerping albedo towards 1 (white) using alpha.
    // Manual adjustment for "lighter" multiply effect (similar to "premultiplied alpha")
    // would be painting whiter pixels in the texture.
    // This emulates that procedure in shader, so it should be applied to the base/source color.
#if defined(_ALPHAMODULATE_ON)
    return lerp(1, albedo, alpha);
#else
    return albedo;
#endif
}

half3 AlphaPremultiply(half3 albedo, half alpha)
{
    // Multiply alpha into albedo only for Preserve Specular material diffuse part.
    // Preserve Specular material (glass like) has different alpha for diffuse and specular lighting.
    // Logically this is "variable" Alpha blending.
    // (HW blend mode is premultiply, but with alpha multiply in shader.)
#if defined(_ALPHAPREMULTIPLY_ON)
    return albedo * alpha;
#endif
    return albedo;
}

// A word on normalization of normals:
// For better quality normals should be normalized before and after
// interpolation.
// 1) In vertex, skinning or blend shapes might vary significantly the lenght of normal.
// 2) In fragment, because even outputting unit-length normals interpolation can make it non-unit.
// 3) In fragment when using normal map, because mikktspace sets up non orthonormal basis.
// However we will try to balance performance vs quality here as also let users configure that as
// shader quality tiers.
// Low Quality Tier: Don't normalize per-vertex.
// Medium Quality Tier: Always normalize per-vertex.
// High Quality Tier: Always normalize per-vertex.
//
// Always normalize per-pixel.
// Too many bug like lighting quality issues otherwise.
half3 NormalizeNormalPerVertex(half3 normalWS)
{
    #if defined(SHADER_QUALITY_LOW) && defined(_NORMALMAP)
        return normalWS;
    #else
        return normalize(normalWS);
    #endif
}

float3 NormalizeNormalPerVertex(float3 normalWS)
{
    #if defined(SHADER_QUALITY_LOW) && defined(_NORMALMAP)
        return normalWS;
    #else
        return normalize(normalWS);
    #endif
}

half3 NormalizeNormalPerPixel(half3 normalWS)
{
// With XYZ normal map encoding we sporadically sample normals with near-zero-length causing Inf/NaN
#if defined(UNITY_NO_DXT5nm) && defined(_NORMALMAP)
    return SafeNormalize(normalWS);
#else
    return normalize(normalWS);
#endif
}

float3 NormalizeNormalPerPixel(float3 normalWS)
{
#if defined(UNITY_NO_DXT5nm) && defined(_NORMALMAP)
    return SafeNormalize(normalWS);
#else
    return normalize(normalWS);
#endif
}



real ComputeFogFactorZ0ToFar(float z)
{
    #if defined(FOG_LINEAR)
    // factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
    float fogFactor = saturate(z * unity_FogParams.z + unity_FogParams.w);
    return real(fogFactor);
    #elif defined(FOG_EXP) || defined(FOG_EXP2)
    // factor = exp(-(density*z)^2)
    // -density * z computed at vertex
    return real(unity_FogParams.x * z);
    #else
        return real(0.0);
    #endif
}

real ComputeFogFactor(float zPositionCS)
{
    float clipZ_0Far = UNITY_Z_0_FAR_FROM_CLIPSPACE(zPositionCS);
    return ComputeFogFactorZ0ToFar(clipZ_0Far);
}

half ComputeFogIntensity(half fogFactor)
{
    half fogIntensity = half(0.0);
    #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
        #if defined(FOG_EXP)
            // factor = exp(-density*z)
            // fogFactor = density*z compute at vertex
            fogIntensity = saturate(exp2(-fogFactor));
        #elif defined(FOG_EXP2)
            // factor = exp(-(density*z)^2)
            // fogFactor = density*z compute at vertex
            fogIntensity = saturate(exp2(-fogFactor * fogFactor));
        #elif defined(FOG_LINEAR)
            fogIntensity = fogFactor;
        #endif
    #endif
    return fogIntensity;
}

// Force enable fog fragment shader evaluation
#define _FOG_FRAGMENT 1
real InitializeInputDataFog(float4 positionWS, real vertFogFactor)
{
    real fogFactor = 0.0;
#if defined(_FOG_FRAGMENT)
    #if (defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2))
        // Compiler eliminates unused math --> matrix.column_z * vec
        float viewZ = -(mul(UNITY_MATRIX_V, positionWS).z);
        // View Z is 0 at camera pos, remap 0 to near plane.
        float nearToFarZ = max(viewZ - _ProjectionParams.y, 0);
        fogFactor = ComputeFogFactorZ0ToFar(nearToFarZ);
    #endif
#else
    fogFactor = vertFogFactor;
#endif
    return fogFactor;
}

float ComputeFogIntensity(float fogFactor)
{
    float fogIntensity = 0.0;
    #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
        #if defined(FOG_EXP)
            // factor = exp(-density*z)
            // fogFactor = density*z compute at vertex
            fogIntensity = saturate(exp2(-fogFactor));
        #elif defined(FOG_EXP2)
            // factor = exp(-(density*z)^2)
            // fogFactor = density*z compute at vertex
            fogIntensity = saturate(exp2(-fogFactor * fogFactor));
        #elif defined(FOG_LINEAR)
            fogIntensity = fogFactor;
        #endif
    #endif
    return fogIntensity;
}

half3 MixFogColor(half3 fragColor, half3 fogColor, half fogFactor)
{
    #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
        half fogIntensity = ComputeFogIntensity(fogFactor);
        fragColor = lerp(fogColor, fragColor, fogIntensity);
    #endif
    return fragColor;
}

float3 MixFogColor(float3 fragColor, float3 fogColor, float fogFactor)
{
    #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
    if (IsFogEnabled())
    {
        float fogIntensity = ComputeFogIntensity(fogFactor);
        fragColor = lerp(fogColor, fragColor, fogIntensity);
    }
    #endif
    return fragColor;
}

half3 MixFog(half3 fragColor, half fogFactor)
{
    return MixFogColor(fragColor, unity_FogColor.rgb, fogFactor);
}

float3 MixFog(float3 fragColor, float fogFactor)
{
    return MixFogColor(fragColor, unity_FogColor.rgb, fogFactor);
}

// Linear depth buffer value between [0, 1] or [1, 0] to eye depth value between [near, far]
half LinearDepthToEyeDepth(half rawDepth)
{
    #if UNITY_REVERSED_Z
        return half(_ProjectionParams.z - (_ProjectionParams.z - _ProjectionParams.y) * rawDepth);
    #else
        return half(_ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * rawDepth);
    #endif
}

float LinearDepthToEyeDepth(float rawDepth)
{
    #if UNITY_REVERSED_Z
        return _ProjectionParams.z - (_ProjectionParams.z - _ProjectionParams.y) * rawDepth;
    #else
        return _ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * rawDepth;
    #endif
}

void TransformScreenUV(inout float2 uv, float screenHeight)
{
    #if UNITY_UV_STARTS_AT_TOP
    uv.y = screenHeight - (uv.y * _ScaleBiasRt.x + _ScaleBiasRt.y * screenHeight);
    #endif
}

void TransformScreenUV(inout float2 uv)
{
    #if UNITY_UV_STARTS_AT_TOP
    TransformScreenUV(uv, GetScaledScreenParams().y);
    #endif
}

void TransformNormalizedScreenUV(inout float2 uv)
{
    #if UNITY_UV_STARTS_AT_TOP
    TransformScreenUV(uv, 1.0);
    #endif
}

float2 GetNormalizedScreenSpaceUV(float2 positionCS)
{
    float2 normalizedScreenSpaceUV = positionCS.xy * rcp(GetScaledScreenParams().xy);
    TransformNormalizedScreenUV(normalizedScreenSpaceUV);
    return normalizedScreenSpaceUV;
}

float2 GetNormalizedScreenSpaceUV(float4 positionCS)
{
    return GetNormalizedScreenSpaceUV(positionCS.xy);
}

#if defined(UNITY_SINGLE_PASS_STEREO)
    float2 TransformStereoScreenSpaceTex(float2 uv, float w)
    {
        // TODO: RVS support can be added here, if Universal decides to support it
        float4 scaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];
        return uv.xy * scaleOffset.xy + scaleOffset.zw * w;
    }

    float2 UnityStereoTransformScreenSpaceTex(float2 uv)
    {
        return TransformStereoScreenSpaceTex(saturate(uv), 1.0);
    }
#else
    #define UnityStereoTransformScreenSpaceTex(uv) uv
#endif // defined(UNITY_SINGLE_PASS_STEREO)

#endif // UNITY_SHADER_VARIABLES_FUNCTIONS_INCLUDED
