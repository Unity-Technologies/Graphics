#ifndef HDRP_SPEEDTREE8_LITDATA_INCLUDED
#define HDRP_SPEEDTREE8_LITDATA_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
#if !defined(SHADER_QUALITY_LOW)
    #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
        #ifdef EFFECT_BILLBOARD
            LODDitheringTransition(input.interpolated.clipPos.xyz, unity_LODFade.x);
        #endif
    #endif
#endif

    surfaceData.baseColor = input.color.rgb * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texCoord0.xy).rgb;
    float alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texCoord0.xy).a * _Color.a;

#ifdef SPEEDTREE_ALPHATEST
    clip(alpha - _Cutoff);
#endif

    ApplyDoubleSidedFlipOrMirror(input, doubleSidedConstants); // Apply double sided flip on the vertex normal
    GetNormalWS(input, float3(0.0, 0.0, 1.0), surfaceData.normalWS, doubleSidedConstants);

    surfaceData.geomNormalWS = input.worldToTangent[2];

#ifdef EFFECT_BUMP
    float3 tanNorm = 2.0 * (SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.texCoord0.xy).rgb - float3(0.5, 0.5, 0.5));

    // adjust billboard normals to improve GI and matching
#ifdef EFFECT_BILLBOARD
    tanNorm.z *= 0.5;
    tanNorm = normalize(tanNorm);
#endif

    surfaceData.normalWS = normalize(mul(tanNorm.zyx, input.worldToTangent));
#endif

    // adjust billboard normals to improve GI and matching
#ifdef EFFECT_BILLBOARD
    normalTs.z *= 0.5;
    normalTs = normalize(normalTs);
#endif

#ifdef EFFECT_HUE_VARIATION
    float3 shiftedColor = lerp(surfaceData.baseColor, _HueVariation.rgb, input.texCoord0.z);
    float maxBase = max(surfaceData.baseColor.r, max(surfaceData.baseColor.g, surfaceData.baseColor.b));
    float newMaxBase = max(shiftedColor.r, max(shiftedColor.g, shiftedColor.b));
    maxBase /= newMaxBase;
    maxBase = maxBase * 0.5f + 0.5f;

    surfaceData.baseColor = saturate(shiftedColor.rgb * maxBase);
#endif

    surfaceData.ambientOcclusion = input.color.a;
}

#endif
