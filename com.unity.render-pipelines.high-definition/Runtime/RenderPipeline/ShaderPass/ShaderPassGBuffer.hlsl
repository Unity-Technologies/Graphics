#if SHADERPASS != SHADERPASS_GBUFFER
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

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

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/TessellationShare.hlsl"

#endif // TESSELLATION_ON

void Frag(  PackedVaryingsToPS packedInput,
            OUTPUT_GBUFFER(outGBuffer)
            #ifdef _DEPTHOFFSET_ON
            , out float outputDepth : SV_Depth
            #endif
//forest-begin: G-Buffer motion vectors
#if defined(HAS_VEGETATION_ANIM) && defined(GBUFFER_MOTION_VECTORS)
    #if   GBUFFERMATERIAL_COUNT == 4
			, out float2 velocityTexture : SV_Target4
    #elif GBUFFERMATERIAL_COUNT == 5
			, out float2 velocityTexture : SV_Target5
    #elif GBUFFERMATERIAL_COUNT == 6
			, out float2 velocityTexture : SV_Target6
    #endif          
#endif
//forest-end:
            )
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

//forest-begin: G-Buffer motion vectors
#if defined(HAS_VEGETATION_ANIM) && defined(GBUFFER_MOTION_VECTORS)
	float2 velocity = CalculateMotionVector(packedInput.vmesh.mvPositionCS, packedInput.vmesh.mvPrevPositionCS);

	// Convert from Clip space (-1..1) to NDC 0..1 space.
	// Note it doesn't mean we don't have negative value, we store negative or positive offset in NDC space.
	// Note: ((positionCS * 0.5 + 0.5) - (previousPositionCS * 0.5 + 0.5)) = (velocity * 0.5)
	float4 packedVelocity;
	EncodeMotionVector(velocity * 0.5f, packedVelocity);

	// Note: unity_MotionVectorsParams.y is 0 is forceNoMotion is enabled
	// unity_MotionVectorsParams is not setup for G-Buffer pass, though, so we're saved by the fact that we don't use it.

	velocityTexture = packedVelocity.xy;
#endif
//forest-end:

#ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
#else
    // Unused
    float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
#endif

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    ENCODE_INTO_GBUFFER(surfaceData, builtinData, posInput.positionSS, outGBuffer);

#ifdef _DEPTHOFFSET_ON
    outputDepth = posInput.deviceDepth;
#endif
}
