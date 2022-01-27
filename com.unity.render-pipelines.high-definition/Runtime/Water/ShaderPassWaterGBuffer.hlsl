#if SHADERPASS != SHADERPASS_GBUFFER
#error SHADERPASS_is_not_correctly_define
#endif
// Given that the geometry is either procedural or a mesh given by the user, we only need this as an input
struct VaryingsToPS
{
    VaryingsMeshToPS vmesh;
};

struct PackedVaryingsToPS
{
    PackedVaryingsMeshToPS vmesh;

    // SGVs must be packed after all non-SGVs have been packed.
    // If there are several SGVs, they are packed in the order of HLSL declaration.

    UNITY_VERTEX_OUTPUT_STEREO

#if defined(PLATFORM_SUPPORTS_PRIMITIVE_ID_IN_PIXEL_SHADER) && SHADER_STAGE_FRAGMENT
#if (defined(VARYINGS_NEED_PRIMITIVEID) || (SHADERPASS == SHADERPASS_FULL_SCREEN_DEBUG))
    uint primitiveID : SV_PrimitiveID;
#endif
#endif

#if defined(VARYINGS_NEED_CULLFACE) && SHADER_STAGE_FRAGMENT
    FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryingsToPS PackVaryingsToPS(VaryingsToPS input)
{
    PackedVaryingsToPS output;
    output.vmesh = PackVaryingsMeshToPS(input.vmesh);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    return output;
}

// We only need the tessellation version
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/WaterVertexTessellation.hlsl"

FragInputs UnpackVaryingsToFragInputs(PackedVaryingsToPS packedInput)
{
    FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);

#if defined(PLATFORM_SUPPORTS_PRIMITIVE_ID_IN_PIXEL_SHADER) && SHADER_STAGE_FRAGMENT
#if (defined(VARYINGS_NEED_PRIMITIVEID) || (SHADERPASS == SHADERPASS_FULL_SCREEN_DEBUG))
    input.primitiveID = packedInput.primitiveID;
#endif
#endif

#if defined(VARYINGS_NEED_CULLFACE) && SHADER_STAGE_FRAGMENT
    input.isFrontFace = IS_FRONT_VFACE(packedInput.cullFace, true, false);
#endif

    return input;
}

void Frag(PackedVaryingsToPS packedInput,
    out uint2 outGBuffer0 : SV_Target0,
    out uint4 outGBuffer1 : SV_Target1,
    out uint2 outGBuffer2 : SV_Target2)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsToFragInputs(packedInput);

    // Compute the tile index
    uint2 tileIndex = uint2(input.positionSS.xy) / GetTileSize();

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS.xyz, tileIndex);

#ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
#else
    // Unused
    float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
#endif

    // Get the surface and built in data
    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    // Light layers need to be set manually here as there is no mesh renderer
    builtinData.renderingLayers = DEFAULT_LIGHT_LAYERS;

    // We manually set the ambient probe as this is not a mesh renderer
    builtinData.bakeDiffuseLighting = _WaterAmbientProbe.xyz;

    // Compute the BSDF Data
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);

    // Encode the water into the gbuffer
    EncodeIntoGBuffer(bsdfData, builtinData, posInput.positionSS, outGBuffer0, outGBuffer1, outGBuffer2);
}
