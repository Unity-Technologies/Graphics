#if SHADER_STAGE_FRAGMENT

void Frag(  PackedVaryings packedInput,
			OUTPUT_GBUFFER(outGBuffer)
			#ifdef VELOCITY_IN_GBUFFER
			, OUTPUT_GBUFFER_VELOCITY(outGBuffer)
			#endif
			, OUTPUT_GBUFFER_BAKE_LIGHTING(outGBuffer)
			)
{
	Varyings input = UnpackVaryings(packedInput);
	float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);
	float3 positionWS = input.positionWS;

	SurfaceData surfaceData;
	BuiltinData builtinData;
	GetSurfaceAndBuiltinData(input, surfaceData, builtinData);

	BSDFData bsdfData = ConvertSurfaceDataToBSDFData(surfaceData);
	Coordinate coord = GetCoordinate(input.positionHS.xy, _ScreenSize.zw);
	PreLightData preLightData = GetPreLightData(V, positionWS, coord, bsdfData);

	ENCODE_INTO_GBUFFER(surfaceData, outGBuffer);
	#ifdef VELOCITY_IN_GBUFFER
	ENCODE_VELOCITY_INTO_GBUFFER(builtinData.velocity, outGBuffer);
	#endif
	ENCODE_BAKE_LIGHTING_INTO_GBUFFER(GetBakedDiffuseLigthing(preLightData, surfaceData, builtinData, bsdfData), outGBuffer);
}

#endif