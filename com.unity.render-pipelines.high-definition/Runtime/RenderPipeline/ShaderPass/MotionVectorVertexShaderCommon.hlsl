#ifndef MOTION_VEC_VERTEX_COMMON_INCLUDED
#define MOTION_VEC_VERTEX_COMMON_INCLUDED

// Available semantic start from TEXCOORD4
struct AttributesPass
{
    float3 previousPositionOS : TEXCOORD4; // Contain previous transform position (in case of skinning for example)
#if defined (_ADD_PRECOMPUTED_VELOCITY)
    float3 precomputedVelocity    : TEXCOORD5; // Add Precomputed Velocity (Alembic computes velocities on runtime side).
#endif
};

struct VaryingsPassToPS
{
    // Note: Z component is not use currently
    // This is the clip space position. Warning, do not confuse with the value of positionCS in PackedVarying which is SV_POSITION and store in positionSS
    float4 positionCS;
    float4 previousPositionCS;
};

// Available interpolator start from TEXCOORD8
struct PackedVaryingsPassToPS
{
    // Note: Z component is not use
    float3 interpolators0 : TEXCOORD8;
    float3 interpolators1 : TEXCOORD9;
};

PackedVaryingsPassToPS PackVaryingsPassToPS(VaryingsPassToPS input)
{
    PackedVaryingsPassToPS output;
    output.interpolators0 = float3(input.positionCS.xyw);
    output.interpolators1 = float3(input.previousPositionCS.xyw);

    return output;
}

VaryingsPassToPS UnpackVaryingsPassToPS(PackedVaryingsPassToPS input)
{
    VaryingsPassToPS output;
    output.positionCS = float4(input.interpolators0.xy, 0.0, input.interpolators0.z);
    output.previousPositionCS = float4(input.interpolators1.xy, 0.0, input.interpolators1.z);

    return output;
}

#ifdef TESSELLATION_ON

// Available interpolator start from TEXCOORD4

struct VaryingsPassToDS
{
    // For Tessellation we currently keep previous world position to only project in the last step to clip space
    // No need to keep world position as we will recompute it from the VaryingMesh struct
    float3 previousPositionRWS;
};

// Available interpolator start from TEXCOORD8
struct PackedVaryingsPassToDS
{
    float3 interpolators0 : TEXCOORD8;
};

PackedVaryingsPassToDS PackVaryingsPassToDS(VaryingsPassToDS input)
{
    PackedVaryingsPassToDS output;
    output.interpolators0 = input.previousPositionRWS;

    return output;
}

VaryingsPassToDS UnpackVaryingsPassToDS(PackedVaryingsPassToDS input)
{
    VaryingsPassToDS output;
    output.previousPositionRWS = input.interpolators0;

    return output;
}

VaryingsPassToDS InterpolateWithBaryCoordsPassToDS(VaryingsPassToDS input0, VaryingsPassToDS input1, VaryingsPassToDS input2, float3 baryCoords)
{
    VaryingsPassToDS output;

    TESSELLATION_INTERPOLATE_BARY(previousPositionRWS, baryCoords);

    return output;
}

#endif // TESSELLATION_ON

#ifdef TESSELLATION_ON
#define VaryingsPassType VaryingsPassToDS
#else
#define VaryingsPassType VaryingsPassToPS
#endif

// We will use custom attributes for this pass
#define VARYINGS_NEED_PASS
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

void MotionVectorPositionZBias(VaryingsToPS input)
{
#if UNITY_REVERSED_Z
    input.vmesh.positionCS.z -= unity_MotionVectorsParams.z * input.vmesh.positionCS.w;
#else
    input.vmesh.positionCS.z += unity_MotionVectorsParams.z * input.vmesh.positionCS.w;
#endif
}

