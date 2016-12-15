#if SHADERPASS != SHADERPASS_FORWARD_UNLIT
#error SHADERPASS_is_not_correctly_define
#endif

float4 Frag(PackedVaryings packedInput) : SV_Target
{
    FragInputs input = UnpackVaryings(packedInput);

    // input.unPositionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.unPositionSS.xy, _ScreenSize.zw);
    UpdatePositionInput(input.unPositionSS.z, input.unPositionSS.w, input.positionWS, posInput);
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);

	SurfaceData surfaceData;
	BuiltinData builtinData;
	GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);
	
	// Not lit here (but emissive is allowed)

	BSDFData bsdfData = ConvertSurfaceDataToBSDFData(surfaceData);
		
	// TODO: we must not access bsdfData here, it break the genericity of the code!
    return float4(bsdfData.color + builtinData.emissiveColor, builtinData.opacity);
}

