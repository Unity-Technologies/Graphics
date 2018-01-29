#if SHADERPASS != SHADERPASS_VELOCITY
#error SHADERPASS_is_not_correctly_define
#endif

// Available semantic start from TEXCOORD4
struct AttributesPass
{
    float3 previousPositionOS : TEXCOORD4; // Contain previous transform position (in case of skinning for example)
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

// Same as ToPS here
#define VaryingsPassToDS VaryingsPassToPS
#define PackedVaryingsPassToDS PackedVaryingsPassToPS
#define PackVaryingsPassToDS PackVaryingsPassToPS
#define UnpackVaryingsPassToDS UnpackVaryingsPassToPS

VaryingsPassToDS InterpolateWithBaryCoordsPassToDS(VaryingsPassToDS input0, VaryingsPassToDS input1, VaryingsPassToDS input2, float3 baryCoords)
{
    VaryingsPassToDS output;

    TESSELLATION_INTERPOLATE_BARY(positionCS, baryCoords);
    TESSELLATION_INTERPOLATE_BARY(previousPositionCS, baryCoords);

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
#include "VertMesh.hlsl"

// Transforms normal from object to world space
float3 TransformPreviousObjectToWorldNormal(float3 normalOS)
{
#ifdef UNITY_ASSUME_UNIFORM_SCALING
    return normalize(mul((float3x3)unity_MatrixPreviousM, normalOS));
#else
    // Normal need to be multiply by inverse transpose
    // mul(IT_M, norm) => mul(norm, I_M) => {dot(norm, I_M.col0), dot(norm, I_M.col1), dot(norm, I_M.col2)}
    return normalize(mul(normalOS, (float3x3)unity_MatrixPreviousMI));
#endif
}

void VelocityPositionZBias(VaryingsToPS input)
{
#if defined(UNITY_REVERSED_Z)
	input.vmesh.positionCS.z -= unity_MotionVectorsParams.z * input.vmesh.positionCS.w;
#else
	input.vmesh.positionCS.z += unity_MotionVectorsParams.z * input.vmesh.positionCS.w;
#endif
}

PackedVaryingsType Vert(AttributesMesh inputMesh,
                        AttributesPass inputPass)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = VertMesh(inputMesh);

#if !defined(TESSELLATION_ON)
	VelocityPositionZBias(varyingsType);
#endif

    // It is not possible to correctly generate the motion vector for tesselated geometry as tessellation parameters can change
    // from one frame to another (adaptative, lod) + in Unity we only receive information for one non tesselated vertex.
    // So motion vetor will be based on interpolate previous position at vertex level instead.
	varyingsType.vpass.positionCS = mul(_NonJitteredViewProjMatrix, float4(varyingsType.vmesh.positionWS, 1.0));

    // Note: unity_MotionVectorsParams.y is 0 is forceNoMotion is enabled
    bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;
    if (forceNoMotion)
    {
        varyingsType.vpass.previousPositionCS = float4(0.0, 0.0, 0.0, 1.0);
    }
    else
    {
        bool hasDeformation = unity_MotionVectorsParams.x > 0.0; // Skin or morph target
        //Need to apply any vertex animation to the previous worldspace position, if we want it to show up in the velocity buffer
	    float3 previousPositionWS = mul(unity_MatrixPreviousM, hasDeformation ? float4(inputPass.previousPositionOS, 1.0) : float4(inputMesh.positionOS, 1.0)).xyz;
#ifdef ATTRIBUTES_NEED_NORMAL
		float3 normalWS = TransformPreviousObjectToWorldNormal(inputMesh.normalOS);
#else
        float3 normalWS = float3(0.0, 0.0, 0.0);
#endif

 #if defined(HAVE_VERTEX_MODIFICATION)
		ApplyVertexModification(inputMesh, normalWS, previousPositionWS, _LastTime);
#endif

	    //Need this since we are using the current position from VertMesh()
	    previousPositionWS = GetCameraRelativePositionWS(previousPositionWS);

	    varyingsType.vpass.previousPositionCS = mul(_PrevViewProjMatrix, float4(previousPositionWS, 1.0));
    }

    return PackVaryingsType(varyingsType);
}

#ifdef TESSELLATION_ON

PackedVaryingsToPS VertTesselation(VaryingsToDS input)
{
    VaryingsToPS output;

    output.vmesh = VertMeshTesselation(input.vmesh);

	VelocityPositionZBias(output);

    output.vpass.positionCS = input.vpass.positionCS;
    output.vpass.previousPositionCS = input.vpass.previousPositionCS;

    return PackVaryingsToPS(output);
}

#include "TessellationShare.hlsl"

#endif // TESSELLATION_ON

float4 Frag(PackedVaryingsToPS packedInput) : SV_Target
{
    // Note: unity_MotionVectorsParams.y is 0 is forceNoMotion is enabled
    bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;
    if (forceNoMotion)
        return float4(0.0, 0.0, 0.0, 0.0);

    FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionWS);

#ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);
#else
    float3 V = 0; // Avoid the division by 0
#endif

    // Perform alpha testing + get velocity
    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    VaryingsPassToPS inputPass = UnpackVaryingsPassToPS(packedInput.vpass);
#ifdef _DEPTHOFFSET_ON
    inputPass.positionCS.w += builtinData.depthOffset;
    inputPass.previousPositionCS.w += builtinData.depthOffset;
#endif

    // TODO: How to allow overriden velocity vector from GetSurfaceAndBuiltinData ?
    float2 velocity = CalculateVelocity(inputPass.positionCS, inputPass.previousPositionCS);

    float4 outBuffer;
    EncodeVelocity(velocity, outBuffer);
    return outBuffer;
}
