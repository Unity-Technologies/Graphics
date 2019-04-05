#ifndef HDRP_SPEEDTREE8_LITDATA_INCLUDED
#define HDRP_SPEEDTREE8_LITDATA_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
#if !defined(SHADER_QUALITY_LOW)
    #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
        #ifdef EFFECT_BILLBOARD
            LODDitheringTransition(input.positionSS.xyz, unity_LODFade.x);
        #endif
    #endif
#endif

    surfaceData.baseColor = input.color.rgb * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texCoord0.xy).rgb;
    float alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texCoord0.xy).a * _Color.a * input.color.a;

    clip(alpha - _Cutoff);

#ifdef EFFECT_BACKSIDE_NORMALS
    float3 doubleSidedConstants = float3(1.0, 1.0, -1.0);
#else
    float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
#endif

    ApplyDoubleSidedFlipOrMirror(input, doubleSidedConstants); // Apply double sided flip on the vertex normal
    GetNormalWS(input, float3(0.0, 0.0, 1.0), surfaceData.normalWS, doubleSidedConstants);

    surfaceData.geomNormalWS = input.worldToTangent[2];
    surfaceData.tangentWS = normalize(input.worldToTangent[0].xyz); // The tangent is not normalize in worldToTangent for mikkt. Tag: SURFACE_GRADIENT

#ifdef EFFECT_BUMP
    float3 tanNorm = 2.0 * (SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.texCoord0.xy).rgb - float3(0.5, 0.5, 0.5));

    // adjust billboard normals to improve GI and matching
#ifdef EFFECT_BILLBOARD
    tanNorm.z *= 0.5;
    tanNorm = normalize(tanNorm);
#endif

    surfaceData.normalWS = normalize(mul(tanNorm.zyx, input.worldToTangent));
#endif

#ifdef EFFECT_HUE_VARIATION
    float3 shiftedColor = lerp(surfaceData.baseColor, _HueVariationColor.rgb, input.texCoord0.z);
    float maxBase = max(surfaceData.baseColor.r, max(surfaceData.baseColor.g, surfaceData.baseColor.b));
    float newMaxBase = max(shiftedColor.r, max(shiftedColor.g, shiftedColor.b));
    maxBase /= newMaxBase;
    maxBase = maxBase * 0.5f + 0.5f;

    surfaceData.baseColor = saturate(shiftedColor.rgb * maxBase);
#endif

    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;

    surfaceData.anisotropy = 0.0;
    surfaceData.specularColor = 1.0;
    surfaceData.ambientOcclusion = input.color.a;
    surfaceData.coatMask = 0.0;
    surfaceData.iridescenceThickness = 0.0;
    surfaceData.iridescenceMask = 0.0;

    // extra
#ifdef EFFECT_EXTRA_TEX
    float3 extra = SAMPLE_TEXTURE2D(_ExtraTex, sampler_ExtraTex, input.texCoord0.xy).rgb;
    surfaceData.perceptualSmoothness = extra.r;
    surfaceData.metallic = extra.g;
    surfaceData.ambientOcclusion *= extra.b;
#else
    surfaceData.perceptualSmoothness = _Glossiness;
    surfaceData.metallic = _Metallic;
#endif

    if (surfaceData.ambientOcclusion != 1.0f)
        surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(dot(surfaceData.normalWS, V)), surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
    else
        surfaceData.specularOcclusion = 1.0f;

    // Transparency parameters
    // Use thickness from SSS
    surfaceData.ior = 1.0;
    surfaceData.atDistance = 1000000.0;
#ifdef EFFECT_SUBSURFACE
    surfaceData.transmittanceColor = SAMPLE_TEXTURE2D(_SubsurfaceTex, sampler_SubsurfaceTex, input.texCoord0.xy).rgb * _SubsurfaceColor.rgb;

    if (input.texcoord0.w > GEOM_TYPE_FROND)
    {
        surfaceData.transmittanceMask = 1.0;
        surfaceData.subsurfaceMask = 1.0;
    }
    else
    {
        surfaceData.transmittanceMask = 0.0;
        surfaceData.subsurfaceMask = 0;
    }
#else
    surfaceData.transmittanceColor = float3(1.0, 1.0, 1.0);
    surfaceData.transmittanceMask = 0.0;
    surfaceData.subsurfaceMask = 0;
#endif

    surfaceData.thickness = 1;
    surfaceData.diffusionProfileHash = 0;

    InitBuiltinData(posInput, alpha, surfaceData.normalWS, -surfaceData.geomNormalWS, input.texCoord1, input.texCoord2, builtinData);

    PostInitBuiltinData(V, posInput, surfaceData, builtinData);
}

#endif
