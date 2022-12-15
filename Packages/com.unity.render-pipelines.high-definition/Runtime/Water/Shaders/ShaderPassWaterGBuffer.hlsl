#if SHADERPASS != SHADERPASS_GBUFFER
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/ShaderPassWaterCommon.hlsl"

void Frag(PackedVaryingsToPS packedInput,
    out float4 outGBuffer0 : SV_Target0,
    out float4 outGBuffer1 : SV_Target1,
    out float4 outGBuffer2 : SV_Target2,
    out float4 outGBuffer3 : SV_Target3)
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
    builtinData.renderingLayers = RENDERING_LAYERS_MASK;

    // The indirect diffuse term will be injected in the lighting pass
    builtinData.bakeDiffuseLighting = 0.0;

    // Compute the BSDF Data
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);

    // If the camera is in the underwater region of this surface and the the camera is under the surface
#if defined(SHADER_STAGE_FRAGMENT)
    bsdfData.frontFace = packedInput.cullFace;
#endif

    // If we are on a back face, we need to flip the normals
    if (!bsdfData.frontFace)
    {
        // For now we simply flip the normals and kill the caustics
        bsdfData.normalWS = -bsdfData.normalWS;
        bsdfData.lowFrequencyNormalWS = -bsdfData.lowFrequencyNormalWS;
    }

    // Encode the water into the gbuffer
    EncodeIntoGBuffer(bsdfData, builtinData, posInput.positionSS, outGBuffer0, outGBuffer1, outGBuffer2, outGBuffer3);
}
