#ifndef UNIVERSAL_SPEEDTREE_UTILITY
#define UNIVERSAL_SPEEDTREE_UTILITY

uint2 ComputeFadeMaskSeed(float3 V, uint2 positionSS)
{
    uint2 fadeMaskSeed;

    // Is this a reasonable quality gate?
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
    {
        // Can't use the view direction, it is the same across the entire screen.
        fadeMaskSeed = positionSS;
    }

    return fadeMaskSeed;
}

#endif
