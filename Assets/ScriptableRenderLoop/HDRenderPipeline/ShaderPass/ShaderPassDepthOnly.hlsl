#if SHADERPASS != SHADERPASS_DEPTH_ONLY
#error SHADERPASS_is_not_correctly_define
#endif

void Frag(  PackedVaryings packedInput,
            out float4 outColor : SV_Target
            #ifdef _DEPTHOFFSET_ON
            , out float outputDepth : SV_Depth
            #endif
        )
{
    FragInputs input = UnpackVaryings(packedInput);

    // input.unPositionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.unPositionSS.xy, _ScreenSize.zw);
    UpdatePositionInput(input.unPositionSS.z, input.unPositionSS.w, input.positionWS, posInput);
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    // TODO: handle cubemap shadow
    outColor = float4(0.0, 0.0, 0.0, 0.0);

#ifdef _DEPTHOFFSET_ON
    outputDepth = posInput.rawDepth;
#endif
}

