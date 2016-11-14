#if SHADERPASS != SHADERPASS_GBUFFER
#error SHADERPASS_is_not_correctly_define
#endif

void Frag(  PackedVaryings packedInput,
			OUTPUT_GBUFFER(outGBuffer)
            OUTPUT_GBUFFER_VELOCITY(outVelocityBuffer)
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
    float3 bakeDiffuseLighting = GetBakedDiffuseLigthing(surfaceData, builtinData, bsdfData, preLightData);

	ENCODE_INTO_GBUFFER(surfaceData, bakeDiffuseLighting, outGBuffer);
    ENCODE_VELOCITY_INTO_GBUFFER(builtinData.velocity, outVelocityBuffer);
}
