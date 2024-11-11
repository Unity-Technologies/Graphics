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

// Constants that represent material surface types
//
// These are expected to align with the commonly used "_Surface" material property
static const half kSurfaceTypeOpaque = 0.0;
static const half kSurfaceTypeTransparent = 1.0;

// Returns true if the input value represents an opaque surface
bool IsSurfaceTypeOpaque(half surfaceType)
{
    return (surfaceType == kSurfaceTypeOpaque);
}

// Returns true if the input value represents a transparent surface
bool IsSurfaceTypeTransparent(half surfaceType)
{
    return (surfaceType == kSurfaceTypeTransparent);
}

// Only define the alpha clipping helpers when the alpha test define is present.
// This should help identify usage errors early.
#if defined(_ALPHATEST_ON)
// Returns true if AlphaToMask functionality is currently available
// NOTE: This does NOT guarantee that AlphaToMask is enabled for the current draw. It only indicates that AlphaToMask functionality COULD be enabled for it.
//       In cases where AlphaToMask COULD be enabled, we export a specialized alpha value from the shader.
//       When AlphaToMask is enabled:     The specialized alpha value is combined with the sample mask
//       When AlphaToMask is not enabled: The specialized alpha value is either written into the framebuffer or dropped entirely depending on the color write mask
bool IsAlphaToMaskAvailable()
{
    return (_AlphaToMaskAvailable != 0.0);
}

// Returns a sharpened alpha value for use with alpha to coverage
// This function behaves correctly in cases where alpha and cutoff are constant values (degenerate usage of alpha clipping)
half SharpenAlphaStrict(half alpha, half alphaClipTreshold)
{
    half dAlpha = fwidth(alpha);
    return saturate(((alpha - alphaClipTreshold - (0.5 * dAlpha)) / max(dAlpha, 0.0001)) + 1.0);
}

// When AlphaToMask is available:     Returns a modified alpha value that should be exported from the shader so it can be combined with the sample mask
// When AlphaToMask is not available: Terminates the current invocation if the alpha value is below the cutoff and returns the input alpha value otherwise
half AlphaClip(half alpha, half cutoff)
{
    bool a2c = IsAlphaToMaskAvailable();

    // We explicitly detect cases where the alpha cutoff threshold is zero or below.
    // When this case occurs, we need to modify the alpha to coverage logic to avoid visual artifacts.
    bool zeroCutoff = (cutoff <= 0.0);

    // If the user has specified zero as the cutoff threshold, the expectation is that the shader will function as if alpha-clipping was disabled.
    // Ideally, the user should just turn off the alpha-clipping feature in this case, but in order to make this case work as expected, we force alpha
    // to 1.0 here to ensure that alpha-to-coverage never throws away samples when its active. (This would cause opaque objects to appear transparent)
    half alphaToCoverageAlpha = zeroCutoff ? 1.0 : SharpenAlphaStrict(alpha, cutoff);

    // When the alpha to coverage alpha is used for clipping, we subtract a small value from it to ensure that pixels with zero alpha exit early
    // rather than running the entire shader and then multiplying the sample coverage mask by zero which outputs nothing.
    half clipVal = (a2c && !zeroCutoff) ? (alphaToCoverageAlpha - 0.0001) : (alpha - cutoff);

    // When alpha-to-coverage is available:     Use the specialized value which will be exported from the shader and combined with the MSAA coverage mask.
    // When alpha-to-coverage is not available: Use the "clipped" value. A clipped value will always result in thread termination via the clip() logic below.
    half outputAlpha = a2c ? alphaToCoverageAlpha : alpha;

    clip(clipVal);

    return outputAlpha;
}
#endif

// Terminates the current invocation if the input alpha value is below the specified cutoff value and returns an updated alpha value otherwise.
// When provided, the offset value is added to the cutoff value during the comparison logic.
// The return value from this function should be exported as the final alpha value in fragment shaders so it can be combined with the MSAA coverage mask.
//
// When _ALPHATEST_ON is defined:     The returned value follows the behavior noted in the AlphaClip function
// When _ALPHATEST_ON is not defined: The returned value is equal to the original alpha input parameter
//
// NOTE: When _ALPHATEST_ON is not defined, this function is effectively a no-op.
real AlphaDiscard(real alpha, real cutoff, real offset = real(0.0))
{
#if defined(_ALPHATEST_ON)
    if (IsAlphaDiscardEnabled())
        alpha = AlphaClip(alpha, cutoff + offset);
#endif

    return alpha;
}

