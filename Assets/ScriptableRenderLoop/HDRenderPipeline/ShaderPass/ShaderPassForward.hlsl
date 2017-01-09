#if SHADERPASS != SHADERPASS_FORWARD
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

    bool twoSided = false;
    // This will always produce the correct 'NdotV' value, but potentially
    // reduce the length of the normal at edges of geometry.
    float NdotV = GetShiftedNdotV(surfaceData.normalWS, V, twoSided);

    // Orthonormalize the basis vectors using the Gram-Schmidt process.
    surfaceData.normalWS  = normalize(surfaceData.normalWS);
    surfaceData.tangentWS = normalize(surfaceData.tangentWS - dot(surfaceData.tangentWS, surfaceData.normalWS));

    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(surfaceData);

	PreLightData preLightData = GetPreLightData(V, NdotV, posInput, bsdfData);

	float3 diffuseLighting;
	float3 specularLighting;
    float3 bakeDiffuseLighting = GetBakedDiffuseLigthing(surfaceData, builtinData, bsdfData, preLightData);
    LightLoop(V, posInput, preLightData, bsdfData, bakeDiffuseLighting, diffuseLighting, specularLighting);

    outColor = float4(diffuseLighting + specularLighting, builtinData.opacity);

#ifdef _DEPTHOFFSET_ON
    outputDepth = posInput.rawDepth;
#endif
}

