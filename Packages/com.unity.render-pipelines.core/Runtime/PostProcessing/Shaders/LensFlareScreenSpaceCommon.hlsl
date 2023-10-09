#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ScreenCoordOverride.hlsl"

TEXTURE2D(_LensFlareScreenSpaceSpectralLut);

TEXTURE2D_X(_LensFlareScreenSpaceStreakTex);
TEXTURE2D_X(_LensFlareScreenSpaceBloomMipTexture);
TEXTURE2D_X(_LensFlareScreenSpaceResultTexture);

float4 _LensFlareScreenSpaceBloomMipTexture_TexelSize;
float4 _LensFlareScreenSpaceStreakTex_TexelSize;

float4 _LensFlareScreenSpaceParams1;
float4 _LensFlareScreenSpaceParams2;
float4 _LensFlareScreenSpaceParams3;
float4 _LensFlareScreenSpaceParams4;
float4 _LensFlareScreenSpaceParams5;

int _LensFlareScreenSpaceMipLevel;
float3 _LensFlareScreenSpaceTintColor;

#define LensFlareScreenSpaceIntensity               _LensFlareScreenSpaceParams1.x
#define LensFlareScreenSpaceFirstIntensity          _LensFlareScreenSpaceParams1.y
#define LensFlareScreenSpaceSecondaryIntensity      _LensFlareScreenSpaceParams1.z
#define LensFlareScreenSpacePolarIntensity          _LensFlareScreenSpaceParams1.w

#define LensFlareScreenSpaceVignetteIntensity       _LensFlareScreenSpaceParams2.x
#define LensFlareScreenSpaceStartingPosition        _LensFlareScreenSpaceParams2.y
#define LensFlareScreenSpaceScale                   _LensFlareScreenSpaceParams2.z

#define LensFlareScreenSpaceCount                   _LensFlareScreenSpaceParams3.x
#define LensFlareScreenSpaceCountDimmer             _LensFlareScreenSpaceParams3.y
#define LensFlareScreenSpaceChromaIntensity         _LensFlareScreenSpaceParams3.z
#define LensFlareScreenSpaceChromaSample            _LensFlareScreenSpaceParams3.w

#define LensFlareScreenSpaceStreakIntensity         _LensFlareScreenSpaceParams4.x
#define LensFlareScreenSpaceStreakLength            _LensFlareScreenSpaceParams4.y
#define LensFlareScreenSpaceStreakOrientation       _LensFlareScreenSpaceParams4.z
#define LensFlareScreenSpaceStreakThreshold         _LensFlareScreenSpaceParams4.w

#define LensFlareScreenSpaceRatio                   _LensFlareScreenSpaceParams5.x
#define LensFlareScreenSpaceWarpedScaleX            _LensFlareScreenSpaceParams5.y
#define LensFlareScreenSpaceWarpedScaleY            _LensFlareScreenSpaceParams5.z

#define REGULAR_FLARE_MULTIPLIER    0.1
#define STREAK_FLARE_MULTIPLIER     1

