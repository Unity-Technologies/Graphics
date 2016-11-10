#if SHADERPASS != SHADERPASS_GBUFFER
#error SHADERPASS_is_not_correctly_define
#endif

void Frag(  PackedVaryings packedInput,
			OUTPUT_GBUFFER(outGBuffer),
			OUTPUT_GBUFFER_BAKE_LIGHTING(outGBuffer)
            #ifdef VELOCITY_IN_GBUFFER
            , OUTPUT_GBUFFER_VELOCITY(outGBuffer)
            #endif
			)
{
    FragInput input = UnpackVaryings(packedInput);
	float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);
	float3 positionWS = input.positionWS;

	SurfaceData surfaceData;
	BuiltinData builtinData;
	GetSurfaceAndBuiltinData(input, surfaceData, builtinData);

	BSDFData bsdfData = ConvertSurfaceDataToBSDFData(surfaceData);
	Coordinate coord = GetCoordinate(input.unPositionSS.xy, _ScreenSize.zw);
	PreLightData preLightData = GetPreLightData(V, positionWS, coord, bsdfData);

	ENCODE_INTO_GBUFFER(surfaceData, outGBuffer);
	ENCODE_BAKE_LIGHTING_INTO_GBUFFER(GetBakedDiffuseLigthing(preLightData, surfaceData, builtinData, bsdfData), outGBuffer);
	#ifdef VELOCITY_IN_GBUFFER
	ENCODE_VELOCITY_INTO_GBUFFER(builtinData.velocity, outGBuffer);
	#endif
}
