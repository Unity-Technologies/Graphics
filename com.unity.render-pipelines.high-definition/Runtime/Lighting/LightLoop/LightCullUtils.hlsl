#ifndef __LIGHTCULLUTILS_H__
#define __LIGHTCULLUTILS_H__

// Used to index into our SFiniteLightBound (g_data) and
// LightVolumeData (_LightVolumeData) buffers.
uint GenerateLightCullDataIndex(uint lightIndex, uint numVisibleLights, uint eyeIndex)
{
    lightIndex = min(lightIndex, numVisibleLights - 1); // Stay within bounds

    // For monoscopic, there is just one set of light cull data structs.
    // In stereo, all of the left eye structs are first, followed by the right eye structs.
    const uint perEyeBaseIndex = eyeIndex * numVisibleLights;
    return (perEyeBaseIndex + lightIndex);
}

struct ScreenSpaceBoundsIndices
{
    uint min;
    uint max;
};

// The returned values are used to index into our AABB screen space bounding box buffer
// Usually named g_vBoundsBuffer.  The two values represent the min/max indices.
ScreenSpaceBoundsIndices GenerateScreenSpaceBoundsIndices(uint lightIndex, uint numVisibleLights, uint eyeIndex)
{
    // In the monoscopic mode, there is one set of bounds (min,max -> 2 * g_iNrVisibLights)
    // In stereo, there are two sets of bounds (leftMin, leftMax, rightMin, rightMax -> 4 * g_iNrVisibLights)
    const uint eyeRelativeBase = eyeIndex * 2 * numVisibleLights;

    ScreenSpaceBoundsIndices indices;
    indices.min = eyeRelativeBase + lightIndex;
    indices.max = indices.min + numVisibleLights;

    return indices;
}

#endif //__LIGHTCULLUTILS_H__