struct AttributesSSLF
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsSSLF
{
    float4 positionCS : SV_POSITION;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

float2 GetAnamorphism()
{
    float f = frac(LensFlareScreenSpaceStreakOrientation);
    bool even = ((floor(LensFlareScreenSpaceStreakOrientation) % 2) == 0);

    float x = even ? -(1.0 - f) : -(1.0 - (1.0 - f));
    float y = even ? f : -(1.0 - f);

    return float2(x, y);
}

float map01To(float value, float min, float max)
{
    return (max - min) * (value - 0.5);
}

float map01(float value, float min, float max)
{
    float r = rcp(max - min);
    return Remap01(value, r, min * r);
}

float2 scaleUV(float2 uv, float2 center, float scale, bool invert, bool polar)
{
    if (polar)
    {
        scale = rcp(scale);
        // Correctly map the UV before doing polar conversion
        uv = float2(LensFlareScreenSpaceWarpedScaleX * map01To(uv.x, -scale, scale), LensFlareScreenSpaceWarpedScaleY * map01To(uv.y, -scale, scale));

        // Polar coordinate conversion
        float x = SafeSqrt(SafePositivePow(uv.x, 2) + SafePositivePow(uv.y, 2));
        float x1 = map01(x, 0.0, sqrt(2.0));

        float y = FastAtan2(uv.x, uv.y);
        float y1 = 1.0 - map01(y, -PI, PI);

        uv = float2(y1, invert ? (1.0 - x1) : x1);
    }
    else
    {
        // First, we substract the center before scaling
        uv -= center;

        uv *= 1.0 / scale;

        // Then, we add center back
        uv += center;

        if (invert)
        {
            uv = 1.0 - uv;
        }
    }

    return uv;
}

float3 GetFlareTexture(float2 uv, float scale, float intensity, bool polar, bool regularFlarePass)
{
    bool invert = scale < 0.0;
    bool distortUV = (scale != 1 || polar);
    float signScale = sign(scale);

    bool chromaticAberration = (LensFlareScreenSpaceChromaIntensity > 0);

    //Chromatic
    float2 coords = 2.0 * uv - 1.0;
    float2 end = uv - coords * dot(coords, coords) * LensFlareScreenSpaceChromaIntensity;
    float2 diff = end - uv;

    if (distortUV)
        uv = scaleUV(uv, float2(0.5, 0.5), abs(scale), invert, polar);

#if defined (HDRP_LENS_FLARE_SCREEN_SPACE)

    // HDRP Implementation taken from UberPost shader
    int samples = 1;
    if (chromaticAberration)
    {
        samples = int(length(_PostProcessScreenSize.xy * diff / 2.0));
        samples = clamp(samples, 3, (int)LensFlareScreenSpaceChromaSample);
    }

    float2 delta = diff / samples;

    float3 sum = 0.0;
    float3 filterSum = 0.0;
    float3 filter = 0.0;

    for (int i = 0; i < samples; i++)
    {
        float3 s = 0.0;

        // Depending on if we are computing regular flares or streaks, we sample a different texture.
        if (regularFlarePass)
        {
            s = SAMPLE_TEXTURE2D_X_LOD(_LensFlareScreenSpaceBloomMipTexture, s_linear_clamp_sampler, ClampAndScaleUVForBilinearPostProcessTexture(SCREEN_COORD_REMOVE_SCALEBIAS(uv)), 0.0).xyz;
        }
        else
        {
            s = SAMPLE_TEXTURE2D_X_LOD(_LensFlareScreenSpaceStreakTex, s_linear_clamp_sampler, ClampAndScaleUVForBilinearPostProcessTexture(SCREEN_COORD_REMOVE_SCALEBIAS(uv)), 0.0).xyz;
        }

        s *= _LensFlareScreenSpaceTintColor;

        UNITY_BRANCH
        if (chromaticAberration)
        {
            float t = (i + 0.5) / samples;
            filter = SAMPLE_TEXTURE2D_LOD(_LensFlareScreenSpaceSpectralLut, s_linear_clamp_sampler, saturate(float2(t, 0.0)), 0.0).xyz;
        }
        else
        {
            filter = 1.0;
        }

        sum += s * filter;
        filterSum += filter;
        uv += signScale * delta;
    }
    float3 result = sum / filterSum;

#endif

#if defined (URP_LENS_FLARE_SCREEN_SPACE)

    // Taken from URP UberPost Implementation
    float3 result = 0.0;

    // If chromaticAberration Intenisty is zero, we do only one sample.
    if (chromaticAberration)
    {
        diff = diff / 3.0;
        float r, g, b;

        if (regularFlarePass)
        {
            r = SAMPLE_TEXTURE2D_X(_LensFlareScreenSpaceBloomMipTexture, sampler_LinearClamp, ClampAndScaleUVForBilinear(SCREEN_COORD_REMOVE_SCALEBIAS(uv), _LensFlareScreenSpaceBloomMipTexture_TexelSize.xy)               ).x;
            g = SAMPLE_TEXTURE2D_X(_LensFlareScreenSpaceBloomMipTexture, sampler_LinearClamp, ClampAndScaleUVForBilinear(SCREEN_COORD_REMOVE_SCALEBIAS((diff + uv)), _LensFlareScreenSpaceBloomMipTexture_TexelSize.xy)      ).y;
            b = SAMPLE_TEXTURE2D_X(_LensFlareScreenSpaceBloomMipTexture, sampler_LinearClamp, ClampAndScaleUVForBilinear(SCREEN_COORD_REMOVE_SCALEBIAS((diff * 2.0 + uv)), _LensFlareScreenSpaceBloomMipTexture_TexelSize.xy)).z;
        }
        else
        {
            r = SAMPLE_TEXTURE2D_X(_LensFlareScreenSpaceStreakTex, sampler_LinearClamp, ClampAndScaleUVForBilinear(SCREEN_COORD_REMOVE_SCALEBIAS(uv))               ).x;
            g = SAMPLE_TEXTURE2D_X(_LensFlareScreenSpaceStreakTex, sampler_LinearClamp, ClampAndScaleUVForBilinear(SCREEN_COORD_REMOVE_SCALEBIAS((diff + uv))      )).y;
            b = SAMPLE_TEXTURE2D_X(_LensFlareScreenSpaceStreakTex, sampler_LinearClamp, ClampAndScaleUVForBilinear(SCREEN_COORD_REMOVE_SCALEBIAS((diff * 2.0 + uv)))).z;
        }

        result = float3(r, g, b);
    }
    else
    {
        if (regularFlarePass)
        {
            result = SAMPLE_TEXTURE2D_X(_LensFlareScreenSpaceBloomMipTexture, sampler_LinearClamp, ClampAndScaleUVForBilinear(SCREEN_COORD_REMOVE_SCALEBIAS(uv))).xyz;
        }
        else
        {
            result = SAMPLE_TEXTURE2D_X(_LensFlareScreenSpaceStreakTex, sampler_LinearClamp, ClampAndScaleUVForBilinear(SCREEN_COORD_REMOVE_SCALEBIAS(uv))).xyz;
        }
    }

    result *= _LensFlareScreenSpaceTintColor;

#endif

    return result * intensity;
}

VaryingsSSLF vert(AttributesSSLF input)
{
    VaryingsSSLF output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float4 positionCS = float4(2.0, 2.0, 1.0, 1.0) * GetQuadVertexPosition(input.vertexID) - float4(1.0, 1.0, 0.0, 0.0);
    float2 uv = GetQuadTexCoord(input.vertexID);
    uv.y = 1.0 - uv.y;

    output.positionCS = positionCS;
    output.texcoord = uv;

    return output;
}

// Prefilter: Shrink horizontally and apply threshold.
float4 FragmentPrefilter(VaryingsSSLF input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = input.texcoord;
#if defined (HDRP_LENS_FLARE_SCREEN_SPACE)
    uv /= _RTHandlePostProcessScale.xy;
#endif

    float dy = GetAnamorphism().x * _LensFlareScreenSpaceBloomMipTexture_TexelSize.y;
    float dx = GetAnamorphism().y * _LensFlareScreenSpaceBloomMipTexture_TexelSize.x;

    float2 u0 = saturate(float2(uv.x - dx, uv.y - dy));
    float2 u1 = saturate(float2(uv.x + dx, uv.y + dy));

#if defined (HDRP_LENS_FLARE_SCREEN_SPACE)
    float3 c0 = SAMPLE_TEXTURE2D_X_LOD(_LensFlareScreenSpaceBloomMipTexture, s_linear_clamp_sampler, ClampAndScaleUVForBilinearPostProcessTexture(SCREEN_COORD_REMOVE_SCALEBIAS(u0)), 0.0).xyz;
    float3 c1 = SAMPLE_TEXTURE2D_X_LOD(_LensFlareScreenSpaceBloomMipTexture, s_linear_clamp_sampler, ClampAndScaleUVForBilinearPostProcessTexture(SCREEN_COORD_REMOVE_SCALEBIAS(u1)), 0.0).xyz;
#endif

#if defined (URP_LENS_FLARE_SCREEN_SPACE)
    float3 c0 = SAMPLE_TEXTURE2D_X(_LensFlareScreenSpaceBloomMipTexture, sampler_LinearClamp, ClampUVForBilinear(SCREEN_COORD_REMOVE_SCALEBIAS(u0), _LensFlareScreenSpaceBloomMipTexture_TexelSize.xy)).xyz;
    float3 c1 = SAMPLE_TEXTURE2D_X(_LensFlareScreenSpaceBloomMipTexture, sampler_LinearClamp, ClampUVForBilinear(SCREEN_COORD_REMOVE_SCALEBIAS(u1), _LensFlareScreenSpaceBloomMipTexture_TexelSize.xy)).xyz;
#endif

    float3 c = (c0 + c1) / 2.0;

    float br = max(c.r, max(c.g, c.b));
    c *= max(br - LensFlareScreenSpaceStreakThreshold, 0.0) / max(br, 1e-4);

    return float4(c, 1.0);
}

#if defined (HDRP_LENS_FLARE_SCREEN_SPACE)
float3 SampleScaled(TEXTURE2D_X(tex), float2 uv)
{
    return SAMPLE_TEXTURE2D_X(tex, s_linear_clamp_sampler, ClampAndScaleUVForBilinearPostProcessTexture(SCREEN_COORD_REMOVE_SCALEBIAS(uv))).xyz;
}
#endif

#if defined (URP_LENS_FLARE_SCREEN_SPACE)
float3 SampleScaled(TEXTURE2D_X(tex), float2 uv)
{
    return SAMPLE_TEXTURE2D_X(tex, sampler_LinearClamp, ClampUVForBilinear(SCREEN_COORD_REMOVE_SCALEBIAS(uv))).xyz;
}
#endif

//Downsampler Pass
float4 FragmentDownsample(VaryingsSSLF input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = input.texcoord.xy;
#if defined (HDRP_LENS_FLARE_SCREEN_SPACE)
    uv /= _RTHandlePostProcessScale.xy;
#endif

    float dy = GetAnamorphism().y * _LensFlareScreenSpaceStreakTex_TexelSize.y * LensFlareScreenSpaceStreakLength * ((float)_LensFlareScreenSpaceMipLevel + 1.0) / LensFlareScreenSpaceRatio;
    float dx = GetAnamorphism().x * _LensFlareScreenSpaceStreakTex_TexelSize.x * LensFlareScreenSpaceStreakLength * ((float)_LensFlareScreenSpaceMipLevel + 1.0) / LensFlareScreenSpaceRatio;

    float2 u0 = saturate(float2(uv.x - dx * 5.0, uv.y - dy * 5.0));
    float2 u1 = saturate(float2(uv.x - dx * 3.0, uv.y - dy * 3.0));
    float2 u2 = saturate(float2(uv.x - dx * 1.0, uv.y - dy * 1.0));
    float2 u3 = saturate(float2(uv.x + dx * 1.0, uv.y + dy * 1.0));
    float2 u4 = saturate(float2(uv.x + dx * 3.0, uv.y + dy * 3.0));
    float2 u5 = saturate(float2(uv.x + dx * 5.0, uv.y + dy * 5.0));

    float3 c0 = 1.0 * SampleScaled(_LensFlareScreenSpaceStreakTex, u0) / 12.0;
    float3 c1 = 2.0 * SampleScaled(_LensFlareScreenSpaceStreakTex, u1) / 12.0;
    float3 c2 = 3.0 * SampleScaled(_LensFlareScreenSpaceStreakTex, u2) / 12.0;
    float3 c3 = 3.0 * SampleScaled(_LensFlareScreenSpaceStreakTex, u3) / 12.0;
    float3 c4 = 2.0 * SampleScaled(_LensFlareScreenSpaceStreakTex, u4) / 12.0;
    float3 c5 = 1.0 * SampleScaled(_LensFlareScreenSpaceStreakTex, u5) / 12.0;

    return float4((c0 + c1 + c2 + c3 + c4 + c5), 1.0);
}

//Upsampler Pass
float4 FragmentUpsample(VaryingsSSLF input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = input.texcoord;
#if defined (HDRP_LENS_FLARE_SCREEN_SPACE)
    uv /= _RTHandlePostProcessScale.xy;
#endif

    float dy = GetAnamorphism().y * _LensFlareScreenSpaceStreakTex_TexelSize.y * LensFlareScreenSpaceStreakLength * 1.5 * ((float)_LensFlareScreenSpaceMipLevel + 1.0) / LensFlareScreenSpaceRatio;
    float dx = GetAnamorphism().x * _LensFlareScreenSpaceStreakTex_TexelSize.x * LensFlareScreenSpaceStreakLength * 1.5 * ((float)_LensFlareScreenSpaceMipLevel + 1.0) / LensFlareScreenSpaceRatio;

    float2 u0 = saturate(float2(uv.x - dx, uv.y - dy));
    float2 u1 = saturate(float2(uv.x, uv.y));
    float2 u2 = saturate(float2(uv.x + dx, uv.y + dy));

    float3 c0 = 1.0 * SampleScaled(_LensFlareScreenSpaceStreakTex, u0) / 4.0;
    float3 c1 = 2.0 * SampleScaled(_LensFlareScreenSpaceStreakTex, u1) / 4.0;
    float3 c2 = 1.0 * SampleScaled(_LensFlareScreenSpaceStreakTex, u2) / 4.0;

    return float4(c0 + c1 + c2, 1.0);
}

// Final composition
float4 FragmentComposition(VaryingsSSLF input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = input.texcoord;

    float3 streakFlare = 0.0;
    float3 regularFlare = 0.0;

#if defined (HDRP_LENS_FLARE_SCREEN_SPACE)
    uv /= _RTHandlePostProcessScale.xy;
#endif

    // Streaks
    if (LensFlareScreenSpaceStreakIntensity > 0.0)
    {
        streakFlare = GetFlareTexture(uv, 1, LensFlareScreenSpaceStreakIntensity * STREAK_FLARE_MULTIPLIER, false, false);
    }

    // Vignette Parameters
    float vignettePow = 2.0;
    float vignetteScale = 1.0;
    float vignetteSquaredness = 2.0; // 1.0 and lower means more star shaped, 2.0 ellipsis, above more squared

    // Vignette textures
    float2 uvVignette = scaleUV(uv, float2(0.5, 0.5), vignetteScale, false, false);
    float vignetteX = saturate(SafePositivePow(abs(2.0 * uvVignette.x - 1.0), (vignetteSquaredness)));
    float vignetteY = saturate(SafePositivePow(abs(2.0 * uvVignette.y - 1.0), (vignetteSquaredness)));
    float vignetteRound = saturate(SafePositivePow((vignetteX + vignetteY), (vignettePow)));

    // Texture to have flares only on the edges of the screen
    vignetteRound = lerp(1, vignetteRound, LensFlareScreenSpaceVignetteIntensity);

    // Regular Flare
    if (LensFlareScreenSpaceIntensity > 0.0)
    {
        float3 classic = 0.0;
        float3 classicInv = 0.0;
        float3 polarInv = 0.0;

        // The scale of the texture scales with (index + start)^scale
        for (int i = 0; i < LensFlareScreenSpaceCount; ++i)
        {
            float scale = SafePositivePow(abs((i + LensFlareScreenSpaceStartingPosition)), LensFlareScreenSpaceScale);
            float currentSampleDimmer = SafePositivePow(LensFlareScreenSpaceCountDimmer, i);

            if (LensFlareScreenSpaceSecondaryIntensity > 0.0)
            {
                classic += GetFlareTexture(uv, -scale, LensFlareScreenSpaceSecondaryIntensity * REGULAR_FLARE_MULTIPLIER * currentSampleDimmer, false, true);
            }

            if (LensFlareScreenSpaceFirstIntensity > 0.0)
            {
                classicInv += GetFlareTexture(uv, scale, LensFlareScreenSpaceFirstIntensity * REGULAR_FLARE_MULTIPLIER * currentSampleDimmer, false, true);
            }

            if (LensFlareScreenSpacePolarIntensity > 0.0)
            {
                polarInv += GetFlareTexture(uv, -scale, LensFlareScreenSpacePolarIntensity * REGULAR_FLARE_MULTIPLIER * currentSampleDimmer, true, true);
            }
        }

        // Compositing
        regularFlare += (classicInv + classic) * vignetteRound; // VignetteRound is to avoid having flare in the middle of the screen.
        regularFlare += polarInv * vignetteX;                   // VignetteX is to prevent discontinuities because polar doesn't tile properly.
    }

    float4 final = float4(regularFlare + streakFlare, 1.0);

    final *= LensFlareScreenSpaceIntensity;

    return final;
}

// Write Result to Bloom
real4 FragmentWrite(VaryingsSSLF input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = input.texcoord;

#if defined (HDRP_LENS_FLARE_SCREEN_SPACE)
    uv /= _RTHandlePostProcessScale.xy;
#endif

    return float4(SampleScaled(_LensFlareScreenSpaceResultTexture, uv), 1.0);
}