half OutputAlpha(half alpha, bool isTransparent)
{
    if (isTransparent)
    {
        return alpha;
    }
    else
    {
#if defined(_ALPHATEST_ON)
        // Opaque materials should always export an alpha value of 1.0 unless alpha-to-coverage is available
        return IsAlphaToMaskAvailable() ? alpha : 1.0;
#else
        return 1.0;
#endif
    }
}

half3 AlphaModulate(half3 albedo, half alpha)
{
    // Fake alpha for multiply blend by lerping albedo towards 1 (white) using alpha.
    // Manual adjustment for "lighter" multiply effect (similar to "premultiplied alpha")
    // would be painting whiter pixels in the texture.
    // This emulates that procedure in shader, so it should be applied to the base/source color.
#if defined(_ALPHAMODULATE_ON)
    return lerp(half3(1.0, 1.0, 1.0), albedo, alpha);
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

// Normalization used to depend on SHADER_QUALITY
// Currently we always normalize to avoid lighting issues
// and platform inconsistencies.
half3 NormalizeNormalPerVertex(half3 normalWS)
{
    return normalize(normalWS);
}

float3 NormalizeNormalPerVertex(float3 normalWS)
{
    return normalize(normalWS);
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
    #if defined(FOG_LINEAR_KEYWORD_DECLARED)
    if (FOG_LINEAR)
    {
        // factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
        float fogFactor = saturate(z * unity_FogParams.z + unity_FogParams.w);
        return real(fogFactor);
    }
    else if (FOG_EXP || FOG_EXP2)
    {
        // factor = exp(-(density*z)^2)
        // -density * z computed at vertex
        return real(unity_FogParams.x * z);
    }
    else
    {
        // This process is necessary to avoid errors in iOS graphics tests
        // when using the dynamic branching of fog keywords.
        return real(0.0);
    }
    #else // #if defined(FOG_LINEAR_KEYWORD_DECLARED)
    return real(0.0);
    #endif // #if defined(FOG_LINEAR_KEYWORD_DECLARED)
}

real ComputeFogFactor(float zPositionCS)
{
    float clipZ_0Far = UNITY_Z_0_FAR_FROM_CLIPSPACE(zPositionCS);
    return ComputeFogFactorZ0ToFar(clipZ_0Far);
}

half ComputeFogIntensity(half fogFactor)
{
    half fogIntensity = half(0.0);
    #if defined(FOG_LINEAR_KEYWORD_DECLARED)
    if (FOG_EXP)
    {
        // factor = exp(-density*z)
        // fogFactor = density*z compute at vertex
        fogIntensity = saturate(exp2(-fogFactor));
    }
    else if (FOG_EXP2)
    {
        // factor = exp(-(density*z)^2)
        // fogFactor = density*z compute at vertex
        fogIntensity = saturate(exp2(-fogFactor * fogFactor));
    }
    else if (FOG_LINEAR)
    {
        fogIntensity = fogFactor;
    }
    #endif // #if defined(FOG_LINEAR_KEYWORD_DECLARED
    return fogIntensity;
}

// Force enable fog fragment shader evaluation
#define _FOG_FRAGMENT 1
real InitializeInputDataFog(float4 positionWS, real vertFogFactor)
{
    real fogFactor = 0.0;
#if defined(_FOG_FRAGMENT)
    #if defined(FOG_LINEAR_KEYWORD_DECLARED)
    if (FOG_LINEAR || FOG_EXP || FOG_EXP2)
    {
        // Compiler eliminates unused math --> matrix.column_z * vec
        float viewZ = -(mul(UNITY_MATRIX_V, positionWS).z);
        // View Z is 0 at camera pos, remap 0 to near plane.
        float nearToFarZ = max(viewZ - _ProjectionParams.y, 0);
        fogFactor = ComputeFogFactorZ0ToFar(nearToFarZ);
    }
    #endif // #if defined(FOG_LINEAR_KEYWORD_DECLARED)
#else // #if defined(_FOG_FRAGMENT)
    fogFactor = vertFogFactor;
#endif // #if defined(_FOG_FRAGMENT)
    return fogFactor;
}

float ComputeFogIntensity(float fogFactor)
{
    float fogIntensity = 0.0;
    #if defined(FOG_LINEAR_KEYWORD_DECLARED)
        if (FOG_EXP)
        {
            // factor = exp(-density*z)
            // fogFactor = density*z compute at vertex
            fogIntensity = saturate(exp2(-fogFactor));
        }
        else if (FOG_EXP2)
        {
            // factor = exp(-(density*z)^2)
            // fogFactor = density*z compute at vertex
            fogIntensity = saturate(exp2(-fogFactor * fogFactor));
        }
        else if (FOG_LINEAR)
        {
            fogIntensity = fogFactor;
        }
    #endif // #if defined(FOG_LINEAR_KEYWORD_DECLARED)
    return fogIntensity;
}

half3 MixFogColor(half3 fragColor, half3 fogColor, half fogFactor)
{
    #if defined(FOG_LINEAR_KEYWORD_DECLARED)
        if (FOG_LINEAR || FOG_EXP || FOG_EXP2)
        {
            half fogIntensity = ComputeFogIntensity(fogFactor);
            // Workaround for UUM-61728: using a manual lerp to avoid rendering artifacts on some GPUs when Vulkan is used
            fragColor = fragColor * fogIntensity + fogColor * (half(1.0) - fogIntensity);    
        }
    #endif // #if defined(FOG_LINEAR_KEYWORD_DECLARED)
    return fragColor;
}

float3 MixFogColor(float3 fragColor, float3 fogColor, float fogFactor)
{
    #if defined(FOG_LINEAR_KEYWORD_DECLARED)
        if (FOG_LINEAR || FOG_EXP || FOG_EXP2)
        {
            if (IsFogEnabled())
            {
                float fogIntensity = ComputeFogIntensity(fogFactor);
                fragColor = lerp(fogColor, fragColor, fogIntensity);
            }
        }
    #endif // #if defined(FOG_LINEAR_KEYWORD_DECLARED)
    return fragColor;
} 

half3 MixFog(half3 fragColor, half fogFactor)
{
    return MixFogColor(fragColor, half3(unity_FogColor.rgb), fogFactor);
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

// Select uint4 component by index.
// Helper to improve codegen for 2d indexing (data[x][y])
// Replace:
// data[i / 4][i % 4];
// with:
// select4(data[i / 4], i % 4);
uint Select4(uint4 v, uint i)
{
    // x = 0 = 00
    // y = 1 = 01
    // z = 2 = 10
    // w = 3 = 11
    uint mask0 = uint(int(i << 31) >> 31);
    uint mask1 = uint(int(i << 30) >> 31);
    return
        (((v.w & mask0) | (v.z & ~mask0)) & mask1) |
        (((v.y & mask0) | (v.x & ~mask0)) & ~mask1);
}

#if SHADER_TARGET < 45
uint URP_FirstBitLow(uint m)
{
    // http://graphics.stanford.edu/~seander/bithacks.html#ZerosOnRightFloatCast
    return (asuint((float)(m & asuint(-asint(m)))) >> 23) - 0x7F;
}
#define FIRST_BIT_LOW URP_FirstBitLow
#else
#define FIRST_BIT_LOW firstbitlow
#endif

#define UnityStereoTransformScreenSpaceTex(uv) uv

uint GetMeshRenderingLayer()
{
    return asuint(unity_RenderingLayer.x);
}

float EncodeMeshRenderingLayer(uint renderingLayer)
{
    // Force any bits above max to be skipped
    renderingLayer &= _RenderingLayerMaxInt;

    // This is copy of "real PackInt(uint i, uint numBits)" from com.unity.render-pipelines.core\ShaderLibrary\Packing.hlsl
    // Differences of this copy:
    // - Pre-computed rcpMaxInt
    // - Returns float instead of real
    float rcpMaxInt = _RenderingLayerRcpMaxInt;
    return saturate(renderingLayer * rcpMaxInt);
}

uint DecodeMeshRenderingLayer(float renderingLayer)
{
    // This is copy of "uint UnpackInt(real f, uint numBits)" from com.unity.render-pipelines.core\ShaderLibrary\Packing.hlsl
    // Differences of this copy:
    // - Pre-computed maxInt
    // - Parameter f is float instead of real
    uint maxInt = _RenderingLayerMaxInt;
    return (uint)(renderingLayer * maxInt + 0.5); // Round instead of truncating
}

// TODO: implement
float GetCurrentExposureMultiplier()
{
    return 1;
}

#endif // UNITY_SHADER_VARIABLES_FUNCTIONS_INCLUDED