PackedVaryingsType MotionVectorVS(VaryingsType varyingsType, AttributesMesh inputMesh, AttributesPass inputPass
#ifdef HAVE_VFX_MODIFICATION
    , AttributesElement inputElement
#endif
)
{
    // With tessellation we will do following processing after tessellation modification
#ifndef TESSELLATION_ON
    MotionVectorPositionZBias(varyingsType);

    // Use unjiterred matrix for motion vector
    varyingsType.vpass.positionCS = mul(UNITY_MATRIX_UNJITTERED_VP, float4(varyingsType.vmesh.positionRWS, 1.0));
#endif // TESSELLATION_ON

    // Note: unity_MotionVectorsParams.y is 0 is forceNoMotion is enabled
    bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;

    //Motion vector is enabled in SG but not active in VFX
#if defined(HAVE_VFX_MODIFICATION) && !VFX_FEATURE_MOTION_VECTORS
    forceNoMotion = true;
#endif

    if (forceNoMotion)
    {
#ifdef TESSELLATION_ON
        // Dummy value, will not be used
        varyingsType.vpass.previousPositionRWS = float3(0.0, 0.0, 0.0);
#else
        varyingsType.vpass.previousPositionCS = float4(0.0, 0.0, 0.0, 1.0);
#endif
    }
    else
    {
        bool previousPositionCSComputed = false;
        float3 effectivePositionOS = (float3)0.0f;
        float3 previousPositionRWS = (float3)0.0f;

#if defined(HAVE_VFX_MODIFICATION)
        GetMeshAndElementIndex(inputMesh, inputElement);
        effectivePositionOS = inputMesh.positionOS; //no skin or morph target in vfx
#else
        bool hasDeformation = unity_MotionVectorsParams.x > 0.0; // Skin or morph target
        effectivePositionOS = (hasDeformation ? inputPass.previousPositionOS : inputMesh.positionOS);
#endif

        // See _TransparentCameraOnlyMotionVectors in HDCamera.cs
#ifdef _WRITE_TRANSPARENT_MOTION_VECTOR
        if (_TransparentCameraOnlyMotionVectors > 0)
        {
            previousPositionRWS = varyingsType.vmesh.positionRWS.xyz;
#ifndef TESSELLATION_ON
            varyingsType.vpass.previousPositionCS = mul(UNITY_MATRIX_PREV_VP, float4(previousPositionRWS, 1.0));
#endif
            previousPositionCSComputed = true;
        }
#endif

#if defined(VFX_FEATURE_MOTION_VECTORS_VERTS)
#if defined(HAVE_VERTEX_MODIFICATION) || defined(_ADD_CUSTOM_VELOCITY) || defined(TESSELLATION_ON) || defined(_ADD_PRECOMPUTED_VELOCITY)
#error Unexpected fast path rendering VFX motion vector while there are vertex modification afterwards.
#endif
        if (!previousPositionCSComputed)
        {
            // previousPositionRWS is only needed for TESSELLATION_ON
            varyingsType.vpass.previousPositionCS = VFXGetPreviousClipPosition(inputMesh, inputElement);
            previousPositionCSComputed = true;
        }
#endif

        if (!previousPositionCSComputed)
        {
            // Need to apply any vertex animation to the previous worldspace position, if we want it to show up in the motion vector buffer
#if defined(HAVE_MESH_MODIFICATION)
            AttributesMesh previousMesh = inputMesh;
            previousMesh.positionOS = effectivePositionOS;

            previousMesh = ApplyMeshModification(previousMesh, _LastTimeParameters.xyz
#ifdef USE_CUSTOMINTERP_SUBSTRUCT
                , varyingsType.vmesh
#endif
#ifdef HAVE_VFX_MODIFICATION
                , inputElement
#endif
            );

#if defined(HAVE_VFX_MODIFICATION)
            // Only handle the VFX case here since it is only used with ShaderGraph (and ShaderGraph always has mesh modification enabled).
            previousMesh = VFXTransformMeshToPreviousElement(previousMesh, inputElement);
#endif

#if defined(_ADD_CUSTOM_VELOCITY) // For shader graph custom velocity
            // Note that to fetch custom velocity here we must use the inputMesh and not the previousMesh
            // in the case the custom velocity depends on the positionOS
            // otherwise it will apply two times the modifications.
            // However the vertex animation will still be perform correctly as we used previousMesh position
            // where we could ahve trouble is if time is used to drive custom velocity, this will not work
            previousMesh.positionOS -= GetCustomVelocity(inputMesh
#ifdef HAVE_VFX_MODIFICATION
                , inputElement
#endif
            );
#endif

#if defined(_ADD_PRECOMPUTED_VELOCITY)
            previousMesh.positionOS -= inputPass.precomputedVelocity;
#endif

            previousPositionRWS = TransformPreviousObjectToWorld(previousMesh.positionOS);
#else

#if defined(_ADD_CUSTOM_VELOCITY) // For shader graph custom velocity
            effectivePositionOS -= GetCustomVelocity(inputMesh
#ifdef HAVE_VFX_MODIFICATION
                , inputElement
#endif
            );
#endif

#if defined(_ADD_PRECOMPUTED_VELOCITY)
            effectivePositionOS -= inputPass.precomputedVelocity;
#endif

            previousPositionRWS = TransformPreviousObjectToWorld(effectivePositionOS);
#endif

#ifdef ATTRIBUTES_NEED_NORMAL
            float3 normalWS = TransformPreviousObjectToWorldNormal(inputMesh.normalOS);
#else
            float3 normalWS = float3(0.0, 0.0, 0.0);
#endif

#if defined(HAVE_VERTEX_MODIFICATION)
            ApplyVertexModification(inputMesh, normalWS, previousPositionRWS, _LastTimeParameters.xyz);
#endif
        }

#ifdef TESSELLATION_ON
        // With tessellation we will apply the tessellation modification on top of previousPositionRWS
        // so don't convert to CS yet.
        varyingsType.vpass.previousPositionRWS = previousPositionRWS;
#else
        // Final computation from previousPositionRWS (if not already done)
        if (!previousPositionCSComputed)
        {
            varyingsType.vpass.previousPositionCS = mul(UNITY_MATRIX_PREV_VP, float4(previousPositionRWS, 1.0));
        }
#endif
    }

    return PackVaryingsType(varyingsType);
}

