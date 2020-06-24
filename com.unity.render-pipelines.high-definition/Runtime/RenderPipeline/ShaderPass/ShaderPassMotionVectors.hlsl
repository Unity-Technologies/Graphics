#if SHADERPASS != SHADERPASS_MOTION_VECTORS
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/MotionVectorVertexShaderCommon.hlsl"

PackedVaryingsType Vert(AttributesMesh inputMesh,
                        AttributesPass inputPass)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = VertMesh(inputMesh);

    return MotionVectorVS(varyingsType, inputMesh, inputPass);
}

#ifdef TESSELLATION_ON

PackedVaryingsToPS VertTesselation(VaryingsToDS input)
{
    VaryingsToPS output;

    output.vmesh = VertMeshTesselation(input.vmesh);

    MotionVectorPositionZBias(output);

    output.vpass.positionCS = input.vpass.positionCS;
    output.vpass.previousPositionCS = input.vpass.previousPositionCS;

    return PackVaryingsToPS(output);
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/TessellationShare.hlsl"

#endif // TESSELLATION_ON

void Frag(  PackedVaryingsToPS packedInput
            #ifdef WRITE_MSAA_DEPTH
            // We need the depth color as SV_Target0 for alpha to coverage
            , out float4 depthColor : SV_Target0
            , out float4 outMotionVector : SV_Target1
                #ifdef WRITE_NORMAL_BUFFER
                , out float4 outNormalBuffer : SV_Target2
                #endif
            #else
            // When no MSAA, the motion vector is always the first buffer
            , out float4 outMotionVector : SV_Target0
                #ifdef WRITE_NORMAL_BUFFER
                , out float4 outNormalBuffer : SV_Target1
                #endif
            #endif

            #ifdef _DEPTHOFFSET_ON
            , out float outputDepth : SV_Depth
            #endif
        )
{
    FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

#ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
#else
    // Unused
    float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
#endif

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    VaryingsPassToPS inputPass = UnpackVaryingsPassToPS(packedInput.vpass);
#ifdef _DEPTHOFFSET_ON
    inputPass.positionCS.w += builtinData.depthOffset;
    inputPass.previousPositionCS.w += builtinData.depthOffset;
#endif

    // TODO: How to allow overriden motion vector from GetSurfaceAndBuiltinData ?
    float2 motionVector = CalculateMotionVector(inputPass.positionCS, inputPass.previousPositionCS);

    // Convert from Clip space (-1..1) to NDC 0..1 space.
    // Note it doesn't mean we don't have negative value, we store negative or positive offset in NDC space.
    // Note: ((positionCS * 0.5 + 0.5) - (previousPositionCS * 0.5 + 0.5)) = (motionVector * 0.5)
    EncodeMotionVector(motionVector * 0.5, outMotionVector);

    // Note: unity_MotionVectorsParams.y is 0 is forceNoMotion is enabled
    bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;

    // Setting the motionVector to a value more than 2 set as a flag for "force no motion". This is valid because, given that the velocities are in NDC,
    // a value of >1 can never happen naturally, unless explicitely set. 
    if (forceNoMotion)
        outMotionVector = float4(2.0, 0.0, 0.0, 0.0);

// Depth and Alpha to coverage
#ifdef WRITE_MSAA_DEPTH
    // In case we are rendering in MSAA, reading the an MSAA depth buffer is way too expensive. To avoid that, we export the depth to a color buffer
    depthColor = packedInput.vmesh.positionCS.z;

    #ifdef _ALPHATOMASK_ON
    // Alpha channel is used for alpha to coverage
    depthColor.a = SharpenAlpha(builtinData.opacity, builtinData.alphaClipTreshold);
    #endif
#endif

// Normal Buffer Processing
#ifdef WRITE_NORMAL_BUFFER
    EncodeIntoNormalBuffer(ConvertSurfaceDataToNormalData(surfaceData), posInput.positionSS, outNormalBuffer);
#endif

#ifdef _DEPTHOFFSET_ON
    outputDepth = posInput.deviceDepth;
#endif
}
