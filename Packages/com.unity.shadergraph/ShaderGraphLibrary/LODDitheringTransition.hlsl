// Shadergraph-friendly implementation of LODDitheringTransition.
// The function as defined in Common.hlsl terminates on clip(f).
// However, since it does not return or output anything, shadergraph
// doesn't recognize it as code that gets used. This file can be removed
// and replaced with a string custom function if Shader Graph ever adds
// support for flagging custom function nodes as used, even if not
// connected to anything.
#ifndef SHADERGRAPH_CROSSFADE_INCLUDED
#define SHADERGRAPH_CROSSFADE_INCLUDED
#ifndef UNITY_MATERIAL_INCLUDED
uint2 ComputeFadeMaskSeed(float3 V, uint2 positionSS)
{
    uint2 fadeMaskSeed;

    // Is this a reasonable quality gate?
#if defined(SHADER_QUALITY_HIGH)
    if (IsPerspectiveProjection())
    {
        // Start with the world-space direction V. It is independent from the orientation of the camera,
        // and only depends on the position of the camera and the position of the fragment.
        // Now, project and transform it into [-1, 1].
        float2 pv = PackNormalOctQuadEncode(V);
        // Rescale it to account for the resolution of the screen.
        pv *= _ScreenParams.xy;
        // The camera only sees a small portion of the sphere, limited by hFoV and vFoV.
        // Therefore, we must rescale again (before quantization), roughly, by 1/tan(FoV/2).
        pv *= UNITY_MATRIX_P._m00_m11;
        // Truncate and quantize.
        fadeMaskSeed = asuint((int2)pv);
    }
    else
#endif
    {
        // Can't use the view direction, it is the same across the entire screen.
        fadeMaskSeed = positionSS;
    }

    return fadeMaskSeed;
}
#endif
void LODDitheringTransitionSG_float(float3 viewDirWS, float4 screenPos, out float multiplyAlpha)
{
#if !defined (SHADER_API_GLES) && !defined(SHADER_STAGE_RAY_TRACING)
    float p = GenerateHashedRandomFloat(ComputeFadeMaskSeed(viewDirWS, screenPos.xy));
    float f = unity_LODFade.x - CopySign(p, unity_LODFade.x);
        multiplyAlpha = f < 0 ? 0.0f : 1.0f;
#endif
}
void DoLODCrossFade_half(float3 viewDirWS, float4 screenPos, out half halfAlpha)
{
#if !defined (SHADER_API_GLES) && !defined(SHADER_STAGE_RAY_TRACING)
    float p = GenerateHashedRandomFloat(ComputeFadeMaskSeed(viewDirWS, screenPos.xy));
    float f = unity_LODFade.x - CopySign(p, unity_LODFade.x);
    float multiplyAlpha = f < 0 ? 0.0f : 1.0f;
    halfAlpha = (half)multiplyAlpha;
#endif
}
#endif
