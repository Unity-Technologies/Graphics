#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

struct PickingAttributesMesh
{
    float3 positionOS   : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct PickingMeshToDS
{
    float3 positionRWS : INTERNALTESSPOS;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct PickingMeshToPS
{
    float4 positionCS : SV_Position;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

#ifdef TESSELLATION_ON
#define PickingVertexOutput PickingMeshToDS
#else
#define PickingVertexOutput PickingMeshToPS
#endif

float4x4 _DOTSPickingViewMatrix;
float4x4 _DOTSPickingProjMatrix;
float4 _DOTSPickingCameraWorldPos;

#undef unity_ObjectToWorld
float4x4 LoadObjectToWorldMatrixDOTSPicking()
{
    float4x4 objectToWorld = LoadDOTSInstancedData_float4x4_from_float3x4(UNITY_DOTS_INSTANCED_METADATA_NAME(float3x4, unity_ObjectToWorld));
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    objectToWorld._m03_m13_m23 -= _DOTSPickingCameraWorldPos.xyz;
 #endif
    return objectToWorld;
}

float4 ComputePositionCS(float3 positionWS)
{
    float4x4 viewMatrix = _DOTSPickingViewMatrix;
    // HDRP expects no translation in the matrix because of camera relative rendering
    viewMatrix._m03_m13_m23_m33 = float4(0,0,0,1);
    return mul(_DOTSPickingProjMatrix, mul(viewMatrix, float4(positionWS, 1)));
}

PickingVertexOutput Vert(PickingAttributesMesh input)
{
    PickingVertexOutput output;
    ZERO_INITIALIZE(PickingVertexOutput, output);
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    float4x4 objectToWorld = LoadObjectToWorldMatrixDOTSPicking();
    float4 positionWS = mul(objectToWorld, float4(input.positionOS, 1.0));

#ifdef TESSELLATION_ON
    output.positionRWS = positionWS;
#else
    output.positionCS = ComputePositionCS(positionWS.xyz);
#endif

    return output;
}

// No-op Tessellation stage to make at least the shader compile correctly
#ifdef TESSELLATION_ON

#define MAX_TESSELLATION_FACTORS 1.0

struct PickingTessellationFactors
{
    float edge[3] : SV_TessFactor;
    float inside : SV_InsideTessFactor;
};

PickingTessellationFactors HullConstant(InputPatch<PickingMeshToDS, 3> input)
{
    PickingTessellationFactors output;
    output.edge[0] = 1;
    output.edge[1] = 1;
    output.edge[2] = 1;
    output.inside  = 1;

    return output;
}

[maxtessfactor(MAX_TESSELLATION_FACTORS)]
[domain("tri")]
[partitioning("fractional_odd")]
[outputtopology("triangle_cw")]
[patchconstantfunc("HullConstant")]
[outputcontrolpoints(3)]
PickingMeshToDS Hull(InputPatch<PickingMeshToDS, 3> input, uint id : SV_OutputControlPointID)
{
    return input[id];
}

PickingMeshToDS PickingInterpolateWithBaryCoordsMeshToDS(PickingMeshToDS input0, PickingMeshToDS input1, PickingMeshToDS input2, float3 baryCoords)
{
    PickingMeshToDS output;
    UNITY_TRANSFER_INSTANCE_ID(input0, output);
    output.positionRWS = input0.positionRWS * baryCoords.x +  input1.positionRWS * baryCoords.y +  input2.positionRWS * baryCoords.z;

    return output;
}

[domain("tri")]
PickingMeshToPS Domain(PickingTessellationFactors tessFactors, const OutputPatch<PickingMeshToDS, 3> inputs, float3 baryCoords : SV_DomainLocation)
{
    PickingMeshToDS input = PickingInterpolateWithBaryCoordsMeshToDS(inputs[0], inputs[1], inputs[2], baryCoords);
    UNITY_SETUP_INSTANCE_ID(input);

    PickingMeshToPS output;
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    output.positionCS = ComputePositionCS(input.positionRWS);

    return output;
}

#endif // TESSELLATION_ON

void Frag(PickingMeshToPS input, out float4 outColor : SV_Target0)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    UNITY_SETUP_INSTANCE_ID(input);

    outColor = _SelectionID;
}
