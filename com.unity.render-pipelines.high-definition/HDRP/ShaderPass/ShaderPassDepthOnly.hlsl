#if (SHADERPASS != SHADERPASS_DEPTH_ONLY && SHADERPASS != SHADERPASS_SHADOWS)
#error SHADERPASS_is_not_correctly_define
#endif

#include "VertMesh.hlsl"

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = VertMesh(inputMesh);
    return PackVaryingsType(varyingsType);
}

#ifdef TESSELLATION_ON

PackedVaryingsToPS VertTesselation(VaryingsToDS input)
{
    VaryingsToPS output;
    output.vmesh = VertMeshTesselation(input.vmesh);
    return PackVaryingsToPS(output);
}

#include "TessellationShare.hlsl"

#endif // TESSELLATION_ON

void Frag(  PackedVaryingsToPS packedInput,
            #ifdef WRITE_NORMAL_BUFFER
            OUTPUT_NORMALBUFFER(outNormalBuffer)
            #else
            out float4 outColor : SV_Target
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
    float3 V = 0; // Avoid the division by 0
#endif

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

#ifdef _DEPTHOFFSET_ON
    outputDepth = posInput.deviceDepth;
#endif

#ifdef WRITE_NORMAL_BUFFER
    ENCODE_INTO_NORMALBUFFER(surfaceData, posInput.positionSS, outNormalBuffer);
#elif defined(SCENESELECTIONPASS)
    // We use depth prepass for scene selection in the editor, this code allow to output the outline correctly
    outColor = float4(_ObjectId, _PassValue, 1.0, 1.0);
#else
    outColor = float4(0.0, 0.0, 0.0, 0.0);
#endif
}
