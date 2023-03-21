#ifndef SG_MOTION_VECTORS_PASS_INCLUDED
#define SG_MOTION_VECTORS_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MotionVectorsCommon.hlsl"

struct MotionVectorPassAttributes
{
    float3 previousPositionOS  : TEXCOORD4; // Contains previous frame local vertex position (for skinned meshes)
};

// Note: these will have z == 0.0f in the pixel shader to save on bandwidth
struct MotionVectorPassVaryings
{
    float4 positionCSNoJitter;
    float4 previousPositionCSNoJitter;
};

struct PackedMotionVectorPassVaryings
{
    float3 positionCSNoJitter         : CLIP_POSITION_NO_JITTER;
    float3 previousPositionCSNoJitter : PREVIOUS_CLIP_POSITION_NO_JITTER;
};

PackedMotionVectorPassVaryings PackMotionVectorVaryings(MotionVectorPassVaryings regularVaryings)
{
    PackedMotionVectorPassVaryings packedVaryings;
    packedVaryings.positionCSNoJitter = regularVaryings.positionCSNoJitter.xyw;
    packedVaryings.previousPositionCSNoJitter = regularVaryings.previousPositionCSNoJitter.xyw;
    return packedVaryings;
}

MotionVectorPassVaryings UnpackMotionVectorVaryings(PackedMotionVectorPassVaryings packedVaryings)
{
    MotionVectorPassVaryings regularVaryings;
    regularVaryings.positionCSNoJitter = float4(packedVaryings.positionCSNoJitter.xy, 0, packedVaryings.positionCSNoJitter.z);
    regularVaryings.previousPositionCSNoJitter = float4(packedVaryings.previousPositionCSNoJitter.xy, 0, packedVaryings.previousPositionCSNoJitter.z);
    return regularVaryings;
}

// -------------------------------------
// Vertex
void vert(
    Attributes input,
    MotionVectorPassAttributes passInput,
    out PackedMotionVectorPassVaryings packedMvOutput,
    out PackedVaryings packedOutput)
{
    Varyings output = (Varyings)0;
    MotionVectorPassVaryings mvOutput = (MotionVectorPassVaryings)0;
    output = BuildVaryings(input);
    ApplyMotionVectorZBias(output.positionCS);
    packedOutput = PackVaryings(output);

    const bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;
    if(!forceNoMotion)
    {
        const bool hasDeformation = unity_MotionVectorsParams.x == 1; // Mesh has skinned deformation
        float3 previousPositionOS = hasDeformation ? passInput.previousPositionOS : input.positionOS;

        mvOutput.positionCSNoJitter = mul(_NonJitteredViewProjMatrix, mul(UNITY_MATRIX_M, float4(input.positionOS, 1.0f)));
        mvOutput.previousPositionCSNoJitter = mul(_PrevViewProjMatrix, mul(UNITY_PREV_MATRIX_M, float4(previousPositionOS, 1.0f)));
    }

    packedMvOutput = PackMotionVectorVaryings(mvOutput);
}

// -------------------------------------
// Fragment
float4 frag(
    // Note: packedMvInput needs to be before packedInput as otherwise we get the following error in the speed tree 8 SG:
    // "Non system-generated input signature parameter () cannot appear after a system generated value"
    PackedMotionVectorPassVaryings packedMvInput,
    PackedVaryings packedInput) : SV_Target
{
    Varyings input = UnpackVaryings(packedInput);
    MotionVectorPassVaryings mvInput = UnpackMotionVectorVaryings(packedMvInput);
    UNITY_SETUP_INSTANCE_ID(input);
    SurfaceDescription surfaceDescription = BuildSurfaceDescription(input);

#if _ALPHATEST_ON
    clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
#endif

#if defined(LOD_FADE_CROSSFADE) && USE_UNITY_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    return float4(CalcNdcMotionVectorFromCsPositions(mvInput.positionCSNoJitter, mvInput.previousPositionCSNoJitter), 0, 0);
}
#endif