#if defined(TESSELLATION_ON)

PackedVaryingsToPS MotionVectorTessellation(VaryingsToPS output, VaryingsToDS input)
{
    MotionVectorPositionZBias(output);

    // Use unjittered matrix for motion vector
    output.vpass.positionCS = mul(UNITY_MATRIX_UNJITTERED_VP, float4(input.vmesh.positionRWS, 1.0));

    // It is not possible to correctly generate the motion vector for tessellated geometry as tessellation parameters can change
    // from one frame to another (adaptative, lod) but still better than doing nothing, so we calculate the previous position with
    // current frame tessellation parameters

    // Note: unity_MotionVectorsParams.y is 0 is forceNoMotion is enabled
    bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;

    //Motion vector is enabled in SG but not active in VFX
#if defined(HAVE_VFX_MODIFICATION) && !VFX_FEATURE_MOTION_VECTORS
    forceNoMotion = true;
#endif

    if (forceNoMotion)
    {
        output.vpass.previousPositionCS = float4(0.0, 0.0, 0.0, 1.0);
    }
    else
    {
        float3 previousPositionRWS = input.vmesh.positionRWS.xyz;

        // Need to apply any tessellation animation to the previous worldspace position, if we want it to show up in the motion vector buffer
#if defined(HAVE_TESSELLATION_MODIFICATION)
    #ifdef _WRITE_TRANSPARENT_MOTION_VECTOR
        if (_TransparentCameraOnlyMotionVectors == 0)
        {
    #endif
            VaryingsMeshToDS previousMesh = input.vmesh;
            previousMesh.positionRWS.xyz = input.vpass.previousPositionRWS;
            previousMesh = ApplyTessellationModification(previousMesh, _LastTimeParameters.xyz);
            previousPositionRWS = previousMesh.positionRWS.xyz;
    #ifdef _WRITE_TRANSPARENT_MOTION_VECTOR
        }
    #endif
#endif

        output.vpass.previousPositionCS = mul(UNITY_MATRIX_PREV_VP, float4(previousPositionRWS, 1.0));
    }

    return PackVaryingsToPS(output);
}

#endif


#endif
