#ifndef UNITY_DATA_EXTRACTION_INCLUDED
#define UNITY_DATA_EXTRACTION_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

// These correspond to UnityEngine.Camera.RenderRequestMode enum values
#define RENDER_OBJECT_ID               1
#define RENDER_DEPTH                   2
#define RENDER_WORLD_NORMALS_FACE_RGB  3
#define RENDER_WORLD_POSITION_RGB      4
#define RENDER_ENTITY_ID               5
#define RENDER_BASE_COLOR_RGBA         6
#define RENDER_SPECULAR_RGB            7
#define RENDER_METALLIC_R              8
#define RENDER_EMISSION_RGB            9
#define RENDER_WORLD_NORMALS_PIXEL_RGB 10
#define RENDER_SMOOTHNESS_R            11
#define RENDER_OCCLUSION_R             12
#define RENDER_DIFFUSE_COLOR_RGBA      13
#define RENDER_OUTLINE_MASK            14

float4 ComputeSelectionMask(float objectGroupId, float3 ndcWithZ, TEXTURE2D_PARAM(depthBuffer, sampler_depthBuffer))
{
    float sceneZ = SAMPLE_TEXTURE2D(depthBuffer, sampler_depthBuffer, ndcWithZ.xy).r;
    // Use a small multiplicative Z bias to make it less likely for objects to self occlude in the outline buffer
    static const float zBias = 0.02;
#if UNITY_REVERSED_Z
    float pixelZ = ndcWithZ.z * (1 + zBias);
    bool occluded = pixelZ < sceneZ;
#else
    float pixelZ = ndcWithZ.z * (1 - zBias);
    bool occluded = pixelZ > sceneZ;
#endif
    // Red channel = unique identifier, can be used to separate groups of objects from each other
    //               to get outlines between them.
    // Green channel = occluded behind depth buffer (0) or not occluded (1)
    // Blue channel  = always 1 = not cleared to zero = there's an outlined object at this pixel
    return float4(objectGroupId, occluded ? 0 : 1, 1, 1);
}

#endif

