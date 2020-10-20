#ifndef UNITY_SHADER_VARIABLES_FUNCTIONS_INCLUDED
#define UNITY_SHADER_VARIABLES_FUNCTIONS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

// Helper function for Rendering Layers
#define DEFAULT_LIGHT_LAYERS (RENDERING_LIGHT_LAYERS_MASK >> RENDERING_LIGHT_LAYERS_MASK_SHIFT)
#define DEFAULT_DECAL_LAYERS (RENDERING_DECAL_LAYERS_MASK >> RENDERING_DECAL_LAYERS_MASK_SHIFT)

// Note: we need to mask out only 8bits of the layer mask before encoding it as otherwise any value > 255 will map to all layers active if save in a buffer
uint GetMeshRenderingLightLayer()
{ 
    return _EnableLightLayers ? (asuint(unity_RenderingLayer.x) & RENDERING_LIGHT_LAYERS_MASK) >> RENDERING_LIGHT_LAYERS_MASK_SHIFT : DEFAULT_LIGHT_LAYERS;
}

uint GetMeshRenderingDecalLayer()
{
    return _EnableDecalLayers ? ((asuint(unity_RenderingLayer.x) & RENDERING_DECAL_LAYERS_MASK) >> RENDERING_DECAL_LAYERS_MASK_SHIFT) : DEFAULT_DECAL_LAYERS;
}

// Return absolute world position of current object
float3 GetObjectAbsolutePositionWS()
{
    float4x4 modelMatrix = UNITY_MATRIX_M;
    return GetAbsolutePositionWS(modelMatrix._m03_m13_m23); // Translation object to world
}

float3 GetPrimaryCameraPosition()
{
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    return float3(0, 0, 0);
#else
    return _WorldSpaceCameraPos;
#endif
}

// Could be e.g. the position of a primary camera or a shadow-casting light.
float3 GetCurrentViewPosition()
{
#if (defined(SHADERPASS) && (SHADERPASS != SHADERPASS_SHADOWS))
    return GetPrimaryCameraPosition();
#else
    // This is a generic solution.
    // However, using '_WorldSpaceCameraPos' is better for cache locality,
    // and in case we enable camera-relative rendering, we can statically set the position is 0.
    return UNITY_MATRIX_I_V._14_24_34;
#endif
}

// Returns the forward (central) direction of the current view in the world space.
float3 GetViewForwardDir()
{
    float4x4 viewMat = GetWorldToViewMatrix();
    return -viewMat[2].xyz;
}

// Returns the forward (up) direction of the current view in the world space.
float3 GetViewUpDir()
{
    float4x4 viewMat = GetWorldToViewMatrix();
    return viewMat[1].xyz;
}

// Returns 'true' if the current view performs a perspective projection.
bool IsPerspectiveProjection()
{
#if defined(SHADERPASS) && (SHADERPASS != SHADERPASS_SHADOWS)
    return (unity_OrthoParams.w == 0);
#else
    // This is a generic solution.
    // However, using 'unity_OrthoParams' is better for cache locality.
    // TODO: set 'unity_OrthoParams' during the shadow pass.
    return UNITY_MATRIX_P[3][3] == 0;
#endif
}

// Computes the world space view direction (pointing towards the viewer).
float3 GetWorldSpaceViewDir(float3 positionRWS)
{
    if (IsPerspectiveProjection())
    {
        // Perspective
        return GetCurrentViewPosition() - positionRWS;
    }
    else
    {
        // Orthographic
        return -GetViewForwardDir();
    }
}

float3 GetWorldSpaceNormalizeViewDir(float3 positionRWS)
{
    return normalize(GetWorldSpaceViewDir(positionRWS));
}

// UNITY_MATRIX_V defines a right-handed view space with the Z axis pointing towards the viewer.
// This function reverses the direction of the Z axis (so that it points forward),
// making the view space coordinate system left-handed.
void GetLeftHandedViewSpaceMatrices(out float4x4 viewMatrix, out float4x4 projMatrix)
{
    viewMatrix = UNITY_MATRIX_V;
    viewMatrix._31_32_33_34 = -viewMatrix._31_32_33_34;

    projMatrix = UNITY_MATRIX_P;
    projMatrix._13_23_33_43 = -projMatrix._13_23_33_43;
}

// This method should be used for rendering any full screen quad that uses an auto-scaling Render Targets (see RTHandle/HDCamera)
// It will account for the fact that the textures it samples are not necesarry using the full space of the render texture but only a partial viewport.
float2 GetNormalizedFullScreenTriangleTexCoord(uint vertexID)
{
    return GetFullScreenTriangleTexCoord(vertexID) * _RTHandleScale.xy;
}

float4 SampleSkyTexture(float3 texCoord, float lod, int sliceIndex)
{
    return SAMPLE_TEXTURECUBE_ARRAY_LOD(_SkyTexture, s_trilinear_clamp_sampler, texCoord, sliceIndex, lod);
}

