#if SHADERPASS != SHADERPASS_GBUFFER
#error SHADERPASS_is_not_correctly_define
#endif

void Frag(  PackedVaryings packedInput,
			OUTPUT_GBUFFER(outGBuffer)
            OUTPUT_GBUFFER_VELOCITY(outVelocityBuffer)
            #ifdef _DEPTHOFFSET_ON
            , float outputDepth : SV_Depth
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

    bool twoSided = false;
    // This will always produce the correct 'NdotV' value, but potentially
    // reduce the length of the normal at edges of geometry.
    float NdotV = GetShiftedNdotV(surfaceData.normalWS, V, twoSided);

    // Orthonormalize the basis vectors using the Gram-Schmidt process.
    surfaceData.normalWS  = normalize(surfaceData.normalWS);
    surfaceData.tangentWS = normalize(surfaceData.tangentWS - dot(surfaceData.tangentWS, surfaceData.normalWS));

    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(surfaceData);

	PreLightData preLightData = GetPreLightData(V, NdotV, posInput, bsdfData);

    float3 bakeDiffuseLighting = GetBakedDiffuseLigthing(surfaceData, builtinData, bsdfData, preLightData);

    ENCODE_INTO_GBUFFER(surfaceData, bakeDiffuseLighting, outGBuffer);
    ENCODE_VELOCITY_INTO_GBUFFER(builtinData.velocity, outVelocityBuffer);

#ifdef _DEPTHOFFSET_ON
    outputDepth = posInput.rawDepth;
#endif
}
