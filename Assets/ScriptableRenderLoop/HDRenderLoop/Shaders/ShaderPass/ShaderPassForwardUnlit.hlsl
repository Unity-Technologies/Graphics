#if SHADERPASS != SHADERPASS_FORWARD_UNLIT
#error SHADERPASS_is_not_correctly_define
#endif

#if SHADER_STAGE_FRAGMENT

float4 Frag(PackedVaryings packedInput) : SV_Target
{
	Varyings input = UnpackVaryings(packedInput);
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);

	SurfaceData surfaceData;
	BuiltinData builtinData;
	GetSurfaceAndBuiltinData(V, input, surfaceData, builtinData);
	
	// Not lit here (but emissive is allowed)

	BSDFData bsdfData = ConvertSurfaceDataToBSDFData(surfaceData);
		
	// TODO: we must not access bsdfData here, it break the genericity of the code!
	return float4(bsdfData.color, builtinData.opacity);
}

#endif