// This function assumes the bitangent flip is encoded in tangentWS.w
float3x3 BuildTangentToWorld(float4 tangentWS, float3 normalWS)
{
    // tangentWS must not be normalized (mikkts requirement)

    // Normalize normalWS vector but keep the renormFactor to apply it to bitangent and tangent
    float3 unnormalizedNormalWS = normalWS;
    float renormFactor = 1.0 / max(FLT_MIN, length(unnormalizedNormalWS));

    // bitangent on the fly option in xnormal to reduce vertex shader outputs.
    // this is the mikktspace transformation (must use unnormalized attributes)
    float3x3 tangentToWorld = CreateTangentToWorld(unnormalizedNormalWS, tangentWS.xyz, tangentWS.w > 0.0 ? 1.0 : -1.0);

    // surface gradient based formulation requires a unit length initial normal. We can maintain compliance with mikkts
    // by uniformly scaling all 3 vectors since normalization of the perturbed normal will cancel it.
    tangentToWorld[0] = tangentToWorld[0] * renormFactor;
    tangentToWorld[1] = tangentToWorld[1] * renormFactor;
    tangentToWorld[2] = tangentToWorld[2] * renormFactor;		// normalizes the interpolated vertex normal

    return tangentToWorld;
}

// Transforms normal from object to world space
float3 TransformPreviousObjectToWorldNormal(float3 normalOS)
{
#ifdef UNITY_ASSUME_UNIFORM_SCALING
    return normalize(mul((float3x3)unity_MatrixPreviousM, normalOS));
#else
    // Normal need to be multiply by inverse transpose
    return normalize(mul(normalOS, (float3x3)unity_MatrixPreviousMI));
#endif
}

// Transforms local position to camera relative world space
float3 TransformPreviousObjectToWorld(float3 positionOS)
{
    float4x4 previousModelMatrix = ApplyCameraTranslationToMatrix(unity_MatrixPreviousM);
    return mul(previousModelMatrix, float4(positionOS, 1.0)).xyz;
}


// ----------------------------------------------------------------------------
// Scalarization helper functions.
// These assume a scalarization of a list of elements as described in https://flashypixels.wordpress.com/2018/11/10/intro-to-gpu-scalarization-part-2-scalarize-all-the-lights/

bool IsFastPath(uint lightStart, out uint lightStartLane0)
{
#ifdef PLATFORM_SUPPORTS_WAVE_INTRINSICS
    // Fast path is when we all pixels in a wave are accessing same tile or cluster.
    lightStartLane0 = WaveReadLaneFirst(lightStart);
    return WaveActiveAllTrue(lightStart == lightStartLane0);
#else
    lightStartLane0 = lightStart;
    return false;
#endif
}

// This function scalarize an index accross all lanes. To be effecient it must be used in the context
// of the scalarization of a loop. It is to use with IsFastPath so it can optimize the number of
// element to load, which is optimal when all the lanes are contained into a tile.
// Please note that if PLATFORM_SUPPORTS_WAVE_INTRINSICS is not defined, this will *not* scalarize the index.
uint ScalarizeElementIndex(uint v_elementIdx, bool fastPath)
{
    uint s_elementIdx = v_elementIdx;
#ifdef PLATFORM_SUPPORTS_WAVE_INTRINSICS
    if (!fastPath)
    {
        // If we are not in fast path, v_elementIdx is not scalar, so we need to query the Min value across the wave.
        s_elementIdx = WaveActiveMin(v_elementIdx);
        // If WaveActiveMin returns 0xffffffff it means that all lanes are actually dead, so we can safely ignore the loop and move forward.
        // This could happen as an helper lane could reach this point, hence having a valid v_elementIdx, but their values will be ignored by the WaveActiveMin
        if (s_elementIdx == -1)
        {
            return -1;
        }
    }
    // Note that the WaveReadLaneFirst should not be needed, but the compiler might insist in putting the result in VGPR.
    // However, we are certain at this point that the index is scalar.
    s_elementIdx = WaveReadLaneFirst(s_elementIdx);
#endif
    return s_elementIdx;
}

//-----------------------------------------------------------------------------
// LoD Fade
//-----------------------------------------------------------------------------

// Helper for LODDitheringTransition.
uint2 ComputeFadeMaskSeed(float3 V, uint2 positionSS)
{
    uint2 fadeMaskSeed;

    if (IsPerspectiveProjection())
    {
        // Start with the world-space direction V. It is independent from the orientation of the camera,
        // and only depends on the position of the camera and the position of the fragment.
        // Now, project and transform it into [-1, 1].
        float2 pv = PackNormalOctQuadEncode(V);
        // Rescale it to account for the resolution of the screen.
        pv *= _ScreenSize.xy;
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

#endif // UNITY_SHADER_VARIABLES_FUNCTIONS_INCLUDED
