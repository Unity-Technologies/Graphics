#ifndef UNIVERSAL_DETAILMAP_INCLUDED
#define UNIVERSAL_DETAILMAP_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
// Utility functionality for Universal RP materials that has the _DETAIL_MULX2 _DETAIL_SCALED shader feature.

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

// FEATURES_DETAIL_MULX2 allows the preprocessor to know if the _DETAIL_MULX2 might be used.
// Dynamic branch mode: always true if _DETAIL_MULX2 feature exists; use UseDetailMapMul2X() to query runtime usage.
// Specialized variants mode: only true when _DETAIL_MULX2 is active.
#if (_DETAIL_MULX2_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #undef FEATURES_DETAILMAP_MULX2
    #define FEATURES_DETAILMAP_MULX2 1
#elif !defined(_DETAIL_MULX2_KEYWORD_DECLARED) || (_DETAIL_MULX2_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if DEFINED_NONZERO(_DETAIL_MULX2)
        #undef FEATURES_DETAILMAP_MULX2
        #define FEATURES_DETAILMAP_MULX2 1
    #endif
#endif

// FEATURES_DETAIL_SCALED allows the preprocessor to know if the _DETAIL_SCALED might be used.
// Dynamic branch mode: always true if _DETAIL_SCALED feature exists; use UseDetailMapScaled() to query runtime usage.
// Specialized variants mode: only true when _DETAIL_SCALED is active.
#if (_DETAIL_SCALED_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #undef FEATURES_DETAILMAP_SCALED
    #define FEATURES_DETAILMAP_SCALED 1
#elif !defined(_DETAIL_SCALED_KEYWORD_DECLARED) || (_DETAIL_SCALED_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if DEFINED_NONZERO(_DETAIL_SCALED)
        #undef FEATURES_DETAILMAP_SCALED
        #define FEATURES_DETAILMAP_SCALED 1
    #endif
#endif

// FEATURES_DETAIL allows the preprocessor to know if FEATURES_DETAIL_MULX2 or _DETAIL_SCALED might be used.
// Dynamic branch mode: always true if _DETAIL_MULX2 or _DETAIL_SCALED feature exists; use UseDetailMap() to query runtime usage.
// Specialized variants mode: only true when _DETAIL_MULX2 or _DETAIL_SCALED is active.
#define FEATURES_DETAILMAP FEATURES_DETAILMAP_MULX2 || FEATURES_DETAILMAP_SCALED

// Returns true if the _DETAIL_MULX2 shader feature is enabled, otherwise false.
bool UseDetailMapMul2X()
{
    #if defined(_DETAIL_MULX2_KEYWORD_DECLARED)
        return _DETAIL_MULX2;
    #else
        #if defined(_DETAIL_MULX2)
            return _DETAIL_MULX2;
        #else
            return false;
        #endif
    #endif
}

// Returns true if the _DETAIL_SCALED shader feature is enabled, otherwise false.
bool UseDetailMapScaled()
{
    #if defined(_DETAIL_SCALED_KEYWORD_DECLARED)
        return _DETAIL_SCALED;
    #else
        #if defined(_DETAIL_SCALED)
            return _DETAIL_SCALED;
        #else
            return false;
        #endif
    #endif
}

// Returns true if the _DETAIL_MULX2 or _DETAIL_SCALED shader features are enabled, otherwise false.
bool UseDetailMap()
{
    return UseDetailMapMul2X() || UseDetailMapScaled();
}

// Simple getters the including material's input file must define.
half4 SampleDetailAlbedoMap(float2 uv);
half4 SampleDetailNormalMap(float2 uv);
half GetDetailAlbedoMapScale();
half GetDetailNormalMapScale();

// Used for scaling detail albedo. Main features:
// - Depending if detailAlbedo brightens or darkens, scale magnifies effect.
// - No effect is applied if detailAlbedo is 0.5.
half3 ScaleDetailAlbedo(half3 detailAlbedo, half scale)
{
    // detailAlbedo = detailAlbedo * 2.0h - 1.0h;
    // detailAlbedo *= _DetailAlbedoMapScale;
    // detailAlbedo = detailAlbedo * 0.5h + 0.5h;
    // return detailAlbedo * 2.0f;

    // A bit more optimized
    return half(2.0) * detailAlbedo * scale - scale + half(1.0);
}

half3 ApplyDetailAlbedo(float2 detailUv, half3 albedo, half detailMask)
{
    half3 result = albedo;

    if (UseDetailMap())
    {
        half3 detailAlbedo = SampleDetailAlbedoMap(detailUv).rgb;

        // In order to have same performance as builtin, we do scaling only if scale is not 1.0 (Scaled version has 6 additional instructions)
        if (UseDetailMapScaled())
            detailAlbedo = ScaleDetailAlbedo(detailAlbedo, GetDetailAlbedoMapScale());
        else
            detailAlbedo = half(2.0) * detailAlbedo;

        result *= LerpWhiteTo(detailAlbedo, detailMask);
    }

    return result;
}

half3 ApplyDetailNormal(float2 detailUv, half3 normalTS, half detailMask)
{
    half3 result = normalTS;

    if (UseDetailMap())
    {
        #if BUMP_SCALE_NOT_SUPPORTED
        half3 detailNormalTS = UnpackNormal(SampleDetailNormalMap(detailUv));
        #else
        half3 detailNormalTS = UnpackNormalScale(SampleDetailNormalMap(detailUv), GetDetailNormalMapScale());
        #endif

        // With UNITY_NO_DXT5nm unpacked vector is not normalized for BlendNormalRNM
        // For visual consistancy we going to do in all cases
        detailNormalTS = normalize(detailNormalTS);

        result = lerp(normalTS, BlendNormalRNM(normalTS, detailNormalTS), detailMask); // todo: detailMask should lerp the angle of the quaternion rotation, not the normals
    }

    return result;
}

#endif
