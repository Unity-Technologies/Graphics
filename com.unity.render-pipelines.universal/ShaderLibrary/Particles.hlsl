#ifndef UNIVERSAL_PARTICLES_INCLUDED
#define UNIVERSAL_PARTICLES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

TEXTURE2D(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);
TEXTURE2D(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);

// Pre-multiplied alpha helper
#if defined(_ALPHAPREMULTIPLY_ON)
#define ALBEDO_MUL albedo
#else
#define ALBEDO_MUL albedo.a
#endif

// Color blending fragment function
float4 MixParticleColor(float4 baseColor, float4 particleColor, float4 colorAddSubDiff)
{
#if defined(_COLOROVERLAY_ON) // Overlay blend
    float4 output = baseColor;
    output.rgb = lerp(1 - 2 * (1 - baseColor.rgb) * (1 - particleColor.rgb), 2 * baseColor.rgb * particleColor.rgb, step(baseColor.rgb, 0.5));
    output.a *= particleColor.a;
    return output;
#elif defined(_COLORCOLOR_ON) // Color blend
    half3 aHSL = RgbToHsv(baseColor.rgb);
    half3 bHSL = RgbToHsv(particleColor.rgb);
    half3 rHSL = half3(bHSL.x, bHSL.y, aHSL.z);
    return half4(HsvToRgb(rHSL), baseColor.a * particleColor.a);
#elif defined(_COLORADDSUBDIFF_ON) // Additive, Subtractive and Difference blends based on 'colorAddSubDiff'
    float4 output = baseColor;
    output.rgb = baseColor.rgb + particleColor.rgb * colorAddSubDiff.x;
    output.rgb = lerp(output.rgb, abs(output.rgb), colorAddSubDiff.y);
    output.a *= particleColor.a;
    return output;
#else // Default to Multiply blend
    return baseColor * particleColor;
#endif
}

// Soft particles - returns alpha value for fading particles based on the depth to the background pixel
float SoftParticles(float near, float far, float4 projection)
{
    float fade = 1;
    if (near > 0.0 || far > 0.0)
    {
        float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, projection.xy / projection.w), _ZBufferParams);
        float thisZ = LinearEyeDepth(projection.z / projection.w, _ZBufferParams);
        fade = saturate (far * ((sceneZ - near) - thisZ));
    }
    return fade;
}

// Camera fade - returns alpha value for fading particles based on camera distance
half CameraFade(float near, float far, float4 projection)
{
    float thisZ = LinearEyeDepth(projection.z / projection.w, _ZBufferParams);
    return saturate((thisZ - near) * far);
}

half3 AlphaModulate(half3 albedo, half alpha)
{
#if defined(_ALPHAMODULATE_ON)
    return lerp(half3(1.0h, 1.0h, 1.0h), albedo, alpha);
#elif defined(_ALPHAPREMULTIPLY_ON)
    return albedo * alpha;
#else
    return albedo;
#endif
}

half3 Distortion(float4 baseColor, float3 normal, half strength, half blend, float4 projection)
{
    float2 screenUV = (projection.xy / projection.w) + normal.xy * strength * baseColor.a;
    float4 Distortion = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV);
    return lerp(Distortion.rgb, baseColor.rgb, saturate(baseColor.a - blend));
}

// Sample a texture and do blending for texture sheet animation if needed
half4 BlendTexture(TEXTURE2D_PARAM(_Texture, sampler_Texture), float2 uv, float3 blendUv)
{
    half4 color = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, uv);
#ifdef _FLIPBOOKBLENDING_ON
    half4 color2 = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, blendUv.xy);
    color = lerp(color, color2, blendUv.z);
#endif
    return color;
}

// Sample a normal map in tangent space
half3 SampleNormalTS(float2 uv, float3 blendUv, TEXTURE2D_PARAM(bumpMap, sampler_bumpMap), half scale = 1.0h)
{
#if defined(_NORMALMAP)
    half4 n = BlendTexture(TEXTURE2D_ARGS(bumpMap, sampler_bumpMap), uv, blendUv);
    #if BUMP_SCALE_NOT_SUPPORTED
        return UnpackNormal(n);
    #else
        return UnpackNormalScale(n, scale);
    #endif
#else
    return half3(0.0h, 0.0h, 1.0h);
#endif
}

#endif // UNIVERSAL_PARTICLES_INCLUDED
