#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
#ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
    uint3 fadeMaskSeed = asuint((int3)(V * _ScreenSize.xyx)); // Quantize V to _ScreenSize values
    LODDitheringTransition(fadeMaskSeed, unity_LODFade.x);
#endif

    ApplyDoubleSidedFlipOrMirror(input); // Apply double sided flip on the vertex normal
    GetNormalWS(input, float3(0.0, 0.0, 1.0), surfaceData.normalWS);

    surfaceData.geomNormalWS = input.worldToTangent[2];

    surfaceData.baseColor = input.color.rgb * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texCoord0.xy).rgb;
    float alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texCoord0.xy).a * _Color.a;

#ifdef SPEEDTREE_ALPHATEST
    clip(alpha - _Cutoff);
#endif

    surfaceData.metallic = 0;
    surfaceData.ambientOcclusion = input.color.a;

#ifdef EFFECT_HUE_VARIATION
    float3 shiftedColor = lerp(surfaceData.baseColor, _HueVariation.rgb, input.texCoord0.z);
    float maxBase = max(surfaceData.baseColor.r, max(surfaceData.baseColor.g, surfaceData.baseColor.b));
    float newMaxBase = max(shiftedColor.r, max(shiftedColor.g, shiftedColor.b));
    maxBase /= newMaxBase;
    maxBase = maxBase * 0.5f + 0.5f;

    surfaceData.baseColor = saturate(shiftedColor.rgb * maxBase);
#endif

    surfaceData.tangentWS = normalize(input.worldToTangent[0].xyz); // The tangent is not normalize in worldToTangent for mikkt. Tag: SURFACE_GRADIENT
    surfaceData.subsurfaceMask = 0;
    surfaceData.thickness = 1;
    surfaceData.diffusionProfile = 0;

#ifdef EFFECT_BUMP
#endif

    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;

    // Init other parameters
    surfaceData.anisotropy = 0.0;
    surfaceData.specularColor = SAMPLE_TEXTURE2D(_SpecTex, sampler_MainTex, input.texCoord0.xy).rgb;
    surfaceData.perceptualSmoothness = SAMPLE_TEXTURE2D(_SpecTex, sampler_MainTex, input.texCoord0.xy).a;
    surfaceData.coatMask = 0.0;
    surfaceData.iridescenceThickness = 0.0;
    surfaceData.iridescenceMask = 0.0;

    if (surfaceData.ambientOcclusion != 1.0f)
        surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(dot(surfaceData.normalWS, V)), surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
    else
        surfaceData.specularOcclusion = 1.0f;

    // Transparency parameters
    // Use thickness from SSS
    surfaceData.ior = 1.0;
    surfaceData.transmittanceColor = float3(1.0, 1.0, 1.0);
    surfaceData.atDistance = 1000000.0;
    surfaceData.transmittanceMask = 0.0;

    InitBuiltinData(alpha, surfaceData.normalWS, -surfaceData.geomNormalWS, input.positionRWS, input.texCoord1, input.texCoord2, builtinData);
    PostInitBuiltinData(V, posInput, surfaceData, builtinData);
}
