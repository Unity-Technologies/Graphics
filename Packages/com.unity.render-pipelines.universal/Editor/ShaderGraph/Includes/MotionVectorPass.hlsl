#ifndef SG_MOTION_VECTORS_PASS_INCLUDED
#define SG_MOTION_VECTORS_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MotionVectorsCommon.hlsl"

struct MotionVectorPassAttributes
{
    float3 previousPositionOS  : TEXCOORD4; // Contains previous frame local vertex position (for skinned meshes)
#if defined (_ADD_PRECOMPUTED_VELOCITY)
    float3 alembicMotionVectorOS : TEXCOORD5; // Alembic precomputed object space motion vector (offset from last frame's position)
#endif
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

float3 GetLastFrameDeformedPosition(Attributes input, MotionVectorPassOutput currentFrameMvData, float3 previousPositionOS)
{
    Attributes lastFrameInputAttributes = input;
    lastFrameInputAttributes.positionOS = previousPositionOS;

    VertexDescriptionInputs lastFrameVertexDescriptionInputs = BuildVertexDescriptionInputs(lastFrameInputAttributes);
#if defined(AUTOMATIC_TIME_BASED_MOTION_VECTORS) && defined(GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT)
    lastFrameVertexDescriptionInputs.TimeParameters = _LastTimeParameters.xyz;
#endif

    VertexDescription lastFrameVertexDescription = VertexDescriptionFunction(lastFrameVertexDescriptionInputs
#if defined(HAVE_VFX_MODIFICATION)
        , currentFrameMvData.vfxGraphProperties
#endif
    );

#if defined(HAVE_VFX_MODIFICATION)
    lastFrameInputAttributes.positionOS = lastFrameVertexDescription.Position.xyz;
    lastFrameInputAttributes = VFXTransformMeshToPreviousElement(lastFrameInputAttributes, currentFrameMvData.vfxElementAttributes);
    previousPositionOS = lastFrameInputAttributes.positionOS;
#else
    previousPositionOS = lastFrameVertexDescription.Position.xyz;
#endif
    return previousPositionOS;
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
    MotionVectorPassOutput currentFrameMvData = (MotionVectorPassOutput)0;
    output = BuildVaryings(input, currentFrameMvData);
    packedOutput = PackVaryings(output);

#if defined(HAVE_VFX_MODIFICATION) && !VFX_FEATURE_MOTION_VECTORS
    //Motion vector is enabled in SG but not active in VFX
    const bool forceNoMotion = true;
#else
    const bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;
#endif

    if (!forceNoMotion)
    {
#if defined(HAVE_VFX_MODIFICATION)
    float3 previousPositionOS = currentFrameMvData.vfxParticlePositionOS;
    #if defined(VFX_FEATURE_MOTION_VECTORS_VERTS)
        const bool applyDeformation = false;
    #else
        const bool applyDeformation = true;
    #endif
#else
    const bool hasDeformation = unity_MotionVectorsParams.x == 1; // Mesh has skinned deformation
    float3 previousPositionOS = hasDeformation ? passInput.previousPositionOS : input.positionOS;

    #if defined(AUTOMATIC_TIME_BASED_MOTION_VECTORS) && defined(GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT)
        const bool applyDeformation = true;
    #else
        const bool applyDeformation = hasDeformation;
    #endif
#endif

#if defined(FEATURES_GRAPH_VERTEX)
    if (applyDeformation)
        previousPositionOS = GetLastFrameDeformedPosition(input, currentFrameMvData, previousPositionOS);
    else
        previousPositionOS = currentFrameMvData.positionOS;

    #if defined(FEATURES_GRAPH_VERTEX_MOTION_VECTOR_OUTPUT)
        previousPositionOS -= currentFrameMvData.motionVector;
    #endif
#endif

#if defined(UNITY_DOTS_INSTANCING_ENABLED) && defined(DOTS_DEFORMED)
    // Deformed vertices in DOTS are not cumulative with built-in Unity skinning/blend shapes
    // Needs to be called after vertex modification has been applied otherwise it will be
    // overwritten by Compute Deform node
    ApplyPreviousFrameDeformedVertexPosition(input.vertexID, previousPositionOS);
#endif
        
#if defined (_ADD_PRECOMPUTED_VELOCITY)
        previousPositionOS -= passInput.alembicMotionVectorOS;
#endif

#if defined(APPLICATION_SPACE_WARP_MOTION)
        // We do not need jittered position in ASW
        mvOutput.positionCSNoJitter = mul(_NonJitteredViewProjMatrix, float4(currentFrameMvData.positionWS, 1.0f));
        packedOutput.positionCS = mvOutput.positionCSNoJitter;
        mvOutput.previousPositionCSNoJitter = mul(_PrevViewProjMatrix, mul(UNITY_PREV_MATRIX_M, float4(previousPositionOS, 1.0f)));
#else
        mvOutput.positionCSNoJitter = mul(_NonJitteredViewProjMatrix, float4(currentFrameMvData.positionWS, 1.0f));

    #if defined(HAVE_VFX_MODIFICATION)
        #if defined(VFX_FEATURE_MOTION_VECTORS_VERTS)
            #if defined(FEATURES_GRAPH_VERTEX_MOTION_VECTOR_OUTPUT) || defined(_ADD_PRECOMPUTED_VELOCITY)
                #error Unexpected fast path rendering VFX motion vector while there are vertex modification afterwards.
            #endif
            mvOutput.previousPositionCSNoJitter = VFXGetPreviousClipPosition(input, currentFrameMvData.vfxElementAttributes, mvOutput.positionCSNoJitter);
        #else
            #if VFX_WORLD_SPACE
                //previousPositionOS is already in world space
                const float3 previousPositionWS = previousPositionOS;
            #else
                const float3 previousPositionWS = mul(UNITY_PREV_MATRIX_M, float4(previousPositionOS, 1.0f)).xyz;
            #endif
            mvOutput.previousPositionCSNoJitter = mul(_PrevViewProjMatrix, float4(previousPositionWS, 1.0f));
        #endif
    #else
            mvOutput.previousPositionCSNoJitter = mul(_PrevViewProjMatrix, mul(UNITY_PREV_MATRIX_M, float4(previousPositionOS, 1.0f)));
    #endif
#endif
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

#if defined(_ALPHATEST_ON)
    clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
#endif

#if defined(LOD_FADE_CROSSFADE) && USE_UNITY_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif


#if defined(APPLICATION_SPACE_WARP_MOTION)
    return float4(CalcAswNdcMotionVectorFromCsPositions(mvInput.positionCSNoJitter, mvInput.previousPositionCSNoJitter), 1);
#else
    return float4(CalcNdcMotionVectorFromCsPositions(mvInput.positionCSNoJitter, mvInput.previousPositionCSNoJitter), 0, 0);
#endif
}
#endif
